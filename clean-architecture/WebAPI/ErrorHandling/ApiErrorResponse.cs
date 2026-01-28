namespace WebAPI.ErrorHandling;

/// <summary>
/// Standard API error envelope based on RFC 7807 Problem Details semantics.
/// This shape is used for both client and server errors to avoid ad-hoc
/// anonymous error objects and to keep internal exception details server-side.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// HTTP status code of the error response.
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// A URI reference that identifies the problem type.
    /// </summary>
    public string Type { get; init; } = ErrorResponseTypes.GenericErrorType;

    /// <summary>
    /// A short, human-readable summary of the problem type.
    /// </summary>
    public string Title { get; init; } = ErrorResponseTitles.GenericErrorTitle;

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// For security reasons, this should not contain sensitive implementation details
    /// for server-side or infrastructure errors.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem.
    /// When not set explicitly, this may be derived from the request path.
    /// </summary>
    public string? Instance { get; init; }

    /// <summary>
    /// Correlation identifier associated with the current request/response cycle.
    /// This is intended for log and trace correlation and is safe to include in
    /// client-facing payloads.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Additional, non-standard properties that provide more detail about the
    /// error. This can be used to attach domain-specific metadata while keeping
    /// the core fields consistent.
    /// </summary>
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>();
}

/// <summary>
/// Specialized envelope for validation errors that need to carry per-field
/// error information in addition to the standard problem details fields.
/// </summary>
public sealed class ApiValidationErrorResponse : ApiErrorResponse
{
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}

/// <summary>
/// Centralizes well-known problem type URIs to avoid magic strings scattered
/// across controllers and filters.
/// </summary>
public static class ErrorResponseTypes
{
    public const string ValidationErrorType = "https://httpstatuses.io/400/validation-error";
    public const string NotFoundErrorType = "https://httpstatuses.io/404/resource-not-found";
    public const string ConflictErrorType = "https://httpstatuses.io/409/conflict";
    public const string InfrastructureErrorType = "https://httpstatuses.io/500/infrastructure-error";
    public const string GenericErrorType = "https://httpstatuses.io/500/generic-error";
}

/// <summary>
/// Centralizes default titles used for different categories of errors so they
/// can be audited and updated without touching controller code.
/// </summary>
public static class ErrorResponseTitles
{
    public const string ValidationErrorTitle = "Validation error";
    public const string NotFoundErrorTitle = "Resource not found";
    public const string ConflictErrorTitle = "Conflict";
    public const string InfrastructureErrorTitle = "Infrastructure error";
    public const string GenericErrorTitle = "Unexpected error";
}
