# Contributing to ArchPulse

Thanks for being here. ArchPulse is a terminal-first architecture governance tool for C#. Contributions that improve signal quality, explainability, or the terminal experience are especially welcome.

## Ground rules

- Be kind. Read the [Code of Conduct](CODE_OF_CONDUCT.md).
- Prefer small, focused pull requests.
- Every new rule needs a test and a rationale.
- Do not add a dependency to `ArchPulse.Core` unless it is essential. The core stays focused on analysis.
- Discuss large changes in an issue or RFC before writing a lot of code.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

No Docker, database, or web server is required.

## Build and test

```bash
dotnet restore ArchPulse.slnx
dotnet build ArchPulse.slnx
dotnet test ArchPulse.slnx
```

Run the CLI:

```bash
dotnet run --project src/ArchPulse.Cli -- scan .
dotnet run --project src/ArchPulse.Cli -- check samples/ContosoShop
dotnet run --project src/ArchPulse.Cli -- graph samples/ContosoShop --depth 2
dotnet run --project src/ArchPulse.Cli -- rules
```

Publish a local binary:

```bash
dotnet publish src/ArchPulse.Cli -c Release -o tools/archpulse
./tools/archpulse scan .
```

## Project layout

| Path | Purpose |
|---|---|
| `src/ArchPulse.Core` | Scanner, graph, rules, renderers, diff, rule catalog. |
| `src/ArchPulse.Cli` | Spectre.Console commands, settings, terminal renderer, exit codes. |
| `tests/ArchPulse.Core.Tests` | Unit tests for the core. |
| `samples/ContosoShop` | Intentionally flawed sample used by the demo and tests. |
| `docs` | Vision, architecture, rules, roadmap, ADRs. |

## Adding a rule

1. Add the rule definition to `ArchPulse.Core/Rules/RuleCatalog.cs`.
2. Implement the check in `ArchPulse.Core/Rules/ArchitectureRuleEngine.cs`.
3. Add a minimal fixture and a test in `tests/ArchPulse.Core.Tests`.
4. Update `docs/rules.md` and the README rule table.
5. If the rule needs a demo, add a deliberate violation to `samples/ContosoShop` — but keep the sample small.

Rules must be deterministic, explainable, and backed by evidence in the graph. They must not build or execute the target repository.

## Adding a command

1. Add a settings class in `src/ArchPulse.Cli/CliSettings.cs`.
2. Add a command in `src/ArchPulse.Cli/Commands`.
3. Register it in `src/ArchPulse.Cli/Program.cs`.
4. Reuse `TerminalRenderer` for rich output and keep machine-readable formats free of terminal decoration.
5. Add at least a smoke test or document how to test it manually.

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

Look for labels `good first issue`, `help wanted`, `rule`, `cli`, and `docs`. If you are not sure where to start, open a discussion or comment on an issue.

## License

By contributing, you agree that your contribution is licensed under the MIT License.
