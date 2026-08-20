using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using HeaterEnhancement.Core;
using UnityEngine;

namespace HeaterEnhancement.Runtime
{
    internal static class HeaterRuntime
    {
        private const string HeatSystemBuffName = "HeatSystem";
        private const float RuntimeSyncInterval = 0.5f;

        private static readonly FieldInfo LocalPositionField =
            AccessTools.Field(typeof(Building_Heater), "List_LocalPos");
        private static readonly FieldInfo BlockedPositionField =
            AccessTools.Field(typeof(Building_Heater), "List_BlockPos");
        private static readonly FieldInfo AreaObjectField =
            AccessTools.Field(typeof(Building_Heater), "Obj_Area");
        private static readonly FieldInfo MiningBoxBuildInfoField =
            AccessTools.Field(typeof(MiningBox), "m_BuildInfo");
        private static readonly FieldInfo MiningBoxTransformField =
            AccessTools.Field(typeof(MiningBox), "Tf");
        private static readonly FieldInfo MiningBoxWaterTileConditionField =
            AccessTools.Field(typeof(MiningBox), "m_WaterTileCondition");
        private static readonly HeaterCoverageRegistry Coverage = new HeaterCoverageRegistry();
        private static readonly HeaterCoverageRegistry EffectiveCoverage =
            new HeaterCoverageRegistry();
        private static readonly HeaterDisableTracker DisableTracker = new HeaterDisableTracker();
        private static readonly Dictionary<int, HeaterState> HeaterStates =
            new Dictionary<int, HeaterState>();
        private static readonly List<GreenBox> ConstructionPreviewBoxes = new List<GreenBox>();

        private static ManualLogSource _logger;
        private static BuildingMgr _buildingManager;
        private static bool _enabled;
        private static bool _shuttingDown;
        private static bool _firstTickLogged;
        private static bool _orphanAuditDirty;
        private static int _lastSeasonValue = int.MinValue;
        private static float _nextRuntimeSyncAt;
        private static MiningBox _constructionPreviewOwner;
        private static Vector2Int _constructionPreviewOrigin;
        private static bool _hasConstructionPreviewOrigin;

        public static void Configure(ManualLogSource logger)
        {
            _logger = logger;
            _enabled = true;
            _shuttingDown = false;
            ResetSession();
        }

        public static void OnHeaterInitialized(Building_Heater heater)
        {
            if (!CanUseHeater(heater))
            {
                return;
            }

            try
            {
                InitializeHeater(heater);
            }
            catch (Exception exception)
            {
                LogError("初始化加热器范围", exception);
            }
        }

        public static void OnHeaterLoaded(Building_Heater heater)
        {
            if (!CanUseHeater(heater))
            {
                return;
            }

            try
            {
                EnsureHeaterTracked(heater);
                ApplySeasonState(heater);
            }
            catch (Exception exception)
            {
                LogError("读档后同步加热器", exception);
            }
        }

        public static void OnHeaterWorkingUpdateCompleted(Building_Heater heater, bool succeeded)
        {
            if (!_enabled || _shuttingDown || heater == null)
            {
                return;
            }

            try
            {
                if (succeeded)
                {
                    SynchronizeEffectiveCoverage(heater);
                }
                else
                {
                    InvalidateEffectiveCoverage(heater.m_ID);
                }
            }
            catch (Exception exception)
            {
                InvalidateEffectiveCoverage(heater.m_ID);
                LogError("同步加热器实际有效范围", exception);
            }
        }

        public static void OnHeaterWireCheckCompleted(Building building, bool result)
        {
            var heater = building as Building_Heater;
            if (!_enabled || _shuttingDown || heater == null || result)
            {
                return;
            }

            InvalidateEffectiveCoverage(heater.m_ID);
        }

        public static void OnHeaterWorkingStopped(Building_Heater heater)
        {
            if (!_enabled || _shuttingDown || heater == null)
            {
                return;
            }

            InvalidateEffectiveCoverage(heater.m_ID);
        }

