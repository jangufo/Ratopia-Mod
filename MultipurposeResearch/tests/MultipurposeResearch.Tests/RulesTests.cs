using System;
using MultipurposeResearch.Core;
using Xunit;

namespace MultipurposeResearch.Tests
{
    public sealed class RulesTests
    {
        [Fact]
        public void EveryDefaultActionCostsFiftyResearchPoints()
        {
            var config = MultipurposeResearchConfig.Default;
            Assert.Equal(50, config.ProductivityCost);
            Assert.Equal(50, config.SaleCost);
            Assert.Equal(50, config.QueenUpgradeBaseCost);
            Assert.Equal(0, config.QueenUpgradeCostStep);
            Assert.Equal(50, MultipurposeResearchRules.GetQueenUpgradeCost(config, 0));
            Assert.Equal(50, MultipurposeResearchRules.GetQueenUpgradeCost(config, 1));
            Assert.Equal(50, MultipurposeResearchRules.GetQueenUpgradeCost(config, 20));
        }

        [Fact]
        public void LegacyDefaultCostsMigrateToTheNewFiftyPointDefaults()
        {
            var legacy = new MultipurposeResearchConfig(
                100, 3, 0.10f, 100, 8000, 2, 5, 0.001f, 0.02f,
                0.90f, 1.20f, 100, 50, 1);

            var migrated = MigrateLegacyDefaults(legacy);

            Assert.Equal(50, migrated.ProductivityCost);
            Assert.Equal(50, migrated.SaleCost);
            Assert.Equal(50, migrated.QueenUpgradeBaseCost);
            Assert.Equal(0, migrated.QueenUpgradeCostStep);
        }

        [Fact]
        public void LegacyMigrationPreservesCustomizedCosts()
        {
            var customized = new MultipurposeResearchConfig(
                75, 3, 0.10f, 80, 8000, 2, 5, 0.001f, 0.02f,
                0.90f, 1.20f, 90, 25, 1);

            var migrated = MigrateLegacyDefaults(customized);

            Assert.Equal(75, migrated.ProductivityCost);
            Assert.Equal(80, migrated.SaleCost);
            Assert.Equal(90, migrated.QueenUpgradeBaseCost);
            Assert.Equal(25, migrated.QueenUpgradeCostStep);
        }

        [Fact]
        public void SaleMultiplierUsesRelationsAndProsperityAndIsClamped()
        {
            var config = MultipurposeResearchConfig.Default;
            Assert.Equal(1.15f, MultipurposeResearchRules.GetSaleMultiplier(config, 50, 6), 3);
            Assert.Equal(config.SaleMinimumMultiplier, MultipurposeResearchRules.GetSaleMultiplier(config, -1000, -100), 3);
            Assert.Equal(config.SaleMaximumMultiplier, MultipurposeResearchRules.GetSaleMultiplier(config, 10000, 10000), 3);
        }

        [Fact]
        public void InvalidConfigFallsBackToDefaults()
        {
            var invalid = new MultipurposeResearchConfig(
                0, 0, float.NaN, 0, 0, -1, -1, float.NaN, float.PositiveInfinity,
                2, 1, 0, 0, 0);
            var normalized = MultipurposeResearchConfig.Normalize(invalid);
            Assert.Equal(MultipurposeResearchConfig.Default.ProductivityCost, normalized.ProductivityCost);
            Assert.Equal(MultipurposeResearchConfig.Default.ProductivityPercent, normalized.ProductivityPercent);
            Assert.Equal(MultipurposeResearchConfig.Default.SaleMinimumMultiplier, normalized.SaleMinimumMultiplier);
            Assert.Equal(MultipurposeResearchConfig.Default.SaleMaximumMultiplier, normalized.SaleMaximumMultiplier);
        }

        [Fact]
        public void ProductivityRefreshReplacesPercentAndExpiryWithoutStacking()
        {
            var state = MultipurposeResearchState.Empty;
            state = MultipurposeResearchRules.RefreshProductivity(state, 0.1f, 100, 30);
            state = MultipurposeResearchRules.RefreshProductivity(state, 0.2f, 110, 30);
            Assert.Equal(0.2f, state.ProductivityPercent, 3);
            Assert.Equal(140, state.ProductivityExpiresAt);
        }

        [Fact]
        public void QueenAttributesTrackIndependentPurchaseCounts()
        {
            var state = MultipurposeResearchState.Empty;
            state = MultipurposeResearchRules.ApplyQueenUpgrade(state, QueenUpgrade.Power, 1);
            state = MultipurposeResearchRules.ApplyQueenUpgrade(state, QueenUpgrade.Intelligence, 1);
            Assert.Equal(1, state.QueenPowerBonus);
            Assert.Equal(1, state.QueenIntelligenceBonus);
            Assert.Equal(1, state.QueenPowerPurchases);
            Assert.Equal(1, state.QueenIntelligencePurchases);
            Assert.Equal(0, state.QueenDexterityPurchases);
        }

        [Fact]
        public void StateCodecRoundTripsVersionedData()
        {
            var state = new MultipurposeResearchState(0.15f, 321, 2, 3, 4, 2, 3, 4);
            var serialized = MultipurposeResearchCodec.Serialize(state);
            Assert.True(MultipurposeResearchCodec.TryDeserialize(serialized, out var restored));
            Assert.Equal(state.ProductivityPercent, restored.ProductivityPercent, 3);
            Assert.Equal(state.ProductivityExpiresAt, restored.ProductivityExpiresAt);
            Assert.Equal(state.QueenDexterityPurchases, restored.QueenDexterityPurchases);
        }

        private static MultipurposeResearchConfig MigrateLegacyDefaults(MultipurposeResearchConfig config)
        {
            var method = typeof(MultipurposeResearchConfig).GetMethod(
                "MigrateLegacyCostDefaults",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (MultipurposeResearchConfig)method.Invoke(null, new object[] { config });
        }
    }
}
