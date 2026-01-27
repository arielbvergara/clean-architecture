using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WebAPI.Authentication;

namespace WebAPI.Configuration;

public static class PipelineConfiguration
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Apply EF Core migrations for relational database providers only. This replaces
        /// EnsureCreated so that schema changes (e.g., new columns for soft delete or roles)
        /// are handled via migrations instead of ad-hoc schema creation.
        ///
        /// In tests and other scenarios that use the in-memory provider, calling Migrate()
        /// would throw (relational-only API). Guard against that by checking IsRelational().
        /// </summary>
        public IApplicationBuilder UseDatabaseMigration()
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (context.Database.IsRelational())
            {
                context.Database.Migrate();
            }

            return app;
        }

        /// <summary>
        /// In the Testing environment, skip admin seeding entirely so that integration tests
        /// do not require real Firebase Admin credentials. In all other environments, perform
        /// the idempotent admin bootstrap.
        /// </summary>
        public IApplicationBuilder UseAdminUserSeeding()
        {
            using var scope = app.ApplicationServices.CreateScope();
            var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

            if (!hostEnvironment.IsEnvironment("Testing"))
            {
                // Seed the initial admin user if configured. This operation is idempotent and
                // relies on Firebase custom claims for authorization, with a corresponding
                // domain user record for reporting and future domain logic.
                var adminBootstrapper = scope.ServiceProvider.GetRequiredService<IAdminUserBootstrapper>();
                adminBootstrapper.SeedAdminUserAsync().GetAwaiter().GetResult();
            }

            return app;
        }
    }
}
