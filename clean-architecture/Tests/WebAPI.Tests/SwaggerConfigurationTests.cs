using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace WebAPI.Tests;

public class SwaggerConfigurationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceProvider _serviceProvider = factory.Services;

    [Fact]
    public void ConfigureSwagger_ShouldRegisterBearerSecurityScheme_WhenBuildingSwaggerDocument()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

        // Act
        var document = provider.GetSwagger("v1");

        // Assert
        document.Components.SecuritySchemes.Should().ContainKey("bearer");

        var bearerScheme = document.Components.SecuritySchemes["bearer"];
        bearerScheme.Should().NotBeNull();
        bearerScheme.Type.Should().Be(SecuritySchemeType.Http);
        bearerScheme.Scheme.Should().Be("bearer");
        bearerScheme.BearerFormat.Should().Be("JWT");
    }

    [Fact]
    public void ConfigureSwagger_ShouldAddJwtBearerSecurityRequirement_WhenBuildingSwaggerDocument()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

        // Act
        var document = provider.GetSwagger("v1");

        // Assert
        document.Security.Should().NotBeNull();
        document.Security.Should().NotBeEmpty();

        document.Security.Should().Contain(requirement =>
            requirement.Keys.Any(schemeReference =>
                string.Equals(schemeReference.Reference.Id, "bearer", StringComparison.OrdinalIgnoreCase)));
    }
}