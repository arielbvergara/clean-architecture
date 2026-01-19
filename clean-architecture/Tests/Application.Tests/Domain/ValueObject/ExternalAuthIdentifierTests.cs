using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class ExternalAuthIdentifierTests
{
    [Fact]
    public void Create_ShouldCreateExternalAuthIdentifier_WhenValueIsValid()
    {
        // Arrange
        var input = "  provider|123  ";

        // Act
        var externalId = ExternalAuthIdentifier.Create(input);

        // Assert
        externalId.Value.Should().Be("provider|123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenValueIsNullOrWhitespace(string? value)
    {
        // Act
        var act = () => ExternalAuthIdentifier.Create(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("External auth identifier cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenValueExceedsMaximumLength()
    {
        // Arrange
        var input = new string('a', 256);

        // Act
        var act = () => ExternalAuthIdentifier.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("External auth identifier cannot exceed 255 characters*");
    }
}
