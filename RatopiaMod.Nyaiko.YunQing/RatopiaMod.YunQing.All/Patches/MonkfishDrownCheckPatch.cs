using HarmonyLib;
using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Logging;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(Monkfish), "DrownCheck")]
    internal static class MonkfishDrownCheckPatch
    {
        [HarmonyPrefix]
        private static bool MakeMonkfishDrown(Monkfish __instance)
        {
            var config = ModConfig.Instance;
            if (config == null || !config.FishDrowningEnabled.Value)
            {
                return true;
            }

            ModLog.Debug($"鮟鱇鱼淹死功能触发：{__instance.name}。");
            __instance.BeAttacked(-5f);
            return false;
        }
    }
}
