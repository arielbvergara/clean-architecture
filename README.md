# Clean Architecture .NET Sample

This repository contains a layered .NET 10.0 Web API implementing a simple user management domain using Clean Architecture principles.

## Project Structure

- `clean-architecture.slnx` – Solution file targeting `net10.0`.
- `clean-architecture/Domain` – Core domain model:
   - Entities, value objects, and primitives (e.g., `Result<T, TE>`).
   - No dependencies on other layers; persistence-agnostic.
- `clean-architecture/Application` – Application layer:
   - Use cases, DTOs, interfaces (ports), and application exceptions.
   - Uses `Result<..., AppException>` to represent success/failure.
- `clean-architecture/Infrastructure` – Infrastructure layer:
   - EF Core `AppDbContext`, entity configurations, repositories.
   - Implements application interfaces (e.g., `IUserRepository`).
- `clean-architecture/WebAPI` – ASP.NET Core Web API:
   - Controllers, global exception filter, DI wiring, and Swagger.
   - Configurable persistence (in-memory, PostgreSQL, or Firestore via Google Cloud Firestore).
- `clean-architecture/Tests` – Test projects:
   - `Application.Tests`, `Infrastructure.Tests`, `WebAPI.Tests` using xUnit and Microsoft Testing Platform.

## Architecture Decision Records (ADRs)

Architecture decisions are documented under the `ADRs/` directory. Key ADRs related to authentication, authorization, and user lifecycle:

- `001-use-microsoft-testing-platform-runner.md` – adopt Microsoft Testing Platform as the test runner.
- `002-postgres-and-docker-compose.md` – configure PostgreSQL and Docker Compose for local development.
- `003-webapi-authentication-and-authorization-for-user-endpoints.md` – initial WebAPI authZ model for `UserController`.
- `004-guid-identity-and-authentication-provider.md` – defines the identity provider abstraction and how JWT-based authentication is integrated without leaking provider specifics into the Domain/Application layers.
- `005-webapi-auth-refinements-jwt-policies-me-endpoints.md` – refine JWT config for Firebase, add policies/OwnsUser handler, and introduce `/me` endpoints and failure-path tests.
- `006-user-role-and-soft-delete-lifecycle.md` – add `Role`, `IsDeleted`, `DeletedAt` to `User` and implement a soft-delete lifecycle for users.
- `007-admin-bootstrap-and-firebase-admin-integration.md` – define the secure, idempotent admin user bootstrap process and Firebase Admin integration owned by the WebAPI layer.
- `008-admin-only-user-id-email-endpoints.md` – make id/email-based user management endpoints admin-only and reserve `/me` routes for self-service operations.
- `009-automated-dependency-management-with-dependabot.md` – configure Dependabot to keep NuGet packages, GitHub Actions, and Docker images up to date.
- `010-hardened-production-configuration.md` – introduce secure-by-default production configuration for CORS, `AllowedHosts`, and environment-specific settings.
- `011-security-headers-and-rate-limiting.md` – add standardized security headers and ASP.NET Core rate limiting policies (`Fixed`/`Strict`) to harden the WebAPI surface.
- `012-supply-chain-and-dependency-scanning-ci.md` – add a `security-and-deps` CI job for `dotnet list package` vulnerability scans and SBOM generation, with guardrails.
- `013-standardized-error-handling-and-security-logging.md` – define standardized API error envelopes, correlation IDs, and structured security logging aligned with OWASP A09/A10.
- `014-security-design-and-threat-model-for-user-endpoints.md` – canonical security design and threat model for user endpoints, defining roles, ownership, and enumeration resistance mechanisms.

## Getting Started

### Prerequisites

- .NET SDK 10.0 (or later)
- Optional: SQL Server instance if not using the in‑memory database

### Build

From the repository root:

```bash
dotnet build clean-architecture.slnx
```

### Authentication & Authorization

The Web API is secured using ASP.NET Core authentication/authorization:

- **JWT Bearer authentication** is configured in `clean-architecture/WebAPI/Program.cs`.
- Configuration values are read from `clean-architecture/WebAPI/appsettings*.json` under the `Authentication` section:
  - `Authentication:Authority` – the issuer/authority for JWT tokens (e.g., your OIDC / Entra ID authority URL).
  - `Authentication:Audience` – the API audience / resource identifier expected in the token.
- A **fallback authorization policy** requires all endpoints to be authenticated by default.
- The `UserController` is decorated with `[Authorize]`, and additional ownership checks ensure that users can only access their own user record unless they are in an elevated role.

For local development, you can:

- Use real JWTs from an identity provider (for example, Firebase Authentication) and configure `Authentication:Authority` / `Authentication:Audience` accordingly.
- Or use test-only authentication via the WebAPI tests (see below) without hitting a real IdP.

### Using Firebase Authentication (email/password)

This project can be used with Firebase Authentication as the identity provider. For the Firebase project `clean-architecture-ariel`:

- Set in `clean-architecture/WebAPI/appsettings.Development.json`:
  - `Authentication:Authority = "https://securetoken.google.com/clean-architecture-ariel"`
  - `Authentication:Audience = "clean-architecture-ariel"`
- On the client side (or in Postman), obtain a Firebase **ID token** for an authenticated user and send it as:
  - `Authorization: Bearer <firebase-id-token>`
- When creating the domain user via `POST /api/User`, use the Firebase UID (the `sub` claim from the token) as `externalAuthId`:

