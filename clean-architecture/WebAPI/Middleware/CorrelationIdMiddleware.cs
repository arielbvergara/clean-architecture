using Microsoft.Extensions.Primitives;

namespace WebAPI.Middleware;

/// <summary>
/// Adds and propagates a correlation identifier for each HTTP request so that
/// logs and error responses can be tied together without exposing internals.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers);

        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   { CorrelationIdDefaults.CorrelationIdLogScopeKey, correlationId }
               }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(CorrelationIdDefaults.CorrelationIdHeaderName, out StringValues values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            return values.ToString();
        }

        return Guid.NewGuid().ToString("D");
    }
}

/// <summary>
/// Centralizes correlation id–related constants to avoid magic strings and to
/// make it easy to audit how correlation is handled.
/// </summary>
public static class CorrelationIdDefaults
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdLogScopeKey = "CorrelationId";
}
