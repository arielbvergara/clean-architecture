using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authentication;

/// <summary>
/// Configures JWT Bearer authentication and authorization policies for the WebAPI host.
///
/// This is intentionally kept in the WebAPI layer so that the choice of identity provider
/// (Firebase, Entra ID, Auth0, etc.) can be changed without impacting Domain/Application.
///
/// In the current setup, Firebase Authentication is the primary identity provider. Tokens are
/// validated strictly against the configured Firebase project (issuer/audience), and the
/// custom claim "role" is surfaced as the ASP.NET Core role claim so that
/// User.IsInRole / [Authorize(Roles = "...")] behave as expected.
/// </summary>
public static class JwtAuthenticationExtensions
{
    private const string AuthenticationSectionName = "Authentication";

    public static IServiceCollection AddJwtAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authSection = configuration.GetSection(AuthenticationSectionName);

        var authority = authSection["Authority"];
        var audience = authSection["Audience"];

        // Default to standard JWT handler map (no legacy Microsoft-specific claim mappings).
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // If Authority/Audience are not configured, JwtBearer will still be registered,
                // but incoming tokens will fail validation. This is preferable to silently
                // accepting tokens from an unexpected issuer.
                if (!string.IsNullOrWhiteSpace(audience))
                {
                    options.TokenValidationParameters.ValidAudience = audience;
                    options.TokenValidationParameters.ValidateAudience = true;
                }

                if (!string.IsNullOrWhiteSpace(authority))
                {
                    options.Authority = authority;

                    // For Firebase, issuer and authority share the same base URL.
                    options.TokenValidationParameters.ValidIssuer = authority;
                    options.TokenValidationParameters.ValidateIssuer = true;

                    // In development, allow HTTPs metadata to be optional if needed.
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                }

                // Enforce token lifetime with a small clock skew to avoid overly long reuse windows.
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);

                // Map Firebase custom claim "role" into ASP.NET Core's role system so that
                // User.IsInRole("admin") and [Authorize(Roles = "admin")] work as expected.
                options.TokenValidationParameters.RoleClaimType = "role";
            });

        services.AddAuthorization(options =>
        {
            // Require authenticated users by default for all endpoints. Individual endpoints
            // can still opt out via [AllowAnonymous] or refine via [Authorize(Roles = "...")].
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
