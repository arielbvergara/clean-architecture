using Application.Dtos.User;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByExternalAuthIdAsync(ExternalAuthIdentifier externalAuthId, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>()
            .FirstOrDefaultAsync(u => u.ExternalAuthId == externalAuthId, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        UserQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // Load the current user set from the database and apply search, ordering, and
        // pagination in memory. This keeps the repository simple and avoids complex
        // provider-specific translation issues for value objects while still honoring
        // the configured global query filters (e.g. soft delete).
        var users = await context.Set<User>()
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
 
        IEnumerable<User> filtered = users;
 
        if (criteria.IsDeletedFilter.HasValue)
        {
            filtered = filtered.Where(user => user.IsDeleted == criteria.IsDeletedFilter.Value);
        }
 
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.Trim();

            Guid? parsedId = null;
            if (Guid.TryParse(searchTerm, out var guidValue))
            {
                parsedId = guidValue;
            }

            filtered = filtered.Where(user =>
                (parsedId.HasValue && user.Id.Value == parsedId.Value) ||
                user.Email.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                user.Name.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        filtered = ApplyOrdering(filtered, criteria);

        var totalCount = filtered.Count();

        var skip = (criteria.PageNumber - 1) * criteria.PageSize;

        var items = filtered
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Set<User>().AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Set<User>().Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.MarkDeleted();
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<User> ApplyOrdering(IEnumerable<User> query, UserQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (UserSortField.Email, SortDirection.Ascending) =>
                query.OrderBy(user => user.Email.Value)
                     .ThenBy(user => user.Id.Value),
            (UserSortField.Email, SortDirection.Descending) =>
                query.OrderByDescending(user => user.Email.Value)
                     .ThenByDescending(user => user.Id.Value),
            (UserSortField.Name, SortDirection.Ascending) =>
                query.OrderBy(user => user.Name.Value)
                     .ThenBy(user => user.Id.Value),
            (UserSortField.Name, SortDirection.Descending) =>
                query.OrderByDescending(user => user.Name.Value)
                     .ThenByDescending(user => user.Id.Value),
            (UserSortField.CreatedAt, SortDirection.Descending) =>
                query.OrderByDescending(user => user.CreatedAt)
                     .ThenByDescending(user => user.Id.Value),
            _ =>
                query.OrderBy(user => user.CreatedAt)
                     .ThenBy(user => user.Id.Value)
        };
    }
}
