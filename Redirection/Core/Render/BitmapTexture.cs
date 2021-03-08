
using OpenTK.Graphics.OpenGL;

namespace Dan200.Core.Render
{
    public class BitmapTexture : ITexture
    {
        private int m_texture;

        private Bitmap m_bitmap;
        private int m_width;
        private int m_height;
        private bool m_filter;
        private bool m_wrap;

        public int GLTexture
        {
            get
            {
                return m_texture;
            }
        }

        public int Width
        {
            get
            {
                return m_width;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public bool Filter
        {
            get
            {
                return m_filter;
            }
            set
            {
                if (m_filter != value)
                {
                    m_filter = value;
                    GL.BindTexture(TextureTarget.Texture2D, m_texture);
                    TextureUtil.SetParameters(m_filter, m_wrap);
                }
            }
        }

        public bool Wrap
        {
            get
            {
                return m_wrap;
            }
            set
            {
                if (m_wrap != value)
                {
                    m_wrap = value;
                    GL.BindTexture(TextureTarget.Texture2D, m_texture);
                    TextureUtil.SetParameters(m_filter, m_wrap);
                }
            }
        }

        public BitmapTexture(Bitmap bitmap)
        {
            m_filter = false;
            m_wrap = true;
            Load(bitmap);
        }

        public void Dispose()
        {
            Unload();
        }

        public void Update()
        {
            TextureUtil.UpdateTextureFromBitmap(m_texture, m_bitmap);
            m_width = m_bitmap.Width;
            m_height = m_bitmap.Height;
        }

        private void Load(Bitmap bitmap)
        {
            m_bitmap = bitmap;
            m_texture = TextureUtil.CreateTextureFromBitmap(bitmap, m_filter, m_wrap);
            m_width = bitmap.Width;
            m_height = bitmap.Height;
        }

        private void Unload()
        {
            GL.DeleteTexture(m_texture);
            m_texture = -1;
        }
    }
}

