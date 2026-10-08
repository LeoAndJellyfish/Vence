[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$OutputDirectory = "",

    [string]$CertificatePath = "artifacts\certs\Vence.TestCertificate.pfx",

    [string]$CertificatePassword = "password",

    [switch]$Unsigned,

    [switch]$ForceNewCertificate
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot
$projectPath = Get-VenceAppProjectPath -RepoRoot $repoRoot
$runtimeIdentifier = Get-VenceRuntimeIdentifier -Platform $Platform

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = "artifacts\publish\Vence.App\$Configuration\$Platform-msix"
}

$resolvedOutputDirectory = Resolve-VencePath -RepoRoot $repoRoot -Path $OutputDirectory
New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

Initialize-VenceDotNetEnvironment -RepoRoot $repoRoot

$msbuildArgs = @(
    "publish",
    $projectPath,
    "-m:1",
    "-c", $Configuration,
    "-r", $runtimeIdentifier,
    "-p:Platform=$Platform",
    "-p:WindowsPackageType=MSIX",
    "-p:GenerateAppxPackageOnBuild=true",
    "-p:AppxBundle=Never",
    "-p:UapAppxPackageBuildMode=SideloadOnly",
    "-p:AppxPackageDir=$resolvedOutputDirectory\",
    "-v:minimal"
)

$certificateInfo = $null
if ($Unsigned) {
    $msbuildArgs += "-p:AppxPackageSigningEnabled=false"
}
else {
    $certificateInfo = New-VenceCodeSigningCertificate `
        -RepoRoot $repoRoot `
        -CertificatePath $CertificatePath `
        -Password $CertificatePassword `
        -Force:$ForceNewCertificate

    $msbuildArgs += "-p:AppxPackageSigningEnabled=true"
    $msbuildArgs += "-p:PackageCertificateKeyFile=$($certificateInfo.PfxPath)"
    $msbuildArgs += "-p:PackageCertificatePassword=$CertificatePassword"
}

Write-Host "Publishing Vence.App MSIX ($Configuration, $Platform, $runtimeIdentifier)..."
dotnet @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish MSIX failed with exit code $LASTEXITCODE."
}

$packages = Get-ChildItem -Path $resolvedOutputDirectory -Recurse -File -Include *.msix,*.msixbundle |
    Sort-Object LastWriteTime -Descending

if (-not $packages) {
    throw "MSIX package was not found under: $resolvedOutputDirectory"
}

Write-Host "MSIX output: $($packages[0].FullName)"
if ($certificateInfo) {
    Write-Host "Certificate: $($certificateInfo.CerPath)"
}
