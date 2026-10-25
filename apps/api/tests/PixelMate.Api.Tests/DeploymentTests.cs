using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;

namespace PixelMate.Api.Tests;

/// <summary>A deployment replaces the API container. Players who are signed in must stay signed in.</summary>
public class DeploymentTests
{
    private static async Task<string> SignIn(AuthFactory f, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        var res = await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return string.Join("; ", res.Headers.GetValues("Set-Cookie").Select(v => v.Split(';')[0]));
    }

    private static async Task<HttpStatusCode> Me(AuthFactory f, string cookie)
    {
        var c = f.NewClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Add("Cookie", cookie);
        return (await c.SendAsync(req)).StatusCode;
    }

    [Fact]
    public async Task A_player_signed_in_before_a_restart_is_still_signed_in_on_the_new_instance()
    {
        var db = Guid.NewGuid().ToString("N");
        using var keepAlive = new Microsoft.Data.Sqlite.SqliteConnection($"DataSource=file:pm_{db}?mode=memory&cache=shared");   // the database outlives both instances, like MariaDB
        keepAlive.Open();
        string cookie;
        using (var before = new AuthFactory { DbName = db })
        {
            cookie = await SignIn(before);
            Assert.Equal(HttpStatusCode.OK, await Me(before, cookie));
            using var scope = before.Services.CreateScope();
            Assert.True(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>().AnyAsync(),
                "the key ring that signs the cookie must live in the database, not in the container");
            GC.KeepAlive(before);
        }

        // the first instance is gone; a brand new one (new process, new key cache) starts on the same database
        using var after = new AuthFactory { DbName = db, Seed = false };
        after.NewClient();
        Assert.Equal(HttpStatusCode.OK, await Me(after, cookie));
    }

    [Fact]
    public async Task A_cookie_from_another_installation_is_not_accepted()
    {
        using var a = new AuthFactory();
        var cookie = await SignIn(a);
        using var other = new AuthFactory();                              // a different database: different keys, different players
        other.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, await Me(other, cookie));
    }

    [Fact]
    public void The_cookie_keys_are_tied_to_a_fixed_application_name()
    {
        // A container's path or image can change with every deployment; the key ring is shared by name, so the name must not depend on them.
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "apps", "api", "src", "PixelMate.Api", "Program.cs"));
        Assert.Contains("SetApplicationName(\"PixelMate\")", program);
        Assert.Contains("PersistKeysToDbContext<AppDbContext>", program);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "apps", "api", "src", "PixelMate.Api", "Program.cs"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repository root not found");
    }
}
