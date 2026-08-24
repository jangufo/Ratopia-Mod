using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal sealed class HeaterSeasonBatchCoordinator
    {
        private const int UnsetSeason = int.MinValue;

        private readonly HashSet<int> _batchHeaterIds = new HashSet<int>();
        private readonly HashSet<int> _pendingHeaterIds = new HashSet<int>();
        private int _targetSeason = UnsetSeason;
        private bool _batchOpen;

        public int LastCommittedSeason { get; private set; } = UnsetSeason;

        public void Reset()
        {
            _batchHeaterIds.Clear();
            _pendingHeaterIds.Clear();
            _targetSeason = UnsetSeason;
            LastCommittedSeason = UnsetSeason;
            _batchOpen = false;
        }

        public void BeginBatch(int season, IEnumerable<int> heaterIds)
        {
            _targetSeason = season;
            _batchHeaterIds.Clear();
            _pendingHeaterIds.Clear();
            _batchOpen = true;
            AddUnseenHeaters(heaterIds);
        }

        public void EnsureSeasonBatch(int season, IEnumerable<int> heaterIds)
        {
            if (_targetSeason != season || (LastCommittedSeason != season && !_batchOpen))
            {
                BeginBatch(season, heaterIds);
                return;
            }

            if (_batchOpen && LastCommittedSeason != season)
            {
                AddUnseenHeaters(heaterIds);
            }
        }

        public void QueueHeater(int season, int heaterId)
        {
            if (_targetSeason != season || !_batchOpen)
            {
                BeginBatch(season, new[] { heaterId });
                return;
            }

            AddUnseenHeater(heaterId);
        }

        public void RemoveHeater(int heaterId)
        {
            _batchHeaterIds.Remove(heaterId);
            _pendingHeaterIds.Remove(heaterId);
        }

        public IReadOnlyList<int> GetPendingHeaterIds()
        {
            var pending = new List<int>(_pendingHeaterIds);
            pending.Sort();
            return pending;
        }

        public void MarkSucceeded(int heaterId)
        {
            _pendingHeaterIds.Remove(heaterId);
        }

        public bool TryCommit(out int committedSeason)
        {
            committedSeason = LastCommittedSeason;
            if (!_batchOpen || _pendingHeaterIds.Count > 0)
            {
                return false;
            }

            LastCommittedSeason = _targetSeason;
            committedSeason = _targetSeason;
            _batchOpen = false;
            return true;
        }

        private void AddUnseenHeaters(IEnumerable<int> heaterIds)
        {
            if (heaterIds == null)
            {
                return;
            }

            foreach (var heaterId in heaterIds)
            {
                AddUnseenHeater(heaterId);
            }
        }

        private void AddUnseenHeater(int heaterId)
        {
            if (_batchHeaterIds.Add(heaterId))
            {
                _pendingHeaterIds.Add(heaterId);
            }
        }
    }
}
