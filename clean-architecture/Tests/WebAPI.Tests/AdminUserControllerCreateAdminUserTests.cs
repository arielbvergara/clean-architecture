using System.Net;
using System.Net.Http.Json;
using Application.Dtos.User;
using Application.Interfaces;
using Domain.Constants;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.DTOs;
using Xunit;

namespace WebAPI.Tests;

public class AdminUserControllerCreateAdminUserTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminUserControllerCreateAdminUserTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAdminUser_ShouldReturnForbidden_WhenCallerIsNotAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string externalId = "non-admin-create-admin";

        var requestBody = new CreateAdminUserDto(
            Email: "admin-nonadmin@example.com",
            DisplayName: "Non Admin Attempt",
            Password: "StrongPassword!123");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/User")
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("X-Test-Only-ExternalId", externalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAdminUser_ShouldCreateAdminUser_WhenCallerIsAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string adminExternalId = "admin-create-admin";

        var notifier = (TestSecurityEventNotifier)_factory.Services.GetRequiredService<ISecurityEventNotifier>();
        notifier.ClearEvents();

        var createDto = new CreateAdminUserDto(
            Email: "admin-created@example.com",
            DisplayName: "Created Admin",
            Password: "StrongPassword!456");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/User")
        {
            Content = JsonContent.Create(createDto)
        };

        request.Headers.Add("X-Test-Only-ExternalId", adminExternalId);
        request.Headers.Add("X-Test-Only-Role", "Admin");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminUser = await response.Content.ReadFromJsonAsync<UserResponse>();
        adminUser.Should().NotBeNull();
        adminUser!.Email.Should().Be(Email.Create(createDto.Email).Value);
        adminUser.Name.Should().Be(createDto.DisplayName);
        adminUser.IsDeleted.Should().BeFalse();

        // Verify that the user is persisted and has admin role in the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedUser = context.Users.SingleOrDefault(u => u.Email.Value == adminUser.Email);
        persistedUser.Should().NotBeNull();
        persistedUser!.Role.Should().Be(UserRoleConstants.Admin);

        // Verify security event notifier captured the admin creation event
        var events = notifier.Events;
        events.Should().NotBeEmpty();
        events.Should().Contain(e =>
            e.EventName == SecurityEventNames.UserCreated &&
            e.SubjectId == adminUser.Id.ToString() &&
            e.Outcome == SecurityEventOutcomes.Success);
    }
}
