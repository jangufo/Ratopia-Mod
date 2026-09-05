using HarmonyLib;
using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Logging;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(Fish), "DrownCheck")]
    internal static class FishDrownCheckPatch
    {
        [HarmonyPrefix]
        private static bool MakeFishDrown(Fish __instance)
        {
            var config = ModConfig.Instance;
            if (config == null || !config.FishDrowningEnabled.Value)
            {
                return true;
            }

            ModLog.Debug($"鱼淹死功能触发：{__instance.name}。");
            __instance.BeAttacked(-5f);
            return false;
        }
    }
}
