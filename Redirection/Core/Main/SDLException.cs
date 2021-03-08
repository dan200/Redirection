using System;

namespace Dan200.Core.Main
{
    public class SDLException : Exception
    {
        public SDLException(string method) : base(method + " failed.")
        {
        }

        public SDLException(string method, string message) : base(method + " failed: " + message + ".")
        {
        }
    }
}

