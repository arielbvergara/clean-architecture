namespace WebAPI.Configuration;

public static class CorsConfiguration
{
    private const string ClientAppCorsPolicyName = "ClientAppCorsPolicy";
    private const string ClientAppConfigSection = "ClientApp";
    private const string ClientAppOriginConfigKey = "Origin";

    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var clientAppOrigin = configuration.GetSection(ClientAppConfigSection)[ClientAppOriginConfigKey];

        if (string.IsNullOrWhiteSpace(clientAppOrigin))
        {
            // In the Testing environment, fall back to a safe default origin so that
            // WebAPI.Tests can run without requiring full configuration. In all other
            // environments, fail fast to surface misconfiguration early.
            if (environment.IsEnvironment("Testing"))
            {
                clientAppOrigin = "http://localhost";
            }
            else
            {
                throw new InvalidOperationException(
                    $"Client app origin configuration '{ClientAppConfigSection}:{ClientAppOriginConfigKey}' is missing.");
            }
        }

        // CORS for frontend client
        var corsSection = configuration.GetSection("Cors");
        var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>() ?? [];
        var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(ClientAppCorsPolicyName, policyBuilder =>
            {
                policyBuilder.WithOrigins(clientAppOrigin);

                if (allowedMethods.Length > 0)
                {
                    policyBuilder.WithMethods(allowedMethods);
                }
                else
                {
                    policyBuilder.AllowAnyMethod();
                }

                if (allowedHeaders.Length > 0)
                {
                    policyBuilder.WithHeaders(allowedHeaders);
                }
                else
                {
                    policyBuilder.AllowAnyHeader();
                }
            });
        });

        return services;
    }

    public static string GetCorsPolicyName()
    {
        return ClientAppCorsPolicyName;
    }
}
