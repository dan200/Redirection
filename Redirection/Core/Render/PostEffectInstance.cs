
using Dan200.Game.User;
using OpenTK;

#if OPENGLES
using OpenTK.Graphics.ES20;
#else
#endif


namespace Dan200.Core.Render
{
    public class PostEffectInstance : EffectInstance
    {
        public ITexture Texture;
        private Texture NoiseTexture;
        public float Desaturation;
        public float Warp;
        public float Time;
        public float Gamma;

        public static Effect ChooseEffect(Settings settings)
        {
            if (settings.AAMode == AntiAliasingMode.FXAA)
            {
                if (settings.FancyRewind)
                {
                    return Effect.Get("shaders/post_fxaa.effect");
                }
                else
                {
                    return Effect.Get("shaders/post_fxaa_nowarp.effect");
                }
            }
            else
            {
                if (settings.FancyRewind)
                {
                    return Effect.Get("shaders/post.effect");
                }
                else
                {
                    return Effect.Get("shaders/post_nowarp.effect");
                }
            }
        }

        public PostEffectInstance(Settings settings) : base(ChooseEffect(settings))
        {
            Texture = Render.Texture.White;
            NoiseTexture = Render.Texture.Get("shaders/warp.png", true);
            NoiseTexture.Wrap = true;
            Desaturation = 0.0f;
            Warp = 0.0f;
            Time = 0.0f;
            Gamma = 1.0f;
        }

        public override void Bind()
        {
            base.Bind();
            Set("texture", Texture, 0);
            Set("viewportSize", new Vector2(Texture.Width, Texture.Height));
            Set("noiseTexture", NoiseTexture, 1);
            Set("desaturation", Desaturation);
            Set("warp", Warp);
            Set("time", Time);
            Set("gamma", Gamma);
        }
    }
}
