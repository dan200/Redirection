using Dan200.Core.Main;
using Dan200.Core.Render;
using System;
using System.IO;

namespace Dan200.Game.Game
{
    public class Screenshot : IDisposable
    {
        public int Width;
        public int Height;
        public Bitmap Bitmap;

        public Screenshot(int width = 0, int height = 0)
        {
            Width = width;
            Height = height;
        }

        public void Dispose()
        {
            if (Bitmap != null)
            {
                Bitmap.Dispose();
                Bitmap = null;
            }
        }

        public void Save(string path)
        {
            // Save image
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Bitmap.Save(path);
                App.Log("Screenshot saved to " + path);
            }
            catch (IOException e)
            {
                App.Log("Screenshot save failed: " + e);
            }
        }
    }
}

