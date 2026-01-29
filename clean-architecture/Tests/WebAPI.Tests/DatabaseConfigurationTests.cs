using System;
using Application.Interfaces;
using FluentAssertions;
using Google.Cloud.Firestore;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WebAPI.Configuration;
using Xunit;

namespace WebAPI.Tests;

public sealed class DatabaseConfigurationTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        // Not used in these tests
        public string ApplicationName { get; set; } = "WebAPI";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void AddDatabaseConfiguration_ShouldRegisterFirestoreServices_WhenProviderIsFirestore()
    {
        // Arrange
        var services = new ServiceCollection();

        var settings = new Dictionary<string, string?>
        {
            ["Database:Provider"] = DatabaseProviderNames.Firestore,
            ["Database:FirestoreProjectId"] = "test-project-id",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();

        var environment = new FakeHostEnvironment("Development");

        // Act
        services.AddDatabaseConfiguration(configuration, environment);

        // Assert
        // FirestoreDb registered as singleton using factory
        var firestoreDescriptor = services.SingleOrDefault(
            descriptor => descriptor.ServiceType == typeof(FirestoreDb));

        firestoreDescriptor.Should().NotBeNull();
        firestoreDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        firestoreDescriptor.ImplementationFactory.Should().NotBeNull();

        // Firestore data store registered as scoped
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IFirestoreUserDataStore) &&
            descriptor.ImplementationType == typeof(FirestoreUserDataStore) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);

        // IUserRepository overridden to use FirestoreUserRepository
        var userRepositoryDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IUserRepository))
            .ToList();

        userRepositoryDescriptors.Should().NotBeEmpty();
        userRepositoryDescriptors.Should().Contain(descriptor =>
            descriptor.ImplementationType == typeof(FirestoreUserRepository));
    }

    [Fact]
    public void AddDatabaseConfiguration_ShouldUseInMemoryDatabase_WhenEnvironmentIsTestingEvenWhenFirestoreConfigured()
    {
        // Arrange
        var services = new ServiceCollection();

        var settings = new Dictionary<string, string?>
        {
            ["Database:Provider"] = DatabaseProviderNames.Firestore,
            ["Database:FirestoreProjectId"] = "test-project-id",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();

        var environment = new FakeHostEnvironment("Testing");

        // Act
        services.AddDatabaseConfiguration(configuration, environment);

        // Assert
        // In Testing environment, Firestore-specific registrations should not be present
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(FirestoreDb));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IFirestoreUserDataStore));

        // IUserRepository should remain the default EF-based implementation
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IUserRepository) &&
            descriptor.ImplementationType == typeof(UserRepository));
    }

    [Fact]
    public void AddDatabaseConfiguration_ShouldThrowInvalidOperation_WhenFirestoreProviderIsMissingProjectId()
    {
        // Arrange
        var services = new ServiceCollection();

        var settings = new Dictionary<string, string?>
        {
            ["Database:Provider"] = DatabaseProviderNames.Firestore,
            // Intentionally omit Database:FirestoreProjectId to trigger validation
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();

        var environment = new FakeHostEnvironment("Development");

        // Act
        var act = () => services.AddDatabaseConfiguration(configuration, environment);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Firestore database provider is configured but Database:FirestoreProjectId is missing.");
    }
}
