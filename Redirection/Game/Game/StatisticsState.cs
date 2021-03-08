using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Game.User;
using OpenTK;

namespace Dan200.Game.Game
{
    public class StatisticsState : MenuState
    {
        private Text[] m_statsText;
        private TextMenu m_statsMenu;

        private string GetStatString(string statisticID, long value)
        {
            return Game.Language.TranslateCount("stats." + statisticID, value);
        }

        public StatisticsState(Game game) : base(game, "menus.stats.title", "levels/empty.level", MenuArrangement.FullScreen)
        {
            {
                // Create stats readout
                var statistics = new Statistic[] {
                    Statistic.LevelsCompleted,
                    Statistic.ObstaclesPlaced,
                    Statistic.RobotsRescued,
                    Statistic.RobotsDrowned,
                    Statistic.RobotsLost,
                    Statistic.Rewinds
                };
                float yPos = -0.5f * (float)(statistics.Length + (Game.Network.SupportsAchievements ? 1 : 0)) * UIFonts.Default.Height;

                // Statistics
                m_statsText = new Text[statistics.Length];
                for (int i = 0; i < statistics.Length; ++i)
                {
                    var stat = statistics[i];
                    long value = Game.User.Progress.GetStatistic(stat);
                    var text = new Text(
                        UIFonts.Default,
                        GetStatString(stat.GetID(), value),
                        UIColours.Text,
                        TextAlignment.Center
                    );
                    text.Anchor = Anchor.CentreMiddle;
                    text.LocalPosition = new Vector2(0.0f, yPos);

                    m_statsText[i] = text;
                    yPos += text.Font.Height;
                }

                if (Game.Network.SupportsAchievements)
                {
                    // Achievements
                    m_statsMenu = new TextMenu(
                        UIFonts.Default,
                        new string[] {
                            Game.Language.Translate( "menus.stats.show_achievements" )
                        },
                        TextAlignment.Center,
                        MenuDirection.Vertical
                    );
                    m_statsMenu.Anchor = Anchor.CentreMiddle;
                    m_statsMenu.LocalPosition = new Vector2(0.0f, yPos);
                    m_statsMenu.TextColour = UIColours.Link;
                    m_statsMenu.OnClicked += delegate
                    {
                        Game.Network.OpenAchievementsHub();
                    };
                }
            }
        }

        protected override void OnInit()
        {
            base.OnInit();
            for (int i = 0; i < m_statsText.Length; ++i)
            {
                var text = m_statsText[i];
                Game.Screen.Elements.Add(text);
            }
            if (m_statsMenu != null)
            {
                Game.Screen.Elements.Add(m_statsMenu);
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            ShowSelectPrompt = Game.Screen.InputMethod != InputMethod.Mouse && (m_statsMenu != null && m_statsMenu.Focus >= 0);
        }

        protected override void OnShutdown()
        {
            for (int i = 0; i < m_statsText.Length; ++i)
            {
                var text = m_statsText[i];
                Game.Screen.Elements.Remove(text);
                text.Dispose();
            }
            if (m_statsMenu != null)
            {
                Game.Screen.Elements.Remove(m_statsMenu);
            }
            base.OnShutdown();
        }

        protected override void GoBack()
        {
            WipeToState(new MainOptionsState(Game));
        }
    }
}

