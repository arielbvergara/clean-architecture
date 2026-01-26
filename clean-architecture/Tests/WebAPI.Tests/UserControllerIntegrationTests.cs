using System.Net;
using System.Net.Http.Json;
using Application.Dtos.User;
using FluentAssertions;
using WebAPI.Controllers;
using Xunit;

namespace WebAPI.Tests;

public class UserControllerIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UserLifecycle_ShouldCreateGetUpdateAndDeleteUser_WhenUsingUserEndpoints()
    {
        // Arrange
        const string email = "test@test.com";
        const string name = "test";
        const string externalAuthId = "external-test-id";
        const string updatedName = "test modified";

        // Authenticated user for the whole lifecycle (TEST-ONLY header, handled by TestAuthHandler)
        _client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", externalAuthId);

        // 1) create a user
        var createRequest = new CreateUserDto(email, name);

        var createResponse = await _client.PostAsJsonAsync("/api/User", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(email);
        createdUser.Name.Should().Be(name);
        createdUser.ExternalAuthId.Should().Be(externalAuthId);
        createdUser.IsDeleted.Should().BeFalse();

        var userId = createdUser.Id;

        // 2) get current user via /me
        var getMeResponse = await _client.GetAsync("/api/User/me");
        getMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meUser = await getMeResponse.Content.ReadFromJsonAsync<UserResponse>();
        meUser.Should().NotBeNull();
        meUser!.Id.Should().Be(userId);
        meUser.Email.Should().Be(email);
        meUser.Name.Should().Be(name);
        meUser.IsDeleted.Should().BeFalse();

        // 4) (intentionally no direct /api/User/{id} call here)
        // Id-based endpoints are now admin-only; the self-service flow uses /me.

        // 5) modify the user's name (to "test modified") via /me
        var updateBody = new UpdateUserNameDto(updatedName);

        var updateResponse = await _client.PutAsJsonAsync("/api/User/me/name", updateBody);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserResponse>();
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be(updatedName);
        updatedUser.IsDeleted.Should().BeFalse();

        // 6) get current user via /me and check name was modified
        var getAfterUpdateResponse = await _client.GetAsync("/api/User/me");
        getAfterUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userAfterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<UserResponse>();
        userAfterUpdate.Should().NotBeNull();
        userAfterUpdate!.Name.Should().Be(updatedName);
        userAfterUpdate.IsDeleted.Should().BeFalse();

        // 7) delete the current user via /me
        var deleteResponse = await _client.DeleteAsync("/api/User/me");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // verify user is deleted: /me should now fail with 404 (current user no longer resolvable)
        var getAfterDeleteMeResponse = await _client.GetAsync("/api/User/me");
        getAfterDeleteMeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
