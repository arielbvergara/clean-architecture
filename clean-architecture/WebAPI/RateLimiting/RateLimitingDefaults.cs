using System;

namespace WebAPI.RateLimiting;

/// <summary>
/// Centralizes numeric and key defaults for rate limiting policies to avoid
/// magic numbers and strings scattered throughout the codebase.
/// </summary>
public static class RateLimitingDefaults
{
    public const int FixedPermitLimit = 100;
    public const int FixedQueueLimit = 2;

    public const int StrictPermitLimit = 10;
    public const int StrictQueueLimit = 0;

    public static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan StrictWindow = TimeSpan.FromMinutes(1);

    public const string UnknownAuthenticatedUserPartitionKey = "unknown_authenticated_user";
    public const string UnknownAnonymousPartitionKey = "unknown_anonymous";
}
