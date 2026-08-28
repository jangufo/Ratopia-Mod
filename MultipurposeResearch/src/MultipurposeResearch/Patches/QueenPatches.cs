using HarmonyLib;
using MultipurposeResearch.Runtime;

namespace MultipurposeResearch.Patches
{
    [HarmonyPatch(typeof(T_Queen), nameof(T_Queen.LoadSetting2))]
    internal static class QueenLoadPatch
    {
        private static void Postfix()
        {
            ResearchUiRuntime.Cleanup();
        }
    }

    [HarmonyPatch(typeof(GameUnit), nameof(GameUnit.GetPure_PDI))]
    internal static class QueenPurePdiPatch
    {
        private static void Postfix(GameUnit __instance, PDI _pdi, ref int __result)
        {
            var queen = __instance as T_Queen;
            if (queen != null)
            {
                __result += QueenUpgradeRuntime.GetBonus(queen, _pdi);
            }
        }
    }
}
