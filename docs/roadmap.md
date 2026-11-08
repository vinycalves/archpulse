# Roadmap

ArchPulse is intentionally early. The goal for v0.1 is a complete, honest loop: scan a repository, understand the map, catch a real boundary violation, and compare two scans.

## v0.1 — Foundation

- [x] .NET solution with Core, Infrastructure, API, Worker, Web, CLI, AppHost, and tests.
- [x] Project and project-reference discovery.
- [x] Aspire AppHost parser (`AddProject`, data resources, `WithReference`).
- [x] Architecture graph with layers and kinds.
- [x] Built-in rules AP000–AP006.
- [x] Mermaid, Markdown, and JSON output.
- [x] Scan queue with PostgreSQL persistence.
- [x] Background worker.
- [x] Interactive dashboard with graph, findings, report, and recent scans.
- [x] CLI `scan` and `diff`.
- [x] CI and architecture-diff workflow.

## v0.2 — Signal

- [ ] OpenTelemetry runtime overlay (actual calls, latency, error rates) on the graph.
- [ ] GitHub App or Action that comments the architecture diff on every PR.
- [ ] `.archpulseignore` and path globs.
- [ ] Rule packs (`Aspire`, `Clean Architecture`, `Modular Monolith`) selected in config.
- [ ] Scan leases and stale-job recovery for multiple workers.
- [ ] EF Core migrations.
- [ ] Authentication for the API and multi-user scan history.

## v0.3 — Ecosystem

- [ ] Plugin API for analyzers (Java, Node.js, Python, Terraform).
- [ ] C4/Structurizr export.
- [ ] Architecture decision records linked to components.
- [ ] Drift timeline and trend charts.
- [ ] Hosted demo with a curated set of public repositories.
- [ ] VS Code extension that opens the map for the current workspace.

## v1.0 — Trust

- [ ] Stable graph and report JSON schema with versioning.
- [ ] Performance budget for 500+ project repositories.
- [ ] Deployment guides for Kubernetes and Azure Container Apps.
- [ ] Security review and SBOM in releases.
- [ ] Contributor ladder, maintainers per area, and an RFC process.
- [ ] Public case studies and a “State of Architecture Drift” report.

## Good first issues

- Add a new renderer (D2, Graphviz, SVG).
- Improve the Cytoscape layout and accessibility.
- Add a rule for “service has no health check”.
- Add a sample repository with a hexagonal architecture.
- Translate the UI and docs to Portuguese.
- Add a `--format sarif` output for GitHub code scanning.
- Add tests for malformed `.csproj` and AppHost files.
- Improve error messages for invalid Git URLs.
