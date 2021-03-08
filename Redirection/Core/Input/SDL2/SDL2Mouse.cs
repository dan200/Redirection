using Dan200.Core.Util;
using Dan200.Core.Window.SDL2;
using SDL2;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Input.SDL2
{
    public class SDL2Mouse : IMouse
    {
        private SDL2Window m_window;
        private bool m_hadMouseFocus;

        private int m_x;
        private int m_y;
        private int m_dx;
        private int m_dy;
        private int m_wheel;
        private int m_pendingWheel;

        private Dictionary<MouseButton, IButton> m_buttons;
        private IReadOnlyDictionary<MouseButton, IButton> m_buttonsReadOnly;

        public int X
        {
            get
            {
                return m_x;
            }
        }

        public int Y
        {
            get
            {
                return m_y;
            }
        }

        public int DX
        {
            get
            {
                return m_dx;
            }
        }

        public int DY
        {
            get
            {
                return m_dy;
            }
        }

        public int Wheel
        {
            get
            {
                return m_wheel;
            }
        }

        public Dan200.Core.Util.IReadOnlyDictionary<MouseButton, IButton> Buttons
        {
            get
            {
                return m_buttonsReadOnly;
            }
        }

        public SDL2Mouse(SDL2Window window)
        {
            m_window = window;
            m_buttons = new Dictionary<MouseButton, IButton>();
            m_buttonsReadOnly = m_buttons.ToReadOnly();
            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
            {
                if (button != MouseButton.None)
                {
                    m_buttons.Add(button, new SimpleButton());
                }
            }

            m_hadMouseFocus = false;
            Update();
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    {
                        // Mouse wheeling
                        if (m_window.MouseFocus)
                        {
                            if (e.wheel.y > 0)
                            {
                                m_pendingWheel++;
                            }
                            else if (e.wheel.y < 0)
                            {
                                m_pendingWheel--;
                            }
                        }
                        break;
                    }
            }
        }

        public void Update()
        {
            int newX, newY;
            bool focus = m_window.Focus;
            bool mouseFocus = m_window.MouseFocus;
            uint buttons = SDL.SDL_GetMouseState(out newX, out newY);

            bool newlyGainedMouseFocus = false;
            if (mouseFocus)
            {
                if (m_hadMouseFocus)
                {
                    m_dx = newX - m_x;
                    m_dy = newY - m_y;
                }
                else
                {
                    m_dx = 0;
                    m_dy = 0;
                    newlyGainedMouseFocus = true;
                }
                m_x = newX;
                m_y = newY;
                m_hadMouseFocus = true;
            }
            else
            {
                m_dx = 0;
                m_dy = 0;
                m_x = -99;
                m_y = -99;
                m_hadMouseFocus = false;
            }

            m_wheel = focus ? m_pendingWheel : 0;
            m_pendingWheel = 0;

            foreach (MouseButton button in m_buttons.Keys)
            {
                var simpleButton = (SimpleButton)m_buttons[button];
                bool pressed = ((buttons & SDL.SDL_BUTTON((uint)button)) != 0);
                if (newlyGainedMouseFocus)
                {
                    simpleButton.IgnoreCurrentPress();
                }
                simpleButton.Update(focus && pressed);
            }
        }
    }
}

