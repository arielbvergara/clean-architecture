using Application.Exceptions;
using Domain.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.ErrorHandling;

/// <summary>
/// Central place for mapping application-layer results and exceptions into
/// standardized HTTP responses using <see cref="ApiErrorResponse"/>.
/// </summary>
public static class ErrorResponseMapper
{
    /// <summary>
    /// Maps an <see cref="AppException"/> into a standardized error response.
    /// </summary>
    public static IActionResult ToActionResult(
        this ControllerBase controller,
        AppException error,
        string? correlationId = null)
    {
        var statusCode = GetStatusCode(error);
        var instance = controller.HttpContext?.Request.Path.HasValue == true
            ? controller.HttpContext.Request.Path.Value
            : null;

        ApiErrorResponse response = error switch
        {
            ValidationException validationError => new ApiValidationErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Type = ErrorResponseTypes.ValidationErrorType,
                Title = ErrorResponseTitles.ValidationErrorTitle,
                Detail = validationError.Message,
                Instance = instance,
                CorrelationId = correlationId,
                Errors = new Dictionary<string, string[]>(validationError.Errors)
            },
            NotFoundException notFoundError => new ApiErrorResponse
            {
                Status = StatusCodes.Status404NotFound,
                Type = ErrorResponseTypes.NotFoundErrorType,
                Title = ErrorResponseTitles.NotFoundErrorTitle,
                Detail = notFoundError.Message,
                Instance = instance,
                CorrelationId = correlationId
            },
            ConflictException conflictError => new ApiErrorResponse
            {
                Status = StatusCodes.Status409Conflict,
                Type = ErrorResponseTypes.ConflictErrorType,
                Title = ErrorResponseTitles.ConflictErrorTitle,
                Detail = conflictError.Message,
                Instance = instance,
                CorrelationId = correlationId
            },
            InfraException => new ApiErrorResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = ErrorResponseTypes.InfrastructureErrorType,
                Title = ErrorResponseTitles.InfrastructureErrorTitle,
                Detail = GenericClientSafeServerErrorDetail,
                Instance = instance,
                CorrelationId = correlationId
            },
            _ => new ApiErrorResponse
            {
                Status = statusCode,
                Type = ErrorResponseTypes.GenericErrorType,
                Title = ErrorResponseTitles.GenericErrorTitle,
                Detail = statusCode >= StatusCodes.Status500InternalServerError
                    ? GenericClientSafeServerErrorDetail
                    : error.Message,
                Instance = instance,
                CorrelationId = correlationId
            }
        };

        return controller.StatusCode(response.Status, response);
    }

    private static int GetStatusCode(AppException error) => error.Type switch
    {
        ExceptionType.Validation => StatusCodes.Status400BadRequest,
        ExceptionType.NotFound => StatusCodes.Status404NotFound,
        ExceptionType.Conflict => StatusCodes.Status409Conflict,
        ExceptionType.Infrastructure => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Generic, client-safe detail message used for server-side and infrastructure
    /// failures so we do not leak internal implementation details.
    /// </summary>
    public const string GenericClientSafeServerErrorDetail =
        "An unexpected error occurred while processing the request. Please try again later.";
}
