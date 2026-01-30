# 17. Admin User Creation Endpoint and Shared Use Case

## Status
- **Status**: Accepted
- **Date**: 2026-01-30
- **Related issue**: #60

## Context

Previous ADRs (notably ADR-004, ADR-006, ADR-007, ADR-008, ADR-014, and ADR-016) define the authentication model, user lifecycle, admin bootstrap behavior, and the separation of admin-only routes under `/api/admin/User`.

Before this change, the system had the following characteristics:

- An admin user could be **seeded on startup** via `AdminUserBootstrapper`, which coordinated with Firebase Admin to ensure the existence of an admin principal and appropriate custom claims.
- There was **no first-class HTTP endpoint** for creating or ensuring an admin user; seeding was primarily an infrastructure concern.
- Multiple user-related use cases (`CreateUser`, `GetUserById`, `GetUserByEmail`, `GetUserByExternalAuthId`, `UpdateUserName`, etc.) each contained a private `MapToResponse` helper that converted `Domain.Entities.User` to `UserResponse`.
- Security event logging for user lifecycle operations existed for self-service endpoints (e.g., `user.created`, `user.updated`, `user.deleted`), but **admin user creation** itself was not emitting a dedicated security event.

As part of evolving the admin story, we need:

- A **dedicated admin-only endpoint** to create or ensure an admin user, aligned with the `/api/admin/User` route space introduced in ADR-016.
- A **shared use case** that unifies admin seeding logic and the new endpoint behavior, avoiding duplication and divergence.
- **Centralized mapping** from domain `User` entities to `UserResponse` DTOs to reduce repetition and drift across use cases.
- **Consistent security event logging** for admin user creation, so that creation/ensure flows are observable and auditable.

## Decision

We will:

1. **Introduce `CreateAdminUserUseCase` in the Application layer**

   - New use case: `Application.UseCases.User.CreateAdminUserUseCase`.
   - Dependencies:
     - `IUserRepository` for domain persistence.
     - `IIdentityProviderService` for creating or ensuring the admin principal and claims in the identity provider (Firebase Admin via `IFirebaseAdminClient` in the current implementation).
   - Behavior:
     - Validates the request email and name using existing value objects (`Email`, `UserName`).
     - Checks if a user with the given email already exists in the repository.
       - If the user **exists**:
         - Calls `IIdentityProviderService.EnsureAdminUserAsync` to ensure admin claims and existence in the identity provider.
         - Returns a successful `Result<UserResponse, AppException>` for the existing user.
       - If the user **does not exist**:
         - Calls `EnsureAdminUserAsync` to create or ensure the admin principal and obtain an external auth identifier.
         - Creates a new admin domain user via `Domain.Entities.User.CreateAdmin`.
         - Persists the new user via `IUserRepository.AddAsync`.
         - Returns a successful `Result<UserResponse, AppException>` for the newly created admin user.
     - On validation or domain errors, returns appropriate `AppException` instances (e.g., `ValidationException`, `InfraException`) wrapped in the `Result` type.

2. **Expose a new admin-only HTTP endpoint for admin creation**

   - In `WebAPI/Controllers/AdminUserController.cs`, add:
     - `POST /api/admin/User` – `CreateAdminUser`.
   - Endpoint characteristics:
     - Attributes:
       - `[ApiController]`.
       - `[Route("api/admin/User")]` (as per ADR-016, via the existing route prefix constant).
       - `[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]` at the controller level.
       - `[EnableRateLimiting(RateLimitingPolicies.Fixed)]` on the action.
     - Request DTO: `WebAPI.DTOs.CreateAdminUserDto` with data annotations:
       - `Email` – `[Required][EmailAddress]`.
       - `DisplayName` – `[Required]`.
       - `Password` – `[Required][MinLength(12)]`.
     - Internally constructs `CreateAdminUserRequest` and calls `CreateAdminUserUseCase.ExecuteAsync`.
     - Maps `Result<AppException>` failures to HTTP responses via the existing `ToActionResult` extension, logging failures with `ILogger<AdminUserController>`.
     - On success, returns `200 OK` with the `UserResponse` payload.

3. **Refactor `AdminUserBootstrapper` to use the shared use case**

   - `WebAPI.Authentication.AdminUserBootstrapper` no longer interacts directly with `IFirebaseAdminClient` or `IUserRepository`.
   - Instead, it:
     - Validates `AdminUserOptions` (email, display name, password) and enforces stricter password rules outside of development, as before.
     - Constructs a `CreateAdminUserRequest` from configuration values.
     - Calls `CreateAdminUserUseCase.ExecuteAsync` to perform the seed/ensure operation.
     - Treats any failure result as a genuine error condition and logs/throws accordingly.
   - This change ensures that both seeding and the admin HTTP endpoint share identical invariants and behavior.

