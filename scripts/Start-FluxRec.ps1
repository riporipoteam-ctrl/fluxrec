[CmdletBinding()]
param(
    [string]$GamePath = "",
    [switch]$ServerOnly
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Import-DotEnv([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#") -or -not $trimmed.Contains("=")) { continue }
        $parts = $trimmed.Split("=", 2)
        [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
    }
}

Import-DotEnv (Join-Path $repositoryRoot ".env")
if (-not $GamePath) { $GamePath = $env:FLUXREC_GAME_PATH }
if (-not $GamePath) {
    $pathFile = Join-Path $repositoryRoot "runtime\game-path.txt"
    if (Test-Path -LiteralPath $pathFile) { $GamePath = (Get-Content -LiteralPath $pathFile -Raw).Trim() }
}

$virtualPython = Join-Path $repositoryRoot ".venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $virtualPython)) {
    throw "Flux Rec is not installed. Run scripts\Install-FluxRec.ps1 first."
}

$runtimePath = Join-Path $repositoryRoot "runtime"
New-Item -ItemType Directory -Force -Path $runtimePath | Out-Null
if (-not $env:OPENREC_DATABASE_URL) { $env:OPENREC_DATABASE_URL = "sqlite:///./runtime/fluxrec.db" }
if (-not $env:OPENREC_PUBLIC_BASE_URL) { $env:OPENREC_PUBLIC_BASE_URL = "http://127.0.0.1:8081" }
if ($env:OPENREC_REQUEST_LOG -eq "0") { $env:OPENREC_REQUEST_LOG = "" }
if (-not $env:OPENREC_SECRET_KEY) {
    $secretFile = Join-Path $runtimePath "jwt-secret.txt"
    if (-not (Test-Path -LiteralPath $secretFile)) {
        $randomBytes = New-Object byte[] 48
        [Security.Cryptography.RandomNumberGenerator]::Fill($randomBytes)
        Set-Content -LiteralPath $secretFile -Value ([Convert]::ToBase64String($randomBytes)) -Encoding ASCII
    }
    $env:OPENREC_SECRET_KEY = (Get-Content -LiteralPath $secretFile -Raw).Trim()
}

$serverProcess = Start-Process -FilePath $virtualPython -ArgumentList @(
    "-m", "uvicorn", "server.main:app", "--host", "127.0.0.1", "--port", "8081"
) -WorkingDirectory $repositoryRoot -WindowStyle Hidden -PassThru

try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 75; $attempt++) {
        if ($serverProcess.HasExited) { throw "The Flux Rec server exited with code $($serverProcess.ExitCode)." }
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:8081/api/versioncheck/v4" -TimeoutSec 1
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 200 }
    }
    if (-not $ready) { throw "The Flux Rec server did not become ready on port 8081." }

    Write-Host "Flux Rec server is ready at http://127.0.0.1:8081"
    if ($ServerOnly) {
        Wait-Process -Id $serverProcess.Id
        return
    }

    if (-not $GamePath) { throw "GamePath is not configured. Run the installer or pass -GamePath." }
    $gameExecutable = Join-Path $GamePath "RecRoom.exe"
    if (-not (Test-Path -LiteralPath $gameExecutable)) { throw "Missing game executable: $gameExecutable" }

    $gameProcess = Start-Process -FilePath $gameExecutable -WorkingDirectory $GamePath -PassThru
    Wait-Process -Id $gameProcess.Id
} finally {
    if ($serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id
        $serverProcess.WaitForExit(5000) | Out-Null
    }
}
