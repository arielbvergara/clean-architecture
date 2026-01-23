using System.Net;
using System.Net.Http.Json;
using Application.Dtos.User;
using FluentAssertions;
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
        var createRequest = new CreateUserRequest(email, name, externalAuthId);

        var createResponse = await _client.PostAsJsonAsync("/api/User", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(email);
        createdUser.Name.Should().Be(name);

        var userId = createdUser.Id;

        // 2) get user by email
        var getByEmailResponse = await _client.GetAsync($"/api/User/email/{Uri.EscapeDataString(email)}");
        getByEmailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userByEmail = await getByEmailResponse.Content.ReadFromJsonAsync<UserResponse>();
        userByEmail.Should().NotBeNull();
        userByEmail!.Id.Should().Be(userId);
        userByEmail.Email.Should().Be(email);

        // 3) get current user via /me
        var getMeResponse = await _client.GetAsync("/api/User/me");
        getMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meUser = await getMeResponse.Content.ReadFromJsonAsync<UserResponse>();
        meUser.Should().NotBeNull();
        meUser!.Id.Should().Be(userId);
        meUser.Email.Should().Be(email);
        meUser.Name.Should().Be(name);

        // 4) get user by id
        var getByIdResponse = await _client.GetAsync($"/api/User/{userId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userById = await getByIdResponse.Content.ReadFromJsonAsync<UserResponse>();
        userById.Should().NotBeNull();
        userById!.Email.Should().Be(email);
        userById.Name.Should().Be(name);

        // 5) modify the user's name (to "test modified") via /me
        var updateBody = new UpdateUserNameDto(updatedName);

        var updateResponse = await _client.PutAsJsonAsync("/api/User/me/name", updateBody);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserResponse>();
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be(updatedName);

        // 6) get current user via /me and check name was modified
        var getAfterUpdateResponse = await _client.GetAsync("/api/User/me");
        getAfterUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userAfterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<UserResponse>();
        userAfterUpdate.Should().NotBeNull();
        userAfterUpdate!.Name.Should().Be(updatedName);

        // 7) delete the current user via /me
        var deleteResponse = await _client.DeleteAsync("/api/User/me");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // verify user is deleted: /me should now fail with 404 (current user no longer resolvable)
        var getAfterDeleteMeResponse = await _client.GetAsync("/api/User/me");
        getAfterDeleteMeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // direct access by id should now be forbidden because the current principal can no longer
        // be resolved to a user record and the OwnsUser policy fails closed.
        var getAfterDeleteByIdResponse = await _client.GetAsync($"/api/User/{userId}");
        getAfterDeleteByIdResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
