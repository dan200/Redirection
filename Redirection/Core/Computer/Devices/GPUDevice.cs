using Dan200.Core.Computer.APIs;
using Dan200.Core.Computer.Devices.GPU;
using Dan200.Core.Lua;
using System;
using System.IO;

namespace Dan200.Core.Computer.Devices
{
    public class GPUDevice : Device
    {
        private string m_description;
        private Computer m_computer;
        private Graphics m_graphics;
        private LuaObjectRef<LuaImage> m_target;
        private LuaObjectRef<LuaFont> m_font;

        public override string Type
        {
            get
            {
                return "gpu";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public GPUDevice(string description)
        {
            m_description = description;
            m_graphics = new Graphics();
            m_target = new LuaObjectRef<LuaImage>();
            m_font = new LuaObjectRef<LuaFont>();
        }

        public override void Attach(Computer computer)
        {
            m_computer = computer;
        }

        public override void Detach()
        {
            m_computer = null;
            m_graphics.Reset();
            m_target.Value = null;
            m_font.Value = null;
        }

        private string GetFileModeString(LuaFileOpenMode mode, LuaFileContentType type)
        {
            if (mode == LuaFileOpenMode.Read && type == LuaFileContentType.Text)
            {
                return "r";
            }
            else if (mode == LuaFileOpenMode.Read && type == LuaFileContentType.Binary)
            {
                return "rb";
            }
            else if (mode == LuaFileOpenMode.Write && type == LuaFileContentType.Text)
            {
                return "w";
            }
            else
            {
                return "wb";
            }
        }

        private void CheckFileMode(LuaFile file, string format, LuaFileOpenMode expectedMode, LuaFileContentType expectedType)
        {
            if (file.ContentType != expectedType || file.OpenMode != expectedMode)
            {
                throw new LuaError(string.Format(
                    "Incorrect file mode for format {0}. Expected {1}, got {2}",
                    format,
                    GetFileModeString(expectedMode, expectedType),
                    GetFileModeString(file.OpenMode, file.ContentType)
                ));
            }
            if (!file.IsOpen)
            {
                throw new LuaError("File is closed");
            }
        }

        [LuaMethod]
        public LuaArgs newImage(LuaArgs args)
        {
            var width = args.GetInt(0);
            var height = args.GetInt(1);
            var color = args.IsNil(2) ? (byte)0 : args.GetByte(2);
            if (width <= 0)
            {
                throw new LuaError("Image width must be positive");
            }
            if (height <= 0)
            {
                throw new LuaError("Image height must be positive");
            }
            if (m_computer.Memory.Alloc(width * height))
            {
                var image = new Image(width, height, color);
                return new LuaArgs(new LuaImage(image, m_computer.Memory));
            }
            else
            {
                throw new LuaError("not enough memory");
            }
        }

        [LuaMethod]
        public LuaArgs loadTGA(LuaArgs args)
        {
            try
            {
                // Load image
                Image image;
                Palette palette;
                if (args.IsString(0))
                {
                    // Load from a string
                    var bytes = args.GetByteString(0);
                    using (var stream = new MemoryStream(bytes))
                    {
                        image = TGAImage.Decode(stream, m_computer.Memory, out palette);
                    }
                }
                else
                {
                    // Load from a stream
                    var file = args.GetObject<LuaFile>(0);
                    CheckFileMode(file, "tga", LuaFileOpenMode.Read, LuaFileContentType.Binary);
                    image = TGAImage.Decode(file.InnerStream, m_computer.Memory, out palette);
                }

                // Return image
                return new LuaArgs(
                    new LuaImage(image, m_computer.Memory),
                    new LuaPalette(palette, m_computer.Memory)
                );
            }
            catch (OutOfMemoryException)
            {
                throw new LuaError("not enough memory");
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs saveTGA(LuaArgs args)
        {
            var image = args.GetObject<LuaImage>(0).Image;
            var palette = args.GetObject<LuaPalette>(1).Palette;
            try
            {
                if (args.IsNil(2))
                {
                    // Save to a string and return it
                    using (var stream = new MemoryStream())
                    {
                        TGAImage.Encode(image, palette, stream);
                        return new LuaArgs(stream.ToArray());
                    }
                }
                else
                {
                    // Save to a stream
                    var file = args.GetObject<LuaFile>(2);
                    CheckFileMode(file, "tga", LuaFileOpenMode.Write, LuaFileContentType.Binary);
                    TGAImage.Encode(image, palette, file.InnerStream);
                    return LuaArgs.Empty;
                }
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs newPalette(LuaArgs args)
        {
            var size = args.IsNil(0) ? 16 : args.GetInt(0);
            if (size < 1 || size > 256)
            {
                throw new LuaError("Palette size must be in range 1-256");
            }
            if (m_computer.Memory.Alloc(size * 3))
            {
                // Create a simple greyscale palette
                uint usize = (uint)size;
                uint[] colors = new uint[usize];
                colors[0] = 0x000000ff;
                for (uint i = 1; i < colors.Length; ++i)
                {
                    uint bright = ((i * 255) / (usize - 1)) & 0xff;
                    colors[i] = (bright << 24) + (bright << 16) + (bright << 8) + 255;
                }
                var palette = new Palette(colors);
                return new LuaArgs(new LuaPalette(palette, m_computer.Memory));
            }
            else
            {
                throw new LuaError("not enough memory");
            }
        }

        [LuaMethod]
        public LuaArgs getTarget(LuaArgs args)
        {
            return new LuaArgs(m_target.Value);
        }

        [LuaMethod]
        public LuaArgs setTarget(LuaArgs args)
        {
            var target = args.IsNil(0) ? null : args.GetObject<LuaImage>(0);
            if (target != null)
            {
                if (target.ReadOnly)
                {
                    throw new LuaError("Cannot target a readonly image");
                }
                m_graphics.Target = target.Image;
                m_target.Value = target;
            }
            else
            {
                m_graphics.Target = null;
                m_target.Value = null;
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getTargetSize(LuaArgs args)
        {
            var target = m_target.Value;
            if (target != null)
            {
                return new LuaArgs(target.Image.Width, target.Image.Height);
            }
            else
            {
                return new LuaArgs(0, 0);
            }
        }

        [LuaMethod]
        public LuaArgs getTargetWidth(LuaArgs args)
        {
            var target = m_graphics.Target;
            if (target != null)
            {
                return new LuaArgs(target.Width);
            }
            else
            {
                return new LuaArgs(0);
            }
        }

        [LuaMethod]
        public LuaArgs getTargetHeight(LuaArgs args)
        {
            var target = m_graphics.Target;
            if (target != null)
            {
                return new LuaArgs(target.Height);
            }
            else
            {
                return new LuaArgs(0);
            }
        }

        [LuaMethod]
        public LuaArgs getOffset(LuaArgs args)
        {
            return new LuaArgs(m_graphics.OffsetX, m_graphics.OffsetY);
        }

        [LuaMethod]
        public LuaArgs setOffset(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            m_graphics.OffsetX = x;
            m_graphics.OffsetY = y;
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getTransparentColor(LuaArgs args)
        {
            if (m_graphics.TransparentColor.HasValue)
            {
                return new LuaArgs(m_graphics.TransparentColor.Value);
            }
            else
            {
				return LuaArgs.Nil;
            }
        }

        [LuaMethod]
        public LuaArgs setTransparentColor(LuaArgs args)
        {
            byte? color = null;
            if (!args.IsNil(0))
            {
                color = args.GetByte(0);
            }
            m_graphics.TransparentColor = color;
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getColorMapping(LuaArgs args)
        {
            // Return a single mapping
            var inColor = args.GetByte(0);
            var outColor = m_graphics.GetColorMapping(inColor);
            return new LuaArgs(outColor);
        }

        [LuaMethod]
        public LuaArgs mapColor(LuaArgs args)
        {
            // Set a single color mapping
            var inColor = args.GetByte(0);
            var outColor = args.GetByte(1);
            m_graphics.SetColorMapping(inColor, outColor);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs clear(LuaArgs args)
        {
            var color = args.IsNil(0) ? (byte)0 : args.GetByte(0);
            m_graphics.Clear(color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getPixel(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            var color = m_graphics.GetPixel(x, y);
            return new LuaArgs(color);
        }

        [LuaMethod]
        public LuaArgs drawPixel(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            var color = args.GetByte(2);
            m_graphics.DrawPixel(x, y, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawLine(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            int endX = args.GetInt(2);
            int endY = args.GetInt(3);
            var color = args.GetByte(4);
            m_graphics.DrawLine(startX, startY, endX, endY, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawTriangle(LuaArgs args)
        {
            int aX = args.GetInt(0);
            int aY = args.GetInt(1);
            int bX = args.GetInt(2);
            int bY = args.GetInt(3);
            int cX = args.GetInt(4);
            int cY = args.GetInt(5);
            var color = args.GetByte(6);
            m_graphics.DrawTriangle(aX, aY, bX, bY, cX, cY, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawTriangleOutline(LuaArgs args)
        {
            int aX = args.GetInt(0);
            int aY = args.GetInt(1);
            int bX = args.GetInt(2);
            int bY = args.GetInt(3);
            int cX = args.GetInt(4);
            int cY = args.GetInt(5);
            var color = args.GetByte(6);
            m_graphics.DrawTriangleOutline(aX, aY, bX, bY, cX, cY, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawBox(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            int width = args.GetInt(2);
            if (width < 0)
            {
                throw new LuaError("Box width must be positive");
            }
            int height = args.GetInt(3);
            if (height < 0)
            {
                throw new LuaError("Box height must be positive");
            }
            var color = args.GetByte(4);
            m_graphics.DrawBox(startX, startY, width, height, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawBoxOutline(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            int width = args.GetInt(2);
            if (width < 0)
            {
                throw new LuaError("Box width must be positive");
            }
            int height = args.GetInt(3);
            if (height < 0)
            {
                throw new LuaError("Box height must be positive");
            }
            var color = args.GetByte(4);
            m_graphics.DrawBoxOutline(startX, startY, width, height, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawEllipse(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            int width = args.GetInt(2);
            if (width < 0)
            {
                throw new LuaError("Ellipse width must be positive");
            }
            int height = args.GetInt(3);
            if (height < 0)
            {
                throw new LuaError("Ellipse height must be positive");
            }
            var color = args.GetByte(4);
            m_graphics.DrawEllipse(startX, startY, width, height, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawEllipseOutline(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            int width = args.GetInt(2);
            if (width < 0)
            {
                throw new LuaError("Ellipse width must be positive");
            }
            int height = args.GetInt(3);
            if (height < 0)
            {
                throw new LuaError("Ellipse height must be positive");
            }
            var color = args.GetByte(4);
            m_graphics.DrawEllipseOutline(startX, startY, width, height, color);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawImage(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            var image = args.GetObject<LuaImage>(2).Image;
            int scale = args.IsNil(3) ? 1 : args.GetInt(3);
            if (scale <= 0)
            {
                throw new LuaError("Scale must be an integer 1 or greater");
            }
            m_graphics.DrawImage(startX, startY, image, scale);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs xorImage(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            var image = args.GetObject<LuaImage>(2).Image;
            int scale = args.IsNil(3) ? 1 : args.GetInt(3);
            if (scale <= 0)
            {
                throw new LuaError("Scale must be an integer 1 or greater");
            }
            m_graphics.XorImage(startX, startY, image, scale);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs newFont(LuaArgs args)
        {
            var image = args.GetObject<LuaImage>(0);
            var characters = args.GetString(1);
            var characterWidth = args.GetInt(2);
            var characterHeight = args.GetInt(3);
            if (characterWidth <= 0)
            {
                throw new LuaError("Character width must be positive");
            }
            if (characterWidth > image.Image.Width)
            {
                throw new LuaError("Character width must be less than image width");
            }
            if (characterHeight <= 0)
            {
                throw new LuaError("Character height must be positive");
            }
            if (characterHeight > image.Image.Height)
            {
                throw new LuaError("Character width must be less than image height");
            }

            var font = new Font(image.Image, characters, characterWidth, characterHeight, true);
            return new LuaArgs(new LuaFont(font, image));
        }

        [LuaMethod]
        public LuaArgs setFont(LuaArgs args)
        {
            var font = args.IsNil(0) ? null : args.GetObject<LuaFont>(0);
            if (font != null)
            {
                m_graphics.Font = font.Font;
                m_font.Value = font;
            }
            else
            {
                m_graphics.Font = null;
                m_font.Value = null;
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getFont(LuaArgs args)
        {
            return new LuaArgs(m_font.Value);
        }

        [LuaMethod]
        public LuaArgs measureText(LuaArgs args)
        {
            var text = args.GetString(0);
            int width, height;
            m_graphics.MeasureText(text, out width, out height);
            return new LuaArgs(width, height);
        }

        [LuaMethod]
        public LuaArgs drawText(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            var text = args.GetString(2);
            m_graphics.DrawText(startX, startY, text);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs drawMap(LuaArgs args)
        {
            int startX = args.GetInt(0);
            int startY = args.GetInt(1);
            var map = args.GetObject<LuaImage>(2).Image;
            var tileset = args.GetObject<LuaImage>(3).Image;
            int scale = args.IsNil(4) ? 1 : args.GetInt(4);
            if (scale <= 0)
            {
                throw new LuaError("Scale must be an integer 1 or greater");
            }
            m_graphics.DrawMap(startX, startY, map, tileset, scale);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs floodFill(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            var color = args.GetByte(2);
            m_graphics.FloodFill(x, y, color);
            return LuaArgs.Empty;
        }
    }
}
