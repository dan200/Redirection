// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// Changes to this file will be reverted when you update Steamworks.NET

namespace Steamworks
{
    public struct SNetListenSocket_t : System.IEquatable<SNetListenSocket_t>, System.IComparable<SNetListenSocket_t>
    {
        public uint m_SNetListenSocket;

        public SNetListenSocket_t(uint value)
        {
            m_SNetListenSocket = value;
        }

        public override string ToString()
        {
            return m_SNetListenSocket.ToString();
        }

        public override bool Equals(object other)
        {
            return other is SNetListenSocket_t && this == (SNetListenSocket_t)other;
        }

        public override int GetHashCode()
        {
            return m_SNetListenSocket.GetHashCode();
        }

        public static bool operator ==(SNetListenSocket_t x, SNetListenSocket_t y)
        {
            return x.m_SNetListenSocket == y.m_SNetListenSocket;
        }

        public static bool operator !=(SNetListenSocket_t x, SNetListenSocket_t y)
        {
            return !(x == y);
        }

        public static explicit operator SNetListenSocket_t(uint value)
        {
            return new SNetListenSocket_t(value);
        }

        public static explicit operator uint(SNetListenSocket_t that)
        {
            return that.m_SNetListenSocket;
        }

        public bool Equals(SNetListenSocket_t other)
        {
            return m_SNetListenSocket == other.m_SNetListenSocket;
        }

        public int CompareTo(SNetListenSocket_t other)
        {
            return m_SNetListenSocket.CompareTo(other.m_SNetListenSocket);
        }
    }
}
