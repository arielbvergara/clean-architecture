using Application.Dtos.User;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Repositories;

public sealed class FirestoreUserRepository(IFirestoreUserDataStore dataStore) : IUserRepository
{
    private readonly IFirestoreUserDataStore _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByIdAsync(id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByEmailAsync(email, cancellationToken);
    }

    public Task<User?> GetByExternalAuthIdAsync(
        ExternalAuthIdentifier externalAuthId,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByExternalAuthIdAsync(externalAuthId, cancellationToken);
    }

    public Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        UserQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetPagedAsync(criteria, cancellationToken);
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dataStore.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dataStore.UpdateAsync(user, cancellationToken);
    }

    public Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return _dataStore.SoftDeleteAsync(id, cancellationToken);
    }
}
