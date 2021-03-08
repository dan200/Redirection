using OpenTK.Graphics.OpenGL;
using System;

namespace Dan200.Core.Main
{
    public class OpenGLException : Exception
    {
        public OpenGLException(ErrorCode error) : this(error.ToString())
        {
        }

        public OpenGLException(String message) : base(message)
        {
        }
    }
}