        public static bool AllowOriginalHeaterUpdate(Building_Heater heater)
        {
            if (_shuttingDown || !_enabled || heater == null)
            {
                return true;
            }

            SeasonState season;
            return !TryGetSeasonState(out season) || season == SeasonState.Winter;
        }

        public static void EnforceNonWinterWireResult(Building building, ref bool result)
        {
            var heater = building as Building_Heater;
            if (AllowOriginalHeaterUpdate(heater))
            {
                return;
            }

            result = false;
            DisableForNonWinter(heater);
        }

        public static bool SuppressNonWinterWireCheck(Building building, ref bool result)
        {
            var heater = building as Building_Heater;
            if (AllowOriginalHeaterUpdate(heater))
            {
                return true;
            }

            // The SpecialRatizens postfix treats true as already handled and will not reconnect it.
            result = true;
            return false;
        }

        public static void PreparePreviewRange(Building_Heater heater)
        {
            if (!CanUseHeater(heater) || heater.List_BuildPos == null || heater.List_BuildPos.Count == 0)
            {
                return;
            }

            try
            {
                SynchronizeExtendedRange(heater);
            }
            catch (Exception exception)
            {
                LogError("同步加热器范围提示", exception);
            }
        }

        public static void UpdateConstructionPreview(MiningBox miningBox)
        {
            if (!_enabled || _shuttingDown || miningBox == null ||
                MiningBoxBuildInfoField == null || MiningBoxTransformField == null)
            {
                HideConstructionPreview();
                return;
            }

            try
            {
                var buildInfo = MiningBoxBuildInfoField.GetValue(miningBox) as BuildInfo;
                if (buildInfo == null || buildInfo.Name != BuildingName.Heater)
                {
                    HideConstructionPreview();
                    return;
                }

                HideLegacyConstructionPreview(miningBox);

                var transform = MiningBoxTransformField.GetValue(miningBox) as Transform;
                var game = GameMgr.Instance;
                var buildingManager = game != null ? game._BuildingMgr : null;
                var poolManager = game != null ? game._PoolMgr : null;
                if (transform == null || buildingManager == null || poolManager == null ||
                    poolManager.Pool_GreenBox == null)
                {
                    HideConstructionPreview();
                    return;
                }

                var origin = new Vector2Int(
                    Mathf.CeilToInt(transform.position.x),
                    Mathf.CeilToInt(transform.position.y));
                if (ReferenceEquals(_constructionPreviewOwner, miningBox) &&
                    _hasConstructionPreviewOrigin && _constructionPreviewOrigin == origin &&
                    ConstructionPreviewBoxes.Count == HeaterRangeCalculator.EffectiveCellCount)
                {
                    return;
                }

                HideConstructionPreview();

                var footprint = buildingManager.GetTileList(true, origin, buildInfo.GetSize());
                var center = FindFirstRowCenter(footprint);
                var coverage = CreateCoveragePoints(footprint, center);
                foreach (var point in coverage)
                {
                    var previewObject = poolManager.Pool_GreenBox.GetNextObj();
                    var greenBox = previewObject != null ? previewObject.GetComponent<GreenBox>() : null;
                    if (greenBox == null)
                    {
                        continue;
                    }

                    var activeBox = greenBox.G_BoxSet(
                        new Vector2(point.X, point.Y),
                        new Color(1f, 1f, 1f, 0.4f),
                        "None_GrayScale",
                        null);
                    if (activeBox != null)
                    {
                        ConstructionPreviewBoxes.Add(activeBox);
                    }
                }

                _constructionPreviewOwner = miningBox;
                _constructionPreviewOrigin = origin;
                _hasConstructionPreviewOrigin = true;
            }
            catch (Exception exception)
            {
                HideConstructionPreview();
                LogError("同步施工阶段加热器范围提示", exception);
            }
        }

