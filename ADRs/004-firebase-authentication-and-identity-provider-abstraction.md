# 004 – Firebase authentication and identity provider abstraction

- **Status**: Accepted
- **Date**: 2026-01-22
- **Related issue**: GitHub `issue #7` (WebAPI authentication & authorization hardening)

## Context

After implementing authentication and authorization for `UserController` (see ADR 003), the WebAPI was wired to use generic JWT Bearer authentication driven by configuration keys:

- `Authentication:Authority`
- `Authentication:Audience`

However, the project is intended as a **clean architecture boilerplate**, and we expect that:

- Different deployments might choose *different* identity providers (Firebase, Entra ID, Auth0, custom OIDC, etc.).
- The Domain and Application layers should remain **agnostic** of the chosen identity provider and only reason about an opaque `ExternalAuthId`.
- The WebAPI layer should own all HTTP/auth concerns but should do so in a way that is easy to **swap** the underlying identity provider.

We decided to start with **Firebase Authentication (email/password)** as the default provider for this sample, using a dedicated Firebase project:

- Firebase project ID: `clean-architecture-ariel`.
- Email/password sign-in enabled.

The WebAPI must accept Firebase **ID tokens** as Bearer tokens while keeping the design open for future providers.

## Decision

1. **Introduce a WebAPI-only JWT auth configuration abstraction**

   - Created `WebAPI/Authentication/JwtAuthenticationExtensions.cs` with:
     - `AddJwtAuthenticationAndAuthorization(this IServiceCollection, IConfiguration, IHostEnvironment)`.
   - This extension:
     - Reads `Authentication:Authority` and `Authentication:Audience` from configuration.
     - Clears `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap` to avoid legacy claim remapping.
     - Registers `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` using those values.
     - Configures `TokenValidationParameters` to validate issuer, audience, and lifetime.
     - Adds an authorization **fallback policy** that requires authenticated users by default.
   - All identity-provider-specific wiring now lives in **one WebAPI-only place**, not scattered across `Program.cs` or deeper layers.

2. **Update `Program.cs` to use the abstraction**

   - Removed direct `AddAuthentication().AddJwtBearer(...)` and `AddAuthorization(...)` calls from `Program.cs`.
   - Replaced them with a single line:
     - `builder.Services.AddJwtAuthenticationAndAuthorization(builder.Configuration, builder.Environment);`
   - Left the rest of `Program.cs` responsible only for:
     - Wiring controllers, filters, Swagger.
     - Registering repositories and use cases.
     - Setting up EF Core (in-memory vs PostgreSQL).

3. **Configure Firebase as the current identity provider via appsettings**

   - For development (`WebAPI/appsettings.Development.json`):

     ```json
     "Authentication": {
       "Authority": "https://securetoken.google.com/clean-architecture-ariel",
       "Audience": "clean-architecture-ariel"
     }
     ```

   - These values match the Firebase project `clean-architecture-ariel` and the standard Firebase ID token format:
     - Issuer / authority: `https://securetoken.google.com/<project-id>`
     - Audience: `<project-id>`
   - The same keys can be overridden in production (`appsettings.Production.json`, environment variables, etc.) if a different Firebase project or a different IdP is used.

4. **Keep Domain and Application layers unaware of Firebase**

   - No changes were made to:
     - Domain entities (`Domain/Entities/User.cs`).
     - Application use cases (`Application/UseCases/...`).
   - Ownership and identity continue to be expressed purely in terms of:
     - `ExternalAuthId` as a string value object.
     - The `sub` claim from the current principal, mapped in the WebAPI layer.
   - The mapping from **external identity** (Firebase UID, OIDC subject, etc.) to a domain `User` remains encapsulated in:
     - WebAPI’s `UserController` helper (`GetExternalAuthIdFromClaims()` and `AuthorizeCurrentUserForResourceAsync`).
     - `GetUserByExternalAuthIdUseCase` in the Application layer.

