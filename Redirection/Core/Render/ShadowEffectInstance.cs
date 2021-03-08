
using OpenTK;

#if OPENGLES
using OpenTK.Graphics.ES20;
#else
#endif

namespace Dan200.Core.Render
{
    public class ShadowEffectInstance : WorldEffectInstance
    {
        public DirectionalLight Light;

        public ShadowEffectInstance() : base("shaders/shadows.effect")
        {
            Light = null;
        }

        public override void Bind()
        {
            base.Bind();
            if (Light != null && Light.Active)
            {
                Set("lightDirection", Light.Direction);
            }
            else
            {
                Set("lightDirection", Vector3.Zero);
            }
        }
    }
}
