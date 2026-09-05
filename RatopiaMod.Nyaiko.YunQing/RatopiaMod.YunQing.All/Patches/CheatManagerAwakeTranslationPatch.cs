using HarmonyLib;
using RatopiaMod.YunQing.All.Localization;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(CheatMgr), "Awake")]
    internal static class CheatManagerAwakeTranslationPatch
    {
        [HarmonyPostfix]
        private static void TranslateCheatPanel(CheatMgr __instance)
        {
            CheatPanelTranslationProvider.Translator.Localize(__instance);
        }
    }
}
