using Dan200.Core.Audio;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using OpenTK;
using System;

namespace Dan200.Core.GUI
{
    public class Button : Element
    {
        private float m_width;
        private float m_height;

        private Vector4 m_colour;
        private Vector4 m_highlightColour;
        private Vector4 m_disabledColour;

        private Texture m_texture;
        private Quad m_region;
        private Quad m_highlightRegion;
        private Quad m_disabledRegion;

        private Geometry m_geometry;

        private Key? m_shortcutKey;
        private GamepadButton? m_shortcutButton;
        private GamepadButton? m_altShortcutButton;
        private SteamControllerButton? m_shortcutSteamControllerButton;

        private bool m_disabled;
        private bool m_hover;

        private Text m_shortcutPrompt;
        private bool m_alwaysShowShortcutPrompt;
        private bool m_allowDuringDialogue;
        private int m_frame;

        public Texture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                m_texture = value;
            }
        }

        public Quad Region
        {
            get
            {
                return m_region;
            }
            set
            {
                m_region = value;
                RequestRebuild();
            }
        }

        public Quad HighlightRegion
        {
            get
            {
                return m_highlightRegion;
            }
            set
            {
                m_highlightRegion = value;
                RequestRebuild();
            }
        }

        public Quad DisabledRegion
        {
            get
            {
                return m_disabledRegion;
            }
            set
            {
                m_disabledRegion = value;
                RequestRebuild();
            }
        }

        public float Width
        {
            get
            {
                return m_width;
            }
        }

        public float Height
        {
            get
            {
                return m_height;
            }
        }

        public Key? ShortcutKey
        {
            get
            {
                return m_shortcutKey;
            }
            set
            {
                m_shortcutKey = value;
            }
        }

        public GamepadButton? ShortcutButton
        {
            get
            {
                return m_shortcutButton;
            }
            set
            {
                m_shortcutButton = value;
            }
        }

        public GamepadButton? AltShortcutButton
        {
            get
            {
                return m_altShortcutButton;
            }
            set
            {
                m_altShortcutButton = value;
            }
        }

        public SteamControllerButton? ShortcutSteamControllerButton
        {
            get
            {
                return m_shortcutSteamControllerButton;
            }
            set
            {
                m_shortcutSteamControllerButton = value;
            }
        }

        public bool ShowShortcutPrompt
        {
            get
            {
                return m_shortcutPrompt.Visible;
            }
            set
            {
                m_shortcutPrompt.Visible = value;
                if (value && Screen != null)
                {
                    UpdatePromptText();
                }
            }
        }

        public bool AlwaysShowShortcutPrompt
        {
            get
            {
                return m_alwaysShowShortcutPrompt;
            }
            set
            {
                m_alwaysShowShortcutPrompt = value;
                if (Screen != null)
                {
                    UpdatePromptText();
                }
            }
        }

        public bool AllowDuringDialogue
        {
            get
            {
                return m_allowDuringDialogue;
            }
            set
            {
                m_allowDuringDialogue = value;
            }
        }

        public bool Pressed
        {
            get
            {
                return CheckPressed();
            }
        }

        public bool Held
        {
            get
            {
                return CheckHeld();
            }
        }

        public Vector4 Colour
        {
            get
            {
                return m_colour;
            }
            set
            {
                m_colour = value;
            }
        }

        public Vector4 HighlightColour
        {
            get
            {
                return m_highlightColour;
            }
            set
            {
                m_highlightColour = value;
            }
        }

        public Vector4 DisabledColour
        {
            get
            {
                return m_disabledColour;
            }
            set
            {
                m_disabledColour = value;
            }
        }

        public bool Disabled
        {
            get
            {
                return m_disabled;
            }
            set
            {
                m_disabled = value;
            }
        }

        public event EventHandler OnClicked;

        public Button(Texture texture, float width, float height)
        {
            m_texture = texture;
            m_width = width;
            m_height = height;

            m_geometry = new Geometry(Primitive.Triangles, 4, 18);

            m_colour = Vector4.One;
            m_highlightColour = Vector4.One;
            m_disabledColour = Vector4.One;

            m_region = Quad.UnitSquare;
            m_highlightRegion = Quad.UnitSquare;
            m_disabledRegion = Quad.UnitSquare;

            m_shortcutKey = null;
            m_shortcutButton = null;
            m_altShortcutButton = null;
            m_shortcutSteamControllerButton = null;
            m_shortcutPrompt = new Text((height >= 60.0f) ? UIFonts.Default : UIFonts.Smaller, "", UIColours.White, TextAlignment.Left);
            m_shortcutPrompt.Visible = false;
            m_alwaysShowShortcutPrompt = false;
            m_allowDuringDialogue = false;

            m_hover = false;
            m_frame = 0;
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
            m_shortcutPrompt.Dispose();
        }

        protected override void OnInit()
        {
            m_hover = TestMouse();
            m_shortcutPrompt.Init(Screen);
            UpdatePromptText();
        }

        protected override void OnUpdate(float dt)
        {
            var hover = TestMouse();
            if (hover != m_hover)
            {
                m_hover = hover;
                if (hover) PlayHighlightSound();
            }
            if (CheckPressed())
            {
                if (Screen.InputMethod == InputMethod.Mouse)
                {
                    PlaySelectSound();
                }
                if (OnClicked != null)
                {
                    OnClicked.Invoke(this, EventArgs.Empty);
                }
            }
            if (ShowShortcutPrompt)
            {
                UpdatePromptText();
            }
            m_frame++;
        }

        private void UpdatePromptText()
        {
            var method = Screen.InputMethod;
            if (method == InputMethod.Keyboard && m_shortcutKey.HasValue)
            {
                m_shortcutPrompt.String = m_shortcutKey.Value.GetPrompt();
            }
            else if (method == InputMethod.Gamepad && m_shortcutButton.HasValue)
            {
                m_shortcutPrompt.String = m_shortcutButton.Value.GetPrompt(Screen.Gamepad.Type);
            }
            else if (method == InputMethod.SteamController && m_shortcutSteamControllerButton.HasValue)
            {
                m_shortcutPrompt.String = m_shortcutSteamControllerButton.Value.GetPrompt(Screen.SteamController);
            }
            else
            {
                if (m_alwaysShowShortcutPrompt && m_shortcutKey.HasValue)
                {
                    m_shortcutPrompt.String = m_shortcutKey.Value.GetPrompt();
                }
                else
                {
                    m_shortcutPrompt.String = "";
                }
            }
        }

        protected override void OnRebuild()
        {
            // Rebuild self
            Vector2 origin = Position;
            m_geometry.Clear();
            m_geometry.Add2DQuad(origin, origin + new Vector2(m_width, m_height), m_region);
            m_geometry.Add2DQuad(origin, origin + new Vector2(m_width, m_height), m_highlightRegion);
            m_geometry.Add2DQuad(origin, origin + new Vector2(m_width, m_height), m_disabledRegion);
            m_geometry.Rebuild();

            // Rebuild text
            m_shortcutPrompt.Anchor = Anchor;
            m_shortcutPrompt.LocalPosition = LocalPosition + new Vector2(-6.0f, Height - m_shortcutPrompt.Height + 6.0f);
            m_shortcutPrompt.RequestRebuild();
        }

        protected override void OnDraw()
        {
            // Draw self
            var disabled = m_disabled || IsBlockingDialogPresent();
            Screen.Effect.Colour = disabled ? m_disabledColour : (m_hover ? m_highlightColour : m_colour);
            Screen.Effect.Texture = m_texture;
            Screen.Effect.Bind();
            if (disabled)
            {
                m_geometry.DrawRange(12, 6);
            }
            else if (m_hover)
            {
                m_geometry.DrawRange(6, 6);
            }
            else
            {
                m_geometry.DrawRange(0, 6);
            }

            // Draw prompt text
            if (!disabled)
            {
                m_shortcutPrompt.Draw();
            }
        }

        private bool IsBlockingDialogPresent()
        {
            if (Screen.ModalDialog == this.Parent)
            {
                return false;
            }
            else if (Screen.ModalDialog is DialogueBox)
            {
                var dialogue = (DialogueBox)Screen.ModalDialog;
                return dialogue.BlockInput && !m_allowDuringDialogue;
            }
            else
            {
                return true;
            }
        }

        private bool CheckPressed()
        {
            if (Visible && !m_disabled && m_frame > 0 && !IsBlockingDialogPresent())
            {
                if (TestMouse() && Screen.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    return true;
                }
                if (m_shortcutKey.HasValue && Screen.Keyboard.Keys[m_shortcutKey.Value].Pressed)
                {
                    Screen.InputMethod = InputMethod.Keyboard;
                    return true;
                }
                if (Screen.Gamepad != null)
                {
                    if ((m_shortcutButton.HasValue && Screen.Gamepad.Buttons[m_shortcutButton.Value].Pressed) ||
                        (m_altShortcutButton.HasValue && Screen.Gamepad.Buttons[m_altShortcutButton.Value].Pressed))
                    {
                        Screen.InputMethod = InputMethod.Gamepad;
                        return true;
                    }
                }
                if (Screen.SteamController != null)
                {
                    if (m_shortcutSteamControllerButton.HasValue && Screen.SteamController.Buttons[m_shortcutSteamControllerButton.Value.GetID()].Pressed)
                    {
                        Screen.InputMethod = InputMethod.SteamController;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckHeld()
        {
            if (Visible && !m_disabled && m_frame > 0 && !IsBlockingDialogPresent())
            {
                if (TestMouse() && Screen.Mouse.Buttons[MouseButton.Left].Held)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    return true;
                }
                if (m_shortcutKey.HasValue && Screen.Keyboard.Keys[m_shortcutKey.Value].Held)
                {
                    Screen.InputMethod = InputMethod.Keyboard;
                    return true;
                }
                if (Screen.Gamepad != null)
                {
                    if ((m_shortcutButton.HasValue && Screen.Gamepad.Buttons[m_shortcutButton.Value].Held) ||
                        (m_altShortcutButton.HasValue && Screen.Gamepad.Buttons[m_altShortcutButton.Value].Held))
                    {
                        Screen.InputMethod = InputMethod.Gamepad;
                        return true;
                    }
                }
                if (Screen.SteamController != null)
                {
                    if (m_shortcutSteamControllerButton.HasValue && Screen.SteamController.Buttons[m_shortcutSteamControllerButton.Value.GetID()].Held)
                    {
                        Screen.InputMethod = InputMethod.SteamController;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TestMouse()
        {
            if (Visible && !m_disabled && !IsBlockingDialogPresent())
            {
                Vector2 mouseLocal = Screen.MousePosition - Position;
                if (mouseLocal.X >= 0.0f && mouseLocal.X < m_width &&
                     mouseLocal.Y >= 0.0f && mouseLocal.Y < m_height)
                {
                    return true;
                }
            }
            return false;
        }

        private void PlayHighlightSound()
        {
            Screen.Audio.PlaySound("sound/menu_highlight.wav");
        }

        private void PlaySelectSound()
        {
            Screen.Audio.PlaySound("sound/menu_select.wav");
        }
    }
}
