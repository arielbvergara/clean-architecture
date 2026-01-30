# 15. Sentry Monitoring and Observability Integration for Web API

## Status
- **Status**: Proposed
- **Date**: 2026-01-29
- **Related issue**: [GitHub issue #55](https://github.com/arielbvergara/clean-architecture/issues/55)

## Context

The Clean Architecture WebAPI currently lacks centralized error and performance monitoring across environments. While existing ADRs define standardized error handling, security logging, and threat models for user endpoints, there is no dedicated observability solution to:

- Aggregate unhandled exceptions with stack traces and HTTP context.
- Capture important application errors and security-relevant events.
- Provide performance traces (e.g., slow endpoints, database calls) for troubleshooting.

From a clean architecture perspective, any monitoring solution must:

- Keep the **Domain** and **Application** layers free of direct dependencies on Sentry or other infrastructure libraries.
- Preserve the existing dependency direction (Domain → Application → Infrastructure → WebAPI).
- Respect existing patterns for configuration and dependency injection.

From a security and reliability perspective, better monitoring directly supports:

- OWASP Top 10:2025 categories related to logging and error handling (e.g., **A09/A10 – Security Logging and Mishandling of Exceptional Conditions**), by ensuring that exceptional conditions are captured in a structured, centralized way.
- MITRE ATT&CK-aligned detection and response, by making suspicious failures and anomalous behaviors visible in monitoring dashboards.

Sentry.io is a widely used error tracking and performance monitoring platform with first-class support for ASP.NET Core. The GitHub issue proposes using the official Sentry .NET/ASP.NET Core SDK as the primary monitoring solution for the Web API, while avoiding any leakage of Sentry-specific types into inner layers.

## Decision

We will integrate Sentry.io into the Clean Architecture Web API as the primary error and performance monitoring solution, with the following design:

### 1. Integration scope and boundaries

- **WebAPI**
  - Configure and initialize the official Sentry ASP.NET Core SDK in `Program.cs` as part of the hosting pipeline.
  - Enable capture of unhandled exceptions and 5xx responses with relevant HTTP context (route, method, status code, correlation identifiers where available).
  - Optionally enable HTTP performance tracing (transactions/spans) for incoming requests, controlled via configuration.
- **Infrastructure**
  - May use Sentry for additional non-HTTP scenarios (e.g., background jobs, repository-level observability) via abstractions defined in the Application layer.
  - Sentry-specific types remain confined to Infrastructure and WebAPI.
- **Application and Domain**
  - Remain unaware of Sentry and any concrete monitoring provider.
  - Continue to use existing result and exception patterns (e.g., `Result<..., AppException>`) without referencing Sentry types.

### 2. Configuration and no magic values

- Introduce a dedicated `Sentry` configuration section in `WebAPI/appsettings.json` and environment-specific configuration files, containing values such as:
  - `Enabled` (boolean flag to turn Sentry integration on or off).
  - `Dsn` (Data Source Name for Sentry project).
  - `Environment` (e.g., `Development`, `Staging`, `Production`).
  - `TracesSampleRate` (fractional sampling rate for performance traces).
- All Sentry options are read from configuration using named options classes or strongly-typed accessors.
- No DSN or other secrets are committed to source control; these are provided via environment variables or secret management in higher environments.
- Literal values that are likely to change (e.g., role names, environment names, sample rates) are expressed via named constants or configuration entries, avoiding magic numbers and magic strings in code.

### 3. WebAPI pipeline behavior

- Sentry is initialized as early as possible in the ASP.NET Core host so that:
  - Unhandled exceptions during request processing are captured automatically by Sentry.
  - Relevant contextual information (HTTP request data, user identity when available, correlation IDs) is attached to events.
- The existing `GlobalExceptionFilter` and standardized error handling (see ADR-013) remain the primary mechanism for shaping HTTP responses:
  - Sentry complements these behaviors by capturing exceptions and error events, but does not change the response contracts.
  - Where appropriate, the filter may explicitly notify Sentry of unhandled exceptions, or rely on Sentry’s built-in middleware integration.
- Local development behavior:
  - Sentry can be disabled or configured with low sampling rates in `Development` to avoid noisy or unnecessary telemetry while still enabling targeted testing.

### 4. Optional observability abstraction

- If the Application layer needs to record custom events, metrics, or security-relevant signals beyond what Sentry captures automatically, we will:
  - Define a small interface in the `Application` project (e.g., `IObservabilityService` or `IMonitoringService`) with methods such as `CaptureError`, `CaptureWarning`, or `CaptureMetric`.
  - Provide an implementation in the `Infrastructure` project that uses Sentry under the hood.
  - Register this implementation in dependency injection via existing Application/Infrastructure DI extension points.
- This abstraction ensures that:
  - Use cases depend only on an application-level contract, not on Sentry types.
  - Future changes to the monitoring provider (or multi-provider setups) do not require changes to the Domain or Application layers.

### 5. Testing and safety

- The application must start successfully whether Sentry is enabled or disabled via configuration.
- Existing tests (`Application.Tests`, `Infrastructure.Tests`, `WebAPI.Tests`) must continue to pass without modification.
- Where practical, new tests will verify that:
  - Sentry integration is enabled or disabled based on configuration flags.
  - Misconfiguration (e.g., missing DSN when `Enabled` is `true`) is handled gracefully and does not crash the Web API.

## Consequences

### Positive

- Centralized, structured error and performance monitoring for the Web API via Sentry, improving observability and incident response.
- Clean architecture boundaries are preserved: Domain and Application layers remain free of Sentry-specific dependencies.
- Configuration-driven behavior (no magic values) makes it easy to adjust environments, sampling rates, and rollout strategies without code changes.
- Improved support for security logging and detection aligned with OWASP and MITRE ATT&CK guidance, building on existing ADRs.

### Negative / Trade-offs

- Additional operational dependency on Sentry as an external SaaS service.
- Slightly increased complexity in `Program.cs` and Infrastructure to wire up Sentry configuration and DI.
- Some performance overhead from capturing telemetry and traces, which must be tuned via sampling and configuration.

## Implementation Notes

- Add the official Sentry ASP.NET Core / .NET SDK package(s) to the `WebAPI` project (and to `Infrastructure` if needed for non-HTTP scenarios).
- Introduce a strongly-typed options class for the `Sentry` configuration section and bind it in `Program.cs`.
- Initialize Sentry in the Web API host using configuration values, ensuring that `Enabled` and `Dsn` control whether monitoring is active.
- Keep all Sentry usage confined to WebAPI and Infrastructure; do not introduce Sentry references into Domain or Application projects.
- Extend or add tests in `WebAPI.Tests` (and, if an observability abstraction is introduced, in `Application.Tests`/`Infrastructure.Tests`) using the established `{MethodName}_Should{DoSomething}_When{Condition}` naming convention.
- Update high-level project documentation (e.g., `README.md`) with a short section explaining how to configure Sentry per environment and where to find telemetry for this application.