```json
{
  "email": "user@example.com",
  "name": "User Name",
  "externalAuthId": "<firebase-uid-from-token-sub>"
}
```

The WebAPI uses the `sub` claim to resolve the current user and enforce record ownership, so this mapping keeps authentication and authorization aligned.

### Configuring Firebase Admin credentials for admin bootstrap

To support the admin user bootstrap flow (see ADR 007), the WebAPI uses the Firebase Admin SDK with **Application Default Credentials (ADC)**. You must provide a Firebase service account JSON key file and point `GOOGLE_APPLICATION_CREDENTIALS` at it.

> These steps are for local development and Docker-based environments only. Do not commit service account JSON files to source control.

#### 1. Create/download the Firebase Admin SDK service account JSON

1. In the Firebase console for your project (e.g., `clean-architecture-ariel`), go to **Project settings → Service accounts**.
2. Click **Generate new private key** for the Firebase Admin SDK service account.
3. Download the JSON file and save it outside the repository, for example:
   - `~/secrets/firebase-adminsdk.json`

#### 2. Configure local development (dotnet run)

`WebAPI` uses the `GOOGLE_APPLICATION_CREDENTIALS` environment variable to locate the service account JSON. In development:

- `clean-architecture/WebAPI/Properties/launchSettings.json` is preconfigured with:
  - `"GOOGLE_APPLICATION_CREDENTIALS": "~/secrets/firebase-adminsdk.json"`
- `Program.cs` expands `~` to your user profile directory on startup, so the JSON file must exist at that path.

To run locally with admin bootstrap enabled:

1. Place the downloaded JSON at `~/secrets/firebase-adminsdk.json`.
2. Ensure `GOOGLE_APPLICATION_CREDENTIALS` is set (either via `launchSettings.json` or your shell):

   ```bash
   export GOOGLE_APPLICATION_CREDENTIALS=~/secrets/firebase-adminsdk.json
   ```

3. Configure `AdminUser` in `clean-architecture/WebAPI/appsettings.Development.json` or via environment variables (see ADR 007 for details).
4. Run the Web API:

   ```bash
   dotnet run --project clean-architecture/WebAPI/WebAPI.csproj
   ```

#### 3. Configure Docker Compose

The provided `docker-compose.yml` mounts the same JSON file into the WebAPI container and sets `GOOGLE_APPLICATION_CREDENTIALS` accordingly:

- Volume mount:

  ```yaml
  - ~/secrets/firebase-adminsdk.json:/app/firebase-adminsdk.json
  ```

- Environment variable:

  ```yaml
  GOOGLE_APPLICATION_CREDENTIALS: /app/firebase-adminsdk.json
  ```

To use this setup:

1. Ensure `~/secrets/firebase-adminsdk.json` exists on the host machine.
2. Start the stack:

   ```bash
   docker compose up --build
   ```

The WebAPI container will use the mounted service account file for Firebase Admin operations, including seeding the initial admin user when configured.

### Database providers (InMemory, Postgres, Firestore)

The WebAPI supports multiple database providers behind the same `IUserRepository` abstraction:

- **InMemory** – fast, ephemeral database used by default in the `Testing` environment.
- **Postgres** – relational database configured via `ConnectionStrings:DbContext` and used by default in development/Docker Compose (see ADR-002).
- **Firestore** – document database backed by Google Cloud Firestore, configured via `Database:FirestoreProjectId` and `GOOGLE_APPLICATION_CREDENTIALS`.

Provider selection is controlled by the `Database:Provider` configuration value (see ADR-015):

```json
{
  "UseInMemoryDB": "false",
  "ConnectionStrings": {
    "DbContext": "Host=localhost;Port=5432;Database=cleanarchitecture;Username=appuser;Password=devpassword;"
  },
  "Database": {
    "Provider": "Postgres", // or "InMemory" or "Firestore"
    "FirestoreProjectId": "your-firestore-project-id"
  }
}
```

- In the `Testing` environment, the application always uses the in-memory provider, regardless of `Database:Provider`.
- In other environments, when `Database:Provider` is set, it selects between `InMemory`, `Postgres`, and `Firestore` without changing application or domain code.

### Test authentication in `WebAPI.Tests`

The `WebAPI.Tests` project uses a lightweight test authentication handler (`TestAuthHandler`) wired via `CustomWebApplicationFactory`:

- **TEST-ONLY** headers are used to simulate identities and roles when running tests:
  - `X-Test-Only-ExternalId` is mapped to the `sub` claim.
  - `X-Test-Only-Role` (for example, `Admin`) is mapped to a role claim.
- These headers are only honored inside the in-memory test host configured by `CustomWebApplicationFactory`; the real WebAPI uses JWT bearer tokens and ignores these headers entirely.
- The application then maps this external identifier to the domain user via `ExternalAuthId` and `GetUserByExternalAuthIdUseCase`.

This setup allows integration tests to exercise authentication and record-ownership behavior without depending on a real identity provider, while keeping a clear separation from production authentication flows.

### Run with Docker Compose (PostgreSQL + WebAPI)

From the repository root, you can start the API and a PostgreSQL database using Docker Compose:

```bash
docker compose up --build
```

This will:
- Start a `postgres` container with a dev-only database and credentials.
- Build and start the `webapi` container configured to talk to that Postgres instance.

Once running:
- API base URL (inside Docker): `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`

To stop the environment:

```bash
docker compose down
```
