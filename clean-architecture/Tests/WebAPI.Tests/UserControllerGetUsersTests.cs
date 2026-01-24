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

public class UserControllerGetUsersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerGetUsersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_ShouldReturnForbidden_WhenCallerIsNotAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string externalId = "non-admin-external";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User");
        request.Headers.Add("X-Test-Only-ExternalId", externalId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPagedUsers_WhenCallerIsAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string adminExternalId = "admin-list-external";

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Seed multiple users
            for (var index = 0; index < 15; index++)
            {
                var email = Email.Create($"listuser{index}@example.com");
                var name = UserName.Create($"List User {index}");
                var externalId = ExternalAuthIdentifier.Create($"list-external-{index}");

                var user = User.Create(email, name, externalId);
                context.Users.Add(user);
            }

            var adminEmail = Email.Create("admin-list@example.com");
            var adminName = UserName.Create("Admin List User");
            var adminExtId = ExternalAuthIdentifier.Create(adminExternalId);
            var adminUser = User.Create(adminEmail, adminName, adminExtId);

            context.Users.Add(adminUser);

            await context.SaveChangesAsync(CancellationToken.None);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User?pageNumber=1&pageSize=10");
        request.Headers.Add("X-Test-Only-ExternalId", adminExternalId);
        request.Headers.Add("X-Test-Only-Role", "Admin");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedUsersResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(10);
        body.Pagination.TotalItems.Should().BeGreaterOrEqualTo(15);
        body.Pagination.PageNumber.Should().Be(1);
        body.Pagination.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOnlyDeletedUsers_WhenIsDeletedFilterIsTrue()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string adminExternalId = "admin-list-deleted-external";

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Seed deleted users
            for (var index = 0; index < 3; index++)
            {
                var email = Email.Create($"deleted{index}@example.com");
                var name = UserName.Create($"Deleted User {index}");
                var externalId = ExternalAuthIdentifier.Create($"deleted-external-{index}");

                var user = User.Create(email, name, externalId);
                user.MarkDeleted();
                context.Users.Add(user);
            }

            // Seed active users
            for (var index = 0; index < 2; index++)
            {
                var email = Email.Create($"active{index}@example.com");
                var name = UserName.Create($"Active User {index}");
                var externalId = ExternalAuthIdentifier.Create($"active-external-{index}");

                var user = User.Create(email, name, externalId);
                context.Users.Add(user);
            }

            // Seed admin user
            var adminEmail = Email.Create("admin-deleted@example.com");
            var adminName = UserName.Create("Admin Deleted User");
            var adminExtId = ExternalAuthIdentifier.Create(adminExternalId);
            var adminUser = User.Create(adminEmail, adminName, adminExtId);

            context.Users.Add(adminUser);

            await context.SaveChangesAsync(CancellationToken.None);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User?isDeleted=true&pageNumber=1&pageSize=10");
        request.Headers.Add("X-Test-Only-ExternalId", adminExternalId);
        request.Headers.Add("X-Test-Only-Role", "Admin");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedUsersResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(user => user.IsDeleted);
    }
}
