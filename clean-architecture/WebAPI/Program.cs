using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Application.UseCases;
using Infrastructure.Data;
using Infrastructure.Repositories;
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
        builder.Services.AddSwaggerGen();

        // Repositories
        // Always use in-memory database for the Testing environment, regardless of configuration
        var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDB") ||
                            builder.Environment.IsEnvironment("Testing");

        builder.Services.AddScoped<IUserRepository, UserRepository>();

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