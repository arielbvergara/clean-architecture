using Application.UseCases.User;
using Microsoft.Extensions.Options;
using WebAPI.Configuration;

namespace WebAPI.Authentication;

/// <summary>
/// Default implementation of <see cref="IAdminUserBootstrapper"/> that coordinates
/// Firebase admin provisioning with creation of a corresponding domain user.
/// </summary>
public sealed class AdminUserBootstrapper(
    CreateAdminUserUseCase createAdminUserUseCase,
    IOptions<AdminUserOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<AdminUserBootstrapper> logger)
    : IAdminUserBootstrapper
{
    private const int MinimumAdminPasswordLength = 12;

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
            if (hostEnvironment.IsDevelopment())
            {
                logger.LogWarning(
                    "Admin user seeding is enabled but AdminUser options are incomplete. " +
                    "Email, Password, and DisplayName must all be provided.");
                return;
            }

            throw new InvalidOperationException(
                "Admin user seeding is enabled but required AdminUser options are missing in this environment.");
        }

        if (!hostEnvironment.IsDevelopment())
        {
            if (_options.Password.Length < MinimumAdminPasswordLength ||
                string.Equals(_options.Password, _options.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Admin user password does not meet minimum complexity requirements in non-development environments.");
            }
        }

        logger.LogInformation("Seeding initial admin user with email {Email}.", _options.Email);

        var request = new Application.Dtos.User.CreateAdminUserRequest(
            _options.Email,
            _options.DisplayName,
            _options.Password);

        var result = await createAdminUserUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            // The CreateAdminUserUseCase handles the "user already exists" case by
            // ensuring admin claims in the identity provider and returning success.
            // Therefore, any failure here represents a genuine error condition.
            logger.LogError(result.Error!.InnerException, "Admin user seeding failed: {Error}", result.Error.Message);
            throw new InvalidOperationException($"Admin user seeding failed: {result.Error.Message}");
        }

        logger.LogInformation("Admin user seeding completed for email {Email}.", _options.Email);
    }
}
