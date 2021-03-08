using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dan200.Core.Lua
{
    public struct LuaValue
    {
        public static readonly LuaValue Nil = new LuaValue();
        public static readonly LuaValue True = new LuaValue(true);
        public static readonly LuaValue False = new LuaValue(false);

        [StructLayout(LayoutKind.Explicit)]
        private struct ValueUnion
        {
            [FieldOffset(0)]
            public readonly bool Boolean;

#if LUA_32BITS
            [FieldOffset(0)]
			public readonly int Integer;

            [FieldOffset(0)]
			public readonly float Number;
#else
            [FieldOffset(0)]
            public readonly long Integer;

            [FieldOffset(0)]
            public readonly double Number;
#endif

            [FieldOffset(0)]
            public readonly IntPtr Userdata;

            [FieldOffset(8)]
			public readonly string String;

            [FieldOffset(8)]
			public readonly byte[] ByteString;

            [FieldOffset(8)]
            public readonly LuaTable Table;

            [FieldOffset(8)]
            public readonly LuaObject Object;

            [FieldOffset(8)]
            public readonly LuaFunction Function;

            [FieldOffset(8)]
            public readonly LuaCFunction CFunction;

            [FieldOffset(8)]
            public readonly LuaCoroutine Coroutine;

            public ValueUnion(bool value) : this()
            {
                Boolean = value;
            }

            public ValueUnion(int value) : this()
            {
#if LUA_32BITS
				Integer = value;
#else
                Integer = (long)value;
#endif
            }

            public ValueUnion(long value) : this()
            {
#if LUA_32BITS
				Integer = (int)value;
#else
                Integer = value;
#endif
            }

            public ValueUnion(float value) : this()
            {
#if LUA_32BITS
				Number = value;
#else
                Number = (double)value;
#endif
            }

            public ValueUnion(double value) : this()
            {
#if LUA_32BITS
				Number = (float)value;
#else
                Number = value;
#endif
            }

			public ValueUnion(String value) : this()
			{
				String = value;
			}

			public ValueUnion(byte[] value) : this()
            {
                ByteString = value;
            }

            public ValueUnion(LuaTable value) : this()
            {
                Table = value;
            }

            public ValueUnion(LuaObject value) : this()
            {
                Object = value;
            }

            public ValueUnion(LuaCFunction value) : this()
            {
                CFunction = value;
            }

            public ValueUnion(LuaFunction value) : this()
            {
                Function = value;
            }

            public ValueUnion(LuaCoroutine value) : this()
            {
                Coroutine = value;
            }

            public ValueUnion(IntPtr value) : this()
            {
                Userdata = value;
            }
        }

        private readonly LuaValueType m_type;
        private readonly ValueUnion m_value;

		internal LuaValueType Type
        {
            get
            {
                return m_type;
            }
        }

        public LuaValue(bool value)
        {
            m_type = LuaValueType.Boolean;
            m_value = new ValueUnion(value);
        }

        public LuaValue(int value)
        {
            m_type = LuaValueType.Integer;
            m_value = new ValueUnion(value);
        }

        public LuaValue(long value)
        {
            m_type = LuaValueType.Integer;
            m_value = new ValueUnion(value);
        }

        public LuaValue(float value)
        {
            m_type = LuaValueType.Number;
            m_value = new ValueUnion(value);
        }

        public LuaValue(double value)
        {
            m_type = LuaValueType.Number;
            m_value = new ValueUnion(value);
        }

        public LuaValue(string value)
        {
            if (value != null)
            {
                m_type = LuaValueType.String;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(byte[] value)
        {
            if (value != null)
            {
				m_type = LuaValueType.ByteString;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(LuaTable value)
        {
            if (value != null)
            {
                m_type = LuaValueType.Table;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(LuaObject value)
        {
            if (value != null)
            {
                m_type = LuaValueType.Object;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(LuaFunction value)
        {
            if (value != null)
            {
                m_type = LuaValueType.Function;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(LuaCFunction value)
        {
            if (value != null)
            {
                m_type = LuaValueType.CFunction;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(LuaCoroutine value)
        {
            if (value != null)
            {
                m_type = LuaValueType.Coroutine;
                m_value = new ValueUnion(value);
            }
            else
            {
                m_type = LuaValueType.Nil;
                m_value = new ValueUnion();
            }
        }

        public LuaValue(IntPtr value)
        {
            m_type = LuaValueType.Userdata;
            m_value = new ValueUnion(value);
        }

        public static implicit operator LuaValue(bool value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(int value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(long value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(float value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(double value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(string value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(byte[] value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(LuaTable value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(LuaObject value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(LuaFunction value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(LuaCFunction value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(LuaCoroutine value)
        {
            return new LuaValue(value);
        }

        public static implicit operator LuaValue(IntPtr value)
        {
            return new LuaValue(value);
        }

        public string GetTypeName()
        {
            if (m_type == LuaValueType.Object)
            {
                return m_value.Object.TypeName;
            }
            else
            {
                return m_type.GetTypeName();
            }
        }

        public bool IsNil()
        {
            return m_type == LuaValueType.Nil;
        }

        public bool IsBool()
        {
            return m_type == LuaValueType.Boolean;
        }

        public bool GetBool()
        {
            ExpectType(LuaValueType.Boolean);
            return m_value.Boolean;
        }

        public bool IsNumber()
        {
            return m_type == LuaValueType.Integer || m_type == LuaValueType.Number;
        }

        public bool IsInteger()
        {
            return m_type == LuaValueType.Integer;
        }

        public float GetFloat()
        {
            if (m_type == LuaValueType.Number)
            {
                return (float)m_value.Number;
            }
            else if (m_type == LuaValueType.Integer)
            {
                return (float)m_value.Integer;
            }
            else
            {
                throw GenerateTypeError("number");
            }
        }

        public double GetDouble()
        {
            if (m_type == LuaValueType.Number)
            {
                return (double)m_value.Number;
            }
            else if (m_type == LuaValueType.Integer)
            {
                return (double)m_value.Integer;
            }
            else
            {
                throw GenerateTypeError("number");
            }
        }

        public int GetInt()
        {
            if (m_type == LuaValueType.Integer)
            {
                return (int)m_value.Integer;
            }
            else if (m_type == LuaValueType.Number)
            {
                return (int)m_value.Number;
            }
            else
            {
                throw GenerateTypeError("number");
            }
        }

        public long GetLong()
        {
            if (m_type == LuaValueType.Integer)
            {
                return (long)m_value.Integer;
            }
            else if (m_type == LuaValueType.Number)
            {
                return (long)m_value.Number;
            }
            else
            {
                throw GenerateTypeError("number");
            }
        }

        public byte GetByte()
        {
            var n = GetInt();
            if (n < byte.MinValue || n > byte.MaxValue)
            {
                throw new LuaError(
                    string.Format("Value not in range. Expected {0}-{1}", byte.MinValue, byte.MaxValue)
                );
            }
            return (byte)n;
        }

        public bool IsString()
        {
			return m_type == LuaValueType.String || m_type == LuaValueType.ByteString;
        }

        public bool IsByteString()
		{
			return m_type == LuaValueType.ByteString;
		}

		public string GetString()
        {
			if (m_type == LuaValueType.String)
			{
				return m_value.String;
			}
			else if (m_type == LuaValueType.ByteString)
			{
				return Encoding.UTF8.GetString(m_value.ByteString);
			}
			else
			{
				throw GenerateTypeError("string");
			}
        }

        public byte[] GetByteString()
        {
			if (m_type == LuaValueType.String)
			{
				return Encoding.UTF8.GetBytes(m_value.String);
			}
			else if (m_type == LuaValueType.ByteString)
			{
				return m_value.ByteString;
			}
			else
			{
				throw GenerateTypeError("string");
			}
        }

        public bool IsTable()
        {
            return m_type == LuaValueType.Table;
        }

        public LuaTable GetTable()
        {
            ExpectType(LuaValueType.Table);
            return m_value.Table;
        }

        public bool IsObject()
        {
            return m_type == LuaValueType.Object;
        }

        public LuaObject GetObject()
        {
            ExpectType(LuaValueType.Object);
            return m_value.Object;
        }

        public bool IsObject(Type type)
        {
            return m_type == LuaValueType.Object && type.IsInstanceOfType(m_value.Object);
        }

        public LuaObject GetObject(Type type)
        {
            if (m_type == LuaValueType.Object && type.IsInstanceOfType(m_value.Object))
            {
                return m_value.Object;
            }
            else
            {
                throw GenerateTypeError(LuaObject.GetTypeName(type));
            }
        }

        public bool IsObject<T>() where T : LuaObject
        {
            return m_type == LuaValueType.Object && m_value.Object is T;
        }

        public T GetObject<T>() where T : LuaObject
        {
            if (m_type == LuaValueType.Object && m_value.Object is T)
            {
                return (T)m_value.Object;
            }
            else
            {
                throw GenerateTypeError(LuaObject.GetTypeName(typeof(T)));
            }
        }

        public bool IsFunction()
        {
            return m_type == LuaValueType.Function;
        }

        public LuaFunction GetFunction()
        {
            ExpectType(LuaValueType.Function);
            return m_value.Function;
        }

        public bool IsCFunction()
        {
            return m_type == LuaValueType.CFunction;
        }

        public LuaCFunction GetCFunction()
        {
            ExpectType(LuaValueType.CFunction);
            return m_value.CFunction;
        }

        public bool IsUserdata()
        {
            return m_type == LuaValueType.Userdata;
        }

        public IntPtr GetUserdata()
        {
            ExpectType(LuaValueType.Userdata);
            return m_value.Userdata;
        }

        public bool IsCoroutine()
        {
            return m_type == LuaValueType.Coroutine;
        }

        public LuaCoroutine GetCoroutine()
        {
            ExpectType(LuaValueType.Coroutine);
            return m_value.Coroutine;
        }

        public override bool Equals(object obj)
        {
            if (obj is LuaValue)
            {
                return Equals((LuaValue)obj);
            }
            return false;
        }

        public static bool operator ==(LuaValue a, LuaValue b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(LuaValue a, LuaValue b)
        {
            return !a.Equals(b);
        }

        private bool Equals(LuaValue other)
        {
            switch (m_type)
            {
                case LuaValueType.Nil:
                default:
                    {
                        return other.m_type == m_type;
                    }
                case LuaValueType.Boolean:
                    {
                        return other.m_type == LuaValueType.Boolean && other.m_value.Boolean == m_value.Boolean;
                    }
                case LuaValueType.Integer:
                    {
                        if (other.m_type == LuaValueType.Integer)
                        {
                            return other.m_value.Integer == m_value.Integer;
                        }
                        else if (other.m_type == LuaValueType.Number)
                        {
#if LUA_32BITS
						return other.m_value.Number == (float)m_value.Integer;
#else
                            return other.m_value.Number == (double)m_value.Integer;
#endif
                        }
                        else
                        {
                            return false;
                        }
                    }
                case LuaValueType.Number:
                    {
                        if (other.m_type == LuaValueType.Number)
                        {
                            return other.m_value.Number == m_value.Number;
                        }
                        else if (other.m_type == LuaValueType.Integer)
                        {
#if LUA_32BITS
						return (float)other.m_value.Integer == m_value.Number;
#else
                            return (double)other.m_value.Integer == m_value.Number;
#endif
                        }
                        else
                        {
                            return false;
                        }
                    }
				case LuaValueType.String:
					{
						if (other.m_type == LuaValueType.String)
						{
							return other.m_value.String.Equals(m_value.String);
						}
						else if (other.m_type == LuaValueType.ByteString)
						{
							var str = Encoding.UTF8.GetBytes(m_value.String);
							var ostr = other.m_value.ByteString;
							return ByteStringEquals(str, ostr);
						}
						else
						{
							return false;
						}
					}
				case LuaValueType.ByteString:
                    {
						if (other.m_type == LuaValueType.ByteString)
						{
							var str = m_value.ByteString;
							var ostr = other.m_value.ByteString;
							return ByteStringEquals(str, ostr);
						}
						else if (other.m_type == LuaValueType.String)
						{
							var str = m_value.ByteString;
							var ostr = Encoding.UTF8.GetBytes(other.m_value.String);
							return ByteStringEquals(str, ostr);
						}
						else
                        {
                            return false;
                        }
                    }
                case LuaValueType.Table:
                    {
                        return other.m_type == LuaValueType.Table && other.m_value.Table == m_value.Table;
                    }
                case LuaValueType.Object:
                    {
                        return other.m_type == LuaValueType.Object && other.m_value.Object == m_value.Object;
                    }
                case LuaValueType.Function:
                    {
                        return other.m_type == LuaValueType.Function && other.m_value.Function == m_value.Function;
                    }
                case LuaValueType.CFunction:
                    {
                        return other.m_type == LuaValueType.CFunction && other.m_value.CFunction == m_value.CFunction;
                    }
                case LuaValueType.Coroutine:
                    {
                        return other.m_type == LuaValueType.Coroutine && other.m_value.Coroutine == m_value.Coroutine;
                    }
                case LuaValueType.Userdata:
                    {
                        return other.m_type == LuaValueType.Userdata && other.m_value.Userdata == m_value.Userdata;
                    }
            }
        }

        public override int GetHashCode()
        {
            switch (m_type)
            {
                case LuaValueType.Nil:
                default:
                    {
                        return (int)m_type;
                    }
                case LuaValueType.Boolean:
                    {
                        return m_value.Boolean.GetHashCode();
                    }
                case LuaValueType.Integer:
                    {
#if LUA_32BITS
					return ((float)m_value.Integer).GetHashCode ();
#else
                        return ((double)m_value.Integer).GetHashCode();
#endif
                    }
                case LuaValueType.Number:
                    {
                        return m_value.Number.GetHashCode();
                    }
				case LuaValueType.String:
					{
						return ByteStringHash(Encoding.UTF8.GetBytes(m_value.String));
					}
				case LuaValueType.ByteString:
                    {
						return ByteStringHash(m_value.ByteString);
                    }
                case LuaValueType.Table:
                    {
                        return m_value.Table.GetHashCode();
                    }
                case LuaValueType.Object:
                    {
                        return m_value.Object.GetHashCode();
                    }
                case LuaValueType.Function:
                    {
                        return m_value.Function.GetHashCode();
                    }
                case LuaValueType.CFunction:
                    {
                        return m_value.CFunction.GetHashCode();
                    }
                case LuaValueType.Coroutine:
                    {
                        return m_value.Coroutine.GetHashCode();
                    }
                case LuaValueType.Userdata:
                    {
                        return m_value.Userdata.GetHashCode();
                    }
            }
        }

		private static bool ByteStringEquals(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
			{
				return false;
			}
			for (int i = 0; i < a.Length; ++i)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}

		private static int ByteStringHash(byte[] a)
		{
			int hash = 0;
			var bytes = a;
			for (int i = 0; i < bytes.Length; ++i)
			{
				hash += bytes[i];
				hash += (hash << 10);
				hash ^= (hash >> 6);
			}
			hash += (hash << 3);
			hash ^= (hash >> 11);
			hash += (hash << 15);
			return hash;
		}

        public override string ToString()
        {
            switch (m_type)
            {
                case LuaValueType.Nil:
                default:
                    {
                        return "nil";
                    }
                case LuaValueType.Boolean:
                    {
                        return m_value.Boolean ? "true" : "false";
                    }
                case LuaValueType.Integer:
                    {
                        return m_value.Integer.ToString();
                    }
                case LuaValueType.Number:
                    {
                        return m_value.Number.ToString();
                    }
				case LuaValueType.String:
					{
						return m_value.String;
					}
                case LuaValueType.ByteString:
                    {
                        return Encoding.UTF8.GetString(m_value.ByteString);
                    }
                case LuaValueType.Table:
                    {
                        return m_value.Table.ToString();
                    }
                case LuaValueType.Object:
                    {
                        return m_value.Object.ToString();
                    }
                case LuaValueType.Function:
                case LuaValueType.CFunction:
                    {
                        return "function";
                    }
                case LuaValueType.Coroutine:
                    {
                        return "coroutine";
                    }
                case LuaValueType.Userdata:
                    {
                        return m_value.Userdata.ToString();
                    }
            }
        }

        private void ExpectType(LuaValueType type)
        {
            if (m_type != type)
            {
                throw GenerateTypeError(type.GetTypeName());
            }
        }

        private LuaError GenerateTypeError(string expectedTypeName)
        {
            return new LuaError(string.Format("Expected {0}, got {1}", expectedTypeName, GetTypeName()));
        }
    }
}
