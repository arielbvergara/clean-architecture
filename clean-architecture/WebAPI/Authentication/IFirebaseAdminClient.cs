namespace WebAPI.Authentication;

/// <summary>
/// Abstraction over Firebase Admin operations required by the WebAPI host.
///
/// This interface is intentionally defined in the WebAPI layer so that the identity
/// provider implementation (Firebase, Entra ID, etc.) can change without impacting
/// Domain or Application layers.
/// </summary>
public interface IFirebaseAdminClient
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
