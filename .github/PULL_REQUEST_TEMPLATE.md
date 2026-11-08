## What changed

<!-- Describe the change and the problem it solves. -->

## Why

<!-- Explain the architectural or product rationale. Link an issue if relevant. -->

Closes #

## How to test

<!-- Exact commands, URLs, or sample repositories. -->

```bash
dotnet build ArchPulse.slnx
dotnet test ArchPulse.slnx
dotnet run --project src/ArchPulse.Cli -- scan samples/ContosoShop --output artifacts/contoso
```

## Checklist

- [ ] The change is focused and has a clear “why”.
- [ ] New behavior has tests.
- [ ] `dotnet build ArchPulse.slnx` passes.
- [ ] `dotnet test ArchPulse.slnx` passes.
- [ ] Documentation or an ADR is updated if the architecture changed.
- [ ] No secrets, local absolute paths, or generated artifacts are committed.
- [ ] The PR does not introduce a new architecture error (see the ArchPulse diff artifact).

## Screenshots

<!-- For UI changes, include before/after screenshots. -->
