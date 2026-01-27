using System.Net;
using FluentAssertions;
using WebAPI.Middleware;
using Xunit;

namespace WebAPI.Tests;

public class SecurityHeadersTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpoint_ShouldIncludeSecurityHeaders_WhenRequestIsProcessed()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.TryGetValues(SecurityHeaderConstants.XContentTypeOptionsHeaderName,
                out var xContentTypeOptions)
            .Should().BeTrue("X-Content-Type-Options header should be present");
        xContentTypeOptions!.Should().ContainSingle()
            .Which.Should().Be(SecurityHeaderConstants.XContentTypeOptionsNoSniffValue);

        response.Headers.TryGetValues(SecurityHeaderConstants.XFrameOptionsHeaderName,
                out var xFrameOptions)
            .Should().BeTrue("X-Frame-Options header should be present");
        xFrameOptions!.Should().ContainSingle()
            .Which.Should().Be(SecurityHeaderConstants.XFrameOptionsDenyValue);

        response.Headers.TryGetValues(SecurityHeaderConstants.ReferrerPolicyHeaderName,
                out var referrerPolicy)
            .Should().BeTrue("Referrer-Policy header should be present");
        referrerPolicy!.Should().ContainSingle()
            .Which.Should().Be(SecurityHeaderConstants.ReferrerPolicyStrictOriginWhenCrossOriginValue);

        response.Headers.TryGetValues(SecurityHeaderConstants.ContentSecurityPolicyHeaderName,
                out var contentSecurityPolicy)
            .Should().BeTrue("Content-Security-Policy header should be present");
        contentSecurityPolicy!.Should().ContainSingle()
            .Which.Should().Be(SecurityHeaderConstants.ContentSecurityPolicyDefaultSelfValue);
    }
}
