
using OpenTK;

namespace Dan200.Core.Render
{
    public class LitEffectInstance : ModelEffectInstance
    {
        public AmbientLight AmbientLight;
        public DirectionalLight Light;
        public DirectionalLight Light2;

        private static string GetEffectForPass(RenderPass pass)
        {
            switch (pass)
            {
                case RenderPass.Opaque:
                default:
                    {
                        return "shaders/lit_opaque.effect";
                    }
                case RenderPass.Cutout:
                    {
                        return "shaders/lit_cutout.effect";
                    }
                case RenderPass.Translucent:
                    {
                        return "shaders/lit_translucent.effect";
                    }
            }
        }

        public LitEffectInstance(RenderPass pass) : base(GetEffectForPass(pass))
        {
        }

        public override void Bind()
        {
            base.Bind();
            if (AmbientLight != null && AmbientLight.Active)
            {
                Set("ambientLightColour", AmbientLight.Colour);
            }
            else
            {
                Set("ambientLightColour", Vector3.Zero);
            }

            if (Light != null && Light.Active)
            {
                Set("light1Colour", Light.Colour);
                Set("light1Direction", Light.Direction);
            }
            else
            {
                Set("light1Colour", Vector3.Zero);
            }

            if (Light2 != null && Light2.Active)
            {
                Set("light2Colour", Light2.Colour);
                Set("light2Direction", Light2.Direction);
            }
            else
            {
                Set("light2Colour", Vector3.Zero);
            }
        }
    }
}
