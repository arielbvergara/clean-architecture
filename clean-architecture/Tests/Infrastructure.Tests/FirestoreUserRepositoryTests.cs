using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;
using Moq;
using Xunit;

namespace Infrastructure.Tests;

public class FirestoreUserRepositoryTests
{
    private const string DefaultEmail = "user@example.com";
    private const string DefaultName = "Test User";
    private const string DefaultExternalAuthId = "provider|123";

    private static User CreateUser()
    {
        var email = Email.Create(DefaultEmail);
        var name = UserName.Create(DefaultName);
        var externalAuthId = ExternalAuthIdentifier.Create(DefaultExternalAuthId);

        return User.Create(email, name, externalAuthId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();
        var userId = user.Id;

        dataStoreMock
            .Setup(store => store.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await repository.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(user);
        dataStoreMock.Verify(store => store.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();
        var email = user.Email;

        dataStoreMock
            .Setup(store => store.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await repository.GetByEmailAsync(email, CancellationToken.None);

        // Assert
        result.Should().Be(user);
        dataStoreMock.Verify(store => store.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByExternalAuthIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();
        var externalAuthId = user.ExternalAuthId;

        dataStoreMock
            .Setup(store => store.GetByExternalAuthIdAsync(externalAuthId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await repository.GetByExternalAuthIdAsync(externalAuthId, CancellationToken.None);

        // Assert
        result.Should().Be(user);
        dataStoreMock.Verify(store => store.GetByExternalAuthIdAsync(externalAuthId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnItemsAndTotalCount_WhenDataStoreReturnsResults()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();
        var users = new List<User> { user };
        var criteria = new UserQueryCriteria(
            SearchTerm: null,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 10,
            IsDeletedFilter: null);

        dataStoreMock
            .Setup(store => store.GetPagedAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users.AsReadOnly(), users.Count));

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        items.Should().ContainSingle().Which.Should().Be(user);
        dataStoreMock.Verify(store => store.GetPagedAsync(criteria, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnUser_WhenDataStoreAddsUser()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();

        dataStoreMock
            .Setup(store => store.AddAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await repository.AddAsync(user, CancellationToken.None);

        // Assert
        result.Should().Be(user);
        dataStoreMock.Verify(store => store.AddAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldForwardToDataStore_WhenCalled()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();

        // Act
        await repository.UpdateAsync(user, CancellationToken.None);

        // Assert
        dataStoreMock.Verify(store => store.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallSoftDeleteAsync_WhenCalled()
    {
        // Arrange
        var dataStoreMock = new Mock<IFirestoreUserDataStore>();
        var repository = new FirestoreUserRepository(dataStoreMock.Object);

        var user = CreateUser();
        var userId = user.Id;

        // Act
        await repository.DeleteAsync(userId, CancellationToken.None);

        // Assert
        dataStoreMock.Verify(store => store.SoftDeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
