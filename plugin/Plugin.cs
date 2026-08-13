using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using RecNetPlugin.Patches;


namespace RecNetPlugin;

[BepInPlugin("net.rec.plugin", "RecNet Plugin", "1.0.0")]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public static ConfigEntry<string> AppIdRT { get; private set; }
    public static ConfigEntry<string> AppIdVoice { get; private set; }
    public static ConfigEntry<string> AppIdChat { get; private set; }
    public static ConfigEntry<string> ServerHostname { get; private set; }
    public static ConfigEntry<bool> EnableAdvancedSettings { get; private set; }
    public static ConfigEntry<string> PhotonHostname { get; private set; }
    public static ConfigEntry<int> PhotonPort { get; private set; }
    public static ConfigEntry<bool> Debug { get; private set; }
    public static ConfigEntry<bool> SimulateDUIDMismatch { get; private set; }
    public static ConfigEntry<bool> SuppressDUIDMismatch { get; private set; }
    public static ConfigEntry<bool> CorruptStoredDUID { get; private set; }
    public static ConfigEntry<bool> RestoreStoredDUID { get; private set; }
    public static ConfigEntry<string> DeviceIdResponseOverride { get; private set; }
    public static ConfigEntry<int> DeviceIdResponseStatus { get; private set; }
    public static ConfigEntry<bool> DisableSignatureVerification { get; private set; }
    public static ConfigEntry<string> SigningModulusOverride { get; private set; }
    public static ConfigEntry<bool> BypassStockInitialSceneLoad { get; private set; }
    public static ConfigEntry<bool> SkipStockPostLoadState { get; private set; }
    public static ConfigEntry<bool> LogSceneDiagnostics { get; private set; }
    public static ConfigEntry<bool> ForceLocalRoomSceneLoad { get; private set; }
    public static ConfigEntry<bool> DirectOrientationSceneLoad { get; private set; }
    public static ConfigEntry<string> OrientationSceneName { get; private set; }
    public static ConfigEntry<string> OrientationAdditiveSceneName { get; private set; }
    public static ConfigEntry<float> DirectSceneLoadDelaySeconds { get; private set; }
    public static ConfigEntry<bool> SuppressPreferenceExceptions { get; private set; }
    public static ConfigEntry<string> NameserverGateMode { get; private set; }
    public static ConfigEntry<bool> InstallServiceMap { get; private set; }
    public static ConfigEntry<bool> UseOfflineRoomTravel { get; private set; }
    public static ConfigEntry<bool> UseBootLocalPlayerToRoom { get; private set; }
    public static ConfigEntry<bool> PhotonOfflineMode { get; private set; }
    public static ConfigEntry<float> BigDataGateSeconds { get; private set; }
    public static ConfigEntry<bool> StartPlayerSpawn { get; private set; }
    public static ConfigEntry<bool> ShowLoadingScreen { get; private set; }
    public static ConfigEntry<string> LoadingScreenLabel { get; private set; }
    public static ConfigEntry<bool> AutoCreateAccountAndLaunch { get; private set; }
    public static ConfigEntry<float> OfflineMouseLookSensitivity { get; private set; }
    public static ConfigEntry<float> OfflineCameraEyeForwardOffset { get; private set; }

    public override void Load()
    {
        Log = base.Log;

        AppIdRT = Config.Bind("Photon", "App Id Realtime", "", "Photon Realtime App ID");
        AppIdVoice = Config.Bind("Photon", "App Id Voice", "", "Photon Voice App ID");
        AppIdChat = Config.Bind("Photon", "App Id Chat", "", "Photon Chat App ID");
        EnableAdvancedSettings = Config.Bind("Advanced", "Enabled Advanced Settings", false, "Allows other fields below in the advanced section to be modified.");
        PhotonHostname = Config.Bind("Advanced", "Photon NameServer", "", "Custom Photon NameServer");
        PhotonPort = Config.Bind("Advanced", "Photon NameServer Port", 0, "Custom Photon NameServer Port (if 0, it will be default)");
        ServerHostname = Config.Bind("Server", "RecNet NameServer Host", "https://ns.rec.net", "Host for the RecNet NameServer.");
        Debug = Config.Bind("Advanced", "Debug", false, "Show debug logs (HTTP tracing, etc. WARNING: will include sensitive information such as passwords and auth tokens in the logs, be careful when sharing them!)");
        SimulateDUIDMismatch = Config.Bind("Advanced", "Simulate DUID Mismatch", false, "Force CheckForDUIDMismatch to return TRUE (fakes the comparison only). Reproduces the hang path but does not corrupt any stored value. Leave false for normal play.");
        SuppressDUIDMismatch = Config.Bind("Advanced", "Suppress DUID Mismatch", true, "Force CheckForDUIDMismatch to return FALSE (the workaround fix, ON by default): the client never migrates and never takes the Create Account hang path. No-op on healthy machines (the real check returns false anyway); on mismatched machines it skips the hang. Set false only to observe the real mismatch behavior for debugging.");
        CorruptStoredDUID = Config.Bind("Advanced", "Corrupt Stored DUID", false, "ONE-SHOT TEST: on next launch, write a truncated device id into the DUID pref via the game's own WriteDUIDs, producing a genuinely corrupt STORED value (real current id) — exactly the friend's condition. After it logs '[CORRUPT] wrote', set this back to false and relaunch to drive the real mismatch path. Use 'Restore Stored DUID' to undo.");
        RestoreStoredDUID = Config.Bind("Advanced", "Restore Stored DUID", false, "ONE-SHOT UNDO: on next launch, call WriteDUIDs with the real device id, overwriting any corrupt stored value with a good one. Set back to false after it logs '[CORRUPT] restored'.");
        DeviceIdResponseOverride = Config.Bind("Advanced", "DeviceId Response Override", "", "Replace the body of the PlayerReporting/v1/deviceId response with this text, to test what shape the client will accept. Empty = leave the server's response alone.");
        DeviceIdResponseStatus = Config.Bind("Advanced", "DeviceId Response Status", 200, "HTTP status to force on the PlayerReporting/v1/deviceId response. Only applies when the override body is set.");

        DisableSignatureVerification = Config.Bind("Signing", "Disable Signature Verification", true, "Force RSA signature verification to succeed (ON by default), so the client stops checking that images are signed with Rec Room's private key. This is what lets a self-hosted server serve its own images without the baked-in modulus matching. Set false only if you actually want signed images, in which case use 'Signing Modulus Override' instead. NOTE: this forces ALL mscorlib RSA verification to pass, not just image signatures.");
        SigningModulusOverride = Config.Bind("Signing", "Signing Modulus Override", "", "Optional alternative to disabling verification: your own RSA public modulus, base64, RAW 2048-bit (256 bytes decoded) — NOT a PEM/DER key. When set, it is substituted for the modulus baked into global-metadata.dat and real verification still runs, so images stay signed with your keypair. Redundant while 'Disable Signature Verification' is true. Empty = leave the stock modulus alone.");

        BypassStockInitialSceneLoad = Config.Bind("Orientation", "Bypass Stock Initial Scene Load", true, "Replace BootSequence.FALKOHHOCKF with a locally-settled promise instead of letting the stock initial-scene load run. This was needed when the native matchmaking state was still not EXCLUSIVELY_LOGGED_IN at that point; it also suppresses the real scene load. Set false to let the stock loader run.");
        SkipStockPostLoadState = Config.Bind("Orientation", "Skip Stock Post Load State", true, "Skip BootSequence.MBNNOLJEJJP (POST_LOAD_INITIAL_SCENE / state 101) and only set IsBootSequenceReadyForSceneChanges. Set false to let the stock state run.");
        LogSceneDiagnostics = Config.Bind("Orientation", "Log Scene Diagnostics", false, "Log the active Unity scene, RecRoomSceneManager state, and BootSequence state once a second after the launch handoff. Diagnostic only.");
        ForceLocalRoomSceneLoad = Config.Bind("Orientation", "Force Local Room Scene Load", true, "After the local Orientation matchmaking promise settles, call SessionManager.LocalPlayerRequestJoinRoomScene to start the real room-scene load. The replaced stock FALKOHHOCKF was what normally started it, so without this the boot sequence finishes with TitleScreen still loaded.");

        DirectOrientationSceneLoad = Config.Bind("Orientation", "Direct Orientation Scene Load", true, "If the room loader leaves TitleScreen active after the launch handoff, load the bundled Orientation scene directly through Unity. This is the fallback that actually clears the white screen while the native RecNet connection is still broken.");
        OrientationSceneName = Config.Bind("Orientation", "Orientation Scene Name", "Orientation_additive", "Bootstrap scene loaded first by the direct fallback. Despite its historical name, this depot's Orientation_additive scene owns RecRoomSceneManager, CommonSceneSystems, and OrientationManager, so it must be the Single/base scene.");
        OrientationAdditiveSceneName = Config.Bind("Orientation", "Orientation Additive Scene Name", "Orientation_Scene1", "Tutorial level scene loaded additively only after the Orientation bootstrap scene has created RecRoomSceneManager and SceneSpawnManager. Orientation_Scene1 owns the welcome geometry and spawn points.");
        UseOfflineRoomTravel = Config.Bind("Orientation", "Use Offline Room Travel", true, "Call RecNet.Matchmaking.EHAJFDHHBCF(offlineRoom) directly - the real 'travel to a bundled offline room' entry point. FALKOHHOCKF calls it internally, and because the plugin replaces FALKOHHOCKF wholesale it has never actually run here. It owns the room load and the player spawn. Falls back to the direct scene load if it does nothing.");
        UseBootLocalPlayerToRoom = Config.Bind("Orientation", "Use Boot Local Player To Room", true, "Call SessionManager.BootLocalPlayerToDormRoom instead of loading the scene raw. This is the game's own offline room-entry path and it owns the player spawn, which a bare SceneManager.LoadScene cannot do. Falls back to the direct scene load if it is unavailable or throws.");
        PhotonOfflineMode = Config.Bind("Photon", "Photon Offline Mode", true, "Force ServerSettings.StartInOfflineMode. There is no Photon session to join here - the client only logs PhotonHandler.Awake then Disconnect - and the local player is spawned through a PhotonView, so without a session there is no player. Offline mode runs instantiation and RPCs locally. Turn off if you ever get real Photon multiplayer working.");
        BigDataGateSeconds = Config.Bind("Orientation", "Big Data Gate Seconds", 1.5f, "How long the spawn may sit at WaitingForBigData before the room big-data retrieval is cancelled. Orientation ships its content in the scene and has no big-data payload, so with no backend serving one the retrieval never completes and the spawn stalls at 100%. Set high to disable the override.");
        StartPlayerSpawn = Config.Bind("Orientation", "Start Player Spawn", true, "Call SceneSpawnManager.CLMOOCHEOHN once the room scenes are up. That is the local player spawn state machine; nothing else starts it here, so LocalPlayerSpawnState stays Uninitialized, no player exists and Camera.main is null - which is why the view is an empty default camera.");
        ShowLoadingScreen = Config.Bind("Orientation", "Show Loading Screen", true, "Raise the game's own LoadingScreen during the Orientation handoff, with a destination label and a progress bar. The stock room loader normally does this; it never runs here, which is why the transition had nothing on screen.");
        LoadingScreenLabel = Config.Bind("Orientation", "Loading Screen Label", "^Orientation", "Destination text shown on the loading screen. SetLabel runs the value through the LoadingScreen localization table, so a bare word logs 'Could not find an entry with key'. The game's own strings use a '^' prefix for literal text (\"Going to ^DormRoom\"), so keep the caret. Empty skips the label entirely.");
        OfflineMouseLookSensitivity = Config.Bind("Orientation", "Offline Mouse Look Sensitivity", 5.5f, "Relative mouse/touchpad look sensitivity for the offline desktop Orientation controller. Higher values turn faster.");
        OfflineCameraEyeForwardOffset = Config.Bind("Orientation", "Offline Camera Eye Forward Offset", 0.18f, "How far in metres the desktop first-person camera sits in front of the real avatar head pivot. This keeps the view at the eyes instead of inside the head.");
        DirectSceneLoadDelaySeconds = Config.Bind("Orientation", "Direct Scene Load Delay Seconds", 1.5f, "How long to let the stock room loader try before the direct fallback takes over. Lower = faster Orientation entry.");
        InstallServiceMap = Config.Bind("Server", "Install Service Map", true, "Fill HEEMOONFCAF's BPIFHBEBGHO->Uri dictionary directly after the nameserver response is handled. The stock parse leaves it empty on this depot, so every service lookup throws KeyNotFound and the client never issues a single RecNet request - which is what breaks the loading screen, the room load and the player spawn.");
        NameserverGateMode = Config.Bind("Server", "Nameserver Gate Mode", "passthrough", "How to treat HEEMOONFCAF.CNCONLNMEIA, the check between the nameserver response arriving and the bootstrap branching to success or failure. 'passthrough' runs the game's own logic (default). 'force-true' was the old behaviour and appears to pin the bootstrap on the failure branch, leaving the service map empty. 'force-false' forces the success branch.");
        SuppressPreferenceExceptions = Config.Bind("Orientation", "Suppress Preference Exceptions", true, "Return the caller's default instead of throwing when the player-preference store is uninitialized. The store never initializes while the native RecNet connection is down, and the thrown exception otherwise kills the Orientation scene's Awake chain.");
        AutoCreateAccountAndLaunch = Config.Bind("Server", "Auto Create Account And Launch", true, "When true (default), auto-fills signup through Welcome. Does NOT auto-press Let's Play — you click that so the Welcome page is visible. Set RECNET_VALIDATE_ACCOUNT_LAUNCH=1 only for fully unattended CI.");

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);
        SendRequestPatch.InstallExplicitPatches(new Harmony("net.rec.plugin.explicit-http"));
        Log.LogInfo($"RecNet Plugin patches applied; local endpoint={ServerHostname.Value}");
    }
}
