namespace Application.Interfaces;

/// <summary>
/// Abstraction for identity provider operations required by the application.
/// </summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Ensures that a user with the specified email exists in the identity provider
    /// and has administrator privileges.
    /// </summary>
    /// <param name="email">Email address for the admin user.</param>
    /// <param name="password">Initial password for the admin user.</param>
    /// <param name="displayName">Display name for the admin user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The external authentication identifier (e.g. Firebase UID).</returns>
    Task<string> EnsureAdminUserAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);
}
