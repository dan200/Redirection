using Dan200.Core.Computer.APIs;
using Dan200.Core.Computer.Devices.DiskDrive;
using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Computer.Devices
{
    public class LuaCPUDevice : Device
    {
		private enum CPUUpdateResult
		{
			Continue,
			Shutdown,
			Reboot,
			Halt,
			Boot
		}

        private struct Timer
        {
            public readonly int ID;
            public readonly TimeSpan Limit;

            public Timer(int id, TimeSpan limit)
            {
                ID = id;
                Limit = limit;
            }
        }

        private struct Alarm
        {
            public readonly int ID;
            public readonly DateTime Time;

            public Alarm(int id, DateTime time)
            {
                ID = id;
                Time = time;
            }
        }

        private string m_description;
        private Computer m_computer;
        private bool m_isMainCPU;
        private LuaMachine m_machine;
        private LuaCoroutine m_mainRoutine;
        private FileSystem m_fileSystem;
        private HashSet<string> m_eventFilter;

        private CPUUpdateResult m_updateResult;
		private LuaObjectRef<LuaMount> m_nextBootMount;

        public override string Type
        {
            get
            {
                return "cpu";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

		public bool Halted
		{
			get
			{
				return m_machine == null;
			}
		}

        public LuaCPUDevice(string description)
        {
            m_description = description;
            m_computer = null;
            m_isMainCPU = false;
            m_machine = null;
            m_mainRoutine = null;
            m_fileSystem = new FileSystem();
			m_eventFilter = new HashSet<string>();
			m_nextBootMount = new LuaObjectRef<LuaMount>();
        }

        public override void Attach(Computer computer)
        {
            m_computer = computer;
            m_isMainCPU = (m_computer.Devices.GetName(this) == Type);
        }

        public override DeviceUpdateResult Boot()
        {
            // If we are the main CPU
            if (m_isMainCPU)
            {
                // Find the ROM
                var rom = (m_computer.Devices["rom"] as ROMDevice);
                if (rom == null)
                {
                    m_computer.ErrorOutput.WriteLine("Error starting CPU: No ROM");
                    return DeviceUpdateResult.Shutdown;
                }

                // Boot from the ROM
				return HandleUpdateResult(ReallyBoot(rom.LuaMount));
            }
            else
            {
                return DeviceUpdateResult.Continue;
            }
        }

        public override void Detach()
        {
            if (m_machine != null)
            {
                Halt();
            }
            m_computer = null;
        }

        public override void FreeUnusedMemory()
        {
            if (m_machine != null)
            {
                m_machine.CollectGarbage();
            }
        }

		private DeviceUpdateResult HandleUpdateResult(CPUUpdateResult result)
		{
			switch (result)
			{
				case CPUUpdateResult.Continue:
				default:
					{
						return DeviceUpdateResult.Continue;
					}
				case CPUUpdateResult.Reboot:
					{
						return DeviceUpdateResult.Reboot;
					}
				case CPUUpdateResult.Shutdown:
					{
						return DeviceUpdateResult.Shutdown;
					}					
				case CPUUpdateResult.Halt:
					{
						if (!Halted)
						{
							Halt();
						}
						return DeviceUpdateResult.Continue;
					}
				case CPUUpdateResult.Boot:
					{
						if (!Halted)
						{
							Halt();
						}
						return HandleUpdateResult(ReallyBoot(m_nextBootMount.Value));
					}
			}
		}

		private CPUUpdateResult ReallyBoot(LuaMount bootMount)
        {
            if (m_machine != null)
            {
                throw new InvalidOperationException();
            }

            // Check if the system has any RAM
            if (m_computer.Memory.TotalMemory == 0)
            {
                m_computer.ErrorOutput.WriteLine("Error starting CPU: No RAM");
                return m_isMainCPU ? CPUUpdateResult.Shutdown : CPUUpdateResult.Continue;
            }

            // Mount the ROM
            m_fileSystem.Mount(bootMount, FilePath.Empty, FilePath.Empty, true);

			// Check if the boot path exists
			var bootPath = new FilePath("boot.lua");
            if (!m_fileSystem.Exists(bootPath) || m_fileSystem.IsDir(bootPath))
            {
                m_computer.ErrorOutput.WriteLine("Error starting CPU: ROM does not countain {0}", bootPath);
                m_fileSystem.UnmountAll();
                return m_isMainCPU ? CPUUpdateResult.Shutdown : CPUUpdateResult.Continue;
            }

            // Init lua machine
            m_machine = new LuaMachine(m_computer.Memory);
            m_machine.AllowByteCodeLoading = false;
            m_machine.EnforceTimeLimits = true;
            try
            {
                // Remove default APIs we don't want
                m_machine.RemoveUnsafeGlobals();

                // Install basic APIs
                if (m_computer.Host != null)
                {
                    m_machine.SetGlobal("_HOST", m_computer.Host);
                }
                InstallAPI(new IOAPI(m_computer, m_fileSystem));
                InstallAPI(new OSAPI(m_computer, this, m_fileSystem));
                InstallAPI(new PackageAPI(m_computer, m_fileSystem));

                // Install custom APIs
                PreloadAPI(new SystemAPI(m_computer, this));
                PreloadAPI(new FSAPI(m_computer, m_fileSystem));
            }
            catch (LuaError e)
            {
                m_computer.ErrorOutput.WriteLine("Error starting CPU: {0}", e.Message);
                Halt();
                return m_isMainCPU ? CPUUpdateResult.Shutdown : CPUUpdateResult.Continue;
            }

			// Load the boot script
            try
            {
                string bootScript;
                using (var reader = m_fileSystem.OpenForRead(bootPath))
                {
                    bootScript = reader.ReadToEnd();
                }
                var bootFunction = m_machine.LoadString(bootScript, "@" + bootPath);
                m_mainRoutine = m_machine.CreateCoroutine(bootFunction);
                m_eventFilter.Clear();
            }
            catch (IOException e)
            {
                m_computer.ErrorOutput.WriteLine("Error loading {0}: {1}", bootPath, e.Message);
                Halt();
                return m_isMainCPU ? CPUUpdateResult.Shutdown : CPUUpdateResult.Continue;
            }
            catch (LuaError e)
            {
				m_computer.ErrorOutput.WriteLine("Error parsing {0}: {1}", bootPath, e.Message);
                Halt();
                return m_isMainCPU ? CPUUpdateResult.Shutdown : CPUUpdateResult.Continue;
            }

            // Start the boot script
            m_eventFilter.Clear();
            return Resume(LuaArgs.Empty);
        }

        public override DeviceUpdateResult HandleEvent(Event e)
        {
            if (m_machine != null)
            {
                if (m_eventFilter.Count == 0 || m_eventFilter.Contains(e.Name))
                {
					return HandleUpdateResult(Resume(LuaArgs.Concat(new LuaArgs(e.Name), e.Arguments)));
                }
            }
            return DeviceUpdateResult.Continue;
        }

        private void Halt()
        {
            if (m_machine == null)
            {
                throw new InvalidOperationException();
            }

            // Shutdown lua machine
            m_machine.Dispose();
            m_machine = null;
            m_mainRoutine = null;
            m_eventFilter.Clear();

            // Shutdown file system
            m_fileSystem.UnmountAll();
        }

        [LuaMethod]
        public LuaArgs getStatus(LuaArgs args)
        {
			if (Halted)
            {
                return new LuaArgs("halted");
            }
            else
            {
                return new LuaArgs("running");
            }
        }

        [LuaMethod]
        public LuaArgs boot(LuaArgs args)
        {
            var mount = args.GetObject<LuaMount>(0);
			RequestBoot(mount);
			throw new LuaYield(LuaArgs.Empty, delegate {
				return LuaArgs.Empty;
			});
        }

        [LuaMethod]
        public LuaArgs halt(LuaArgs args)
        {
			if (Halted)
            {
                throw new LuaError("Already halted");
            }
			RequestHalt();
			throw new LuaYield(LuaArgs.Empty, delegate {
				return LuaArgs.Empty;	
			});
        }

        private void InstallAPI(LuaAPI api)
        {
            api.Init(m_machine);
        }

        private void PreloadAPI(LuaAPI api)
        {
            api.Init(m_machine);
            m_machine.DoString(string.Format(@"
				local g = _G
				local api = g[ ""{0}"" ]
				package.preload[ ""{0}"" ] = function()
					g[ ""{0}"" ] = api
					return api
				end
				g[ ""{0}"" ] = nil
			", api.Name),
            "=LuaCPUDevice.PreloadAPI");
        }

		private CPUUpdateResult Resume(LuaArgs args)
        {
            try
            {
                // Setup
                m_updateResult = CPUUpdateResult.Continue;
				m_nextBootMount.Value = null;

                // Resume
                var results = m_mainRoutine.Resume(args);

                // Update event filter
                m_eventFilter.Clear();
                for (int i = 0; i < results.Length; ++i)
                {
                    if (results.IsNil(i))
                    {
                        break;
                    }
                    else if (results.IsString(i))
                    {
                        m_eventFilter.Add(results.GetString(i));
                    }
                    else
                    {
                        m_eventFilter.Add(null);
                    }
                }

                // Handle result
                if (m_mainRoutine.IsFinished)
                {
                    Halt();
                    return m_isMainCPU ? CPUUpdateResult.Shutdown : m_updateResult;
                }
                else
                {
                    if (m_updateResult == CPUUpdateResult.Continue)
                    {
                        m_machine.CollectGarbage();
                    }
                    return m_updateResult;
                }
            }
            catch (LuaError e)
            {
				var bootPath = new FilePath("boot.lua");
				m_computer.ErrorOutput.WriteLine("Error resuming {0}: {1}", bootPath, e.Message);
                Halt();
                return m_isMainCPU ? CPUUpdateResult.Shutdown : m_updateResult;
            }
        }

		internal void RequestHalt()
		{
			m_updateResult = CPUUpdateResult.Halt;
		}

		internal void RequestBoot( LuaMount mount )
		{
			m_updateResult = CPUUpdateResult.Boot;
			m_nextBootMount.Value = mount;
		}

        internal void RequestShutdown()
        {
			m_updateResult = CPUUpdateResult.Shutdown;
        }

        internal void RequestReboot()
        {
            m_updateResult = CPUUpdateResult.Reboot;
        }
    }
}
