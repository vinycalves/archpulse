# Vision

## Mission

Make software architecture **visible, testable, and reviewable** in the place where it actually lives: the repository.

## The problem

Architecture knowledge is usually stored in three fragile places:

1. Diagrams that are drawn once and never updated.
2. Conventions that exist only in senior engineers' heads.
3. Pull requests where a new dependency slips through because nobody noticed it.

The result is architectural drift: the system slowly becomes something nobody designed. ArchPulse turns the repository into the source of truth for a living architecture model, then makes drift visible at the moment it is introduced.

## Who it is for

| Persona | Job to be done |
|---|---|
| Tech lead | Understand the real dependency graph before changing a boundary. |
| Software architect | Encode decisions as executable rules and review architectural deltas. |
| Platform engineer | Add architecture checks to CI without a heavyweight governance program. |
| Contributor | Understand a new codebase without reading every project file. |
| Educator | Show students the architecture of real repositories, not slideware. |

## Principles

1. **Derive, don't duplicate.** The model comes from project files and Aspire declarations, not from a second diagram that can rot.
2. **Explain every finding.** A violation must say which rule fired, which components are involved, and why it matters.
3. **Aspire-native, .NET-first.** The first-class experience is .NET + Aspire; other ecosystems arrive through plugins.
4. **Local-first.** The scanner must work on a laptop with no cloud account. The hosted experience is an optional layer.
5. **Deterministic output.** The same repository produces the same graph and the same findings.
6. **Small, reviewable rules.** Architecture policy should be readable by the team that owns it.
7. **Open by default.** MIT license, public roadmap, RFCs for platform changes.

## Non-goals

- A drag-and-drop diagram editor.
- A runtime APM or tracing product.
- A replacement for code review or human judgment.
- A tool that executes or builds the target repository.
- A generic graph database or CMDB.

## Success metrics

- **Time to first map:** under 5 minutes from clone to dashboard.
- **Signal quality:** every new rule has tests and a documented rationale.
- **Adoption:** scans run in CI on every pull request.
- **Community:** external contributors own rules, renderers, and sample repositories.
- **Architectural outcomes:** teams can point to a drift finding they caught before production.

## The long game

ArchPulse should become the place where an architecture decision is not just written down, but continuously checked. The project is successful when a reviewer can look at a pull request and say: “this changes the architecture, and here is exactly how.”
