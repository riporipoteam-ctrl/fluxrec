using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace RecNetPlugin;

/// <summary>
/// Minimal DontDestroyOnLoad Update host for offline WASD/cursor.
/// Heavy work is forbidden here — previous crashes came from FindObjectsOfTypeAll
/// and stock avatar APIs inside the tick, not from ClassInjector itself.
/// </summary>
public class OrientationGameplayDriver : MonoBehaviour
{
    public OrientationGameplayDriver(IntPtr ptr) : base(ptr)
    {
    }

    private static bool _registered;
    private static bool _installed;
    private static Texture2D _circleTexture;
    private static GUIStyle _promptStyle;
    private static GUIStyle _coachStyle;

    public static void EnsureInstalled()
    {
        if (_installed)
            return;
        try
        {
            if (!_registered)
            {
                ClassInjector.RegisterTypeInIl2Cpp<OrientationGameplayDriver>();
                _registered = true;
            }

            var go = new GameObject("FluxRec_OrientationGameplayDriver");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<OrientationGameplayDriver>();
            // The retired desktop build otherwise renders uncapped during the
            // offline room path, saturating a laptop CPU/GPU and making mouse
            // look feel delayed even when the scene itself is healthy.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            _installed = true;
            Plugin.Log?.LogWarning(
                "[GAMEPLAY] installed minimal Orientation Update driver (60 FPS cap)");
        }
        catch (Exception e)
        {
            Plugin.Log?.LogError(
                "[GAMEPLAY] driver install failed: " +
                e.GetBaseException().Message);
        }
    }

    private void Update()
    {
        try
        {
            Patches.SendRequestPatch.OrientationGameplayTick();
        }
        catch
        {
            // never kill the process
        }
    }

    private void OnGUI()
    {
        try
        {
            if (!Patches.SendRequestPatch.ShouldDrawOrientationReticle())
                return;

            EnsureGuiResources();
            var oldColor = GUI.color;

            // Rec Room's screen reticle is a compact circular point, not the
            // square four-pixel placeholder used by the emergency controller.
            var outer = 9f;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(
                (Screen.width - outer) * 0.5f,
                (Screen.height - outer) * 0.5f,
                outer, outer), _circleTexture);
            var inner = 5f;
            GUI.color = new Color(1f, 1f, 1f, 0.96f);
            GUI.DrawTexture(new Rect(
                (Screen.width - inner) * 0.5f,
                (Screen.height - inner) * 0.5f,
                inner, inner), _circleTexture);

            if (Patches.SendRequestPatch.TryGetOrientationDoorScreenRect(
                    out var doorRect))
            {
                var pulse = 0.78f +
                            Mathf.Sin(Time.unscaledTime * 5f) * 0.18f;
                GUI.color = new Color(1f, 0.25f, 0.02f, pulse);
                const float thickness = 7f;
                GUI.DrawTexture(new Rect(
                    doorRect.x, doorRect.y, doorRect.width, thickness),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(
                    doorRect.x, doorRect.yMax - thickness,
                    doorRect.width, thickness), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(
                    doorRect.x, doorRect.y, thickness, doorRect.height),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(
                    doorRect.xMax - thickness, doorRect.y,
                    thickness, doorRect.height), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            var coach = Patches.SendRequestPatch.GetOrientationCoachPrompt();
            if (!string.IsNullOrEmpty(coach))
            {
                var width = Mathf.Min(760f, Screen.width - 60f);
                GUI.Box(new Rect(
                    (Screen.width - width) * 0.5f,
                    Screen.height * 0.12f,
                    width, 68f), coach, _coachStyle);
            }

            var interaction =
                Patches.SendRequestPatch.GetOrientationInteractionPrompt();
            if (!string.IsNullOrEmpty(interaction))
            {
                var width = Mathf.Min(560f, Screen.width - 50f);
                GUI.Box(new Rect(
                    (Screen.width - width) * 0.5f,
                    Screen.height * 0.72f,
                    width, 64f), interaction, _promptStyle);
            }

            GUI.color = oldColor;
        }
        catch
        {
            // Rendering a reticle must never interrupt the game loop.
        }
    }

    private static void EnsureGuiResources()
    {
        if (_circleTexture == null)
        {
            _circleTexture = new Texture2D(
                16, 16, TextureFormat.RGBA32, false);
            _circleTexture.name = "FluxRec_StockStyleReticle";
            _circleTexture.wrapMode = TextureWrapMode.Clamp;
            _circleTexture.filterMode = FilterMode.Bilinear;
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var dx = x - 7.5f;
                    var dy = y - 7.5f;
                    var alpha = Mathf.Clamp01(8f - Mathf.Sqrt(dx * dx + dy * dy));
                    _circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            _circleTexture.Apply(false, true);
        }

        if (_promptStyle == null)
        {
            _promptStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 34, 22, 36),
                fontStyle = FontStyle.Bold,
            };
            _promptStyle.normal.textColor = Color.white;
        }

        if (_coachStyle == null)
        {
            _coachStyle = new GUIStyle(_promptStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 38, 20, 32),
            };
        }
    }
}
