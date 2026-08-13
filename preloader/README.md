# Preloader patcher

This source was reconstructed from the deployed 1.0.0 compatibility patcher because the original source directory was missing. It restores delegate/event members needed by the generated IL2CPP interop surface. The hardcoded development-machine resolver path from the old binary was removed; BepInEx resolves its own dependencies from `BepInEx/core`.

Build with:

```powershell
dotnet build PreloaderPatcher.csproj -c Release -p:GamePath="D:\Games\RecRoomLegacy"
```

