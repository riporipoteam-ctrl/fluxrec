using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Realtime;

namespace RecNetPlugin.Patches;

/**
    Patches Photon to use the App IDs and server hostname/port specified in the plugin config.
 */
// Obfuscated names shift every game build. Re-resolve by signature: the target is the only
// instance, 0-param method returning Photon.Realtime.AppSettings in Assembly-CSharp.
// 20230414 build: HPEENKELKDJ.MGKINLFMJLB (was LEALBOODIEE.GBNKOFMAJPA, was GPFPFDBGCEK.AMOHMPKKGHL).
[HarmonyPatch(typeof(GPFPFDBGCEK), "AMOHMPKKGHL")]
public class PhotonPatches
{
    private static bool _offlineModeApplied;

    // Orientation is a bundled offline room and there is no Photon session to
    // join: the client only ever logs PhotonHandler.Awake followed by
    // Disconnect. PUN's offline mode makes instantiation and RPCs run locally,
    // which is what SceneSpawnManager needs to spawn the local player at all
    // (RpcSpawnNewPlayer takes a PhotonView). Applied here because this runs
    // while Photon settings are being read, before any connect attempt.
    private static void ApplyOfflineMode()
    {
        if (_offlineModeApplied || !Plugin.PhotonOfflineMode.Value)
            return;
        _offlineModeApplied = true;

        try
        {
            var all = UnityEngine.Resources.FindObjectsOfTypeAll(
                Il2CppInterop.Runtime.Il2CppType.Of<Photon.Pun.ServerSettings>());
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogWarning(
                    "[PHOTON] no ServerSettings asset found; cannot force offline mode");
                _offlineModeApplied = false;
                return;
            }

            var applied = 0;
            for (var i = 0; i < all.Length; i++)
            {
                var settings = all[i]?.TryCast<Photon.Pun.ServerSettings>();
                if (settings == null)
                    continue;
                settings.StartInOfflineMode = true;
                applied++;
            }

            Plugin.Log.LogWarning(
                $"[PHOTON] StartInOfflineMode forced true on {applied} ServerSettings asset(s)");
        }
        catch (System.Exception e)
        {
            _offlineModeApplied = false;
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[PHOTON] could not force offline mode: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    [HarmonyPostfix]
    private static void Postfix(ref AppSettings __result)
    {
        ApplyOfflineMode();

        if (__result != null)
        {
            __result.AppIdRealtime = Plugin.AppIdRT.Value;
            __result.AppIdVoice = Plugin.AppIdVoice.Value;
            __result.AppIdChat = Plugin.AppIdChat.Value;
            __result.FixedRegion = "us";
            __result.UseNameServer = true;
            __result.Protocol = ConnectionProtocol.Udp;

            if (Plugin.EnableAdvancedSettings.Value)
            {
                __result.Server = Plugin.PhotonHostname.Value;
                __result.Port = Plugin.PhotonPort.Value == 0
                    ? 4533
                    : Plugin.PhotonPort.Value;
            }
        }
    }
}
