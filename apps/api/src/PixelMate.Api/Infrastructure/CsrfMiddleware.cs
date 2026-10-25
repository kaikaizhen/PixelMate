namespace PixelMate.Api.Infrastructure;

/// <summary>
/// CSRF defence for cookie sessions: every state-changing request must carry <c>X-Requested-With: pixelmate</c>.
/// A cross-site form or image cannot add a custom header, and a cross-origin script needs a CORS preflight that
/// only our exact allowed origins pass.
/// </summary>
public sealed class CsrfMiddleware(RequestDelegate next)
{
    public const string Header = "X-Requested-With";
    public const string Value = "pixelmate";

    /// <summary>The marker may arrive twice (the SignalR client adds its headers in two places), as separate values or joined by a comma.</summary>
    private static bool HasMarker(HttpContext ctx) =>
        ctx.Request.Headers[Header].SelectMany(v => (v ?? "").Split(',')).Any(v => string.Equals(v.Trim(), Value, StringComparison.Ordinal));

    public Task InvokeAsync(HttpContext ctx)
    {
        var unsafeMethod = !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method)
                           && !HttpMethods.IsOptions(ctx.Request.Method);
        if (unsafeMethod && ctx.Request.Path.StartsWithSegments("/api")
            && !ctx.Request.Path.StartsWithSegments("/api/v1/device")        // devices send a Bearer credential, not a cookie: nothing for a cross-site page to ride on
            && !HasMarker(ctx))
        {
            return ErrorHandling.WriteAsync(ctx, StatusCodes.Status403Forbidden, ErrorCodes.CsrfRejected, "請求來源不被允許。");
        }
        return next(ctx);
    }
}
