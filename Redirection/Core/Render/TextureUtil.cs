using Dan200.Core.Main;
using OpenTK.Graphics.OpenGL;
using System;

namespace Dan200.Core.Render
{
    public static class TextureUtil
    {
        public static int CreateBlankTexture(int width, int height, bool filter, bool wrap)
        {
            var tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                width, height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                IntPtr.Zero
            );
            App.CheckOpenGLError();
            SetParameters(filter, wrap);
            return tex;
        }

        public static int CreateTextureFromBitmap(Bitmap bitmap, bool filter, bool wrap)
        {
            var tex = GL.GenTexture();
            UpdateTextureFromBitmap(tex, bitmap);
            SetParameters(filter, wrap);
            return tex;
        }

        public static void UpdateTextureFromBitmap(int tex, Bitmap bitmap)
        {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            using (var bits = bitmap.Lock())
            {
                try
                {
                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, bits.Stride / bits.BytesPerPixel);
                    GL.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        PixelInternalFormat.Rgba,
                        bits.Width, bits.Height,
                        0,
                        (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                        PixelType.UnsignedByte,
                        bits.Data
                    );
                }
                finally
                {
                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
                    App.CheckOpenGLError();
                }
            }
        }

        public static void BlitSubTextureFromBitmap(int destTexture, int xPosition, int yPosition, Bitmap bitmap, int padding)
        {
            GL.BindTexture(TextureTarget.Texture2D, destTexture);
            using (var bits = bitmap.Lock())
            {
                try
                {
                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, bits.Stride / bits.BytesPerPixel);
                    if (padding == 0)
                    {
                        // Whole image
                        GL.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            xPosition, yPosition,
                            bitmap.Width, bitmap.Height,
                            (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                            PixelType.UnsignedByte,
                            bits.Data
                        );
                    }
                    else
                    {
                        // Top padding
                        GL.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            xPosition + 1, yPosition,
                            bitmap.Width, 1,
                            (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                            PixelType.UnsignedByte,
                            bits.Data
                        );
                        // Left padding
                        GL.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            xPosition, yPosition + 1,
                            1, bitmap.Height,
                            (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                            PixelType.UnsignedByte,
                            bits.Data
                        );
                        // Image
                        GL.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            xPosition + 1, yPosition + 1,
                            bitmap.Width, bitmap.Height,
                            (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                            PixelType.UnsignedByte,
                            bits.Data
                        );
                        // Right padding
                        GL.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            xPosition + 1 + bitmap.Width, yPosition + 1,
                            1, bitmap.Height,
                            (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                            PixelType.UnsignedByte,
                            new IntPtr(bits.Data.ToInt64() + bits.Stride - bits.BytesPerPixel)
                        );
                        // Bottom padding
                        GL.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            xPosition + 1, yPosition + 1 + bitmap.Height,
                            bitmap.Width, 1,
                            (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                            PixelType.UnsignedByte,
                            new IntPtr(bits.Data.ToInt64() + (bitmap.Height - 1) * bits.Stride)
                        );
                    }
                }
                finally
                {
                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
                    App.CheckOpenGLError();
                }
            }
        }

        public static void SetParameters(bool filter, bool wrap)
        {
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                filter ? (int)TextureMinFilter.Linear : (int)TextureMinFilter.Nearest
            );
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                filter ? (int)TextureMagFilter.Linear : (int)TextureMagFilter.Nearest
            );
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS,
                wrap ? (int)TextureWrapMode.Repeat : (int)TextureWrapMode.ClampToEdge
            );
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,
                wrap ? (int)TextureWrapMode.Repeat : (int)TextureWrapMode.ClampToEdge
            );
            App.CheckOpenGLError();
        }
    }
}

