using Dan200.Core.Animation;
using Dan200.Core.Render;
using Dan200.Core.Util;
using OpenTK;

namespace Dan200.Game.Level
{
    public class SkyInstance
    {
        private Sky m_sky;
        private LitEffectInstance m_litEffect;

        private IAnimation m_animation;
        private float m_animTime;
        private ModelInstance m_model;
        private ModelInstance m_foregroundModel;

        private Vector3 m_backgroundColour;
        private Vector3 m_ambientColour;
        private Vector3 m_lightColour;
        private Vector3 m_lightDirection;
        private Vector3 m_light2Colour;
        private Vector3 m_light2Direction;

        public Sky Sky
        {
            get
            {
                return m_sky;
            }
        }

        public Vector3 BackgroundColour
        {
            get
            {
                return m_backgroundColour;
            }
        }

        public Vector3 AmbientColour
        {
            get
            {
                return m_ambientColour;
            }
        }

        public Vector3 LightColour
        {
            get
            {
                return m_lightColour;
            }
        }

        public Vector3 LightDirection
        {
            get
            {
                return m_lightDirection;
            }
        }

        public Vector3 Light2Colour
        {
            get
            {
                return m_light2Colour;
            }
        }

        public Vector3 Light2Direction
        {
            get
            {
                return m_light2Direction;
            }
        }

        public float AnimTime
        {
            get
            {
                return m_animTime;
            }
            set
            {
                m_animTime = value;
                if (m_model != null)
                {
                    m_model.AnimTime = m_animTime;
                }
                if (m_foregroundModel != null)
                {
                    m_foregroundModel.AnimTime = m_animTime;
                }
            }
        }

        public Matrix4 ForegroundModelTransform
        {
            get
            {
                if (m_foregroundModel != null)
                {
                    return m_foregroundModel.Transform;
                }
                return Matrix4.Identity;
            }
            set
            {
                if (m_foregroundModel != null)
                {
                    m_foregroundModel.Transform = value;
                }
            }
        }

        public SkyInstance(Sky sky)
        {
            m_sky = sky;

            m_litEffect = new LitEffectInstance(sky.RenderPass);
            m_litEffect.AmbientLight = new AmbientLight(Vector3.Zero);
            m_litEffect.Light = new DirectionalLight(Vector3.UnitY, Vector3.Zero);
            m_litEffect.Light2 = new DirectionalLight(Vector3.UnitY, Vector3.Zero);

            if (m_sky.AnimPath != null)
            {
                m_animation = LuaAnimation.Get(m_sky.AnimPath);
                m_animTime = 0.0f;
            }
            if (m_sky.ModelPath != null)
            {
                var model = Model.Get(m_sky.ModelPath);
                m_model = new ModelInstance(model, Matrix4.Identity);
                m_model.Animation = m_animation;
                m_model.AnimTime = m_animTime;
                m_model.Animate();
            }
            if (m_sky.ForegroundModelPath != null)
            {
                var model = Model.Get(m_sky.ForegroundModelPath);
                m_foregroundModel = new ModelInstance(model, Matrix4.Identity);
                m_foregroundModel.Animation = m_animation;
                m_foregroundModel.AnimTime = m_animTime;
                m_foregroundModel.Animate();
            }
            AnimateLights();
        }

        public void ReloadAssets()
        {
            // Refresh models
            if (m_sky.ModelPath != null)
            {
                var model = Model.Get(m_sky.ModelPath);
                m_model = new ModelInstance(model, Matrix4.Identity);
            }
            else
            {
                m_model = null;
            }

            if (m_sky.ForegroundModelPath != null)
            {
                var model = Model.Get(m_sky.ForegroundModelPath);
                var oldTransform = (m_foregroundModel != null) ? m_foregroundModel.Transform : Matrix4.Identity;
                m_foregroundModel = new ModelInstance(model, oldTransform);
            }
            else
            {
                m_foregroundModel = null;
            }

            // Refresh animation
            if (m_sky.AnimPath != null)
            {
                m_animation = LuaAnimation.Get(m_sky.AnimPath);
            }
            else
            {
                m_animation = null;
            }

            Animate();
        }

