namespace WebAPI.Authorization;

/// <summary>
/// Centralizes the names of authorization policies used by the WebAPI.
/// </summary>
public static class AuthorizationPoliciesConstants
{
    public const string AdminOnly = "AdminOnly";
    public const string User = "User";
    public const string OwnsUser = "OwnsUser";
}
