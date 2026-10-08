[CmdletBinding()]
param(
    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = "artifacts\dev\Vence.App\$Platform-msix"
}

$resolvedOutputDirectory = Resolve-VencePath -RepoRoot $repoRoot -Path $OutputDirectory

& (Join-Path $PSScriptRoot "package-msix.ps1") `
    -Configuration Debug `
    -Platform $Platform `
    -OutputDirectory $resolvedOutputDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Dev MSIX staging failed with exit code $LASTEXITCODE."
}
