using System;

namespace Dan200.Core.Computer.Devices.GPU
{
    public unsafe class Image
    {
        public readonly object Lock;
        private readonly ChangeListener m_changeListener;
        public readonly byte[] Data;
        public readonly int Start;
        public readonly int Width;
        public readonly int Height;
        public readonly int Stride;

        public int Version
        {
            get
            {
                return m_changeListener.Version;
            }
        }

        public byte this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new ArgumentOutOfRangeException();
                }
                fixed (byte* pData = Data)
                {
                    return pData[Start + x + y * Stride];
                }
            }
            set
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new ArgumentOutOfRangeException();
                }
                fixed (byte* pData = Data)
                {
                    pData[Start + x + y * Stride] = value;
                }
                Change();
            }
        }

        private Image(object _lock, ChangeListener changeListener, byte[] data, int start, int width, int height, int stride)
        {
            Lock = _lock;
            m_changeListener = changeListener;
            Data = data;
            Start = start;
            Width = width;
            Height = height;
            Stride = stride;
        }

        public Image(int width, int height, byte fill = 0)
        {
            Lock = new object();
            m_changeListener = new ChangeListener();
            int length = width * height;
            var data = new byte[length];
            if (fill != 0)
            {
                fixed (byte* pData = data)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        pData[i] = fill;
                    }
                }
            }
            Data = data;
            Start = 0;
            Width = width;
            Height = height;
            Stride = width;
        }

        public byte[] Read(int x, int y, int length)
        {
            var data = Data;
            var start = Start;
            var stride = Stride;
            var width = Width;
            var height = Height;
            if (x < 0 || x >= width ||
                y < 0 || y >= height ||
                length < 0 || (x + y * width + length) > width * height)
            {
                throw new InvalidOperationException();
            }

            var outData = new byte[length];
            fixed (byte* pData = data)
            {
                fixed (byte* pOutData = outData)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        pOutData[i] = pData[start + x + y * stride];
                        ++x;
                        if (x >= width)
                        {
                            x = 0;
                            ++y;
                        }
                    }
                }
            }
            return outData;
        }

        public void Blit(Image image, int x, int y)
        {
            int startX = Math.Max(x, 0);
            int startY = Math.Max(y, 0);
            int endX = Math.Min(x + image.Width, Width);
            int endY = Math.Min(y + image.Height, Height);
            var data = Data;
            var start = Start;
            var stride = Stride;
            var srcData = image.Data;
            var srcStart = image.Start;
            var srcStride = image.Stride;
            if (endX > startX)
            {
                for (int py = startY; py < endY; ++py)
                {
                    Buffer.BlockCopy(
                        srcData, (srcStart + (startX - x) + (py - y) * srcStride) * sizeof(byte),
                        data, (start + startX + py * stride) * sizeof(byte),
                        (endX - startX) * sizeof(byte)
                    );
                }
                Change();
            }
        }

        public void Write(byte[] src, int srcStart, int srcLength, int x, int y)
        {
            var data = Data;
            var start = Start;
            var stride = Stride;
            var width = Width;
            var height = Height;
            if (x < 0 || x >= width ||
                y < 0 || y >= height ||
                srcStart < 0 ||
                srcLength < 0 ||
                srcStart + srcLength > src.Length ||
                (x + y * width + srcLength) > width * height)
            {
                throw new InvalidOperationException();
            }

            fixed (byte* pData = data)
            {
                fixed (byte* pSrcData = src)
                {
                    var srcEnd = srcStart + srcLength;
                    for (int i = srcStart; i < srcEnd; ++i)
                    {
                        pData[start + x + y * stride] = pSrcData[i];
                        ++x;
                        if (x >= width)
                        {
                            x = 0;
                            ++y;
                        }
                    }
                }
            }
        }

        public void Fill(byte fill)
        {
            Fill(fill, 0, 0, Width, Height);
        }

        public void Fill(byte fill, int x, int y, int width, int height)
        {
            if (x < 0 || width < 0 || x + width > Width ||
                y < 0 || height < 0 || y + height > Height)
            {
                throw new InvalidOperationException();
            }

            var data = Data;
            var start = Start;
            var stride = Stride;
            var startX = x;
            var startY = y;
            var endX = x + width;
            var endY = y + height;
            fixed (byte* pData = data)
            {
                for (int py = startY; py < endY; ++py)
                {
                    for (int px = startX; px < endX; ++px)
                    {
                        pData[start + px + py * stride] = fill;
                    }
                }
            }
            Change();
        }

        public void FlipX()
        {
            var data = Data;
            var start = Start;
            var width = Width;
            var height = Height;
            var stride = Stride;
            fixed (byte* pData = data)
            {
                for (int py = 0; py < height; ++py)
                {
                    for (int px = 0; px < (width / 2); ++px)
                    {
                        int sx = (width - 1) - px;
                        var temp = pData[start + sx + py * stride];
                        pData[start + sx + py * stride] = pData[start + px + py * stride];
                        pData[start + px + py * stride] = temp;
                    }
                }
            }
            Change();
        }

        public void FlipY()
        {
            var data = Data;
            var start = Start;
            var width = Width;
            var height = Height;
            var stride = Stride;
            fixed (byte* pData = data)
            {
                for (int py = 0; py < (height / 2); ++py)
                {
                    int sy = (height - 1) - py;
                    for (int px = 0; px < width; ++px)
                    {
                        var temp = pData[start + px + sy * stride];
                        pData[start + px + sy * stride] = pData[start + px + py * stride];
                        pData[start + px + py * stride] = temp;
                    }
                }
            }
            Change();
        }

        private void Transpose()
        {
            var data = Data;
            var start = Start;
            var width = Width;
            var height = Height;
            var stride = Stride;
            if (width != height)
            {
                throw new InvalidOperationException();
            }
            fixed (byte* pData = data)
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int x = y + 1; x < width; ++x)
                    {
                        var temp = pData[start + x + y * stride];
                        pData[start + x + y * stride] = pData[start + y + x * stride];
                        pData[start + y + x * stride] = temp;
                    }
                }
            }
        }

        public void Rotate90()
        {
            Transpose();
            FlipX();
        }

        public void Rotate180()
        {
            var data = Data;
            var start = Start;
            var width = Width;
            var height = Height;
            var stride = Stride;
            fixed (byte* pData = data)
            {
                for (int py = 0; py < (height / 2); ++py)
                {
                    int sy = (height - 1) - py;
                    for (int px = 0; px < width; ++px)
                    {
                        int sx = (width - 1) - px;
                        var temp = pData[start + sx + sy * stride];
                        pData[start + sx + sy * stride] = pData[start + px + py * stride];
                        pData[start + px + py * stride] = temp;
                    }
                }
            }
            Change();
        }

        public void Rotate270()
        {
            Transpose();
            FlipY();
        }

        public void Translate(int x, int y, byte fill)
        {
            if (x == 0 && y == 0)
            {
                return;
            }

            var data = Data;
            var start = Start;
            var width = Width;
            var height = Height;
            var stride = Stride;
            var diff = x + y * stride;
            if (diff < 0)
            {
                fixed (byte* pData = data)
                {
                    for (int py = 0; py < height; ++py)
                    {
                        int sy = py - y;
                        for (int px = 0; px < width; ++px)
                        {
                            int sx = px - x;
                            if (sx >= 0 && sx < width && sy >= 0 && sy < height)
                            {
                                pData[start + px + py * stride] = pData[start + sx + sy * stride];
                            }
                            else
                            {
                                pData[start + px + py * stride] = fill;
                            }
                        }
                    }
                }
            }
            else
            {
                fixed (byte* pData = data)
                {
                    for (int py = height - 1; py >= 0; --py)
                    {
                        int sy = py - y;
                        for (int px = width - 1; px >= 0; --px)
                        {
                            int sx = px - x;
                            if (sx >= 0 && sx < width && sy >= 0 && sy < height)
                            {
                                pData[start + px + py * stride] = pData[start + sx + sy * stride];
                            }
                            else
                            {
                                pData[start + px + py * stride] = fill;
                            }
                        }
                    }
                }
            }
            Change();
        }

        public void Replace(byte inColor, byte outColor)
        {
            if (inColor == outColor)
            {
                return;
            }

            var data = Data;
            var start = Start;
            var stride = Stride;
            var width = Width;
            var height = Height;
            fixed (byte* pData = data)
            {
                for (int py = 0; py < height; ++py)
                {
                    for (int px = 0; px < width; ++px)
                    {
                        var address = start + px + py * stride;
                        if (pData[address] == inColor)
                        {
                            pData[address] = outColor;
                        }
                    }
                }
            }
            Change();
        }

        public Image Sub(int x, int y, int width, int height)
        {
            if (x == 0 && width == Width && y == 0 && height == Height)
            {
                return this;
            }
            else
            {
                var data = Data;
                var start = Start;
                var stride = Stride;
                return new Image(Lock, m_changeListener, data, start + x + y * stride, width, height, stride);
            }
        }

        public Image Copy()
        {
            var data = Data;
            var start = Start;
            var width = Width;
            var height = Height;
            var stride = Stride;
            var outData = new byte[width * height];
            fixed (byte* pOutData = outData)
            {
                fixed (byte* pData = data)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            pOutData[x + y * width] = pData[start + x + y * stride];
                        }
                    }
                }
            }
            return new Image(new object(), new ChangeListener(), outData, 0, width, height, width);
        }

        public void Change()
        {
            m_changeListener.Change();
        }
    }
}
