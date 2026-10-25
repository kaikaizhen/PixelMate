using System.Threading.RateLimiting;
using PixelMate.Api.Realtime;
using PixelMate.Api.Rooms;
using PixelMate.Api.Social;
using PixelMate.Api.Weather;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Auth;
using PixelMate.Api.Data;
using PixelMate.Api.Endpoints;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Items;
using PixelMate.Api.Mail;
using PixelMate.Api.Pets;
using Scalar.AspNetCore;
using Serilog;

// Local settings live in apps/api/.env (and .env.local, which wins). Real environment variables always win over both.
// Both files are git-ignored; production passes real environment variables and has no .env.
if (Environment.GetEnvironmentVariable("PIXELMATE_SKIP_DOTENV") != "1")     // tests must not pick up a developer's own files
{
    foreach (var file in new[] { ".env.local", ".env" })
    {
        DotNetEnv.Env.NoClobber().TraversePath().Load(file);
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ---- configuration: secrets come from user-secrets (dev) or environment variables (prod); never from the repo.
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=pixelmate;Uid=unset;Pwd=unset";

// No EnableRetryOnFailure: it forbids the explicit transactions that gifts, pairing and the room editor use (EF throws on BeginTransaction).
// Conflicts are handled where they happen (revision / unique keys); a dropped connection simply fails the request and the app retries.
builder.Services.AddSingleton<OutboxSignal>();
builder.Services.AddSingleton<PokeInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) =>
    o.AddInterceptors(sp.GetRequiredService<PokeInterceptor>()).UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11, 8)),
            my => my.CommandTimeout(15))
        .UseSnakeCaseNamingConvention());

// ---- identity (cookie sessions) -------------------------------------------------------------------------------
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 10;
        o.Password.RequireDigit = true;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.Lockout.AllowedForNewUsers = true;
        o.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// A password reset rotates the security stamp; other sessions are dropped at the next check (default every 60 s).
builder.Services.Configure<SecurityStampValidatorOptions>(o =>
    o.ValidationInterval = TimeSpan.FromSeconds(builder.Configuration.GetValue("Auth:SecurityStampValidationSeconds", 60)));
builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(1));
builder.Services.AddDataProtection().SetApplicationName("PixelMate").PersistKeysToDbContext<AppDbContext>();   // keys survive restarts

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = ".pixelmate.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Configuration.GetValue("Auth:SecureCookie", true) ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => ErrorHandling.WriteAsync(ctx.HttpContext, 401, ErrorCodes.AuthRequired, "請先登入。");
    o.Events.OnRedirectToAccessDenied = ctx => ErrorHandling.WriteAsync(ctx.HttpContext, 403, ErrorCodes.NotOwner, "沒有權限執行這個操作。");
});
builder.Services.AddAuthorization();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<MailOptions>(builder.Configuration.GetSection("Mail"));
builder.Services.AddScoped<IMailSender>(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MailOptions>>().Value.IsConfigured
        ? ActivatorUtilities.CreateInstance<SmtpMailSender>(sp)
        : ActivatorUtilities.CreateInstance<LogMailSender>(sp));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHostedService<ItemSeedHostedService>();
builder.Services.Configure<PixelMate.Api.Life.LifeOptions>(builder.Configuration.GetSection("Life"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PixelMate.Api.Life.LifeService>();
builder.Services.AddHostedService<PixelMate.Api.Life.LifeWorker>();
builder.Services.AddSignalR();

// ---- weather: CWA (Taiwan) + Open-Meteo, shared through HybridCache (memory + Redis when configured) ----------------------------------
var weatherOptions = builder.Configuration.GetSection("Weather").Get<WeatherOptions>() ?? new WeatherOptions();
builder.Services.Configure<WeatherOptions>(builder.Configuration.GetSection("Weather"));
var redis = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redis))
{
    builder.Services.AddStackExchangeRedisCache(o => { o.Configuration = redis; o.InstanceName = "pixelmate:"; });   // the level-2 cache; if Redis is down the caches fall back to memory
}
builder.Services.AddHybridCache();
builder.Services.AddHttpClient("openmeteo", c => { c.BaseAddress = new Uri(weatherOptions.OpenMeteoBaseUrl); c.Timeout = TimeSpan.FromSeconds(weatherOptions.TimeoutSeconds); });
builder.Services.AddHttpClient("cwa", c => { c.BaseAddress = new Uri(weatherOptions.CwaBaseUrl); c.Timeout = TimeSpan.FromSeconds(weatherOptions.TimeoutSeconds); });
builder.Services.AddSingleton<OpenMeteoClient>();
builder.Services.AddSingleton<CwaClient>();
builder.Services.AddSingleton<WeatherService>();
builder.Services.Configure<RealtimeOptions>(builder.Configuration.GetSection("Realtime"));
builder.Services.AddScoped<EventOutbox>();
builder.Services.AddSingleton<OutboxDispatcher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcher>());
builder.Services.Configure<PixelMate.Api.Social.LetterOptions>(builder.Configuration.GetSection("Letters"));

builder.Services.AddOpenApi();

builder.Services.AddCors(o => o.AddPolicy("web", p =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
    {
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();   // exact origins only, never "*" with credentials
    }
}));

static string ClientIp(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

builder.Services.AddRateLimiter(o =>
{
    var permit = builder.Configuration.GetValue("RateLimit:PermitPerMinute", 120);
    var authPermit = builder.Configuration.GetValue("RateLimit:AuthPerMinute", 10);
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter("auth:" + ClientIp(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    var pairingPermit = builder.Configuration.GetValue("RateLimit:PairingPerMinute", 10);   // guessing codes / sending letters
    o.AddPolicy("pairing", ctx => RateLimitPartition.GetFixedWindowLimiter("pairing:" + ClientIp(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = pairingPermit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = (rc, _) => new ValueTask(ErrorHandling.WriteAsync(rc.HttpContext, StatusCodes.Status429TooManyRequests,
        ErrorCodes.RateLimited, "請求太頻繁，請稍後再試。"));
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();                 // the API only listens behind our own nginx container
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<RequestIdMiddleware>();
app.UseSerilogRequestLogging();                // logs method + path + status only (no bodies, no query strings with tokens)
app.UseExceptionHandler(errorApp => errorApp.Run(ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    return ex is null ? Task.CompletedTask : ErrorHandling.HandleAsync(ctx, ex, ctx.RequestServices.GetRequiredService<ILogger<Program>>());
}));
app.UseStatusCodePages(ErrorHandling.StatusPageAsync);
app.UseRateLimiter();
app.UseCors("web");
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api/v1");
api.MapHealth();
api.MapAuth();
api.MapPets();
api.MapItems();
api.MapPairing();
api.MapRoom();
api.MapEvents();
api.MapWeather();
api.MapMail();
if (app.Environment.IsDevelopment())
{
    api.MapDev();      // test helpers; never mapped outside Development
    api.MapItemDev();
}

app.Run();

public partial class Program;   // for WebApplicationFactory in tests
