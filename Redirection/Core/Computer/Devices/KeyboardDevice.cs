using Dan200.Core.Lua;
using System.Collections.Generic;

namespace Dan200.Core.Computer.Devices
{
    public class KeyboardDevice : Device
    {
        private string m_description;
        private Computer m_computer;
        private HashSet<int> m_keysHeld;

        public override string Type
        {
            get
            {
                return "keyboard";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public KeyboardDevice(string description)
        {
            m_description = description;
            m_keysHeld = new HashSet<int>();
        }

        public override void Attach(Computer computer)
        {
            m_computer = computer;
        }

        public override void Detach()
        {
            m_computer = null;
        }

        [LuaMethod]
        public LuaArgs getKey(LuaArgs args)
        {
            int code = args.GetInt(0);
            return new LuaArgs(IsKeyHeld(code));
        }

        public bool IsKeyHeld(int code)
        {
            return m_keysHeld.Contains(code);
        }

        public void KeyDown(int code, bool repeat)
        {
            if (m_keysHeld.Contains(code))
            {
                if (repeat)
                {
                    QueueEvent("key", new LuaArgs(code, true));
                }
            }
            else
            {
                m_keysHeld.Add(code);
                QueueEvent("key", new LuaArgs(code, false));
            }
        }

        public void KeyUp(int code)
        {
            if (m_keysHeld.Contains(code))
            {
                m_keysHeld.Remove(code);
                QueueEvent("key_up", new LuaArgs(code));
            }
        }

        public void Char(int c)
        {
            QueueEvent("char", new LuaArgs(char.ConvertFromUtf32(c)));
        }

        public void Text(string str)
        {
            QueueEvent("text", new LuaArgs(str));
        }

        private void QueueEvent(string eventName, LuaArgs args)
        {
            if (m_computer != null)
            {
                m_computer.Events.Queue(eventName, args);
            }
        }
    }
}
