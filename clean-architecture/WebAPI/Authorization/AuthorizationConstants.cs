namespace WebAPI.Authorization;

/// <summary>
/// Centralizes claim type and role name constants used by the WebAPI layer
/// to avoid magic strings in authentication and authorization logic.
/// </summary>
public static class AuthorizationConstants
{
    /// <summary>
    /// Custom claim type key used by Firebase tokens to carry the user's role.
    /// </summary>
    public const string RoleClaimKey = "role";

    /// <summary>
    /// OpenID Connect subject claim type used as the stable external identifier.
    /// </summary>
    public const string SubjectClaimType = "sub";
}
