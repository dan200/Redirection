using Dan200.Core.Lua;
using System.Threading;

namespace Dan200.Core.Computer.Devices
{
    public class GamepadDevice : Device
    {
        private string m_description;
        private Computer m_computer;
        private int[] m_buttons;
        private float[] m_axes;

        public override string Type
        {
            get
            {
                return "gamepad";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public GamepadDevice(string description, int buttons, int axes)
        {
            m_description = description;
            m_buttons = new int[buttons];
            m_axes = new float[axes];
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
        public LuaArgs getNumButtons(LuaArgs args)
        {
            return new LuaArgs(m_buttons.Length);
        }

        [LuaMethod]
        public LuaArgs getNumAxes(LuaArgs args)
        {
            return new LuaArgs(m_axes.Length);
        }

        [LuaMethod]
        public LuaArgs getButton(LuaArgs args)
        {
            int button = args.GetInt(0);
            if (button >= 0 && button < m_buttons.Length)
            {
                return new LuaArgs(m_buttons[button] > 0);
            }
            return new LuaArgs(false);
        }

        [LuaMethod]
        public LuaArgs getAxis(LuaArgs args)
        {
            int axis = args.GetInt(0);
            if (axis >= 0 && axis < m_axes.Length)
            {
                return new LuaArgs(m_axes[axis]);
            }
            return new LuaArgs(0.0f);
        }

        public void UpdateButton(int button, bool value)
        {
            if (button >= 0 && button < m_buttons.Length)
            {
                var wasHeld = Interlocked.Exchange(ref m_buttons[button], value ? 1 : 0);
                if (value && wasHeld == 0)
                {
                    QueueEvent("gamepad_button", new LuaArgs(button));
                }
                else if (!value && wasHeld > 0)
                {
                    QueueEvent("gamepad_up", new LuaArgs(button));
                }
            }
        }

        public void UpdateAxis(int axis, float value)
        {
            if (axis >= 0 && axis < m_axes.Length)
            {
                var oldValue = Interlocked.Exchange(ref m_axes[axis], value);
                if (value != oldValue)
                {
                    QueueEvent("gamepad_axis", new LuaArgs(axis));
                }
            }
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
