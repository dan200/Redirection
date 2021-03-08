using Dan200.Core.Animation;
using Dan200.Game.Level;
using OpenTK;

namespace Dan200.Game.Game
{
    public abstract class RobotOptionsState : OptionsState
    {
        private CutsceneEntity m_robot;

        protected CutsceneEntity Robot
        {
            get
            {
                return m_robot;
            }
        }

        protected RobotOptionsState(Game game, string title) : base(game, title, "levels/startscreen.level", MenuArrangement.RobotScreen)
        {
        }

        protected override void OnReveal()
        {
            base.OnReveal();

            // Create robot
            m_robot = CreateEntity("models/entities/new/red_robot.obj");

            // Start animation
            StartCameraAnimation("animation/menus/options/camera.anim.lua");
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/options/robot.anim.lua"), false);

            // Reposition sky
            Game.Sky.ForegroundModelTransform = Matrix4.CreateTranslation(-5.0f, 5.0f, -20.0f);
        }

        protected void FuzzToState(State state)
        {
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/options/robot_fuzz.anim.lua"), false);
            CutToState(state, 0.5f);
        }
    }
}
