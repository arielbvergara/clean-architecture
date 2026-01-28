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

public class DeleteUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldDeleteUser_WhenCallerIsOwner()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var ownershipServiceMock = new Mock<IUserOwnershipService>();
        var useCase = new DeleteUserUseCase(repositoryMock.Object, ownershipServiceMock.Object);

        var targetUserId = Guid.NewGuid();
        var externalAuthId = ExternalAuthIdentifier.Create("provider|owner-123");
        var user = DomainUser.Create(
            Email.Create("owner@example.com"),
            UserName.Create("Owner"),
            externalAuthId);

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        ownershipServiceMock
            .Setup(s => s.EnsureOwnerOrAdminAsync(
                user,
                It.IsAny<CurrentUserContext?>(),
                targetUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppException?)null);

        var currentUserContext = new CurrentUserContext(externalAuthId.Value, UserRoleConstants.User);
        var request = new DeleteUserRequest(targetUserId, currentUserContext);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        repositoryMock.Verify(r => r.DeleteAsync(It.Is<UserId>(id => id.Value == targetUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteUser_WhenCallerIsAdmin()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var ownershipServiceMock = new Mock<IUserOwnershipService>();
        var useCase = new DeleteUserUseCase(repositoryMock.Object, ownershipServiceMock.Object);

        var targetUserId = Guid.NewGuid();
        var externalAuthId = ExternalAuthIdentifier.Create("provider|user-123");
        var user = DomainUser.Create(
            Email.Create("user@example.com"),
            UserName.Create("User"),
            externalAuthId);

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        ownershipServiceMock
            .Setup(s => s.EnsureOwnerOrAdminAsync(
                user,
                It.IsAny<CurrentUserContext?>(),
                targetUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppException?)null);

        var currentUserContext = new CurrentUserContext("provider|admin-456", UserRoleConstants.Admin);
        var request = new DeleteUserRequest(targetUserId, currentUserContext);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        repositoryMock.Verify(r => r.DeleteAsync(It.Is<UserId>(id => id.Value == targetUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenCallerIsNonOwnerNonAdmin()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var ownershipServiceMock = new Mock<IUserOwnershipService>();
        var useCase = new DeleteUserUseCase(repositoryMock.Object, ownershipServiceMock.Object);

        var targetUserId = Guid.NewGuid();
        var targetExternalAuthId = ExternalAuthIdentifier.Create("provider|target-123");
        var targetUser = DomainUser.Create(
            Email.Create("target@example.com"),
            UserName.Create("Target"),
            targetExternalAuthId);

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        ownershipServiceMock
            .Setup(s => s.EnsureOwnerOrAdminAsync(
                targetUser,
                It.IsAny<CurrentUserContext?>(),
                targetUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotFoundException("User", targetUserId));

        var callerExternalAuthId = ExternalAuthIdentifier.Create("provider|caller-999");
        var currentUserContext = new CurrentUserContext(callerExternalAuthId.Value, UserRoleConstants.User);
        var request = new DeleteUserRequest(targetUserId, currentUserContext);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var ownershipServiceMock = new Mock<IUserOwnershipService>();
        var useCase = new DeleteUserUseCase(repositoryMock.Object, ownershipServiceMock.Object);

        var targetUserId = Guid.NewGuid();

        repositoryMock
            .Setup(r => r.GetByIdAsync(It.Is<UserId>(id => id.Value == targetUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainUser?)null);

        var request = new DeleteUserRequest(targetUserId, null);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
