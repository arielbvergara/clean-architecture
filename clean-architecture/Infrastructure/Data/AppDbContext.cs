using Domain.Entities;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    //pass connection string from configuration/programe.cs
    //All these are tables which are entities in domain layer. this way we can access the database tables
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // Exclude soft-deleted users from all queries by default.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        base.OnModelCreating(modelBuilder); // Call to the base method
    }
}
