using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Configurations;

namespace TodoApp.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    //pass connection string from configuration/programe.cs

    //All these are tables which are entities in domain layer. this way we can access the database tables
    public DbSet<ToDoList> ToDoLists { get; set; }
    public DbSet<ToDoItem> ToDoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ToDoListConfiguration());
        modelBuilder.ApplyConfiguration(new ToDoItemConfiguration());
        base.OnModelCreating(modelBuilder); // Call to the base method
    }
}