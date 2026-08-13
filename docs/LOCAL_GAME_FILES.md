# Local game files

The client depot is intentionally external to Git. Obtain it through an authorized source. The project was developed against Steam app `471710`, depot `471711`, manifest `7859140924515540835` (legacy build). Availability and entitlement are controlled by Steam.

If you already have DepotDownloader, `scripts/Acquire-CompatibleDepot.ps1` supplies those identifiers and still relies on Steam to enforce ownership. The script does not contain or upload Steam credentials.

The installer expects these local paths:

- `RecRoom.exe`
- `GameAssembly.dll`
- `RecRoom_Data/`
- `BepInEx/core/`
- `BepInEx/interop/`

The hashes in `game-build.json` identify the build used during development. A mismatch is treated as a warning because rebuilding against a different IL2CPP API can compile incorrectly or fail at runtime.

Never commit the depot, generated interop assemblies, player databases, logs, TLS keys, Firebase service-account JSON, or BepInEx runtime binaries.
