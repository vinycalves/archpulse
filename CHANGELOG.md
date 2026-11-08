# Changelog

All notable changes to ArchPulse are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Initial project skeleton and vision.
- Project discovery for `.csproj` files, project references, and package references.
- Aspire AppHost parser for projects, data resources, and `WithReference` edges.
- Architecture graph with component kinds and layers.
- Built-in rules AP000–AP006.
- Custom forbidden dependency rules in `.archpulse/rules.json`.
- Mermaid, Markdown, and JSON renderers.
- Architecture diff and Markdown diff report.
- CLI `scan` and `diff`.
- PostgreSQL-backed scan queue with a background worker.
- Minimal API and interactive web dashboard.
- Aspire AppHost orchestration and service defaults.
- Unit tests for discovery, rules, rendering, and diff.
- CI and architecture-diff GitHub Actions workflows.

## [0.1.0] - 2026-11-08

Initial public alpha.
