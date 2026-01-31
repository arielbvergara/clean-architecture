# Clean Architecture .NET Boilerplate

This repository is a **.NET 10.0 Clean Architecture boilerplate** – a base project for creating new services that will eventually be published as a reusable GitHub template.

It implements a simple **user CRUD** with extra functionality around **user management, roles, authentication and authorization**, with **security as a first‑class concern**.

## Technologies

- **.NET 10.0 Web API** following Clean Architecture (Domain, Application, Infrastructure, WebAPI).
- **PostgreSQL** as the primary relational database, usually run via Docker Compose.
- **Firebase Authentication (JWT Bearer)** as the default identity provider, fully abstracted behind the WebAPI so Domain/Application stay provider‑agnostic.
- **Docker & Docker Compose** for a reproducible local environment (WebAPI + Postgres with a single command).
- **Sentry** for error and performance monitoring, wired into the WebAPI/Infrastructure layers via an observability abstraction.

Testing and operational choices (Microsoft Testing Platform, security headers, rate limiting, CI dependency scanning, etc.) are captured in the ADRs.

## What you get

- A ready‑to‑use **starting point for new APIs** built with Clean Architecture.
- A secure **user CRUD** with:
  - User create/read/update/delete.
  - Roles and admin workflows.
  - Authentication and authorization built on top of Firebase ID tokens.
  - Hardened configuration for production (CORS, hosts, security headers, rate limiting).
- Integrated **monitoring with Sentry** and opinionated logging and error handling.

## Architecture decisions

Key architectural choices are documented under `ADRs/` and include, among others:

- PostgreSQL + Docker Compose for local/dev environments.
- JWT/Firebase authentication, `/me` self‑service endpoints, and admin‑only management routes.
- User role and soft‑delete lifecycle.
- Admin bootstrap flow and Firebase Admin SDK integration.
- Security headers, rate limiting, hardened production configuration.
- Sentry integration and standardized error handling/logging.

When adapting this boilerplate for a new service, review the ADRs to understand the default security and operations posture before changing anything.

## Running the project (local development)

### 1. Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/) or later.
- [Docker](https://www.docker.com/) and Docker Compose.
- A Firebase project (if you want to exercise authenticated endpoints).
- Optional: a Sentry project (if you want monitoring enabled locally).

### 2. Quick start with Docker Compose (WebAPI + Postgres)

From the repository root:

```bash
docker compose up --build
```

This will:
- Build and start the WebAPI container.
- Start a PostgreSQL container configured for local development.

Once running:
- API base URL: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`

Stop everything with:

```bash
docker compose down
```

### 3. Run the WebAPI directly (without Docker)

If you prefer to run the API directly from the SDK (for example when iterating quickly on code):

```bash
dotnet build clean-architecture.slnx
DotNetCliToolReference

dotnet run --project clean-architecture/WebAPI/WebAPI.csproj
```

By default, the WebAPI reads its database and auth settings from `clean-architecture/WebAPI/appsettings.Development.json` and environment variables. You can point it to a local Postgres instance or use the in‑memory database depending on your configuration.

### 4. Configure Firebase Authentication (optional but recommended)

To call secured endpoints, configure Firebase as the identity provider (example values):

- In `clean-architecture/WebAPI/appsettings.Development.json`:
  - `Authentication:Authority = "https://securetoken.google.com/<your-firebase-project-id>"`
  - `Authentication:Audience = "<your-firebase-project-id>"`
- Obtain a Firebase **ID token** for a signed‑in user and send it as:
  - `Authorization: Bearer <firebase-id-token>`

The API validates the token and maps the subject (`sub`) claim to the `ExternalAuthId` used by the user domain model.

### 5. Configure Sentry monitoring (optional)

Sentry is completely optional; if disabled, the app still runs normally.

Set these environment variables (or the equivalent settings in `appsettings*.json`) to enable Sentry when running locally or via Docker:

```bash
export Sentry__Enabled=true
export Sentry__Dsn={{SENTRY_DSN}}
export Sentry__Environment=Development
export Sentry__TracesSampleRate=0.2
```

Replace `{{SENTRY_DSN}}` with your own DSN from Sentry. When enabled, unhandled errors and selected warnings from the WebAPI will be sent to Sentry with route and environment context.

## Configuration and environment variables

Most configuration lives in `clean-architecture/WebAPI/appsettings.json` plus the environment-specific files (`appsettings.Development.json`, `appsettings.Production.json`). Any `Section:Key` can be overridden with an environment variable named `Section__Key` (double underscore).

Common examples:

- **Database**
  - `UseInMemoryDB` – `true` to use the in-memory database, `false` to use Postgres.
  - `ConnectionStrings:DbContext` / `ConnectionStrings__DbContext` – Postgres connection string when `UseInMemoryDB` is `false`.
- **Authentication (Firebase)**
  - `Authentication:Authority` / `Authentication__Authority` – JWT issuer (e.g. `https://securetoken.google.com/<project-id>`).
  - `Authentication:Audience` / `Authentication__Audience` – JWT audience (usually the Firebase project id).
- **CORS / frontend**
  - `ClientApp:Origin` / `ClientApp__Origin` – allowed browser origin (e.g. `http://localhost:3000` in development).
- **Admin bootstrap (optional)**
  - `AdminUser:SeedOnStartup` / `AdminUser__SeedOnStartup` – whether to seed an admin user at startup.
  - `AdminUser:DisplayName`, `AdminUser:Email`, `AdminUser:Password` – admin identity details, typically set via environment variables in higher environments.
- **Sentry (optional)**
  - `Sentry:Enabled` / `Sentry__Enabled` – toggle Sentry on/off.
  - `Sentry:EnableLogs` / `Sentry__EnableLogs` – toggle Sentry logs on/off.
  - `Sentry:Dsn` / `Sentry__Dsn` – Sentry DSN (must come from a secret store or environment variable).
  - `Sentry:Environment` / `Sentry__Environment` – logical environment name (Development, Staging, Production).

In production, you are expected to:

- Set `AllowedHosts` to the hostnames the API should serve.
- Configure `ClientApp:Origin` and, if needed, CORS overrides via environment variables.
- Provide real Postgres, Firebase, admin, and Sentry settings via your preferred secret management solution.

---

In short: **clone this repo, configure database, Firebase, and (optionally) Sentry/env-specific values, then run either `docker compose up --build` or `dotnet run --project clean-architecture/WebAPI/WebAPI.csproj` to start the boilerplate API backed by PostgreSQL.**
