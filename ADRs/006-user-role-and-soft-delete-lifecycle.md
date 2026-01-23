# 006 – User Role and soft delete lifecycle

- **Status**: Accepted
- **Date**: 2026-01-23
- **Related issue**: GitHub `issue #18`

## Context

The initial `User` aggregate and persistence model had the following characteristics:

- `Domain.Entities.User` included:
  - `Id`, `Email`, `Name`, `ExternalAuthId`, `CreatedAt`, and `UpdatedAt`.
  - No `Role` property for domain/business-level role information.
  - No soft delete fields; users were hard-deleted from the database.
- `Infrastructure.Repositories.UserRepository.DeleteAsync` implemented deletion by:
  - Loading the user by `UserId`.
  - Calling `context.Set<User>().Remove(user);` and `SaveChangesAsync`.
- `AppDbContext` and `UserConfiguration`:
  - Mapped value objects (`UserId`, `Email`, `UserName`, `ExternalAuthId`).
  - No global filter or columns for deletion state.

From a security and lifecycle perspective:

- Hard deleting users complicates auditing and potential recovery.
- There was no first-class place to store a business-level role for reporting or domain logic (authorization was entirely JWT-claim-based).
- OWASP A10 (Mishandling of Exceptional Conditions) and general data-governance best practices recommend clear lifecycle state and avoiding inconsistent behavior around deletes.

## Decision

We enriched the `User` aggregate and persistence model with a role and a soft delete lifecycle.

### 1. Extend User with Role and soft delete fields

`Domain.Entities.User` now has:

- `public string Role { get; private set; }`
- `public bool IsDeleted { get; private set; }`
- `public DateTime? DeletedAt { get; private set; }`

The private constructor and factory were updated:

- Constructor parameters include `string role`, `bool isDeleted`, `DateTime? deletedAt`.
- `User.Create(Email email, UserName name, ExternalAuthIdentifier externalAuthId)` now creates:

  ```csharp
  var user = new User(
      UserId.NewId(),
      email,
      name,
      externalAuthId,
      role: "User",
      createdAt: DateTime.UtcNow,
      isDeleted: false,
      deletedAt: null);
  ```

We introduced a soft-delete operation:

- `public void MarkDeleted()`:
  - If `IsDeleted` is already `true`, returns without changing state (idempotent).
  - Otherwise sets `IsDeleted = true` and `DeletedAt = DateTime.UtcNow`.

**Note:**

- `Role` here is used for domain/reporting purposes and is **not** the source of truth for authorization.
- Authorization decisions remain based on JWT `role` claims and ASP.NET Core policies (per ADR 005), preventing divergence between token roles and DB state.

### 2. EF Core configuration and global query filter

`Infrastructure.Configurations.UserConfiguration` was updated to map the new scalar properties:

- `builder.Property(u => u.Role).HasMaxLength(100);`
- `builder.Property(u => u.IsDeleted);`
- `builder.Property(u => u.DeletedAt);`

`AppDbContext.OnModelCreating` now applies a global query filter to exclude soft-deleted users by default:

```csharp
modelBuilder.ApplyConfiguration(new UserConfiguration());

// Exclude soft-deleted users from all queries by default.
modelBuilder.Entity<User>()
    .HasQueryFilter(u => !u.IsDeleted);
```

This ensures:

- All application queries for `User` (via repositories or direct DbSet usage) behave as if soft-deleted rows do not exist.
- Existing use cases that expect a deleted user to be "missing" continue to receive `null` from repository methods like `GetByIdAsync`.

### 3. Repository DeleteAsync now performs soft delete

`Infrastructure.Repositories.UserRepository.DeleteAsync` was changed from hard delete to soft delete:

- **Before:**

  ```csharp
  var user = await GetByIdAsync(id, cancellationToken);
  if (user is null)
  {
      return;
  }

  context.Set<User>().Remove(user);
  await context.SaveChangesAsync(cancellationToken);
  ```

- **After:**

  ```csharp
  var user = await GetByIdAsync(id, cancellationToken);
  if (user is null)
  {
      return;
  }

  user.MarkDeleted();
  await context.SaveChangesAsync(cancellationToken);
  ```

The behavior as seen by application code remains the same:

- Calling `DeleteAsync` for an existing user marks it as deleted rather than removing the row.
- Subsequent reads via repository methods return `null` due to the global filter.
- Calling `DeleteAsync` for a non-existent user remains a no-op.

`DeleteUserUseCase` did not require a behavioral change, as it already:

- Returns `NotFound` when the user cannot be loaded.
- Returns success when `DeleteAsync` completes, regardless of internal delete mechanics.

## Consequences

### Positive

- **Improved data lifecycle control**:
  - Users are now soft-deleted by default, allowing for potential recovery, audits, and compliance investigations.
  - The global query filter ensures soft-deleted rows do not appear in normal operations, preserving current API semantics (e.g., 404 after delete).
- **Domain enrichment**:
  - `User.Role` provides a place for domain-meaningful roles without overloading JWT claims.
  - Soft-delete metadata (`IsDeleted`, `DeletedAt`) is part of the aggregate, making lifecycle state explicit and testable.
- **Safer handling of exceptional conditions**:
  - Deleting a user no longer risks inconsistent behavior if other code accidentally assumes a complete row removal; instead the state transition is explicit.

### Negative / Trade-offs

- **More columns and logic in the User model**:
  - Schema and entity complexity increased (additional fields and query filter).
- **Potential surprises with IgnoreQueryFilters**:
  - Developers using `IgnoreQueryFilters()` must remember that soft-deleted rows will then appear and should be handled carefully.
- **No hard-delete endpoint yet**:
  - Hard deletes (e.g., GDPR purge) are not yet exposed via a dedicated use case / endpoint; those would need to be added in a separate change if required.

## Alternatives considered

1. **Continue with hard deletes only**
   - Rejected because it offers weaker support for audit trails, investigations, and potential recovery.

2. **Represent deletion state outside the aggregate (e.g., separate table or shadow properties)**
   - Considered, but embedding `IsDeleted` / `DeletedAt` in the aggregate keeps lifecycle state explicit and easier to reason about and test.

3. **Use Role for live authorization decisions**
   - Rejected in favor of using JWT `role` claims and ASP.NET Core policies as the source of truth for authorization, keeping DB role state decoupled from token-based auth concerns.

## Implementation references

- Domain aggregate:
  - `clean-architecture/Domain/Entities/User.cs`
- EF Core configuration and context:
  - `clean-architecture/Infrastructure/Configurations/UserConfiguration.cs`
  - `clean-architecture/Infrastructure/Data/AppDbContext.cs`
- Repository behavior:
  - `clean-architecture/Infrastructure/Repositories/UserRepository.cs`
- Tests:
  - Domain: `clean-architecture/Tests/Application.Tests/Domain/Entities/UserTests.cs`
  - Infrastructure: `clean-architecture/Tests/Infrastructure.Tests/SoftDeleteUserRepositoryTests.cs`
