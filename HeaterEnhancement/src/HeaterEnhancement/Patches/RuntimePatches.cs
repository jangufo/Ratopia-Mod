using System;
using HarmonyLib;
using HeaterEnhancement.Runtime;

namespace HeaterEnhancement.Patches
{
    [HarmonyPatch(typeof(Building_Heater), nameof(Building_Heater.BuildingSet))]
    internal static class HeaterBuildingSetPatch
    {
        private static void Prefix(Building_Heater __instance, out object __state)
        {
            __state = HeaterRuntime.BeginBuildingSetUpdateScope(__instance);
        }

        private static void Postfix(Building_Heater __instance)
        {
            HeaterRuntime.OnHeaterInitialized(__instance);
        }

        private static Exception Finalizer(object __state, Exception __exception)
        {
            try
            {
                HeaterRuntime.EndUpdateScope(__state);
            }
            catch
            {
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(Building_Heater), nameof(Building_Heater.Building_Update3))]
    internal static class HeaterBuildingUpdatePatch
    {
        private static bool Prefix(Building_Heater __instance)
        {
            if (HeaterRuntime.SuppressBuildingSetUpdate(__instance))
            {
                return false;
            }

            if (HeaterRuntime.AllowOriginalHeaterUpdate(__instance))
            {
                return true;
            }

            HeaterRuntime.DisableForNonWinter(__instance);
            return false;
        }

        private static Exception Finalizer(
            Building_Heater __instance,
            bool __runOriginal,
            Exception __exception)
        {
            try
            {
                HeaterRuntime.OnHeaterOperationException(
                    __instance,
                    __runOriginal,
                    __exception);
            }
            catch
            {
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(Building_Heater), nameof(Building_Heater.Building_Update))]
    internal static class HeaterWorkingUpdatePatch
    {
        private static bool Prefix(Building_Heater __instance)
        {
            if (HeaterRuntime.AllowOriginalHeaterWorkingUpdate(__instance))
            {
                return true;
            }

            HeaterRuntime.OnHeaterWorkingUpdateSkipped(__instance);
            return false;
        }

        private static Exception Finalizer(
            Building_Heater __instance,
            bool __runOriginal,
            Exception __exception)
        {
            try
            {
                HeaterRuntime.OnHeaterWorkingUpdateCompleted(
                    __instance,
                    __runOriginal,
                    __exception == null);
            }
            catch
            {
            }

            return __exception;
        }
    }

    [HarmonyPatch(
        typeof(Building_Heater),
        nameof(Building_Heater.BuildingWorkingStop),
        new[] { typeof(bool) })]
    internal static class HeaterWorkingStopPatch
    {
        private static void Prefix(Building_Heater __instance)
        {
            HeaterRuntime.OnHeaterWorkingStopped(__instance);
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.WireCheck), new[] { typeof(bool) })]
    [HarmonyAfter("cn.ratopia.specialratizens")]
    internal static class HeaterWireCheckPatch
    {
        private static bool Prefix(Building __instance, ref bool __result)
        {
            return HeaterRuntime.SuppressNonWinterWireCheck(__instance, ref __result);
        }

        private static void Postfix(Building __instance, ref bool __result)
        {
            HeaterRuntime.EnforceNonWinterWireResult(__instance, ref __result);
            HeaterRuntime.OnHeaterWireCheckCompleted(__instance, __result);
        }

        private static Exception Finalizer(
            Building __instance,
            bool __runOriginal,
            Exception __exception)
        {
            try
            {
                HeaterRuntime.OnHeaterOperationException(
                    __instance,
                    __runOriginal,
                    __exception);
            }
            catch
            {
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(Building_Heater), nameof(Building_Heater.IsFunction3OK), new[] { typeof(int) })]
    internal static class HeaterPreviewPatch
    {
        private static void Prefix(Building_Heater __instance)
        {
            HeaterRuntime.PreparePreviewRange(__instance);
        }

        private static void Postfix(Building_Heater __instance)
        {
            HeaterRuntime.HideLegacyPreviewArea(__instance);
        }
    }

    [HarmonyPatch(typeof(MiningBox), "BuildEnableCheck")]
    internal static class HeaterConstructionPreviewPatch
    {
        private static void Postfix(MiningBox __instance)
        {
            HeaterRuntime.UpdateConstructionPreview(__instance);
        }
    }

    [HarmonyPatch(typeof(MiningBox), "EscapeFunction", new[] { typeof(bool) })]
    internal static class HeaterConstructionPreviewCleanupPatch
    {
        private static void Prefix()
        {
            HeaterRuntime.HideConstructionPreview();
        }
    }

    [HarmonyPatch(
        typeof(Building_ElecBase),
        nameof(Building_ElecBase.ActivateCheck),
        new[] { typeof(bool), typeof(bool) })]
    internal static class HeaterActivateCheckPatch
    {
        private static void Postfix(Building_ElecBase __instance)
        {
            HeaterRuntime.SuppressNonWinterPowerAlarm(__instance);
        }
    }

    [HarmonyPatch(typeof(Building_Heater), nameof(Building_Heater.LoadSetting3))]
    internal static class HeaterLoadPatch
    {
        private static void Postfix(Building_Heater __instance)
        {
            HeaterRuntime.OnHeaterLoaded(__instance);
        }
    }

    [HarmonyPatch(typeof(T_Queen), "Update")]
    internal static class HeaterRuntimeTickPatch
    {
        private static void Postfix(T_Queen __instance)
        {
            HeaterRuntime.TickSafely(__instance);
        }
    }

    [HarmonyPatch(typeof(WeatherMgr), nameof(WeatherMgr.SeasonState_Update))]
    internal static class WeatherSeasonStatePatch
    {
        private static void Postfix()
        {
            HeaterRuntime.OnSeasonStateUpdated();
        }
    }

    [HarmonyPatch(typeof(BuildingMgr), nameof(BuildingMgr.RefreshElecUseBuilding))]
    internal static class ElectricityRefreshPatch
    {
        private static void Prefix(out object __state)
        {
            __state = HeaterRuntime.BeginElectricityRefreshScope();
        }

        private static void Postfix(object __state)
        {
            HeaterRuntime.OnElectricityRefreshCompleted(__state);
        }

        private static Exception Finalizer(object __state, Exception __exception)
        {
            try
            {
                HeaterRuntime.EndUpdateScope(__state);
            }
            catch
            {
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(PlantData), MethodType.Constructor, new[] { typeof(WorldObject) })]
    internal static class PlantDataSavePatch
    {
        private static void Postfix(PlantData __instance, WorldObject __0)
        {
            HeaterRuntime.SanitizePlantSaveData(__instance);
        }
    }

    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.LoadSetting), new[] { typeof(PlantData) })]
    internal static class PlantLoadPatch
    {
        private static void Prefix(PlantData __0)
        {
            HeaterRuntime.SanitizePlantLoadData(__0);
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.BuildingDemolition))]
    internal static class BuildingDemolitionPatch
    {
        private static void Prefix(Building __instance)
        {
            HeaterRuntime.OnBuildingDemolishing(__instance);
        }
    }

    [HarmonyPatch(typeof(LoadingSceneMgr), "Start")]
    internal static class LoadingSceneStartPatch
    {
        private static void Prefix()
        {
            HeaterRuntime.OnLoadingSceneStarting();
        }
    }
}
