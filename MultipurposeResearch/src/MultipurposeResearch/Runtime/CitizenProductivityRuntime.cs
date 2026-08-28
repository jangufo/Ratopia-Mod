using System;
using System.Collections.Generic;
using MultipurposeResearch.Core;

namespace MultipurposeResearch.Runtime
{
    internal static class CitizenProductivityRuntime
    {
        internal const string BuffReference = "MultipurposeResearch.Productivity";
        internal const string DisplayName = "全民动员";
        internal const string IconAddress = "GameScene/UI/UI_Canvas/Icon/Icon_Research_Scientist";

        internal static bool TryActivate()
        {
            var config = Plugin.Settings;
            if (config == null || !SaveStateStore.IsReady())
            {
                return false;
            }

            var currentDay = GetCurrentDay();
            if (!ResearchPointRuntime.TrySpend(config.ProductivityCost))
            {
                return false;
            }

            var state = MultipurposeResearchRules.RefreshProductivity(
                Plugin.State,
                config.ProductivityPercent,
                currentDay,
                config.ProductivityDurationDays);
            if (!Plugin.TrySaveState(state))
            {
                ResearchPointRuntime.Refund(config.ProductivityCost);
                return false;
            }

            ApplyCurrentToAll();
            Plugin.LogInfo("已启用全民动员；重复购买只刷新完整持续时间。");
            return true;
        }

        internal static void ApplyCurrentToAll()
        {
            var citizens = GameMgr.Instance?._T_UnitMgr?.List_Citizen;
            if (citizens == null)
            {
                Plugin.LogWarning("全民动员同步失败：当前没有可用的鼠民列表。");
                return;
            }

            var active = MultipurposeResearchRules.IsProductivityActive(Plugin.State, GetCurrentDay());
            var appliedCount = 0;
            var failedCount = 0;
            foreach (var citizen in citizens)
            {
                try
                {
                    if (Apply(citizen, active))
                    {
                        appliedCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (Exception error)
                {
                    failedCount++;
                    Plugin.LogError("为单个鼠民同步全民动员时发生异常。", error);
                }
            }

            var remainingHours = active
                ? Math.Max(1, 24 * Math.Max(1, Plugin.State.ProductivityExpiresAt - GetCurrentDay()))
                : 0;
            Plugin.LogInfo($"全民动员同步完成：生效={active}，成功={appliedCount}，失败={failedCount}，剩余约={remainingHours}小时。");
        }

        internal static bool Apply(T_Citizen citizen, bool forceActive = true)
        {
            if (!SaveStateStore.IsReady() || citizen?.m_Buff == null)
            {
                return false;
            }

            if (!forceActive || !MultipurposeResearchRules.IsProductivityActive(Plugin.State, GetCurrentDay()))
            {
                citizen.m_Buff.RefKill(BuffReference);
                return !citizen.m_Buff.IsExistRef(BuffReference);
            }

            var hours = Math.Max(1, (int)(24f *
                Math.Max(1, Plugin.State.ProductivityExpiresAt - GetCurrentDay())));
            ReplaceProductivityBuff(citizen, hours);
            return citizen.m_Buff.IsExistRef(BuffReference);
        }

        private static void ReplaceProductivityBuff(T_Citizen citizen, int hours)
        {
            citizen.m_Buff.RefKill(BuffReference);
            citizen.m_Buff.BuffRefSet(
                C_Buff.ProductivityUp,
                BuffReference,
                C_Buff_Category.Event,
                Plugin.State.ProductivityPercent,
                hours,
                true,
                true);
        }

        internal static void EnhanceReferenceMetadata(List<CitizenBuff.RefInfo> references)
        {
            if (references == null)
            {
                return;
            }

            for (var index = 0; index < references.Count; index++)
            {
                var info = references[index];
                if (!string.Equals(info.RefName, BuffReference, StringComparison.Ordinal))
                {
                    continue;
                }

                info.T_Name = DisplayName;
                info.Icon_Address = IconAddress;
                references[index] = info;
            }
        }

        internal static bool TryGetDisplayName(string referenceName, out string displayName)
        {
            displayName = null;
            if (!string.Equals(referenceName, BuffReference, StringComparison.Ordinal))
            {
                return false;
            }

            displayName = DisplayName;
            return true;
        }

        internal static bool TryGetIconAddress(string referenceName, out string iconAddress)
        {
            iconAddress = null;
            if (!string.Equals(referenceName, BuffReference, StringComparison.Ordinal))
            {
                return false;
            }

            iconAddress = IconAddress;
            return true;
        }

        internal static bool TryGetDescription(CitizenBuff.RefInfo info, out string description)
        {
            description = null;
            if (!string.Equals(info.RefName, BuffReference, StringComparison.Ordinal))
            {
                return false;
            }

            var effect = info.GetEffectScript();
            description = string.IsNullOrWhiteSpace(effect)
                ? "全民动员期间提高工作效率；重复购买只刷新持续时间，不叠加数值。"
                : $"{effect}\n重复购买只刷新持续时间，不叠加数值。";
            return true;
        }

        private static int GetCurrentDay()
        {
            return GameMgr.Instance?._SysMgr?.m_Day ?? 0;
        }
    }
}
