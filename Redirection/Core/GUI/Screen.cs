using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Window;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using OpenTK;
using SDL2;
using System.Collections.Generic;

namespace Dan200.Core.GUI
{
    public class Screen
    {
        private IMouse m_mouse;
        private IKeyboard m_keyboard;
        private IGamepad m_gamepad;
        private ISteamController m_steamController;
        private IAudio m_audio;
        private InputMethod m_inputMethod;
        private Language m_language;

        private IWindow m_window;
        private float m_width;
        private float m_height;
        private int m_pixelWidth;
        private int m_pixelHeight;

        private List<Element> m_elements;
        private ScreenEffectInstance m_screenEffect;

        public Element ModalDialog;

        public float Width
        {
            get
            {
                return m_width;
            }
            set
            {
                if (m_width != value)
                {
                    m_width = value;
                    Rebuild();
                }
            }
        }

        public float Height
        {
            get
            {
                return m_height;
            }
            set
            {
                if (m_height != value)
                {
                    m_height = value;
                    Rebuild();
                }
            }
        }

        public int PixelWidth
        {
            get
            {
                return m_pixelWidth;
            }
            set
            {
                m_pixelWidth = value;
            }
        }

        public int PixelHeight
        {
            get
            {
                return m_pixelHeight;
            }
            set
            {
                m_pixelHeight = value;
            }
        }

        public InputMethod InputMethod
        {
            get
            {
                return m_inputMethod;
            }
            set
            {
                if (value == InputMethod.Gamepad && Gamepad == null)
                {
                    return;
                }
                if (value == InputMethod.SteamController && SteamController == null)
                {
                    return;
                }
                if (m_inputMethod != value)
                {
                    m_inputMethod = value;
                    SDL.SDL_ShowCursor((value == InputMethod.Mouse || value == InputMethod.Keyboard) ? 1 : 0);
                    App.DebugLog("Input method changed to {0}", m_inputMethod);
                }
            }
        }

        public Language Language
        {
            get
            {
                return m_language;
            }
            set
            {
                m_language = value;
            }
        }

        public IMouse Mouse
        {
            get
            {
                return m_mouse;
            }
        }

        public IKeyboard Keyboard
        {
            get
            {
                return m_keyboard;
            }
        }

        public IGamepad Gamepad
        {
            get
            {
                return m_gamepad;
            }
            set
            {
                m_gamepad = value;
                if (m_gamepad == null && InputMethod == InputMethod.Gamepad)
                {
                    InputMethod = InputMethod.Mouse;
                }
            }
        }

        public ISteamController SteamController
        {
            get
            {
                return m_steamController;
            }
            set
            {
                m_steamController = value;
                if (m_steamController == null && InputMethod == InputMethod.SteamController)
                {
                    InputMethod = InputMethod.Mouse;
                }
            }
        }

        public IAudio Audio
        {
            get
            {
                return m_audio;
            }
        }

        public Vector2 MousePosition
        {
            get
            {
                return new Vector2(
                    ((float)m_mouse.X / (float)m_window.Width) * m_width,
                    ((float)m_mouse.Y / (float)m_window.Height) * m_height
                );
            }
        }

        public Cursor Cursor;

        public class ElementSet
        {
            private Screen m_owner;

            public ElementSet(Screen owner)
            {
                m_owner = owner;
            }

            public void Add(Element element)
            {
                if (!m_owner.m_elements.Contains(element))
                {
                    m_owner.m_elements.Add(element);
                    element.Init(m_owner);
                }
            }

            public void AddBefore(Element element, Element before)
            {
                if (!m_owner.m_elements.Contains(element))
                {
                    var index = m_owner.m_elements.IndexOf(before);
                    if (index >= 0)
                    {
                        m_owner.m_elements.Insert(index, element);
                    }
                    else
                    {
                        m_owner.m_elements.Add(element);
                    }
                    element.Init(m_owner);
                }
            }

