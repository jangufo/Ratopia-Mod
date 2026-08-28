using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal sealed class HeaterUpdateScopeCoordinator
    {
        private readonly List<ScopeToken> _buildingSetScopes = new List<ScopeToken>();
        private readonly List<ScopeToken> _buildingSetModUpdates = new List<ScopeToken>();
        private readonly List<ScopeToken> _electricityRefreshScopes = new List<ScopeToken>();

        public object BeginBuildingSet(int heaterId)
        {
            var scope = new ScopeToken(ScopeKind.BuildingSet, heaterId);
            _buildingSetScopes.Add(scope);
            return scope;
        }

        public object BeginBuildingSetModUpdate(int heaterId)
        {
            var kind = HasActiveBuildingSet(heaterId)
                ? ScopeKind.BuildingSetModUpdate
                : ScopeKind.NoOp;
            var scope = new ScopeToken(kind, heaterId);
            if (kind == ScopeKind.BuildingSetModUpdate)
            {
                _buildingSetModUpdates.Add(scope);
            }

            return scope;
        }

        public bool ShouldSuppressBuildingSetUpdate(int heaterId)
        {
            if (!HasActiveBuildingSet(heaterId))
            {
                return false;
            }

            for (var index = _buildingSetModUpdates.Count - 1; index >= 0; index--)
            {
                var update = _buildingSetModUpdates[index];
                if (!update.Ended && !update.Consumed && update.HeaterId == heaterId)
                {
                    update.Consumed = true;
                    return false;
                }
            }

            return true;
        }

        public object BeginElectricityRefresh()
        {
            var scope = new ScopeToken(ScopeKind.ElectricityRefresh, 0);
            _electricityRefreshScopes.Add(scope);
            return scope;
        }

        public void RecordSuccessfulOriginalUpdate(int heaterId)
        {
            foreach (var scope in _electricityRefreshScopes)
            {
                if (!scope.Ended)
                {
                    scope.SuccessfulOriginalUpdates.Add(heaterId);
                }
            }
        }

        public IReadOnlyList<int> GetSuccessfulOriginalUpdates(object scope)
        {
            var token = scope as ScopeToken;
            if (token == null || token.Ended || token.Kind != ScopeKind.ElectricityRefresh)
            {
                return new int[0];
            }

            var successful = new List<int>(token.SuccessfulOriginalUpdates);
            successful.Sort();
            return successful;
        }

        public void EndScope(object scope)
        {
            var token = scope as ScopeToken;
            if (token == null || token.Ended)
            {
                return;
            }

            token.Ended = true;
            switch (token.Kind)
            {
                case ScopeKind.BuildingSet:
                    _buildingSetScopes.Remove(token);
                    break;
                case ScopeKind.BuildingSetModUpdate:
                    _buildingSetModUpdates.Remove(token);
                    break;
                case ScopeKind.ElectricityRefresh:
                    _electricityRefreshScopes.Remove(token);
                    break;
            }
        }

        public void Reset()
        {
            EndAll(_buildingSetModUpdates);
            EndAll(_buildingSetScopes);
            EndAll(_electricityRefreshScopes);
        }

        private bool HasActiveBuildingSet(int heaterId)
        {
            foreach (var scope in _buildingSetScopes)
            {
                if (!scope.Ended && scope.HeaterId == heaterId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EndAll(ICollection<ScopeToken> scopes)
        {
            foreach (var scope in scopes)
            {
                scope.Ended = true;
            }

            scopes.Clear();
        }

        private enum ScopeKind
        {
            NoOp,
            BuildingSet,
            BuildingSetModUpdate,
            ElectricityRefresh
        }

        private sealed class ScopeToken
        {
            public ScopeToken(ScopeKind kind, int heaterId)
            {
                Kind = kind;
                HeaterId = heaterId;
            }

            public ScopeKind Kind { get; }
            public int HeaterId { get; }
            public bool Consumed { get; set; }
            public bool Ended { get; set; }
            public HashSet<int> SuccessfulOriginalUpdates { get; } = new HashSet<int>();
        }
    }
}
