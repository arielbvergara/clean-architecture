using System.Net;
using System.Net.Http.Json;
using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

public class UserControllerAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUser_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        // Arrange
        var client = _factory.CreateClient();
        var createRequest = new
        {
            Email = "anonymous@example.com",
            Name = "Anonymous User"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_ShouldPersistExternalAuthIdFromToken_WhenAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string externalIdFromToken = "create-user-token-external-id";
        const string externalIdFromBody = "body-override-external-id";

        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", externalIdFromToken);

        var createRequest = new
        {
            Email = "create-user@example.com",
            Name = "Create User",
            // This field is intentionally not part of the WebAPI contract and should be ignored
            ExternalAuthId = externalIdFromBody
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await response.Content.ReadFromJsonAsync<UserResponse>();
        createdUser.Should().NotBeNull();
        createdUser!.ExternalAuthId.Should().Be(externalIdFromToken);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        // Arrange
        var client = _factory.CreateClient();

        // No X-Test-Only-ExternalId header -> TestAuthHandler returns no identity -> 401 due to [Authorize].
        var userId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/User/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnForbidden_WhenNonAdminAccessesAnyUser()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string externalId = "user-1-external";

        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var email = Email.Create("user1@example.com");
            var userName = UserName.Create("User One");
            var extId = ExternalAuthIdentifier.Create(externalId);

            var user = User.Create(email, userName, extId);
            context.Users.Add(user);
            await context.SaveChangesAsync(CancellationToken.None);

            userId = user.Id.Value;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{userId}");
        request.Headers.Add("X-Test-Only-ExternalId", externalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnForbidden_WhenUserAccessesAnotherUsersResource()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string ownerExternalId = "owner-external";
        const string attackerExternalId = "attacker-external";

        Guid ownerUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var ownerEmail = Email.Create("owner@example.com");
            var ownerName = UserName.Create("Owner User");
            var ownerExtId = ExternalAuthIdentifier.Create(ownerExternalId);

            var ownerUser = User.Create(ownerEmail, ownerName, ownerExtId);

            var attackerEmail = Email.Create("attacker@example.com");
            var attackerName = UserName.Create("Attacker User");
            var attackerExtId = ExternalAuthIdentifier.Create(attackerExternalId);
            var attackerUser = User.Create(attackerEmail, attackerName, attackerExtId);

            context.Users.Add(ownerUser);
            context.Users.Add(attackerUser);
            await context.SaveChangesAsync(CancellationToken.None);

            ownerUserId = ownerUser.Id.Value;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{ownerUserId}");
        request.Headers.Add("X-Test-Only-ExternalId", attackerExternalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WhenAdminAccessesAnotherUsersResource()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string ownerExternalId = "owner-admin-target";
        const string adminExternalId = "admin-external";

        Guid ownerUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var ownerEmail = Email.Create("admin-target@example.com");
            var ownerName = UserName.Create("Admin Target User");
            var ownerExtId = ExternalAuthIdentifier.Create(ownerExternalId);

            var ownerUser = User.Create(ownerEmail, ownerName, ownerExtId);

            var adminEmail = Email.Create("admin@example.com");
            var adminName = UserName.Create("Admin User");
            var adminExtId = ExternalAuthIdentifier.Create(adminExternalId);
            var adminUser = User.Create(adminEmail, adminName, adminExtId);

            context.Users.Add(ownerUser);
            context.Users.Add(adminUser);
            await context.SaveChangesAsync(CancellationToken.None);

            ownerUserId = ownerUser.Id.Value;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{ownerUserId}");
        request.Headers.Add("X-Test-Only-ExternalId", adminExternalId);
        request.Headers.Add("X-Test-Only-Role", "Admin");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(ownerUserId);
    }

    [Fact]
    public async Task GetMe_ShouldReturnNotFound_WhenCurrentUserRecordDoesNotExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string externalId = "nonexistent-user-external-id";

        // No user is seeded for this external id. The controller should fail closed without
        // leaking internal details and respond with 404 for the /me endpoint.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User/me");
        request.Headers.Add("X-Test-Only-ExternalId", externalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnForbidden_WhenNonAdminAccessesWithoutBackingUserRecord()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string externalId = "nonexistent-user-external-id";

        // Create a target user that is not associated with the current principal's external id.
        Guid targetUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var email = Email.Create("target@example.com");
            var userName = UserName.Create("Target User");
            var extId = ExternalAuthIdentifier.Create("some-other-external-id");

            var user = User.Create(email, userName, extId);
            context.Users.Add(user);
            await context.SaveChangesAsync(CancellationToken.None);

            targetUserId = user.Id.Value;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{targetUserId}");
        request.Headers.Add("X-Test-Only-ExternalId", externalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
