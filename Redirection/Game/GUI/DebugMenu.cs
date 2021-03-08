using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Game;
using OpenTK;
using System;
using System.IO;
using System.Linq;

namespace Dan200.Game.GUI
{
    public class DebugMenu : Element
    {
        private Game.Game m_game;
        private Text[] m_text;
        private Text[] m_statusText;
        private Text[] m_recentLogText;
        private int m_justPressed;
        private Promise<Screenshot> m_pendingScreenshot;

        public DebugMenu(Game.Game game)
        {
            string[] options = {
                App.Info.Title + " " + App.Info.Version,
                "F1 - Toggle Debug Menu",
                "F2 - Take Screenshot",
                "F3 - Toggle GUI",
                "F4 - Toggle FlyCam",
                "F5 - Reload Assets",
                "F6 - Toggle Debug Language",
                App.Platform != Platform.OSX ? "F11 - Toggle Fullscreen" : "", // OSX blocks this keys
			};

            m_game = game;
            m_text = new Text[options.Length];
            for (int i = 0; i < m_text.Length; ++i)
            {
                m_text[i] = new Text(UIFonts.Smaller, options[i], UIColours.Text, TextAlignment.Left);
                m_text[i].Anchor = Anchor.TopLeft;
                m_text[i].LocalPosition = new Vector2(0.0f, i * UIFonts.Smaller.Height);
            }
            m_statusText = new Text[3];
            for (int i = 0; i < m_statusText.Length; ++i)
            {
                m_statusText[i] = new Text(UIFonts.Smaller, "", UIColours.Blue, TextAlignment.Right);
                m_statusText[i].Anchor = Anchor.TopRight;
                m_statusText[i].LocalPosition = new Vector2(0.0f, i * UIFonts.Smaller.Height);
            }
            m_recentLogText = new Text[App.RECENT_LOG_SIZE];
            for (int i = 0; i < m_recentLogText.Length; ++i)
            {
                m_recentLogText[i] = new Text(UIFonts.Smaller, "", UIColours.White, TextAlignment.Left);
                m_recentLogText[i].Anchor = Anchor.BottomLeft;
                m_recentLogText[i].LocalPosition = new Vector2(0.0f, (-m_recentLogText.Length + i) * UIFonts.Smaller.Height);
                m_recentLogText[i].ParseImages = false;
            }

            m_justPressed = -1;
            UpdateColours();
        }

        private void UpdateColours()
        {
            m_text[0].Colour = UIColours.Red;
            for (int i = 1; i < m_text.Length; ++i)
            {
                m_text[i].Colour = (m_justPressed == i) ? UIColours.Blue : UIColours.Text;
            }
            m_text[1].Colour = Visible ? UIColours.Blue : UIColours.Text;
            m_text[3].Colour = m_game.RenderUI ? UIColours.Blue : UIColours.Text;
            m_text[4].Colour = m_game.UseDebugCamera ? UIColours.Blue : UIColours.Text;
            m_text[6].Colour = m_game.Language.IsDebug ? UIColours.Blue : UIColours.Text;
            m_text[7].Colour = m_game.Window.Fullscreen ? UIColours.Blue : UIColours.Text;
        }

        public override void Dispose()
        {
            // Dispose elements
            for (int i = 0; i < m_text.Length; ++i)
            {
                m_text[i].Dispose();
            }
            for (int i = 0; i < m_statusText.Length; ++i)
            {
                m_statusText[i].Dispose();
            }
            for (int i = 0; i < m_recentLogText.Length; ++i)
            {
                m_recentLogText[i].Dispose();
            }
            base.Dispose();
        }

        protected override void OnInit()
        {
            for (int i = 0; i < m_text.Length; ++i)
            {
                m_text[i].Init(Screen);
            }
            for (int i = 0; i < m_statusText.Length; ++i)
            {
                m_statusText[i].Init(Screen);
            }
            for (int i = 0; i < m_recentLogText.Length; ++i)
            {
                m_recentLogText[i].Init(Screen);
            }
        }

