using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.User;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainUser = Domain.Entities.User;

namespace Application.Tests.UseCases.User;

public class CreateAdminUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldEnsureAdminClaims_WhenUserAlreadyExists()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var identityProviderServiceMock = new Mock<IIdentityProviderService>();
        var useCase = new CreateAdminUserUseCase(userRepositoryMock.Object, identityProviderServiceMock.Object);

        const string email = "admin-existing@example.com";
        const string password = "StrongPassword!123";
        const string name = "Existing Admin";

        var existingUser = DomainUser.CreateAdmin(
            Email.Create(email),
            UserName.Create(name),
            ExternalAuthIdentifier.Create("external-existing-admin"));

        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        identityProviderServiceMock
            .Setup(s => s.EnsureAdminUserAsync(email, password, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync("external-existing-admin");

        var request = new CreateAdminUserRequest(email, name, password);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existingUser.Id.Value);
        result.Value.Email.Should().Be(existingUser.Email.Value);
        result.Value.Name.Should().Be(existingUser.Name.Value);
        result.Value.ExternalAuthId.Should().Be(existingUser.ExternalAuthId.Value);

        identityProviderServiceMock.Verify(
            s => s.EnsureAdminUserAsync(email, password, name, It.IsAny<CancellationToken>()),
            Times.Once);

        userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateAdminUser_WhenUserDoesNotExist()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var identityProviderServiceMock = new Mock<IIdentityProviderService>();
        var useCase = new CreateAdminUserUseCase(userRepositoryMock.Object, identityProviderServiceMock.Object);

        const string email = "admin-new@example.com";
        const string password = "StrongPassword!456";
        const string name = "New Admin";
        const string externalId = "external-new-admin";

        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainUser?)null);

        identityProviderServiceMock
            .Setup(s => s.EnsureAdminUserAsync(email, password, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        DomainUser? addedUser = null;
        userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()))
            .Callback<DomainUser, CancellationToken>((user, _) => addedUser = user)
            .Returns((DomainUser user, CancellationToken _) => Task.FromResult(user));

        var request = new CreateAdminUserRequest(email, name, password);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(email.ToLowerInvariant());
        result.Value.Name.Should().Be(name);
        result.Value.ExternalAuthId.Should().Be(externalId);

        identityProviderServiceMock.Verify(
            s => s.EnsureAdminUserAsync(email, password, name, It.IsAny<CancellationToken>()),
            Times.Once);

        userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()),
            Times.Once);

        addedUser.Should().NotBeNull();
        addedUser!.Email.Value.Should().Be(email.ToLowerInvariant());
        addedUser.Name.Value.Should().Be(name);
        addedUser.ExternalAuthId.Value.Should().Be(externalId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenEmailIsInvalid()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var identityProviderServiceMock = new Mock<IIdentityProviderService>();
        var useCase = new CreateAdminUserUseCase(userRepositoryMock.Object, identityProviderServiceMock.Object);

        const string invalidEmail = "not-an-email";
        const string password = "StrongPassword!789";
        const string name = "Invalid Email Admin";

        var request = new CreateAdminUserRequest(invalidEmail, name, password);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<ValidationException>();

        userRepositoryMock.Verify(
            r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()),
            Times.Never);

        identityProviderServiceMock.Verify(
            s => s.EnsureAdminUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInfraException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var identityProviderServiceMock = new Mock<IIdentityProviderService>();
        var useCase = new CreateAdminUserUseCase(userRepositoryMock.Object, identityProviderServiceMock.Object);

        const string email = "admin-error@example.com";
        const string password = "StrongPassword!000";
        const string name = "Error Admin";

        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected repository failure"));

        var request = new CreateAdminUserRequest(email, name, password);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<InfraException>();
        result.Error!.InnerException.Should().NotBeNull();
        result.Error.InnerException!.Message.Should().Be("Unexpected repository failure");
    }
}
