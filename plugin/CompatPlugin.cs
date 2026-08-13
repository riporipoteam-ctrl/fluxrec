using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;

namespace RecNetPlugin;

[BepInPlugin("net.rec.plugin", "RecNet Local Server Compatibility", "1.1.0")]
public sealed class CompatPlugin : BasePlugin
{
    internal static ManualLogSource Log = null!;
    internal static ConfigEntry<string> ServerHostname = null!;
    internal static ConfigEntry<string> AppIdRealtime = null!;
    internal static ConfigEntry<string> AppIdVoice = null!;
    internal static ConfigEntry<string> AppIdChat = null!;
    internal static ConfigEntry<bool> Debug = null!;

    private static Type? FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name, throwOnError: false);
            if (type != null) return type;
        }

        foreach (var assemblyName in new[] { "RecNet.Runtime", "Assembly-CSharp" })
        {
            try
            {
                var type = Assembly.Load(assemblyName).GetType(name, throwOnError: false);
                if (type != null) return type;
            }
            catch { }
        }
        return null;
    }

    public override void Load()
    {
        Log = base.Log;
        ServerHostname = Config.Bind("Server", "RecNet NameServer Host", "http://127.0.0.1:8081", "Complete local RecNet base URL.");
        AppIdRealtime = Config.Bind("Photon", "App Id Realtime", "", "Photon Realtime App ID.");
        AppIdVoice = Config.Bind("Photon", "App Id Voice", "", "Photon Voice App ID.");
        AppIdChat = Config.Bind("Photon", "App Id Chat", "", "Photon Chat App ID.");
        Debug = Config.Bind("Advanced", "Debug", false, "Log redirected requests.");
        Harmony.CreateAndPatchAll(typeof(CompatPlugin).Assembly);
        Log.LogInfo("RecNet local-server compatibility plugin loaded");
    }

    [HarmonyPatch]
    private static class PhotonSettingsPatch
    {
        private static bool Prepare() => FindType("GPFPFDBGCEK") != null;

        private static MethodBase TargetMethod()
        {
            var type = FindType("GPFPFDBGCEK");
            return AccessTools.Method(type, "AMOHMPKKGHL")
                ?? throw new MissingMethodException("Photon settings factory was not found");
        }

        private static void Postfix(object __result)
        {
            if (__result == null) return;
            Set(__result, "AppIdRealtime", AppIdRealtime.Value);
            Set(__result, "AppIdVoice", AppIdVoice.Value);
            Set(__result, "AppIdChat", AppIdChat.Value);
            Set(__result, "FixedRegion", "us");
        }

        private static void Set(object target, string property, object value)
        {
            var member = target.GetType().GetProperty(property);
            if (member?.CanWrite == true) member.SetValue(target, value);
        }
    }

    [HarmonyPatch]
    private static class RecNetRedirectPatch
    {
        private const string OfficialHost = "ns.rec.net";

        private static MethodBase TargetMethod()
        {
            var manager = FindType("BestHTTP.HTTPManager");
            if (manager == null)
                throw new InvalidOperationException("BestHTTP.HTTPManager was not found");

            foreach (var method in manager.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name != "SendRequest")
                    continue;
                return method;
            }

            throw new MissingMethodException("BestHTTP.HTTPManager.SendRequest");
        }

        private static void Prefix(object[] __args)
        {
            if (__args.Length != 1 || __args[0] == null)
                return;

            var request = __args[0];
            var uriProperty = request.GetType().GetProperty("Uri");
            var original = uriProperty?.GetValue(request);
            if (original == null || !string.Equals(original.GetType().GetProperty("Host")?.GetValue(original)?.ToString(), OfficialHost, StringComparison.OrdinalIgnoreCase))
                return;

            var configured = new Uri(ServerHostname.Value);
            var originalUri = new Uri(original.ToString());
            var localUri = new UriBuilder(originalUri)
            {
                Scheme = configured.Scheme,
                Host = configured.Host,
                Port = configured.IsDefaultPort ? -1 : configured.Port,
            }.Uri;
            var gameUri = Activator.CreateInstance(original.GetType(), localUri.ToString());
            uriProperty?.SetValue(request, gameUri);

            if (Debug.Value)
                Log.LogInfo($"[HTTP] redirected {original} -> {localUri}");
        }
    }
}
