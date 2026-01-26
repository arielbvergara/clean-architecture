namespace WebAPI.Configuration;

/// <summary>
/// Options for seeding an initial administrator user in both Firebase and the local database.
/// </summary>
public sealed class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    /// <summary>
    /// When <c>true</c>, the application will attempt to ensure the admin user
    /// exists on startup using the configured credentials.
    /// </summary>
    public bool SeedOnStartup { get; init; }

    /// <summary>
    /// Display name to assign to the admin user.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Email address for the admin user.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Initial password for the admin user.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