        public static void HideConstructionPreview()
        {
            foreach (var greenBox in ConstructionPreviewBoxes)
            {
                if (greenBox == null)
                {
                    continue;
                }

                try
                {
                    greenBox.Off();
                }
                catch
                {
                    greenBox.gameObject.SetActive(false);
                }
            }

            ConstructionPreviewBoxes.Clear();
            _constructionPreviewOwner = null;
            _constructionPreviewOrigin = default(Vector2Int);
            _hasConstructionPreviewOrigin = false;
        }

        private static void HideLegacyConstructionPreview(MiningBox miningBox)
        {
            if (miningBox.m_RangeCircle != null)
            {
                miningBox.m_RangeCircle.gameObject.SetActive(false);
            }

            var waterTileCondition = MiningBoxWaterTileConditionField?.GetValue(miningBox) as Component;
            if (waterTileCondition != null)
            {
                waterTileCondition.gameObject.SetActive(false);
            }
        }

        public static void SuppressNonWinterPowerAlarm(Building_ElecBase building)
        {
            var heater = building as Building_Heater;
            if (AllowOriginalHeaterUpdate(heater))
            {
                return;
            }

            if (heater.m_BuildState != BuildState.NoElec &&
                heater.m_BuildState != BuildState.NoBattery)
            {
                return;
            }

            heater.m_BuildState = BuildState.Basic;
            heater.AlarmSet(BuildState.Basic);
        }

        public static void HideLegacyPreviewArea(Building_Heater heater)
        {
            if (!CanUseHeater(heater) || AreaObjectField == null)
            {
                return;
            }

            try
            {
                var area = AreaObjectField.GetValue(heater) as GameObject;
                area?.SetActive(false);
            }
            catch (Exception exception)
            {
                LogError("隐藏原版加热器范围底图", exception);
            }
        }

        public static void TickSafely(T_Queen queen)
        {
            try
            {
                if (!_enabled || _shuttingDown)
                {
                    return;
                }

                if (!_firstTickLogged)
                {
                    _firstTickLogged = true;
                    _logger?.LogInfo("加热器运行时补丁已首次调用，开始同步当前存档的加热器。");
                }

                var currentTime = Time.unscaledTime;
                if (currentTime < _nextRuntimeSyncAt)
                {
                    return;
                }

                _nextRuntimeSyncAt = currentTime + RuntimeSyncInterval;

                var game = GameMgr.Instance;
                var buildingManager = game != null ? game._BuildingMgr : null;
                var weatherManager = game != null ? game._WeatherMgr : null;
                if (queen == null || buildingManager == null || weatherManager == null ||
                    buildingManager.List_Building == null)
                {
                    ResetSession();
                    return;
                }

                if (!ReferenceEquals(_buildingManager, buildingManager))
                {
                    ResetSession();
                    _buildingManager = buildingManager;
                }

                var activeHeaterIds = new HashSet<int>();
                var heatersFound = 0;
                foreach (var building in buildingManager.List_Building)
                {
                    var heater = building as Building_Heater;
                    if (heater == null)
                    {
                        continue;
                    }

                    heatersFound++;
                    activeHeaterIds.Add(heater.m_ID);
                    EnsureHeaterTracked(heater);
                }

                RemoveMissingHeaters(activeHeaterIds);

                var seasonValue = (int)weatherManager.m_SeasonState;
                if (_lastSeasonValue != seasonValue)
                {
                    _lastSeasonValue = seasonValue;
                    _logger?.LogInfo(
                        $"加热器会话已同步：发现 {heatersFound} 个加热器，当前季节 {weatherManager.m_SeasonState}。");
                    ReapplyAllSeasonStates();
                }

                RemoveOrphanedHeatSystemBuffs(weatherManager.m_SeasonState, game._EnvMgr);
            }
            catch (Exception exception)
            {
                LogError("加热器运行时同步", exception);
            }
        }

