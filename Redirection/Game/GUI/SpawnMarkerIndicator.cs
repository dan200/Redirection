using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Game.Game;
using Dan200.Game.Input;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class SpawnMarkerIndicator : EntityIndicator<SpawnMarker>
    {
        private const float ARROW_SIZE = 28.0f;
        private const float ARROW_OFFSET = 22.0f;

        private const float REPEAT_DELAY = 0.5f;
        private const float REPEAT_RATE = 8.0f;

        private Font m_font;
        private Geometry[] m_textGeometry;
        private Texture[] m_textTextures;
        private Geometry m_leftArrowGeometry;
        private Geometry m_rightArrowGeometry;
        private Texture m_arrowTexture;

        private InGameState m_state;
        private bool m_selected;

        private int m_hover;
        private float m_holdTime;

        private int? m_lastNumKey;

        public bool Selected
        {
            get
            {
                return m_selected;
            }
            set
            {
                m_selected = value;
            }
        }

        public SpawnMarkerIndicator(InGameState state, SpawnMarker entity, Camera camera) : base(entity, camera)
        {
            m_font = UIFonts.Default;//Font.Get( "fonts/countdown.fnt" );
            m_textGeometry = new Geometry[m_font.PageCount + 2];
            m_textTextures = new Texture[m_font.PageCount + 2];
            for (int i = 0; i < m_textGeometry.Length; ++i)
            {
                m_textGeometry[i] = new Geometry(Primitive.Triangles, 8, 12, false);
            }

            m_leftArrowGeometry = new Geometry(Primitive.Triangles, 4, 6, true);
            m_rightArrowGeometry = new Geometry(Primitive.Triangles, 4, 6, true);
            m_arrowTexture = Texture.Get("gui/arrows.png", true);

            m_state = state;
            m_selected = false;

            m_hover = 0;
            m_holdTime = -1.0f;

            m_lastNumKey = null;
        }

        private Vector2 GetCenterPos()
        {
            return CalculatePosition(new Vector3(0.5f, 0.375f, 0.5f));
        }

        public int TestMouse()
        {
            var pos = GetCenterPos();
            if (Screen.MousePosition.X >= pos.X - (ARROW_OFFSET + ARROW_SIZE) &&
                Screen.MousePosition.X < pos.X - ARROW_OFFSET &&
                Screen.MousePosition.Y >= pos.Y - 0.5f * ARROW_SIZE &&
                Screen.MousePosition.Y < pos.Y + 0.5f * ARROW_SIZE)
            {
                return -1;
            }
            if (Screen.MousePosition.X >= pos.X + ARROW_OFFSET &&
                Screen.MousePosition.X < pos.X + (ARROW_OFFSET + ARROW_SIZE) &&
                Screen.MousePosition.Y >= pos.Y - 0.5f * ARROW_SIZE &&
                Screen.MousePosition.Y < pos.Y + 0.5f * ARROW_SIZE)
            {
                return 1;
            }
            return 0;
        }

        private bool CheckTweak(out int o_tweakDirection)
        {
            var dialog = Screen.ModalDialog;
            var allowInput = dialog == null || (dialog is DialogBox && !((DialogBox)dialog).BlockInput);
            if (allowInput)
            {
                if (Screen.SteamController != null)
                {
                    if (Screen.SteamController.Buttons[SteamControllerButton.InGameTweakUp.GetID()].Held)
                    {
                        Screen.InputMethod = InputMethod.SteamController;
                        o_tweakDirection = 1;
                        return true;
                    }
                    if (Screen.SteamController.Buttons[SteamControllerButton.InGameTweakDown.GetID()].Held)
                    {
                        Screen.InputMethod = InputMethod.SteamController;
                        o_tweakDirection = -1;
                        return true;
                    }
                }
                if (Screen.Gamepad != null)
                {
                    if (Screen.Gamepad.Buttons[m_state.Game.User.Settings.GetPadBind(Bind.IncreaseDelay)].Held)
                    {
                        Screen.InputMethod = InputMethod.Gamepad;
                        o_tweakDirection = 1;
                        return true;
                    }
                    if (Screen.Gamepad.Buttons[m_state.Game.User.Settings.GetPadBind(Bind.DecreaseDelay)].Held)
                    {
                        Screen.InputMethod = InputMethod.Gamepad;
                        o_tweakDirection = -1;
                        return true;
                    }
                }
                if (Screen.Mouse.Buttons[MouseButton.Left].Held)
                {
                    var mouse = TestMouse();
                    if (mouse != 0)
                    {
                        Screen.InputMethod = InputMethod.Mouse;
                        o_tweakDirection = mouse;
                        return true;
                    }
                }
                if (Screen.Keyboard.Keys[m_state.Game.User.Settings.GetKeyBind(Bind.IncreaseDelay)].Held)
                {
                    Screen.InputMethod = InputMethod.Keyboard;
                    o_tweakDirection = 1;
                    return true;
                }
                if (Screen.Keyboard.Keys[m_state.Game.User.Settings.GetKeyBind(Bind.DecreaseDelay)].Held)
                {
                    Screen.InputMethod = InputMethod.Keyboard;
                    o_tweakDirection = -1;
                    return true;
                }
            }
            o_tweakDirection = 0;
            return false;
        }

        private int GetRepeatCount(float holdTime)
        {
            if (holdTime < 0.0f)
            {
                return 0;
            }
            else if (holdTime < REPEAT_DELAY)
            {
                return 1;
            }
            else
            {
                return 2 + (int)((holdTime - REPEAT_DELAY) * REPEAT_RATE);
            }
        }

        private bool CheckTweakRepeat(float dt, out int o_tweakDirection)
        {
            if (CheckTweak(out o_tweakDirection))
            {
                var oldTime = m_holdTime;
                if (oldTime < 0.0f)
                {
                    m_holdTime = 0.0f;
                    return true;
                }
                m_holdTime += dt;
                if (GetRepeatCount(oldTime) != GetRepeatCount(m_holdTime))
                {
                    return true;
                }
                return false;
            }
            else
            {
                m_holdTime = -1.0f;
                return false;
            }
        }

        private int? CheckNumber()
        {
            if (Screen.Keyboard.Keys[Key.Zero].Pressed || Screen.Keyboard.Keys[Key.NumpadZero].Pressed)
            {
                Screen.InputMethod = InputMethod.Keyboard;
                return 0;
            }
            for (int i = 1; i <= 9; ++i)
            {
                if (Screen.Keyboard.Keys[(Key)((int)Key.One + i - 1)].Pressed ||
                    Screen.Keyboard.Keys[(Key)((int)Key.NumpadOne + i - 1)].Pressed)
                {
                    Screen.InputMethod = InputMethod.Keyboard;
                    return i;
                }
            }
            return null;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            m_hover = TestMouse();

            if (m_selected && !m_state.TweakDisabled && m_state.State == GameState.Planning)
            {
                int tweakDirection;
                if (CheckTweakRepeat(dt, out tweakDirection))
                {
                    if (tweakDirection > 0 && Entity.SpawnDelay < SpawnMarker.MAX_DELAY)
                    {
                        Entity.SpawnDelay = Entity.SpawnDelay + 1;
                        Screen.Audio.PlaySound("sound/tweak_countdown.wav");
                    }
                    else if (tweakDirection < 0 && Entity.SpawnDelay > 0)
                    {
                        Entity.SpawnDelay = Entity.SpawnDelay - 1;
                        Screen.Audio.PlaySound("sound/tweak_countdown.wav");
                    }
                    m_lastNumKey = null;
                }

                var numKey = CheckNumber();
                if (numKey.HasValue)
                {
                    int number = numKey.Value;
                    if (m_lastNumKey.HasValue)
                    {
                        number += 10 * m_lastNumKey.Value;
                    }
                    if (number > SpawnMarker.MAX_DELAY)
                    {
                        Entity.SpawnDelay = SpawnMarker.MAX_DELAY;
                        Screen.Audio.PlaySound("sound/tweak_countdown.wav");
                        m_lastNumKey = (Entity.SpawnDelay % 10);
                    }
                    else
                    {
                        Entity.SpawnDelay = number;
                        Screen.Audio.PlaySound("sound/tweak_countdown.wav");
                        m_lastNumKey = numKey;
                    }
                }
            }
            else
            {
                m_holdTime = -1.0f;
                m_lastNumKey = null;
            }
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();

            // Get position
            var position = GetCenterPos();

            // Rebuild text
            var font = m_font;
            var timeLeft = Entity.TimeLeft;
            var text = Math.Ceiling(timeLeft).ToString();
            var width = font.Measure(text, true);
            var height = font.Height;
            font.Render(
                text,
                position.X - 0.5f * width, position.Y - 0.5f * height - 1.0f,
                m_textGeometry,
                m_textTextures,
                false,
                1.0f
            );

            // Rebuild arrows
            m_leftArrowGeometry.Clear();
            m_leftArrowGeometry.Add2DQuad(
                position + new Vector2(-(ARROW_SIZE + ARROW_OFFSET), -0.5f * ARROW_SIZE),
                position + new Vector2(-(ARROW_OFFSET), 0.5f * ARROW_SIZE),
                new Quad(0.0f, 0.5f, 0.5f, 0.5f)
            );
            m_leftArrowGeometry.Rebuild();

            m_rightArrowGeometry.Clear();
            m_rightArrowGeometry.Add2DQuad(
                position + new Vector2(ARROW_OFFSET, -0.5f * ARROW_SIZE),
                position + new Vector2(ARROW_OFFSET + ARROW_SIZE, 0.5f * ARROW_SIZE),
                new Quad(0.0f, 0.0f, 0.5f, 0.5f)
            );
            m_rightArrowGeometry.Rebuild();
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            if (!Entity.Spawned)
            {
                var alpha = Entity.CalculateCurrentGhostAlpha();
                if ((m_selected || Entity.SpawnDelay > 0) && alpha > 0.0f)
                {
                    // Draw the text
                    var textColour = new Vector4(1.0f, 1.0f, 1.0f, alpha);
                    for (int i = 0; i < m_textGeometry.Length; ++i)
                    {
                        var geometry = m_textGeometry[i];
                        if (geometry.IndexCount > 0)
                        {
                            Screen.Effect.Colour = textColour;
                            Screen.Effect.Texture = m_textTextures[i];
                            Screen.Effect.Bind();
                            geometry.Draw();
                        }
                    }
                }
                if (m_selected && m_state.State == GameState.Planning && !m_state.TweakDisabled)
                {
                    // Draw the arrows
                    if (Entity.SpawnDelay > 0)
                    {
                        Screen.Effect.Colour = m_hover == -1 ? UIColours.Hover : UIColours.Text;
                        Screen.Effect.Texture = m_arrowTexture;
                        Screen.Effect.Bind();
                        m_leftArrowGeometry.Draw();
                    }
                    else
                    {
                        Screen.Effect.Colour = UIColours.Disabled;
                        Screen.Effect.Texture = m_arrowTexture;
                        Screen.Effect.Bind();
                        m_leftArrowGeometry.Draw();
                    }

                    if (Entity.SpawnDelay < SpawnMarker.MAX_DELAY)
                    {
                        Screen.Effect.Colour = m_hover == 1 ? UIColours.Hover : UIColours.Text;
                        Screen.Effect.Texture = m_arrowTexture;
                        Screen.Effect.Bind();
                        m_rightArrowGeometry.Draw();
                    }
                    else
                    {
                        Screen.Effect.Colour = UIColours.Disabled;
                        Screen.Effect.Texture = m_arrowTexture;
                        Screen.Effect.Bind();
                        m_rightArrowGeometry.Draw();
                    }
                }
            }
        }
    }
}
