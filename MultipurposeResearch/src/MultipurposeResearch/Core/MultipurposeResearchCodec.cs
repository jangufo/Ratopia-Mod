using System;
using System.Globalization;

namespace MultipurposeResearch.Core
{
    internal static class MultipurposeResearchCodec
    {
        private const string Version = "v1";

        internal static string Serialize(MultipurposeResearchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return string.Join("|", Version,
                state.ProductivityPercent.ToString("R", CultureInfo.InvariantCulture),
                state.ProductivityExpiresAt.ToString(CultureInfo.InvariantCulture),
                state.QueenPowerBonus.ToString(CultureInfo.InvariantCulture),
                state.QueenDexterityBonus.ToString(CultureInfo.InvariantCulture),
                state.QueenIntelligenceBonus.ToString(CultureInfo.InvariantCulture),
                state.QueenPowerPurchases.ToString(CultureInfo.InvariantCulture),
                state.QueenDexterityPurchases.ToString(CultureInfo.InvariantCulture),
                state.QueenIntelligencePurchases.ToString(CultureInfo.InvariantCulture));
        }

        internal static bool TryDeserialize(string text, out MultipurposeResearchState state)
        {
            state = MultipurposeResearchState.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var values = text.Split('|');
            if (values.Length != 9 || values[0] != Version)
            {
                return false;
            }

            if (!float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ||
                float.IsNaN(percent) || float.IsInfinity(percent) || percent < 0f ||
                !TryInt(values[2], out var expiry) || expiry < 0 ||
                !TryInt(values[3], out var power) || power < 0 ||
                !TryInt(values[4], out var dexterity) || dexterity < 0 ||
                !TryInt(values[5], out var intelligence) || intelligence < 0 ||
                !TryInt(values[6], out var powerPurchases) || powerPurchases < 0 ||
                !TryInt(values[7], out var dexterityPurchases) || dexterityPurchases < 0 ||
                !TryInt(values[8], out var intelligencePurchases) || intelligencePurchases < 0)
            {
                return false;
            }

            state = new MultipurposeResearchState(percent, expiry, power, dexterity, intelligence,
                powerPurchases, dexterityPurchases, intelligencePurchases);
            return true;
        }

        private static bool TryInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