        public static void DisableForNonWinter(Building_Heater heater)
        {
            if (heater == null || _shuttingDown)
            {
                return;
            }

            try
            {
                InvalidateEffectiveCoverage(heater.m_ID);
                var firstDisable = DisableTracker.BeginNonWinterDisable(heater.m_ID);
                if (firstDisable)
                {
                    ClearCurrentRange(heater);
                }

                heater.m_ElecNum = 0;

                if (firstDisable && heater.m_Body != null && heater.m_Body.m_Animator != null)
                {
                    heater.m_Body.m_Animator.Play("End");
                }

                if (heater.m_ElecWire != null && heater.m_ElecWire.Obj != null)
                {
                    heater.m_ElecWire.Obj.SetActive(false);
                }

                var game = GameMgr.Instance;
                if (game != null && game._BuildingMgr != null && heater.m_Info != null)
                {
                    game._BuildingMgr.ConnectUseBuild(heater.m_ID, -1, heater.m_Info.ElecCost);
                }

                SuppressNonWinterPowerAlarm(heater);
            }
            catch (Exception exception)
            {
                LogError("非冬季停用加热器", exception);
            }
        }

        public static void ReapplyAllSeasonStates()
        {
            if (!_enabled || _shuttingDown)
            {
                return;
            }

            var staleHeaterIds = new List<int>();
            var heaters = new List<Building_Heater>();
            foreach (var pair in HeaterStates)
            {
                if (pair.Value.Heater == null)
                {
                    staleHeaterIds.Add(pair.Key);
                }
                else
                {
                    heaters.Add(pair.Value.Heater);
                }
            }

            foreach (var heaterId in staleHeaterIds)
            {
                InvalidateEffectiveCoverage(heaterId);
                RemoveHeatSystemAt(Coverage.Unregister(heaterId));
                DisableTracker.Remove(heaterId);
                HeaterStates.Remove(heaterId);
            }

            foreach (var heater in heaters)
            {
                ApplySeasonState(heater);
            }

            var game = GameMgr.Instance;
            if (game != null && game._WeatherMgr != null)
            {
                RemoveOrphanedHeatSystemBuffs(game._WeatherMgr.m_SeasonState, game._EnvMgr);
            }
        }

        public static void OnBuildingDemolishing(Building building)
        {
            var heater = building as Building_Heater;
            if (heater == null)
            {
                return;
            }

            try
            {
                InvalidateEffectiveCoverage(heater.m_ID);
                var tracked = HeaterStates.ContainsKey(heater.m_ID);
                var uncovered = Coverage.Unregister(heater.m_ID);
                if (tracked)
                {
                    RemoveHeatSystemAt(uncovered);
                }
                else
                {
                    ClearCurrentRange(heater);
                }

                DisableTracker.Remove(heater.m_ID);
                HeaterStates.Remove(heater.m_ID);
            }
            catch (Exception exception)
            {
                LogError("拆除加热器", exception);
            }
        }

        public static void OnLoadingSceneStarting()
        {
            ResetSession();
        }

        public static void SanitizePlantSaveData(PlantData plantData)
        {
            if (plantData == null)
            {
                return;
            }

            PlantSaveBuffSanitizer.RemoveHeatSystemPairs(
                plantData.List_BuffName,
                plantData.List_BuffValue,
                HeatSystemBuffName);
        }

        public static void SanitizePlantLoadData(PlantData plantData)
        {
            SanitizePlantSaveData(plantData);
        }

        public static void Shutdown()
        {
            if (!_enabled)
            {
                return;
            }

            _shuttingDown = true;
            try
            {
                try
                {
                    HideConstructionPreview();
                }
                catch (Exception exception)
                {
                    LogError("卸载时回收施工范围提示", exception);
                }

                foreach (var state in HeaterStates.Values)
                {
                    RestoreHeaterOnShutdown(state);
                }
            }
            finally
            {
                Coverage.Clear();
                EffectiveCoverage.Clear();
                DisableTracker.Clear();
                HeaterStates.Clear();
                _buildingManager = null;
                _enabled = false;
                _shuttingDown = false;
                _firstTickLogged = false;
                _lastSeasonValue = int.MinValue;
                _orphanAuditDirty = false;
                _logger = null;
            }
        }

