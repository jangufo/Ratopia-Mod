using System;
using System.Collections.Generic;
using System.Linq;
using CasselGames.Component;
using CasselGames.Diplomatic.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MultipurposeResearch.Core;

namespace MultipurposeResearch.Runtime
{
    internal static class ResearchUiRuntime
    {
        private const string CustomContentRootName = "MultipurposeResearch.Content";
        private const string CustomDetailRootName = "MultipurposeResearch.Detail";
        private const int CountryColumnCount = 6;
        private const float CountryColumnSpacing = 150f;
        private const float CountryRowSpacing = 160f;
        private static ResearchUI _research;
        private static readonly List<GameObject> Nodes = new List<GameObject>();
        private static RectTransform _customContentRoot;
        private static CustomDetailView _customDetailView;
        private static NodeAction _selectedAction;
        private static string _selectedCountryKey;
        private static bool _selected;
        private static bool _customActive;
        private static bool _reassertingCustomView;
        private static bool _originalContentCaptured;
        private static bool _originalContentActive;
        private static bool _originalDetailCaptured;
        private static bool _originalDetailActive;
        private static bool _originalCategoryGroupCaptured;
        private static bool _originalCategoryGroupActive;
        private static ResearchListUI _researchList;
        private static GameObject _researchListEntryObject;
        private static bool _researchListSetLogged;

        internal static void AttachResearchListEntry(ResearchListUI researchList)
        {
            if (researchList == null)
            {
                return;
            }

            _researchList = researchList;
            if (!_researchListSetLogged)
            {
                _researchListSetLogged = true;
                Plugin.LogInfo($"首次捕获研究分类选择界面：原版卡片数组={researchList.m_CategorySlots?.Length ?? 0}，当前列表={researchList.List_Slots?.Count ?? 0}，选择界面={(researchList.Obj_TypeSelectUI != null && researchList.Obj_TypeSelectUI.activeSelf ? "显示" : "隐藏")}。");
            }
            if (_researchListEntryObject != null)
            {
                var existingSlot = _researchListEntryObject.GetComponent<ResearchCategorySlot>();
                if (existingSlot != null && researchList.List_Slots != null &&
                    !researchList.List_Slots.Contains(existingSlot))
                {
                    researchList.List_Slots.Add(existingSlot);
                }

                _researchListEntryObject.SetActive(researchList.Obj_TypeSelectUI != null &&
                    researchList.Obj_TypeSelectUI.activeSelf);
                RefreshResearchListEntryVisuals(_researchListEntryObject);
                return;
            }

            var activeSlots = researchList.m_CategorySlots == null
                ? new ResearchCategorySlot[0]
                : researchList.m_CategorySlots
                    .Where(slot => slot != null && slot.gameObject != null &&
                        slot.gameObject.activeSelf && !IsResearchListEntry(slot))
                    .ToArray();
            var source = activeSlots.FirstOrDefault() ??
                (researchList.m_CategorySlots == null
                    ? null
                    : researchList.m_CategorySlots.FirstOrDefault(slot => slot != null && slot.gameObject != null));
            if (source == null)
            {
                Plugin.LogWarning("未找到研究分类卡片模板，暂不添加多用途研究点入口。");
                return;
            }

            _researchListEntryObject = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
            _researchListEntryObject.name = "MultipurposeResearch.ResearchListEntry";
            _researchListEntryObject.SetActive(true);

            var slot = _researchListEntryObject.GetComponent<ResearchCategorySlot>();
            if (slot != null)
            {
                if (slot.Obj_Highlight != null)
                {
                    slot.Obj_Highlight.SetActive(false);
                }
            }

            _researchListEntryObject.AddComponent<MultipurposeResearchListEntry>();
            if (slot != null && researchList.List_Slots != null)
            {
                researchList.List_Slots.Add(slot);
            }

            RefreshResearchListEntryVisuals(_researchListEntryObject);

            var entryButton = _researchListEntryObject.GetComponent<Button>() ??
                _researchListEntryObject.AddComponent<Button>();
            entryButton.onClick.RemoveAllListeners();
            entryButton.onClick.AddListener(OpenCustomFromResearchList);
            if (entryButton.targetGraphic == null && slot != null && slot.Img_Back != null)
            {
                entryButton.targetGraphic = slot.Img_Back;
            }

            var lastVisible = activeSlots.LastOrDefault();
            if (lastVisible != null)
            {
                _researchListEntryObject.transform.SetSiblingIndex(lastVisible.transform.GetSiblingIndex() + 1);
                var entryRect = _researchListEntryObject.transform as RectTransform;
                var lastRect = lastVisible.transform as RectTransform;
                if (entryRect != null && lastRect != null)
                {
                    var spacing = GetResearchListEntrySpacing(researchList, activeSlots, lastRect);
                    if (spacing > 1f)
                    {
                        entryRect.anchoredPosition = lastRect.anchoredPosition + new Vector2(spacing, 0f);
                    }
                }
            }

            Plugin.LogInfo($"已创建研究分类入口：当前可见原版卡片 {activeSlots.Length} 张，多用途研究点入口已加入卡片栏。");
        }

