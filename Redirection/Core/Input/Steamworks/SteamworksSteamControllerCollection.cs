using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Window.SDL2;
using Steamworks;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Input.Steamworks
{
    public class SteamworksSteamControllerCollection : IReadOnlyCollection<ISteamController>, IDisposable
    {
        private SDL2Window m_window;

        private ControllerHandle_t[] m_controllerHandles;
        private int m_numControllerHandles;

        private Dictionary<string, ControllerActionSetHandle_t> m_actionSetHandles;
        private Dictionary<string, ControllerDigitalActionHandle_t> m_digitalActionHandles;
        private Dictionary<string, ControllerAnalogActionHandle_t> m_analogActionHandles;
        private string[] m_analogActionNames;
        private string[] m_analogTriggerActionNames;

        private List<SteamworksSteamController> m_controllers;

        public int Count
        {
            get
            {
                return m_controllers.Count;
            }
        }

        public IEnumerable<string> ActionSetNames
        {
            get
            {
                return m_actionSetHandles.Keys;
            }
        }

        public IEnumerable<string> DigitalActionNames
        {
            get
            {
                return m_digitalActionHandles.Keys;
            }
        }

        public IEnumerable<string> AnalogActionNames
        {
            get
            {
                return m_analogActionNames;
            }
        }

        public IEnumerable<string> AnalogTriggerActionNames
        {
            get
            {
                return m_analogTriggerActionNames;
            }
        }

        public SteamworksSteamControllerCollection(SDL2Window window, string[] actionSetNames, string[] digitalActionNames, string[] analogActionNames, string[] analogTriggerActionNames)
        {
            m_window = window;

            m_controllerHandles = new ControllerHandle_t[Constants.STEAM_CONTROLLER_MAX_COUNT];
            m_numControllerHandles = 0;

            m_controllers = new List<SteamworksSteamController>();
            App.CheckSteamworksResult("SteamController::Init", SteamController.Init());

            m_actionSetHandles = new Dictionary<string, ControllerActionSetHandle_t>();
            for (int i = 0; i < actionSetNames.Length; ++i)
            {
                var name = actionSetNames[i];
                m_actionSetHandles.Add(name, SteamController.GetActionSetHandle(name));
            }

            m_digitalActionHandles = new Dictionary<string, ControllerDigitalActionHandle_t>();
            for (int i = 0; i < digitalActionNames.Length; ++i)
            {
                var name = digitalActionNames[i];
                m_digitalActionHandles.Add(name, SteamController.GetDigitalActionHandle(name));
            }

            m_analogActionHandles = new Dictionary<string, ControllerAnalogActionHandle_t>();
            m_analogActionNames = analogActionNames;
            for (int i = 0; i < m_analogActionNames.Length; ++i)
            {
                var name = m_analogActionNames[i];
                m_analogActionHandles.Add(name, SteamController.GetAnalogActionHandle(name));
            }
            m_analogTriggerActionNames = analogTriggerActionNames;
            for (int i = 0; i < m_analogTriggerActionNames.Length; ++i)
            {
                var name = m_analogTriggerActionNames[i];
                m_analogActionHandles.Add(name, SteamController.GetAnalogActionHandle(name));
            }

            DetectControllers();
        }

        public void Dispose()
        {
            App.CheckSteamworksResult("SteamController::Shutdown", SteamController.Shutdown());
        }

        public IEnumerator<ISteamController> GetEnumerator()
        {
            return m_controllers.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_controllers.GetEnumerator();
        }

        public void Update()
        {
            SteamController.RunFrame();
            DetectControllers();
            for (int i = 0; i < m_controllers.Count; ++i)
            {
                m_controllers[i].Update();
            }
        }

        public ControllerActionSetHandle_t GetActionSetHandle(string name)
        {
            return m_actionSetHandles[name];
        }

        public ControllerDigitalActionHandle_t GetDigitalActionHandle(string name)
        {
            return m_digitalActionHandles[name];
        }

        public ControllerAnalogActionHandle_t GetAnalogActionHandle(string name)
        {
            return m_analogActionHandles[name];
        }

        private void DetectControllers()
        {
            // Find all connected controllers
            m_numControllerHandles = SteamController.GetConnectedControllers(m_controllerHandles);

            // Disconnect all old controllers
            for (int j = m_controllers.Count - 1; j >= 0; --j)
            {
                var controller = m_controllers[j];
                int match = -1;
                for (int i = 0; i < m_numControllerHandles; ++i)
                {
                    var handle = m_controllerHandles[i];
                    if (controller.Handle == handle)
                    {
                        match = i;
                        break;
                    }
                }
                if (match < 0)
                {
                    controller.Disconnect();
                    m_controllers.RemoveAt(j);
                }
            }

            // Add all new controllers
            for (int i = 0; i < m_numControllerHandles; ++i)
            {
                var handle = m_controllerHandles[i];
                SteamworksSteamController match = null;
                foreach (var controller in m_controllers)
                {
                    if (controller.Handle == handle)
                    {
                        match = controller;
                        break;
                    }
                }
                if (match == null)
                {
                    m_controllers.Add(new SteamworksSteamController(this, m_window, handle));
                }
            }
        }
    }
}

