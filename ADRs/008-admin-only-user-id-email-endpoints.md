# 008 – Admin-only user id/email endpoints and self-service `/me` flows

## Status

- **Status**: Accepted
- **Date**: 2026-01-25
- **Related issue**: [GitHub issue #35](https://github.com/arielbvergara/clean-architecture/issues/35)

## Context

We previously tightened authentication and authorization for `UserController` by:
- Requiring authentication by default via the fallback policy and controller-level `[Authorize]`.
- Introducing `/api/User/me` endpoints for self-service operations by the current authenticated user.
- Using an `OwnsUser` resource-based authorization policy to enforce ownership on id/email-based endpoints with an admin escape hatch.

As we evolve the API surface, we want a clearer separation between:
- **Admin-only management operations** that can act on any user by id or email.
- **Self-service operations** that are restricted to the current authenticated user and expressed via `/me` routes.

The existing resource-based `OwnsUser` checks on id/email endpoints add complexity for the controller and tests, and overlap with the more user-centric `/me` endpoints.

## Decision

We will:

- Treat the following endpoints as **admin-only management APIs**:
  - `GET /api/User/{id}`
  - `GET /api/User/email/{email}`
  - `PUT /api/User/{id}/name`
  - `DELETE /api/User/{id}`
- Enforce admin access using the `AdminOnly` authorization policy via attributes:
  - `[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]` on the actions above.
- Keep `/api/User/me`-style endpoints as **self-service** for the current authenticated user and continue to rely on the existing helper that resolves the current user from the external auth identifier.
- Remove resource-based `OwnsUser` checks from the id/email-based endpoints; ownership for normal users is handled exclusively via `/me` routes.
- Update WebAPI tests to:
  - Verify that non-admin callers receive `403 Forbidden` when hitting id-based admin endpoints.
  - Exercise admin access to id/email-based endpoints via the test-only role mechanism.
  - Keep the full user lifecycle test focused on `/me`-based self-service flows.

## Consequences

### Positive

- **Clear separation of responsibilities**
  - Id/email-based endpoints are explicitly admin-only and use a simple policy annotation.
  - Self-service behavior is consistently expressed via `/me`, reducing ambiguity about who can call what.
- **Simpler controller logic**
  - Admin endpoints no longer need to perform explicit resource-based authorization checks; the policy attribute handles access control.
  - Ownership concerns for normal users are isolated to `/me` flows.
- **More focused tests**
  - Authorization tests explicitly cover admin vs non-admin behavior on id-based endpoints.
  - The integration lifecycle test validates the self-service story end-to-end without mixing in admin-only paths.

### Negative / Trade-offs

- **Reduced flexibility for non-admin id/email access**
  - Normal users can no longer access their own records by id/email; they must use `/me`.
  - Any future requirement for non-admin id/email access will either need new endpoints or reintroduction of resource-based authorization.
- **Behavioral change for existing clients**
  - Clients that previously relied on id/email-based endpoints as normal users will now see `403 Forbidden` unless they are granted the admin role.

## Links

- Related ADRs:
  - 003 – WebAPI authentication and authorization for user endpoints
  - 005 – WebAPI auth refinements, JWT policies, and `/me` endpoints
  - 006 – User role and soft delete lifecycle
  - 007 – Admin bootstrap and Firebase Admin integration

- Related work:
-  - GitHub issue: https://github.com/arielbvergara/clean-architecture/issues/35
-  - Branch: `feature/admin-only-user-id-email-endpoints`
