[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$GamePath)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot "game-build.json") -Raw | ConvertFrom-Json
$failed = $false
foreach ($property in $manifest.files.PSObject.Properties) {
    $relativePath = $property.Name.Replace("/", "\")
    $fullPath = Join-Path $GamePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        Write-Warning "Missing: $relativePath"
        $failed = $true
        continue
    }
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $fullPath).Hash
    if ($actualHash -ne $property.Value.sha256) {
        Write-Warning "Hash mismatch: $relativePath"
        $failed = $true
    } else {
        Write-Host "OK: $relativePath"
    }
}
if ($failed) { exit 1 }

