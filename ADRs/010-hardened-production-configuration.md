# 10. Hardened Production Configuration Strategy

Date: 2026-01-26

## Status

Accepted

## Context

The application's WebAPI was previously using default configuration values that were permissive and potentially insecure for production environments. Specifically:
- `AllowedHosts` was set to `*` (wildcard), allowing the application to respond to requests directed at any hostname.
- CORS policies were implicitly configured using `AllowAnyOrigin`, `AllowAnyMethod`, and `AllowAnyHeader`, which exposes the API to cross-origin attacks if not properly restricted.
- The default `appsettings.json` did not enforce strict separation between development conveniences and production security requirements.

OWASP Top 10 A02:2025 (Cryptographic Failures) and previous A05:2021 (Security Misconfiguration) highlight the importance of secure defaults and hardening configuration for production.

## Decision

We will implement a "secure by default" configuration strategy for production environments:

1.  **Strict `AllowedHosts`**: 
    - In `appsettings.Production.json`, `AllowedHosts` will be set to an empty string or specific strict defaults, forcing operators to explicitly define the allowed hostnames (e.g., via environment variable `AllowedHosts`) for the application to function correctly in production.
    - We strictly avoid `*` in production.

2.  **Configuration-Driven CORS**:
    - The CORS policy in `Program.cs` will no longer hardcode `AllowAnyMethod()` or `AllowAnyHeader()`.
    - Instead, it will read allowed methods and headers from the `Cors` configuration section.
    - If these sections are missing or empty, the application will default to a restrictive policy (or fail to apply the permissive one), rather than defaulting to "allow all".
    - `appsettings.Production.json` will define valid HTTP methods (GET, POST, PUT, DELETE, OPTIONS) and standard headers, but require the `Origin` to be explicitly set.

3.  **Environment-Specific Configuration Files**:
    - We introduce `appsettings.Production.json` to explicitly override insecure defaults from `appsettings.json`.
    - Production secrets and environment-specific constraints (Host, Origin) **must** be injected via environment variables or a secret manager, not committed to the repo.

## Consequences

### Positive
- **Improved Security**: Reduces the attack surface by restricting host header attacks and enforcing stricter CORS policies.
- **Compliance**: Aligns with OWASP recommendations for security misconfiguration.
- **Explicitness**: Production requirements are now explicit; the application won't just "accidentally" work in an insecure state.

### Negative
- **Operational Complexity**: Deploying the application now requires setting specific environment variables (e.g., `ClientApp:Origin`, `AllowedHosts`, `Cors:AllowedHeaders`). Failure to do so may cause the app to reject requests.
- **Maintenance**: Developers must ensure that new headers or methods required by the frontend are added to the configuration.

## Implementation Details

- `appsettings.Production.json` introduced with restrictive defaults.
- `Program.cs` updated to use `builder.Configuration.GetSection("Cors")` for building policies.
