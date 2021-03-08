// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// Changes to this file will be reverted when you update Steamworks.NET

namespace Steamworks
{
    public struct ControllerHandle_t : System.IEquatable<ControllerHandle_t>, System.IComparable<ControllerHandle_t>
    {
        public ulong m_ControllerHandle;

        public ControllerHandle_t(ulong value)
        {
            m_ControllerHandle = value;
        }

        public override string ToString()
        {
            return m_ControllerHandle.ToString();
        }

        public override bool Equals(object other)
        {
            return other is ControllerHandle_t && this == (ControllerHandle_t)other;
        }

        public override int GetHashCode()
        {
            return m_ControllerHandle.GetHashCode();
        }

        public static bool operator ==(ControllerHandle_t x, ControllerHandle_t y)
        {
            return x.m_ControllerHandle == y.m_ControllerHandle;
        }

        public static bool operator !=(ControllerHandle_t x, ControllerHandle_t y)
        {
            return !(x == y);
        }

        public static explicit operator ControllerHandle_t(ulong value)
        {
            return new ControllerHandle_t(value);
        }

        public static explicit operator ulong(ControllerHandle_t that)
        {
            return that.m_ControllerHandle;
        }

        public bool Equals(ControllerHandle_t other)
        {
            return m_ControllerHandle == other.m_ControllerHandle;
        }

        public int CompareTo(ControllerHandle_t other)
        {
            return m_ControllerHandle.CompareTo(other.m_ControllerHandle);
        }
    }
}
