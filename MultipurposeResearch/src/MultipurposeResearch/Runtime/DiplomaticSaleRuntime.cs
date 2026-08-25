using System;
using System.Collections.Generic;
using CasselGames.Diplomatic.Data;
using MultipurposeResearch.Core;

namespace MultipurposeResearch.Runtime
{
    internal static class DiplomaticSaleRuntime
    {
        internal static IReadOnlyList<DiplomaticCountryData> GetAvailableCountries()
        {
            var manager = GameMgr.Instance?.DiplomaticMgr;
            var countries = manager?.GetExploredCountryArray();
            var result = new List<DiplomaticCountryData>();
            if (countries == null)
            {
                return result;
            }

            foreach (var country in countries)
            {
                if (country == null || string.IsNullOrEmpty(country.Key) ||
                    manager.IsEnemyHometownAndCountry(country.Key))
                {
                    continue;
                }

                result.Add(country);
            }

            return result;
        }

        internal static int GetPreviewIncome(DiplomaticCountryData country)
        {
            return country == null
                ? 0
                : MultipurposeResearchRules.GetSaleIncome(Plugin.Settings, country.NowRelations, country.NowProsperityLevel);
        }

        internal static bool TrySell(string countryKey)
        {
            var manager = GameMgr.Instance?.DiplomaticMgr;
            var country = manager?.GetCountryData(countryKey);
            if (country == null || string.IsNullOrEmpty(country.Key) ||
                !manager.IsExploredInCountry(country.Key) || manager.IsEnemyHometownAndCountry(country.Key))
            {
                return false;
            }

            var config = Plugin.Settings;
            var income = MultipurposeResearchRules.GetSaleIncome(config, country.NowRelations, country.NowProsperityLevel);
            var relationsBefore = country.NowRelations;
            var prosperityBefore = country.NowProsperityValue;
            var goldApplied = false;
            if (!ResearchPointRuntime.TrySpend(config.SaleCost))
            {
                return false;
            }

            try
            {
                GameMgr.Instance._EcoMgr.Country_GetGold_Diplomatic(country.Key, income);
                goldApplied = true;
                manager.IncreaseRelations(country.Key, config.SaleRelationsReward);
                manager.IncreaseProsperity(country.Key, config.SaleProsperityReward);
                Plugin.LogInfo($"已向 {country.Name} 出售研究点，获得 {income} 金币。");
                return true;
            }
            catch (Exception error)
            {
                ResearchPointRuntime.Refund(config.SaleCost);
                try
                {
                    if (goldApplied)
                    {
                        GameMgr.Instance?._EcoMgr?.Country_GetGold_Refund(country.Key, income);
                    }
                    country.SetRelations(relationsBefore);
                    country.SetProsperityValue(prosperityBefore);
                }
                catch (Exception rollbackError)
                {
                    Plugin.LogError("学术出口补偿回滚也失败，需人工检查外交账本。", rollbackError);
                }
                Plugin.LogError("学术出口结算失败，已退还研究点。", error);
                return false;
            }
        }
    }
}
