using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Application.Interfaces;
using Application.UseCases;
using Infrastructure.Data;
using Infrastructure.Repositories;
using WebAPI.Authentication;
using WebAPI.Filters;

namespace WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers(options => { options.Filters.Add<GlobalExceptionFilter>(); });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        const string bearerSchemeId = "bearer"; // lowercase per RFC 7235

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
        // Repositories
        // Always use in-memory database for the Testing environment, regardless of configuration
        var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDB") ||
                            builder.Environment.IsEnvironment("Testing");

        builder.Services.AddScoped<IUserRepository, UserRepository>();

        // Authentication & Authorization
        builder.Services.AddJwtAuthenticationAndAuthorization(builder.Configuration, builder.Environment);

        // Application use cases
        builder.Services.AddUseCases();

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

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHttpsRedirection();
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

        app.Run();
    }
}