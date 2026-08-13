# Flux Rec

Flux Rec is the reproducible source and local-hosting stack for the legacy PC client used by this project. It contains the local RecNet-compatible API, the BepInEx IL2CPP compatibility plugin, the preloader patcher source, Orientation loading integration, configuration templates, and Windows install/start scripts.

## What is in this repository

- `server/` — local FastAPI service for authentication, accounts, saving, rooms, economy, settings, matchmaking, and Photon configuration.
- `plugin/` — the IL2CPP BepInEx plugin that redirects the legacy client and repairs its local Orientation flow.
- `preloader/` — source for the small BepInEx preloader compatibility patcher.
- `config/` — safe templates; credentials and app IDs are intentionally blank.
- `scripts/` — portable Windows installation, build, validation, and launch scripts.
- `server/data/avataritems.json` — the local avatar item catalogue used by the API.
- `protocol/` — reverse-engineering notes used to implement the compatible service.
- `dist/` — verified binaries built from this repository and a project-owned runtime package.
- `depot-file-manifest.csv` — path, size, category, and SHA-256 for all 7,593 files in the development depot.

## What is deliberately not in this repository

This repository does **not** redistribute Rec Room executables, asset bundles, Unity data, IL2CPP metadata, BepInEx binaries, account databases, private keys, certificates, logs, crash dumps, or build output. The original game depot is several gigabytes, contains files above GitHub's 100 MB limit, and is proprietary. Users must legally obtain their own compatible copy. See [docs/LOCAL_GAME_FILES.md](docs/LOCAL_GAME_FILES.md).

## Windows quick start

Requirements:

- Windows 10 or 11
- Python 3.10+
- .NET 6 SDK
- A compatible local game depot with BepInEx IL2CPP already installed and initialized

From PowerShell:

```powershell
.\scripts\Install-FluxRec.ps1 -GamePath "D:\Games\RecRoomLegacy"
Copy-Item .env.example .env
# Edit .env and set a strong OPENREC_SECRET_KEY.
.\Start-FluxRec.bat
```

The installer verifies the expected game files, creates a Python virtual environment, installs backend dependencies, builds both C# projects against local BepInEx/interop assemblies, deploys the two DLLs, and writes a safe plugin configuration. It never copies the proprietary depot into this repository.

Run `scripts\Test-FluxRec.ps1` to compile and smoke-test the backend and build both C# projects without launching the game.

To acquire the compatible depot through Steam entitlement checks, use `scripts\Acquire-CompatibleDepot.ps1`. To compare a local depot with the complete development inventory, use `scripts\Verify-DepotManifest.ps1`.

## Configuration

Server settings live in `.env`; plugin settings live in the game's `BepInEx\config\net.rec.plugin.cfg`. Photon and Firebase identifiers are deployment configuration, not source defaults. Admin access is only granted when `OPENREC_ADMIN_EMAIL` is configured and the matching Firebase email is verified.

This project is an independent compatibility effort and is not affiliated with or endorsed by Rec Room Inc. Do not use it to bypass access controls or distribute software/assets you do not have permission to share.
