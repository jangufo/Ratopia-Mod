using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal static class PlantSaveBuffSanitizer
    {
        public static void RemoveHeatSystemPairs(
            IList<string> buffNames,
            IList<float> buffValues,
            string heatSystemName)
        {
            if (buffNames == null || buffValues == null || heatSystemName == null)
            {
                return;
            }

            var pairCount = buffNames.Count < buffValues.Count
                ? buffNames.Count
                : buffValues.Count;
            for (var index = pairCount - 1; index >= 0; index--)
            {
                if (buffNames[index] != heatSystemName)
                {
                    continue;
                }

                buffNames.RemoveAt(index);
                buffValues.RemoveAt(index);
            }
        }
    }
}
