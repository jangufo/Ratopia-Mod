using HarmonyLib;
using MultipurposeResearch.Runtime;

namespace MultipurposeResearch.Patches
{
    [HarmonyPatch(typeof(PlayDataMgr), nameof(PlayDataMgr.BeforeLoad))]
    internal static class GameDataResetPatch
    {
        private static void Prefix()
        {
            Plugin.ResetGameSession();
        }
    }

    [HarmonyPatch(typeof(PlayDataMgr), nameof(PlayDataMgr.LoadData))]
    internal static class GameDataLoadPatch
    {
        private static void Postfix()
        {
            Plugin.BeginGameSession();
        }
    }

    [HarmonyPatch(typeof(T_UnitMgr), nameof(T_UnitMgr.LoadSettings2))]
    internal static class CitizenLoadPatch
    {
        private static void Postfix()
        {
            CitizenProductivityRuntime.ApplyCurrentToAll();
        }
    }

    [HarmonyPatch(typeof(T_Citizen), nameof(T_Citizen.CitizenInit))]
    internal static class CitizenInitPatch
    {
        private static void Postfix(T_Citizen __instance)
        {
            CitizenProductivityRuntime.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(T_Citizen), nameof(T_Citizen.CitizenImmigrant))]
    internal static class CitizenImmigrantPatch
    {
        private static void Postfix(T_Citizen __instance)
        {
            CitizenProductivityRuntime.Apply(__instance);
        }
    }
}