        public void Animate()
        {
            if (m_model != null)
            {
                m_model.Animation = m_animation;
                m_model.Animate();
            }
            if (m_foregroundModel != null)
            {
                m_foregroundModel.Animation = m_animation;
                m_foregroundModel.Animate();
            }
            AnimateLights();
        }

        public void DrawBackground(Camera camera)
        {
            if (m_model != null)
            {
                // Setup transforms
                var cameraTrans = camera.Transform;
                cameraTrans.Row3 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

                m_litEffect.WorldMatrix = Matrix4.Identity;
                m_litEffect.ModelMatrix = m_model.Transform;
                m_litEffect.ViewMatrix = cameraTrans;
                m_litEffect.ProjectionMatrix = camera.CreateProjectionMatrix();

                m_litEffect.WorldMatrix = m_litEffect.WorldMatrix;
                m_litEffect.ModelMatrix = m_litEffect.ModelMatrix;
                m_litEffect.ViewMatrix = m_litEffect.ViewMatrix;
                m_litEffect.ProjectionMatrix = m_litEffect.ProjectionMatrix;

                // Draw the geometry
                m_litEffect.AmbientLight.Colour = m_ambientColour;
                m_litEffect.Light.Colour = m_lightColour;
                m_litEffect.Light.Direction = m_lightDirection;
                m_litEffect.Light.Active = (m_lightColour.LengthSquared > 0.0f);
                m_litEffect.Light2.Colour = m_light2Colour;
                m_litEffect.Light2.Direction = m_light2Direction;
                m_litEffect.Light2.Active = (m_light2Colour.LengthSquared > 0.0f);
                m_model.Draw(m_litEffect);
            }
        }

        public void DrawForeground(ModelEffectInstance effect, RenderPass pass)
        {
            if (m_foregroundModel != null && m_sky.ForegroundRenderPass == pass)
            {
                var worldMatrix = effect.WorldMatrix;
                effect.WorldMatrix = Matrix4.Identity;
                m_foregroundModel.Draw(effect);
                effect.WorldMatrix = worldMatrix;
            }
        }

        public void DrawForegroundShadows(ShadowEffectInstance effect)
        {
            if (m_foregroundModel != null && m_sky.CastShadows && m_sky.ForegroundRenderPass == RenderPass.Opaque)
            {
                var worldMatrix = effect.WorldMatrix;
                effect.WorldMatrix = Matrix4.Identity;
                m_foregroundModel.DrawShadows(effect);
                effect.WorldMatrix = worldMatrix;
            }
        }

        private void AnimateLights()
        {
            if (m_animation != null)
            {
                m_backgroundColour = SampleLightAnim("Background", m_sky.BackgroundColour);
                m_ambientColour = SampleLightAnim("Ambient", m_sky.AmbientColour);
                m_lightColour = SampleLightAnim("Light", m_sky.LightColour, m_sky.LightDirection, out m_lightDirection);
                m_light2Colour = SampleLightAnim("Light2", m_sky.Light2Colour, m_sky.Light2Direction, out m_light2Direction);
            }
            else
            {
                m_backgroundColour = m_sky.BackgroundColour;
                m_ambientColour = m_sky.AmbientColour;
                m_lightColour = m_sky.LightColour;
                m_lightDirection = m_sky.LightDirection;
                m_light2Colour = m_sky.Light2Colour;
                m_light2Direction = m_sky.Light2Direction;
            }
        }

        private Vector3 SampleLightAnim(string partName, Vector3 inColour)
        {
            var unused = Vector3.UnitZ;
            return SampleLightAnim(partName, inColour, unused, out unused);
        }

        private Vector3 SampleLightAnim(string partName, Vector3 inColour, Vector3 inDir, out Vector3 o_outDir)
        {
            bool visible;
            Matrix4 transform;
            Vector2 uvOffset;
            Vector2 uvScale;
            Vector4 colour;
            float cameraFOV;
            m_animation.Animate(partName, m_animTime, out visible, out transform, out uvOffset, out uvScale, out colour, out cameraFOV);
            MathUtils.FastInvert(ref transform);
            o_outDir = Vector3.TransformNormalInverse(inDir, transform);
            return visible ? new Vector3(
                inColour.X * colour.X * colour.W,
                inColour.Y * colour.Y * colour.W,
                inColour.Z * colour.Z * colour.W
            ) : Vector3.Zero;
        }
    }
}

