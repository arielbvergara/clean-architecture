using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Create_ShouldCreateUserWithExpectedValues_WhenValidArgumentsProvided()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var name = UserName.Create("Test User");
        var externalAuthId = ExternalAuthIdentifier.Create("provider|123");

        var before = DateTime.UtcNow;

        // Act
        var user = User.Create(email, name, externalAuthId);

        var after = DateTime.UtcNow;

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.ExternalAuthId.Should().Be(externalAuthId);

        user.Id.Should().NotBe(null);
        user.Id.Value.Should().NotBe(Guid.Empty);

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        user.UpdatedAt.Should().BeNull();

        user.Role.Should().Be("User");
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkDeleted_ShouldSetSoftDeleteFields_WhenUserIsActive()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var name = UserName.Create("Test User");
        var externalAuthId = ExternalAuthIdentifier.Create("provider|123");
        var user = User.Create(email, name, externalAuthId);

        var before = DateTime.UtcNow;

        // Act
        user.MarkDeleted();

        var after = DateTime.UtcNow;

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        user.DeletedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkDeleted_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var name = UserName.Create("Test User");
        var externalAuthId = ExternalAuthIdentifier.Create("provider|123");
        var user = User.Create(email, name, externalAuthId);

        user.MarkDeleted();
        var firstDeletedAt = user.DeletedAt;

        // Act
        user.MarkDeleted();

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().Be(firstDeletedAt);
    }

    [Fact]
    public void UpdateName_ShouldUpdateNameAndSetUpdatedAt_WhenCalledWithNewValidName()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var originalName = UserName.Create("Original Name");
        var externalAuthId = ExternalAuthIdentifier.Create("provider|123");
        var user = User.Create(email, originalName, externalAuthId);
        var originalCreatedAt = user.CreatedAt;

        var newName = UserName.Create("Updated Name");
        var before = DateTime.UtcNow;

        // Act
        user.UpdateName(newName);

        var after = DateTime.UtcNow;

        // Assert
        user.Name.Should().Be(newName);
        user.CreatedAt.Should().Be(originalCreatedAt);

        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateName_ShouldUpdateUpdatedAt_WhenCalledMultipleTimes()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var originalName = UserName.Create("Original Name");
        var externalAuthId = ExternalAuthIdentifier.Create("provider|123");
        var user = User.Create(email, originalName, externalAuthId);

        var firstNewName = UserName.Create("First Name");
        user.UpdateName(firstNewName);

        var secondNewName = UserName.Create("Second Name");
        var beforeSecondUpdate = DateTime.UtcNow;

        // Act
        user.UpdateName(secondNewName);

        var afterSecondUpdate = DateTime.UtcNow;

        // Assert
        user.Name.Should().Be(secondNewName);
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeOnOrAfter(beforeSecondUpdate).And.BeOnOrBefore(afterSecondUpdate);
    }
}
