using System;

namespace Dan200.Core.Lua
{
    [LuaType("object")]
    public abstract class LuaObject : IDisposable
    {
        public static string GetTypeName(Type t)
        {
            object[] attributes = t.GetCustomAttributes(typeof(LuaTypeAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                var attribute = (LuaTypeAttribute)attributes[0];
                if (attribute.CustomName != null)
                {
                    return attribute.CustomName;
                }
            }
            return t.Name;
        }

        private string m_typeName;
        private int m_refCount;

        public string TypeName
        {
            get
            {
                return m_typeName;
            }
        }

        protected LuaObject()
        {
            m_typeName = GetTypeName(GetType());
            m_refCount = 0;
        }

        public abstract void Dispose();

        public int Ref()
        {
            return ++m_refCount;
        }

        public int UnRef()
        {
            return --m_refCount;
        }

        public override string ToString()
        {
            return string.Format("{0}: 0x{1:x8}", TypeName, GetHashCode());
        }

        [LuaMethod]
        public LuaArgs getType(LuaArgs args)
        {
            return new LuaArgs(TypeName);
        }
    }
}

