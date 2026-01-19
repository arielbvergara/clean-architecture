using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class EmailTests
{
    [Fact]
    public void Create_ShouldCreateNormalizedEmail_WhenValueIsValid()
    {
        // Arrange
        var input = "  USER@example.COM  ";

        // Act
        var email = Email.Create(input);

        // Assert
        email.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenValueIsNullOrWhitespace(string? value)
    {
        // Act
        var act = () => Email.Create(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenValueExceedsMaximumLength()
    {
        // Arrange
        var localPart = new string('a', 245);
        var input = $"{localPart}@example.com"; // > 254 total characters

        // Act
        var act = () => Email.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot exceed 254 characters*");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("missing-at.example.com")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public void Create_ShouldThrowArgumentException_WhenFormatIsInvalid(string value)
    {
        // Act
        var act = () => Email.Create(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email format is invalid*");
    }
}
