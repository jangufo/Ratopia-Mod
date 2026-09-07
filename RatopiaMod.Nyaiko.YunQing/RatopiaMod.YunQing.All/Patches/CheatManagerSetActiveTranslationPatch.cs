using HarmonyLib;
using RatopiaMod.YunQing.All.Localization;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(CheatMgr), "SetActive")]
    internal static class CheatManagerSetActiveTranslationPatch
    {
        [HarmonyPostfix]
        private static void TranslateCheatPanel(CheatMgr __instance, bool _act)
        {
            if (_act)
            {
                CheatPanelTranslationProvider.Translator.Localize(__instance);
            }
        }
    }
}
