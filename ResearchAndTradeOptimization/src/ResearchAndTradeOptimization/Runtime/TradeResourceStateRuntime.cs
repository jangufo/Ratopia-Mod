using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CasselGames.Diplomatic.Data;
using CasselGames.Diplomatic.UI;
using HarmonyLib;
using ResearchAndTradeOptimization.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ResearchAndTradeOptimization.Runtime
{
    /// <summary>
    /// 国家详情进出口列表的"正在交易"高亮。
    /// 旧实现用整格不透明底色 + 图标 <see cref="Outline"/> 描边，导致图标出现重影、
    /// 格子边缘发虚（"糊"）。这里改成给格子套一层 圆角空心边框 Sprite：
    /// 中心透明、只画边框圈，不遮挡图标与羊皮纸底色，边界清晰不发虚。
    /// </summary>
    internal static class TradeResourceStateRuntime
    {
        private static readonly AccessTools.FieldRef<
            DiplomaticWorldDetailUI,
            DiplomaticCountryData> Country =
                AccessTools.FieldRefAccess<
                    DiplomaticWorldDetailUI,
                    DiplomaticCountryData>("_country");

        private static readonly AccessTools.FieldRef<
            DiplomaticWorldDetailUI,
            DiplomaticWorldDetailResourceLayoutUI> ImportsLayout =
                AccessTools.FieldRefAccess<
                    DiplomaticWorldDetailUI,
                    DiplomaticWorldDetailResourceLayoutUI>("_importsLayoutUI");

        private static readonly AccessTools.FieldRef<
            DiplomaticWorldDetailUI,
            DiplomaticWorldDetailResourceLayoutUI> ExportsLayout =
                AccessTools.FieldRefAccess<
                    DiplomaticWorldDetailUI,
                    DiplomaticWorldDetailResourceLayoutUI>("_exportsLayoutUI");

        private static readonly AccessTools.FieldRef<
            DiplomaticWorldDetailResourceLayoutUI,
            List<DiplomaticWorldDetailResourceSlotUI>> Slots =
                AccessTools.FieldRefAccess<
                    DiplomaticWorldDetailResourceLayoutUI,
                    List<DiplomaticWorldDetailResourceSlotUI>>("_slotsUI");

        private static readonly AccessTools.FieldRef<
            DiplomaticWorldDetailResourceSlotUI,
            TileType> SlotTileType =
                AccessTools.FieldRefAccess<
                    DiplomaticWorldDetailResourceSlotUI,
                    TileType>("_tileType");

        private static readonly AccessTools.FieldRef<
            DiplomaticWorldDetailResourceSlotUI,
            Image> SlotIcon =
                AccessTools.FieldRefAccess<
                    DiplomaticWorldDetailResourceSlotUI,
                    Image>("_icon");

        // 槽位 -> 高亮边框 Image。对象池复用时必须把所有高亮格子清干净，否则
        // 上一个国家正在交易的边框会残留在复用槽位上。
        private static readonly ConditionalWeakTable<
            DiplomaticWorldDetailResourceSlotUI,
            Image> HighlightFrames =
                new ConditionalWeakTable<
                    DiplomaticWorldDetailResourceSlotUI,
                    Image>();

        // 圆角空心边框 Sprite 只生成一次，所有高亮格子共用。
        private static Sprite _frameSprite;
        private static bool _loggedHighlightFailure;

        internal static void ApplyActiveTradeHighlight(
            DiplomaticWorldDetailUI detail)
        {
            try
            {
                if (detail == null)
                {
                    return;
                }

                var country = Country(detail);
                if (country == null)
                {
                    return;
                }

                ApplyToLayout(ImportsLayout(detail), country);
                ApplyToLayout(ExportsLayout(detail), country);
            }
            catch (Exception exception)
            {
                if (!_loggedHighlightFailure)
                {
                    _loggedHighlightFailure = true;
                    Plugin.LogRuntimeError(
                        "应用国家详情贸易中商品高亮失败，已跳过本帧高亮。",
                        exception);
                }
            }
        }

        private static void ApplyToLayout(
            DiplomaticWorldDetailResourceLayoutUI layout,
            DiplomaticCountryData country)
        {
            var slots = layout == null ? null : Slots(layout);
            if (slots == null)
            {
                return;
            }

            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                if (slot == null || !slot.IsActivate)
                {
                    continue;
                }

                // 锁定槽（繁荣度不足）时原版会把 _icon 隐藏并显示 _disableObject。
                // 高亮边框会跟随整格，锁定槽若不清理也会残留上一位国家的边框，
                // 因此先把它视为不参与高亮。
                var icon = SlotIcon(slot);
                if (icon != null && !icon.gameObject.activeSelf)
                {
                    HideHighlight(slot);
                    continue;
                }

                var tileType = SlotTileType(slot);
                var kind = GetHighlightKind(country, tileType);
                if (kind == TradeHighlightKind.None)
                {
                    HideHighlight(slot);
                    continue;
                }

                ShowHighlight(
                    slot,
                    kind == TradeHighlightKind.Infinite
                        ? Plugin.InfiniteTradeHighlightColor
                        : Plugin.ActiveTradeHighlightColor);
            }
        }

        private static TradeHighlightKind GetHighlightKind(
            DiplomaticCountryData country,
            TileType tileType)
        {
            var sheets = country?.Sheets;
            if (sheets == null)
            {
                return TradeHighlightKind.None;
            }

            for (var index = 0; index < sheets.Count; index++)
            {
                var sheet = sheets[index];
                if (sheet == null ||
                    sheet.Resource != tileType ||
                    sheet.IsEnded())
                {
                    continue;
                }

                return TradeResourceStateRules.GetHighlightKind(
                    isVisibleSlot: true,
                    isCurrentlyTrading: true,
                    isInfinitePeriod: sheet.IsInfinitePeriod());
            }

            return TradeHighlightKind.None;
        }

        private static void ShowHighlight(
            DiplomaticWorldDetailResourceSlotUI slot,
            Color color)
        {
            var frame = GetOrCreateHighlightFrame(slot);
            frame.color = color;
            frame.gameObject.SetActive(true);
        }

        private static void HideHighlight(
            DiplomaticWorldDetailResourceSlotUI slot)
        {
            if (HighlightFrames.TryGetValue(slot, out var frame) &&
                frame != null)
            {
                frame.gameObject.SetActive(false);
            }
        }

        private static Image GetOrCreateHighlightFrame(
            DiplomaticWorldDetailResourceSlotUI slot)
        {
            if (HighlightFrames.TryGetValue(slot, out var existing) &&
                existing != null)
            {
                return existing;
            }

            var image = CreateHighlightFrame(slot);
            HighlightFrames.Add(slot, image);
            return image;
        }

        private static Image CreateHighlightFrame(
            DiplomaticWorldDetailResourceSlotUI slot)
        {
            var gameObject = new GameObject(
                "TradeResourceHighlight",
                typeof(RectTransform),
                typeof(Image));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(slot.transform, false);
            // 放最底层：边框圈在格子边缘，中心透明，不会额外遮住图标。
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = gameObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = GetFrameSprite();
            image.type = Image.Type.Simple;
            return image;
        }

        private static Sprite GetFrameSprite()
        {
            if (_frameSprite != null)
            {
                return _frameSprite;
            }

            _frameSprite = CreateFrameSprite();
            return _frameSprite;
        }

        /// <summary>
        /// 生成一张 圆角空心边框 Sprite：白色边框圈 + 透明中心。
        /// 用 highlight 颜色直接给整张 Sprite 着色即可得到不同颜色的边框。
        /// </summary>
        private static Sprite CreateFrameSprite()
        {
            const int size = 64;
            const int border = 4;
            const int radius = 12;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = new Color(1f, 1f, 1f, 1f);
            var clear = new Color(0f, 0f, 0f, 0f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var outer = IsInRoundedRect(x, y, size, 0, radius);
                    var inner = IsInRoundedRect(
                        x,
                        y,
                        size,
                        border,
                        Math.Max(0, radius - border));
                    texture.SetPixel(
                        x,
                        y,
                        outer && !inner ? fill : clear);
                }
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return sprite;
        }

        /// <summary>
        /// 判断 (x, y) 是否落在内缩 inset、圆角半径 radius 的圆角矩形内。
        /// </summary>
        private static bool IsInRoundedRect(
            int x,
            int y,
            int size,
            int inset,
            int radius)
        {
            var lo = inset;
            var hi = size - 1 - inset;
            if (x < lo || x > hi || y < lo || y > hi)
            {
                return false;
            }

            var r = Math.Max(0, Math.Min(radius, (hi - lo) / 2));
            var cx = x < lo + r ? lo + r : (x > hi - r ? hi - r : x);
            var cy = y < lo + r ? lo + r : (y > hi - r ? hi - r : y);
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }
    }
}
