# Flux Rec IL2CPP plugin

Build against a compatible local game with generated BepInEx interop assemblies:

```powershell
dotnet build RecNetPlugin.csproj -c Release -p:GamePath="D:\Games\RecRoomLegacy" -p:DeployOnBuild=false
```

If you have permission to use a custom Orientation image, place it at `Assets/Loading/Activity_Image_Orientation.png`; it is embedded when present. Otherwise the plugin first looks for the texture in the locally installed game and falls back to the game's default loading picture. No game DLL, artwork, or generated interop assembly is committed. `GamePath.props.example` can be copied to the ignored `GamePath.props` for local development.

`CompatPlugin.csproj` is retained for diagnostics and older bootstrap compatibility; normal installation deploys `RecNetPlugin.csproj`.
