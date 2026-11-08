# ADR 0001: Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-11-08
- **Deciders:** ArchPulse maintainers

## Context

ArchPulse is a project about architecture. Its own architecture must be as legible as the systems it analyzes. Decisions that live only in chat threads or pull request comments are hard to revisit and easy to contradict.

## Decision

Record significant decisions as Architecture Decision Records in `docs/adr`. Each ADR has a status, date, context, decision, and consequences. Superseded ADRs remain in the repository and link to their replacement.

## Consequences

- Contributors can understand why the system looks the way it does.
- Reviews can challenge a decision explicitly instead of relitigating it implicitly.
- The repository becomes a portfolio artifact for architectural thinking, not only code.
- The cost is small: one short document per significant decision.
