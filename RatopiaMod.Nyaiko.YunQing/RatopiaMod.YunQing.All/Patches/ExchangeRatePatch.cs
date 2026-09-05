using System.Collections.Generic;
using System.Linq;
using CasselGames.Diplomatic.Data;
using Extensions;
using HarmonyLib;
using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Domain;
using RatopiaMod.YunQing.All.Logging;

namespace RatopiaMod.YunQing.All.Patches
{
    [HarmonyPatch(typeof(DiplomaticExchangeData), "GetRandomTicket")]
    internal static class ExchangeRatePatch
    {
        [HarmonyPostfix]
        private static void UseConfiguredExchangeRate(DiplomaticExchangeData __instance,
            ref DiplomaticExchangeTicketData __result)
        {
            var config = ModConfig.Instance;
            if (config == null)
            {
                return;
            }

            var mode = config.ExchangeRateModeSetting.Value;
            if (mode == ExchangeRateMode.COMMON)
            {
                ModLog.Debug("汇率券模式为官方正常值，保留原始结果。");
                return;
            }

            ModLog.Debug($"修改前汇率 {__result.ExchangeRate}，目标模式：{mode}。");

            var tickets = Traverse.Create(__instance).Field("_exchangeTicketList")
                .GetValue<List<DiplomaticExchangeTicketData>>();
            var positiveMax = tickets.OrderByDescending(ticket => ticket.ExchangeRate).First();
            var negativeMax = tickets.OrderBy(ticket => ticket.ExchangeRate).First();
            tickets.Shuffle();
            var positive = tickets.First(ticket => ticket.ExchangeRate >= 0);
            var negative = tickets.First(ticket => ticket.ExchangeRate <= 0);

            switch (mode)
            {
                case ExchangeRateMode.POSITIVE:
                    __result = positive;
                    break;
                case ExchangeRateMode.POSITIVE_MAX:
                    __result = positiveMax;
                    break;
                case ExchangeRateMode.NEGATIVE:
                    __result = negative;
                    break;
                case ExchangeRateMode.NEGATIVE_MAX:
                    __result = negativeMax;
                    break;
                default:
                    ModLog.Warn($"出现了未知汇率配置：{mode}，已保留原始结果。");
                    break;
            }

            ModLog.Debug($"修改后汇率 {__result.ExchangeRate}。");
        }
    }
}