        private static void RestoreHeaterOnShutdown(HeaterState state)
        {
            if (state.Heater == null)
            {
                return;
            }

            try
            {
                ClearCurrentRange(state.Heater);
            }
            catch (Exception exception)
            {
                LogError($"卸载时清理加热器 {state.Heater.m_ID} 的扩展范围", exception);
            }

            try
            {
                SetLocalPositions(state.Heater, new List<Vector2Int>(state.VanillaPositions));
                state.Heater.Building_Update3();
            }
            catch (Exception exception)
            {
                LogError($"卸载时恢复加热器 {state.Heater.m_ID} 的原版范围", exception);
            }
        }

        private static void EnsureHeaterTracked(Building_Heater heater)
        {
            if (!CanUseHeater(heater))
            {
                return;
            }

            HeaterState state;
            if (HeaterStates.TryGetValue(heater.m_ID, out state) && ReferenceEquals(state.Heater, heater))
            {
                return;
            }

            InitializeHeater(heater);
        }

        private static void InitializeHeater(Building_Heater heater)
        {
            SynchronizeExtendedRange(heater);
            ApplySeasonState(heater);
        }

        private static void SynchronizeExtendedRange(Building_Heater heater)
        {
            HeaterState existingState;
            var alreadyTracked = HeaterStates.TryGetValue(heater.m_ID, out existingState) &&
                                 ReferenceEquals(existingState.Heater, heater);
            var vanillaPositions = alreadyTracked
                ? existingState.VanillaPositions
                : CopyLocalPositions(heater);

            if (!alreadyTracked)
            {
                ClearCurrentRange(heater);
            }

            RegisterExtendedRange(heater, vanillaPositions);
        }

        private static void RemoveMissingHeaters(ISet<int> activeHeaterIds)
        {
            var missingHeaterIds = new List<int>();
            foreach (var heaterId in HeaterStates.Keys)
            {
                if (!activeHeaterIds.Contains(heaterId))
                {
                    missingHeaterIds.Add(heaterId);
                }
            }

            foreach (var heaterId in missingHeaterIds)
            {
                InvalidateEffectiveCoverage(heaterId);
                RemoveHeatSystemAt(Coverage.Unregister(heaterId));
                DisableTracker.Remove(heaterId);
                HeaterStates.Remove(heaterId);
            }
        }

        private static void ResetSession()
        {
            HideConstructionPreview();
            Coverage.Clear();
            EffectiveCoverage.Clear();
            DisableTracker.Clear();
            HeaterStates.Clear();
            _buildingManager = null;
            _lastSeasonValue = int.MinValue;
            _nextRuntimeSyncAt = 0f;
            _orphanAuditDirty = true;
        }

        private static void SynchronizeEffectiveCoverage(Building_Heater heater)
        {
            SeasonState season;
            if (BlockedPositionField == null || !TryGetSeasonState(out season))
            {
                InvalidateEffectiveCoverage(heater.m_ID);
                return;
            }

            var effective = HeaterEffectiveCoverageCalculator.Create(
                ToGridPoints(GetLocalPositions(heater)),
                ToGridPoints(GetBlockedPositions(heater)),
                season == SeasonState.Winter,
                heater.m_Activation,
                heater.m_ElecNum == 1);
            if (effective.Count == 0)
            {
                InvalidateEffectiveCoverage(heater.m_ID);
                return;
            }

            var uncovered = EffectiveCoverage.Register(heater.m_ID, effective);
            if (uncovered.Count > 0)
            {
                _orphanAuditDirty = true;
            }
        }

