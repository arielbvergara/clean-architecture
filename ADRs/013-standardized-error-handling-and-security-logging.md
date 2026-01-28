# 13. Standardized Error Handling and Security Logging

## Status
- **Status**: Accepted
- **Date**: 2026-01-28
- **Related issue**: [GitHub issue #11](https://github.com/arielbvergara/clean-architecture/issues/11)

## Context

The WebAPI currently mixes several patterns for error handling and logging:

- `GlobalExceptionFilter` logs unhandled exceptions and returns a generic 500 response with a raw string message.
- Controllers, particularly `UserController`, map `Result<T, AppException>` to HTTP responses inline, often returning anonymous objects like `new { error.Message }` for both validation errors and unexpected failures.
- Logging is present (warnings for missing claims, errors for failed user operations) but there is no standardized event shape, correlation identifier, or clear separation between security-significant events and general diagnostics.

This creates several risks relative to OWASP Top 10:2025:

- **A09 – Security Logging and Alerting Failures**
  - Important security events (authorization failures, unexpected exceptions, suspicious access patterns) may not be logged consistently or with sufficient structured context.
  - Logs are harder to consume by centralized observability and alerting systems, which depend on predictable fields (such as correlation id, user identifier, route, and outcome) to detect and respond to incidents.

- **A10 – Mishandling of Exceptional Conditions**
  - Returning raw or semi-raw error messages risks leaking implementation details or internal state.
  - Inconsistent handling of exceptions across controllers and filters makes it harder to reason about which failures are safely exposed versus internal-only.

From a MITRE ATT&CK perspective, weak and inconsistent error handling and logging can make it easier for attackers to:

- Impair or evade defenses by exploiting gaps in logging coverage (for example, techniques related to "Indicator Removal" or "Impair Defenses").
- Probe the system using malformed or malicious requests and infer implementation details from error responses.

At the same time:

- The application already uses a `Result<T, AppException>` pattern at the application layer, which can be leveraged to standardize HTTP error mapping.
- The WebAPI includes security middleware such as authentication/authorization, security headers, and rate limiting, so standardized error handling and logging will fit into an existing security-hardening effort.

## Decision

We will standardize error handling and security logging in the WebAPI around three core concepts:

1. **A shared API error envelope based on RFC 7807 Problem Details**
   - All user-facing error responses (4xx and 5xx) will use a consistent structure including fields such as `status`, `type`, `title`, `detail`, and `instance`, with an extensions bag for additional metadata.
   - Internal exception details (stack traces, connection strings, low-level error codes) will not be included in client-facing responses.

2. **A first-class correlation identifier**
   - Each HTTP request will be associated with a correlation id (taken from an incoming header or generated when missing).
   - The correlation id will be attached to log scopes and, where appropriate, included in the error envelope and response headers.

3. **Structured, security-focused logging**
   - Important security-relevant events (authorization failures, user lifecycle operations, and unexpected exceptions) will be logged using structured properties, including correlation id, route, user identifier (when available), and outcome.
   - Logs will be shaped so that downstream systems (for example, centralized logging and alerting platforms) can define alerts for repeated failures or suspicious patterns.

### 1. Standardized API error envelope

- Introduce a reusable error response model in the WebAPI layer (for example, `ApiErrorResponse` and `ApiValidationErrorResponse`) that:
  - Aligns with Problem Details semantics (status, type, title, detail, instance, and extensions).
  - Supports attaching a correlation id in a safe way.
- Provide a mapping helper that:
  - Converts `AppException` (validation, not-found, conflict, and infrastructure-style failures) into HTTP responses using the shared envelope.
  - Translates `Result<T, AppException>` into a corresponding `IActionResult` consistently across controllers.
- Use generic, user-safe messages for 5xx and infrastructure errors, while preserving more detailed messages for validation scenarios where appropriate.

### 2. Correlation id strategy

- Define a single, named configuration for the correlation id header (for example, `X-Correlation-ID`), avoiding magic strings by centralizing the header name in a constant or configuration option.
- Add middleware that:
  - Reads the incoming correlation id header if present; otherwise generates a new identifier.
  - Stores the correlation id on the `HttpContext` and includes it in a logging scope.
  - Writes the correlation id back to the response header so clients and logs can be correlated.
- Expose a small helper to retrieve the current correlation id for use in filters and controllers when building responses.

### 3. Structured security logging and alerting hooks

- Standardize logging for key events, including:
  - User create/update/delete operations.
  - Access-denied and authorization failures.
  - Unhandled exceptions caught by `GlobalExceptionFilter`.
- Use structured logging properties (e.g., `CorrelationId`, `UserId`, `Route`, `Action`, `Outcome`, `Reason`) rather than relying solely on string interpolation.
- Where appropriate, provide an abstraction (for example, `ISecurityEventNotifier` with a default no-op implementation) that can be integrated with external observability and alerting platforms in production deployments.

## Consequences

### Positive

- **Improved safety of error responses**
  - Client-facing error payloads become consistent, predictable, and free of internal implementation details.
  - OWASP A10 risks are reduced by making exceptional condition handling explicit and centrally managed.

- **Better observability and incident response**
  - Correlation ids make it easier to trace a request across logs and systems.
  - Structured logging for security events improves alignment with OWASP A09 and enables more reliable alerting.

- **Clearer contracts between layers**
  - The WebAPI layer becomes responsible for mapping `Result<T, AppException>` to HTTP responses via a small, well-defined abstraction.
  - Application and domain layers remain free of HTTP-specific concerns.

### Negative / Trade-offs

- **Increased implementation complexity**
  - Introducing new middleware, models, and mapping helpers adds moving parts to the WebAPI.
  - Developers need to learn and follow the new patterns when adding endpoints.

- **Potential for misconfiguration**
  - If correlation id handling or logging scopes are incorrectly wired, logs may become harder to correlate until issues are discovered and fixed.

- **Migration effort for existing endpoints**
  - Existing controllers must be refactored to adopt the new mapping helpers, which may involve non-trivial changes to tests and response shapes.

## Implementation Notes

- Add new error response models and mapping helpers in the WebAPI project, keeping them HTTP-focused and free of domain or infrastructure dependencies.
- Update `GlobalExceptionFilter` to:
  - Use the standardized error envelope for unhandled exceptions.
  - Log correlation id, route, and user identity (when available) using structured properties.
- Refactor `UserController` to:
  - Delegate error handling to the centralized mapping helpers, instead of constructing anonymous `new { error.Message }` responses.
  - Continue logging security-relevant events, now enriched with correlation id and consistent field names.
- Use extension methods on `ControllerBase` (via `ErrorResponseMapper`) rather than a shared base controller to keep controllers loosely coupled and avoid imposing an inheritance hierarchy. A dedicated base controller can be introduced later if more shared WebAPI behavior emerges.
- Introduce correlation id middleware early in the HTTP pipeline and expose a helper for retrieving the current correlation id.
- Extend tests (primarily in `WebAPI.Tests`) to cover:
  - The shape of error responses.
  - Correlation id propagation behaviors.
  - Logging of key security events.

## Related ADRs

- [ADR-014: Security Design and Threat Model for User Endpoints](./014-security-design-and-threat-model-for-user-endpoints.md) – defines the security events (e.g., authorization failures) that must be logged.


## References

- [GitHub issue #11](https://github.com/arielbvergara/clean-architecture/issues/11)
- OWASP Top 10:2025 A09 – Security Logging and Alerting Failures
- OWASP Top 10:2025 A10 – Mishandling of Exceptional Conditions
- MITRE ATT&CK techniques related to impairing defenses and indicator removal/obfuscation
- `clean-architecture/WebAPI/Filters/GlobalExceptionFilter.cs`
- `clean-architecture/WebAPI/Controllers/UserController.cs`