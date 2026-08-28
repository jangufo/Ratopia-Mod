namespace MultipurposeResearch.Core
{
    internal sealed class MultipurposeResearchState
    {
        internal MultipurposeResearchState(
            float productivityPercent,
            int productivityExpiresAt,
            int queenPowerBonus,
            int queenDexterityBonus,
            int queenIntelligenceBonus,
            int queenPowerPurchases,
            int queenDexterityPurchases,
            int queenIntelligencePurchases)
        {
            ProductivityPercent = productivityPercent;
            ProductivityExpiresAt = productivityExpiresAt;
            QueenPowerBonus = queenPowerBonus;
            QueenDexterityBonus = queenDexterityBonus;
            QueenIntelligenceBonus = queenIntelligenceBonus;
            QueenPowerPurchases = queenPowerPurchases;
            QueenDexterityPurchases = queenDexterityPurchases;
            QueenIntelligencePurchases = queenIntelligencePurchases;
        }

        internal float ProductivityPercent { get; }
        internal int ProductivityExpiresAt { get; }
        internal int QueenPowerBonus { get; }
        internal int QueenDexterityBonus { get; }
        internal int QueenIntelligenceBonus { get; }
        internal int QueenPowerPurchases { get; }
        internal int QueenDexterityPurchases { get; }
        internal int QueenIntelligencePurchases { get; }

        internal static MultipurposeResearchState Empty => new MultipurposeResearchState(0f, 0, 0, 0, 0, 0, 0, 0);
    }
}
