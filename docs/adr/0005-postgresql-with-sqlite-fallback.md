# ADR 0005: PostgreSQL in Aspire, SQLite fallback for standalone use

- **Status:** Accepted
- **Date:** 2026-11-08

## Context

The full experience needs a shared database between the API and the worker. PostgreSQL is a realistic production choice and Aspire can provision it locally. However, contributors should be able to run the API or CLI tests without Docker, and a hard PostgreSQL dependency would make the first contribution unnecessarily heavy.

## Decision

- The Aspire AppHost provisions PostgreSQL and injects the `archpulse` connection string.
- `AddArchPulseDatabase` uses `AddNpgsqlDbContext` when the connection string exists.
- Otherwise it registers a SQLite `DbContext` at a shared temp path.
- The schema is created with `EnsureCreatedAsync` in v0.1; migrations are planned for v0.2.

## Consequences

- The primary development path demonstrates a real database and Aspire integration.
- Unit tests remain database-free.
- SQLite is a development fallback, not a production recommendation.
- The worker and API must use the same SQLite path if someone runs them standalone; the temp path makes that the default on a single machine.
- Schema evolution will require migrations before v1.0.
