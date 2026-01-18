using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Application.UseCases;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;
using TodoApp.WebAPI.Filters;

namespace TodoApp.WebAPI;

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
        var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDB");
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
            //use this for real database on your sql server
            builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(
                        builder.Configuration.GetConnectionString("DbContext"),
                        providerOptions => providerOptions.EnableRetryOnFailure()
                    );
                }
            );
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

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