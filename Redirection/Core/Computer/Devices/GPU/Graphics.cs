using System;

namespace Dan200.Core.Computer.Devices.GPU
{
    public unsafe class Graphics
    {
        private Image m_target;
        private byte[] m_colorMapping;
        private Font m_font;
        private int m_pixelsDrawn;

        public Image Target
        {
            get
            {
                return m_target;
            }
            set
            {
                m_target = value;
            }
        }

        public Font Font
        {
            get
            {
                return m_font;
            }
            set
            {
                m_font = value;
            }
        }

        public int PixelsDrawn
        {
            get
            {
                return m_pixelsDrawn;
            }
        }

        public int OffsetX;
        public int OffsetY;
        public byte? TransparentColor;

        public Graphics()
        {
            m_target = null;
            m_colorMapping = new byte[256];
            m_font = null;
            m_pixelsDrawn = 0;
            Reset();
        }

        public void Reset()
        {
            ResetColorMappings();
            OffsetX = 0;
            OffsetY = 0;
            TransparentColor = null;
            Font = null;
            Target = null;
        }

        public byte GetColorMapping(byte inColor)
        {
            return Remap(inColor);
        }

        public void SetColorMapping(byte inColor, byte outColor)
        {
            if (inColor < m_colorMapping.Length)
            {
                m_colorMapping[inColor] = outColor;
            }
        }

        public void ResetColorMappings()
        {
            for (int i = 0; i < m_colorMapping.Length; ++i)
            {
                m_colorMapping[i] = (byte)i;
            }
        }