        protected override void OnUpdate(float dt)
        {
            if (m_justPressed >= 0)
            {
                m_justPressed = -1;
                UpdateColours();
            }

            // Update status text
            if (Visible)
            {
                m_statusText[0].String = string.Format("FPS: {0:F0}Hz Tris/Calls: {1}/{2}", App.FPS, RenderStats.Triangles, RenderStats.DrawCalls);

                var level = (m_game.CurrentState is LevelState) ? ((LevelState)m_game.CurrentState).LevelLoadPath : null;
                m_statusText[1].String = (level != null) ? level : "";

                var cameraTransInv = m_game.Camera.Transform;
                MathUtils.FastInvert(ref cameraTransInv);

                var cameraPos = Vector3.TransformPosition(Vector3.Zero, cameraTransInv);
                if (m_game.CurrentState is LevelState)
                {
                    var levelTransInv = ((LevelState)m_game.CurrentState).Level.Transform;
                    MathUtils.FastInvert(ref levelTransInv);
                    cameraPos = Vector3.TransformPosition(cameraPos, levelTransInv);
                }
                m_statusText[2].String = string.Format("{0:N2},{1:N2},{2:N2}", cameraPos.X, cameraPos.Y, cameraPos.Z);
            }

            // Update log
            if (Visible)
            {
                string[] log = App.RecentLog.ToArray();
                for (int i = 0; i < m_recentLogText.Length; ++i)
                {
                    var logIndex = i - (m_recentLogText.Length - log.Length);
                    if (logIndex >= 0)
                    {
                        var text = log[logIndex];
                        var error = text.ToLowerInvariant().Contains("error");
                        m_recentLogText[i].String = text;
                        m_recentLogText[i].Colour = error ? UIColours.Important : UIColours.Text;
                    }
                    else
                    {
                        m_recentLogText[i].String = "";
                    }
                }
            }

            // Handle option selection
            if (Screen.Keyboard.Keys[Key.F1].Pressed)
            {
                Visible = !Visible;
                if (Visible)
                {
                    GC.Collect();
                }
                m_justPressed = 1;
                UpdateColours();
            }
            if (Screen.Keyboard.Keys[Key.F2].Pressed)
            {
                m_pendingScreenshot = m_game.QueueScreenshot();
                m_justPressed = 2;
                UpdateColours();
            }
            if (Screen.Keyboard.Keys[Key.F3].Pressed)
            {
                m_game.RenderUI = !m_game.RenderUI;
                m_justPressed = 3;
                UpdateColours();
            }
            if (Screen.Keyboard.Keys[Key.F4].Pressed)
            {
                m_game.UseDebugCamera = !m_game.UseDebugCamera;
                m_justPressed = 4;
                UpdateColours();
            }
            if (Screen.Keyboard.Keys[Key.F5].Pressed)
            {
                App.Log("Reloading assets");
                Assets.ReloadAll();
                TextureAtlas.ReloadAll();
                LuaAnimation.ReloadAll();
                var state = m_game.CurrentState;
                if (state is LevelState)
                {
                    ((LevelState)state).OnReloadAssets();
                }
                GC.Collect();
                m_justPressed = 5;
                UpdateColours();
                App.Log("Assets reloaded", LogLevel.User);
            }
            if (Screen.Keyboard.Keys[Key.F6].Pressed)
            {
                if (m_game.Language.IsDebug)
                {
                    var desiredLanguageCode = m_game.Network.LocalUser.Language;
                    if (m_game.User.Settings.Language != "system")
                    {
                        desiredLanguageCode = m_game.User.Settings.Language;
                    }
                    m_game.Language = Language.GetMostSimilarTo(desiredLanguageCode);
                }
                else
                {
                    m_game.Language = Language.Get("languages/debug.lang");
                }
                var state = m_game.CurrentState;
                if (state is LevelState)
                {
                    ((LevelState)state).OnReloadAssets();
                }
                m_justPressed = 6;
                UpdateColours();
            }
            if (Screen.Keyboard.Keys[Key.F11].Pressed)
            {
                m_game.Window.Fullscreen = !m_game.Window.Fullscreen;
                m_game.User.Settings.Fullscreen = m_game.Window.Fullscreen;
                m_game.User.Settings.Save();
                m_justPressed = 7;
                UpdateColours();
            }

            // Handle requested screenshots
            if (m_pendingScreenshot != null && m_pendingScreenshot.Status != Status.Waiting)
            {
                if (m_pendingScreenshot.Status == Status.Complete)
                {
                    var screenshot = m_pendingScreenshot.Result;
                    screenshot.Save(
                        Path.Combine(
                            App.SavePath,
                            Path.Combine("screenshots", DateTime.Now.ToString("s").Replace(":", "-") + ".png")
                        )
                    );
                    screenshot.Dispose();
                }
                m_pendingScreenshot = null;
            }
        }

        protected override void OnRebuild()
        {
            for (int i = 0; i < m_text.Length; ++i)
            {
                m_text[i].RequestRebuild();
            }
            for (int i = 0; i < m_statusText.Length; ++i)
            {
                m_statusText[i].RequestRebuild();
            }
            for (int i = 0; i < m_recentLogText.Length; ++i)
            {
                m_recentLogText[i].RequestRebuild();
            }
        }

        protected override void OnDraw()
        {
            for (int i = 0; i < m_text.Length; ++i)
            {
                m_text[i].Draw();
            }
            for (int i = 0; i < m_statusText.Length; ++i)
            {
                m_statusText[i].Draw();
            }
            for (int i = 0; i < m_recentLogText.Length; ++i)
            {
                m_recentLogText[i].Draw();
            }
        }
    }
}