            public void Remove(Element element)
            {
                m_owner.m_elements.Remove(element);
                if (m_owner.ModalDialog == element)
                {
                    m_owner.ModalDialog = null;
                }
            }

            public void Clear()
            {
                m_owner.m_elements.Clear();
            }
        }

        public ElementSet Elements
        {
            get
            {
                return new ElementSet(this);
            }
        }

        public ScreenEffectInstance Effect
        {
            get
            {
                return m_screenEffect;
            }
        }

        public Screen(IMouse mouse, IKeyboard keyboard, Language language, IWindow window, IAudio audio, float width, float height, int pixelWidth, int pixelHeight)
        {
            m_mouse = mouse;
            m_keyboard = keyboard;
            m_gamepad = null;
            m_audio = audio;
            m_inputMethod = InputMethod.Mouse;
            m_language = language;

            m_window = window;
            m_width = width;
            m_height = height;
            m_pixelWidth = pixelWidth;
            m_pixelHeight = pixelHeight;

            m_elements = new List<Element>();
            m_screenEffect = new ScreenEffectInstance(this);
        }

        public void Dispose()
        {
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].Dispose();
            }
            m_elements = null;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].Update(dt);
            }
        }

        public void Draw()
        {
            // Draw 2D content
            m_screenEffect.Bind();
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].Draw();
            }
        }

        public void DrawExcept(Element element, Element element2 = null)
        {
            // Draw 2D content
            m_screenEffect.Bind();
            for (int i = 0; i < m_elements.Count; ++i)
            {
                var e = m_elements[i];
                if (e != element && e != element2)
                {
                    e.Draw();
                }
            }
        }
        public void Draw3D()
        {
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].Draw3D();
            }
        }

        public void DrawOnly(Element element)
        {
            m_screenEffect.Bind();
            element.Draw();
        }

        private void Rebuild()
        {
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].RequestRebuild();
            }
        }

        public Vector2 GetAnchorPosition(Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft:
                default:
                    return new Vector2(0.0f, 0.0f);
                case Anchor.TopMiddle:
                    return new Vector2(Width * 0.5f, 0.0f);
                case Anchor.TopRight:
                    return new Vector2(Width, 0.0f);
                case Anchor.CentreLeft:
                    return new Vector2(0.0f, Height * 0.5f);
                case Anchor.CentreMiddle:
                    return new Vector2(Width * 0.5f, Height * 0.5f);
                case Anchor.CentreRight:
                    return new Vector2(Width, Height * 0.5f);
                case Anchor.BottomLeft:
                    return new Vector2(0.0f, Height);
                case Anchor.BottomMiddle:
                    return new Vector2(Width * 0.5f, Height);
                case Anchor.BottomRight:
                    return new Vector2(Width, Height);
            }
        }

        public bool CheckBack()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuBack.GetID()].Pressed ||
                    SteamController.Buttons[SteamControllerButton.InGameBack.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.Back].Pressed ||
                    Gamepad.Buttons[GamepadButton.B].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Escape].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        public bool CheckSelect()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuSelect.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.A].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Return].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        public bool CheckAltSelect()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuAltSelect.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.Y].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Tab].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        public bool CheckUp()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuUp.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.Up].Pressed ||
                    Gamepad.Buttons[GamepadButton.LeftStickUp].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Up].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        public bool CheckDown()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuDown.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.Down].Pressed ||
                    Gamepad.Buttons[GamepadButton.LeftStickDown].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Down].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        public bool CheckLeft()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuLeft.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.Left].Pressed ||
                    Gamepad.Buttons[GamepadButton.LeftStickLeft].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Left].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        public bool CheckRight()
        {
            if (SteamController != null)
            {
                if (SteamController.Buttons[SteamControllerButton.MenuRight.GetID()].Pressed)
                {
                    InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Gamepad != null)
            {
                if (Gamepad.Buttons[GamepadButton.Right].Pressed ||
                    Gamepad.Buttons[GamepadButton.LeftStickRight].Pressed)
                {
                    InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Keyboard.Keys[Key.Right].Pressed)
            {
                InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }
    }
}

