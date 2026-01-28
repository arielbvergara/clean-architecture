using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.User;
using Domain.Constants;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainUser = Domain.Entities.User;

namespace Application.Tests.UseCases.User;

public class UpdateUserNameUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldUpdateName_WhenCallerIsOwner()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var useCase = new UpdateUserNameUseCase(repositoryMock.Object);

        var targetUserId = Guid.NewGuid();
        var externalAuthId = ExternalAuthIdentifier.Create("provider|owner-123");
        var user = DomainUser.Create(
            Email.Create("owner@example.com"),
            UserName.Create("Original Name"),
            externalAuthId);

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repositoryMock
            .Setup(r => r.GetByExternalAuthIdAsync(It.Is<ExternalAuthIdentifier>(e => e.Value == externalAuthId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var currentUserContext = new CurrentUserContext(externalAuthId.Value, UserRoleConstants.User);
        var request = new UpdateUserNameRequest(targetUserId, "New Name", currentUserContext);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        repositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateName_WhenCallerIsAdmin()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var useCase = new UpdateUserNameUseCase(repositoryMock.Object);

        var targetUserId = Guid.NewGuid();
        var externalAuthId = ExternalAuthIdentifier.Create("provider|user-123");
        var user = DomainUser.Create(
            Email.Create("user@example.com"),
            UserName.Create("Original Name"),
            externalAuthId);

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var currentUserContext = new CurrentUserContext("provider|admin-456", UserRoleConstants.Admin);
        var request = new UpdateUserNameRequest(targetUserId, "Admin Updated Name", currentUserContext);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Admin Updated Name");
        repositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenCallerIsNonOwnerNonAdmin()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var useCase = new UpdateUserNameUseCase(repositoryMock.Object);

        var targetUserId = Guid.NewGuid();
        var targetExternalAuthId = ExternalAuthIdentifier.Create("provider|target-123");
        var targetUser = DomainUser.Create(
            Email.Create("target@example.com"),
            UserName.Create("Target"),
            targetExternalAuthId);

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        // current user resolved from repository is a different user
        var callerExternalAuthId = ExternalAuthIdentifier.Create("provider|caller-999");
        var callerUser = DomainUser.Create(
            Email.Create("caller@example.com"),
            UserName.Create("Caller"),
            callerExternalAuthId);

        repositoryMock
            .Setup(r => r.GetByExternalAuthIdAsync(
                It.Is<ExternalAuthIdentifier>(e => e.Value == callerExternalAuthId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(callerUser);

        var currentUserContext = new CurrentUserContext(callerExternalAuthId.Value, UserRoleConstants.User);
        var request = new UpdateUserNameRequest(targetUserId, "New Name", currentUserContext);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var useCase = new UpdateUserNameUseCase(repositoryMock.Object);

        var targetUserId = Guid.NewGuid();

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.Is<UserId>(id => id.Value == targetUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainUser?)null);

        var request = new UpdateUserNameRequest(targetUserId, "New Name", null);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
