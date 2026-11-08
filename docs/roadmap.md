# Roadmap

ArchPulse is intentionally focused: a terminal-first architecture governance tool for C#. The CLI is the product.

## v0.2 — CLI-first foundation

- [x] .NET solution with Core, CLI, and tests.
- [x] Project and project-reference discovery.
- [x] Aspire AppHost parser (`AddProject`, data resources, `WithReference`).
- [x] Architecture graph with layers and kinds.
- [x] Built-in rules AP000–AP007 and AP100.
- [x] Mermaid, Markdown, and JSON output.
- [x] `scan` terminal report.
- [x] `check` with CI-friendly exit codes.
- [x] `graph` dependency tree.
- [x] `diff` architecture delta.
- [x] `init`, `rules`, `explain`, and `watch`.
- [x] CI and architecture-diff workflow.

## v0.3 — Deeper C# analysis

- [ ] Roslyn namespace-level dependency rules.
- [ ] `.archpulseignore` and path globs.
- [ ] SARIF output for GitHub code scanning.
- [ ] NuGet global tool package (`dotnet tool install -g ArchPulse`).
- [ ] GitHub composite action.
- [ ] Rule packs (`Clean Architecture`, `Modular Monolith`, `Aspire`).
- [ ] Stable JSON schema version.

## v0.4 — Terminal experience

- [ ] Interactive `archpulse explore` TUI.
- [ ] Graph filters by layer, kind, and dependency type.
- [ ] Baseline cache for faster diffs in CI.
- [ ] Shell completions.
- [ ] `archpulse explain` links to docs and examples.

## v1.0 — Trust

- [ ] Performance budget for 500+ project repositories.
- [ ] Plugin analyzers for other .NET project types.
- [ ] Security review and SBOM in releases.
- [ ] Contributor ladder, maintainers per area, and an RFC process.
- [ ] Public case studies and a “State of Architecture Drift” report.

## Good first issues

- Add a new built-in rule with tests and documentation.
- Add `.archpulseignore` support.
- Add SARIF output.
- Publish the CLI as a .NET global tool.
- Translate the README and CLI messages to Portuguese.
- Add a hexagonal architecture sample.
- Improve terminal graph rendering with filters.
- Add tests for malformed `.csproj` and AppHost files.
