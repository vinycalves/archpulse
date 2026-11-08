# Sample repository

`ContosoShop` is a tiny, intentionally flawed .NET Aspire application used by the
ArchPulse demo and tests.

The domain project references the infrastructure project, which violates the
built-in `AP002` layer rule. This gives the scanner something meaningful to report
without requiring you to break your own repository.

Scan it with:

```bash
dotnet run --project src/ArchPulse.Cli -- scan samples/ContosoShop --output artifacts/contoso
```

Open `artifacts/contoso/report.md` to see the findings.
