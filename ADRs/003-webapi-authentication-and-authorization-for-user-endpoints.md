# 003 – WebAPI authentication and authorization for user endpoints

- **Status**: Accepted
- **Date**: 2026-01-21
- **Related issue**: GitHub `issue #7` (WebAPI authentication & authorization hardening for user endpoints)

## Context

Initially, the `clean-architecture/WebAPI` project exposed the `UserController` endpoints without any authentication or authorization. This meant any caller could:

- Create users (`POST /api/User`).
- Read users by ID or email (`GET /api/User/{id}`, `GET /api/User/email/{email}`).
- Update user names (`PUT /api/User/{id}/name`).
- Delete users (`DELETE /api/User/{id}`).

This is a classic case of missing access control / insecure direct object reference:

- No API-wide authentication mechanism was configured (no `AddAuthentication`, no `UseAuthentication`).
- No authorization policies were defined (`AddAuthorization` not used, no fallback policy).
- `UserController` actions were not annotated with `[Authorize]`.
- There was no notion of record ownership (any caller could operate on any `UserId`).

From a security perspective, this maps to:

- **OWASP Top 10 2025 – A01: Broken Access Control**: missing enforcement of authenticated users and record ownership.
- **OWASP Top 10 2025 – A07: Identification and Authentication Failures**: sensitive operations allowed without authentication.
- **MITRE ATT&CK (e.g., T1078 Valid Accounts)**: if an attacker obtains valid credentials or a way to hit the endpoints, they can freely act on user records without further checks.

Issue #7 was raised to introduce a secure-by-default authentication and authorization model for the WebAPI, starting with the existing `UserController` and leaving room for future anonymous endpoints.

## Decision

We decided to:

1. **Adopt JWT Bearer authentication for the WebAPI**
   - Configure `JwtBearerDefaults.AuthenticationScheme` in `clean-architecture/WebAPI/Program.cs`:
     - `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`.
   - Read identity provider settings from configuration:
     - `Authentication:Authority` – issuer/authority for JWTs.
     - `Authentication:Audience` – API audience / resource identifier.
   - Rely on environment-specific configuration (appsettings + environment variables / secrets) so no secrets are hard-coded.

2. **Require authentication by default (fallback policy)**
   - Use `builder.Services.AddAuthorization(options => { options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(); });`.
   - This ensures that *all* endpoints require an authenticated user unless explicitly opted out with `[AllowAnonymous]`.

3. **Secure `UserController` with ASP.NET Core authorization attributes**
   - Add `[Authorize]` at the controller level so that all current actions under `UserController` require authentication.
   - As of 2026-01-26, **all** user operations (create/read/update/delete) are considered sensitive and require authentication; `POST /api/User` is no longer anonymous.
   - The `CreateUser` endpoint derives the external authentication identifier exclusively from the caller's token claims (`sub` / `ClaimTypes.NameIdentifier`) rather than trusting an `externalAuthId` field in the request body.

4. **Enforce record ownership using `ExternalAuthId`**
   - The `Domain.Entities.User` entity already includes an `ExternalAuthId` value object and there is a `GetUserByExternalAuthIdUseCase` in the Application layer.
   - We added an ownership helper method in `UserController`:
     - Reads the current external identifier from claims, preferring `sub` and falling back to `ClaimTypes.NameIdentifier`.
     - Resolves the current domain user via `GetUserByExternalAuthIdUseCase`.
     - Compares the requested `UserId` to the current user’s `Id`.
     - If they differ and the principal is **not** in role `Admin`, returns `Forbid()`.
     - If the current user cannot be resolved (e.g., no user record for the external ID) and the underlying error is `NotFound`, returns `NotFound`; otherwise returns `Forbid()`.
   - This ownership check is applied to:
     - `GET /api/User/{id}`
     - `GET /api/User/email/{email}` (after resolving the user)
     - `PUT /api/User/{id}/name`
     - `DELETE /api/User/{id}`

5. **Allow for future admin capabilities via role-based override**
   - Ownership enforcement includes a role-based escape hatch:
     - If `User.IsInRole("Admin")` is true, the controller allows access even when the `UserId` does not match the current user.
   - This enables future admin-only behavior (e.g., operations across any user) while still enforcing strict ownership for normal users.

