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