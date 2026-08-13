[CmdletBinding()]
param([string]$GamePath = "")

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $GamePath) { $GamePath = $env:FLUXREC_GAME_PATH }
if (-not $GamePath) {
    $pathFile = Join-Path $repositoryRoot "runtime\game-path.txt"
    if (Test-Path -LiteralPath $pathFile) { $GamePath = (Get-Content -LiteralPath $pathFile -Raw).Trim() }
}
if (-not $GamePath) { throw "Pass -GamePath or run Install-FluxRec.ps1 first." }

$python = Join-Path $repositoryRoot ".venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $python)) { throw "Missing .venv. Run Install-FluxRec.ps1 first." }

& $python -m compileall -q (Join-Path $repositoryRoot "server") (Join-Path $repositoryRoot "tests")
if ($LASTEXITCODE -ne 0) { throw "Python compilation failed." }
& $python (Join-Path $repositoryRoot "tests\smoke_test.py")
if ($LASTEXITCODE -ne 0) { throw "Backend smoke test failed." }

dotnet build (Join-Path $repositoryRoot "plugin\RecNetPlugin.csproj") -c Release "-p:GamePath=$GamePath" -p:DeployOnBuild=false
if ($LASTEXITCODE -ne 0) { throw "Plugin build failed." }
dotnet build (Join-Path $repositoryRoot "preloader\PreloaderPatcher.csproj") -c Release "-p:GamePath=$GamePath" -p:DeployOnBuild=false
if ($LASTEXITCODE -ne 0) { throw "Preloader build failed." }

Write-Host "All Flux Rec validation checks passed."

