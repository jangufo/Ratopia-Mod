using MultipurposeResearch.Core;

namespace MultipurposeResearch.Runtime
{
    internal static class QueenUpgradeRuntime
    {
        internal static bool TryUpgrade(QueenUpgrade upgrade)
        {
            var config = Plugin.Settings;
            if (config == null || !SaveStateStore.IsReady())
            {
                return false;
            }

            var purchases = GetPurchases(Plugin.State, upgrade);
            var cost = MultipurposeResearchRules.GetQueenUpgradeCost(config, purchases);
            if (!ResearchPointRuntime.TrySpend(cost))
            {
                return false;
            }

            var state = MultipurposeResearchRules.ApplyQueenUpgrade(
                Plugin.State,
                upgrade,
                config.QueenAttributeIncrease);
            if (!Plugin.TrySaveState(state))
            {
                ResearchPointRuntime.Refund(cost);
                return false;
            }

            var queen = GameMgr.Instance?._T_UnitMgr?.m_Queen;
            queen?.PDI_Calculate();

            Plugin.LogInfo($"女王进修完成：{upgrade} +{config.QueenAttributeIncrease}。");
            return true;
        }

        internal static int GetBonus(T_Queen queen, PDI pdi)
        {
            if (queen == null)
            {
                return 0;
            }

            switch (pdi)
            {
                case PDI.Power: return Plugin.State.QueenPowerBonus;
                case PDI.Dex: return Plugin.State.QueenDexterityBonus;
                case PDI.Int: return Plugin.State.QueenIntelligenceBonus;
                default: return 0;
            }
        }

        private static int GetPurchases(MultipurposeResearchState state, QueenUpgrade upgrade)
        {
            switch (upgrade)
            {
                case QueenUpgrade.Power: return state.QueenPowerPurchases;
                case QueenUpgrade.Dexterity: return state.QueenDexterityPurchases;
                default: return state.QueenIntelligencePurchases;
            }
        }
    }
}
