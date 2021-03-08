
namespace Dan200.Core.Render
{
    public class UpscaleEffectInstance : EffectInstance
    {
        public ITexture Texture;

        public UpscaleEffectInstance() : base("shaders/upscale.effect")
        {
            Texture = Dan200.Core.Render.Texture.White;
        }

        public override void Bind()
        {
            base.Bind();
            Set("texture", Texture, 0);
        }
    }
}
