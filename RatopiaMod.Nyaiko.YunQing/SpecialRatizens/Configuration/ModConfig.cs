using System;
using BepInEx.Configuration;

namespace SpecialRatizens.Configuration
{
    /// <summary>
    /// BepInEx 配置项绑定。独立版只暴露当前已安装补丁真正读取的设置，
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
            CustomNames = config.Bind("General", "CustomNames", true,
                "启用「更多名称」：移民与随机市民从 Data/Names.json 生成中文姓名，并避开当前存档已占用的名字。");

            Instance = this;
        }

        public static ModConfig Instance { get; private set; }

        /// <summary>启用特殊鼠鼠</summary>
        public ConfigEntry<bool> Enabled { get; }

        /// <summary>启用更多名称</summary>
        public ConfigEntry<bool> CustomNames { get; }
    }
}
