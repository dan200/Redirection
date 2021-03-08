using Dan200.Core.Computer.APIs;
using Dan200.Core.Lua;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Computer.Devices
{
    public class ClockDevice : Device
    {
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
        private double m_defaultTimeOffset;
        private double m_timeOffset;

        private TimeSpan m_clock;
        private List<Timer> m_timers;
        private int m_nextTimerID;
        private List<Alarm> m_alarms;
        private int m_nextAlarmID;

        public override string Type
        {
            get
            {
                return "clock";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public TimeSpan Clock
        {
            get
            {
                return m_clock;
            }
        }

        public DateTime DefaultTime
        {
            get
            {
                return DateTime.UtcNow.AddSeconds(m_defaultTimeOffset);
            }
        }

        public DateTime Time
        {
            get
            {
                return DateTime.UtcNow.AddSeconds(m_timeOffset);
            }
            set
            {
                var dateNow = OSAPI.TimeFromDate(DateTime.UtcNow);
                var dateTarget = OSAPI.TimeFromDate(value);
                m_timeOffset = dateTarget - dateNow;
            }
        }

        public ClockDevice(string description, double offset = 0.0)
        {
            m_description = description;
            m_timers = new List<Timer>();
            m_alarms = new List<Alarm>();
            m_defaultTimeOffset = offset;
            m_timeOffset = offset;
            m_clock = TimeSpan.Zero;
        }

        public override void Attach(Computer computer)
        {
            m_computer = computer;
            m_clock = TimeSpan.Zero;
            m_timers.Clear();
            m_alarms.Clear();
            m_nextTimerID = 0;
            m_nextAlarmID = 0;
            m_timeOffset = m_defaultTimeOffset;
        }

        public override DeviceUpdateResult Update(TimeSpan dt)
        {
            m_clock += dt;

            // Advance timers
            var clockNow = Clock;
            lock (m_timers)
            {
                for (int i = 0; i < m_timers.Count; ++i)
                {
                    var timer = m_timers[i];
                    if (clockNow >= timer.Limit)
                    {
                        m_computer.Events.Queue("timer", new LuaArgs(timer.ID));
                        m_timers.RemoveAt(i);
                        --i;
                    }
                }
            }

            // Advance alarms
            var timeNow = Time.ToUniversalTime();
            lock (m_alarms)
            {
                for (int i = 0; i < m_alarms.Count; ++i)
                {
                    var alarm = m_alarms[i];
                    if (timeNow >= alarm.Time)
                    {
                        m_computer.Events.Queue("alarm", new LuaArgs(alarm.ID));
                        m_alarms.RemoveAt(i);
                        --i;
                    }
                }
            }

            return DeviceUpdateResult.Continue;
        }

        public override void Detach()
        {
            m_computer = null;
        }

        [LuaMethod]
        public LuaArgs getClock(LuaArgs args)
        {
            return new LuaArgs(Clock.TotalSeconds);
        }

        [LuaMethod]
        public LuaArgs getTime(LuaArgs args)
        {
            return new LuaArgs(OSAPI.TimeFromDate(Time));
        }

        [LuaMethod]
        public LuaArgs setTime(LuaArgs args)
        {
            var dateNow = OSAPI.TimeFromDate(DateTime.UtcNow);
            var dateTarget = args.GetDouble(0);
            m_timeOffset = dateTarget - dateNow;
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs resetTime(LuaArgs args)
        {
            m_timeOffset = m_defaultTimeOffset;
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs startTimer(LuaArgs args)
        {
            var duration = args.GetDouble(0);
            var limit = Clock + TimeSpan.FromSeconds(duration);
            lock (m_timers)
            {
                var id = m_nextTimerID++;
                m_timers.Add(new Timer(id, limit));
                return new LuaArgs(id);
            }
        }

        [LuaMethod]
        public LuaArgs cancelTimer(LuaArgs args)
        {
            var id = args.GetInt(0);
            lock (m_timers)
            {
                for (int i = m_timers.Count - 1; i >= 0; --i)
                {
                    var timer = m_timers[i];
                    if (timer.ID == id)
                    {
                        m_timers.RemoveAt(i);
                    }
                }
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setAlarm(LuaArgs args)
        {
            var seconds = args.GetDouble(0);
            var date = OSAPI.DateFromTime(seconds).ToUniversalTime();
            lock (m_alarms)
            {
                var id = m_nextAlarmID++;
                m_alarms.Add(new Alarm(id, date));
                return new LuaArgs(id);
            }
        }

        [LuaMethod]
        public LuaArgs cancelAlarm(LuaArgs args)
        {
            var id = args.GetInt(0);
            lock (m_alarms)
            {
                for (int i = m_alarms.Count - 1; i >= 0; --i)
                {
                    var alarm = m_alarms[i];
                    if (alarm.ID == id)
                    {
                        m_alarms.RemoveAt(i);
                    }
                }
            }
            return LuaArgs.Empty;
        }
    }
}
