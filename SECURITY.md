# Security policy

## Supported versions

ArchPulse is in early alpha. Security fixes are applied to the `main` branch.

| Version | Supported |
|---|---|
| 0.2.x | Yes |
| Older | No |

## Reporting a vulnerability

Please **do not** open a public issue for security vulnerabilities.

Send a private report to **vinycius.florencio@gmail.com** with:

- A description of the issue and its impact.
- Steps to reproduce or a proof of concept.
- The affected commit or version.
- Any suggested remediation.

You can expect an acknowledgment within 72 hours. We will coordinate a fix and a disclosure timeline with you.

## Scope

ArchPulse is a local CLI. Areas of interest:

- Path traversal or scanning outside the selected repository.
- XML external entity (XXE) or malformed `.csproj` parsing issues.
- Command injection through paths or rule files.
- Denial of service through large repositories or malicious project files.
- Terminal escape sequence injection through repository-controlled names or paths.
- Accidental disclosure of secrets in JSON, Markdown, or terminal output.

## Current security posture

- The CLI scans local paths only; there is no network listener or remote API.
- It does not build, restore, or execute the target repository.
- It does not require network access for the core workflow.
- `.csproj` files are parsed with `XDocument.Load`; the runtime default prohibits DTD processing.
- Reports may contain repository names, paths, and package names; review them before publishing.
