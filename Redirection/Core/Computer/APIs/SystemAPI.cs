using Dan200.Core.Computer.Devices;
using Dan200.Core.Computer.Devices.CPU;
using Dan200.Core.Lua;
using System.Collections.Generic;
using System.Reflection;

namespace Dan200.Core.Computer.APIs
{
    public class SystemAPI : LuaAPI
    {
        private class DeviceMethodCollection
        {
            private Dictionary<string, LuaCFunction> m_methods;

            public IEnumerable<string> Names
            {
                get
                {
                    return m_methods.Keys;
                }
            }

            public LuaCFunction this[string name]
            {
                get
                {
                    return m_methods[name];
                }
            }

            private class DeviceMethodCaller
            {
                private DeviceCollection m_devices;
                private Device m_device;
                private MethodInfo m_method;
                private object[] m_args;

                public DeviceMethodCaller(DeviceCollection devices, Device device, MethodInfo method)
                {
                    m_devices = devices;
                    m_device = device;
                    m_method = method;
                    m_args = new object[1];
                }

                public LuaArgs CallMethod(LuaArgs args)
                {
                    if (!m_devices.Contains(m_device))
                    {
                        throw new LuaError("Device disconnected");
                    }
                    try
                    {
                        m_args[0] = args; // Saves an allocation
                        return (LuaArgs)m_method.Invoke(m_device, m_args);
                    }
                    catch (TargetInvocationException e)
                    {
                        throw e.InnerException;
                    }
                }
            }

            public DeviceMethodCollection(DeviceCollection devices, Device device)
            {
                var type = device.GetType();
                var methods = type.GetMethods();
                m_methods = new Dictionary<string, LuaCFunction>();
                for (int i = 0; i < methods.Length; ++i)
                {
                    var method = methods[i];
                    var name = method.Name;
                    var attributes = method.GetCustomAttributes(typeof(LuaMethodAttribute), true);
                    if (attributes != null && attributes.Length > 0)
                    {
                        var attribute = (LuaMethodAttribute)attributes[0];
                        if (attribute.CustomName != null)
                        {
                            name = attribute.CustomName;
                        }
                        var caller = new DeviceMethodCaller(devices, device, method);
                        m_methods[name] = caller.CallMethod;
                    }
                }
            }
        }

        private Computer m_computer;
        private LuaCPUDevice m_cpu;
        private Dictionary<Device, DeviceMethodCollection> m_deviceMethods;

        public SystemAPI(Computer computer, LuaCPUDevice cpu) : base("system")
        {
            m_computer = computer;
            m_cpu = cpu;
            m_deviceMethods = new Dictionary<Device, DeviceMethodCollection>();
        }

