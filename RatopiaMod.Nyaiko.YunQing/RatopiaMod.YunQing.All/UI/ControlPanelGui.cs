using System;
using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Domain;
using RatopiaMod.YunQing.All.Logging;
using UnityEngine;

namespace RatopiaMod.YunQing.All.UI
{
    internal sealed class ControlPanelGui : MonoBehaviour
    {
        private ModConfig _config;
        private bool _showGui;
        private Rect _windowRect;

        public void Initialize(ModConfig config)
        {
            _config = config;
            _windowRect = new Rect(config.WindowPosX.Value, config.WindowPosY.Value,
                config.WindowWidth.Value, config.WindowHeight.Value);
            ModLog.Info("控制面板初始化完成。");
        }

        public void Toggle()
        {
            _showGui = !_showGui;
            ModLog.Info(_showGui ? "控制面板已打开。" : "控制面板已关闭。");
        }

        public void Hide()
        {
            if (!_showGui)
            {
                return;
            }

            _showGui = false;
            ModLog.Info("控制面板已因打开其他界面自动关闭。");
        }

        private void OnGUI()
        {
            if (!_showGui)
            {
                return;
            }

            _windowRect = GUILayout.Window(0, _windowRect, DrawWindow,
                $"YunQing Mod 控制面板 (v{PluginConstants.Version})");
            SaveWindowRectIfChanged();
        }

        private void SaveWindowRectIfChanged()
        {
            if (Math.Abs(_windowRect.x - _config.WindowPosX.Value) > 0.5f ||
                Math.Abs(_windowRect.y - _config.WindowPosY.Value) > 0.5f ||
                Math.Abs(_windowRect.width - _config.WindowWidth.Value) > 0.5f ||
                Math.Abs(_windowRect.height - _config.WindowHeight.Value) > 0.5f)
            {
                _config.WindowPosX.Value = _windowRect.x;
                _config.WindowPosY.Value = _windowRect.y;
                _config.WindowWidth.Value = _windowRect.width;
                _config.WindowHeight.Value = _windowRect.height;
                ModLog.Debug($"控制面板窗口位置已保存：({_windowRect.x:F1}, {_windowRect.y:F1})，" +
                             $"大小：({_windowRect.width:F1} x {_windowRect.height:F1})。");
            }
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            DrawFishDrowningSection();
            DrawExchangeRateSection();
            DrawBankExchangeSection();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawFishDrowningSection()
        {
            GUILayout.Space(10);
            GUILayout.Label("━━━ 鱼淹死在水里功能 ━━━");

            GUILayout.BeginHorizontal();
            GUILayout.Label("开关控制：");
            if (GUILayout.Button(_config.FishDrowningEnabled.Value ? "◉ 关闭" : "○ 打开", GUILayout.Width(100)))
            {
                _config.FishDrowningEnabled.Value = !_config.FishDrowningEnabled.Value;
                ModLog.Info($"鱼淹死功能已{(_config.FishDrowningEnabled.Value ? "开启" : "关闭")}。");
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"   配置项当前状态：{(_config.FishDrowningEnabled.Value ? "打开" : "关闭")}");
        }

        private void DrawExchangeRateSection()
        {
            GUILayout.Space(15);
            GUILayout.Label("━━━ 手动控制汇率券 ━━━");

            GUILayout.BeginHorizontal();
            DrawExchangeRateButton("正汇率", ExchangeRateMode.POSITIVE);
            DrawExchangeRateButton("正汇率最大", ExchangeRateMode.POSITIVE_MAX);
            DrawExchangeRateButton("官方正常值", ExchangeRateMode.COMMON);
            DrawExchangeRateButton("负汇率", ExchangeRateMode.NEGATIVE);
            DrawExchangeRateButton("负汇率最大", ExchangeRateMode.NEGATIVE_MAX);
            GUILayout.EndHorizontal();

            GUILayout.Label($"   配置项当前状态：{GetExchangeRateModeLabel(_config.ExchangeRateModeSetting.Value)}");
        }

        private void DrawBankExchangeSection()
        {
            GUILayout.Space(15);
            GUILayout.Label("━━━ 银行兑换倍数 ━━━");

            GUILayout.BeginHorizontal();
            DrawBankExchangeButton("x1", BankExchangeMultiplier.X1);
            DrawBankExchangeButton("x10", BankExchangeMultiplier.X10);
            DrawBankExchangeButton("x100", BankExchangeMultiplier.X100);
            DrawBankExchangeButton("x500", BankExchangeMultiplier.X500);
            GUILayout.EndHorizontal();

            GUILayout.Label($"   配置项当前状态：x{(int)_config.BankExchangeMultiplierSetting.Value}");
        }

        private void DrawExchangeRateButton(string label, ExchangeRateMode mode)
        {
            if (GUILayout.Button(label, GetSelectedButtonStyle(_config.ExchangeRateModeSetting.Value == mode)))
            {
                _config.ExchangeRateModeSetting.Value = mode;
                ModLog.Info($"汇率券模式已切换为：{GetExchangeRateModeLabel(mode)}。");
            }
        }

        private void DrawBankExchangeButton(string label, BankExchangeMultiplier multiplier)
        {
            if (GUILayout.Button(label, GetSelectedButtonStyle(_config.BankExchangeMultiplierSetting.Value == multiplier)))
            {
                _config.BankExchangeMultiplierSetting.Value = multiplier;
                ModLog.Info($"银行兑换倍数已切换为：x{(int)multiplier}。");
            }
        }

        private static string GetExchangeRateModeLabel(ExchangeRateMode mode)
        {
            switch (mode)
            {
                case ExchangeRateMode.POSITIVE:
                    return "正汇率";
                case ExchangeRateMode.POSITIVE_MAX:
                    return "正汇率最大值";
                case ExchangeRateMode.COMMON:
                    return "官方正常值";
                case ExchangeRateMode.NEGATIVE:
                    return "负汇率";
                case ExchangeRateMode.NEGATIVE_MAX:
                    return "负汇率最大值";
                default:
                    return "未知";
            }
        }

        private static GUIStyle GetSelectedButtonStyle(bool selected)
        {
            var style = new GUIStyle(GUI.skin.button);
            if (selected)
            {
                style.normal.textColor = Color.green;
            }

            return style;
        }
    }
}
