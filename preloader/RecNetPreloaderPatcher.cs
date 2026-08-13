using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Preloader.Core.Patching;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FluxRec.Preloader;

[PatcherPluginInfo("recnet.preloader.patcher", "RecNet Preloader Patcher", "1.0.0")]
public sealed class RecNetPreloaderPatcher : BasePatcher
{
    public override void Initialize()
    {
        Log.LogInfo("RecNet Preloader Patcher: initializing");
        Context.PatchDefinitions.Add(new PatchDefinition(
            new TargetAssemblyAttribute("UnityEngine.CoreModule"),
            this,
            typeof(RecNetPreloaderPatcher).GetMethod(nameof(PatchSceneManager))!));
        Context.PatchDefinitions.Add(new PatchDefinition(
            new TargetAssemblyAttribute("UnityEngine.CoreModule"),
            this,
            typeof(RecNetPreloaderPatcher).GetMethod(nameof(PatchUnityAction))!));
    }

    [TargetAssembly("UnityEngine.CoreModule")]
    [TargetType("UnityEngine.CoreModule", "UnityEngine.SceneManagement.SceneManager")]
    public void PatchSceneManager(TypeDefinition sceneManager)
    {
        foreach (var eventName in new[] { "sceneLoaded", "sceneUnloaded", "activeSceneChanged" })
        {
            var field = sceneManager.Fields.FirstOrDefault(candidate => candidate.Name == eventName);
            if (field is null)
                continue;

            AddGetter(sceneManager, field);
            AddSetter(sceneManager, field);
        }
    }

    [TargetAssembly("UnityEngine.CoreModule")]
    [TargetType("UnityEngine.CoreModule", "UnityEngine.Events.UnityAction")]
    public void PatchUnityAction(TypeDefinition unityAction)
    {
        if (!unityAction.FullName.StartsWith("UnityEngine.Events.UnityAction", StringComparison.Ordinal))
            return;

        foreach (var existingOperator in unityAction.Methods.Where(method => method.Name.StartsWith("op_", StringComparison.Ordinal)).ToList())
            unityAction.Methods.Remove(existingOperator);

        var genericCount = unityAction.HasGenericParameters ? unityAction.GenericParameters.Count : 0;
        var actionName = genericCount == 0 ? "System.Action" : $"System.Action`{genericCount}";
        var runtimeReference = unityAction.Module.AssemblyReferences.FirstOrDefault(reference =>
            reference.Name is "mscorlib" or "System.Runtime");
        if (runtimeReference is null)
        {
            Log.LogWarning("Could not find mscorlib/System.Runtime reference");
            return;
        }

        var runtimeAssembly = Context.AvailableAssemblies.TryGetValue(runtimeReference.Name, out var exact)
            ? exact
            : Context.AvailableAssemblies.Values.FirstOrDefault(assembly => assembly.Name.Name == runtimeReference.Name);
        var actionDefinition = runtimeAssembly?.MainModule.Types.FirstOrDefault(type => type.FullName == actionName);
        if (actionDefinition is null)
        {
            Log.LogWarning($"Could not find {actionName} in {runtimeReference.Name}");
            return;
        }

        TypeReference actionType;
        if (genericCount > 0)
        {
            var genericAction = new GenericInstanceType(unityAction.Module.ImportReference(actionDefinition));
            foreach (var parameter in unityAction.GenericParameters)
                genericAction.GenericArguments.Add(parameter);
            actionType = genericAction;
        }
        else
        {
            actionType = unityAction.Module.ImportReference(actionDefinition);
        }

        AddImplicitOperator(unityAction, actionType);
        AddBinaryOperator(unityAction, "op_Addition");
        AddBinaryOperator(unityAction, "op_Subtraction");
    }

    private static void AddGetter(TypeDefinition owner, FieldDefinition field)
    {
        var name = $"get_{field.Name}";
        if (owner.Methods.Any(method => method.Name == name))
            return;

        var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, field.FieldType);
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldsfld, field);
        il.Emit(OpCodes.Ret);
        owner.Methods.Add(method);
    }

    private static void AddSetter(TypeDefinition owner, FieldDefinition field)
    {
        var name = $"set_{field.Name}";
        if (owner.Methods.Any(method => method.Name == name))
            return;

        var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, owner.Module.TypeSystem.Void);
        method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, field.FieldType));
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);
        owner.Methods.Add(method);
    }

    private static void AddImplicitOperator(TypeDefinition owner, TypeReference actionType)
    {
        var method = new MethodDefinition("op_Implicit", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, owner);
        method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, actionType));
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);
        owner.Methods.Add(method);
    }

    private static void AddBinaryOperator(TypeDefinition owner, string name)
    {
        var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, owner);
        method.Parameters.Add(new ParameterDefinition("left", ParameterAttributes.None, owner));
        method.Parameters.Add(new ParameterDefinition("right", ParameterAttributes.None, owner));
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);
        owner.Methods.Add(method);
    }
}

