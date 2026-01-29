using System;
using Application.Interfaces;
using Google.Cloud.Firestore;
using Infrastructure.Data;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;

namespace WebAPI.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Testing environment always uses the in-memory database to avoid real infrastructure dependencies
        var isTestingEnvironment = environment.IsEnvironment("Testing");

        if (isTestingEnvironment)
        {
            services.AddInMemoryDatabase();
            return services;
        }

        // Prefer the strongly-typed Database:Provider configuration when present
        var databaseProviderOptions = configuration
            .GetSection(DatabaseProviderOptions.SectionName)
            .Get<DatabaseProviderOptions>();

        var configuredProvider = databaseProviderOptions?.Provider;

        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            if (string.Equals(configuredProvider, DatabaseProviderNames.InMemory, StringComparison.OrdinalIgnoreCase))
            {
                services.AddInMemoryDatabase();
                return services;
            }

            if (string.Equals(configuredProvider, DatabaseProviderNames.Postgres, StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = databaseProviderOptions?.ConnectionString
                                       ?? configuration.GetConnectionString("DbContext")
                                       ?? throw new InvalidOperationException(
                                           "PostgreSQL connection string is missing for the configured database provider.");

                services.AddPostgresDatabase(connectionString);
                return services;
            }

            if (string.Equals(configuredProvider, DatabaseProviderNames.Firestore, StringComparison.OrdinalIgnoreCase))
            {
                var projectId = databaseProviderOptions?.FirestoreProjectId;

                if (string.IsNullOrWhiteSpace(projectId))
                {
                    throw new InvalidOperationException(
                        "Firestore database provider is configured but Database:FirestoreProjectId is missing.");
                }

                services.AddSingleton(provider => FirestoreDb.Create(projectId));
                services.AddScoped<IFirestoreUserDataStore, FirestoreUserDataStore>();

                // Override the default EF-based repository registration when Firestore is selected
                services.AddScoped<IUserRepository, FirestoreUserRepository>();

                return services;
            }

            throw new InvalidOperationException(
                $"Unsupported database provider '{configuredProvider}'. Supported providers are: '{DatabaseProviderNames.InMemory}', '{DatabaseProviderNames.Postgres}', '{DatabaseProviderNames.Firestore}'.");
        }

        // Fallback to existing behavior when Database:Provider is not configured
        var useInMemoryDb = configuration.GetValue<bool>("UseInMemoryDB");

        if (useInMemoryDb)
        {
            // we could have written that logic here but as per clean architecture, we are separating these into their own piece of code
            services.AddInMemoryDatabase();
        }
        else
        {
            // Use PostgreSQL as the real database when not using the in-memory provider
            var connectionString = configuration.GetConnectionString("DbContext")
                                   ?? throw new InvalidOperationException(
                                       "PostgreSQL connection string 'DbContext' is missing.");

            services.AddPostgresDatabase(connectionString);
        }

        return services;
    }
}
