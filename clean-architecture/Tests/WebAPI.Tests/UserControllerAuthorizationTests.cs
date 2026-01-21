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
    public async Task GetUserById_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        // Arrange
        var client = _factory.CreateClient();

        // No X-Test-ExternalId header -> TestAuthHandler returns no identity -> 401 due to [Authorize].
        var userId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/User/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WhenUserAccessesOwnResource()
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
        request.Headers.Add("X-Test-ExternalId", externalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(userId);
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
        request.Headers.Add("X-Test-ExternalId", attackerExternalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}