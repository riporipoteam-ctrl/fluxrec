using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BestHTTP;
using CodeStage.AntiCheat.ObscuredTypes;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace RecNetPlugin.Patches;

/**
    Intercept a variety of HTTP requests and rewrite them to point to our own custom server.
 */
public class SendRequestPatch
{
    // Explicit registration is used as a fallback for IL2CPP-generated BestHTTP
    // methods. Some generated overloads do not get discovered reliably by
    // Harmony's assembly-wide attribute scan.
    public static void InstallExplicitPatches(Harmony harmony)
    {
        PatchExplicit(harmony, typeof(HTTPManager), "SendRequest", new[] { typeof(HTTPRequest) }, nameof(ExplicitRequestPrefix));
        PatchExplicit(harmony, typeof(HTTPManager), "SendRequestImpl", new[] { typeof(HTTPRequest) }, nameof(ExplicitImplPrefix));
        PatchExplicit(harmony, typeof(HTTPRequest), "Send", Type.EmptyTypes, nameof(ExplicitSendPrefix));
        PatchExplicit(harmony, typeof(BestHTTP.Forms.HTTPFormBase), "AddField", new[] { typeof(string), typeof(string) }, nameof(FormAddFieldPrefix));
        PatchExplicit(harmony, typeof(UnityWebRequest), "SendWebRequest", Type.EmptyTypes, nameof(UnityWebRequestSendPrefix));
        PatchExplicit(harmony, typeof(UnityWebRequest), "BeginWebRequest", Type.EmptyTypes, nameof(UnityWebRequestSendPrefix));
        PatchExplicit(harmony, typeof(UnityWebRequest), "Send", Type.EmptyTypes, nameof(UnityWebRequestSendPrefix));
        PatchExplicit(harmony, typeof(Org.BouncyCastle.Crypto.Tls.LegacyTlsAuthentication), "NotifyServerCertificate", new[] { typeof(BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls.Certificate) }, nameof(ExplicitCertificatePrefix));
        PatchExplicit(
            harmony,
            typeof(CPHOFHOLPMI),
            "IPJLLJKJCNK",
            new[] { typeof(string) },
            nameof(StatsigGatePrefix));
        PatchExplicit(
            harmony,
            typeof(JJHDHDGGDHG),
            "MDPMJNHBEAD",
            new[] { typeof(string) },
            nameof(RecRoomStatsigGatePrefix));
        PatchExplicit(
            harmony,
            typeof(JJHDHDGGDHG),
            "FFKAFKHMHHB",
            new[] { typeof(string) },
            nameof(RecRoomStatsigExperimentPrefix));
        PatchExplicit(
            harmony,
            typeof(JJHDHDGGDHG),
            "IAAEGCOAIHC",
            new[] { typeof(string) },
            nameof(RecRoomStatsigExperimentPrefix));
        PatchExplicit(
            harmony,
            typeof(JJHDHDGGDHG),
            "BEHGIFKMMHI",
            new[] { typeof(string) },
            nameof(RecRoomStatsigLayerPrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.Avatars.Data.Runtime.AvatarItem),
            "ODBNPIJENBP",
            Type.EmptyTypes,
            nameof(AvatarItemResourceKeyPrefix));
        PatchExplicitPostfix(harmony, typeof(HIBHFHKEMCJ), "GFPLJAAAHOE", Type.EmptyTypes, nameof(NameserverStatusPostfix));
        // Desktop player/backpack prefabs request the global culling service
        // while Photon is instantiating them. That service is not registered in
        // the direct bundled-room path, and the resulting null dereference
        // interrupts Player.SpawnLocal before it can enable the camera/input.
        // The client ships MMDAJOCGCAI specifically as the no-op implementation
        // of PHNOFPCAHIJ, so use it for this offline Orientation bootstrap.
        PatchExplicit(
            harmony,
            typeof(EMKCEIODBCE),
            "AOOKGCFEGKB",
            new[] { typeof(int) },
            nameof(DesktopCullingGroupPrefix));
        // The depot's URP pipeline asset does not expose its serialized Rec Room
        // quality block when booted outside the production bootstrap. Every
        // provider getter then dereferences null; PUNNetworkManager.Initialize
        // is the fatal call that prevents Orientation from ever starting.
        PatchExplicit(
            harmony,
            typeof(UnityEngine.Rendering.Universal.UrpRecRoomQualityConfigProvider),
            "get_UrpConfig",
            Type.EmptyTypes,
            nameof(OfflineUrpQualityConfigPrefix));
        var urpQualityProvider =
            typeof(UnityEngine.Rendering.Universal.UrpRecRoomQualityConfigProvider);
        foreach (var getter in new[]
                 {
                     "get_SupportsNormalMappingUGC",
                     "get_SupportsMacroMaterialsUGC",
                     "get_SupportsHighQualitySkyboxes",
                     "get_SupportsDepthSampling",
                     "get_SupportsAlphaClipping",
                     "get_ClothSimulationEnabled",
                     "get_OptimizedUIRaycasts",
                 })
            PatchExplicit(harmony, urpQualityProvider, getter, Type.EmptyTypes,
                nameof(OfflineUrpQualityTruePrefix));
        foreach (var getter in new[]
                 {
                     "get_LimitNumberOfShadows",
                     "get_EnableAdditionalFogClipPlanes",
                     "get_ResamplePhotonAudioToCatchUp",
                 })
            PatchExplicit(harmony, urpQualityProvider, getter, Type.EmptyTypes,
                nameof(OfflineUrpQualityFalsePrefix));
        foreach (var getter in new[]
                 {
                     "get_PlayerPuppetRenderFramerate",
                     "get_ShareCameraPreviewRenderFramerate",
                 })
            PatchExplicit(harmony, urpQualityProvider, getter, Type.EmptyTypes,
                nameof(OfflineUrpQualityThirtyPrefix));
        foreach (var getter in new[]
                 {
                     "get_MaximumParallelLoadRequests",
                     "get_ImposterRenderingUpdateRate",
                 })
            PatchExplicit(harmony, urpQualityProvider, getter, Type.EmptyTypes,
                nameof(OfflineUrpQualityFourPrefix));
        foreach (var getter in new[]
                 {
                     "get_MaximumModelRefreshPerFrame",
                     "get_MaximumControllerRefreshPerFrame",
                 })
            PatchExplicit(harmony, urpQualityProvider, getter, Type.EmptyTypes,
                nameof(OfflineUrpQualityEightPrefix));
        PatchExplicit(harmony, urpQualityProvider,
            "get_MaximumSimultaneousPooledParticleSystems", Type.EmptyTypes,
            nameof(OfflineUrpQualityParticleBudgetPrefix));
        PatchExplicit(harmony, urpQualityProvider,
            "get_MaximumImposterRendersPerUpdate", Type.EmptyTypes,
            nameof(OfflineUrpQualityTwoPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_TransparencyDetail", Type.EmptyTypes,
            nameof(OfflineUrpTransparencyPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_SceneDecorationDetail", Type.EmptyTypes,
            nameof(OfflineUrpSceneDecorationPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_LODScalingQuality", Type.EmptyTypes,
            nameof(OfflineUrpLodScalingPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_ParticleQuality", Type.EmptyTypes,
            nameof(OfflineUrpParticleQualityPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_TerrainQuality", Type.EmptyTypes,
            nameof(OfflineUrpTerrainQualityPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_UpdateThrottling", Type.EmptyTypes,
            nameof(OfflineUrpUpdateThrottlingPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_BackgroundAnimationDetail", Type.EmptyTypes,
            nameof(OfflineUrpBackgroundAnimationPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_CreatorToolVisualFidelity", Type.EmptyTypes,
            nameof(OfflineUrpCreatorFidelityPrefix));
        PatchExplicit(harmony, urpQualityProvider, "get_WeaponDetailQuality", Type.EmptyTypes,
            nameof(OfflineUrpWeaponQualityPrefix));
        PatchExplicit(harmony, urpQualityProvider,
            "get_ShareCameraPreviewRenderResolution", Type.EmptyTypes,
            nameof(OfflineUrpShareCameraResolutionPrefix));
        PatchExplicit(harmony, urpQualityProvider,
            "get_CustomAvatarItemTextureResolution", Type.EmptyTypes,
            nameof(OfflineUrpAvatarTextureResolutionPrefix));
        // The only tick guaranteed alive while the travel UI is up and after the
        // room scene replaces the title scene. TitleScreenManager.Update dies
        // with the scene we are leaving, so it cannot retire the loading screen.
        PatchExplicitPostfix(harmony, typeof(LoadingScreen), "Update", Type.EmptyTypes, nameof(LoadingScreenUpdatePostfix));
        // Primary post-load gameplay tick: the game's coroutine pump runs every
        // frame even after LoadingScreen.Update stops (set_IsVisible kills it).
        PatchGameLoopTick(harmony);
        // Unity calls the static FireOnPreCull(Camera) bridge immediately
        // before each camera renders. This build does not expose a zero-arg
        // instance FireOnPreCull, so patch its real signature explicitly.
        try
        {
            var fireOnPreCull = AccessTools.Method(
                typeof(UnityEngine.Camera),
                "FireOnPreCull",
                new[] { typeof(UnityEngine.Camera) });
            if (fireOnPreCull != null)
            {
                harmony.Patch(
                    fireOnPreCull,
                    postfix: new HarmonyMethod(
                        typeof(SendRequestPatch),
                        nameof(CameraPreCullPostfix)));
                Plugin.Log.LogWarning(
                    "[HTTP] patched Camera.FireOnPreCull(Camera) for final avatar/camera alignment");
            }
            else
            {
                Plugin.Log.LogWarning("[HTTP] Camera.FireOnPreCull(Camera) not found; live gameplay tick remains active");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[HTTP] Camera tick patch failed: " + e.GetBaseException().Message);
        }
        // The bundled offline Orientation has no big-data payload and no
        // backend serves one, so the stock retrieval task never completes and
        // the spawn parks at WaitingForBigData forever. Returning a completed
        // Il2Cpp Task lets the stock state machine advance on its own. (Calling
        // BaseBigDataNetworkingManager.Cancel used to be the unblock, but the
        // game crashes inside the native cancel machinery - the retrieval's
        // task continuations run against a broken HTTP path.)
        //
        // CRITICAL: the method returns Il2CppSystem.Threading.Tasks.Task, NOT
        // System.Threading.Tasks.Task. Using the managed Task type for __result
        // makes Harmony throw at load and aborts every subsequent explicit
        // patch (boot sequence, orientation, nameserver, account flow, ...).
        PatchExplicit(
            harmony,
            typeof(RecRoom.Core.Creation.BaseBigDataNetworkingManager),
            "RunAllDataRetrieval",
            null,
            nameof(BigDataRetrievalPrefix));
        PatchExplicit(
            harmony,
            typeof(UnityEngine.Cursor),
            "set_visible",
            new[] { typeof(bool) },
            nameof(OrientationCursorVisiblePrefix));
        PatchExplicit(
            harmony,
            typeof(UnityEngine.Cursor),
            "set_lockState",
            new[] { typeof(UnityEngine.CursorLockMode) },
            nameof(OrientationCursorLockStatePrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.Core.ScreenHUD),
            "EnableCursor",
            new[] { typeof(bool), typeof(Il2CppSystem.Object) },
            nameof(OfflineScreenHudEnableCursorPrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.Core.ScreenHUD),
            "get_IsCursorEnabled",
            Type.EmptyTypes,
            nameof(OfflineScreenHudIsCursorEnabledPrefix));
        // Player.Awake can fail late in the retired offline bootstrap after the
        // real Photon Player already exists.  The shipped Orientation tutorial,
        // portals and voice-over all query these static properties, so expose
        // that live local instance while the offline Orientation fallback owns
        // the spawn instead of leaving the room in a permanent "no player"
        // state.
        PatchExplicit(
            harmony,
            typeof(Player),
            "get_LocalPlayer",
            Type.EmptyTypes,
            nameof(OfflineOrientationLocalPlayerPrefix));
        PatchExplicit(
            harmony,
            typeof(Player),
            "get_LocalPlayerExists",
            Type.EmptyTypes,
            nameof(OfflineOrientationLocalPlayerExistsPrefix));
        PatchExplicit(
            harmony,
            typeof(Player),
            "get_LocalPlayerIsSpawnedAndNotFading",
            Type.EmptyTypes,
            nameof(OfflineOrientationLocalPlayerReadyPrefix));
        PatchExplicit(
            harmony,
            typeof(Player),
            "get_ControllerDisplayMode",
            Type.EmptyTypes,
            nameof(OfflineOrientationControllerDisplayModePrefix));
        PatchExplicit(
            harmony,
            typeof(Player),
            "get_DeveloperDisplayMode",
            Type.EmptyTypes,
            nameof(OfflineOrientationDeveloperDisplayModePrefix));
        PatchExplicit(
            harmony,
            typeof(Player),
            "Update",
            Type.EmptyTypes,
            nameof(OfflineIncompletePlayerUpdatePrefix));
        PatchExplicit(
            harmony,
            typeof(PlayerNameTag),
            "LateUpdate",
            Type.EmptyTypes,
            nameof(OfflineIncompletePlayerUpdatePrefix));
        PatchExplicit(
            harmony,
            typeof(PlayerNameTag),
            "UpdatePosition",
            Type.EmptyTypes,
            nameof(OfflineIncompletePlayerUpdatePrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.Players.Watch.WatchMenuProjector),
            "Update",
            Type.EmptyTypes,
            nameof(OfflineIncompletePlayerUpdatePrefix));
        PatchExplicit(harmony, typeof(HEEMOONFCAF), "CNCONLNMEIA", new[] { typeof(HIBHFHKEMCJ), typeof(bool) }, nameof(NameserverValidationPrefix));
        PatchExplicit(
            harmony,
            typeof(HEEMOONFCAF),
            "CABDIKHOLFK",
            new[] { typeof(MPKGFLPHDAJ), typeof(HIBHFHKEMCJ), typeof(HAAHJPGNIMD) },
            nameof(NameserverSuccessPrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(HEEMOONFCAF),
            "CABDIKHOLFK",
            new[] { typeof(MPKGFLPHDAJ), typeof(HIBHFHKEMCJ), typeof(HAAHJPGNIMD) },
            nameof(NameserverSuccessPostfix));
        PatchExplicit(
            harmony,
            typeof(HEEMOONFCAF),
            "HPGGFJHCJKF",
            new[] { typeof(MPKGFLPHDAJ), typeof(HIBHFHKEMCJ), typeof(HAAHJPGNIMD), typeof(string) },
            nameof(NameserverFailurePrefix));
        var titleStart = AccessTools.Method(typeof(TitleScreenManager), "Start", Type.EmptyTypes);
        if (titleStart != null)
            harmony.Patch(titleStart, postfix: new HarmonyMethod(typeof(SendRequestPatch), nameof(TitleStartPostfix)));
        var titleUpdate = AccessTools.Method(typeof(TitleScreenManager), "Update", Type.EmptyTypes);
        if (titleUpdate != null)
            harmony.Patch(titleUpdate, postfix: new HarmonyMethod(typeof(SendRequestPatch), nameof(TitleUpdatePostfix)));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "CreateNewAccountThenGoToStartAccountCreationFlow", Type.EmptyTypes, nameof(AccountCreationPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "CreateNewAccountThenGoToStartAccountCreationFlow", Type.EmptyTypes, nameof(AccountCreationPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "ManuallyLogin", Type.EmptyTypes, nameof(ManualLoginPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "ManuallyLogin", Type.EmptyTypes, nameof(ManualLoginPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitEmail", Type.EmptyTypes, nameof(SubmitEmailPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitEmail", Type.EmptyTypes, nameof(SubmitEmailPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAccountCreationBirthdayAndGoToNext", Type.EmptyTypes, nameof(BirthdaySubmitPrefix));
        // Next stays greyed-out while stock validity is false (no date / prefs
        // store down). Force valid so the button is clickable, then our submit
        // prefix actually advances off BIRTHDAY.
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModel),
            "get_AccountCreationBirthdayIsValid",
            Type.EmptyTypes,
            nameof(AccountCreationBirthdayIsValidPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.SubmitAccountCreationBirthdayAndGoToNextButtonImpl),
            "LMHIDFNEMEK",
            Type.EmptyTypes,
            nameof(BirthdayNextButtonPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAvatarCustomizationAndGoToNext", Type.EmptyTypes, nameof(AvatarSubmitPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAvatarCustomizationAndGoToNext", Type.EmptyTypes, nameof(AvatarSubmitPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAccountCreationUsernameAndGoToNext", Type.EmptyTypes, nameof(UsernameSubmitPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAccountCreationPasswordAndGoToNext", Type.EmptyTypes, nameof(PasswordSubmitPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAccountCreationConsolidatedInfoAndGoToNext", Type.EmptyTypes, nameof(ConsolidatedSubmitPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "SubmitAccountCreationConsolidatedInfoAndGoToNext", Type.EmptyTypes, nameof(ConsolidatedSubmitPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "AcceptCodeOfConductAndGoToNext", Type.EmptyTypes, nameof(AcceptCodeOfConductPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "LaunchGameAccountCreation", Type.EmptyTypes, nameof(LaunchGameAccountCreationPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "LaunchGameAccountCreation", Type.EmptyTypes, nameof(LaunchGameAccountCreationPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "LaunchGameCachedAccount", Type.EmptyTypes, nameof(LaunchGameCachedAccountPrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "LaunchGameCachedAccount", Type.EmptyTypes, nameof(LaunchGameCachedAccountPostfix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "FDPAPOCOHJJ", new[] { typeof(bool) }, nameof(TitleLaunchPipelinePrefix));
        PatchExplicitPostfix(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "FDPAPOCOHJJ", new[] { typeof(bool) }, nameof(TitleLaunchPipelinePostfix));
        PatchExplicit(harmony, typeof(BootSequence), "LaunchGame", new[] { typeof(BootSequence.DJHPHOBJLHM) }, nameof(BootSequenceLaunchPrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.RoomCalibration),
            "get_RequiresCalibration",
            Type.EmptyTypes,
            nameof(RoomCalibrationRequiresCalibrationPrefix));
        PatchExplicit(
            harmony,
            typeof(BootSequence),
            "JMDNAEAAEAH",
            new[] { typeof(ushort), typeof(ushort) },
            nameof(BootCalibrationStatePrefix));
        PatchExplicit(
            harmony,
            typeof(BootSequence),
            "FEBEIECHHNP",
            new[] { typeof(ushort), typeof(ushort) },
            nameof(BootPostLoginInitializationStatePrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(BootSequence),
            "FEBEIECHHNP",
            new[] { typeof(ushort), typeof(ushort) },
            nameof(BootPostLoginInitializationStatePostfix));
        PatchExplicit(
            harmony,
            typeof(BootSequence),
            "EHJDBFFHNAK",
            new[] { typeof(ushort), typeof(ushort) },
            nameof(BootLoadInitialSceneStatePrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(BootSequence),
            "EHJDBFFHNAK",
            new[] { typeof(ushort), typeof(ushort) },
            nameof(BootLoadInitialSceneStatePostfix));
        PatchExplicit(
            harmony,
            typeof(BootSequence),
            "FALKOHHOCKF",
            Type.EmptyTypes,
            nameof(BootLoadInitialScenePromisePrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(BootSequence),
            "INCOCGBNDAJ",
            Type.EmptyTypes,
            nameof(BootInitialSceneSuccessPostfix));
        PatchExplicit(
            harmony,
            typeof(BootSequence),
            "MBNNOLJEJJP",
            new[] { typeof(ushort), typeof(ushort) },
            nameof(BootPostLoadInitialSceneStatePrefix));
        PatchExplicit(
            harmony,
            typeof(TutorialManager),
            "get_HasCompletedNUXTutorial",
            Type.EmptyTypes,
            nameof(HasCompletedNuxTutorialPrefix));
        PatchExplicit(
            harmony,
            typeof(TutorialManager),
            "get_HasCompletedOrientation",
            Type.EmptyTypes,
            nameof(HasCompletedOrientationPrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.Activities.Orientation.OrientationManager),
            "get_IsReturningPlayer",
            Type.EmptyTypes,
            nameof(OrientationIsReturningPlayerPrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(TutorialManager),
            "set_HasCompletedOrientation",
            new[] { typeof(bool) },
            nameof(HasCompletedOrientationPostfix));
        PatchExplicit(
            harmony,
            typeof(NBDIDJMANNH),
            "DEFDBPCLIJB",
            Type.EmptyTypes,
            nameof(PlayerPreferencesInitializationGuardPrefix));
        PatchExplicit(
            harmony,
            typeof(NBDIDJMANNH),
            "LAIIDCONNEJ",
            new[] { typeof(string), typeof(string) },
            nameof(PreferenceStringGetPrefix));
        PatchExplicit(
            harmony,
            typeof(NBDIDJMANNH),
            "PKJLGHKECEI",
            new[] { typeof(string) },
            nameof(PreferenceKeyExistsPrefix));
        PatchExplicit(
            harmony,
            typeof(AvatarUpdateLODSystem),
            "Awake",
            Type.EmptyTypes,
            nameof(OfflineOrientationAvatarLodAwakePrefix));
        PatchExplicit(
            harmony,
            typeof(AvatarUpdateLODSystem),
            "UpdateAvatarLODs",
            new[] { typeof(MGNDJKECDKI), typeof(MGNDJKECDKI) },
            nameof(OfflineOrientationAvatarLodUpdatePrefix));
        // Player.Awake is where desktop camera/input attach. The offline path
        // is missing several DI services; OfflineOrientationPlayerAwakePrefix
        // repairs them before stock Awake runs. Without this registration the
        // repair methods exist but never fire, Camera.main stays null, and the
        // loading screen sits at 100% forever after SpawnedAndFadedIn.
        var playerAwake = AccessTools.Method(typeof(Player), "Awake", Type.EmptyTypes);
        if (playerAwake != null)
        {
            try
            {
                harmony.Patch(
                    playerAwake,
                    prefix: new HarmonyMethod(
                        typeof(SendRequestPatch),
                        nameof(OfflineOrientationPlayerAwakePrefix)),
                    finalizer: new HarmonyMethod(
                        typeof(SendRequestPatch),
                        nameof(OfflineOrientationPlayerAwakeFinalizer)));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(
                    $"[HTTP] failed to patch Player.Awake: " +
                    $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
            }
        }
        else
        {
            Plugin.Log.LogWarning("[HTTP] explicit target not found: Player.Awake");
        }

        // ScreenPlayerController.IBKAKBCOICD is the per-frame desktop controller
        // tick. After a partial offline bind it NRE's every frame (ScreenHUD /
        // stock camera missing) and floods the log + freezes input. Skip the
        // stock tick while our offline FP locomotion owns the player.
        PatchScreenPlayerControllerTicks(harmony);
        PatchExplicit(
            harmony,
            typeof(PlayerAvatar),
            "Awake",
            Type.EmptyTypes,
            nameof(OfflineOrientationPlayerAvatarAwakePrefix));
        PatchExplicit(
            harmony,
            typeof(RecRoom.Core.PlayerToolEquipSlots),
            "Awake",
            Type.EmptyTypes,
            nameof(OfflineOrientationToolEquipSlotsAwakePrefix));
        PatchExplicit(
            harmony,
            typeof(NPOMLPCGIBH),
            "KJIAAPJEONB",
            Type.EmptyTypes,
            nameof(OfflineOrientationJoinedRoomLabelPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "RandomizeAccountCreationUsername", Type.EmptyTypes, nameof(RandomizeAccountCreationUsernamePrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "get_AccountCreationUsernameValidity", Type.EmptyTypes, nameof(AccountCreationUsernameValidityPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "get_AccountCreationPasswordValidity", Type.EmptyTypes, nameof(AccountCreationPasswordValidityPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "get_EmailIsValid", Type.EmptyTypes, nameof(AccountCreationEmailValidityPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "get_ShouldHideAccountCreationEmail", Type.EmptyTypes, nameof(ShouldHideAccountCreationEmailPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "get_ShouldHideAccountCreationPhone", Type.EmptyTypes, nameof(ShouldHideAccountCreationPhonePrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.TitleScreenFlowModel), "get_AccountCreationConsolidatedInfoIsValid", Type.EmptyTypes, nameof(AccountCreationConsolidatedInfoIsValidPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.AccountCreationUsernameInputFieldImpl),
            "HGJKOFEAFNB",
            new[] { typeof(string) },
            nameof(AccountCreationUsernameInputChangedPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.AccountCreationUsernameInputFieldImpl),
            "JFJMCPFEHLH",
            Type.EmptyTypes,
            nameof(AccountCreationUsernameInputRefreshPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.AccountCreationPasswordInputFieldImpl),
            "HGJKOFEAFNB",
            new[] { typeof(string) },
            nameof(AccountCreationPasswordInputChangedPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.AccountCreationPasswordInputFieldImpl),
            "JFJMCPFEHLH",
            Type.EmptyTypes,
            nameof(AccountCreationPasswordInputRefreshPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.EmailInputFieldImpl),
            "HGJKOFEAFNB",
            new[] { typeof(string) },
            nameof(AccountCreationEmailInputChangedPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.EmailInputFieldImpl),
            "JFJMCPFEHLH",
            Type.EmptyTypes,
            nameof(AccountCreationEmailInputRefreshPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.AccountCreationPhoneInputFieldImpl),
            "HGJKOFEAFNB",
            new[] { typeof(string) },
            nameof(AccountCreationPhoneInputChangedPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.HideIfNoAccountCreationPhoneImpl),
            "JFJMCPFEHLH",
            Type.EmptyTypes,
            nameof(HideAccountCreationPhonePrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.RandomizeAccountCreationUsernameButtonImpl),
            "JFJMCPFEHLH",
            Type.EmptyTypes,
            nameof(AccountCreationButtonRefreshPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.RandomizeAccountCreationUsernameButtonImpl),
            "LMHIDFNEMEK",
            Type.EmptyTypes,
            nameof(RandomizeAccountCreationUsernameButtonPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.SubmitAccountCreationConsolidatedInfoAndGoToNextButtonImpl),
            "JFJMCPFEHLH",
            Type.EmptyTypes,
            nameof(AccountCreationButtonRefreshPrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.TitleScreenFlowModelController.SubmitAccountCreationConsolidatedInfoAndGoToNextButtonImpl),
            "LMHIDFNEMEK",
            Type.EmptyTypes,
            nameof(SubmitAccountCreationConsolidatedInfoButtonPrefix));
        PatchExplicit(harmony, typeof(RRUI.Data.AvatarCustomizationModel), "HKFNNLOIDEM", Type.EmptyTypes, nameof(AvatarModelInitializePrefix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.AnimatedPlayerPuppetAvatarModel),
            "HKFNNLOIDEM",
            Type.EmptyTypes,
            nameof(AnimatedAvatarModelInitializePrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(RRUI.Data.AnimatedPlayerPuppetAvatarModel),
            "HKFNNLOIDEM",
            Type.EmptyTypes,
            nameof(AnimatedAvatarModelInitializedPostfix));
        PatchExplicitPostfix(
            harmony,
            typeof(RecRoom.Avatars.Outfit.OutfitManager),
            "Start",
            Type.EmptyTypes,
            nameof(OutfitManagerStartPostfix));
        PatchExplicit(
            harmony,
            typeof(RRUI.Data.AvatarCustomizationItemListModel),
            "KGEDHDOFEPN",
            new[] { typeof(Il2CppSystem.Collections.Generic.IEnumerable<string>) },
            nameof(AvatarItemListInitializePrefix));
        PatchExplicitPostfix(
            harmony,
            typeof(RRUI.Data.AvatarCustomizationItemListModel),
            "KGEDHDOFEPN",
            new[] { typeof(Il2CppSystem.Collections.Generic.IEnumerable<string>) },
            nameof(AvatarItemListInitializedPostfix));
        PatchExplicit(harmony, typeof(HEEMOONFCAF.HBDGOANBFLN), "_ConnectionRequestCallback_b__0", new[] { typeof(MPKGFLPHDAJ), typeof(HIBHFHKEMCJ) }, nameof(NameserverCallbackPrefix));
        // This lambda is where BestHTTP's completed request is converted into
        // RecNet's own response record. RecNet ends up with status 0 and a null
        // body even though the transport saw a 200, so watch the conversion
        // inputs directly.
        PatchExplicit(
            harmony,
            typeof(CBJNKDIHCKK.GJBKHNDJJLO),
            "_Send_b__0",
            new[] { typeof(HTTPRequest), typeof(HTTPResponse) },
            nameof(RecNetResponseConversionPrefix));
    }

    public static void RecNetResponseConversionPrefix(
        HTTPRequest __0,
        HTTPResponse __1)
    {
        if (_responseConversionLogCount >= 10)
            return;
        _responseConversionLogCount++;

        try
        {
            var uri = __0?.Uri?.ToString() ?? "<null>";
            var state = __0 == null ? "<null>" : __0.State.ToString();
            var detail = __1 == null
                ? "response=<null>"
                : $"status={__1.StatusCode} success={__1.IsSuccess} " +
                  $"dataLen={(__1.Data == null ? -1 : __1.Data.Length)}";
            Plugin.Log.LogWarning($"[RECNET-CONV] uri={uri} state={state} {detail}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[RECNET-CONV] inspect failed: {e.GetBaseException().GetType().Name}");
        }
    }

    public static bool BigDataRetrievalPrefix(ref Il2CppSystem.Threading.Tasks.Task __result)
    {
        // Must be the IL2CPP Task type. System.Threading.Tasks.Task breaks
        // Harmony load with "Cannot assign method return type Il2CppSystem...Task
        // to __result type System.Threading.Tasks.Task" and aborts every patch
        // registered after this one.
        __result = Il2CppSystem.Threading.Tasks.Task.CompletedTask;
        if (!_bigDataRetrievalShortCircuitLogged)
        {
            _bigDataRetrievalShortCircuitLogged = true;
            Plugin.Log.LogWarning(
                "[BIGDATA] short-circuited the stock room big-data retrieval " +
                "with a completed task (bundled Orientation has no payload)");
        }
        return false;
    }

    private static void PatchExplicit(Harmony harmony, Type type, string methodName, Type[] parameters, string prefixName)
    {
        var target = AccessTools.Method(type, methodName, parameters);
        if (target == null)
        {
            Plugin.Log.LogWarning($"[HTTP] explicit target not found: {type.FullName}.{methodName}");
            return;
        }

        try
        {
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(SendRequestPatch), prefixName));
        }
        catch (Exception e)
        {
            // One bad patch used to kill the entire InstallExplicitPatches
            // chain, leaving Orientation with no boot/camera/nameserver hooks.
            Plugin.Log.LogError(
                $"[HTTP] failed to patch {type.FullName}.{methodName} " +
                $"via {prefixName}: {e.GetBaseException().GetType().Name}: " +
                $"{e.GetBaseException().Message}");
        }
    }

    private static Harmony _gameLoopHarmony;
    private static bool _gameLoopPatched;

    private static void PatchGameLoopTick(Harmony harmony)
    {
        // Types may not be loaded yet at plugin Load — also retry later.
        _gameLoopHarmony = harmony;
        TryPatchGameLoopTickNow();
    }

    private static void TryPatchGameLoopTickNow()
    {
        if (_gameLoopPatched || _gameLoopHarmony == null)
            return;

        try
        {
            Type type = null;
            // Search all loaded assemblies — TypeByName fails at cold start.
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = ass.GetType("BEDELEOJJKI", false);
                    if (type != null)
                        break;
                    foreach (var t in ass.GetTypes())
                    {
                        if (t != null && t.Name == "BEDELEOJJKI")
                        {
                            type = t;
                            break;
                        }
                    }
                    if (type != null)
                        break;
                }
                catch
                {
                    // Dynamic assemblies can throw on GetTypes().
                }
            }

            if (type == null)
                type = AccessTools.TypeByName("BEDELEOJJKI");

            if (type == null)
                return;

            var method = AccessTools.Method(type, "LOGJNOLNCGE", new[] { typeof(float) })
                         ?? AccessTools.Method(type, "LOGJNOLNCGE", Type.EmptyTypes);
            if (method == null)
                return;

            _gameLoopHarmony.Patch(
                method,
                postfix: new HarmonyMethod(
                    typeof(SendRequestPatch),
                    nameof(GameLoopTickPostfix)));
            _gameLoopPatched = true;
            Plugin.Log.LogWarning(
                "[HTTP] patched BEDELEOJJKI.LOGJNOLNCGE as permanent gameplay tick");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[HTTP] game-loop tick patch failed: " + e.GetBaseException().Message);
        }
    }

    public static void GameLoopTickPostfix()
    {
        // Ultra-light: only after Orientation spawn is live.
        if (!Plugin.DirectOrientationSceneLoad.Value)
            return;
        if (!_offlineLocomotionReady && !_localPlayerSpawnSucceededLogged)
            return;
        OrientationGameplayTick();
    }

    private static int _lastGameplayFrame = -1;

    public static void CameraPreCullPostfix(UnityEngine.Camera __0)
    {
        CameraTickPostfix(__0);
    }

    public static void CameraTickPostfix(UnityEngine.Camera __instance)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value)
            return;
        if (!_offlineLocomotionReady && !_localPlayerSpawnSucceededLogged)
            return;
        if (__instance == null || __instance.Pointer == IntPtr.Zero)
            return;

        // ONLY our FluxRec camera — never touch other cameras' render paths.
        try
        {
            var n = __instance.gameObject?.name ?? string.Empty;
            if (n.IndexOf("FluxRec_PlayerCamera", StringComparison.OrdinalIgnoreCase) < 0)
                return;
        }
        catch { return; }

        // Let the shared once-per-frame tick update player/camera state first,
        // then do only the final render-time eye/head alignment here.
        OrientationGameplayTick();
        try { AlignRealAvatarHeadAndEyeCamera(__instance); }
        catch { /* never break rendering */ }
    }

    private static void PatchScreenPlayerControllerTicks(Harmony harmony)
    {
        // Managed interop type may be missing; resolve by name.
        Type spcType = null;
        foreach (var name in new[]
                 {
                     "RecRoom.ScreenPlayerController",
                     "ScreenPlayerController",
                 })
        {
            spcType = AccessTools.TypeByName(name);
            if (spcType != null)
                break;
        }

        if (spcType == null)
        {
            Plugin.Log.LogWarning(
                "[HTTP] ScreenPlayerController type not found; cannot suppress offline ticks");
            return;
        }

        // Per-frame / lifecycle methods that NRE without full desktop setup.
        foreach (var methodName in new[]
                 {
                     "IBKAKBCOICD", // continuous update — main flood source
                     "HIEJDAFLAGL",
                     "LCHLGIBOMKD",
                     "HIBFBNNHKJL",
                     "KCOJOCFHFPP",
                     "GBHBBGNFJEL",
                 })
        {
            try
            {
                var method = AccessTools.Method(spcType, methodName, Type.EmptyTypes);
                if (method == null)
                    continue;
                harmony.Patch(
                    method,
                    prefix: new HarmonyMethod(
                        typeof(SendRequestPatch),
                        nameof(OfflineScreenPlayerTickPrefix)));
                Plugin.Log.LogInfo(
                    $"[HTTP] patched {spcType.Name}.{methodName} for offline skip");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    $"[HTTP] could not patch ScreenPlayerController.{methodName}: " +
                    e.GetBaseException().Message);
            }
        }
    }

    /// <summary>
    /// Skip stock ScreenPlayerController ticks while offline Orientation uses
    /// FluxRec camera + CharacterController locomotion. Prevents the
    /// NullReferenceException spam from IBKAKBCOICD every frame.
    /// </summary>
    public static bool OfflineScreenPlayerTickPrefix()
    {
        if (!Plugin.DirectOrientationSceneLoad.Value)
            return true;

        if (_suppressScreenPlayerTicks)
            return false; // skip original

        return true;
    }

    private static void PatchExplicitPostfix(Harmony harmony, Type type, string methodName, Type[] parameters, string postfixName)
    {
        var target = AccessTools.Method(type, methodName, parameters);
        if (target == null)
        {
            Plugin.Log.LogWarning($"[HTTP] explicit target not found: {type.FullName}.{methodName}");
            return;
        }

        try
        {
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(SendRequestPatch), postfixName));
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(
                $"[HTTP] failed to patch postfix {type.FullName}.{methodName} " +
                $"via {postfixName}: {e.GetBaseException().GetType().Name}: " +
                $"{e.GetBaseException().Message}");
        }
    }

    public static void ExplicitRequestPrefix(ref HTTPRequest request) => ConnectToRecNetPatch.Prefix(ref request);
    public static void ExplicitImplPrefix(HTTPRequest __0) => ConnectToRecNetImplPatch.Prefix(__0);
    public static void ExplicitSendPrefix(HTTPRequest __instance) => ConnectToRecNetRequestSendPatch.Prefix(__instance);
    public static void FormAddFieldPrefix(ref string __1)
    {
        // This client build passes null for optional account bootstrap fields.
        // BestHTTP's IL2CPP form encoder dereferences the value instead of
        // treating it as empty, aborting the title flow before any request.
        __1 ??= string.Empty;
    }
    public static void UnityWebRequestSendPrefix(UnityWebRequest __instance)
    {
        try
        {
            var original = __instance?.url;
            var redirected = RedirectUrl(original);
            if (!string.Equals(original, redirected, StringComparison.Ordinal))
            {
                __instance.url = redirected;
                if (Plugin.Debug.Value)
                    Plugin.Log.LogInfo($"[UWR] redirected {original} -> {redirected}");
            }
        }
        catch (Exception e)
        {
            if (Plugin.Debug.Value)
                Plugin.Log.LogWarning($"[UWR] redirect failed: {e.Message}");
        }
    }
    public static bool ExplicitCertificatePrefix()
    {
        Plugin.Log.LogInfo("[HTTP] TLS certificate validation bypassed");
        return false;
    }

    public static bool DesktopCullingGroupPrefix(ref PHNOFPCAHIJ __result)
    {
        var noOp = new MMDAJOCGCAI();
        __result = noOp.TryCast<PHNOFPCAHIJ>();
        if (!_desktopCullingFallbackLogged)
        {
            _desktopCullingFallbackLogged = true;
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] using the shipped no-op culling group while " +
                "the offline Orientation player prefab initializes");
        }
        return false;
    }

    public static unsafe bool OfflineUrpQualityConfigPrefix(
        ref UnityEngine.Rendering.Universal.RecRoomQualityConfig __result)
    {
        if (_offlineUrpQualityConfig == null ||
            _offlineUrpQualityConfig.Pointer == IntPtr.Zero)
        {
            var config = new UnityEngine.Rendering.Universal.RecRoomQualityConfig();
            var configClass =
                Il2CppClassPointerStore<UnityEngine.Rendering.Universal.RecRoomQualityConfig>
                    .NativeClassPtr;

            // Conservative desktop defaults. The important part is supplying a
            // real stock config object; these values also prevent zero-budget
            // update/load loops while keeping unavailable online audio resampling
            // disabled in the local Photon room.
            WriteIl2CppBoolField(config.Pointer, configClass, "supportsNormalMappingUGC", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "supportsMacroMaterialsUGC", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "supportsHighQualitySkyboxes", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "supportsDepthSampling", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "supportsAlphaClipping", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "clothSimulationEnabled", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "optimizedUIRaycasts", true);
            WriteIl2CppBoolField(config.Pointer, configClass, "resamplePhotonAudioToCatchUp", false);
            WriteIl2CppInt32Field(config.Pointer, configClass, "playerPuppetRenderFramerate", 30);
            WriteIl2CppInt32Field(config.Pointer, configClass, "shareCameraPreviewRenderFramerate", 30);
            WriteIl2CppInt32Field(config.Pointer, configClass, "maximumSimultaneousPooledParticleSystems", 128);
            WriteIl2CppInt32Field(config.Pointer, configClass, "maximumImposterRendersPerUpdate", 2);
            WriteIl2CppInt32Field(config.Pointer, configClass, "imposterRenderingUpdateRate", 4);
            WriteIl2CppInt32Field(config.Pointer, configClass, "maximumParallelLoadRequests", 4);
            WriteIl2CppInt32Field(config.Pointer, configClass, "modelRefreshPerFrame", 8);
            WriteIl2CppInt32Field(config.Pointer, configClass, "controllerRefreshPerFrame", 8);
            _offlineUrpQualityConfig = config;
        }

        __result = _offlineUrpQualityConfig;
        if (!_offlineUrpQualityConfigReadyLogged)
        {
            _offlineUrpQualityConfigReadyLogged = true;
            Plugin.Log.LogWarning(
                "[QUALITY] installed local stock RecRoomQualityConfig for URP/PUN bootstrap");
        }
        return false;
    }

    public static bool OfflineUrpQualityTruePrefix(ref bool __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = true;
        return false;
    }

    public static bool OfflineUrpQualityFalsePrefix(ref bool __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = false;
        return false;
    }

    public static bool OfflineUrpQualityThirtyPrefix(ref int __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = 30;
        return false;
    }

    public static bool OfflineUrpQualityFourPrefix(ref int __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = 4;
        return false;
    }

    public static bool OfflineUrpQualityEightPrefix(ref int __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = 8;
        return false;
    }

    public static bool OfflineUrpQualityParticleBudgetPrefix(ref int __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = 128;
        return false;
    }

    public static bool OfflineUrpQualityTwoPrefix(ref int __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = 2;
        return false;
    }

    public static bool OfflineUrpTransparencyPrefix(
        ref UnityEngine.Rendering.Universal.TransparencyDetailLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.TransparencyDetailLevel.Medium;
        return false;
    }

    public static bool OfflineUrpSceneDecorationPrefix(
        ref UnityEngine.Rendering.Universal.SceneDecorationDetailLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.SceneDecorationDetailLevel.Normal;
        return false;
    }

    public static bool OfflineUrpLodScalingPrefix(
        ref UnityEngine.Rendering.Universal.LODScalingQualityLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.LODScalingQualityLevel.Normal;
        return false;
    }

    public static bool OfflineUrpParticleQualityPrefix(
        ref UnityEngine.Rendering.Universal.ParticleQualityLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.ParticleQualityLevel.Normal;
        return false;
    }

    public static bool OfflineUrpTerrainQualityPrefix(
        ref UnityEngine.Rendering.Universal.TerrainQualityLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.TerrainQualityLevel.Normal;
        return false;
    }

    public static bool OfflineUrpUpdateThrottlingPrefix(
        ref UnityEngine.Rendering.Universal.UpdateRateLimitType __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.UpdateRateLimitType.Unlimited;
        return false;
    }

    public static bool OfflineUrpBackgroundAnimationPrefix(
        ref UnityEngine.Rendering.Universal.BackgroundAnimationDetailLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.BackgroundAnimationDetailLevel.Normal;
        return false;
    }

    public static bool OfflineUrpCreatorFidelityPrefix(
        ref UnityEngine.Rendering.Universal.CreatorToolVisualFidelityLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.CreatorToolVisualFidelityLevel.Normal;
        return false;
    }

    public static bool OfflineUrpWeaponQualityPrefix(
        ref UnityEngine.Rendering.Universal.WeaponDetailQualityLevel __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.WeaponDetailQualityLevel.Normal;
        return false;
    }

    public static bool OfflineUrpShareCameraResolutionPrefix(
        ref UnityEngine.Rendering.Universal.ShareCameraPreviewResolution __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.ShareCameraPreviewResolution.Res_360P;
        return false;
    }

    public static bool OfflineUrpAvatarTextureResolutionPrefix(
        ref UnityEngine.Rendering.Universal.CustomAvatarItemTextureResolutionType __result)
    {
        LogOfflineUrpDirectDefaults();
        __result = UnityEngine.Rendering.Universal.CustomAvatarItemTextureResolutionType.Square_512;
        return false;
    }

    private static void LogOfflineUrpDirectDefaults()
    {
        if (_offlineUrpDirectDefaultsLogged)
            return;
        _offlineUrpDirectDefaultsLogged = true;
        Plugin.Log.LogWarning(
            "[QUALITY] supplying direct local URP quality defaults to the inlined provider getters");
    }

    public static bool OfflineOrientationAvatarLodAwakePrefix()
    {
        // The direct bundled-room path intentionally has no global remote-player
        // LOD service. The stock component dereferences that absent service in
        // Awake, while its only job is distance culling for other networked
        // players. It is unnecessary for the local Orientation player and its
        // exception interrupts the prefab's initialization chain.
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            !_localPlayerSpawnStarted ||
            _localPlayerSpawnSucceededLogged)
        {
            return true;
        }

        if (!_avatarLodBypassLogged)
        {
            _avatarLodBypassLogged = true;
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] disabled remote-player LOD registration for " +
                "the local offline Orientation avatar");
        }
        return false;
    }

    public static bool OfflineOrientationAvatarLodUpdatePrefix()
    {
        // Awake is intentionally skipped for the solo/offline Orientation
        // player, so its later coroutine must not tick the uninitialized
        // remote-player LOD registry. This system only selects distance LODs
        // between networked avatars; it does not animate or render the local
        // player's avatar.
        return !Plugin.DirectOrientationSceneLoad.Value ||
               (!_localPlayerSpawnStarted && !_localPlayerSpawnSucceededLogged);
    }

    public static void OrientationCursorVisiblePrefix(ref bool __0)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            !ShouldCaptureOrientationCursor())
            return;

        // The travel screen is UI and must retain a usable pointer. Once the
        // room is live, this incomplete offline Player graph continuously asks
        // Unity to release the cursor even though no watch/menu is visible.
        // Reject those stale requests and keep desktop look deterministic.
        if (_loadingScreenShown)
        {
            _orientationUiCursorRequested = false;
            __0 = true;
            return;
        }

        _orientationUiCursorRequested = false;
        __0 = false;
    }

    public static void OrientationCursorLockStatePrefix(
        ref UnityEngine.CursorLockMode __0)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            !ShouldCaptureOrientationCursor())
            return;

        if (_loadingScreenShown)
        {
            _orientationUiCursorRequested = false;
            __0 = UnityEngine.CursorLockMode.None;
            return;
        }

        _orientationUiCursorRequested = false;
        __0 = UnityEngine.CursorLockMode.Locked;
    }

    public static void OfflineScreenHudEnableCursorPrefix(ref bool __0)
    {
        if (Plugin.DirectOrientationSceneLoad.Value &&
            !_loadingScreenShown && ShouldCaptureOrientationCursor())
            __0 = false;
    }

    public static bool OfflineScreenHudIsCursorEnabledPrefix(ref bool __result)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            _loadingScreenShown || !ShouldCaptureOrientationCursor())
            return true;
        __result = false;
        return false;
    }

    private static bool ShouldCaptureOrientationCursor()
    {
        if (_loadingScreenShown || _localPlayerSpawnStarted || _localPlayerSpawnSucceededLogged)
            return true;
        if (_forceOrientationEnterDone)
            return true;
        try
        {
            if (GetLocalPlayerExists())
                return true;
        }
        catch { /* ignore */ }
        return AreOrientationScenesLoaded();
    }

    public static bool ShouldDrawOrientationReticle()
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            _loadingScreenShown ||
            _orientationUiCursorRequested)
            return false;

        return (_offlineLocomotionReady || _localPlayerSpawnSucceededLogged) &&
               AreOrientationScenesLoaded();
    }

    public static string GetOrientationInteractionPrompt()
    {
        return _orientationDoorPromptVisible
            ? "Press LEFT MOUSE BUTTON to open"
            : string.Empty;
    }

    public static bool TryGetOrientationDoorScreenRect(
        out UnityEngine.Rect rect)
    {
        rect = default;
        if (!_orientationDoorPromptVisible)
            return false;

        try
        {
            var camera = ResolveFluxPlayerCamera(null) ?? UnityEngine.Camera.main;
            if (camera != null && _orientationDoorVisualBoundsValid)
            {
                var min = _orientationDoorVisualBounds.min;
                var max = _orientationDoorVisualBounds.max;
                var corners = new[]
                {
                    new UnityEngine.Vector3(min.x, min.y, min.z),
                    new UnityEngine.Vector3(max.x, min.y, min.z),
                    new UnityEngine.Vector3(min.x, max.y, min.z),
                    new UnityEngine.Vector3(max.x, max.y, min.z),
                    new UnityEngine.Vector3(min.x, min.y, max.z),
                    new UnityEngine.Vector3(max.x, min.y, max.z),
                    new UnityEngine.Vector3(min.x, max.y, max.z),
                    new UnityEngine.Vector3(max.x, max.y, max.z),
                };
                var left = float.MaxValue;
                var top = float.MaxValue;
                var right = float.MinValue;
                var bottom = float.MinValue;
                var visible = 0;
                for (var i = 0; i < corners.Length; i++)
                {
                    var screen = camera.WorldToScreenPoint(corners[i]);
                    if (screen.z <= 0.01f)
                        continue;
                    visible++;
                    left = Math.Min(left, screen.x);
                    right = Math.Max(right, screen.x);
                    var guiY = UnityEngine.Screen.height - screen.y;
                    top = Math.Min(top, guiY);
                    bottom = Math.Max(bottom, guiY);
                }
                if (visible >= 2)
                {
                    var padding = 24f;
                    var width = right - left;
                    var height = bottom - top;
                    if (width >= 24f && height >= 24f &&
                        width <= UnityEngine.Screen.width * 0.94f &&
                        height <= UnityEngine.Screen.height * 0.94f)
                    {
                        rect = new UnityEngine.Rect(
                            Math.Max(12f, left - padding),
                            Math.Max(12f, top - padding),
                            Math.Min(UnityEngine.Screen.width - 24f,
                                width + padding * 2f),
                            Math.Min(UnityEngine.Screen.height - 24f,
                                height + padding * 2f));
                        return true;
                    }
                }
            }
        }
        catch { /* centered fallback below is intentionally guaranteed */ }

        // Guaranteed visual fallback for this fixed bundled entrance. The
        // vestibule gate ensures this is never drawn during ordinary walking.
        var fallbackWidth = UnityEngine.Screen.width * 0.42f;
        var fallbackHeight = UnityEngine.Screen.height * 0.64f;
        rect = new UnityEngine.Rect(
            (UnityEngine.Screen.width - fallbackWidth) * 0.5f,
            UnityEngine.Screen.height * 0.12f,
            fallbackWidth,
            fallbackHeight);
        return true;
    }

    public static string GetOrientationCoachPrompt()
    {
        if (!_orientationContentEnteredAt.HasValue ||
            !string.Equals(_orientationContentScene, "Orientation_Scene1",
                StringComparison.Ordinal))
            return string.Empty;

        var elapsed = (DateTime.UtcNow - _orientationContentEnteredAt.Value)
            .TotalSeconds;
        if (elapsed < 3.2)
            return "Welcome to Rec Room!";
        if (elapsed < 6.4)
            return "Look around with your mouse or touchpad";
        if (elapsed < 10.5)
            return "Use  W A S D  to walk";
        return string.Empty;
    }

    /// <summary>
    /// Restarts the shipped Orientation components after the local/offline
    /// Player exists. Their normal PlayerSpawned event is never delivered when
    /// Player.Awake exits through the guarded NRE fallback, which left every
    /// Coach line, vignette, prompt, and portal dormant even though the assets
    /// and scene objects were loaded correctly.
    /// </summary>
    private static void TickOfflineOrientationStockFlow()
    {
        try
        {
            var currentScene = GetLoadedOrientationContentSceneName();
            if (string.IsNullOrEmpty(currentScene))
                return;

            if (!string.Equals(
                    currentScene, _orientationContentScene,
                    StringComparison.Ordinal))
            {
                EnterOfflineOrientationContentScene(currentScene);
            }

            TryInitializeStockOrientationScene(currentScene);

            if (_stockOrientationIntroduction != null &&
                _stockOrientationIntroduction.Pointer ==
                _stockOrientationIntroductionPtr)
            {
                try { _stockOrientationIntroduction.ManualUpdate(); }
                catch (Exception e)
                {
                    if (!_stockOrientationIntroUpdateErrorLogged)
                    {
                        _stockOrientationIntroUpdateErrorLogged = true;
                        Plugin.Log.LogWarning(
                            "[ORIENTATION-FLOW] stock introduction update fell " +
                            "back to encounter/UI bridge: " +
                            e.GetBaseException().Message);
                    }
                }
            }

            TickFirstOrientationSceneSequence();
            TickOfflineOrientationDoorInteraction(currentScene);
            TickOfflineOrientationSceneTransition();

            if (string.Equals(currentScene, "Orientation_Rewards",
                    StringComparison.Ordinal) &&
                !_orientationWatchUnlockAttempted &&
                _orientationContentEnteredAt.HasValue &&
                (DateTime.UtcNow - _orientationContentEnteredAt.Value)
                    .TotalSeconds >= 8.0)
            {
                _orientationWatchUnlockAttempted = true;
                try
                {
                    RecRoom.Activities.Orientation.OrientationManager.UnlockWatch();
                    Plugin.Log.LogWarning(
                        "[ORIENTATION-FLOW] restored the shipped watch unlock " +
                        "after the Rewards pickup sequence");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning(
                        "[ORIENTATION-FLOW] watch unlock deferred: " +
                        e.GetBaseException().Message);
                }
            }
        }
        catch (Exception e)
        {
            if (!_orientationStockFlowErrorLogged)
            {
                _orientationStockFlowErrorLogged = true;
                Plugin.Log.LogWarning(
                    "[ORIENTATION-FLOW] guarded stock-flow error: " +
                    e.GetBaseException().Message);
            }
        }
    }

    private static string GetLoadedOrientationContentSceneName()
    {
        for (var i = _offlineOrientationSceneOrder.Length - 1; i >= 0; i--)
        {
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager
                    .GetSceneByName(_offlineOrientationSceneOrder[i]);
                if (scene.IsValid() && scene.isLoaded)
                    return _offlineOrientationSceneOrder[i];
            }
            catch { /* keep probing */ }
        }
        return string.Empty;
    }

    private static void EnterOfflineOrientationContentScene(string sceneName)
    {
        _orientationContentScene = sceneName;
        _orientationContentEnteredAt = DateTime.UtcNow;
        _orientationDoorPromptVisible = false;
        _orientationTargetDoor = null;
        _orientationTargetDoorPtr = IntPtr.Zero;
        _orientationNearbyDoor = null;
        _orientationNearbyDoorPtr = IntPtr.Zero;
        _orientationDoorNextScanAt = null;
        _orientationSceneDoors.Clear();
        _orientationDoorVisualBoundsValid = false;
        _orientationDoorVisualName = string.Empty;
        _orientationWatchUnlockAttempted = false;
        _leftMouseWasDown = false;
        ClearOrientationDoorHighlight();
        _orientationDoorVisualRoot = null;
        _orientationDoorVisualRootPtr = IntPtr.Zero;
        _orientationDoorVisualCollider = null;
        _orientationDoorAnimator = null;
        _orientationDoorHighlightApplied = false;
        _orientationDoorArmedLogged = false;
        _orientationDoorVisualProbeNextAt = null;
        _validationOrientationDoorPositioned = false;
        _validationOrientationDoorPressed = false;
        _validationOrientationDoorPressAt = null;

        if (string.Equals(sceneName, "Orientation_Scene1",
                StringComparison.Ordinal))
        {
            _orientationIntroEncounterLevelVo = null;
            _orientationIntroEncounterWalk = null;
            _orientationIntroEncounterHands = null;
            _orientationIntroEncounterLook = null;
            _orientationIntroLevelVoActivated = false;
            _orientationIntroWalkActivated = false;
            _orientationIntroHandsActivated = false;
            _orientationIntroLookActivated = false;
            _stockOrientationIntroduction = null;
            _stockOrientationIntroductionPtr = IntPtr.Zero;
            _stockOrientationIntroInitAttempts = 0;
            _stockOrientationIntroUpdateErrorLogged = false;
        }

        Plugin.Log.LogWarning(
            $"[ORIENTATION-FLOW] entered shipped content scene '{sceneName}'");
    }

    private static void TryInitializeStockOrientationScene(string sceneName)
    {
        if (_initializedOrientationScenes.Contains(sceneName))
            return;
        if (_orientationContentEnteredAt.HasValue &&
            (DateTime.UtcNow - _orientationContentEnteredAt.Value)
                .TotalSeconds < 0.4)
            return;

        _initializedOrientationScenes.Add(sceneName);
        var initialized = 0;
        try
        {
            var encounters = UnityEngine.Resources.FindObjectsOfTypeAll<
                RecRoom.Core.Encounters.GameEncounter>();
            if (encounters != null)
            {
                for (var i = 0; i < encounters.Length; i++)
                {
                    var encounter = encounters[i];
                    if (encounter == null || encounter.Pointer == IntPtr.Zero ||
                        encounter.gameObject == null ||
                        !string.Equals(encounter.gameObject.scene.name, sceneName,
                            StringComparison.Ordinal))
                        continue;

                    try
                    {
                        encounter.gameObject.SetActive(true);
                        encounter.enabled = true;
                        encounter.Initialize();
                        initialized++;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning(
                            $"[ORIENTATION-FLOW] encounter init " +
                            $"'{encounter.gameObject.name}' deferred: " +
                            e.GetBaseException().Message);
                    }

                    if (!string.Equals(sceneName, "Orientation_Scene1",
                            StringComparison.Ordinal))
                        continue;
                    switch (encounter.gameObject.name)
                    {
                        case "Level Start VO":
                            _orientationIntroEncounterLevelVo = encounter;
                            break;
                        case "Walk Prompt":
                            _orientationIntroEncounterWalk = encounter;
                            break;
                        case "LookAtHands":
                            _orientationIntroEncounterHands = encounter;
                            break;
                        case "Look Prompt":
                            _orientationIntroEncounterLook = encounter;
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[ORIENTATION-FLOW] encounter scan deferred: " +
                e.GetBaseException().Message);
        }

        if (string.Equals(sceneName, "Orientation_Scene1",
                StringComparison.Ordinal))
        {
            TryInitializeStockOrientationIntroduction();
        }

        Plugin.Log.LogWarning(
            $"[ORIENTATION-FLOW] initialized shipped scene systems " +
            $"scene='{sceneName}' encounters={initialized}");
    }

    private static void TryInitializeStockOrientationIntroduction()
    {
        if (_stockOrientationIntroduction != null ||
            _stockOrientationIntroInitAttempts >= 2)
            return;

        _stockOrientationIntroInitAttempts++;
        try
        {
            var intros = UnityEngine.Resources.FindObjectsOfTypeAll<
                RecRoom.Activities.Orientation.OrientationIntroduction>();
            if (intros == null)
                return;
            for (var i = 0; i < intros.Length; i++)
            {
                var intro = intros[i];
                if (intro == null || intro.Pointer == IntPtr.Zero ||
                    intro.gameObject == null ||
                    !string.Equals(intro.gameObject.scene.name,
                        "Orientation_Scene1", StringComparison.Ordinal))
                    continue;
                intro.gameObject.SetActive(true);
                intro.enabled = true;
                intro.Init();
                _stockOrientationIntroduction = intro;
                _stockOrientationIntroductionPtr = intro.Pointer;
                Plugin.Log.LogWarning(
                    "[ORIENTATION-FLOW] restarted the shipped " +
                    "OrientationIntroduction (Coach + screen vignette)");
                return;
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[ORIENTATION-FLOW] stock introduction init deferred: " +
                e.GetBaseException().Message);
        }
    }

    private static void TickFirstOrientationSceneSequence()
    {
        if (!string.Equals(_orientationContentScene,
                "Orientation_Scene1", StringComparison.Ordinal) ||
            !_orientationContentEnteredAt.HasValue)
            return;

        var elapsed = (DateTime.UtcNow - _orientationContentEnteredAt.Value)
            .TotalSeconds;
        if (elapsed >= 0.8 && !_orientationIntroLevelVoActivated)
        {
            _orientationIntroLevelVoActivated = true;
            ActivateOrientationEncounter(
                _orientationIntroEncounterLevelVo, "Level Start VO");
        }
        if (elapsed >= 3.3 && !_orientationIntroWalkActivated)
        {
            _orientationIntroWalkActivated = true;
            ActivateOrientationEncounter(
                _orientationIntroEncounterWalk, "Walk Prompt");
        }
        if (elapsed >= 5.3 && !_orientationIntroHandsActivated)
        {
            _orientationIntroHandsActivated = true;
            ActivateOrientationEncounter(
                _orientationIntroEncounterHands, "LookAtHands vignette");
        }
        if (elapsed >= 8.0 && !_orientationIntroLookActivated)
        {
            _orientationIntroLookActivated = true;
            ActivateOrientationEncounter(
                _orientationIntroEncounterLook, "Look Prompt");
        }
    }

    private static void ActivateOrientationEncounter(
        RecRoom.Core.Encounters.GameEncounter encounter,
        string label)
    {
        if (encounter == null || encounter.Pointer == IntPtr.Zero)
            return;
        try
        {
            encounter.MasterActivate();
            Plugin.Log.LogWarning(
                $"[ORIENTATION-FLOW] activated shipped encounter '{label}'");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[ORIENTATION-FLOW] encounter '{label}' fallback UI active: " +
                e.GetBaseException().Message);
        }
    }

    private static void TickOfflineOrientationDoorInteraction(
        string currentScene)
    {
        var cam = ResolveFluxPlayerCamera(null) ?? UnityEngine.Camera.main;
        if (cam == null)
            return;

        var elapsed = _orientationContentEnteredAt.HasValue
            ? (DateTime.UtcNow - _orientationContentEnteredAt.Value)
                .TotalSeconds
            : 0.0;
        var unattendedValidation = string.Equals(
            Environment.GetEnvironmentVariable("RECNET_VALIDATE_ACCOUNT_LAUNCH"),
            "1",
            StringComparison.Ordinal);
        if (unattendedValidation &&
            string.Equals(currentScene, "Orientation_Scene1",
                StringComparison.Ordinal) &&
            elapsed >= 10.25 &&
            !_validationOrientationDoorPositioned)
        {
            // CI-only traversal of the exact player repro. Normal launchers do
            // not set this environment variable, so player movement is never
            // altered in real play. Leave the real door highlighted for a full
            // second before clicking so the animator path is observable.
            _validationOrientationDoorPositioned = true;
            _validationOrientationDoorPressAt = DateTime.UtcNow.AddSeconds(1);
            cam.transform.position = new UnityEngine.Vector3(
                -153.35f, -10.39f, -301.25f);
            cam.transform.rotation = UnityEngine.Quaternion.Euler(0f, 270f, 0f);
            Plugin.Log.LogWarning(
                "[VALIDATION] positioned desktop camera at the shipped " +
                "Orientation entrance for highlight/click verification");
        }

        var isScene1Entrance = string.Equals(
                                   currentScene, "Orientation_Scene1",
                                   StringComparison.Ordinal) &&
                               IsInsideShippedOrientationEntrance(
                                   cam.transform.position);
        var door = FindOrientationDoorTarget(cam, currentScene, 5.25f);
        var unlockAfter = string.Equals(currentScene, "Orientation_Scene1",
            StringComparison.Ordinal) ? 10.0 : 8.0;
        // Scene1's visible school door and its LockableDoor wrapper are offset
        // in the shipped hierarchy. The exact world-space entrance is still a
        // valid desktop interaction even if the IL2CPP wrapper is temporarily
        // unavailable. Later scenes continue to require a real portal object.
        var hasTarget = door != null || isScene1Entrance;
        var targetReady = hasTarget && elapsed >= unlockAfter &&
                          !_orientationSceneTransitionInProgress;

        var changed = (door?.Pointer ?? IntPtr.Zero) != _orientationTargetDoorPtr;
        if (changed)
        {
            ClearOrientationDoorHighlight();
            _orientationTargetDoor = door;
            _orientationTargetDoorPtr = door?.Pointer ?? IntPtr.Zero;
            _orientationDoorHighlightApplied = false;
            _orientationDoorArmedLogged = false;
        }

        // Highlighting must follow the unlocked state, not only the one frame
        // on which the target pointer changed. The old coupling permanently
        // missed the highlight whenever a door was discovered during the intro.
        if (targetReady && !_orientationDoorHighlightApplied)
        {
            if (door != null)
                SetOfflineOrientationDoorLocked(door, false);
            SetOrientationDoorHighlight(door, true);
            _orientationDoorHighlightApplied = true;

            if (!_orientationDoorArmedLogged)
            {
                _orientationDoorArmedLogged = true;
                var nearest = _orientationDoorVisualRoot != null
                    ? UnityEngine.Vector3.Distance(
                        cam.transform.position,
                        _orientationDoorVisualRoot.transform.position)
                    : 0f;
                Plugin.Log.LogWarning(
                    $"[ORIENTATION-FLOW] interaction target armed " +
                    $"door='{door?.gameObject?.name ?? "SchoolEntranceDoor_fbx"}' " +
                    $"nearest={nearest:0.00}m visual='{_orientationDoorVisualName}' " +
                    $"scene='{currentScene}'");
            }
        }
        else if (!targetReady && _orientationDoorHighlightApplied)
        {
            ClearOrientationDoorHighlight();
            _orientationDoorHighlightApplied = false;
        }

        if (targetReady)
            UpdateOrientationDoorOutline(door, cam);

        _orientationDoorPromptVisible = targetReady;

        var mouseDown = false;
        var pressed = false;
        try
        {
            pressed = UnityEngine.Input.GetMouseButtonDown(0);
            mouseDown = UnityEngine.Input.GetMouseButton(0);
        }
        catch { /* use Win32 fallback */ }
        try
        {
            // Read the Win32 state once: high bit is held, low bit records a
            // press since the previous query and survives short touchpad taps.
            var rawMouse = GetAsyncKeyState(0x01);
            var winMouseDown = (rawMouse & 0x8000) != 0;
            pressed = pressed || (rawMouse & 0x0001) != 0;
            mouseDown = mouseDown || winMouseDown;
        }
        catch { /* ignore */ }
        pressed = pressed || (mouseDown && !_leftMouseWasDown);
        _leftMouseWasDown = mouseDown;

        if (unattendedValidation &&
            targetReady &&
            !_validationOrientationDoorPressed &&
            _validationOrientationDoorPressAt.HasValue &&
            DateTime.UtcNow >= _validationOrientationDoorPressAt.Value)
        {
            _validationOrientationDoorPressed = true;
            _validationOrientationDoorPressAt = null;
            pressed = true;
            Plugin.Log.LogWarning(
                "[VALIDATION] clicking the armed shipped Orientation entrance");
        }

        if (!pressed || !_orientationDoorPromptVisible)
            return;

        var player = FindLiveLocalPlayer();
        if (door != null && player != null)
        {
            SetOfflineOrientationDoorLocked(door, false);
            PlayShippedOrientationDoorOpen(door);
            try
            {
                door.Use(player, false);
                Plugin.Log.LogWarning(
                    $"[ORIENTATION-FLOW] used shipped portal " +
                    $"door='{door.gameObject?.name}' scene='{currentScene}'");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    "[ORIENTATION-FLOW] stock portal call failed; direct bundled " +
                    "scene fallback armed: " + e.GetBaseException().Message);
            }
        }

        var index = Array.IndexOf(_offlineOrientationSceneOrder, currentScene);
        if (index < 0 || index >= _offlineOrientationSceneOrder.Length - 1)
            return;

        _orientationPortalSourceScene = currentScene;
        _orientationPortalTargetScene = _offlineOrientationSceneOrder[index + 1];
        _orientationPortalUsePendingAt = DateTime.UtcNow;
        _orientationSceneTransitionInProgress = true;
        _orientationDoorPromptVisible = false;
        ClearOrientationDoorHighlight();
        Plugin.Log.LogWarning(
            $"[ORIENTATION-FLOW] desktop entrance accepted; transition armed " +
            $"'{_orientationPortalSourceScene}' -> '{_orientationPortalTargetScene}'");
    }

    private static LockableDoor FindCenteredOrientationDoor(
        UnityEngine.Camera camera,
        float maxDistance)
    {
        try
        {
            if (!UnityEngine.Physics.Raycast(
                    camera.transform.position,
                    camera.transform.forward,
                    out var hit,
                    maxDistance,
                    ~0,
                    UnityEngine.QueryTriggerInteraction.Collide) ||
                hit.collider == null)
                return null;

            var node = hit.collider.transform;
            for (var depth = 0; node != null && depth < 8; depth++)
            {
                var door = node.gameObject.GetComponent<LockableDoor>();
                if (door != null && door.Pointer != IntPtr.Zero)
                    return door;
                node = node.parent;
            }
        }
        catch { /* no centered interactable */ }
        return null;
    }

    /// <summary>
    /// The entrance glass panes use colliders on sibling objects, so a centered
    /// ray often cannot walk up to the shipped LockableDoor component. Prefer
    /// the exact ray result, then periodically choose the nearest door in the
    /// active Orientation subscene that is both close to and in front of the
    /// desktop camera.
    /// </summary>
    private static LockableDoor FindOrientationDoorTarget(
        UnityEngine.Camera camera,
        string currentScene,
        float maxDistance)
    {
        // Scene1 has one authored school entrance. Bind it by its shipped asset
        // name before doing any physics query; the serialized collider and
        // animator are on SchoolEntranceDoor_fbx while the logical portal root
        // is offset elsewhere in the hierarchy.
        if (string.Equals(currentScene, "Orientation_Scene1",
                StringComparison.Ordinal))
        {
            var entranceDoor = ResolveShippedScene1EntranceDoor();
            if (IsInsideShippedOrientationEntrance(camera.transform.position))
                return entranceDoor;

            // The player is still on the approach path. Avoid an allocating
            // ray/renderer scan every frame until they reach the real doorway.
            return null;
        }

        var centered = FindCenteredOrientationDoor(camera, maxDistance);
        if (centered != null)
        {
            TryCaptureCenteredOrientationDoorVisual(camera, maxDistance + 3f);
            return centered;
        }

        var visualDoorHit = TryCaptureCenteredOrientationDoorVisual(
            camera, maxDistance + 3f);

        // Later Orientation segments also ship one logical exit door whose
        // component root may be offset. A real centered world hit plus exactly
        // one cached door is unambiguous and avoids hard-coded later geometry.
        if (visualDoorHit && _orientationSceneDoors.Count == 1 &&
            !string.Equals(currentScene, "Orientation_Scene1",
                StringComparison.Ordinal))
            return _orientationSceneDoors[0];

        if (_orientationNearbyDoor != null &&
            _orientationNearbyDoorPtr != IntPtr.Zero &&
            _orientationNearbyDoor.Pointer == _orientationNearbyDoorPtr &&
            IsOrientationDoorUsableTarget(
                _orientationNearbyDoor, camera, currentScene,
                maxDistance + 2.25f, 0.42f, out _))
        {
            return _orientationNearbyDoor;
        }

        if (_orientationSceneDoors.Count == 0 &&
            (!_orientationDoorNextScanAt.HasValue ||
             DateTime.UtcNow >= _orientationDoorNextScanAt.Value))
        {
            _orientationDoorNextScanAt = DateTime.UtcNow.AddSeconds(1.0);
            try
            {
                var doors = UnityEngine.Resources.FindObjectsOfTypeAll<LockableDoor>();
                if (doors != null)
                {
                    for (var i = 0; i < doors.Length; i++)
                    {
                        var candidate = doors[i];
                        if (candidate == null || candidate.Pointer == IntPtr.Zero ||
                            candidate.gameObject == null)
                            continue;
                        var sceneName = candidate.gameObject.scene.name ?? string.Empty;
                        if (!string.Equals(sceneName, currentScene, StringComparison.Ordinal) &&
                            !sceneName.StartsWith("Orientation_", StringComparison.Ordinal))
                            continue;
                        _orientationSceneDoors.Add(candidate);
                    }
                }
                if (!string.Equals(_orientationDoorScanLoggedScene, currentScene,
                        StringComparison.Ordinal))
                {
                    _orientationDoorScanLoggedScene = currentScene;
                    Plugin.Log.LogWarning(
                        $"[ORIENTATION-FLOW] cached shipped doors " +
                        $"scene='{currentScene}' count={_orientationSceneDoors.Count}");
                }
            }
            catch { /* retry once content objects finish activating */ }
        }

        LockableDoor best = null;
        var bestScore = float.MaxValue;
        for (var i = 0; i < _orientationSceneDoors.Count; i++)
        {
            var candidate = _orientationSceneDoors[i];
            if (!IsOrientationDoorUsableTarget(
                    candidate, camera, currentScene,
                    maxDistance + 2.25f, 0.42f, out var score) ||
                score >= bestScore)
                continue;
            best = candidate;
            bestScore = score;
        }

        _orientationNearbyDoor = best;
        _orientationNearbyDoorPtr = best?.Pointer ?? IntPtr.Zero;
        return best;
    }

    private static bool IsInsideShippedOrientationEntrance(
        UnityEngine.Vector3 position)
    {
        return position.x <= -149.0f && position.x >= -160.5f &&
               position.y >= -14.5f && position.y <= -6.0f &&
               position.z >= -310.0f && position.z <= -292.0f;
    }

    private static LockableDoor ResolveShippedScene1EntranceDoor()
    {
        try
        {
            if (_orientationDoorVisualRoot != null &&
                _orientationDoorVisualRootPtr != IntPtr.Zero &&
                _orientationDoorVisualRoot.Pointer == _orientationDoorVisualRootPtr)
            {
                if (_orientationNearbyDoor != null &&
                    _orientationNearbyDoorPtr != IntPtr.Zero &&
                    _orientationNearbyDoor.Pointer == _orientationNearbyDoorPtr)
                    return _orientationNearbyDoor;
                var cachedDoor =
                    _orientationDoorVisualRoot.GetComponent<LockableDoor>();
                _orientationNearbyDoor = cachedDoor;
                _orientationNearbyDoorPtr = cachedDoor?.Pointer ?? IntPtr.Zero;
                return cachedDoor;
            }
        }
        catch
        {
            _orientationDoorVisualRoot = null;
            _orientationDoorVisualRootPtr = IntPtr.Zero;
        }

        try
        {
            var root = UnityEngine.GameObject.Find("SchoolEntranceDoor_fbx");
            if (root == null || root.Pointer == IntPtr.Zero ||
                !string.Equals(root.scene.name, "Orientation_Scene1",
                    StringComparison.Ordinal))
                return null;

            _orientationDoorVisualRoot = root;
            _orientationDoorVisualRootPtr = root.Pointer;
            _orientationDoorVisualName = root.name;
            _orientationDoorVisualCollider =
                root.GetComponent<UnityEngine.Collider>();
            _orientationDoorAnimator =
                root.GetComponent<UnityEngine.Animator>();
            if (_orientationDoorVisualCollider != null)
            {
                _orientationDoorVisualBounds =
                    _orientationDoorVisualCollider.bounds;
                _orientationDoorVisualBoundsValid = true;
            }

            var door = root.GetComponent<LockableDoor>();
            _orientationNearbyDoor = door;
            _orientationNearbyDoorPtr = door?.Pointer ?? IntPtr.Zero;
            Plugin.Log.LogWarning(
                $"[ORIENTATION-FLOW] bound shipped entrance visual='{root.name}' " +
                $"world={root.transform.position} collider=" +
                $"{(_orientationDoorVisualCollider != null)} animator=" +
                $"{(_orientationDoorAnimator != null)} portal={(door != null)}");
            return door;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[ORIENTATION-FLOW] shipped entrance bind deferred: " +
                e.GetBaseException().Message);
            return null;
        }
    }

    private static bool TryCaptureCenteredOrientationDoorVisual(
        UnityEngine.Camera camera,
        float maxDistance)
    {
        try
        {
            if (_orientationDoorVisualProbeNextAt.HasValue &&
                DateTime.UtcNow < _orientationDoorVisualProbeNextAt.Value)
                return _orientationDoorVisualBoundsValid;
            _orientationDoorVisualProbeNextAt =
                DateTime.UtcNow.AddMilliseconds(75);

            _orientationDoorRayHitBuffer ??=
                new Il2CppStructArray<UnityEngine.RaycastHit>(12);
            var hitCount = UnityEngine.Physics.RaycastNonAlloc(
                camera.transform.position,
                camera.transform.forward,
                _orientationDoorRayHitBuffer,
                maxDistance,
                ~0,
                UnityEngine.QueryTriggerInteraction.Collide);
            if (hitCount <= 0)
                return false;

            UnityEngine.RaycastHit? best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _orientationDoorRayHitBuffer[i];
                if (hit.collider == null)
                    continue;
                var colliderName = hit.collider.name ?? string.Empty;
                var objectName = hit.collider.gameObject?.name ?? string.Empty;
                if (colliderName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colliderName.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("[Player]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("FluxRec", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (hit.distance >= bestDistance)
                    continue;
                best = hit;
                bestDistance = hit.distance;
            }
            if (!best.HasValue)
                return false;

            var selected = best.Value;
            _orientationDoorVisualBounds = selected.collider.bounds;
            _orientationDoorVisualBoundsValid = true;
            _orientationDoorVisualName = selected.collider.gameObject?.name ??
                                         selected.collider.name ?? "door collider";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsOrientationDoorUsableTarget(
        LockableDoor door,
        UnityEngine.Camera camera,
        string currentScene,
        float maxDistance,
        float minimumFacing,
        out float score)
    {
        score = float.MaxValue;
        try
        {
            if (door == null || door.Pointer == IntPtr.Zero ||
                door.gameObject == null || camera == null ||
                (!string.Equals(door.gameObject.scene.name, currentScene,
                     StringComparison.Ordinal) &&
                 !(door.gameObject.scene.name ?? string.Empty).StartsWith(
                     "Orientation_", StringComparison.Ordinal)))
                return false;

            var cameraPosition = camera.transform.position;
            var target = door.transform.position;
            var nearestDistance = UnityEngine.Vector3.Distance(
                cameraPosition, target);
            TryGetNearestOrientationDoorPoint(
                door, cameraPosition, out target, out nearestDistance);

            // Standing inside a door renderer's AABB produces a nearest-point
            // distance of exactly zero. That is the strongest possible
            // proximity signal, not an invalid target. Once close, facing is
            // deliberately irrelevant so laptop users can click either handle.
            if (nearestDistance <= 4.25f)
            {
                score = nearestDistance;
                return true;
            }
            if (nearestDistance > maxDistance)
                return false;
            var offset = target - cameraPosition;
            var distance = offset.magnitude;
            if (distance < 0.05f)
                return true;
            var facing = UnityEngine.Vector3.Dot(
                camera.transform.forward, offset / distance);
            if (facing < minimumFacing)
                return false;

            score = nearestDistance + (1f - facing) * 3.5f;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetNearestOrientationDoorPoint(
        LockableDoor door,
        UnityEngine.Vector3 origin,
        out UnityEngine.Vector3 nearestPoint,
        out float nearestDistance)
    {
        nearestPoint = door != null
            ? door.transform.position
            : UnityEngine.Vector3.zero;
        nearestDistance = UnityEngine.Vector3.Distance(origin, nearestPoint);
        try
        {
            var renderers = door?.gameObject?
                .GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (renderers == null)
                return false;
            var found = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;
                var point = renderer.bounds.ClosestPoint(origin);
                var distance = UnityEngine.Vector3.Distance(origin, point);
                if (found && distance >= nearestDistance)
                    continue;
                found = true;
                nearestPoint = point;
                nearestDistance = distance;
            }
            return found;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetOrientationDoorBounds(
        LockableDoor door,
        out UnityEngine.Bounds bounds)
    {
        bounds = default;
        try
        {
            var renderers = door?.gameObject?
                .GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return false;
            var found = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }
        catch
        {
            return false;
        }
    }

    private static void SetOfflineOrientationDoorLocked(
        LockableDoor door,
        bool locked)
    {
        if (door == null || door.Pointer == IntPtr.Zero)
            return;
        try
        {
            var setter = AccessTools.Method(
                typeof(LockableDoor), "set_IsLocked",
                new[] { typeof(bool) });
            setter?.Invoke(door, new object[] { locked });
        }
        catch { /* prompt/fallback transition still remains usable */ }
    }

    private static void SetOrientationDoorHighlight(
        LockableDoor door,
        bool enabled)
    {
        try
        {
            var root = door?.gameObject ?? _orientationDoorVisualRoot;
            if (root == null)
                return;
            _orientationDoorVisualRoot = root;
            _orientationDoorVisualRootPtr = root.Pointer;
            _orientationDoorAnimator ??=
                root.GetComponent<UnityEngine.Animator>();
            if (_orientationDoorAnimator != null)
            {
                _orientationDoorAnimator.enabled = true;
                _orientationDoorAnimator.SetBool("Locked", false);
                _orientationDoorAnimator.SetBool("Highlighted", enabled);
            }

            _orientationHighlightedRenderers = root
                .GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (_orientationHighlightBlock == null)
                _orientationHighlightBlock =
                    new UnityEngine.MaterialPropertyBlock();
            _orientationHighlightBlock.Clear();
            if (enabled)
            {
                _orientationHighlightBlock.SetColor(
                    UnityEngine.Shader.PropertyToID("_EmissionColor"),
                    new UnityEngine.Color(1.0f, 0.22f, 0.04f, 1.0f));
            }
            if (_orientationHighlightedRenderers == null)
                return;
            for (var i = 0; i < _orientationHighlightedRenderers.Length; i++)
            {
                var renderer = _orientationHighlightedRenderers[i];
                if (renderer != null)
                    renderer.SetPropertyBlock(_orientationHighlightBlock);
            }
            if (_orientationDoorOutline != null)
                _orientationDoorOutline.gameObject.SetActive(enabled);
        }
        catch { /* visual highlight is best effort */ }
    }

    private static void PlayShippedOrientationDoorOpen(LockableDoor door)
    {
        try
        {
            var root = door?.gameObject ?? _orientationDoorVisualRoot;
            if (root == null)
                return;
            _orientationDoorAnimator ??=
                root.GetComponent<UnityEngine.Animator>();
            if (_orientationDoorAnimator == null)
                return;
            _orientationDoorAnimator.enabled = true;
            _orientationDoorAnimator.SetBool("Locked", false);
            _orientationDoorAnimator.SetBool("Highlighted", false);
            _orientationDoorAnimator.Play("ActivityDoor_Open", 0, 0f);
        }
        catch { /* the direct scene transition does not depend on animation */ }
    }

    private static void UpdateOrientationDoorOutline(
        LockableDoor door,
        UnityEngine.Camera camera)
    {
        try
        {
            if (camera == null ||
                (door == null && !_orientationDoorVisualBoundsValid))
                return;

            UnityEngine.Bounds bounds;
            if (_orientationDoorVisualBoundsValid)
                bounds = _orientationDoorVisualBounds;
            else if (!TryGetOrientationDoorBounds(door, out bounds))
                return;

            if (_orientationDoorOutline == null)
            {
                var outlineObject =
                    new UnityEngine.GameObject("FluxRec_OrientationDoorOutline");
                UnityEngine.Object.DontDestroyOnLoad(outlineObject);
                _orientationDoorOutline = outlineObject.AddComponent<
                    UnityEngine.LineRenderer>();
                _orientationDoorOutline.useWorldSpace = true;
                _orientationDoorOutline.loop = true;
                _orientationDoorOutline.positionCount = 4;
                _orientationDoorOutline.startWidth = 0.032f;
                _orientationDoorOutline.endWidth = 0.032f;
                _orientationDoorOutline.numCornerVertices = 3;
                var shader = UnityEngine.Shader.Find("Sprites/Default") ??
                             UnityEngine.Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    _orientationDoorOutline.material =
                        new UnityEngine.Material(shader);
                    _orientationDoorOutline.material.renderQueue = 5000;
                    if (_orientationDoorOutline.material.HasProperty("_ZTest"))
                    {
                        _orientationDoorOutline.material.SetInt(
                            "_ZTest",
                            (int)UnityEngine.Rendering.CompareFunction.Always);
                    }
                }
                var orange = new UnityEngine.Color(1f, 0.28f, 0.03f, 1f);
                _orientationDoorOutline.startColor = orange;
                _orientationDoorOutline.endColor = orange;
                _orientationDoorOutline.sortingOrder = 32767;
            }

            var center = bounds.center;
            var right = camera.transform.right;
            var up = UnityEngine.Vector3.up;
            var halfWidth = Math.Clamp(bounds.extents.magnitude * 0.72f, 0.55f, 2.2f);
            var halfHeight = Math.Clamp(bounds.extents.y, 0.8f, 2.35f);
            // Pull the outline a little toward the player so it remains visible
            // over the frosted glass regardless of the door material shader.
            center += (camera.transform.position - center).normalized * 0.035f;
            _orientationDoorOutline.SetPosition(0, center - right * halfWidth - up * halfHeight);
            _orientationDoorOutline.SetPosition(1, center + right * halfWidth - up * halfHeight);
            _orientationDoorOutline.SetPosition(2, center + right * halfWidth + up * halfHeight);
            _orientationDoorOutline.SetPosition(3, center - right * halfWidth + up * halfHeight);
            _orientationDoorOutline.gameObject.SetActive(true);
        }
        catch { /* emission highlight and interaction still work */ }
    }

    private static void ClearOrientationDoorHighlight()
    {
        try
        {
            if (_orientationDoorAnimator != null)
                _orientationDoorAnimator.SetBool("Highlighted", false);
            if (_orientationHighlightedRenderers == null)
                return;
            if (_orientationHighlightBlock == null)
                _orientationHighlightBlock =
                    new UnityEngine.MaterialPropertyBlock();
            _orientationHighlightBlock.Clear();
            for (var i = 0; i < _orientationHighlightedRenderers.Length; i++)
            {
                var renderer = _orientationHighlightedRenderers[i];
                if (renderer != null)
                    renderer.SetPropertyBlock(_orientationHighlightBlock);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _orientationHighlightedRenderers = null;
            if (_orientationDoorOutline != null)
                _orientationDoorOutline.gameObject.SetActive(false);
        }
    }

    private static void TickOfflineOrientationSceneTransition()
    {
        if (!_orientationSceneTransitionInProgress ||
            string.IsNullOrEmpty(_orientationPortalTargetScene) ||
            !_orientationPortalUsePendingAt.HasValue)
            return;

        try
        {
            var target = UnityEngine.SceneManagement.SceneManager
                .GetSceneByName(_orientationPortalTargetScene);
            if (target.IsValid() && target.isLoaded)
            {
                CompleteOfflineOrientationSceneTransition();
                return;
            }

            if (_orientationSceneLoadOperation == null &&
                (DateTime.UtcNow - _orientationPortalUsePendingAt.Value)
                    .TotalSeconds >= 0.35)
            {
                _orientationSceneLoadOperation =
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                        _orientationPortalTargetScene,
                        UnityEngine.SceneManagement.LoadSceneMode.Additive);
                Plugin.Log.LogWarning(
                    $"[ORIENTATION-FLOW] stock portal timed out; loading " +
                    $"bundled '{_orientationPortalTargetScene}' directly");
            }

            if (_orientationSceneLoadOperation != null &&
                _orientationSceneLoadOperation.isDone)
                CompleteOfflineOrientationSceneTransition();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[ORIENTATION-FLOW] scene transition deferred: " +
                e.GetBaseException().Message);
        }
    }

    private static void CompleteOfflineOrientationSceneTransition()
    {
        var source = _orientationPortalSourceScene;
        var targetName = _orientationPortalTargetScene;
        try
        {
            MoveOfflinePlayerToSceneSpawn(targetName);
            if (!string.IsNullOrEmpty(source) &&
                !string.Equals(source, targetName, StringComparison.Ordinal))
            {
                var oldScene = UnityEngine.SceneManagement.SceneManager
                    .GetSceneByName(source);
                if (oldScene.IsValid() && oldScene.isLoaded)
                    UnityEngine.SceneManagement.SceneManager
                        .UnloadSceneAsync(oldScene);
            }
            EnterOfflineOrientationContentScene(targetName);
            Plugin.Log.LogWarning(
                $"[ORIENTATION-FLOW] bundled transition complete " +
                $"'{source}' -> '{targetName}'");
        }
        finally
        {
            _orientationSceneLoadOperation = null;
            _orientationPortalUsePendingAt = null;
            _orientationPortalSourceScene = string.Empty;
            _orientationPortalTargetScene = string.Empty;
            _orientationSceneTransitionInProgress = false;
        }
    }

    private static void MoveOfflinePlayerToSceneSpawn(string sceneName)
    {
        try
        {
            SceneSpawnPoint chosen = null;
            var points = UnityEngine.Resources
                .FindObjectsOfTypeAll<SceneSpawnPoint>();
            if (points != null)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    if (point == null || point.Pointer == IntPtr.Zero ||
                        point.gameObject == null ||
                        !string.Equals(point.gameObject.scene.name, sceneName,
                            StringComparison.Ordinal))
                        continue;
                    chosen = point;
                    if ((point.gameObject.name ?? string.Empty).IndexOf(
                            "Player7SpawnPoint", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
            }

            if (chosen == null)
            {
                Plugin.Log.LogWarning(
                    $"[ORIENTATION-FLOW] no spawn point in '{sceneName}'");
                return;
            }

            var floor = chosen.transform.position;
            var yaw = chosen.transform.eulerAngles.y;
            _spawnFloorY = floor.y;
            _freeCamX = floor.x;
            _freeCamY = floor.y + 1.6f;
            _freeCamZ = floor.z;
            _freeCamYaw = yaw;
            _offlineCameraPitch = 0f;

            var player = FindLiveLocalPlayer();
            if (player != null)
            {
                player.transform.SetPositionAndRotation(
                    floor, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
            }
            Plugin.Log.LogWarning(
                $"[ORIENTATION-FLOW] moved player to '{chosen.gameObject.name}' " +
                $"in '{sceneName}' at {floor}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[ORIENTATION-FLOW] spawn relocation deferred: " +
                e.GetBaseException().Message);
        }
    }

    private static void ApplyOfflineOrientationIntroHandVignette(
        UnityEngine.Camera camera,
        UnityEngine.Quaternion lookRotation)
    {
        if (!string.Equals(_orientationContentScene,
                "Orientation_Scene1", StringComparison.Ordinal) ||
            !_orientationContentEnteredAt.HasValue ||
            _capturedCustomizationPuppet == null)
            return;

        var elapsed = (DateTime.UtcNow - _orientationContentEnteredAt.Value)
            .TotalSeconds;
        if (elapsed < 4.6 || elapsed > 7.5)
            return;

        try
        {
            var left = _capturedCustomizationPuppet.DJMKFFPEAHO;
            var right = _capturedCustomizationPuppet.DFALAJEAEFK;
            if (left == null || right == null)
                return;

            var t = (float)(elapsed - 4.6);
            var rise = Math.Clamp(t / 0.75f, 0f, 1f);
            var wave = (float)Math.Sin(t * 8.5f) * 18f;
            var origin = camera.transform.position;
            var forward = lookRotation * UnityEngine.Vector3.forward;
            var side = lookRotation * UnityEngine.Vector3.right;
            var up = lookRotation * UnityEngine.Vector3.up;
            var downOffset = UnityEngine.Vector3.Lerp(
                up * -0.62f, up * -0.25f, rise);

            left.transform.SetPositionAndRotation(
                origin + forward * 0.68f - side * 0.31f + downOffset,
                lookRotation * UnityEngine.Quaternion.Euler(10f, -12f, -8f));
            right.transform.SetPositionAndRotation(
                origin + forward * 0.68f + side * 0.31f + downOffset,
                lookRotation * UnityEngine.Quaternion.Euler(10f, 12f, wave));
        }
        catch { /* shipped vignette remains the primary animation path */ }
    }

    private static bool TryGetCachedOrientationPlayer(out Player player)
    {
        player = null;
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            (!_localPlayerSpawnStarted &&
             !_localPlayerSpawnSucceededLogged &&
             !_offlineLocomotionReady &&
             !_offlineLocalPlayerLifecyclePublished) ||
            _cachedLocalPlayer == null ||
            _cachedLocalPlayerPtr == IntPtr.Zero ||
            _cachedLocalPlayer.Pointer != _cachedLocalPlayerPtr)
            return false;

        try
        {
            if (_cachedLocalPlayer.gameObject == null)
                return false;
            player = _cachedLocalPlayer;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool OfflineOrientationLocalPlayerPrefix(ref Player __result)
    {
        if (!TryGetCachedOrientationPlayer(out var player))
            return true;
        __result = player;
        return false;
    }

    public static bool OfflineOrientationLocalPlayerExistsPrefix(ref bool __result)
    {
        if (!TryGetCachedOrientationPlayer(out _))
            return true;
        __result = true;
        return false;
    }

    public static bool OfflineOrientationLocalPlayerReadyPrefix(ref bool __result)
    {
        if (!TryGetCachedOrientationPlayer(out _) ||
            (!_offlineLocomotionReady && !_localPlayerSpawnSucceededLogged))
            return true;
        __result = true;
        return false;
    }

    public static bool OfflineOrientationControllerDisplayModePrefix(
        ref JANJMPMDDOG __result)
    {
        if (!TryGetCachedOrientationPlayer(out _))
            return true;
        __result = JANJMPMDDOG.SCREEN;
        return false;
    }

    public static bool OfflineOrientationDeveloperDisplayModePrefix(
        ref GJBOCNCLNPJ __result)
    {
        if (!TryGetCachedOrientationPlayer(out _))
            return true;
        __result = GJBOCNCLNPJ.None;
        return false;
    }

    public static bool OfflineIncompletePlayerUpdatePrefix()
    {
        // Player.Awake did not finish, so its normal Update/name-tag/watch
        // graph dereferences a controller that does not exist. The fallback
        // driver owns those responsibilities for this one local room.
        return !Plugin.DirectOrientationSceneLoad.Value ||
               !_offlinePlayerAwakeFailed;
    }

    public static unsafe void OfflineOrientationPlayerAwakePrefix(Player __instance)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            !_localPlayerSpawnStarted ||
            __instance == null ||
            __instance.Pointer == IntPtr.Zero)
        {
            return;
        }

        // Publish the live instance before stock Awake queries Player.LocalPlayer.
        // This is the Photon-owned player created by our one local spawn path.
        _cachedLocalPlayer = __instance;
        _cachedLocalPlayerPtr = __instance.Pointer;
        _cachedLocalPlayerAt = DateTime.UtcNow;

        // Best-effort repairs: a failure here must not prevent stock Awake from
        // running, or we leave a half-spawned Player with no camera forever.
        // Prior log: Player.Awake NRE with objectModel=0x0 even after install.
        try
        {
            EnsureOfflineObjectModelService();
            EnsureOfflinePlayerSettingsService();
            EnsureOfflinePlayerRegistry();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-AWAKE] dependency reassert failed: " +
                $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
        }

        try
        {
            EnsureOfflinePlayerToolEquipSlotsInitialized(__instance);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-AWAKE] tool-equip pre-init failed: " +
                $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
        }

        try
        {
            EnsureOfflinePlayerAvatarInitialized(__instance);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-AWAKE] avatar pre-init failed: " +
                $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
        }

        if (_offlinePlayerAwakeDiagnosticsLogged)
            return;

        _offlinePlayerAwakeDiagnosticsLogged = true;
        try
        {
            var playerClass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "Player");
            var requiredFields = new[]
            {
                "trackingSpace", "head", "body", "leftHand", "rightHand",
                "hideInFirstPerson", "voiceObject", "toolEquipSlots",
                "localPlayerOnlyObjects", "remotePlayerOnlyObjects",
                "animateInOut", "playerAudio", "playerAvatar", "playerUI",
                "playerNameTag", "playerParty", "playerModeration",
                "dailiesObjectiveTracker", "playerProgression", "playerEmotes",
                "playerEvents", "playerMovement", "playerMovementFeedback",
                "backpack", "playerChaperone", "playerPersonalSpace",
                "synchedData", "avatarSkeleton", "updateLODSystem",
                "respawnLoopDetector",
            };

            var missing = new List<string>();
            foreach (var fieldName in requiredFields)
            {
                if (ReadIl2CppReferenceField(
                        __instance.Pointer, playerClass, fieldName) == IntPtr.Zero)
                {
                    missing.Add(fieldName);
                }
            }

            var rootPointer = InvokeNative(
                "KMHLGEMLKMO", "KFFEIKCJKKF", IntPtr.Zero, null, 0,
                "RecRoom.AgInitialization.Runtime.dll");
            var objectModel = AccessTools.Method(
                    typeof(RecRoom.ObjectModel.ObjectModelManager),
                    "HPGAKFKICFA",
                    Type.EmptyTypes)
                ?.Invoke(null, null) as Il2CppSystem.Object;
            var photonPeer = DBMCMCHBCII.GANDAMAEOGN;
            var playerRegistry = BLACGKAKJIG.KGGJIHLJBIH;

            Plugin.Log.LogWarning(
                "[PLAYER-AWAKE] prefab dependency snapshot " +
                $"missing=[{string.Join(",", missing)}] " +
                $"root=0x{rootPointer.ToInt64():X} " +
                $"objectModel=0x{(objectModel?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                $"photonPeer=0x{(photonPeer?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                $"playerRegistry=0x{(playerRegistry?.Pointer ?? IntPtr.Zero).ToInt64():X}");
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogWarning(
                $"[PLAYER-AWAKE] dependency snapshot failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    public static Exception OfflineOrientationPlayerAwakeFinalizer(
        Player __instance,
        Exception __exception)
    {
        if (__exception == null ||
            !Plugin.DirectOrientationSceneLoad.Value ||
            !_localPlayerSpawnStarted ||
            __instance == null ||
            __instance.Pointer == IntPtr.Zero)
        {
            return __exception;
        }

        try
        {
            var playerClass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "Player");
            var avatarPointer = ReadIl2CppReferenceField(
                __instance.Pointer, playerClass, "playerAvatar");
            var avatarClass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "PlayerAvatar");
            var account = KCBFNPPKMAB.NJFGOALHCGK;
            var objectModelValue = AccessTools.Method(
                    typeof(RecRoom.ObjectModel.ObjectModelManager),
                    "HPGAKFKICFA",
                    Type.EmptyTypes)
                ?.Invoke(null, null);
            var objectModelPointer = GetManagedIl2CppPointer(objectModelValue);

            Plugin.Log.LogError(
                "[PLAYER-AWAKE] original Awake failed after dependency repair " +
                $"error={__exception.GetBaseException().GetType().Name}:" +
                $"{__exception.GetBaseException().Message} " +
                $"settings=0x{ReadIl2CppReferenceField(__instance.Pointer, playerClass, "FEJPFDBPCJD").ToInt64():X} " +
                $"objectView=0x{ReadIl2CppReferenceField(__instance.Pointer, playerClass, "<JGKHKMOBGJM>k__BackingField").ToInt64():X} " +
                $"avatar=0x{avatarPointer.ToInt64():X} " +
                $"avatarDisplay=0x{ReadIl2CppReferenceField(avatarPointer, avatarClass, "playerAvatarDisplay").ToInt64():X} " +
                $"avatarPlayer=0x{ReadIl2CppReferenceField(avatarPointer, avatarClass, "player").ToInt64():X} " +
                $"avatarSync=0x{ReadIl2CppReferenceField(avatarPointer, avatarClass, "FBDLPKNCKLJ").ToInt64():X} " +
                $"account=0x{(account?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                $"objectModel=0x{objectModelPointer.ToInt64():X} " +
                $"camera=0x{(UnityEngine.Camera.main?.Pointer ?? IntPtr.Zero).ToInt64():X}");
        }
        catch (Exception diagnosticError)
        {
            var root = diagnosticError.GetBaseException();
            Plugin.Log.LogError(
                $"[PLAYER-AWAKE] final dependency snapshot failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }

        // Awake often dies offline before desktop camera/input attach. Swallow
        // the NRE so the Photon/LPC player remains live. Do NOT call heavy
        // Unity APIs here (finalizer runs mid-Awake and hard-crashes the process).
        // Camera/env repair is deferred to the Update pump via these flags.
        _postSpawnRepairUntil = DateTime.UtcNow.AddSeconds(15);
        _postSpawnRepairNextAt = DateTime.UtcNow.AddSeconds(0.25);
        _offlineCameraRecoveryAttempted = false;
        _offlinePlayerAwakeFailed = true;
        Plugin.Log.LogWarning(
            "[PLAYER-AWAKE] swallowed Awake NRE; scheduled deferred camera/env recovery");
        return null;
    }

    public static bool OfflineOrientationPlayerAvatarAwakePrefix(
        PlayerAvatar __instance)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            !_localPlayerSpawnStarted ||
            __instance == null ||
            __instance.Pointer == IntPtr.Zero)
        {
            return true;
        }

        var klass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "PlayerAvatar");
        var initialized = ReadIl2CppReferenceField(
            __instance.Pointer, klass, "FBDLPKNCKLJ") != IntPtr.Zero;
        if (initialized && _offlinePlayerAvatarInitializationInProgress)
            return false;
        if (initialized && _offlinePlayerAvatarInitializedEarly)
            return false;
        return true;
    }

    private static void EnsureOfflinePlayerAvatarInitialized(Player player)
    {
        var playerClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "Player");
        var avatarPointer = ReadIl2CppReferenceField(
            player.Pointer, playerClass, "playerAvatar");
        if (avatarPointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "The stock Player prefab has no PlayerAvatar component.");

        var avatar = new PlayerAvatar(avatarPointer);
        var avatarClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "PlayerAvatar");
        if (avatarClass == IntPtr.Zero)
            throw new InvalidOperationException(
                "PlayerAvatar IL2CPP class was not found.");

        if (ReadIl2CppReferenceField(
                avatar.Pointer, avatarClass, "FBDLPKNCKLJ") != IntPtr.Zero)
        {
            return;
        }

        var missing = new List<string>();
        foreach (var fieldName in new[] { "playerAvatarDisplay", "player" })
        {
            if (ReadIl2CppReferenceField(
                    avatar.Pointer, avatarClass, fieldName) == IntPtr.Zero)
            {
                missing.Add(fieldName);
            }
        }
        if (missing.Count != 0)
        {
            throw new InvalidOperationException(
                "The stock PlayerAvatar prefab is missing serialized fields: " +
                string.Join(",", missing));
        }

        var awake = AccessTools.Method(
            typeof(PlayerAvatar), "Awake", Type.EmptyTypes);
        if (awake == null)
            throw new InvalidOperationException(
                "PlayerAvatar.Awake was not found.");

        _offlinePlayerAvatarInitializationInProgress = true;
        try
        {
            awake.Invoke(avatar, null);
        }
        finally
        {
            _offlinePlayerAvatarInitializationInProgress = false;
        }

        if (ReadIl2CppReferenceField(
                avatar.Pointer, avatarClass, "FBDLPKNCKLJ") == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "The stock PlayerAvatar did not finish initialization.");
        }

        _offlinePlayerAvatarInitializedEarly = true;
        if (!_offlinePlayerAvatarReadyLogged)
        {
            _offlinePlayerAvatarReadyLogged = true;
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] initialized the stock PlayerAvatar before " +
                "Player.Awake to restore avatar, animation, and camera ordering");
        }
    }

    public static bool OfflineOrientationToolEquipSlotsAwakePrefix(
        RecRoom.Core.PlayerToolEquipSlots __instance)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            !_localPlayerSpawnStarted ||
            __instance == null ||
            __instance.Pointer == IntPtr.Zero)
        {
            return true;
        }

        var klass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll",
            "RecRoom.Core",
            "PlayerToolEquipSlots");
        var initialized = ReadIl2CppReferenceField(
            __instance.Pointer, klass, "JEMMHHLNFFI") != IntPtr.Zero;
        if (initialized && _offlineToolEquipSlotsInitializationInProgress)
            return false;
        if (initialized && _offlineToolEquipSlotsInitializedEarly)
            return false;
        return true;
    }

    private static void EnsureOfflinePlayerToolEquipSlotsInitialized(
        Player player)
    {
        var slots = player.BGGALKJCGNM;
        if (slots == null || slots.Pointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "The stock Player prefab has no PlayerToolEquipSlots component.");

        var klass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll",
            "RecRoom.Core",
            "PlayerToolEquipSlots");
        if (klass == IntPtr.Zero)
            throw new InvalidOperationException(
                "PlayerToolEquipSlots IL2CPP class was not found.");

        if (ReadIl2CppReferenceField(
                slots.Pointer, klass, "JEMMHHLNFFI") != IntPtr.Zero)
        {
            return;
        }

        var awake = AccessTools.Method(
            typeof(RecRoom.Core.PlayerToolEquipSlots),
            "Awake",
            Type.EmptyTypes);
        if (awake == null)
            throw new InvalidOperationException(
                "PlayerToolEquipSlots.Awake was not found.");

        _offlineToolEquipSlotsInitializationInProgress = true;
        try
        {
            awake.Invoke(slots, null);
        }
        finally
        {
            _offlineToolEquipSlotsInitializationInProgress = false;
        }

        if (ReadIl2CppReferenceField(
                slots.Pointer, klass, "JEMMHHLNFFI") == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "The stock PlayerToolEquipSlots did not finish initialization.");
        }

        _offlineToolEquipSlotsInitializedEarly = true;
        if (!_offlineToolEquipSlotsReadyLogged)
        {
            _offlineToolEquipSlotsReadyLogged = true;
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] initialized the stock PlayerToolEquipSlots " +
                "before Player.Awake to preserve the normal role/equipment order");
        }
    }

    private static unsafe IntPtr ReadIl2CppReferenceField(
        IntPtr instance,
        IntPtr klass,
        string fieldName)
    {
        if (instance == IntPtr.Zero || klass == IntPtr.Zero)
            return IntPtr.Zero;

        var field = IL2CPP.il2cpp_class_get_field_from_name(klass, fieldName);
        if (field == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr value = IntPtr.Zero;
        IL2CPP.il2cpp_field_get_value(instance, field, &value);
        return value;
    }

    private static IntPtr GetManagedIl2CppPointer(object value)
    {
        if (value == null)
            return IntPtr.Zero;

        try
        {
            var property = value.GetType().GetProperty("Pointer");
            if (property?.GetValue(value) is IntPtr pointer)
                return pointer;
        }
        catch
        {
            // This is diagnostic-only and must never interfere with Player.Awake.
        }

        return IntPtr.Zero;
    }

    public static bool OfflineOrientationJoinedRoomLabelPrefix(
        ref string __result)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            (!_localPlayerSpawnDueAt.HasValue && !_localPlayerSpawnStarted))
        {
            return true;
        }

        // The production lookup indexes a cloud-populated room-label map. That
        // map is empty in the local Photon room and its KeyNotFoundException
        // aborts the rest of OnJoinedRoom callbacks, including player-role
        // setup. The local room's stable label is sufficient for those hooks.
        __result = "FluxRecOrientation";
        if (!_offlineJoinedRoomLabelLogged)
        {
            _offlineJoinedRoomLabelLogged = true;
            Plugin.Log.LogWarning(
                "[PHOTON-ROOM] supplied local room label to stock joined-room callbacks");
        }
        return false;
    }

    public static bool StatsigGatePrefix(ref bool __result)
    {
        // The production Statsig singleton is intentionally absent on a local
        // server. Feature checks must fall back to disabled instead of throwing
        // while avatar cards and their addressable thumbnails are being bound.
        __result = false;
        return false;
    }

    public static bool RecRoomStatsigGatePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    public static bool RecRoomStatsigExperimentPrefix(ref HPFBDLJHKJO __result)
    {
        __result = HPFBDLJHKJO.OKLBJLNFLNK;
        return false;
    }

    public static bool RecRoomStatsigLayerPrefix(ref KGLGGNKCODP __result)
    {
        __result = KGLGGNKCODP.OKLBJLNFLNK;
        return false;
    }

    public static bool AvatarItemResourceKeyPrefix(
        ref RecRoom.Avatars.Data.Runtime.AvatarItem __instance,
        ref string __result)
    {
        // AvatarItem is an IL2CPP value type. Harmony must pass the instance by
        // reference or the null guard can inspect a detached/default wrapper
        // while the native method still receives an empty value. A visual-only
        // legacy item can still use its exact bundled prefab/material descriptor;
        // only a completely empty item has no safe resource key.
        var visualData = __instance?.AvatarItemVisualData;
        if (visualData == null)
        {
            __result = string.Empty;
            return false;
        }

        if (__instance.AvatarItemData != null)
            return true;

        try
        {
            __result = visualData.IOLEJOEOLFJ() ?? string.Empty;
        }
        catch
        {
            __result = string.Empty;
        }
        return false;
    }

    // The generated RecNet nameserver path reports the local HEAD probe as 403
    // even though the redirected request receives a valid response. Treat only
    // those gateway-style statuses as successful so the normal parser can run.
    public static void NameserverStatusPostfix(ref int __result)
    {
        // Do not touch the response object here. Reading its payload fields from
        // this postfix crashed the game on startup - it runs while the object is
        // still being populated. What it would have told us is already known:
        // the RecNet layer receives status 0 with a null body even though the
        // transport hook saw a 200 with the full service map.
        if (__result == 403 || __result == 405)
            __result = 200;
    }

    // CNCONLNMEIA(response, bool) -> bool sits between the response arriving and
    // the bootstrap branching to either CABDIKHOLFK (stores the service map) or
    // HPGGFJHCJKF (the "RecNet name server query failed" error handler). It was
    // previously forced to TRUE on the assumption that it means "accept this
    // response". The observed behaviour says otherwise: the transport delivers a
    // clean 200 with the full map, the conversion lambda runs, and yet the
    // service dictionary stays empty and every lookup throws KeyNotFound - which
    // is what a permanently-taken error branch looks like. So the sense is more
    // likely "has error". Default to passthrough and let the game decide.
    public static bool NameserverValidationPrefix(ref bool __result)
    {
        var mode = (Plugin.NameserverGateMode.Value ?? "passthrough").Trim();

        if (!_nameserverValidationLogged)
        {
            _nameserverValidationLogged = true;
            Plugin.Log.LogWarning($"[NS] response gate reached; mode={mode}");
        }

        if (mode.Equals("force-true", StringComparison.OrdinalIgnoreCase))
        {
            __result = true;
            return false;
        }

        if (mode.Equals("force-false", StringComparison.OrdinalIgnoreCase))
        {
            __result = false;
            return false;
        }

        return true;
    }

    // Which of these two fires is the whole question: one stores the service map,
    // the other is the failure path that leaves it empty.
    public static void NameserverSuccessPrefix(HIBHFHKEMCJ __1)
    {
        if (_nameserverSuccessLogged)
            return;
        _nameserverSuccessLogged = true;

        // Safe to read here, unlike in the status postfix: by the time the
        // success handler runs the record is fully constructed. This
        // distinguishes "conversion produced an empty record" from "record is
        // fine but the JSON shape does not deserialize".
        var detail = "<unreadable>";
        try
        {
            var status = __1 == null ? -1 : __1.CHEPHPEPILO;
            var body = __1?.ODAKPEOCFDN;
            var err = __1?.KAHAGJOAFAH;
            var ok = __1?.AIDBDOOGGPH;
            detail =
                $"status={status} ok={ok} err='{err}' " +
                $"bodyLen={(body == null ? -1 : body.Length)} " +
                $"body='{(string.IsNullOrEmpty(body) ? "" : body.Substring(0, Math.Min(body.Length, 200)))}'";
        }
        catch (Exception e)
        {
            detail = $"<threw {e.GetBaseException().GetType().Name}>";
        }

        Plugin.Log.LogWarning($"[NS] SUCCESS handler reached; response {detail}");
    }

    // Runs after the stock success handler has had its go, so anything it does
    // manage to store is kept and only the missing entries are filled in.
    public static void NameserverSuccessPostfix()
    {
        if (_serviceMapInstalled)
            return;
        _serviceMapInstalled = true;

        if (!Plugin.InstallServiceMap.Value)
            return;

        try
        {
            PopulateRecNetServiceMap();
        }
        catch (Exception e)
        {
            _serviceMapInstalled = false;
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[RECNET-MAP] could not install the service map: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    public static void NameserverFailurePrefix(string __3)
    {
        if (_nameserverFailureLogged)
            return;
        _nameserverFailureLogged = true;
        Plugin.Log.LogError(
            $"[NS] FAILURE handler reached - service map will stay empty. reason='{__3}'");
    }

    public static void PromiseReadyPrefix(ref bool __result)
    {
        // The native bootstrap waits on this promise after parsing the service
        // map. A locally supplied map has no production telemetry completion,
        // so mark the wait state ready instead of leaving the loading scene.
        __result = true;
    }

    public static void LoginLoadedPrefix(ref bool __result)
    {
        __result = true;
    }

    private static void PatchTitleRouteGetter(Harmony harmony, string property, string prefix)
    {
        var getter = AccessTools.Method(typeof(RRUI.Data.TitleScreenFlowModel), "get_" + property, Type.EmptyTypes);
        if (getter != null)
            harmony.Patch(getter, prefix: new HarmonyMethod(typeof(SendRequestPatch), prefix));
    }

    public static void ForceNotLoadingPrefix(ref bool __result) => __result = false;
    public static void ForceNoCachedAccountPrefix(ref bool __result) => __result = false;
    public static void ForceNoAccountCreationPrefix(ref bool __result) => __result = false;

    public static void TitleStartPostfix(TitleScreenManager __instance)
    {
        try
        {
            var model = __instance?.flowModel;
            if (model != null)
            {
                model.GoToLogin();
                _pendingGameLaunchModel = null;
                _pendingGameLaunchStartedAt = null;
                _bootSequenceFallbackStartedAt = null;
                _bootSequenceFallbackOrientation = false;
                ResetLocalMatchmakingLaunchState();
                _dispatchingNativeGameLaunch = false;
                Plugin.Log.LogInfo("[BOOTSTRAP] forced title flow to login");
                // Auto-create is the normal private-server path. Env flags still
                // force it on for unattended CI even if the config knob is off.
                _validationAccountLaunchEnabled =
                    Plugin.AutoCreateAccountAndLaunch.Value ||
                    string.Equals(
                        Environment.GetEnvironmentVariable("RECNET_VALIDATE_ACCOUNT_LAUNCH"),
                        "1",
                        StringComparison.Ordinal);
                var avatarOnlyProbe = string.Equals(
                    Environment.GetEnvironmentVariable("RECNET_VALIDATE_AVATAR"),
                    "1",
                    StringComparison.Ordinal);
                if (_validationAccountLaunchEnabled || avatarOnlyProbe)
                {
                    _validationProbeStage = 0;
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(3);
                    Plugin.Log.LogInfo(
                        _validationAccountLaunchEnabled
                            ? "[VALIDATION] auto account create + Orientation launch scheduled"
                            : "[VALIDATION] scheduled one-shot avatar screen check");
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[BOOTSTRAP] login handoff failed: {e.Message}");
        }
    }

    public static void AccountCreationPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        _accountCreationStartedAt = DateTime.UtcNow;
        _localRegistrationTask = null;
        _localCredentialLoginTask = null;
        _localAccountLoadTask = null;
        _launchCreatedAccountAfterAuth = false;
        _registrationLoginInProgress = false;
        _localRegistrationUsername = string.Empty;
        _localRegistrationPassword = string.Empty;
        _pendingGameLaunchModel = null;
        _pendingGameLaunchStartedAt = null;
        _bootSequenceFallbackStartedAt = null;
        _bootSequenceFallbackOrientation = false;
        ResetLocalMatchmakingLaunchState();
        _dispatchingNativeGameLaunch = false;
        _avatarPreviewRefreshAt = null;
        _avatarPreviewRefreshed = false;
        _avatarPreviewRefreshAttempts = 0;
        _nativeAvatarReferenceRebound = false;
        Plugin.Log.LogInfo($"[AUTH] account creation started state={AuthState(__instance)}");
    }

    public static void AccountCreationPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        Plugin.Log.LogInfo($"[AUTH] account creation bootstrap returned state={AuthState(__instance)}");
    }

    public static bool ManualLoginPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        _manualLoginStartedAt = DateTime.UtcNow;
        Plugin.Log.LogInfo($"[AUTH] ManuallyLogin invoked state={AuthState(__instance)}");

        var username = __instance.BIIMFMGPLHB?.Trim() ?? string.Empty;
        var password = __instance.HKCOLMHPAAD ?? string.Empty;
        BeginManagedCredentialLogin(__instance, username, password, false);

        // This exact depot's native ManualLoginInternal chain waits forever for
        // retired platform proof before it can call /connect/token. The local
        // login above replaces that dead segment. The native token installer
        // remains authoritative; account/me is fetched with the installed
        // token and copied into the native account cache on Unity's main thread.
        return false;
    }

    public static void ManualLoginPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        Plugin.Log.LogInfo($"[AUTH] managed ManuallyLogin handoff returned state={AuthState(__instance)}");
    }

    private static void BeginManagedCredentialLogin(
        RRUI.Data.TitleScreenFlowModel model,
        string username,
        string password,
        bool launchCreatedAccount)
    {
        if (_localCredentialLoginTask != null || _localAccountLoadTask != null)
        {
            Plugin.Log.LogInfo("[AUTH] local credential login is already in progress");
            return;
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            model.FLACNNLAPNN = "Enter your username or email and password.";
            _manualLoginStartedAt = null;
            _registrationLoginInProgress = false;
            Plugin.Log.LogWarning("[AUTH] local credential login was missing username or password");
            return;
        }

        try
        {
            ShowCredentialLoadingScreen(model);
            _registrationLoginInProgress = launchCreatedAccount;
            _launchCreatedAccountAfterAuth = launchCreatedAccount;
            var endpoint = Plugin.ServerHostname.Value.TrimEnd('/') + "/connect/token";
            _localCredentialLoginTask =
                LoginLocalAccountAsync(endpoint, username.Trim(), password);
            Plugin.Log.LogInfo(
                $"[AUTH] local credential login started username={username.Trim()} newAccount={launchCreatedAccount}");
        }
        catch (Exception e)
        {
            _localCredentialLoginTask = null;
            _registrationLoginInProgress = false;
            _launchCreatedAccountAfterAuth = false;
            _manualLoginStartedAt = null;
            model.FLACNNLAPNN = "Login could not be started.";
            model.GoToLogin();
            Plugin.Log.LogWarning(
                $"[AUTH] could not start local credential login: {e.GetBaseException().Message}");
        }
    }

    private static void ShowCredentialLoadingScreen(
        RRUI.Data.TitleScreenFlowModel model)
    {
        try
        {
            var browser = model.AHHCCKDCIAH;
            var page = model.switchAccountLoadingScreenPage;
            if (browser != null && page != null)
                browser.GoTo(page.Route);
        }
        catch (Exception e)
        {
            // Authentication can still complete if this damaged title prefab is
            // missing its optional loading route.
            Plugin.Log.LogWarning($"[AUTH] login loading screen unavailable: {e.Message}");
        }
    }

    private static async Task<LocalLoginResult> LoginLocalAccountAsync(
        string endpoint,
        string username,
        string password)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(35),
            };
            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = username,
                    ["password"] = password,
                    ["client_id"] = "recroom",
                    ["client_secret"] = "VxZ53kgbbEaRoZAeMe00MagtgD12GLL2",
                });
            using var response = await client.PostAsync(endpoint, content).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                var error = ReadJsonString(body, "detail", "error_description", "error");
                if (string.IsNullOrWhiteSpace(error))
                    error = $"Local account server returned HTTP {statusCode}.";
                return LocalLoginResult.Failed(statusCode, error);
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var accessToken = ReadJsonString(
                root,
                "access_token",
                "accessToken",
                "AccessToken",
                "Token");
            var refreshToken = ReadJsonString(
                root,
                "refresh_token",
                "refreshToken",
                "RefreshToken");
            var key = ReadJsonString(root, "key", "Key");
            if (string.IsNullOrWhiteSpace(accessToken) ||
                string.IsNullOrWhiteSpace(refreshToken) ||
                string.IsNullOrWhiteSpace(key))
            {
                return LocalLoginResult.Failed(
                    statusCode,
                    "The local account server returned an incomplete login token.");
            }

            return LocalLoginResult.Succeeded(statusCode, accessToken, refreshToken, key);
        }
        catch (TaskCanceledException)
        {
            return LocalLoginResult.Failed(
                0,
                "The local account server did not finish login in time.");
        }
        catch (Exception e)
        {
            return LocalLoginResult.Failed(
                0,
                $"Could not reach the local account server: {e.GetBaseException().Message}");
        }
    }

    private static string ReadJsonString(string body, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return ReadJsonString(document.RootElement, names);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ReadJsonString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool TryInstallNativeToken(
        LocalLoginResult result,
        out string error)
    {
        try
        {
            var installer = AccessTools.Method(
                typeof(JCAIPKPAAFO),
                "ICKJNCMAHNO",
                new[] { typeof(string), typeof(string), typeof(string) });
            if (installer == null)
            {
                error = "The native token installer was not found.";
                return false;
            }

            // Native LoginHelper passes the response fields at +0x20, +0x28,
            // and +0x30 in this exact order. Disassembly confirms those are
            // access token, refresh token, and the base64 token-encryption key.
            installer.Invoke(
                null,
                new object[] { result.AccessToken, result.RefreshToken, result.Key });
            if (!JCAIPKPAAFO.KDCCDBOHJCH)
            {
                error = "The game rejected the installed access token.";
                return false;
            }

            error = string.Empty;
            return true;
        }
        catch (Exception e)
        {
            error = $"The game could not install the access token: {e.GetBaseException().Message}";
            return false;
        }
    }

    private static async Task<LocalAccountResult> LoadLocalAccountAsync(
        string endpoint,
        string accessToken)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20),
            };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                var error = ReadJsonString(body, "detail", "error_description", "error");
                if (string.IsNullOrWhiteSpace(error))
                    error = $"Local account server returned HTTP {statusCode}.";
                return LocalAccountResult.Failed(statusCode, error);
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var accountId = ReadJsonInt32(root, "accountId", "AccountId", "AccountID", "Id", "UserId", "PlayerId");
            var username = ReadJsonString(root, "username", "Username");
            var displayName = ReadJsonString(root, "displayName", "DisplayName");
            var profileImage = ReadJsonString(root, "profileImage", "PhotoUrl");
            var email = ReadJsonString(root, "email", "Email");
            if (accountId <= 0 || string.IsNullOrWhiteSpace(username))
            {
                return LocalAccountResult.Failed(
                    statusCode,
                    "The local account server returned an incomplete account profile.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = username;
            return LocalAccountResult.Succeeded(
                statusCode,
                new LocalAccountProfile(
                    accountId,
                    username,
                    displayName,
                    profileImage,
                    email,
                    ReadJsonBoolean(root, false, "isJunior", "IsJunior"),
                    ReadJsonInt32(root, "platforms", "Platforms"),
                    ReadJsonInt32(root, "personalPronouns", "PersonalPronouns"),
                    ReadJsonInt32(root, "identityFlags", "IdentityFlags"),
                    Math.Max(
                        0,
                        ReadJsonInt32(
                            root,
                            "availableUsernameChanges",
                            "AvailableUsernameChanges"))));
        }
        catch (TaskCanceledException)
        {
            return LocalAccountResult.Failed(
                0,
                "The local account server did not finish loading the account profile in time.");
        }
        catch (Exception e)
        {
            return LocalAccountResult.Failed(
                0,
                $"Could not load the signed-in account: {e.GetBaseException().Message}");
        }
    }

    private static int ReadJsonInt32(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric))
                return numeric;
            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), out numeric))
                return numeric;
        }

        return 0;
    }

    private static bool ReadJsonBoolean(
        JsonElement root,
        bool fallback,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.True)
                return true;
            if (value.ValueKind == JsonValueKind.False)
                return false;
            if (value.ValueKind == JsonValueKind.String &&
                bool.TryParse(value.GetString(), out var boolean))
                return boolean;
        }

        return fallback;
    }

    private static bool TryInstallNativeAccount(
        LocalAccountProfile profile,
        out string error)
    {
        var stage = "constructing the native account";
        try
        {
            var account = new AJFFLHOACDK(new ObscuredInt(profile.AccountId));

            // The base account normalizer requires all non-optional obscured
            // string fields to exist. In particular, the bio field at native
            // offset 0x48 must be an empty ObscuredString rather than null.
            // If normalization aborts, the account-change event never reaches
            // the player-preferences bootstrap and the title flow later dies
            // on a blank screen while checking the Orientation tutorial.
            stage = "setting required native account fields";
            account.IIMCLNELAMI = new ObscuredString(profile.Username);
            account.PAJJODPNBON = new ObscuredString(profile.Username);
            account.AFNODECBFEC = new ObscuredString(profile.DisplayName);
            account.EMCCGEMJCEG = new ObscuredString(string.Empty);
            account.BCBDKBEHGBN = new ObscuredBool(profile.IsJunior);

            TrySetOptionalNativeAccountField(
                "profile image",
                () => account.APCMGDHJGLJ = new ObscuredString(profile.ProfileImage));
            TrySetOptionalNativeAccountField(
                "country",
                () => account.KHOPHKENBIF = new ObscuredString("US"));
            TrySetOptionalNativeAccountField(
                "email",
                () => account.FCFPJEFJJAP = new ObscuredString(profile.Email));
            TrySetOptionalNativeAccountField(
                "platforms",
                () => account.BJBBADKELOO = (JNPLFFEBPAK)profile.Platforms);
            TrySetOptionalNativeAccountField(
                "personal pronouns",
                () => account.PBHHHCHLJEO = (MADDPANHBOJ)profile.PersonalPronouns);
            TrySetOptionalNativeAccountField(
                "identity flags",
                () => account.IJIJMIJHBLP = (JBAFLDLEKLK)profile.IdentityFlags);
            TrySetOptionalNativeAccountField(
                "username changes",
                () => account.PLGNHACHDCE = profile.AvailableUsernameChanges);

            stage = "normalizing the native account";
            try
            {
                account.KCDABJLACCF();
                Plugin.Log.LogInfo("[AUTH] native account normalization completed");
            }
            catch (Exception normalizationError)
            {
                // Native JSON deserialization normally performs this callback,
                // but its optional service hooks are not required for title
                // launch. Keep the valid core account and continue.
                Plugin.Log.LogWarning(
                    $"[AUTH] native account normalization skipped: " +
                    $"{normalizationError.GetBaseException().GetType().Name}: " +
                    $"{normalizationError.GetBaseException().Message}");
            }

            stage = "publishing the native account cache";
            try
            {
                KCBFNPPKMAB.NJFGOALHCGK = account;
                Plugin.Log.LogInfo("[AUTH] native account cache event setter completed");
            }
            catch (Exception cacheEventError)
            {
                Plugin.Log.LogWarning(
                    $"[AUTH] native account cache event setter failed; using backing cache: " +
                    $"{cacheEventError.GetBaseException().GetType().Name}: " +
                    $"{cacheEventError.GetBaseException().Message}");
            }

            if (KCBFNPPKMAB.NJFGOALHCGK == null)
            {
                stage = "publishing the native account backing cache";
                KCBFNPPKMAB.OPAIAMGODNB = account;
            }

            if (KCBFNPPKMAB.NJFGOALHCGK == null)
            {
                error = "The game rejected the loaded account profile.";
                return false;
            }

            Plugin.Log.LogInfo(
                $"[AUTH] native account cache installed accountId={profile.AccountId} " +
                $"username={profile.Username}");
            error = string.Empty;
            return true;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            error =
                $"The game could not cache the account profile while {stage}: " +
                $"{root.GetType().Name}: {root.Message}";
            return false;
        }
    }

    private static void TrySetOptionalNativeAccountField(
        string fieldName,
        Action setter)
    {
        try
        {
            setter();
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogWarning(
                $"[AUTH] optional native account field '{fieldName}' skipped: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    private static void FailManagedLogin(
        RRUI.Data.TitleScreenFlowModel model,
        string message,
        string logStage)
    {
        _localCredentialLoginTask = null;
        _localAccountLoadTask = null;
        _launchCreatedAccountAfterAuth = false;
        _registrationLoginInProgress = false;
        _manualLoginStartedAt = null;
        var displayMessage = string.IsNullOrWhiteSpace(message)
            ? "Login did not complete."
            : message;
        Plugin.Log.LogWarning($"[AUTH] {logStage}: {displayMessage}");
        model.GoToLogin();
        model.FLACNNLAPNN = displayMessage;
    }

    private sealed class LocalLoginResult
    {
        private LocalLoginResult(
            bool success,
            int statusCode,
            string error,
            string accessToken,
            string refreshToken,
            string key)
        {
            Success = success;
            StatusCode = statusCode;
            Error = error;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            Key = key;
        }

        public bool Success { get; }
        public int StatusCode { get; }
        public string Error { get; }
        public string AccessToken { get; }
        public string RefreshToken { get; }
        public string Key { get; }

        public static LocalLoginResult Succeeded(
            int statusCode,
            string accessToken,
            string refreshToken,
            string key) =>
            new(true, statusCode, string.Empty, accessToken, refreshToken, key);

        public static LocalLoginResult Failed(int statusCode, string error) =>
            new(false, statusCode, error, string.Empty, string.Empty, string.Empty);
    }

    private sealed class LocalAccountProfile
    {
        public LocalAccountProfile(
            int accountId,
            string username,
            string displayName,
            string profileImage,
            string email,
            bool isJunior,
            int platforms,
            int personalPronouns,
            int identityFlags,
            int availableUsernameChanges)
        {
            AccountId = accountId;
            Username = username;
            DisplayName = displayName;
            ProfileImage = profileImage;
            Email = email;
            IsJunior = isJunior;
            Platforms = platforms;
            PersonalPronouns = personalPronouns;
            IdentityFlags = identityFlags;
            AvailableUsernameChanges = availableUsernameChanges;
        }

        public int AccountId { get; }
        public string Username { get; }
        public string DisplayName { get; }
        public string ProfileImage { get; }
        public string Email { get; }
        public bool IsJunior { get; }
        public int Platforms { get; }
        public int PersonalPronouns { get; }
        public int IdentityFlags { get; }
        public int AvailableUsernameChanges { get; }
    }

    private sealed class LocalAccountResult
    {
        private LocalAccountResult(
            bool success,
            int statusCode,
            string error,
            LocalAccountProfile profile)
        {
            Success = success;
            StatusCode = statusCode;
            Error = error;
            Profile = profile;
        }

        public bool Success { get; }
        public int StatusCode { get; }
        public string Error { get; }
        public LocalAccountProfile Profile { get; }

        public static LocalAccountResult Succeeded(
            int statusCode,
            LocalAccountProfile profile) =>
            new(true, statusCode, string.Empty, profile);

        public static LocalAccountResult Failed(int statusCode, string error) =>
            new(false, statusCode, error, null);
    }

    public static void SubmitEmailPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        Plugin.Log.LogInfo($"[AUTH] SubmitEmail invoked state={AuthState(__instance)}");
    }

    public static void SubmitEmailPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        Plugin.Log.LogInfo($"[AUTH] SubmitEmail returned state={AuthState(__instance)}");
    }

    public static bool AccountCreationBirthdayIsValidPrefix(ref bool __result)
    {
        // Stock validity reads the date picker + preference store. On this
        // private-server path the store is uninitialized and the picker often
        // never commits, so Next stays disabled forever. Force valid.
        __result = true;
        return false;
    }

    public static bool BirthdayNextButtonPrefix(
        RRUI.Data.TitleScreenFlowModelController.SubmitAccountCreationBirthdayAndGoToNextButtonImpl __instance)
    {
        try
        {
            var model = __instance?.Model;
            if (model == null)
                return true;

            EnsureAdultAccountCreationBirthday(model);
            model.SubmitAccountCreationBirthdayAndGoToNext();
            return false;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[AUTH] birthday Next button failed: {e.GetBaseException().Message}");
            return true;
        }
    }

    private static void EnsureAdultAccountCreationBirthday(RRUI.Data.TitleScreenFlowModel model)
    {
        if (model == null)
            return;

        // 2000-06-15: clearly 18+ so junior/age gates do not block the flow.
        // Interop exposes get_/set_ methods; the C# property name is not always generated.
        var adult = new Il2CppSystem.DateTime(2000, 6, 15, 0, 0, 0, 0);
        var setter = AccessTools.Method(
            typeof(RRUI.Data.TitleScreenFlowModel),
            "set_AccountCreationBirthday",
            new[] { typeof(Il2CppSystem.DateTime) });
        if (setter != null)
        {
            setter.Invoke(model, new object[] { adult });
            return;
        }

        // Fallback: private Nullable<DateTime> writer observed in this depot.
        var alt = AccessTools.Method(
            typeof(RRUI.Data.TitleScreenFlowModel),
            "HIADMEIJBCF");
        alt?.Invoke(model, new object[] { adult });
    }

    public static bool BirthdaySubmitPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        try
        {
            var before = AuthState(__instance);
            // Ignore repeat fires once we have already left the birthday step.
            if (!string.Equals(before, "BIRTHDAY", StringComparison.Ordinal))
            {
                Plugin.Log.LogInfo($"[AUTH] birthday submit ignored; already state={before}");
                return false;
            }

            EnsureAdultAccountCreationBirthday(__instance);
            EnsureLocalAvatarModel();

            // Local title flow routes through avatarCustomizationPage.
            // Watchdog parks birthday there; restore the real next page so we
            // leave BIRTHDAY. Never rewrite birthdayPage itself (that broke
            // navigation and left the user stuck on "What's your birthday?").
            var destination = _originalAvatarPage;
            if (destination == null || destination.Pointer == IntPtr.Zero)
                destination = __instance.usernamePage;
            if (destination != null && destination.Pointer != IntPtr.Zero)
                __instance.avatarCustomizationPage = destination;

            __instance.GoToCachedAccountStartAccountCreationFlow();

            var state = AuthState(__instance);
            if (string.Equals(state, "BIRTHDAY", StringComparison.Ordinal))
            {
                // Still stuck: skip avatar and open username directly.
                if (__instance.usernamePage != null &&
                    __instance.usernamePage.Pointer != IntPtr.Zero)
                {
                    __instance.avatarCustomizationPage = __instance.usernamePage;
                    __instance.GoToCachedAccountStartAccountCreationFlow();
                    state = AuthState(__instance);
                }
            }

            if (string.Equals(state, "BIRTHDAY", StringComparison.Ordinal))
            {
                // Last resort: run stock submit with the adult date already set.
                Plugin.Log.LogWarning(
                    "[AUTH] birthday local route still on BIRTHDAY; running stock submit");
                return true;
            }

            Plugin.Log.LogInfo($"[AUTH] birthday accepted; advanced to state={state}");
            return false;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AUTH] birthday handoff failed: {e.Message}");
            return true;
        }
    }

    public static bool AvatarSubmitPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        EnsureLocalAvatarModel();
        // Pin the customization the player just finished so Orientation cannot
        // fall back to a bare stock default after scene load.
        PinLocalCustomizationAvatar("AvatarSubmitPrefix");
        __instance.avatarCustomizationPage = __instance.usernamePage;
        __instance.GoToCachedAccountStartAccountCreationFlow();
        Plugin.Log.LogInfo($"[AUTH] avatar accepted; opened username state={AuthState(__instance)}");
        return false;
    }

    public static void AvatarSubmitPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        PinLocalCustomizationAvatar("AvatarSubmitPostfix");
        Plugin.Log.LogInfo($"[AUTH] avatar customization completed state={AuthState(__instance)}");
    }

    public static bool UsernameSubmitPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        __instance.avatarCustomizationPage = __instance.passwordPage;
        __instance.GoToCachedAccountStartAccountCreationFlow();
        Plugin.Log.LogInfo($"[AUTH] username accepted; opened password state={AuthState(__instance)}");
        return false;
    }

    public static bool PasswordSubmitPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        __instance.avatarCustomizationPage = __instance.consolidatedInfoPage;
        __instance.GoToCachedAccountStartAccountCreationFlow();
        Plugin.Log.LogInfo($"[AUTH] password accepted; opened account details state={AuthState(__instance)}");
        return false;
    }

    public static bool ConsolidatedSubmitPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        // This private server does not collect or look up phone numbers.
        // Clear any value left behind by the stock title flow before registration.
        __instance.PIKOMEAJGFD = string.Empty;
        var email = __instance.FCFPJEFJJAP?.Trim();
        __instance.FCFPJEFJJAP =
            string.IsNullOrEmpty(email) || IsUsableEmail(email)
                ? email ?? string.Empty
                : string.Empty;
        __instance.avatarCustomizationPage =
            __instance.accountCreationCodeOfConductPage;
        __instance.GoToCachedAccountStartAccountCreationFlow();
        Plugin.Log.LogInfo(
            $"[AUTH] account details accepted; opened original Code of Conduct " +
            $"state={AuthState(__instance)}");
        return false;
    }

    public static void ConsolidatedSubmitPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        var state = AuthState(__instance);
        Plugin.Log.LogInfo($"[AUTH] account details returned state={state}");
    }

    public static bool AcceptCodeOfConductPrefix(
        RRUI.Data.TitleScreenFlowModel __instance)
    {
        try
        {
            // Keep the depot's original Code of Conduct page and its Agree
            // button, but replace the retired public account service that used
            // to run after it. The local registration task owns that operation.
            __instance.avatarCustomizationPage =
                __instance.accountCreationInterstitialPage;
            __instance.GoToCachedAccountStartAccountCreationFlow();
            Plugin.Log.LogInfo(
                $"[AUTH] Code of Conduct accepted; starting local registration " +
                $"state={AuthState(__instance)}");
            BeginLocalRegistration(__instance);
            return false;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[AUTH] Code of Conduct handoff failed: " +
                $"{root.GetType().Name}: {root.Message}");
            __instance.HNHEGEMFHOI =
                "The account could not be created. Please try again.";
            return false;
        }
    }

    public static bool LaunchGameAccountCreationPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        PreserveCustomizationForOrientation("LaunchGameAccountCreation");
        if (_dispatchingNativeGameLaunch)
        {
            Plugin.Log.LogInfo(
                $"[AUTH] native LaunchGameAccountCreation invoked state={AuthState(__instance)}");
            return true;
        }

        // In this depot LaunchGameAccountCreation is the action behind the
        // original Get Started button.  Registration used to call it directly,
        // which skipped accountCreationCompletePage and left only the blank
        // backing canvas visible.  Keep the original Welcome/Get Started page,
        // then move to its stock Orientation loading page only after the click.
        __instance.avatarCustomizationPage =
            __instance.accountCreationLaunchGamePage;
        __instance.GoToCachedAccountStartAccountCreationFlow();
        QueueNativeGameLaunch(__instance, true);
        Plugin.Log.LogInfo(
            $"[AUTH] Get Started accepted; opened original Orientation loading page " +
            $"state={AuthState(__instance)}");
        return false;
    }

    public static void LaunchGameAccountCreationPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        Plugin.Log.LogInfo($"[AUTH] LaunchGameAccountCreation returned state={AuthState(__instance)}");
    }

    public static void LaunchGameCachedAccountPrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        PreserveCustomizationForOrientation("LaunchGameCachedAccount");
        Plugin.Log.LogInfo($"[AUTH] LaunchGameCachedAccount invoked state={AuthState(__instance)}");
    }

    public static void LaunchGameCachedAccountPostfix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        Plugin.Log.LogInfo($"[AUTH] LaunchGameCachedAccount returned state={AuthState(__instance)}");
    }

    public static void TitleLaunchPipelinePrefix(
        RRUI.Data.TitleScreenFlowModel __instance,
        bool __0)
    {
        PreserveCustomizationForOrientation("TitleLaunchPipeline");
        Plugin.Log.LogInfo(
            $"[BOOTSTRAP] entered native title launch pipeline " +
            $"accountCreation={__0} state={AuthState(__instance)}");
    }

    public static void TitleLaunchPipelinePostfix(
        RRUI.Data.TitleScreenFlowModel __instance,
        bool __0)
    {
        Plugin.Log.LogInfo(
            $"[BOOTSTRAP] native title launch pipeline returned " +
            $"accountCreation={__0} state={AuthState(__instance)}");
    }

    public static void BootSequenceLaunchPrefix(
        BootSequence.DJHPHOBJLHM __0)
    {
        PreserveCustomizationForOrientation("BootSequenceLaunch");
        // Keep the account-creation tutorial value alive through Orientation.
        // The native preference store is still unavailable at this point; if
        // this guard is cleared here, OrientationIntroduction throws before it
        // can play the welcome sequence or open the first door.
        _bootSequenceFallbackStartedAt = null;
        _bootSequenceFallbackOrientation = false;
        ResetLocalMatchmakingLaunchState();
        Plugin.Log.LogInfo(
            $"[BOOTSTRAP] BootSequence.LaunchGame reached " +
            $"targetPresent={__0 != null} orientation={__0?.AGOIDOKPDOH}");
    }

    public static bool RoomCalibrationRequiresCalibrationPrefix(ref bool __result)
    {
        if (!_localBootHandoffUntil.HasValue ||
            DateTime.UtcNow > _localBootHandoffUntil.Value)
            return true;

        // The desktop title flow normally reports RoomCalibrationType.Off.
        // When the retired preference service is still uninitialized, the
        // stock RequiresCalibration getter instead defaults to true and loads
        // room_calibration. In this depot that scene is a black frame, not the
        // Orientation travel page. Limit the override to the local post-login
        // handoff so in-game/manual VR calibration behavior is untouched.
        __result = false;
        if (!_calibrationBypassLogged)
        {
            _calibrationBypassLogged = true;
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] skipped stale VR room calibration for desktop launch");
        }
        return false;
    }

    public static void BootCalibrationStatePrefix()
    {
        Plugin.Log.LogInfo("[BOOTSTRAP] entered native calibration state");
    }


    public static bool BootPostLoginInitializationStatePrefix()
    {
        if (_localBootHandoffUntil.HasValue &&
            DateTime.UtcNow <= _localBootHandoffUntil.Value)
        {
            // This legacy state never returns against the local service stack:
            // it waits inside retired platform/analytics/fade initialization
            // after account, Photon, and launch-target setup have already
            // completed. Skipping only this local state lets the postfix move
            // the real BootSequence state machine to LOAD_INITIAL_SCENE.
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] bypassing retired local post-login platform state");
            return false;
        }

        Plugin.Log.LogInfo(
            "[BOOTSTRAP] entered native post-login initialization state");
        return true;
    }

    public static void BootPostLoginInitializationStatePostfix(
        BootSequence __instance)
    {
        if (_localPostLoginAdvanceDispatched ||
            !_localBootHandoffUntil.HasValue ||
            DateTime.UtcNow > _localBootHandoffUntil.Value)
            return;

        _localPostLoginAdvanceDispatched = true;
        try
        {
            // The stock state performs its synchronous platform/core setup,
            // fades the title canvas to black, then waits for the legacy fade
            // promise before selecting LOAD_INITIAL_SCENE (state 100). The
            // visuals finish fading in this depot, but that completion promise
            // never invokes its callback. Advance through the state machine
            // after the original method returns so all of its initialization
            // work is retained and the original Orientation loader can start.
            var transition =
                AccessTools.Method(typeof(BootSequence), "GFADEMIIKAK");
            if (transition == null)
                throw new MissingMethodException(
                    typeof(BootSequence).FullName,
                    "GFADEMIIKAK");

            var parameters = transition.GetParameters();
            if (parameters.Length != 1 || !parameters[0].ParameterType.IsEnum)
                throw new InvalidOperationException(
                    "BootSequence transition signature is not the expected enum state.");

            var loadInitialScene =
                Enum.ToObject(parameters[0].ParameterType, 100);
            transition.Invoke(__instance, new[] { loadInitialScene });
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] completed stale camera-fade handoff to initial-scene loading");
        }
        catch (Exception e)
        {
            _localPostLoginAdvanceDispatched = false;
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[BOOTSTRAP] initial-scene state handoff failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    public static void BootLoadInitialSceneStatePrefix()
    {
        _localPostLoginAdvanceDispatched = true;
        Plugin.Log.LogInfo(
            "[BOOTSTRAP] entered native initial-scene loading state");
    }

    public static void BootLoadInitialSceneStatePostfix(BootSequence __instance)
    {
        if (_localInitialSceneContinuationDispatched ||
            !_localBootHandoffUntil.HasValue ||
            DateTime.UtcNow > _localBootHandoffUntil.Value)
            return;

        _localInitialSceneContinuationDispatched = true;
        try
        {
            // EHJDBFFHNAK resolves the launch target, updates the title loading
            // state, and asks ANHBBOGDLGD for any remaining target metadata.
            // For the stock Orientation target that metadata promise is already
            // completed. This legacy promise implementation drops an Action
            // attached after completion, so its stock continuation never runs
            // and the title canvas remains blank.
            //
            // LPKAECAODDA is that exact stock continuation. It calls
            // FALKOHHOCKF to start the real initial-scene load, then registers
            // EDBFDLCBEDH (loading UI), INCOCGBNDAJ (state 101), and
            // KKEADDGGMPM (failure diagnostics). Invoke it once after the state
            // method has retained all of its normal setup.
            var continueInitialScene =
                AccessTools.Method(typeof(BootSequence), "LPKAECAODDA", Type.EmptyTypes);
            if (continueInitialScene == null)
                throw new MissingMethodException(
                    typeof(BootSequence).FullName,
                    "LPKAECAODDA");

            continueInitialScene.Invoke(__instance, null);
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] dispatched stock initial-scene continuation for Orientation");
        }
        catch (Exception e)
        {
            _localInitialSceneContinuationDispatched = false;
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[BOOTSTRAP] stock initial-scene continuation failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    public static bool BootLoadInitialScenePromisePrefix(
        ref NDNJBANLHJC __result)
    {
        if (!_localBootHandoffOrientation ||
            !_localBootHandoffUntil.HasValue ||
            DateTime.UtcNow > _localBootHandoffUntil.Value)
            return true;

        if (!Plugin.BypassStockInitialSceneLoad.Value)
        {
            // Letting the stock method run is what actually starts the initial
            // scene load; the local replacement below only settles a promise.
            // It still needs its inputs though: without the offline-room preset
            // installed first it asks the retired room-data service for
            // Orientation and never settles.
            if (!_stockInitialSceneLoadLogged)
            {
                _stockInitialSceneLoadLogged = true;
                try
                {
                    var orientation = OAILMIHJFAK.JDJEDHFBNGE;
                    if (orientation == null)
                        throw new InvalidOperationException(
                            "The bundled Orientation room preset is unavailable.");

                    var prepareLoginLock =
                        AccessTools.Method(
                            typeof(RecNet.Matchmaking),
                            "MMJJAAPKDFP",
                            Type.EmptyTypes);
                    prepareLoginLock?.Invoke(null, null);

                    PrepareStockLocalOrientationTravel(orientation);
                    Plugin.Log.LogInfo(
                        "[ORIENTATION] installed stock offline-room preset, then handed " +
                        "off to the stock initial-scene load");
                }
                catch (Exception e)
                {
                    var root = e.GetBaseException();
                    Plugin.Log.LogError(
                        $"[ORIENTATION] could not prepare the stock initial-scene load: " +
                        $"{root.GetType().Name}: {root.Message}");
                }
            }
            return true;
        }

        try
        {
            // FALKOHHOCKF normally asks the retired room-data service for
            // Orientation details before it calls this exact matchmaking
            // method. The bundled OAILMIHJFAK Orientation preset already
            // contains the offline scene type (14), name, and private join
            // mode. The legacy BestHTTP /matchmake/none request in this depot
            // creates a promise but never sends or settles it after the local
            // managed login handoff. Keep the stock pending promise shape, set
            // the exact native offline-room preset, and settle the promise on
            // the Unity thread after the local endpoint answers. LPKAECAODDA
            // therefore retains its original success callback, which advances
            // BootSequence to state 101 and displays the real travel loader.
            if (_localOrientationMatchmakingPromise == null)
            {
                var orientation = OAILMIHJFAK.JDJEDHFBNGE;
                if (orientation == null)
                    throw new InvalidOperationException(
                        "The bundled Orientation room preset is unavailable.");

                var prepareLoginLock =
                    AccessTools.Method(
                        typeof(RecNet.Matchmaking),
                        "MMJJAAPKDFP",
                        Type.EmptyTypes);
                prepareLoginLock?.Invoke(null, null);

                PrepareStockLocalOrientationTravel(orientation);

                _localOrientationMatchmakingPromise =
                    new FEKGIBNPEAH<RecNet.Matchmaking.NPKOLENFHIH>();
                _localOrientationMatchmakingTask =
                    MatchmakeLocalOrientationAsync(_activeLocalAccessToken);
                Plugin.Log.LogInfo(
                    "[ORIENTATION] installed stock offline-room preset and started local matchmaking");
            }

            Plugin.Log.LogInfo(
                "[BOOTSTRAP] returned pending local Orientation promise to the stock loader");
            // Generic IL2CPP interface inheritance is not represented as a
            // normal CLR implicit conversion in the generated interop
            // assembly. Cast through the native object pointer so Harmony
            // receives the exact NDNJBANLHJC interface returned by the stock
            // FALKOHHOCKF method.
            __result =
                _localOrientationMatchmakingPromise.Cast<NDNJBANLHJC>();
            return false;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[BOOTSTRAP] direct Orientation matchmaking failed: " +
                $"{root.GetType().Name}: {root.Message}");
            return true;
        }
    }

    private static void PrepareStockLocalOrientationTravel(
        OAILMIHJFAK orientation)
    {
        // Reproduce every native side effect that EHAJFDHHBCF performs
        // before it sends matchmake/none. Merely installing the preset and
        // resolving its promise advances BootSequence to state 101 without
        // ever notifying the stock room loader; that leaves the title fade
        // visible and state 101 crashes because no initial scene exists.
        var setDestinationName =
            AccessTools.Method(
                typeof(RecNet.Matchmaking),
                "JJEIEFFJCNI",
                new[] { typeof(string) });
        var getCurrentRoomInstance =
            AccessTools.Method(
                typeof(KCBFNPPKMAB),
                "CAEINEGJFHC",
                Type.EmptyTypes);
        var getCurrentPresence =
            AccessTools.Method(
                typeof(RecNet.Matchmaking),
                "FIKGJGAILEJ",
                new[] { typeof(int) });
        var beginRoomTransition =
            AccessTools.Method(
                typeof(RecNet.Matchmaking),
                "BKEJFBAHDNK",
                new[] { typeof(NFAEEPLGGPJ), typeof(bool) });
        var installOfflineRoom =
            AccessTools.Method(
                typeof(RecNet.Matchmaking),
                "KHDEFDLLPLL",
                new[] { typeof(OAILMIHJFAK) });

        if (setDestinationName == null ||
            getCurrentRoomInstance == null ||
            getCurrentPresence == null ||
            beginRoomTransition == null ||
            installOfflineRoom == null)
        {
            throw new MissingMethodException(
                "The stock local-room transition methods are unavailable.");
        }

        setDestinationName.Invoke(
            null,
            new object[] { orientation.LJDFOHKOPOI });

        var roomInstance =
            (int)getCurrentRoomInstance.Invoke(null, null);
        var presence =
            getCurrentPresence.Invoke(
                null,
                new object[] { roomInstance }) as NFAEEPLGGPJ;
        if (presence != null)
        {
            beginRoomTransition.Invoke(
                null,
                new object[] { presence, true });
        }

        installOfflineRoom.Invoke(
            null,
            new object[] { orientation });
        Plugin.Log.LogInfo(
            $"[ORIENTATION] notified the stock room loader " +
            $"destination={orientation.LJDFOHKOPOI} " +
            $"existingPresence={presence != null}");
    }

    private static async Task<string> MatchmakeLocalOrientationAsync(
        string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return "The local Orientation launch has no account token.";

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                Plugin.ServerHostname.Value.TrimEnd('/') + "/matchmake/none");
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                "Bearer " + accessToken);
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15),
            };
            using var response =
                await client.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return $"Local Orientation matchmaking returned HTTP " +
                       $"{(int)response.StatusCode}.";
            }

            return string.Empty;
        }
        catch (Exception e)
        {
            return "Local Orientation matchmaking failed: " +
                   e.GetBaseException().Message;
        }
    }

    private static void PumpLocalOrientationMatchmaking()
    {
        if (_localOrientationMatchmakingPromise == null ||
            _localOrientationMatchmakingTask == null ||
            !_localOrientationMatchmakingTask.IsCompleted)
            return;

        var promise = _localOrientationMatchmakingPromise;
        var task = _localOrientationMatchmakingTask;
        _localOrientationMatchmakingPromise = null;
        _localOrientationMatchmakingTask = null;

        try
        {
            var error = task.GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(error))
            {
                Plugin.Log.LogError($"[ORIENTATION] {error}");
                promise.FJMKLADBHHO(error);
                return;
            }

            Plugin.Log.LogInfo(
                "[ORIENTATION] local matchmaking completed; releasing the stock state-101 loader transition");
            promise.ENDFAGEBOAN(
                RecNet.Matchmaking.NPKOLENFHIH.Success);

            // Settling the promise only advances the boot state machine. The
            // stock FALKOHHOCKF we replaced was also the call that started the
            // real room-scene load, so without this the boot sequence reaches
            // POST_LOAD_INITIAL_SCENE with TitleScreen still the active scene
            // and the player sees the faded title canvas.
            // Guarded at the call site, not inside: referencing SessionManager
            // makes the JIT load a generated interop type whose base-class
            // generic constraint it rejects, so the method throws on entry when
            // the workaround is not wanted.
            if (Plugin.ForceLocalRoomSceneLoad.Value && !_localRoomSceneLoadStarted)
                StartLocalRoomSceneLoad();

            // Arm the direct fallback. The stock loader gets first refusal.
            _directSceneLoadDueAt =
                DateTime.UtcNow.AddSeconds(
                    Math.Max(0f, Plugin.DirectSceneLoadDelaySeconds.Value));
            _directSceneLoadArmedAt = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            var error =
                $"Could not release the Orientation loader: " +
                $"{root.GetType().Name}: {root.Message}";
            Plugin.Log.LogError($"[ORIENTATION] {error}");
            promise.FJMKLADBHHO(error);
        }
    }

    public static void BootInitialSceneSuccessPostfix()
    {
        Plugin.Log.LogInfo(
            "[ORIENTATION] stock initial-scene promise succeeded and requested loader state 101");
    }

    public static bool BootPostLoadInitialSceneStatePrefix()
    {
        var localOrientationHandoff =
            _localPostLoadBypassHandled ||
            (_localBootHandoffOrientation &&
             _localBootHandoffUntil.HasValue &&
             DateTime.UtcNow <= _localBootHandoffUntil.Value);

        _localInitialSceneContinuationDispatched = true;
        _localBootHandoffUntil = null;
        _localBootHandoffOrientation = false;

        if (!localOrientationHandoff)
        {
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] entered native post-load scene state");
            return true;
        }

        if (!Plugin.SkipStockPostLoadState.Value)
        {
            if (!_localPostLoadBypassHandled)
            {
                _localPostLoadBypassHandled = true;
                Plugin.Log.LogInfo(
                    "[ORIENTATION] running the stock POST_LOAD_INITIAL_SCENE state " +
                    "(skip disabled)");
            }
            return true;
        }

        if (!_localPostLoadBypassHandled)
        {
            _localPostLoadBypassHandled = true;

            // POST_LOAD_INITIAL_SCENE is a batch of retired remote catalog,
            // storefront, moderation, and inventory downloads. Running it
            // before those old services exist throws from its first missing
            // scene singleton and strands the camera fade. The room loader
            // only needs this readiness signal; local account/avatar data is
            // already installed by the managed bootstrap.
            BootSequence.GIFADIBMPBC = true;
            Plugin.Log.LogInfo(
                "[ORIENTATION] skipped retired post-load RecNet batch and released native scene changes");
        }

        return false;
    }

    public static bool HasCompletedNuxTutorialPrefix(ref bool __result)
    {
        if (!_tutorialCompletionFallbackUntil.HasValue ||
            DateTime.UtcNow > _tutorialCompletionFallbackUntil.Value)
        {
            // Outside the guarded window the stock getter reads the preference
            // store, which throws for the whole session while the native RecNet
            // connection is down. Inside the Orientation scene that kills
            // OrientationSubScene.Awake, so keep answering "not completed".
            if (!Plugin.SuppressPreferenceExceptions.Value)
                return true;

            __result = false;
            return false;
        }

        __result = _tutorialCompletionFallbackValue;
        Plugin.Log.LogInfo(
            $"[ORIENTATION] supplied guarded tutorial completion fallback={__result}");
        return false;
    }

    public static bool HasCompletedOrientationPrefix(ref bool __result)
    {
        var fallbackActive =
            _tutorialCompletionFallbackUntil.HasValue &&
            DateTime.UtcNow <= _tutorialCompletionFallbackUntil.Value;
        if (!fallbackActive)
        {
            // Once the local preference service is genuinely available the
            // stock getter is authoritative, including after completion.
            return true;
        }

        __result = _tutorialCompletionFallbackValue;
        if (!_hasCompletedOrientationFallbackLogged)
        {
            _hasCompletedOrientationFallbackLogged = true;
            Plugin.Log.LogWarning(
                $"[ORIENTATION] supplied guarded HasCompletedOrientation=" +
                $"{__result} while native player preferences initialize");
        }
        return false;
    }

    public static bool OrientationIsReturningPlayerPrefix(ref bool __result)
    {
        if (!Plugin.DirectOrientationSceneLoad.Value ||
            (!_localPlayerSpawnStarted && !_localPlayerSpawnSucceededLogged))
            return true;

        // This launch entered Orientation from new-account creation. The
        // production preference graph is unavailable in the local bootstrap,
        // and its default used to classify the new player as returning. That
        // selects the quick/empty branch and suppresses the shipped Coach,
        // hand vignette, and first-time prompts.
        __result = false;
        return false;
    }

    // Every typed preference accessor on NBDIDJMANNH funnels through this string
    // getter, so one prefix keeps the whole store from throwing and hands back
    // the caller's own default instead. Without it the Orientation scene's Awake
    // chain dies on the first preference read.
    public static bool PreferenceStringGetPrefix(
        NBDIDJMANNH __instance,
        string __1,
        ref string __result)
    {
        if (!Plugin.SuppressPreferenceExceptions.Value)
            return true;

        try
        {
            if (__instance != null && __instance.BFCALEBFFJP)
                return true;
        }
        catch
        {
            // Unreadable ready-flag: treat as not ready and use the default.
        }

        __result = __1;
        if (!_preferenceFallbackLogged)
        {
            _preferenceFallbackLogged = true;
            Plugin.Log.LogWarning(
                "[PREFS] preference store is not initialized; returning callers' " +
                "defaults instead of throwing (further occurrences not logged)");
        }
        return false;
    }

    public static bool PreferenceKeyExistsPrefix(
        NBDIDJMANNH __instance,
        ref bool __result)
    {
        if (!Plugin.SuppressPreferenceExceptions.Value)
            return true;

        try
        {
            if (__instance != null && __instance.BFCALEBFFJP)
                return true;
        }
        catch
        {
            // Treat an unreadable ready flag as an empty local preference store.
        }

        // DFCGFJAHDDJ asks HasKey before reading each typed preference. Returning
        // false makes it use the caller's supplied default. Previously this
        // method threw inside PlayerBackpack.SetFavoriteTool, which unwound
        // InstantiateLocalPlayer and reset SceneSpawnManager to NotRunning.
        __result = false;
        if (!_preferenceKeyFallbackLogged)
        {
            _preferenceKeyFallbackLogged = true;
            Plugin.Log.LogWarning(
                "[PREFS] treating the uninitialized local preference store as " +
                "empty while the Orientation player prefab starts");
        }
        return false;
    }

    public static void HasCompletedOrientationPostfix(bool __0)
    {
        _tutorialCompletionFallbackValue = __0;
        if (!__0)
            return;

        Plugin.Log.LogInfo(
            "[ORIENTATION] native completion flag was set; persisting for future Dorm launches");
        if (!string.IsNullOrEmpty(_activeLocalAccessToken) &&
            (_orientationCompletionSaveTask == null ||
             _orientationCompletionSaveTask.IsCompleted))
        {
            _orientationCompletionSaveTask =
                SaveOrientationCompletionAsync(_activeLocalAccessToken);
        }
    }

    private static async Task SaveOrientationCompletionAsync(string accessToken)
    {
        try
        {
            var endpoint =
                Plugin.ServerHostname.Value.TrimEnd('/') + "/playersettings";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                "Bearer " + accessToken);
            request.Content = new StringContent(
                "{\"Key\":\"HAS_COMPLETED_ORIENTATION\",\"Value\":\"true\"}",
                Encoding.UTF8,
                "application/json");
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15),
            };
            using var response = await client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Plugin.Log.LogInfo(
                    "[ORIENTATION] completion persisted; returning logins will launch the Dorm");
            }
            else
            {
                Plugin.Log.LogWarning(
                    $"[ORIENTATION] completion persistence returned HTTP " +
                    $"{(int)response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[ORIENTATION] completion persistence failed: " +
                $"{e.GetBaseException().Message}");
        }
    }

    public static bool PlayerPreferencesInitializationGuardPrefix(
        NBDIDJMANNH __instance)
    {
        try
        {
            if (__instance != null && __instance.BFCALEBFFJP)
            {
                if (!_playerPreferencesReadyLogged)
                {
                    _playerPreferencesReadyLogged = true;
                    Plugin.Log.LogInfo(
                        "[BOOTSTRAP] native player preferences became ready; fallback disabled");
                }
                return true;
            }
        }
        catch
        {
            // Treat an unreadable initialization flag as not ready. The
            // stock method only throws in this state, which aborts unrelated
            // title-page controllers and can leave their UI empty.
        }

        if (!_playerPreferencesGuardSuppressionLogged)
        {
            _playerPreferencesGuardSuppressionLogged = true;
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] suppressing stock player-preferences guard until initialization completes");
        }
        return false;
    }

    private static void QueueNativeGameLaunch(
        RRUI.Data.TitleScreenFlowModel model,
        bool launchCreatedAccount)
    {
        _pendingGameLaunchModel = model;
        _pendingGameLaunchCreatedAccount = launchCreatedAccount;
        _pendingGameLaunchStartedAt = DateTime.UtcNow;
        _playerPreferencesInitializationRequested = false;
        _playerPreferencesReadyLogged = false;
        _playerPreferences = null;
        Plugin.Log.LogInfo(
            launchCreatedAccount
                ? "[BOOTSTRAP] waiting for player preferences before Orientation launch"
                : "[BOOTSTRAP] waiting for player preferences before cached-account launch");
    }

    private static void PumpNativeGameLaunch()
    {
        var model = _pendingGameLaunchModel;
        if (model == null || !_pendingGameLaunchStartedAt.HasValue)
            return;

        var preferencesReady = false;
        try
        {
            if (_playerPreferences == null)
                _playerPreferences = FindPlayerPreferencesService(model);

            if (_playerPreferences != null &&
                !_playerPreferencesInitializationRequested)
            {
                _playerPreferencesInitializationRequested = true;
                _playerPreferences.OHDDAKDDMHB();
                Plugin.Log.LogInfo(
                    "[BOOTSTRAP] requested native player-preferences initialization");
            }

            preferencesReady =
                _playerPreferences != null &&
                _playerPreferences.DDINFLMPBCP();
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogWarning(
                $"[BOOTSTRAP] player-preferences initialization check failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }

        var elapsed =
            (DateTime.UtcNow - _pendingGameLaunchStartedAt.Value).TotalSeconds;
        var preferenceWaitSeconds =
            string.IsNullOrEmpty(_activeLocalAccessToken) ? 5.0 : 1.0;
        if (!preferencesReady && elapsed < preferenceWaitSeconds)
            return;

        var launchCreatedAccount = _pendingGameLaunchCreatedAccount;
        _pendingGameLaunchModel = null;
        _pendingGameLaunchStartedAt = null;
        _pendingGameLaunchCreatedAccount = false;

        // A newly-created account must enter Orientation. If the obsolete
        // remote preference store cannot finish, use the semantically correct
        // default (tutorial incomplete) for this transition only. For cached
        // accounts, the timeout fallback treats Orientation as completed so a
        // temporary settings outage cannot trap the player on a white screen.
        if (launchCreatedAccount || !preferencesReady)
        {
            _tutorialCompletionFallbackValue = !launchCreatedAccount;
            _tutorialCompletionFallbackUntil = DateTime.UtcNow.AddMinutes(20);
        }
        if (!preferencesReady)
        {
            Plugin.Log.LogWarning(
                "[BOOTSTRAP] legacy remote preferences timed out; using the native " +
                "in-memory defaults until the store finishes initialization");
        }

        Plugin.Log.LogInfo(
            $"[BOOTSTRAP] launching game preferencesReady={preferencesReady} " +
            $"newAccount={launchCreatedAccount} waited={elapsed:0.0}s");

        try
        {
            if (launchCreatedAccount)
            {
                _dispatchingNativeGameLaunch = true;
                try
                {
                    model.LaunchGameAccountCreation();
                }
                finally
                {
                    _dispatchingNativeGameLaunch = false;
                }
            }
            else
            {
                _dispatchingNativeGameLaunch = true;
                try
                {
                    model.LaunchGameCachedAccount();
                }
                finally
                {
                    _dispatchingNativeGameLaunch = false;
                }
            }

            // The retired matchmaking/session-takeover promise used by this
            // client may never settle against a local server.  Give the native
            // continuation a short opportunity to run; TitleUpdatePostfix then
            // first restores the stock regular-login -> exclusive-login state
            // transition, then supplies the same launch target that the stock
            // closure creates for Orientation (or the default Dorm target).
            ResetLocalMatchmakingLaunchState();
            _bootSequenceFallbackStartedAt = DateTime.UtcNow;
            _bootSequenceFallbackOrientation = launchCreatedAccount;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[BOOTSTRAP] native game launch failed safely: " +
                $"{root.GetType().Name}: {root.Message}");
            model.GoToLogin();
            model.FLACNNLAPNN =
                "The game could not finish loading your account. Please try again.";
        }
    }

    private static void ResetLocalMatchmakingLaunchState()
    {
        _localMatchmakingLoginAttemptedAt = null;
        _localMatchmakingExclusiveLoginAttemptedAt = null;
        _localMatchmakingLoginAttempts = 0;
        _localMatchmakingExclusiveLoginAttempts = 0;
        _localMatchmakingLastObservedState = null;
        _localMatchmakingReadyLogged = false;
        _localMatchmakingFailureLogged = false;
        _localPlayerSessionTask = null;
        _localPlayerSessionCompletionHandled = false;
        _localOrientationMatchmakingPromise = null;
        _localOrientationMatchmakingTask = null;
        _localPostLoadBypassHandled = false;
        _localRoomSceneLoadStarted = false;
        _roomDependencyContainerAttempts = 0;
        _roomDependencyContainerReadyLogged = false;
        _roomDependencyContainerFailureLogged = false;
        _orientationContentLoadAttempts = 0;
        _orientationBaseReadyLogged = false;
        _orientationBaseWaitLogged = false;

        // Everything below belongs to one room-launch attempt.  Keeping any of
        // it across a retry makes the travel page reach 100% while the previous
        // Player/camera/tutorial state is still considered complete.
        _directSceneLoadDueAt = null;
        _directSceneLoadArmedAt = null;
        _hideLoadingScreenAt = null;
        _additiveSceneDueAt = null;
        _roomContentReportAt = null;
        _playerSpawnStarted = false;
        _localPlayerSpawnDueAt = null;
        _localPlayerSpawnStartedAt = null;
        _localPlayerSpawnAttempts = 0;
        _localPlayerSpawnStarted = false;
        _localPlayerSpawnSucceededLogged = false;
        _localPlayerSpawnTimeoutLogged = false;
        _localPlayerPresentationWaitLogged = false;
        _localPlayerPresentationWaitSince = null;
        _cachedLocalPlayer = null;
        _cachedLocalPlayerPtr = IntPtr.Zero;
        _cachedLocalPlayerAt = DateTime.MinValue;

        _postSpawnRepairUntil = null;
        _postSpawnRepairNextAt = null;
        _offlineLocomotionReady = false;
        _offlineGameplayRepairActive = false;
        _suppressScreenPlayerTicks = false;
        _stockScreenPlayerReady = false;
        _stockPlayerInitializedStateEntered = false;
        _offlineLocalPlayerLifecyclePublished = false;
        _offlinePlayerAwakeFailed = false;
        _screenPlayerBoundLogged = false;
        _screenPlayerLifecycleWarned = false;
        _offlineCameraRecoveryAttempted = false;
        _freeCamReady = false;
        _cachedFluxCamera = null;

        _lateMainRootLoadRequested = false;
        _offlineCoreGameplayWaitLogged = false;
        _offlineCoreGameplayReadyLogged = false;
        _offlineAudioManagerInitializationAttempted = false;
        _offlineAudioManagerInitializationFailureLogged = false;
        _offlineInstantiationHandoffCompleted = false;
        _offlinePlayerAwakeDiagnosticsLogged = false;
        _hasCompletedOrientationFallbackLogged = false;
        _offlinePlayerAvatarInitializationInProgress = false;
        _offlinePlayerAvatarInitializedEarly = false;
        _offlinePlayerAvatarReadyLogged = false;
        _offlineToolEquipSlotsInitializationInProgress = false;
        _offlineToolEquipSlotsInitializedEarly = false;
        _offlineToolEquipSlotsReadyLogged = false;
        _orientationScenesReadyAt = null;
        _forceOrientationEnterDone = false;
        _offlinePhotonRoomJoinStarted = false;
        _offlinePhotonRoomJoinAttempts = 0;
        _offlinePhotonRoomLastJoinAt = null;
        _offlinePhotonRoomReadyLogged = false;

        _loadingScreenShown = false;
        _loadingScreenLastState = string.Empty;
        _loadingScreenActivatedAt = null;
        _orientationUiCursorRequested = false;
        _offlineScreenHudCursors.Clear();
        _offlineScreenHudCursorScanAt = null;
        _offlineScreenHudCursorHiddenLogged = false;
        ClearOrientationDoorHighlight();
        _initializedOrientationScenes.Clear();
        _orientationContentScene = string.Empty;
        _orientationContentEnteredAt = null;
        _stockOrientationIntroduction = null;
        _stockOrientationIntroductionPtr = IntPtr.Zero;
        _stockOrientationIntroInitAttempts = 0;
        _stockOrientationIntroUpdateErrorLogged = false;
        _orientationStockFlowErrorLogged = false;
        _orientationIntroEncounterLevelVo = null;
        _orientationIntroEncounterWalk = null;
        _orientationIntroEncounterHands = null;
        _orientationIntroEncounterLook = null;
        _orientationIntroLevelVoActivated = false;
        _orientationIntroWalkActivated = false;
        _orientationIntroHandsActivated = false;
        _orientationIntroLookActivated = false;
        _orientationTargetDoor = null;
        _orientationTargetDoorPtr = IntPtr.Zero;
        _orientationNearbyDoor = null;
        _orientationNearbyDoorPtr = IntPtr.Zero;
        _orientationDoorNextScanAt = null;
        _orientationSceneDoors.Clear();
        _orientationDoorScanLoggedScene = string.Empty;
        _orientationDoorVisualBoundsValid = false;
        _orientationDoorVisualName = string.Empty;
        _orientationDoorPromptVisible = false;
        _leftMouseWasDown = false;
        ClearOrientationDoorHighlight();
        _orientationDoorVisualRoot = null;
        _orientationDoorVisualRootPtr = IntPtr.Zero;
        _orientationDoorVisualCollider = null;
        _orientationDoorAnimator = null;
        _orientationDoorHighlightApplied = false;
        _orientationDoorArmedLogged = false;
        _orientationDoorVisualProbeNextAt = null;
        _validationOrientationDoorPositioned = false;
        _validationOrientationDoorPressed = false;
        _validationOrientationDoorPressAt = null;
        _offlineGroundProbeNextAt = null;
        _orientationSceneTransitionInProgress = false;
        _orientationPortalSourceScene = string.Empty;
        _orientationPortalTargetScene = string.Empty;
        _orientationPortalUsePendingAt = null;
        _orientationSceneLoadOperation = null;
        _orientationWatchUnlockAttempted = false;
        if (_orientationDoorOutline != null)
        {
            try { UnityEngine.Object.Destroy(_orientationDoorOutline.gameObject); }
            catch { /* reset still continues */ }
            _orientationDoorOutline = null;
        }
        _offlineAvatarAnimationRigReady = false;
        _offlineAvatarAnimationPuppetPtr = IntPtr.Zero;
        _offlineAvatarBody = null;
        _offlineAvatarLeftHand = null;
        _offlineAvatarRightHand = null;
        _offlineAvatarMoveAmount = 0f;
        _offlineAvatarAnimationDt = 1f / 60f;
        _offlineTravelAttempted = false;
        _bootLocalPlayerAttempted = false;

        _realAvatarMounted = false;
        _realAvatarTrackingBound = false;
        _realAvatarTrackingLogged = false;
        _realAvatarTrackingFailureLogged = false;
        _legacyPlayerVisualsDisabledLogged = false;
        _mountFailLogged = false;
        _lastRealMountAttemptAt = null;
        _avatarApplySucceeded = false;
        _avatarApplyFailLogged = false;
        _playerAvatarMefTried = false;
        _avatarOutfitTried = false;
        _avatarApplyStage = 0;
        _avatarApplyUntil = null;
        _avatarApplyNextAt = null;
    }

    private static void PumpLocalMatchmakingLogin()
    {
        if (!_bootSequenceFallbackStartedAt.HasValue)
            return;

        try
        {
            // The retired platform LoginLock service fails before it creates an
            // HTTPRequest in this depot. Managed authentication has already
            // installed and validated the local bearer token, so establish the
            // two server-side session records directly and then promote the
            // native state to the same value produced by ExclusiveLogin.
            if (!string.IsNullOrEmpty(_activeLocalAccessToken))
            {
                if (_localPlayerSessionTask == null)
                {
                    _localPlayerSessionTask =
                        EstablishLocalPlayerSessionAsync(_activeLocalAccessToken);
                    Plugin.Log.LogInfo(
                        "[MATCHMAKING] establishing local player and exclusive sessions");
                }

                if (!_localPlayerSessionTask.IsCompleted)
                    return;

                if (!_localPlayerSessionCompletionHandled)
                {
                    _localPlayerSessionCompletionHandled = true;
                    var sessionError =
                        _localPlayerSessionTask.GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(sessionError))
                        throw new InvalidOperationException(sessionError);

                    RecNet.Matchmaking.DPCOCDCKBDF =
                        RecNet.Matchmaking.NGBIBIMMPHE.EXCLUSIVELY_LOGGED_IN;
                    Plugin.Log.LogInfo(
                        "[MATCHMAKING] local exclusive session installed in native state");
                }
            }

            var state = (int)RecNet.Matchmaking.DPCOCDCKBDF;
            if (!_localMatchmakingLastObservedState.HasValue ||
                _localMatchmakingLastObservedState.Value != state)
            {
                _localMatchmakingLastObservedState = state;
                Plugin.Log.LogInfo(
                    $"[MATCHMAKING] native login state changed to {state}");
            }

            if (state >= 2)
            {
                if (!_localMatchmakingReadyLogged)
                {
                    _localMatchmakingReadyLogged = true;
                    Plugin.Log.LogInfo(
                        "[MATCHMAKING] exclusive login established; Orientation handoff is ready");
                }
                return;
            }

            var now = DateTime.UtcNow;
            if (state <= 0)
            {
                var mayRetry =
                    !_localMatchmakingLoginAttemptedAt.HasValue ||
                    (now - _localMatchmakingLoginAttemptedAt.Value).TotalSeconds >= 6;
                if (mayRetry && _localMatchmakingLoginAttempts < 3)
                {
                    _localMatchmakingLoginAttemptedAt = now;
                    _localMatchmakingLoginAttempts++;
                    Plugin.Log.LogInfo(
                        $"[MATCHMAKING] starting stock player/login " +
                        $"attempt={_localMatchmakingLoginAttempts}");
                    RecNet.Matchmaking.GAIOHLLNLMF();
                }
            }
            else if (state == 1)
            {
                var mayRetry =
                    !_localMatchmakingExclusiveLoginAttemptedAt.HasValue ||
                    (now - _localMatchmakingExclusiveLoginAttemptedAt.Value).TotalSeconds >= 6;
                if (mayRetry && _localMatchmakingExclusiveLoginAttempts < 3)
                {
                    _localMatchmakingExclusiveLoginAttemptedAt = now;
                    _localMatchmakingExclusiveLoginAttempts++;
                    Plugin.Log.LogInfo(
                        $"[MATCHMAKING] starting stock player/exclusivelogin " +
                        $"attempt={_localMatchmakingExclusiveLoginAttempts}");
                    RecNet.Matchmaking.JOEPMCJNDFI(true);
                }
            }

            var elapsed =
                (now - _bootSequenceFallbackStartedAt.Value).TotalSeconds;
            if (elapsed >= 22 && !_localMatchmakingFailureLogged)
            {
                _localMatchmakingFailureLogged = true;
                Plugin.Log.LogError(
                    $"[MATCHMAKING] stock login did not reach exclusive state " +
                    $"(state={state}); Orientation launch remains gated to avoid " +
                    "the legacy black-screen promise deadlock");
            }
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            if (!_localMatchmakingFailureLogged)
            {
                _localMatchmakingFailureLogged = true;
                Plugin.Log.LogError(
                    $"[MATCHMAKING] stock login transition failed: " +
                    $"{root.GetType().Name}: {root.Message}");
            }
        }
    }

    private static async Task<string> EstablishLocalPlayerSessionAsync(
        string accessToken)
    {
        try
        {
            var baseUrl = Plugin.ServerHostname.Value.TrimEnd('/');
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10),
            };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            using var login =
                await client.PostAsync(
                    baseUrl + "/player/login",
                    null).ConfigureAwait(false);
            if (!login.IsSuccessStatusCode)
                return $"Local player/login returned HTTP {(int)login.StatusCode}.";

            using var exclusive =
                await client.PostAsync(
                    baseUrl + "/player/exclusivelogin",
                    null).ConfigureAwait(false);
            if (!exclusive.IsSuccessStatusCode)
                return $"Local player/exclusivelogin returned HTTP {(int)exclusive.StatusCode}.";

            return string.Empty;
        }
        catch (Exception e)
        {
            return $"Could not establish the local player session: " +
                   e.GetBaseException().Message;
        }
    }

    // Reports what the engine and the boot state machine actually think is
    // loaded. The white/black frame after "requested loader state 101" is
    // indistinguishable in the log from a scene that loaded but rendered
    // nothing, so read the scene state directly instead of inferring it.
    // Drives the stock room-scene entry that FALKOHHOCKF would have started.
    // SessionManager.LocalPlayerRequestJoinRoomScene is the same call the
    // in-game travel path uses, so it brings the real loading screen with it.
    // Everything here goes through raw IL2CPP rather than the generated
    // SessionManager wrapper. Il2CppInterop emits
    // InjectedSingletonMonoBehaviour<TInt,TImpl> with a `TImpl : TInt`
    // constraint but does not record SessionManager as implementing
    // EBLECDGBCBE, so the CLR throws TypeLoadException the moment any JIT'd
    // method mentions the type. The native class and method are fine.
    private static unsafe void StartLocalRoomSceneLoad()
    {
        _localRoomSceneLoadStarted = true;
        try
        {
            var sessionManagerClass =
                IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "SessionManager");
            if (sessionManagerClass == IntPtr.Zero)
                throw new InvalidOperationException(
                    "The native SessionManager class is unavailable.");

            var joinRoomScene =
                IL2CPP.il2cpp_class_get_method_from_name(
                    sessionManagerClass,
                    "LocalPlayerRequestJoinRoomScene",
                    4);
            if (joinRoomScene == IntPtr.Zero)
                throw new InvalidOperationException(
                    "SessionManager.LocalPlayerRequestJoinRoomScene is unavailable.");

            var sessionManagerType =
                Il2CppType.TypeFromPointer(sessionManagerClass, "SessionManager");
            var sessionManager =
                UnityEngine.Object.FindObjectOfType(sessionManagerType);
            if (sessionManager == null || sessionManager.Pointer == IntPtr.Zero)
            {
                Plugin.Log.LogError(
                    "[ORIENTATION] no live SessionManager; cannot start the " +
                    "room-scene load.");
                _localRoomSceneLoadStarted = false;
                return;
            }

            var currentRoomScene =
                AccessTools.Method(
                    typeof(RecNet.Matchmaking),
                    "DKKHGIBIPEN",
                    Type.EmptyTypes);
            if (currentRoomScene == null)
                throw new MissingMethodException(
                    typeof(RecNet.Matchmaking).FullName,
                    "DKKHGIBIPEN");

            // Read only the native pointer so the room-scene descriptor's own
            // generated wrapper never has to be named here either.
            var descriptor =
                currentRoomScene.Invoke(null, null)
                    as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;
            if (descriptor == null || descriptor.Pointer == IntPtr.Zero)
            {
                Plugin.Log.LogError(
                    "[ORIENTATION] the installed offline room produced no " +
                    "room-scene descriptor; cannot start the room-scene load.");
                _localRoomSceneLoadStarted = false;
                return;
            }

            // BOOT is the load source the stock boot path uses; None means the
            // join carries no party-invite behaviour.
            var loadSource = (int)ENLDHOFNCPL.BOOT;
            var inviteMode = 0;
            byte followParty = 0;

            var descriptorPointer = descriptor.Pointer;
            var arguments = stackalloc void*[4];
            arguments[0] = (void*)descriptorPointer;
            arguments[1] = &loadSource;
            arguments[2] = &inviteMode;
            arguments[3] = &followParty;

            Plugin.Log.LogInfo(
                "[ORIENTATION] starting the stock room-scene load through " +
                "SessionManager.LocalPlayerRequestJoinRoomScene");

            var exception = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(
                joinRoomScene,
                sessionManager.Pointer,
                arguments,
                ref exception);
            if (exception != IntPtr.Zero)
            {
                _localRoomSceneLoadStarted = false;
                Plugin.Log.LogError(
                    "[ORIENTATION] SessionManager.LocalPlayerRequestJoinRoomScene " +
                    "threw inside the game.");
                return;
            }

            Plugin.Log.LogInfo("[ORIENTATION] stock room-scene load requested");
        }
        catch (Exception e)
        {
            _localRoomSceneLoadStarted = false;
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[ORIENTATION] room-scene load failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    // Last-resort fallback. While the native RecNet connection never completes,
    // the room loader has no data to act on and leaves TitleScreen active — the
    // white frame. The Orientation scenes are bundled in this depot's build
    // settings, so load them straight through Unity.
    private static void PumpDirectOrientationSceneLoad()
    {
        if (!Plugin.DirectOrientationSceneLoad.Value)
            return;

        // Drive the travel UI EVERY tick during the wait, before the due-time
        // gate below. This used to sit after that gate, which meant it could
        // only ever run in the same instant the scene was loaded - the screen
        // was switched on and immediately torn down by the scene swap, so it
        // never actually appeared and only left its black fade overlay behind.
        if (Plugin.ShowLoadingScreen.Value && _directSceneLoadArmedAt.HasValue)
        {
            var total = Math.Max(0.25f, Plugin.DirectSceneLoadDelaySeconds.Value);
            var elapsed = (float)(DateTime.UtcNow - _directSceneLoadArmedAt.Value)
                .TotalSeconds;
            try
            {
                DriveLoadingScreen(
                    Plugin.LoadingScreenLabel.Value,
                    Math.Min(0.95f, elapsed / total));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    $"[LOADING] loading screen update failed: " +
                    $"{e.GetBaseException().GetType().Name}: " +
                    $"{e.GetBaseException().Message}");
                _directSceneLoadArmedAt = null;
            }
        }

        if (!_directSceneLoadDueAt.HasValue ||
            DateTime.UtcNow < _directSceneLoadDueAt.Value)
            return;

        var sceneName = Plugin.OrientationSceneName.Value?.Trim();
        if (string.IsNullOrEmpty(sceneName))
        {
            _directSceneLoadDueAt = null;
            return;
        }

        try
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!string.Equals(active.name, "TitleScreen", StringComparison.Ordinal))
            {
                // The stock loader got there first; nothing to do.
                _directSceneLoadDueAt = null;
                Plugin.Log.LogInfo(
                    $"[ORIENTATION] room loader reached '{active.name}'; " +
                    "direct scene fallback not needed");
                return;
            }

            // Peg the bar before the load so the last thing on screen is a
            // completed transition rather than a stalled one.
            if (Plugin.ShowLoadingScreen.Value)
            {
                try { DriveLoadingScreen(Plugin.LoadingScreenLabel.Value, 1f); }
                catch { /* cosmetic only; never block the scene load */ }
            }

            LogMatchmakingState();

            // Try the real offline-room travel first: it owns the room load and
            // the player spawn, which a bare scene load cannot do. Give it a few
            // seconds to actually change scene before falling back.
            if (Plugin.UseOfflineRoomTravel.Value && !_offlineTravelAttempted)
            {
                _offlineTravelAttempted = true;
                try
                {
                    if (TravelToOfflineOrientation())
                    {
                        // Re-arm: if travel really works the scene changes and
                        // this pump stops being reached. If it silently no-ops
                        // we come back and fall through to the scene load. Use
                        // the same configured window as the first arm so the
                        // loading screen actually gets to fade in and hold
                        // before the scene swap tears it down, instead of a
                        // hardcoded 6s that cut it off mid-fade.
                        _directSceneLoadDueAt =
                            DateTime.UtcNow.AddSeconds(
                                Math.Max(0f, Plugin.DirectSceneLoadDelaySeconds.Value));
                        return;
                    }
                }
                catch (Exception e)
                {
                    var root = e.GetBaseException();
                    Plugin.Log.LogError(
                        $"[TRAVEL] failed: {root.GetType().Name}: {root.Message}");
                }
            }

            // BootLocalPlayerToDormRoom often returns without changing the scene
            // on this private-server path. Only try ONCE, then always fall through
            // to the real Orientation scene load. Retrying forever left users on
            // "Going to Orientation 100%" with no room.
            if (Plugin.UseBootLocalPlayerToRoom.Value && !_bootLocalPlayerAttempted)
            {
                _bootLocalPlayerAttempted = true;
                try
                {
                    BootLocalPlayerToRoom();
                    // Give it one short chance to leave TitleScreen, then load
                    // Orientation ourselves if still stuck.
                    _directSceneLoadDueAt = DateTime.UtcNow.AddSeconds(2);
                    Plugin.Log.LogWarning(
                        "[BOOT-ROOM] one-shot BootLocalPlayerToDormRoom done; " +
                        "will direct-load Orientation if TitleScreen remains");
                    return;
                }
                catch (Exception e)
                {
                    var root = e.GetBaseException();
                    Plugin.Log.LogError(
                        $"[BOOT-ROOM] failed, falling back to the direct scene load: " +
                        $"{root.GetType().Name}: {root.Message}");
                }
            }

            // The stock room loader creates a Room-scoped dependency container
            // immediately before it asks Unity to load the room scene. A bare
            // SceneManager.LoadScene skips that lifecycle step. Without it the
            // room-level managers null-reference during Awake. Reproduce the
            // stock pre-load contract before touching the scene.
            if (!EnsureRoomDependencyContainer())
            {
                _roomDependencyContainerAttempts++;
                if (_roomDependencyContainerAttempts <= 10)
                {
                    _directSceneLoadDueAt = DateTime.UtcNow.AddMilliseconds(500);
                    if (!_roomDependencyContainerFailureLogged)
                    {
                        _roomDependencyContainerFailureLogged = true;
                        Plugin.Log.LogWarning(
                            "[ROOM-DI] session scope is not ready; delaying the " +
                            "Orientation scene instead of loading a broken room");
                    }
                    return;
                }

                _directSceneLoadDueAt = null;
                _directSceneLoadArmedAt = null;
                throw new InvalidOperationException(
                    "Rec Room's session dependency scope never became ready, so " +
                    "the Orientation scene was not loaded.");
            }

            _roomDependencyContainerAttempts = 0;
            _directSceneLoadDueAt = null;
            _directSceneLoadArmedAt = null;

            Plugin.Log.LogWarning(
                $"[ORIENTATION] room loader left TitleScreen active; loading " +
                $"bootstrap scene '{sceneName}' directly");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                sceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            // Do not retire the loading screen on a timer. The stock room
            // pipeline keeps it up until SceneSpawnManager has produced the
            // local Player/camera. Hiding after an arbitrary eight seconds is
            // what exposed the empty sky/white frame when player spawning had
            // not run yet.
            _hideLoadingScreenAt = null;

            // The shipped build order is significant. level31 / Orientation_additive
            // owns [Orientation_SceneManager], [CommonSceneSystems], and
            // [OrientationManager]. level32 / Orientation_Scene1 owns the actual
            // tutorial geometry and SceneSpawnPoints. Loading Scene1 first makes
            // OrientationSubScene.Awake dereference a missing RecRoomSceneManager.
            // Give the bootstrap scene a later frame to finish Awake/Start, then
            // load the tutorial level only after its real spawn manager exists.
            var additive = Plugin.OrientationAdditiveSceneName.Value?.Trim();
            if (!string.IsNullOrEmpty(additive))
            {
                _additiveSceneDueAt = DateTime.UtcNow.AddSeconds(1.0);
                Plugin.Log.LogInfo(
                    $"[ORIENTATION] scheduled tutorial level '{additive}' after " +
                    "the bootstrap scene initializes");
            }
        }
        catch (Exception e)
        {
            _directSceneLoadDueAt = null;
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[ORIENTATION] direct scene load failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    // Decisive check on whether the nameserver bootstrap actually landed: if the
    // service map is stored, these resolve to real URIs and RecNet is healthy;
    // if they come back null, nothing downstream can ever build a request.
    private static void LogRecNetServiceMapOnce()
    {
        if (_serviceMapLogged)
            return;
        if (string.IsNullOrEmpty(_activeLocalAccessToken))
            return;

        _serviceMapLogged = true;
        try
        {
            // The throwing getter cannot distinguish "no service map" from a
            // marshalling problem in this probe, so use the Try variant.
            var tryGetUri =
                AccessTools.Method(
                    typeof(HEEMOONFCAF),
                    "CJLNFDIGJFO",
                    new[]
                    {
                        typeof(BPIFHBEBGHO),
                        typeof(Il2CppSystem.Uri).MakeByRefType(),
                    });
            var getUri =
                AccessTools.Method(
                    typeof(HEEMOONFCAF),
                    "EANKOCIKIHM",
                    new[] { typeof(BPIFHBEBGHO) });

            var services = new[]
            {
                BPIFHBEBGHO.Auth,
                BPIFHBEBGHO.API,
                BPIFHBEBGHO.Rooms,
                BPIFHBEBGHO.Matchmaking,
                BPIFHBEBGHO.PlayerSettings,
            };

            foreach (var service in services)
            {
                var tried = "<no TryGet>";
                if (tryGetUri != null)
                {
                    try
                    {
                        var args = new object[] { service, null };
                        var found = (bool)tryGetUri.Invoke(null, args);
                        tried = $"found={found} uri={args[1]?.ToString() ?? "<null>"}";
                    }
                    catch (Exception e)
                    {
                        tried =
                            $"<TryGet threw {e.GetBaseException().GetType().Name}: " +
                            $"{e.GetBaseException().Message}>";
                    }
                }

                var direct = "<skipped>";
                if (getUri != null)
                {
                    try
                    {
                        direct =
                            getUri.Invoke(null, new object[] { service })?.ToString()
                            ?? "<null>";
                    }
                    catch (Exception e)
                    {
                        direct =
                            $"<threw {e.GetBaseException().GetType().Name}: " +
                            $"{e.GetBaseException().Message}>";
                    }
                }

                Plugin.Log.LogWarning(
                    $"[RECNET-MAP] {service} TryGet[{tried}] direct[{direct}]");
            }
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogWarning(
                $"[RECNET-MAP] probe failed: {root.GetType().Name}: {root.Message}");
        }
    }

    // The client receives a perfectly good service map (200, full JSON, success
    // handler runs) and still ends up with an empty service dictionary, so every
    // BPIFHBEBGHO lookup throws KeyNotFound and no RecNet request is ever built.
    // That single fact is what stops the loading screen, the room load and the
    // player spawn. Rather than keep guessing why the stock parse drops it, fill
    // HEEMOONFCAF's dictionary directly - the field is
    //   private static Dictionary<BPIFHBEBGHO, Uri> OHCJPLDPDLA
    // confirmed against the Il2CppDumper output for this exact GameAssembly.
    private static unsafe bool PopulateRecNetServiceMap()
    {
        var klass =
            IL2CPP.GetIl2CppClass("RecNet.Runtime.dll", "", "HEEMOONFCAF");
        if (klass == IntPtr.Zero)
            throw new InvalidOperationException("HEEMOONFCAF class not found.");

        var field =
            IL2CPP.il2cpp_class_get_field_from_name(klass, "OHCJPLDPDLA");
        if (field == IntPtr.Zero)
            throw new InvalidOperationException(
                "HEEMOONFCAF.OHCJPLDPDLA field not found.");

        IntPtr existing;
        IL2CPP.il2cpp_field_static_get_value(field, &existing);

        var map = existing == IntPtr.Zero
            ? null
            : new Il2CppSystem.Collections.Generic.Dictionary<
                BPIFHBEBGHO, Il2CppSystem.Uri>(existing);

        if (map == null)
        {
            map = new Il2CppSystem.Collections.Generic.Dictionary<
                BPIFHBEBGHO, Il2CppSystem.Uri>();
            var created = map.Pointer;
            IL2CPP.il2cpp_field_static_set_value(field, &created);
            Plugin.Log.LogWarning(
                "[RECNET-MAP] service dictionary was null; installed a new one");
        }

        // Install the OFFICIAL https host per service, not the local endpoint
        // and NOT one identical host for every entry. RecNet validates the
        // scheme when it constructs a request and rejects a plain http service
        // URI outright:
        //   "Exception constructing RecNet request (Post Matchmaking
        //    player/exclusivelogin): Invalid URI scheme"
        // so https is required. But installing the SAME https://api.rec.net for
        // all 26 services (the first version of this fix) produced
        //   [Error] https://api.rec.net
        //   Could not find an error code matching the message: 'https://api.rec.net'
        // at boot, before login - some part of the boot flow reacts badly to
        // every service resolving to one identical host. Give each service its
        // own distinct official host instead, matching exactly what this
        // server's own nameserver response already uses (server/main.py
        // SERVICE_MAP). The existing send-time redirect rewrites any *.rec.net
        // request to the local endpoint regardless of which subdomain it is, so
        // per-service hosts still land locally.
        var hosts = new Dictionary<BPIFHBEBGHO, string>
        {
            [BPIFHBEBGHO.Auth] = "auth.rec.net",
            [BPIFHBEBGHO.API] = "api.rec.net",
            [BPIFHBEBGHO.WWW] = "rec.net",
            [BPIFHBEBGHO.Notifications] = "notify.rec.net",
            [BPIFHBEBGHO.Images] = "img.rec.net",
            [BPIFHBEBGHO.CDN] = "cdn.rec.net",
            [BPIFHBEBGHO.Commerce] = "commerce.rec.net",
            [BPIFHBEBGHO.Matchmaking] = "match.rec.net",
            [BPIFHBEBGHO.Storage] = "storage.rec.net",
            [BPIFHBEBGHO.Chat] = "chat.rec.net",
            [BPIFHBEBGHO.Leaderboard] = "leaderboard.rec.net",
            [BPIFHBEBGHO.Accounts] = "accounts.rec.net",
            [BPIFHBEBGHO.Link] = "link.rec.net",
            [BPIFHBEBGHO.RoomComments] = "roomcomments.rec.net",
            [BPIFHBEBGHO.Clubs] = "clubs.rec.net",
            [BPIFHBEBGHO.Rooms] = "rooms.rec.net",
            [BPIFHBEBGHO.PlatformNotifications] = "platformnotifications.rec.net",
            [BPIFHBEBGHO.Moderation] = "moderation.rec.net",
            [BPIFHBEBGHO.DataCollection] = "datacollection.rec.net",
            [BPIFHBEBGHO.BugReporting] = "bugreporting.rec.net",
            [BPIFHBEBGHO.Discovery] = "discovery.rec.net",
            [BPIFHBEBGHO.PlayerSettings] = "playersettings.rec.net",
            [BPIFHBEBGHO.Studio] = "studio.rec.net",
            [BPIFHBEBGHO.GameLogs] = "gamelogs.rec.net",
            [BPIFHBEBGHO.Strings] = "strings.rec.net",
            [BPIFHBEBGHO.Econ] = "econ.rec.net",
        };

        var added = 0;
        foreach (BPIFHBEBGHO service in Enum.GetValues(typeof(BPIFHBEBGHO)))
        {
            try
            {
                if (map.ContainsKey(service))
                    continue;
                if (!hosts.TryGetValue(service, out var host))
                {
                    Plugin.Log.LogWarning(
                        $"[RECNET-MAP] no known host for {service}; leaving unset");
                    continue;
                }

                var builder = new Il2CppSystem.UriBuilder
                {
                    Scheme = "https",
                    Host = host,
                    Port = -1,
                    Path = "/",
                };
                var uri = builder.Uri;
                if (uri == null || string.IsNullOrEmpty(uri.Scheme))
                {
                    Plugin.Log.LogWarning(
                        $"[RECNET-MAP] could not build a Uri for {service} -> {host}");
                    continue;
                }

                map[service] = uri;
                added++;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    $"[RECNET-MAP] could not install {service}: " +
                    $"{e.GetBaseException().GetType().Name}");
            }
        }

        Plugin.Log.LogWarning(
            $"[RECNET-MAP] installed {added} distinct service endpoints " +
            $"(total now {map.Count})");
        return added > 0;
    }

    // Find a live component by native class name, bypassing the generated
    // wrapper. Types deriving from InjectedSingletonMonoBehaviour cannot be
    // named in a JIT'd method without a TypeLoadException, so never reference
    // them directly - go through the native class instead.
    private static UnityEngine.Object FindNativeComponent(
        string assembly,
        string className)
    {
        var klass = IL2CPP.GetIl2CppClass(assembly, "", className);
        if (klass == IntPtr.Zero)
            return null;
        var type = Il2CppType.TypeFromPointer(klass, className);

        // FindObjectOfType skips inactive objects, and UI like the loading
        // screen sits disabled until something raises it, so it would never be
        // found. FindObjectsOfTypeAll includes inactive instances.
        //
        // Do NOT fall back to Object.FindObjectOfType on this IL2CPP depot:
        // it NullReferenceExceptions when the type has no live instances and
        // was aborting the Orientation spawn path with "[SPAWN] failed".
        var all = UnityEngine.Resources.FindObjectsOfTypeAll(type);
        if (all == null)
            return null;

        for (var i = 0; i < all.Length; i++)
        {
            var candidate = all[i];
            if (candidate == null || candidate.Pointer == IntPtr.Zero)
                continue;

            // Prefer a live scene instance over prefab assets.
            var component = candidate.TryCast<UnityEngine.Component>();
            var go = component?.gameObject;
            if (go != null && go.scene.IsValid())
                return candidate;
        }

        for (var i = 0; i < all.Length; i++)
        {
            var candidate = all[i];
            if (candidate != null && candidate.Pointer != IntPtr.Zero)
                return candidate;
        }

        return null;
    }

    private static unsafe IntPtr InvokeNative(
        string className,
        string methodName,
        IntPtr instance,
        void** args,
        int argCount,
        string assemblyName = "Assembly-CSharp.dll")
    {
        var klass = IL2CPP.GetIl2CppClass(assemblyName, "", className);
        if (klass == IntPtr.Zero)
            throw new InvalidOperationException(
                $"{className} not found in {assemblyName}.");
        var method =
            IL2CPP.il2cpp_class_get_method_from_name(klass, methodName, argCount);
        if (method == IntPtr.Zero)
            throw new InvalidOperationException(
                $"{className}.{methodName} not found.");

        var exception = IntPtr.Zero;
        var result = IL2CPP.il2cpp_runtime_invoke(method, instance, args, ref exception);
        if (exception != IntPtr.Zero)
            throw new InvalidOperationException(
                $"{className}.{methodName} threw inside the game.");
        return result;
    }

    // Mirrors AOBBCBGOKEC.DLBMACPBGKB, the stock room provider's pre-load
    // callback. KMHLGEMLKMO owns Root/Session/Room dependency scopes and enum
    // value 2 is JAIGHIDJKOL.Room in this depot. Use raw IL2CPP calls because
    // several generated wrappers in this build carry invalid CLR constraints.
    private static unsafe bool EnsureRoomDependencyContainer()
    {
        const string dependencyAssembly = "RecRoom.AgInitialization.Runtime.dll";
        var root = InvokeNative(
            "KMHLGEMLKMO", "KFFEIKCJKKF", IntPtr.Zero, null, 0,
            dependencyAssembly);
        var session = InvokeNative(
            "KMHLGEMLKMO", "HHMJKAAAGDM", IntPtr.Zero, null, 0,
            dependencyAssembly);
        var room = InvokeNative(
            "KMHLGEMLKMO", "EKFBIOPDICB", IntPtr.Zero, null, 0,
            dependencyAssembly);

        if (root == IntPtr.Zero || session == IntPtr.Zero)
        {
            if (!_roomDependencyContainerFailureLogged)
            {
                Plugin.Log.LogWarning(
                    $"[ROOM-DI] prerequisite missing root=0x{root.ToInt64():X} " +
                    $"session=0x{session.ToInt64():X} room=0x{room.ToInt64():X}");
            }
            return false;
        }

        var created = false;
        if (room == IntPtr.Zero)
        {
            var roomScope = 2;
            var args = stackalloc void*[1];
            args[0] = &roomScope;
            room = InvokeNative(
                "KMHLGEMLKMO", "PKMPMLGBAME", IntPtr.Zero, args, 1,
                dependencyAssembly);
            created = true;
        }

        // Verify the global room slot, not only the factory's return value. The
        // scene managers resolve this exact slot during Awake.
        var installedRoom = InvokeNative(
            "KMHLGEMLKMO", "EKFBIOPDICB", IntPtr.Zero, null, 0,
            dependencyAssembly);
        if (room == IntPtr.Zero || installedRoom == IntPtr.Zero)
        {
            Plugin.Log.LogError(
                $"[ROOM-DI] room scope creation failed returned=0x{room.ToInt64():X} " +
                $"installed=0x{installedRoom.ToInt64():X}");
            return false;
        }

        if (!_roomDependencyContainerReadyLogged)
        {
            _roomDependencyContainerReadyLogged = true;
            _roomDependencyContainerFailureLogged = false;
            Plugin.Log.LogInfo(
                $"[ROOM-DI] ready root=0x{root.ToInt64():X} " +
                $"session=0x{session.ToInt64():X} room=0x{installedRoom.ToInt64():X} " +
                $"created={created}");
        }
        return true;
    }

    // Resources.FindObjectsOfTypeAll returns loaded ASSETS as well as scene
    // objects, so the first LoadingScreen it hands back is usually the prefab.
    // Setting IsVisible on a prefab makes the engine dutifully log
    // "LoadingScreen.IsVisible False -> True" while nothing renders, which is
    // exactly the symptom we had. Prefer an object that actually belongs to a
    // loaded scene, and if only the prefab exists, instantiate it.
    private static UnityEngine.Object ResolveLiveLoadingScreen()
    {
        var klass = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "LoadingScreen");
        if (klass == IntPtr.Zero)
            return null;

        var all = UnityEngine.Resources.FindObjectsOfTypeAll(
            Il2CppType.TypeFromPointer(klass, "LoadingScreen"));
        if (all == null || all.Length == 0)
        {
            Plugin.Log.LogWarning("[LOADING] no LoadingScreen object exists at all");
            return null;
        }

        UnityEngine.Object prefab = null;

        for (var i = 0; i < all.Length; i++)
        {
            var candidate = all[i];
            if (candidate == null || candidate.Pointer == IntPtr.Zero)
                continue;

            var screen = candidate.TryCast<LoadingScreen>();
            if (screen == null)
                continue;

            var go = screen.gameObject;
            if (go == null)
                continue;

            if (go.scene.IsValid())
            {
                // GameObject.activeSelf and Behaviour.enabled are independent.
                // A component can sit on an active object with enabled=false,
                // in which case Unity never calls Update/OnEnable/coroutines on
                // it - the object "exists" but nothing drives its fade.
                var behaviour = candidate.TryCast<UnityEngine.Behaviour>();
                Plugin.Log.LogWarning(
                    $"[LOADING] using live LoadingScreen '{go.name}' " +
                    $"in scene '{go.scene.name}' (activeSelf={go.activeSelf} " +
                    $"componentEnabled={behaviour?.enabled})");
                if (!go.activeSelf)
                    go.SetActive(true);
                if (behaviour != null && !behaviour.enabled)
                    behaviour.enabled = true;
                return candidate;
            }

            prefab ??= candidate;
        }

        if (prefab == null)
            return null;

        try
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            var cloneScreen = clone?.TryCast<LoadingScreen>();
            var cloneGo = cloneScreen?.gameObject;
            if (cloneGo == null)
            {
                Plugin.Log.LogWarning(
                    "[LOADING] instantiated LoadingScreen had no GameObject");
                return null;
            }

            cloneGo.SetActive(true);
            UnityEngine.Object.DontDestroyOnLoad(cloneGo);
            Plugin.Log.LogWarning(
                $"[LOADING] only the LoadingScreen prefab existed; instantiated it " +
                $"as '{cloneGo.name}' (scene='{cloneGo.scene.name}')");
            return clone;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[LOADING] could not instantiate the LoadingScreen prefab: " +
                $"{root.GetType().Name}: {root.Message}");
            return null;
        }
    }

    // The direct room path reaches LoadingScreen before the desktop platform
    // service has selected one of the stock presentation branches. In that
    // state IsVisible becomes true, but neither imageTemplate nor textTemplate
    // is activated and imageFade paints a bare black frame. Bind the prefab's
    // own image template explicitly and use the original
    // Activity_Image_Orientation texture shipped in resources.assets.
    private static unsafe void BindOrientationLoadingTemplate(
        LoadingScreen screen,
        string label)
    {
        if (screen == null || screen.Pointer == IntPtr.Zero)
            return;

        // Update checks this flag before drawing the progress bar and label.
        var lsClass =
            IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "LoadingScreen");
        var flagField = lsClass == IntPtr.Zero
            ? IntPtr.Zero
            : IL2CPP.il2cpp_class_get_field_from_name(lsClass, "IIJKKANBEJJ");
        if (flagField != IntPtr.Zero)
        {
            byte enabled = 1;
            IL2CPP.il2cpp_field_set_value(screen.Pointer, flagField, &enabled);
        }

        if (screen.canvas != null)
        {
            screen.canvas.gameObject.SetActive(true);
            screen.canvas.enabled = true;
        }

        if (screen.progressBar != null)
        {
            screen.progressBar.gameObject.SetActive(true);
            screen.progressBar.enabled = true;
        }

        if (screen.percentProgressText != null)
        {
            screen.percentProgressText.gameObject.SetActive(true);
            screen.percentProgressText.enabled = true;
        }

        // imageFade is the full-screen black transition layer. The normal room
        // loader animates it away before revealing the card; that animation is
        // absent on this fallback path, so keep the real card above a clear
        // transition instead of leaving the player on black.
        if (screen.imageFade != null)
        {
            if (!_loadingScreenImageFadeStateCaptured)
            {
                _loadingScreenImageFadeStateCaptured = true;
                _loadingScreenImageFadeWasEnabled = screen.imageFade.enabled;
            }
            screen.imageFade.enabled = false;
        }

        var image = screen.imageTemplate;
        if (screen.textTemplate?.template != null)
            screen.textTemplate.template.SetActive(false);
        if (image?.template == null)
            throw new InvalidOperationException(
                "The shipped LoadingScreen image template is missing.");

        image.template.SetActive(true);
        if (image.titleText != null)
            image.titleText.text = "Welcome to Rec Room";
        if (image.messageText != null)
            image.messageText.text =
                "Let's see what Rec Room is all about. Head inside to start playing!";
        if (image.nextRoomName != null)
            image.nextRoomName.text = string.IsNullOrWhiteSpace(label)
                ? "Orientation"
                : label.Trim().TrimStart('^');
        if (image.loadingText != null)
            image.loadingText.text = "DOWNLOADING...";

        UnityEngine.Texture2D orientationImage = null;
        var textures = UnityEngine.Resources.FindObjectsOfTypeAll(
            Il2CppType.Of<UnityEngine.Texture2D>());
        if (textures != null)
        {
            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i]?.TryCast<UnityEngine.Texture2D>();
                if (texture != null &&
                    string.Equals(
                        texture.name,
                        "Activity_Image_Orientation",
                        StringComparison.Ordinal))
                {
                    orientationImage = texture;
                    break;
                }
            }
        }

        orientationImage ??= LoadEmbeddedOrientationLoadingTexture();
        orientationImage ??= screen.customLoadScreenDefaultPicture;
        if (image.backgroundImage != null && orientationImage != null)
        {
            image.backgroundImage.gameObject.SetActive(true);
            image.backgroundImage.enabled = true;
            image.backgroundImage.texture = orientationImage;
        }

        if (screen.recroomLogo != null)
        {
            screen.recroomLogo.gameObject.SetActive(true);
            screen.recroomLogo.enabled = true;
        }

        if (!_loadingScreenTemplateBound)
        {
            _loadingScreenTemplateBound = true;
            Plugin.Log.LogWarning(
                $"[LOADING] bound shipped image template and " +
                $"'{orientationImage?.name ?? "<no texture>"}' texture");
        }
    }

    private static UnityEngine.Texture2D LoadEmbeddedOrientationLoadingTexture()
    {
        if (_embeddedOrientationLoadingTexture != null &&
            _embeddedOrientationLoadingTexture.Pointer != IntPtr.Zero)
        {
            return _embeddedOrientationLoadingTexture;
        }

        const string resourceName =
            "RecNetPlugin.Assets.Activity_Image_Orientation.png";
        try
        {
            using var stream = typeof(SendRequestPatch).Assembly
                .GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Plugin.Log.LogWarning(
                    $"[LOADING] embedded depot texture '{resourceName}' was not found");
                return null;
            }

            using var reader = new BinaryReader(stream);
            var bytes = reader.ReadBytes(checked((int)stream.Length));
            var texture = new UnityEngine.Texture2D(
                2,
                2,
                UnityEngine.TextureFormat.RGBA32,
                false);
            if (!UnityEngine.ImageConversion.LoadImage(
                    texture,
                    new Il2CppStructArray<byte>(bytes),
                    false))
            {
                UnityEngine.Object.Destroy(texture);
                Plugin.Log.LogWarning(
                    "[LOADING] could not decode the embedded Orientation texture");
                return null;
            }

            texture.name = "Activity_Image_Orientation";
            texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            UnityEngine.Object.DontDestroyOnLoad(texture);
            _embeddedOrientationLoadingTexture = texture;
            Plugin.Log.LogInfo(
                $"[LOADING] decoded original depot Orientation artwork " +
                $"{texture.width}x{texture.height}");
            return texture;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogWarning(
                $"[LOADING] embedded Orientation texture failed: " +
                $"{root.GetType().Name}: {root.Message}");
            return null;
        }
    }

    // Drives the game's own LoadingScreen for the Orientation handoff. The stock
    // room loader is what normally raises this UI and it never runs here, so the
    // transition was a bare fade with nothing on screen.
    private static unsafe void DriveLoadingScreen(string label, float progress)
    {
        if (_loadingScreen == null || _loadingScreen.Pointer == IntPtr.Zero)
        {
            _loadingScreen = ResolveLiveLoadingScreen();
            if (_loadingScreen == null || _loadingScreen.Pointer == IntPtr.Zero)
            {
                if (!_loadingScreenMissingLogged)
                {
                    _loadingScreenMissingLogged = true;
                    Plugin.Log.LogWarning(
                        "[LOADING] could not obtain a live LoadingScreen");
                }
                return;
            }
        }

        var ptr = _loadingScreen.Pointer;

        // The object sits inactive from boot, so Unity has not run Start() on it
        // yet at the moment we enable it. Setting IsVisible in that same frame
        // leaves the fade machinery uninitialised - the flag flips but the
        // canvas alpha never moves, which shows up as
        // visible=True fadingIn=False fadedIn=False and a blank screen. Give it
        // a few frames after activation before asking it to show.
        if (!_loadingScreenActivatedAt.HasValue)
        {
            _loadingScreenActivatedAt = DateTime.UtcNow;
            return;
        }
        if ((DateTime.UtcNow - _loadingScreenActivatedAt.Value).TotalSeconds < 0.35)
            return;

        if (!_loadingScreenShown)
        {
            _loadingScreenShown = true;
            MaintainOrientationCursorCapture();

            var screenForContent = _loadingScreen.TryCast<LoadingScreen>();
            BindOrientationLoadingTemplate(screenForContent, label);

            byte visible = 1;
            var visArgs = stackalloc void*[1];
            visArgs[0] = &visible;
            InvokeNative("LoadingScreen", "set_IsVisible", ptr, visArgs, 1);

            // set_IsVisible's own internal platform branch has been observed to
            // resolve to state 0 (FullyFadedOut) instead of starting the fade
            // on this hardware profile (Desktop_VRMissing) - the disassembly of
            // set_IsVisible shows a call MLBANGIKDEF(0) exactly on that path.
            // MLBANGIKDEF is the private state setter backing the
            // FullyFadedOut/FadingIn/FullyFadedIn/FadingOut state machine (field
            // at +0x18, confirmed against get_IsFadingIn/get_IsFullyFadedIn).
            // Call it directly with FadingIn (1) to force the fade the platform
            // branch was skipping.
            // Do NOT force the fade state here. That state machine drives
            // imageFade - the screen's fade-to-BLACK overlay - not the
            // visibility of the loading screen's content. Forcing FadingIn(1)
            // or FullyFadedIn(2) makes that black overlay opaque, which is
            // exactly the black screen: the loading screen genuinely was up
            // and taking input focus (the mouse cursor lock/unlock confirmed
            // it), just painted solid black over everything. Leaving the state
            // alone lets the game manage its own fade.

            // The explicit template binding owns the visible destination text;
            // SetLabel treats literal room names as localization keys and logs
            // a missing-table warning in this depot.
            if (!_loadingScreenTemplateBound && !string.IsNullOrEmpty(label))
            {
                var labelPtr = IL2CPP.ManagedStringToIl2Cpp(label);
                var labelArgs = stackalloc void*[1];
                labelArgs[0] = (void*)labelPtr;
                InvokeNative("LoadingScreen", "SetLabel", ptr, labelArgs, 1);
            }

            Plugin.Log.LogWarning(
                $"[LOADING] loading screen shown, destination '{label}'");
        }

        var clamped = Math.Max(0f, Math.Min(1f, progress));
        var progArgs = stackalloc void*[1];
        progArgs[0] = &clamped;
        InvokeNative("LoadingScreen", "SetProgress", ptr, progArgs, 1);


        // Confirm it is genuinely on screen rather than just flagged visible.
        // Logged on change only.
        try
        {
            var screen = _loadingScreen.TryCast<LoadingScreen>();
            var go = screen?.gameObject;

            string Get(string getter)
            {
                var m = AccessTools.Method(
                    typeof(LoadingScreen), getter, Type.EmptyTypes);
                return m == null ? "?" : (m.Invoke(screen, null)?.ToString() ?? "null");
            }

            // The game itself exposes this for exactly this question. Il2Cpp
            // interop turns the public field into get_/set_ methods rather
            // than a real FieldInfo, so it has to be read as a method.
            var debugString = Get("get_IsVisibleDebugString");

            var behaviour = _loadingScreen.TryCast<UnityEngine.Behaviour>();
            var componentEnabled = behaviour?.enabled;

            var state =
                $"visible={Get("get_IsVisible")} fadingIn={Get("get_IsFadingIn")} " +
                $"fadedIn={Get("get_IsFullyFadedIn")} " +
                $"activeInHierarchy={go?.activeInHierarchy} componentEnabled={componentEnabled} " +
                $"progress={clamped:0.00} debug='{debugString}'";
            if (state != _loadingScreenLastState)
            {
                _loadingScreenLastState = state;
                Plugin.Log.LogInfo($"[LOADING] {state}");
            }
        }
        catch
        {
            // diagnostics only
        }
    }

    public static void LoadingScreenUpdatePostfix()
    {
        // Runs on the LoadingScreen singleton, which lives in DontDestroyOnLoad
        // and so survives the Single scene load - the only tick available once
        // the title scene is gone.
        //
        if (_loadingScreenShown || _localPlayerSpawnStarted || _offlineLocomotionReady)
            MaintainOrientationCursorCapture();

        if (_additiveSceneDueAt.HasValue && DateTime.UtcNow >= _additiveSceneDueAt.Value)
        {
            var additive = Plugin.OrientationAdditiveSceneName.Value?.Trim();
            if (!string.IsNullOrEmpty(additive))
            {
                var existing = UnityEngine.SceneManagement.SceneManager.GetSceneByName(additive);
                if (existing.IsValid() && existing.isLoaded)
                {
                    _additiveSceneDueAt = null;
                    Plugin.Log.LogInfo(
                        $"[ORIENTATION] tutorial level '{additive}' was already " +
                        "loaded by RecRoomSceneManager; skipping the fallback load");
                    _roomContentReportAt = DateTime.UtcNow.AddSeconds(2.5);
                    if (!_orientationScenesReadyAt.HasValue)
                        _orientationScenesReadyAt = DateTime.UtcNow;
                    if (!_localPlayerSpawnDueAt.HasValue && !_localPlayerSpawnStarted)
                        _localPlayerSpawnDueAt = DateTime.UtcNow.AddMilliseconds(500);
                }
                else if (!TryGetOrientationBaseRuntimeReady(out var readiness))
                {
                    _orientationContentLoadAttempts++;
                    if (!_orientationBaseWaitLogged)
                    {
                        _orientationBaseWaitLogged = true;
                        Plugin.Log.LogWarning(
                            "[ORIENTATION] waiting for the bootstrap scene's real " +
                            $"spawn manager before loading '{additive}' ({readiness})");
                    }

                    if (_orientationContentLoadAttempts <= 40)
                    {
                        _additiveSceneDueAt = DateTime.UtcNow.AddMilliseconds(250);
                        // still fall through to spawn/gameplay below
                    }
                    else
                    {
                        _additiveSceneDueAt = null;
                        Plugin.Log.LogError(
                            "[ORIENTATION] bootstrap scene never produced a usable " +
                            $"RecRoomSceneManager/SceneSpawnManager ({readiness}); " +
                            "refusing to load a tutorial scene that would crash in Awake");
                        _roomContentReportAt = DateTime.UtcNow.AddSeconds(1);
                    }
                }
                else
                {
                    _additiveSceneDueAt = null;
                    if (!_orientationBaseReadyLogged)
                    {
                        _orientationBaseReadyLogged = true;
                        Plugin.Log.LogInfo(
                            $"[ORIENTATION] bootstrap runtime ready ({readiness})");
                    }

                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        additive,
                        UnityEngine.SceneManagement.LoadSceneMode.Additive);
                    Plugin.Log.LogWarning(
                            $"[ORIENTATION] loaded tutorial level '{additive}' " +
                            "additively after the bootstrap runtime became ready");

                    // The direct scene fallback bypasses ObjectModelCallbacks'
                    // RecreateLocalPlayerAsync routine. That stock routine is
                    // the sole caller of SceneSpawnManager.PHHFEHIGAAD in this
                    // depot. Schedule its player-spawn half only after Scene1's
                    // OrientationSubScene has registered the real spawn points.
                    _localPlayerSpawnDueAt = DateTime.UtcNow.AddMilliseconds(500);
                    _orientationScenesReadyAt = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    var root = e.GetBaseException();
                    Plugin.Log.LogError(
                        $"[ORIENTATION] additive load of '{additive}' failed: " +
                        $"{root.GetType().Name}: {root.Message}");
                }

                    _roomContentReportAt = DateTime.UtcNow.AddSeconds(2.5);
                }
            }
        }

        PumpOrientationLocalPlayerSpawn();
        ForceOrientationEnterIfStuck();

        // Continuous gameplay tick every LoadingScreen frame.
        OrientationGameplayTick();

        if (_roomContentReportAt.HasValue && DateTime.UtcNow >= _roomContentReportAt.Value)
        {
            _roomContentReportAt = null;
            try
            {
                var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
                var totalRoots = 0;
                var parts = new List<string>();
                for (var i = 0; i < count; i++)
                {
                    var sc = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    var roots = sc.GetRootGameObjects();
                    var n = roots == null ? 0 : roots.Length;
                    totalRoots += n;
                    parts.Add($"'{sc.name}'(loaded={sc.isLoaded} roots={n})");
                }

                Plugin.Log.LogWarning(
                    $"[ROOM] scenes={count} totalRootObjects={totalRoots} :: " +
                    string.Join(" ", parts));

                var cam = UnityEngine.Camera.main;
                if (cam == null)
                {
                    var allCams = UnityEngine.Camera.allCameras;
                    Plugin.Log.LogWarning(
                        $"[ROOM] Camera.main is NULL; total cameras={(allCams == null ? 0 : allCams.Length)}");
                }
                else
                {
                    var t = cam.transform.position;
                    Plugin.Log.LogWarning(
                        $"[ROOM] Camera.main='{cam.name}' pos=({t.x:0.0},{t.y:0.0},{t.z:0.0})");
                }

                // The room scenes are up and the managers exist, but nothing has
                // started the spawn. Do it now, then re-check shortly to see
                // whether the state actually advanced off Uninitialized.
                if (Plugin.StartPlayerSpawn.Value && !_playerSpawnStarted)
                {
                    _playerSpawnStarted = true;
                    try
                    {
                        // Use the real RecRoomSceneManager spawn path (PHHFEHIGAAD),
                        // not bare CLMOOCHEOHN lookup which often cannot find the manager.
                        if (!_localPlayerSpawnDueAt.HasValue && !_localPlayerSpawnStarted)
                            _localPlayerSpawnDueAt = DateTime.UtcNow;
                        _roomContentReportAt = DateTime.UtcNow.AddSeconds(4);
                    }
                    catch (Exception e)
                    {
                        var root = e.GetBaseException();
                        Plugin.Log.LogError(
                            $"[SPAWN] failed: {root.GetType().Name}: {root.Message}");
                    }
                }

                Plugin.Log.LogWarning(
                    $"[ROOM] localPlayerExists={GetLocalPlayerExists()} " +
                    $"spawnState={GetLocalPlayerSpawnState()}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    $"[ROOM] content report failed: " +
                    $"{e.GetBaseException().GetType().Name}");
            }
        }

        if (!_hideLoadingScreenAt.HasValue ||
            DateTime.UtcNow < _hideLoadingScreenAt.Value)
            return;

        _hideLoadingScreenAt = null;
        try
        {
            HideLoadingScreen();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[LOADING] retire failed: {e.GetBaseException().GetType().Name}");
        }
    }

    private static bool GetLocalPlayerExists()
    {
        try
        {
            var getter = AccessTools.Method(
                typeof(Player), "get_LocalPlayerExists", Type.EmptyTypes);
            return getter != null && getter.Invoke(null, null) is bool exists && exists;
        }
        catch
        {
            return false;
        }
    }

    private static string GetLocalPlayerSpawnState()
    {
        try
        {
            var getter = AccessTools.Method(
                typeof(SceneSpawnManager),
                "get_LocalPlayerSpawnState",
                Type.EmptyTypes);
            return getter?.Invoke(null, null)?.ToString() ?? "<null>";
        }
        catch (Exception e)
        {
            return $"<{e.GetBaseException().GetType().Name}>";
        }
    }

    // Creates the same no-op stack timer used by
    // ObjectModelCallbacks.RecreateLocalPlayerAsync. SceneSpawnManager's async
    // routine immediately dereferences this child timer, so passing null (as
    // older patches did) cannot work. Raw IL2CPP invocation avoids the invalid
    // CLR generic constraints emitted for this depot's generated wrappers.
    private static unsafe IntPtr CreateLocalPlayerSpawnTimer()
    {
        const string timerAssembly = "RecRoom.Datastructures.Runtime.dll";
        var klass = IL2CPP.GetIl2CppClass(
            timerAssembly, "", "ELGKOFCLCNI");
        if (klass == IntPtr.Zero)
            throw new InvalidOperationException(
                $"ELGKOFCLCNI was not found in {timerAssembly}.");

        var timer = IL2CPP.il2cpp_object_new(klass);
        if (timer == IntPtr.Zero)
            throw new InvalidOperationException("Could not allocate the stock stack timer.");

        // Nullable<int> is an eight-byte zero value here (HasValue=false).
        long noDepthLimit = 0;
        var ctorArgs = stackalloc void*[6];
        ctorArgs[0] = null;                 // title
        ctorArgs[1] = &noDepthLimit;        // Nullable<int>
        ctorArgs[2] = null;                 // Stopwatch
        ctorArgs[3] = null;                 // onStart
        ctorArgs[4] = null;                 // onStop
        ctorArgs[5] = null;                 // onDispose
        InvokeNative(
            "ELGKOFCLCNI", ".ctor", timer, ctorArgs, 6,
            timerAssembly);

        var child = InvokeNative(
            "ELGKOFCLCNI", "EIOEONLCOFN", timer, null, 0,
            timerAssembly);
        if (child == IntPtr.Zero)
            throw new InvalidOperationException("The stock stack timer had no child timer.");

        _localPlayerSpawnTimer = timer;
        return child;
    }

    // The production room loader starts RoomKeysManager's remote download and
    // waits for its status to leave Running before spawning. The direct
    // bundled-Orientation fallback has no remote room-key payload (and needs
    // none), so that download is never started and the stock player routine
    // waits forever in WaitingForRoomKeys. Complete only this offline room's
    // empty key gate through the manager's real private status setter.
    private static bool CompleteOfflineOrientationRoomKeysGate()
    {
        var all = UnityEngine.Resources.FindObjectsOfTypeAll(
            Il2CppType.Of<RecRoom.RoomKeysManager>());
        RecRoom.RoomKeysManager roomKeys = null;
        if (all != null)
        {
            for (var i = 0; i < all.Length; i++)
            {
                roomKeys = all[i]?.TryCast<RecRoom.RoomKeysManager>();
                if (roomKeys != null && roomKeys.Pointer != IntPtr.Zero)
                    break;
            }
        }

        if (roomKeys == null || roomKeys.Pointer == IntPtr.Zero)
        {
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] RoomKeysManager is not ready yet");
            return false;
        }

        var status = roomKeys.LIGGFJHLMPD;
        if (status == RecRoom.RoomKeysManager.PMGFILENCEI.Running)
        {
            var setter = AccessTools.Method(
                typeof(RecRoom.RoomKeysManager),
                "FLECLCNLEII",
                new[] { typeof(RecRoom.RoomKeysManager.PMGFILENCEI) });
            if (setter == null)
                throw new InvalidOperationException(
                    "RoomKeysManager status setter was not found.");

            setter.Invoke(
                roomKeys,
                new object[] { RecRoom.RoomKeysManager.PMGFILENCEI.Completed });
            status = roomKeys.LIGGFJHLMPD;
        }

        if (status != RecRoom.RoomKeysManager.PMGFILENCEI.Completed)
            throw new InvalidOperationException(
                $"Offline room-key gate ended in unexpected status {status}.");

        if (!_offlineOrientationRoomKeysLogged)
        {
            _offlineOrientationRoomKeysLogged = true;
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] completed empty room-key gate for bundled Orientation");
        }
        return true;
    }

    // ObjectModelCallbacks.RecreateLocalPlayerAsync does not use
    // PUNNetworkManager.PhotonNetworkedObjectBacking. It resolves
    // NKKMLDCLAFH from the ROOT dependency scope (KMHLGEMLKMO.KFFEIKCJKKF)
    // and passes that implementation to SceneSpawnManager. The static PUN
    // shortcut happens to implement the same interface, but it does not own a
    // real Player for this room; using it makes PhotonView.RpcPlayer throw
    // "networkedPlayer must be a Player" during Instantiating.
    private static unsafe IntPtr ResolveStockNetworkedObjectBacking()
    {
        const string dependencyAssembly = "RecRoom.AgInitialization.Runtime.dll";
        var rootPointer = InvokeNative(
            "KMHLGEMLKMO", "KFFEIKCJKKF", IntPtr.Zero, null, 0,
            dependencyAssembly);
        if (rootPointer == IntPtr.Zero)
            return IntPtr.Zero;

        // Use the generated wrapper only for this already-instantiated generic
        // method. IL2CPP generated a concrete
        // BHKHBEKDAPI.PEBNPJONHNJ<NKKMLDCLAFH>(string) body for this depot,
        // which is the exact call made at ObjectModelCallbacks.MoveNext
        // RVA 0x1BC65E7..0x1BC6601. Passing null selects the unnamed binding.
        var root = new BHKHBEKDAPI(rootPointer);
        var networking = root.PEBNPJONHNJ<NKKMLDCLAFH>(null);
        return networking?.Pointer ?? IntPtr.Zero;
    }

    // Player.Awake and several desktop locomotion components resolve the live
    // settings facade from Root DI. The normal online boot registers it after
    // player-preference download; our local server intentionally supplies
    // defaults, so that registration never occurs. Build the game's concrete
    // facade with the already-located preference service and register it before
    // the player prefab is instantiated.
    private static unsafe bool EnsureOfflinePlayerSettingsService()
    {
        const string dependencyAssembly = "RecRoom.AgInitialization.Runtime.dll";
        var rootPointer = InvokeNative(
            "KMHLGEMLKMO", "KFFEIKCJKKF", IntPtr.Zero, null, 0,
            dependencyAssembly);
        if (rootPointer == IntPtr.Zero)
            return false;

        var root = new BHKHBEKDAPI(rootPointer);
        var existing = root.PEBNPJONHNJ<BDAADBIABIH>(null);
        if (existing != null && existing.Pointer != IntPtr.Zero)
        {
            EnsureOfflineSettingDefaults(existing.Pointer);
            return true;
        }

        if (_playerPreferences == null || _playerPreferences.Pointer == IntPtr.Zero)
        {
            if (!_offlineSettingsServiceFailureLogged)
            {
                _offlineSettingsServiceFailureLogged = true;
                Plugin.Log.LogWarning(
                    "[PLAYER-SPAWN] cannot install local settings facade: " +
                    "player-preferences service is unavailable");
            }
            return false;
        }

        var settingsClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "EFBONOGGOPI");
        if (settingsClass == IntPtr.Zero)
            throw new InvalidOperationException(
                "The stock EFBONOGGOPI settings facade was not found.");

        var settingsPointer = IL2CPP.il2cpp_object_new(settingsClass);
        if (settingsPointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "Could not allocate the stock settings facade.");

        var preferencesPointer = _playerPreferences.Pointer;
        var args = stackalloc void*[1];
        args[0] = &preferencesPointer;
        InvokeNative(
            "EFBONOGGOPI", ".ctor", settingsPointer, args, 1);

        var settingsObject = new Il2CppSystem.Object(settingsPointer);
        var settings = settingsObject.TryCast<BDAADBIABIH>();
        if (settings == null || settings.Pointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "The stock settings facade did not implement BDAADBIABIH.");

        root.IEAEBHHLMJP<BDAADBIABIH>(settings, null);
        var installed = root.PEBNPJONHNJ<BDAADBIABIH>(null);
        if (installed == null || installed.Pointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "Root DI rejected the stock settings facade.");

        EnsureOfflineSettingDefaults(installed.Pointer);

        _offlineSettingsServiceFailureLogged = false;
        if (!_offlineSettingsServiceReadyLogged)
        {
            _offlineSettingsServiceReadyLogged = true;
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] installed stock local settings facade " +
                $"settings=0x{installed.Pointer.ToInt64():X}");
        }
        return true;
    }

    // The normal room provider assigns ObjectModelManager.CIFMMJJOMMN while it
    // builds the room pipeline. Our direct bundled-scene fallback already has
    // the real Root/Session/Room containers, but it bypasses that one static
    // assignment. Player.Awake then sees ObjectModelManager.KLKODJCENGP as
    // null and aborts before enabling the desktop camera, controls and avatar.
    // Reconnect the manager to the shipped DI graph and only create the game's
    // own local object-model container if no scope has resolved one yet.
    private static unsafe bool EnsureOfflineObjectModelService()
    {
        const string dependencyAssembly = "RecRoom.AgInitialization.Runtime.dll";
        var rootPointer = InvokeNative(
            "KMHLGEMLKMO", "KFFEIKCJKKF", IntPtr.Zero, null, 0,
            dependencyAssembly);
        var sessionPointer = InvokeNative(
            "KMHLGEMLKMO", "HHMJKAAAGDM", IntPtr.Zero, null, 0,
            dependencyAssembly);
        var roomPointer = InvokeNative(
            "KMHLGEMLKMO", "EKFBIOPDICB", IntPtr.Zero, null, 0,
            dependencyAssembly);
        if (rootPointer == IntPtr.Zero)
            return false;

        var root = new BHKHBEKDAPI(rootPointer);
        var rootSetter = AccessTools.Method(
            typeof(RecRoom.ObjectModel.ObjectModelManager),
            "JICAHKBBPJC",
            new[] { typeof(BHKHBEKDAPI) });
        if (rootSetter == null)
            throw new InvalidOperationException(
                "ObjectModelManager root-container setter was not found.");

        // This setter deliberately clears the manager's cached room object
        // model, so call it once rather than on every spawn-pump frame.
        if (!_offlineObjectModelRootInstalled)
        {
            rootSetter.Invoke(null, new object[] { root });
            _offlineObjectModelRootInstalled = true;
        }

        var containerGetter = AccessTools.Method(
            typeof(RecRoom.ObjectModel.ObjectModelManager),
            "HEMKJOJALNN",
            Type.EmptyTypes);
        var serviceGetter = AccessTools.Method(
            typeof(RecRoom.ObjectModel.ObjectModelManager),
            "HPGAKFKICFA",
            Type.EmptyTypes);
        var containerSetter = AccessTools.Method(
            typeof(RecRoom.ObjectModel.ObjectModelManager),
            "MOCKGNDJLGK",
            new[] { typeof(GHOBGEJJNGE) });
        if (containerGetter == null || serviceGetter == null || containerSetter == null)
            throw new InvalidOperationException(
                "ObjectModelManager accessors were not found.");

        var container = containerGetter.Invoke(null, null) as GHOBGEJJNGE;
        if (container == null || container.Pointer == IntPtr.Zero)
        {
            // The root is normally enough because ObjectModelManager lazily
            // resolves GHOBGEJJNGE from it. Also inspect child scopes because
            // the direct room provider may have registered the binding there.
            var scopePointers = new[] { rootPointer, sessionPointer, roomPointer };
            for (var i = 0; i < scopePointers.Length; i++)
            {
                var pointer = scopePointers[i];
                if (pointer == IntPtr.Zero)
                    continue;
                var scope = new BHKHBEKDAPI(pointer);
                if (scope.KNEMJPBHGAN<GHOBGEJJNGE>(out var candidate) &&
                    candidate != null && candidate.Pointer != IntPtr.Zero)
                {
                    container = candidate;
                    break;
                }
            }
        }

        if (container == null || container.Pointer == IntPtr.Zero)
        {
            // Use Rec Room's concrete factory, not a replacement object model.
            // NOEEIPFGABG is the already-running ECS/world service registered
            // by the stock root bootstrap, and GDJBKNLFFHN creates the matching
            // GHOBGEJJNGE/FJFNAPPGKJO pair used by this depot.
            var world = root.PEBNPJONHNJ<NOEEIPFGABG>(null);
            if (world == null || world.Pointer == IntPtr.Zero)
            {
                if (!_offlineObjectModelFailureLogged)
                {
                    _offlineObjectModelFailureLogged = true;
                    Plugin.Log.LogWarning(
                        "[OBJECT-MODEL] stock ECS world is not ready yet");
                }
                return false;
            }

            var factory = AccessTools.Method(
                typeof(GDJBKNLFFHN),
                "PELCFOCODBC",
                new[] { typeof(NOEEIPFGABG), typeof(GEDGEKALBHM) });
            if (factory == null)
                throw new InvalidOperationException(
                    "The stock local object-model factory was not found.");

            container = factory.Invoke(
                null,
                new object[] { world, GEDGEKALBHM.Default }) as GHOBGEJJNGE;
            if (container == null || container.Pointer == IntPtr.Zero)
                throw new InvalidOperationException(
                    "The stock local object-model factory returned null.");
            _offlineObjectModelContainer = container;
            containerSetter.Invoke(null, new object[] { container });
        }

        var service = serviceGetter.Invoke(null, null) as FJFNAPPGKJO;
        if (service == null || service.Pointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "ObjectModelManager did not expose FJFNAPPGKJO after initialization.");

        if (!EnsureOfflineUnitySceneService(container, service))
            return false;

        _offlineObjectModelFailureLogged = false;
        if (!_offlineObjectModelReadyLogged)
        {
            _offlineObjectModelReadyLogged = true;
            Plugin.Log.LogWarning(
                $"[OBJECT-MODEL] stock room object model is ready " +
                $"root=0x{rootPointer.ToInt64():X} " +
                $"container=0x{container.Pointer.ToInt64():X} " +
                $"service=0x{service.Pointer.ToInt64():X}");
        }
        return true;
    }

    // Player.Awake asks the object-model service container for LIOPMJFBPIJ
    // (the shipped RecRoom.ObjectModel.Systems.UnitySceneService) immediately
    // after obtaining ObjectModelManager.HPGAKFKICFA. A room created by the
    // normal provider has already run the Assembly-CSharp service registerers;
    // the direct bundled-scene fallback can expose EDBMMDOKCAG while that one
    // scene bridge is still absent. In that state Player.Awake reaches the
    // object model successfully and then throws before it can initialize the
    // local avatar, camera and controls.
    //
    // Prefer the service produced by the game's own container. If it is absent,
    // instantiate the exact shipped UnitySceneService, inject the already-live
    // WorldService and SceneService, register it under both its concrete and
    // interface types, and run its normal object-model lifecycle hook.
    private static unsafe bool EnsureOfflineUnitySceneService(
        GHOBGEJJNGE objectModelContainer,
        FJFNAPPGKJO objectModel)
    {
        var lifetimeWorld = objectModelContainer.ECODFCBICCB;
        var services = lifetimeWorld?.BEHLICJJGMD(FPOGPJMGMEG.OMRoom);
        if (services == null || services.Pointer == IntPtr.Zero)
        {
            if (!_offlineUnitySceneServiceFailureLogged)
            {
                _offlineUnitySceneServiceFailureLogged = true;
                Plugin.Log.LogWarning(
                    "[OBJECT-MODEL] OMRoom service container is not ready yet");
            }
            return false;
        }

        var interfaceClass = IL2CPP.GetIl2CppClass(
            "RecRoom.ObjectModel.Interfaces.Runtime.dll", "", "LIOPMJFBPIJ");
        var concreteClass = IL2CPP.GetIl2CppClass(
            "RecRoom.ObjectModel.Systems.Runtime.dll",
            "RecRoom.ObjectModel.Systems",
            "UnitySceneService");
        var sceneServiceClass = IL2CPP.GetIl2CppClass(
            "RecRoom.ObjectModel.Systems.Runtime.dll",
            "RecRoom.ObjectModel.Systems",
            "SceneService");
        if (interfaceClass == IntPtr.Zero ||
            concreteClass == IntPtr.Zero ||
            sceneServiceClass == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "The stock UnitySceneService object-model types were not found.");
        }

        var interfaceType = Il2CppType.TypeFromPointer(
            interfaceClass, "LIOPMJFBPIJ");
        var concreteType = Il2CppType.TypeFromPointer(
            concreteClass, "RecRoom.ObjectModel.Systems.UnitySceneService");
        var sceneServiceType = Il2CppType.TypeFromPointer(
            sceneServiceClass, "RecRoom.ObjectModel.Systems.SceneService");

        var existing = services.PEBNPJONHNJ(interfaceType);
        if (existing != null && existing.Pointer != IntPtr.Zero)
        {
            _offlineUnitySceneServiceFailureLogged = false;
            if (!_offlineUnitySceneServiceReadyLogged)
            {
                _offlineUnitySceneServiceReadyLogged = true;
                Plugin.Log.LogWarning(
                    $"[OBJECT-MODEL] stock UnitySceneService is ready " +
                    $"service=0x{existing.Pointer.ToInt64():X} repaired=False");
            }
            return true;
        }

        var world = objectModel.PHIKONIBLIA;
        var sceneService = services.PEBNPJONHNJ(sceneServiceType);
        if (world == null || world.Pointer == IntPtr.Zero ||
            sceneService == null || sceneService.Pointer == IntPtr.Zero)
        {
            if (!_offlineUnitySceneServiceFailureLogged)
            {
                _offlineUnitySceneServiceFailureLogged = true;
                Plugin.Log.LogWarning(
                    "[OBJECT-MODEL] UnitySceneService prerequisites are not ready " +
                    $"world=0x{(world?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                    $"scene=0x{(sceneService?.Pointer ?? IntPtr.Zero).ToInt64():X}");
            }
            return false;
        }

        var servicePointer = IL2CPP.il2cpp_object_new(concreteClass);
        if (servicePointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "Could not allocate the stock UnitySceneService.");

        var constructor = IL2CPP.il2cpp_class_get_method_from_name(
            concreteClass, ".ctor", 0);
        if (constructor == IntPtr.Zero)
            throw new InvalidOperationException(
                "UnitySceneService constructor was not found.");
        var exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(
            constructor, servicePointer, null, ref exception);
        if (exception != IntPtr.Zero)
            throw new InvalidOperationException(
                "UnitySceneService constructor threw inside the game.");

        WriteIl2CppReferenceField(
            servicePointer, concreteClass, "LIPGMHFIHCI", world.Pointer);
        WriteIl2CppReferenceField(
            servicePointer,
            concreteClass,
            "POMGNNIENEK",
            sceneService.Pointer);

        var serviceObject = new Il2CppSystem.Object(servicePointer);
        var dependencyContainer = services.CIFMMJJOMMN;
        if (dependencyContainer == null || dependencyContainer.Pointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "Object-model dependency container was null.");

        dependencyContainer.IEAEBHHLMJP(
            interfaceType, serviceObject, null);
        dependencyContainer.IEAEBHHLMJP(
            concreteType, serviceObject, null);

        var lifecycle = IL2CPP.il2cpp_class_get_method_from_name(
            concreteClass, "PBJBMEPEDIE", 1);
        if (lifecycle == IntPtr.Zero)
            throw new InvalidOperationException(
                "UnitySceneService lifecycle hook was not found.");
        var servicesPointer = services.Pointer;
        var args = stackalloc void*[1];
        args[0] = &servicesPointer;
        exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(
            lifecycle, servicePointer, args, ref exception);
        if (exception != IntPtr.Zero)
            throw new InvalidOperationException(
                "UnitySceneService lifecycle hook threw inside the game.");

        existing = services.PEBNPJONHNJ(interfaceType);
        if (existing == null || existing.Pointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "Object-model DI rejected the stock UnitySceneService.");

        _offlineUnitySceneServiceFailureLogged = false;
        if (!_offlineUnitySceneServiceReadyLogged)
        {
            _offlineUnitySceneServiceReadyLogged = true;
            Plugin.Log.LogWarning(
                $"[OBJECT-MODEL] stock UnitySceneService is ready " +
                $"service=0x{existing.Pointer.ToInt64():X} repaired=True");
        }
        return true;
    }

    private static unsafe void WriteIl2CppReferenceField(
        IntPtr instance,
        IntPtr klass,
        string fieldName,
        IntPtr value)
    {
        var field = IL2CPP.il2cpp_class_get_field_from_name(klass, fieldName);
        if (field == IntPtr.Zero)
            throw new InvalidOperationException(
                $"{fieldName} was not found on the stock IL2CPP type.");
        IL2CPP.il2cpp_field_set_value(instance, field, &value);
    }

    private static unsafe void WriteIl2CppBoolField(
        IntPtr instance,
        IntPtr klass,
        string fieldName,
        bool value)
    {
        var field = IL2CPP.il2cpp_class_get_field_from_name(klass, fieldName);
        if (field == IntPtr.Zero)
            throw new InvalidOperationException(
                $"{fieldName} was not found on the stock IL2CPP type.");
        byte nativeValue = value ? (byte)1 : (byte)0;
        IL2CPP.il2cpp_field_set_value(instance, field, &nativeValue);
    }

    private static unsafe void WriteIl2CppInt32Field(
        IntPtr instance,
        IntPtr klass,
        string fieldName,
        int value)
    {
        var field = IL2CPP.il2cpp_class_get_field_from_name(klass, fieldName);
        if (field == IntPtr.Zero)
            throw new InvalidOperationException(
                $"{fieldName} was not found on the stock IL2CPP type.");
        IL2CPP.il2cpp_field_set_value(instance, field, &value);
    }

    private static unsafe void EnsureOfflineSettingDefaults(IntPtr settingsPointer)
    {
        if (_offlineSettingDefaultsInitialized)
            return;
        if (settingsPointer == IntPtr.Zero)
            throw new InvalidOperationException(
                "Cannot initialize settings defaults on a null facade.");

        // In the normal online pipeline SettingsManagerBootstrapper injects
        // this serialized object after preferences download. Direct bundled
        // Orientation leaves EFBONOGGOPI.LFGILPFBNHA null, causing both
        // Player.Awake and PlayerPersonalSpace.Start to fail. Supply complete
        // local defaults for every buffer enum through the facade's own public
        // initializer instead of bypassing those gameplay systems.
        var defaults = new RecRoom.Settings.SettingDefaults();

        var teleport = new Il2CppStructArray<
            RecRoom.Settings.TeleportBufferSize>(3);
        teleport[0] = new RecRoom.Settings.TeleportBufferSize
        {
            buffer = INLNPELFEIN.Small,
            distance = 0.45f,
        };
        teleport[1] = new RecRoom.Settings.TeleportBufferSize
        {
            buffer = INLNPELFEIN.Medium,
            distance = 0.75f,
        };
        teleport[2] = new RecRoom.Settings.TeleportBufferSize
        {
            buffer = INLNPELFEIN.Large,
            distance = 1.10f,
        };
        defaults.teleportBufferSizes = teleport;

        var ignore = new Il2CppStructArray<
            RecRoom.Settings.IgnoreBufferSize>(6);
        ignore[0] = new RecRoom.Settings.IgnoreBufferSize
        {
            buffer = BAAJHMLDJCC.Disabled,
            localDistance = 0f,
            remoteDistance = 0f,
            invisibleDistance = 0f,
        };
        ignore[1] = new RecRoom.Settings.IgnoreBufferSize
        {
            buffer = BAAJHMLDJCC.Small,
            localDistance = 0.35f,
            remoteDistance = 0.35f,
            invisibleDistance = 0.20f,
        };
        ignore[2] = new RecRoom.Settings.IgnoreBufferSize
        {
            buffer = BAAJHMLDJCC.Medium,
            localDistance = 0.60f,
            remoteDistance = 0.60f,
            invisibleDistance = 0.35f,
        };
        ignore[3] = new RecRoom.Settings.IgnoreBufferSize
        {
            buffer = BAAJHMLDJCC.Large,
            localDistance = 0.90f,
            remoteDistance = 0.90f,
            invisibleDistance = 0.55f,
        };
        ignore[4] = new RecRoom.Settings.IgnoreBufferSize
        {
            buffer = BAAJHMLDJCC.Junior,
            localDistance = 1.20f,
            remoteDistance = 1.20f,
            invisibleDistance = 0.75f,
        };
        ignore[5] = new RecRoom.Settings.IgnoreBufferSize
        {
            buffer = BAAJHMLDJCC.Blocked,
            localDistance = 1000f,
            remoteDistance = 1000f,
            invisibleDistance = 1000f,
        };
        defaults.ignoreBufferSizes = ignore;

        var defaultsPointer = defaults.Pointer;
        var args = stackalloc void*[1];
        args[0] = (void*)defaultsPointer;
        InvokeNative(
            "EFBONOGGOPI", "NPFGFFCDMGH", settingsPointer, args, 1);

        _offlineSettingDefaultsInitialized = true;
        Plugin.Log.LogWarning(
            "[PLAYER-SPAWN] initialized stock settings facade with complete " +
            "offline movement/personal-space defaults");
    }

    private static unsafe bool EnsureOfflinePlayerRegistry()
    {
        var existing = BLACGKAKJIG.KGGJIHLJBIH;
        if (existing != null && existing.Pointer != IntPtr.Zero)
            return true;

        var registry = new BLACGKAKJIG();
        if (registry.Pointer == IntPtr.Zero)
            return false;

        var klass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "BLACGKAKJIG");
        var field = klass == IntPtr.Zero
            ? IntPtr.Zero
            : IL2CPP.il2cpp_class_get_field_from_name(klass, "OKMIOODAMOI");
        if (field == IntPtr.Zero)
            throw new InvalidOperationException(
                "The stock player-registry singleton field was not found.");

        var registryPointer = registry.Pointer;
        IL2CPP.il2cpp_field_static_set_value(field, &registryPointer);

        // Run the depot's normal initializer now that the singleton exists. It
        // wires the registry's player-added/removed events into Root DI.
        try
        {
            AccessTools.Method(
                    typeof(BLACGKAKJIG), "DIICKCLOPJG", Type.EmptyTypes)
                ?.Invoke(null, null);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] player registry event wiring deferred: " +
                $"{e.GetBaseException().GetType().Name}");
        }

        existing = BLACGKAKJIG.KGGJIHLJBIH;
        if (existing == null || existing.Pointer == IntPtr.Zero)
            return false;

        if (!_offlinePlayerRegistryReadyLogged)
        {
            _offlinePlayerRegistryReadyLogged = true;
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] installed stock local player registry " +
                $"registry=0x{existing.Pointer.ToInt64():X}");
        }
        return true;
    }

    // StartInOfflineMode creates the PUN client, but it does not put that
    // client in a room. SceneSpawnManager sends its spawn request to
    // PhotonNetwork.MasterClient; without an offline room that property is
    // null and PhotonView.RpcPlayer throws "networkedPlayer must be a Player".
    // LBAMFDKPLEJ is this depot's PhotonNetwork.JoinOrCreateRoom overload
    // (confirmed from its native JoinOrCreateRoom diagnostics).
    private static bool EnsureOfflinePhotonRoom()
    {
        var localPlayer = DBMCMCHBCII.JLDBICMPKFJ;
        var masterClient = DBMCMCHBCII.JMNHODEAPNM;
        if (masterClient != null && masterClient.Pointer != IntPtr.Zero)
        {
            if (!_offlinePhotonRoomReadyLogged)
            {
                _offlinePhotonRoomReadyLogged = true;
                Plugin.Log.LogWarning(
                    $"[PHOTON-ROOM] ready offline={DBMCMCHBCII.KKPEIJDGKAA} " +
                    $"local=0x{(localPlayer?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                    $"master=0x{masterClient.Pointer.ToInt64():X}");
            }
            return true;
        }

        // Force offline mode every attempt (PUN can clear it on failed online joins).
        try
        {
            DBMCMCHBCII.KKPEIJDGKAA = true;
            var all = UnityEngine.Resources.FindObjectsOfTypeAll(
                Il2CppInterop.Runtime.Il2CppType.Of<Photon.Pun.ServerSettings>());
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    var settings = all[i]?.TryCast<Photon.Pun.ServerSettings>();
                    if (settings != null)
                        settings.StartInOfflineMode = true;
                }
            }
        }
        catch
        {
            // best-effort
        }

        // Retry join — a single failed attempt used to lock us out forever.
        if (_offlinePhotonRoomJoinAttempts >= 25)
            return false;

        // Throttle joins to ~4/sec
        if (_offlinePhotonRoomLastJoinAt.HasValue &&
            (DateTime.UtcNow - _offlinePhotonRoomLastJoinAt.Value).TotalMilliseconds < 250)
            return false;

        _offlinePhotonRoomLastJoinAt = DateTime.UtcNow;
        _offlinePhotonRoomJoinAttempts++;

        var options = new ABBPINLGGMK();
        var accepted = DBMCMCHBCII.LBAMFDKPLEJ(
            "FluxRecOrientation",
            options,
            KNIKHDEJMLA.OKLBJLNFLNK,
            null);
        if (_offlinePhotonRoomJoinAttempts <= 3 || accepted)
        {
            Plugin.Log.LogWarning(
                $"[PHOTON-ROOM] JoinOrCreateRoom attempt={_offlinePhotonRoomJoinAttempts} " +
                $"accepted={accepted} offline={DBMCMCHBCII.KKPEIJDGKAA} " +
                $"local=0x{(DBMCMCHBCII.JLDBICMPKFJ?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                $"master=0x{(DBMCMCHBCII.JMNHODEAPNM?.Pointer ?? IntPtr.Zero).ToInt64():X}");
        }

        masterClient = DBMCMCHBCII.JMNHODEAPNM;
        return masterClient != null && masterClient.Pointer != IntPtr.Zero;
    }

    // Replays the exact local-player half of the stock object-model callback:
    // root-DI networking backing + CancellationToken.None + a valid stack
    // timer are passed to the live scene's SceneSpawnManager. The real Rec
    // Room Player prefab, hands, camera and Orientation logic are instantiated
    // by the game's own code; no placeholder avatar/camera is created here.
    private static unsafe bool StartOrientationLocalPlayerSpawn()
    {
        // Player.Awake reaches the real desktop-controller and audio singleton
        // after its avatar/object-model setup. Production loads these from
        // build scene 2 (late_main_root) before any room player is created.
        // The direct Orientation fallback previously skipped that core scene,
        // leaving LocalPlayerController.Instance null and aborting Awake before
        // the stock camera/input controller could be attached.
        if (!EnsureOfflineCoreGameplayRuntime())
            return false;

        var manager = InvokeStatic<RecRoomSceneManager>("get_Instance");
        if (manager == null || manager.Pointer == IntPtr.Zero)
            return false;

        var spawnGetter = AccessTools.Method(
            typeof(RecRoomSceneManager), "get_SpawnManager", Type.EmptyTypes);
        var spawnManager = spawnGetter?.Invoke(manager, null) as SceneSpawnManager;
        if (spawnManager == null || spawnManager.Pointer == IntPtr.Zero)
            return false;

        if (!CompleteOfflineOrientationRoomKeysGate())
            return false;

        if (!EnsureOfflinePhotonRoom())
            return false;

        if (!EnsureOfflinePlayerRegistry())
            return false;

        if (!EnsureOfflinePlayerSettingsService())
            return false;

        if (!EnsureOfflineObjectModelService())
            return false;

        // PHHFEHIGAAD hard-crashes on this path. Use the game's real local
        // entry: LocalPlayerController.InstantiateLocalPlayer, with Photon
        // Instantiate('[Player]') as fallback.
        _localPlayerSpawnStarted = true;
        _localPlayerSpawnStartedAt = DateTime.UtcNow;

        try
        {
            StartLocalPlayerSpawnViaSceneManagerSafe(spawnManager);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] CLMOOCHEOHN soft-fail: {e.GetBaseException().Message}");
        }

        var pos = ResolveOrientationSpawnPose(out var rot);

        // 1) ScreenPlayerController.NGOEPFPOEGG — real desktop path (camera,
        //    WASD, hands, head height). 2) Photon '[Player]'. 3) LPC native.
        try
        {
            if (TryScreenPlayerControllerInstantiate(pos, rot, out var spcDetail))
            {
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] ScreenPlayerController.NGOEPFPOEGG: {spcDetail}");
                FinishLocalPlayerSpawnPresentation();
                return true;
            }

            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] ScreenPlayerController spawn failed: {spcDetail}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] ScreenPlayerController spawn threw: " +
                $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
        }

        try
        {
            if (TryPhotonInstantiateLocalPlayer(pos, rot, out var goDetail))
            {
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] Photon instantiated local player: {goDetail}");
                FinishLocalPlayerSpawnPresentation();
                return true;
            }

            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] Photon Instantiate failed: {goDetail}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] Photon Instantiate threw: " +
                $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
        }

        try
        {
            if (TryLocalPlayerControllerInstantiate(pos, rot, out var lpcDetail))
            {
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] LocalPlayerController.InstantiateLocalPlayer: {lpcDetail}");
                FinishLocalPlayerSpawnPresentation();
                return true;
            }

            Plugin.Log.LogError(
                $"[PLAYER-SPAWN] LocalPlayerController path failed: {lpcDetail}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(
                $"[PLAYER-SPAWN] LocalPlayerController path threw " +
                $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
        }

        _localPlayerSpawnStarted = false;
        Plugin.Log.LogError(
            "[PLAYER-SPAWN] ScreenPlayer + Photon + LocalPlayerController all failed");
        return false;
    }

    private static void FinishLocalPlayerSpawnPresentation()
    {
        try
        {
            var setter = AccessTools.Method(
                typeof(SceneSpawnManager), "set_LocalPlayerSpawnState",
                new[] { typeof(SceneSpawnManager.GOHDFONKFML) });
            setter?.Invoke(null, new object[]
            {
                SceneSpawnManager.GOHDFONKFML.SpawnedAndFadedIn,
            });
        }
        catch { /* best effort */ }

        // Immediate + delayed repairs: Player.Awake used to NRE before the two
        // core scenes were loaded, leaving no desktop controller.
        _suppressScreenPlayerTicks = true;
        _offlineLocomotionReady = true;
        try
        {
            var pl = FindLiveLocalPlayer();
            if (pl != null)
                _spawnFloorY = pl.transform.position.y;
        }
        catch { /* ignore */ }
        RunPostSpawnPresentationRepairs(force: true);
        PublishOfflineLocalPlayerLifecycle();
        _postSpawnRepairUntil = DateTime.UtcNow.AddSeconds(20);
        _postSpawnRepairNextAt = DateTime.UtcNow.AddSeconds(0.35);
        _offlineGameplayRepairActive = true;
        if (_realAvatarMounted)
        {
            // The customize-page puppet is already the live player visual.
            // Rebuilding the incomplete stock PlayerAvatar a second time is
            // redundant and crashed in stage 2 on the previous validation.
            _avatarApplySucceeded = true;
            _avatarApplyStage = 4;
            _avatarApplyUntil = null;
            _avatarApplyNextAt = null;
        }
        else
        {
            // Defer the real-puppet handoff if it was not available yet.
            _avatarApplySucceeded = false;
            _avatarApplyFailLogged = false;
            _playerAvatarMefTried = false;
            _avatarOutfitTried = false;
            _avatarApplyStage = 0;
            _avatarApplyUntil = DateTime.UtcNow.AddSeconds(30);
            _avatarApplyNextAt = DateTime.UtcNow.AddSeconds(0.75);
        }
        EnsureLoadingScreenTickHostAlive();
        // Minimal ClassInjector driver — only after player is cached.
        try { RecNetPlugin.OrientationGameplayDriver.EnsureInstalled(); }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[GAMEPLAY] driver: " + e.GetBaseException().Message);
        }
        Plugin.Log.LogWarning(
            _stockScreenPlayerReady
                ? "[GAMEPLAY] stock desktop controller + tutorial lifecycle ready"
                : "[GAMEPLAY] fallback walk/cursor tick: LoadingScreen + Camera + Driver");
    }

    private static void PublishOfflineLocalPlayerLifecycle()
    {
        if (_offlineLocalPlayerLifecyclePublished)
            return;

        var player = FindLiveLocalPlayer();
        if (player == null || player.Pointer == IntPtr.Zero)
            return;

        try
        {
            var getInitialized = AccessTools.Method(
                typeof(Player), "get_IsInitialized", Type.EmptyTypes);
            var initialized =
                getInitialized?.Invoke(player, null) is bool ready && ready;

            if (!initialized || _offlinePlayerAwakeFailed)
            {
                var playerClass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "", "Player");
                WriteIl2CppBoolField(
                    player.Pointer,
                    playerClass,
                    "<BFDBAKLDHNL>k__BackingField",
                    true);

                // Do not invoke the static lifecycle event wrappers directly.
                // On this IL2CPP depot their backing delegates are not valid
                // after the retired online bootstrap is bypassed; invoking one
                // causes a fatal coreclr AccessViolation. Orientation's native
                // ManualUpdate safely observes the initialized/local-player and
                // SpawnedAndFadedIn state written above.
            }

            _offlineLocalPlayerLifecyclePublished = true;
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] local-player lifecycle published " +
                $"stockInitialized={initialized} awakeFailed={_offlinePlayerAwakeFailed} " +
                $"player=0x{player.Pointer.ToInt64():X}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] local-player lifecycle publish deferred: " +
                e.GetBaseException().Message);
        }
    }

    /// <summary>
    /// Called every frame from OrientationGameplayDriver and LoadingScreen.Update.
    /// Owns cursor lock, WASD, sky, and camera after the loading UI is gone.
    /// Keep this path extremely light — heavy work here hard-crashed the process.
    /// </summary>
    public static void OrientationGameplayTick()
    {
        if (!Plugin.DirectOrientationSceneLoad.Value)
            return;

        if (!_offlineLocomotionReady && !_localPlayerSpawnSucceededLogged)
            return;

        // LoadingScreen.Update, the permanent game-loop hook, the injected
        // driver, and Camera pre-cull can all run during one rendered frame.
        // Let the first caller own gameplay for that frame; previously every
        // caller repeated movement, physics, avatar and door work and produced
        // severe laptop stutter plus unreliable input edges.
        var frame = UnityEngine.Time.frameCount;
        if (frame == _lastGameplayFrame)
            return;
        _lastGameplayFrame = frame;

        // Keep this method bulletproof and tiny — heavy work here crashed the
        // process ~5–9s after spawn on this IL2CPP build.
        try
        {
            MaintainOrientationCursorCapture();
            // Prefer the depot's complete ScreenPlayerController whenever both
            // core scenes supplied it. The transform fallback remains available
            // only for a genuinely incomplete desktop runtime.
            if (!_stockScreenPlayerReady)
                TickOfflineDesktopLocomotion();
            else
                TickMountedAvatarFollow();
            TickOfflineOrientationStockFlow();

            if (_realAvatarMounted && _avatarApplyUntil.HasValue)
            {
                // The official customize-page puppet is already attached.
                // Cancel the delayed stock PlayerAvatar stages so they cannot
                // re-enable the removed fallback/Grok body on a later frame.
                _avatarApplySucceeded = true;
                _avatarApplyStage = 4;
                _avatarApplyUntil = null;
                _avatarApplyNextAt = null;
            }

            // Deferred avatar apply was scheduled but never invoked — that is
            // why customization never showed in Orientation.
            if (_avatarApplyUntil.HasValue &&
                DateTime.UtcNow <= _avatarApplyUntil.Value &&
                (!_avatarApplySucceeded ||
                 (_avatarApplyNextAt.HasValue &&
                  DateTime.UtcNow >= _avatarApplyNextAt.Value)))
            {
                if (!_avatarApplyNextAt.HasValue ||
                    DateTime.UtcNow >= _avatarApplyNextAt.Value)
                {
                    _avatarApplyNextAt = DateTime.UtcNow.AddSeconds(1.25);
                    try
                    {
                        TryApplyCustomAvatarToOrientationPlayer(
                            force: !_avatarApplySucceeded);
                    }
                    catch (Exception e)
                    {
                        if (!_avatarApplyFailLogged)
                        {
                            _avatarApplyFailLogged = true;
                            Plugin.Log.LogWarning(
                                "[AVATAR] tick apply: " +
                                e.GetBaseException().Message);
                        }
                    }
                }
            }
        }
        catch
        {
            // Never throw out of the gameplay tick.
        }
    }

    private static void ForceBrightSkyOnPlayerCameraOnly()
    {
        try
        {
            var sky = new UnityEngine.Color(0.45f, 0.72f, 0.98f);
            try { UnityEngine.RenderSettings.skybox = null; } catch { /* ignore */ }
            UnityEngine.RenderSettings.ambientLight =
                new UnityEngine.Color(0.65f, 0.72f, 0.85f);

            var cam = ResolveFluxPlayerCamera(null);
            if (cam == null)
                cam = UnityEngine.Camera.main;
            if (cam == null || IsJunkOrNonPlayerCamera(cam.gameObject?.name))
                return;
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
        }
        catch { /* ignore */ }
    }

    private static void EnsureLoadingScreenTickHostAlive()
    {
        try
        {
            if (_loadingScreen == null || _loadingScreen.Pointer == IntPtr.Zero)
                return;
            var screen = _loadingScreen.TryCast<LoadingScreen>();
            if (screen == null)
                return;
            // Keep the component ticking; never re-show the canvas.
            if (screen.gameObject != null && !screen.gameObject.activeSelf)
                screen.gameObject.SetActive(true);
            screen.enabled = true;
            if (screen.canvas != null)
                screen.canvas.enabled = false;
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Reject LoadingScreen / PerfCam / pure UI cameras as the "player view".
    /// </summary>
    private static bool IsJunkOrNonPlayerCamera(string camName, string sceneName = null)
    {
        camName ??= string.Empty;
        sceneName ??= string.Empty;
        if (camName.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (camName.IndexOf("PerfCam", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (camName.IndexOf("UICamera", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (camName.Equals("UI", StringComparison.OrdinalIgnoreCase))
            return true;
        if (sceneName.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    private static void RunPostSpawnPresentationRepairs(bool force = false)
    {
        try
        {
            // Bind the stock desktop controller so first-person camera, hands,
            // and WASD locomotion are driven by Rec Room — not a free-cam hack.
            if (TryBindScreenPlayerController(out var bindDetail))
            {
                if (force || !_screenPlayerBoundLogged)
                {
                    _screenPlayerBoundLogged = true;
                    Plugin.Log.LogWarning(
                        $"[PLAYER-SPAWN] ScreenPlayerController bound: {bindDetail}");
                }
            }
            else if (force)
            {
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] ScreenPlayerController bind incomplete: {bindDetail}");
            }

            if (TryActivatePlayerCameras(out var camDetail))
            {
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] cameras after spawn: {camDetail}");
            }
            else
            {
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] player camera repair incomplete: {camDetail}");
                if (TryActivateOrientationCameras(out var roomCam) &&
                    roomCam != null &&
                    roomCam.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Plugin.Log.LogWarning(
                        $"[PLAYER-SPAWN] orientation room cameras: {roomCam}");
                }
            }

            if (!_stockScreenPlayerReady)
            {
                // Only synthesize the emergency camera/controller when the
                // shipped desktop controller is unavailable. Running both was
                // responsible for double movement and missing stock animation.
                FixFirstPersonCameraRig(out var rigDetail);
                if (force)
                    Plugin.Log.LogWarning($"[PLAYER-SPAWN] FP rig: {rigDetail}");

                EnsureOfflineLocomotionComponents(out var locoDetail);
                if (force)
                    Plugin.Log.LogWarning($"[PLAYER-SPAWN] locomotion: {locoDetail}");
            }
            else if (force)
            {
                Plugin.Log.LogWarning(
                    "[PLAYER-SPAWN] stock desktop camera/locomotion retained");
            }

            RepairOrientationEnvironment();
            DisableJunkCameras();
            MaintainOrientationCursorCapture();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] post-spawn repair: {e.GetBaseException().Message}");
        }
    }

    private static void DisableJunkCameras()
    {
        try
        {
            var all = UnityEngine.Camera.allCameras;
            if (all == null)
                return;
            for (var i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null || c.gameObject == null)
                    continue;
                var n = c.gameObject.name ?? string.Empty;
                if (IsJunkOrNonPlayerCamera(n, c.gameObject.scene.name))
                    c.enabled = false;
            }
        }
        catch { /* ignore */ }
    }

    private static Player _cachedLocalPlayer;
    private static IntPtr _cachedLocalPlayerPtr;
    private static DateTime _cachedLocalPlayerAt = DateTime.MinValue;

    private static Player FindLiveLocalPlayer()
    {
        try
        {
            // NEVER expire a good cache. FindObjectsOfTypeAll throws TypeLoadException
            // on this depot after the first call, which previously made walk die
            // after ~5s when the cache window ended.
            if (_cachedLocalPlayer != null &&
                _cachedLocalPlayerPtr != IntPtr.Zero &&
                _cachedLocalPlayer.Pointer == _cachedLocalPlayerPtr)
            {
                try
                {
                    // Touching .gameObject can throw if destroyed; only then clear.
                    var go = _cachedLocalPlayer.gameObject;
                    if (go != null)
                        return _cachedLocalPlayer;
                }
                catch
                {
                    _cachedLocalPlayer = null;
                    _cachedLocalPlayerPtr = IntPtr.Zero;
                }
            }

            var players = UnityEngine.Resources.FindObjectsOfTypeAll<Player>();
            if (players == null)
                return null;
            for (var i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null || p.Pointer == IntPtr.Zero || p.gameObject == null)
                    continue;
                // Accept DontDestroyOnLoad / any valid or even invalid scene as long
                // as the object is live — offline Orientation sometimes marks weird.
                try
                {
                    if (p.gameObject == null)
                        continue;
                }
                catch { continue; }

                _cachedLocalPlayer = p;
                _cachedLocalPlayerPtr = p.Pointer;
                _cachedLocalPlayerAt = DateTime.UtcNow;
                Plugin.Log.LogWarning(
                    $"[GAMEPLAY] cached local player 0x{p.Pointer.ToInt64():X} " +
                    $"name='{p.gameObject.name}'");
                return p;
            }
        }
        catch (Exception e)
        {
            if (!_playerFindFailLogged)
            {
                _playerFindFailLogged = true;
                Plugin.Log.LogWarning(
                    "[GAMEPLAY] FindLiveLocalPlayer failed: " +
                    e.GetBaseException().Message);
            }
        }
        return _cachedLocalPlayer; // last known good even if refresh failed
    }

    // Resolve ScreenPlayerController from LPC.screenController field first
    // (stock path), then FindObjectsOfTypeAll including inactive DDOL objects.
    private static unsafe UnityEngine.Object ResolveScreenPlayerController(
        out string how)
    {
        how = "none";
        try
        {
            var lpc = FindLiveNativeComponent(
                "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
            if (lpc != null && lpc.Pointer != IntPtr.Zero)
            {
                var lpcClass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
                // Prefer the serialized field name from the dump.
                foreach (var fieldName in new[]
                         {
                             "screenController", "ScreenController",
                         })
                {
                    var ptr = ReadIl2CppReferenceField(
                        lpc.Pointer, lpcClass, fieldName);
                    if (ptr != IntPtr.Zero)
                    {
                        how = $"lpc.{fieldName}";
                        return new UnityEngine.Object(ptr);
                    }
                }

                // Also scan components on the LPC GameObject tree.
                try
                {
                    var lpcComp = lpc.TryCast<UnityEngine.Component>();
                    if (lpcComp != null && lpcComp.gameObject != null)
                    {
                        lpcComp.gameObject.SetActive(true);
                        var behaviours =
                            lpcComp.GetComponentsInChildren<UnityEngine.Behaviour>(true);
                        if (behaviours != null)
                        {
                            for (var i = 0; i < behaviours.Length; i++)
                            {
                                var b = behaviours[i];
                                if (b == null)
                                    continue;
                                var n = b.GetType().Name ?? string.Empty;
                                if (n.IndexOf("ScreenPlayerController",
                                        StringComparison.Ordinal) >= 0)
                                {
                                    how = "lpc.childBehaviour";
                                    return b;
                                }
                            }
                        }
                    }
                }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }

        // Broad search including inactive assets.
        foreach (var ns in new[] { "", "RecRoom" })
        {
            var found = FindLiveNativeComponent(
                "Assembly-CSharp.dll", ns, "ScreenPlayerController");
            if (found != null && found.Pointer != IntPtr.Zero)
            {
                how = $"FindLive ns='{ns}'";
                return found;
            }

            found = FindNativeComponent(
                "Assembly-CSharp.dll", "ScreenPlayerController");
            if (found != null && found.Pointer != IntPtr.Zero)
            {
                how = "FindNative";
                return found;
            }
        }

        // Last resort: raw IL2CPP class pointer + FindObjectsOfTypeAll without
        // the managed ScreenPlayerController interop type (often unavailable).
        try
        {
            var klass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "ScreenPlayerController");
            if (klass == IntPtr.Zero)
                klass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "RecRoom", "ScreenPlayerController");
            if (klass != IntPtr.Zero)
            {
                var type = Il2CppType.TypeFromPointer(klass, "ScreenPlayerController");
                var all = UnityEngine.Resources.FindObjectsOfTypeAll(type);
                if (all != null)
                {
                    for (var i = 0; i < all.Length; i++)
                    {
                        var c = all[i];
                        if (c == null || c.Pointer == IntPtr.Zero)
                            continue;
                        var comp = c.TryCast<UnityEngine.Component>();
                        if (comp?.gameObject != null)
                        {
                            comp.gameObject.SetActive(true);
                            if (comp.gameObject.scene.IsValid())
                            {
                                how = "il2cpp-FindObjectsOfTypeAll-live";
                                return c;
                            }
                        }
                    }

                    for (var i = 0; i < all.Length; i++)
                    {
                        if (all[i] != null && all[i].Pointer != IntPtr.Zero)
                        {
                            how = "il2cpp-FindObjectsOfTypeAll-any";
                            return all[i];
                        }
                    }
                }

                how = "class found but no instances";
            }
            else
            {
                how = "IL2CPP class not found";
            }
        }
        catch (Exception e)
        {
            how = "il2cpp search failed: " + e.GetBaseException().Message;
        }

        return null;
    }

    // ScreenPlayerController owns desktop FP camera, WASD, and hand poses.
    // Photon Instantiate alone never binds it — that is why users got a dead
    // body under the map with no walk and a free cursor.
    private static unsafe bool TryScreenPlayerControllerInstantiate(
        UnityEngine.Vector3 pos,
        UnityEngine.Quaternion rot,
        out string detail)
    {
        detail = "none";
        try
        {
            var spc = ResolveScreenPlayerController(out var how);
            if (spc == null || spc.Pointer == IntPtr.Zero)
            {
                detail = "ScreenPlayerController missing (" + how + ")";
                return false;
            }

            var lpc = FindLiveNativeComponent(
                "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
            var spcClass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "ScreenPlayerController");
            if (spcClass == IntPtr.Zero)
                spcClass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "RecRoom", "ScreenPlayerController");
            if (lpc != null && lpc.Pointer != IntPtr.Zero && spcClass != IntPtr.Zero)
            {
                var init = IL2CPP.il2cpp_class_get_method_from_name(
                    spcClass, "NPFGFFCDMGH", 1);
                if (init != IntPtr.Zero)
                {
                    var lpcPtr = lpc.Pointer;
                    var args = stackalloc void*[1];
                    args[0] = &lpcPtr;
                    var ex = IntPtr.Zero;
                    IL2CPP.il2cpp_runtime_invoke(init, spc.Pointer, args, ref ex);
                    if (ex != IntPtr.Zero)
                        Plugin.Log.LogWarning(
                            "[PLAYER-SPAWN] SPC.NPFGFFCDMGH: " +
                            DescribeIl2CppException(ex));
                }
            }

            var method = spcClass != IntPtr.Zero
                ? IL2CPP.il2cpp_class_get_method_from_name(spcClass, "NGOEPFPOEGG", 2)
                : IntPtr.Zero;
            if (method == IntPtr.Zero)
            {
                detail = "NGOEPFPOEGG(2) not found via=" + how;
                return false;
            }

            var posPtr = stackalloc byte[sizeof(float) * 3];
            *(float*)posPtr = pos.x;
            *((float*)posPtr + 1) = pos.y;
            *((float*)posPtr + 2) = pos.z;
            var rotPtr = stackalloc byte[sizeof(float) * 4];
            *(float*)rotPtr = rot.x;
            *((float*)rotPtr + 1) = rot.y;
            *((float*)rotPtr + 2) = rot.z;
            *((float*)rotPtr + 3) = rot.w;
            var args2 = stackalloc void*[2];
            args2[0] = posPtr;
            args2[1] = rotPtr;
            var exception = IntPtr.Zero;
            var result = IL2CPP.il2cpp_runtime_invoke(
                method, spc.Pointer, args2, ref exception);
            if (exception != IntPtr.Zero)
            {
                detail = DescribeIl2CppException(exception) + " via=" + how;
                return false;
            }

            if (result == IntPtr.Zero)
            {
                detail = "NGOEPFPOEGG returned null via=" + how;
                return false;
            }

            detail = $"player=0x{result.ToInt64():X} pos={pos} via={how}";
            return true;
        }
        catch (Exception e)
        {
            detail = $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}";
            return false;
        }
    }

    private static unsafe bool TryBindScreenPlayerController(out string detail)
    {
        detail = "none";
        try
        {
            var player = FindLiveLocalPlayer();
            if (player == null || player.Pointer == IntPtr.Zero)
            {
                detail = "no local Player";
                return false;
            }

            var spc = ResolveScreenPlayerController(out var how);
            if (spc == null || spc.Pointer == IntPtr.Zero)
            {
                detail = "ScreenPlayerController missing (" + how + ")";
                return false;
            }

            var spcClass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "ScreenPlayerController");
            if (spcClass == IntPtr.Zero)
                spcClass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "RecRoom", "ScreenPlayerController");
            if (spcClass == IntPtr.Zero)
            {
                detail = "ScreenPlayerController class missing";
                return false;
            }

            var lpc = FindLiveNativeComponent(
                "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
            if (lpc != null && lpc.Pointer != IntPtr.Zero)
            {
                var init = IL2CPP.il2cpp_class_get_method_from_name(
                    spcClass, "NPFGFFCDMGH", 1);
                if (init != IntPtr.Zero)
                {
                    var lpcPtr = lpc.Pointer;
                    var args = stackalloc void*[1];
                    args[0] = &lpcPtr;
                    var ex = IntPtr.Zero;
                    IL2CPP.il2cpp_runtime_invoke(init, spc.Pointer, args, ref ex);
                }
            }

            // Assign the live Player reference after both shipped core scenes
            // have populated the desktop controller.
            var assign = IL2CPP.il2cpp_class_get_method_from_name(
                spcClass, "NNOPCBMDMGC", 1);
            if (assign != IntPtr.Zero)
            {
                var playerPtr = player.Pointer;
                var args = stackalloc void*[1];
                args[0] = &playerPtr;
                var ex = IntPtr.Zero;
                IL2CPP.il2cpp_runtime_invoke(assign, spc.Pointer, args, ref ex);
                if (ex != IntPtr.Zero)
                {
                    detail = "NNOPCBMDMGC: " + DescribeIl2CppException(ex);
                    _stockScreenPlayerReady = false;
                    _suppressScreenPlayerTicks = true;
                    return false;
                }
            }

            var missing = new List<string>();
            foreach (var fieldName in new[]
                     {
                         "modeConfigs",
                         "defaultCameraSettings",
                         "defaultGameplayCursorSettings",
                         "locomotionContent",
                         "locomotionAnimationContent",
                     })
            {
                if (ReadIl2CppReferenceField(
                        spc.Pointer, spcClass, fieldName) == IntPtr.Zero)
                    missing.Add(fieldName);
            }

            if (missing.Count > 0)
            {
                _stockScreenPlayerReady = false;
                _suppressScreenPlayerTicks = true;
                detail = "incomplete serialized controller fields=" +
                         string.Join(",", missing) + " via=" + how;
                return false;
            }

            if (!_stockPlayerInitializedStateEntered &&
                lpc != null && lpc.Pointer != IntPtr.Zero)
            {
                var lpcClass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
                var enter = lpcClass != IntPtr.Zero
                    ? IL2CPP.il2cpp_class_get_method_from_name(
                        lpcClass, "EnterPlayerInitializedState", 0)
                    : IntPtr.Zero;
                if (enter != IntPtr.Zero)
                {
                    var enterException = IntPtr.Zero;
                    IL2CPP.il2cpp_runtime_invoke(
                        enter, lpc.Pointer, null, ref enterException);
                    if (enterException != IntPtr.Zero)
                    {
                        _stockScreenPlayerReady = false;
                        _suppressScreenPlayerTicks = true;
                        detail = "EnterPlayerInitializedState: " +
                                 DescribeIl2CppException(enterException);
                        return false;
                    }
                    _stockPlayerInitializedStateEntered = true;
                }
            }

            _stockScreenPlayerReady = true;
            _suppressScreenPlayerTicks = false;
            detail =
                $"spc=0x{spc.Pointer.ToInt64():X} player=0x{player.Pointer.ToInt64():X} " +
                $"via={how} stockTicks=enabled";
            return true;
        }
        catch (Exception e)
        {
            _stockScreenPlayerReady = false;
            _suppressScreenPlayerTicks = true;
            detail = e.GetBaseException().Message;
            return false;
        }
    }

    // Stock desktop head height is 1.6m (ScreenPlayerController). Camera under
    // the map was caused by parenting to a zeroed Head bone after failed Awake.
    private static void FixFirstPersonCameraRig(out string detail)
    {
        detail = "none";
        try
        {
            var player = FindLiveLocalPlayer();
            if (player == null)
            {
                detail = "no player";
                return;
            }

            const float headHeight = 1.6f;
            UnityEngine.Camera cam = null;

            // Prefer our fallback camera or any non-junk cam under the player.
            var cams = player.GetComponentsInChildren<UnityEngine.Camera>(true);
            if (cams != null)
            {
                for (var i = 0; i < cams.Length; i++)
                {
                    var c = cams[i];
                    if (c == null || c.gameObject == null)
                        continue;
                    if (IsJunkOrNonPlayerCamera(c.gameObject.name))
                        continue;
                    cam = c;
                    if ((c.gameObject.name ?? string.Empty).IndexOf(
                            "FluxRec", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
            }

            if (cam == null)
            {
                var camGo = new UnityEngine.GameObject("FluxRec_PlayerCamera");
                camGo.transform.SetParent(player.transform, false);
                cam = camGo.AddComponent<UnityEngine.Camera>();
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 800f;
                cam.fieldOfView = 75f;
                cam.depth = 100;
            }

            // Force world-space eye height first, then re-parent with
            // worldPositionStays so a broken Head/scale cannot pin the cam
            // under the plaza (prior bug: localY=1.6 but worldY stayed at feet).
            var p = player.transform.position;
            var spawn = ResolveOrientationSpawnPose(out var spawnRot);
            // If player is far below the spawn pad, snap to spawn.
            if (p.y < spawn.y - 2.0f || p.y < -40f)
            {
                player.transform.SetPositionAndRotation(spawn, spawnRot);
                p = spawn;
            }

            // Keep the camera UNPARENTED. Parenting under [Player] made world Y
            // stick to the feet (player root scale / tracking space collapses
            // local offsets). Drive absolute world eye height every frame in
            // TickOfflineDesktopLocomotion instead.
            cam.transform.SetParent(null, true);
            var eyeY = p.y + headHeight;
            cam.transform.SetPositionAndRotation(
                new UnityEngine.Vector3(p.x, eyeY, p.z),
                player.transform.rotation);

            cam.gameObject.SetActive(true);
            cam.enabled = true;
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = new UnityEngine.Color(0.52f, 0.74f, 0.95f);
            try { cam.gameObject.tag = "MainCamera"; } catch { /* ignore */ }
            if (cam.GetComponent<UnityEngine.AudioListener>() == null)
            {
                try { cam.gameObject.AddComponent<UnityEngine.AudioListener>(); }
                catch { /* ignore */ }
            }

            DisableJunkCameras();
            var cw = cam.transform.position;
            detail =
                $"cam='{cam.name}' eye={headHeight} " +
                $"world=({cw.x:0.00},{cw.y:0.00},{cw.z:0.00}) " +
                $"player=({p.x:0.00},{p.y:0.00},{p.z:0.00})";
        }
        catch (Exception e)
        {
            detail = e.GetBaseException().Message;
        }
    }

    private static void EnsureOfflineLocomotionComponents(out string detail)
    {
        detail = "none";
        try
        {
            var player = FindLiveLocalPlayer();
            if (player == null)
            {
                detail = "no player";
                return;
            }

            var go = player.gameObject;
            // Enable PlayerMovement behaviours if present.
            var behaviours = go.GetComponentsInChildren<UnityEngine.Behaviour>(true);
            var movementEnabled = 0;
            if (behaviours != null)
            {
                for (var i = 0; i < behaviours.Length; i++)
                {
                    var b = behaviours[i];
                    if (b == null)
                        continue;
                    var tn = b.GetType().Name ?? string.Empty;
                    if (tn.IndexOf("PlayerMovement", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("Locomotion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("CharacterController", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("ScreenPlayer", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        b.enabled = true;
                        b.gameObject.SetActive(true);
                        movementEnabled++;
                    }
                }
            }

            // Prefer transform motion only. Adding CharacterController offline
            // has hard-crashed the process when physics layers are incomplete.
            _offlineLocomotionReady = true;
            detail = $"movementBehaviours={movementEnabled} transformWalk=True";
        }
        catch (Exception e)
        {
            detail = e.GetBaseException().Message;
            _offlineLocomotionReady = true; // still try walk
        }
    }

        // Win32 input — UnityEngine.Input is often dead offline (custom/Rewired path).
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private static bool WinKey(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    // Free-camera offline walk. Moving the networked [Player] root is fought by
    // Photon NetworkTransform (logs showed MOVING then snap-back). Drive the
    // camera itself; optionally drag the player root after killing net sync.
    private static void TickOfflineDesktopLocomotion()
    {
        if (!_offlineLocomotionReady)
            return;

        try
        {
            var player = _cachedLocalPlayer;
            if (player == null || player.Pointer == IntPtr.Zero ||
                player.Pointer != _cachedLocalPlayerPtr)
            {
                player = FindLiveLocalPlayer();
                if (player == null)
                    return;
            }

            // Init free-cam from player spawn once. Do body/net work later —
            // enabling avatar meshes + net disable in the same frame as free-cam
            // hard-crashed (~12s) on this depot.
            if (!_freeCamReady)
            {
                var p = player.transform.position;
                if (!_spawnFloorY.HasValue)
                    _spawnFloorY = p.y;
                _freeCamX = p.x;
                _freeCamY = (_spawnFloorY ?? p.y) + 1.6f;
                _freeCamZ = p.z;
                _freeCamYaw = player.transform.eulerAngles.y;
                _freeCamReady = true;
                // Faster body/avatar enable — was 2s and felt like "can't move".
                _bodyForceAt = DateTime.UtcNow.AddSeconds(0.4);
                Plugin.Log.LogWarning(
                    $"[GAMEPLAY] free-cam ready at ({_freeCamX:0.00},{_freeCamY:0.00},{_freeCamZ:0.00})");
            }

            if (!_networkSyncDisabled &&
                _bodyForceAt.HasValue &&
                DateTime.UtcNow >= _bodyForceAt.Value)
            {
                _networkSyncDisabled = true;
                try { DisablePlayerNetworkTransformSync(player); } catch { /* ignore */ }
                try
                {
                    if (!_avatarApplySucceeded)
                        TryApplyCustomAvatarToOrientationPlayer(force: true);
                }
                catch { /* ignore */ }
            }

            // Heartbeat
            if (!_gameplayHeartbeatAt.HasValue ||
                (DateTime.UtcNow - _gameplayHeartbeatAt.Value).TotalSeconds >= 5.0)
            {
                _gameplayHeartbeatAt = DateTime.UtcNow;
                Plugin.Log.LogWarning(
                    $"[GAMEPLAY] tick alive cam=({_freeCamX:0.00},{_freeCamY:0.00},{_freeCamZ:0.00}) " +
                    $"yaw={_freeCamYaw:0.0} lock={UnityEngine.Cursor.lockState} " +
                    $"vis={UnityEngine.Cursor.visible}");
            }

            // --- Input (Win32) ---
            float h = 0f, v = 0f;
            if (WinKey(0x41) || WinKey(0x25)) h -= 1f;
            if (WinKey(0x44) || WinKey(0x27)) h += 1f;
            if (WinKey(0x53) || WinKey(0x28)) v -= 1f;
            if (WinKey(0x57) || WinKey(0x26)) v += 1f;
            // Also Q/E and space for extra feel
            if (WinKey(0x51)) _freeCamY -= 3f * Math.Min(UnityEngine.Time.deltaTime, 0.05f);
            if (WinKey(0x45)) _freeCamY += 3f * Math.Min(UnityEngine.Time.deltaTime, 0.05f);
            h = Math.Clamp(h, -1f, 1f);
            v = Math.Clamp(v, -1f, 1f);

            // Unity's relative mouse axes keep producing deltas while the
            // cursor is locked and work with laptop touchpads. The old code
            // only sampled the absolute Windows cursor, which Unity constantly
            // recentres while locked, leaving little or no camera movement.
            float mx = 0f, my = 0f;
            var relativeLookRead = false;
            try
            {
                mx = UnityEngine.Input.GetAxisRaw("Mouse X");
                my = UnityEngine.Input.GetAxisRaw("Mouse Y");
                relativeLookRead =
                    Math.Abs(mx) > 0.0001f || Math.Abs(my) > 0.0001f;
            }
            catch
            {
                mx = 0f;
                my = 0f;
            }

            // Absolute-cursor fallback for machines where the legacy Unity
            // input axes really are unavailable. Never combine both paths or
            // the same physical movement would be counted twice.
            if (!relativeLookRead && GetCursorPos(out var cursor))
            {
                if (_hasLastCursor &&
                    UnityEngine.Cursor.lockState != UnityEngine.CursorLockMode.Locked)
                {
                    mx = (cursor.X - _lastCursorX) * 0.10f;
                    my = (cursor.Y - _lastCursorY) * -0.10f;
                }
                _lastCursorX = cursor.X;
                _lastCursorY = cursor.Y;
                _hasLastCursor = true;
            }
            else if (relativeLookRead)
            {
                // Avoid an absolute-cursor jump if the fallback is used after
                // the window temporarily loses focus.
                _hasLastCursor = false;
            }

            if (Math.Abs(mx) > 0.001f || Math.Abs(my) > 0.001f)
            {
                var lookSensitivity = Math.Clamp(
                    Plugin.OfflineMouseLookSensitivity.Value, 0.5f, 20f);
                _freeCamYaw += mx * lookSensitivity;
                _offlineCameraPitch = Math.Clamp(
                    _offlineCameraPitch - my * lookSensitivity, -82f, 82f);
            }

            var speed = WinKey(0x10) ? 10f : 6f;
            var dt = UnityEngine.Time.deltaTime;
            if (dt <= 0f || dt > 0.05f) dt = 0.016f;

            var yawRad = _freeCamYaw * (float)(Math.PI / 180.0);
            var sin = (float)Math.Sin(yawRad);
            var cos = (float)Math.Cos(yawRad);
            // Proposed walk target (before collision).
            var proposedX = _freeCamX + (sin * v + cos * h) * speed * dt;
            var proposedZ = _freeCamZ + (cos * v + -sin * h) * speed * dt;
            var proposedY = _freeCamY;
            // Keep near floor eye height unless user Q/E
            var floorEye = (_spawnFloorY ?? (_freeCamY - 1.6f)) + 1.6f;
            if (!WinKey(0x51) && !WinKey(0x45))
                proposedY = floorEye;

            // Soft ground/wall collision so free-cam is not pure ghost mode.
            ApplyOfflineWalkCollision(
                ref proposedX, ref proposedY, ref proposedZ,
                _freeCamX, _freeCamY, _freeCamZ);
            _freeCamX = proposedX;
            _freeCamY = proposedY;
            _freeCamZ = proposedZ;

            var moveAmt = Math.Clamp(Math.Abs(h) + Math.Abs(v), 0f, 1f);
            if (moveAmt > 0.01f)
                _walkBobPhase += speed * 1.4f * dt;

            // Apply free-cam to camera (this is what the player SEES).
            var cam = _cachedFluxCamera;
            if (cam == null || cam.Pointer == IntPtr.Zero)
            {
                try
                {
                    var go = UnityEngine.GameObject.Find("FluxRec_PlayerCamera");
                    if (go != null)
                        cam = go.GetComponent<UnityEngine.Camera>();
                    if (cam == null)
                    {
                        // Create if missing
                        var camGo = new UnityEngine.GameObject("FluxRec_PlayerCamera");
                        cam = camGo.AddComponent<UnityEngine.Camera>();
                        cam.nearClipPlane = 0.05f;
                        cam.farClipPlane = 800f;
                        cam.fieldOfView = 75f;
                        cam.depth = 100;
                        try { camGo.AddComponent<UnityEngine.AudioListener>(); } catch { }
                    }
                    _cachedFluxCamera = cam;
                    if (cam.transform.parent != null)
                        cam.transform.SetParent(null, true);
                }
                catch { cam = null; }
            }

            if (cam != null)
            {
                var lookRotation = UnityEngine.Quaternion.Euler(
                    _offlineCameraPitch, _freeCamYaw, 0f);
                var baseEye = new UnityEngine.Vector3(
                    _freeCamX, _freeCamY, _freeCamZ);
                var eyeOffset = Math.Clamp(
                    Plugin.OfflineCameraEyeForwardOffset.Value, 0.08f, 0.35f);
                cam.transform.SetPositionAndRotation(
                    baseEye + (lookRotation * UnityEngine.Vector3.forward * eyeOffset),
                    lookRotation);
                cam.enabled = true;
                cam.depth = 200;
                // Layer 30 is reserved below for the local avatar's head and
                // the retired player-prefab visuals. Re-applying ~0 every
                // frame made both visible again after the one-time avatar
                // setup, which is why the old rectangular arms/head returned.
                try { cam.cullingMask = ~(1 << 30); } catch { /* ignore */ }
                // Prefer skybox when the scene has one; solid color is fallback.
                try
                {
                    if (UnityEngine.RenderSettings.skybox != null)
                        cam.clearFlags = UnityEngine.CameraClearFlags.Skybox;
                    else
                    {
                        cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                        cam.backgroundColor =
                            new UnityEngine.Color(0.45f, 0.72f, 0.98f);
                    }
                }
                catch
                {
                    cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                    cam.backgroundColor =
                        new UnityEngine.Color(0.45f, 0.72f, 0.98f);
                }
                try { cam.gameObject.tag = "MainCamera"; } catch { /* ignore */ }

                // Kill competing cameras so the free-cam is what the player SEES.
                // Without this, WASD moves our cam while another cam still draws.
                if (!_competingCamsDisabled ||
                    !_lastCamSoleLogAt.HasValue ||
                    (DateTime.UtcNow - _lastCamSoleLogAt.Value).TotalSeconds > 3.0)
                {
                    try
                    {
                        SoleFluxCamera(cam);
                        _competingCamsDisabled = true;
                        _lastCamSoleLogAt = DateTime.UtcNow;
                    }
                    catch { /* ignore */ }
                }

                // EVERY frame: body under camera + hands in first-person view
                // with walk bob. Without this the user only sees a floating cam.
                try
                {
                    PresentFirstPersonPlayerAvatar(player, cam, moveAmt, dt);
                }
                catch { /* never kill the walk tick */ }

                // Final head/eye alignment and the animation layer run from the
                // verified Camera.FireOnPreCull hook after stock LateUpdate.
                // Repeating them here made the genuine puppet fight itself
                // twice per frame and contributed to visible jitter.
            }

            // Keep player root under the camera every frame (not only while
            // keys held) so avatar/shadow stay with the view when idle too.
            if (_networkSyncDisabled)
            {
                try
                {
                    var bodyY = (_spawnFloorY ?? (_freeCamY - 1.6f));
                    player.transform.position = new UnityEngine.Vector3(
                        _freeCamX, bodyY, _freeCamZ);
                    player.transform.rotation =
                        UnityEngine.Quaternion.Euler(0f, _freeCamYaw, 0f);
                }
                catch { /* stock may fight — free-cam still owns view */ }
            }

            if ((Math.Abs(h) > 0.01f || Math.Abs(v) > 0.01f) &&
                (!_lastMoveLogAt.HasValue ||
                 (DateTime.UtcNow - _lastMoveLogAt.Value).TotalSeconds > 1.0))
            {
                _lastMoveLogAt = DateTime.UtcNow;
                Plugin.Log.LogWarning(
                    $"[GAMEPLAY] MOVING h={h:0.#} v={v:0.#} " +
                    $"cam=({_freeCamX:0.0},{_freeCamY:0.0},{_freeCamZ:0.0})");
            }
        }
        catch
        {
            // never crash
        }
    }

    /// <summary>
    /// Soft wall + ground collision for free-cam walk. Uses Physics queries so
    /// we do not depend on a CharacterController (which hard-crashed offline).
    /// </summary>
    private static void ApplyOfflineWalkCollision(
        ref float x, ref float y, ref float z,
        float prevX, float prevY, float prevZ)
    {
        try
        {
            const float eye = 1.6f;
            var bodyY = y - eye;
            var chest = new UnityEngine.Vector3(prevX, bodyY + 0.95f, prevZ);
            var target = new UnityEngine.Vector3(x, bodyY + 0.95f, z);
            var delta = target - chest;
            var dist = delta.magnitude;
            if (dist > 0.0005f)
            {
                // Sphere cast for walls / props.
                if (UnityEngine.Physics.SphereCast(
                        chest,
                        0.28f,
                        delta.normalized,
                        out var hit,
                        dist + 0.05f,
                        ~0,
                        UnityEngine.QueryTriggerInteraction.Ignore))
                {
                    var safe = Math.Max(0f, hit.distance - 0.1f);
                    var p = chest + delta.normalized * safe;
                    x = p.x;
                    z = p.z;
                    if (!_collisionHitLogged)
                    {
                        _collisionHitLogged = true;
                        Plugin.Log.LogWarning(
                            $"[GAMEPLAY] collision active hit='{hit.collider?.name}'");
                    }
                }
            }

            // Ground snap. Moving players probe every rendered frame; while
            // idle, 5 Hz is enough to retain the floor and avoids burning a
            // physics query continuously.
            var now = DateTime.UtcNow;
            if (dist <= 0.0005f && _offlineGroundProbeNextAt.HasValue &&
                now < _offlineGroundProbeNextAt.Value)
                return;
            _offlineGroundProbeNextAt = now.AddMilliseconds(
                dist > 0.0005f ? 16 : 200);

            var rayOrigin = new UnityEngine.Vector3(x, bodyY + 2.5f, z);
            _offlineGroundRayHitBuffer ??=
                new Il2CppStructArray<UnityEngine.RaycastHit>(16);
            var hitCount = UnityEngine.Physics.RaycastNonAlloc(
                rayOrigin,
                UnityEngine.Vector3.down,
                _offlineGroundRayHitBuffer,
                6.0f,
                ~0,
                UnityEngine.QueryTriggerInteraction.Ignore);
            if (hitCount > 0)
            {
                // Nearest valid floor hit.
                UnityEngine.RaycastHit? best = null;
                var bestDist = float.MaxValue;
                for (var i = 0; i < hitCount; i++)
                {
                    var h = _offlineGroundRayHitBuffer[i];
                    if (h.collider == null)
                        continue;
                    var n = h.collider.name ?? string.Empty;
                    var goName = h.collider.gameObject?.name ?? string.Empty;
                    if (n.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("[Player]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("FluxRec", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    // Prefer roughly upward surfaces.
                    if (h.normal.y < 0.35f)
                        continue;
                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        best = h;
                    }
                }

                if (best.HasValue)
                {
                    bodyY = best.Value.point.y;
                    _spawnFloorY = bodyY;
                    y = bodyY + eye;
                    if (!_groundHitLogged)
                    {
                        _groundHitLogged = true;
                        Plugin.Log.LogWarning(
                            $"[GAMEPLAY] ground snap y={bodyY:0.00} " +
                            $"collider='{best.Value.collider?.name}'");
                    }
                }
            }
        }
        catch
        {
            // No physics yet — keep free movement.
        }
    }

    /// <summary>
    /// First-person presentation of the REAL customization avatar (title
    /// AnimatedPlayerPuppet with wardrobe addressable meshes). No fake capsules.
    /// </summary>
    private static void PresentFirstPersonPlayerAvatar(
        Player player,
        UnityEngine.Camera cam,
        float moveAmount,
        float dt)
    {
        if (player == null || cam == null)
            return;

        // Mount official title puppet once (no spam).
        if (!_realAvatarMounted && !_mountFailLogged &&
            (!_lastRealMountAttemptAt.HasValue ||
             (DateTime.UtcNow - _lastRealMountAttemptAt.Value).TotalSeconds > 2.0))
        {
            _lastRealMountAttemptAt = DateTime.UtcNow;
            try { MountRealCustomizationAvatarOnPlayer(player); }
            catch { /* ignore */ }
        }

        var puppet = ResolveOfficialPuppet();
        if (puppet == null)
            return;

        // Let Rec Room's PlayerPuppet follow the real Player. Parenting the
        // title preview directly below Player bypassed its tracking provider and
        // produced camera-attached, unanimated arms.
        BindOfficialPuppetToPlayer(player, puppet, refresh: false);
        _offlineAvatarMoveAmount = Math.Clamp(moveAmount, 0f, 1f);
        _offlineAvatarAnimationDt = Math.Clamp(dt, 0.001f, 0.05f);

        // Heavy renderer/animator discovery is needed once after the official
        // puppet mounts. Repeating it every 15 seconds caused visible laptop
        // hitches (hierarchy arrays, all-light scan, material updates) despite
        // the avatar already being stable.
        var doHeavy = !_fpPresentationReady;
        if (!doHeavy)
            return;
        _lastFpHeavyAt = DateTime.UtcNow;

        var handMesh = 0;
        var bodyMesh = 0;
        try
        {
            var display = puppet.playerAvatarDisplay;
            var root = display != null ? display.gameObject : puppet.gameObject;
            root.SetActive(true);
            var rs = root.GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (rs != null)
            {
                for (var i = 0; i < rs.Length; i++)
                {
                    var r = rs[i];
                    if (r == null || r.gameObject == null)
                        continue;
                    var n = r.gameObject.name ?? string.Empty;
                    var isHand =
                        n.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Arm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Finger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Wrist", StringComparison.OrdinalIgnoreCase) >= 0;
                    var isHead =
                        n.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Mouth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Ear", StringComparison.OrdinalIgnoreCase) >= 0;
                    // Hide only name tag / UI under the puppet, never body/head/hands.
                    if (n.IndexOf("NameTag", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("RawImage", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        r.enabled = false;
                        continue;
                    }

                    r.gameObject.SetActive(true);
                    r.enabled = true;
                    if (isHead)
                        r.gameObject.layer = 30;
                    try
                    {
                        r.shadowCastingMode =
                            UnityEngine.Rendering.ShadowCastingMode.On;
                        r.receiveShadows = true;
                    }
                    catch { /* ignore */ }
                    if (isHand) handMesh++;
                    else bodyMesh++;
                }
            }

            var ans = root.GetComponentsInChildren<UnityEngine.Animator>(true);
            if (ans != null)
            {
                for (var i = 0; i < ans.Length; i++)
                {
                    if (ans[i] == null)
                        continue;
                    ans[i].enabled = true;
                    ans[i].cullingMode =
                        UnityEngine.AnimatorCullingMode.AlwaysAnimate;
                    if (ans[i].speed < 0.05f)
                        ans[i].speed = 1f;
                }
            }
        }
        catch { /* ignore */ }

        ConfigureRealAvatarFirstPerson(player, puppet);

        if (!_fpPresentationReady)
        {
            _fpPresentationReady = true;
            Plugin.Log.LogWarning(
                $"[GAMEPLAY] REAL FP avatar ready puppet='{puppet.gameObject.name}' " +
                $"handMeshes={handMesh} bodyMeshes={bodyMesh} " +
                $"selections={HEBLKMJBIBO.IJEMMGDMKPE?.FMGNNCFFGLB?.Count ?? 0}");
        }
        else if (!_fpPresentationRefreshLogged ||
                 (DateTime.UtcNow - (_lastFpPresentLogAt ?? DateTime.MinValue))
                 .TotalSeconds > 8)
        {
            _lastFpPresentLogAt = DateTime.UtcNow;
            _fpPresentationRefreshLogged = true;
            Plugin.Log.LogWarning(
                $"[GAMEPLAY] REAL FP avatar refresh handMeshes={handMesh} " +
                $"bodyMeshes={bodyMesh}");
        }
    }

    private static void ConfigureRealAvatarFirstPerson(
        Player player,
        RecRoom.Players.Puppet.AnimatedPlayerPuppet puppet)
    {
        if (player == null || puppet == null)
            return;

        try
        {
            // Hide the real avatar's head from only the local first-person
            // camera. The body, shirt, hands and other customized items remain
            // attached and visible; this prevents the face from filling the
            // screen while looking around.
            var realRenderers = puppet.gameObject
                .GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (realRenderers != null)
            {
                for (var i = 0; i < realRenderers.Length; i++)
                {
                    var renderer = realRenderers[i];
                    if (renderer == null || renderer.gameObject == null)
                        continue;
                    var name = renderer.gameObject.name ?? string.Empty;
                    if (name.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Mouth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Ear", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        renderer.gameObject.layer = 30;
                    }
                }
            }

            // Renderer names are not stable across outfit combinations. Hide
            // the entire stock Head transform subtree as well, so a combined
            // face/hair mesh cannot end up directly in front of the camera.
            try
            {
                var head = puppet.IIGNNGEMFHP;
                var headRenderers = head != null
                    ? head.GetComponentsInChildren<UnityEngine.Renderer>(true)
                    : null;
                if (headRenderers != null)
                {
                    for (var i = 0; i < headRenderers.Length; i++)
                    {
                        var renderer = headRenderers[i];
                        if (renderer?.gameObject != null)
                            renderer.gameObject.layer = 30;
                    }
                }
            }
            catch { /* name-based head filtering above remains active */ }

            var camera = ResolveFluxPlayerCamera(player) ?? UnityEngine.Camera.main;
            if (camera != null)
                camera.cullingMask &= ~(1 << 30);

            // Remove the old fallback/Grok player body from rendering. Keep
            // only the official customized puppet that is now parented below.
            var disabled = 0;
            var oldRenderers = player.GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (oldRenderers != null)
            {
                for (var i = 0; i < oldRenderers.Length; i++)
                {
                    var renderer = oldRenderers[i];
                    if (renderer == null || renderer.transform == null)
                        continue;
                    var isRealPuppet =
                        renderer.transform.Pointer == puppet.transform.Pointer ||
                        renderer.transform.IsChildOf(puppet.transform);
                    if (isRealPuppet)
                        continue;
                    var wasEnabled = renderer.enabled;
                    renderer.enabled = false;
                    // Keep the old player-prefab geometry out of both the FP
                    // camera and the directional-light shadow pass even if a
                    // stock lifecycle callback flips Renderer.enabled later.
                    renderer.gameObject.layer = 30;
                    try
                    {
                        renderer.shadowCastingMode =
                            UnityEngine.Rendering.ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                    }
                    catch { /* renderer still remains disabled + culled */ }
                    if (wasEnabled)
                        disabled++;
                }
            }

            if (!_legacyPlayerVisualsDisabledLogged)
            {
                _legacyPlayerVisualsDisabledLogged = true;
                Plugin.Log.LogWarning(
                    $"[AVATAR] removed legacy fallback player visuals disabled={disabled}; " +
                    "official customize-page puppet now owns the local body");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[AVATAR] first-person real-avatar setup: " +
                e.GetBaseException().Message);
        }
    }

    /// <summary>
    /// Final pre-cull correction for the real customization avatar. The stock
    /// puppet Animator runs in LateUpdate, so doing this from Camera pre-cull
    /// makes the visible head follow the desktop camera without replacing or
    /// disabling the avatar's real animation controllers. The camera is then
    /// anchored just in front of the actual head pivot instead of inside it.
    /// </summary>
    private static void AlignRealAvatarHeadAndEyeCamera(UnityEngine.Camera camera)
    {
        if (camera == null || !_freeCamReady || !_realAvatarMounted ||
            _capturedCustomizationPuppet == null ||
            _capturedCustomizationPuppetPtr == IntPtr.Zero ||
            _capturedCustomizationPuppet.Pointer != _capturedCustomizationPuppetPtr)
            return;

        var head = _capturedCustomizationPuppet.IIGNNGEMFHP;
        if (head == null || head.Pointer == IntPtr.Zero)
            return;

        var lookRotation = UnityEngine.Quaternion.Euler(
            _offlineCameraPitch, _freeCamYaw, 0f);
        head.rotation = lookRotation;

        var baseEye = new UnityEngine.Vector3(
            _freeCamX, _freeCamY, _freeCamZ);
        var eyeOffset = Math.Clamp(
            Plugin.OfflineCameraEyeForwardOffset.Value, 0.08f, 0.35f);
        // The puppet's fidget animator moves its head pivot in LateUpdate. A
        // camera anchored to that animated point slowly drifted back into the
        // face and visibly bobbed on a laptop. Keep the eye at the locomotion
        // controller's stable head-height position; only the avatar head
        // follows the view rotation.
        camera.transform.SetPositionAndRotation(
            baseEye + (lookRotation * UnityEngine.Vector3.forward * eyeOffset),
            lookRotation);

        ApplyOfflineAvatarLocomotionAnimation(
            _capturedCustomizationPuppet,
            _offlineAvatarMoveAmount,
            _offlineAvatarAnimationDt);
        ApplyOfflineOrientationIntroHandVignette(camera, lookRotation);

        if (!_realAvatarCameraRigLogged)
        {
            _realAvatarCameraRigLogged = true;
            Plugin.Log.LogWarning(
                $"[CAMERA] relative laptop look active sensitivity=" +
                $"{Plugin.OfflineMouseLookSensitivity.Value:0.0} " +
                $"eyeOffset={eyeOffset:0.00} headFollow=True stableEye=True");
        }
    }

    private static void DisablePlayerNetworkTransformSync(Player player)
    {
        try
        {
            var behaviours = player.GetComponentsInChildren<UnityEngine.Behaviour>(true);
            if (behaviours == null)
                return;
            var killed = 0;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null) continue;
                var n = b.GetType().Name ?? "";
                // Keep this narrow — bulk-disabling behaviours crashed before.
                if (string.Equals(n, "NetworkTransform", StringComparison.Ordinal) ||
                    string.Equals(n, "PhotonTransformView", StringComparison.Ordinal) ||
                    string.Equals(n, "PhotonTransformViewClassic", StringComparison.Ordinal))
                {
                    b.enabled = false;
                    killed++;
                }
            }

            Plugin.Log.LogWarning(
                $"[GAMEPLAY] disabled {killed} network transform behaviours");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[GAMEPLAY] DisablePlayerNetworkTransformSync: " +
                e.GetBaseException().Message);
        }
    }

    private static UnityEngine.Camera ResolveFluxPlayerCamera(Player player)
    {
        try
        {
            var main = UnityEngine.Camera.main;
            if (main != null &&
                !IsJunkOrNonPlayerCamera(main.gameObject?.name) &&
                (main.gameObject.name ?? string.Empty).IndexOf(
                    "FluxRec", StringComparison.OrdinalIgnoreCase) >= 0)
                return main;

            var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>();
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null || c.gameObject == null)
                        continue;
                    var n = c.gameObject.name ?? string.Empty;
                    if (n.IndexOf("FluxRec_PlayerCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                }
            }

            if (player != null)
            {
                var cams = player.GetComponentsInChildren<UnityEngine.Camera>(true);
                if (cams != null)
                {
                    for (var i = 0; i < cams.Length; i++)
                    {
                        if (cams[i] == null || IsJunkOrNonPlayerCamera(cams[i].gameObject?.name))
                            continue;
                        return cams[i];
                    }
                }
            }
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// Staged, soft avatar handoff. One stage per tick so a single stock method
    /// cannot hard-kill the process mid-spawn.
    /// Stages: 0=pin+store field, 1=settings, 2=outfit selections, 3=meshes.
    /// Skips PlayerAvatar.MEFHNFGOCML / Rebuild (known offline hard-crash).
    /// </summary>
    private static unsafe void TryApplyCustomAvatarToOrientationPlayer(bool force)
    {
        if (_avatarApplySucceeded && !force)
            return;

        try
        {
            RestorePinnedCustomizationAvatar();
            EnsureLocalAvatarModel();
            var model = HEBLKMJBIBO.IJEMMGDMKPE;
            if (model == null || model.Pointer == IntPtr.Zero)
            {
                if (force)
                    Plugin.Log.LogWarning("[AVATAR] no local HEBLKMJBIBO model to apply");
                return;
            }

            var selections = model.FMGNNCFFGLB?.Count ?? 0;
            var player = FindLiveLocalPlayer();
            if (player == null)
            {
                if (force)
                    Plugin.Log.LogWarning("[AVATAR] no local player yet for avatar apply");
                return;
            }

            var playerClass = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "Player");
            var avatarPtr = ReadIl2CppReferenceField(
                player.Pointer, playerClass, "playerAvatar");
            if (avatarPtr == IntPtr.Zero)
            {
                if (force)
                    Plugin.Log.LogWarning("[AVATAR] Player.playerAvatar is null");
                return;
            }

            var avatarClass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "", "PlayerAvatar");
            var displayPtr = ReadIl2CppReferenceField(
                avatarPtr, avatarClass, "playerAvatarDisplay");
            var modelPtr = model.Pointer;
            var stage = _avatarApplyStage;
            Plugin.Log.LogWarning(
                $"[AVATAR] stage={stage} selections={selections} " +
                $"pinned={_pinnedCustomizationAvatarPtr != IntPtr.Zero} " +
                $"display=0x{displayPtr.ToInt64():X}");

            // Stage 0: bind model pointer onto PlayerAvatar only.
            if (stage <= 0)
            {
                try
                {
                    WriteIl2CppReferenceField(
                        avatarPtr, avatarClass, "EAIBBFBGLCG", modelPtr);
                    Plugin.Log.LogWarning("[AVATAR] stage0 stored model on PlayerAvatar");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning(
                        "[AVATAR] stage0 field write: " + e.GetBaseException().Message);
                }

                _avatarApplyStage = 1;
                return;
            }

            var displayClass = IntPtr.Zero;
            if (displayPtr != IntPtr.Zero)
            {
                displayClass = IL2CPP.GetIl2CppClass(
                    "Assembly-CSharp.dll", "RecRoom.Avatars", "PlayerAvatarDisplay");
                if (displayClass == IntPtr.Zero)
                    displayClass = IL2CPP.GetIl2CppClass(
                        "Assembly-CSharp.dll", "", "PlayerAvatarDisplay");
            }

            // Stage 1: enable display GameObject only (no stock visual rebuild —
            // SetCustomizationSettings hard-crashed the process offline).
            if (stage == 1)
            {
                try
                {
                    if (displayPtr != IntPtr.Zero)
                    {
                        var go = new UnityEngine.Object(displayPtr)
                            .TryCast<UnityEngine.Component>()?.gameObject;
                        if (go != null)
                        {
                            go.SetActive(true);
                            Plugin.Log.LogWarning(
                                $"[AVATAR] stage1 display active='{go.name}'");
                        }
                    }
                    else
                    {
                        Plugin.Log.LogWarning("[AVATAR] stage1 no display");
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning(
                        "[AVATAR] stage1: " + e.GetBaseException().Message);
                }

                _avatarApplyStage = 2;
                return;
            }

            // Stage 2: hand off only to the preserved official customization
            // puppet. The old implementation force-enabled Player.leftHand,
            // Player.rightHand and PlayerAvatar renderers here, resurrecting
            // the rectangular fallback/Grok arms after the real avatar mounted.
            if (stage == 2)
            {
                Plugin.Log.LogWarning(
                    $"[AVATAR] stage2 official-puppet-only handoff " +
                    $"selections={selections}");

                // Stage 3 path: mount the REAL title customization puppet
                // (AnimatedPlayerPuppet with wardrobe addressable meshes).
                try { MountRealCustomizationAvatarOnPlayer(player); }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning(
                        "[AVATAR] stage2 mount: " + e.GetBaseException().Message);
                }

                _avatarApplyStage = 4;
                _avatarApplySucceeded = true;
                _avatarOutfitTried = true;
                Plugin.Log.LogWarning(
                    $"[AVATAR] customization handoff complete " +
                    $"(REAL puppet path) selections={selections} " +
                    $"pinned={_pinnedCustomizationAvatarPtr != IntPtr.Zero} " +
                    $"mounted={_realAvatarMounted}");
                return;
            }

            if (stage >= 4)
            {
                _avatarApplySucceeded = true;
            }
        }
        catch (Exception e)
        {
            // Advance stage so one bad step cannot loop forever / re-crash.
            if (_avatarApplyStage < 4)
                _avatarApplyStage++;
            if (force)
                Plugin.Log.LogWarning(
                    "[AVATAR] apply failed stage=" + _avatarApplyStage + ": " +
                    e.GetBaseException().Message);
        }
    }

    private static UnityEngine.Vector3 ResolveOrientationSpawnPose(
        out UnityEngine.Quaternion rot)
    {
        rot = UnityEngine.Quaternion.identity;
        var pos = new UnityEngine.Vector3(0f, 1f, 0f);
        try
        {
            var all = UnityEngine.Resources.FindObjectsOfTypeAll(
                Il2CppInterop.Runtime.Il2CppType.Of<SceneSpawnPoint>());
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    var sp = all[i]?.TryCast<SceneSpawnPoint>();
                    if (sp == null || sp.gameObject == null || !sp.gameObject.scene.IsValid())
                        continue;
                    pos = sp.transform.position;
                    rot = sp.transform.rotation;
                    break;
                }
            }
        }
        catch { /* ignore */ }
        return pos;
    }

    // Pure IL2CPP path — must not reference typeof(RecRoom.LocalPlayerController)
    // or TryCast to it (TypeLoadException on InjectedSingletonMonoBehaviour`2).
    private static unsafe bool TryLocalPlayerControllerInstantiate(
        UnityEngine.Vector3 pos,
        UnityEngine.Quaternion rot,
        out string detail)
    {
        detail = "none";
        try
        {
            var lpcObj = FindLiveNativeComponent(
                "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
            if (lpcObj == null || lpcObj.Pointer == IntPtr.Zero)
            {
                detail = "LocalPlayerController missing";
                return false;
            }

            var klass = IL2CPP.GetIl2CppClass(
                "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
            if (klass == IntPtr.Zero)
            {
                detail = "LocalPlayerController IL2CPP class missing";
                return false;
            }

            var method = IL2CPP.il2cpp_class_get_method_from_name(
                klass, "InstantiateLocalPlayer", 2);
            if (method == IntPtr.Zero)
            {
                detail = "InstantiateLocalPlayer(2) not found";
                return false;
            }

            // Box Vector3 / Quaternion as IL2CPP value-type args.
            var posPtr = stackalloc byte[sizeof(float) * 3];
            *(float*)posPtr = pos.x;
            *((float*)posPtr + 1) = pos.y;
            *((float*)posPtr + 2) = pos.z;
            var rotPtr = stackalloc byte[sizeof(float) * 4];
            *(float*)rotPtr = rot.x;
            *((float*)rotPtr + 1) = rot.y;
            *((float*)rotPtr + 2) = rot.z;
            *((float*)rotPtr + 3) = rot.w;

            var args = stackalloc void*[2];
            args[0] = posPtr;
            args[1] = rotPtr;
            var exception = IntPtr.Zero;
            var result = IL2CPP.il2cpp_runtime_invoke(
                method, lpcObj.Pointer, args, ref exception);
            if (exception != IntPtr.Zero)
            {
                detail = DescribeIl2CppException(exception);
                return false;
            }

            if (result == IntPtr.Zero)
            {
                detail = "InstantiateLocalPlayer returned null";
                return false;
            }

            detail = $"player=0x{result.ToInt64():X} pos={pos} via=IL2CPP";
            return true;
        }
        catch (Exception e)
        {
            detail = $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}";
            return false;
        }
    }

    private static void RepairOrientationEnvironment()
    {
        // Basic lighting so the plaza isn't pure black.
        try
        {
            UnityEngine.RenderSettings.ambientMode =
                UnityEngine.Rendering.AmbientMode.Trilight;
            UnityEngine.RenderSettings.ambientSkyColor =
                new UnityEngine.Color(0.55f, 0.65f, 0.85f);
            UnityEngine.RenderSettings.ambientEquatorColor =
                new UnityEngine.Color(0.45f, 0.45f, 0.45f);
            UnityEngine.RenderSettings.ambientGroundColor =
                new UnityEngine.Color(0.25f, 0.22f, 0.20f);
            UnityEngine.RenderSettings.ambientIntensity = 1.1f;
            UnityEngine.RenderSettings.fog = false;
        }
        catch { /* ignore */ }

        // Prefer a real scene directional light; create one if missing.
        try
        {
            var lights = UnityEngine.Resources.FindObjectsOfTypeAll(
                Il2CppInterop.Runtime.Il2CppType.Of<UnityEngine.Light>());
            var hasDirectional = false;
            if (lights != null)
            {
                for (var i = 0; i < lights.Length; i++)
                {
                    var lightComp = lights[i]?.TryCast<UnityEngine.Light>();
                    if (lightComp != null &&
                        lightComp.type == UnityEngine.LightType.Directional &&
                        lightComp.gameObject != null &&
                        lightComp.gameObject.scene.IsValid())
                    {
                        lightComp.enabled = true;
                        lightComp.intensity = Math.Max(lightComp.intensity, 1.0f);
                        hasDirectional = true;
                        break;
                    }
                }
            }

            if (!hasDirectional)
            {
                var sun = new UnityEngine.GameObject("FluxRec_Sun");
                var light = sun.AddComponent<UnityEngine.Light>();
                light.type = UnityEngine.LightType.Directional;
                light.intensity = 1.15f;
                light.color = new UnityEngine.Color(1f, 0.96f, 0.9f);
                sun.transform.rotation = UnityEngine.Quaternion.Euler(50f, -30f, 0f);
                Plugin.Log.LogWarning("[ENV] created directional sun light");
            }
        }
        catch { /* ignore */ }

        // Always force a bright solid sky offline. Black/void skybox materials
        // and URP overrides keep resetting clear flags otherwise.
        try
        {
            var skyColor = new UnityEngine.Color(0.45f, 0.72f, 0.98f);
            try { UnityEngine.RenderSettings.skybox = null; } catch { /* ignore */ }
            UnityEngine.RenderSettings.ambientMode =
                UnityEngine.Rendering.AmbientMode.Flat;
            UnityEngine.RenderSettings.ambientLight =
                new UnityEngine.Color(0.65f, 0.72f, 0.85f);
            UnityEngine.RenderSettings.ambientIntensity = 1.2f;
            UnityEngine.RenderSettings.fog = false;

            void PaintCam(UnityEngine.Camera cam)
            {
                if (cam == null || cam.gameObject == null)
                    return;
                if (IsJunkOrNonPlayerCamera(cam.gameObject.name, cam.gameObject.scene.name))
                    return;
                cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                cam.backgroundColor = skyColor;
            }

            var live = UnityEngine.Camera.allCameras;
            if (live != null)
            {
                for (var i = 0; i < live.Length; i++)
                    PaintCam(live[i]);
            }

            // Also paint MainCamera / FluxRec by direct lookup (avoid heavy
            // FindObjectsOfTypeAll every frame — it has TypeLoad crashed).
            try { PaintCam(UnityEngine.Camera.main); } catch { /* ignore */ }
            try
            {
                var flux = UnityEngine.GameObject.Find("FluxRec_PlayerCamera");
                if (flux != null)
                    PaintCam(flux.GetComponent<UnityEngine.Camera>());
            }
            catch { /* ignore */ }

            if (!_envSkyForcedLogged)
            {
                _envSkyForcedLogged = true;
                Plugin.Log.LogWarning(
                    "[ENV] forced solid bright Orientation sky on all player cameras");
            }
        }
        catch { /* ignore */ }

        // Ensure Orientation scene roots are active (some spawn empty/hidden).
        try
        {
            var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                var sc = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!sc.IsValid() || !sc.isLoaded)
                    continue;
                var name = sc.name ?? string.Empty;
                if (name.IndexOf("Orientation", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var roots = sc.GetRootGameObjects();
                if (roots == null)
                    continue;
                for (var r = 0; r < roots.Length; r++)
                {
                    if (roots[r] != null && !roots[r].activeSelf)
                        roots[r].SetActive(true);
                }
            }
        }
        catch { /* ignore */ }

        if (!_envSkyForcedLogged)
            Plugin.Log.LogWarning("[ENV] Orientation lighting/sky repair applied");
    }

    private static unsafe void StartLocalPlayerSpawnViaSceneManagerSafe(
        SceneSpawnManager spawnManager)
    {
        if (spawnManager == null || spawnManager.Pointer == IntPtr.Zero)
            return;

        var klass = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "SceneSpawnManager");
        var method = IL2CPP.il2cpp_class_get_method_from_name(klass, "CLMOOCHEOHN", 1);
        if (method == IntPtr.Zero)
            return;

        var token = stackalloc IntPtr[2];
        token[0] = IntPtr.Zero;
        token[1] = IntPtr.Zero;
        var args = stackalloc void*[1];
        args[0] = token;
        var exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(method, spawnManager.Pointer, args, ref exception);
        if (exception != IntPtr.Zero)
            Plugin.Log.LogWarning(
                $"[PLAYER-SPAWN] CLMOOCHEOHN: {DescribeIl2CppException(exception)}");
        else
            Plugin.Log.LogWarning("[PLAYER-SPAWN] CLMOOCHEOHN started");
    }

    // PhotonNetwork.Instantiate equivalent: GMIKKPABJEA(prefab, pos, rot, ...)
    private static bool TryPhotonInstantiateLocalPlayer(
        UnityEngine.Vector3 pos,
        UnityEngine.Quaternion rot,
        out string detail)
    {
        detail = "none";
        try
        {
            // Prefab name candidates used by Rec Room / PUN setups.
            var names = new[]
            {
                "[Player]",
                "PlayerPrefab",
                "Player",
                "LocalPlayer",
                "PlayerDesktop",
                "DesktopPlayer",
            };

            // Exact interop overload:
            // GMIKKPABJEA(String, Vector3, Quaternion, Single, Byte, Il2CppReferenceArray, Boolean)
            var method = AccessTools.Method(
                typeof(DBMCMCHBCII),
                "GMIKKPABJEA",
                new[]
                {
                    typeof(string),
                    typeof(UnityEngine.Vector3),
                    typeof(UnityEngine.Quaternion),
                    typeof(float),
                    typeof(byte),
                    typeof(Il2CppReferenceArray<Il2CppSystem.Object>),
                    typeof(bool),
                });
            if (method == null)
            {
                detail = "GMIKKPABJEA(string,Vector3,Quaternion,float,byte,array,bool) not found";
                Plugin.Log.LogError($"[PLAYER-SPAWN] {detail}");
                return false;
            }

            foreach (var name in names)
            {
                try
                {
                    var result = method.Invoke(null, new object[]
                    {
                        name,
                        pos,
                        rot,
                        0f,
                        (byte)0,
                        null,
                        false,
                    });

                    var go = result as UnityEngine.GameObject;
                    if (go != null && go.Pointer != IntPtr.Zero)
                    {
                        // Cache immediately so walk never depends on FindObjectsOfTypeAll.
                        try
                        {
                            var p = go.GetComponent<Player>();
                            if (p == null)
                                p = go.GetComponentInChildren<Player>(true);
                            if (p != null && p.Pointer != IntPtr.Zero)
                            {
                                _cachedLocalPlayer = p;
                                _cachedLocalPlayerPtr = p.Pointer;
                                _cachedLocalPlayerAt = DateTime.UtcNow;
                                _spawnFloorY = go.transform.position.y;
                                Plugin.Log.LogWarning(
                                    $"[GAMEPLAY] cached Photon player " +
                                    $"0x{p.Pointer.ToInt64():X} go='{go.name}'");
                            }
                        }
                        catch { /* ignore */ }

                        detail = $"prefab='{name}' go='{go.name}' pos={pos}";
                        return true;
                    }

                    Plugin.Log.LogWarning(
                        $"[PLAYER-SPAWN] Instantiate '{name}' returned null");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning(
                        $"[PLAYER-SPAWN] Instantiate '{name}' failed: " +
                        $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
                }
            }

            detail = "all prefab names failed";
            return false;
        }
        catch (Exception e)
        {
            detail = e.GetBaseException().Message;
            return false;
        }
    }

    private static bool EnsureOfflineCoreGameplayRuntime()
    {
        var localPlayerController = FindLiveNativeComponent(
            "Assembly-CSharp.dll", "RecRoom", "LocalPlayerController");
        var audioManager = FindLiveNativeComponent(
            "Assembly-CSharp.dll", "RecRoom.Audio", "AudioManager");
        if (localPlayerController != null && audioManager != null)
        {
            // In the normal online boot sequence AudioManager.Initialize is
            // called after account services are ready.  The direct offline
            // Orientation path reaches player spawning without that stage,
            // leaving MicSpamMonitor null. Player.Awake subscribes to that
            // monitor and otherwise aborts before it creates the real desktop
            // camera, input controller, avatar animation, and locomotion.
            if (!EnsureOfflineAudioManagerInitialized(audioManager))
                return false;

            if (!_offlineCoreGameplayReadyLogged)
            {
                _offlineCoreGameplayReadyLogged = true;
                Plugin.Log.LogWarning(
                    "[CORE-RUNTIME] shipped LocalPlayerController and AudioManager " +
                    $"are live controller=0x{localPlayerController.Pointer.ToInt64():X} " +
                    $"audio=0x{audioManager.Pointer.ToInt64():X}");
            }
            return true;
        }

        // main_root owns the complete title/VR signup boot stack. Loading it
        // additively after Orientation re-enters account creation. The shipped
        // late root is the safe source for gameplay/audio singletons here.
        const string sceneName = "late_main_root";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if ((!scene.IsValid() || !scene.isLoaded) && !_lateMainRootLoadRequested)
        {
            _lateMainRootLoadRequested = true;
            Plugin.Log.LogWarning(
                "[CORE-RUNTIME] loading shipped late_main_root before the local " +
                "Player (main_root intentionally skipped)");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                sceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Additive);
            return false;
        }

        if (!_offlineCoreGameplayWaitLogged)
        {
            _offlineCoreGameplayWaitLogged = true;
            Plugin.Log.LogWarning(
                "[CORE-RUNTIME] waiting for shipped late core systems " +
                $"sceneLoaded={(scene.IsValid() && scene.isLoaded)} " +
                $"controller=0x{(localPlayerController?.Pointer ?? IntPtr.Zero).ToInt64():X} " +
                $"audio=0x{(audioManager?.Pointer ?? IntPtr.Zero).ToInt64():X}");
        }
        return false;
    }

    private static unsafe bool EnsureOfflineAudioManagerInitialized(
        UnityEngine.Object audioManager)
    {
        if (audioManager == null || audioManager.Pointer == IntPtr.Zero)
            return false;

        var audioClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "RecRoom.Audio", "AudioManager");
        if (audioClass == IntPtr.Zero)
            throw new InvalidOperationException(
                "The shipped AudioManager IL2CPP class was not found.");

        var micSpamMonitor = ReadIl2CppReferenceField(
            audioManager.Pointer, audioClass, "MicSpamMonitor");
        if (micSpamMonitor != IntPtr.Zero)
            return EnsureOfflineMicSpamMonitorEvent(micSpamMonitor);

        if (_offlineAudioManagerInitializationAttempted)
            return false;
        _offlineAudioManagerInitializationAttempted = true;

        var initialize = IL2CPP.il2cpp_class_get_method_from_name(
            audioClass, "Initialize", 0);
        if (initialize == IntPtr.Zero)
            throw new InvalidOperationException(
                "The shipped AudioManager.Initialize method was not found.");

        var exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(
            initialize, audioManager.Pointer, null, ref exception);
        if (exception != IntPtr.Zero)
        {
            if (!_offlineAudioManagerInitializationFailureLogged)
            {
                _offlineAudioManagerInitializationFailureLogged = true;
                Plugin.Log.LogError(
                    "[CORE-RUNTIME] shipped AudioManager.Initialize threw " +
                    $"({DescribeIl2CppException(exception)}); restoring its " +
                    "stock MicSpamMonitor finalization stage directly");
            }
        }

        micSpamMonitor = ReadIl2CppReferenceField(
            audioManager.Pointer, audioClass, "MicSpamMonitor");
        if (micSpamMonitor == IntPtr.Zero)
        {
            micSpamMonitor = CreateStockMicSpamMonitor(
                audioManager.Pointer, audioClass);
        }

        if (!EnsureOfflineMicSpamMonitorEvent(micSpamMonitor))
            return false;

        if (!_offlineAudioManagerInitializedLogged)
        {
            _offlineAudioManagerInitializedLogged = true;
            Plugin.Log.LogWarning(
                "[CORE-RUNTIME] completed the stock AudioManager.Initialize " +
                $"stage micSpamMonitor=0x{micSpamMonitor.ToInt64():X}");
        }
        return true;
    }

    private static unsafe IntPtr CreateStockMicSpamMonitor(
        IntPtr audioManager,
        IntPtr audioClass)
    {
        // This is the exact tail of AudioManager.Initialize in this depot:
        // LAMKHIEAINA.AIEOHDGIDAA supplies the real voice-account service,
        // KPHPLINLEMH::.ctor builds the monitor and its stock event, and the
        // result is assigned to AudioManager.MicSpamMonitor.
        var servicesClass = IL2CPP.GetIl2CppClass(
            "RecNet.Runtime.dll", "", "LAMKHIEAINA");
        var monitorClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "KPHPLINLEMH");
        if (servicesClass == IntPtr.Zero || monitorClass == IntPtr.Zero)
            throw new InvalidOperationException(
                "The shipped microphone-spam monitor types were not found.");

        var getVoiceAccountService = IL2CPP.il2cpp_class_get_method_from_name(
            servicesClass, "AIEOHDGIDAA", 0);
        var constructor = IL2CPP.il2cpp_class_get_method_from_name(
            monitorClass, ".ctor", 1);
        if (getVoiceAccountService == IntPtr.Zero || constructor == IntPtr.Zero)
            throw new InvalidOperationException(
                "The shipped microphone-spam monitor constructor path was not found.");

        var exception = IntPtr.Zero;
        var voiceAccountService = IL2CPP.il2cpp_runtime_invoke(
            getVoiceAccountService, IntPtr.Zero, null, ref exception);
        if (exception != IntPtr.Zero || voiceAccountService == IntPtr.Zero)
        {
            // LAMKHIEAINA normally receives api/config before AudioManager is
            // initialized. The local backend intentionally has no legacy
            // config payload, so install the exact stock RecNet.Runtime config
            // objects using IBFJKHEIMCE's own default constructor values.
            voiceAccountService = EnsureOfflineVoiceSpamConfiguration(
                servicesClass,
                exception == IntPtr.Zero
                    ? "service was null"
                    : DescribeIl2CppException(exception));
        }

        var monitor = IL2CPP.il2cpp_object_new(monitorClass);
        if (monitor == IntPtr.Zero)
            throw new InvalidOperationException(
                "Could not allocate the shipped microphone-spam monitor.");

        var args = stackalloc void*[1];
        // Reference-type arguments are passed to the IL2CPP invoker as the
        // object pointer itself. Passing the address of a stack local made the
        // monitor retain a bogus service pointer and destabilized Player.Awake.
        args[0] = (void*)voiceAccountService;
        exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(
            constructor, monitor, args, ref exception);
        if (exception != IntPtr.Zero)
            throw new InvalidOperationException(
                "Microphone-spam monitor constructor threw: " +
                DescribeIl2CppException(exception));

        WriteIl2CppReferenceField(
            audioManager, audioClass, "MicSpamMonitor", monitor);
        Plugin.Log.LogWarning(
            "[CORE-RUNTIME] restored the real stock MicSpamMonitor " +
            $"monitor=0x{monitor.ToInt64():X} service=0x{voiceAccountService.ToInt64():X}");
        return monitor;
    }

    private static unsafe bool EnsureOfflineMicSpamMonitorEvent(IntPtr monitor)
    {
        if (monitor == IntPtr.Zero)
            return false;

        var monitorClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "KPHPLINLEMH");
        var eventClass = IL2CPP.GetIl2CppClass(
            "Assembly-CSharp.dll", "", "JEAMPGBKNAI");
        if (monitorClass == IntPtr.Zero || eventClass == IntPtr.Zero)
            return false;

        var eventPointer = ReadIl2CppReferenceField(
            monitor, monitorClass, "MLBOHMHOMKC");
        if (eventPointer == IntPtr.Zero)
        {
            eventPointer = AllocateAndConstructStockObject(
                eventClass, "JEAMPGBKNAI");
            WriteIl2CppReferenceField(
                monitor, monitorClass, "MLBOHMHOMKC", eventPointer);
            Plugin.Log.LogWarning(
                "[CORE-RUNTIME] repaired null MicSpamMonitor event " +
                $"event=0x{eventPointer.ToInt64():X}");
        }

        if (eventPointer != IntPtr.Zero &&
            (_offlineMicSpamMonitorEvent == null ||
             _offlineMicSpamMonitorEvent.Pointer != eventPointer))
        {
            _offlineMicSpamMonitorEvent = new Il2CppSystem.Object(eventPointer);
        }

        return eventPointer != IntPtr.Zero;
    }

    private static unsafe IntPtr EnsureOfflineVoiceSpamConfiguration(
        IntPtr servicesClass,
        string reason)
    {
        var configClass = IL2CPP.GetIl2CppClass(
            "RecNet.Runtime.dll", "", "LJAPOLJDCFP");
        var voiceSpamClass = IL2CPP.GetIl2CppClass(
            "RecNet.Runtime.dll", "", "IBFJKHEIMCE");
        if (configClass == IntPtr.Zero || voiceSpamClass == IntPtr.Zero)
            throw new InvalidOperationException(
                "The shipped RecNet runtime config types were not found.");

        var configField = IL2CPP.il2cpp_class_get_field_from_name(
            servicesClass, "GLONDIMDLCD");
        if (configField == IntPtr.Zero)
            throw new InvalidOperationException(
                "The shipped RecNet runtime config field was not found.");

        var config = IntPtr.Zero;
        IL2CPP.il2cpp_field_static_get_value(configField, &config);
        if (config == IntPtr.Zero)
        {
            config = AllocateAndConstructStockObject(configClass, "LJAPOLJDCFP");
            IL2CPP.il2cpp_field_static_set_value(configField, &config);
        }

        var voiceSpam = ReadIl2CppReferenceField(
            config,
            configClass,
            "<GIMMKLMAILF>k__BackingField");
        if (voiceSpam == IntPtr.Zero)
        {
            voiceSpam = AllocateAndConstructStockObject(
                voiceSpamClass, "IBFJKHEIMCE");
            WriteIl2CppReferenceField(
                config,
                configClass,
                "<GIMMKLMAILF>k__BackingField",
                voiceSpam);
        }

        Plugin.Log.LogWarning(
            "[CORE-RUNTIME] installed the stock default voice-spam config " +
            $"after legacy api/config was absent ({reason}) " +
            $"config=0x{config.ToInt64():X} voice=0x{voiceSpam.ToInt64():X}");
        return voiceSpam;
    }

    private static unsafe IntPtr AllocateAndConstructStockObject(
        IntPtr klass,
        string className)
    {
        var instance = IL2CPP.il2cpp_object_new(klass);
        if (instance == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Could not allocate the shipped {className} object.");

        var constructor = IL2CPP.il2cpp_class_get_method_from_name(
            klass, ".ctor", 0);
        if (constructor == IntPtr.Zero)
            throw new InvalidOperationException(
                $"The shipped {className} constructor was not found.");

        var exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(
            constructor, instance, null, ref exception);
        if (exception != IntPtr.Zero)
            throw new InvalidOperationException(
                $"The shipped {className} constructor threw: " +
                DescribeIl2CppException(exception));
        return instance;
    }

    private static string DescribeIl2CppException(IntPtr exception)
    {
        if (exception == IntPtr.Zero)
            return "none";

        try
        {
            return new Il2CppSystem.Exception(exception).ToString();
        }
        catch
        {
            return $"IL2CPP exception 0x{exception.ToInt64():X}";
        }
    }

    private static UnityEngine.Object FindLiveNativeComponent(
        string assembly,
        string namespaceName,
        string className)
    {
        var klass = IL2CPP.GetIl2CppClass(
            assembly, namespaceName, className);
        if (klass == IntPtr.Zero)
            return null;

        var type = Il2CppType.TypeFromPointer(
            klass,
            string.IsNullOrEmpty(namespaceName)
                ? className
                : namespaceName + "." + className);
        var all = UnityEngine.Resources.FindObjectsOfTypeAll(type);
        if (all == null)
            return null;

        for (var i = 0; i < all.Length; i++)
        {
            var candidate = all[i];
            if (candidate == null || candidate.Pointer == IntPtr.Zero)
                continue;

            var component = candidate.TryCast<UnityEngine.Component>();
            var gameObject = component?.gameObject;
            if (gameObject == null || gameObject.Pointer == IntPtr.Zero)
                continue;

            // Prefab assets also appear in FindObjectsOfTypeAll. Only accept a
            // component whose GameObject belongs to a live scene (including
            // DontDestroyOnLoad), because only that instance owns the singleton.
            if (gameObject.scene.IsValid())
                return candidate;
        }

        return null;
    }

    // The spawn parks at WaitingForBigData(4) forever on a bundled offline
    // room. That state waits on BaseBigDataNetworkingManager.RunAllDataRetrieval,
    // which downloads a room's saved "big data" (maker-pen creations, circuits,
    // ...). Orientation ships its content in the scene and has no such payload
    // to fetch, and with no backend serving one the retrieval task never
    // completes - so the spawn stalls and the loading screen sits at 100%.
    // Cancel() is the shipped, public way to end that retrieval; cancelling
    // resolves the awaited task and lets the state machine move on.
    private static void ReleaseBigDataGateIfStuck(string state)
    {
        if (_bigDataGateReleased)
            return;
        if (!string.Equals(state, "WaitingForBigData", StringComparison.Ordinal))
            return;
        if (!_localPlayerSpawnStateSince.HasValue)
            return;

        // Orientation has no big-data payload — release quickly (min 0.5s).
        var stuckFor =
            (DateTime.UtcNow - _localPlayerSpawnStateSince.Value).TotalSeconds;
        if (stuckFor < Math.Max(0.5, Math.Min(2.0, Plugin.BigDataGateSeconds.Value)))
            return;

        _bigDataGateReleased = true;
        try
        {
            // BaseBigDataNetworkingManager.Cancel() cannot be used here: the
            // game crashes inside its native cancellation machinery (the
            // retrieval's task continuations run against a broken HTTP path),
            // killing the process before this line can even log. Instead push
            // the spawn state forward ourselves; the pump then completes the
            // offline instantiation handoff on a short timer.
            var setter = AccessTools.Method(
                typeof(SceneSpawnManager), "set_LocalPlayerSpawnState",
                new[] { typeof(SceneSpawnManager.GOHDFONKFML) });
            if (setter == null)
            {
                Plugin.Log.LogWarning(
                    "[BIGDATA] SceneSpawnManager.set_LocalPlayerSpawnState not found");
                return;
            }

            setter.Invoke(null, new object[]
            {
                SceneSpawnManager.GOHDFONKFML.Instantiating,
            });

            Plugin.Log.LogWarning(
                $"[BIGDATA] bypassed WaitingForBigData after {stuckFor:0.0}s and " +
                "forced the stock spawn into its instantiating phase (bundled " +
                "Orientation has no big-data payload to download)");
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[BIGDATA] could not bypass the retrieval: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    private static void PumpOrientationLocalPlayerSpawn()
    {
        if (!_localPlayerSpawnDueAt.HasValue && !_localPlayerSpawnStarted)
            return;

        var exists = GetLocalPlayerExists();
        var state = GetLocalPlayerSpawnState();
        if (state != _localPlayerSpawnLastState)
        {
            _localPlayerSpawnLastState = state;
            _localPlayerSpawnStateSince = DateTime.UtcNow;
            Plugin.Log.LogInfo(
                $"[PLAYER-SPAWN] state={state} localPlayerExists={exists}");
        }

        ReleaseBigDataGateIfStuck(state);

        // A Player object exists as early as WaitingForCameraFade, but its
        // SpawnLocal completion callback has not yet enabled desktop camera and
        // locomotion at that point. Treat only the game's terminal state as a
        // successful handoff and keep both this pump and the loading screen
        // alive until then.
        // As soon as a local Player exists, enable its cameras and clear loading.
        // Do not wait for a perfect Camera.main tag — that hung users at 100%.
        // Keep re-running camera/env repair for several seconds after spawn;
        // Player.Awake often NRE's before the stock desktop camera attaches.
        if (exists)
        {
            if (_postSpawnRepairUntil.HasValue &&
                DateTime.UtcNow <= _postSpawnRepairUntil.Value &&
                (!_postSpawnRepairNextAt.HasValue ||
                 DateTime.UtcNow >= _postSpawnRepairNextAt.Value))
            {
                _postSpawnRepairNextAt = DateTime.UtcNow.AddSeconds(1.0);
                RunPostSpawnPresentationRepairs();
            }
            else if (!_offlineCameraRecoveryAttempted)
            {
                _offlineCameraRecoveryAttempted = true;
                RunPostSpawnPresentationRepairs(force: true);
            }

            var presentationReady = TryGetOrientationPlayerPresentationReady(
                out var presentationState);
            var completed = string.Equals(state, "SpawnedAndFadedIn", StringComparison.Ordinal) ||
                            string.Equals(state, "SpawnedWaitingForFade", StringComparison.Ordinal);

            // Only declare success when we have a real player view (not LoadingScreen).
            if (presentationReady &&
                (completed ||
                 (_localPlayerSpawnStartedAt.HasValue &&
                  (DateTime.UtcNow - _localPlayerSpawnStartedAt.Value).TotalSeconds >= 2.0)))
            {
                _localPlayerSpawnDueAt = null;
                _localPlayerSpawnStarted = false;
                try { HideLoadingScreen(); }
                catch { _hideLoadingScreenAt = DateTime.UtcNow; }
                if (!_localPlayerSpawnSucceededLogged)
                {
                    _localPlayerSpawnSucceededLogged = true;
                    Plugin.Log.LogWarning(
                        $"[PLAYER-SPAWN] PLAYER IN ORIENTATION state={state} " +
                        $"cam={presentationState}; loading screen cleared");
                }
                _forceOrientationEnterDone = true;
                return;
            }

            // Player exists but still no real camera after a few seconds: keep
            // loading clear so the user is not stuck at 100%, but keep repairing.
            if (_localPlayerSpawnStartedAt.HasValue &&
                (DateTime.UtcNow - _localPlayerSpawnStartedAt.Value).TotalSeconds >= 5.0 &&
                !_localPlayerSpawnSucceededLogged)
            {
                try { HideLoadingScreen(); }
                catch { _hideLoadingScreenAt = DateTime.UtcNow; }
                Plugin.Log.LogWarning(
                    $"[PLAYER-SPAWN] player exists but presentation incomplete " +
                    $"(state={state} cam={presentationState}); loading cleared, still repairing");
                _localPlayerSpawnSucceededLogged = true;
            }
        }

        var completedState = string.Equals(
            state,
            "SpawnedAndFadedIn",
            StringComparison.Ordinal);
        var presentationReady2 = TryGetOrientationPlayerPresentationReady(
            out var presentationState2);
        if (exists && completedState && presentationReady2)
        {
            _localPlayerSpawnDueAt = null;
            _localPlayerSpawnStarted = false;
            try { HideLoadingScreen(); } catch { _hideLoadingScreenAt = DateTime.UtcNow; }
            return;
        }

        if (_localPlayerSpawnStarted)
        {
            var spawnAge = _localPlayerSpawnStartedAt.HasValue
                ? (DateTime.UtcNow - _localPlayerSpawnStartedAt.Value).TotalSeconds
                : 0;
            // The stock path often jumps Instantiating -> SpawnedAndFadedIn
            // without lingering, so also trigger the handoff when presentation
            // is stuck with a player that already "exists".
            if (!_offlineInstantiationHandoffCompleted &&
                spawnAge > (_bigDataGateReleased ? 2.0 : 8.0) &&
                (string.Equals(state, "Instantiating", StringComparison.Ordinal) ||
                 string.Equals(state, "WaitingForBigData", StringComparison.Ordinal) ||
                 (exists && !presentationReady2)))
            {
                _offlineInstantiationHandoffCompleted = true;
                CompleteOfflineOrientationInstantiationHandoff();
                return;
            }

            if (_localPlayerSpawnStartedAt.HasValue &&
                spawnAge > 20 &&
                !_localPlayerSpawnTimeoutLogged)
            {
                _localPlayerSpawnTimeoutLogged = true;
                if (exists)
                    TryActivatePlayerCameras(out _);
                TryActivateOrientationCameras(out _);
                try { HideLoadingScreen(); } catch { _hideLoadingScreenAt = DateTime.UtcNow; }
                _localPlayerSpawnStarted = false;
                Plugin.Log.LogError(
                    $"[PLAYER-SPAWN] timeout after 20s (exists={exists} state={state}); " +
                    "cleared loading screen");
            }
            return;
        }

        if (!_localPlayerSpawnDueAt.HasValue ||
            DateTime.UtcNow < _localPlayerSpawnDueAt.Value)
            return;

        // Proven path (orientation-playerawake backup): full dep install then
        // SceneSpawnManager.PHHFEHIGAAD. That is what instantiates the real
        // Player prefab + avatar. CLMOOCHEOHN alone leaves state Uninitialized.
        _localPlayerSpawnAttempts++;
        try
        {
            if (StartOrientationLocalPlayerSpawn())
            {
                _localPlayerSpawnDueAt = null;
                return;
            }
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[PLAYER-SPAWN] start attempt {_localPlayerSpawnAttempts} failed: " +
                $"{root.GetType().Name}: {root.Message}");
            _localPlayerSpawnStarted = false;
        }

        if (_localPlayerSpawnAttempts < 40)
        {
            _localPlayerSpawnDueAt = DateTime.UtcNow.AddMilliseconds(350);
            return;
        }

        _localPlayerSpawnDueAt = null;
        Plugin.Log.LogError(
            "[PLAYER-SPAWN] stock PHHFEHIGAAD never started after 40 attempts");
        // If a player somehow exists, still clear loading; otherwise keep trying
        // camera recovery path from ForceOrientationEnterIfStuck.
        if (GetLocalPlayerExists())
            ForceOrientationEnter("spawn-timeout-with-player", requirePlayer: true);
    }

    // Clear the stuck 100% bar once Orientation is loaded and we've given spawn
    // a fair chance. Also self-heal if we never left TitleScreen.
    private static void ForceOrientationEnterIfStuck()
    {
        if (_forceOrientationEnterDone)
            return;

        // If still on TitleScreen with loading at 100% for too long, force the
        // direct Orientation scene load (the BootLocalPlayer loop used to block this).
        if (_loadingScreenShown && _directSceneLoadArmedAt.HasValue)
        {
            var loadAge = (DateTime.UtcNow - _directSceneLoadArmedAt.Value).TotalSeconds;
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "";
            if (loadAge >= 8.0 &&
                string.Equals(active, "TitleScreen", StringComparison.Ordinal) &&
                _directSceneLoadDueAt.HasValue)
            {
                Plugin.Log.LogWarning(
                    "[ORIENTATION] still on TitleScreen after 8s at loading; " +
                    "forcing direct Orientation scene load now");
                _bootLocalPlayerAttempted = true;
                _offlineTravelAttempted = true;
                _directSceneLoadDueAt = DateTime.UtcNow;
            }
        }

        if (!_loadingScreenShown)
            return;

        if (!_orientationScenesReadyAt.HasValue)
        {
            if (AreOrientationScenesLoaded())
                _orientationScenesReadyAt = DateTime.UtcNow;
            else
                return;
        }

        var age = (DateTime.UtcNow - _orientationScenesReadyAt.Value).TotalSeconds;
        var exists = GetLocalPlayerExists();
        var presentationReady = TryGetOrientationPlayerPresentationReady(out var presentationState);

        // Real success path.
        if (exists && presentationReady)
        {
            ForceOrientationEnter(
                $"player-ready age={age:0.0}s {presentationState}",
                requirePlayer: true);
            return;
        }

        // Keep retrying stock spawn.
        if (age > 1.0 && age < 30.0 &&
            !_localPlayerSpawnStarted &&
            !_localPlayerSpawnDueAt.HasValue)
        {
            _localPlayerSpawnDueAt = DateTime.UtcNow;
            _localPlayerSpawnAttempts = Math.Min(_localPlayerSpawnAttempts, 20);
        }

        // Player exists but camera missing — recover and hide bar.
        if (exists && age >= 8.0)
        {
            ForceOrientationEnter(
                $"player-exists-camera-recovery age={age:0.0}s {presentationState}",
                requirePlayer: true);
            return;
        }

        // Give stock spawn more time (Photon join can take 10–20 attempts).
        // Never mark enter "done" without a player — that aborted spawn forever.
        if (age >= 45.0 && !exists)
        {
            // Keep retrying spawn; only clear the stuck bar.
            try { HideLoadingScreen(); } catch { /* ignore */ }
            if (!_localPlayerSpawnDueAt.HasValue && !_localPlayerSpawnStarted)
                _localPlayerSpawnDueAt = DateTime.UtcNow;
            if (age >= 45.0 && age < 46.0)
                Plugin.Log.LogWarning(
                    $"[ORIENTATION] still no player after {age:0.0}s; " +
                    "cleared loading bar but still spawning");
        }
    }

    private static bool AreOrientationScenesLoaded()
    {
        try
        {
            var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            var sawBootstrap = false;
            var sawLevel = false;
            for (var i = 0; i < count; i++)
            {
                var sc = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!sc.IsValid() || !sc.isLoaded)
                    continue;
                var name = sc.name ?? string.Empty;
                if (name.IndexOf("Orientation_additive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(name, Plugin.OrientationSceneName.Value, StringComparison.OrdinalIgnoreCase))
                    sawBootstrap = true;
                if (name.IndexOf("Orientation_Scene", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(name, Plugin.OrientationAdditiveSceneName.Value, StringComparison.OrdinalIgnoreCase))
                    sawLevel = true;
                if (name.StartsWith("Orientation", StringComparison.OrdinalIgnoreCase) &&
                    !sawBootstrap)
                    sawBootstrap = true;
            }

            return sawBootstrap || sawLevel;
        }
        catch
        {
            return false;
        }
    }

    private static void ForceOrientationEnter(string reason, bool requirePlayer = true)
    {
        if (_forceOrientationEnterDone)
            return;

        if (requirePlayer && !GetLocalPlayerExists())
        {
            Plugin.Log.LogWarning(
                $"[ORIENTATION] refuse enter without a local Player ({reason})");
            return;
        }

        _forceOrientationEnterDone = true;

        try
        {
            _localPlayerSpawnDueAt = null;

            // Prefer player-owned cameras over PerfCam / debug cameras.
            TryActivatePlayerCameras(out var camDetail);
            if (UnityEngine.Camera.main == null)
                TryActivateOrientationCameras(out camDetail);

            // ALWAYS hide the stuck downloading bar.
            try { HideLoadingScreen(); }
            catch { _hideLoadingScreenAt = DateTime.UtcNow; }

            // Extra belt-and-suspenders: force LoadingScreen invisible via native.
            try
            {
                if (_loadingScreen != null && _loadingScreen.Pointer != IntPtr.Zero)
                {
                    unsafe
                    {
                        byte visible = 0;
                        var args = stackalloc void*[1];
                        args[0] = &visible;
                        InvokeNative("LoadingScreen", "set_IsVisible", _loadingScreen.Pointer, args, 1);
                    }
                    var screen = _loadingScreen.TryCast<LoadingScreen>();
                    if (screen?.canvas != null)
                        screen.canvas.enabled = false;
                }
            }
            catch { /* ignore */ }

            _loadingScreenShown = false;
            _localPlayerSpawnSucceededLogged = true;
            Plugin.Log.LogWarning(
                $"[ORIENTATION] loading screen CLEARED " +
                $"(reason={reason} cam={camDetail} playerExists={GetLocalPlayerExists()})");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(
                $"[ORIENTATION] enter cleanup failed: {e.GetBaseException().Message}");
            try { HideLoadingScreen(); }
            catch { _hideLoadingScreenAt = DateTime.UtcNow; }
            _loadingScreenShown = false;
        }
    }

    // Enable cameras on the local Player hierarchy (desktop first-person / head).
    private static bool TryActivatePlayerCameras(out string detail)
    {
        try
        {
            var players = UnityEngine.Resources.FindObjectsOfTypeAll<Player>();
            if (players == null || players.Length == 0)
            {
                detail = "no Player objects";
                return false;
            }

            Player local = null;
            for (var i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null || p.Pointer == IntPtr.Zero || p.gameObject == null)
                    continue;
                if (!p.gameObject.scene.IsValid())
                    continue;
                local = p;
                // Prefer something named like local
                var n = p.gameObject.name ?? string.Empty;
                if (n.IndexOf("Local", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0)
                    break;
            }

            if (local == null)
            {
                detail = "no live Player";
                return false;
            }

            // Activate entire player hierarchy first (Awake may have aborted mid-way).
            try
            {
                local.gameObject.SetActive(true);
                var transforms = local.GetComponentsInChildren<UnityEngine.Transform>(true);
                if (transforms != null)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        if (transforms[i] != null && transforms[i].gameObject != null &&
                            !transforms[i].gameObject.activeSelf)
                            transforms[i].gameObject.SetActive(true);
                    }
                }
            }
            catch { /* ignore */ }

            var cams = local.GetComponentsInChildren<UnityEngine.Camera>(true);
            var enabled = 0;
            UnityEngine.Camera best = null;
            if (cams != null)
            {
                for (var i = 0; i < cams.Length; i++)
                {
                    var cam = cams[i];
                    if (cam == null)
                        continue;
                    var go = cam.gameObject;
                    var name = go.name ?? string.Empty;
                    if (name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        name.IndexOf("Head", StringComparison.OrdinalIgnoreCase) < 0 &&
                        name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    go.SetActive(true);
                    cam.enabled = true;
                    enabled++;
                    if (best == null ||
                        name.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Main", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Desktop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0)
                        best = cam;
                }
            }

            // If Player prefab has no Camera components (common when Awake aborts
            // before desktop camera attach), add a first-person camera at the
            // stock desktop eye height (1.6m) on the player root — never under
            // a zeroed Head bone (that put the view under the map).
            if (best == null)
            {
                try
                {
                    var allPlayerCams = local.GetComponentsInChildren<UnityEngine.Camera>(true);
                    if (allPlayerCams != null)
                    {
                        for (var i = 0; i < allPlayerCams.Length; i++)
                        {
                            var cam = allPlayerCams[i];
                            if (cam == null || cam.gameObject == null)
                                continue;
                            if (IsJunkOrNonPlayerCamera(cam.gameObject.name))
                                continue;
                            cam.gameObject.SetActive(true);
                            cam.enabled = true;
                            best = cam;
                            enabled = 1;
                            break;
                        }
                    }
                }
                catch { /* fall through to create */ }

                if (best == null)
                {
                    try
                    {
                        UnityEngine.Transform root = local.transform;
                        var existing = root.Find("FluxRec_PlayerCamera");
                        UnityEngine.GameObject camGo;
                        if (existing != null)
                        {
                            camGo = existing.gameObject;
                            best = camGo.GetComponent<UnityEngine.Camera>() ??
                                   camGo.AddComponent<UnityEngine.Camera>();
                        }
                        else
                        {
                            camGo = new UnityEngine.GameObject("FluxRec_PlayerCamera");
                            camGo.transform.SetParent(root, false);
                            best = camGo.AddComponent<UnityEngine.Camera>();
                        }

                        // Unparented absolute eye camera (parent scale was pinning
                        // the view to the feet / under the map).
                        camGo.transform.SetParent(null, true);
                        var pp = local.transform.position;
                        camGo.transform.SetPositionAndRotation(
                            new UnityEngine.Vector3(pp.x, pp.y + 1.6f, pp.z),
                            local.transform.rotation);
                        best.nearClipPlane = 0.05f;
                        best.farClipPlane = 800f;
                        best.fieldOfView = 75f;
                        best.depth = 100;
                        best.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                        best.backgroundColor = new UnityEngine.Color(0.52f, 0.74f, 0.95f);
                        best.enabled = true;
                        camGo.SetActive(true);
                        enabled = Math.Max(enabled, 1);
                        Plugin.Log.LogWarning(
                            "[PLAYER-SPAWN] attached unparented FP camera at eye " +
                            $"height worldY={pp.y + 1.6f:0.00} playerY={pp.y:0.00}");
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning(
                            $"[PLAYER-SPAWN] fallback player camera failed: " +
                            $"{e.GetBaseException().Message}");
                    }
                }
            }

            if (best != null)
            {
                try { best.gameObject.tag = "MainCamera"; } catch { /* ignore */ }
                DisableJunkCameras();
                // AudioListener so the game has a listening point.
                try
                {
                    if (best.GetComponent<UnityEngine.AudioListener>() == null)
                        best.gameObject.AddComponent<UnityEngine.AudioListener>();
                }
                catch { /* ignore */ }
            }

            var mainName = UnityEngine.Camera.main == null
                ? "null"
                : UnityEngine.Camera.main.name;
            detail =
                $"player='{local.gameObject.name}' cams={enabled} " +
                $"main={mainName} best={(best == null ? "null" : best.name)} " +
                $"pos={local.transform.position}";
            // Success only if we have a non-junk MainCamera or an enabled best cam.
            if (best != null && best.enabled)
                return true;
            if (UnityEngine.Camera.main != null &&
                !IsJunkOrNonPlayerCamera(UnityEngine.Camera.main.name))
                return true;
            return false;
        }
        catch (Exception e)
        {
            detail = e.GetBaseException().GetType().Name;
            return false;
        }
    }

    private static void CompleteOfflineOrientationInstantiationHandoff()
    {
        try
        {
            var setter = AccessTools.Method(
                typeof(SceneSpawnManager), "set_LocalPlayerSpawnState",
                new[] { typeof(SceneSpawnManager.GOHDFONKFML) });
            setter?.Invoke(null, new object[]
            {
                SceneSpawnManager.GOHDFONKFML.SpawnedAndFadedIn,
            });

            TryActivateOrientationCameras(out var camDetail);

            _localPlayerSpawnStarted = false;
            _localPlayerSpawnDueAt = null;
            _hideLoadingScreenAt = DateTime.UtcNow;
            _roomContentReportAt = DateTime.UtcNow.AddSeconds(1);
            Plugin.Log.LogWarning(
                "[PLAYER-SPAWN] released offline Orientation from the stalled " +
                $"network-instantiation gate ({camDetail})");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(
                "[PLAYER-SPAWN] offline instantiation handoff failed: " +
                e.GetBaseException().Message);
        }
    }

    // Find any camera in the Orientation scenes (or any live scene camera as
    // fallback), enable it, and tag it MainCamera so Camera.main resolves and
    // the loading-screen gate can finally open.
    private static bool TryActivateOrientationCameras(out string detail)
    {
        try
        {
            var cameras = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>();
            if (cameras == null || cameras.Length == 0)
            {
                detail = "no cameras in Resources";
                return false;
            }

            UnityEngine.Camera best = null;
            UnityEngine.Camera fallback = null;
            var liveCount = 0;
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera.Pointer == IntPtr.Zero ||
                    camera.gameObject == null)
                    continue;
                var go = camera.gameObject;
                if (!go.scene.IsValid())
                    continue;
                liveCount++;
                var sceneName = go.scene.name ?? string.Empty;
                var camName = go.name ?? string.Empty;
                // Never promote LoadingScreen / PerfCam / UI as the player view.
                if (IsJunkOrNonPlayerCamera(camName, sceneName))
                    continue;
                if (sceneName.StartsWith("Orientation", StringComparison.Ordinal) ||
                    sceneName.IndexOf("Orientation", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Prefer head/desktop/main / flux fallback over random scene cams.
                    if (best == null ||
                        camName.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        camName.IndexOf("Desktop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        camName.IndexOf("FluxRec", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        camName.IndexOf("Main", StringComparison.OrdinalIgnoreCase) >= 0)
                        best = camera;
                    if (camName.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
                // Prefer non-UI / non-loading / non-DDOL cameras for the fallback.
                if (fallback == null &&
                    !string.Equals(sceneName, "DontDestroyOnLoad", StringComparison.Ordinal) &&
                    !string.Equals(sceneName, "TitleScreen", StringComparison.Ordinal))
                {
                    fallback = camera;
                }
            }

            var chosen = best ?? fallback;
            if (chosen == null)
            {
                detail = $"liveCameras={liveCount} none choosable (junk excluded)";
                return false;
            }

            // Walk up and activate parents; inactive parents leave the camera
            // out of hierarchy even if the component itself is enabled.
            var node = chosen.transform;
            while (node != null)
            {
                if (!node.gameObject.activeSelf)
                    node.gameObject.SetActive(true);
                node = node.parent;
            }

            chosen.gameObject.SetActive(true);
            chosen.enabled = true;
            try { chosen.gameObject.tag = "MainCamera"; } catch { /* tag may be reserved */ }

            // Also nudge any other Orientation cameras awake.
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera.Pointer == IntPtr.Zero ||
                    camera == chosen || camera.gameObject == null ||
                    !camera.gameObject.scene.IsValid())
                    continue;
                var sceneName = camera.gameObject.scene.name ?? string.Empty;
                if (!sceneName.StartsWith("Orientation", StringComparison.Ordinal))
                    continue;
                camera.gameObject.SetActive(true);
                camera.enabled = true;
            }

            var main = UnityEngine.Camera.main;
            detail =
                $"activated='{chosen.gameObject.name}' " +
                $"scene='{chosen.gameObject.scene.name}' " +
                $"main={(main == null ? "null" : $"'{main.gameObject.name}'")} " +
                $"live={liveCount}";
            return main != null || chosen.enabled;
        }
        catch (Exception e)
        {
            detail = $"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}";
            return false;
        }
    }

    private static bool TryGetOrientationPlayerPresentationReady(out string state)
    {
        try
        {
            if (!GetLocalPlayerExists())
            {
                state = "no local Player";
                return false;
            }

            var camera = UnityEngine.Camera.main;
            if (camera != null && camera.Pointer != IntPtr.Zero &&
                camera.gameObject != null)
            {
                var mainName = camera.gameObject.name ?? string.Empty;
                var mainScene = camera.gameObject.scene.name ?? string.Empty;
                if (IsJunkOrNonPlayerCamera(mainName, mainScene))
                {
                    state = $"Camera.main is junk '{mainName}'";
                    // Fall through and look for a real player/orientation cam.
                }
                else if (camera.enabled && camera.gameObject.activeInHierarchy)
                {
                    state =
                        $"camera=0x{camera.Pointer.ToInt64():X} enabled=True " +
                        $"active=True name='{mainName}'";
                    return true;
                }
            }

            // Camera.main null or junk: accept any enabled live player/Orientation
            // camera so we do not pin the loading bar at 100% forever.
            var cameras = UnityEngine.Camera.allCameras;
            if (cameras != null)
            {
                for (var i = 0; i < cameras.Length; i++)
                {
                    var candidate = cameras[i];
                    if (candidate == null || !candidate.enabled ||
                        candidate.gameObject == null ||
                        !candidate.gameObject.activeInHierarchy)
                        continue;
                    var sceneName = candidate.gameObject.scene.name ?? string.Empty;
                    var camName = candidate.gameObject.name ?? string.Empty;
                    if (IsJunkOrNonPlayerCamera(camName, sceneName))
                        continue;

                    var isPlayerCam =
                        camName.IndexOf("FluxRec", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        camName.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        camName.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        camName.IndexOf("Desktop", StringComparison.OrdinalIgnoreCase) >= 0;
                    var isOrientation =
                        sceneName.StartsWith("Orientation", StringComparison.Ordinal) ||
                        sceneName.IndexOf("Orientation", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!isPlayerCam && !isOrientation)
                        continue;

                    state =
                        $"orientationCam=0x{candidate.Pointer.ToInt64():X} " +
                        $"name='{camName}' scene='{sceneName}'";
                    return true;
                }
            }

            state = camera == null || camera.Pointer == IntPtr.Zero
                ? "Camera.main=null"
                : $"Camera.main junk or inactive name='{camera.gameObject?.name}'";
            return false;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            state = $"{root.GetType().Name}: {root.Message}";
            return false;
        }
    }

    // The game's own "put the local player in a room" entry point. Unlike the
    // matchmaking path it is designed to work with no backend (it is what the
    // client falls back to when offline - see the "Going to offline dorm did
    // not work!" diagnostic), and crucially it owns the player spawn rather
    // than leaving it to SceneSpawnManager's null static.
    private static unsafe bool BootLocalPlayerToRoom()
    {
        var sessionManager =
            FindNativeComponent("Assembly-CSharp.dll", "SessionManager");
        if (sessionManager == null || sessionManager.Pointer == IntPtr.Zero)
        {
            Plugin.Log.LogError(
                "[BOOT-ROOM] no SessionManager instance; cannot boot the local player");
            return false;
        }

        var reason = IL2CPP.ManagedStringToIl2Cpp(string.Empty);
        var loadSource = (int)ENLDHOFNCPL.BOOT;
        byte force = 1;

        var args = stackalloc void*[3];
        args[0] = (void*)reason;
        args[1] = &loadSource;
        args[2] = &force;

        Plugin.Log.LogWarning(
            "[BOOT-ROOM] calling SessionManager.BootLocalPlayerToDormRoom");
        InvokeNative(
            "SessionManager", "BootLocalPlayerToDormRoom",
            sessionManager.Pointer, args, 3);
        Plugin.Log.LogWarning("[BOOT-ROOM] boot call returned without throwing");
        return true;
    }

    // Reports whether the preconditions for room travel actually hold. Six
    // different entry points have now been called successfully and silently
    // done nothing, which points at missing matchmaking state rather than the
    // wrong entry point.
    private static void LogMatchmakingState()
    {
        var parts = new List<string>();

        void Probe(string name)
        {
            try
            {
                var m = AccessTools.Method(
                    typeof(RecNet.Matchmaking), name, Type.EmptyTypes);
                parts.Add(m == null
                    ? $"{name}=<absent>"
                    : $"{name}={m.Invoke(null, null) ?? "<null>"}");
            }
            catch (Exception e)
            {
                parts.Add($"{name}=<{e.GetBaseException().GetType().Name}>");
            }
        }

        Probe("EBMDEGNBMHE");   // offline-ish flags
        Probe("CPBEDFOMLLP");
        Probe("HJKLBNJEHLP");
        Probe("EGGLHNBPKGI");
        Probe("get_IsInOwnDorm");
        Probe("DKKHGIBIPEN");   // current room-scene descriptor
        Probe("BAHPLBDICBP");   // current room preset
        Probe("MKNENKGBOMD");

        try
        {
            parts.Add($"loginState={(int)RecNet.Matchmaking.DPCOCDCKBDF}");
        }
        catch
        {
            parts.Add("loginState=<unreadable>");
        }

        Plugin.Log.LogWarning($"[MM-STATE] {string.Join(" ", parts)}");
    }

    // The actual offline-room travel call. FALKOHHOCKF - which the plugin
    // replaces wholesale - calls this internally, so it has never once been
    // invoked in this setup. If matchmaking state is sound this is the
    // legitimate "travel to the bundled Orientation room" entry point, and it
    // owns the room load and the player spawn.
    private static bool TravelToOfflineOrientation()
    {
        var orientation = OAILMIHJFAK.JDJEDHFBNGE;
        if (orientation == null)
        {
            Plugin.Log.LogError(
                "[TRAVEL] bundled Orientation preset unavailable");
            return false;
        }

        var travel =
            AccessTools.Method(
                typeof(RecNet.Matchmaking),
                "EHAJFDHHBCF",
                new[] { typeof(OAILMIHJFAK), typeof(KFHPPEDHCNA) });
        if (travel == null)
        {
            Plugin.Log.LogError(
                "[TRAVEL] Matchmaking.EHAJFDHHBCF(offline-room) not found");
            return false;
        }

        Plugin.Log.LogWarning(
            "[TRAVEL] calling Matchmaking.EHAJFDHHBCF for the bundled Orientation room");
        var promise = travel.Invoke(null, new object[] { orientation, null });
        Plugin.Log.LogWarning(
            $"[TRAVEL] returned promise={(promise == null ? "<null>" : "present")}");
        return promise != null;
    }

    private static unsafe void HideLoadingScreen()
    {
        if (!_loadingScreenShown ||
            _loadingScreen == null ||
            _loadingScreen.Pointer == IntPtr.Zero)
            return;

        try
        {
            // Do NOT call set_IsVisible(false) — that stops LoadingScreen.Update
            // on this depot (log freezes while the game still runs). Hide only
            // visual children so Update keeps pumping gameplay ticks.
            var screen = _loadingScreen.TryCast<LoadingScreen>();
            if (screen?.imageTemplate?.template != null)
                screen.imageTemplate.template.SetActive(false);
            if (screen?.imageFade != null)
                screen.imageFade.enabled = false;
            if (screen?.canvas != null)
                screen.canvas.enabled = false;
            MaintainOrientationCursorCapture();
            EnsureLoadingScreenTickHostAlive();
            Plugin.Log.LogInfo(
                "[LOADING] loading visuals hidden; Update kept alive for gameplay");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[LOADING] could not hide the loading screen: " +
                $"{e.GetBaseException().GetType().Name}");
        }
        finally
        {
            _loadingScreenShown = false;
            _orientationUiCursorRequested = false;
            MaintainOrientationCursorCapture();
            EnsureLoadingScreenTickHostAlive();
        }
    }

    private static void MaintainOrientationCursorCapture()
    {
        try
        {
            if (_loadingScreenShown)
            {
                UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                return;
            }

            // No stock watch/menu is functional during the repaired first
            // Orientation segment, so any live-scene cursor request is stale.
            _orientationUiCursorRequested = false;
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
            MaintainOfflineScreenHudCursorHidden();
        }
        catch
        {
            // Cursor capture is best-effort while Unity changes scenes.
        }
    }

    private static void MaintainOfflineScreenHudCursorHidden()
    {
        if (_loadingScreenShown)
            return;

        try
        {
            if (_offlineScreenHudCursors.Count == 0 &&
                (!_offlineScreenHudCursorScanAt.HasValue ||
                 DateTime.UtcNow >= _offlineScreenHudCursorScanAt.Value))
            {
                _offlineScreenHudCursorScanAt = DateTime.UtcNow.AddSeconds(1.0);
                var cursors = UnityEngine.Resources.FindObjectsOfTypeAll<
                    RecRoom.UI.ScreenHUDCursor>();
                if (cursors != null)
                {
                    for (var i = 0; i < cursors.Length; i++)
                    {
                        var cursor = cursors[i];
                        if (cursor == null || cursor.Pointer == IntPtr.Zero ||
                            cursor.gameObject == null)
                            continue;
                        _offlineScreenHudCursors.Add(cursor);
                    }
                }
                if (_offlineScreenHudCursors.Count > 0 &&
                    !_offlineScreenHudCursorHiddenLogged)
                {
                    _offlineScreenHudCursorHiddenLogged = true;
                    Plugin.Log.LogWarning(
                        $"[CURSOR] disabled shipped ScreenHUD cursor objects " +
                        $"count={_offlineScreenHudCursors.Count}; center reticle remains active");
                }
            }

            for (var i = _offlineScreenHudCursors.Count - 1; i >= 0; i--)
            {
                var cursor = _offlineScreenHudCursors[i];
                if (cursor == null || cursor.Pointer == IntPtr.Zero ||
                    cursor.gameObject == null)
                {
                    _offlineScreenHudCursors.RemoveAt(i);
                    continue;
                }
                if (cursor.gameObject.activeSelf)
                    cursor.gameObject.SetActive(false);
            }
        }
        catch
        {
            // Unity/OS cursor lock above remains active even if HUD discovery
            // races a scene load.
        }
    }

    // Kick the local player spawn. SceneSpawnManager exists once the bootstrap
    // scene is up, but its LocalPlayerSpawnState sits at Uninitialized(0)
    // because nothing ever starts the spawn state machine - normally the room
    // pipeline does. CLMOOCHEOHN(CancellationToken) is that state machine: an
    // async method whose body is AsyncTaskMethodBuilder.Start<LPHGNFKIFKG>.
    // Without it there is no player and therefore no camera, which is why
    // Camera.main is null and the view is a default origin camera.
    private static unsafe bool StartLocalPlayerSpawn()
    {
        var spawnManager =
            FindNativeComponent("Assembly-CSharp.dll", "SceneSpawnManager");
        if (spawnManager == null || spawnManager.Pointer == IntPtr.Zero)
        {
            Plugin.Log.LogWarning(
                "[SPAWN] no SceneSpawnManager instance yet; cannot start the spawn");
            return false;
        }

        var klass =
            IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "SceneSpawnManager");
        var method =
            IL2CPP.il2cpp_class_get_method_from_name(klass, "CLMOOCHEOHN", 1);
        if (method == IntPtr.Zero)
        {
            Plugin.Log.LogWarning(
                "[SPAWN] SceneSpawnManager.CLMOOCHEOHN not found");
            return false;
        }

        // A default CancellationToken is a struct wrapping a single null
        // reference, so zeroed storage is a valid "none" token.
        var token = stackalloc IntPtr[2];
        token[0] = IntPtr.Zero;
        token[1] = IntPtr.Zero;

        var args = stackalloc void*[1];
        args[0] = token;

        var exception = IntPtr.Zero;
        IL2CPP.il2cpp_runtime_invoke(method, spawnManager.Pointer, args, ref exception);
        if (exception != IntPtr.Zero)
        {
            Plugin.Log.LogError("[SPAWN] CLMOOCHEOHN threw inside the game");
            return false;
        }

        Plugin.Log.LogWarning("[SPAWN] started the local player spawn state machine");
        return true;
    }

    private static T InvokeStatic<T>(string methodName)
    {
        var method =
            AccessTools.Method(typeof(RecRoomSceneManager), methodName, Type.EmptyTypes);
        if (method == null)
            throw new MissingMethodException(
                typeof(RecRoomSceneManager).FullName,
                methodName);
        var value = method.Invoke(null, null);
        return value is T typed ? typed : default;
    }

    private static bool TryGetOrientationBaseRuntimeReady(out string state)
    {
        try
        {
            var manager = InvokeStatic<RecRoomSceneManager>("get_Instance");
            if (manager == null || manager.Pointer == IntPtr.Zero)
            {
                state = "RecRoomSceneManager.Instance=null";
                return false;
            }

            var spawnGetter =
                AccessTools.Method(
                    typeof(RecRoomSceneManager),
                    "get_SpawnManager",
                    Type.EmptyTypes);
            if (spawnGetter == null)
            {
                state = "get_SpawnManager missing";
                return false;
            }

            var spawnManager = spawnGetter.Invoke(manager, null) as SceneSpawnManager;
            if (spawnManager == null || spawnManager.Pointer == IntPtr.Zero)
            {
                state = $"manager=0x{manager.Pointer.ToInt64():X} spawnManager=null";
                return false;
            }

            var scene = manager.gameObject.scene;
            state =
                $"scene='{scene.name}' manager=0x{manager.Pointer.ToInt64():X} " +
                $"spawnManager=0x{spawnManager.Pointer.ToInt64():X}";
            return true;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            state = $"{root.GetType().Name}: {root.Message}";
            return false;
        }
    }

    private static void PumpSceneDiagnostics()
    {
        if (!Plugin.LogSceneDiagnostics.Value)
            return;
        if (_sceneDiagnosticsNextAt.HasValue &&
            DateTime.UtcNow < _sceneDiagnosticsNextAt.Value)
            return;

        _sceneDiagnosticsNextAt = DateTime.UtcNow.AddSeconds(1);
        try
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var loaded = UnityEngine.SceneManagement.SceneManager.sceneCount;

            // Obfuscation strips the property metadata from these, so the
            // interop only carries the bare get_ methods. Bind them by name.
            var recRoomScene = "<no RecRoomSceneManager>";
            try
            {
                var initialized = InvokeStatic<bool>("get_IsInitialized");
                if (initialized)
                {
                    recRoomScene =
                        $"{InvokeStatic<string>("get_CurrentSceneName")} " +
                        $"(friendly={InvokeStatic<string>("get_CurrentSceneFriendlyName")})";
                }
                else
                {
                    recRoomScene = "<not initialized>";
                }
            }
            catch (Exception e)
            {
                recRoomScene = $"<threw {e.GetBaseException().GetType().Name}>";
            }

            var bootState = "<unavailable>";
            try
            {
                var bootSequence = SingletonMonoBehaviour<BootSequence>.KGGJIHLJBIH;
                if (bootSequence != null)
                {
                    var getState =
                        AccessTools.Method(typeof(BootSequence), "GPDCKOCIJCJ");
                    if (getState != null)
                        bootState = getState.Invoke(bootSequence, null)?.ToString()
                                    ?? "<null>";
                }
            }
            catch (Exception e)
            {
                bootState = $"<threw {e.GetBaseException().GetType().Name}>";
            }

            // HEEMOONFCAF's parameterless bool statics are the RecNet
            // connection's state flags. Which is which is unknown, so print
            // them all and watch which ones flip.
            var recNetFlags = new StringBuilder();
            foreach (var flag in RecNetStateFlagNames)
            {
                try
                {
                    var getter =
                        AccessTools.Method(typeof(HEEMOONFCAF), flag, Type.EmptyTypes);
                    var value = getter == null
                        ? "?"
                        : (getter.Invoke(null, null) as bool? == true ? "1" : "0");
                    recNetFlags.Append(value);
                }
                catch
                {
                    recNetFlags.Append('!');
                }
            }

            var line =
                $"active='{active.name}' loadedScenes={loaded} " +
                $"recRoomScene={recRoomScene} bootState={bootState} " +
                $"readyForSceneChanges={BootSequence.GIFADIBMPBC} " +
                $"recNet[{string.Join(",", RecNetStateFlagNames)}]={recNetFlags}";

            // Only the transitions matter; a per-second repeat of a stalled
            // state buries them.
            if (line == _sceneDiagnosticsLastLine)
                return;
            _sceneDiagnosticsLastLine = line;
            Plugin.Log.LogInfo($"[SCENE] {line}");
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogWarning(
                $"[SCENE] diagnostics failed: {root.GetType().Name}: {root.Message}");
        }
    }

    private static void PumpBootSequenceFallback()
    {
        if (!_bootSequenceFallbackStartedAt.HasValue)
            return;

        var elapsed =
            (DateTime.UtcNow - _bootSequenceFallbackStartedAt.Value).TotalSeconds;
        if (elapsed < 1)
            return;

        int matchmakingState;
        try
        {
            matchmakingState = (int)RecNet.Matchmaking.DPCOCDCKBDF;
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            if (!_localMatchmakingFailureLogged)
            {
                _localMatchmakingFailureLogged = true;
                Plugin.Log.LogError(
                    $"[MATCHMAKING] could not read native login state: " +
                    $"{root.GetType().Name}: {root.Message}");
            }
            return;
        }

        // Matchmaking.EHAJFDHHBCF rejects immediately unless the native state
        // is EXCLUSIVELY_LOGGED_IN (2). In this legacy promise implementation,
        // handlers attached after that synchronous rejection are lost, which
        // leaves BootSequence's LOAD_INITIAL_SCENE promise pending forever.
        if (matchmakingState != 2)
            return;

        var orientation = _bootSequenceFallbackOrientation;
        _bootSequenceFallbackStartedAt = null;
        _bootSequenceFallbackOrientation = false;

        try
        {
            var bootSequence =
                SingletonMonoBehaviour<BootSequence>.KGGJIHLJBIH;
            if (bootSequence == null)
                throw new InvalidOperationException(
                    "BootSequence singleton is not available.");

            // Orientation needs the explicit flag used by the stock
            // TitleScreenFlowModel closure.  For a returning player pass null
            // so BootSequence.KCIBCNLJNAP resolves the normal Dorm target from
            // the existing LinkManager fallback instead of receiving a
            // half-populated matchmaking object.
            var launchTarget = orientation
                ? new BootSequence.DJHPHOBJLHM
                {
                    AGOIDOKPDOH = true,
                    EHNMGLEENPN =
                        new Il2CppSystem.Nullable<ENLDHOFNCPL>(
                            ENLDHOFNCPL.SESSION_TAKEOVER),
                }
                : null;
            Plugin.Log.LogWarning(
                $"[BOOTSTRAP] native exclusive login is ready; " +
                $"preparing {(orientation ? "Orientation" : "Dorm")} through BootSequence");

            // BootSequence.FLAENGPEIEO (POST_LOGIN_RECNET_OPERATIONS) always
            // attaches its continuation to the Photon region-ping promise at
            // BootSequence +0x100. The retired matchmaking continuation used
            // to call this preparation before LaunchGame; our local fallback
            // previously skipped it, so +0x100 stayed null and the native
            // state transition threw before the Orientation loader appeared.
            //
            // Both calls are native and idempotent: StartPhotonPing exits when
            // the promise already exists. Keeping them here restores the stock
            // prerequisite without bypassing the remaining boot states.
            bootSequence.OnLaunchingGameStarted();
            bootSequence.StartPhotonPing();
            _localBootHandoffUntil = DateTime.UtcNow.AddMinutes(3);
            _localBootHandoffOrientation = orientation;
            _calibrationBypassLogged = false;
            _localPostLoginAdvanceDispatched = false;
            _localInitialSceneContinuationDispatched = false;
            Plugin.Log.LogInfo(
                "[BOOTSTRAP] native launch timing and Photon region ping initialized");

            Plugin.Log.LogWarning(
                $"[BOOTSTRAP] launching {(orientation ? "Orientation" : "Dorm")} through BootSequence");
            bootSequence.LaunchGame(launchTarget);
        }
        catch (Exception e)
        {
            var root = e.GetBaseException();
            Plugin.Log.LogError(
                $"[BOOTSTRAP] direct BootSequence handoff failed: " +
                $"{root.GetType().Name}: {root.Message}");
        }
    }

    private static BEFJBNCFADF FindPlayerPreferencesService(
        RRUI.Data.TitleScreenFlowModel model)
    {
        if (model == null || model.Pointer == IntPtr.Zero)
            return null;

        try
        {
            // This exact depot serializes the preference service directly on
            // TitleScreenFlowModel. Looking for TutorialManager.FOFFFLMCNKH
            // through managed reflection can never work in generated IL2CPP
            // interop because that private native instance field is not exposed.
            var preferences = model.FOFFFLMCNKH;
            if (preferences != null)
            {
                if (!_playerPreferencesLocatedLogged)
                {
                    _playerPreferencesLocatedLogged = true;
                    Plugin.Log.LogInfo(
                        "[BOOTSTRAP] located title-flow player-preferences service");
                }
                return preferences;
            }
        }
        catch (Exception e)
        {
            if (!_playerPreferencesLookupFailureLogged)
            {
                _playerPreferencesLookupFailureLogged = true;
                var root = e.GetBaseException();
                Plugin.Log.LogWarning(
                    $"[BOOTSTRAP] title-flow preferences lookup failed: " +
                    $"{root.GetType().Name}: {root.Message}");
            }
        }

        return null;
    }

    private static void BeginLocalRegistration(RRUI.Data.TitleScreenFlowModel model)
    {
        if (_localRegistrationTask != null)
        {
            Plugin.Log.LogInfo("[AUTH] local registration already in progress");
            return;
        }

        var username = model.HMDKPGLBOMK?.Trim() ?? string.Empty;
        var password = model.HMAPPDEEFIC ?? string.Empty;
        var email = model.FCFPJEFJJAP?.Trim() ?? string.Empty;
        if (username.Length < 3 || password.Length < 8)
        {
            model.HNHEGEMFHOI = "Username or password is incomplete.";
            Plugin.Log.LogWarning("[AUTH] local registration rejected incomplete account details");
            return;
        }

        _localRegistrationUsername = username;
        _localRegistrationPassword = password;
        var endpoint = Plugin.ServerHostname.Value.TrimEnd('/') + "/register";
        _localRegistrationTask = RegisterLocalAccountAsync(endpoint, username, password, email);
        Plugin.Log.LogInfo($"[AUTH] local registration started username={username}");
    }

    private static async Task<LocalRegistrationResult> RegisterLocalAccountAsync(
        string endpoint,
        string username,
        string password,
        string email)
    {
        var payload = JsonSerializer.Serialize(new
        {
            username,
            password,
            displayName = username,
            email = string.IsNullOrEmpty(email) ? null : email,
        });

        // Retry aggressively: the local uvicorn process can briefly refuse
        // connections while it is restarting, and that used to abort signup
        // with a permanent "Could not reach the local account server" error.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                // Firebase account creation can legitimately take longer than
                // the old eight-second local-only timeout. Later attempts are
                // safe and idempotent: if an earlier request completed after
                // a client timeout, /register returns 409 and we continue
                // through the normal credential login with the same password.
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(attempt == 0 ? 35 : 20),
                };
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(endpoint, content).ConfigureAwait(false);
                var statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                    return new LocalRegistrationResult(true, false, statusCode, string.Empty);

                // If a previous click or timed-out request completed
                // registration, continue to login. The normal login response
                // still rejects a mismatched password.
                if (statusCode == 409)
                    return new LocalRegistrationResult(true, true, statusCode, string.Empty);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var error = $"Local account server returned HTTP {statusCode}.";
                try
                {
                    using var document = JsonDocument.Parse(body);
                    if (document.RootElement.TryGetProperty("detail", out var detail) &&
                        detail.ValueKind == JsonValueKind.String)
                        error = detail.GetString() ?? error;
                }
                catch
                {
                    // Keep the status-only error; never place a response token in UI/logs.
                }

                // 5xx is often a cold-start race on the private server; retry.
                if (statusCode >= 500 && attempt < 4)
                {
                    Plugin.Log.LogWarning(
                        $"[AUTH] registration HTTP {statusCode}; retry {attempt + 1}/5");
                    await Task.Delay(1000 + attempt * 500).ConfigureAwait(false);
                    continue;
                }

                return new LocalRegistrationResult(false, false, statusCode, error);
            }
            catch (TaskCanceledException) when (attempt < 4)
            {
                Plugin.Log.LogWarning(
                    $"[AUTH] account registration timed out; retry {attempt + 1}/5");
                await Task.Delay(750 + attempt * 250).ConfigureAwait(false);
            }
            catch (Exception e) when (attempt < 4)
            {
                Plugin.Log.LogWarning(
                    $"[AUTH] account registration connect failed " +
                    $"({e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}); " +
                    $"retry {attempt + 1}/5");
                await Task.Delay(1000 + attempt * 500).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return new LocalRegistrationResult(
                    false,
                    false,
                    0,
                    $"Could not reach the local account server: {e.Message}");
            }
        }

        return new LocalRegistrationResult(
            false,
            false,
            0,
            "The local account server did not finish registration in time. " +
            "Make sure Start-LocalRecRoom is running the server on port 8081.");
    }

    private sealed class LocalRegistrationResult
    {
        public LocalRegistrationResult(bool success, bool alreadyExists, int statusCode, string error)
        {
            Success = success;
            AlreadyExists = alreadyExists;
            StatusCode = statusCode;
            Error = error;
        }

        public bool Success { get; }
        public bool AlreadyExists { get; }
        public int StatusCode { get; }
        public string Error { get; }
    }

    public static bool RandomizeAccountCreationUsernamePrefix(RRUI.Data.TitleScreenFlowModel __instance)
    {
        var suffix = Math.Abs(DateTime.UtcNow.Ticks % 1_000_000L);
        __instance.HMDKPGLBOMK = $"player{suffix:000000}";
        Plugin.Log.LogInfo("[AUTH] generated local username");
        return false;
    }

    public static bool RandomizeAccountCreationUsernameButtonPrefix(
        RRUI.Data.TitleScreenFlowModelController.RandomizeAccountCreationUsernameButtonImpl __instance)
    {
        __instance?.Model?.RandomizeAccountCreationUsername();
        return false;
    }

    public static bool SubmitAccountCreationConsolidatedInfoButtonPrefix(
        RRUI.Data.TitleScreenFlowModelController.SubmitAccountCreationConsolidatedInfoAndGoToNextButtonImpl __instance)
    {
        __instance?.Model?.SubmitAccountCreationConsolidatedInfoAndGoToNext();
        return false;
    }

    public static bool AccountCreationUsernameInputChangedPrefix(
        RRUI.Data.TitleScreenFlowModelController.AccountCreationUsernameInputFieldImpl __instance,
        string FPLJCBGEJAP)
    {
        if (__instance?.Model != null)
            __instance.Model.HMDKPGLBOMK = (FPLJCBGEJAP ?? string.Empty).TrimStart('@').Trim();
        return false;
    }

    public static bool AccountCreationPasswordInputChangedPrefix(
        RRUI.Data.TitleScreenFlowModelController.AccountCreationPasswordInputFieldImpl __instance,
        string FPLJCBGEJAP)
    {
        if (__instance?.Model != null)
            __instance.Model.HMAPPDEEFIC = FPLJCBGEJAP ?? string.Empty;
        return false;
    }

    public static bool AccountCreationEmailInputChangedPrefix(
        RRUI.Data.TitleScreenFlowModelController.EmailInputFieldImpl __instance,
        string FPLJCBGEJAP)
    {
        if (__instance?.Model != null)
            __instance.Model.FCFPJEFJJAP = (FPLJCBGEJAP ?? string.Empty).Trim();
        return false;
    }

    public static bool AccountCreationPhoneInputChangedPrefix(
        RRUI.Data.TitleScreenFlowModelController.AccountCreationPhoneInputFieldImpl __instance)
    {
        if (__instance?.Model != null)
            __instance.Model.PIKOMEAJGFD = string.Empty;
        return false;
    }

    public static bool AccountCreationUsernameInputRefreshPrefix(
        RRUI.Data.TitleScreenFlowModelController.AccountCreationUsernameInputFieldImpl __instance)
    {
        var input = __instance?.inputField;
        EnsureWritableInput(input, 20);
        SetInputTextWithoutNotify(input, __instance?.Model?.HMDKPGLBOMK);
        return false;
    }

    public static bool AccountCreationPasswordInputRefreshPrefix(
        RRUI.Data.TitleScreenFlowModelController.AccountCreationPasswordInputFieldImpl __instance)
    {
        var input = __instance?.inputField;
        EnsureWritableInput(input, 64);
        SetInputTextWithoutNotify(input, __instance?.Model?.HMAPPDEEFIC);
        return false;
    }

    public static bool AccountCreationEmailInputRefreshPrefix(
        RRUI.Data.TitleScreenFlowModelController.EmailInputFieldImpl __instance)
    {
        var input = __instance?.inputField;
        EnsureWritableInput(input, 254);
        SetInputTextWithoutNotify(input, __instance?.Model?.FCFPJEFJJAP);
        return false;
    }

    public static bool AccountCreationButtonRefreshPrefix(
        RRUI.Data.ButtonControllerImpl<RRUI.Data.TitleScreenFlowModel> __instance)
    {
        var button = __instance?.button;
        if (button != null)
        {
            button.enabled = true;
            button.interactable = true;
        }
        return false;
    }

    public static bool HideAccountCreationPhonePrefix(
        RRUI.Data.TitleScreenFlowModelController.HideIfNoAccountCreationPhoneImpl __instance)
    {
        if (__instance?.GameObject != null)
            __instance.GameObject.SetActive(false);
        return false;
    }

    private static void EnsureWritableInput(TMP_InputField input, int characterLimit)
    {
        if (input == null)
            return;

        input.enabled = true;
        input.interactable = true;
        input.readOnly = false;
        input.characterLimit = characterLimit;
    }

    private static void SetInputTextWithoutNotify(TMP_InputField input, string value)
    {
        if (input == null)
            return;

        var normalized = value ?? string.Empty;
        if (!string.Equals(input.text, normalized, StringComparison.Ordinal))
            input.SetTextWithoutNotify(normalized);
    }

    public static bool AccountCreationUsernameValidityPrefix(
        RRUI.Data.TitleScreenFlowModel __instance,
        ref FFIGJBPCNHH.ONMLGNMLOJB __result)
    {
        var username = __instance?.HMDKPGLBOMK?.Trim();
        if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 20)
        {
            __result = FFIGJBPCNHH.ONMLGNMLOJB.INVALID_LENGTH;
            return false;
        }

        foreach (var character in username)
        {
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
            {
                __result = FFIGJBPCNHH.ONMLGNMLOJB.INVALID_CHARACTERS;
                return false;
            }
        }

        __result = FFIGJBPCNHH.ONMLGNMLOJB.VALID;
        return false;
    }

    public static bool AccountCreationPasswordValidityPrefix(
        RRUI.Data.TitleScreenFlowModel __instance,
        ref FFIGJBPCNHH.ONMLGNMLOJB __result)
    {
        var password = __instance?.HMAPPDEEFIC;
        if (!string.IsNullOrEmpty(password) &&
            password.Length >= 8 &&
            password.Length <= 64)
        {
            __result = FFIGJBPCNHH.ONMLGNMLOJB.VALID;
            return false;
        }

        __result = FFIGJBPCNHH.ONMLGNMLOJB.INVALID_LENGTH;
        return false;
    }

    public static bool AccountCreationEmailValidityPrefix(
        RRUI.Data.TitleScreenFlowModel __instance,
        ref bool __result)
    {
        var email = __instance?.FCFPJEFJJAP?.Trim();
        __result = string.IsNullOrEmpty(email) || IsUsableEmail(email);
        return false;
    }

    public static bool ShouldHideAccountCreationEmailPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    public static bool ShouldHideAccountCreationPhonePrefix(ref bool __result)
    {
        __result = true;
        return false;
    }

    public static bool AccountCreationConsolidatedInfoIsValidPrefix(
        RRUI.Data.TitleScreenFlowModel __instance,
        ref bool __result)
    {
        var username = __instance?.HMDKPGLBOMK;
        var password = __instance?.HMAPPDEEFIC;
        __result =
            !string.IsNullOrWhiteSpace(username) &&
            username.Length >= 3 &&
            username.Length <= 20 &&
            !string.IsNullOrEmpty(password) &&
            password.Length >= 8 &&
            password.Length <= 64;
        return false;
    }

    private static bool IsUsableEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            return false;

        var at = email.IndexOf('@');
        return at > 0 &&
               at == email.LastIndexOf('@') &&
               at < email.Length - 3 &&
               email.IndexOf('.', at + 2) > at + 1;
    }

    private static DateTime? _accountCreationStartedAt;
    private static DateTime? _manualLoginStartedAt;
    private static Task<LocalRegistrationResult> _localRegistrationTask;
    private static Task<LocalLoginResult> _localCredentialLoginTask;
    private static Task<LocalAccountResult> _localAccountLoadTask;
    private static Task _orientationCompletionSaveTask;
    private static bool _launchCreatedAccountAfterAuth;
    private static string _activeLocalAccessToken = string.Empty;
    private static RRUI.Data.TitleScreenFlowModel _pendingGameLaunchModel;
    private static bool _pendingGameLaunchCreatedAccount;
    private static DateTime? _pendingGameLaunchStartedAt;
    private static bool _playerPreferencesInitializationRequested;
    private static BEFJBNCFADF _playerPreferences;
    private static bool _playerPreferencesLocatedLogged;
    private static bool _playerPreferencesLookupFailureLogged;
    private static bool _playerPreferencesGuardSuppressionLogged;
    private static bool _playerPreferencesReadyLogged;
    private static DateTime? _tutorialCompletionFallbackUntil;
    private static bool _tutorialCompletionFallbackValue;
    private static bool _hasCompletedOrientationFallbackLogged;
    private static bool _dispatchingNativeGameLaunch;
    private static DateTime? _bootSequenceFallbackStartedAt;
    private static bool _bootSequenceFallbackOrientation;
    private static DateTime? _localMatchmakingLoginAttemptedAt;
    private static DateTime? _localMatchmakingExclusiveLoginAttemptedAt;
    private static int _localMatchmakingLoginAttempts;
    private static int _localMatchmakingExclusiveLoginAttempts;
    private static int? _localMatchmakingLastObservedState;
    private static bool _localMatchmakingReadyLogged;
    private static bool _localMatchmakingFailureLogged;
    private static Task<string> _localPlayerSessionTask;
    private static bool _localPlayerSessionCompletionHandled;
    private static FEKGIBNPEAH<RecNet.Matchmaking.NPKOLENFHIH>
        _localOrientationMatchmakingPromise;
    private static Task<string> _localOrientationMatchmakingTask;
    private static DateTime? _localBootHandoffUntil;
    private static bool _localBootHandoffOrientation;
    private static bool _calibrationBypassLogged;
    private static bool _localPostLoginAdvanceDispatched;
    private static bool _localInitialSceneContinuationDispatched;
    private static bool _localPostLoadBypassHandled;
    private static bool _registrationLoginInProgress;
    private static string _localRegistrationUsername = string.Empty;
    private static string _localRegistrationPassword = string.Empty;
    private static bool _stockInitialSceneLoadLogged;
    private static bool _localRoomSceneLoadStarted;
    private static int _roomDependencyContainerAttempts;
    private static bool _roomDependencyContainerReadyLogged;
    private static bool _roomDependencyContainerFailureLogged;
    private static DateTime? _directSceneLoadDueAt;
    private static DateTime? _directSceneLoadArmedAt;
    private static DateTime? _hideLoadingScreenAt;
    private static DateTime? _additiveSceneDueAt;
    private static DateTime? _roomContentReportAt;
    private static bool _playerSpawnStarted;
    private static DateTime? _localPlayerSpawnDueAt;
    private static DateTime? _localPlayerSpawnStartedAt;
    private static int _localPlayerSpawnAttempts;
    private static bool _localPlayerSpawnStarted;
    private static bool _localPlayerSpawnSucceededLogged;
    private static DateTime? _postSpawnRepairUntil;
    private static DateTime? _postSpawnRepairNextAt;
    private static bool _offlineLocomotionReady;
    private static float _offlineCameraPitch;
    private static bool _screenPlayerBoundLogged;
    private static bool _screenPlayerLifecycleWarned;
    private static bool _offlineGameplayRepairActive;
    private static bool _suppressScreenPlayerTicks;
    private static bool _stockScreenPlayerReady;
    private static bool _stockPlayerInitializedStateEntered;
    private static bool _offlineLocalPlayerLifecyclePublished;
    private static bool _offlinePlayerAwakeFailed;
    private static DateTime? _lastSkyForceAt;
    private static DateTime? _avatarApplyUntil;
    private static DateTime? _avatarApplyNextAt;
    private static bool _avatarApplySucceeded;
    private static bool _offlineMoveLogged;
    private static bool _hasLastCursor;
    private static int _lastCursorX;
    private static int _lastCursorY;
    private static bool _envSkyForcedLogged;
    private static bool _avatarApplyFailLogged;
    private static bool _playerAvatarMefTried;
    private static int _avatarApplyStage;
    private static bool _avatarOutfitTried;
    private static HEBLKMJBIBO _pinnedCustomizationAvatar;
    private static IntPtr _pinnedCustomizationAvatarPtr;
    private static int _pinnedCustomizationSelectionCount;
    private static bool _pinnedRestoreLogged;
    private static bool _competingCamsDisabled;
    private static bool _soleCamLogged;
    private static DateTime? _lastCamSoleLogAt;
    private static DateTime? _gameplayHeartbeatAt;
    private static DateTime? _lastMoveLogAt;
    private static float? _spawnFloorY;
    private static UnityEngine.Camera _cachedFluxCamera;
    private static bool _playerFindFailLogged;
    private static bool _freeCamReady;
    private static float _freeCamX, _freeCamY, _freeCamZ, _freeCamYaw;
    private static bool _networkSyncDisabled;
    private static DateTime? _bodyForceAt;
    private static float _walkBobPhase;
    private static bool _fpPresentationReady;
    private static bool _fpPresentationRefreshLogged;
    private static DateTime? _lastFpHeavyAt;
    private static DateTime? _lastFpPresentLogAt;
    private static bool _collisionHitLogged;
    private static bool _groundHitLogged;
    private static RecRoom.Players.Puppet.AnimatedPlayerPuppet _capturedCustomizationPuppet;
    private static IntPtr _capturedCustomizationPuppetPtr;
    private static RecRoom.Players.Puppet.AnimatedPlayerPuppet _liveTitlePuppet;
    private static IntPtr _liveTitlePuppetPtr;
    private static bool _liveTitlePuppetLogged;
    private static int _liveTitlePuppetRendererCount;
    private static UnityEngine.GameObject _officialAvatarHolder;
    private static bool _realAvatarMounted;
    private static bool _realAvatarTrackingBound;
    private static bool _realAvatarTrackingLogged;
    private static bool _realAvatarTrackingFailureLogged;
    private static bool _offlineAvatarAnimationRigReady;
    private static IntPtr _offlineAvatarAnimationPuppetPtr;
    private static UnityEngine.Transform _offlineAvatarBody;
    private static UnityEngine.Transform _offlineAvatarLeftHand;
    private static UnityEngine.Transform _offlineAvatarRightHand;
    private static UnityEngine.Vector3 _offlineAvatarBodyBasePosition;
    private static UnityEngine.Quaternion _offlineAvatarBodyBaseRotation;
    private static UnityEngine.Vector3 _offlineAvatarLeftBasePosition;
    private static UnityEngine.Quaternion _offlineAvatarLeftBaseRotation;
    private static UnityEngine.Vector3 _offlineAvatarRightBasePosition;
    private static UnityEngine.Quaternion _offlineAvatarRightBaseRotation;
    private static float _offlineAvatarMoveAmount;
    private static float _offlineAvatarAnimationDt = 1f / 60f;
    private static bool _legacyPlayerVisualsDisabledLogged;
    private static bool _realAvatarCameraRigLogged;
    private static bool _mountFailLogged;
    private static DateTime? _lastRealMountAttemptAt;
    private static bool _localPlayerSpawnTimeoutLogged;
    private static bool _localPlayerPresentationWaitLogged;
    private static DateTime? _localPlayerPresentationWaitSince;
    private static bool _offlineCameraRecoveryAttempted;
    private static DateTime? _orientationScenesReadyAt;
    private static bool _forceOrientationEnterDone;
    private static bool _offlineOrientationRoomKeysLogged;
    private static bool _offlinePhotonRoomJoinStarted;
    private static int _offlinePhotonRoomJoinAttempts;
    private static DateTime? _offlinePhotonRoomLastJoinAt;
    private static bool _offlinePhotonRoomReadyLogged;
    private static bool _offlineSettingsServiceReadyLogged;
    private static bool _offlineSettingsServiceFailureLogged;
    private static bool _offlineSettingDefaultsInitialized;
    private static bool _offlineObjectModelRootInstalled;
    private static bool _offlineObjectModelReadyLogged;
    private static bool _offlineObjectModelFailureLogged;
    private static bool _offlineUnitySceneServiceReadyLogged;
    private static bool _offlineUnitySceneServiceFailureLogged;
    private static GHOBGEJJNGE _offlineObjectModelContainer;
    private static bool _offlinePlayerRegistryReadyLogged;
    private static bool _offlinePlayerAwakeDiagnosticsLogged;
    private static bool _offlinePlayerAvatarInitializationInProgress;
    private static bool _offlinePlayerAvatarInitializedEarly;
    private static bool _offlinePlayerAvatarReadyLogged;
    private static bool _offlineToolEquipSlotsInitializationInProgress;
    private static bool _offlineToolEquipSlotsInitializedEarly;
    private static bool _offlineToolEquipSlotsReadyLogged;
    private static bool _lateMainRootLoadRequested;
    private static bool _offlineCoreGameplayWaitLogged;
    private static bool _offlineCoreGameplayReadyLogged;
    private static bool _offlineAudioManagerInitializationAttempted;
    private static bool _offlineAudioManagerInitializedLogged;
    private static bool _offlineAudioManagerInitializationFailureLogged;
    private static Il2CppSystem.Object _offlineMicSpamMonitorEvent;
    private static bool _offlineInstantiationHandoffCompleted;
    private static UnityEngine.Rendering.Universal.RecRoomQualityConfig _offlineUrpQualityConfig;
    private static bool _offlineUrpQualityConfigReadyLogged;
    private static bool _offlineUrpDirectDefaultsLogged;
    private static bool _offlineJoinedRoomLabelLogged;
    private static bool _desktopCullingFallbackLogged;
    private static bool _avatarLodBypassLogged;
    private static string _localPlayerSpawnLastState = string.Empty;
    private static DateTime? _localPlayerSpawnStateSince;
    private static bool _bigDataGateReleased;
    private static bool _bigDataRetrievalShortCircuitLogged;
    private static IntPtr _localPlayerSpawnTimer;
    private static IntPtr _localPlayerSpawnTask;
    private static int _orientationContentLoadAttempts;
    private static bool _orientationBaseReadyLogged;
    private static bool _orientationBaseWaitLogged;
    private static UnityEngine.Object _loadingScreen;
    private static UnityEngine.Texture2D _embeddedOrientationLoadingTexture;
    private static bool _loadingScreenTemplateBound;
    private static bool _loadingScreenImageFadeStateCaptured;
    private static bool _loadingScreenImageFadeWasEnabled;
    private static bool _loadingScreenShown;
    private static bool _orientationUiCursorRequested;
    private static readonly List<RecRoom.UI.ScreenHUDCursor>
        _offlineScreenHudCursors = new();
    private static DateTime? _offlineScreenHudCursorScanAt;
    private static bool _offlineScreenHudCursorHiddenLogged;
    private static readonly string[] _offlineOrientationSceneOrder =
    {
        "Orientation_Scene1",
        "Orientation_Scene2",
        "Orientation_Scene3",
        "Orientation_PracticeGym",
        "Orientation_Scene5",
        "Orientation_Rewards",
    };
    private static readonly HashSet<string> _initializedOrientationScenes =
        new(StringComparer.Ordinal);
    private static string _orientationContentScene = string.Empty;
    private static DateTime? _orientationContentEnteredAt;
    private static RecRoom.Activities.Orientation.OrientationIntroduction
        _stockOrientationIntroduction;
    private static IntPtr _stockOrientationIntroductionPtr;
    private static int _stockOrientationIntroInitAttempts;
    private static bool _stockOrientationIntroUpdateErrorLogged;
    private static bool _orientationStockFlowErrorLogged;
    private static RecRoom.Core.Encounters.GameEncounter
        _orientationIntroEncounterLevelVo;
    private static RecRoom.Core.Encounters.GameEncounter
        _orientationIntroEncounterWalk;
    private static RecRoom.Core.Encounters.GameEncounter
        _orientationIntroEncounterHands;
    private static RecRoom.Core.Encounters.GameEncounter
        _orientationIntroEncounterLook;
    private static bool _orientationIntroLevelVoActivated;
    private static bool _orientationIntroWalkActivated;
    private static bool _orientationIntroHandsActivated;
    private static bool _orientationIntroLookActivated;
    private static LockableDoor _orientationTargetDoor;
    private static IntPtr _orientationTargetDoorPtr;
    private static LockableDoor _orientationNearbyDoor;
    private static IntPtr _orientationNearbyDoorPtr;
    private static DateTime? _orientationDoorNextScanAt;
    private static readonly List<LockableDoor> _orientationSceneDoors = new();
    private static string _orientationDoorScanLoggedScene = string.Empty;
    private static UnityEngine.Bounds _orientationDoorVisualBounds;
    private static bool _orientationDoorVisualBoundsValid;
    private static string _orientationDoorVisualName = string.Empty;
    private static UnityEngine.GameObject _orientationDoorVisualRoot;
    private static IntPtr _orientationDoorVisualRootPtr;
    private static UnityEngine.Collider _orientationDoorVisualCollider;
    private static UnityEngine.Animator _orientationDoorAnimator;
    private static bool _orientationDoorHighlightApplied;
    private static bool _orientationDoorArmedLogged;
    private static DateTime? _orientationDoorVisualProbeNextAt;
    private static bool _validationOrientationDoorPositioned;
    private static bool _validationOrientationDoorPressed;
    private static DateTime? _validationOrientationDoorPressAt;
    private static Il2CppStructArray<UnityEngine.RaycastHit>
        _orientationDoorRayHitBuffer;
    private static bool _orientationDoorPromptVisible;
    private static bool _leftMouseWasDown;
    private static UnityEngine.Renderer[] _orientationHighlightedRenderers;
    private static UnityEngine.MaterialPropertyBlock _orientationHighlightBlock;
    private static UnityEngine.LineRenderer _orientationDoorOutline;
    private static bool _orientationSceneTransitionInProgress;
    private static string _orientationPortalSourceScene = string.Empty;
    private static string _orientationPortalTargetScene = string.Empty;
    private static DateTime? _orientationPortalUsePendingAt;
    private static UnityEngine.AsyncOperation _orientationSceneLoadOperation;
    private static bool _orientationWatchUnlockAttempted;
    private static bool _loadingScreenMissingLogged;
    private static string _loadingScreenLastState = string.Empty;
    private static DateTime? _loadingScreenActivatedAt;
    private static bool _offlineTravelAttempted;
    private static bool _bootLocalPlayerAttempted;
    private static bool _preferenceFallbackLogged;
    private static bool _preferenceKeyFallbackLogged;
    private static int _responseConversionLogCount;
    private static bool _serviceMapLogged;
    private static bool _nameserverValidationLogged;
    private static bool _nameserverSuccessLogged;
    private static bool _serviceMapInstalled;
    private static bool _nameserverFailureLogged;

    private static readonly string[] RecNetStateFlagNames =
    {
        "BOLGMJODBNG", "NEOMAJDFALL", "PCEDPIHLODJ", "BNEHMMICCFI", "AGJLBHJBJID",
        "PJNBIJGAEDA", "EHDNHNNFPFK", "DACLFAOCJLG", "GODDMODOBAI",
    };
    private static DateTime? _sceneDiagnosticsNextAt;
    private static string _sceneDiagnosticsLastLine = string.Empty;
    private static Il2CppStructArray<UnityEngine.RaycastHit>
        _offlineGroundRayHitBuffer;
    private static DateTime? _offlineGroundProbeNextAt;
    private static DateTime? _validationProbeAt;
    private static int _validationProbeStage = -1;
    private static string _validationUsername = string.Empty;
    private static bool _validationAccountLaunchEnabled;
    private static bool _autoWelcomeLaunchPressed;
    private static DateTime? _avatarPreviewRefreshAt;
    private static bool _avatarPreviewRefreshed;
    private static int _avatarPreviewRefreshAttempts;
    private static bool _nativeAvatarReferenceRebound;
    private static bool _avatarCatalogLogged;
    private static int _registeredOutfitManagerInstanceId;
    private static int _registeredBrowsableItemCount;
    private static RRUI.Data.TitleScreenFlowModel.TitleScreenPage _originalAvatarPage;

    private static void PreserveCustomizationForOrientation(string reason)
    {
        try
        {
            EnsureLocalAvatarModel();

            // The normal account-creation path remembers this from the model's
            // lifecycle callback. Cached-login and accelerated flows can skip
            // that callback, so perform one bounded, strongly typed lookup while
            // TitleScreen is still alive (never during the gameplay tick).
            var rememberedAlive = false;
            try
            {
                rememberedAlive = _liveTitlePuppet != null &&
                                  _liveTitlePuppet.Pointer != IntPtr.Zero &&
                                  _liveTitlePuppet.gameObject != null;
            }
            catch { rememberedAlive = false; }

            if (!rememberedAlive)
            {
                var models = UnityEngine.Resources
                    .FindObjectsOfTypeAll<RRUI.Data.AnimatedPlayerPuppetAvatarModel>();
                if (models != null)
                {
                    for (var i = 0; i < models.Length; i++)
                    {
                        var model = models[i];
                        if (model?.NCKMNOBLALG == null ||
                            model.NCKMNOBLALG.Pointer == IntPtr.Zero ||
                            model.gameObject == null)
                            continue;
                        RememberLiveTitlePuppet(
                            model.NCKMNOBLALG,
                            reason + "-boundary-scan");
                        break;
                    }
                }
            }

            PinLocalCustomizationAvatar(reason);
            PromoteRememberedTitlePuppetToPersistent(reason);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[AVATAR] launch-boundary preservation failed ({reason}): " +
                e.GetBaseException().Message);
        }
    }

    private static void PinLocalCustomizationAvatar(string reason)
    {
        try
        {
            var avatar = HEBLKMJBIBO.IJEMMGDMKPE;
            if (avatar == null || avatar.Pointer == IntPtr.Zero)
            {
                Plugin.Log.LogWarning(
                    $"[AVATAR] pin skipped ({reason}): no IJEMMGDMKPE model");
                return;
            }

            _pinnedCustomizationAvatar = avatar;
            _pinnedCustomizationAvatarPtr = avatar.Pointer;
            _pinnedCustomizationSelectionCount = avatar.FMGNNCFFGLB?.Count ?? 0;
            Plugin.Log.LogWarning(
                $"[AVATAR] pinned customization model ({reason}) " +
                $"ptr=0x{avatar.Pointer.ToInt64():X} " +
                $"selections={_pinnedCustomizationSelectionCount}");

            // Only promote the official puppet when the user finishes customization
            // (AvatarSubmit*). Promoting during title-lifecycle detaches it from
            // the UI early and the scene unload then destroys it.
            if (reason != null &&
                reason.IndexOf("AvatarSubmit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PromoteRememberedTitlePuppetToPersistent(reason);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[AVATAR] pin failed ({reason}): " + e.GetBaseException().Message);
        }
    }

    /// <summary>
    /// Store a direct reference to the official title-screen
    /// [PlayerPuppet]AnimatedVariant while the title UI is still alive.
    /// </summary>
    private static void RememberLiveTitlePuppet(
        RecRoom.Players.Puppet.AnimatedPlayerPuppet puppet,
        string reason)
    {
        if (puppet == null || puppet.Pointer == IntPtr.Zero || puppet.gameObject == null)
            return;

        _liveTitlePuppet = puppet;
        _liveTitlePuppetPtr = puppet.Pointer;

        int rendererCount = 0;
        try
        {
            var display = puppet.playerAvatarDisplay;
            var rs = display != null
                ? display.GetComponentsInChildren<UnityEngine.Renderer>(true)
                : puppet.GetComponentsInChildren<UnityEngine.Renderer>(true);
            rendererCount = rs?.Length ?? 0;
        }
        catch { /* ignore */ }

        if (!_liveTitlePuppetLogged || rendererCount > _liveTitlePuppetRendererCount)
        {
            _liveTitlePuppetLogged = true;
            _liveTitlePuppetRendererCount = rendererCount;
            Plugin.Log.LogWarning(
                $"[AVATAR] remembered OFFICIAL title puppet ({reason}) " +
                $"name='{puppet.gameObject.name}' ptr=0x{puppet.Pointer.ToInt64():X} " +
                $"renderers={rendererCount}");
        }
    }

    /// <summary>
    /// Detach the official title customization puppet into a permanent holder
    /// so TitleScreen unload cannot destroy it. Same stock
    /// [PlayerPuppet]AnimatedVariant the customize page already built.
    /// </summary>
    private static void PromoteRememberedTitlePuppetToPersistent(string reason)
    {
        try
        {
            // Reuse the detached clone on repeated launch-pipeline callbacks.
            var existing = ResolveOfficialPuppet();
            if (existing != null && existing.Pointer != IntPtr.Zero)
            {
                var currentAvatar = HEBLKMJBIBO.IJEMMGDMKPE;
                if (currentAvatar != null)
                    existing.SetAvatarVisuals(currentAvatar, -1, true);
                return;
            }

            var puppet = _liveTitlePuppet;
            if (puppet == null || puppet.Pointer == IntPtr.Zero)
            {
                Plugin.Log.LogWarning(
                    $"[AVATAR] no remembered official title puppet to promote ({reason})");
                return;
            }

            UnityEngine.GameObject puppetGo = null;
            try { puppetGo = puppet.gameObject; }
            catch { puppetGo = null; }
            if (puppetGo == null)
            {
                Plugin.Log.LogWarning(
                    $"[AVATAR] remembered title puppet GameObject is dead ({reason})");
                return;
            }

            var avatar = HEBLKMJBIBO.IJEMMGDMKPE;

            // Title-screen model teardown explicitly destroys its own puppet;
            // merely reparenting that object to a DDOL holder does not survive
            // the model's OnDestroy. Clone the real stock puppet (including its
            // wardrobe renderers and Animator) and leave the UI-owned original
            // untouched until TitleScreen exits.
            var persistentGo = UnityEngine.Object.Instantiate(puppetGo);
            if (persistentGo == null)
                throw new InvalidOperationException(
                    "The official customization puppet could not be cloned.");
            persistentGo.name = "FluxRec_OfficialCustomizationPuppet";
            var persistentPuppet = persistentGo
                .GetComponent<RecRoom.Players.Puppet.AnimatedPlayerPuppet>();
            if (persistentPuppet == null)
            {
                persistentPuppet = persistentGo
                    .GetComponentInChildren<RecRoom.Players.Puppet.AnimatedPlayerPuppet>(true);
            }
            if (persistentPuppet == null || persistentPuppet.Pointer == IntPtr.Zero)
                throw new InvalidOperationException(
                    "The cloned customization object has no AnimatedPlayerPuppet.");

            try
            {
                if (avatar != null)
                    persistentPuppet.SetAvatarVisuals(avatar, -1, true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    "[AVATAR] promote SetAvatarVisuals: " +
                    e.GetBaseException().Message);
            }

            // Permanent holder owns the independent clone across TitleScreen
            // teardown. Keep it inactive until the Orientation player exists.
            try
            {
                if (_officialAvatarHolder == null)
                {
                    _officialAvatarHolder =
                        new UnityEngine.GameObject("FluxRec_OfficialCustomizationAvatar");
                    UnityEngine.Object.DontDestroyOnLoad(_officialAvatarHolder);
                }

                persistentGo.SetActive(true);
                persistentPuppet.transform.SetParent(
                    _officialAvatarHolder.transform, false);
                persistentPuppet.transform.localPosition = UnityEngine.Vector3.zero;
                persistentPuppet.transform.localRotation = UnityEngine.Quaternion.identity;
                persistentPuppet.transform.localScale = UnityEngine.Vector3.one;
                _officialAvatarHolder.SetActive(false);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    "[AVATAR] promote holder: " + e.GetBaseException().Message);
                try
                {
                    persistentGo.transform.SetParent(null, true);
                    UnityEngine.Object.DontDestroyOnLoad(persistentGo);
                    persistentGo.SetActive(false);
                }
                catch { /* ignore */ }
            }

            _capturedCustomizationPuppet = persistentPuppet;
            _capturedCustomizationPuppetPtr = persistentPuppet.Pointer;
            _mountFailLogged = false;
            _realAvatarMounted = false;
            _realAvatarTrackingBound = false;
            _realAvatarTrackingLogged = false;
            _realAvatarTrackingFailureLogged = false;

            int rendererCount = 0;
            try
            {
                var display = persistentPuppet.playerAvatarDisplay;
                var rs = display != null
                    ? display.GetComponentsInChildren<UnityEngine.Renderer>(true)
                    : persistentPuppet.GetComponentsInChildren<UnityEngine.Renderer>(true);
                rendererCount = rs?.Length ?? 0;
                // Force every stock wardrobe mesh visible now.
                if (rs != null)
                {
                    for (var i = 0; i < rs.Length; i++)
                    {
                        if (rs[i] == null) continue;
                        rs[i].gameObject.SetActive(true);
                        rs[i].enabled = true;
                    }
                }
            }
            catch { /* ignore */ }

            Plugin.Log.LogWarning(
                $"[AVATAR] PROMOTED official customization puppet ({reason}) " +
                $"name='{persistentGo.name}' holder=FluxRec_OfficialCustomizationAvatar " +
                $"renderers={rendererCount} " +
                $"selections={avatar?.FMGNNCFFGLB?.Count ?? 0}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[AVATAR] promote failed ({reason}): " +
                e.GetBaseException().Message);
        }
    }

    private static RecRoom.Players.Puppet.AnimatedPlayerPuppet ResolveOfficialPuppet()
    {
        // Prefer stored reference if still alive.
        try
        {
            if (_capturedCustomizationPuppet != null &&
                _capturedCustomizationPuppet.Pointer != IntPtr.Zero &&
                _capturedCustomizationPuppet.Pointer == _capturedCustomizationPuppetPtr)
            {
                var go = _capturedCustomizationPuppet.gameObject;
                if (go != null)
                    return _capturedCustomizationPuppet;
            }
        }
        catch { /* dead IL2CPP wrapper */ }

        // Recover from permanent holder.
        try
        {
            if (_officialAvatarHolder == null)
            {
                var found = UnityEngine.GameObject.Find(
                    "FluxRec_OfficialCustomizationAvatar");
                if (found != null)
                    _officialAvatarHolder = found;
            }

            if (_officialAvatarHolder != null)
            {
                var puppet = _officialAvatarHolder
                    .GetComponentInChildren<RecRoom.Players.Puppet.AnimatedPlayerPuppet>(true);
                if (puppet != null && puppet.Pointer != IntPtr.Zero)
                {
                    _capturedCustomizationPuppet = puppet;
                    _capturedCustomizationPuppetPtr = puppet.Pointer;
                    Plugin.Log.LogWarning(
                        $"[AVATAR] recovered official puppet from holder " +
                        $"name='{puppet.gameObject.name}'");
                    return puppet;
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[AVATAR] resolve holder: " + e.GetBaseException().Message);
        }

        return null;
    }

    /// <summary>
    /// Places the captured real customization puppet on the Orientation player
    /// and drives stock SetAvatarVisuals — no fake geometry.
    /// </summary>
    private static void MountRealCustomizationAvatarOnPlayer(Player player)
    {
        if (player == null)
            return;

        try
        {
            RestorePinnedCustomizationAvatar();
            var model = HEBLKMJBIBO.IJEMMGDMKPE;
            if (model == null)
            {
                Plugin.Log.LogWarning("[AVATAR] mount: no HEBLKMJBIBO model");
                return;
            }

            // Official title puppet only — never invent geometry.
            var puppet = ResolveOfficialPuppet();
            if (puppet == null)
            {
                // Last chance: promote remembered title reference if still valid.
                PromoteRememberedTitlePuppetToPersistent("mount");
                puppet = ResolveOfficialPuppet();
            }

            if (puppet == null)
            {
                if (!_mountFailLogged)
                {
                    _mountFailLogged = true;
                    Plugin.Log.LogWarning(
                        "[AVATAR] mount: official title puppet missing " +
                        "(must complete customize page so it can be promoted)");
                }
                return;
            }

            // Stock apply path used by title customization (public, works offline).
            try
            {
                puppet.SetAvatarVisuals(model, -1, true);
                Plugin.Log.LogWarning(
                    $"[AVATAR] mount SetAvatarVisuals OK " +
                    $"selections={model.FMGNNCFFGLB?.Count ?? 0}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(
                    "[AVATAR] mount SetAvatarVisuals: " +
                    e.GetBaseException().Message);
            }

            // Bind puppet to the local Orientation player (stock API).
            try
            {
                var models = UnityEngine.Resources
                    .FindObjectsOfTypeAll<RRUI.Data.AnimatedPlayerPuppetAvatarModel>();
                if (models != null)
                {
                    for (var i = 0; i < models.Length; i++)
                    {
                        var m = models[i];
                        if (m == null)
                            continue;
                        if (m.NCKMNOBLALG != null &&
                            m.NCKMNOBLALG.Pointer == puppet.Pointer)
                        {
                            m.SetAvatarReference(model);
                            try { m.TrackPlayer(player); } catch { /* optional */ }
                            try { m.DNOLMGGJKDA(); } catch { /* optional */ }
                            Plugin.Log.LogWarning(
                                "[AVATAR] mount TrackPlayer + DNOLMGGJKDA on title model");
                            break;
                        }
                    }
                }
            }
            catch { /* ignore */ }

            // Mount the stock customization puppet on the live player's world
            // root while leaving its AnimatedPlayerPuppet provider in charge
            // of the original idle/fidget/face animation system.
            BindOfficialPuppetToPlayer(player, puppet, refresh: true);

            // Enable every renderer on the REAL display (wardrobe meshes).
            var enabled = 0;
            try
            {
                var display = puppet.playerAvatarDisplay;
                var root = display != null ? display.gameObject : puppet.gameObject;
                root.SetActive(true);
                var rs = root.GetComponentsInChildren<UnityEngine.Renderer>(true);
                if (rs != null)
                {
                    for (var i = 0; i < rs.Length; i++)
                    {
                        if (rs[i] == null)
                            continue;
                        rs[i].gameObject.SetActive(true);
                        rs[i].enabled = true;
                        try
                        {
                            rs[i].shadowCastingMode =
                                UnityEngine.Rendering.ShadowCastingMode.On;
                            rs[i].receiveShadows = true;
                        }
                        catch { /* ignore */ }
                        enabled++;
                    }
                }

                var ans = root.GetComponentsInChildren<UnityEngine.Animator>(true);
                if (ans != null)
                {
                    for (var i = 0; i < ans.Length; i++)
                    {
                        if (ans[i] == null)
                            continue;
                        ans[i].enabled = true;
                        ans[i].cullingMode =
                            UnityEngine.AnimatorCullingMode.AlwaysAnimate;
                        if (ans[i].speed < 0.05f)
                            ans[i].speed = 1f;
                    }
                }
            }
            catch { /* ignore */ }

            _capturedCustomizationPuppet = puppet;
            _capturedCustomizationPuppetPtr = puppet.Pointer;
            _realAvatarMounted = true;
            ConfigureRealAvatarFirstPerson(player, puppet);
            Plugin.Log.LogWarning(
                $"[AVATAR] REAL customization avatar mounted on Orientation " +
                $"puppet='{puppet.gameObject.name}' renderers={enabled} " +
                $"selections={model.FMGNNCFFGLB?.Count ?? 0}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                "[AVATAR] mount failed: " + e.GetBaseException().Message);
        }
    }

    private static void TickMountedAvatarFollow()
    {
        if (!_realAvatarMounted ||
            _capturedCustomizationPuppet == null ||
            _capturedCustomizationPuppetPtr == IntPtr.Zero ||
            _capturedCustomizationPuppet.Pointer != _capturedCustomizationPuppetPtr)
            return;

        if (!TryGetCachedOrientationPlayer(out var player))
            return;

        try
        {
            BindOfficialPuppetToPlayer(
                player,
                _capturedCustomizationPuppet,
                refresh: !_realAvatarTrackingBound);
        }
        catch
        {
            _realAvatarMounted = false;
            _realAvatarTrackingBound = false;
        }
    }

    /// <summary>
    /// Moves the preserved stock customization puppet with the live Player while
    /// keeping it under a DontDestroyOnLoad holder. AnimatedPlayerPuppet uses an
    /// animation-data provider and deliberately inherits PlayerPuppet's throwing
    /// TargetPlayer setter in this depot; only LivePlayerPuppet supports that
    /// property. Its own Update/LateUpdate therefore continues to own the real
    /// head, body, hands, face and fidget animations while this method owns only
    /// the world-space player root.
    /// </summary>
    private static void BindOfficialPuppetToPlayer(
        Player player,
        RecRoom.Players.Puppet.AnimatedPlayerPuppet puppet,
        bool refresh)
    {
        if (player == null || puppet == null)
            return;

        try
        {
            if (_officialAvatarHolder == null)
            {
                _officialAvatarHolder =
                    new UnityEngine.GameObject("FluxRec_OfficialCustomizationAvatar");
                UnityEngine.Object.DontDestroyOnLoad(_officialAvatarHolder);
            }

            var holderTransform = _officialAvatarHolder.transform;
            var playerTransform = player.transform;
            holderTransform.position = playerTransform.position;
            holderTransform.rotation = playerTransform.rotation;
            holderTransform.localScale = UnityEngine.Vector3.one;
            _officialAvatarHolder.SetActive(true);

            var puppetTransform = puppet.transform;
            if (puppetTransform.parent == null ||
                puppetTransform.parent.Pointer != holderTransform.Pointer)
            {
                puppetTransform.SetParent(holderTransform, false);
            }
            if (!_realAvatarTrackingBound)
            {
                puppetTransform.localPosition = UnityEngine.Vector3.zero;
                puppetTransform.localRotation = UnityEngine.Quaternion.identity;
                puppetTransform.localScale = UnityEngine.Vector3.one;
            }

            puppet.gameObject.SetActive(true);
            puppet.enabled = true;

            if (!_realAvatarTrackingBound)
            {
                if (refresh)
                {
                    try
                    {
                        // Refresh reads AnimatedPlayerPuppet's own provider and
                        // rebuilds its stock facial/hand animation state. It does
                        // not require (or permit) assigning TargetPlayer.
                        puppet.Refresh();
                    }
                    catch (Exception refreshError)
                    {
                        Plugin.Log.LogWarning(
                            "[AVATAR] official animated-puppet refresh was skipped: " +
                            refreshError.GetBaseException().Message);
                    }
                }
                _realAvatarTrackingBound = true;
            }

            if (!_realAvatarTrackingLogged)
            {
                _realAvatarTrackingLogged = true;
                var animatorCount = puppet.gameObject
                    .GetComponentsInChildren<UnityEngine.Animator>(true)?.Length ?? 0;
                Plugin.Log.LogWarning(
                    $"[AVATAR] official animated customization root bound " +
                    $"player='{player.gameObject.name}' animators={animatorCount} " +
                    $"world={holderTransform.position}");
            }
        }
        catch (Exception e)
        {
            _realAvatarTrackingBound = false;
            if (!_realAvatarTrackingFailureLogged)
            {
                _realAvatarTrackingFailureLogged = true;
                Plugin.Log.LogWarning(
                    "[AVATAR] official customization root bind failed: " +
                    e.GetBaseException().Message);
            }
        }
    }

    /// <summary>
    /// The preserved title puppet keeps Rec Room's genuine meshes, wardrobe,
    /// face and animator. Its network animation-data provider is absent in the
    /// offline room, though, so apply a small render-time locomotion layer to
    /// the genuine body and hands instead of falling back to fabricated arms.
    /// </summary>
    private static void ApplyOfflineAvatarLocomotionAnimation(
        RecRoom.Players.Puppet.AnimatedPlayerPuppet puppet,
        float moveAmount,
        float dt)
    {
        try
        {
            if (puppet == null || puppet.Pointer == IntPtr.Zero)
                return;

            var body = puppet.KKMHDMGBKAM;
            var leftHand = puppet.DJMKFFPEAHO?.transform;
            var rightHand = puppet.DFALAJEAEFK?.transform;
            if (body == null || leftHand == null || rightHand == null)
                return;

            if (_offlineAvatarAnimationPuppetPtr != puppet.Pointer ||
                !_offlineAvatarAnimationRigReady)
            {
                _offlineAvatarAnimationPuppetPtr = puppet.Pointer;
                _offlineAvatarAnimationRigReady = true;
                _offlineAvatarBody = body;
                _offlineAvatarLeftHand = leftHand;
                _offlineAvatarRightHand = rightHand;
                _offlineAvatarBodyBasePosition = body.localPosition;
                _offlineAvatarBodyBaseRotation = body.localRotation;
                _offlineAvatarLeftBasePosition = leftHand.localPosition;
                _offlineAvatarLeftBaseRotation = leftHand.localRotation;
                _offlineAvatarRightBasePosition = rightHand.localPosition;
                _offlineAvatarRightBaseRotation = rightHand.localRotation;
                Plugin.Log.LogWarning(
                    "[AVATAR] genuine puppet offline locomotion layer ready");
            }

            if (_offlineAvatarBody == null || _offlineAvatarLeftHand == null ||
                _offlineAvatarRightHand == null)
                return;

            var amount = Math.Clamp(moveAmount, 0f, 1f);
            var phase = _walkBobPhase;
            var stride = (float)Math.Sin(phase * 2.0f);
            var opposite = (float)Math.Sin(phase * 2.0f + Math.PI);
            var bob = Math.Abs((float)Math.Sin(phase * 2.0f)) * 0.018f * amount;
            var idle = (float)Math.Sin(UnityEngine.Time.unscaledTime * 1.65f) *
                       (1f - amount);
            var blend = Math.Clamp(dt * 12f, 0.08f, 1f);

            var bodyPosition = _offlineAvatarBodyBasePosition +
                               UnityEngine.Vector3.up * bob;
            var bodyRotation = _offlineAvatarBodyBaseRotation *
                               UnityEngine.Quaternion.Euler(
                                   idle * 0.45f,
                                   stride * 1.4f * amount,
                                   -stride * 2.2f * amount);
            _offlineAvatarBody.localPosition = UnityEngine.Vector3.Lerp(
                _offlineAvatarBody.localPosition, bodyPosition, blend);
            _offlineAvatarBody.localRotation = UnityEngine.Quaternion.Slerp(
                _offlineAvatarBody.localRotation, bodyRotation, blend);

            var leftPosition = _offlineAvatarLeftBasePosition +
                new UnityEngine.Vector3(0f, stride * 0.025f, stride * 0.06f) * amount;
            var rightPosition = _offlineAvatarRightBasePosition +
                new UnityEngine.Vector3(0f, opposite * 0.025f, opposite * 0.06f) * amount;
            var leftRotation = _offlineAvatarLeftBaseRotation *
                UnityEngine.Quaternion.Euler(stride * 18f * amount, 0f, -idle * 1.4f);
            var rightRotation = _offlineAvatarRightBaseRotation *
                UnityEngine.Quaternion.Euler(opposite * 18f * amount, 0f, idle * 1.4f);

            _offlineAvatarLeftHand.localPosition = UnityEngine.Vector3.Lerp(
                _offlineAvatarLeftHand.localPosition, leftPosition, blend);
            _offlineAvatarRightHand.localPosition = UnityEngine.Vector3.Lerp(
                _offlineAvatarRightHand.localPosition, rightPosition, blend);
            _offlineAvatarLeftHand.localRotation = UnityEngine.Quaternion.Slerp(
                _offlineAvatarLeftHand.localRotation, leftRotation, blend);
            _offlineAvatarRightHand.localRotation = UnityEngine.Quaternion.Slerp(
                _offlineAvatarRightHand.localRotation, rightRotation, blend);
        }
        catch { /* stock animator remains enabled as the primary animation path */ }
    }

    private static void RestorePinnedCustomizationAvatar()
    {
        try
        {
            if (_pinnedCustomizationAvatarPtr == IntPtr.Zero)
                return;

            var current = HEBLKMJBIBO.IJEMMGDMKPE;
            if (current != null &&
                current.Pointer == _pinnedCustomizationAvatarPtr &&
                HasRenderableNativeAvatar(current))
                return;

            // Prefer the live pinned object; fall back to re-wrapping the pointer.
            HEBLKMJBIBO restore = null;
            if (_pinnedCustomizationAvatar != null &&
                _pinnedCustomizationAvatar.Pointer == _pinnedCustomizationAvatarPtr)
            {
                restore = _pinnedCustomizationAvatar;
            }
            else if (_pinnedCustomizationAvatarPtr != IntPtr.Zero)
            {
                restore = new HEBLKMJBIBO(_pinnedCustomizationAvatarPtr);
            }

            if (restore == null || restore.Pointer == IntPtr.Zero)
                return;

            HEBLKMJBIBO.IJEMMGDMKPE = restore;
            if (!_pinnedRestoreLogged)
            {
                _pinnedRestoreLogged = true;
                Plugin.Log.LogWarning(
                    $"[AVATAR] restored pinned customization into IJEMMGDMKPE " +
                    $"selections={restore.FMGNNCFFGLB?.Count ?? 0}");
            }
        }
        catch (Exception e)
        {
            if (!_pinnedRestoreLogged)
            {
                _pinnedRestoreLogged = true;
                Plugin.Log.LogWarning(
                    "[AVATAR] restore pinned failed: " +
                    e.GetBaseException().Message);
            }
        }
    }

    private static void SoleFluxCamera(UnityEngine.Camera flux)
    {
        if (flux == null)
            return;
        try
        {
            var all = UnityEngine.Camera.allCameras;
            var disabled = 0;
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null || c.Pointer == IntPtr.Zero)
                        continue;
                    if (c.Pointer == flux.Pointer)
                        continue;
                    var n = c.gameObject?.name ?? string.Empty;
                    // Keep audio/UI only if named clearly; disable everything else.
                    if (n.IndexOf("FluxRec", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (c.enabled)
                    {
                        c.enabled = false;
                        disabled++;
                    }
                }
            }

            if (!_soleCamLogged)
            {
                _soleCamLogged = true;
                Plugin.Log.LogWarning(
                    $"[GAMEPLAY] sole FluxRec camera active; disabled others={disabled}");
            }
        }
        catch { /* ignore */ }
    }

    private static void EnsureLocalAvatarModel()
    {
        EnsureAvatarItemCache();

        // Always reassert the customization the player made if we pinned it.
        RestorePinnedCustomizationAvatar();

        var avatar = HEBLKMJBIBO.IJEMMGDMKPE;
        if (HasRenderableNativeAvatar(avatar))
            return;

        // Do not wipe a pinned customization that has selections even if the
        // addressable renderability check fails offline.
        if (_pinnedCustomizationAvatarPtr != IntPtr.Zero &&
            avatar != null &&
            avatar.Pointer == _pinnedCustomizationAvatarPtr &&
            (avatar.FMGNNCFFGLB?.Count ?? 0) > 0)
            return;

        // Do not rebuild AvatarItemSelection lists from managed code. Both
        // AvatarItem and AvatarItemSelection are IL2CPP value types in this
        // client; inserting wrappers for them into native generic collections
        // corrupts the copied struct payload. Let the game's own default-profile
        // constructor create the complete native selection list instead.
        var nativeDefault = HEBLKMJBIBO.IAFIPJGMOJE();
        if (nativeDefault != null)
        {
            avatar = nativeDefault;
            HEBLKMJBIBO.IJEMMGDMKPE = avatar;
            Plugin.Log.LogInfo(
                $"[AVATAR] installed stock native default profile " +
                $"selections={avatar.FMGNNCFFGLB?.Count ?? 0} " +
                $"renderable={HasRenderableNativeAvatar(avatar)}");
            return;
        }

        if (avatar == null)
        {
            avatar = new HEBLKMJBIBO();
            HEBLKMJBIBO.IJEMMGDMKPE = avatar;
            Plugin.Log.LogWarning(
                "[AVATAR] stock default profile was unavailable; installed empty native model");
        }
    }

    private static bool HasRenderableNativeAvatar(HEBLKMJBIBO avatar)
    {
        var selections = avatar?.FMGNNCFFGLB;
        if (selections == null || selections.Count == 0)
            return false;

        for (var index = 0; index < selections.Count; index++)
        {
            var selection = selections[index];
            if (selection != null && IsCompleteNativeAvatarItem(selection.AvatarItem))
                return true;
        }

        return false;
    }

    private static bool IsCompleteNativeAvatarItem(
        RecRoom.Avatars.Data.Runtime.AvatarItem item)
    {
        var data = item?.AvatarItemData;
        var visual = item?.AvatarItemVisualData;
        var config = RecRoom.Avatars.Data.Runtime.AvatarItemWardrobeRuntimeConfig.Config;
        if (data == null || visual == null || config == null)
            return false;

        try
        {
            // PlayerAvatarDisplay does not load the strings directly. The
            // shipped wardrobe config first resolves them through three
            // serialized lookup tables, then starts the addressable operations.
            // A non-null AvatarItemVisualData with a missing lookup result is
            // exactly what produced the null LoadAssets results in the title log.
            var prefabGuid = visual.PrefabGuid;
            var prefabReference = string.IsNullOrWhiteSpace(prefabGuid)
                ? null
                : config.KBIFONPIIDJ(prefabGuid);
            if (prefabReference == null || !prefabReference.RuntimeKeyIsValid())
                return false;

            var combinationGuid = visual.CombinationGuid;
            if (!string.IsNullOrWhiteSpace(combinationGuid))
            {
                var combinationReference = config.JPKLHDINJKC(combinationGuid);
                if (combinationReference == null ||
                    !combinationReference.RuntimeKeyIsValid())
                    return false;
            }

            var materialReferences = config.IEJJNJKLAEB(visual);
            if (materialReferences == null)
                return false;
            for (var index = 0; index < materialReferences.Length; index++)
            {
                var materialReference = materialReferences[index];
                if (materialReference == null ||
                    !materialReference.RuntimeKeyIsValid())
                    return false;
            }

            var description = item.JFEADHBKFIE();
            return !string.IsNullOrWhiteSpace(description);
        }
        catch
        {
            return false;
        }
    }

    private static void RefreshTitleAvatarPreview()
    {
        PopulateAllAvatarItemLists();
        var avatar = HEBLKMJBIBO.IJEMMGDMKPE;
        if (avatar == null)
            return;

        var models = UnityEngine.Resources
            .FindObjectsOfTypeAll<RRUI.Data.AnimatedPlayerPuppetAvatarModel>();
        Plugin.Log.LogInfo($"[AVATAR] native animated-avatar models found={models.Length}");
        RecRoom.Players.Puppet.AnimatedPlayerPuppet activePuppet = null;
        for (var i = 0; i < models.Length; i++)
        {
            var model = models[i];
            if (model == null || model.gameObject == null)
                continue;
            var modelScene = model.gameObject.scene;
            if (!modelScene.IsValid() ||
                !modelScene.isLoaded ||
                !model.gameObject.activeInHierarchy)
                continue;

            if (model.APHIKPEHHJI != avatar)
                model.SetAvatarReference(avatar);
            if (!_nativeAvatarReferenceRebound)
            {
                // The model's own post-initialize lifecycle method is the sole
                // owner of SetAvatarVisuals. Calling PlayerAvatarDisplay.Rebuild
                // in parallel with it creates overlapping LoadAssets operations
                // and was the source of repeated null load results.
                model.DNOLMGGJKDA();
                _nativeAvatarReferenceRebound = true;
            }
            var puppet = model.NCKMNOBLALG;
            if (puppet == null)
                continue;

            activePuppet = puppet;
            puppet.gameObject.SetActive(true);
            var display = puppet.playerAvatarDisplay;
            var renderers = display?.gameObject
                .GetComponentsInChildren<UnityEngine.Renderer>(true);
            Plugin.Log.LogInfo(
                $"[AVATAR] stock puppet ready '{puppet.gameObject.name}' " +
                $"revision={model.HDNFNELJKGK} renderers={renderers?.Length ?? 0} " +
                $"stockVisualsApplied={_nativeAvatarReferenceRebound}");
            // Cache while title is alive — this is the real customization avatar.
            RememberLiveTitlePuppet(puppet, "title-refresh");
        }
        if (activePuppet != null)
            RepairStockAvatarRenderPipeline(activePuppet);
    }

    private static void RepairStockAvatarRenderPipeline(
        RecRoom.Players.Puppet.AnimatedPlayerPuppet puppet)
    {
        var renderModels = UnityEngine.Resources
            .FindObjectsOfTypeAll<RRUI.Data.AnimatedPlayerPuppetRenderTextureModel>();
        RRUI.Data.AnimatedPlayerPuppetRenderTextureModel activeRenderModel = null;
        for (var index = 0; index < renderModels.Length; index++)
        {
            var renderModel = renderModels[index];
            if (renderModel == null ||
                renderModel.gameObject == null ||
                !renderModel.gameObject.scene.IsValid() ||
                !renderModel.gameObject.scene.isLoaded ||
                !renderModel.gameObject.activeInHierarchy)
                continue;

            activeRenderModel = renderModel;
            if (renderModel.NCKMNOBLALG != puppet)
            {
                renderModel.NCKMNOBLALG = puppet;
                renderModel.ILINONAMFKD();
            }
            Plugin.Log.LogInfo(
                $"[AVATAR] stock render model active puppet={renderModel.NCKMNOBLALG != null} " +
                $"camera={renderModel.DKOCLFPBJGM != null} " +
                $"primaryTexture={TextureDescription(renderModel.FLMIKFCLAJG)} " +
                $"outputTexture={TextureDescription(renderModel.NJBIAHCLBMH)}");
            break;
        }

        var controllers = UnityEngine.Resources
            .FindObjectsOfTypeAll<RRUI.Data.AnimatedPlayerPuppetRenderTextureModelController>();
        for (var index = 0; index < controllers.Length; index++)
        {
            var controller = controllers[index];
            if (controller == null ||
                controller.gameObject == null ||
                !controller.gameObject.scene.IsValid() ||
                !controller.gameObject.scene.isLoaded ||
                !controller.gameObject.activeInHierarchy)
                continue;

            if (activeRenderModel != null && controller.model != activeRenderModel)
                controller.model = activeRenderModel;
            controller.OnShow();
        }

        var rawImages = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.UI.RawImage>();
        for (var index = 0; index < rawImages.Length; index++)
        {
            var image = rawImages[index];
            if (image == null ||
                image.gameObject == null ||
                !string.Equals(image.gameObject.name, "[AnimatedAvatarImage]", StringComparison.Ordinal) ||
                !image.gameObject.scene.IsValid() ||
                !image.gameObject.scene.isLoaded ||
                !image.gameObject.activeInHierarchy)
                continue;

            var nativeTexture = activeRenderModel?.NJBIAHCLBMH ??
                                activeRenderModel?.FLMIKFCLAJG ??
                                activeRenderModel?.LOAOAPCKJLC;
            if (image.texture == null && nativeTexture != null)
                image.texture = nativeTexture;
            image.enabled = true;
            image.color = UnityEngine.Color.white;
            Plugin.Log.LogInfo(
                $"[AVATAR] stock RawImage texture={TextureDescription(image.texture)} " +
                $"rect={image.rectTransform.rect} color={image.color}");
        }
    }

    private static string TextureDescription(UnityEngine.Texture texture) =>
        texture == null ? "<null>" : $"{texture.name}:{texture.width}x{texture.height}";

    public static void AvatarModelInitializePrefix(RRUI.Data.AvatarCustomizationModel __instance)
    {
        try
        {
            EnsureLocalAvatarModel();
            var avatar = HEBLKMJBIBO.IJEMMGDMKPE;
            if (avatar == null)
                return;

            __instance.GICHEFGLELE = true;
            __instance.SetAvatarReference(avatar);
            Plugin.Log.LogInfo(
                $"[AVATAR] attached local avatar before customization initialization " +
                $"settings={avatar.GAANBMIIPHK != null} selections={avatar.FMGNNCFFGLB?.Count ?? -1}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AVATAR] pre-initialization binding failed: {e}");
        }
    }

    public static void AnimatedAvatarModelInitializePrefix(
        RRUI.Data.AnimatedPlayerPuppetAvatarModel __instance)
    {
        try
        {
            EnsureLocalAvatarModel();
            var avatar = HEBLKMJBIBO.IJEMMGDMKPE;
            if (avatar != null)
                __instance.SetAvatarReference(avatar);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AVATAR] native preview pre-bind failed: {e.Message}");
        }
    }

    public static void AnimatedAvatarModelInitializedPostfix(
        RRUI.Data.AnimatedPlayerPuppetAvatarModel __instance)
    {
        try
        {
            var avatar = HEBLKMJBIBO.IJEMMGDMKPE;
            if (avatar == null ||
                avatar.FMGNNCFFGLB == null ||
                avatar.FMGNNCFFGLB.Count == 0 ||
                __instance?.NCKMNOBLALG == null ||
                __instance.gameObject == null ||
                !__instance.gameObject.activeInHierarchy)
                return;

            if (__instance.APHIKPEHHJI != avatar)
                __instance.SetAvatarReference(avatar);
            __instance.DNOLMGGJKDA();
            _nativeAvatarReferenceRebound = true;

            // Hold the LIVE stock title puppet reference (no FindObjectsOfTypeAll —
            // that fails after scene load on this depot). This is the official
            // [PlayerPuppet]AnimatedVariant with wardrobe meshes.
            RememberLiveTitlePuppet(__instance.NCKMNOBLALG, "title-lifecycle");

            if (avatar.FMGNNCFFGLB.Count >= _pinnedCustomizationSelectionCount)
                PinLocalCustomizationAvatar("title-puppet-lifecycle");
            Plugin.Log.LogInfo(
                $"[AVATAR] applied repaired outfit through stock animated-puppet lifecycle " +
                $"selections={avatar.FMGNNCFFGLB.Count}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AVATAR] stock animated-puppet handoff failed: {e}");
        }
    }

    public static void OutfitManagerStartPostfix(
        RecRoom.Avatars.Outfit.OutfitManager __instance)
    {
        try
        {
            if (__instance == null || __instance.gameObject == null)
                return;
            EnsureAvatarItemCache();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(
                $"[AVATAR] wardrobe registration after OutfitManager.Start failed: {e}");
        }
    }

    private static void EnsureAvatarItemCache()
    {
        var config = RecRoom.Avatars.Data.Runtime.AvatarItemWardrobeRuntimeConfig.Config;
        if (config == null)
        {
            Plugin.Log.LogWarning("[AVATAR] wardrobe runtime config is not loaded yet");
            return;
        }

        if (!_avatarCatalogLogged)
        {
            // Keep the cache created by the IL2CPP runtime. AvatarItem is a
            // native value type, so replacing Dictionary<string, AvatarItem>
            // and filling it through managed generic Add corrupts its values.
            // The serialized config lists below are already native-owned and
            // are the canonical source for the title wardrobe.
            _avatarCatalogLogged = true;
            Plugin.Log.LogInfo(
                $"[AVATAR] using stock native wardrobe cache " +
                $"cache={RecRoom.Avatars.Data.Runtime.AvatarItem._avatarItemCache?.Count ?? -1} " +
                $"all={config.allPossibleCombinations?.Count ?? -1} " +
                $"unlocked={config.defaultUnlockedAvatarItems?.Count ?? -1} " +
                $"initial={config.initialAvatarItems?.Count ?? -1}");
        }

        EnsureBrowsableAvatarItems(config);
    }

    private static void EnsureBrowsableAvatarItems(
        RecRoom.Avatars.Data.Runtime.AvatarItemWardrobeRuntimeConfig config)
    {
        RecRoom.Avatars.Outfit.OutfitManager manager;
        try
        {
            manager = NetworkedSingletonMonoBehaviour<RecRoom.Avatars.Outfit.OutfitManager>.KGGJIHLJBIH;
        }
        catch
        {
            return;
        }

        if (manager == null || manager.gameObject == null)
            return;

        var instanceId = manager.GetInstanceID();
        if (_registeredOutfitManagerInstanceId == instanceId &&
            _registeredBrowsableItemCount > 0 &&
            manager.KFAIOBHPPLD != null &&
            manager.KFAIOBHPPLD.Count >= _registeredBrowsableItemCount)
            return;

        // These collections are AOT-created by OutfitManager. Constructing or
        // mutating Dictionary<AvatarItem, ...> from managed code is unsafe
        // because AvatarItem is an IL2CPP value type. If Start has not built
        // them yet, retry from the page/list lifecycle instead.
        if (manager.KFAIOBHPPLD == null ||
            manager.DAODPALCJCC == null ||
            manager.GDJFAKDHMBL == null)
        {
            Plugin.Log.LogInfo(
                "[AVATAR] OutfitManager native caches are not ready; registration deferred");
            return;
        }

        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var added = 0;
        var existing = 0;
        var failed = 0;
        RegisterBrowsableAvatarItems(
            manager,
            config.defaultUnlockedAvatarItems,
            seen,
            ref added,
            ref existing,
            ref failed);
        RegisterBrowsableAvatarItems(
            manager,
            config.initialAvatarItems,
            seen,
            ref added,
            ref existing,
            ref failed);
        RegisterBrowsableAvatarItems(
            manager,
            config.defaultTorsoAvatarItems,
            seen,
            ref added,
            ref existing,
            ref failed);
        // Do not latch a failed early initialization. The next title-frame or
        // item-list initialization will retry after OutfitManager.Start.
        if (added + existing > 0)
        {
            _registeredOutfitManagerInstanceId = instanceId;
            _registeredBrowsableItemCount = added + existing;
        }

        Plugin.Log.LogInfo(
            $"[AVATAR] registered native browsable wardrobe added={added} " +
            $"existing={existing} failed={failed} total={manager.KFAIOBHPPLD.Count}");
    }

    private static void RegisterBrowsableAvatarItems(
        RecRoom.Avatars.Outfit.OutfitManager manager,
        Il2CppSystem.Collections.Generic.List<RecRoom.Avatars.Data.Runtime.AvatarItem> items,
        System.Collections.Generic.HashSet<string> seen,
        ref int added,
        ref int existing,
        ref int failed,
        RecRoom.Avatars.Data.Shared.OutfitType? onlyOutfitType = null,
        int maximumToRegister = int.MaxValue)
    {
        if (items == null)
            return;

        var registeredForType = 0;
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (onlyOutfitType.HasValue &&
                item?.AvatarItemData?.OutfitType != onlyOutfitType.Value)
                continue;
            if (registeredForType >= maximumToRegister)
                break;
            if (!IsCompleteNativeAvatarItem(item))
                continue;

            string description;
            try
            {
                description = item.JFEADHBKFIE();
            }
            catch
            {
                failed++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(description) || !seen.Add(description))
                continue;

            try
            {
                // Use the native metadata owner. Its generated implementation
                // performs the value-type dictionary insertion inside IL2CPP;
                // calling Dictionary<AvatarItem, ...>.Add through interop can
                // corrupt the marshalled key and crash the process.
                var metadata = manager.GetItemMetadata(item);
                if (metadata == null)
                {
                    failed++;
                    continue;
                }
                metadata.IsNew = false;
                metadata.FriendlyName = item.AvatarItemData.Name ?? "Avatar item";
                metadata.Tooltip = item.AvatarItemData.Name ?? string.Empty;
                metadata.TagList ??=
                    new Il2CppSystem.Collections.Generic.List<string>();
                metadata.AvatarItemId = 0;
                metadata.ThumbnailFilename ??= string.Empty;

                var browsable = manager.GetBrowsableAvatarItem(item);
                if (browsable == null)
                {
                    manager.OAPDCMIEJPD(item);
                    browsable = manager.GetBrowsableAvatarItem(item);
                    if (browsable == null)
                    {
                        failed++;
                        continue;
                    }
                    added++;
                }
                else
                {
                    existing++;
                }
                registeredForType++;

                if (browsable != null)
                {
                    if (browsable.NAFADOFDFFM == null)
                        browsable.NAFADOFDFFM = metadata;
                    if (!manager.DAODPALCJCC.Contains(browsable))
                        manager.DAODPALCJCC.Add(browsable);
                }
            }
            catch (Exception exception)
            {
                failed++;
                if (failed <= 3)
                    Plugin.Log.LogWarning(
                        $"[AVATAR] could not register '{item.AvatarItemData?.Name}': " +
                        exception.Message);
            }
        }
    }

    private static void EnsureBrowsableAvatarItemsForType(
        RecRoom.Avatars.Data.Runtime.AvatarItemWardrobeRuntimeConfig config,
        RecRoom.Avatars.Data.Shared.OutfitType outfitType,
        int maximumToRegister)
    {
        RecRoom.Avatars.Outfit.OutfitManager manager;
        try
        {
            manager = NetworkedSingletonMonoBehaviour<RecRoom.Avatars.Outfit.OutfitManager>.KGGJIHLJBIH;
        }
        catch
        {
            return;
        }

        if (manager == null ||
            manager.gameObject == null ||
            manager.KFAIOBHPPLD == null ||
            manager.DAODPALCJCC == null ||
            manager.GDJFAKDHMBL == null)
            return;

        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var added = 0;
        var existing = 0;
        var failed = 0;
        RegisterBrowsableAvatarItems(
            manager,
            config.allPossibleCombinations,
            seen,
            ref added,
            ref existing,
            ref failed,
            outfitType,
            maximumToRegister);

        if (added > 0 || failed > 0)
            Plugin.Log.LogInfo(
                $"[AVATAR] registered native {outfitType} cards " +
                $"added={added} existing={existing} failed={failed}");
    }

    public static void AvatarItemListInitializePrefix(
        RRUI.Data.AvatarCustomizationItemListModel __instance,
        ref Il2CppSystem.Collections.Generic.IEnumerable<string> __0)
    {
        try
        {
            EnsureAvatarItemCache();
            var config = RecRoom.Avatars.Data.Runtime.AvatarItemWardrobeRuntimeConfig.Config;
            if (config == null)
                return;
            EnsureBrowsableAvatarItemsForType(config, __instance.outfitType, 120);

            var original = __0?.TryCast<Il2CppSystem.Collections.Generic.List<string>>();
            var descriptions = original ?? new Il2CppSystem.Collections.Generic.List<string>();
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < descriptions.Count; index++)
            {
                var existing = descriptions[index];
                if (!string.IsNullOrEmpty(existing))
                    seen.Add(existing);
            }

            AddAvatarItemDescriptions(descriptions, seen, config.defaultUnlockedAvatarItems, __instance.outfitType);
            AddAvatarItemDescriptions(descriptions, seen, config.initialAvatarItems, __instance.outfitType);
            AddAvatarItemDescriptions(descriptions, seen, config.defaultTorsoAvatarItems, __instance.outfitType);
            AddAvatarItemDescriptions(
                descriptions,
                seen,
                config.allPossibleCombinations,
                __instance.outfitType,
                120);

            if (original == null)
                __0 = descriptions.Cast<Il2CppSystem.Collections.Generic.IEnumerable<string>>();
            Plugin.Log.LogInfo(
                $"[AVATAR] prepared {descriptions.Count} native descriptors for {__instance.outfitType}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AVATAR] item-list prefill failed: {e}");
        }
    }

    public static void AvatarItemListInitializedPostfix(
        RRUI.Data.AvatarCustomizationItemListModel __instance)
    {
        try
        {
            EnsureAvatarItemCache();

            var config = RecRoom.Avatars.Data.Runtime.AvatarItemWardrobeRuntimeConfig.Config;
            var descriptions = __instance.LEIFEBCDNEP;
            if (config == null || descriptions == null)
                return;
            EnsureBrowsableAvatarItemsForType(config, __instance.outfitType, 120);

            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < descriptions.Count; index++)
            {
                var existing = descriptions[index];
                if (!string.IsNullOrEmpty(existing))
                    seen.Add(existing);
            }

            AddAvatarItemDescriptions(descriptions, seen, config.defaultUnlockedAvatarItems, __instance.outfitType);
            AddAvatarItemDescriptions(descriptions, seen, config.initialAvatarItems, __instance.outfitType);
            AddAvatarItemDescriptions(descriptions, seen, config.defaultTorsoAvatarItems, __instance.outfitType);

            // Some old builds ship an empty starter list but still contain the
            // complete local addressable wardrobe. In that case expose those
            // built-in combinations instead of leaving the grid blank.
            if (descriptions.Count == 0)
                AddAvatarItemDescriptions(
                    descriptions,
                    seen,
                    config.allPossibleCombinations,
                    __instance.outfitType,
                    40);

            if (descriptions.Count > 0)
                Plugin.Log.LogInfo(
                    $"[AVATAR] supplied {descriptions.Count} local items for {__instance.outfitType} " +
                    $"parent={__instance.PLFIAENOAJB != null}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AVATAR] item-list recovery failed: {e}");
        }
    }

    private static void PopulateAllAvatarItemLists()
    {
        try
        {
            var models = UnityEngine.Resources
                .FindObjectsOfTypeAll<RRUI.Data.AvatarCustomizationItemListModel>();
            for (var index = 0; index < models.Length; index++)
            {
                var model = models[index];
                if (model == null ||
                    model.gameObject == null ||
                    !model.gameObject.scene.IsValid() ||
                    !model.gameObject.scene.isLoaded ||
                    !model.gameObject.activeInHierarchy)
                    continue;
                AvatarItemListInitializedPostfix(model);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AVATAR] could not populate active item lists: {e.Message}");
        }
    }

    private static void AddAvatarItemDescriptions(
        Il2CppSystem.Collections.Generic.List<string> destination,
        System.Collections.Generic.HashSet<string> seen,
        Il2CppSystem.Collections.Generic.List<RecRoom.Avatars.Data.Runtime.AvatarItem> items,
        RecRoom.Avatars.Data.Shared.OutfitType outfitType,
        int maximumToAdd = int.MaxValue)
    {
        if (items == null)
            return;

        var added = 0;
        for (var i = 0; i < items.Count && added < maximumToAdd; i++)
        {
            var item = items[i];
            if (item?.AvatarItemData == null ||
                item.AvatarItemData.OutfitType != outfitType ||
                !IsCompleteNativeAvatarItem(item))
                continue;

            var description = item.JFEADHBKFIE();
            if (!string.IsNullOrEmpty(description) && seen.Add(description))
            {
                destination.Add(description);
                added++;
            }
        }
    }

    public static void TitleUpdatePostfix(TitleScreenManager __instance)
    {
        try
        {
            var model = __instance?.flowModel;
            if (model == null)
                return;

            if (_localRegistrationTask != null && _localRegistrationTask.IsCompleted)
            {
                var completedTask = _localRegistrationTask;
                _localRegistrationTask = null;
                var result = completedTask.GetAwaiter().GetResult();
                if (result.Success)
                {
                    Plugin.Log.LogInfo(
                        $"[AUTH] local registration accepted status={result.StatusCode} " +
                        $"existing={result.AlreadyExists}; verifying credentials locally");
                    model.BIIMFMGPLHB = _localRegistrationUsername;
                    model.HKCOLMHPAAD = _localRegistrationPassword;
                    model.LJNNOCNOEPK = true;
                    BeginManagedCredentialLogin(
                        model,
                        _localRegistrationUsername,
                        _localRegistrationPassword,
                        true);
                }
                else
                {
                    FailManagedLogin(
                        model,
                        result.Error,
                        $"local registration failed status={result.StatusCode}");
                }
            }

            if (_localCredentialLoginTask != null &&
                _localCredentialLoginTask.IsCompleted)
            {
                var completedTask = _localCredentialLoginTask;
                _localCredentialLoginTask = null;
                var result = completedTask.GetAwaiter().GetResult();
                if (!result.Success)
                {
                    FailManagedLogin(
                        model,
                        result.Error,
                        $"local credential login failed status={result.StatusCode}");
                }
                else if (!TryInstallNativeToken(result, out var installError))
                {
                    FailManagedLogin(model, installError, "native token installation failed");
                }
                else
                {
                    _activeLocalAccessToken = result.AccessToken;
                    Plugin.Log.LogInfo(
                        "[AUTH] local credentials accepted and native access token installed; dispatching account/me");
                    var accountEndpoint =
                        Plugin.ServerHostname.Value.TrimEnd('/') + "/account/me";
                    _localAccountLoadTask =
                        LoadLocalAccountAsync(accountEndpoint, result.AccessToken);
                }
            }

            if (_localAccountLoadTask != null &&
                _localAccountLoadTask.IsCompleted)
            {
                var completedTask = _localAccountLoadTask;
                _localAccountLoadTask = null;
                var result = completedTask.GetAwaiter().GetResult();
                if (!result.Success)
                {
                    FailManagedLogin(
                        model,
                        result.Error,
                        $"managed account/me failed status={result.StatusCode}");
                }
                else if (!TryInstallNativeAccount(result.Profile, out var accountError))
                {
                    FailManagedLogin(
                        model,
                        accountError,
                        "native account cache installation failed");
                }
                else
                {
                    var launchCreatedAccount = _launchCreatedAccountAfterAuth;
                    _launchCreatedAccountAfterAuth = false;
                    _registrationLoginInProgress = false;
                    _manualLoginStartedAt = null;
                    if (launchCreatedAccount)
                    {
                        // Registration finishes on the depot's real account
                        // completion page. Its Get Started button is patched
                        // above to perform the guarded Orientation launch.
                        model.avatarCustomizationPage =
                            model.accountCreationCompletePage;
                        model.GoToCachedAccountStartAccountCreationFlow();
                        Plugin.Log.LogInfo(
                            $"[AUTH] account/me cached; opened original Welcome/Get Started page " +
                            $"state={AuthState(model)}");
                    }
                    else
                    {
                        Plugin.Log.LogInfo(
                            "[AUTH] account/me cached; launching signed-in account");
                        QueueNativeGameLaunch(model, false);
                    }
                }
            }

            PumpNativeGameLaunch();
            PumpLocalMatchmakingLogin();
            PumpBootSequenceFallback();
            PumpLocalOrientationMatchmaking();
            PumpSceneDiagnostics();
            LogRecNetServiceMapOnce();
            // Safe to pump from the title screen: it stays loaded for the whole
            // wait, precisely because the room loader never replaces it.
            PumpDirectOrientationSceneLoad();

            // Auto-drive signup -> Welcome -> Let's Play when enabled (default).
            // Also used by RECNET_VALIDATE_ACCOUNT_LAUNCH for unattended CI.
            if (_validationProbeAt.HasValue && DateTime.UtcNow >= _validationProbeAt.Value)
            {
                _validationProbeAt = null;
                if (_validationProbeStage == 0)
                {
                    _validationProbeStage = 1;
                    model.CreateNewAccountThenGoToStartAccountCreationFlow();
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                }
                else if (_validationProbeStage == 1 &&
                         string.Equals(AuthState(model), "BIRTHDAY", StringComparison.Ordinal))
                {
                    _validationProbeStage = 2;
                    BirthdaySubmitPrefix(model);
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(3);
                }
                else if (_validationProbeStage == 2 &&
                         (string.Equals(AuthState(model), "AVATAR_CUSTOMIZATION", StringComparison.Ordinal) ||
                          string.Equals(AuthState(model), "USERNAME_NON_JUNIOR", StringComparison.Ordinal)))
                {
                    // Birthday handoff may skip straight to username on this depot.
                    if (string.Equals(AuthState(model), "AVATAR_CUSTOMIZATION", StringComparison.Ordinal))
                    {
                        _validationProbeStage = 3;
                        Plugin.Log.LogInfo("[VALIDATION] avatar customization reached");
                        AvatarSubmitPrefix(model);
                        _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                    }
                    else
                    {
                        _validationProbeStage = 4;
                        _validationUsername =
                            $"player{Math.Abs(DateTime.UtcNow.Ticks % 1_000_000L):000000}";
                        model.HMDKPGLBOMK = _validationUsername;
                        UsernameSubmitPrefix(model);
                        _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                    }
                }
                else if (_validationProbeStage == 3 &&
                         string.Equals(AuthState(model), "USERNAME_NON_JUNIOR", StringComparison.Ordinal))
                {
                    _validationProbeStage = 4;
                    _validationUsername =
                        $"player{Math.Abs(DateTime.UtcNow.Ticks % 1_000_000L):000000}";
                    model.HMDKPGLBOMK = _validationUsername;
                    UsernameSubmitPrefix(model);
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                }
                else if (_validationProbeStage == 4 &&
                         string.Equals(AuthState(model), "PASSWORD", StringComparison.Ordinal))
                {
                    _validationProbeStage = 5;
                    model.HMAPPDEEFIC = "FluxRec-Local-728!";
                    PasswordSubmitPrefix(model);
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                }
                else if (_validationProbeStage == 5 &&
                         string.Equals(AuthState(model), "ACCOUNT_CREATION_CONSOLIDATED_INFO", StringComparison.Ordinal))
                {
                    _validationProbeStage = 6;
                    if (string.IsNullOrEmpty(_validationUsername))
                        _validationUsername = model.HMDKPGLBOMK ?? $"player{Math.Abs(Environment.TickCount):000000}";
                    Plugin.Log.LogInfo(
                        $"[VALIDATION] consolidated page reached username={_validationUsername}");
                    if (_validationAccountLaunchEnabled)
                    {
                        // Empty email is fine for the private server; force a
                        // unique local-only address so stock validation passes.
                        model.FCFPJEFJJAP = $"{_validationUsername}@local.test";
                        model.SubmitAccountCreationConsolidatedInfoAndGoToNext();
                        _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                    }
                }
                else if (_validationProbeStage == 6 && _validationAccountLaunchEnabled)
                {
                    // The Code of Conduct "Agree" button is a real click in
                    // normal play. Drive it here so unattended runs reach
                    // registration + Orientation without user input.
                    _validationProbeStage = 7;
                    Plugin.Log.LogInfo(
                        $"[VALIDATION] accepting Code of Conduct state={AuthState(model)} " +
                        $"username={_validationUsername}");
                    model.AcceptCodeOfConductAndGoToNext();
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(3);
                }
                else if (_validationProbeStage == 7 && _validationAccountLaunchEnabled)
                {
                    var state = AuthState(model);
                    if (string.Equals(state, "ACCOUNT_CREATION_COMPLETE", StringComparison.Ordinal))
                    {
                        // Stop on Welcome / Let's Play. Only fully unattended CI
                        // (RECNET_VALIDATE_ACCOUNT_LAUNCH=1) auto-presses it.
                        _validationProbeStage = 8;
                        var unattended = string.Equals(
                            Environment.GetEnvironmentVariable("RECNET_VALIDATE_ACCOUNT_LAUNCH"),
                            "1",
                            StringComparison.Ordinal);
                        if (unattended)
                        {
                            Plugin.Log.LogInfo(
                                $"[VALIDATION] unattended mode: pressing Let's Play state={state}");
                            model.LaunchGameAccountCreation();
                        }
                        else
                        {
                            Plugin.Log.LogWarning(
                                "[VALIDATION] Welcome / Let's Play is ready — " +
                                "waiting for the player to click Let's Play");
                        }
                    }
                    else if (string.Equals(state, "ACCOUNT_CREATION_LAUNCH_GAME", StringComparison.Ordinal))
                    {
                        _validationProbeStage = 8;
                    }
                    else
                    {
                        Plugin.Log.LogInfo(
                            $"[VALIDATION] waiting for Welcome page state={state}");
                        _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                    }
                }
            }

            if (string.Equals(AuthState(model), "AVATAR_CUSTOMIZATION", StringComparison.Ordinal))
            {
                if (!_avatarPreviewRefreshed && !_avatarPreviewRefreshAt.HasValue)
                    _avatarPreviewRefreshAt = DateTime.UtcNow.AddSeconds(2);
                if (!_avatarPreviewRefreshed &&
                    _avatarPreviewRefreshAt.HasValue &&
                    DateTime.UtcNow >= _avatarPreviewRefreshAt.Value)
                {
                    _avatarPreviewRefreshAttempts++;
                    // One delayed render-pipeline repair is sufficient after
                    // the model postfix has started the stock addressable load.
                    // Rebuilding the display repeatedly races that load.
                    _avatarPreviewRefreshed = true;
                    _avatarPreviewRefreshAt = null;
                    RefreshTitleAvatarPreview();
                }
            }

            // Temporary local recovery for the native bootstrap's two known
            // indefinite loading states. The native flow remains in control
            // unless it has actually exceeded the recovery window.
            if (_accountCreationStartedAt.HasValue &&
                (DateTime.UtcNow - _accountCreationStartedAt.Value).TotalSeconds >= 8)
            {
                _accountCreationStartedAt = null;
                // The local bootstrap has no platform avatar object, so the
                // stock customization page has an empty descriptor source and
                // cannot submit. Reuse the next real signup page as the first
                // local route while preserving the native route graph.
                _originalAvatarPage = model.avatarCustomizationPage;
                model.avatarCustomizationPage = model.birthdayPage;
                model.GoToCachedAccountStartAccountCreationFlow();
                if (_validationProbeStage == 1)
                    _validationProbeAt = DateTime.UtcNow.AddSeconds(2);
                Plugin.Log.LogWarning($"[AUTH] account-creation watchdog entered local creation flow state={AuthState(model)}");
            }

            if (_manualLoginStartedAt.HasValue &&
                _localCredentialLoginTask == null &&
                _localAccountLoadTask == null &&
                (DateTime.UtcNow - _manualLoginStartedAt.Value).TotalSeconds >= 60)
            {
                FailManagedLogin(
                    model,
                    "Login did not complete.",
                    "managed login watchdog expired");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AUTH] watchdog failed: {e.Message}");
        }
    }

    private static string AuthState(RRUI.Data.TitleScreenFlowModel model)
    {
        try
        {
            return model == null ? "<null>" : model.CJLBEBHIIKI.ToString();
        }
        catch
        {
            return "<unavailable>";
        }
    }

    public static void NameserverCallbackPrefix(MPKGFLPHDAJ __0, HIBHFHKEMCJ __1)
    {
        try
        {
            var status = __1 == null ? -1 : __1.CHEPHPEPILO;
            var body = __1?.KAHAGJOAFAH;
            var preview = string.IsNullOrEmpty(body) ? "<empty>" : body.Substring(0, Math.Min(body.Length, 300));
            Plugin.Log.LogWarning($"[NS-CALLBACK] status={status} url={__0?.PJLCPNELOFH} body={preview}");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[NS-CALLBACK] inspect failed: {e.Message}");
        }
    }

    // The modern client contacts several RecNet hosts before it ever reaches the
    // nameserver (notably api.rec.net for version/config bootstrap). Redirect all
    // official RecNet service hosts to the configured local endpoint.
    private static bool IsOfficialRecNetHost(string host) =>
        host.Equals("rec.net", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".rec.net", StringComparison.OrdinalIgnoreCase);

    // Skip when HTTP-logging so we don't spam the logs.
    private static readonly string[] LogIgnoreSubstrings =
    {
        "/api/gamesight/event",
        "/data/heartbeat",
        "/identify",
        "/httpapi",
        "/data/event",
    };

    private static bool IsIgnoredForLogging(string url)
    {
        foreach (var s in LogIgnoreSubstrings)
            if (url.Contains(s, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string RedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (!System.Uri.TryCreate(url, UriKind.Absolute, out var original)) return url;
        if (!IsOfficialRecNetHost(original.Host)) return url;

        var configured = new System.Uri(Plugin.ServerHostname.Value);
        var builder = new System.UriBuilder(original)
        {
            Scheme = configured.Scheme,
            Host = configured.Host,
            Port = configured.IsDefaultPort ? -1 : configured.Port,
        };
        var redirected = builder.Uri.ToString();
        if (Plugin.Debug.Value)
            Plugin.Log.LogInfo($"[HTTP] redirected URL {url} -> {redirected}");
        return redirected;
    }

    private static void RewriteHostHeader(HTTPRequest request)
    {
        try
        {
            var configured = new System.Uri(Plugin.ServerHostname.Value);
            request.RemoveHeader("Host");
            request.SetHeader("Host", configured.IsDefaultPort ? configured.Host : $"{configured.Host}:{configured.Port}");
        }
        catch { }
    }

    // Cap logged bodies so a large response/request doesn't flood the log.
    private const int MaxLoggedBodyLength = 10000;

    private static string Truncate(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= MaxLoggedBodyLength)
            return s;
        return s.Substring(0, MaxLoggedBodyLength) + $"... <truncated {s.Length - MaxLoggedBodyLength} chars>";
    }

    [HarmonyPatch(typeof(HTTPManager), "SendRequest", new[] { typeof(HTTPRequest) })]
    public class ConnectToRecNetPatch
    {
        public static void Prefix(ref HTTPRequest request)
        {
            var debug = Plugin.Debug.Value && !IsIgnoredForLogging(request.Uri.AbsoluteUri);

            if (debug)
            {
                var entityBody = request.GetEntityBody();
                string body;
                if (entityBody == null)
                    body = "<none>";
                else if (IsBinaryContentType(request.GetFirstHeaderValue("content-type")) || LooksBinary(entityBody))
                    body = BinaryPreview(entityBody);
                else
                    body = System.Text.Encoding.UTF8.GetString(entityBody);
                Plugin.Log.LogInfo($"[HTTP] {request.MethodType} {request.Uri.AbsoluteUri} body={Truncate(body)}");
            }

            var host = request.Uri.Host;
            if (IsOfficialRecNetHost(host))
            {
                // Redirect the nameserver lookup to the complete configured endpoint.
                // Swapping only the host leaves the original https scheme and port in place,
                // which makes an http://127.0.0.1:8081 server unreachable.
                var configured = new System.Uri(Plugin.ServerHostname.Value);
                var builder = new Il2CppSystem.UriBuilder(request.Uri)
                {
                    Scheme = configured.Scheme,
                    Host = configured.Host,
                    Port = configured.IsDefaultPort ? -1 : configured.Port,
                };
                request.Uri = builder.Uri;

                // The IL2CPP nameserver validator rejects a local response when
                // the original virtual-host header is still ns.rec.net.
                try
                {
                    var authority = configured.IsDefaultPort
                        ? configured.Host
                        : $"{configured.Host}:{configured.Port}";
                    request.SetHeader("Host", authority);
                }
                catch (Exception e)
                {
                    if (Plugin.Debug.Value)
                        Plugin.Log.LogWarning($"[HTTP] could not rewrite Host header: {e.Message}");
                }

                // BestHTTP's HEAD probe against ns.rec.net is misreported by this
                // IL2CPP build as a 403 after local redirection. The probe only
                // checks reachability, so use the equivalent JSON GET locally.
                if (host.Equals("ns.rec.net", StringComparison.OrdinalIgnoreCase) &&
                    request.MethodType == HTTPMethods.Head)
                {
                    request.MethodType = HTTPMethods.Get;
                }

                if (debug)
                    Plugin.Log.LogInfo($"[HTTP] intercepted {host} -> {request.Uri}");
            }

            if (debug)
                LogResponseWhenDone(request);
        }
    }

    // Some client paths construct HTTPRequest directly and never call the
    // HTTPManager overloads above. Patch the final send boundary as well.
    [HarmonyPatch(typeof(HTTPRequest), "Send", new Type[0])]
    public class ConnectToRecNetRequestSendPatch
    {
        public static void Prefix(HTTPRequest __instance)
        {
            var uri = __instance.Uri;
            if (Plugin.Debug.Value)
                Plugin.Log.LogInfo($"[HTTP] request send hook uri={uri}");
            if (uri == null || !IsOfficialRecNetHost(uri.Host)) return;

            var configured = new System.Uri(Plugin.ServerHostname.Value);
            var builder = new Il2CppSystem.UriBuilder(uri)
            {
                Scheme = configured.Scheme,
                Host = configured.Host,
                Port = configured.IsDefaultPort ? -1 : configured.Port,
            };
            __instance.Uri = builder.Uri;
            RewriteHostHeader(__instance);

            if (Plugin.Debug.Value)
                Plugin.Log.LogInfo($"[HTTP] request send redirected -> {__instance.Uri}");
        }
    }

    [HarmonyPatch(typeof(HTTPManager), "SendRequestImpl", new[] { typeof(HTTPRequest) })]
    public class ConnectToRecNetImplPatch
    {
        public static void Prefix(HTTPRequest __0)
        {
            var uri = __0?.Uri;
            if (Plugin.Debug.Value)
                Plugin.Log.LogInfo($"[HTTP] request impl hook uri={uri}");
            if (uri == null || !IsOfficialRecNetHost(uri.Host)) return;

            var configured = new System.Uri(Plugin.ServerHostname.Value);
            var builder = new Il2CppSystem.UriBuilder(uri)
            {
                Scheme = configured.Scheme,
                Host = configured.Host,
                Port = configured.IsDefaultPort ? -1 : configured.Port,
            };
            __0.Uri = builder.Uri;
        }
    }

    [HarmonyPatch(typeof(HTTPManager), "SendRequest", new[] { typeof(string), typeof(OnRequestFinishedDelegate) })]
    public class ConnectToRecNetStringPatch
    {
        private static void Prefix(ref string __0) => __0 = RedirectUrl(__0);
    }

    [HarmonyPatch(typeof(HTTPManager), "SendRequest", new[] { typeof(string), typeof(HTTPMethods), typeof(OnRequestFinishedDelegate) })]
    public class ConnectToRecNetStringMethodPatch
    {
        private static void Prefix(ref string __0) => __0 = RedirectUrl(__0);
    }

    [HarmonyPatch(typeof(HTTPManager), "SendRequest", new[] { typeof(string), typeof(HTTPMethods), typeof(bool), typeof(OnRequestFinishedDelegate) })]
    public class ConnectToRecNetStringMethodBoolPatch
    {
        private static void Prefix(ref string __0) => __0 = RedirectUrl(__0);
    }

    [HarmonyPatch(typeof(HTTPManager), "SendRequest", new[] { typeof(string), typeof(HTTPMethods), typeof(bool), typeof(bool), typeof(OnRequestFinishedDelegate) })]
    public class ConnectToRecNetStringMethodBoolsPatch
    {
        private static void Prefix(ref string __0) => __0 = RedirectUrl(__0);
    }

    // Wraps the request's completion callback so we log the response (status + body) when it
    // finishes, then forwards to the game's original callback. This is how we see *which*
    // request comes back empty (RecNet throws "Response was empty" on a blank body).
    private static void LogResponseWhenDone(HTTPRequest request)
    {
        try
        {
            var original = request.Callback;
            var url = request.Uri.AbsoluteUri;

            request.Callback = DelegateSupport.ConvertDelegate<OnRequestFinishedDelegate>(
                (Action<HTTPRequest, HTTPResponse>)((req, resp) =>
                {
                    if (resp == null)
                        Plugin.Log.LogWarning($"[HTTP] <- {url} NO RESPONSE (state={req.State})");
                    else
                    {
                        string text;
                        if (IsBinaryContentType(resp.GetFirstHeaderValue("content-type")))
                            text = "<binary>";
                        else
                        {
                            text = resp.DataAsText;
                            if (string.IsNullOrEmpty(text)) text = "<empty>";
                        }
                        var msg = $"[HTTP] <- {resp.StatusCode} {url} body={Truncate(text)}";
                        if (resp.StatusCode is >= 200 and < 300)
                            Plugin.Log.LogInfo(msg);
                        else
                            Plugin.Log.LogError(msg);
                    }

                    original?.Invoke(req, resp);
                }));
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[HTTP] failed to attach response logger: {e}");
        }
    }

    // Content-Type prefixes/keywords we treat as textual; anything else is logged as <binary> so we
    // don't dump image/asset bytes into the log.
    private static readonly string[] TextContentTypes =
    {
        "text/", "application/json", "application/xml", "application/javascript",
        "application/x-www-form-urlencoded", "+json", "+xml",
    };

    // True if the body is (probably) binary and shouldn't be logged as text. Defaults to text when
    // there's no Content-Type, so we err toward logging rather than hiding.
    private static bool IsBinaryContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;

        foreach (var t in TextContentTypes)
            if (contentType.Contains(t, StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }

    // Render the leading bytes of a binary body as text so structured framing (e.g. multipart form
    // boundaries and part headers) stays readable, while raw bytes are shown as \xNN escapes. Capped
    // at MaxLoggedBodyLength since the interesting framing is at the front.
    private static string BinaryPreview(byte[] data)
    {
        if (data.Length == 0) return "<binary empty>";

        var sb = new System.Text.StringBuilder(MaxLoggedBodyLength + 32);
        sb.Append("<binary ").Append(data.Length).Append(" bytes> ");
        var i = 0;
        // Cap on rendered length, not byte count: escapes expand a byte to 4 chars, so this keeps the
        // preview near MaxLoggedBodyLength and avoids a second pass by Truncate at the log site.
        for (; i < data.Length && sb.Length < MaxLoggedBodyLength; i++)
        {
            var b = data[i];
            if (b == 0x09 || b == 0x0A || b == 0x0D || (b >= 0x20 && b < 0x7F))
                sb.Append((char)b);
            else
                sb.Append("\\x").Append(b.ToString("x2"));
        }
        if (i < data.Length)
            sb.Append($"... <truncated {data.Length - i} bytes>");
        return sb.ToString();
    }

    // Content sniff for raw request bytes — the Content-Type header isn't reliably set at
    // SendRequest time (e.g. multipart form bodies set it lazily, and the body still embeds the
    // raw image), so look at the bytes: a NUL byte, or a high ratio of non-text control bytes in
    // the first chunk, means it's binary (or binary-mixed like a multipart upload).
    private static bool LooksBinary(byte[] data)
    {
        if (data.Length == 0) return false;

        var sample = Math.Min(data.Length, 4096);
        var nonText = 0;
        for (var i = 0; i < sample; i++)
        {
            var b = data[i];
            if (b == 0) return true;
            // Control chars other than tab/newline/carriage-return.
            if (b < 0x20 && b != 0x09 && b != 0x0A && b != 0x0D) nonText++;
        }
        return nonText * 100 / sample > 10;
    }
}
