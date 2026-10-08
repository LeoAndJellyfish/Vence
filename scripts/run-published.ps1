[CmdletBinding()]
param(
    [ValidateSet("MSIX", "Unpackaged")]
    [string]$Deployment = "MSIX",

    [ValidateSet("Dev", "Release")]
    [string]$Channel = "Release",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$AppDirectory = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot

if ($Deployment -eq "MSIX") {
    $manifestPath = Get-VencePackageManifestPath -RepoRoot $repoRoot
    [xml]$manifest = Get-Content -Path $manifestPath
    $packageName = $manifest.Package.Identity.Name
    $package = Get-AppxPackage -Name $packageName |
        Sort-Object Version -Descending |
        Select-Object -First 1

    if (-not $package) {
        throw "MSIX package is not installed: $packageName"
    }

    $appUserModelId = "$($package.PackageFamilyName)!App"
    Write-Host "Starting installed MSIX app: $appUserModelId"
    Start-Process "shell:AppsFolder\$appUserModelId"
    return
}

if ([string]::IsNullOrWhiteSpace($AppDirectory)) {
    if ($Channel -eq "Dev") {
        $AppDirectory = "artifacts\dev\Vence.App\$Platform"
    }
    else {
        $AppDirectory = "artifacts\publish\Vence.App\Release\$Platform-selfcontained"
    }
}

$resolvedAppDirectory = Resolve-VencePath -RepoRoot $repoRoot -Path $AppDirectory
$exePath = Join-Path $resolvedAppDirectory "Vence.App.exe"

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Vence.App.exe was not found at: $exePath"
}

Write-Host "Starting $exePath"
Start-Process -FilePath $exePath -WorkingDirectory $resolvedAppDirectory
