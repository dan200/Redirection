using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Dan200.Core.Lua
{
    public class BLONEncoder
    {
        private LuaValue[] m_stringCache;
        private byte m_stringCachePosition;
        private Dictionary<LuaValue, byte> m_reverseStringCache;

        public bool EncodeDoubleAsFloat = false;

        public BLONEncoder()
        {
            m_stringCache = new LuaValue[256];
            m_stringCachePosition = 0;
            m_reverseStringCache = new Dictionary<LuaValue, byte>();
        }

        public byte[] Encode(LuaValue value)
        {
            using (var memory = new MemoryStream())
            {
                Encode(memory, value);
                return memory.ToArray();
            }
        }

        public void Encode(Stream output, LuaValue value)
        {
            using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
            {
                var writer = new BinaryWriter(output, Encoding.UTF8);
                Encode(writer, value);
            }
        }

        private void Encode(BinaryWriter output, LuaValue value)
        {
            EncodeImpl(output, value);
        }

        public byte[] Encode(LuaArgs values)
        {
            using (var memory = new MemoryStream())
            {
                Encode(memory, values);
                return memory.ToArray();
            }
        }

        public void Encode(Stream output, LuaArgs values)
        {
            using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
            {
                var writer = new BinaryWriter(gzip, Encoding.UTF8);
                Encode(writer, values);
            }
        }

        private void Encode(BinaryWriter output, LuaArgs values)
        {
            if (values.Length > byte.MaxValue)
            {
                throw new InvalidDataException("Maximum 255 values may be encoded in a list");
            }
            output.Write((byte)values.Length);
            for (int i = 0; i < values.Length; ++i)
            {
                EncodeImpl(output, values[i]);
            }
        }

        private void EncodeImpl(BinaryWriter output, LuaValue value)
        {
            if (value.IsNil())
            {
                output.Write((byte)BLONValueType.Nil);
            }
            else if (value.IsBool())
            {
                var b = value.GetBool();
                if (b)
                {
                    output.Write((byte)BLONValueType.True);
                }
                else
                {
                    output.Write((byte)BLONValueType.False);
                }
            }
            else if (value.IsNumber())
            {
                if (value.IsInteger())
                {
                    var n = value.GetLong();
                    if (n >= 0)
                    {
                        if (n == 0)
                        {
                            output.Write((byte)BLONValueType.Zero);
                        }
                        else if (n == 1)
                        {
                            output.Write((byte)BLONValueType.One);
                        }
                        else if (n <= Byte.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt8);
                            output.Write((byte)n);
                        }
                        else if (n <= UInt16.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt16);
                            output.Write((ushort)n);
                        }
                        else if (n <= UInt32.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt32);
                            output.Write((uint)n);
                        }
                        else
                        {
                            output.Write((byte)BLONValueType.Int64);
                            output.Write(n);
                        }
                    }
                    else
                    {
                        if (n >= -Byte.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt8_Negative);
                            output.Write((byte)-n);
                        }
                        else if (n >= -UInt16.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt16_Negative);
                            output.Write((ushort)-n);
                        }
                        else if (n >= -UInt32.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt32_Negative);
                            output.Write((uint)-n);
                        }
                        else
                        {
                            output.Write((byte)BLONValueType.Int64);
                            output.Write(n);
                        }
                    }
                }
                else
                {
                    var d = value.GetDouble();
                    if (EncodeDoubleAsFloat && d >= float.MinValue && d <= float.MaxValue)
                    {
                        output.Write((byte)BLONValueType.Float32);
                        output.Write((float)d);
                    }
                    else
                    {
                        output.Write((byte)BLONValueType.Float64);
                        output.Write(d);
                    }
                }
            }
            else if (value.IsString())
            {
                byte cacheIndex;
                if (m_reverseStringCache.TryGetValue(value, out cacheIndex))
                {
                    output.Write((byte)BLONValueType.PreviouslyCachedString);
                    output.Write(cacheIndex);
                }
                else
                {
                    var s = value.GetByteString();
                    bool cached = TryCacheString(s);
                    if (s.Length <= Byte.MaxValue)
                    {
                        output.Write((byte)(cached ? BLONValueType.String8_Cached : BLONValueType.String8));
                        output.Write((byte)s.Length);
                    }
                    else if (s.Length <= UInt16.MaxValue)
                    {
                        output.Write((byte)(cached ? BLONValueType.String16_Cached : BLONValueType.String16));
                        output.Write((ushort)s.Length);
                    }
                    else
                    {
                        output.Write((byte)(cached ? BLONValueType.String32_Cached : BLONValueType.String32));
                        output.Write(s.Length);
                    }
                    output.Write(s);
                }
            }
            else if (value.IsTable())
            {
                var t = value.GetTable();
                if (t.Count <= Byte.MaxValue)
                {
                    output.Write((byte)BLONValueType.Table8);
                    output.Write((byte)t.Count);
                }
                else if (t.Count <= UInt16.MaxValue)
                {
                    output.Write((byte)BLONValueType.Table16);
                    output.Write((ushort)t.Count);
                }
                else
                {
                    output.Write((byte)BLONValueType.Table32);
                    output.Write(t.Count);
                }
                foreach (var k in t.Keys)
                {
                    var v = t[k];
                    if (!k.IsNil() && !v.IsNil())
                    {
                        EncodeImpl(output, k);
                        EncodeImpl(output, v);
                    }
                }
            }
            else
            {
                throw new InvalidDataException(string.Format("Cannot encode type {0}", value.GetTypeName()));
            }
        }

        private bool TryCacheString(byte[] s)
        {
            if (s.Length <= 32)
            {
                var existing = m_stringCache[m_stringCachePosition];
                if (m_reverseStringCache.ContainsKey(existing))
                {
                    m_reverseStringCache.Remove(existing);
                }
                m_stringCache[m_stringCachePosition] = new LuaValue(s);
                m_reverseStringCache.Add(m_stringCache[m_stringCachePosition], m_stringCachePosition);
                m_stringCachePosition++;
                return true;
            }
            return false;
        }
    }
}
