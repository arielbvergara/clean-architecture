using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

/// <summary>
/// Authorization requirement that enforces that the current principal either owns
/// the target user resource or has administrative privileges.
/// </summary>
public sealed class OwnsUserRequirement : IAuthorizationRequirement
{
}
