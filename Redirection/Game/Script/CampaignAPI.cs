using Dan200.Core.Lua;
using Dan200.Core.Modding;
using Dan200.Game.Game;

namespace Dan200.Game.Script
{
    public class CampaignAPI : API
    {
        private LevelState m_state;

        public CampaignAPI(LevelState state)
        {
            m_state = state;
        }

        [LuaMethod]
        public LuaArgs getPath(LuaArgs args)
        {
            var campaign = GetCampaign();
            if (campaign != null)
            {
                return new LuaArgs(campaign.Path);
            }
            return LuaArgs.Nil;
        }

        [LuaMethod]
        public LuaArgs getLevelCount(LuaArgs args)
        {
            var campaign = GetCampaign();
            if (campaign != null)
            {
                return new LuaArgs(campaign.Levels.Count);
            }
            return new LuaArgs(0);
        }

        [LuaMethod]
        public LuaArgs getCurrentLevel(LuaArgs args)
        {
            var index = GetLevelIndex();
            if (index >= 0)
            {
                return new LuaArgs(index);
            }
            return new LuaArgs(0);
        }

        [LuaMethod]
        public LuaArgs getRobotCount(LuaArgs args)
        {
            var campaign = GetCampaign();
            if (campaign != null)
            {
                int total;
                int rescued = ProgressUtils.CountRobotsRescued(campaign, GetMod(), m_state.Game.User, out total, GetIgnoreLevel());
                return new LuaArgs(total, rescued);
            }
            return new LuaArgs(0, 0);
        }

        [LuaMethod]
        public LuaArgs isLevelCompleted(LuaArgs args)
        {
            var num = args.GetInt(0);
            var campaign = GetCampaign();
            if (campaign != null)
            {
                return new LuaArgs(ProgressUtils.IsLevelCompleted(campaign, GetMod(), num, m_state.Game.User, GetIgnoreLevel()));
            }
            return new LuaArgs(false);
        }

        [LuaMethod]
        public LuaArgs getCompletedLevelCount(LuaArgs args)
        {
            var campaign = GetCampaign();
            if (campaign != null)
            {
                return new LuaArgs(ProgressUtils.CountLevelsCompleted(campaign, GetMod(), m_state.Game.User, GetIgnoreLevel()));
            }
            return new LuaArgs(false);
        }

        [LuaMethod]
        public LuaArgs isLevelUnlocked(LuaArgs args)
        {
            var num = args.GetInt(0);
            var campaign = GetCampaign();
            if (campaign != null)
            {
                return new LuaArgs(ProgressUtils.IsLevelUnlocked(campaign, GetMod(), num, m_state.Game.User, GetIgnoreLevel()));
            }
            return new LuaArgs(false);
        }

        [LuaMethod]
        public LuaArgs getUnlockedLevelCount(LuaArgs args)
        {
            var campaign = GetCampaign();
            if (campaign != null)
            {
                return new LuaArgs(ProgressUtils.CountLevelsUnlocked(campaign, GetMod(), m_state.Game.User, GetIgnoreLevel()));
            }
            return new LuaArgs(false);
        }

        private Campaign GetCampaign()
        {
            if (m_state is CampaignState)
            {
                return ((CampaignState)m_state).Campaign;
            }
            else if (m_state is TestState)
            {
                return ((TestState)m_state).Campaign;
            }
            else if (m_state is CutsceneState)
            {
                return ((CutsceneState)m_state).Campaign;
            }
            else
            {
                return null;
            }
        }

        private Mod GetMod()
        {
            if (m_state is CampaignState)
            {
                return ((CampaignState)m_state).Mod;
            }
            else if (m_state is TestState)
            {
                return ((TestState)m_state).Mod;
            }
            else if (m_state is CutsceneState)
            {
                return ((CutsceneState)m_state).Mod;
            }
            else
            {
                return null;
            }
        }

        private int GetLevelIndex()
        {
            if (m_state is CampaignState)
            {
                return ((CampaignState)m_state).LevelIndex;
            }
            else if (m_state is TestState)
            {
                return ((TestState)m_state).LevelIndex;
            }
            else if (m_state is CutsceneState)
            {
                return ((CutsceneState)m_state).LevelIndex;
            }
            else
            {
                return -1;
            }
        }

        private int GetIgnoreLevel()
        {
            if (m_state is CampaignState)
            {
                var campaignState = (CampaignState)m_state;
                if (campaignState.NewLevel)
                {
                    return campaignState.LevelIndex;
                }
            }
            return -1;
        }
    }
}
