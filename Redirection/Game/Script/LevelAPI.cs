using Dan200.Core.Lua;
using Dan200.Game.Game;
using Dan200.Game.Level;
using System.Linq;

namespace Dan200.Game.Script
{
    public class LevelAPI : API
    {
        private LevelState m_state;

        public LevelAPI(LevelState state)
        {
            m_state = state;
        }

        [LuaMethod]
        public LuaArgs getPath(LuaArgs args)
        {
            return new LuaArgs(m_state.Level.Info.Path);
        }

        [LuaMethod]
        public LuaArgs getHintLocations(LuaArgs args)
        {
            var hints = m_state.Level.Hints.GetHints().ToArray();
            var table = new LuaTable();
            for (int i = 0; i < hints.Length; ++i)
            {
                var hint = hints[i];
                table[i + 1] = hint.ToLuaValue();
            }
            return new LuaArgs(table);
        }

        [LuaMethod]
        public LuaArgs getRobotLocations(LuaArgs args)
        {
            var result = new LuaTable();
            foreach (Entity e in m_state.Level.Entities)
            {
                if (!e.Dead && e is Robot.Robot)
                {
                    var robot = (Robot.Robot)e;
                    if (!robot.Immobile)
                    {
                        result[result.Count + 1] = robot.Location.ToLuaValue();
                    }
                }
            }
            return new LuaArgs(result);
        }
    }
}