        private static float GetResearchListEntrySpacing(
            ResearchListUI researchList,
            ResearchCategorySlot[] activeSlots,
            RectTransform lastRect)
        {
            if (activeSlots != null && activeSlots.Length > 1)
            {
                var previousRect = activeSlots[activeSlots.Length - 2].transform as RectTransform;
                if (previousRect != null)
                {
                    var activeSpacing = Mathf.Abs(
                        lastRect.anchoredPosition.x - previousRect.anchoredPosition.x);
                    if (activeSpacing > 1f)
                    {
                        return activeSpacing;
                    }
                }
            }

            var closestSpacing = float.MaxValue;
            foreach (var candidate in researchList?.m_CategorySlots ?? new ResearchCategorySlot[0])
            {
                var candidateRect = candidate?.transform as RectTransform;
                if (candidateRect == null || candidateRect == lastRect)
                {
                    continue;
                }

                var candidateSpacing = Mathf.Abs(
                    candidateRect.anchoredPosition.x - lastRect.anchoredPosition.x);
                if (candidateSpacing > 1f && candidateSpacing < closestSpacing)
                {
                    closestSpacing = candidateSpacing;
                }
            }

            if (closestSpacing < float.MaxValue)
            {
                return closestSpacing;
            }

            return Mathf.Max(240f, Mathf.Abs(lastRect.rect.width) + 20f);
        }

        private static void RefreshResearchListEntryVisuals(GameObject entryObject)
        {
            if (entryObject == null)
            {
                return;
            }

            var labels = entryObject.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                if (label != null)
                {
                    label.text = "多用途研究点";
                    label.enableAutoSizing = true;
                }
            }
        }

        internal static void OpenCustomFromResearchList()
        {
            try
            {
                if (_researchList != null)
                {
                    _researchList.ResearchListtUI_Set(false);
                }

                var research = GameMgr.Instance?._ResearchUI;
                if (research == null)
                {
                    Plugin.LogWarning("点击多用途研究点入口时未找到 ResearchUI。");
                    return;
                }

                research.Research_IconBtn(false);
                EnterCustom(research);
            }
            catch (Exception error)
            {
                Plugin.LogError("打开多用途研究点分类失败。", error);
            }
        }

        internal static bool IsResearchListEntry(ResearchCategorySlot slot)
        {
            return slot != null && slot.GetComponent<MultipurposeResearchListEntry>() != null;
        }

        internal static bool TryOpenSelectedResearchListEntry(ResearchListUI researchList)
        {
            if (researchList?.List_Slots == null || researchList.m_CategoryIndex < 0 ||
                researchList.m_CategoryIndex >= researchList.List_Slots.Count)
            {
                return false;
            }

            if (!IsResearchListEntry(researchList.List_Slots[researchList.m_CategoryIndex]))
            {
                return false;
            }

            OpenCustomFromResearchList();
            return true;
        }

        internal static void EnterCustom(ResearchUI research)
        {
            if (research == null)
            {
                return;
            }

            var isSameCustomSession = _customActive && ReferenceEquals(_research, research);
            _research = research;
            if (!isSameCustomSession)
            {
                CaptureAndHideVanillaResearch(research);
            }
            _customActive = true;
            HideVanillaResearchRoots(research);
            _selected = false;
            _selectedCountryKey = null;
            ClearNodes();
            GetOrCreateCustomContentRoot(research);
            GetOrCreateCustomDetailView(research);

            CreateProductivityNode(research);
            CreateQueenNode(research, QueenUpgrade.Power, "女王力量", 0);
            CreateQueenNode(research, QueenUpgrade.Intelligence, "女王智慧", 1);
            CreateQueenNode(research, QueenUpgrade.Dexterity, "女王敏捷", 2);

            var countries = DiplomaticSaleRuntime.GetAvailableCountries();
            var countryIndex = 0;
            foreach (var country in countries)
            {
                CreateCountryNode(research, country, countryIndex++, countries.Count);
            }

            if (countries.Count == 0)
            {
                CreateLabelNode(research, "暂无可交易国家", 0, 1);
            }

            ShowSelectionPrompt(research);
            Plugin.LogInfo("已打开多用途研究点分类。");
        }

