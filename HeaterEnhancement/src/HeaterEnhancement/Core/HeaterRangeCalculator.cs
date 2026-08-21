using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal static class HeaterRangeCalculator
    {
        public const int EffectiveCellCount = 39;

        public static IReadOnlyList<GridPoint> CreateCoverage(
            GridPoint firstRowCenter,
            IEnumerable<GridPoint> buildingFootprint)
        {
            var occupied = new HashSet<GridPoint>();
            if (buildingFootprint != null)
            {
                foreach (var point in buildingFootprint)
                {
                    occupied.Add(point);
                }
            }

            var coverage = new List<GridPoint>(45);
            for (var row = 0; row < 5; row++)
            {
                for (var offsetX = -4; offsetX <= 4; offsetX++)
                {
                    var point = new GridPoint(firstRowCenter.X + offsetX, firstRowCenter.Y - row);
                    if (!occupied.Contains(point))
                    {
                        coverage.Add(point);
                    }
                }
            }

            return coverage;
        }
    }
}
