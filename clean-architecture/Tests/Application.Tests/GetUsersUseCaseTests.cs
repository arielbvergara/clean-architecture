using Application.Dtos.User;
using Application.Interfaces;
using Application.UseCases.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.Tests;

public class GetUsersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnPagedUsers_WhenCriteriaIsValid()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var useCase = new GetUsersUseCase(userRepositoryMock.Object);

        var user = User.Create(
            Email.Create("user@example.com"),
            UserName.Create("User"),
            ExternalAuthIdentifier.Create("external-id"));

        var criteria = new UserQueryCriteria(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            20,
            null);

        userRepositoryMock
            .Setup(repo => repo.GetPagedAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User> { user }, 1));

        var request = new GetUsersRequest(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            20,
            null,
            null);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        var singleUser = result.Value.Items.Single();
        singleUser.Email.Should().Be("user@example.com");
        singleUser.IsDeleted.Should().BeFalse();
        result.Value.Pagination.TotalItems.Should().Be(1);
        result.Value.Pagination.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldClampPageSize_WhenRequestedSizeIsTooLarge()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var useCase = new GetUsersUseCase(userRepositoryMock.Object);

        var request = new GetUsersRequest(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            5000,
            null,
            null);

        UserQueryCriteria? capturedCriteria = null;
        userRepositoryMock
            .Setup(repo => repo.GetPagedAsync(It.IsAny<UserQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<UserQueryCriteria, CancellationToken>((c, _) => capturedCriteria = c)
            .ReturnsAsync((Array.Empty<User>(), 0));

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.PageSize.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizePageNumber_WhenRequestedPageIsLessThanOne()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var useCase = new GetUsersUseCase(userRepositoryMock.Object);

        var request = new GetUsersRequest(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            0,
            10,
            null,
            null);

        UserQueryCriteria? capturedCriteria = null;
        userRepositoryMock
            .Setup(repo => repo.GetPagedAsync(It.IsAny<UserQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<UserQueryCriteria, CancellationToken>((c, _) => capturedCriteria = c)
            .ReturnsAsync((Array.Empty<User>(), 0));

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassIsDeletedFilterToCriteria_WhenFilterIsProvided()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var useCase = new GetUsersUseCase(userRepositoryMock.Object);

        var request = new GetUsersRequest(
            null,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            1,
            10,
            true,
            null);

        UserQueryCriteria? capturedCriteria = null;
        userRepositoryMock
            .Setup(repo => repo.GetPagedAsync(It.IsAny<UserQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<UserQueryCriteria, CancellationToken>((c, _) => capturedCriteria = c)
            .ReturnsAsync((Array.Empty<User>(), 0));

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.IsDeletedFilter.Should().BeTrue();
    }
}
