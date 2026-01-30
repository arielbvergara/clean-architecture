# 16. Admin User Endpoints Routing and Controller Separation

## Status
- **Status**: Accepted
- **Date**: 2026-01-30
- **Related issue**: N/A

## Context

Earlier ADRs (notably ADR-003, ADR-005, ADR-006, ADR-007, ADR-008, and ADR-014) established the security model, lifecycle, and authorization policies for user management in the Web API. In particular:

- There are **admin-only** operations that allow privileged callers to:
  - List users with pagination and filters.
  - Look up users by internal id or email.
  - Update another user's display name.
  - Soft-delete users.
- There are also **self-service** operations for end users:
  - Creating their own user record after authentication.
  - Reading/updating their own profile via `/api/User/me` endpoints.
  - Deleting their own account.

Originally, both admin-only and self-service endpoints lived in a single `UserController` under the base route `/api/User`. Admin-only operations were distinguished solely via `[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]` plus supporting policies and handlers.

While this preserved clean architecture boundaries and enforced role/policy checks correctly, it had several drawbacks:

- **Route semantics were unclear**: admin-only operations (e.g., `GET /api/User/{id}`) were not obviously different, by URL, from self-service routes.
- **Operational policy targeting was harder**: configuring gateways, rate limiting, or monitoring rules specifically for admin-only APIs required inspecting policies rather than using a simple path prefix.
- **Controller mixed responsibilities**: a single controller handled both self-service and administrator workflows, reducing readability and increasing the chance of accidental coupling.

The feature work on the `feature/separate-admin-endpoints` branch introduces a clear separation in both routing and controller structure, while preserving the existing policy- and role-based authorization behavior defined in prior ADRs.

## Decision

We will:

1. **Introduce a dedicated `AdminUserController`** for admin-only user management operations:
   - Route prefix: `api/admin/User`.
   - Attributes:
     - `[ApiController]`.
     - `[Route("api/admin/User")]` (via a named constant in the controller to avoid magic strings).
     - `[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]` at the class level.
   - Responsibilities:
     - List users with pagination and filtering.
     - Fetch user details by id.
     - Fetch user details by email.
     - Update another user's display name.
     - Soft-delete users.

2. **Restrict admin-only endpoints to `/api/admin/User` paths**
   - Effective routes are now:
     - `GET    /api/admin/User`                      – list users (paginated, supports filters).
     - `GET    /api/admin/User/{id}`                 – get user by id.
     - `GET    /api/admin/User/email/{email}`        – get user by email.
     - `PUT    /api/admin/User/{id}/name`            – update a user's display name.
     - `DELETE /api/admin/User/{id}`                 – delete (soft-delete) a user by id.
   - All of these endpoints:
     - Require the `AdminOnly` policy.
     - Are covered by existing authorization handlers and security logging patterns.

3. **Keep self-service endpoints in `UserController` under `/api/User`**
   - `UserController` retains:
     - `POST /api/User`           – create the current authenticated user's record.
     - `GET /api/User/me`         – get the current user's profile.
     - `PUT /api/User/me/name`    – update the current user's display name.
     - `DELETE /api/User/me`      – delete the current user's account.
   - `UserController` no longer contains admin-only operations; those are exclusively handled by `AdminUserController`.

4. **Align tests and behavior with the new routes**
   - `WebAPI.Tests` now targets the new admin routes when exercising admin-only behaviors:
     - Access control and anti-enumeration tests use `/api/admin/User/{id}` and `/api/admin/User/email/{email}`.
     - List-users tests use `/api/admin/User` with appropriate query parameters (e.g., `pageNumber`, `pageSize`, `isDeleted`).
   - Self-service tests continue to use `/api/User` and `/api/User/me` routes.

This decision refines the Web API surface area without changing the underlying domain model, application use cases, or authorization policies.

## Consequences

### Positive

- **Clear separation of concerns**
  - Admin-only behavior is localized in `AdminUserController` and `/api/admin/User` routes, while self-service behavior remains in `UserController` under `/api/User`.
  - Improves readability and maintainability by grouping endpoints by audience and privilege level.

- **Improved operational controls**
  - Gateways, WAFs, reverse proxies, and monitoring tools can easily target `/api/admin/**` routes for:
    - Stricter rate limiting.
    - IP allowlists or additional authentication steps.
    - Enhanced logging and monitoring.

- **Better alignment with ADR-014 security design**
  - The URL space now reflects the privilege boundary, complementing policy-based authorization.
  - Anti-enumeration and least-privilege behaviors are still enforced via policies and handlers, but are now easier to reason about and test.

- **Backward-compatible self-service behavior**
  - Existing self-service clients that use `/api/User/me` and related routes remain unaffected, reducing risk for user-facing flows.

### Negative / Trade-offs

- **Breaking change for admin clients**
  - Any existing admin tooling or integrations that called the old admin-only routes under `/api/User` must be updated to use `/api/admin/User`.
  - Mitigation is via documentation and coordinated rollout; no compatibility shim is provided by default.

- **Slightly increased controller surface**
  - The number of controllers grows, which adds some structural overhead, but this is intentional to improve clarity.

## Implementation Notes

- **Controller changes**
  - A new `AdminUserController` was introduced in `clean-architecture/WebAPI/Controllers/AdminUserController.cs` to host all admin-only user management operations.
  - `UserController` in `clean-architecture/WebAPI/Controllers/UserController.cs` was simplified to self-service operations and updated to reference `AdminUserController` when generating `Location` headers for newly created users.

- **Routing and attributes**
  - `AdminUserController` uses a named constant for the base route (`"api/admin/User"`) to avoid magic strings.
  - All admin-only endpoints use attribute routing relative to this base, ensuring routes are consistently prefixed with `/api/admin/User`.

- **Authorization**
  - `AdminUserController` uses `[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]` at the class level, ensuring any new admin actions added in the future automatically inherit the admin-only requirement.
  - Self-service endpoints continue to rely on `[Authorize]` plus existing ownership/identity semantics.

- **Testing**
  - `WebAPI.Tests` were updated to:
    - Use `/api/admin/User` for admin-only access control and listing scenarios.
    - Continue to use `/api/User/me` for current-user scenarios.
  - Existing test naming conventions (`{MethodName}_Should{DoSomething}_When{Condition}`) were preserved.

- **Future considerations**
  - If additional admin areas are introduced (e.g., `/api/admin/Role`, `/api/admin/Audit`), they should follow the same pattern:
    - Dedicated controllers with `/api/admin/...` routing.
    - Class-level `[Authorize(Policy = ...)]` declarations aligned with ADR-003/ADR-008/ADR-014.
  - If versioning is introduced (e.g., `/api/v2/admin/User`), this ADR should be revisited or superseded with a versioned routing strategy that preserves the admin prefix while clarifying stability guarantees for clients.
