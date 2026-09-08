using System;
using BepInEx.Configuration;

namespace SpecialRatizens.Configuration
{
    /// <summary>
    /// BepInEx 配置项绑定。独立版只暴露当前已安装补丁真正读取的设置，
    /// 整合版遗留但未安装补丁的功能开关不再对外暴露。
    /// </summary>
    internal sealed class ModConfig
    {
        public ModConfig(ConfigFile config)
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("ModConfig has already been initialized.");
            }

            Enabled = config.Bind("General", "Enabled", true,
                "启用特殊鼠鼠的生成与特性效果。关闭时仍注册特性定义，以便读取已有存档。");
            OnlyGoodCharacteristic = config.Bind("Generation", "OnlyGoodCharacteristic", false,
                "移民候选全部为正面特性（仅影响特殊鼠鼠 mod 的候选生成流程）。");
            NewCitizenGenderLimit = config.Bind("Generation", "NewCitizenGenderLimit", -1,
                "移民性别限制：-1 不限制（原版行为），0 仅男性，1 仅女性。");

            Instance = this;
        }

        public static ModConfig Instance { get; private set; }

        /// <summary>启用特殊鼠鼠（原 CustomSettings.CustomSpecialUnit）。</summary>
        public ConfigEntry<bool> Enabled { get; }

        /// <summary>移民候选全正面特性（原 CustomSettings.OnlyGoodCharacteristic）。</summary>
        public ConfigEntry<bool> OnlyGoodCharacteristic { get; }

        /// <summary>移民性别限制（原 CustomSettings.NewCitizenGenderLimit）。</summary>
        public ConfigEntry<int> NewCitizenGenderLimit { get; }
    }
}
