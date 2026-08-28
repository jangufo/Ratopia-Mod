namespace HeaterEnhancement.Core
{
    internal enum PlantThermalAuditAction
    {
        None,
        RemoveHeatSystem,
        StopGrowing,
        RemoveHeatSystemAndStopGrowing
    }

    internal static class PlantThermalAuditPlanner
    {
        public static PlantThermalAuditAction Decide(
            bool isWinter,
            bool isCovered,
            bool hasHeatSystem,
            bool isGrowingPaused)
        {
            if (!isWinter)
            {
                return hasHeatSystem
                    ? PlantThermalAuditAction.RemoveHeatSystem
                    : PlantThermalAuditAction.None;
            }

            if (isCovered)
            {
                return PlantThermalAuditAction.None;
            }

            if (hasHeatSystem)
            {
                return PlantThermalAuditAction.RemoveHeatSystemAndStopGrowing;
            }

            return isGrowingPaused
                ? PlantThermalAuditAction.None
                : PlantThermalAuditAction.StopGrowing;
        }
    }
}
