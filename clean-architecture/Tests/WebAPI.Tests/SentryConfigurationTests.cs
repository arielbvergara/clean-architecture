using System.Net;
using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

public class SentryConfigurationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetHealth_ShouldReturnOk_WhenSentryIsDisabled()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOkAndRegisterSentryServices_WhenSentryIsEnabled()
    {
        // Arrange
        var sentryEnabledFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var sentrySettings = new Dictionary<string, string?>
                {
                    ["Sentry:Enabled"] = "true",
                    ["Sentry:Dsn"] = "https://examplePublicKey@o0.ingest.sentry.io/0",
                    ["Sentry:Environment"] = "Testing",
                    ["Sentry:TracesSampleRate"] = "0.0"
                };

                configurationBuilder.AddInMemoryCollection(sentrySettings!);
            });
        });

        using var client = sentryEnabledFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // When Sentry is enabled via configuration, observability services
        // should still be resolvable and use the Sentry-backed implementation.
        using var scope = sentryEnabledFactory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var observability = provider.GetService<IObservabilityService>();
        observability.Should().NotBeNull();
        observability.Should().BeOfType<SentryObservabilityService>();
    }
}
