
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Game.Arcade;
using Dan200.Game.GUI;
using Dan200.Game.User;
using System;
using System.Linq;

namespace Dan200.Game.Game
{
    public class CampaignState : InGameState
    {
        private Mod m_mod;
        private Playthrough m_playthrough;
        private bool m_newLevel;

        public Campaign Campaign
        {
            get
            {
                return m_playthrough.Campaign;
            }
        }

        public Mod Mod
        {
            get
            {
                return m_mod;
            }
        }

        public int LevelIndex
        {
            get
            {
                return m_playthrough.Level;
            }
        }

        public bool NewLevel
        {
            get
            {
                return m_newLevel;
            }
        }

        public CampaignState(Game game, Mod mod, Playthrough playthrough) : base(game, playthrough.Campaign.Levels[playthrough.Level])
        {
            m_mod = mod;
            m_playthrough = playthrough;
            m_newLevel = !game.User.Progress.IsLevelCompleted(Level.Info.ID);
        }

        protected override void Reset()
        {
            if (!Game.User.Progress.IsLevelCompleted(Level.Info.ID))
            {
                Game.User.Progress.AddPlaytime(Level.Info.ID, TimeInState);
            }
            WipeToState(new CampaignState(Game, m_mod, m_playthrough));
        }

        protected override void OnMenuRequested()
        {
            ShowPauseMenu();
        }

