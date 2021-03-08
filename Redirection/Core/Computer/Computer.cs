using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Computer
{
    public class Computer : IDisposable
    {
        private bool m_on;
        private string m_host;
        private TextWriter m_output;
        private TextWriter m_errorOutput;
        private MemoryTracker m_memory;
        private PortCollection m_ports;
        private DeviceCollection m_devices;
        private EventQueue m_eventQueue;
        private Guid m_guid;

        private PowerStatus m_powerStatus;
        private double m_chargeLevel;

        public bool IsOn
        {
            get
            {
                return m_on;
            }
        }

        public string Host
        {
            get
            {
                return m_host;
            }
            set
            {
                m_host = value;
            }
        }

        public TextWriter Output
        {
            get
            {
                return m_output;
            }
            set
            {
                m_output = value;
            }
        }

        public TextWriter ErrorOutput
        {
            get
            {
                return m_errorOutput;
            }
            set
            {
                m_errorOutput = value;
            }
        }

        public MemoryTracker Memory
        {
            get
            {
                return m_memory;
            }
        }

        public PortCollection Ports
        {
            get
            {
                return m_ports;
            }
        }

        public DeviceCollection Devices
        {
            get
            {
                return m_devices;
            }
        }

        public EventQueue Events
        {
            get
            {
                return m_eventQueue;
            }
        }

        public Guid GUID
        {
            get
            {
                return m_guid;
            }
        }

        public Computer(Guid id)
        {
            m_guid = id;

            m_on = false;
            m_host = null;
            m_output = TextWriter.Null;
            m_errorOutput = TextWriter.Null;

            m_memory = new MemoryTracker(0, delegate ()
            {
                foreach (var device in m_devices)
                {
                    device.FreeUnusedMemory();
                }
            });

            m_ports = new PortCollection();
            m_devices = new DeviceCollection();
            m_eventQueue = new EventQueue();

            m_powerStatus = PowerStatus.Fixed;
            m_chargeLevel = 1.0f;
        }

        public void Update(TimeSpan dt)
        {
            lock (m_devices)
            {
                Event e;
                while (Events.Dequeue(out e))
                {
                    foreach (var device in m_devices)
                    {
                        var result = device.HandleEvent(e);
                        if (result != DeviceUpdateResult.Continue)
                        {
                            HandleUpdateResult(result);
                            break;
                        }
                    }
                }

                foreach (var device in m_devices)
                {
                    var result = device.Update(dt);
                    if (result != DeviceUpdateResult.Continue)
                    {
                        HandleUpdateResult(result);
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            TurnOff();
        }

        public void TurnOn()
        {
            if (!m_on)
            {
                // Attach devices
                var devices = GatherDevices();
                foreach (var device in devices)
                {
                    m_devices.Add(device);
                    Attach(device);
                }

                // Machine is now on
                m_eventQueue.Clear();
                m_on = true;

                // Boot the devices CPU
                foreach (var device in m_devices)
                {
                    var result = device.Boot();
                    if (result != DeviceUpdateResult.Continue)
                    {
                        HandleUpdateResult(result);
                        break;
                    }
                }
            }
        }

        public void TurnOff()
        {
            if (m_on)
            {
                // Machine is now off
                m_on = false;

                // Detach devices
                var oldDevices = new List<Device>(m_devices);
                foreach (var device in oldDevices)
                {
                    Detach(device, m_devices.GetName(device));
                    m_devices.Remove(device);
                }
            }
        }

        public void Reboot()
        {
            TurnOff();
            TurnOn();
        }

        public void SetPowerStatus(PowerStatus status, double chargeLevel)
        {
            m_powerStatus = status;
            m_chargeLevel = chargeLevel;
        }

        public void GetPowerStatus(out PowerStatus o_status, out double o_chargeLevel)
        {
            o_status = m_powerStatus;
            o_chargeLevel = m_chargeLevel;
        }

        private void HandleUpdateResult(DeviceUpdateResult result)
        {
            switch (result)
            {
                case DeviceUpdateResult.Shutdown:
                    {
                        TurnOff();
                        break;
                    }
                case DeviceUpdateResult.Reboot:
                    {
                        Reboot();
                        break;
                    }
                case DeviceUpdateResult.Continue:
                default:
                    {
                        break;
                    }
            }
        }

        public void RefreshDevices()
        {
            if (m_on)
            {
                var newDevices = GatherDevices();

                // Detach the devices no longer present
                var removedDevices = new List<Device>();
                foreach (var device in m_devices)
                {
                    if (!newDevices.Contains(device))
                    {
                        removedDevices.Add(device);
                    }
                }
                foreach (var device in removedDevices)
                {
                    Detach(device, m_devices.GetName(device));
                    m_devices.Remove(device);
                }

                // Attach the new devices
                foreach (var device in newDevices)
                {
                    if (!m_devices.Contains(device))
                    {
                        m_devices.Add(device);
                        Attach(device);
                    }
                }
            }
        }

        private List<Device> GatherDevices()
        {
            var newDevices = new List<Device>();
            foreach (var port in m_ports)
            {
                var devices = port.Devices;
                if (devices != null)
                {
                    foreach (var device in devices)
                    {
                        if (device != null && !newDevices.Contains(device))
                        {
                            newDevices.Add(device);
                        }
                    }
                }
            }
            return newDevices;
        }

        private void Attach(Device device)
        {
            device.Attach(this);
            if (m_on)
            {
                m_eventQueue.Queue("device_added", new LuaArgs(m_devices.GetName(device)));
            }
        }

        private void Detach(Device device, string previousName)
        {
            device.Detach();
            if (m_on)
            {
                m_eventQueue.Queue("device_removed", new LuaArgs(previousName));
            }
        }
    }
}
