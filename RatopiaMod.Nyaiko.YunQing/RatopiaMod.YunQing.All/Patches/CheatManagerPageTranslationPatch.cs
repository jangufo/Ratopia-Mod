using HarmonyLib;
using RatopiaMod.YunQing.All.Localization;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(CheatMgr), "PageNumSet")]
    internal static class CheatManagerPageTranslationPatch
    {
        [HarmonyPostfix]
        private static void TranslateCheatPanel(CheatMgr __instance)
        {
            CheatPanelTranslationProvider.Translator.Localize(__instance);
        }
    }
}
