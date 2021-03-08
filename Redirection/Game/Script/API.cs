using Dan200.Core.Lua;
using System;
using System.Reflection;

namespace Dan200.Game.Script
{
    public class API
    {
        public API()
        {
        }

        public LuaTable ToTable()
        {
            var type = this.GetType();
            var result = new LuaTable();

            MethodInfo[] methods = type.GetMethods();
            for (int i = 0; i < methods.Length; ++i)
            {
                var method = methods[i];
                var name = method.Name;
                object[] attributes = method.GetCustomAttributes(typeof(LuaMethodAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    var attribute = (LuaMethodAttribute)attributes[0];
                    if (attribute.CustomName != null)
                    {
                        name = attribute.CustomName;
                    }
                    result[name] = (LuaCFunction)Delegate.CreateDelegate(typeof(LuaCFunction), this, method);
                }
            }

            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; ++i)
            {
                var field = fields[i];
                var name = field.Name;
                object[] attributes = field.GetCustomAttributes(typeof(LuaFieldAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    var attribute = (LuaFieldAttribute)attributes[0];
                    if (attribute.CustomName != null)
                    {
                        name = attribute.CustomName;
                    }
                    result[name] = (LuaValue)field.GetValue(this);
                }
            }

            return result;
        }
    }
}

