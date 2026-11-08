# ADR 0007: Terminal-first CLI architecture

- **Status:** Accepted
- **Date:** 2026-11-08
- **Supersedes:** ADR 0002, ADR 0003, ADR 0005, ADR 0006

## Context

The first ArchPulse iteration included a web dashboard, API, background worker, PostgreSQL, and Aspire orchestration. That stack demonstrated distributed-system concerns, but it also added a lot of surface area before the core value was proven: **architecture rules that run in a terminal and fail CI**.

The project is meant to be an ArchUnit-style governance tool for C#, not a website. The web UI also made the first-run experience heavier and distracted from the command-line workflow that teams actually need in pull requests.

## Decision

Make the CLI the product:

- Keep `ArchPulse.Core` as the pure analysis library.
- Keep `ArchPulse.Cli` as the primary executable.
- Remove the API, worker, web frontend, PostgreSQL persistence, Aspire AppHost, and service defaults.
- Use Spectre.Console for terminal tables, trees, colors, and command parsing.
- Make `archpulse check` the CI entry point with meaningful exit codes.
- Keep JSON, Mermaid, and Markdown as secondary outputs.

## Consequences

- The repository is smaller, faster to build, and easier to contribute to.
- There is no server to deploy, no database to migrate, and no browser to test.
- The tool fits naturally into pre-commit hooks, CI jobs, and code review.
- A future interactive TUI can be added without reintroducing a web stack.
- The project no longer demonstrates hosted/distributed operation; if that becomes a goal, it should be a separate product decision rather than accidental scope.
