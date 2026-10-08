# ADR: MSIX Packaging Upgrade

Date: 2026-05-31

## Status

Accepted

## Context

Vence originally used an unpackaged self-contained publish directory for private testing. That made early double-click testing simple, but it blurred the line between build output and distributable software. It also did not give the app a real package identity, so it could not exercise installed-app behavior, update behavior, Start menu launch, or future channel separation.

Files-style Dev/Preview/Stable management depends on package identity. Moving to MSIX is therefore the right next step before private testing becomes broader.

## Decision

Vence.App is now an MSIX packaged app:

- `WindowsPackageType` is `MSIX`.
- `WindowsAppSDKSelfContained` remains enabled so the package carries its Windows App SDK runtime payload.
- `scripts\dev.ps1` keeps the daily inner loop through `dotnet run` with package identity.
- `scripts\package-msix.ps1` is the low-level MSIX package entry.
- `scripts\release.ps1` publishes Release MSIX packages by default.
- `scripts\stage-dev.ps1` publishes Debug MSIX packages for install/update testing.
- `scripts\install-msix.ps1` installs the MSIX and can trust the local test certificate for the current user.
- `scripts\package.ps1` remains as an unpackaged self-contained fallback only.

The default Release package path is:

```text
artifacts/publish/Vence.App/Release/x64-msix
```

The local test certificate path is:

```text
artifacts/certs/Vence.TestCertificate.pfx
artifacts/certs/Vence.TestCertificate.cer
```

## Consequences

The project now has a clearer installed-app path: build and test code normally, publish MSIX for handoff, install the package for user-like behavior. This also prepares the repo for separate package identities such as `Vence.App.Dev`, `Vence.App.Preview`, and `Vence.App` if side-by-side release channels become necessary.

The trade-off is operational complexity: MSIX packages need signing certificates. Local development uses a generated test certificate under `artifacts`, while production distribution will need a real trusted certificate or Microsoft Store signing.

## Follow-up

Add explicit Dev/Preview/Stable package identities once update channels matter. At that point, split package name, display name, publisher policy, appinstaller feed, and installer script behavior by channel.
