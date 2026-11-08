# ADR 0008: Spectre.Console for terminal UX

- **Status:** Accepted
- **Date:** 2026-11-08

## Context

A terminal-first tool still needs a good user experience. Plain `Console.WriteLine` output is hard to scan: architecture reports contain summaries, trees, findings, severities, and paths. The tool also needs command parsing, help text, and validation.

## Decision

Use **Spectre.Console** for rendering and **Spectre.Console.Cli** for command parsing:

- Tables for summaries and findings.
- Trees for components and dependency graphs.
- Panels for rule explanations.
- Markup for severity colors.
- Command/settings classes for `scan`, `check`, `graph`, `diff`, `init`, `rules`, `explain`, and `watch`.

Keep JSON, Markdown, and Mermaid output free of terminal decoration so they remain machine-readable.

## Consequences

- The CLI feels like a product rather than a script.
- Commands and settings are discoverable through `--help`.
- Terminal output is readable in local terminals and CI logs.
- Spectre.Console is a focused dependency; it does not pull in a web stack.
- Renderers must be careful to escape repository-controlled text before passing it as markup.
