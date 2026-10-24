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

    public Task InvokeAsync(HttpContext ctx)
    {
        var unsafeMethod = !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method)
                           && !HttpMethods.IsOptions(ctx.Request.Method);
        if (unsafeMethod && ctx.Request.Path.StartsWithSegments("/api")
            && !string.Equals(ctx.Request.Headers[Header], Value, StringComparison.Ordinal))
        {
            return ErrorHandling.WriteAsync(ctx, StatusCodes.Status403Forbidden, ErrorCodes.CsrfRejected, "請求來源不被允許。");
        }
        return next(ctx);
    }
}
