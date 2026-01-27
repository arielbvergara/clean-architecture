namespace WebAPI.RateLimiting;

/// <summary>
/// Centralizes the names of rate limiting policies used by the WebAPI.
/// Keeping policy names in one place avoids typos between configuration
/// and endpoint attributes and makes security reviews easier.
/// </summary>
public static class RateLimitingPolicies
{
    public const string Fixed = "fixed";
    public const string Strict = "strict";
}
