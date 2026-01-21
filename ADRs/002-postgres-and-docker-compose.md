# ADR 002: Use PostgreSQL with Dockerfile and docker-compose

## Status
Accepted

## Context
We want a simple, repeatable way to run the API against a real relational database for local and dev work. Relying only on the in-memory database (or a manually configured SQL Server) makes it harder to mirror production‑style usage and to share a consistent setup across machines.

## Decision
We will use PostgreSQL as the primary example relational database and run it together with the WebAPI using:
- A Dockerfile for building and running the WebAPI container.
- A docker-compose file that starts both the WebAPI and a Postgres database with a single command.

## Rationale
- **Reproducible dev environment**: `docker compose up` gives any developer a working API + database without extra manual setup.
- **PostgreSQL as a common default**: Postgres is widely supported on cloud and container platforms and is a good default for relational workloads.
- **Closer to production**: Running against a real database helps catch issues that would be hidden by the in-memory provider.
- **Clear separation of concerns**: The Dockerfile focuses on building/running the app image, while docker-compose wires the app image to infrastructure like Postgres.

## Consequences
- Developers can choose between:
  - In-memory database for fast local testing and experiments.
  - PostgreSQL via docker-compose for more realistic, persistent storage.
- The README documents how to run the Postgres + WebAPI stack so onboarding is straightforward.
- Database credentials in docker-compose remain dev-only; real environments must provide secrets via secure mechanisms.