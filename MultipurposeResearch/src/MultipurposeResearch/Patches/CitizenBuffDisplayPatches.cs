using System.Collections.Generic;
using HarmonyLib;
using MultipurposeResearch.Runtime;

namespace MultipurposeResearch.Patches
{
    [HarmonyPatch(typeof(CitizenBuff), nameof(CitizenBuff.LogToAllRef))]
    internal static class CitizenBuffReferenceListPatch
    {
        private static void Postfix(ref List<CitizenBuff.RefInfo> __result)
        {
            CitizenProductivityRuntime.EnhanceReferenceMetadata(__result);
        }
    }

    [HarmonyPatch(typeof(CitizenBuff.RefInfo), "Get_T_Name", new[]
    {
        typeof(string), typeof(C_Buff_Category), typeof(bool)
    })]
    internal static class CitizenBuffReferenceNamePatch
    {
        private static bool Prefix(string __0, ref string __result)
        {
            if (!CitizenProductivityRuntime.TryGetDisplayName(__0, out var displayName))
            {
                return true;
            }

            __result = displayName;
            return false;
        }
    }

    [HarmonyPatch(typeof(CitizenBuff.RefInfo), "GetIconAddress", new[]
    {
        typeof(string), typeof(C_Buff_Category)
    })]
    internal static class CitizenBuffReferenceIconPatch
    {
        private static bool Prefix(string __0, ref string __result)
        {
            if (!CitizenProductivityRuntime.TryGetIconAddress(__0, out var iconAddress))
            {
                return true;
            }

            __result = iconAddress;
            return false;
        }
    }

    [HarmonyPatch(typeof(CitizenBuff.RefInfo), nameof(CitizenBuff.RefInfo.GetDescript))]
    internal static class CitizenBuffReferenceDescriptionPatch
    {
        private static bool Prefix(CitizenBuff.RefInfo __instance, ref string __result)
        {
            if (!CitizenProductivityRuntime.TryGetDescription(__instance, out var description))
            {
                return true;
            }

            __result = description;
            return false;
        }
    }
}
