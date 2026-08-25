using MultipurposeResearch.Core;
using Utility.Savable;

namespace MultipurposeResearch.Runtime
{
    internal static class SaveStateStore
    {
        internal const string StateKey = "cn.ratopia.multipurposeresearch.state";

        internal static MultipurposeResearchState LoadCurrent(out bool malformed)
        {
            malformed = false;
            var data = PlayDataMgr.Instance?.m_GameData;
            var mods = data?.ModsData;
            if (mods == null || !mods.HasKey(StateKey))
            {
                return MultipurposeResearchState.Empty;
            }

            var raw = mods.GetValue<string>(StateKey, null);
            if (MultipurposeResearchCodec.TryDeserialize(raw, out var state))
            {
                return state;
            }

            malformed = true;
            return MultipurposeResearchState.Empty;
        }

        internal static bool IsReady()
        {
            return PlayDataMgr.Instance?.m_GameData != null;
        }

        internal static bool TrySaveCurrent(MultipurposeResearchState state)
        {
            var data = PlayDataMgr.Instance?.m_GameData;
            if (data == null || state == null)
            {
                return false;
            }

            if (data.ModsData == null)
            {
                data.ModsData = SavableData.Create();
            }

            var serialized = MultipurposeResearchCodec.Serialize(state);
            data.ModsData.AddData(StateKey, serialized);
            return serialized == data.ModsData.GetValue<string>(StateKey, null);
        }
    }
}
