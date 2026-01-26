# 007 – Admin bootstrap and Firebase Admin integration

- **Status**: Accepted
- **Date**: 2026-01-25
- **Related issue**: [GitHub issue #34](https://github.com/arielbvergara/clean-architecture/issues/34) & [Github issue #29](https://github.com/arielbvergara/clean-architecture/issues/29)

## Context

We need a reliable way to provision and maintain an initial administrator user across both the identity provider and the application database while keeping the core Domain and Application layers free from identity-provider specific dependencies.

Prior to this change:

- WebAPI already used Firebase-backed JWT authentication and ASP.NET Core authorization policies (see ADRs 003, 004, 005).
- `Domain.Entities.User` had a `Role` property and soft delete lifecycle (see ADR 006), but there was **no** dedicated flow to:
  - Ensure an administrator account exists in the identity provider with the correct role claim.
  - Ensure a corresponding domain `User` record exists for reporting and future domain logic.
- Admin creation was effectively manual and ad-hoc.

From a security and operational perspective, we wanted to:

- Automate admin provisioning in an **idempotent** way that can safely run on every startup.
- Avoid baking Firebase Admin SDK details into the Domain or Application layers (identity remains a host concern per ADR 004).
- Align with OWASP Top 10 guidance, especially:
  - A02 – Security Misconfiguration (no insecure defaults in higher environments).
  - A07 – Authentication and Identification Failures (avoid weak or default admin credentials).
  - A10 – Mishandling of Exceptional Conditions (fail fast on misconfiguration rather than running in a half-secure state).

## Decision

We introduced a WebAPI-level admin bootstrap flow that orchestrates identity-provider specific admin provisioning and domain user creation, with explicit, environment-aware security behavior, while reusing the existing Firebase integration from ADR 004.

### 1. Admin bootstrapper in WebAPI

We added an `AdminUserBootstrapper` service in the WebAPI layer that is responsible for one-time, idempotent admin seeding at startup:

- `WebAPI.Authentication.IAdminUserBootstrapper` defines `Task SeedAdminUserAsync(CancellationToken cancellationToken = default)`.
- `WebAPI.Authentication.AdminUserBootstrapper` implements the interface and is invoked once from `Program.Main` after migrations.

`AdminUserBootstrapper`:

- Depends on:
  - `IFirebaseAdminClient` (WebAPI abstraction over the identity provider, as per ADR 004).
  - `Application.Interfaces.IUserRepository` (Application port for persistence).
  - `IOptions<AdminUserOptions>` (WebAPI configuration binding from the `AdminUser` section).
  - `IHostEnvironment` (to distinguish Development vs non-Development behavior).
  - `ILogger<AdminUserBootstrapper>`.
- Implements the following behavior:
  - If `AdminUser.SeedOnStartup` is `false`, it returns immediately.
  - If Email/Password/DisplayName are missing:
    - In **Development**: logs a warning and skips seeding.
    - In **non-Development**: throws `InvalidOperationException` to fail startup (no half-configured admin state).
  - In **non-Development** environments, enforces a minimal password rule:
    - Password length must be at least 12 characters.
    - Password must not be equal to the admin email (case-insensitive).
    - Violations cause startup to fail with an `InvalidOperationException`.
  - If a domain `User` with the configured admin email already exists:
    - Calls `IFirebaseAdminClient.EnsureAdminUserAsync` to ensure identity provider claims are in sync.
    - Does **not** create a duplicate domain user.
  - If no such domain user exists:
    - Calls `EnsureAdminUserAsync` to obtain/create an external admin account with the correct claims.
    - Creates a domain admin user via `User.CreateAdmin(Email, UserName, ExternalAuthIdentifier)` and persists it via `IUserRepository.AddAsync`.

This keeps startup orchestration and environment-specific behavior in WebAPI while reusing Domain and Application abstractions.

### 2. Identity provider abstraction (referencing ADR 004)

The identity provider integration pattern (Firebase Admin SDK behind a WebAPI-local abstraction) was already established in ADR 004. This ADR reuses that pattern:

- `WebAPI.Authentication.IFirebaseAdminClient` remains the abstraction for the limited admin operations required by WebAPI.
- `WebAPI.Authentication.FirebaseAdminClient` continues to:
  - Use Application Default Credentials (`GoogleCredential.GetApplicationDefault()`), with `GOOGLE_APPLICATION_CREDENTIALS` or platform identity providing secrets.
  - Create or load the Firebase user by email and ensure the custom role claim is present.

The key **new** decision here is not the Firebase integration itself (see ADR 004), but how it is orchestrated at startup via `AdminUserBootstrapper` and `AdminUserOptions`.

### 3. Configuration and startup behavior

We updated configuration and startup wiring to support secure, environment-aware admin bootstrap:

- `WebAPI.Configuration.AdminUserOptions` binds the `AdminUser` section:
  - `SeedOnStartup`, `DisplayName`, `Email`, `Password`.
- `WebAPI.Program`:
  - Configures `AdminUserOptions` via `builder.Services.Configure<AdminUserOptions>(builder.Configuration.GetSection(AdminUserOptions.SectionName));`.
  - Registers `IFirebaseAdminClient` and `IAdminUserBootstrapper`.
  - Applies EF Core migrations for relational providers only.
  - In non-`Testing` environments, resolves `IAdminUserBootstrapper` from DI and calls `SeedAdminUserAsync()` once at startup.
  - Skips admin bootstrap entirely in the `Testing` environment so integration tests do not require real Firebase Admin credentials.
- `appsettings.Development.json`:
  - `AdminUser.SeedOnStartup` is now `false` by default.
  - `AdminUser.Email` and `AdminUser.Password` are empty by default, requiring explicit, local configuration via environment variables, user secrets, or override files.

Centralizing this behavior in WebAPI keeps higher-environment safety (strict checks and failures) while preserving developer ergonomics in Development.

### 4. Note on authorization constants

As a supporting implementation detail, we:

- Use `Domain.Constants.UserRoleConstants` as the single source of truth for domain role names (e.g., `Admin`).
- Introduced `WebAPI.Authorization.AuthorizationConstants` for auth-related claim keys (`role`, `sub`).
- Updated `ClaimsPrincipalExtensions` and JWT wiring to consume these constants instead of magic strings.

These changes do not represent a separate architectural decision; they simply make the admin bootstrap and authorization behavior less error-prone and easier to maintain.

## Consequences

### Positive

- **Environment-aware security defaults**:
  - Non-Development environments cannot start with incomplete or obviously weak admin credentials when `SeedOnStartup` is enabled, addressing OWASP A02 and A07 concerns.
  - Development remains convenient (warnings instead of hard failures) while avoiding committed default admin credentials.
- **Idempotent, automated admin provisioning**:
  - Startup can safely call `SeedAdminUserAsync()` multiple times without creating duplicate admin accounts.
  - Existing admin users are kept in sync with expected identity-provider claims.
- **Clean architecture preserved**:
  - Domain and Application layers remain agnostic of Firebase and startup seeding logic.
  - WebAPI continues to own identity-provider integration and bootstrap orchestration via dedicated abstractions (per ADR 004).
- **Better handling of exceptional conditions**:
  - Missing or insecure admin configuration in higher environments causes explicit startup failures instead of silently running in a degraded security posture, aligning with OWASP A10.

### Negative / Trade-offs

- **Additional complexity in WebAPI startup**:
  - `Program.cs` now orchestrates admin seeding in addition to DB configuration, CORS, and Swagger.
  - There are more moving parts (options, environment, bootstrapper, Firebase Admin client) to consider when debugging startup issues.
- **Minimal built-in password policy**:
  - The rule (length >= 12 and not equal to email) is intentionally basic and may need to be expanded or replaced with an org-wide policy.

## Alternatives considered

1. **Manual, out-of-band admin creation**
   - Rely on operators to create an admin user directly in Firebase and seed a matching DB record manually.
   - Rejected because it is error-prone, non-idempotent, and hard to audit or reproduce across environments.

2. **Embed Firebase Admin logic in Application or Infrastructure**
   - Would have mixed identity-provider concerns with domain use cases or repositories.
   - Rejected to preserve clean architecture boundaries and keep identity integration in the host (WebAPI) layer (as established in ADR 004).

3. **Use database role as the primary source of truth for authorization**
   - Make `User.Role` drive all authorization decisions instead of JWT claims.
   - Rejected in favor of token-based roles (per ADRs 003 and 005) to avoid divergence between token state and DB state and to keep ASP.NET Core authorization simple and stateless.

4. **Always hard-fail on bad admin config, including Development**
   - Would have maximized safety but at the cost of local developer ergonomics, especially when Firebase Admin credentials are not configured.
   - Rejected in favor of an environment-aware approach: hard fail only in non-Development environments and log warnings in Development.

## Implementation references

- Admin bootstrap and options:
  - `clean-architecture/WebAPI/Authentication/IAdminUserBootstrapper.cs`
  - `clean-architecture/WebAPI/Authentication/AdminUserBootstrapper.cs`
  - `clean-architecture/WebAPI/Configuration/AdminUserOptions.cs`
- Identity provider abstraction and Firebase implementation:
  - `clean-architecture/WebAPI/Authentication/IFirebaseAdminClient.cs`
  - `clean-architecture/WebAPI/Authentication/FirebaseAdminClient.cs`
  - See also ADR 004 – Firebase authentication and identity provider abstraction.
- Authorization helpers and constants:
  - `clean-architecture/Domain/Constants/UserRoleConstants.cs`
  - `clean-architecture/WebAPI/Authorization/AuthorizationConstants.cs`
  - `clean-architecture/WebAPI/Authorization/ClaimsPrincipalExtensions.cs`
  - `clean-architecture/WebAPI/Authentication/JwtAuthenticationExtensions.cs`
- Startup wiring and configuration:
  - `clean-architecture/WebAPI/Program.cs`
  - `clean-architecture/WebAPI/appsettings.json`
  - `clean-architecture/WebAPI/appsettings.Development.json`
  - `docker-compose.yml`
