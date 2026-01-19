# AGENTS.md

This file provides guidance to AI agents (e.g., Warp, Cursor, Claude, GitHub Copilot) when working with code in this repository.

## Repository layout and architecture

- Root
  - `clean-architecture.slnx` is the .NET solution file targeting `net10.0`.
  - `README.md` documents high-level TODOs (e.g., adopting `Microsoft.Testing.Platform` as the test runner).
- Main code lives under `clean-architecture/`:
  - `Domain/`
    - Core domain model: entities (`Entities/`), value objects (`ValueObject/`), and primitives like `Result<T, TE>`.
    - No dependencies on other projects; everything here should be persistence-agnostic.
  - `Application/`
    - Application layer orchestrating use cases and DTOs.
    - `Dtos/User/` contains request/response DTOs used by the API and use cases.
    - `Interfaces/` defines ports such as `IUserRepository`, which the infrastructure implements.
    - `Exceptions/` defines `AppException` and specific exception types (validation, conflict, not-found, infrastructure, etc.).
    - `UseCases/` contains use case classes, grouped by feature (e.g., `UseCases/User/`). Each use case typically:
      - Validates and creates domain value objects.
      - Interacts with repositories via interfaces.
      - Returns `Domain.Primitives.Result<..., AppException>` to encode success/failure.
    - `UseCases/DependencyInjection.cs` exposes `AddUseCases(this IServiceCollection)` to register all use cases with DI.
  - `Infrastructure/`
    - Data access and other external concerns.
    - `Data/AppDbContext` is the EF Core `DbContext` for the application, configured with entity type configurations.
    - `Data/AppDbContextFactory.AddInMemoryDatabase` wires an in-memory `AppDb` for development/testing scenarios.
    - `Configurations/UserConfiguration` maps domain value objects to scalar database columns using EF Core value converters.
    - `Repositories/UserRepository` implements `IUserRepository` on top of `AppDbContext`.
  - `WebAPI/`
    - ASP.NET Core Web API host and composition root.
    - `Program.cs` wires up:
      - Controllers and global filters (including `GlobalExceptionFilter`).
      - Swagger/Swashbuckle for API exploration in development.
      - Dependency injection for `IUserRepository` → `UserRepository` and application use cases via `AddUseCases()`.
      - Database configuration:
        - Reads `UseInMemoryDB` from configuration.
        - When `UseInMemoryDB` is `true`, uses `AddInMemoryDatabase()` to configure an in-memory EF Core database.
        - Otherwise, configures SQL Server via `UseSqlServer` using the `ConnectionStrings:DbContext` connection string.
      - Database initialization at startup (`EnsureCreated` when using a real database).
    - `Controllers/UserController` exposes CRUD-style endpoints for `User`, consuming application use cases directly via constructor injection and mapping `Result<..., AppException>` to HTTP status codes.
    - `Filters/GlobalExceptionFilter` provides a last-resort 500 handler for unhandled exceptions.
    - `appsettings.json` and `appsettings.Development.json` configure logging, `UseInMemoryDB`, and database connection strings.
  - `Tests/`
    - `Application.Tests/`, `Infrastructure.Tests/`, and `WebAPI.Tests` test projects target `net10.0` and reference the corresponding layers.
    - All test projects set `<DotNetTestRunner>Microsoft.Testing.Platform</DotNetTestRunner>` and use xUnit (`[Fact]`, etc.) plus supporting packages like `FluentAssertions` and `Moq`.
    - `Application.Tests` currently includes a smoke test (`MicrosoftTestingPlatformSmokeTests`) to verify the Microsoft Testing Platform runner is correctly configured.

Overall dependency direction:

- `Domain` → no project references.
- `Application` → depends on `Domain`.
- `Infrastructure` → depends on `Application` and `Domain`.
- `WebAPI` → depends on `Application`, `Domain`, and `Infrastructure`.
- Test projects reference only the layers they are intended to validate.

HTTP requests flow: `WebAPI` controller → `Application` use case → `Domain` entities/value objects and `Infrastructure` repositories → back to controller via `Result<T, AppException>`, then mapped to HTTP responses, with `GlobalExceptionFilter` as a final safeguard.

## Build and run

Run these commands from the repository root (where `clean-architecture.slnx` lives).

### Build the solution

- Build all projects:
  - `dotnet build clean-architecture.slnx`

This will compile the Web API, application, domain, infrastructure, and test projects targeting `net10.0`.

### Run the Web API

- Run the API using the WebAPI project:
  - `dotnet run --project clean-architecture/WebAPI/WebAPI.csproj`

Behavior:

- In `Development` environment, Swagger UI is enabled and configuration is read from `WebAPI/appsettings.Development.json`.
- Database selection is controlled by configuration:
  - `UseInMemoryDB = true` → uses in-memory EF Core database (`AddInMemoryDatabase`).
  - `UseInMemoryDB = false` → uses SQL Server with the `ConnectionStrings:DbContext` connection string and calls `EnsureCreated()` on startup.

## Testing

All test projects use xUnit with `Microsoft.Testing.Platform` as the runner.

### Run all tests

- From the repository root:
  - `dotnet test clean-architecture.slnx`

This will run tests across `Application.Tests`, `Infrastructure.Tests`, and `WebAPI.Tests` using the configured Microsoft Testing Platform runner.

### Run tests for a single project

- Application layer tests only:
  - `dotnet test clean-architecture/Tests/Application.Tests/Application.Tests.csproj`
- Infrastructure layer tests only:
  - `dotnet test clean-architecture/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
- Web API tests only:
  - `dotnet test clean-architecture/Tests/WebAPI.Tests/WebAPI.Tests.csproj`

### Run a single test or test class

Use the standard `--filter` syntax supported by `dotnet test` (and honored by Microsoft.Testing.Platform):

- Run a single test method (example uses the existing smoke test):
  - `dotnet test clean-architecture/Tests/Application.Tests/Application.Tests.csproj --filter "Name=Smoke_ShouldRun_WhenUsingMicrosoftTestingPlatform"`

- Run all tests in a given class (replace `UserTests` with the actual class name):
  - `dotnet test clean-architecture/Tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~UserTests"`

## Notes on tooling and linting

- There is currently no repo-specific `.config/dotnet-tools.json` or dedicated lint/format configuration.
- Static analysis and nullable reference checks run as part of the normal `dotnet build` process using the SDK’s built-in analyzers and project settings (e.g., `<Nullable>enable</Nullable>`).