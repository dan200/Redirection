using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Window;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

namespace Dan200.Core.Input.Steamworks
{
    public class SteamController : ISteamController
    {
        private SteamControllerCollection m_owner;
        private IWindow m_window;
        private ControllerHandle_t m_handle;
        private bool m_connected;

        private Dictionary<string, IButton> m_buttons;
        private Dan200.Core.Util.IReadOnlyDictionary<string, IButton> m_buttonsReadOnly;

        private Dictionary<string, IAxis> m_axes;
        private Dan200.Core.Util.IReadOnlyDictionary<string, IAxis> m_axesReadOnly;

        private Dictionary<string, IJoystick> m_joysticks;
        private Dan200.Core.Util.IReadOnlyDictionary<string, IJoystick> m_joysticksReadOnly;

        private string m_currentActionSet;

        public bool Connected
        {
            get
            {
                return m_connected;
            }
        }

        public ControllerHandle_t Handle
        {
            get
            {
                return m_handle;
            }
        }

        public string ActionSet
        {
            get
            {
                return m_currentActionSet;
            }
            set
            {
                if (m_currentActionSet != value)
                {
                    m_currentActionSet = value;
                    global::Steamworks.SteamController.ActivateActionSet(m_handle, m_owner.GetActionSetHandle(m_currentActionSet));
                }
            }
        }

        public IReadOnlyDictionary<string, IButton> Buttons
        {
            get
            {
                return m_buttonsReadOnly;
            }
        }

        public IReadOnlyDictionary<string, IAxis> Axes
        {
            get
            {
                return m_axesReadOnly;
            }
        }

        public IReadOnlyDictionary<string, IJoystick> Joysticks
        {
            get
            {
                return m_joysticksReadOnly;
            }
        }

        public SteamController(SteamControllerCollection owner, IWindow window, ControllerHandle_t handle)
        {
            m_owner = owner;
            m_window = window;
            m_handle = handle;

            // Buttons
            m_buttons = new Dictionary<string, IButton>();
            m_buttonsReadOnly = m_buttons.ToReadOnly();
            foreach (string name in m_owner.DigitalActionNames)
            {
                m_buttons.Add(name, new SimpleButton());
            }

            // Axes
            m_axes = new Dictionary<string, IAxis>();
            m_axesReadOnly = m_axes.ToReadOnly();
            foreach (string name in m_owner.AnalogTriggerActionNames)
            {
                m_axes.Add(name, new SimpleAxis());
            }

            // Joysticks
            m_joysticks = new Dictionary<string, IJoystick>();
            m_joysticksReadOnly = m_joysticks.ToReadOnly();
            foreach (string name in m_owner.AnalogActionNames)
            {
                m_joysticks.Add(name, new SimpleJoystick());
            }

            // State
            m_connected = true;
            App.Log("Steam controller connected");

            // Set initial action set
            m_currentActionSet = m_owner.ActionSetNames.First();
            global::Steamworks.SteamController.ActivateActionSet(m_handle, m_owner.GetActionSetHandle(m_currentActionSet));

            // Get initial state
            Update();
        }

        public void Update()
        {
            if (m_connected)
            {
                bool focus = m_window.Focus;
                foreach (var entry in m_buttons)
                {
                    var name = entry.Key;
                    var button = entry.Value;
                    var data = global::Steamworks.SteamController.GetDigitalActionData(m_handle, m_owner.GetDigitalActionHandle(name));
                    ((SimpleButton)button).Update(focus && (data.bActive != 0) && (data.bState != 0));
                }
                foreach (var entry in m_axes)
                {
                    var name = entry.Key;
                    var axis = entry.Value;
                    var data = global::Steamworks.SteamController.GetAnalogActionData(m_handle, m_owner.GetAnalogActionHandle(name));
                    ((SimpleAxis)axis).Value = (focus && data.bActive != 0) ? data.x : 0.0f;
                }
                foreach (var entry in m_joysticks)
                {
                    var name = entry.Key;
                    var joystick = entry.Value;
                    var data = global::Steamworks.SteamController.GetAnalogActionData(m_handle, m_owner.GetAnalogActionHandle(name));
                    ((SimpleJoystick)joystick).X = (focus && data.bActive != 0) ? data.x : 0.0f;
                    ((SimpleJoystick)joystick).Y = (focus && data.bActive != 0) ? data.y : 0.0f;
                }
            }
        }

        public string GetButtonPromptPath(string id, string actionSetID)
        {
            var origins = new EControllerActionOrigin[global::Steamworks.Constants.STEAM_CONTROLLER_MAX_ORIGINS];
            int numOrigins = global::Steamworks.SteamController.GetDigitalActionOrigins(
                m_handle,
                m_owner.GetActionSetHandle((actionSetID != null) ? actionSetID : m_currentActionSet),
                m_owner.GetDigitalActionHandle(id),
                origins
            );
            if (numOrigins > 0)
            {
                return GetPromptPath(GetBestOrigin(origins, numOrigins));
            }
            return null;
        }

        public string GetAxisPromptPath(string id, string actionSetID)
        {
            return GetJoystickPromptPath(id, actionSetID);
        }

