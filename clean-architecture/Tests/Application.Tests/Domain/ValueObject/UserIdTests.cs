using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class UserIdTests
{
    [Fact]
    public void Create_ShouldCreateUserId_WhenGuidIsValid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var userId = UserId.Create(guid);

        // Assert
        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenGuidIsEmpty()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var act = () => UserId.Create(guid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("User ID cannot be empty*");
    }

    [Fact]
    public void NewId_ShouldCreateUserIdWithNonEmptyGuid_WhenCalled()
    {
        // Act
        var userId = UserId.NewId();

        // Assert
        userId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_ShouldCreateUniqueIds_WhenCalledMultipleTimes()
    {
        // Act
        var first = UserId.NewId();
        var second = UserId.NewId();

        // Assert
        first.Value.Should().NotBe(Guid.Empty);
        second.Value.Should().NotBe(Guid.Empty);
        second.Value.Should().NotBe(first.Value);
    }
}
