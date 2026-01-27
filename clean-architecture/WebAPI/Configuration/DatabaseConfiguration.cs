using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

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

        // Always use in-memory database for the Testing environment, regardless of configuration
        var useInMemoryDb = configuration.GetValue<bool>("UseInMemoryDB") ||
                            environment.IsEnvironment("Testing");

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