5. **Document Firebase usage and keep tests IdP-agnostic**

   - Updated `README.md` to document how to use Firebase Authentication:
     - How to set `Authentication:Authority` / `Authentication:Audience` for the Firebase project `clean-architecture-ariel`.
     - How to call `POST /api/User` with:
       - `Authorization: Bearer <firebase-id-token>`.
       - `externalAuthId` set to the Firebase UID (`sub` claim).
   - Existing WebAPI tests (`WebAPI.Tests`) continue to use **test-only** authentication (`TestAuthHandler` and headers `X-Test-Only-ExternalId` / `X-Test-Only-Role`) and do not depend on Firebase.
   - This ensures we can validate authorization/ownership behavior without coupling tests to a specific IdP.

## Consequences

### Positive

- **Pluggable identity provider**:
  - Swapping Firebase for another IdP mainly involves changing the configuration values and, if necessary, the internals of `JwtAuthenticationExtensions`, without touching Domain/Application.
- **Clean architecture preserved**:
  - Identity provider knowledge is confined to the WebAPI layer.
  - Domain/Application reason only about `ExternalAuthId` and use cases, not about JWTs or Firebase.
- **Centralized auth configuration**:
  - All JWT-related configuration (issuer, audience, lifetime) is in one extension class.
  - `Program.cs` remains small and focused on composition.
- **Good developer experience**:
  - README shows concrete examples for Firebase.
  - Tests continue to run independently of any real IdP.

### Negative / Trade-offs

- **Another layer of indirection**:
  - Developers must look into `WebAPI/Authentication/JwtAuthenticationExtensions.cs` to understand exactly how auth is configured.
- **Configuration must be correct**:
  - If `Authentication:Authority` / `Authentication:Audience` are misconfigured, all tokens will fail validation.
  - This requires coordination with whoever manages the identity provider.
- **Assumes JWT-based providers**:
  - The current abstraction is tailored around JWT Bearer tokens. Using a non-JWT provider would require a different extension or authentication scheme.

## Alternatives considered

1. **Keep provider-specific wiring directly in `Program.cs`**
   - Simpler in the short term but scatters identity provider dependencies into the main composition root.
   - Harder to swap providers without editing core startup code.
   - Rejected to keep the WebAPI startup clean and minimize future churn.

2. **Push auth configuration into a shared infrastructure library**
   - Could centralize auth across multiple hosts, but would couple multiple services to the same IdP at a lower layer.
   - For this boilerplate, keeping IdP choice in the WebAPI host is clearer and maintains the typical clean-architecture dependency direction (UI → Infrastructure, not the other way around).

3. **Make Firebase-specific concepts first-class (e.g., `FirebaseUserId` value object)**
   - Would leak provider concepts into Domain/Application and harm replaceability.
   - Rejected; we keep `ExternalAuthId` provider-agnostic.

## Implementation references

- **JWT auth abstraction**: `clean-architecture/WebAPI/Authentication/JwtAuthenticationExtensions.cs`
- **WebAPI startup**: `clean-architecture/WebAPI/Program.cs`
- **Configuration**: `clean-architecture/WebAPI/appsettings.Development.json` (and other environment-specific appsettings as needed)
- **Domain user entity**: `clean-architecture/Domain/Entities/User.cs`
- **External auth ID lookup use case**: `clean-architecture/Application/UseCases/User/GetUserByExternalAuthIdUseCase.cs`
- **WebAPI tests (test-only auth)**:
  - `clean-architecture/Tests/WebAPI.Tests/TestAuthHandler.cs`
  - `clean-architecture/Tests/WebAPI.Tests/CustomWebApplicationFactory.cs`
  - `clean-architecture/Tests/WebAPI.Tests/UserControllerIntegrationTests.cs`
  - `clean-architecture/Tests/WebAPI.Tests/UserControllerAuthorizationTests.cs`
