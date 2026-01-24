using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class AppDbContextFactory
{
    extension(IServiceCollection services)
    {
        public void AddInMemoryDatabase()
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("AppDb"));
        }

        public void AddPostgresDatabase(string connectionString)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure();
                }));
        }
    }
}
