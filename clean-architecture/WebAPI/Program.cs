using Microsoft.OpenApi;
using Application.Interfaces;
using Application.UseCases;
using Infrastructure.Data;
using Infrastructure.Repositories;
using WebAPI.Authentication;
using WebAPI.Filters;

namespace WebAPI;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers(options => { options.Filters.Add<GlobalExceptionFilter>(); });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        const string bearerSchemeId = "bearer";

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CleanArchitecture API",
                Version = "v1"
            });

            // Enable JWT bearer token support in Swagger UI
            options.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // lowercase per RFC 7235
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

        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
            app.UseSwagger();
            app.UseSwaggerUI();
        // }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        if (useInMemoryDb)
        {
            // Seed data. Use this config for in memory database
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }
        else
        {
            // Automatically create the database if it does not exist. This is required only for real database
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated(); // This creates the database if it doesn't exist
        }

        app.Run();
    }
}