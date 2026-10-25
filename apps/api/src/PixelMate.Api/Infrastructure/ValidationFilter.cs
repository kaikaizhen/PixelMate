using FluentValidation;

namespace PixelMate.Api.Infrastructure;

/// <summary>Runs the FluentValidation validator for the request body and answers 422 with per-field messages.</summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var body = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (body is null)
        {
            return Results.Json(new ApiError(ErrorCodes.ValidationFailed, "請求內容缺少或格式錯誤。", ctx.HttpContext.TraceIdentifier),
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var result = await ctx.HttpContext.RequestServices.GetRequiredService<IValidator<T>>().ValidateAsync(body, ctx.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return await next(ctx);
        }

        var errors = result.Errors.GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        return Results.Json(new ApiError(ErrorCodes.ValidationFailed, "請檢查輸入的內容。", ctx.HttpContext.TraceIdentifier, errors),
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder Validate<T>(this RouteHandlerBuilder b) where T : class => b.AddEndpointFilter<ValidationFilter<T>>();
}
