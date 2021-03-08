using Dan200.Core.Assets;
using Dan200.Core.Main;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Render
{
    public class TextureAtlas : ITexture
    {
        private const int PADDING = 1;

        private static Dictionary<string, TextureAtlas> s_atlases = new Dictionary<string, TextureAtlas>();

        public static void Reload(string path)
        {
            if (s_atlases.ContainsKey(path))
            {
                s_atlases[path].Reload();
            }
            else
            {
                s_atlases[path] = new TextureAtlas(path);
            }
        }

        public static void ReloadAll()
        {
            foreach (var pair in s_atlases)
            {
                pair.Value.Reload();
            }
        }

        public static TextureAtlas Get(string path)
        {
            if (s_atlases.ContainsKey(path))
            {
                return s_atlases[path];
            }
            throw new AssetLoadException(path, "No such atlas");
        }

        private string m_path;
        private int m_texture;
        private int m_width;
        private int m_height;
        private IDictionary<string, Quad> m_textureAreas;

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

        public TextureAtlas(string path)
        {
            m_path = path;
            m_textureAreas = new Dictionary<string, Quad>();
            Load();
        }

        public void Reload()
        {
            Unload();
            Load();
        }

        public void Dispose()
        {
            Unload();
        }

        public Quad? GetTextureArea(string path)
        {
            if (m_textureAreas.ContainsKey(path))
            {
                return m_textureAreas[path];
            }
            return null;
        }

        private int NextPowerOfTwo(int x)
        {
            int result = 1;
            while (result < x)
            {
                result <<= 1;
            }
            return result;
        }

        private int NextSquarePowerOfTwo(int x)
        {
            int result = 1;
            while ((result * result) < x)
            {
                result <<= 1;
            }
            return result;
        }

        private void TryStoreTextures(List<Texture> textures, int x, int y, int squareSize)
        {
            // If we ran out of textures, nothing left to do
            if (textures.Count == 0)
            {
                return;
            }

            // Try to find a texture which fits this space exactly
            for (int i = 0; i < textures.Count; ++i)
            {
                var texture = textures[i];
                var textureSquareSize = Math.Max(
                    NextPowerOfTwo(texture.Width + 2 * PADDING),
                    NextPowerOfTwo(texture.Height + 2 * PADDING)
                );
                if (textureSquareSize == squareSize)
                {
                    // If we find one, take up the whole area
                    // Blit it onto the output texture
                    TextureUtil.BlitSubTextureFromBitmap(m_texture, x, y, texture.Bitmap, PADDING);

                    // Store the dimensions
                    m_textureAreas.Add(texture.Path, new Quad(
                        (float)(x + PADDING) / (float)m_width,
                        (float)(y + PADDING) / (float)m_height,
                        (float)texture.Width / (float)m_width,
                        (float)texture.Height / (float)m_height
                    ));

                    // Remove from consideration for elsewhere
                    textures.RemoveAt(i);
                    return;
                }
            }

            // Otherwise, find space for the smaller textures by descending into quadtrees
            int halfSize = squareSize / 2;
            if (halfSize > 0)
            {
                TryStoreTextures(textures, x, y, halfSize);
                TryStoreTextures(textures, x + halfSize, y, halfSize);
                TryStoreTextures(textures, x, y + halfSize, halfSize);
                TryStoreTextures(textures, x + halfSize, y + halfSize, halfSize);
            }
        }

        private void Load()
        {
            // Determine the list of all the textures to load
            var textures = new List<Texture>();
            textures.Add(Texture.Get("defaults/default.png", false));
            textures.Add(Texture.Get("black.png", false));
            textures.Add(Texture.Get("white.png", false));
            textures.Add(Texture.Get("flat.png", false));
            textures.AddRange(Assets.Assets.Find<Texture>(m_path));

            // Measure all the textures
            int totalPixels = 0;
            int smallestSquareSize = 0;
            foreach (var texture in textures)
            {
                var squareSize = Math.Max(
                    NextPowerOfTwo(texture.Width + 2 * PADDING),
                    NextPowerOfTwo(texture.Height + 2 * PADDING)
                );
                totalPixels += squareSize * squareSize;
                smallestSquareSize = Math.Max(smallestSquareSize, squareSize);
            }

            // Create and populate the output texture
            int size = Math.Max(smallestSquareSize, NextSquarePowerOfTwo(totalPixels));
            m_width = size;
            m_height = size;
            m_texture = TextureUtil.CreateBlankTexture(m_width, m_height, false, false);
            TryStoreTextures(textures, 0, 0, size);

            App.DebugLog("Created atlas {0} sized {1}x{2} containing {3} textures", m_path, m_width, m_height, m_textureAreas.Count);
        }

        private void Unload()
        {
            m_textureAreas.Clear();
            GL.DeleteTexture(m_texture);
            m_texture = -1;
        }
    }
}

