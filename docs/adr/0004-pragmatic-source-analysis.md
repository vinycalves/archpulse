# ADR 0004: Parse project files and AppHost source instead of building the target

- **Status:** Accepted
- **Date:** 2026-11-08

## Context

ArchPulse must analyze arbitrary repositories. Building the target with MSBuild would give precise semantic information, but it is slow, requires SDKs and dependencies, may execute custom targets, and often fails on machines that do not match the target environment. The first version needs to be safe, fast, and deterministic.

## Decision

Use a pragmatic source-analysis pipeline:

1. Parse `.csproj` XML for SDK, output type, `ProjectReference`, and `PackageReference`.
2. Read Aspire AppHost `.cs` files and extract `AddProject`, `AddPostgres`, `AddRedis`, `AddRabbitMQ`, `AddDatabase`, and `WithReference` using focused regular expressions.
3. Build the graph from those facts.
4. Treat unmatched resources as external nodes rather than failing the scan.

Do not build or execute the target repository.

## Consequences

- Scans are fast and work without the target's SDK or dependencies.
- The analyzer is tolerant of partial or broken repositories.
- Regex parsing can miss dynamic or heavily abstracted AppHost code; those cases should become explicit diagnostics or plugin analyzers.
- The model is structural, not semantic. A future Roslyn-based analyzer can enrich it without changing the graph contract.
