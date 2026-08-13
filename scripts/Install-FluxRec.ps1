[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath,
    [string]$PhotonAppId = ""
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path

$requiredPaths = @(
    "RecRoom.exe",
    "GameAssembly.dll",
    "RecRoom_Data",
    "BepInEx\core\BepInEx.Core.dll",
    "BepInEx\core\BepInEx.Preloader.Core.dll",
    "BepInEx\interop\Assembly-CSharp.dll"
)
foreach ($relativePath in $requiredPaths) {
    $candidate = Join-Path $resolvedGamePath $relativePath
    if (-not (Test-Path -LiteralPath $candidate)) {
        throw "Missing required game/runtime path: $candidate"
    }
}

$runtimePath = Join-Path $repositoryRoot "runtime"
New-Item -ItemType Directory -Force -Path $runtimePath | Out-Null
Set-Content -LiteralPath (Join-Path $runtimePath "game-path.txt") -Value $resolvedGamePath -Encoding UTF8

$pythonCommand = Get-Command py -ErrorAction SilentlyContinue
if (-not $pythonCommand) {
    $pythonCommand = Get-Command python -ErrorAction SilentlyContinue
}
if (-not $pythonCommand) {
    throw "Python 3.10 or newer is required."
}

$virtualPython = Join-Path $repositoryRoot ".venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $virtualPython)) {
    if ($pythonCommand.Name -eq "py.exe") {
        & $pythonCommand.Source -3 -m venv (Join-Path $repositoryRoot ".venv")
    } else {
        & $pythonCommand.Source -m venv (Join-Path $repositoryRoot ".venv")
    }
}
& $virtualPython -m pip install --disable-pip-version-check -r (Join-Path $repositoryRoot "server\requirements.txt")

dotnet build (Join-Path $repositoryRoot "plugin\RecNetPlugin.csproj") -c Release "-p:GamePath=$resolvedGamePath" -p:DeployOnBuild=false
if ($LASTEXITCODE -ne 0) { throw "RecNetPlugin build failed." }
dotnet build (Join-Path $repositoryRoot "preloader\PreloaderPatcher.csproj") -c Release "-p:GamePath=$resolvedGamePath" -p:DeployOnBuild=false
if ($LASTEXITCODE -ne 0) { throw "PreloaderPatcher build failed." }

$pluginDestination = Join-Path $resolvedGamePath "BepInEx\plugins"
$patcherDestination = Join-Path $resolvedGamePath "BepInEx\patchers"
New-Item -ItemType Directory -Force -Path $pluginDestination, $patcherDestination | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot "plugin\bin\Release\net6.0\RecNetPlugin.dll") -Destination $pluginDestination -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot "preloader\bin\Release\net6.0\PreloaderPatcher.dll") -Destination $patcherDestination -Force

$configDestination = Join-Path $resolvedGamePath "BepInEx\config\net.rec.plugin.cfg"
if (Test-Path -LiteralPath $configDestination) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    Copy-Item -LiteralPath $configDestination -Destination "$configDestination.backup-$timestamp"
}
$configuration = Get-Content -LiteralPath (Join-Path $repositoryRoot "config\net.rec.plugin.cfg.example") -Raw
if ($PhotonAppId) {
    $configuration = $configuration -replace "(?m)^App Id Realtime =.*$", "App Id Realtime = $PhotonAppId"
    $configuration = $configuration -replace "(?m)^App Id Voice =.*$", "App Id Voice = $PhotonAppId"
    $configuration = $configuration -replace "(?m)^App Id Chat =.*$", "App Id Chat = $PhotonAppId"
}
Set-Content -LiteralPath $configDestination -Value $configuration -Encoding UTF8

Write-Host "Flux Rec installed successfully."
Write-Host "Game: $resolvedGamePath"
Write-Host "Run: $repositoryRoot\Start-FluxRec.bat"

