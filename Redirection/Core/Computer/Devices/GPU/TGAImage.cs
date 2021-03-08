using Dan200.Core.Lua;
using System;
using System.IO;

namespace Dan200.Core.Computer.Devices.GPU
{
    public static class TGAImage
    {
        public static Image Decode(Stream stream, MemoryTracker memory, out Palette o_palette)
        {
            var reader = new BinaryReader(stream);

            // Read the header
            int imageIDLength = reader.ReadByte();
            int colorMapType = reader.ReadByte();
            int imageType = reader.ReadByte();
            int colorMapStart = reader.ReadUInt16();
            int colorMapLength = reader.ReadUInt16();
            int colorMapBitsPerPixel = reader.ReadByte();
            reader.ReadUInt16(); // xOffset
            reader.ReadUInt16(); // yOffset
            int width = reader.ReadUInt16();
            int height = reader.ReadUInt16();
            int bitsPerPixel = reader.ReadByte();
            int imageDescriptor = reader.ReadByte();

            if (bitsPerPixel != 8 ||
                width == 0 || height == 0 ||
                colorMapType != 1 ||
                (colorMapStart + colorMapLength) == 0 ||
                (colorMapStart + colorMapLength) > 256 ||
                (colorMapBitsPerPixel != 24 && colorMapBitsPerPixel != 32) ||
                (imageType != 1 && imageType != 9))
            {
                throw new IOException("Unsupported TGA file");
            }

            // Read the ID
            if (imageIDLength > 0)
            {
                reader.ReadBytes(imageIDLength);
            }

            // Read the color map
            int colorMapBytesPerPixel = colorMapBitsPerPixel / 8;
            var colorMap = reader.ReadBytes(colorMapLength * colorMapBytesPerPixel);
            var colors = new uint[colorMapStart + colorMapLength];
            for (int i = 0; i < colors.Length; ++i)
            {
                if (i < colorMapStart)
                {
                    colors[i] = 0x000000ff;
                }
                else
                {
                    uint b = colorMap[(i - colorMapStart) * colorMapBytesPerPixel];
                    uint g = colorMap[(i - colorMapStart) * colorMapBytesPerPixel + 1];
                    uint r = colorMap[(i - colorMapStart) * colorMapBytesPerPixel + 2];
                    colors[i] = (r << 24) + (g << 16) + (b << 8) + 0xff;
                }
            }
            var palette = new Palette(colors);

            // Decode the image
            long size = width * height;
            if (palette != null)
            {
                size += 3 * palette.Size;
            }
            if (!memory.Alloc(size))
            {
                throw new OutOfMemoryException();
            }
            try
            {
                // Read the pixels
                byte[] buffer;
                bool rle = (imageType >= 8);
                if (rle)
                {
                    // RLE
                    buffer = new byte[width * height];
                    int pos = 0;
                    while (pos < buffer.Length)
                    {
                        byte b = reader.ReadByte();
                        if (((int)b & 0x80) == 0x80)
                        {
                            // Run-length packet
                            int count = ((int)b & 0x7f) + 1;
                            byte value = reader.ReadByte();
                            int limit = Math.Min(pos + count, buffer.Length);
                            while (pos < limit)
                            {
                                buffer[pos++] = value;
                            }
                        }
                        else
                        {
                            // Non-run-length packet
                            int count = ((int)b & 0x7f) + 1;
                            int limit = Math.Min(pos + count, buffer.Length);
                            while (pos < limit)
                            {
                                buffer[pos++] = reader.ReadByte();
                            }
                        }
                    }
                }
                else
                {
                    // Non RLE
                    buffer = reader.ReadBytes(width * height);
                }

                // Create the image
                bool flipY = (imageDescriptor & 0x20) == 0;
                var image = new Image(width, height);
                if (flipY)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        int flippedY = height - 1 - y;
                        image.Write(buffer, y * width, width, 0, flippedY);
                    }
                }
                else
                {
                    image.Write(buffer, 0, width * height, 0, 0);
                }
                o_palette = palette;
                return image;
            }
            catch
            {
                memory.Free(size);
                throw;
            }
        }

        public static void Encode(Image image, Palette palette, Stream stream)
        {
            var writer = new BinaryWriter(stream);

            // Write header
            writer.Write((byte)0); // imageIDLength
            writer.Write((byte)1); // colorMapType
            writer.Write((byte)9); // imageType
            writer.Write((ushort)0); // colorMapStart
            writer.Write((ushort)palette.Size); // colorMapLength
            writer.Write((byte)24); // colorMapBitsPerPixel
            writer.Write((ushort)0); // xOffset
            writer.Write((ushort)0); // yOffset
            writer.Write((ushort)image.Width); // width
            writer.Write((ushort)image.Height); // height
            writer.Write((byte)8); // bitsPerPixel
            writer.Write((byte)0); // imageDescriptor

            // Write color map
            for (int i = 0; i < palette.Size; ++i)
            {
                var c = palette[i];
                var r = (byte)((c & 0xff000000) >> 24);
                var g = (byte)((c & 0x00ff0000) >> 16);
                var b = (byte)((c & 0x0000ff00) >> 8);
                writer.Write(b);
                writer.Write(g);
                writer.Write(r);
            }

            // Write image data, bottom to top
            for (int y = image.Height - 1; y >= 0; --y)
            {
                byte[] line = image.Read(0, y, image.Width);
                int runStart = 0;
                byte lastPixel = 0;
                int runType = -1;
                for (int x = 0; x < line.Length; ++x)
                {
                    byte pixel = line[x];
                    if (x == runStart)
                    {
                        // Start a new run
                        runType = -1;
                    }
                    else if ((x - runStart) == 128)
                    {
                        // Finish a maximum length run
                        int count = x - runStart;
                        if (runType == 1)
                        {
                            // Complete rle run
                            writer.Write((byte)((count - 1) + 0x80));
                            writer.Write(lastPixel);
                        }
                        else
                        {
                            // Complete non-rle run
                            writer.Write((byte)(count - 1));
                            writer.Write(line, runStart, count);
                        }

                        // Start a new run
                        runStart = x;
                        runType = -1;
                    }
                    else if (lastPixel == pixel)
                    {
                        if (runType == -1)
                        {
                            // Set run type to rle
                            runType = 1;
                        }
                        else if (runType == 0)
                        {
                            // Complete non-rle run
                            int count = x - runStart;
                            writer.Write((byte)(count - 1));
                            writer.Write(line, runStart, count);

                            // Start a new run
                            runStart = x;
                            runType = -1;
                        }
                    }
                    else
                    {
                        if (runType == -1)
                        {
                            // Set run type to non-rle
                            runType = 0;
                        }
                        else if (runType == 1)
                        {
                            // Complete rle run
                            int count = x - runStart;
                            writer.Write((byte)((count - 1) + 0x80));
                            writer.Write(lastPixel);

                            // Start a new run
                            runStart = x;
                            runType = -1;
                        }
                    }
                    lastPixel = pixel;
                }

                // Complete current run
                if (runStart < line.Length)
                {
                    int count = line.Length - runStart;
                    if (runType == 1)
                    {
                        // Complete rle run
                        writer.Write((byte)((count - 1) + 0x80));
                        writer.Write(lastPixel);
                    }
                    else
                    {
                        // Complete non-rle run
                        writer.Write((byte)(count - 1));
                        writer.Write(line, runStart, count);
                    }
                }
            }
        }
    }
}
