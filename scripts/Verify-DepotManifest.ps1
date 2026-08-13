[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath,
    [string]$ManifestPath = ""
)

$ErrorActionPreference = "Stop"
$resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path
if (-not $ManifestPath) {
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $ManifestPath = Join-Path $repositoryRoot "depot-file-manifest.csv"
}
$rows = Import-Csv -LiteralPath $ManifestPath
$failures = [Collections.Generic.List[string]]::new()
$index = 0
foreach ($row in $rows) {
    $index++
    $relativePath = $row.Path.Replace('/', '\')
    $fullPath = Join-Path $resolvedGamePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $failures.Add("MISSING $($row.Path)")
        continue
    }
    $file = Get-Item -LiteralPath $fullPath
    if ([string]$file.Length -ne [string]$row.Size) {
        $failures.Add("SIZE $($row.Path)")
        continue
    }
    $hash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -ne $row.SHA256) { $failures.Add("HASH $($row.Path)") }
    if (($index % 250) -eq 0) { Write-Progress -Activity "Verifying depot" -Status "$index of $($rows.Count)" -PercentComplete (($index / $rows.Count) * 100) }
}
Write-Progress -Activity "Verifying depot" -Completed
if ($failures.Count) {
    $failures | Select-Object -First 100 | Write-Error
    throw "Depot verification failed for $($failures.Count) files."
}
Write-Host "Verified $($rows.Count) depot files."

