using Dan200.Core.Lua;

namespace Dan200.Core.Computer.Devices.CPU
{
    [LuaType("buffer")]
    public class LuaBuffer : LuaObject
    {
        public readonly Buffer Buffer;
        private MemoryTracker m_memory;
        private LuaObjectRef<LuaBuffer> m_parent;

        public LuaBuffer(Buffer buffer, MemoryTracker memory)
        {
            Buffer = buffer;
            m_memory = memory;
        }

        private LuaBuffer(Buffer buffer, LuaBuffer parent)
        {
            Buffer = buffer;
            m_parent = new LuaObjectRef<LuaBuffer>(parent);
        }

        public override void Dispose()
        {
            if (m_memory != null)
            {
                m_memory.Free(Buffer.Length);
            }
            else
            {
                m_parent.Dispose();
            }
        }

        [LuaMethod]
        public LuaArgs len(LuaArgs args)
        {
            var result = Buffer.Length;
            return new LuaArgs(result);
        }

        [LuaMethod]
        public LuaArgs read(LuaArgs args)
        {
            int start = args.GetInt(0);
            if (args.IsNil(1))
            {
                if (start >= 0 && start < Buffer.Length)
                {
                    var b = Buffer[start];
                    return new LuaArgs(b);
                }
                return new LuaArgs(0);
            }
            else
            {
                int length = args.GetInt(1);
                if (length < 0)
                {
                    throw new LuaError("Read length must be positive");
                }
                if (start < 0 || start + length > Buffer.Length)
                {
                    throw new LuaError("Read must start and end within the buffer");
                }
                var result = Buffer.Read(start, length);
                return new LuaArgs(result);
            }
        }

        [LuaMethod]
        public LuaArgs write(LuaArgs args)
        {
            if (args.IsNumber(1))
            {
                int start = args.GetInt(0);
                var b = args.GetByte(1);
                if (start >= 0 && start < Buffer.Length)
                {
                    Buffer[start] = b;
                }
                return LuaArgs.Empty;
            }
            else
            {
                int start = args.GetInt(0);
                var bytes = args.GetByteString(1);
                if (start < 0 || start + bytes.Length > Buffer.Length)
                {
                    throw new LuaError("Write must start and end within the buffer");
                }
                Buffer.Write(start, bytes);
                return LuaArgs.Empty;
            }
        }

        [LuaMethod]
        public LuaArgs fill(LuaArgs args)
        {
            var b = args.GetByte(0);
            if (args.IsNil(1))
            {
                Buffer.Fill(b);
            }
            else
            {
                int start = args.GetInt(1);
                int length = args.GetInt(2);
                if (length < 0)
                {
                    throw new LuaError("Fill length must be positive");
                }
                if (start < 0 || start + length > Buffer.Length)
                {
                    throw new LuaError("Fill must start and end within the buffer");
                }
                Buffer.Fill(b, start, length);
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs sub(LuaArgs args)
        {
            int start = args.GetInt(0);
            int length = args.IsNil(1) ? (Buffer.Length - start) : args.GetInt(1);
            if (length < 0)
            {
                throw new LuaError("Subbuffer size must be positive");
            }
            if (start < 0 || start + length > Buffer.Length)
            {
                throw new LuaError("Subbuffer bounds must lie within the buffer");
            }
            var result = Buffer.Sub(start, length);
            return new LuaArgs(new LuaBuffer(result, this));
        }

        [LuaMethod]
        public LuaArgs copy(LuaArgs args)
        {
            if (m_memory.Alloc(Buffer.Length))
            {
                var result = Buffer.Copy();
                return new LuaArgs(new LuaBuffer(result, m_memory));
            }
            else
            {
                throw new LuaError("not enough memory");
            }
        }
    }
}