        internal static void LeaveCustom()
        {
            _customActive = false;
            ClearNodes();
            DestroyCustomContentRoot();
            DestroyCustomDetailView();
            if (_research != null)
            {
                RestoreVanillaResearch(_research);
            }
            _selected = false;
            _selectedCountryKey = null;
            _research = null;
        }

        internal static bool IsCustomActive => _customActive;

        internal static void ReassertCustomView()
        {
            if (NeedsCustomViewReassert(_research))
            {
                ReassertCustomView(_research);
            }
        }

        internal static void ReassertCustomView(ResearchUI research)
        {
            if (!IsCurrentCustomResearch(research) || _reassertingCustomView)
            {
                return;
            }

            _reassertingCustomView = true;
            try
            {
                HideVanillaResearchRoots(research);
                var customRoot = GetOrCreateCustomContentRoot(research);
                if (customRoot != null)
                {
                    customRoot.gameObject.SetActive(true);
                    customRoot.SetAsLastSibling();
                }

                foreach (var node in Nodes)
                {
                    if (node != null)
                    {
                        node.SetActive(true);
                    }
                }

                var customDetail = GetOrCreateCustomDetailView(research);
                if (customDetail?.Root != null)
                {
                    customDetail.Root.SetActive(true);
                    customDetail.Root.transform.SetAsLastSibling();
                }

                research.m_SelecNode = null;
                research.m_DetailCheck = false;
                if (research.Tf_SelectBox != null)
                {
                    research.Tf_SelectBox.gameObject.SetActive(false);
                }
                ReassertCustomDetail();
            }
            catch (Exception error)
            {
                Plugin.LogError("重新接管多用途研究点界面失败。", error);
            }
            finally
            {
                _reassertingCustomView = false;
            }
        }

        internal static void Cleanup()
        {
            LeaveCustom();
            if (_researchListEntryObject != null)
            {
                UnityEngine.Object.Destroy(_researchListEntryObject);
                _researchListEntryObject = null;
            }

            _reassertingCustomView = false;
            _originalContentCaptured = false;
            _originalDetailCaptured = false;
            _originalCategoryGroupCaptured = false;
            _customContentRoot = null;
            _customDetailView = null;
            _research = null;
            _researchList = null;
            _researchListSetLogged = false;
        }

        private static void CreateProductivityNode(ResearchUI research)
        {
            var active = MultipurposeResearchRules.IsProductivityActive(Plugin.State, GetCurrentDay());
            var text = active
                ? $"全民动员\n{Plugin.State.ProductivityPercent:P0} / 剩余 {Math.Max(0, Plugin.State.ProductivityExpiresAt - GetCurrentDay())} 天"
                : $"全民动员\n消耗 {Plugin.Settings.ProductivityCost} 研究点";
            CreateNode(research, text, LoadSprite("GameScene/UI/UI_Canvas/Icon/Icon_Research_Scientist"),
                NodeAction.Productivity, null, 0, NodeRegion.Feature, 4);
        }

        private static void CreateQueenNode(ResearchUI research, QueenUpgrade upgrade, string title, int index)
        {
            var purchases = GetPurchases(upgrade);
            var cost = MultipurposeResearchRules.GetQueenUpgradeCost(Plugin.Settings, purchases);
            var bonus = GetBonus(upgrade);
            var text = $"{title}\n+{bonus} / 下次 {cost}";
            CreateNode(research, text, LoadSprite("GameScene/UI/UI_Canvas/Icon/Icon_Research_Magician"),
                NodeAction.Queen, upgrade.ToString(), index + 1, NodeRegion.Feature, 4);
        }

        private static void CreateCountryNode(
            ResearchUI research,
            DiplomaticCountryData country,
            int countryIndex,
            int countryCount)
        {
            var income = DiplomaticSaleRuntime.GetPreviewIncome(country);
            var text = $"{country.Name}\n收入 {income} / {Plugin.Settings.SaleCost} 研究点";
            CreateNode(research, text, country.Icon, NodeAction.Sale, country.Key,
                countryIndex, NodeRegion.Country, countryCount);
        }

