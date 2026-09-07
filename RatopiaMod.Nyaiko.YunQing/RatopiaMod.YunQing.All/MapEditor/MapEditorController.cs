using RatopiaMod.YunQing.All.Logging;
using RatopiaMod.YunQing.All.UI;
using UnityEngine;

namespace RatopiaMod.YunQing.All.MapEditor
{
    internal sealed class MapEditorController : MonoBehaviour
    {
        private const float EditorCameraZoom = 20f;

        private ControlPanelGui _controlPanel;
        private bool _mapEditorMode;
        private float _savedCameraZoom = EditorCameraZoom;

        public void Initialize(ControlPanelGui controlPanel)
        {
            _controlPanel = controlPanel;
            ModLog.Info("地形编辑器控制器初始化完成。");
        }

        public void Toggle()
        {
            SetMapEditorMode(!_mapEditorMode);
        }

        private void SetMapEditorMode(bool value)
        {
            if (value && GameMenuIsActive)
            {
                ModLog.Warn("游戏菜单处于打开状态，无法启动地形编辑器。");
                return;
            }

            _mapEditorMode = value;
            ModLog.Info($"地形编辑器已{(value ? "开启" : "关闭")}。");

            if (value)
            {
                _controlPanel.Hide();
                ApplyEditorCameraZoom();
            }
            else
            {
                RestoreCameraZoom();
            }

            Time.timeScale = value ? 0.3f : 1f;
            SetPallateActive(value);
        }

        private void ApplyEditorCameraZoom()
        {
            var cameraManager = GameMgr.Instance?._CamMgr;
            if (cameraManager?.m_MainCam == null)
            {
                ModLog.Warn("未找到主摄像机，地形编辑器无法调整缩放。");
                return;
            }

            _savedCameraZoom = cameraManager.m_MainCam.orthographicSize;
            cameraManager.ZoomSizeUpdate(EditorCameraZoom);
            ModLog.Debug($"地形编辑器相机缩放已调整为 {EditorCameraZoom}，原缩放为 {_savedCameraZoom}。");
        }

        private void RestoreCameraZoom()
        {
            var cameraManager = GameMgr.Instance?._CamMgr;
            if (cameraManager == null)
            {
                ModLog.Warn("未找到相机管理器，无法恢复地形编辑器前的缩放。");
                return;
            }

            cameraManager.ZoomSizeUpdate(_savedCameraZoom);
            ModLog.Debug($"相机缩放已恢复为 {_savedCameraZoom}。");
        }

        private static bool GameMenuIsActive =>
            Utility.UI.GameMenuMgr.Instance != null && Utility.UI.GameMenuMgr.Instance.IsActivate;

        private static PallateMgr PallateMgr => DebugMgr.Instance?._PallateMgr;

        private static void SetPallateActive(bool active)
        {
            var tileManager = GameMgr.Instance?._TileMgr;
            if (tileManager != null)
            {
                tileManager.IsSandBoxMode = active;
            }
            else
            {
                ModLog.Warn("未找到地块管理器，沙盒模式未修改。");
            }

            var pallate = PallateMgr;
            if (pallate == null)
            {
                ModLog.Warn("未找到地形编辑器面板，面板开关未修改。");
                return;
            }

            if (!active)
            {
                foreach (var icon in pallate.m_Icons)
                {
                    if (icon.m_Outline.enabled)
                    {
                        icon.MouseUp();
                    }
                }

                pallate.m_BrushType = 0;
            }

            pallate.Obj_Main.SetActive(active);
            ModLog.Debug($"地形编辑器面板已{(active ? "显示" : "隐藏")}。");
        }
    }
}
