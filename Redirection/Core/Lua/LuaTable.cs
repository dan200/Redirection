using System;
using System.Collections.Generic;

namespace Dan200.Core.Lua
{
    public class LuaTable
    {
        private Dictionary<LuaValue, LuaValue> m_values;

        public IEnumerable<LuaValue> Keys
        {
            get
            {
                return m_values.Keys;
            }
        }

        public int Count
        {
            get
            {
                return m_values.Count;
            }
        }

        public LuaValue this[LuaValue key]
        {
            get
            {
                if (m_values.ContainsKey(key))
                {
                    return m_values[key];
                }
                return LuaValue.Nil;
            }
            set
            {
                if (key.IsNil())
                {
                    throw new LuaError("table index is nil");
                }
                else if (!value.IsNil())
                {
                    m_values[key] = value;
                }
                else
                {
                    m_values.Remove(key);
                }
            }
        }

        public LuaTable()
        {
            m_values = new Dictionary<LuaValue, LuaValue>();
        }

        public LuaTable(int initialCapacity)
        {
            m_values = new Dictionary<LuaValue, LuaValue>(initialCapacity);
        }

        public string GetTypeName(LuaValue key)
        {
            LuaValue value;
            if (m_values.TryGetValue(key, out value))
            {
                return value.GetTypeName();
            }
            return "nil";
        }

        public bool IsNil(LuaValue key)
        {
            return this[key].IsNil();
        }

        public bool IsBool(LuaValue key)
        {
            return this[key].IsBool();
        }

        public bool GetBool(LuaValue key)
        {
            ExpectType(key, LuaValueType.Boolean);
            return this[key].GetBool();
        }

        public bool IsNumber(LuaValue key)
        {
            return this[key].IsNumber();
        }

        public bool IsInteger(int index)
		{
			return this[index].IsInteger();
		}

		public double GetDouble(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetDouble();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public float GetFloat(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetFloat();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

		public long GetLong(LuaValue key)
		{
			var value = this[key];
			if (value.IsNumber())
			{
				return value.GetLong();
			}
			else
			{
				throw GenerateTypeError("number", key);
			}
		}

		public int GetInt(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetInt();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public byte GetByte(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                var n = value.GetByte();
                if (n < byte.MinValue || n > byte.MaxValue)
                {
                    throw new LuaError(
                        string.Format("Value {0} not in range. Expected {1}-{2}", key, byte.MinValue, byte.MaxValue)
                    );
                }
                return (byte)n;
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public bool IsString(LuaValue key)
        {
            return this[key].IsString();
        }

        public bool IsByteString(LuaValue key)
		{
			return this[key].IsByteString();
		}

		public string GetString(LuaValue key)
        {
			var value = this[key];
			if (value.IsString())
			{
				return value.GetString();
			}
			else
			{
				throw GenerateTypeError("string", key);
			}
        }

        public byte[] GetByteString(LuaValue key)
        {
			var value = this[key];
			if (value.IsString())
			{
				return value.GetByteString();
			}
			else
			{
				throw GenerateTypeError("string", key);
			}
        }

        public bool IsTable(LuaValue key)
        {
            return this[key].IsTable();
        }

        public LuaTable GetTable(LuaValue key)
        {
            ExpectType(key, LuaValueType.Table);
            return this[key].GetTable();
        }

        public bool IsObject(LuaValue key)
        {
            return this[key].IsObject();
        }

        public LuaObject GetObject(LuaValue key)
        {
            ExpectType(key, LuaValueType.Object);
            return this[key].GetObject();
        }

        public bool IsObject(LuaValue key, Type type)
        {
            return this[key].IsObject(type);
        }

        public LuaObject GetObject(LuaValue key, Type type)
        {
            if (!this[key].IsObject(type))
            {
                throw GenerateTypeError(LuaObject.GetTypeName(type), key);
            }
            return this[key].GetObject(type);
        }

        public bool IsObject<T>(LuaValue key) where T : LuaObject
        {
            return this[key].IsObject<T>();
        }

        public T GetObject<T>(LuaValue key) where T : LuaObject
        {
            if (!this[key].IsObject<T>())
            {
                throw GenerateTypeError(LuaObject.GetTypeName(typeof(T)), key);
            }
            return this[key].GetObject<T>();
        }

        public bool IsUserdata(LuaValue key)
        {
            return this[key].IsUserdata();
        }

        public IntPtr GetUserdata(LuaValue key)
        {
            ExpectType(key, LuaValueType.Userdata);
            return this[key].GetUserdata();
        }

        public string ToString(LuaValue key)
        {
            return this[key].ToString();
        }

        private void ExpectType(LuaValue key, LuaValueType type)
        {
            var value = this[key];
            if (value.Type != type)
            {
                throw GenerateTypeError(type.GetTypeName(), key);
            }
        }

        private LuaError GenerateTypeError(string expectedType, LuaValue key)
        {
            throw new LuaError(string.Format("Expected {0} for key {1}, got {2}", expectedType, key.ToString(), GetTypeName(key)));
        }
    }
}
