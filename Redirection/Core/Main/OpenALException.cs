using OpenTK.Audio.OpenAL;
using System;

namespace Dan200.Core.Main
{
    public class OpenALException : Exception
    {
        public OpenALException(ALError error) : this(error.ToString())
        {
        }

        public OpenALException(String message) : base(message)
        {
        }
    }
}

