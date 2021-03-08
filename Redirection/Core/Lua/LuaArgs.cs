using System;
using System.Text;

namespace Dan200.Core.Lua
{
    public struct LuaArgs
    {
        public static readonly LuaArgs Empty = new LuaArgs();
        public static readonly LuaArgs Nil = new LuaArgs(LuaValue.Nil);

        public static LuaArgs Concat(LuaArgs a, LuaArgs b)
        {
            if (b.Length == 0)
            {
                return a;
            }
            else if (b.Length == 0)
            {
                return b;
            }

            var totalLength = a.Length + b.Length;
            if (totalLength == 0)
            {
                return LuaArgs.Empty;
            }
            else if (totalLength == 1)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                return new LuaArgs(arg0);
            }
            else if (totalLength == 2)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                return new LuaArgs(arg0, arg1);
            }
            else if (totalLength == 3)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                var arg2 = (a.Length > 2) ? a[2] : b[2 - a.Length];
                return new LuaArgs(arg0, arg1, arg2);
            }
            else if (totalLength == 4)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                var arg2 = (a.Length > 2) ? a[2] : b[2 - a.Length];
                var arg3 = (a.Length > 3) ? a[3] : b[3 - a.Length];
                return new LuaArgs(arg0, arg1, arg2, arg3);
            }
            else
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                var arg2 = (a.Length > 2) ? a[2] : b[2 - a.Length];
                var arg3 = (a.Length > 3) ? a[3] : b[3 - a.Length];
                var extraArgs = new LuaValue[totalLength - 4];
                for (int i = 0; i < extraArgs.Length; ++i)
                {
                    var n = i + 4;
                    extraArgs[i] = (a.Length > n) ? a[n] : b[n - a.Length];
                }
                return new LuaArgs(arg0, arg1, arg2, arg3, extraArgs);
            }
        }

        private LuaValue m_arg0;
        private LuaValue m_arg1;
        private LuaValue m_arg2;
        private LuaValue m_arg3;
        private LuaValue[] m_extraArgs;
        private int m_start;
        private int m_length;

        public int Length
        {
            get
            {
                return m_length;
            }
        }

        public LuaValue this[int index]
        {
            get
            {
                if (index >= 0 && index < m_length)
                {
                    var realIndex = m_start + index;
                    if (realIndex == 0)
                    {
                        return m_arg0;
                    }
                    else if (realIndex == 1)
                    {
                        return m_arg1;
                    }
                    else if (realIndex == 2)
                    {
                        return m_arg2;
                    }
                    else if (realIndex == 3)
                    {
                        return m_arg3;
                    }
                    else
                    {
                        return m_extraArgs[realIndex - 4];
                    }
                }
                return LuaValue.Nil;
            }
        }

        public LuaArgs(LuaValue arg0)
        {
            m_arg0 = arg0;
            m_arg1 = LuaValue.Nil;
            m_arg2 = LuaValue.Nil;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = null;
            m_start = 0;
            m_length = 1;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = LuaValue.Nil;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = null;
            m_start = 0;
            m_length = 2;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = null;
            m_start = 0;
            m_length = 3;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = arg3;
            m_extraArgs = null;
            m_start = 0;
            m_length = 4;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2, LuaValue arg3, params LuaValue[] extraArgs)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = arg3;
            m_extraArgs = extraArgs;
            m_start = 0;
            m_length = 4 + extraArgs.Length;
        }

        public LuaArgs(LuaValue[] args)
        {
            m_arg0 = LuaValue.Nil;
            m_arg1 = LuaValue.Nil;
            m_arg2 = LuaValue.Nil;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = args;
            m_start = 4;
            m_length = args.Length;
        }

        public LuaArgs Select(int start)
        {
            return Select(start, Math.Max(m_length - start, 0));
        }

        public LuaArgs Select(int start, int length)
        {
            if (start < 0 || length < 0 || start + length > m_length)
            {
                throw new ArgumentOutOfRangeException();
            }
            var copy = this;
            copy.m_start = m_start + start;
            copy.m_length = length;
            return copy;
        }

        public string GetTypeName(int index)
        {
            return this[index].GetTypeName();
        }

        public bool IsNil(int index)
        {
            return this[index].IsNil();
        }

        public bool IsBool(int index)
        {
            return this[index].IsBool();
        }

        public bool GetBool(int index)
        {
            ExpectType(LuaValueType.Boolean, index);
            return this[index].GetBool();
        }

        public bool IsNumber(int index)
        {
            return this[index].IsNumber();
        }

        public bool IsInteger(int index)
        {
            return this[index].IsInteger();
        }

        public double GetDouble(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetDouble();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

        public float GetFloat(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetFloat();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

        public long GetLong(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetLong();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

        public int GetInt(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetInt();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

        public byte GetByte(int index)
        {
            var value = this[index];
            if (value.Type == LuaValueType.Integer || value.Type == LuaValueType.Number)
            {
                var n = value.GetInt();
                if (n < byte.MinValue || n > byte.MaxValue)
                {
                    throw new LuaError(
                        string.Format("Argument {0} not in range. Expected {1}-{2}", index + m_start + 1, byte.MinValue, byte.MaxValue)
                    );
                }
                return (byte)n;
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

        public bool IsString(int index)
        {
            return this[index].IsString();
        }

		public bool IsByteString(int index)
		{
			return this[index].IsByteString();
		}

		public string GetString(int index)
        {
            var value = this[index];
			if (value.IsString())
			{
				return value.GetString();
			}
			else
			{
				throw GenerateTypeError("string", index);
			}
        }

        public byte[] GetByteString(int index)
        {
            var value = this[index];
			if (value.IsString())
			{
				return value.GetByteString();
			}
			else
			{
				throw GenerateTypeError("string", index);
			}
        }

        public bool IsTable(int index)
        {
            return this[index].IsTable();
        }

        public LuaTable GetTable(int index)
        {
            ExpectType(LuaValueType.Table, index);
            return this[index].GetTable();
        }

        public bool IsObject(int index)
        {
            return this[index].IsObject();
        }

        public LuaObject GetObject(int index)
        {
            ExpectType(LuaValueType.Object, index);
            return this[index].GetObject();
        }

        public bool IsObject(int index, Type type)
        {
            return this[index].IsObject(type);
        }

        public LuaObject GetObject(int index, Type type)
        {
            if (!this[index].IsObject(type))
            {
                throw GenerateTypeError(LuaObject.GetTypeName(type), index);
            }
            return this[index].GetObject(type);
        }

        public bool IsObject<T>(int index) where T : LuaObject
        {
            return this[index].IsObject<T>();
        }

        public T GetObject<T>(int index) where T : LuaObject
        {
            if (!this[index].IsObject<T>())
            {
                throw GenerateTypeError(LuaObject.GetTypeName(typeof(T)), index);
            }
            return this[index].GetObject<T>();
        }

        public bool IsFunction(int index)
        {
            return this[index].IsFunction();
        }

        public LuaFunction GetFunction(int index)
        {
            ExpectType(LuaValueType.Function, index);
            return this[index].GetFunction();
        }

        public bool IsCFunction(int index)
        {
            return this[index].IsCFunction();
        }

        public LuaCFunction GetCFunction(int index)
        {
            ExpectType(LuaValueType.CFunction, index);
            return this[index].GetCFunction();
        }

        public bool IsCoroutine(int index)
        {
            return this[index].IsCoroutine();
        }

        public LuaCoroutine GetCoroutine(int index)
        {
            ExpectType(LuaValueType.Coroutine, index);
            return this[index].GetCoroutine();
        }

        public bool IsUserdata(int index)
        {
            return this[index].IsUserdata();
        }

        public IntPtr GetUserdata(int index)
        {
            ExpectType(LuaValueType.Userdata, index);
            return this[index].GetUserdata();
        }

        public string ToString(int index)
        {
            return this[index].ToString();
        }

        public override string ToString()
        {
            var builder = new StringBuilder("[");
            for (int i = 0; i < Length; ++i)
            {
                builder.Append(ToString(i));
                if (i < Length - 1)
                {
                    builder.Append(",");
                }
            }
            builder.Append("]");
            return builder.ToString();
        }

        private void ExpectType(LuaValueType type, int index)
        {
            var foundType = this[index].Type;
            if (foundType != type)
            {
                throw GenerateTypeError(type.GetTypeName(), index);
            }
        }

        private LuaError GenerateTypeError(string expectedTypeName, int index)
        {
            return new LuaError(string.Format("Expected {0} at argument #{1}, got {2}", expectedTypeName, index + m_start + 1, GetTypeName(index)));
        }
    }
}
