using System.IO;

namespace Dan200.Core.Assets
{
    public static class StreamExtensions
    {
        public static byte[] ReadToEnd(this Stream stream)
        {
            int length = (int)stream.Length;
            byte[] bytes = new byte[length];
            int pos = 0;
            while (pos < length)
            {
                int bytesRead = stream.Read(bytes, pos, length - pos);
                if (bytesRead >= 0)
                {
                    pos += bytesRead;
                }
                else
                {
                    break;
                }
            }
            return bytes;
        }
    }
}