        public string GetJoystickPromptPath(string id, string actionSetID)
        {
            var origins = new EControllerActionOrigin[global::Steamworks.Constants.STEAM_CONTROLLER_MAX_ORIGINS];
            int numOrigins = global::Steamworks.SteamController.GetAnalogActionOrigins(
                m_handle,
                m_owner.GetActionSetHandle((actionSetID != null) ? actionSetID : m_currentActionSet),
                m_owner.GetAnalogActionHandle(id),
                origins
            );
            if (numOrigins > 0)
            {
                return GetPromptPath(GetBestOrigin(origins, numOrigins));
            }
            return null;
        }

        private static EControllerActionOrigin[] s_sortedOrigins = new EControllerActionOrigin[]
        {
            EControllerActionOrigin.k_EControllerActionOrigin_A,
            EControllerActionOrigin.k_EControllerActionOrigin_B,
            EControllerActionOrigin.k_EControllerActionOrigin_X,
            EControllerActionOrigin.k_EControllerActionOrigin_Y,
            EControllerActionOrigin.k_EControllerActionOrigin_Start,
            EControllerActionOrigin.k_EControllerActionOrigin_Back,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftBumper,
            EControllerActionOrigin.k_EControllerActionOrigin_RightBumper,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftTrigger_Click,
            EControllerActionOrigin.k_EControllerActionOrigin_RightTrigger_Click,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftTrigger_Pull,
            EControllerActionOrigin.k_EControllerActionOrigin_RightTrigger_Pull,

            EControllerActionOrigin.k_EControllerActionOrigin_LeftStick_Move,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftStick_Click,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftStick_DPadNorth,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftStick_DPadSouth,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftStick_DPadWest,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftStick_DPadEast,

            EControllerActionOrigin.k_EControllerActionOrigin_LeftGrip,
            EControllerActionOrigin.k_EControllerActionOrigin_RightGrip,

            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_Touch,
            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_Touch,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_Click,
            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_Click,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_Swipe,
            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_Swipe,

            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_DPadNorth,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_DPadSouth,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_DPadWest,
            EControllerActionOrigin.k_EControllerActionOrigin_LeftPad_DPadEast,

            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_DPadNorth,
            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_DPadSouth,
            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_DPadWest,
            EControllerActionOrigin.k_EControllerActionOrigin_RightPad_DPadEast,

            EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Move,
            EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Pitch,
            EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Yaw,
            EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Roll
        };

        private static EControllerActionOrigin GetBestOrigin(EControllerActionOrigin[] origins, int count)
        {
            int best = 0;
            int bestPriority = s_sortedOrigins.Length;
            for (int i = 0; i < count; ++i)
            {
                var origin = origins[i];
                for (int priority = 0; priority < bestPriority; ++priority)
                {
                    if (s_sortedOrigins[priority] == origin)
                    {
                        best = i;
                        bestPriority = priority;
                        break;
                    }
                }
            }
            if (best < s_sortedOrigins.Length)
            {
                return origins[best];
            }
            else
            {
                return EControllerActionOrigin.k_EControllerActionOrigin_None;
            }
        }

        private static string GetPromptPath(EControllerActionOrigin origin)
        {
            string name;
            switch (origin)
            {
                case EControllerActionOrigin.k_EControllerActionOrigin_None:
                    {
                        return null;
                    }
                case EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Move:
                case EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Pitch:
                case EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Yaw:
                case EControllerActionOrigin.k_EControllerActionOrigin_Gyro_Roll:
                    {
                        name = "gyro";
                        break;
                    }
                default:
                    {
                        name = origin.ToString().Substring("k_EControllerActionOrigin_".Length).ToLowerUnderscored();
                        break;
                    }
            }
            return "gui/prompts/steam_controller/" + name + ".png";
        }

        public void Disconnect()
        {
            if (m_connected)
            {
                m_connected = false;
                foreach (var button in m_buttons)
                {
                    ((SimpleButton)button.Value).Disconnect();
                }
                foreach (var axis in m_axes)
                {
                    ((SimpleAxis)axis.Value).Value = 0.0f;
                }

                App.Log("Steam controller disconnected");
            }
        }

        public void Rumble(float duration)
        {
            if (m_connected)
            {
                var intervalMicros = 35000;
                var durationMicros = (int)(duration * 1000000f);
                global::Steamworks.SteamController.TriggerRepeatedHapticPulse(
                    m_handle,
                    ESteamControllerPad.k_ESteamControllerPad_Left,
                    (ushort)(intervalMicros / 2),
                    (ushort)(intervalMicros / 2),
                    (ushort)(durationMicros / intervalMicros),
                    0
                );
                global::Steamworks.SteamController.TriggerRepeatedHapticPulse(
                    m_handle,
                    ESteamControllerPad.k_ESteamControllerPad_Right,
                    (ushort)(intervalMicros / 2),
                    (ushort)(intervalMicros / 2),
                    (ushort)(durationMicros / intervalMicros),
                    0
                );
            }
        }
    }
}

