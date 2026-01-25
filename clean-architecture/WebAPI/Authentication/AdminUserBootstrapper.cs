using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Microsoft.Extensions.Options;
using WebAPI.Configuration;

namespace WebAPI.Authentication;

/// <summary>
/// Default implementation of <see cref="IAdminUserBootstrapper"/> that coordinates
/// Firebase admin provisioning with creation of a corresponding domain user.
/// </summary>
public sealed class AdminUserBootstrapper(
    IFirebaseAdminClient firebaseAdminClient,
    IUserRepository userRepository,
    IOptions<AdminUserOptions> options,
    ILogger<AdminUserBootstrapper> logger)
    : IAdminUserBootstrapper
{
    private readonly AdminUserOptions _options = options.Value;

    public async Task SeedAdminUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.SeedOnStartup)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Email) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.DisplayName))
        {
            logger.LogWarning(
                "Admin user seeding is enabled but AdminUser options are incomplete. " +
                "Email, Password, and DisplayName must all be provided.");
            return;
        }

        var email = Email.Create(_options.Email);

        // If the admin user already exists in our database, we only need to ensure
        // the identity provider has the correct admin claims.
        var existingUser = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogInformation("Admin user with email {Email} already exists. Ensuring admin claims in identity provider.",
                _options.Email);

            await firebaseAdminClient.EnsureAdminUserAsync(
                _options.Email,
                _options.Password,
                _options.DisplayName,
                cancellationToken);

            return;
        }

        logger.LogInformation("Seeding initial admin user with email {Email}.", _options.Email);

        string externalId = await firebaseAdminClient.EnsureAdminUserAsync(
            _options.Email,
            _options.Password,
            _options.DisplayName,
            cancellationToken);

        var name = UserName.Create(_options.DisplayName);
        var externalAuthId = ExternalAuthIdentifier.Create(externalId);

        var adminUser = User.CreateAdmin(email, name, externalAuthId);
        await userRepository.AddAsync(adminUser, cancellationToken);

        logger.LogInformation("Admin user seeding completed for email {Email}.", _options.Email);
    }
}
