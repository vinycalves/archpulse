# ADR 0002: Modular monolith plus background worker

- **Status:** Superseded by [ADR 0007](0007-terminal-first-cli.md)
- **Date:** 2026-11-08

## Context

ArchPulse needs a web API, a dashboard, a scan engine, and background processing. The first instinct for a distributed-systems portfolio project is to split everything into microservices. That would create operational overhead, distributed transactions, and a lot of empty ceremony before the product has users.

## Decision

Start with a modular monolith for the API and a separate background worker:

- `ArchPulse.Core` is the pure analysis module.
- `ArchPulse.Infrastructure` contains persistence and source resolution.
- `ArchPulse.Api` is a deployable host.
- `ArchPulse.Worker` is a separate deployable host that shares the database and core.
- `ArchPulse.Web` is a separate static frontend host.

The worker is separated because scans are long-running, CPU/IO-bound, and need to survive API restarts. Everything else stays together until a real constraint justifies extraction.

## Consequences

- Two deployables and one database: simple enough to run locally, real enough to demonstrate distributed concerns.
- The API and worker share a schema, which requires discipline and migrations.
- A future extraction of the scan runner or rules engine is possible because the core has no infrastructure dependencies.
- We avoid the distributed monolith trap: no service calls between modules for in-process work.
