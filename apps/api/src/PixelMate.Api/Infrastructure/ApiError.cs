namespace PixelMate.Api.Infrastructure;

/// <summary>Single source of truth for API error codes (docs/後端.md §4, docs/API契約.md §9).</summary>
public static class ErrorCodes
{
    public const string AuthRequired = "AUTH_REQUIRED";
    public const string NotOwner = "NOT_OWNER";
    public const string NotFound = "NOT_FOUND";
    public const string InsufficientPoints = "INSUFFICIENT_POINTS";
    public const string InvalidState = "INVALID_STATE";
    public const string AlreadyProcessed = "ALREADY_PROCESSED";
    public const string NotConnected = "NOT_CONNECTED";
    public const string RateLimited = "RATE_LIMITED";
    public const string PetAlreadyExists = "PET_ALREADY_EXISTS";
    public const string RevisionConflict = "REVISION_CONFLICT";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string Internal = "INTERNAL_ERROR";
}

/// <summary>Error body returned by every failing request: <c>{ code, message, requestId }</c>.</summary>
public sealed record ApiError(string Code, string Message, string RequestId);

/// <summary>Thrown by application services; the middleware turns it into <see cref="ApiError"/> + HTTP status.</summary>
public sealed class ApiException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
