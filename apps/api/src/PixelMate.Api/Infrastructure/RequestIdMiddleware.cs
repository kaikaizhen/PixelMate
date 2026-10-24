namespace PixelMate.Api.Infrastructure;

/// <summary>Gives every request a short id, echoes it in <c>X-Request-Id</c> and uses it as the log scope.</summary>
public sealed class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> log)
{
    public const string Header = "X-Request-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = $"req-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21];
        ctx.TraceIdentifier = id;
        ctx.Response.Headers[Header] = id;
        using (log.BeginScope(new Dictionary<string, object> { ["requestId"] = id }))
        {
            await next(ctx);
        }
    }
}
