
//using TexLib;

using Dan200.Core.Assets;


#if OPENGLES
using OpenTK.Graphics.ES20;
using TextureUnit=OpenTK.Graphics.ES20.All;
using TextureTarget=OpenTK.Graphics.ES20.All;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render
{
    public class Texture : ITexture, IBasicAsset
    {
        public static Texture Get(string path, bool filter)
        {
            var texture = Assets.Assets.Get<Texture>(path);
            if (texture != null)
            {
                texture.Filter = filter;
            }
            return texture;
        }

        public static Texture GetLocalised(string path, Language language, bool filter)
        {
            // Try the current language and it's fallbacks
            bool triedEnglish = false;
            while (language != null)
            {
                var specificPath = AssetPath.Combine(
                    AssetPath.GetDirectoryName(path),
                    AssetPath.GetFileNameWithoutExtension(path) + "_" + language.Code + ".png"
                );
                if (Assets.Assets.Exists<Texture>(specificPath))
                {
                    return Texture.Get(specificPath, filter);
                }
                if (language.Code == "en")
                {
                    triedEnglish = true;
                }
                language = language.Fallback;
            }
            if (!triedEnglish)
            {
                // Try english
                var englishPath = AssetPath.Combine(
                    AssetPath.GetDirectoryName(path),
                    AssetPath.GetFileNameWithoutExtension(path) + "_en.png"
                );
                if (Assets.Assets.Exists<Texture>(englishPath))
                {
                    return Texture.Get(englishPath, filter);
                }
            }

            // Try unlocalised
            return Texture.Get(path, filter);
        }

        public static Texture Black
        {
            get
            {
                return Texture.Get("black.png", false);
            }
        }

        public static Texture White
        {
            get
            {
                return Texture.Get("white.png", false);
            }
        }

        public static Texture Flat
        {
            get
            {
                return Texture.Get("flat.png", false);
            }
        }

        private string m_path;
        private int m_texture;
        private int m_width;
        private int m_height;
        private Bitmap m_bitmap;
        private bool m_filter;
        private bool m_wrap;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

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

        public Bitmap Bitmap
        {
            get
            {
                return m_bitmap;
            }
        }

        public Texture(string path, IFileStore store)
        {
            m_path = path;
            m_filter = false;
            m_wrap = false;
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Load(IFileStore store)
        {
            using (var stream = store.OpenFile(m_path))
            {
                m_bitmap = new Bitmap(stream);
                m_texture = TextureUtil.CreateTextureFromBitmap(m_bitmap, m_filter, m_wrap);
                m_width = m_bitmap.Width;
                m_height = m_bitmap.Height;
            }
        }

        private void Unload()
        {
            GL.DeleteTexture(m_texture);
            m_texture = -1;

            m_bitmap.Dispose();
            m_bitmap = null;
        }
    }
}

