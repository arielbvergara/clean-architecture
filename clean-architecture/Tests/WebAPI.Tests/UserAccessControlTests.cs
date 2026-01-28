using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebAPI.Tests;

public class UserAccessControlTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetUserById_ShouldReturnForbidden_WhenCallerIsAuthenticatedNonAdmin()
    {
        // This test will be fully implemented once a stable authentication test harness
        // is available for simulating authenticated non-admin callers.
        // For now, it acts as a placeholder linked to ADR-014 and issue #12.
        await Task.CompletedTask;
    }
}
