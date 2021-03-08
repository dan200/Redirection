using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Input;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.GUI
{
    public enum Unlockable
    {
        Level,
        Levels,
        Arcade,
        ArcadeGame
    }

    public class LevelCompleteDetails
    {
        public int? RobotsRescued;
        public int ObstaclesRemaining;
        public readonly List<Unlockable> ThingsUnlocked;

        public LevelCompleteDetails()
        {
            RobotsRescued = null;
            ThingsUnlocked = new List<Unlockable>();
            ObstaclesRemaining = 0;
        }
    }

    public class LevelCompleteMessage : Element
    {
        private static Vector4 BACKGROUND_COLOUR = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);

        private const float GROW_TIME = 0.5f;

        private Geometry m_background;
        private Text m_mainLine;
        private List<Text> m_infoLines;
        private InputPrompt m_continuePrompt;

        private float m_size;
        private float m_targetSize;

        private static string GetIcon(Unlockable unlockable)
        {
            switch (unlockable)
            {
                case Unlockable.Level:
                case Unlockable.Levels:
                default:
                    {
                        return "[gui/prompts/cone.png]";
                    }
                case Unlockable.Arcade:
                case Unlockable.ArcadeGame:
                    {
                        return "[gui/prompts/floppy.png]";
                    }
            }
        }

        public bool Closed
        {
            get
            {
                return m_targetSize <= 0.0f && m_size <= 0.0f;
            }
        }

        private void AddLine(string text)
        {
            var line = new Text(UIFonts.Smaller, text, UIColours.Text, TextAlignment.Center);
            line.Anchor = Anchor.CentreMiddle;
            line.LocalPosition = new Vector2(0.0f, 0.5f * m_mainLine.Height + m_infoLines.Count * UIFonts.Smaller.Height);
            m_infoLines.Add(line);
        }

        public LevelCompleteMessage(Game.Game game, LevelCompleteDetails details)
        {
            m_background = new Geometry(Primitive.Triangles);

            m_mainLine = new Text(
                UIFonts.Bigger,
                game.Language.Translate("menus.level_complete.title"),
                UIColours.Text,
                TextAlignment.Center
            );
            m_mainLine.Anchor = Anchor.CentreMiddle;
            m_mainLine.LocalPosition = new Vector2(0.0f, -0.5f * m_mainLine.Height);

            m_infoLines = new List<Text>();
            if (details.RobotsRescued.HasValue)
            {
                AddLine("[gui/red_robot.png] " + game.Language.TranslateCount("stats.robots_rescued", details.RobotsRescued.Value));
            }
            for (int i = 0; i < details.ThingsUnlocked.Count; ++i)
            {
                var thing = details.ThingsUnlocked[i];
                AddLine(GetIcon(thing) + " " + game.Language.Translate("menus.level_complete." + thing.ToString().ToLowerUnderscored() + "_unlocked"));
            }
            if (details.ObstaclesRemaining > 0)
            {
                AddLine("[gui/optimisation.png] " + game.Language.Translate("menus.level_complete.optimisation"));
            }

            m_continuePrompt = new InputPrompt(UIFonts.Smaller, game.Language.Translate("menus.continue"), TextAlignment.Right);
            m_continuePrompt.Key = Key.Return;
            m_continuePrompt.MouseButton = MouseButton.Left;
            m_continuePrompt.GamepadButton = GamepadButton.A;
            m_continuePrompt.SteamControllerButton = SteamControllerButton.MenuSelect;
            m_continuePrompt.Anchor = Anchor.BottomRight;
            m_continuePrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_continuePrompt.Height);
            m_continuePrompt.OnClick += delegate (object o, EventArgs args)
            {
                Close();
            };

            m_size = 0.0f;
            m_targetSize = 1.0f;
        }

        public override void Dispose()
        {
            base.Dispose();
            m_mainLine.Dispose();
            for (int i = 0; i < m_infoLines.Count; ++i)
            {
                var line = m_infoLines[i];
                line.Dispose();
            }
            m_continuePrompt.Dispose();
            m_background.Dispose();
        }

        protected override void OnInit()
        {
            m_mainLine.Init(Screen);
            for (int i = 0; i < m_infoLines.Count; ++i)
            {
                var line = m_infoLines[i];
                line.Init(Screen);
            }
            m_continuePrompt.Init(Screen);
            UpdateTitleSize();
        }

        protected override void OnUpdate(float dt)
        {
            // Update self
            if (m_size < m_targetSize)
            {
                m_size = Math.Min(m_size + (dt / GROW_TIME), m_targetSize);
                UpdateTitleSize();
            }
            else if (m_size > m_targetSize)
            {
                m_size = Math.Max(m_size - (dt / GROW_TIME), m_targetSize);
                UpdateTitleSize();
            }

            // Update children
            m_mainLine.Update(dt);
            for (int i = 0; i < m_infoLines.Count; ++i)
            {
                var line = m_infoLines[i];
                line.Update(dt);
            }
            m_continuePrompt.Update(dt);

            // Check close
            if (m_targetSize >= 1.0f && m_size >= 1.0f && Screen.ModalDialog == null)
            {
                if (Screen.CheckSelect())
                {
                    Close();
                }
                else if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    Close();
                }
            }
        }

        private void Close()
        {
            if (m_targetSize >= 1.0f && m_size >= 1.0f)
            {
                m_targetSize = 0.0f;
            }
        }

        private void UpdateTitleSize()
        {
            var f = MathUtils.Ease(m_size);
            m_mainLine.Scale = f;
            m_mainLine.LocalPosition = new Vector2(0.0f, -0.5f * m_mainLine.Height);
        }

        protected override void OnRebuild()
        {
            m_background.Clear();
            m_background.Add2DQuad(Vector2.Zero, new Vector2(Screen.Width, Screen.Height));
            m_background.Rebuild();

            m_mainLine.RequestRebuild();
            for (int i = 0; i < m_infoLines.Count; ++i)
            {
                var line = m_infoLines[i];
                line.RequestRebuild();
            }
            m_continuePrompt.RequestRebuild();
        }

        protected override void OnDraw()
        {
            var bgColour = BACKGROUND_COLOUR;
            bgColour.W *= MathUtils.Ease(m_size);
            Screen.Effect.Colour = bgColour;
            Screen.Effect.Texture = Texture.White;
            Screen.Effect.Bind();
            m_background.Draw();

            m_mainLine.Draw();
            if (m_size >= 1.0f)
            {
                for (int i = 0; i < m_infoLines.Count; ++i)
                {
                    var line = m_infoLines[i];
                    line.Draw();
                }
                m_continuePrompt.Draw();
            }
        }
    }
}
