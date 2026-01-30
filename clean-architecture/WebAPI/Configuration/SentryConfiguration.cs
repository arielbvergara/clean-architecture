namespace WebAPI.Configuration;

public static class SentryConfiguration
{
    private const string SentrySectionName = "Sentry";
    private const string EnabledConfigKey = "Enabled";

    public static bool ConfigureSentry(this WebApplicationBuilder builder)
    {
        var sentrySection = builder.Configuration.GetSection(SentrySectionName);

        var isEnabled = sentrySection.GetValue<bool>(EnabledConfigKey);
        if (!isEnabled)
        {
            return false;
        }

        // Bind configuration values (Dsn, Environment, TracesSampleRate, etc.) via Sentry's
        // built-in configuration support. When "Sentry" configuration is present, the
        // options will be bound automatically.
        builder.WebHost.UseSentry();

        return true;
    }
}
