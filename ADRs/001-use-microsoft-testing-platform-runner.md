# ADR 001: Use Microsoft.Testing.Platform as dotnet.test.runner

## Status
Accepted

## Context
This project is new and currently has no automated tests. We want to establish a modern testing baseline that will scale as the solution grows in size and complexity.

The .NET ecosystem is moving toward `Microsoft.Testing.Platform` as a more modern, extensible test platform. We plan to adopt it via the `dotnet.test.runner` configuration, setting:

- `dotnet.test.runner = Microsoft.Testing.Platform`

## Decision
We will configure the solution to use `Microsoft.Testing.Platform` as the test runner for `dotnet test`.

## Rationale / Tradeoffs

### Advantages
- **Modern, extensible platform**: Aligns with the newer test infrastructure in .NET and is likely to receive ongoing investment and features.
- **Better performance for larger test suites**: Can improve test discovery and execution times as the number of tests and projects grows, helping keep CI pipelines fast.
- **Richer extensibility model**: Provides a more flexible plugin model for custom adapters, diagnostics, reporting, and potential future tooling integrations.
- **Improved tooling alignment**: Designed to integrate well with modern `dotnet test` workflows and future IDE/test runner improvements.

### Disadvantages / Risks
- **Ecosystem maturity / compatibility**: Some tools (coverage, custom adapters, or older integrations) may not fully support `Microsoft.Testing.Platform` yet and could require updates or workarounds.
- **Configuration and learning curve**: Requires new configuration and some learning for the team to understand the platform’s options and diagnostics.
- **Potential CI adjustments**: CI scripts and result processing may need to be updated if they rely on behavior or output formats from older runners.
- **Behavior differences**: There may be subtle differences in test discovery, filtering, or execution behavior compared to previous runners that we need to watch for as we add tests.

## Consequences
- All future test projects and CI pipelines will be built around `Microsoft.Testing.Platform` from the beginning, avoiding a migration later.
- As tests are added, we will validate tool compatibility (e.g., coverage, reporting) and adjust configuration as needed.
- If blocking compatibility issues arise, we may need to temporarily fall back to a more traditional runner or add shims until the ecosystem catches up.