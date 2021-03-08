using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Util;
using OpenTK;
using SDL2;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dan200.Core.Render
{
    public class Bitmap : IDisposable
    {
        public class Bits : IDisposable
        {
            private Bitmap m_owner;
            private bool m_needsUnlock;

            public readonly int Width;
            public readonly int Height;
            public readonly int Stride;
            public readonly int BytesPerPixel;
            public readonly IntPtr Data;

            public Bits(Bitmap owner, int width, int height, int stride, int bytesPerPixel, IntPtr data, bool needsUnlock)
            {
                m_owner = owner;
                m_needsUnlock = needsUnlock;

                Width = width;
                Height = height;
                Stride = stride;
                BytesPerPixel = bytesPerPixel;
                Data = data;
            }

            public void Dispose()
            {
                if (m_owner.m_lock == this)
                {
                    if (m_needsUnlock)
                    {
                        SDL.SDL_UnlockSurface(m_owner.m_surface);
                    }
                    m_owner.m_lock = null;
                }
            }

            public Vector4 GetPixel(int x, int y)
            {
                x = MathUtils.Clamp(x, 0, Width - 1);
                y = MathUtils.Clamp(y, 0, Height - 1);
                int r = Marshal.ReadByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 0));
                int g = Marshal.ReadByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 1));
                int b = Marshal.ReadByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 2));
                int a = (BytesPerPixel >= 4) ?
                    Marshal.ReadByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 3)) :
                    255;
                return new Vector4(
                    (float)r / 255.0f,
                    (float)g / 255.0f,
                    (float)b / 255.0f,
                    (float)a / 255.0f
                );
            }

            public Vector4 Sample(float xFrac, float yFrac)
            {
                float x = xFrac * (float)Width;
                float y = yFrac * (float)Height;
                int px = (int)Math.Floor(x);
                int py = (int)Math.Floor(y);
                float fx = x - (float)Math.Floor(x);
                float fy = y - (float)Math.Floor(y);
                Vector4 c00 = GetPixel(px, py);
                Vector4 c01 = GetPixel(px, py + 1);
                Vector4 c10 = GetPixel(px + 1, py);
                Vector4 c11 = GetPixel(px + 1, py + 1);
                Vector4 i0 = (1.0f - fx) * c00 + fx * c10;
                Vector4 i1 = (1.0f - fx) * c01 + fx * c11;
                return (1.0f - fy) * i0 + fy * i1;
            }

            public void SetPixel(int x, int y, Vector4 color)
            {
                byte r = (byte)(color.X * 255.0f);
                byte g = (byte)(color.Y * 255.0f);
                byte b = (byte)(color.Z * 255.0f);
                byte a = (byte)(color.W * 255.0f);
                Marshal.WriteByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 0), r);
                Marshal.WriteByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 1), g);
                Marshal.WriteByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 2), b);
                if (BytesPerPixel >= 4)
                {
                    Marshal.WriteByte(new IntPtr(Data.ToInt64() + x * BytesPerPixel + y * Stride + 3), a);
                }
            }
        }

        private int m_width;
        private int m_height;
        private IntPtr m_surface;
        private Bits m_lock;

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

        public IntPtr SDLSurface
        {
            get
            {
                return m_surface;
            }
        }

        public Bitmap(int width, int height) : this(CreateBlankSurface(width, height))
        {
        }

        public Bitmap(Stream stream) : this(CreateSurfaceFromStream(stream))
        {
        }

        public Bitmap(string path) : this(CreateSurfaceFromFile(path))
        {
        }

        private Bitmap(IntPtr surface)
        {
            IntPtr rgbSurface;
            try
            {
                rgbSurface = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                App.CheckSDLResult("SDL_ConvertSurfaceFormat", rgbSurface);
            }
            finally
            {
                SDL.SDL_FreeSurface(surface);
            }

            m_surface = rgbSurface;
            m_lock = null;

            var surfaceDetails = (SDL.SDL_Surface)Marshal.PtrToStructure(
                m_surface,
                typeof(SDL.SDL_Surface)
            );
            m_width = surfaceDetails.w;
            m_height = surfaceDetails.h;

            var pixelFormat = (SDL.SDL_PixelFormat)Marshal.PtrToStructure(
                surfaceDetails.format,
                typeof(SDL.SDL_PixelFormat)
            );
            if (pixelFormat.BitsPerPixel != 32)
            {
                throw new IOException("Textures must be in 32 bit format");
            }
        }

        public Bits Lock()
        {
            if (m_lock != null)
            {
                throw new InvalidOperationException();
            }

            bool needsLock = SDL.SDL_MUSTLOCK(m_surface);
            if (needsLock)
            {
                App.CheckSDLResult("SDL_LockSurface", SDL.SDL_LockSurface(m_surface));
            }

            var surfaceDetails = (SDL.SDL_Surface)Marshal.PtrToStructure(
                m_surface,
                typeof(SDL.SDL_Surface)
            );
            var pixelFormat = (SDL.SDL_PixelFormat)Marshal.PtrToStructure(
                surfaceDetails.format,
                typeof(SDL.SDL_PixelFormat)
            );
            m_lock = new Bits(this, surfaceDetails.w, surfaceDetails.h, surfaceDetails.pitch, pixelFormat.BytesPerPixel, surfaceDetails.pixels, needsLock);
            return m_lock;
        }

        public void FlipY()
        {
            if (m_lock != null)
            {
                throw new InvalidOperationException();
            }

            using (var bits = Lock())
            {
                var buffer = new byte[bits.Width * bits.BytesPerPixel];
                var srcBuffer = new byte[bits.Width * bits.BytesPerPixel];
                for (int y = 0; y < bits.Height / 2; ++y)
                {
                    int srcY = (bits.Height - 1) - y;
                    Marshal.Copy(
                        new IntPtr(bits.Data.ToInt64() + y * bits.Stride),
                        buffer, 0, buffer.Length
                    );
                    Marshal.Copy(
                        new IntPtr(bits.Data.ToInt64() + srcY * bits.Stride),
                        srcBuffer, 0, srcBuffer.Length
                    );
                    Marshal.Copy(
                        buffer, 0,
                        new IntPtr(bits.Data.ToInt64() + srcY * bits.Stride),
                        buffer.Length
                    );
                    Marshal.Copy(
                        srcBuffer, 0,
                        new IntPtr(bits.Data.ToInt64() + y * bits.Stride),
                        srcBuffer.Length
                    );
                }
            }
        }

        public Bitmap Resize(int width, int height, bool maintainAspect, bool smooth)
        {
            if (m_lock != null)
            {
                throw new InvalidOperationException();
            }

            SDL.SDL_Rect srcRect;
            if (maintainAspect)
            {
                // Crop the image to keep the same aspect ratio
                var aspect = (double)m_width / (double)m_height;
                var resizedAspect = (double)width / (double)height;
                if (aspect > resizedAspect)
                {
                    // Crop horizontally
                    int desiredWidth = (width * m_height) / height;
                    srcRect.x = (m_width - desiredWidth) / 2;
                    srcRect.y = 0;
                    srcRect.w = desiredWidth;
                    srcRect.h = m_height;
                }
                else if (aspect < resizedAspect)
                {
                    // Crop vertically
                    int desiredHeight = (height * m_width) / width;
                    srcRect.x = 0;
                    srcRect.y = (m_height - desiredHeight) / 2;
                    srcRect.w = m_width;
                    srcRect.h = desiredHeight;
                }
                else
                {
                    // Don't crop
                    srcRect.x = 0;
                    srcRect.y = 0;
                    srcRect.w = m_width;
                    srcRect.h = m_height;
                }
            }
            else
            {
                // Don't crop
                srcRect.x = 0;
                srcRect.y = 0;
                srcRect.w = m_width;
                srcRect.h = m_height;
            }

            if (smooth)
            {
                var result = new Bitmap(width, height);
                using (var srcBits = Lock())
                {
                    using (var dstBits = result.Lock())
                    {
                        float fDstWidth = (float)dstBits.Width;
                        float fDstHeight = (float)dstBits.Height;
                        float fSrcAreaStartX = (float)srcRect.x / (float)srcBits.Width;
                        float fSrcAreaStartY = (float)srcRect.y / (float)srcBits.Height;
                        float fSrcAreaWidth = (float)srcRect.w / (float)srcBits.Width;
                        float fSrcAreaHeight = (float)srcRect.h / (float)srcBits.Height;
                        float kernelSize = 0.25f;
                        for (int y = 0; y < dstBits.Height; ++y)
                        {
                            for (int x = 0; x < dstBits.Width; ++x)
                            {
                                var c00 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x - kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y - kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                var c01 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x + kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y - kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                var c10 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x - kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y + kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                var c11 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x + kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y + kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                dstBits.SetPixel(x, y, (c00 + c01 + c10 + c11) * 0.25f);
                            }
                        }
                    }
                }
                return result;
            }
            else
            {
                var result = new Bitmap(width, height);
                App.CheckSDLResult("SDL_BlitScaled", SDL.SDL_BlitScaled(
                    m_surface, ref srcRect,
                    result.m_surface, IntPtr.Zero
                ));
                return result;
            }
        }

        public void Blit(Bitmap src, int xPos, int yPos)
        {
            using (var dstBits = Lock())
            {
                using (var srcBits = src.Lock())
                {
                    var xStart = Math.Max(xPos, 0);
                    var xEnd = Math.Min(xPos + srcBits.Width, dstBits.Width);
                    var yStart = Math.Max(yPos, 0);
                    var yEnd = Math.Min(yPos + srcBits.Height, dstBits.Height);
                    for (int dstY = yStart; dstY < yEnd; ++dstY)
                    {
                        var srcY = dstY - yPos;
                        for (int dstX = xStart; dstX < xEnd; ++dstX)
                        {
                            var srcX = dstX - xPos;
                            var srcColor = srcBits.GetPixel(srcX, srcY);
                            var dstColor = dstBits.GetPixel(dstX, dstY);
                            var blendedColor =
                                dstColor.Xyz * (1.0f - srcColor.W) +
                                srcColor.Xyz * srcColor.W;
                            var blendedAlpha =
                                dstColor.W * (1.0f - srcColor.W) +
                                srcColor.W;
                            dstBits.SetPixel(dstX, dstY, new Vector4(blendedColor, blendedAlpha));
                        }
                    }
                }
            }
        }

        public void Save(string path)
        {
            if (m_lock != null)
            {
                throw new InvalidOperationException();
            }
            App.CheckSDLResult("IMG_SavePNG", SDL_image.IMG_SavePNG(m_surface, path));
        }

        public void Dispose()
        {
            if (m_lock != null)
            {
                throw new InvalidOperationException();
            }
            SDL.SDL_FreeSurface(m_surface);
            m_surface = IntPtr.Zero;
        }

        private static IntPtr CreateBlankSurface(int width, int height)
        {
            var surface = SDL.SDL_CreateRGBSurface(0, width, height, 32, 0, 0, 0, 0);
            App.CheckSDLResult("SDL_CreateRGBSurface", surface);
            return surface;
        }

        private static IntPtr CreateSurfaceFromFile(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return CreateSurfaceFromStream(stream);
            }
        }

        private static unsafe IntPtr CreateSurfaceFromStream(Stream stream)
        {
            var data = stream.ReadToEnd();
            fixed (byte* pData = data)
            {
                var rwops = SDL.SDL_RWFromMem(data, data.Length);
                App.CheckSDLResult("SDL_RWFromMem", rwops);

                var surface = SDL_image.IMG_Load_RW(rwops, 1);
                App.CheckSDLResult("IMG_Load_RW", surface);
                return surface;
            }
        }
    }
}
