using System.IO;
using System.IO.Compression;
using System.Text;

namespace Dan200.Core.Lua
{
    public class BLONDecoder
    {
        private LuaValue[] m_stringCache;
        private byte m_stringCachePosition;

        public BLONDecoder()
        {
            m_stringCache = new LuaValue[256];
            m_stringCachePosition = 0;
        }

        public LuaValue DecodeValue(byte[] input)
        {
            using (var memory = new MemoryStream(input))
            {
                return DecodeValue(memory);
            }
        }

        public LuaValue DecodeValue(Stream input)
        {
            using (var gzip = new GZipStream(input, CompressionMode.Decompress, true))
            {
                var reader = new BinaryReader(gzip, Encoding.UTF8);
                return DecodeValue(reader);
            }
        }

        private LuaValue DecodeValue(BinaryReader input)
        {
            var type = (BLONValueType)input.ReadByte();
            switch (type)
            {
                case BLONValueType.Nil:
                    {
                        return LuaValue.Nil;
                    }
                case BLONValueType.False:
                    {
                        return LuaValue.False;
                    }
                case BLONValueType.True:
                    {
                        return LuaValue.True;
                    }
                case BLONValueType.Zero:
                    {
                        return new LuaValue(0L);
                    }
                case BLONValueType.One:
                    {
                        return new LuaValue(1L);
                    }
                case BLONValueType.UInt8:
                    {
                        return new LuaValue((long)input.ReadByte());
                    }
                case BLONValueType.UInt16:
                    {
                        return new LuaValue((long)input.ReadUInt16());
                    }
                case BLONValueType.UInt32:
                    {
                        return new LuaValue((long)input.ReadUInt32());
                    }
                case BLONValueType.UInt8_Negative:
                    {
                        return new LuaValue(-(long)input.ReadByte());
                    }
                case BLONValueType.UInt16_Negative:
                    {
                        return new LuaValue(-(long)input.ReadUInt16());
                    }
                case BLONValueType.UInt32_Negative:
                    {
                        return new LuaValue(-(long)input.ReadUInt32());
                    }
                case BLONValueType.Int64:
                    {
                        return new LuaValue(input.ReadInt64());
                    }
                case BLONValueType.Float32:
                    {
                        return new LuaValue(input.ReadSingle());
                    }
                case BLONValueType.Float64:
                    {
                        return new LuaValue(input.ReadDouble());
                    }
                case BLONValueType.String8:
                    {
                        var count = input.ReadByte();
                        return DecodeString(input, count, false);
                    }
                case BLONValueType.String16:
                    {
                        var count = input.ReadUInt16();
                        return DecodeString(input, count, false);
                    }
                case BLONValueType.String32:
                    {
                        var count = input.ReadUInt32();
                        return DecodeString(input, count, false);
                    }
                case BLONValueType.String8_Cached:
                    {
                        var count = input.ReadByte();
                        return DecodeString(input, count, true);
                    }
                case BLONValueType.String16_Cached:
                    {
                        var count = input.ReadUInt16();
                        return DecodeString(input, count, true);
                    }
                case BLONValueType.String32_Cached:
                    {
                        var count = input.ReadUInt32();
                        return DecodeString(input, count, true);
                    }
                case BLONValueType.PreviouslyCachedString:
                    {
                        var index = input.ReadByte();
                        return m_stringCache[index];
                    }
                case BLONValueType.Table8:
                    {
                        var count = input.ReadByte();
                        return DecodeTablePairsImpl(input, count);
                    }
                case BLONValueType.Table16:
                    {
                        var count = input.ReadUInt16();
                        return DecodeTablePairsImpl(input, count);
                    }
                case BLONValueType.Table32:
                    {
                        var count = input.ReadUInt32();
                        return DecodeTablePairsImpl(input, count);
                    }
                default:
                    {
                        throw new InvalidDataException(string.Format("Unrecognised type code: {0}", type));
                    }
            }
        }

        public LuaArgs DecodeList(byte[] input)
        {
            using (var memory = new MemoryStream(input))
            {
                return DecodeList(memory);
            }
        }

        public LuaArgs DecodeList(Stream input)
        {
            using (var gzip = new GZipStream(input, CompressionMode.Decompress, true))
            {
                var reader = new BinaryReader(gzip, Encoding.UTF8);
                return DecodeList(reader);
            }
        }

        private LuaArgs DecodeList(BinaryReader input)
        {
            var count = input.ReadByte();
            if (count == 0)
            {
                return LuaArgs.Empty;
            }
            else if (count == 1)
            {
                var arg0 = DecodeValue(input);
                return new LuaArgs(arg0);
            }
            else if (count == 2)
            {
                var arg0 = DecodeValue(input);
                var arg1 = DecodeValue(input);
                return new LuaArgs(arg0, arg1);
            }
            else if (count == 3)
            {
                var arg0 = DecodeValue(input);
                var arg1 = DecodeValue(input);
                var arg2 = DecodeValue(input);
                return new LuaArgs(arg0, arg1, arg2);
            }
            else if (count == 4)
            {
                var arg0 = DecodeValue(input);
                var arg1 = DecodeValue(input);
                var arg2 = DecodeValue(input);
                var arg3 = DecodeValue(input);
                return new LuaArgs(arg0, arg1, arg2, arg3);
            }
            else
            {
                var arg0 = DecodeValue(input);
                var arg1 = DecodeValue(input);
                var arg2 = DecodeValue(input);
                var arg3 = DecodeValue(input);
                var extraArgs = new LuaValue[count - 4];
                for (int i = 0; i < extraArgs.Length; ++i)
                {
                    extraArgs[i] = DecodeValue(input);
                }
                return new LuaArgs(arg0, arg1, arg2, arg3, extraArgs);
            }
        }

        private LuaValue DecodeTablePairsImpl(BinaryReader input, uint count)
        {
            if (count > int.MaxValue)
            {
                throw new InvalidDataException("Table too large");
            }
            var d = new LuaTable((int)count);
            for (uint i = 0; i < count; ++i)
            {
                var k = DecodeValue(input);
                var v = DecodeValue(input);
                d[k] = v;
            }
            return new LuaValue(d);
        }

        private LuaValue DecodeString(BinaryReader input, uint size, bool cache)
        {
            if (size > int.MaxValue)
            {
                throw new InvalidDataException("String too large");
            }
            var result = new LuaValue(input.ReadBytes((int)size));
            if (cache)
            {
                m_stringCache[m_stringCachePosition] = result;
                m_stringCachePosition++;
            }
            return result;
        }
    }
}

