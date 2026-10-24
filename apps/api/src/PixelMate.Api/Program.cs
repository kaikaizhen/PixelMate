using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;
using PixelMate.Api.Endpoints;
using PixelMate.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- configuration: secrets come from user-secrets (dev) or environment variables (prod); never from the repo.
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=pixelmate;Uid=unset;Pwd=unset";

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11, 8)),
        my => my.EnableRetryOnFailure(3).CommandTimeout(15)));

builder.Services.AddOpenApi();

builder.Services.AddCors(o => o.AddPolicy("web", p =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
    {
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();   // exact origins only, never "*" with credentials
    }
}));

builder.Services.AddRateLimiter(o =>
{
    var permit = builder.Configuration.GetValue("RateLimit:PermitPerMinute", 120);
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
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
app.UseExceptionHandler(errorApp => errorApp.Run(ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    return ex is null ? Task.CompletedTask : ErrorHandling.HandleAsync(ctx, ex, ctx.RequestServices.GetRequiredService<ILogger<Program>>());
}));
app.UseStatusCodePages(ErrorHandling.StatusPageAsync);
app.UseRateLimiter();
app.UseCors("web");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGroup("/api/v1").MapHealth();

app.Run();

public partial class Program;   // for WebApplicationFactory in tests
