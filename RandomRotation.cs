using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using ICities;
using HarmonyLib;
using CitiesHarmony.API;
using UnityEngine;
using ColossalFramework.Math;

namespace RandomTreeAndBushRotation
{
    public class Mod : IUserMod
    {
        public string Name
        {
            get { return "Random Tree & Bush Rotation"; }
        }

        public string Description
        {
            get { return "Automatically randomizes rotation of placed trees and bushes/props."; }
        }

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(new Action(Patcher.PatchAll));
        }

        public void OnDisabled()
        {
            Patcher.UnpatchAll();
        }
    }

    public static class Patcher
    {
        private const string HarmonyId = "com.antigravity.randomtreebushrotation";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;
            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                patched = true;
                Debug.Log("[RandomTreeAndBushRotation] Successfully patched tree and bush rotation!");
            }
            catch (Exception ex)
            {
                Debug.LogError("[RandomTreeAndBushRotation] Failed to patch: " + ex);
            }
        }

        public static void UnpatchAll()
        {
            if (!patched) return;
            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                patched = false;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RandomTreeAndBushRotation] Failed to unpatch: " + ex);
            }
        }
    }

    // 1. Bush and Prop Random Rotation
    [HarmonyPatch(typeof(PropManager), "CreateProp", new Type[] {
        typeof(ushort),
        typeof(Randomizer),
        typeof(PropInfo),
        typeof(Vector3),
        typeof(float),
        typeof(bool)
    }, new ArgumentType[] {
        ArgumentType.Out,
        ArgumentType.Ref,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal
    })]
    public static class PropManager_CreateProp_Patch
    {
        public static void Prefix(ref float angle)
        {
            // Randomize angle between 0 and 2*PI radians
            angle = UnityEngine.Random.Range(0f, 6.2831853f);
        }
    }

    // 2. Tree Random Rotation in 3D Rendering
    [HarmonyPatch(typeof(TreeInstance), "RenderInstance", new Type[] {
        typeof(RenderManager.CameraInfo),
        typeof(TreeInfo),
        typeof(Vector3),
        typeof(float),
        typeof(float),
        typeof(Vector4),
        typeof(bool)
    })]
    public static class TreeInstance_RenderInstance_Patch
    {
        public static Quaternion GetTreeRotation(Vector3 position)
        {
            float angle = Mathf.Abs((position.x * 73.13f) + (position.z * 157.37f)) % 360f;
            return Quaternion.Euler(0f, angle, 0f);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo qIdentity = AccessTools.PropertyGetter(typeof(Quaternion), "identity");
            MethodInfo customRot = AccessTools.Method(typeof(TreeInstance_RenderInstance_Patch), "GetTreeRotation");

            foreach (var inst in instructions)
            {
                if (inst.Calls(qIdentity))
                {
                    // Replace Quaternion.identity with GetTreeRotation(position)
                    // arg 2 is Vector3 position
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, customRot);
                }
                else
                {
                    yield return inst;
                }
            }
        }
    }
}
