# Architecture rules

ArchPulse evaluates rules after building the graph. Every finding is deterministic and explainable.

Use the CLI to inspect the catalog:

```bash
archpulse rules
archpulse explain AP002
```

## Built-in rules

| Rule | Severity | Rationale |
|---|---|---|
| `AP000` | Info | The scan found no `.csproj` files. This usually means the wrong path or a non-.NET repository. |
| `AP001` | Error | A project reference cycle makes the build graph fragile and often signals a missing abstraction. |
| `AP002` | Error | Domain/application code must not depend on infrastructure, presentation, runtime, or orchestration. This is the core dependency rule. |
| `AP003` | Error | Production code referencing a test project leaks test dependencies into the product. |
| `AP004` | Error | A service referencing the Aspire AppHost inverts the orchestration dependency. |
| `AP006` | Warning | A domain project referencing EF Core, Npgsql, SQL Server, Dapper, Redis, RabbitMQ, MassTransit, Kafka, or MongoDB couples the domain to persistence. |
| `AP007` | Warning | A project targeting a different framework than the solution majority signals accidental drift. |
| `AP100` | Configurable | A repository-specific forbidden dependency from `.archpulse/rules.json`. |

## Custom rules

Create `.archpulse/rules.json` at the repository root:

```json
{
  "forbiddenDependencies": [
    {
      "id": "SHOP-001",
      "from": "Shop.Api",
      "to": "Shop.Domain",
      "severity": "Warning",
      "title": "API must not depend directly on the domain",
      "message": "Use the Shop.Application layer instead."
    }
  ]
}
```

Generate a starter file with:

```bash
archpulse init .
```

### Fields

| Field | Required | Description |
|---|---|---|
| `id` | No | Rule id shown in the report (default `AP100`). |
| `from` | Yes | Source component name pattern. |
| `to` | Yes | Target component name pattern. |
| `severity` | No | `Info`, `Warning`, `Error` (default), or `Critical`. |
| `title` | No | Short finding title. |
| `message` | No | Explanation shown in reports. |

Patterns support `*` (any sequence) and `?` (one character), case-insensitively:

```json
{ "from": "Shop.*", "to": "Shop.Infrastructure", "severity": "Error" }
```

## Adding a built-in rule

1. Add the rule to `RuleCatalog` in `ArchPulse.Core/Rules`.
2. Implement the check in `ArchPulse.Core/Rules/ArchitectureRuleEngine.cs`.
3. Add a unit test with a minimal fixture in `tests/ArchPulse.Core.Tests`.
4. Update this document and the README rule table.
5. If the rule needs a demo, update `samples/ContosoShop` — but keep the sample small.

Rules must be deterministic, explainable, and backed by evidence in the graph. They must not build or execute the target repository.
