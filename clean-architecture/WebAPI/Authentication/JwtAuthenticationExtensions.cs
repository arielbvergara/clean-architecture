using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authorization;

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

                // Use the standard ClaimTypes.Role for ASP.NET Core role checks so that
                // both tests (which set ClaimTypes.Role) and production tokens work as
                // expected. The IsAdmin() helper still falls back to the raw "role" claim
                // if present.
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            });

        // Register custom authorization handlers.
        services.AddScoped<IAuthorizationHandler, OwnsUserHandler>();

        services.AddAuthorization(options =>
        {
            // Require authenticated users by default for all endpoints. Individual endpoints
            // can still opt out via [AllowAnonymous] or refine via [Authorize(Roles = "...")].
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Policy for endpoints that should only be accessible to administrators. Uses
            // the IsAdmin() helper, which checks both standard role claims and the
            // custom "role" claim used by Firebase tokens.
            options.AddPolicy(AuthorizationPoliciesConstants.AdminOnly, policy =>
            {
                policy.RequireAssertion(context => context.User.IsAdmin());
            });

            // Convenience policy for endpoints that simply require any authenticated user.
            options.AddPolicy(AuthorizationPoliciesConstants.User, policy =>
                policy.RequireAuthenticatedUser());

            // Policy that enforces that the caller owns the target user resource or is an admin.
            options.AddPolicy(AuthorizationPoliciesConstants.OwnsUser, policy =>
                policy.Requirements.Add(new OwnsUserRequirement()));
        });

        return services;
    }
}
