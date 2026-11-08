# Vision

## Mission

Make architecture rules **executable from the terminal** for C# teams: fast to run, easy to explain, and hard to ignore in CI.

## The problem

Architecture knowledge usually lives in three fragile places:

1. Diagrams that are drawn once and never updated.
2. Conventions that exist only in senior engineers' heads.
3. Pull requests where a new dependency slips through because nobody noticed it.

The result is architectural drift: the system slowly becomes something nobody designed. ArchPulse turns the repository into the source of truth for a living architecture model, then makes drift visible at the moment it is introduced.

## Who it is for

| Persona | Job to be done |
|---|---|
| C# tech lead | Understand the real dependency graph before changing a boundary. |
| Software architect | Encode decisions as rules and review architectural deltas. |
| Platform engineer | Add architecture checks to CI without a heavyweight governance program. |
| Contributor | Understand a new .NET codebase without reading every project file. |
| Educator | Show students the architecture of real C# repositories, not slideware. |

## Principles

1. **Terminal-first.** The primary interface is a command, not a website. Output must be useful in a terminal, a CI log, and a code review.
2. **Derive, don't duplicate.** The model comes from `.csproj`, project references, package references, and Aspire AppHost declarations.
3. **Explain every finding.** A violation must say which rule fired, which components are involved, and why it matters.
4. **C#-native.** Understand the things .NET teams actually use: solutions, projects, TFMs, test projects, package references, and AppHost resources.
5. **Local-first.** No account, database, server, or network dependency for the core workflow.
6. **Deterministic output.** The same repository produces the same graph and the same findings.
7. **Small, reviewable rules.** Architecture policy should be readable by the team that owns it.
8. **Open by default.** MIT license, public roadmap, RFCs for platform changes.

## Non-goals

- A web dashboard, SaaS, or hosted service.
- A drag-and-drop diagram editor.
- A runtime APM or tracing product.
- A replacement for code review or human judgment.
- A tool that builds or executes the target repository.
- A generic graph database or CMDB.

## Success metrics

- **Time to first check:** under 5 seconds for a typical solution.
- **Signal quality:** every new rule has tests and a documented rationale.
- **Adoption:** `archpulse check` runs on every pull request.
- **Community:** external contributors own rules, commands, and sample repositories.
- **Architectural outcomes:** teams can point to a drift finding they caught before production.
