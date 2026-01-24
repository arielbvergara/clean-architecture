using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class UserRepositoryGetPagedAsyncTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults_WhenMultipleUsersExist()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        for (var index = 0; index < 25; index++)
        {
            var email = Email.Create($"user{index}@example.com");
            var name = UserName.Create($"User {index}");
            var externalId = ExternalAuthIdentifier.Create($"external-{index}");

            var user = User.Create(email, name, externalId);
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        var criteria = new UserQueryCriteria(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            2,
            10,
            null);

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        // Assert
        totalCount.Should().Be(25);
        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterBySearchTerm_WhenSearchTermMatchesEmail()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        var matchingUser = User.Create(
            Email.Create("match@example.com"),
            UserName.Create("Match User"),
            ExternalAuthIdentifier.Create("match-external"));

        var otherUser = User.Create(
            Email.Create("other@example.com"),
            UserName.Create("Other"),
            ExternalAuthIdentifier.Create("other-external"));

        context.Users.Add(matchingUser);
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        var criteria = new UserQueryCriteria(
            "match",
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            10,
            null);

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        items.Should().ContainSingle();
        items.Single().Email.Value.Should().Be("match@example.com");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnOnlyDeletedUsers_WhenIsDeletedFilterIsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        for (var index = 0; index < 3; index++)
        {
            var email = Email.Create($"deleted{index}@example.com");
            var name = UserName.Create($"Deleted {index}");
            var externalId = ExternalAuthIdentifier.Create($"deleted-{index}");

            var user = User.Create(email, name, externalId);
            user.MarkDeleted();
            context.Users.Add(user);
        }

        for (var index = 0; index < 2; index++)
        {
            var email = Email.Create($"active{index}@example.com");
            var name = UserName.Create($"Active {index}");
            var externalId = ExternalAuthIdentifier.Create($"active-{index}");

            var user = User.Create(email, name, externalId);
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        var criteria = new UserQueryCriteria(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            10,
            true);

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(3);
        items.Should().OnlyContain(user => user.IsDeleted);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnOnlyActiveUsers_WhenIsDeletedFilterIsFalse()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        for (var index = 0; index < 3; index++)
        {
            var email = Email.Create($"deleted{index}@example.com");
            var name = UserName.Create($"Deleted {index}");
            var externalId = ExternalAuthIdentifier.Create($"deleted-{index}");

            var user = User.Create(email, name, externalId);
            user.MarkDeleted();
            context.Users.Add(user);
        }

        for (var index = 0; index < 2; index++)
        {
            var email = Email.Create($"active{index}@example.com");
            var name = UserName.Create($"Active {index}");
            var externalId = ExternalAuthIdentifier.Create($"active-{index}");

            var user = User.Create(email, name, externalId);
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        var criteria = new UserQueryCriteria(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            10,
            false);

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(user => !user.IsDeleted);
    }
}
