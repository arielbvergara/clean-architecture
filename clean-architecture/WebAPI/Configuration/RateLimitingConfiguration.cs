using System.Threading.RateLimiting;
using WebAPI.RateLimiting;

namespace WebAPI.Configuration;

/// <summary>
/// Configures global rate limiting policies for the WebAPI.
///
/// Policies are partitioned by authenticated user identifier when available,
/// and by remote IP address (with safe fallbacks) when not.
/// </summary>
public static class RateLimitingConfiguration
{
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingPolicies.Fixed, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingDefaults.FixedPermitLimit,
                        Window = RateLimitingDefaults.FixedWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = RateLimitingDefaults.FixedQueueLimit
                    }));

            options.AddPolicy(RateLimitingPolicies.Strict, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingDefaults.StrictPermitLimit,
                        Window = RateLimitingDefaults.StrictWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = RateLimitingDefaults.StrictQueueLimit
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var nameIdentifier = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(nameIdentifier))
            {
                return nameIdentifier;
            }

            if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
            {
                return context.User.Identity.Name!;
            }

            return RateLimitingDefaults.UnknownAuthenticatedUserPartitionKey;
        }

        return context.Connection.RemoteIpAddress?.ToString()
               ?? RateLimitingDefaults.UnknownAnonymousPartitionKey;
    }
}
