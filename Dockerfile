# Build and run the Clean Architecture WebAPI using multi-stage Docker build

# =========================================================
# 1) Runtime image (small, optimized for running the API)
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Install GSSAPI/Kerberos library required by Npgsql / PostgreSQL
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Use a non-privileged HTTP port inside the container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Environment variables used by WebAPI configuration
# These can be overridden at `docker run` time with `-e` flags.
#
# UseInMemoryDB controls whether the app uses the in-memory EF Core database
# instead of SQL Server.
#   - true  => use in-memory database (no external DB dependency)
#   - false => use SQL Server connection string (see below)
ENV UseInMemoryDB=true

# Connection string for the real database when UseInMemoryDB=false.
# This maps to configuration key "ConnectionStrings:DbContext" in ASP.NET Core,
# so we use the double-underscore naming convention.
# Example for PostgreSQL running as a service named 'postgres' in docker-compose:
#   Host=postgres;Port=5432;Database=cleanarchitecture;Username=appuser;Password=devpassword;
ENV ConnectionStrings__DbContext="Host=postgres;Port=5432;Database=cleanarchitecture;Username=appuser;Password=devpassword;"

# =========================================================
# 2) Build image (contains SDK and tooling)
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first (for better layer caching)
COPY ["clean-architecture/WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["clean-architecture/Application/Application.csproj", "Application/"]
COPY ["clean-architecture/Domain/Domain.csproj", "Domain/"]
COPY ["clean-architecture/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["clean-architecture/Tests/Application.Tests/Application.Tests.csproj", "Tests/Application.Tests/"]
COPY ["clean-architecture/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj", "Tests/Infrastructure.Tests/"]
COPY ["clean-architecture/Tests/WebAPI.Tests/WebAPI.Tests.csproj", "Tests/WebAPI.Tests/"]
COPY ["clean-architecture.slnx", "."]

RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy the rest of the source
COPY clean-architecture/. .

# Optional: run tests as part of the Docker build. Uncomment if desired.
# This will cause `docker build` to fail if tests fail.
# RUN dotnet test clean-architecture.slnx --configuration Release --no-build

# Publish the WebAPI project
WORKDIR "/src/WebAPI"
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================================
# 3) Final image (runtime + published app only)
# =========================================================
FROM base AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Start the Web API
ENTRYPOINT ["dotnet", "WebAPI.dll"]
