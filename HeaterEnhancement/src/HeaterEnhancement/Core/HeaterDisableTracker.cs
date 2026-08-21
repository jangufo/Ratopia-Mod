using System.Collections.Generic;

namespace HeaterEnhancement.Core
{
    internal sealed class HeaterDisableTracker
    {
        private readonly HashSet<int> _disabledHeaterIds = new HashSet<int>();

        public bool BeginNonWinterDisable(int heaterId)
        {
            return _disabledHeaterIds.Add(heaterId);
        }

        public void MarkWinter(int heaterId)
        {
            _disabledHeaterIds.Remove(heaterId);
        }

        public void Remove(int heaterId)
        {
            _disabledHeaterIds.Remove(heaterId);
        }

        public void Clear()
        {
            _disabledHeaterIds.Clear();
        }
    }
}
