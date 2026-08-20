using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using HeaterEnhancement.Patches;
using HeaterEnhancement.Runtime;
using UnityEngine;

namespace HeaterEnhancement
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "cn.ratopia.heaterenhancement";
        public const string PluginName = "加热器加强优化";
        public const string PluginVersion = "0.1.3";

        private static readonly IReadOnlyList<Type> PatchTypes = new[]
        {
            typeof(LoadingSceneStartPatch),
            typeof(HeaterBuildingSetPatch),
            typeof(HeaterLoadPatch),
            typeof(HeaterRuntimeTickPatch),
            typeof(HeaterBuildingUpdatePatch),
            typeof(HeaterWorkingUpdatePatch),
            typeof(HeaterWorkingStopPatch),
            typeof(HeaterWireCheckPatch),
            typeof(HeaterPreviewPatch),
            typeof(HeaterConstructionPreviewPatch),
            typeof(HeaterConstructionPreviewCleanupPatch),
            typeof(HeaterActivateCheckPatch),
            typeof(WeatherSeasonStatePatch),
            typeof(ElectricityRefreshPatch),
            typeof(BuildingDemolitionPatch),
            typeof(PlantDataSavePatch),
            typeof(PlantLoadPatch)
        };

        private Harmony _harmony;

        private void Awake()
        {
            gameObject.hideFlags |= HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            HeaterRuntime.Configure(Logger);
            _harmony = new Harmony(PluginGuid);

            try
            {
                foreach (var patchType in PatchTypes)
                {
                    Logger.LogDebug($"正在安装补丁：{patchType.Name}");
                    _harmony.CreateClassProcessor(patchType).Patch();
                }

                Logger.LogInfo($"{PluginName} {PluginVersion} 已启用，共安装 {PatchTypes.Count} 个补丁。");
            }
            catch (Exception exception)
            {
                Logger.LogError($"补丁安装失败，正在撤销全部修改并停用功能：{exception}");
                TryShutdownRuntime();
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }

        private void OnDestroy()
        {
            TryShutdownRuntime();
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception exception)
            {
                Logger.LogError($"解除 Harmony 补丁时发生异常：{exception}");
            }
            finally
            {
                _harmony = null;
            }
        }

        private void TryShutdownRuntime()
        {
            try
            {
                HeaterRuntime.Shutdown();
            }
            catch (Exception exception)
            {
                Logger.LogError($"清理加热器扩展范围时发生异常：{exception}");
            }
        }
    }
}
