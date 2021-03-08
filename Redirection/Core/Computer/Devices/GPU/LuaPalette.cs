using Dan200.Core.Lua;
using System;

namespace Dan200.Core.Computer.Devices.GPU
{
    [LuaType("palette")]
    public class LuaPalette : LuaObject
    {
        public readonly Palette Palette;
        private MemoryTracker m_memory;
        public readonly bool ReadOnly;

        public LuaPalette(Palette palette, MemoryTracker memory, bool readOnly = false)
        {
            Palette = palette;
            m_memory = memory;
            ReadOnly = readOnly;
        }

        public override void Dispose()
        {
            m_memory.Free(Palette.Size * 3);
        }

        [LuaMethod]
        public LuaArgs getSize(LuaArgs args)
        {
            return new LuaArgs(Palette.Size);
        }

        [LuaMethod]
        public LuaArgs isReadOnly(LuaArgs args)
        {
            return new LuaArgs(ReadOnly);
        }

        [LuaMethod]
        public LuaArgs getColor(LuaArgs args)
        {
            int n = args.GetInt(0);
            if (n >= 0 && n < Palette.Size)
            {
                uint color = Palette[n];
                float r = (float)((color >> 24) & 0xff) / 255.0f;
                float g = (float)((color >> 16) & 0xff) / 255.0f;
                float b = (float)((color >> 8) & 0xff) / 255.0f;
                return new LuaArgs(r, g, b);
            }
            else
            {
                return LuaArgs.Nil;
            }
        }

        [LuaMethod]
        public LuaArgs setColor(LuaArgs args)
        {
            int n = args.GetInt(0);
            float r = args.GetFloat(1);
            float g = args.GetFloat(2);
            float b = args.GetFloat(3);
            CheckWritable();
            uint rb = (uint)(ClampColorComponent(r) * 255.0f) & 0xff;
            uint gb = (uint)(ClampColorComponent(g) * 255.0f) & 0xff;
            uint bb = (uint)(ClampColorComponent(b) * 255.0f) & 0xff;
            uint color = (rb << 24) + (gb << 16) + (bb << 8) + 255;
            if (n >= 0 && n < Palette.Size)
            {
                Palette[n] = color;
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs copy(LuaArgs args)
        {
            if (m_memory.Alloc(Palette.Size * 3))
            {
                var result = Palette.Copy();
                return new LuaArgs(new LuaPalette(result, m_memory));
            }
            else
            {
                throw new LuaError("not enough memory");
            }
        }

        private float ClampColorComponent(float f)
        {
            return Math.Min(Math.Max(f, 0.0f), 1.0f);
        }

        private void CheckWritable()
        {
            if (ReadOnly)
            {
                throw new LuaError("Palette is readonly");
            }
        }
    }
}
