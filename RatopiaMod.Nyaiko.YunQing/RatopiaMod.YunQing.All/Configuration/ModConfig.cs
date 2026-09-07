using System;
using BepInEx.Configuration;
using RatopiaMod.YunQing.All.Domain;
using RatopiaMod.YunQing.All.Logging;
using UnityEngine;

namespace RatopiaMod.YunQing.All.Configuration
{
    internal sealed class ModConfig
    {
        public ModConfig(ConfigFile config)
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("ModConfig has already been initialized.");
            }

            FishDrowningEnabled = config.Bind("Common", "IsActiveFishDrownInTheWater", true,
                "鱼会自行淹死在水里功能：默认开启");
            ExchangeRateModeSetting = config.Bind("Common", "CustomExchangeRateMode", ExchangeRateMode.COMMON,
                "自定义汇率劵功能\n【正汇率|正汇率最大值|官方正常值|负汇率|负汇率最大值】五种值选1，默认官方正常值");
            BankExchangeMultiplierSetting = config.Bind("Common", "BankExchangeMultiplier",
                BankExchangeMultiplier.X1, "银行兑换倍数：x1(默认) | x10 | x100 | x500");
            GuiToggleKey = config.Bind("GUI", "GuiToggleKey", new KeyboardShortcut(KeyCode.F9),
                "控制面板开关快捷键（默认F9）");
            MapEditorToggleKey = config.Bind("GUI", "MapEditorToggleKey", new KeyboardShortcut(KeyCode.F4),
                "地形编辑器开关快捷键（默认F4）");
            WindowPosX = config.Bind("GUI", "WindowPosX", 900f, "控制面板窗口X坐标");
            WindowPosY = config.Bind("GUI", "WindowPosY", 900f, "控制面板窗口Y坐标");
            WindowWidth = config.Bind("GUI", "WindowWidth", 714f, "控制面板窗口宽度");
            WindowHeight = config.Bind("GUI", "WindowHeight", 612f, "控制面板窗口高度");

            Instance = this;
            ModLog.Info("配置项绑定完成。");
        }

        public static ModConfig Instance { get; private set; }

        public ConfigEntry<bool> FishDrowningEnabled { get; }
        public ConfigEntry<ExchangeRateMode> ExchangeRateModeSetting { get; }
        public ConfigEntry<BankExchangeMultiplier> BankExchangeMultiplierSetting { get; }
        public ConfigEntry<KeyboardShortcut> GuiToggleKey { get; }
        public ConfigEntry<KeyboardShortcut> MapEditorToggleKey { get; }
        public ConfigEntry<float> WindowPosX { get; }
        public ConfigEntry<float> WindowPosY { get; }
        public ConfigEntry<float> WindowWidth { get; }
        public ConfigEntry<float> WindowHeight { get; }
    }
}

