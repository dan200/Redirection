using Dan200.Core.Render;
using Dan200.Game.Level;

namespace Dan200.Game.Arcade
{
    public class ArcadeRobot : CutsceneEntity
    {
        private ITexture m_screenTexture;

        public ITexture ScreenTexture
        {
            get
            {
                return m_screenTexture;
            }
            set
            {
                m_screenTexture = value;
            }
        }

        public ArcadeRobot(Model model) : base(model, RenderPass.Opaque)
        {
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            base.OnDraw(modelEffect, pass);
            if (m_screenTexture != null)
            {
                Model.DrawSingleGroupWithCustomEmissiveTexture(modelEffect, "Screen", m_screenTexture);
            }
        }
    }
}
