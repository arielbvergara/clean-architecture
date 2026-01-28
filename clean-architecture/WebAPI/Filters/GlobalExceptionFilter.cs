using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebAPI.ErrorHandling;

namespace WebAPI.Filters;

public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var httpContext = context.HttpContext;
        var correlationId = httpContext.TraceIdentifier;
        var requestPath = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null;

        logger.LogError(
            context.Exception,
            "Unhandled exception while processing request {CorrelationId} {RequestPath}",
            correlationId,
            requestPath);

        var errorResponse = new ApiErrorResponse
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = ErrorResponseTypes.GenericErrorType,
            Title = ErrorResponseTitles.GenericErrorTitle,
            Detail = ErrorResponseMapper.GenericClientSafeServerErrorDetail,
            Instance = requestPath,
            CorrelationId = correlationId
        };

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
}
