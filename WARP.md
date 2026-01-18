# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Commands

### Restore dependencies

Run from the repository root:

- `dotnet restore TodoApp.WebAPI/TodoApp.WebAPI.sln`

### Build

- Build the full solution (API + layers + tests):
  - `dotnet build TodoApp.WebAPI/TodoApp.WebAPI.sln`

### Run the Web API

From the repository root:

- `dotnet run --project TodoApp.WebAPI/TodoApp.WebAPI.csproj`

By default, the API runs against an in-memory database configured in `TodoApp.WebAPI/appsettings.Development.json` via `UseInMemoryDB`.

To run against SQL Server instead, set `"UseInMemoryDB": "false"` in `appsettings.Development.json` and ensure the `ConnectionStrings:DbContext` connection string is valid.

### Tests

All tests (all layers):

- `dotnet test TodoApp.WebAPI/TodoApp.WebAPI.sln`

Per test project:

- Application layer tests: `dotnet test TodoApp.Tests/TodoApp.Application.Tests/TodoApp.Application.Tests.csproj`
- Infrastructure layer tests: `dotnet test TodoApp.Tests/TodoApp.Infrastructure.Tests/TodoApp.Infrastructure.Tests.csproj`
- Web API tests: `dotnet test TodoApp.Tests/TodoApp.WebAPI.Tests/TodoApp.WebAPI.Tests.csproj`

Run a single test (example):

- `dotnet test TodoApp.Tests/TodoApp.Application.Tests/TodoApp.Application.Tests.csproj --filter "FullyQualifiedName~TodoApp.Application.Tests.ToDoItemServiceTests.GetAllItemsAsync_ReturnsItems"`

### Linting / formatting

There is no dedicated linting or formatting command defined in this repository. Code analysis runs as part of `dotnet build` via the standard .NET SDK analyzers.

## Architecture and structure

### High-level layout

The solution follows a Clean Architecture-style layering, wired together by the `TodoApp.WebAPI` project and the `TodoApp.WebAPI/TodoApp.WebAPI.sln` solution:

- `TodoApp.Domain`: Entity and enum definitions that model the core ToDo domain.
- `TodoApp.Application`: Application-layer contracts and business services.
- `TodoApp.Infrastructure`: EF Core-based data access and repository implementations.
- `TodoApp.WebAPI`: ASP.NET Core Web API host, controllers, filters, and composition root.
- `TodoApp.Tests`: xUnit test projects for the Application, Infrastructure, and WebAPI layers.

### Domain layer (`TodoApp.Domain`)

- Contains the core domain entities (`User`, `ToDoList`, `ToDoItem`) and the `PriorityLevel` enum under `Entities/` and `Enums/`.
- No dependencies on application, infrastructure, or web projects.
- Relationships:
  - `User` → many `ToDoList`.
  - `ToDoList` → many `ToDoItem`.

### Application layer (`TodoApp.Application`)

- Defines repository and service contracts in `Interfaces/` (e.g., `IToDoItemRepository`, `IToDoItemService`, `IUserRepository`, `IUserService`, etc.).
- Implements business logic in `Services/` (e.g., `ToDoItemService` delegates persistence to `IToDoItemRepository`).
- Depends only on the Domain project for entity types.

**Typical flow in this layer:**

1. Web layer calls an `I*Service` interface (e.g., `IToDoItemService`).
2. Service orchestrates business rules and validation.
3. Service calls the corresponding repository interface (e.g., `IToDoItemRepository`).

### Infrastructure layer (`TodoApp.Infrastructure`)

- Implements persistence using EF Core:
  - `Data/AppDbContext` exposes `DbSet<User>`, `DbSet<ToDoList>`, `DbSet<ToDoItem>` and applies entity configurations.
  - `Data/AppDbContextFactory` adds an `AddInMemoryDatabase` extension on `IServiceCollection` to configure an in-memory `AppDbContext` used in development/demo.
- Entity configurations live under `Configurations/` and define mapping and seeding for users, lists, and items.
- Repository implementations in `Repositories/` (e.g., `ToDoItemRepository`) implement the Application-layer repository interfaces and encapsulate EF Core queries and mutations.

**Data access flow:**

1. Application service calls an `I*Repository` interface.
2. Concrete repository (e.g., `ToDoItemRepository`) operates on `AppDbContext` using EF Core.
3. Domain entities are materialized and passed back to the Application layer.

### Web API layer (`TodoApp.WebAPI`)

- Composition root is `Program.cs`:
  - Reads `UseInMemoryDB` from configuration to choose between in-memory DB (`AddInMemoryDatabase`) and SQL Server (`UseSqlServer` on `AppDbContext`).
  - Registers Application services (`IToDoItemService`, `IUserService`, `IToDoListService`) and Infrastructure repositories (`IToDoItemRepository`, `IUserRepository`, `IToDoListRepository`) with the DI container.
  - Configures Swagger, controllers, and the `GlobalExceptionFilter`.
  - Seeds initial data when using the in-memory database.
- Controllers live under `Controllers/` and are thin wrappers over services:
  - `ToDoItemController` exposes CRUD endpoints for `ToDoItem` (`GetAll`, `GetById`, `Create`, `Update`, `Delete`).
  - `ToDoListController` manages `ToDoList` creation and retrieval.
  - `UserController` creates users and fetches individual or all users.
- `Filters/GlobalExceptionFilter` provides centralized handling for unhandled exceptions, returning a generic 500 response and logging the error.

**Request pipeline overview:**

1. HTTP request hits a controller action (e.g., `ToDoItemController.GetAll`).
2. Controller calls the corresponding Application service (e.g., `IToDoItemService.GetAllItemsAsync`).
3. Service delegates data access to an Application repository interface.
4. Infrastructure repository implementation executes EF Core operations through `AppDbContext`.
5. Resulting domain entities are returned back up through the service and controller to the client.

### Testing layout (`TodoApp.Tests`)

- Uses xUnit (`xunit`, `xunit.runner.visualstudio`) and Moq for mocking.
- `TodoApp.Application.Tests` focuses on service-level behavior (e.g., `ToDoItemServiceTests` verifies that services delegate correctly to repositories and handle results).
- `TodoApp.WebAPI.Tests` focuses on controller behavior (e.g., `ToDoItemControllerTests` asserts HTTP status codes and response shapes when services return specific results).
- `TodoApp.Infrastructure.Tests` is present to validate repository/data-access behavior.

All test projects reference the Domain, Application, Infrastructure, and WebAPI projects to allow end-to-end testing of each layer in isolation.
