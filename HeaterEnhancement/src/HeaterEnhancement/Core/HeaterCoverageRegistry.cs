using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal sealed class HeaterCoverageRegistry
    {
        private readonly Dictionary<int, HashSet<GridPoint>> _coverageByHeater =
            new Dictionary<int, HashSet<GridPoint>>();
        private readonly Dictionary<GridPoint, int> _heaterCountByPoint =
            new Dictionary<GridPoint, int>();

        public IReadOnlyList<GridPoint> Register(int heaterId, IEnumerable<GridPoint> coverage)
        {
            var points = new HashSet<GridPoint>();
            if (coverage != null)
            {
                foreach (var point in coverage)
                {
                    points.Add(point);
                }
            }

            HashSet<GridPoint> previousPoints;
            _coverageByHeater.TryGetValue(heaterId, out previousPoints);
            var uncovered = new List<GridPoint>();
            if (previousPoints != null)
            {
                foreach (var point in previousPoints)
                {
                    if (!points.Contains(point))
                    {
                        DecrementCoverage(point, uncovered);
                    }
                }
            }

            foreach (var point in points)
            {
                if (previousPoints != null && previousPoints.Contains(point))
                {
                    continue;
                }

                int count;
                _heaterCountByPoint.TryGetValue(point, out count);
                _heaterCountByPoint[point] = count + 1;
            }

            _coverageByHeater[heaterId] = points;
            return uncovered;
        }

        public IReadOnlyList<GridPoint> Unregister(int heaterId)
        {
            HashSet<GridPoint> points;
            if (!_coverageByHeater.TryGetValue(heaterId, out points))
            {
                return new GridPoint[0];
            }

            var uncovered = new List<GridPoint>();
            foreach (var point in points)
            {
                DecrementCoverage(point, uncovered);
            }

            _coverageByHeater.Remove(heaterId);
            return uncovered;
        }

        public void Clear()
        {
            _coverageByHeater.Clear();
            _heaterCountByPoint.Clear();
        }

        public bool Covers(GridPoint point)
        {
            return _heaterCountByPoint.ContainsKey(point);
        }

        public bool CoversAny(IEnumerable<GridPoint> points)
        {
            if (points == null)
            {
                return false;
            }

            foreach (var point in points)
            {
                if (Covers(point))
                {
                    return true;
                }
            }

            return false;
        }

        private void DecrementCoverage(GridPoint point, ICollection<GridPoint> uncovered)
        {
            var count = _heaterCountByPoint[point] - 1;
            if (count == 0)
            {
                _heaterCountByPoint.Remove(point);
                uncovered.Add(point);
            }
            else
            {
                _heaterCountByPoint[point] = count;
            }
        }
    }
}
