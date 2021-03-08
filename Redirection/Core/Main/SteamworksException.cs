using System;

namespace Dan200.Core.Main
{
    public class SteamworksException : Exception
    {
        public SteamworksException(string method) : base(method + " failed.")
        {
        }

        public SteamworksException(string method, string message) : base(method + " failed: " + message + ".")
        {
        }
    }
}

