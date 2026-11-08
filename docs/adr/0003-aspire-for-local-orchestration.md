# ADR 0003: Use .NET Aspire for local orchestration

- **Status:** Accepted
- **Date:** 2026-11-08

## Context

Developers need PostgreSQL, the API, the worker, and the web frontend running together. Before Aspire, this meant docker-compose, shell scripts, hard-coded ports, and manual connection strings. The project also exists to demonstrate modern .NET architecture, so the developer experience is part of the product.

## Decision

Use .NET Aspire as the local orchestration layer:

- `ArchPulse.AppHost` declares PostgreSQL, the API, the worker, and the web frontend.
- The AppHost injects connection strings and service discovery metadata.
- `ServiceDefaults` provides health checks, resilient HTTP clients, service discovery, and OpenTelemetry.
- The Aspire dashboard is the development observability cockpit.

Aspire is an orchestration and development-time tool, not a runtime framework. Production deployment remains an explicit choice.

## Consequences

- `dotnet run --project src/ArchPulse.AppHost` brings up the whole system.
- The dependency graph is visible in the dashboard and matches the architecture documentation.
- The AppHost adds project references to the service projects for the generated `Projects` constants; those references appear as edges in ArchPulse's own scan and are intentionally kept.
- The team must keep the AppHost and production deployment manifests in sync.
- Docker is required for the PostgreSQL resource in development.
