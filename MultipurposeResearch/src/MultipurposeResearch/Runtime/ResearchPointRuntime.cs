using System;

namespace MultipurposeResearch.Runtime
{
    internal static class ResearchPointRuntime
    {
        internal static bool TrySpend(int cost)
        {
            if (cost < 1)
            {
                return false;
            }

            var research = GameMgr.Instance?._ResearchUI;
            if (research == null || research.m_Point < cost)
            {
                return false;
            }

            var before = research.m_Point;
            try
            {
                research.PointUp(-cost);
            }
            catch (Exception error)
            {
                Plugin.LogError("扣除研究点时发生异常。", error);
                return false;
            }

            if (research.m_Point != before - cost)
            {
                Plugin.LogWarning("研究点扣除结果与预期不符，已拒绝继续结算。");
                var changed = before - research.m_Point;
                if (changed > 0)
                {
                    Refund(changed);
                }
                return false;
            }

            return true;
        }

        internal static void Refund(int amount)
        {
            if (amount < 1)
            {
                return;
            }

            var research = GameMgr.Instance?._ResearchUI;
            if (research != null)
            {
                research.PointUp(amount);
            }
        }
    }
}
