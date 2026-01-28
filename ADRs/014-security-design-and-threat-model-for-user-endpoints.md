# 14. Security Design and Threat Model for User Endpoints

## Status
- **Status**: Accepted
- **Date**: 2026-01-28
- **Related issue**: [GitHub issue #12](https://github.com/arielbvergara/clean-architecture/issues/12)

## Context

The WebAPI exposes a set of user-centric endpoints via `UserController`, including:

- Self-service endpoints such as:
  - `GET /api/User/me`
  - `PUT /api/User/me/name`
  - `DELETE /api/User/me`
- Administrative management endpoints such as:
  - `GET /api/User`
  - `GET /api/User/{id}`
  - `GET /api/User/email/{email}`
  - `PUT /api/User/{id}/name`
  - `DELETE /api/User/{id}`

Previous ADRs have established:

- Authentication and authorization integration for the WebAPI and `/me` endpoints.
- The existence of a user role model (`User` vs `Admin`) and admin bootstrap flow.
- Admin-only access to id/email-based management endpoints.
- Standardized error handling, security logging, and rate limiting for user operations.

However, there has not yet been a single, consolidated security design and threat model for how these endpoints should behave across roles, ownership boundaries, and enumeration resistance.

From an OWASP Top 10:2025 perspective:

- **A06 – Insecure Design**
  - Without an explicit model of roles, ownership, and tenant boundaries, it is harder to reason about and verify correct behavior.
  - Anti-enumeration and abuse protections for identifier/email lookups are not centrally described.
- **A01 – Broken Access Control / A07 – Identification and Authentication Failures**
  - The system must ensure only appropriate identities can invoke sensitive operations and act on given records.
- **A09/A10 – Security Logging and Mishandling of Exceptional Conditions**
  - Error, logging, and rate-limiting behaviors must align with the security model so that abuse is both constrained and observable.

From a MITRE ATT&CK perspective, weak or ambiguous design around these endpoints can make it easier for attackers to:

- Enumerate valid accounts and identifiers.
- Abusively read or modify another user’s data.
- Probe error responses to infer implementation details or authorization gaps.

This ADR defines a clear security design and threat model for user endpoints so that controllers, application use cases, and tests can consistently enforce and verify the intended behavior.

## Decision

We adopt the following security design for user endpoints in the WebAPI.

### 1. Roles and identity model

We standardize on a small, explicit role model for the purposes of user endpoint access:

- `User` (end user): a regular authenticated end user acting on their own account.
- `Admin` (administrator): an operator with broader management privileges over user records.
- `ServiceAccount` (service-level principal): a non-human identity used by trusted backend services when necessary.

These roles are represented using named constants (e.g., `UserRoleConstants`) and, where needed, typed abstractions in the application layer, avoiding magic strings in business logic.

Identity for WebAPI requests is sourced from the configured authentication middleware (for example, Firebase JWT integration), which provides:

- A stable external authentication identifier (e.g., JWT `sub` claim) used to look up the current user.
- Role/permission claims that can be mapped to the application’s role model.

### 2. Ownership and tenant model

For the purposes of the current implementation, the system is logically **single-tenant** but is designed so that a tenant or partition identifier can be introduced later without breaking the conceptual model.

- Each user record is owned by exactly one end user (the subject whose external auth identifier and email/name are stored).
- Ownership rules:
  - End users may only read, update, or delete **their own** user record.
  - Administrators and, where explicitly configured, service accounts may act on any user record, subject to policy.

Tenant-awareness will be introduced by extending the user and context model (for example, adding a `TenantId`) rather than by rewriting authorization logic.

### 3. Endpoint access rules

Access for the key endpoints is defined as follows:

- `GET /api/User/me`
  - **Who**: authenticated `User`, `Admin`, or `ServiceAccount` acting as themselves.
  - **What**: returns the caller’s own user profile.
- `PUT /api/User/me/name`
  - **Who**: same as above.
  - **What**: updates the caller’s own display name.
- `DELETE /api/User/me`
  - **Who**: same as above.
  - **What**: soft-deletes the caller’s own user record.

Administrative management endpoints:

- `GET /api/User`
  - **Who**: `Admin` only.
  - **What**: paged query across users with optional filters; can include soft-deleted records when explicitly requested.
- `GET /api/User/{id}`
  - **Who**: `Admin` only.
  - **What**: retrieves a specific user by internal id.
- `GET /api/User/email/{email}`
  - **Who**: `Admin` only.
  - **What**: retrieves a specific user by email.
- `PUT /api/User/{id}/name`
  - **Who**: `Admin` only.
  - **What**: updates the display name of the target user.
- `DELETE /api/User/{id}`
  - **Who**: `Admin` only.
  - **What**: soft-deletes the target user.

Where `ServiceAccount` roles are enabled, they are treated as equivalent to `Admin` for these endpoints unless more granular policies are introduced in future ADRs.

### 4. Enforcement location and patterns

To avoid scattering authorization logic and to align with clean architecture principles:

- **Controllers** are responsible for:
  - Translating HTTP/ASP.NET identity (claims principal, roles) into a small, well-defined application-level context (for example, a `CurrentUserContext` containing user id, roles, and tenant information).
  - Applying declarative authorization attributes for coarse-grained checks (for example, `[Authorize(Policy = AdminOnly)]`).
- **Application use cases** are responsible for:
  - Enforcing **fine-grained** ownership and role checks when operating on specific user records.
  - Returning `Result<T, AppException>` values that clearly distinguish between validation errors, not-found, authorization failures, and other error types.

This ensures that controllers do not duplicate or reimplement business-level access rules; instead, they consistently delegate to use cases and map results to HTTP responses via the standardized error handling layer described in ADR-013.

### 5. Enumeration resistance and abuse protections

To mitigate account enumeration and similar threats:

- Endpoints that operate on user identifiers or email addresses are **never exposed** to unauthenticated callers.
- Access to id/email-based endpoints is restricted to `Admin` (and select `ServiceAccount`) roles via authorization policies.
- Error handling for user lookup operations adheres to the standardized patterns in ADR-013:
  - Unauthorized or forbidden callers receive generic responses that do not reveal whether a particular user exists.
  - When the caller is authorized, `404 Not Found` is used to indicate a genuinely missing record.
- Rate limiting is applied to sensitive endpoints according to ADR-011, reducing the impact of automated probing or brute-force enumeration.
- Security events (e.g., repeated failed lookups, forbidden access) are logged via `ISecurityEventNotifier` with structured fields, enabling downstream alerting.

### 6. OWASP and MITRE ATT&CK alignment

This design aligns with OWASP Top 10:2025 and MITRE ATT&CK as follows:

- **OWASP A06 – Insecure Design**
  - Provides an explicit, documented model for roles, ownership, and endpoint access.
  - Centralizes fine-grained authorization in the application layer and standardizes controller behavior.
- **OWASP A01 – Broken Access Control**
  - Ensures only appropriate roles can access administrative endpoints.
  - Enforces that end users can only act on their own records.
- **OWASP A07 – Identification and Authentication Failures**
  - Relies on a clear mapping from authentication middleware to application roles and external identifiers.
- **OWASP A09/A10 – Security Logging and Mishandling of Exceptional Conditions**
  - Builds on ADR-013 so that authorization and ownership failures are logged and surfaced using consistent error envelopes.

In MITRE ATT&CK terms, this design is intended to reduce the risk and detectability of techniques related to account discovery, credential misuse, and data exfiltration via unauthorized account access.

## Consequences

### Positive

- The security behavior of user endpoints is explicitly documented and testable.
- Authorization and ownership checks can be implemented once in use cases and reused across controllers.
- Anti-enumeration and rate-limiting behaviors are clearly tied to endpoint design and logging.
- Future changes to roles or tenant models can be introduced centrally rather than dispersed across controllers.

### Negative / Trade-offs

- Slightly increased complexity in the application layer to carry a current-user context and role information.
- More tests are required to verify both positive and negative access scenarios across roles.
- Future tenant or policy extensions must respect the existing design and may require additional ADRs.

## Implementation Notes

- Introduce or refine a `CurrentUserContext`-style abstraction in the application layer so that use cases can reason about the caller’s identity and roles without depending on ASP.NET types.
- Ensure controllers consistently construct and pass this context to use cases for all user endpoints.
- Extend or add tests in `Application.Tests` and `WebAPI.Tests` to cover authorized, unauthorized, and cross-tenant (future) scenarios, using the established `{MethodName}_Should{DoSomething}_When{Condition}` naming convention.
- Keep role names and policy identifiers centralized (for example, via `UserRoleConstants` and policy constants) to avoid magic strings and make future changes easier.
