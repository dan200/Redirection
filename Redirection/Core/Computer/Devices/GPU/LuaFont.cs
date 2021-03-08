using Dan200.Core.Lua;
using System.Collections.Generic;

namespace Dan200.Core.Computer.Devices.GPU
{
    [LuaType("font")]
    public class LuaFont : LuaObject
    {
        public readonly Font Font;
        private LuaObjectRef<LuaImage> m_luaImage;

        public LuaFont(Font font, LuaImage luaImage)
        {
            Font = font;
            m_luaImage = new LuaObjectRef<LuaImage>(luaImage);
        }

        public override void Dispose()
        {
            m_luaImage.Dispose();
        }

        [LuaMethod]
        public LuaArgs getImage(LuaArgs args)
        {
            return new LuaArgs(m_luaImage.Value);
        }

        [LuaMethod]
        public LuaArgs getCharacters(LuaArgs args)
        {
            return new LuaArgs(Font.Characters);
        }

        [LuaMethod]
        public LuaArgs getCharacterImage(LuaArgs args)
        {
            var text = args.GetString(0);
            var values = new List<LuaValue>();
            for (int i = 0; i < text.Length; ++i)
            {
                if (!char.IsLowSurrogate(text, i))
                {
                    var codepoint = char.ConvertToUtf32(text, i);
                    values.Add(
                        new LuaImage(
                            Font.GetCharacterImage(codepoint),
                            m_luaImage.Value
                        )
                    );
                }
            }
            return new LuaArgs(values.ToArray());
        }

        [LuaMethod]
        public LuaArgs measureText(LuaArgs args)
        {
            var text = args.GetString(0);
            int width, height;
            Font.MeasureText(text, out width, out height);
            return new LuaArgs(width, height);
        }
    }
}
