namespace WebAPI.Authentication;

/// <summary>
/// Performs one-time and idempotent bootstrap actions for administrator users.
/// </summary>
public interface IAdminUserBootstrapper
{
    /// <summary>
    /// Ensures the initial administrator user exists in both the identity provider
    /// and the application database.
    /// </summary>
    Task SeedAdminUserAsync(CancellationToken cancellationToken = default);
}
