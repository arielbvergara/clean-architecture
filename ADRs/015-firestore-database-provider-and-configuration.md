# 15. Firestore Database Provider and Database Provider Configuration
## Status
- **Status**: Proposed
- **Date**: 2026-01-29
- **Related issue**: [GitHub issue #56](https://github.com/arielbvergara/clean-architecture/issues/56)

## Context
The application currently supports two database options:
- In-memory EF Core provider (default for Testing, optional for Development).
- PostgreSQL provider configured via connection string and Docker Compose.

Database selection is owned by the WebAPI composition root in `WebAPI/Configuration/DatabaseConfiguration.cs`, which:
- Always uses the in-memory database when the environment is `Testing`.
- Otherwise chooses between in-memory and PostgreSQL based on the `UseInMemoryDB` flag and the `ConnectionStrings:DbContext` configuration value.

ADR-002 established PostgreSQL plus Docker Compose as the primary example for running the API against a real relational database.
ADR-007 and the README document integration with Firebase / Google credentials for admin bootstrap.
ADR-010, ADR-011, ADR-013, and ADR-014 define secure-by-default configuration, rate limiting, standardized error handling, and the security design for user endpoints.

We now want to:
- Add a **Firestore database provider** using the official .NET SDK, as an additional option alongside in-memory and PostgreSQL.
- Allow selection of the database provider via configuration, without changing Domain or Application code and with minimal changes to WebAPI composition.
- Reuse the existing `IUserRepository` abstraction so that controllers and use cases remain agnostic to the underlying database.

## Decision
We will introduce a pluggable database provider configuration model in the WebAPI layer and add a Firestore-backed implementation of `IUserRepository` in the Infrastructure layer.

### 1. Database provider configuration model

We will add a strongly-typed configuration model to represent the selected database provider and its settings.

- Introduce `DatabaseProviderOptions` under `WebAPI.Configuration` (or a dedicated `WebAPI.Configuration.Database` namespace) with at least:
  - `string Provider` — the logical provider name; expected values include `"InMemory"`, `"Postgres"`, and `"Firestore"`.
  - `string? ConnectionString` — optional, primarily for relational providers such as PostgreSQL.
  - `string? FirestoreProjectId` and other Firestore-specific settings as needed.
- Bind this options class from a new `Database` section in `appsettings*.json` and environment variables (e.g., `Database__Provider`).
- Preserve the existing `UseInMemoryDB` flag for backward compatibility, but prefer `Database:Provider` as the primary selector going forward.

Provider selection rules:
- `Testing` environment continues to **always** use the in-memory provider, regardless of configuration, to keep tests isolated from real infrastructure.
- In non-testing environments:
  - If `Database:Provider` is set, it determines which provider to use.
  - If `Database:Provider` is not set, we fall back to the current behavior (in-memory vs PostgreSQL based on `UseInMemoryDB` and `ConnectionStrings:DbContext`).

### 2. Firestore infrastructure implementation

We will add a Firestore-specific implementation of `IUserRepository` in the Infrastructure layer that does not leak Firestore details into Domain or Application.

- Under `clean-architecture/Infrastructure`, introduce a Firestore namespace such as `Infrastructure.Data.Firestore`.
- Define a Firestore data access abstraction, e.g. `IFirestoreUserDataStore`, responsible for:
  - Reading/writing user records to one or more Firestore collections.
  - Mapping between Firestore documents and a Firestore-specific DTO.
- Implement `IUserRepository` as `FirestoreUserRepository` that:
  - Uses `IFirestoreUserDataStore` internally.
  - Preserves existing behavior around roles, soft delete (`IsDeleted`, `DeletedAt`), and external authentication identifiers as defined in earlier ADRs (e.g., ADR-006 and ADR-007).
  - Translates Firestore-specific failures into the existing `Result<..., AppException>` pattern used by the Application layer, consistent with ADR-013.

Firestore client configuration and credentials will follow the same principles as the Firebase Admin integration (ADR-007):
- No service account JSON or secrets committed to source control.
- Use `GOOGLE_APPLICATION_CREDENTIALS` and environment variables or mounted files for credentials.
- Allow use of the Firestore emulator for local development where practical.

### 3. Wiring providers in WebAPI composition

We will extend `DatabaseConfiguration.AddDatabaseConfiguration` to select between in-memory, PostgreSQL, and Firestore based on `DatabaseProviderOptions` while keeping selection logic localized.

- Continue to register `IUserRepository` as a single abstraction for the rest of the application.
- Selection logic:
  - If the environment is `Testing` → use in-memory database and the existing EF-based `UserRepository`.
  - Else, resolve `DatabaseProviderOptions` and switch on `Provider`:
    - `"InMemory"` → call `services.AddInMemoryDatabase()`; register existing EF-based `UserRepository`.
    - `"Postgres"` → call `services.AddPostgresDatabase(connectionString)`; register existing EF-based `UserRepository`.
    - `"Firestore"` → register Firestore client(s) and `IFirestoreUserDataStore`; register `FirestoreUserRepository` as the `IUserRepository` implementation.
- Fail fast with a clear, non-ambiguous exception when:
  - The configured provider is unsupported.
  - Required configuration for the selected provider (connection string, Firestore project id, credentials) is missing.

This keeps the WebAPI project as the composition root (consistent with prior ADRs) and centralizes provider selection in a single place.

### 4. Configuration and documentation

We will extend configuration and documentation to make provider selection explicit.

- Add a `Database` section to `appsettings.Development.json` and other environment-specific configs as needed, for example:
  - `Database:Provider = "Postgres"` (current default when using Docker Compose, consistent with ADR-002).
  - Optionally `Database:Provider = "Firestore"` when testing Firestore locally.
- Keep PostgreSQL connection strings in `ConnectionStrings:DbContext`, as already established by ADR-002.
- Add Firestore-related settings (e.g., `Database:FirestoreProjectId`) and rely on environment variables and `GOOGLE_APPLICATION_CREDENTIALS` for credentials, aligned with ADR-007.
- Update `README.md` to describe:
  - Available providers (`InMemory`, `Postgres`, `Firestore`).
  - How to select a provider using `Database:Provider` and environment variables.
  - How to configure Firestore in local development (project id, emulator vs real project, credentials) without violating security guidance.

### 5. Testing strategy

We will validate the new provider and configuration without weakening existing guarantees from ADR-001 and subsequent test-focused ADRs.

- Add unit tests in `Infrastructure.Tests` for `FirestoreUserRepository` and associated mapping code:
  - Validate CRUD operations, role behavior, and soft delete semantics.
  - Use test doubles or an emulator-friendly abstraction for the Firestore client.
- Add tests in `WebAPI.Tests` (where practical) that:
  - Exercise `Database:Provider` selection logic.
  - Confirm that the `Testing` environment always uses in-memory regardless of configuration.
  - Verify that misconfiguration of `Database:Provider` produces clear failures rather than silent fallbacks.
- Follow the existing conventions:
  - Test naming: `{MethodName}_Should{DoSomething}_When{Condition}`.
  - Use FluentAssertions for assertions.

## Consequences

### Positive

- **Extensibility**: Database provider selection becomes explicit and configuration-driven, making it easier to add or swap providers in the future without touching Domain or Application code.
- **Separation of concerns**: Firestore-specific details are contained within Infrastructure and WebAPI composition, preserving the clean architecture boundaries established by existing ADRs.
- **Consistency**: Error handling, security logging, and security behavior for Firestore-backed operations follow ADR-010, ADR-011, ADR-013, and ADR-014.
- **Flexibility for environments**: Developers can choose in-memory, PostgreSQL, or Firestore for different environments via configuration alone.

### Negative / Trade-offs

- **Increased configuration surface**: A new `Database` configuration section and provider names introduce additional configuration that must be maintained and validated.
- **Operational complexity**: Running against Firestore (particularly in non-emulator modes) introduces cloud connectivity and credential management concerns, which must follow the secure patterns from ADR-007.
- **Testing complexity**: High-fidelity tests for Firestore may require emulator setups or careful use of test doubles to avoid coupling tests to external services.

## Implementation Notes

- Implement the change incrementally:
  1. Introduce `DatabaseProviderOptions` and configuration binding, keeping existing `UseInMemoryDB` behavior as a fallback.
  2. Update `DatabaseConfiguration.AddDatabaseConfiguration` to respect `Database:Provider` while preserving the current Testing behavior.
  3. Add Firestore-specific infrastructure types (`IFirestoreUserDataStore`, DTOs, `FirestoreUserRepository`).
  4. Wire Firestore into DI and selection logic under the `"Firestore"` provider value.
  5. Add or update tests in `Infrastructure.Tests` and `WebAPI.Tests`.
  6. Update documentation (`README.md`) to describe provider options and Firestore configuration.
- Provider names (`"InMemory"`, `"Postgres"`, `"Firestore"`) should be defined as centralized constants to avoid magic strings.
- All new code should continue to follow the repository’s conventions for security, error handling, and logging as defined in existing ADRs.

## Related ADRs

- [ADR-001: Use Microsoft Testing Platform Runner](./001-use-microsoft-testing-platform-runner.md)
- [ADR-002: Use PostgreSQL with Dockerfile and docker-compose](./002-postgres-and-docker-compose.md)
- [ADR-006: User role and soft delete lifecycle](./006-user-role-and-soft-delete-lifecycle.md)
- [ADR-007: Admin bootstrap and Firebase Admin integration](./007-admin-bootstrap-and-firebase-admin-integration.md)
- [ADR-010: Hardened production configuration](./010-hardened-production-configuration.md)
- [ADR-011: Security headers middleware and API rate limiting](./011-security-headers-and-rate-limiting.md)
- [ADR-013: Standardized error handling and security logging](./013-standardized-error-handling-and-security-logging.md)
- [ADR-014: Security design and threat model for user endpoints](./014-security-design-and-threat-model-for-user-endpoints.md)

## References

- [GitHub issue #56](https://github.com/arielbvergara/clean-architecture/issues/56)
- [Google Cloud Firestore documentation](https://cloud.google.com/firestore/docs)
- Existing Firebase / Google credential handling patterns from ADR-007 and the README
