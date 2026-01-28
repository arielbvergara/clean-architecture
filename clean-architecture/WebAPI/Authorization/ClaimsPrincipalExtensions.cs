using System.Security.Claims;
using Domain.Constants;

using Application.Dtos.User;
using Domain.Constants;

namespace WebAPI.Authorization;

/// <summary>
/// Extension methods for working with <see cref="ClaimsPrincipal"/> in the WebAPI layer.
///
/// These helpers centralize logic for resolving the external authentication identifier
/// (e.g. Firebase UID) and determining whether a principal has administrative privileges,
/// and for projecting the principal into an application-friendly <see cref="CurrentUserContext"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns <c>true</c> if the principal should be treated as an administrator.
    /// </summary>
    /// <remarks>
    /// Supports both the standard role-based approach (using <see cref="ClaimsPrincipal.IsInRole"/>)
    /// and the Firebase-style custom <c>role</c> claim so that tests (which use
    /// <see cref="System.Security.Claims.ClaimTypes.Role"/>) and production tokens
    /// (which use "role") are both recognized.
    /// </remarks>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Standard ASP.NET Core role checks (used heavily in tests).
        if (user.IsInRole(UserRoleConstants.Admin))
        {
            return true;
        }

        // Support both Firebase-style "role" and the standard ClaimTypes.Role mapping
        // that many middleware components (including tests) rely on.
        var roleClaim = user.FindFirst(AuthorizationConstants.RoleClaimKey)?.Value
                        ?? user.FindFirst(ClaimTypes.Role)?.Value;

        return string.Equals(roleClaim, UserRoleConstants.Admin, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the external authentication identifier for the principal.
    /// </summary>
    /// <remarks>
    /// Prefers the OpenID Connect <c>sub</c> claim (the stable subject identifier),
    /// but will fall back to <see cref="ClaimTypes.NameIdentifier"/> when present.
    /// </remarks>
    public static string? GetExternalAuthId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Prefer OpenID Connect 'sub' claim, fall back to NameIdentifier if present.
        return user.FindFirst(AuthorizationConstants.SubjectClaimType)?.Value
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Projects the principal into an application-layer <see cref="CurrentUserContext"/>.
    /// </summary>
    /// <remarks>
    /// If the principal does not contain a parseable user identifier, this method returns <c>null</c>.
    /// </remarks>
    public static CurrentUserContext? ToCurrentUserContext(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var externalAuthId = user.GetExternalAuthId();
        if (string.IsNullOrWhiteSpace(externalAuthId))
        {
            return null;
        }

        var role = user.IsAdmin() ? UserRoleConstants.Admin : UserRoleConstants.User;

        return new CurrentUserContext(externalAuthId, role);
    }
}