6. **Introduce test-only authentication infrastructure for WebAPI integration tests**
   - In `clean-architecture/Tests/WebAPI.Tests`:
     - `TestAuthHandler` implements a **test-only** authentication handler, configured via `CustomWebApplicationFactory`.
     - The test host uses a custom scheme (`TestAuthHandler.SchemeName`) instead of JWT Bearer.
     - Identity and roles are injected via HTTP headers that are **only honored in tests**:
       - `X-Test-Only-ExternalId` → mapped to the `sub` claim.
       - `X-Test-Only-Role` → mapped to a role claim.
   - The production WebAPI host is **not** configured with `TestAuthHandler` and ignores these headers.
   - This arrangement allows tests to simulate identities and roles without depending on a real identity provider, while staying clearly separated from production flows.

7. **Add WebAPI tests to cover authN/authZ behavior**
   - New tests live under `clean-architecture/Tests/WebAPI.Tests`:
     - `UserControllerIntegrationTests` validates a full lifecycle (create → read by email → read by id → update → read after update → delete → read after delete) for an authenticated user whose `ExternalAuthId` is used across the flow.
     - `UserControllerAuthorizationTests` validates:
       - Anonymous access to `GET /api/User/{id}` returns `401 Unauthorized`.
       - A user can access **their own** user resource (200).
       - A non-admin user attempting to access **another user’s** resource receives `403 Forbidden`.
       - An `Admin` user (via `X-Test-Only-Role: Admin`) can access another user’s resource successfully (200).

8. **Document the authentication and test-only auth mechanisms**
   - Updated `README.md` with:
     - A description of JWT-based authentication and the `Authentication:Authority` / `Authentication:Audience` configuration.
     - A description of the test-only authentication handler and headers, clarifying that they are only used in tests and that production uses JWT bearer tokens.

## Consequences

### Positive

- **Strong default security posture**:
  - All endpoints require authentication by default via the fallback policy.
  - `UserController` is explicitly `[Authorize]`, reducing the risk of accidental public exposure.
- **Least-privilege and ownership**:
  - Normal users can only access and mutate their own `User` records.
  - Admin behavior is explicit and test-covered.
- **Clear separation of concerns**:
  - Domain and Application layers remain unchanged in their core responsibilities; auth concerns are confined to WebAPI and test infrastructure.
- **Testability**:
  - Integration tests exercise end-to-end behavior (authN, ownership, domain logic) without needing a real IdP or real JWTs.
  - Test-only headers and handlers are clearly labeled and isolated in the test project.

### Negative / Trade-offs

- **Configuration overhead**:
  - Deployments must correctly configure `Authentication:Authority` and `Authentication:Audience`, and ensure tokens issued by the IdP line up with these values.
- **Increased complexity**:
  - Controllers now include some security-related logic (ownership checks) which must be kept consistent across new endpoints.
- **Testing vs production divergence**:
  - The test pipeline uses a custom auth handler, while production uses JWT Bearer. Changes to claim mapping or policies must be reflected consistently in both places.

## Alternatives considered

1. **Allowing unauthenticated access and deferring auth to upstream gateways**
   - Rejected because it would make the WebAPI unsafe by default and tightly couple security to external infrastructure.
   - Violates OWASP guidance recommending defense-in-depth and application-layer access control.

2. **Basic authentication or API keys instead of JWT Bearer**
   - Rejected due to weaker alignment with modern identity providers, poorer support for delegated auth, and less structured claims (especially for roles and external IDs).

3. **Per-endpoint `[Authorize]` without a fallback policy**
   - Rejected because it is easy to forget to add `[Authorize]` to new endpoints, leading to accidental public APIs.
   - The fallback policy approach enforces authentication by default and requires explicit opt-out via `[AllowAnonymous]`.

4. **Embedding ownership logic entirely in the Application layer**
   - Considered, but for this iteration we chose a simpler, controller-level ownership check that leverages existing use cases.
   - Future work could introduce an `ICurrentUser` abstraction and push ownership decisions deeper into the Application layer if needed.

## Implementation references

- **Program configuration**: `clean-architecture/WebAPI/Program.cs`
- **User controller and ownership checks**: `clean-architecture/WebAPI/Controllers/UserController.cs`
- **Domain user entity and external auth ID**: `clean-architecture/Domain/Entities/User.cs`
- **Use case for external auth ID lookup**: `clean-architecture/Application/UseCases/User/GetUserByExternalAuthIdUseCase.cs`
- **WebAPI test host and auth handler**:
  - `clean-architecture/Tests/WebAPI.Tests/CustomWebApplicationFactory.cs`
  - `clean-architecture/Tests/WebAPI.Tests/TestAuthHandler.cs`
- **WebAPI tests**:
  - `clean-architecture/Tests/WebAPI.Tests/UserControllerIntegrationTests.cs`
  - `clean-architecture/Tests/WebAPI.Tests/UserControllerAuthorizationTests.cs`
