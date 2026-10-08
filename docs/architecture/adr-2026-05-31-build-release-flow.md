# ADR: Build and Release Flow

Date: 2026-05-31

## Status

Superseded by [ADR: MSIX Packaging Upgrade](adr-2026-05-31-msix-packaging-upgrade.md).

## Context

Vence is currently a WinUI 3 desktop app using `WindowsPackageType=None` and `WindowsAppSDKSelfContained=true`. This keeps the first private test version easy to copy and run, but the previous workflow mixed several concepts:

- `dotnet build` was sometimes treated as a publish command.
- Debug runs, double-click test builds, and Release distribution shared similar commands.
- The default publish output did not clearly state whether the result was self-contained.
- Files-style Dev/Preview/Stable package identities are useful, but they require a larger MSIX packaging decision.

## Decision

Keep the current unpackaged self-contained deployment model for the MVP, and introduce separate scripts for each workflow:

| Script | Purpose | Default output |
| --- | --- | --- |
| `scripts/dev.ps1` | Run the app for daily Debug development | No publish output |
| `scripts/build.ps1` | Compile the solution | Project `bin/obj` folders |
| `scripts/test.ps1` | Run solution tests | Test results in normal .NET output |
| `scripts/stage-dev.ps1` | Publish a Debug self-contained app for double-click testing | `artifacts/dev/Vence.App/x64` |
| `scripts/release.ps1` | Test and publish the Release self-contained app | `artifacts/publish/Vence.App/Release/x64-selfcontained` |
| `scripts/run-published.ps1` | Start an already published app directory | Existing Dev or Release publish directory |
| `scripts/package.ps1` | Low-level publish primitive used by the higher-level scripts | Configuration/platform-specific self-contained directory |

The visible release executable remains:

```text
artifacts/publish/Vence.App/Release/x64-selfcontained/Vence.App.exe
```

## Consequences

This gives the project a simple rule: use `dev.ps1` while coding, `build.ps1` or `test.ps1` while verifying, `stage-dev.ps1` for a local double-click build, and `release.ps1` for handoff.

The trade-off is that this is not yet a true installed Dev/Preview/Stable package identity model. Adopting the Files approach later should be a separate migration to MSIX identities such as `Vence - Dev`, `Vence - Preview`, and `Vence`, with explicit update channels and install behavior.

## Follow-up

When private testing needs side-by-side installed channels, add an MSIX packaging ADR and split package identity, display name, publisher, certificate, and update channel configuration.
