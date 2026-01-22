using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebAPI.Tests;

/// <summary>
/// TEST-ONLY WebApplicationFactory used by WebAPI.Tests to host the API in-memory.
/// It overrides authentication with TestAuthHandler. DO NOT use this in production code paths.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<WebAPI.Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure the application runs in the Testing environment so that the in-memory DB is used.
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override authentication with a lightweight test scheme.
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    options => { });
        });
    }
}