        private static void InvalidateEffectiveCoverage(int heaterId)
        {
            var uncovered = EffectiveCoverage.Unregister(heaterId);
            if (uncovered.Count > 0)
            {
                _orphanAuditDirty = true;
            }
        }

        private static void RegisterExtendedRange(
            Building_Heater heater,
            IReadOnlyList<Vector2Int> vanillaPositions)
        {
            var firstRowCenter = FindFirstRowCenter(heater.List_BuildPos);
            var coverage = CreateCoveragePoints(heater, firstRowCenter);

            SetLocalPositions(heater, ToVector2Ints(coverage));
            RemoveHeatSystemAt(Coverage.Register(heater.m_ID, coverage));
            HeaterStates[heater.m_ID] = new HeaterState(
                heater,
                new List<Vector2Int>(vanillaPositions));

            _logger?.LogInfo(
                $"加热器范围已写入：ID={heater.m_ID}，有效格数={coverage.Count}，" +
                $"中心=({firstRowCenter.X},{firstRowCenter.Y})，" +
                $"左上=({firstRowCenter.X - 4},{firstRowCenter.Y})，" +
                $"右上=({firstRowCenter.X + 4},{firstRowCenter.Y})，" +
                $"左下=({firstRowCenter.X - 4},{firstRowCenter.Y - 4})，" +
                $"右下=({firstRowCenter.X + 4},{firstRowCenter.Y - 4})。");
        }

        private static void ApplySeasonState(Building_Heater heater)
        {
            if (heater == null)
            {
                return;
            }

            SeasonState season;
            if (!TryGetSeasonState(out season))
            {
                return;
            }

            if (season == SeasonState.Winter)
            {
                DisableTracker.MarkWinter(heater.m_ID);
                heater.Building_Update3();
            }
            else
            {
                DisableForNonWinter(heater);
            }
        }

        private static bool CanUseHeater(Building_Heater heater)
        {
            return _enabled && !_shuttingDown && heater != null && LocalPositionField != null;
        }

        private static bool TryGetSeasonState(out SeasonState season)
        {
            season = default(SeasonState);
            var game = GameMgr.Instance;
            if (game == null || game._WeatherMgr == null)
            {
                return false;
            }

            season = game._WeatherMgr.m_SeasonState;
            return true;
        }

        private static void ClearCurrentRange(Building_Heater heater)
        {
            var positions = GetLocalPositions(heater);
            if (positions != null && positions.Count > 0)
            {
                heater.BuildingWorkingStop(false);
            }
        }

        private static List<Vector2Int> CopyLocalPositions(Building_Heater heater)
        {
            var positions = GetLocalPositions(heater);
            return positions == null ? new List<Vector2Int>() : new List<Vector2Int>(positions);
        }

        private static List<Vector2Int> GetLocalPositions(Building_Heater heater)
        {
            return LocalPositionField.GetValue(heater) as List<Vector2Int>;
        }

        private static List<Vector2Int> GetBlockedPositions(Building_Heater heater)
        {
            return BlockedPositionField.GetValue(heater) as List<Vector2Int>;
        }

        private static void SetLocalPositions(Building_Heater heater, List<Vector2Int> positions)
        {
            LocalPositionField.SetValue(heater, positions);
        }

        private static List<Vector2Int> CreateCoverage(Building_Heater heater)
        {
            var center = FindFirstRowCenter(heater.List_BuildPos);
            return ToVector2Ints(CreateCoveragePoints(heater, center));
        }

        private static IReadOnlyList<GridPoint> CreateCoveragePoints(
            Building_Heater heater,
            GridPoint firstRowCenter)
        {
            return CreateCoveragePoints(heater.List_BuildPos, firstRowCenter);
        }

