
namespace Dan200.Core.Render
{
    public class BackgroundEffectInstance : EffectInstance
    {
        public ITexture Texture;

        public BackgroundEffectInstance() : base("shaders/background.effect")
        {
            Texture = Render.Texture.White;
        }

        public override void Bind()
        {
            base.Bind();
            Set("texture", Texture, 0);
        }
    }
}
