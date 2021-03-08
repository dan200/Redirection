using Dan200.Core.Main;
using Dan200.Core.Window.SDL2;
using SDL2;
using System.Collections.Generic;

namespace Dan200.Core.Input.SDL2
{
    public class SDL2GamepadCollection : Dan200.Core.Util.IReadOnlyCollection<IGamepad>
    {
        private SDL2Window m_window;
        private List<SDL2Gamepad> m_gamepads;

        public int Count
        {
            get
            {
                return m_gamepads.Count;
            }
        }

        public SDL2GamepadCollection(SDL2Window window)
        {
            m_window = window;
            m_gamepads = new List<SDL2Gamepad>();

            int numJoysticks = SDL.SDL_NumJoysticks();
            for (int joystickIndex = 0; joystickIndex < numJoysticks; ++joystickIndex)
            {
                if (SDL.SDL_IsGameController(joystickIndex) == SDL.SDL_bool.SDL_TRUE)
                {
                    m_gamepads.Add(new SDL2Gamepad(window, joystickIndex));
                }
            }
        }

        public IEnumerator<IGamepad> GetEnumerator()
        {
            return m_gamepads.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_gamepads.GetEnumerator();
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            // Handle changed devices
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    {
                        int joystickIndex = e.jdevice.which;
                        if (SDL.SDL_IsGameController(joystickIndex) == SDL.SDL_bool.SDL_FALSE)
                        {
                            App.Log("Error: Unrecognised gamepad: " + SDL.SDL_JoystickNameForIndex(joystickIndex));
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    {
                        // New gamepad
                        int joystickIndex = e.cdevice.which;
                        bool alreadyAdded = false;
                        for (int i = 0; i < m_gamepads.Count; ++i)
                        {
                            if (m_gamepads[i].JoystickIndex == joystickIndex)
                            {
                                alreadyAdded = true;
                                break;
                            }
                        }
                        if (!alreadyAdded)
                        {
                            m_gamepads.Add(new SDL2Gamepad(m_window, joystickIndex));
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    {
                        // Lost gamepad
                        int instanceID = e.cdevice.which;
                        for (int i = m_gamepads.Count - 1; i >= 0; --i)
                        {
                            if (m_gamepads[i].InstanceID == instanceID)
                            {
                                m_gamepads[i].Disconnect();
                                m_gamepads.RemoveAt(i);
                            }
                        }
                        break;
                    }
                default:
                    {
                        // Other events
                        for (int i = 0; i < m_gamepads.Count; ++i)
                        {
                            m_gamepads[i].HandleEvent(ref e);
                        }
                        break;
                    }
            }
        }

        public void Update()
        {
            for (int i = 0; i < m_gamepads.Count; ++i)
            {
                m_gamepads[i].Update();
            }
        }
    }
}

