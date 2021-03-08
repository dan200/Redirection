using Dan200.Core.Lua;

namespace Dan200.Core.Computer.Devices.GPU
{
    [LuaType("image")]
    public class LuaImage : LuaObject
    {
        public readonly Image Image;
        private MemoryTracker m_memory;
        private LuaObjectRef<LuaImage> m_parent;
        public readonly bool ReadOnly;

        public LuaImage(Image image, MemoryTracker memory, bool readOnly = false)
        {
            Image = image;
            m_memory = memory;
            ReadOnly = readOnly;
        }

        public LuaImage(Image image, LuaImage parent)
        {
            Image = image;
            m_parent = new LuaObjectRef<LuaImage>(parent);
            ReadOnly = parent.ReadOnly;
        }

        public override void Dispose()
        {
            if (m_memory != null)
            {
                m_memory.Free(Image.Width * Image.Height);
            }
            else
            {
                m_parent.Dispose();
            }
        }

        [LuaMethod]
        public LuaArgs getSize(LuaArgs args)
        {
            return new LuaArgs(Image.Width, Image.Height);
        }

        [LuaMethod]
        public LuaArgs getWidth(LuaArgs args)
        {
            return new LuaArgs(Image.Width);
        }

        [LuaMethod]
        public LuaArgs getHeight(LuaArgs args)
        {
            return new LuaArgs(Image.Height);
        }

        [LuaMethod]
        public LuaArgs isReadOnly(LuaArgs args)
        {
            return new LuaArgs(ReadOnly);
        }

        [LuaMethod]
        public LuaArgs read(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            if (args.IsNil(2))
            {
                if (x >= 0 && x < Image.Width &&
                    y >= 0 && y < Image.Height)
                {
                    var b = Image[x, y];
                    return new LuaArgs(b);
                }
                return new LuaArgs(0);
            }
            else
            {
                int length = args.GetInt(2);
                if (length < 0)
                {
                    throw new LuaError("Read length must be positive");
                }
                if (x < 0 || x >= Image.Width ||
                    y < 0 || y >= Image.Height ||
                    (x + y * Image.Width + length) > Image.Width * Image.Height)
                {
                    throw new LuaError("Read must start and end within the image bounds");
                }
                var result = Image.Read(x, y, length);
                return new LuaArgs(result);
            }
        }

        [LuaMethod]
        public LuaArgs write(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            if (args.IsNumber(2))
            {
                var n = args.GetByte(2);
                CheckWritable();
                if (x >= 0 && x < Image.Width &&
                    y >= 0 && y < Image.Height)
                {
                    Image[x, y] = n;
                }
                return LuaArgs.Empty;
            }
            else
            {
                var bytes = args.GetByteString(2);
                if (x < 0 || x >= Image.Width ||
                    y < 0 || y >= Image.Height ||
                    (x + y * Image.Width + bytes.Length) > Image.Width * Image.Height)
                {
                    throw new LuaError("Write must start and end within the image bounds");
                }
                CheckWritable();
                Image.Write(bytes, 0, bytes.Length, x, y);
                return LuaArgs.Empty;
            }
        }

        [LuaMethod]
        public LuaArgs blit(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            var image = args.GetObject<LuaImage>(2).Image;
            CheckWritable();
            Image.Blit(image, x, y);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs fill(LuaArgs args)
        {
            var color = args.GetByte(0);
            if (args.IsNil(1))
            {
                CheckWritable();
                Image.Fill(color);
            }
            else
            {
                int x = args.GetInt(1);
                int y = args.GetInt(2);
                int w = args.GetInt(3);
                int h = args.GetInt(4);
                if (w < 0)
                {
                    throw new LuaError("Fill width must be positive");
                }
                if (h < 0)
                {
                    throw new LuaError("Fill height must be positive");
                }
                if (x < 0 || x + w > Image.Width ||
                    y < 0 || y + h > Image.Height)
                {
                    throw new LuaError("Fill must start and end within the image bounds");
                }
                CheckWritable();
                Image.Fill(color, x, y, w, h);
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs flipX(LuaArgs args)
        {
            CheckWritable();
            Image.FlipX();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs flipY(LuaArgs args)
        {
            CheckWritable();
            Image.FlipY();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs translate(LuaArgs args)
        {
            var x = args.GetInt(0);
            var y = args.GetInt(1);
            var fillColor = args.IsNil(2) ? (byte)0 : args.GetByte(2);
            CheckWritable();
            Image.Translate(x, y, fillColor);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs rotate90(LuaArgs args)
        {
            if (Image.Width != Image.Height)
            {
                throw new LuaError("Only square images can be rotated 90 degrees");
            }
            CheckWritable();
            Image.Rotate90();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs rotate180(LuaArgs args)
        {
            CheckWritable();
            Image.Rotate180();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs rotate270(LuaArgs args)
        {
            if (Image.Width != Image.Height)
            {
                throw new LuaError("Only square images can be rotated 270 degrees");
            }
            CheckWritable();
            Image.Rotate270();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs replace(LuaArgs args)
        {
            var inColor = args.GetByte(0);
            var outColor = args.GetByte(1);
            CheckWritable();
            Image.Replace(inColor, outColor);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs sub(LuaArgs args)
        {
            int x = args.GetInt(0);
            int y = args.GetInt(1);
            int width = args.GetInt(2);
            int height = args.GetInt(3);
            if (width <= 0)
            {
                throw new LuaError("Subimage width must be positive");
            }
            if (height <= 0)
            {
                throw new LuaError("Subimage height must be positive");
            }
            if (x < 0 || (x + width) > Image.Width || y < 0 || (y + height) > Image.Height)
            {
                throw new LuaError("Subimage bounds must lie within image");
            }
            var result = Image.Sub(x, y, width, height);
            return new LuaArgs(new LuaImage(result, this));
        }

        [LuaMethod]
        public LuaArgs copy(LuaArgs args)
        {
            if (m_memory.Alloc(Image.Width * Image.Height))
            {
                var result = Image.Copy();
                return new LuaArgs(new LuaImage(result, m_memory));
            }
            else
            {
                throw new LuaError("not enough memory");
            }
        }

        private void CheckWritable()
        {
            if (ReadOnly)
            {
                throw new LuaError("Image is readonly");
            }
        }
    }
}