        public override void Init(LuaMachine machine)
        {
            base.Init(machine);
            machine.DoString(@"
                -- Locals
                local type = type
                local error = error
                local function expect( value, sExpectedType, index )
                    local sFoundType = type( value )
                    if sExpectedType and sFoundType ~= sExpectedType then
                        error( ""Expected "" .. sExpectedType .. "" at argument #"" .. index .. "", got "" .. sFoundType, 3 )
                    end
                    return value
                end

				-- System API methods
				local coroutine_yield = coroutine.yield
                local table_unpack = table.unpack

                function system.pullEvent( a, ... )
                    local tEvent
                    if a ~= nil then
                        tEvent = { coroutine_yield( ""terminate"", a, ... ) }
                    else
                        tEvent = { coroutine_yield() }
                    end
                    local sEvent = tEvent[1]
                    if sEvent == ""terminate"" then
                        error( ""Terminated"", 0 )
                    else
                        return table_unpack( tEvent )
                    end
                end

                function system.pullEventRaw( ... )
                    return coroutine_yield( ... )
                end

				local system_getDevice = system.getDevice
                local system_pullEvent = system.pullEvent
                function system.sleep( nSeconds )
                    expect( nSeconds, ""number"", 1 )
                    local clock = system_getDevice( ""clock"" )
                    if not clock then
                        error( ""Error sleeping: No clock detected"", 2 )
                    end
                    local nTimer = clock.startTimer( nSeconds )
                    while true do
                        local sEvent, p1 = system_pullEvent( ""timer"" )
                        if p1 == nTimer then
                            break
                        end
                    end
                end",
                "=SystemAPI.Init"
            );
        }

        [LuaMethod]
        public LuaArgs alloc(LuaArgs args)
        {
            var length = args.GetInt(0);
            if (length < 0)
            {
                throw new LuaError("Allocation size must be positive");
            }
            if (m_computer.Memory.Alloc(length))
            {
                var buffer = new Devices.CPU.Buffer(0, length);
                return new LuaArgs(new LuaBuffer(buffer, m_computer.Memory));
            }
            else
            {
                throw new LuaError("not enough memory");
            }
        }

        [LuaMethod]
        public LuaArgs getFreeMemory(LuaArgs args)
        {
            return new LuaArgs(m_computer.Memory.FreeMemory);
        }

        [LuaMethod]
        public LuaArgs getTotalMemory(LuaArgs args)
        {
            return new LuaArgs(m_computer.Memory.TotalMemory);
        }

        [LuaMethod]
        public LuaArgs queueEvent(LuaArgs args)
        {
            var eventName = args.GetString(0);
            var result = m_computer.Events.Queue(eventName, args.Select(1));
            if (!result)
            {
                throw new LuaError("Event queue full");
            }
            else
            {
                return LuaArgs.Empty;
            }
        }

        [LuaMethod]
        public LuaArgs reboot(LuaArgs args)
        {
            m_cpu.RequestReboot();
			throw new LuaYield(LuaArgs.Empty, delegate {
				return LuaArgs.Empty;
			});
        }

        [LuaMethod]
        public LuaArgs shutdown(LuaArgs args)
        {
            m_cpu.RequestShutdown();
			throw new LuaYield(LuaArgs.Empty, delegate {
				return LuaArgs.Empty;
			});
        }

        [LuaMethod]
        public LuaArgs getPowerStatus(LuaArgs args)
        {
            PowerStatus status;
            double chargeLevel;
            m_computer.GetPowerStatus(out status, out chargeLevel);
            return new LuaArgs(status.ToString().ToLower(), chargeLevel);
        }

        [LuaMethod]
        public LuaArgs getDeviceNames(LuaArgs args)
        {
            var filter = args.IsNil(0) ? null : args.GetString(0);
            var devices = m_computer.Devices;
            var table = new LuaTable(devices.Count);
            int i = 1;
            foreach (var device in devices)
            {
                if (filter == null || device.Type == filter)
                {
                    table[i++] = devices.GetName(device);
                }
            }
            return new LuaArgs(table);
        }

        [LuaMethod]
        public LuaArgs getDevice(LuaArgs args)
        {
            var name = args.GetString(0);
            var device = m_computer.Devices[name];
            if (device != null)
            {
                return new LuaArgs(Wrap(device));
            }
            return LuaArgs.Nil;
        }

        [LuaMethod]
        public LuaArgs getID(LuaArgs args)
        {
            return new LuaArgs(m_computer.GUID.ToString());
        }

        private LuaTable Wrap(Device device)
        {
            var table = new LuaTable();
            var methods = FindMethods(device);
            foreach (var methodName in methods.Names)
            {
                table[methodName] = methods[methodName];
            }
            return table;
        }

        private LuaArgs GetMethodNames(Device device)
        {
            var methods = FindMethods(device);
            var table = new LuaTable();
            int i = 1;
            foreach (var methodName in methods.Names)
            {
                table[i++] = new LuaValue(methodName);
            }
            return new LuaArgs(table);
        }

        private DeviceMethodCollection FindMethods(Device device)
        {
            if (m_deviceMethods.ContainsKey(device))
            {
                return m_deviceMethods[device];
            }
            else
            {
                var methods = new DeviceMethodCollection(m_computer.Devices, device);
                m_deviceMethods.Add(device, methods);
                return methods;
            }
        }
    }
}
