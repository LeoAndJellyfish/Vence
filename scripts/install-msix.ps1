[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$PackagePath = "",

    [string]$CertificatePath = "artifacts\certs\Vence.TestCertificate.cer",

    [switch]$TrustCertificate
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $packageDirectory = Resolve-VencePath `
        -RepoRoot $repoRoot `
        -Path "artifacts\publish\Vence.App\$Configuration\$Platform-msix"

    $package = Get-ChildItem -Path $packageDirectory -Recurse -File -Include *.msix,*.msixbundle |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $package) {
        throw "No MSIX package was found under: $packageDirectory"
    }

    $PackagePath = $package.FullName
}
else {
    $PackagePath = Resolve-VencePath -RepoRoot $repoRoot -Path $PackagePath
}

if ($TrustCertificate) {
    $resolvedCertificatePath = Resolve-VencePath -RepoRoot $repoRoot -Path $CertificatePath
    if (-not (Test-Path -LiteralPath $resolvedCertificatePath)) {
        throw "Certificate was not found: $resolvedCertificatePath"
    }

    Write-Host "Trusting certificate for current user: $resolvedCertificatePath"
    Import-Certificate -FilePath $resolvedCertificatePath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    Import-Certificate -FilePath $resolvedCertificatePath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
}

Write-Host "Installing MSIX package: $PackagePath"
Add-AppxPackage -Path $PackagePath -ForceUpdateFromAnyVersion
