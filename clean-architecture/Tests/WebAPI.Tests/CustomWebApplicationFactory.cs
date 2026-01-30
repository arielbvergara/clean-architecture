using Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAPI.Authentication;

namespace WebAPI.Tests;

/// <summary>
/// TEST-ONLY WebApplicationFactory used by WebAPI.Tests to host the API in-memory.
/// It overrides authentication with TestAuthHandler. DO NOT use this in production code paths.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<WebAPI.Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure the application runs in the Testing environment so that the in-memory DB is used.
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Inject minimal configuration required for tests so Program.cs does not throw
        // (e.g., ClientApp:Origin required for CORS setup).
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ClientApp:Origin"] = "http://localhost",
            };

            configurationBuilder.AddInMemoryCollection(testSettings!);
        });

        builder.ConfigureServices(services =>
        {
            // Override authentication with a lightweight test scheme.
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    options => { });

            // Replace the real Firebase admin client with a test double that does not
            // require Google Application Default Credentials. This keeps WebAPI tests
            // independent of external identity provider configuration.
            var firebaseDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFirebaseAdminClient));
            if (firebaseDescriptor is not null)
            {
                services.Remove(firebaseDescriptor);
            }

            var idpDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IIdentityProviderService));
            if (idpDescriptor is not null)
            {
                services.Remove(idpDescriptor);
            }

            services.AddSingleton<IFirebaseAdminClient, TestFirebaseAdminClient>();
            services.AddSingleton<IIdentityProviderService>(sp => sp.GetRequiredService<IFirebaseAdminClient>());

            // Replace the logging security event notifier with a test double so that
            // tests can assert which security events were emitted without depending
            // on logging or observability providers.
            var securityNotifierDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISecurityEventNotifier));
            if (securityNotifierDescriptor is not null)
            {
                services.Remove(securityNotifierDescriptor);
            }

            services.AddSingleton<ISecurityEventNotifier, TestSecurityEventNotifier>();
        });
    }

    private sealed class TestFirebaseAdminClient : IFirebaseAdminClient
    {
        public Task<string> EnsureAdminUserAsync(
            string email,
            string password,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            // In tests we simply echo back a stable external identifier derived from
            // the email so that domain entities can be created without contacting
            // Firebase or requiring ADC configuration.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult($"test-{normalizedEmail}");
        }
    }
}

public sealed class TestSecurityEventNotifier : ISecurityEventNotifier
{
    public sealed record SecurityEventRecord(
        string EventName,
        string? SubjectId,
        string Outcome,
        string? CorrelationId,
        IReadOnlyDictionary<string, string?>? Properties);

    private readonly List<SecurityEventRecord> _events = new();

    public IReadOnlyList<SecurityEventRecord> Events => _events;

    public void ClearEvents() => _events.Clear();

    public Task NotifyAsync(
        string eventName,
        string? subjectId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties = null,
        CancellationToken cancellationToken = default)
    {
        _events.Add(new SecurityEventRecord(eventName, subjectId, outcome, correlationId, properties));
        return Task.CompletedTask;
    }
}
