using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Logging;
using RatopiaMod.YunQing.All.MapEditor;
using RatopiaMod.YunQing.All.UI;
using UnityEngine;

namespace RatopiaMod.YunQing.All.Input
{
    internal sealed class HotkeyController : MonoBehaviour
    {
        private ModConfig _config;
        private ControlPanelGui _controlPanel;
        private MapEditorController _mapEditor;

        public void Initialize(ModConfig config, ControlPanelGui controlPanel, MapEditorController mapEditor)
        {
            _config = config;
            _controlPanel = controlPanel;
            _mapEditor = mapEditor;
            ModLog.Info("快捷键监听初始化完成。");
        }

        private void Update()
        {
            if (_config.GuiToggleKey.Value.IsDown())
            {
                ModLog.Debug($"检测到控制面板快捷键：{_config.GuiToggleKey.Value}。");
                _controlPanel.Toggle();
            }

            if (_config.MapEditorToggleKey.Value.IsDown())
            {
                ModLog.Debug($"检测到地形编辑器快捷键：{_config.MapEditorToggleKey.Value}。");
                _mapEditor.Toggle();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
            {
                ToggleCheatConsole();
            }
        }

        private static void ToggleCheatConsole()
        {
            var cheat = DebugMgr.Instance?._CheatMgr;
            if (cheat == null)
            {
                ModLog.Warn("按下 F3，但调试控制台管理器尚未初始化。");
                return;
            }

            var nextActive = !cheat.Obj_Canvas.activeSelf;
            cheat.SetActive(nextActive);
            ModLog.Info($"调试控制台已{(nextActive ? "打开" : "关闭")}。");
        }
    }
}

