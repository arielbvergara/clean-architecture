using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebAPI.Tests;

/// <summary>
/// TEST-ONLY authentication handler used by WebAPI.Tests.
/// DO NOT use this in production; real environments must rely on JWT bearer tokens.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no test header is present, behave as unauthenticated.
        if (!Request.Headers.TryGetValue("X-Test-Only-ExternalId", out var externalIdValues) ||
            string.IsNullOrWhiteSpace(externalIdValues.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var externalId = externalIdValues.ToString();

        var claims = new List<Claim>
        {
            new("sub", externalId),
        };

        if (Request.Headers.TryGetValue("X-Test-Only-Role", out var roleValues) &&
            !string.IsNullOrWhiteSpace(roleValues.ToString()))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleValues.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
