using CasselGames.Diplomatic.Data;
using HarmonyLib;
using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Logging;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(DiplomaticExchangeData), "get_DefaultDarValue")]
    internal static class BankExchangePatch
    {
        [HarmonyPostfix]
        private static void ApplyConfiguredMultiplier(ref float __result)
        {
            var config = ModConfig.Instance;
            if (config == null)
            {
                return;
            }

            var multiplier = (int)config.BankExchangeMultiplierSetting.Value;
            if (multiplier < 1)
            {
                ModLog.Warn($"银行兑换倍数配置无效：x{multiplier}，保留原始结果。");
                return;
            }

            var originalValue = __result;
            __result *= multiplier;
            ModLog.Debug($"银行兑换值已从 {originalValue} 调整为 {__result}，倍数：x{multiplier}。");
        }
    }
}
