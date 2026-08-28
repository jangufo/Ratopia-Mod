using System;

namespace MultipurposeResearch.Core
{
    internal enum QueenUpgrade
    {
        Power,
        Dexterity,
        Intelligence
    }

    internal static class MultipurposeResearchRules
    {
        internal static int GetQueenUpgradeCost(MultipurposeResearchConfig config, int purchases)
        {
            config = MultipurposeResearchConfig.Normalize(config);
            purchases = Math.Max(0, purchases);
            var value = (long)config.QueenUpgradeBaseCost + (long)config.QueenUpgradeCostStep * purchases;
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        internal static float GetSaleMultiplier(MultipurposeResearchConfig config, int relations, int prosperityLevel)
        {
            config = MultipurposeResearchConfig.Normalize(config);
            var raw = 1f + relations * config.SaleRelationsCoefficient +
                (prosperityLevel - 1) * config.SaleProsperityCoefficient;
            return Math.Max(config.SaleMinimumMultiplier, Math.Min(config.SaleMaximumMultiplier, raw));
        }

        internal static int GetSaleIncome(MultipurposeResearchConfig config, int relations, int prosperityLevel)
        {
            config = MultipurposeResearchConfig.Normalize(config);
            var income = config.SaleBaseIncome * GetSaleMultiplier(config, relations, prosperityLevel);
            if (income >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Math.Max(1, (int)Math.Round(income, MidpointRounding.AwayFromZero));
        }

        internal static MultipurposeResearchState RefreshProductivity(
            MultipurposeResearchState state,
            float percent,
            int currentDay,
            int durationDays)
        {
            state = state ?? MultipurposeResearchState.Empty;
            return new MultipurposeResearchState(
                Math.Max(0f, percent),
                SafeAdd(currentDay, Math.Max(1, durationDays)),
                state.QueenPowerBonus,
                state.QueenDexterityBonus,
                state.QueenIntelligenceBonus,
                state.QueenPowerPurchases,
                state.QueenDexterityPurchases,
                state.QueenIntelligencePurchases);
        }

        internal static bool IsProductivityActive(MultipurposeResearchState state, int currentDay)
        {
            return state != null && state.ProductivityPercent > 0f && currentDay < state.ProductivityExpiresAt;
        }

        internal static MultipurposeResearchState ApplyQueenUpgrade(
            MultipurposeResearchState state,
            QueenUpgrade upgrade,
            int increase)
        {
            state = state ?? MultipurposeResearchState.Empty;
            increase = Math.Max(0, increase);
            switch (upgrade)
            {
                case QueenUpgrade.Power:
                    return new MultipurposeResearchState(state.ProductivityPercent, state.ProductivityExpiresAt,
                        SafeAdd(state.QueenPowerBonus, increase), state.QueenDexterityBonus, state.QueenIntelligenceBonus,
                        SafeAdd(state.QueenPowerPurchases, increase > 0 ? 1 : 0), state.QueenDexterityPurchases, state.QueenIntelligencePurchases);
                case QueenUpgrade.Dexterity:
                    return new MultipurposeResearchState(state.ProductivityPercent, state.ProductivityExpiresAt,
                        state.QueenPowerBonus, SafeAdd(state.QueenDexterityBonus, increase), state.QueenIntelligenceBonus,
                        state.QueenPowerPurchases, SafeAdd(state.QueenDexterityPurchases, increase > 0 ? 1 : 0), state.QueenIntelligencePurchases);
                default:
                    return new MultipurposeResearchState(state.ProductivityPercent, state.ProductivityExpiresAt,
                        state.QueenPowerBonus, state.QueenDexterityBonus, SafeAdd(state.QueenIntelligenceBonus, increase),
                        state.QueenPowerPurchases, state.QueenDexterityPurchases, SafeAdd(state.QueenIntelligencePurchases, increase > 0 ? 1 : 0));
            }
        }

        private static int SafeAdd(int left, int right)
        {
            var value = (long)left + right;
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }
    }
}
