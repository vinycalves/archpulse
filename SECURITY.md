# Security policy

## Supported versions

ArchPulse is in early alpha. Security fixes are applied to the `main` branch.

| Version | Supported |
|---|---|
| 0.1.x | Yes |
| Older | No |

## Reporting a vulnerability

Please **do not** open a public issue for security vulnerabilities.

Send a private report to **vinycius.florencio@gmail.com** with:

- A description of the issue and its impact.
- Steps to reproduce or a proof of concept.
- The affected commit or version.
- Any suggested remediation.

You can expect an acknowledgment within 72 hours. We will coordinate a fix and
a disclosure timeline with you.

## Scope

ArchPulse scans repositories and runs a local web stack. Areas of interest:

- Path traversal or scanning outside the configured `Scanner:AllowedRoots`.
- Git URL validation bypass or SSRF through repository sources.
- Command injection through source values or Git arguments.
- Denial of service through large repositories or malicious project files.
- XSS in the dashboard through repository names, paths, or metadata.
- Exposure of secrets or connection strings in logs, reports, or the API.

## Current security posture

- The API has **no authentication** in v0.1. Do not expose it directly to the
  public internet; put it behind an authenticated proxy.
- The worker does not build or execute the target repository.
- GitHub scans accept only public `https://github.com/{owner}/{repo}` URLs.
- Local scans are restricted to configured allowed roots.
- Temporary clone directories are deleted after a scan.