        private static void CreateLabelNode(
            ResearchUI research,
            string title,
            int countryIndex,
            int countryCount)
        {
            CreateNode(research, title, LoadSprite("GameScene/UI/UI_Canvas/Icon/Icon_Research_Scientist"),
                NodeAction.None, null, countryIndex, NodeRegion.Country, countryCount);
        }

        private static void CreateNode(
            ResearchUI research,
            string text,
            Sprite icon,
            NodeAction action,
            string parameter,
            int index,
            NodeRegion region,
            int regionItemCount)
        {
            if (research.Prefab_TechNode == null || research.Tf_Content == null)
            {
                return;
            }

            var contentRoot = GetOrCreateCustomContentRoot(research);
            if (contentRoot == null)
            {
                return;
            }

            var nodeObject = UnityEngine.Object.Instantiate(research.Prefab_TechNode, contentRoot);
            nodeObject.SetActive(false);
            nodeObject.name = "MultipurposeResearch.Node." + index;
            var rect = nodeObject.transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = region == NodeRegion.Country
                    ? GetCountryNodePosition(index, regionItemCount)
                    : new Vector2((index % 4) * 190f - 285f, 180f);
                rect.localScale = Vector3.one;
            }

            var vanillaNodes = nodeObject.GetComponentsInChildren<TechNode>(true);
            var vanillaNode = vanillaNodes.FirstOrDefault();
            var vanillaFrame = vanillaNode == null ? null : vanillaNode.Img_Frame;
            foreach (var node in vanillaNodes)
            {
                if (node.Txt_Name != null)
                {
                    node.Txt_Name.text = text;
                }
                if (icon != null && node.Img_Icon != null)
                {
                    node.Img_Icon.sprite = icon;
                }
                if (node.Img_Lock != null)
                {
                    node.Img_Lock.enabled = false;
                }
                UnityEngine.Object.DestroyImmediate(node);
            }

            foreach (var originalButton in nodeObject.GetComponentsInChildren<Button>(true))
            {
                UnityEngine.Object.DestroyImmediate(originalButton);
            }

            var button = nodeObject.AddComponent<Button>();
            if (vanillaFrame != null)
            {
                button.targetGraphic = vanillaFrame;
            }

