[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot
$projectPath = Join-Path $repoRoot "src\Vence.App\Vence.App.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\publish\Vence.App\$Configuration\$Platform-selfcontained"
}
else {
    $OutputDirectory = Resolve-VencePath -RepoRoot $repoRoot -Path $OutputDirectory
}

$runtimeIdentifier = Get-VenceRuntimeIdentifier -Platform $Platform

Initialize-VenceDotNetEnvironment -RepoRoot $repoRoot

Write-Host "Restoring Vence.App..."
dotnet restore $projectPath `
    -m:1 `
    -p:Platform=$Platform `
    -r $runtimeIdentifier
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

Write-Host "Publishing Vence.App ($Configuration, $Platform, $runtimeIdentifier)..."
dotnet publish $projectPath `
    -m:1 `
    --no-restore `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:WindowsPackageType=None `
    -p:AppxPackage=false `
    -r $runtimeIdentifier `
    --self-contained true `
    -o $OutputDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Publish output: $OutputDirectory"
