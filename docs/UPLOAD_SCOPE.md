# Repository upload scope

This repository contains every recoverable, project-owned Flux Rec source file and the verified binaries built from that source. It does not contain third-party or proprietary game/runtime content, local account state, or secrets.

## Included

- Complete Flux Rec BepInEx plugin source and compiled DLL.
- Complete recovered preloader source and compiled DLL.
- Complete local FastAPI hosting service source.
- Authentication, account, saving, room, matchmaking, economy, social, settings, and Firebase integration code.
- Avatar item catalogue used by the local API.
- Windows installer, launcher, validation scripts, configuration templates, protocol notes, and licenses.
- A distributable ZIP containing the project-owned runtime files.
- `depot-file-manifest.csv`, which identifies every file in the development depot by relative path, size, category, and SHA-256 hash.

## Not uploaded as file content

- Rec Room executables, Unity data, asset bundles, IL2CPP metadata, audio, textures, scenes, and extracted artwork.
- BepInEx, .NET, Steam, Photon, or other third-party binary distributions.
- Private keys, certificates, Firebase credentials, JWT secrets, player databases, authentication tokens, logs, crash dumps, caches, and backups.

Those files cannot become redistributable merely by placing them in a public or private repository. Several original depot files also exceed GitHub's 100 MB per-file limit. Use `scripts/Verify-DepotManifest.ps1` to verify a legally obtained local copy against the committed inventory.

