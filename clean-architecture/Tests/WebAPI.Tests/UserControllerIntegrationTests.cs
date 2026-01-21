using System.Net;
using System.Net.Http.Json;
using Application.Dtos.User;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebAPI.Tests;

public class UserControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
            {
                // Use a dedicated Testing environment that Program.cs treats as always in-memory
                builder.UseEnvironment("Testing");
            })
            .CreateClient();
    }

    [Fact]
    public async Task UserLifecycle_ShouldCreateGetUpdateAndDeleteUser_WhenUsingUserEndpoints()
    {
        // Arrange
        const string email = "test@test.com";
        const string name = "test";
        const string externalAuthId = "external-test-id";
        const string updatedName = "test modified";

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

        // 3) get user by id
        var getByIdResponse = await _client.GetAsync($"/api/User/{userId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userById = await getByIdResponse.Content.ReadFromJsonAsync<UserResponse>();
        userById.Should().NotBeNull();
        userById!.Email.Should().Be(email);
        userById.Name.Should().Be(name);

        // 4) modify the user's name (to "test modified")
        var updateBody = new UpdateUserNameDto(updatedName);

        var updateResponse = await _client.PutAsJsonAsync($"/api/User/{userId}/name", updateBody);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserResponse>();
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be(updatedName);

        // 5) get user by id and check name was modified
        var getAfterUpdateResponse = await _client.GetAsync($"/api/User/{userId}");
        getAfterUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userAfterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<UserResponse>();
        userAfterUpdate.Should().NotBeNull();
        userAfterUpdate!.Name.Should().Be(updatedName);

        // 6) delete the user by id
        var deleteResponse = await _client.DeleteAsync($"/api/User/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // verify user is deleted
        var getAfterDeleteResponse = await _client.GetAsync($"/api/User/{userId}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
