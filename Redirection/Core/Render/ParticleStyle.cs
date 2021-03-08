using Dan200.Core.Assets;
using OpenTK;

namespace Dan200.Core.Render
{
    public class ParticleStyle : IBasicAsset
    {
        public static ParticleStyle Get(string path)
        {
            return Assets.Assets.Get<ParticleStyle>(path);
        }

        private string m_path;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string Texture
        {
            get;
            private set;
        }

        public float Lifetime
        {
            get;
            private set;
        }

        public float EmitterRate
        {
            get;
            private set;
        }

        public Vector3 Position
        {
            get;
            private set;
        }

        public Vector3 PositionRange
        {
            get;
            private set;
        }

        public Vector3 Velocity
        {
            get;
            private set;
        }

        public Vector3 VelocityRange
        {
            get;
            private set;
        }

        public Vector3 Gravity
        {
            get;
            private set;
        }

        public float Radius
        {
            get;
            private set;
        }

        public float FinalRadius
        {
            get;
            private set;
        }

        public Vector4 Colour
        {
            get;
            private set;
        }

        public Vector4 FinalColour
        {
            get;
            private set;
        }

        public ParticleStyle(string path, IFileStore store)
        {
            m_path = path;
            Reload(store);
        }

        public void Dispose()
        {
        }

        public void Reload(IFileStore store)
        {
            var kvp = new KeyValuePairs();
            using (var reader = store.OpenTextFile(m_path))
            {
                kvp.Load(reader);
            }

            Lifetime = kvp.GetFloat("lifetime", 5.0f);
            EmitterRate = kvp.GetFloat("emitter_rate", 1.0f);

            Position = kvp.GetVector("position", Vector3.Zero);
            PositionRange = kvp.GetVector("position_range", Vector3.Zero);
            Velocity = kvp.GetVector("velocity", Vector3.Zero);
            VelocityRange = kvp.GetVector("velocity_range", Vector3.Zero);
            Gravity = kvp.GetVector("gravity", new Vector3(0.0f, -9.8f, 0.0f));
            Radius = kvp.GetFloat("radius", 0.125f);
            FinalRadius = kvp.GetFloat("final_radius", Radius);

            var colour = kvp.GetColour("colour", Vector3.One);
            var alpha = kvp.GetFloat("alpha", 1.0f);
            Colour = new Vector4(colour, alpha);

            var finalColour = kvp.GetColour("final_colour", colour);
            var finalAlpha = kvp.GetFloat("final_alpha", alpha);
            FinalColour = new Vector4(finalColour, finalAlpha);

            Texture = kvp.GetString("texture", "white.png");
        }
    }
}
