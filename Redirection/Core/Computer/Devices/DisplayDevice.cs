using Dan200.Core.Computer.Devices.GPU;
using Dan200.Core.Lua;

namespace Dan200.Core.Computer.Devices
{
    public class DisplayDevice : Device
    {
        private string m_description;

        public override string Type
        {
            get
            {
                return "display";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public readonly int Width;
        public readonly int Height;
        public readonly int Colors;

        public Palette Palette
        {
            get
            {
                return m_palette.HasValue ? m_palette.Value.Palette : null;
            }
        }

        public Image Image
        {
            get
            {
                return m_image.HasValue ? m_image.Value.Image : null;
            }
        }

        private LuaObjectRef<LuaImage> m_image;
        private LuaObjectRef<LuaPalette> m_palette;
        private Palette m_fixedPalette;

        public DisplayDevice(string description, int width, int height, int colors)
        {
            m_description = description;
            Width = width;
            Height = height;
            Colors = colors;
            m_image = new LuaObjectRef<LuaImage>();
            m_palette = new LuaObjectRef<LuaPalette>();
        }

        public DisplayDevice(string description, int width, int height, Palette fixedPalette) : this(description, width, height, fixedPalette.Size)
        {
            m_fixedPalette = fixedPalette;
        }

        public override void Attach(Computer computer)
        {
            if (m_fixedPalette != null && computer.Memory.Alloc(m_fixedPalette.Size * 3))
            {
                m_palette.Value = new LuaPalette(m_fixedPalette, computer.Memory, true);
            }
        }

        public override void Detach()
        {
            m_image.Value = null;
            m_palette.Value = null;
        }

        [LuaMethod]
        public LuaArgs getResolution(LuaArgs args)
        {
            return new LuaArgs(Width, Height);
        }

        [LuaMethod]
        public LuaArgs getWidth(LuaArgs args)
        {
            return new LuaArgs(Width);
        }

        [LuaMethod]
        public LuaArgs getHeight(LuaArgs args)
        {
            return new LuaArgs(Height);
        }

        [LuaMethod]
        public LuaArgs getNumColors(LuaArgs args)
        {
            return new LuaArgs(Colors);
        }

        [LuaMethod]
        public LuaArgs hasFixedPalette(LuaArgs args)
        {
            return new LuaArgs(m_fixedPalette != null);
        }

        [LuaMethod]
        public LuaArgs getImage(LuaArgs args)
        {
            return new LuaArgs(m_image.Value);
        }

        [LuaMethod]
        public LuaArgs setImage(LuaArgs args)
        {
            var image = args.IsNil(0) ? null : args.GetObject<LuaImage>(0);
            if (image != null)
            {
                if (image.Image.Width != Width || image.Image.Height != Height)
                {
                    throw new LuaError("Display images must be the same size as the display");
                }
                m_image.Value = image;
            }
            else
            {
                m_image.Value = null;
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getPalette(LuaArgs args)
        {
            return new LuaArgs(m_palette.Value);
        }

        [LuaMethod]
        public LuaArgs setPalette(LuaArgs args)
        {
            var palette = args.IsNil(0) ? null : args.GetObject<LuaPalette>(0);
            if (m_fixedPalette != null)
            {
                throw new LuaError("Cannot change a fixed palette");
            }
            if (palette != null)
            {
                m_palette.Value = palette;
            }
            else
            {
                m_palette.Value = null;
            }
            return LuaArgs.Empty;
        }
    }
}
