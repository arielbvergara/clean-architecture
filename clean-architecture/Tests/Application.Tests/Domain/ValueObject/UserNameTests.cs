using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class UserNameTests
{
    [Fact]
    public void Create_ShouldCreateUserName_WhenValueIsValid()
    {
        // Arrange
        var input = " John Doe ";

        // Act
        var userName = UserName.Create(input);

        // Assert
        userName.Value.Should().Be("John Doe");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenValueIsNullOrWhitespace(string? value)
    {
        // Act
        var act = () => UserName.Create(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("User name cannot be empty*");
    }

    [Theory]
    [InlineData("A")]
    public void Create_ShouldThrowArgumentException_WhenValueIsShorterThanMinimum(string value)
    {
        // Act
        var act = () => UserName.Create(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("User name must be at least 2 characters*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenValueExceedsMaximumLength()
    {
        // Arrange
        var input = new string('a', 101);

        // Act
        var act = () => UserName.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("User name cannot exceed 100 characters*");
    }
}
