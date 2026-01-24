using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalAuthIdAsync(ExternalAuthIdentifier externalAuthId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        UserQueryCriteria criteria,
        CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserId id, CancellationToken cancellationToken = default);
}
