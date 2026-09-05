using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using RatopiaMod.YunQing.All.Configuration;
using RatopiaMod.YunQing.All.Input;
using RatopiaMod.YunQing.All.Logging;
using RatopiaMod.YunQing.All.MapEditor;
using RatopiaMod.YunQing.All.UI;

namespace RatopiaMod.YunQing.All
{
    [BepInPlugin(PluginConstants.Guid, PluginConstants.Name, PluginConstants.Version)]
    public sealed class YunQingAll : BaseUnityPlugin
    {
        private Harmony _harmony;
        private ControlPanelGui _controlPanel;
        private MapEditorController _mapEditor;
        private HotkeyController _hotkeys;

        private void Awake()
        {
            ModLog.Initialize(Logger);
            ModLog.Info($"{PluginConstants.Name} 初始化开始，版本：{PluginConstants.Version}");

            var config = new ModConfig(Config);

            _controlPanel = gameObject.AddComponent<ControlPanelGui>();
            _mapEditor = gameObject.AddComponent<MapEditorController>();
            _hotkeys = gameObject.AddComponent<HotkeyController>();

            _controlPanel.Initialize(config);
            _mapEditor.Initialize(_controlPanel);
            _hotkeys.Initialize(config, _controlPanel, _mapEditor);
            ModLog.Info("功能组件初始化完成：控制面板、地形编辑器、快捷键监听。");
        }

        private void Start()
        {
            ModLog.Info($"{PluginConstants.Name}插件已加载。版本：{PluginConstants.Version}");

            _harmony = new Harmony(PluginConstants.Guid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            var patchedMethodCount = _harmony.GetPatchedMethods().Count();
            ModLog.Info($"Harmony 补丁注册完成，目标方法数量：{patchedMethodCount}。");
        }

        private void OnDestroy()
        {
            ModLog.Info($"{PluginConstants.Name} 正在卸载，开始移除 Harmony 补丁。");
            _harmony?.UnpatchSelf();
            ModLog.Info($"{PluginConstants.Name} Harmony 补丁移除完成。");
        }
    }
}
