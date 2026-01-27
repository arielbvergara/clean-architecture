using Application.UseCases;
using WebAPI.Authentication;
using WebAPI.Configuration;
using WebAPI.Filters;

namespace WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        // Support `~` in GOOGLE_APPLICATION_CREDENTIALS path
        SetGoogleApplicationCredentialsPath();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers(options => { options.Filters.Add<GlobalExceptionFilter>(); });

        // Swagger/OpenAPI configuration
        builder.Services.AddSwaggerConfiguration();

        // CORS configuration
        builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);

        // Database configuration
        builder.Services.AddDatabaseConfiguration(builder.Configuration, builder.Environment);

        // Authentication & Authorization
        builder.Services.AddJwtAuthenticationAndAuthorization(builder.Configuration, builder.Environment);

        // Application use cases
        builder.Services.AddUseCases();

        // Admin user seeding configuration and services
        builder.Services.Configure<AdminUserOptions>(
            builder.Configuration.GetSection(AdminUserOptions.SectionName)
        );
        builder.Services.AddSingleton<IFirebaseAdminClient, FirebaseAdminClient>();
        builder.Services.AddScoped<IAdminUserBootstrapper, AdminUserBootstrapper>();

        var app = builder.Build();

        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        // CORS must run before authentication/authorization so that preflight
        // and actual requests receive the proper headers.
        app.UseCors(CorsConfiguration.GetCorsPolicyName());

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Database migrations and admin user seeding
        app.UseDatabaseMigration();
        app.UseAdminUserSeeding();

        app.Run();
    }

    private static void SetGoogleApplicationCredentialsPath()
    {
        var googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrEmpty(googleCredentials) || !googleCredentials.StartsWith('~'))
        {
            return;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expandedPath = googleCredentials.Replace("~", home);
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", expandedPath);
    }
}
