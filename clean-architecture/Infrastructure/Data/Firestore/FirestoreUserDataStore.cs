using System.Reflection;
using Domain.Entities;
using Domain.ValueObject;
using Google.Cloud.Firestore;
using Application.Dtos.User;

namespace Infrastructure.Data.Firestore;

public sealed class FirestoreUserDataStore : IFirestoreUserDataStore
{
    private static readonly ConstructorInfo UserRehydrationConstructor = typeof(User)
        .GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            new[]
            {
                typeof(UserId),
                typeof(Email),
                typeof(UserName),
                typeof(ExternalAuthIdentifier),
                typeof(string),
                typeof(DateTime),
                typeof(bool),
                typeof(DateTime?)
            },
            modifiers: null
        )
        ?? throw new InvalidOperationException("User rehydration constructor not found.");

    private static readonly PropertyInfo UpdatedAtProperty = typeof(User)
        .GetProperty("UpdatedAt", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException("User.UpdatedAt property not found.");

    private readonly FirestoreDb _database;

    public FirestoreUserDataStore(FirestoreDb database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return document is null || document.IsDeleted
            ? null
            : MapToDomainUser(document);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var snapshot = await _database.Collection(FirestoreCollectionNames.Users)
            .WhereEqualTo(nameof(UserDocument.Email), email.Value)
            .Limit(1)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var document = snapshot.Documents
            .Select(d => d.ConvertTo<UserDocument>())
            .Where(doc => doc is not null)
            .FirstOrDefault(doc => !doc!.IsDeleted);

        return document is null ? null : MapToDomainUser(document);
    }

    public async Task<User?> GetByExternalAuthIdAsync(
        ExternalAuthIdentifier externalAuthId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _database.Collection(FirestoreCollectionNames.Users)
            .WhereEqualTo(nameof(UserDocument.ExternalAuthId), externalAuthId.Value)
            .Limit(1)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var document = snapshot.Documents
            .Select(d => d.ConvertTo<UserDocument>())
            .Where(doc => doc is not null)
            .FirstOrDefault(doc => !doc!.IsDeleted);

        return document is null ? null : MapToDomainUser(document);
    }

    public async Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        UserQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _database.Collection(FirestoreCollectionNames.Users)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var documents = snapshot.Documents
            .Select(d => d.ConvertTo<UserDocument>())
            .Where(doc => doc is not null)
            .ToList();

        IEnumerable<UserDocument> filtered = documents;

        if (criteria.IsDeletedFilter.HasValue)
        {
            filtered = filtered.Where(document => document.IsDeleted == criteria.IsDeletedFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.Trim();

            Guid? parsedId = null;
            if (Guid.TryParse(searchTerm, out var guidValue))
            {
                parsedId = guidValue;
            }

            filtered = filtered.Where(document =>
                (parsedId.HasValue && Guid.TryParse(document.Id, out var documentId) && documentId == parsedId.Value) ||
                document.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                document.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        filtered = ApplyOrdering(filtered, criteria);

        var users = filtered
            .Select(MapToDomainUser)
            .ToList();

        var totalCount = users.Count;

        var skip = (criteria.PageNumber - 1) * criteria.PageSize;

        var items = users
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(user);

        var documentReference = _database
            .Collection(FirestoreCollectionNames.Users)
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(user);

        var documentReference = _database
            .Collection(FirestoreCollectionNames.Users)
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (document is null || document.IsDeleted)
        {
            return;
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;

        var documentReference = _database
            .Collection(FirestoreCollectionNames.Users)
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<UserDocument?> GetDocumentByIdAsync(UserId id, CancellationToken cancellationToken)
    {
        var documentReference = _database
            .Collection(FirestoreCollectionNames.Users)
            .Document(id.Value.ToString());

        var snapshot = await documentReference.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<UserDocument>();
        return document ?? null;
    }

    private static IEnumerable<UserDocument> ApplyOrdering(
        IEnumerable<UserDocument> documents,
        UserQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (UserSortField.Email, SortDirection.Ascending) =>
                documents.OrderBy(user => user.Email)
                    .ThenBy(user => user.Id),
            (UserSortField.Email, SortDirection.Descending) =>
                documents.OrderByDescending(user => user.Email)
                    .ThenByDescending(user => user.Id),
            (UserSortField.Name, SortDirection.Ascending) =>
                documents.OrderBy(user => user.Name)
                    .ThenBy(user => user.Id),
            (UserSortField.Name, SortDirection.Descending) =>
                documents.OrderByDescending(user => user.Name)
                    .ThenByDescending(user => user.Id),
            (UserSortField.CreatedAt, SortDirection.Descending) =>
                documents.OrderByDescending(user => user.CreatedAt)
                    .ThenByDescending(user => user.Id),
            _ =>
                documents.OrderBy(user => user.CreatedAt)
                    .ThenBy(user => user.Id)
        };
    }

    private static User MapToDomainUser(UserDocument document)
    {
        var id = UserId.Create(Guid.Parse(document.Id));
        var email = Email.Create(document.Email);
        var name = UserName.Create(document.Name);
        var externalAuthId = ExternalAuthIdentifier.Create(document.ExternalAuthId);

        var user = (User)UserRehydrationConstructor.Invoke(
            new object[]
            {
                id,
                email,
                name,
                externalAuthId,
                document.Role,
                document.CreatedAt,
                document.IsDeleted,
                document.DeletedAt
            });

        if (document.UpdatedAt is not null)
        {
            UpdatedAtProperty.SetValue(user, document.UpdatedAt);
        }

        return user;
    }

    private static UserDocument MapToDocument(User user)
    {
        return new UserDocument
        {
            Id = user.Id.Value.ToString(),
            Email = user.Email.Value,
            Name = user.Name.Value,
            ExternalAuthId = user.ExternalAuthId.Value,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt
        };
    }
}
