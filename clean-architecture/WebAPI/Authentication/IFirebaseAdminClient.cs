using Application.Interfaces;

namespace WebAPI.Authentication;

/// <summary>
/// Abstraction over Firebase Admin operations required by the WebAPI host.
///
/// This interface is intentionally defined in the WebAPI layer so that the identity
/// provider implementation (Firebase, Entra ID, etc.) can change without impacting
/// Domain or Application layers.
/// </summary>
public interface IFirebaseAdminClient : IIdentityProviderService
{
}
