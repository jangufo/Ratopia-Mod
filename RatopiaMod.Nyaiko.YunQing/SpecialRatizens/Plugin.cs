using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RatopiaMod;
using SpecialRatizens.Configuration;
using SpecialRatizens.Core;
using SpecialRatizens.Patching;

namespace SpecialRatizens
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "cn.ratopia.specialratizens";
        public const string PluginName = "特殊鼠鼠";
        public const string PluginVersion = "0.1.5";

        private Harmony _harmony;
        private bool _patchingSucceeded;

        internal static Plugin Instance { get; private set; }

        internal static bool Enabled => ModConfig.Instance != null && ModConfig.Instance.Enabled.Value;

        private void Awake()
        {
            Instance = this;
            ModLog.Initialize(Logger);

            try
            {
                new ModConfig(Config);

                var dataRoot = PluginDataPaths.ResolveDataRoot();
                var catalog = SpecialDataCatalog.Load(dataRoot);

                CustomMOD.ConfigureSpecialRatizens(dataRoot, catalog);
                _harmony = new Harmony(PluginGuid);
                PatchRegistry.InstallAll(_harmony);
                _patchingSucceeded = true;
                ModLog.Info(
                    $"{PluginName} v{PluginVersion} 已加载：{catalog.Ratizens.Count} 名特殊鼠鼠、{catalog.Traits.Count} 个特性；" +
                    $"功能当前{(Enabled ? "开启" : "关闭")}。");
            }
            catch (Exception error)
            {
                _patchingSucceeded = false;
                _harmony?.UnpatchSelf();
                CustomMOD.ResetSpecialRatizensSession();
                ModLog.Error($"特殊鼠鼠初始化失败，已回滚全部补丁并停用：{error}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                CustomMOD.ResetSpecialRatizensSession();
            }
            catch (Exception error)
            {
                ModLog.Warn($"清理特殊鼠鼠运行时状态失败：{error}");
            }

            _harmony?.UnpatchSelf();
            _patchingSucceeded = false;
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        internal static void RunSafely(string operation, Action action)
        {
            var plugin = Instance;
            if (plugin == null || !plugin._patchingSucceeded)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception error)
            {
                ModLog.Error($"特殊鼠鼠补丁 {operation} 执行失败，已隔离异常：{error}");
            }
        }

        internal static void LogPatchError(string operation, Exception error)
        {
            ModLog.Error($"特殊鼠鼠补丁 {operation} 执行失败，已回退原版行为：{error}");
        }
    }
}
