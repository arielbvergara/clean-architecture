# 11. Security Headers Middleware and API Rate Limiting

## Status
- **Status**: Accepted
- **Date**: 2026-01-27
- **Related issue**: [GitHub issue #9](https://github.com/arielbvergara/clean-architecture/issues/9)

## Context

The WebAPI layer previously did not enforce a consistent set of HTTP security headers and did not apply first-class rate limiting to its endpoints.

This created several risks:
- **Missing or inconsistent security headers** (e.g., `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Content-Security-Policy`) increased exposure to:
  - Clickjacking.
  - MIME sniffing.
  - Cross-site information leakage.
- **No application-level throttling** made it easier for a single principal (user or IP) to:
  - Degrade availability or trigger cascading failures via excessive request rates.
  - Exercise error paths in a brute-force manner (e.g., repeated account lifecycle operations or validation failures).
- **Security controls were implicit** rather than modeled explicitly in code, making it harder to review them against OWASP and MITRE ATT&CK style threat models.

GitHub issue #9 requested:
- A consistent set of security headers applied across responses at the WebAPI boundary.
- Centralized rate limiting for key API endpoints, with semantics that distinguish between read and write operations.
- Integration tests to validate both rate limiting and security headers.

These changes must align with existing architecture decisions:
- WebAPI remains the composition root and owns HTTP pipeline configuration.
- The "Testing" environment must not depend on real infrastructure (e.g., production database, real identity provider).
- The repository avoids magic numbers and magic strings; security-sensitive values should be centralized and named.

## Decision

We will harden the WebAPI boundary by introducing explicit middleware and configuration for security headers and rate limiting, backed by centralized constants and policies.

### 1. Named rate limiting policies and centralized defaults

- Add `WebAPI.RateLimiting.RateLimitingPolicies` to centralize policy names:
  - `Fixed`: for read-heavy, lower-risk operations.
  - `Strict`: for write/destructive operations and endpoints with higher abuse potential.
- Add `WebAPI.RateLimiting.RateLimitingDefaults` to centralize numeric and key defaults:
  - `FixedPermitLimit = 100`, `FixedQueueLimit = 2`, `FixedWindow = 1 minute`.
  - `StrictPermitLimit = 10`, `StrictQueueLimit = 0`, `StrictWindow = 1 minute`.
  - Fallback partition keys for rate limiting partitions:
    - `UnknownAuthenticatedUserPartitionKey` (for authenticated users with missing identifiers).
    - `UnknownAnonymousPartitionKey` (for anonymous callers without a stable IP).
- Introduce `WebAPI.Configuration.RateLimitingConfiguration.AddRateLimitingConfiguration` to:
  - Register the ASP.NET Core rate limiter with HTTP 429 (Too Many Requests) as the rejection status code.
  - Partition limits by authenticated user identifier when available, or by remote IP address otherwise.
  - Define the `Fixed` and `Strict` policies using the centralized defaults.
- Wire this configuration into `Program` via `builder.Services.AddRateLimitingConfiguration()` and keep `app.UseRateLimiter()` in the middleware pipeline.

### 2. Apply `Fixed` and `Strict` policies to user endpoints

- Import `WebAPI.RateLimiting.RateLimitingPolicies` into `UserController`.
- Annotate endpoints with the appropriate policy based on operation type and risk:
  - **Fixed** (read-oriented):
    - `GET /api/User` (admin list/search).
    - `GET /api/User/me` (current user profile).
  - **Strict** (write/destructive):
    - `POST /api/User` (create current user record).
    - `PUT /api/User/me/name` (update current user display name).
    - `DELETE /api/User/me` (self-service account deletion).
- This encodes a clear rule of thumb:
  - Reads with moderate cost but lower abuse impact use the more permissive `Fixed` policy.
  - Writes and higher-impact operations use the more restrictive `Strict` policy.

### 3. Security headers middleware backed by shared constants

- Add `WebAPI.Middleware.SecurityHeaderConstants` to centralize header names and values:
  - `X-Content-Type-Options: nosniff`.
  - `X-Frame-Options: DENY`.
  - `Referrer-Policy: strict-origin-when-cross-origin`.
  - `Content-Security-Policy: default-src 'self';`.
- Implement `WebAPI.Middleware.SecurityHeadersMiddleware` which:
  - Checks for existing response headers before appending values, to avoid overwriting headers set by upstream components (e.g., reverse proxies or API gateways).
  - Appends the standardized security headers when they are not already present.
- Register `SecurityHeadersMiddleware` in `Program` early in the pipeline (after HTTPS redirection but before CORS and authentication) to ensure responses, including errors, are consistently hardened.

### 4. Anonymous `/health` endpoint

- Add a minimal `GET /health` endpoint in `Program` that returns `200 OK`.
- Mark it as anonymous (e.g., via `AllowAnonymous()`) to bypass the global fallback authorization policy that requires authentication for most endpoints.
- Use this endpoint as a stable target for:
  - Integration tests verifying that security headers are present.
  - External liveness/health checks that should not require authentication.

### 5. Integration tests for rate limiting and security headers

- Add `RateLimitingTests` in `WebAPI.Tests` using `CustomWebApplicationFactory`:
  - Test name follows the repository convention `{MethodName}_Should{DoSomething}_When{Condition}`.
  - The test exercises `POST /api/User` (strict policy) enough times to exceed `StrictPermitLimit` plus a small buffer, and asserts that the final response is HTTP 429.
  - The test uses `RateLimitingDefaults` instead of hard-coded numbers to remain aligned with configuration.
- Add `SecurityHeadersTests` in `WebAPI.Tests` using `CustomWebApplicationFactory`:
  - Calls `GET /health` and asserts that the standardized security headers are present.
  - Asserts that header values match those defined in `SecurityHeaderConstants`.
  - Uses FluentAssertions, consistent with the rest of the test suite.

## Consequences

### Positive

- **Improved security posture**
  - Standardized security headers reduce exposure to clickjacking, MIME sniffing, and referrer leakage.
  - Rate limiting bounds resource usage per principal/IP, mitigating some denial-of-service and brute-force-style behaviors.
  - Behavior is explicit in code and easier to review against OWASP Top 10 and MITRE ATT&CK style threat models.

- **Maintainability and clarity**
  - Policy names and numeric defaults are defined once and reused, avoiding magic numbers and strings in controllers and configuration.
  - Future policies (e.g., for background jobs or admin-only maintenance APIs) can be added in a localized manner within the `RateLimiting` namespace.
  - Tests remain aligned with production configuration by referencing shared constants instead of duplicating values.

- **Testability and observability**
  - The `/health` endpoint provides a simple, authentication-independent target for integration tests and external monitors.
  - Integration tests around rate limiting and security headers provide regression protection and documentation of the intended behavior.

### Negative / Trade-offs

- **Tuning required**
  - The initial defaults (`FixedPermitLimit`, `StrictPermitLimit`, windows, queue limits) are conservative heuristics rather than mathematically derived limits.
  - Real-world usage may require environment-specific tuning to avoid throttling legitimate traffic or under-protecting hot endpoints.

- **Increased pipeline complexity**
  - The HTTP middleware pipeline now includes additional components (rate limiting and security headers), which adds complexity to debugging and reasoning about request flow.
  - Contributors must understand when to choose `Fixed` vs `Strict` (or future policies) when adding or modifying endpoints.

- **Overlap with edge infrastructure**
  - Some security headers and rate limiting rules might also be enforced at API gateways, CDNs, or reverse proxies.
  - Coordination is required to avoid conflicting or duplicated configuration between application and infrastructure layers.

## Implementation Notes

- New namespaces and types:
  - `WebAPI.RateLimiting.RateLimitingPolicies` and `WebAPI.RateLimiting.RateLimitingDefaults`.
  - `WebAPI.Configuration.RateLimitingConfiguration` with `AddRateLimitingConfiguration` extension.
  - `WebAPI.Middleware.SecurityHeaderConstants` and `WebAPI.Middleware.SecurityHeadersMiddleware`.
- `Program` updated to:
  - Call `builder.Services.AddRateLimitingConfiguration()`.
  - Use `SecurityHeadersMiddleware`.
  - Expose an anonymous `/health` endpoint.
- `UserController` updated to:
  - Reference `RateLimitingPolicies` and apply `Fixed`/`Strict` policies via `[EnableRateLimiting]` attributes.
- `WebAPI.Tests` updated to include:
  - `RateLimitingTests` and `SecurityHeadersTests` using `CustomWebApplicationFactory` and FluentAssertions.

## References

- [GitHub issue #9](https://github.com/arielbvergara/clean-architecture/issues/9)
- OWASP Top 10 guidance on security misconfiguration and resilience
- ASP.NET Core rate limiting middleware and security headers best practices