            var customNode = nodeObject.GetComponent<MultipurposeResearchNode>() ??
                nodeObject.AddComponent<MultipurposeResearchNode>();
            customNode.Bind(() => Select(action, parameter));
            nodeObject.SetActive(true);
            Nodes.Add(nodeObject);
        }

        private static Vector2 GetCountryNodePosition(int countryIndex, int countryCount)
        {
            var row = countryIndex / CountryColumnCount;
            var column = countryIndex % CountryColumnCount;
            var rowStartIndex = row * CountryColumnCount;
            var rowItemCount = Math.Min(CountryColumnCount, countryCount - rowStartIndex);
            rowItemCount = Math.Max(1, rowItemCount);
            var x = (column - (rowItemCount - 1) * 0.5f) * CountryColumnSpacing;
            return new Vector2(x, 10f - row * CountryRowSpacing);
        }

        private static void Select(NodeAction action, string parameter)
        {
            _selected = action != NodeAction.None;
            _selectedAction = action;
            _selectedCountryKey = parameter;
            if (_research == null)
            {
                return;
            }

            if (action == NodeAction.None)
            {
                ShowSelectionPrompt(_research);
                return;
            }

            SetCustomDetail(
                _research,
                GetDetailTitle(action, parameter),
                GetDetailDescription(action, parameter),
                GetDetailCost(action, parameter),
                GetDetailAction(action),
                true);
        }

        internal static bool TryConfirmSelected()
        {
            if (!_customActive)
            {
                return false;
            }

            ConfirmSelected();
            return true;
        }

        private static void ConfirmSelected()
        {
            if (!_selected)
            {
                ShowSelectionRequired();
                return;
            }

            var success = false;
            switch (_selectedAction)
            {
                case NodeAction.Sale:
                    success = DiplomaticSaleRuntime.TrySell(_selectedCountryKey);
                    break;
                case NodeAction.Productivity:
                    success = CitizenProductivityRuntime.TryActivate();
                    break;
                case NodeAction.Queen:
                    if (Enum.TryParse(_selectedCountryKey, out QueenUpgrade upgrade))
                    {
                        success = QueenUpgradeRuntime.TryUpgrade(upgrade);
                    }
                    break;
            }

            if (success)
            {
                EnterCustom(_research);
                return;
            }

            ShowActionFailed();
        }

        private static void ShowSelectionPrompt(ResearchUI research)
        {
            SetCustomDetail(research,
                "多用途研究点",
                "请选择左侧项目，查看用途、效果和研究点消耗。",
                "请先选择左侧项目",
                "请选择项目",
                true);
        }

        private static void ReassertCustomDetail()
        {
            if (!IsCurrentCustomResearch(_research))
            {
                return;
            }

            if (!_selected)
            {
                ShowSelectionPrompt(_research);
                return;
            }

            SetCustomDetail(
                _research,
                GetDetailTitle(_selectedAction, _selectedCountryKey),
                GetDetailDescription(_selectedAction, _selectedCountryKey),
                GetDetailCost(_selectedAction, _selectedCountryKey),
                GetDetailAction(_selectedAction),
                true);
        }

        private static void SetCustomDetail(
            ResearchUI research,
            string title,
            string description,
            string cost,
            string action,
            bool buttonActive)
        {
            if (research == null)
            {
                return;
            }

            HideVanillaResearchRoots(research);
            var view = GetOrCreateCustomDetailView(research);
            if (view == null || view.Root == null)
            {
                return;
            }

            view.Root.SetActive(true);
            view.Root.transform.SetAsLastSibling();
            if (view.DetailText != null) view.DetailText.SetActive(true);
            if (view.AlreadyResearch != null) view.AlreadyResearch.SetActive(false);
            if (view.Name != null) view.Name.text = title;
            if (view.Description != null) view.Description.text = description;
            if (view.Need != null) view.Need.text = cost;
            if (view.Action != null) view.Action.text = action;
            if (view.UpgradeTime != null) view.UpgradeTime.text = string.Empty;
            if (view.ExecuteButtonRoot != null) view.ExecuteButtonRoot.SetActive(buttonActive);
        }

        private static void ShowSelectionRequired()
        {
            var alarm = GameMgr.Instance?._CenterAlarmUI;
            if (alarm != null)
            {
                alarm.CenterAlarmCustomSet("请先选择左侧项目。", Color.white);
            }

            Plugin.LogInfo("多用途研究点尚未选择操作项目。");
        }

        private static void ShowActionFailed()
        {
            var alarm = GameMgr.Instance?._CenterAlarmUI;
            if (alarm != null)
            {
                alarm.CenterAlarmCustomSet("研究点不足或当前操作不可用。", Color.red);
            }

            Plugin.LogWarning("多用途研究点操作未执行：研究点不足或当前状态不可用。");
        }

        private static void CaptureAndHideVanillaResearch(ResearchUI research)
        {
            _originalContentCaptured = research.Tf_Content != null;
            _originalContentActive = _originalContentCaptured && research.Tf_Content.gameObject.activeSelf;
            _originalDetailCaptured = research.m_Tech_RPInfo != null;
            _originalDetailActive = _originalDetailCaptured && research.m_Tech_RPInfo.gameObject.activeSelf;
            _originalCategoryGroupCaptured = research.Obj_CategoryGroup != null;
            _originalCategoryGroupActive =
                _originalCategoryGroupCaptured && research.Obj_CategoryGroup.activeSelf;

            research.m_SelecNode = null;
            research.m_DetailCheck = false;
            if (research.Tf_SelectBox != null)
            {
                research.Tf_SelectBox.gameObject.SetActive(false);
            }
            HideVanillaResearchRoots(research);
        }

        private static void HideVanillaResearchRoots(ResearchUI research)
        {
            if (research?.Tf_Content != null && research.Tf_Content.gameObject.activeSelf)
            {
                research.Tf_Content.gameObject.SetActive(false);
            }

            if (research?.m_Tech_RPInfo != null && research.m_Tech_RPInfo.gameObject.activeSelf)
            {
                research.m_Tech_RPInfo.gameObject.SetActive(false);
            }

            if (research?.Obj_CategoryGroup != null && research.Obj_CategoryGroup.activeSelf)
            {
                research.Obj_CategoryGroup.SetActive(false);
            }
        }

        private static void RestoreVanillaResearch(ResearchUI research)
        {
            if (_originalContentCaptured && research?.Tf_Content != null)
            {
                research.Tf_Content.gameObject.SetActive(_originalContentActive);
            }

            if (_originalDetailCaptured && research?.m_Tech_RPInfo != null)
            {
                research.m_Tech_RPInfo.gameObject.SetActive(_originalDetailActive);
            }

            if (_originalCategoryGroupCaptured && research?.Obj_CategoryGroup != null)
            {
                research.Obj_CategoryGroup.SetActive(_originalCategoryGroupActive);
            }

            if (research != null)
            {
                research.m_SelecNode = null;
                research.m_DetailCheck = false;
                if (research.Tf_SelectBox != null)
                {
                    research.Tf_SelectBox.gameObject.SetActive(false);
                }
            }
            _originalContentCaptured = false;
            _originalDetailCaptured = false;
            _originalCategoryGroupCaptured = false;
        }

        private static RectTransform GetOrCreateCustomContentRoot(ResearchUI research)
        {
            if (research?.Tf_Content == null)
            {
                return null;
            }

            if (_customContentRoot != null && _customContentRoot.parent == research.Tf_Content.parent)
            {
                _customContentRoot.SetAsLastSibling();
                return _customContentRoot;
            }

            DestroyCustomContentRoot();
            var rootObject = new GameObject(CustomContentRootName, typeof(RectTransform));
            var root = rootObject.GetComponent<RectTransform>();
            root.SetParent(research.Tf_Content.parent, false);
            CopyRectTransformLayout(research.Tf_Content, root);
            root.SetAsLastSibling();
            _customContentRoot = root;
            rootObject.AddComponent<MultipurposeResearchViewGuard>();
            return root;
        }

        private static void CopyRectTransformLayout(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.anchoredPosition3D = source.anchoredPosition3D;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static CustomDetailView GetOrCreateCustomDetailView(ResearchUI research)
        {
            if (_customDetailView?.Root != null && research?.m_Tech_RPInfo != null &&
                _customDetailView.Root.transform.parent == research.m_Tech_RPInfo.transform.parent)
            {
                _customDetailView.Root.transform.SetAsLastSibling();
                return _customDetailView;
            }

            DestroyCustomDetailView();
            return CreateCustomDetailView(research);
        }

        private static CustomDetailView CreateCustomDetailView(ResearchUI research)
        {
            var originalDetail = research?.m_Tech_RPInfo;
            if (originalDetail == null)
            {
                Plugin.LogWarning("未找到原版研究详情模板，无法创建多用途研究点右侧提示。");
                return null;
            }

            originalDetail.gameObject.SetActive(false);
            var clone = UnityEngine.Object.Instantiate(originalDetail.gameObject,
                originalDetail.transform.parent);
            clone.name = CustomDetailRootName;
            clone.SetActive(false);

            var clonedDetail = clone.GetComponent<Tech_RPInfo>();
            if (clonedDetail == null)
            {
                UnityEngine.Object.Destroy(clone);
                Plugin.LogWarning("克隆的研究详情缺少 Tech_RPInfo，无法创建多用途研究点右侧提示。");
                return null;
            }

            var independentName = CreateIndependentDetailText(clonedDetail.Txt_Name, clone.transform,
                "MultipurposeResearch.Detail.Name", new Vector2(0.08f, 0.75f),
                new Vector2(0.92f, 0.9f), TextAlignmentOptions.Center);
            var independentDescription = CreateIndependentDetailText(clonedDetail.Txt_Description, clone.transform,
                "MultipurposeResearch.Detail.Description", new Vector2(0.08f, 0.42f),
                new Vector2(0.92f, 0.75f), TextAlignmentOptions.TopLeft);

            if (clonedDetail.Txt_Name != null)
            {
                clonedDetail.Txt_Name.enabled = false;
            }
            if (clonedDetail.Txt_Description != null)
            {
                clonedDetail.Txt_Description.enabled = false;
            }

            var view = new CustomDetailView
            {
                Root = clone,
                DetailText = clonedDetail.Obj_DetailTxt,
                Name = independentName,
                Description = independentDescription,
                ExecuteButtonRoot = clonedDetail.Obj_UpgradeBtn,
                Action = clonedDetail.Txt_UpgradeBtn,
                Need = clonedDetail.Txt_NeedBtn,
                UpgradeTime = clonedDetail.Txt_UpgradeTime,
                AlreadyResearch = clonedDetail.Obj_AlreadyResearch
            };

            foreach (var slot in clonedDetail.m_Slot ?? new Tech_RPSlot[0])
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }

            foreach (var hotkey in clone.GetComponentsInChildren<Com_HotKey>(true))
            {
                if (hotkey != null && hotkey.gameObject != clone)
                {
                    hotkey.gameObject.SetActive(false);
                }
            }

            foreach (var oldButton in clone.GetComponentsInChildren<Button>(true))
            {
                UnityEngine.Object.DestroyImmediate(oldButton);
            }
            UnityEngine.Object.DestroyImmediate(clonedDetail);

            if (view.ExecuteButtonRoot != null)
            {
                var executeButton = view.ExecuteButtonRoot.AddComponent<Button>();
                executeButton.targetGraphic = view.ExecuteButtonRoot.GetComponent<Image>() ??
                    view.ExecuteButtonRoot.GetComponentInChildren<Image>(true);
                executeButton.onClick.AddListener(ConfirmSelected);
            }

            _customDetailView = view;
            clone.SetActive(true);
            clone.transform.SetAsLastSibling();
            return view;
        }

        private static TextMeshProUGUI CreateIndependentDetailText(
            TextMeshProUGUI template,
            Transform detailRoot,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAlignmentOptions alignment)
        {
            if (template == null || detailRoot == null)
            {
                return null;
            }

            var textObject = UnityEngine.Object.Instantiate(template.gameObject);
            textObject.name = objectName;
            textObject.SetActive(false);
            textObject.transform.SetParent(detailRoot, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                UnityEngine.Object.Destroy(textObject);
                return null;
            }

            var rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            text.text = string.Empty;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 34f;
            text.raycastTarget = false;
            textObject.SetActive(true);
            rect.SetAsLastSibling();
            return text;
        }

        private static bool IsCurrentCustomResearch(ResearchUI research)
        {
            return _customActive && research != null && ReferenceEquals(_research, research);
        }

        private static bool NeedsCustomViewReassert(ResearchUI research)
        {
            if (!IsCurrentCustomResearch(research))
            {
                return false;
            }

            if ((research.Tf_Content != null && research.Tf_Content.gameObject.activeSelf) ||
                (research.m_Tech_RPInfo != null && research.m_Tech_RPInfo.gameObject.activeSelf) ||
                (research.Obj_CategoryGroup != null && research.Obj_CategoryGroup.activeSelf))
            {
                return true;
            }

            if (_customContentRoot == null || !_customContentRoot.gameObject.activeSelf ||
                _customDetailView?.Root == null || !_customDetailView.Root.activeSelf)
            {
                return true;
            }

            var expectedTitle = _selected
                ? GetDetailTitle(_selectedAction, _selectedCountryKey)
                : "多用途研究点";
            return _customDetailView.DetailText == null || !_customDetailView.DetailText.activeSelf ||
                (_customDetailView.AlreadyResearch != null && _customDetailView.AlreadyResearch.activeSelf) ||
                (_customDetailView.Name != null && _customDetailView.Name.text != expectedTitle) ||
                (_customDetailView.UpgradeTime != null &&
                    !string.IsNullOrEmpty(_customDetailView.UpgradeTime.text));
        }

        private static void DestroyCustomContentRoot()
        {
            if (_customContentRoot != null)
            {
                _customContentRoot.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_customContentRoot.gameObject);
            }

            _customContentRoot = null;
        }

        private static void DestroyCustomDetailView()
        {
            if (_customDetailView?.Root != null)
            {
                _customDetailView.Root.SetActive(false);
                UnityEngine.Object.Destroy(_customDetailView.Root);
            }

            _customDetailView = null;
        }

        private static void ClearNodes()
        {
            foreach (var node in Nodes)
            {
                if (node != null)
                {
                    node.SetActive(false);
                    UnityEngine.Object.Destroy(node);
                }
            }
            Nodes.Clear();
        }

        private static string GetDetailTitle(NodeAction action, string parameter)
        {
            if (action == NodeAction.Sale)
            {
                var country = DiplomaticSaleRuntime.GetAvailableCountries().FirstOrDefault(item => item.Key == parameter);
                return country == null ? "学术出口" : country.Name;
            }
            if (action == NodeAction.Productivity) return "全民动员";
            if (Enum.TryParse(parameter, out QueenUpgrade upgrade))
            {
                return upgrade == QueenUpgrade.Power ? "女王力量" : upgrade == QueenUpgrade.Dexterity ? "女王敏捷" : "女王智慧";
            }
            return "研究点用途";
        }

        private static string GetDetailCost(NodeAction action, string parameter)
        {
            if (action == NodeAction.Sale) return $"{Plugin.Settings.SaleCost} 研究点";
            if (action == NodeAction.Productivity) return $"{Plugin.Settings.ProductivityCost} 研究点";
            if (Enum.TryParse(parameter, out QueenUpgrade upgrade))
            {
                return MultipurposeResearchRules.GetQueenUpgradeCost(Plugin.Settings, GetPurchases(upgrade)) + " 研究点";
            }
            return "";
        }

        private static string GetDetailDescription(NodeAction action, string parameter)
        {
            if (action == NodeAction.Sale)
            {
                var country = DiplomaticSaleRuntime.GetAvailableCountries().FirstOrDefault(item => item.Key == parameter);
                var countryName = country == null ? "该国家" : country.Name;
                var income = country == null ? 0 : DiplomaticSaleRuntime.GetPreviewIncome(country);
                return $"向{countryName}出售学术成果，获得 {income} 金币，并提升关系和繁荣。";
            }
            if (action == NodeAction.Productivity)
            {
                return $"使所有普通鼠民工作效率提高 {Plugin.Settings.ProductivityPercent:P0}，持续 {Plugin.Settings.ProductivityDurationDays} 天；再次执行会刷新持续时间。";
            }
            if (Enum.TryParse(parameter, out QueenUpgrade upgrade))
            {
                var attribute = upgrade == QueenUpgrade.Power ? "力量" :
                    upgrade == QueenUpgrade.Dexterity ? "敏捷" : "智慧";
                if (Plugin.Settings.QueenUpgradeCostStep > 0)
                {
                    return $"永久提升女王{attribute} {Plugin.Settings.QueenAttributeIncrease} 点；同一属性每次执行后，下次消耗增加 {Plugin.Settings.QueenUpgradeCostStep} 研究点。";
                }
                return $"永久提升女王{attribute} {Plugin.Settings.QueenAttributeIncrease} 点；每次执行固定消耗 {Plugin.Settings.QueenUpgradeBaseCost} 研究点。";
            }
            return "请选择一个可执行项目。";
        }

        private static string GetDetailAction(NodeAction action)
        {
            return action == NodeAction.None ? "" : "执行";
        }

        private static int GetPurchases(QueenUpgrade upgrade)
        {
            switch (upgrade)
            {
                case QueenUpgrade.Power: return Plugin.State.QueenPowerPurchases;
                case QueenUpgrade.Dexterity: return Plugin.State.QueenDexterityPurchases;
                default: return Plugin.State.QueenIntelligencePurchases;
            }
        }

        private static int GetBonus(QueenUpgrade upgrade)
        {
            switch (upgrade)
            {
                case QueenUpgrade.Power: return Plugin.State.QueenPowerBonus;
                case QueenUpgrade.Dexterity: return Plugin.State.QueenDexterityBonus;
                default: return Plugin.State.QueenIntelligenceBonus;
            }
        }

        private static int GetCurrentDay()
        {
            return GameMgr.Instance?._SysMgr?.m_Day ?? 0;
        }

        private static Sprite LoadSprite(string path)
        {
            try
            {
                return Func.Instance?.LoadSprite(path);
            }
            catch
            {
                return null;
            }
        }

        private enum NodeAction
        {
            None,
            Sale,
            Productivity,
            Queen
        }

        private sealed class CustomDetailView
        {
            internal GameObject Root;
            internal GameObject DetailText;
            internal TMP_Text Name;
            internal TMP_Text Description;
            internal GameObject ExecuteButtonRoot;
            internal TMP_Text Action;
            internal TMP_Text Need;
            internal TMP_Text UpgradeTime;
            internal GameObject AlreadyResearch;
        }

        private enum NodeRegion
        {
            Feature,
            Country
        }
    }

    internal sealed class MultipurposeResearchNode : MonoBehaviour
    {
        private Action _onClick;

        internal void Bind(Action onClick)
        {
            _onClick = onClick;
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }
    }

    internal sealed class MultipurposeResearchViewGuard : MonoBehaviour
    {
        private void OnEnable()
        {
            ResearchUiRuntime.ReassertCustomView();
        }

        private void LateUpdate()
        {
            ResearchUiRuntime.ReassertCustomView();
        }
    }

    internal sealed class MultipurposeResearchListEntry : MonoBehaviour
    {
    }
}
