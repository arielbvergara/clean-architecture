using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

/// <summary>
/// Authorization requirement that enforces that the current principal is an
/// administrator for endpoints protected by the AdminOnly policy.
/// </summary>
public sealed class AdminOnlyRequirement : IAuthorizationRequirement
{
}
