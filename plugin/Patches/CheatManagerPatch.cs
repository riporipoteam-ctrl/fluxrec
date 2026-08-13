using HarmonyLib;

namespace RecNetPlugin.Patches;

// Replaces the old SceneManager.sceneLoaded event subscription (which failed at runtime because
// the IL2CPP interop doesn't have set_sceneLoaded / op_Implicit). Instead, hooks CheatManager.Start
// directly so we deactivate the GameObject each time a fresh instance is spawned per scene load.
//
// CheatManager boots us out of rooms when it runs, but it's ALSO the DUID service the DI
// container resolves for account creation / login (destroying it removes that service).
// So instead of destroying it, *deactivate* the GameObject: it stops running (no Update /
// coroutines, so no boot) while the component still exists, so the DI container can still
// resolve PGECJHKNIEN and call its DUID methods. It's recreated per scene, so deactivate
// each freshly-spawned (active) instance on every load.
[HarmonyPatch(typeof(CheatManager), "Start")]
public static class CheatManagerPatch
{
    private static bool _corruptDone;

    [HarmonyPrefix]
    private static bool OnStart(CheatManager __instance)
    {
        var go = __instance.gameObject;

        if (Plugin.CorruptStoredDUID.Value && !_corruptDone)
            _corruptDone = CorruptDUIDPatch.CorruptStored(go);
        else if (Plugin.RestoreStoredDUID.Value && !_corruptDone)
            _corruptDone = CorruptDUIDPatch.RestoreStored(go);

        go.SetActive(false);
        Plugin.Log.LogInfo("cheatmanager deactivated");

        return false;
    }
}
