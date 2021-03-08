using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Window.SDL2;
using SDL2;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Input.SDL2
{
    public class SDL2Gamepad : IGamepad
    {
        private SDL2Window m_window;
        private int m_joystickIndex;
        private IntPtr m_joystick;
        private IntPtr m_gameController;
        private IntPtr m_haptic;
        private int m_instanceID;
        private string m_name;
        private string m_joystickName;
        private GamepadType m_type;
        private bool m_enableRumble;

        private bool m_connected;
        private Dictionary<GamepadButton, IButton> m_buttons;
        private IReadOnlyDictionary<GamepadButton, IButton> m_buttonsReadOnly;
        private Dictionary<GamepadAxis, IAxis> m_axes;
        private IReadOnlyDictionary<GamepadAxis, IAxis> m_axesReadOnly;
        private Dictionary<GamepadJoystick, IJoystick> m_joysticks;
        private IReadOnlyDictionary<GamepadJoystick, IJoystick> m_joysticksReadOnly;

        public int JoystickIndex
        {
            get
            {
                return m_joystickIndex;
            }
        }

        public int InstanceID
        {
            get
            {
                return m_instanceID;
            }
        }

        public GamepadType Type
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type = value;
            }
        }

        public bool EnableRumble
        {
            get
            {
                return m_enableRumble;
            }
            set
            {
                if (m_enableRumble != value)
                {
                    m_enableRumble = value;
                    if (!m_enableRumble && m_haptic != IntPtr.Zero)
                    {
                        SDL.SDL_HapticRumbleStop(m_haptic);
                    }
                }
            }
        }

        public bool Connected
        {
            get
            {
                return m_connected;
            }
        }

        public bool SupportsRumble
        {
            get
            {
                return m_haptic != IntPtr.Zero;
            }
        }

        public IReadOnlyDictionary<GamepadButton, IButton> Buttons
        {
            get
            {
                return m_buttonsReadOnly;
            }
        }

        public IReadOnlyDictionary<GamepadAxis, IAxis> Axes
        {
            get
            {
                return m_axesReadOnly;
            }
        }

        public IReadOnlyDictionary<GamepadJoystick, IJoystick> Joysticks
        {
            get
            {
                return m_joysticksReadOnly;
            }
        }

        public SDL2Gamepad(SDL2Window window, int joystickIndex)
        {
            m_window = window;
            m_joystickIndex = joystickIndex;
            m_gameController = SDL.SDL_GameControllerOpen(m_joystickIndex);
            m_joystick = SDL.SDL_GameControllerGetJoystick(m_gameController);
            m_instanceID = SDL.SDL_JoystickInstanceID(m_joystick);
            m_name = SDL.SDL_GameControllerName(m_gameController);
            m_joystickName = SDL.SDL_JoystickName(m_joystick);
            DetectType();

            // Lets get ready to rumple
            m_haptic = SDL.SDL_HapticOpenFromJoystick(m_joystick);
            if (m_haptic != IntPtr.Zero)
            {
                if (SDL.SDL_HapticRumbleSupported(m_haptic) == (int)SDL.SDL_bool.SDL_FALSE ||
                    SDL.SDL_HapticRumbleInit(m_haptic) < 0)
                {
                    SDL.SDL_HapticClose(m_haptic);
                    m_haptic = IntPtr.Zero;
                }
            }

            // Axes
            m_axes = new Dictionary<GamepadAxis, IAxis>();
            m_axesReadOnly = m_axes.ToReadOnly();
            foreach (GamepadAxis axis in Enum.GetValues(typeof(GamepadAxis)))
            {
                if (axis != GamepadAxis.None)
                {
                    var simpleAxis = new SimpleAxis();
                    switch (axis)
                    {
                        case GamepadAxis.LeftStickX:
                        case GamepadAxis.LeftStickY:
                            {
                                simpleAxis.DeadZone = 0.239f; // Derived from XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE
                                break;
                            }
                        case GamepadAxis.RightStickX:
                        case GamepadAxis.RightStickY:
                            {
                                simpleAxis.DeadZone = 0.265f; // Derived from XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE
                                break;
                            }
                        case GamepadAxis.LeftTrigger:
                        case GamepadAxis.RightTrigger:
                            {
                                simpleAxis.DeadZone = 0.117f; // Derived from XINPUT_GAMEPAD_TRIGGER_THRESHOLD
                                break;
                            }
                    }
                    m_axes.Add(axis, simpleAxis);
                }
            }

            // Buttons
            m_buttons = new Dictionary<GamepadButton, IButton>();
            m_buttonsReadOnly = m_buttons.ToReadOnly();
            foreach (GamepadButton button in Enum.GetValues(typeof(GamepadButton)))
            {
                if (button != GamepadButton.None && !button.IsVirtual())
                {
                    m_buttons.Add(button, new SimpleButton());
                }
            }
            m_buttons.Add(GamepadButton.LeftStickUp, new AxisButton(m_axes[GamepadAxis.LeftStickY], -0.5f));
            m_buttons.Add(GamepadButton.LeftStickDown, new AxisButton(m_axes[GamepadAxis.LeftStickY], 0.5f));
            m_buttons.Add(GamepadButton.LeftStickLeft, new AxisButton(m_axes[GamepadAxis.LeftStickX], -0.5f));
            m_buttons.Add(GamepadButton.LeftStickRight, new AxisButton(m_axes[GamepadAxis.LeftStickX], 0.5f));
            m_buttons.Add(GamepadButton.RightStickUp, new AxisButton(m_axes[GamepadAxis.RightStickY], -0.5f));
            m_buttons.Add(GamepadButton.RightStickDown, new AxisButton(m_axes[GamepadAxis.RightStickY], 0.5f));
            m_buttons.Add(GamepadButton.RightStickLeft, new AxisButton(m_axes[GamepadAxis.RightStickX], -0.5f));
            m_buttons.Add(GamepadButton.RightStickRight, new AxisButton(m_axes[GamepadAxis.RightStickX], 0.5f));
            m_buttons.Add(GamepadButton.LeftTrigger, new AxisButton(m_axes[GamepadAxis.LeftTrigger], 0.6f)); // Sometimes buggy triggers idle at 0.5, using 0.6 ensures we don't use these values
            m_buttons.Add(GamepadButton.RightTrigger, new AxisButton(m_axes[GamepadAxis.RightTrigger], 0.6f)); // Sometimes buggy triggers idle at 0.5, using 0.6 ensures we don't use these values

            // Joysticks
            m_joysticks = new Dictionary<GamepadJoystick, IJoystick>();
            m_joysticksReadOnly = m_joysticks.ToReadOnly();
            m_joysticks.Add(GamepadJoystick.Left, new TwoAxisJoystick(m_axes[GamepadAxis.LeftStickX], m_axes[GamepadAxis.LeftStickY]));
            m_joysticks.Add(GamepadJoystick.Right, new TwoAxisJoystick(m_axes[GamepadAxis.RightStickX], m_axes[GamepadAxis.RightStickY]));

            // State
            m_connected = true;
            App.Log("{0} controller connected ({1}, {2})", m_type, m_name, m_joystickName);
            if (m_haptic != IntPtr.Zero)
            {
                App.Log("Rumble supported");
            }

            Update();
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
        }

        public void Update()
        {
            bool focus = m_window.Focus;
            bool connected = (SDL.SDL_GameControllerGetAttached(m_gameController) == SDL.SDL_bool.SDL_TRUE);

            // Axes
            foreach (GamepadAxis axis in m_axes.Keys)
            {
                var simpleAxis = (SimpleAxis)m_axes[axis];
                if (focus && connected)
                {
                    short value = SDL.SDL_GameControllerGetAxis(m_gameController, (SDL.SDL_GameControllerAxis)axis);
                    simpleAxis.Value = (value >= 0) ?
                        ((float)value / 32767.0f) :
                        ((float)value / 32768.0f);
                }
                else
                {
                    simpleAxis.Value = 0.0f;
                }
            }

            // Buttons
            foreach (GamepadButton button in m_buttons.Keys)
            {
                if (!button.IsVirtual())
                {
                    var simpleButton = (SimpleButton)m_buttons[button];
                    if (focus && connected)
                    {
                        byte held = SDL.SDL_GameControllerGetButton(m_gameController, (SDL.SDL_GameControllerButton)button);
                        simpleButton.Update(held == 1);
                    }
                    else
                    {
                        simpleButton.Update(false);
                    }
                }
                else
                {
                    var axisButton = (AxisButton)m_buttons[button];
                    axisButton.Update();
                }
            }
        }

        public void Disconnect()
        {
            if (m_haptic != IntPtr.Zero)
            {
                SDL.SDL_HapticClose(m_haptic);
                m_haptic = IntPtr.Zero;
            }
            SDL.SDL_GameControllerClose(m_gameController);

            foreach (GamepadButton button in m_buttons.Keys)
            {
                if (!button.IsVirtual())
                {
                    ((SimpleButton)m_buttons[button]).Disconnect();
                }
                else
                {
                    ((AxisButton)m_buttons[button]).Disconnect();
                }
            }

            foreach (GamepadAxis axis in m_axes.Keys)
            {
                ((SimpleAxis)m_axes[axis]).Value = 0.0f;
            }

            m_connected = false;
            App.Log("{0} controller disconnected ({1}, {2})", m_type, m_name, m_joystickName);
        }

        public void Rumble(float strength, float duration)
        {
            strength = MathUtils.Clamp(strength, 0.0f, 1.0f);
            duration = Math.Max(duration, 0.0f);
            if (m_enableRumble && m_haptic != IntPtr.Zero)
            {
                SDL.SDL_HapticRumblePlay(m_haptic, strength, (uint)(duration * 1000.0f));
            }
        }

        public void DetectType()
        {
            switch (m_name)
            {
                case "XInput Controller": // Maybe not assume this is 360?
                case "X360 Controller":
                case "X360 Wireless Controller":
                case "Microsoft X-Box 360 pad":
                case "Xbox Gamepad (userspace driver)":
                    {
                        Type = GamepadType.Xbox360;
                        break;
                    }
                case "Afterglow PS3 Controller":
                case "PS3 Controller":
                case "PS3 DualShock":
                case "PS3 Controller (Bluetooth)":
                    {
                        Type = GamepadType.PS3;
                        break;
                    }
                case "PS4 Controller":
                case "Sony DualShock 4":
                case "PS4 Controller (Bluetooth)":
                    {
                        Type = GamepadType.PS4;
                        break;
                    }
                case "Microsoft X-Box One pad":
                case "Microsoft X-Box One pad v2":
                case "Xbox One Wired Controller":
                    {
                        Type = GamepadType.XboxOne;
                        break;
                    }
                default:
                    {
                        Type = GamepadType.Unknown;
                        break;
                    }
            }
        }
    }
}

