using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Dan200.Core.Render
{
    public class EffectInstance
    {
        public static EffectInstance Current
        {
            get;
            private set;
        }

        public Effect Effect
        {
            get;
            set;
        }

        public EffectInstance(string effectPath) : this(Effect.Get(effectPath))
        {
        }

        public EffectInstance(Effect effect)
        {
            Effect = effect;
        }

        protected void Set(string name, float value)
        {
            int location = Effect.GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform1(location, value);
            }
        }

        protected void Set(string name, Vector2 value)
        {
            int location = Effect.GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform2(location, value);
            }
        }

        protected void Set(string name, Vector3 value)
        {
            int location = Effect.GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform3(location, value);
            }
        }

        protected void Set(string name, Vector4 value)
        {
            int location = Effect.GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform4(location, ref value);
            }
        }

        protected void Set(string name, ref Matrix4 value)
        {
            int location = Effect.GetUniformLocation(name);
            if (location >= 0)
            {
                GL.UniformMatrix4(location, false, ref value);
            }
        }

        protected void Set(string name, ITexture texture, int unit)
        {
            int location = Effect.GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform1(location, unit);
                GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
                GL.BindTexture(TextureTarget.Texture2D, texture.GLTexture);
            }
        }

        public virtual void Bind()
        {
            Current = this;
            GL.UseProgram(Effect.GLProgram);
            switch (Effect.BlendMode)
            {
                case BlendMode.Overwrite:
                default:
                    {
                        GL.Disable(EnableCap.Blend);
                        GL.Disable(EnableCap.AlphaTest);
                        break;
                    }
                case BlendMode.Cutout:
                    {
                        GL.Disable(EnableCap.Blend);
                        GL.Enable(EnableCap.AlphaTest);
                        GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
                        break;
                    }
                case BlendMode.Alpha:
                    {
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                        GL.Enable(EnableCap.AlphaTest);
                        GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
                        break;
                    }
                case BlendMode.Additive:
                    {
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

                        GL.Enable(EnableCap.AlphaTest);
                        GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
                        break;
                    }
            }
        }
    }
}

