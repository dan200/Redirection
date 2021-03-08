using Dan200.Core.Computer;
using Dan200.Core.Lua;

namespace Dan200.Game.Arcade
{
    public class ScoreDevice : Device
    {
        private string m_description;

        public override string Type
        {
            get
            {
                return "score";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public int Score
        {
            get;
            set;
        }

        public ScoreDevice(string description)
        {
            m_description = description;
            Score = 0;
        }

        [LuaMethod]
        public LuaArgs get(LuaArgs args)
        {
            return new LuaArgs(Score);
        }

        [LuaMethod]
        public LuaArgs submit(LuaArgs args)
        {
            var score = args.GetInt(0);
            if (score > Score)
            {
                Score = score;
                return new LuaArgs(true);
            }
            return new LuaArgs(false);
        }
    }
}
