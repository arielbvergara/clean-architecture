using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;
using TodoApp.Domain.ValueObject;

namespace TodoApp.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value));

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(u => u.Name)
            .HasConversion(
                name => name.Value,
                value => UserName.Create(value));

        builder.Property(u => u.ExternalAuthId)
            .HasConversion(
                externalId => externalId.Value,
                value => ExternalAuthIdentifier.Create(value));

        // // Seed data - values here are the underlying scalar column types
        // builder.HasData(
        //     new
        //     {
        //         Id = Guid.Parse("df9c7405-1f1e-4968-92b4-7b827c53ceda"),
        //         Email = "john@example.com",
        //         Name = "John Doe",
        //         ExternalAuthId = "auth0|john",
        //         CreatedAt = DateTime.UtcNow,
        //         UpdatedAt = (DateTime?)null
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("df9c7405-1f1e-4968-92b4-7b827c53cedb"),
        //         Email = "jane@example.com",
        //         Name = "Jane Smith",
        //         ExternalAuthId = "auth0|jane",
        //         CreatedAt = DateTime.UtcNow,
        //         UpdatedAt = (DateTime?)null
        //     }
        // );
    }
}
