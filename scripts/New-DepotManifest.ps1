[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath,
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path
if (-not $OutputPath) {
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $OutputPath = Join-Path $repositoryRoot "depot-file-manifest.csv"
}
$resolvedOutputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null
$temporaryPath = "$OutputPath.tmp"

function Get-Category([string]$RelativePath) {
    if ($RelativePath -match '^(BepInEx|dotnet)\\') { return "third-party-runtime-or-generated" }
    if ($RelativePath -match '^(doorstop_config\.ini|\.doorstop_version|Start-|start-|Launch-|launch-)') { return "local-bootstrap" }
    if ($RelativePath -match '(?i)(\.log$|\.bak($|-)|backup|crash|dump|\.pid$)') { return "local-diagnostic-or-backup" }
    return "proprietary-game-depot"
}

$utf8 = [Text.UTF8Encoding]::new($false)
$writer = [IO.StreamWriter]::new($temporaryPath, $false, $utf8)
try {
    $writer.WriteLine('Path,Size,SHA256,Category')
    $files = Get-ChildItem -LiteralPath $resolvedGamePath -Recurse -File | Sort-Object FullName
    $index = 0
    foreach ($file in $files) {
        $index++
        $relativePath = $file.FullName.Substring($resolvedGamePath.Length + 1).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $category = Get-Category $file.FullName.Substring($resolvedGamePath.Length + 1)
        $escapedPath = '"' + $relativePath.Replace('"', '""') + '"'
        $writer.WriteLine("$escapedPath,$($file.Length),$hash,$category")
        if (($index % 250) -eq 0) { Write-Progress -Activity "Hashing depot" -Status "$index of $($files.Count)" -PercentComplete (($index / $files.Count) * 100) }
    }
} finally {
    $writer.Dispose()
}
Move-Item -LiteralPath $temporaryPath -Destination $OutputPath -Force
Write-Progress -Activity "Hashing depot" -Completed
Write-Host "Wrote depot manifest: $OutputPath"

