using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Game.Input;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class InputPrompt : Element
    {
        private InputMethod m_lastInputMethod;
        private GamepadType m_lastPadType;
        private ISteamController m_lastSteamController;
        private string m_string;
        private Key? m_key;
        private MouseButton? m_mouseButton;
        private GamepadButton? m_gamepadButton;
        private GamepadAxis? m_gamepadAxis;
        private SteamControllerButton? m_steamControllerButton;
        private SteamControllerAxis? m_steamControllerAxis;
        private TextMenu m_text;
        private event EventHandler m_onClick;

        public string String
        {
            get
            {
                return m_string;
            }
            set
            {
                if (m_string != value)
                {
                    m_string = value;
                    RequestRebuild();
                }
            }
        }

        public Key? Key
        {
            get
            {
                return m_key;
            }
            set
            {
                if (m_key != value)
                {
                    m_key = value;
                    RequestRebuild();
                }
            }
        }

        public MouseButton? MouseButton
        {
            get
            {
                return m_mouseButton;
            }
            set
            {
                if (m_mouseButton != value)
                {
                    m_mouseButton = value;
                    RequestRebuild();
                }
            }
        }

        public GamepadAxis? GamepadAxis
        {
            get
            {
                return m_gamepadAxis;
            }
            set
            {
                if (m_gamepadAxis != value)
                {
                    m_gamepadAxis = value;
                    RequestRebuild();
                }
            }
        }

        public GamepadButton? GamepadButton
        {
            get
            {
                return m_gamepadButton;
            }
            set
            {
                if (m_gamepadButton != value)
                {
                    m_gamepadButton = value;
                    RequestRebuild();
                }
            }
        }

        public SteamControllerAxis? SteamControllerAxis
        {
            get
            {
                return m_steamControllerAxis;
            }
            set
            {
                if (m_steamControllerAxis != value)
                {
                    m_steamControllerAxis = value;
                    RequestRebuild();
                }
            }
        }

        public SteamControllerButton? SteamControllerButton
        {
            get
            {
                return m_steamControllerButton;
            }
            set
            {
                if (m_steamControllerButton != value)
                {
                    m_steamControllerButton = value;
                    RequestRebuild();
                }
            }
        }

        public Font Font
        {
            get
            {
                return m_text.Font;
            }
        }

        public Vector4 TextColour
        {
            get
            {
                return m_text.TextColour;
            }
            set
            {
                m_text.TextColour = value;
            }
        }

        public float Height
        {
            get
            {
                return m_text.Height;
            }
        }

        public event EventHandler OnClick
        {
            add
            {
                m_onClick += value;
                m_text.Enabled = (m_onClick != null);
            }
            remove
            {
                m_onClick -= value;
                m_text.Enabled = (m_onClick != null);
            }
        }

        public InputPrompt(Font font, string str, TextAlignment alignment)
        {
            m_string = str;
            m_key = null;
            m_mouseButton = null;
            m_gamepadAxis = null;
            m_gamepadButton = null;
            m_text = new TextMenu(font, new string[] { "" }, alignment, MenuDirection.Vertical);
            m_text.MouseOnly = true;
            m_text.Enabled = false;
            m_text.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                if (m_onClick != null)
                {
                    m_onClick.Invoke(this, EventArgs.Empty);
                }
            };
        }

        public override void Dispose()
        {
            m_text.Dispose();
            base.Dispose();
        }

        protected override void OnInit()
        {
            m_lastInputMethod = Screen.InputMethod;
            m_lastPadType = (Screen.Gamepad != null) ? Screen.Gamepad.Type : GamepadType.Unknown;
            m_lastSteamController = Screen.SteamController;

            m_text.Anchor = Anchor;
            m_text.LocalPosition = LocalPosition;
            m_text.Parent = this.Parent;
            m_text.Init(Screen);
        }

        protected override void OnUpdate(float dt)
        {
            var inputMethod = Screen.InputMethod;
            var padType = (Screen.Gamepad != null) ? Screen.Gamepad.Type : GamepadType.Unknown;
            var steamController = Screen.SteamController;
            if (inputMethod != m_lastInputMethod || padType != m_lastPadType || (inputMethod == InputMethod.SteamController)) // Steam controller bindings can change without warning
            {
                m_lastInputMethod = inputMethod;
                m_lastPadType = padType;
                m_lastSteamController = steamController;
                RequestRebuild();
            }

            m_text.Visible = Visible;
            m_text.Update(dt);
        }

        protected override void OnDraw()
        {
            if (Screen.ModalDialog == this.Parent ||
                (Screen.ModalDialog is DialogueBox && !((DialogueBox)Screen.ModalDialog).BlockInput))
            {
                m_text.Draw();
            }
        }

        protected override void OnRebuild()
        {
            m_text.Anchor = Anchor;
            m_text.LocalPosition = LocalPosition;
            m_text.Options[0] = BuildString();
            m_text.Visible = Visible;
            m_text.RequestRebuild();
        }

        public bool TestMouse()
        {
            return m_text.TestMouse() >= 0;
        }

        private string BuildString()
        {
            string prompt = null;
            if (m_lastInputMethod == InputMethod.Mouse)
            {
                if (m_mouseButton.HasValue)
                {
                    prompt = m_mouseButton.Value.GetPrompt();
                }
                else if (m_key.HasValue)
                {
                    prompt = m_key.Value.GetPrompt();
                }
            }
            else if (m_lastInputMethod == InputMethod.Keyboard)
            {
                if (m_key.HasValue)
                {
                    prompt = m_key.Value.GetPrompt();
                }
                else if (m_mouseButton.HasValue)
                {
                    prompt = m_mouseButton.Value.GetPrompt();
                }
            }
            else if (m_lastInputMethod == InputMethod.SteamController)
            {
                if (m_steamControllerAxis.HasValue)
                {
                    prompt = m_steamControllerAxis.Value.GetPrompt(m_lastSteamController);
                }
                else if (m_steamControllerButton.HasValue)
                {
                    prompt = m_steamControllerButton.Value.GetPrompt(m_lastSteamController);
                }
                else if (m_key.HasValue)
                {
                    prompt = m_key.Value.GetPrompt();
                }
            }
            else if (m_lastInputMethod == InputMethod.Gamepad)
            {
                if (m_gamepadAxis.HasValue)
                {
                    prompt = m_gamepadAxis.Value.GetPrompt(m_lastPadType);
                }
                else if (m_gamepadButton.HasValue)
                {
                    prompt = m_gamepadButton.Value.GetPrompt(m_lastPadType);
                }
                else if (m_key.HasValue)
                {
                    prompt = m_key.Value.GetPrompt();
                }
            }

            if (prompt != null && m_string != null && m_string.Length > 0)
            {
                return prompt + " " + m_string;
            }
            else if (prompt != null)
            {
                return prompt;
            }
            else if (m_string != null)
            {
                return m_string;
            }
            return "";
        }
    }
}

