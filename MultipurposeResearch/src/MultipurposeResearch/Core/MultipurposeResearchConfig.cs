using System;

namespace MultipurposeResearch.Core
{
    internal sealed class MultipurposeResearchConfig
    {
        internal MultipurposeResearchConfig(
            int productivityCost,
            int productivityDurationDays,
            float productivityPercent,
            int saleCost,
            int saleBaseIncome,
            int saleRelationsReward,
            int saleProsperityReward,
            float saleRelationsCoefficient,
            float saleProsperityCoefficient,
            float saleMinimumMultiplier,
            float saleMaximumMultiplier,
            int queenUpgradeBaseCost,
            int queenUpgradeCostStep,
            int queenAttributeIncrease)
        {
            ProductivityCost = productivityCost;
            ProductivityDurationDays = productivityDurationDays;
            ProductivityPercent = productivityPercent;
            SaleCost = saleCost;
            SaleBaseIncome = saleBaseIncome;
            SaleRelationsReward = saleRelationsReward;
            SaleProsperityReward = saleProsperityReward;
            SaleRelationsCoefficient = saleRelationsCoefficient;
            SaleProsperityCoefficient = saleProsperityCoefficient;
            SaleMinimumMultiplier = saleMinimumMultiplier;
            SaleMaximumMultiplier = saleMaximumMultiplier;
            QueenUpgradeBaseCost = queenUpgradeBaseCost;
            QueenUpgradeCostStep = queenUpgradeCostStep;
            QueenAttributeIncrease = queenAttributeIncrease;
        }

        internal int ProductivityCost { get; }
        internal int ProductivityDurationDays { get; }
        internal float ProductivityPercent { get; }
        internal int SaleCost { get; }
        internal int SaleBaseIncome { get; }
        internal int SaleRelationsReward { get; }
        internal int SaleProsperityReward { get; }
        internal float SaleRelationsCoefficient { get; }
        internal float SaleProsperityCoefficient { get; }
        internal float SaleMinimumMultiplier { get; }
        internal float SaleMaximumMultiplier { get; }
        internal int QueenUpgradeBaseCost { get; }
        internal int QueenUpgradeCostStep { get; }
        internal int QueenAttributeIncrease { get; }

        internal static MultipurposeResearchConfig Default => new MultipurposeResearchConfig(
            productivityCost: 50,
            productivityDurationDays: 3,
            productivityPercent: 0.10f,
            saleCost: 50,
            saleBaseIncome: 8000,
            saleRelationsReward: 2,
            saleProsperityReward: 5,
            saleRelationsCoefficient: 0.001f,
            saleProsperityCoefficient: 0.02f,
            saleMinimumMultiplier: 0.90f,
            saleMaximumMultiplier: 1.20f,
            queenUpgradeBaseCost: 50,
            queenUpgradeCostStep: 0,
            queenAttributeIncrease: 1);

        internal static MultipurposeResearchConfig MigrateLegacyCostDefaults(
            MultipurposeResearchConfig input)
        {
            if (input == null)
            {
                return Default;
            }

            return new MultipurposeResearchConfig(
                input.ProductivityCost == 100 ? 50 : input.ProductivityCost,
                input.ProductivityDurationDays,
                input.ProductivityPercent,
                input.SaleCost == 100 ? 50 : input.SaleCost,
                input.SaleBaseIncome,
                input.SaleRelationsReward,
                input.SaleProsperityReward,
                input.SaleRelationsCoefficient,
                input.SaleProsperityCoefficient,
                input.SaleMinimumMultiplier,
                input.SaleMaximumMultiplier,
                input.QueenUpgradeBaseCost == 100 ? 50 : input.QueenUpgradeBaseCost,
                input.QueenUpgradeCostStep == 50 ? 0 : input.QueenUpgradeCostStep,
                input.QueenAttributeIncrease);
        }

        internal static MultipurposeResearchConfig Normalize(MultipurposeResearchConfig input)
        {
            var fallback = Default;
            if (input == null)
            {
                return fallback;
            }

            var pairIsValid = IsFinite(input.SaleMinimumMultiplier) &&
                IsFinite(input.SaleMaximumMultiplier) &&
                input.SaleMinimumMultiplier > 0 &&
                input.SaleMinimumMultiplier <= input.SaleMaximumMultiplier;
            var minimum = pairIsValid ? input.SaleMinimumMultiplier : fallback.SaleMinimumMultiplier;
            var maximum = pairIsValid ? input.SaleMaximumMultiplier : fallback.SaleMaximumMultiplier;

            return new MultipurposeResearchConfig(
                input.ProductivityCost >= 1 ? input.ProductivityCost : fallback.ProductivityCost,
                input.ProductivityDurationDays >= 1 ? input.ProductivityDurationDays : fallback.ProductivityDurationDays,
                IsFinite(input.ProductivityPercent) && input.ProductivityPercent >= 0 ? input.ProductivityPercent : fallback.ProductivityPercent,
                input.SaleCost >= 1 ? input.SaleCost : fallback.SaleCost,
                input.SaleBaseIncome >= 1 ? input.SaleBaseIncome : fallback.SaleBaseIncome,
                input.SaleRelationsReward >= 0 ? input.SaleRelationsReward : fallback.SaleRelationsReward,
                input.SaleProsperityReward >= 0 ? input.SaleProsperityReward : fallback.SaleProsperityReward,
                IsFinite(input.SaleRelationsCoefficient) && input.SaleRelationsCoefficient >= 0 ? input.SaleRelationsCoefficient : fallback.SaleRelationsCoefficient,
                IsFinite(input.SaleProsperityCoefficient) && input.SaleProsperityCoefficient >= 0 ? input.SaleProsperityCoefficient : fallback.SaleProsperityCoefficient,
                minimum,
                maximum,
                input.QueenUpgradeBaseCost >= 1 ? input.QueenUpgradeBaseCost : fallback.QueenUpgradeBaseCost,
                input.QueenUpgradeCostStep >= 0 ? input.QueenUpgradeCostStep : fallback.QueenUpgradeCostStep,
                input.QueenAttributeIncrease >= 1 ? input.QueenAttributeIncrease : fallback.QueenAttributeIncrease);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