        private static IReadOnlyList<GridPoint> CreateCoveragePoints(
            IReadOnlyList<Vector2Int> footprint,
            GridPoint firstRowCenter)
        {
            return HeaterRangeCalculator.CreateCoverage(firstRowCenter, ToGridPoints(footprint));
        }

        private static void RemoveHeatSystemAt(IReadOnlyList<GridPoint> points)
        {
            if (points == null || points.Count == 0)
            {
                return;
            }

            var game = GameMgr.Instance;
            var tileManager = game != null ? game._TileMgr : null;
            if (tileManager == null)
            {
                return;
            }

            foreach (var point in points)
            {
                var plant = tileManager.GetPlant(new Vector2(point.X, point.Y));
                if (plant != null && plant.List_BuffName != null &&
                    plant.List_BuffName.Contains(HeatSystemBuffName))
                {
                    plant.RemoveBuff(HeatSystemBuffName);
                }
            }
        }

        private static void RemoveOrphanedHeatSystemBuffs(
            SeasonState season,
            EnvironmentMgr environmentManager)
        {
            if (!_orphanAuditDirty || environmentManager == null ||
                environmentManager.List_WorldObj == null)
            {
                return;
            }

            var removedCount = 0;
            foreach (var plant in environmentManager.List_WorldObj)
            {
                if (plant == null || plant.List_BuffName == null ||
                    !plant.List_BuffName.Contains(HeatSystemBuffName))
                {
                    continue;
                }

                if (season == SeasonState.Winter &&
                    EffectiveCoverage.CoversAny(ToGridPoints(plant.GetSizeRect())))
                {
                    continue;
                }

                plant.RemoveBuff(HeatSystemBuffName);
                removedCount++;
            }

            _orphanAuditDirty = false;
            if (removedCount > 0)
            {
                _logger?.LogInfo($"已清理 {removedCount} 个不再由有效加热器覆盖的 HeatSystem Buff。");
            }
        }

        private static GridPoint FindFirstRowCenter(IReadOnlyList<Vector2Int> footprint)
        {
            if (footprint == null || footprint.Count == 0)
            {
                return new GridPoint(0, 0);
            }

            var highestY = footprint[0].y;
            for (var index = 1; index < footprint.Count; index++)
            {
                if (footprint[index].y > highestY)
                {
                    highestY = footprint[index].y;
                }
            }

            var topRowX = new List<int>();
            for (var index = 0; index < footprint.Count; index++)
            {
                if (footprint[index].y == highestY)
                {
                    topRowX.Add(footprint[index].x);
                }
            }

            topRowX.Sort();
            return new GridPoint(topRowX[topRowX.Count / 2], highestY);
        }

        private static List<GridPoint> ToGridPoints(IReadOnlyList<Vector2Int> points)
        {
            var result = new List<GridPoint>();
            if (points == null)
            {
                return result;
            }

            foreach (var point in points)
            {
                result.Add(new GridPoint(point.x, point.y));
            }

            return result;
        }

        private static List<GridPoint> ToGridPoints(IReadOnlyList<Vector2> points)
        {
            var result = new List<GridPoint>();
            if (points == null)
            {
                return result;
            }

            foreach (var point in points)
            {
                result.Add(new GridPoint((int)point.x, (int)point.y));
            }

            return result;
        }

        private static List<Vector2Int> ToVector2Ints(IReadOnlyList<GridPoint> points)
        {
            var result = new List<Vector2Int>(points.Count);
            foreach (var point in points)
            {
                result.Add(new Vector2Int(point.X, point.Y));
            }

            return result;
        }

        private static void LogError(string operation, Exception exception)
        {
            _logger?.LogError($"{operation}失败：{exception}");
        }

        private sealed class HeaterState
        {
            public HeaterState(Building_Heater heater, List<Vector2Int> vanillaPositions)
            {
                Heater = heater;
                VanillaPositions = vanillaPositions;
            }

            public Building_Heater Heater { get; }

            public List<Vector2Int> VanillaPositions { get; }
        }
    }
}
