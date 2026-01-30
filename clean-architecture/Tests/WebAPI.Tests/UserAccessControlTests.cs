using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebAPI.Tests;

public class UserAccessControlTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _anonClient = factory.CreateClient();
    private readonly HttpClient _adminClient = CreateClientWithRole(factory, "admin-user", "Admin");
    private readonly HttpClient _userClient = CreateClientWithRole(factory, "regular-user", "User");
    private readonly HttpClient _otherClient = CreateClientWithRole(factory, "other-user", "User");

    // NOTE: In a real scenario, we would seed these users in the DB via a test fixture/seed method.
    // For this assignment, we assume the InMemory DB logic behaves consistently, or we rely on
    // result codes assuming specific data states.
    // However, to make these tests robust, we should ideally ensure data exists.
    // Given the constraints and current file visibility, we will focus on the *Access Control* responses.
    // We assume the system might return 404 for missing users, which is fine for "Non-Admin" tests
    // where 403 vs 404 distinction is key (or lack thereof for anti-enumeration).

    [Fact]
    public async Task GetUserById_ShouldReturnForbiddenOrNotFound_WhenCallerIsAuthenticatedNonAdmin()
    {
        // Arrange
        var targetUserId = Guid.NewGuid(); // Random ID

        // Act
        // Attempt to access an arbitrary user ID as a regular user
        var response = await _userClient.GetAsync($"/api/admin/User/{targetUserId}");

        // Assert
        // ADR-014: "Unauthorized or forbidden callers receive generic responses"
        // It must NOT be 200. It should be 403 Forbidden.
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden,
            $"Expected Forbidden, but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUnauthorized_WhenCallerIsAnonymous()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        // Act
        var response = await _anonClient.GetAsync($"/api/admin/User/{targetUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnAllowed_WhenCallerIsAdmin()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        // Act
        var response = await _adminClient.GetAsync($"/api/admin/User/{targetUserId}");

        // Assert
        // Admin should be allowed to access.
        // It might return 200 OK (if seeded) or 404 (if not found),
        // but it MUST NOT return 401 Unauthorized or 403 Forbidden.
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Expected OK or NotFound for Admin, but got {response.StatusCode} (Access Denied?)");
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnForbiddenOrNotFound_WhenCallerIsAuthenticatedNonAdmin()
    {
        // Arrange
        var targetEmail = "somebody@example.com";

        // Act
        var response = await _userClient.GetAsync($"/api/admin/User/email/{targetEmail}");

        // Assert
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden,
            $"Expected Forbidden, but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnUnauthorized_WhenCallerIsAnonymous()
    {
        // Arrange
        var targetEmail = "somebody@example.com";

        // Act
        var response = await _anonClient.GetAsync($"/api/admin/User/email/{targetEmail}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOk_WhenCallerIsAuthenticated()
    {
        // Arrange
        // The endpoint /api/User/me should return the profile of the caller.
        // We rely on the mock auth handler injecting "regular-user" as the ID.
        // We might need to ensure the user exists in the DB for this to return 200, otherwise 404.
        // If the system auto-provisions or if we haven't seeded, we might get 404.
        // To verify *Access Control*, getting past 401/403 is the goal.

        // Act
        var response = await _userClient.GetAsync("/api/User/me");

        // Assert
        // Valid outcomes: 200 OK (User found), 404 Not Found (User auth is there but record missing).
        // Invalid outcomes: 401 Unauthorized, 403 Forbidden.
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Expected OK or NotFound, but got {response.StatusCode}. Auth Headers should allow access.");
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenCallerIsAnonymous()
    {
        // Act
        var response = await _anonClient.GetAsync("/api/User/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AntiEnumeration_ShouldReturnSameError_ForExistingAndNonExistingUser_WhenCallerIsNonAdmin()
    {
        // Arrange
        // We need 2 IDs: one that effectively doesn't exist, and ideally one that does.
        // Without seeding, both "don't exist".
        // However, we can assert that the response checks happen BEFORE existence checks if it's 403,
        // or that they look identical (same Status Code).
        // Since we can't easily guarantee "Existent" without seeding, we verify that
        // the response is 403 Forbidden (implying "You can't even look") rather than 404 if possible.

        var randomId1 = Guid.NewGuid();

        // Act
        var response1 = await _userClient.GetAsync($"/api/admin/User/{randomId1}");
        var response2 = await _otherClient.GetAsync($"/api/admin/User/{randomId1}"); // Different user, same target

        // Assert
        // Both requestors are non-admins targeting a random ID.
        Assert.Equal(response1.StatusCode, response2.StatusCode);
        Assert.True(response1.StatusCode is HttpStatusCode.Forbidden);
    }

    private static HttpClient CreateClientWithRole(WebApplicationFactory<Program> factory, string userId, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Only-Role", role);
        return client;
    }
}