4. **Emit structured security events for admin user creation**

   - `AdminUserController.CreateAdminUser` now uses `ISecurityEventNotifier` to emit events:
     - On failure:
       - `SecurityEventNames.UserCreateFailed` with:
         - `SubjectId = null` (user not reliably established).
         - `Outcome = SecurityEventOutcomes.Failure`.
         - Properties including route and exception type.
       - An error is logged and the standard error response pipeline is used.
     - On success:
       - `SecurityEventNames.UserCreated` with:
         - `SubjectId = createdAdmin.Id`.
         - `Outcome = SecurityEventOutcomes.Success`.
         - Properties including the request route.
   - This aligns admin creation observability with existing user lifecycle events while preserving the abstraction boundary around logging and observability providers.

5. **Extend tests to cover the new behavior**

   - Application tests:
     - `CreateAdminUserUseCaseTests` verify:
       - Ensuring admin claims when the user already exists.
       - Creating a new admin user (including correct mapping and persistence behavior).
       - Validation errors for invalid email inputs.
       - Infra-level errors for unexpected exceptions.
   - Web API tests:
     - `AdminUserControllerCreateAdminUserTests` verify:
       - Non-admin callers receive `403 Forbidden` for `POST /api/admin/User`.
       - Admin callers receive `200 OK`, a persisted admin user with correct role, and the expected security event being emitted.
     - A test-only `TestFirebaseAdminClient` and `TestSecurityEventNotifier` are wired into `CustomWebApplicationFactory` to:
       - Avoid external Firebase dependencies during tests.
       - Allow assertions over emitted security events without coupling to logging sinks.

## Consequences

### Positive

- **Unified admin creation behavior**
  - Both startup seeding and the admin HTTP endpoint now rely on `CreateAdminUserUseCase`, ensuring consistent validation, repository interactions, and identity provider behavior.

- **Improved observability and security auditing**
  - Admin user creation and failures are now captured as structured security events, aligned with user lifecycle events defined in earlier ADRs.
  - This supports alerting, forensic analysis, and compliance needs around privileged account management.

- **Reduced duplication and maintenance overhead**
  - `UserExtensions.ToUserResponse` eliminates repeated mapping logic across multiple use cases, reducing the risk of inconsistent behavior when the `UserResponse` shape evolves.

- **Clearer API surface for admin workflows**
  - The new `POST /api/admin/User` endpoint provides a well-defined, admin-only entry point for provisioning admin users, aligned with the `/api/admin/User` route family established in ADR-016.

### Negative / Trade-offs

- **Increased coupling between seeding and application logic**
  - `AdminUserBootstrapper` now depends on `CreateAdminUserUseCase` rather than working purely at the infrastructure boundary.
  - This is intentional to avoid logic drift, but it does mean changes to the use case can impact seeding behavior and vice versa.

- **More complexity in test scaffolding**
  - Web API tests require additional test doubles (`TestFirebaseAdminClient`, `TestSecurityEventNotifier`) and DI overrides in `CustomWebApplicationFactory`.
  - This adds some overhead but keeps tests hermetic and free from external identity provider dependencies.

## Implementation Notes

- **DI and identity provider wiring**
  - `Program.cs` registers:
    - `IFirebaseAdminClient` → `FirebaseAdminClient` (singleton).
    - `IIdentityProviderService` → `IFirebaseAdminClient` (singleton adapter).
    - `CreateAdminUserUseCase` as a scoped service via `Application.DependencyInjection.AddUseCases()`.
  - Test environment replaces these with `TestFirebaseAdminClient` and the test security notifier.

- **Error handling and result patterns**
  - `CreateAdminUserUseCase` follows the existing `Result<T, AppException>` pattern used throughout the Application layer.
  - Error categories (`ValidationException`, `InfraException`, etc.) are preserved to integrate cleanly with existing `ToActionResult` mapping logic.

- **Security considerations**
  - The admin creation endpoint is protected by the same `AdminOnly` policy and rate limiting mechanisms as other admin endpoints.
  - Password validation for seeded admins includes additional checks in non-development environments (minimum length, not equal to email, etc.), as enforced by `AdminUserBootstrapper`.
  - Security events are emitted in a way that avoids leaking sensitive data (e.g., no raw passwords) while still providing enough context for monitoring.

## References

- ADR-004: Firebase Authentication and Identity Provider Abstraction.
- ADR-006: User Role and Soft Delete Lifecycle.
- ADR-007: Admin Bootstrap and Firebase Admin Integration.
- ADR-008: Admin-Only User Id/Email Endpoints.
- ADR-014: Security Design and Threat Model for User Endpoints.
- ADR-016: Admin User Endpoints Routing and Controller Separation.
- GitHub issue: #60 – *Add admin user creation endpoint and centralize user mapping*.