        public void Clear(byte color)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }
            color = Remap(color);
            if (!TransparentColor.HasValue || color != TransparentColor.Value)
            {
                target.Fill(color);
                m_pixelsDrawn += target.Width * target.Height;
            }
        }

        public byte GetPixel(int x, int y)
        {
            var target = m_target;
            if (target == null)
            {
                return 0;
            }
            Transform(ref x, ref y);
            if (x >= 0 && x < target.Width &&
                y >= 0 && y < target.Height)
            {
                return target[x, y];
            }
            return 0;
        }

        public void DrawPixel(int x, int y, byte color)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref x, ref y);
            color = Remap(color);
            if (x >= 0 && x < target.Width &&
                y >= 0 && y < target.Height)
            {
                if (!TransparentColor.HasValue || color != TransparentColor.Value)
                {
                    target[x, y] = color;
                    ++m_pixelsDrawn;
                }
            }
        }

        private static int GetRegion(int x, int y, int width, int height)
        {
            int code = 0;
            if (y >= height)
            {
                code += 1;
            }
            else if (y < 0)
            {
                code += 2;
            }
            if (x >= width)
            {
                code += 4;
            }
            else if (x < 0)
            {
                code += 8;
            }
            return code;
        }

        private static bool ClipLine(ref int startX, ref int startY, ref int endX, ref int endY, int width, int height)
        {
            int startCode = GetRegion(startX, startY, width, height);
            int endCode = GetRegion(endX, endY, width, height);
            while (true)
            {
                if ((startCode | endCode) == 0)
                {
                    return true;
                }
                else if ((startCode & endCode) != 0)
                {
                    return false;
                }
                else
                {
                    int x, y;
                    int codeOutside = (startCode != 0) ? startCode : endCode;
                    if ((codeOutside & 1) != 0)
                    {
                        x = startX + (endX - startX) * ((height - 1) - startY) / (endY - startY);
                        y = height - 1;
                    }
                    else if ((codeOutside & 2) != 0)
                    {
                        x = startX + (endX - startX) * (0 - startY) / (endY - startY);
                        y = 0;
                    }
                    else if ((codeOutside & 4) != 0)
                    {
                        y = startY + (endY - startY) * ((width - 1) - startX) / (endX - startX);
                        x = width - 1;
                    }
                    else
                    {
                        y = startY + (endY - startY) * (0 - startX) / (endX - startX);
                        x = 0;
                    }
                    if (codeOutside == startCode)
                    {
                        startX = x;
                        startY = y;
                        startCode = GetRegion(x, y, width, height);
                    }
                    else
                    {
                        endX = x;
                        endY = y;
                        endCode = GetRegion(x, y, width, height);
                    }
                }
            }
        }

        private static int DrawLineImpl(Image target, int startX, int startY, int endX, int endY, byte c)
        {
            var width = target.Width;
            var height = target.Height;

            if (!ClipLine(ref startX, ref startY, ref endX, ref endY, width, height))
            {
                return 0;
            }

            var data = target.Data;
            var start = target.Start;
            var stride = target.Stride;
            bool steep = Math.Abs(endY - startY) > Math.Abs(startX - endX);
            if (steep)
            {
                int temp;
                if (startY > endY)
                {
                    temp = startX;
                    startX = endX;
                    endX = temp;
                    temp = startY;
                    startY = endY;
                    endY = temp;
                }

                int dy = endY - startY;
                int dx = endX - startX;
                int err = dy / 2;
                int xStep = (startX < endX) ? 1 : -1;

                int x = startX;
                fixed (byte* pData = data)
                {
                    for (int y = startY; y <= endY; ++y)
                    {
                        pData[start + x + y * stride] = c;
                        err -= dx * xStep;
                        if (err < 0)
                        {
                            x += xStep;
                            err += dy;
                        }
                    }
                }
                return (endY - startY + 1);


            }
            else
            {
                int temp;
                if (startX > endX)
                {
                    temp = startX;
                    startX = endX;
                    endX = temp;
                    temp = startY;
                    startY = endY;
                    endY = temp;
                }

                int dx = endX - startX;
                int dy = Math.Abs(endY - startY);
                int err = dx / 2;
                int yStep = (startY < endY) ? 1 : -1;
                int y = startY;
                fixed (byte* pData = data)
                {
                    for (int x = startX; x <= endX; ++x)
                    {
                        pData[start + x + y * stride] = c;
                        err -= dy;
                        if (err < 0)
                        {
                            y += yStep;
                            err += dx;
                        }
                    }
                }
                return (endX - startX + 1);

            }
        }

        public void DrawLine(int startX, int startY, int endX, int endY, byte color)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref startX, ref startY);
            Transform(ref endX, ref endY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }
            m_pixelsDrawn += DrawLineImpl(target, startX, startY, endX, endY, color);
            target.Change();
        }

        public void DrawTriangle(int aX, int aY, int bX, int bY, int cX, int cY, byte color)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref aX, ref aY);
            Transform(ref bX, ref bY);
            Transform(ref cX, ref cY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }

            // Sort verticies by Y
            if (aY > bY)
            {
                int tempX = aX;
                int tempY = aY;
                aX = bX;
                aY = bY;
                bX = tempX;
                bY = tempY;
            }
            if (bY > cY)
            {
                int tempX = bX;
                int tempY = bY;
                bX = cX;
                bY = cY;
                cX = tempX;
                cY = tempY;
            }
            if (aY > bY)
            {
                int tempX = aX;
                int tempY = aY;
                aX = bX;
                aY = bY;
                bX = tempX;
                bY = tempY;
            }

            if (bY == cY)
            {
                m_pixelsDrawn += DrawFlatBottomTriangleImpl(
                    target,
                    aX, aY,
                    Math.Min(bX, cX), Math.Max(bX, cX), bY,
                    color
                );
            }
            else if (aY == bY)
            {
                m_pixelsDrawn += DrawFlatTopTriangleImpl(
                    target,
                    Math.Min(aX, bX), Math.Max(aX, bX), aY,
                    cX, cY,
                    color
                );
            }
            else
            {
                int dX = aX + (((cX - aX) * (bY - aY)) / (cY - aY));
                m_pixelsDrawn += DrawFlatBottomTriangleImpl(
                    target,
                    aX, aY,
                    Math.Min(bX, dX), Math.Max(bX, dX), bY,
                    color
                );
                m_pixelsDrawn += DrawFlatTopTriangleImpl(
                    target,
                    Math.Min(bX, dX), Math.Max(bX, dX), bY,
                    cX, cY,
                    color
                );
            }
            target.Change();
        }

        private static int DrawFlatBottomTriangleImpl(Image target, int topX, int topY, int bottomLeftX, int bottomRightX, int bottomY, byte c)
        {
            var data = target.Data;
            var start = target.Start;
            var width = target.Width;
            var height = target.Height;
            var stride = target.Stride;

            int dy = bottomY - topY;
            int dlx = bottomLeftX - topX;
            int drx = bottomRightX - topX;
            int lerr = dy / 2;
            int rerr = dy / 2;
            int lxStep = (topX < bottomLeftX) ? 1 : -1;
            int rxStep = (topX < bottomRightX) ? 1 : -1;

            int lx = topX;
            int rx = topX;
            if (topY < 0)
            {
                int yCycles = 0 - topY;
                lerr -= yCycles * dlx * lxStep;
                if (lerr < 0)
                {
                    int cycles = (-lerr / dy) + 1;
                    lerr += dy * cycles;
                    lx += lxStep * cycles;
                }
                rerr -= yCycles * drx * rxStep;
                while (rerr < 0)
                {
                    int cycles = (-rerr / dy) + 1;
                    rerr += dy * cycles;
                    rx += rxStep * cycles;
                }
                topY = 0;
            }
            if (bottomY >= height)
            {
                bottomY = height - 1;
            }
            int pixelsDrawn = 0;
            fixed (byte* pData = data)
            {
                for (int y = topY; y <= bottomY; ++y)
                {
                    int clx = Math.Max(lx, 0);
                    int crx = Math.Min(rx, width - 1);
                    for (int x = clx; x <= crx; ++x)
                    {
                        pData[start + x + y * stride] = c;
                    }
                    pixelsDrawn += (crx - clx + 1);
                    lerr -= dlx * lxStep;
                    if (lerr < 0)
                    {
                        int cycles = (-lerr / dy) + 1;
                        lerr += dy * cycles;
                        lx += lxStep * cycles;
                    }
                    rerr -= drx * rxStep;
                    if (rerr < 0)
                    {
                        int cycles = (-rerr / dy) + 1;
                        rerr += dy * cycles;
                        rx += rxStep * cycles;
                    }
                }
            }
            return pixelsDrawn;
        }

        private static int DrawFlatTopTriangleImpl(Image target, int topLeftX, int topRightX, int topY, int bottomX, int bottomY, byte c)
        {
            var data = target.Data;
            var start = target.Start;
            var width = target.Width;
            var height = target.Height;
            var stride = target.Stride;

            int dy = bottomY - topY;
            int dlx = bottomX - topLeftX;
            int drx = bottomX - topRightX;
            int lerr = dy / 2;
            int rerr = dy / 2;
            int lxStep = (topLeftX < bottomX) ? 1 : -1;
            int rxStep = (topRightX < bottomX) ? 1 : -1;

            int lx = topLeftX;
            int rx = topRightX;
            if (topY < 0)
            {
                int yCycles = 0 - topY;
                lerr -= yCycles * dlx * lxStep;
                if (lerr < 0)
                {
                    int cycles = (-lerr / dy) + 1;
                    lerr += dy * cycles;
                    lx += lxStep * cycles;
                }
                rerr -= yCycles * drx * rxStep;
                while (rerr < 0)
                {
                    int cycles = (-rerr / dy) + 1;
                    rerr += dy * cycles;
                    rx += rxStep * cycles;
                }
                topY = 0;
            }
            if (bottomY >= height)
            {
                bottomY = height - 1;
            }
            int pixelsDrawn = 0;
            fixed (byte* pData = data)
            {
                for (int y = topY; y <= bottomY; ++y)
                {
                    int clx = Math.Max(lx, 0);
                    int crx = Math.Min(rx, width - 1);
                    for (int x = clx; x <= crx; ++x)
                    {
                        pData[start + x + y * stride] = c;
                    }
                    pixelsDrawn += (crx - clx + 1);
                    lerr -= dlx * lxStep;
                    if (lerr < 0)
                    {
                        int cycles = (-lerr / dy) + 1;
                        lerr += dy * cycles;
                        lx += lxStep * cycles;
                    }
                    rerr -= drx * rxStep;
                    if (rerr < 0)
                    {
                        int cycles = (-rerr / dy) + 1;
                        rerr += dy * cycles;
                        rx += rxStep * cycles;
                    }
                }
            }
            return pixelsDrawn;
        }

        public void DrawTriangleOutline(int aX, int aY, int bX, int bY, int cX, int cY, byte color)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref aX, ref aY);
            Transform(ref bX, ref bY);
            Transform(ref cX, ref cY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }
            m_pixelsDrawn += DrawLineImpl(target, aX, aY, bX, bY, color);
            m_pixelsDrawn += DrawLineImpl(target, bX, bY, cX, cY, color);
            m_pixelsDrawn += DrawLineImpl(target, cX, cY, aX, aY, color);
            target.Change();
        }

        public void DrawBox(int startX, int startY, int w, int h, byte color)
        {
            if (w <= 0 || h <= 0)
            {
                return;
            }
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref startX, ref startY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }

            int sx = startX;
            int sy = startY;
            target.Fill(color, sx, sy, w, h);
        }

        public void DrawBoxOutline(int startX, int startY, int w, int h, byte color)
        {
            if (w <= 0 || h <= 0)
            {
                return;
            }
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref startX, ref startY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }

            var width = target.Width;
            var height = target.Height;
            var data = target.Data;
            var start = target.Start;
            var stride = target.Stride;

            int sx = startX;
            int sy = startY;
            int ex = startX + w - 1;
            int ey = startY + h - 1;

            fixed (byte* pData = data)
            {
                int csx = Math.Max(sx, 0);
                int cex = Math.Min(ex, width - 1);
                if (cex >= csx)
                {
                    if (sy >= 0 && sy < height)
                    {
                        for (int x = csx; x <= cex; ++x)
                        {
                            pData[start + x + sy * stride] = color;
                        }
                        m_pixelsDrawn += (cex - csx + 1);
                    }
                    if (ey >= 0 && ey < height)
                    {
                        for (int x = csx; x <= cex; ++x)
                        {
                            pData[start + x + ey * stride] = color;
                        }
                        m_pixelsDrawn += (cex - csx + 1);
                    }
                }

                int csy = Math.Max(sy + 1, 0);
                int cey = Math.Min(ey - 1, height - 1);
                if (cey >= csy)
                {
                    if (sx >= 0 && sx < width)
                    {
                        for (int y = csy; y <= cey; ++y)
                        {
                            pData[start + sx + y * stride] = color;
                        }
                        m_pixelsDrawn += (cey - csy + 1);
                    }
                    if (ex >= 0 && ex < width)
                    {
                        for (int y = csy; y <= cey; ++y)
                        {
                            pData[start + ex + y * stride] = color;
                        }
                        m_pixelsDrawn += (cey - csy + 1);
                    }
                }
            }
            target.Change();
        }

        public void DrawEllipse(int startX, int startY, int w, int h, byte color)
        {
            if (w <= 0 || h <= 0)
            {
                return;
            }
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref startX, ref startY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }

            var endX = startX + w - 1;
            var endY = startY + h - 1;
            if (startX == endX || startY == endY)
            {
                m_pixelsDrawn += DrawLineImpl(target, startX, startY, endX, endY, color);
                return;
            }

            var minY = startY;
            var maxY = endY;
            var minX = startX;
            var maxX = endX;
            var xRadius = (maxX + 1 - minX) / 2;
            var yRadius = (maxY + 1 - minY) / 2;
            var xPush = -((maxX - minX) % 2);
            var yPush = -((maxY - minY) % 2);

            var x = minX + xRadius;
            var y = minY + yRadius;
            var twoASquare = 2 * xRadius * xRadius;
            var twoBSquare = 2 * yRadius * yRadius;

            var data = target.Data;
            var start = target.Start;
            var width = target.Width;
            var height = target.Height;
            var stride = target.Stride;

            fixed (byte* pData = data)
            {
                int yGapStart = 0;
                int yGapEnd = yRadius;
                {
                    // Flat parts
                    int px = 0;
                    int py = yRadius;
                    int xChange = yRadius * yRadius;
                    int yChange = xRadius * xRadius * (1 - 2 * yRadius);
                    int ellipseError = 0;
                    int stoppingX = 0;
                    int stoppingY = twoASquare * yRadius;
                    while (stoppingX <= stoppingY)
                    {
                        // Plot
                        int top = y - py;
                        int bottom = y + py + yPush;
                        int left = x - px;
                        int right = x + px + xPush;
                        if (right < left)
                        {
                            int temp = left;
                            left = right;
                            right = temp;
                        }
                        left = Math.Max(left, 0);
                        right = Math.Min(right, width - 1);
                        if (right >= left)
                        {
                            if (top >= 0 && top < height)
                            {
                                for (int lx = left; lx <= right; ++lx)
                                {
                                    pData[start + lx + top * stride] = color;
                                }
                                m_pixelsDrawn += (right - left + 1);
                            }
                            if (bottom >= 0 && bottom < height)
                            {
                                for (int lx = left; lx <= right; ++lx)
                                {
                                    pData[start + lx + bottom * stride] = color;
                                }
                                m_pixelsDrawn += (right - left + 1);
                            }
                        }

                        // Record
                        yGapEnd = py - 1;

                        // Increment
                        px++;
                        stoppingX += twoBSquare;
                        ellipseError += xChange;
                        xChange += twoBSquare;
                        if (2 * ellipseError + yChange > 0)
                        {
                            py--;
                            stoppingY -= twoASquare;
                            ellipseError += yChange;
                            yChange += twoASquare;
                        }
                    }
                }
                {
                    // Steep parts
                    int px = xRadius;
                    int py = 0;
                    int xChange = yRadius * yRadius * (1 - 2 * xRadius);
                    int yChange = xRadius * xRadius;
                    int ellipseError = 0;
                    int stoppingX = twoBSquare * xRadius;
                    int stoppingY = 0;
                    while (stoppingX >= stoppingY)
                    {
                        // Plot
                        int top = y - py;
                        int bottom = y + py + yPush;
                        int left = x - px;
                        int right = x + px + xPush;
                        if (right < left)
                        {
                            int temp = left;
                            left = right;
                            right = temp;
                        }
                        left = Math.Max(left, 0);
                        right = Math.Min(right, width - 1);
                        if (right >= left)
                        {
                            if (top >= 0 && top < height)
                            {
                                for (int lx = left; lx <= right; ++lx)
                                {
                                    pData[start + lx + top * stride] = color;
                                }
                                m_pixelsDrawn += (right - left + 1);
                            }
                            if (bottom >= 0 && bottom < height)
                            {
                                for (int lx = left; lx <= right; ++lx)
                                {
                                    pData[start + lx + bottom * stride] = color;
                                }
                                m_pixelsDrawn += (right - left + 1);
                            }
                        }

                        // Record
                        yGapStart = py + 1;

                        // Increment
                        py++;
                        stoppingY += twoASquare;
                        ellipseError += yChange;
                        yChange += twoASquare;
                        if (2 * ellipseError + xChange > 0)
                        {
                            px--;
                            stoppingX -= twoBSquare;
                            ellipseError += xChange;
                            xChange += twoBSquare;
                        }
                    }
                }
                {
                    // Plug the weird gap the algorithm leaves on very tall ellipses
                    if (yGapEnd >= yGapStart)
                    {
                        m_pixelsDrawn += DrawLineImpl(target, x, y - yGapStart, x, y - yGapEnd, color);
                        m_pixelsDrawn += DrawLineImpl(target, x, y + yGapStart + yPush, x, y + yGapEnd + yPush, color);
                        if (xPush != 0)
                        {
                            m_pixelsDrawn += DrawLineImpl(target, x + xPush, y - yGapStart, x + xPush, y - yGapEnd, color);
                            m_pixelsDrawn += DrawLineImpl(target, x + xPush, y + yGapStart + yPush, x + xPush, y + yGapEnd + yPush, color);
                        }
                    }
                }
                target.Change();
            }
        }

        public void DrawEllipseOutline(int startX, int startY, int w, int h, byte color)
        {
            if (w <= 0 || h <= 0)
            {
                return;
            }
            var target = m_target;
            if (target == null)
            {
                return;
            }
            Transform(ref startX, ref startY);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }

            var endX = startX + w - 1;
            var endY = startY + h - 1;
            if (startX == endX || startY == endY)
            {
                m_pixelsDrawn += DrawLineImpl(target, startX, startY, endX, endY, color);
                return;
            }

            var minY = startY;
            var maxY = endY;
            var minX = startX;
            var maxX = endX;
            var xRadius = (maxX + 1 - minX) / 2;
            var yRadius = (maxY + 1 - minY) / 2;
            var xPush = -((maxX - minX) % 2);
            var yPush = -((maxY - minY) % 2);

            var x = minX + xRadius;
            var y = minY + yRadius;
            var twoASquare = 2 * xRadius * xRadius;
            var twoBSquare = 2 * yRadius * yRadius;

            var data = target.Data;
            var start = target.Start;
            var width = target.Width;
            var height = target.Height;
            var stride = target.Stride;

            fixed (byte* pData = data)
            {
                int yGapStart = 0;
                int yGapEnd = yRadius;
                int xGapStart = 0;
                int xGapEnd = xRadius;
                {
                    // Flat parts
                    int px = 0;
                    int py = yRadius;
                    int xChange = yRadius * yRadius;
                    int yChange = xRadius * xRadius * (1 - 2 * yRadius);
                    int ellipseError = 0;
                    int stoppingX = 0;
                    int stoppingY = twoASquare * yRadius;
                    while (stoppingX <= stoppingY)
                    {
                        // Plot
                        int top = y - py;
                        int bottom = y + py + yPush;
                        int left = x - px;
                        int right = x + px + xPush;
                        if (top >= 0 && top < height)
                        {
                            if (left >= 0 && left < width)
                            {
                                pData[start + left + top * stride] = color;
                                ++m_pixelsDrawn;
                            }
                            if (right >= 0 && right < width)
                            {
                                pData[start + right + top * stride] = color;
                                ++m_pixelsDrawn;
                            }
                        }
                        if (bottom >= 0 && bottom < height)
                        {
                            if (left >= 0 && left < width)
                            {
                                pData[start + left + bottom * stride] = color;
                                ++m_pixelsDrawn;
                            }
                            if (right >= 0 && right < width)
                            {
                                pData[start + right + bottom * stride] = color;
                                ++m_pixelsDrawn;
                            }
                        }

                        // Record
                        yGapEnd = py - 1;
                        xGapStart = px + 1;

                        // Increment
                        px++;
                        stoppingX += twoBSquare;
                        ellipseError += xChange;
                        xChange += twoBSquare;
                        if (2 * ellipseError + yChange > 0)
                        {
                            py--;
                            stoppingY -= twoASquare;
                            ellipseError += yChange;
                            yChange += twoASquare;
                        }
                    }
                }
                {
                    // Steep parts
                    int px = xRadius;
                    int py = 0;
                    int xChange = yRadius * yRadius * (1 - 2 * xRadius);
                    int yChange = xRadius * xRadius;
                    int ellipseError = 0;
                    int stoppingX = twoBSquare * xRadius;
                    int stoppingY = 0;
                    while (stoppingX >= stoppingY)
                    {
                        // Plot
                        int top = y - py;
                        int bottom = y + py + yPush;
                        int left = x - px;
                        int right = x + px + xPush;
                        if (top >= 0 && top < height)
                        {
                            if (left >= 0 && left < width)
                            {
                                pData[start + left + top * stride] = color;
                                ++m_pixelsDrawn;
                            }
                            if (right >= 0 && right < width)
                            {
                                pData[start + right + top * stride] = color;
                                ++m_pixelsDrawn;
                            }
                        }
                        if (bottom >= 0 && bottom < height)
                        {
                            if (left >= 0 && left < width)
                            {
                                pData[start + left + bottom * stride] = color;
                                ++m_pixelsDrawn;
                            }
                            if (right >= 0 && right < width)
                            {
                                pData[start + right + bottom * stride] = color;
                                ++m_pixelsDrawn;
                            }
                        }

                        // Record
                        yGapStart = py + 1;
                        xGapEnd = px - 1;

                        // Increment
                        py++;
                        stoppingY += twoASquare;
                        ellipseError += yChange;
                        yChange += twoASquare;
                        if (2 * ellipseError + xChange > 0)
                        {
                            px--;
                            stoppingX -= twoBSquare;
                            ellipseError += xChange;
                            xChange += twoBSquare;
                        }
                    }
                }
                {
                    // Plug the weird gap the algorithm leaves on very tall or wide ellipses
                    if (yGapEnd >= yGapStart)
                    {
                        m_pixelsDrawn += DrawLineImpl(target, x, y - yGapStart, x, y - yGapEnd, color);
                        m_pixelsDrawn += DrawLineImpl(target, x, y + yGapStart + yPush, x, y + yGapEnd + yPush, color);
                        if (xPush != 0)
                        {
                            m_pixelsDrawn += DrawLineImpl(target, x + xPush, y - yGapStart, x + xPush, y - yGapEnd, color);
                            m_pixelsDrawn += DrawLineImpl(target, x + xPush, y + yGapStart + yPush, x + xPush, y + yGapEnd + yPush, color);
                        }
                    }
                    if (xGapEnd >= xGapStart)
                    {
                        m_pixelsDrawn += DrawLineImpl(target, x - xGapStart, y, x - xGapEnd, y, color);
                        m_pixelsDrawn += DrawLineImpl(target, x + xGapStart + xPush, y, x + xGapEnd + xPush, y, color);
                        if (yPush != 0)
                        {
                            m_pixelsDrawn += DrawLineImpl(target, x - xGapStart, y + yPush, x - xGapEnd, y + yPush, color);
                            m_pixelsDrawn += DrawLineImpl(target, x + xGapStart + xPush, y + yPush, x + xGapEnd + xPush, y + yPush, color);
                        }
                    }
                }
            }
            target.Change();
        }

        public void DrawImage(int x, int y, Image image, int scale)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }

            Transform(ref x, ref y);

            int startX = Math.Max(x, 0);
            int startY = Math.Max(y, 0);
            int endX = Math.Min(x + scale * image.Width, target.Width);
            int endY = Math.Min(y + scale * image.Height, target.Height);

            var data = target.Data;
            var start = target.Start;
            var stride = target.Stride;

            var srcData = image.Data;
            var srcStart = image.Start;
            var srcStride = image.Stride;

            byte? transparentColour = TransparentColor;
            if (endY > startY && endX > startX)
            {
                if (scale != 1)
                {
                    fixed (byte* pData = data)
                    {
                        fixed (byte* pSrcData = srcData)
                        {
                            for (int py = startY; py < endY; ++py)
                            {
                                int oy = (py - y) / scale;
                                for (int px = startX; px < endX; ++px)
                                {
                                    int ox = (px - x) / scale;
                                    var c = Remap(pSrcData[srcStart + ox + oy * srcStride]);
                                    if (!transparentColour.HasValue || c != transparentColour.Value)
                                    {
                                        pData[start + px + py * stride] = c;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    fixed (byte* pData = data)
                    {
                        fixed (byte* pSrcData = srcData)
                        {
                            for (int py = startY; py < endY; ++py)
                            {
                                int oy = py - y;
                                for (int px = startX; px < endX; ++px)
                                {
                                    int ox = px - x;
                                    var c = Remap(pSrcData[srcStart + ox + oy * srcStride]);
                                    if (!transparentColour.HasValue || c != transparentColour.Value)
                                    {
                                        pData[start + px + py * stride] = c;
                                    }
                                }
                            }
                        }
                    }
                }
                m_pixelsDrawn += (endX - startX) * (endY - startY);
                target.Change();
            }
        }

        public void XorImage(int x, int y, Image image, int scale)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }

            Transform(ref x, ref y);

            int startX = Math.Max(x, 0);
            int startY = Math.Max(y, 0);
            int endX = Math.Min(x + scale * image.Width, target.Width);
            int endY = Math.Min(y + scale * image.Height, target.Height);

            var data = target.Data;
            var start = target.Start;
            var stride = target.Stride;

            var srcData = image.Data;
            var srcStart = image.Start;
            var srcStride = image.Stride;

            byte? transparentColour = TransparentColor;
            if (endY > startY && endX > startX)
            {
                if (scale != 1)
                {
                    fixed (byte* pData = data)
                    {
                        fixed (byte* pSrcData = srcData)
                        {
                            for (int py = startY; py < endY; ++py)
                            {
                                int oy = (py - y) / scale;
                                for (int px = startX; px < endX; ++px)
                                {
                                    int ox = (px - x) / scale;
                                    var a = Remap(pSrcData[srcStart + ox + oy * srcStride]);
                                    if (!transparentColour.HasValue || a != transparentColour.Value)
                                    {
                                        var b = pData[start + px + py * stride];
                                        var result = (byte)(a ^ b);
                                        pData[start + px + py * stride] = result;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    fixed (byte* pData = data)
                    {
                        fixed (byte* pSrcData = srcData)
                        {
                            for (int py = startY; py < endY; ++py)
                            {
                                int oy = py - y;
                                for (int px = startX; px < endX; ++px)
                                {
                                    int ox = px - x;
                                    var a = Remap(pSrcData[srcStart + ox + oy * srcStride]);
                                    if (!transparentColour.HasValue || a != transparentColour.Value)
                                    {
                                        var b = pData[start + px + py * stride];
                                        var result = (byte)(a ^ b);
                                        pData[start + px + py * stride] = result;
                                    }
                                }
                            }
                        }
                    }
                }
                m_pixelsDrawn += (endX - startX) * (endY - startY);
                target.Change();
            }
        }

        public void MeasureText(string text, out int o_width, out int o_height)
        {
            var font = m_font;
            if (font == null)
            {
                o_width = 0;
                o_height = 0;
            }
            else
            {
                font.MeasureText(text, out o_width, out o_height);
            }
        }

        public void DrawText(int x, int y, string text)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }

            var font = m_font;
            if (font == null)
            {
                return;
            }

            Transform(ref x, ref y);
            var data = target.Data;
            var width = target.Width;
            var height = target.Height;
            var start = target.Start;
            var stride = target.Stride;

            var fontImage = font.Image;
            var fontData = fontImage.Data;
            var fontStart = fontImage.Start;
            var fontStride = fontImage.Stride;
            var charHeight = font.CharacterHeight;

            int startY = Math.Max(y, 0);
            int endY = Math.Min(y + charHeight, height);
            if (endY > startY)
            {
                byte? transparentColour = TransparentColor;
                fixed (byte* pData = data)
                {
                    fixed (byte* pFontData = fontData)
                    {
                        for (int n = 0; n < text.Length; ++n)
                        {
                            if (!char.IsLowSurrogate(text, n))
                            {
                                // Decode char
                                var codepoint = char.ConvertToUtf32(text, n);
                                int charX, charY, charWidth;
                                font.GetCharacterPosition(codepoint, out charX, out charY, out charWidth);

                                // Draw char
                                int startX = Math.Max(x, 0);
                                int endX = Math.Min(x + charWidth, width);
                                if (endX > startX)
                                {
                                    for (int py = startY; py < endY; ++py)
                                    {
                                        int oy = charY + (py - y);
                                        for (int px = startX; px < endX; ++px)
                                        {
                                            int ox = charX + (px - x);
                                            var c = Remap(pFontData[fontStart + ox + oy * fontStride]);
                                            if (!transparentColour.HasValue || c != transparentColour.Value)
                                            {
                                                pData[start + px + py * stride] = c;
                                            }
                                        }
                                    }
                                    m_pixelsDrawn += (endY - startY) * (endX - startX);
                                }

                                // Advance
                                x += charWidth;
                                if (x >= width)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                target.Change();
            }
        }

        public void DrawMap(int x, int y, Image map, Image tileset, int scale)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }

            Transform(ref x, ref y);
            var data = target.Data;
            var width = target.Width;
            var height = target.Height;
            var start = target.Start;
            var stride = target.Stride;

            var tilesetData = tileset.Data;
            var tilesetWidth = tileset.Width;
            var tilesetHeight = tileset.Height;
            var tilesetStart = tileset.Start;
            var tilesetStride = tileset.Stride;

            var charWidth = tilesetWidth / 16;
            var charHeight = tilesetHeight / 16;
            if (charWidth == 0 || charHeight == 0)
            {
                return;
            }

            var mapData = map.Data;
            var mapWidth = map.Width;
            var mapHeight = map.Height;
            var mapStart = map.Start;
            var mapStride = map.Stride;

            int mapStartX = Math.Max((0 - x) / (charWidth * scale), 0);
            int mapEndX = Math.Min(((width - 1) - x) / (charWidth * scale), mapWidth - 1);
            int mapStartY = Math.Max((0 - y) / (charHeight * scale), 0);
            int mapEndY = Math.Min(((height - 1) - y) / (charHeight * scale), mapHeight - 1);

            if (mapEndX >= mapStartX && mapEndY >= mapStartY)
            {
                byte? transparentColour = TransparentColor;
                fixed (byte* pData = data)
                {
                    fixed (byte* pMapData = mapData)
                    {
                        fixed (byte* pFontData = tilesetData)
                        {
                            for (int mapY = mapStartY; mapY <= mapEndY; ++mapY)
                            {
                                int originY = y + mapY * charHeight * scale;
                                int startY = Math.Max(originY, 0);
                                int endY = Math.Min(originY + charHeight * scale, height);
                                for (int mapX = mapStartX; mapX <= mapEndX; ++mapX)
                                {
                                    int charIndex = pMapData[mapStart + mapX + mapY * mapStride];
                                    int charX = (charIndex % 16) * charWidth;
                                    int charY = (charIndex / 16) * charHeight;

                                    int originX = x + mapX * charWidth * scale;
                                    int startX = Math.Max(originX, 0);
                                    int endX = Math.Min(originX + charWidth * scale, width);
                                    if (scale != 1)
                                    {
                                        for (int py = startY; py < endY; ++py)
                                        {
                                            int oy = charY + (py - originY) / scale;
                                            for (int px = startX; px < endX; ++px)
                                            {
                                                int ox = charX + (px - originX) / scale;
                                                var c = Remap(pFontData[tilesetStart + ox + oy * tilesetStride]);
                                                if (!transparentColour.HasValue || c != transparentColour.Value)
                                                {
                                                    pData[start + px + py * stride] = c;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int py = startY; py < endY; ++py)
                                        {
                                            int oy = charY + (py - originY);
                                            for (int px = startX; px < endX; ++px)
                                            {
                                                int ox = charX + (px - originX);
                                                var c = Remap(pFontData[tilesetStart + ox + oy * tilesetStride]);
                                                if (!transparentColour.HasValue || c != transparentColour.Value)
                                                {
                                                    pData[start + px + py * stride] = c;
                                                }
                                            }
                                        }
                                    }
                                    m_pixelsDrawn += (endY - startY) * (endX - startX);
                                }
                            }
                        }
                    }
                }
                target.Change();
            }
        }

        public void FloodFill(int x, int y, byte color)
        {
            var target = m_target;
            if (target == null)
            {
                return;
            }

            Transform(ref x, ref y);
            color = Remap(color);
            if (TransparentColor.HasValue && color == TransparentColor.Value)
            {
                return;
            }

            if (x >= 0 && x < target.Width && y >= 0 && y < target.Height)
            {
                var oldC = target[x, y];
                if (color != oldC)
                {
                    int start = target.Start;
                    int width = target.Width;
                    int height = target.Height;
                    int stride = target.Stride;
                    fixed (byte* pData = target.Data)
                    {
                        m_pixelsDrawn += FloodFillImpl(pData, start, width, height, stride, ref x, y, oldC, color);
                    }
                    target.Change();
                }
            }
        }

        private static int FloodFillImpl(byte* pData, int start, int width, int height, int stride, ref int x, int y, byte find, byte replace)
        {
            // Find the start and end of the scanline
            int startX = x;
            while (startX > 0 && pData[start + (startX - 1) + y * stride] == find)
            {
                --startX;
            }
            int endX = x + 1;
            while (endX < width && pData[start + endX + y * stride] == find)
            {
                ++endX;
            }

            // Fill in the scanline
            int pixelsDrawn = endX - startX;
            for (int px = startX; px < endX; ++px)
            {
                pData[start + px + y * stride] = replace;
            }

            if (y > 0)
            {
                // Recurse to row above
                int py = y - 1;
                for (int px = startX; px < endX; ++px)
                {
                    if (pData[start + px + py * stride] == find)
                    {
                        pixelsDrawn += FloodFillImpl(pData, start, width, height, stride, ref px, py, find, replace);
                    }
                }
            }
            if (y < height - 1)
            {
                // Recuse to row below
                int py = y + 1;
                for (int px = startX; px < endX; ++px)
                {
                    if (pData[start + px + py * stride] == find)
                    {
                        pixelsDrawn += FloodFillImpl(pData, start, width, height, stride, ref px, py, find, replace);
                    }
                }
            }

            x = endX;
            return pixelsDrawn;
        }

        private void Transform(ref int x, ref int y)
        {
            x += OffsetX;
            y += OffsetY;
        }

        private byte Remap(byte color)
        {
            if (color < m_colorMapping.Length)
            {
                return m_colorMapping[color];
            }
            return color;
        }
    }
}

