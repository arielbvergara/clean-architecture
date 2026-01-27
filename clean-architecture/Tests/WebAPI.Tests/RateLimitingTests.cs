using System.Net;
using FluentAssertions;
using WebAPI.RateLimiting;
using Xunit;

namespace WebAPI.Tests;

public class RateLimitingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const int ExtraRequestsToEnsureRejection = 5;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateUser_ShouldReturnTooManyRequests_WhenStrictRateLimitExceeded()
    {
        // This test uses the strict policy applied to POST /api/User.
        // UseRateLimiter runs before authentication, so unauthenticated requests
        // still consume tokens based on the IP partition and will eventually
        // receive 429 when the limit is exceeded.

        var url = "/api/User";
        var totalRequests = RateLimitingDefaults.StrictPermitLimit + ExtraRequestsToEnsureRejection;

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < totalRequests; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };

            lastResponse = await _client.SendAsync(request);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
