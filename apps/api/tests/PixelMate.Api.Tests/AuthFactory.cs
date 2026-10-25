using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PixelMate.Api.Data;
using PixelMate.Api.Mail;

namespace PixelMate.Api.Tests;

public sealed record SentMail(string To, string Subject, string Text, string Html = "", int ImageCount = 0)
{
    public string Link => Regex.Match(Text, @"https?://\S+").Value;
    public string Query(string key) => System.Web.HttpUtility.ParseQueryString(new Uri(Link).Query)[key]!;
}

public sealed class FakeMailSender : IMailSender
{
    public ConcurrentQueue<SentMail> Sent { get; } = new();

    public Task SendAsync(string toAddress, MailContent content, CancellationToken ct = default)
    {
        Sent.Enqueue(new SentMail(toAddress, content.Subject, content.Text, content.Html, content.Images.Count));
        return Task.CompletedTask;
    }
}

/// <summary>Real pipeline + real Identity, but an in-memory SQLite database and a mail sender that records messages.</summary>
/// <summary>A clock tests can move forward.</summary>
public sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}

public sealed class AuthFactory(IDictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _db = new("DataSource=:memory:");
    public FakeMailSender Mail { get; } = new();
    public FakeClock Clock { get; } = new(DateTimeOffset.UtcNow);
    public bool Seed { get; init; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _db.Open();
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            var s = new Dictionary<string, string?> { ["Auth:SecureCookie"] = "false", ["Auth:SecurityStampValidationSeconds"] = "0", ["RateLimit:AuthPerMinute"] = "1000", ["Life:WorkerIntervalSeconds"] = "0", ["Realtime:DispatchIntervalSeconds"] = "0" };
            foreach (var kv in settings ?? new Dictionary<string, string?>())
            {
                s[kv.Key] = kv.Value;
            }
            cfg.AddInMemoryCollection(s);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();   // the MySQL options action registered by Program
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_db).UseSnakeCaseNamingConvention());
            services.RemoveAll<IMailSender>();
            services.AddSingleton<IMailSender>(Mail);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public HttpClient NewClient()
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.EnsureCreated() && Seed)            // first call on this factory: build the schema and the item catalogue
            {
                using var manifest = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Seed", "manifest.json"));
                PixelMate.Api.Items.ItemCatalogSeeder.SeedAsync(db, manifest).GetAwaiter().GetResult();
            }
        }
        var c = CreateClient();
        c.DefaultRequestHeaders.Add("X-Requested-With", "pixelmate");
        return c;
    }

    public static Task<HttpResponseMessage> Register(HttpClient c, string email = "a@example.com", string password = "Passw0rd-123", string name = "小栗") =>
        c.PostAsJsonAsync("/api/v1/auth/register", new { email, password, displayName = name });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _db.Dispose();
    }
}
