using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace PixelMate.Api.Infrastructure;

public static class ErrorHandling
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new ApiError(code, message, ctx.TraceIdentifier), Json));
    }

    /// <summary>Maps exceptions to the error envelope. Unknown exceptions never leak details to the client.</summary>
    public static async Task HandleAsync(HttpContext ctx, Exception ex, ILogger log)
    {
        if (ctx.Response.HasStarted)
        {
            return;
        }

        if (ex is ApiException api)
        {
            await WriteAsync(ctx, api.StatusCode, api.Code, api.Message);
            return;
        }

        log.LogError(ex, "Unhandled exception {ExceptionType}", ex.GetType().Name);   // no request bodies / tokens in logs
        await WriteAsync(ctx, StatusCodes.Status500InternalServerError, ErrorCodes.Internal, "伺服器發生錯誤，請稍後再試。");
    }

    /// <summary>Gives bodiless 401/403/404/405 responses the same envelope.</summary>
    public static Task StatusPageAsync(Microsoft.AspNetCore.Diagnostics.StatusCodeContext sc)
    {
        var ctx = sc.HttpContext;
        var (code, message) = ctx.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => (ErrorCodes.AuthRequired, "請先登入。"),
            StatusCodes.Status403Forbidden => (ErrorCodes.NotOwner, "沒有權限執行這個操作。"),
            StatusCodes.Status404NotFound => (ErrorCodes.NotFound, "找不到資源。"),
            StatusCodes.Status405MethodNotAllowed => (ErrorCodes.InvalidState, "不支援的操作。"),
            _ => (ErrorCodes.InvalidState, "請求無法處理。"),
        };
        return WriteAsync(ctx, ctx.Response.StatusCode, code, message);
    }
}
