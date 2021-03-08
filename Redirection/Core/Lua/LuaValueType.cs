namespace Dan200.Core.Lua
{
	internal enum LuaValueType
    {
        // We can store these types
        Nil = 0,
        Boolean,
        Integer,
        Number,
        String,
		ByteString,
        Table,
        Function,
        CFunction,
        Coroutine,
        Object,
        Userdata,
    }

	internal static class ValueTypeExtensions
    {
        public static string GetTypeName(this LuaValueType type)
        {
            switch (type)
            {
                case LuaValueType.Nil:
                default:
                    {
                        return "nil";
                    }
                case LuaValueType.Boolean:
                    {
                        return "boolean";
                    }
                case LuaValueType.Integer:
                case LuaValueType.Number:
                    {
                        return "number";
                    }
                case LuaValueType.String:
				case LuaValueType.ByteString:
                    {
                        return "string";
                    }
                case LuaValueType.Table:
                    {
                        return "table";
                    }
                case LuaValueType.Object:
                    {
                        return "object";
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
                        return "userdata";
                    }
            }
        }
    }
}
