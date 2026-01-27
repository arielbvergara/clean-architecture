using System.Reflection;
using Application.Interfaces;
using Application.UseCases;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
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

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        const string bearerSchemeId = "bearer"; // lowercase per RFC 7235
        const string clientAppCorsPolicyName = "ClientAppCorsPolicy";
        const string clientAppConfigSection = "ClientApp";
        const string clientAppOriginConfigKey = "Origin";

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CleanArchitecture API",
                Version = "v1"
            });

            // Include XML documentation comments so controller and action summaries
            // and remarks appear in the generated OpenAPI spec and Swagger UI.
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            // Enable JWT bearer token support in Swagger UI
            options.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = bearerSchemeId,
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Paste only the JWT, without the 'Bearer ' prefix."
            });

            // Swashbuckle 10 / Microsoft.OpenApi v2+ expects a delegate here
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(bearerSchemeId, document)] = []
            });
        });

        var clientAppOrigin = builder.Configuration.GetSection(clientAppConfigSection)[clientAppOriginConfigKey];

        if (string.IsNullOrWhiteSpace(clientAppOrigin))
        {
            // In the Testing environment, fall back to a safe default origin so that
            // WebAPI.Tests can run without requiring full configuration. In all other
            // environments, fail fast to surface misconfiguration early.
            if (builder.Environment.IsEnvironment("Testing"))
            {
                clientAppOrigin = "http://localhost";
            }
            else
            {
                throw new InvalidOperationException(
                    $"Client app origin configuration '{clientAppConfigSection}:{clientAppOriginConfigKey}' is missing.");
            }
        }

        // CORS for frontend client
        var corsSection = builder.Configuration.GetSection("Cors");
        var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>() ?? [];
        var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(clientAppCorsPolicyName, policyBuilder =>
            {
                policyBuilder.WithOrigins(clientAppOrigin);

                if (allowedMethods.Length > 0)
                {
                    policyBuilder.WithMethods(allowedMethods);
                }
                else
                {
                    policyBuilder.AllowAnyMethod();
                }

                if (allowedHeaders.Length > 0)
                {
                    policyBuilder.WithHeaders(allowedHeaders);
                }
                else
                {
                    policyBuilder.AllowAnyHeader();
                }
            });
        });

        // Repositories
        // Always use in-memory database for the Testing environment, regardless of configuration
        var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDB") ||
                            builder.Environment.IsEnvironment("Testing");

        builder.Services.AddScoped<IUserRepository, UserRepository>();

        // Authentication & Authorization
        builder.Services.AddJwtAuthenticationAndAuthorization(builder.Configuration, builder.Environment);

        // Application use cases
        builder.Services.AddUseCases();

        // Admin user seeding configuration and services
        builder.Services.Configure<AdminUserOptions>(
            builder.Configuration.GetSection(AdminUserOptions.SectionName));
        builder.Services.AddSingleton<IFirebaseAdminClient, FirebaseAdminClient>();
        builder.Services.AddScoped<IAdminUserBootstrapper, AdminUserBootstrapper>();

        if (useInMemoryDb)
        {
            // we could have written that logic here but as per clean architecture, we are separating these into their own piece of code
            builder.Services.AddInMemoryDatabase();
        }
        else
        {
            // Use PostgreSQL as the real database when not using the in-memory provider
            var connectionString = builder.Configuration.GetConnectionString("DbContext")
                                   ?? throw new InvalidOperationException(
                                       "PostgreSQL connection string 'DbContext' is missing.");

            builder.Services.AddPostgresDatabase(connectionString);
        }

        var app = builder.Build();

        //Swagger
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        // CORS must run before authentication/authorization so that preflight
        // and actual requests receive the proper headers.
        app.UseCors(clientAppCorsPolicyName);

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Apply EF Core migrations for relational database providers only. This replaces
        // EnsureCreated so that schema changes (e.g., new columns for soft delete or roles)
        // are handled via migrations instead of ad-hoc schema creation.
        //
        // In tests and other scenarios that use the in-memory provider, calling Migrate()
        // would throw (relational-only API). Guard against that by checking IsRelational().
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }

        // In the Testing environment, skip admin seeding entirely so that integration tests
        // do not require real Firebase Admin credentials. In all other environments, perform
        // the idempotent admin bootstrap.
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!hostEnvironment.IsEnvironment("Testing"))
        {
            // Seed the initial admin user if configured. This operation is idempotent and
            // relies on Firebase custom claims for authorization, with a corresponding
            // domain user record for reporting and future domain logic.
            var adminBootstrapper = scope.ServiceProvider.GetRequiredService<IAdminUserBootstrapper>();
            adminBootstrapper.SeedAdminUserAsync().GetAwaiter().GetResult();
        }

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
