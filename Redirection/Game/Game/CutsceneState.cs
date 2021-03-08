using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Game
{
    public enum CutsceneContext
    {
        LevelIntro,
        LevelOutro,
        Credits,
        Test
    }

    public class CutsceneState : LevelState
    {
        private Mod m_mod;
        private CutsceneContext m_context;
        private Playthrough m_playthrough;
        private string m_initialShotPath;

        private AnimatedCameraController m_animatedCamera;
        private ScrollingText m_scrollingText;
        private Terminal m_terminal;
        private InputPrompt m_skipPrompt;
        private CutsceneBorder m_border;

        private Dictionary<string, IStoppable> m_loopingSounds;

        public bool IsScrollingTextVisible
        {
            get
            {
                return m_scrollingText != null && !m_scrollingText.IsFinished;
            }
        }

        public float ScrollingTextTimeLeft
        {
            get
            {
                if (m_scrollingText != null)
                {
                    return m_scrollingText.TimeLeft;
                }
                return 0.0f;
            }
        }

        public string InitialShotPath
        {
            get
            {
                return m_initialShotPath;
            }
            private set
            {
                m_initialShotPath = value;
            }
        }

        public Campaign Campaign
        {
            get
            {
                if (m_playthrough != null)
                {
                    return m_playthrough.Campaign;
                }
                return null;
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
                if (m_playthrough != null)
                {
                    return m_playthrough.Level;
                }
                return -1;
            }
        }

        public CutsceneState(Game game, Mod mod, string path, CutsceneContext context, Playthrough playthrough = null) : base(game, path, LevelOptions.Cutscene)
        {
            m_mod = mod;
            m_context = context;
            m_playthrough = playthrough;
            m_initialShotPath = path;

            m_border = new CutsceneBorder(true);
            m_animatedCamera = new AnimatedCameraController(Level.TimeMachine);

            m_skipPrompt = new InputPrompt(UIFonts.Smaller, game.Language.Translate("menus.skip"), TextAlignment.Right);
            m_skipPrompt.Anchor = Anchor.BottomRight;
            m_skipPrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_skipPrompt.Font.Height);
            m_skipPrompt.GamepadButton = GamepadButton.A;
            m_skipPrompt.SteamControllerButton = SteamControllerButton.MenuSelect;
            m_skipPrompt.MouseButton = MouseButton.Left;
            m_skipPrompt.Key = Key.Return;
            m_skipPrompt.OnClick += delegate (object sender, EventArgs e)
            {
                Continue();
            };

            m_loopingSounds = new Dictionary<string, IStoppable>();

            var termFont = UIFonts.Smaller;
            var screenHeight = game.Screen.Height - 2.0f * m_border.BarHeight;
            var numLines = (int)(screenHeight / termFont.Height);
            var screenWidth = game.Screen.Width - (game.Screen.Height - numLines * termFont.Height);
            m_terminal = new Terminal(termFont, UIColours.White, screenWidth, numLines);
            m_terminal.Anchor = Anchor.CentreMiddle;
            m_terminal.LocalPosition = new Vector2(-0.5f * m_terminal.Width, -0.5f * m_terminal.Height);
        }

        protected override void OnReveal()
        {
            base.OnReveal();

            // Add the GUI
            Game.Screen.Elements.Add(m_border);
            Game.Screen.Elements.AddBefore(m_terminal, m_border);

            // Start the cutscene
            if (ScriptController != null && ScriptController.HasFunction("run_cutscene"))
            {
                ScriptController.StartFunction("run_cutscene", LuaArgs.Empty);
            }
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Add the GUI
            Game.Screen.Elements.Add(m_skipPrompt);
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();

            // Remove the GUI
            Game.Screen.Elements.Remove(m_skipPrompt);
            m_skipPrompt.Dispose();
        }

        protected override void OnHide()
        {
            base.OnHide();

            // Stop all the sounds
            foreach (var sound in m_loopingSounds.Values)
            {
                sound.Stop();
            }

            // Remove the GUI
            if (m_scrollingText != null)
            {
                Game.Screen.Elements.Remove(m_scrollingText);
                m_scrollingText.Dispose();
                m_scrollingText = null;
            }
            if (m_terminal != null)
            {
                Game.Screen.Elements.Remove(m_terminal);
                m_terminal.Dispose();
                m_terminal = null;
            }

            Game.Screen.Elements.Remove(m_border);
            m_border.Dispose();
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            // Sample the animation
            m_animatedCamera.Populate(camera);

            // Transform from level to world space
            MathUtils.FastInvert(ref camera.Transform);
            camera.Transform = camera.Transform * Level.Transform;
            MathUtils.FastInvert(ref camera.Transform);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            // Update input
            if (Game.Screen.ModalDialog == null)
            {
                if ((App.Debug || (m_mod != null && m_mod.Source == ModSource.Editor)) && Game.Keyboard.Keys[Key.E].Pressed)
                {
                    var campaign = (m_playthrough != null) ? m_playthrough.Campaign : null;
                    var levelIndex = (m_playthrough != null) ? m_playthrough.Level : -1;
                    CutToState(new EditorState(Game, m_mod, campaign, levelIndex, LevelLoadPath, LevelLoadPath));
                }

                // Check input
                if (Game.Screen.CheckSelect() || Game.Screen.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    Skip();
                }
                else if (Game.Keyboard.Keys[Key.R].Pressed)
                {
                    Restart();
                }
                else if (Game.Keyboard.Keys[Key.T].Pressed)
                {
                    RestartShot();
                }
            }
        }

        public override void OnReloadAssets()
        {
            base.OnReloadAssets();
            m_skipPrompt.String = Game.Language.Translate("menus.skip");
        }

        public void StartCameraAnimation(IAnimation animation)
        {
            m_animatedCamera.Play(animation);
        }

        public void StartScrollingText(TextAsset text)
        {
            StopScrollingText();
            m_scrollingText = new ScrollingText(text, m_border);
            Game.Screen.Elements.AddBefore(m_scrollingText, m_border);
        }

        public void StopScrollingText()
        {
            if (m_scrollingText != null)
            {
                Game.Screen.Elements.Remove(m_scrollingText);
                m_scrollingText.Dispose();
                m_scrollingText = null;
            }
        }

        public void SetTerminalLine(int i, string text)
        {
            m_terminal.SetLine(i, text);
        }

        public void SetTerminalAlignment(int i, TextAlignment alignment)
        {
            m_terminal.SetAlignment(i, alignment);
        }

        public int WrapTerminalLine(string text, int start)
        {
            return m_terminal.WordWrap(text, start);
        }

        public string GetTerminalLine(int i)
        {
            return m_terminal.GetLine(i);
        }

        public int GetTerminalHeight()
        {
            return m_terminal.Lines;
        }

        public void ScrollTerminal(int i)
        {
            m_terminal.Scroll(i);
        }

        public void ClearTerminal()
        {
            m_terminal.Clear();
        }

        public void PlaySound(string path, bool looping)
        {
            if (looping)
            {
                if (!m_loopingSounds.ContainsKey(path))
                {
                    var stoppable = Game.Audio.PlaySound(path, true);
                    if (stoppable != null)
                    {
                        m_loopingSounds.Add(path, stoppable);
                    }
                }
            }
            else
            {
                Game.Audio.PlaySound(path, false);
            }
        }

        public void StopSound(string path)
        {
            IStoppable stoppable;
            if (m_loopingSounds.TryGetValue(path, out stoppable))
            {
                stoppable.Stop();
                m_loopingSounds.Remove(path);
            }
        }

        public void PlayMusic(string path, float transition, bool looping)
        {
            Game.Audio.PlayMusic(path, transition, looping);
        }

        public void Rumble(float strength, float duration)
        {
            if (Game.Screen.InputMethod == InputMethod.Gamepad)
            {
                Game.ActiveGamepad.Rumble(strength, duration);
            }
        }

        public void CutTo(string levelPath)
        {
            var nextShotState = new CutsceneState(Game, m_mod, levelPath, m_context, m_playthrough);
            nextShotState.InitialShotPath = InitialShotPath;
            CutToState(nextShotState);
        }

        public void WipeTo(string levelPath)
        {
            var nextShotState = new CutsceneState(Game, m_mod, levelPath, m_context, m_playthrough);
            nextShotState.InitialShotPath = InitialShotPath;
            WipeToState(nextShotState);
        }

        public void Skip()
        {
            Continue();
        }

        public void Complete()
        {
            if (m_mod == null)
            {
                if (m_context == CutsceneContext.LevelIntro &&
                    m_playthrough.Level == 0)
                {
                    Game.User.Progress.UnlockAchievement(Achievement.WatchIntro);
                    Game.User.Progress.Save();
                }
                else if (m_context == CutsceneContext.LevelOutro &&
                    m_playthrough.Level == m_playthrough.Campaign.Levels.Count - 1)
                {
                    Game.User.Progress.UnlockAchievement(Achievement.WatchOutro);
                    Game.User.Progress.Save();
                }
            }
            Continue();
        }

        private void Continue()
        {
            if (m_context == CutsceneContext.LevelIntro)
            {
                // Go to first level
                WipeToState(new CampaignState(Game, m_mod, m_playthrough));
            }
            else if (m_context == CutsceneContext.LevelOutro)
            {
                if (m_playthrough.CampaignCompleted)
                {
                    // Go to Game Over
                    WipeToState(new GameOverState(Game, m_mod, m_playthrough));
                }
                else
                {
                    // Go to level select
                    int level = m_playthrough.Level;
                    int page = level / LevelSelectState.NUM_PER_PAGE;
                    int highlight = Game.Screen.InputMethod != InputMethod.Mouse ?
                        (level % LevelSelectState.NUM_PER_PAGE) :
                        -1;
                    WipeToState(new LevelSelectState(Game, m_mod, m_playthrough.Campaign, page, highlight, false, m_playthrough.JustCompletedLevel));
                }
            }
            else if (m_context == CutsceneContext.Credits)
            {
                // Back to options menu
                WipeToState(new MainOptionsState(Game));
            }
            else if (m_context == CutsceneContext.Test)
            {
                // Restart cutscene
                Restart();
            }
        }

        public void Restart()
        {
            WipeToState(new CutsceneState(Game, m_mod, m_initialShotPath, m_context, m_playthrough));
        }

        public void RestartShot()
        {
            var newState = new CutsceneState(Game, m_mod, LevelLoadPath, m_context, m_playthrough);
            newState.InitialShotPath = InitialShotPath;
            WipeToState(newState);
        }
    }
}

