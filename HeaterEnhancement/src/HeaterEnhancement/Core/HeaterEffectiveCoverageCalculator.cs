using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal static class HeaterEffectiveCoverageCalculator
    {
        public static IReadOnlyList<GridPoint> Create(
            IEnumerable<GridPoint> candidates,
            IEnumerable<GridPoint> blocked,
            bool isWinter,
            bool isActive,
            bool hasElectricity)
        {
            var result = new List<GridPoint>();
            if (!isWinter || !isActive || !hasElectricity || candidates == null)
            {
                return result;
            }

            var blockedPoints = blocked == null
                ? new HashSet<GridPoint>()
                : new HashSet<GridPoint>(blocked);
            foreach (var point in candidates)
            {
                if (!blockedPoints.Contains(point))
                {
                    result.Add(point);
                }
            }

            return result;
        }
    }
}
