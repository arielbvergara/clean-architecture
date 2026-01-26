using Domain.Constants;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using WebAPI.Authorization;

namespace WebAPI.Authentication;

/// <summary>
/// Firebase-based implementation of <see cref="IFirebaseAdminClient"/>.
///
/// This implementation expects service account credentials to be provided via the
/// standard Firebase Admin SDK mechanism (for example, GOOGLE_APPLICATION_CREDENTIALS
/// environment variable). It will ensure that the admin user exists and has a
/// custom claim "role" with the value "admin" so that JwtAuthenticationExtensions
/// can surface it as an ASP.NET Core role.
/// </summary>
public sealed class FirebaseAdminClient : IFirebaseAdminClient
{
    private readonly FirebaseAuth _auth;

    public FirebaseAdminClient()
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            // Use Application Default Credentials (ADC) so that sensitive service account
            // keys are not stored in source control. In development and deployment
            // environments, configure GOOGLE_APPLICATION_CREDENTIALS or a platform-
            // specific identity (e.g., workload identity) to supply credentials.
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault()
            });
        }

        _auth = FirebaseAuth.DefaultInstance;
    }

    public async Task<string> EnsureAdminUserAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or whitespace.", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
        }

        UserRecord? userRecord;

        try
        {
            // Firebase Admin SDK does not currently expose cancellation tokens on these APIs.
            userRecord = await _auth.GetUserByEmailAsync(email).ConfigureAwait(false);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            var args = new UserRecordArgs
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                EmailVerified = true,
            };

            userRecord = await _auth.CreateUserAsync(args).ConfigureAwait(false);
        }

        if (userRecord is null)
        {
            throw new InvalidOperationException("Failed to obtain or create Firebase admin user.");
        }

        var existingClaims = userRecord.CustomClaims is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(userRecord.CustomClaims);

        if (!existingClaims.TryGetValue(AuthorizationConstants.RoleClaimKey, out var roleValue) ||
            !string.Equals(roleValue?.ToString(), UserRoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
        {
            existingClaims[AuthorizationConstants.RoleClaimKey] = UserRoleConstants.Admin;
            await _auth.SetCustomUserClaimsAsync(userRecord.Uid, existingClaims, cancellationToken).ConfigureAwait(false);
        }

        return userRecord.Uid;
    }
}