        private void ShowPauseMenu()
        {
            // Regular pause menu
            var pauseMenu = DialogBox.CreateMenuBox(
                Game.Language.Translate("menus.pause.title"),
                new string[] {
                    Game.Language.Translate( "menus.pause.reset_level" ),
                    //Game.Language.Translate( "menus.pause.skip_level" ),
					Game.Language.Translate( "menus.pause.go_back" ),
                },
                false
            );
            pauseMenu.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // Restart Level
                            Reset();
                            break;
                        }
                    //case 1:
                    //{
                    //    // Skip Level
                    //    OnLevelCompleted( true, 0, 0 );
                    //    break;
                    //}
                    //case 2:
                    case 1:
                        {
                            // Go Back
                            BackToMenu(m_playthrough.Level);
                            break;
                        }
                }
            };
            ShowDialog(pauseMenu);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            // Check debug shortcuts
            if ((App.Debug || (m_mod != null && m_mod.Source == ModSource.Editor)) && Game.Keyboard.Keys[Key.E].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                CutToState(new EditorState(Game, m_mod, m_playthrough.Campaign, m_playthrough.Level, LevelLoadPath, LevelLoadPath));
            }
        }

        protected override void OnOutroStarted(int robotsSaved, LevelCompleteDetails o_completeDetails)
        {
            int levelsPreviouslyUnlocked = 0;
            bool arcadePreviouslyUnlocked = false;
            int arcadeGamesPreviouslyUnlocked = 0;
            if (m_newLevel)
            {
                levelsPreviouslyUnlocked = ProgressUtils.CountLevelsUnlocked(m_playthrough.Campaign, m_mod, Game.User);
                if (ArcadeUtils.IsArcadeUnlocked(Game.User.Progress))
                {
                    arcadePreviouslyUnlocked = true;
                    arcadeGamesPreviouslyUnlocked = ArcadeUtils.GetAllDisks().Count(
                        disk => ArcadeUtils.IsDiskUnlocked(disk.Disk, disk.Mod, Game.User.Progress)
                    );
                }
            }

            // Mark the level as completed and unlock achivements
            if (m_mod == null && Level.Info.PlacementsLeft > 0)
            {
                Game.User.Progress.UnlockAchievement(Achievement.Optimisation);
            }

            var obstaclesUsed = Level.Info.TotalPlacements - Level.Info.PlacementsLeft;
            Game.User.Progress.SetLevelCompleted(Level.Info.ID, obstaclesUsed);

            if (m_newLevel)
            {
                Game.User.Progress.AddPlaytime(Level.Info.ID, TimeInState);
                Game.User.Progress.AddStatistic(Statistic.RobotsRescued, robotsSaved);
                Game.User.Progress.IncrementStatistic(Statistic.LevelsCompleted);
                if (m_mod != null && m_mod.Source == ModSource.Workshop)
                {
                    Game.User.Progress.IncrementStatistic(Statistic.WorkshopLevelsCompleted);
                }
                else if (m_mod == null)
                {
                    Game.User.Progress.IncrementStatistic(Statistic.CampaignLevelsCompleted);
                    if (Game.User.Progress.GetStatistic(Statistic.CampaignLevelsCompleted) > 0)
                    {
                        Game.User.Progress.UnlockAchievement(Achievement.CompleteFirstLevel);
                    }
                    if (Game.User.Progress.GetStatistic(Statistic.CampaignLevelsCompleted) >= m_playthrough.Campaign.Levels.Count)
                    {
                        Game.User.Progress.UnlockAchievement(Achievement.CompleteAllLevels);
                    }
                }
            }
            Game.User.Progress.Save();

            // Determine what was unlocked
            int unused;
            o_completeDetails.RobotsRescued = ProgressUtils.CountRobotsRescued(m_playthrough.Campaign, m_mod, Game.User, out unused);
            if (m_newLevel)
            {
                int newLevelsUnlocked = ProgressUtils.CountLevelsUnlocked(m_playthrough.Campaign, m_mod, Game.User) - levelsPreviouslyUnlocked;
                if (newLevelsUnlocked >= 2)
                {
                    o_completeDetails.ThingsUnlocked.Add(Unlockable.Levels);
                }
                else if (newLevelsUnlocked == 1)
                {
                    o_completeDetails.ThingsUnlocked.Add(Unlockable.Level);
                }

                bool arcadeUnlocked = ArcadeUtils.IsArcadeUnlocked(Game.User.Progress);
                if (arcadeUnlocked)
                {
                    if (!arcadePreviouslyUnlocked)
                    {
                        o_completeDetails.ThingsUnlocked.Add(Unlockable.Arcade);
                    }
                    else
                    {
                        int newArcadeGamesUnlocked = ArcadeUtils.GetAllDisks().Count(
                            disk => ArcadeUtils.IsDiskUnlocked(disk.Disk, disk.Mod, Game.User.Progress)
                        );
                        if (newArcadeGamesUnlocked > arcadeGamesPreviouslyUnlocked)
                        {
                            o_completeDetails.ThingsUnlocked.Add(Unlockable.ArcadeGame);
                        }
                    }
                }
            }
        }

        protected override void OnOutroComplete()
        {
            // Determine where to go next
            if (m_newLevel && Game.User.Progress.IsLevelCompleted(Level.Info.ID))
            {
                m_playthrough.JustCompletedLevel = m_playthrough.Level;
            }
            else
            {
                m_playthrough.JustCompletedLevel = -1;
            }

            if ((m_newLevel || m_playthrough.Level == m_playthrough.Campaign.Levels.Count - 1) &&
                ProgressUtils.IsCampaignCompleted(m_playthrough.Campaign, m_mod, Game.User))
            {
                m_playthrough.CampaignCompleted = true;
            }
            else if (m_newLevel)
            {
                // Find the next unlocked uncompleted level
                for (int i = m_playthrough.Level + 1; i < m_playthrough.Campaign.Levels.Count; ++i)
                {
                    if (!ProgressUtils.IsLevelCompleted(m_playthrough.Campaign, m_mod, i, Game.User) &&
                         ProgressUtils.IsLevelUnlocked(m_playthrough.Campaign, m_mod, i, Game.User))
                    {
                        m_playthrough.Level = i;
                        break;
                    }
                }
            }

            // Go there
            var outroPath = Level.Info.OutroPath;
            if (outroPath != null)
            {
                // Go to outro
                WipeToState(new CutsceneState(Game, m_mod, outroPath, CutsceneContext.LevelOutro, m_playthrough));
            }
            else if (m_playthrough.CampaignCompleted)
            {
                // Go to Game Over
                WipeToState(new GameOverState(Game, m_mod, m_playthrough));
            }
            else
            {
                // Go to level select
                BackToMenu(m_playthrough.Level);
            }
        }

        private void BackToMenu(int levelIndex)
        {
            if (!Game.User.Progress.IsLevelCompleted(Level.Info.ID))
            {
                Game.User.Progress.AddPlaytime(Level.Info.ID, TimeInState);
            }
            Game.User.Progress.Save();

            var levels = m_playthrough.Campaign.Levels;
            if (levels.Count > 1)
            {
                // Go back to level select
                int page = levelIndex / LevelSelectState.NUM_PER_PAGE;
                int highlight = Game.Screen.InputMethod != InputMethod.Mouse ?
                    (levelIndex % LevelSelectState.NUM_PER_PAGE) :
                    -1;
                WipeToState(new LevelSelectState(Game, m_mod, m_playthrough.Campaign, page, highlight, false, m_playthrough.JustCompletedLevel));
            }
            else
            {
                // Go back to campaign select
                Func<State> fnNextState = delegate ()
                {
                    if (App.Demo)
                    {
                        return new StartScreenState(Game);
                    }
                    else
                    {
                        return new CampaignSelectState(Game);
                    }
                };
                if (m_mod != null && !m_mod.AutoLoad)
                {
                    Assets.RemoveSource(m_mod.Assets);
                    m_mod.Loaded = false;
                    LoadToState(fnNextState);
                }
                else
                {
                    WipeToState(fnNextState.Invoke());
                }
            }
        }

        public static string FormatTime(float time)
        {
            int minutes = (int)Math.Floor(time / 60.0f);
            int seconds = (int)Math.Floor(time - (float)(minutes * 60));
            int hundreths = (int)Math.Floor((time - (float)(seconds + minutes * 60)) * 100.0f);
            return string.Format("{0:D1}:{1:D2}.{2:D2}", minutes, seconds, hundreths);
        }

        public static string FormatTime(float? time)
        {
            if (time.HasValue)
            {
                return FormatTime(time.Value);
            }
            else
            {
                return "-:--.--";
            }
        }
    }
}

