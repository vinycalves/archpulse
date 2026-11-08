# Changelog

All notable changes to ArchPulse are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.2.0] - 2026-11-08

### Added

- Spectre.Console terminal reports with summary tables, component trees, and findings.
- `archpulse check` with CI-friendly exit codes.
- `archpulse graph` with tree, Mermaid, and JSON output.
- `archpulse diff` with terminal, Markdown, and JSON output.
- `archpulse init` for starter `.archpulse/rules.json`.
- `archpulse rules` and `archpulse explain` for rule discovery.
- `archpulse watch` for change-driven checks.
- Rule `AP007` for inconsistent target frameworks.
- Central `RuleCatalog` used by the CLI and documentation.
- Project metadata for target framework and nullable context.

### Changed

- Repositioned the project as a terminal-first architecture governance CLI for C#.
- Removed the web frontend, API, worker, PostgreSQL persistence, Aspire AppHost, and service defaults.
- Removed the web dashboard screenshot and Docker prerequisite.
- Updated the architecture docs, ADRs, roadmap, CI, and issue templates for the CLI-first model.

## [0.1.0] - 2026-11-08

Initial public alpha with scanner, API/worker, dashboard, Aspire orchestration, and CLI.
