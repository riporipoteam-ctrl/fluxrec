[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DepotDownloaderPath,
    [Parameter(Mandatory = $true)]
    [string]$InstallDirectory,
    [string]$Username = ""
)

$ErrorActionPreference = "Stop"
$downloader = (Resolve-Path -LiteralPath $DepotDownloaderPath).Path
if ((Get-Item -LiteralPath $downloader).PSIsContainer) {
    $downloader = Join-Path $downloader "DepotDownloader.exe"
}
if (-not (Test-Path -LiteralPath $downloader -PathType Leaf)) {
    throw "DepotDownloader.exe was not found at: $DepotDownloaderPath"
}
New-Item -ItemType Directory -Force -Path $InstallDirectory | Out-Null

$arguments = @(
    "-app", "471710",
    "-depot", "471711",
    "-manifest", "7859140924515540835",
    "-dir", (Resolve-Path -LiteralPath $InstallDirectory).Path,
    "-validate"
)
if ($Username) {
    $arguments += @("-username", $Username)
    Write-Host "DepotDownloader will request your Steam credentials interactively. They are not saved by this script."
}

& $downloader @arguments
if ($LASTEXITCODE -ne 0) {
    throw "DepotDownloader failed with exit code $LASTEXITCODE. Confirm the Steam account is entitled to the app and that the legacy manifest remains available."
}

Write-Host "Compatible depot acquired at: $InstallDirectory"
Write-Host "Run Verify-DepotManifest.ps1 and then Install-FluxRec.ps1."

