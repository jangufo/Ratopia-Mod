using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MultipurposeResearch.Core;
using MultipurposeResearch.Runtime;

namespace MultipurposeResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "cn.ratopia.multipurposeresearch";
        public const string PluginName = "多用途研究点";
        public const string PluginVersion = "0.1.11";

        internal static Plugin Instance { get; private set; }
        internal static MultipurposeResearchConfig Settings { get; private set; }
        internal static MultipurposeResearchState State { get; set; } = MultipurposeResearchState.Empty;

        private Harmony _harmony;
        private bool _enabled;
        private bool _malformedWarningLogged;

        private void Awake()
        {
            Instance = this;
            Settings = BindConfig();
            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                _enabled = true;
                Logger.LogInfo($"{PluginName} v{PluginVersion} 已加载；将在原版研究分类选择界面追加多用途研究点卡片。");
            }
            catch (Exception error)
            {
                _harmony?.UnpatchSelf();
                _harmony = null;
                _enabled = false;
                Logger.LogError($"Harmony 补丁安装失败，已撤销本 Mod：{error}");
            }
        }

        private void OnDestroy()
        {
            _enabled = false;
            ResearchUiRuntime.Cleanup();
            _harmony?.UnpatchSelf();
            _harmony = null;
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        internal static void BeginGameSession()
        {
            var plugin = Instance;
            if (plugin == null || !plugin._enabled)
            {
                return;
            }

            try
            {
                State = SaveStateStore.LoadCurrent(out var malformed);
                if (malformed && !plugin._malformedWarningLogged)
                {
                    plugin.Logger.LogWarning("当前存档的多用途研究点数据无效，已回退为空状态。");
                    plugin._malformedWarningLogged = true;
                }
                else if (!malformed)
                {
                    plugin._malformedWarningLogged = false;
                }

                CitizenProductivityRuntime.ApplyCurrentToAll();
                plugin.Logger.LogInfo(
                    $"已载入多用途研究点状态：全民动员={State.ProductivityPercent:P0}，" +
                    $"女王加成={State.QueenPowerBonus}/{State.QueenDexterityBonus}/{State.QueenIntelligenceBonus}。");
            }
            catch (Exception error)
            {
                State = MultipurposeResearchState.Empty;
                plugin.Logger.LogError($"读取多用途研究点存档数据失败，已回退空状态：{error}");
            }
        }

        internal static void ResetGameSession()
        {
            State = MultipurposeResearchState.Empty;
            ResearchUiRuntime.Cleanup();
            if (Instance != null)
            {
                Instance._malformedWarningLogged = false;
            }
        }

        internal static bool TrySaveState(MultipurposeResearchState state)
        {
            if (!SaveStateStore.TrySaveCurrent(state))
            {
                return false;
            }

            State = state;
            return true;
        }

        internal static void LogInfo(string message)
        {
            Instance?.Logger.LogInfo(message);
        }

        internal static void LogWarning(string message)
        {
            Instance?.Logger.LogWarning(message);
        }

        internal static void LogError(string message, Exception error)
        {
            Instance?.Logger.LogError($"{message} {error}");
        }

        private MultipurposeResearchConfig BindConfig()
        {
            var defaults = MultipurposeResearchConfig.Default;
            var productivityCost = Config.Bind(
                "Costs", "ProductivityCost", defaults.ProductivityCost, "全民动员研究点成本。");
            var saleCost = Config.Bind(
                "Costs", "SaleCost", defaults.SaleCost, "学术出口研究点成本。");
            var queenBaseCost = Config.Bind(
                "Queen", "BaseCost", defaults.QueenUpgradeBaseCost, "女王进修基础成本。");
            var queenCostStep = Config.Bind(
                "Queen", "CostStep", defaults.QueenUpgradeCostStep, "女王进修每次递增成本；0 表示固定成本。");
            var costDefaultsVersion = Config.Bind(
                "Compatibility", "CostDefaultsVersion", 0, "成本默认值迁移版本，请勿手动修改。");
            var config = new MultipurposeResearchConfig(
                productivityCost.Value,
                BindInt("Productivity", "DurationDays", defaults.ProductivityDurationDays, "全民动员持续游戏日。"),
                BindFloat("Productivity", "Percent", defaults.ProductivityPercent, "全民动员工作效率加成，0.10 表示 10%。"),
                saleCost.Value,
                BindInt("Sale", "BaseIncome", defaults.SaleBaseIncome, "学术出口收入基数。"),
                BindInt("Sale", "RelationsReward", defaults.SaleRelationsReward, "学术出口购买国关系奖励。"),
                BindInt("Sale", "ProsperityReward", defaults.SaleProsperityReward, "学术出口购买国繁荣值奖励。"),
                BindFloat("Sale", "RelationsCoefficient", defaults.SaleRelationsCoefficient, "收入关系倍率系数。"),
                BindFloat("Sale", "ProsperityCoefficient", defaults.SaleProsperityCoefficient, "收入繁荣倍率系数。"),
                BindFloat("Sale", "MinimumMultiplier", defaults.SaleMinimumMultiplier, "收入最低倍率。"),
                BindFloat("Sale", "MaximumMultiplier", defaults.SaleMaximumMultiplier, "收入最高倍率。"),
                queenBaseCost.Value,
                queenCostStep.Value,
                BindInt("Queen", "AttributeIncrease", defaults.QueenAttributeIncrease, "女王每次属性增长。"));

            if (costDefaultsVersion.Value < 1)
            {
                config = MultipurposeResearchConfig.MigrateLegacyCostDefaults(config);
                productivityCost.Value = config.ProductivityCost;
                saleCost.Value = config.SaleCost;
                queenBaseCost.Value = config.QueenUpgradeBaseCost;
                queenCostStep.Value = config.QueenUpgradeCostStep;
                costDefaultsVersion.Value = 1;
                Config.Save();
                Logger.LogInfo("已将旧版默认研究点成本迁移为每项 50，女王进修改为固定成本。");
            }

            return MultipurposeResearchConfig.Normalize(config);
        }

        private int BindInt(string section, string key, int fallback, string description)
        {
            return Config.Bind(section, key, fallback, description).Value;
        }

        private float BindFloat(string section, string key, float fallback, string description)
        {
            return Config.Bind(section, key, fallback, description).Value;
        }
    }
}
