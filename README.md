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
   - Configurable persistence (in‑memory or SQL Server).
- `clean-architecture/Tests` – Test projects:
   - `Application.Tests`, `Infrastructure.Tests`, `WebAPI.Tests` using xUnit and Microsoft Testing Platform.

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
