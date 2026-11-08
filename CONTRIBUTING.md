# Contributing to ArchPulse

Thanks for being here. ArchPulse is early, opinionated, and intentionally small at the core. Contributions that improve signal quality, explainability, or the first-run experience are especially welcome.

## Ground rules

- Be kind. Read the [Code of Conduct](CODE_OF_CONDUCT.md).
- Prefer small, focused pull requests.
- Every new rule needs a test and a rationale.
- Do not add a dependency to `ArchPulse.Core` unless it is essential. The core stays infrastructure-free.
- Discuss large changes in an issue or RFC before writing a lot of code.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (for the Aspire PostgreSQL resource)
- Git

## Build and test

```bash
dotnet restore ArchPulse.slnx
dotnet build ArchPulse.slnx
dotnet test ArchPulse.slnx
```

Run the full stack:

```bash
dotnet run --project src/ArchPulse.AppHost
```

Run the CLI against the sample:

```bash
dotnet run --project src/ArchPulse.Cli -- scan samples/ContosoShop --output artifacts/contoso
```

## Project layout

| Path | Purpose |
|---|---|
| `src/ArchPulse.Core` | Scanner, graph, rules, renderers, diff. No infrastructure. |
| `src/ArchPulse.Infrastructure` | EF Core, source resolvers, scan runner, DI. |
| `src/ArchPulse.Api` | Minimal API and scan queue producer. |
| `src/ArchPulse.Worker` | Background scan consumer. |
| `src/ArchPulse.Web` | Static UI and same-origin API proxy. |
| `src/ArchPulse.AppHost` | Aspire orchestration. |
| `src/ArchPulse.Cli` | `scan` and `diff` commands. |
| `tests/ArchPulse.Core.Tests` | Unit tests for the core. |
| `samples/ContosoShop` | Intentionally flawed sample used by the demo. |
| `docs` | Vision, architecture, rules, roadmap, ADRs. |

## Adding a rule

1. Pick an id in the `AP0xx` range and document it in `docs/rules.md`.
2. Implement the check in `ArchPulse.Core/Rules/ArchitectureRuleEngine.cs`.
3. Add a minimal fixture and a test in `tests/ArchPulse.Core.Tests`.
4. Update the README rule table.
5. If the rule needs a demo, add a deliberate violation to `samples/ContosoShop` — but keep the sample small.

Rules must be deterministic, explainable, and backed by evidence in the graph. They must not build or execute the target repository.

## Adding a renderer or exporter

Renderers live in `ArchPulse.Core/Rendering` and should accept the graph or scan outcome, not infrastructure types. Add a test that asserts the stable parts of the output, not the exact formatting of every line.

## Adding a source resolver

Implement `ISourceResolver` in `ArchPulse.Infrastructure/Sources`:

- `CanResolve(sourceType)`
- `ResolveAsync(scanSource, cancellationToken)`

Return a `SourceWorkspace`. If the source is temporary, pass the temp directory so it is deleted on disposal. Never execute code from the target repository.

## Code style

- File-scoped namespaces.
- Nullable reference types enabled.
- Prefer records for immutable contracts.
- Keep public APIs documented when the intent is not obvious.
- Follow the existing formatting. The repository includes an `.editorconfig`.

## Pull request checklist

- [ ] The change is focused and has a clear “why”.
- [ ] `dotnet build ArchPulse.slnx` passes.
- [ ] `dotnet test ArchPulse.slnx` passes.
- [ ] New behavior has tests.
- [ ] Documentation or ADRs are updated when the architecture changes.
- [ ] No secrets, local paths, or generated artifacts are committed.

## Good first issues

Look for labels `good first issue`, `help wanted`, `rule`, `ui`, and `docs`. If you are not sure where to start, open a discussion or comment on an issue.

## License

By contributing, you agree that your contribution is licensed under the MIT License.
