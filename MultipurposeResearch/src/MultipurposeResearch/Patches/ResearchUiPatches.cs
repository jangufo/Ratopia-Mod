using HarmonyLib;
using MultipurposeResearch.Runtime;

namespace MultipurposeResearch.Patches
{
    [HarmonyPatch(typeof(ResearchListUI), "Awake")]
    internal static class ResearchListUiAwakePatch
    {
        private static void Postfix(ResearchListUI __instance)
        {
            Plugin.LogInfo("首次捕获 ResearchListUI.Awake，提前创建多用途研究点卡片。");
            ResearchUiRuntime.AttachResearchListEntry(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchListUI), nameof(ResearchListUI.ResearchListtUI_Set))]
    internal static class ResearchListUiSetPatch
    {
        private static void Postfix(ResearchListUI __instance, bool __0)
        {
            if (__0)
            {
                Plugin.LogInfo("研究分类选择界面已打开，开始创建多用途研究点卡片。");
                ResearchUiRuntime.AttachResearchListEntry(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ResearchListUI), nameof(ResearchListUI.InteracAction))]
    internal static class ResearchListUiInteractPatch
    {
        private static bool Prefix(ResearchListUI __instance)
        {
            return !ResearchUiRuntime.TryOpenSelectedResearchListEntry(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchCategorySlot), nameof(ResearchCategorySlot.PointerClick))]
    internal static class ResearchCategorySlotClickPatch
    {
        private static bool Prefix(ResearchCategorySlot __instance)
        {
            if (!ResearchUiRuntime.IsResearchListEntry(__instance))
            {
                return true;
            }

            Plugin.LogInfo("已点击多用途研究点研究分类卡片。");
            ResearchUiRuntime.OpenCustomFromResearchList();
            return false;
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.ExitBtn))]
    internal static class ResearchUiExitPatch
    {
        private static void Prefix()
        {
            ResearchUiRuntime.LeaveCustom();
        }
    }

    [HarmonyPatch(typeof(Tech_RPInfo), nameof(Tech_RPInfo.UpgradBtn))]
    internal static class ResearchUiConfirmPatch
    {
        private static bool Prefix()
        {
            return !ResearchUiRuntime.TryConfirmSelected();
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.MakeNodeGraph))]
    internal static class ResearchUiGraphRedrawPatch
    {
        private static void Postfix(ResearchUI __instance)
        {
            ResearchUiRuntime.ReassertCustomView(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.Research_IconBtn))]
    internal static class ResearchUiVanillaCategoryPatch
    {
        private static void Prefix()
        {
            ResearchUiRuntime.LeaveCustom();
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.EngineeringResearch_IconBtn))]
    internal static class ResearchUiEngineeringCategoryPatch
    {
        private static void Prefix()
        {
            ResearchUiRuntime.LeaveCustom();
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.MagicianResearch_IconBtn))]
    internal static class ResearchUiMagicianCategoryPatch
    {
        private static void Prefix()
        {
            ResearchUiRuntime.LeaveCustom();
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.Cat_LeftBtn))]
    internal static class ResearchUiLeftPatch
    {
        private static void Prefix()
        {
            if (ResearchUiRuntime.IsCustomActive)
            {
                ResearchUiRuntime.LeaveCustom();
            }
        }
    }

    [HarmonyPatch(typeof(ResearchUI), nameof(ResearchUI.Cat_RightBtn))]
    internal static class ResearchUiRightPatch
    {
        private static void Prefix()
        {
            if (ResearchUiRuntime.IsCustomActive)
            {
                ResearchUiRuntime.LeaveCustom();
            }
        }
    }
}
