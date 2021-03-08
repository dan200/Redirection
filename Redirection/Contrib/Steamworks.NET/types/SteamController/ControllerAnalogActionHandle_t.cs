// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// Changes to this file will be reverted when you update Steamworks.NET

namespace Steamworks
{
    public struct ControllerAnalogActionHandle_t : System.IEquatable<ControllerAnalogActionHandle_t>, System.IComparable<ControllerAnalogActionHandle_t>
    {
        public ulong m_ControllerAnalogActionHandle;

        public ControllerAnalogActionHandle_t(ulong value)
        {
            m_ControllerAnalogActionHandle = value;
        }

        public override string ToString()
        {
            return m_ControllerAnalogActionHandle.ToString();
        }

        public override bool Equals(object other)
        {
            return other is ControllerAnalogActionHandle_t && this == (ControllerAnalogActionHandle_t)other;
        }

        public override int GetHashCode()
        {
            return m_ControllerAnalogActionHandle.GetHashCode();
        }

        public static bool operator ==(ControllerAnalogActionHandle_t x, ControllerAnalogActionHandle_t y)
        {
            return x.m_ControllerAnalogActionHandle == y.m_ControllerAnalogActionHandle;
        }

        public static bool operator !=(ControllerAnalogActionHandle_t x, ControllerAnalogActionHandle_t y)
        {
            return !(x == y);
        }

        public static explicit operator ControllerAnalogActionHandle_t(ulong value)
        {
            return new ControllerAnalogActionHandle_t(value);
        }

        public static explicit operator ulong(ControllerAnalogActionHandle_t that)
        {
            return that.m_ControllerAnalogActionHandle;
        }

        public bool Equals(ControllerAnalogActionHandle_t other)
        {
            return m_ControllerAnalogActionHandle == other.m_ControllerAnalogActionHandle;
        }

        public int CompareTo(ControllerAnalogActionHandle_t other)
        {
            return m_ControllerAnalogActionHandle.CompareTo(other.m_ControllerAnalogActionHandle);
        }
    }
}
