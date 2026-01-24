using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class SoftDeleteUserRepositoryTests
{
    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteUser_WhenUserExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "SoftDeleteUserRepositoryTests_Delete")
            .Options;

        await using var context = new AppDbContext(options);

        var email = Email.Create("user@example.com");
        var name = UserName.Create("Test User");
        var externalAuthId = ExternalAuthIdentifier.Create("provider|123");
        var user = User.Create(email, name, externalAuthId);

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync(CancellationToken.None);

        var repository = new UserRepository(context);

        // Act
        await repository.DeleteAsync(user.Id, CancellationToken.None);

        // Assert
        // With the global query filter, the user should no longer be returned by normal queries.
        var fromRepo = await repository.GetByIdAsync(user.Id, CancellationToken.None);
        fromRepo.Should().BeNull();

        // But ignoring query filters, the row should still exist and be marked as deleted.
        var fromDb = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id, CancellationToken.None);

        fromDb.Should().NotBeNull();
        fromDb!.IsDeleted.Should().BeTrue();
        fromDb.DeletedAt.Should().NotBeNull();
    }
}
