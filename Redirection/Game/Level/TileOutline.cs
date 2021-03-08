using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public class TileOutline : IDisposable
    {
        private FlatEffectInstance m_effect;
        private Geometry m_geometry;
        private Level m_level;

        private bool m_visible;
        private TileCoordinates m_position;
        private int m_height;
        private bool m_red;

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
            }
        }

        public TileCoordinates Position
        {
            get
            {
                return m_position;
            }
            set
            {
                if (m_position != value)
                {
                    m_position = value;
                    Rebuild();
                }
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
            set
            {
                if (m_height != value)
                {
                    m_height = value;
                    Rebuild();
                }
            }
        }

        public bool Red
        {
            get
            {
                return m_red;
            }
            set
            {
                m_red = value;
            }
        }

        public TileOutline(Level level)
        {
            m_effect = new FlatEffectInstance();
            m_geometry = new Geometry(Primitive.Lines, 8, 24);
            m_level = level;

            m_visible = false;
            m_position = TileCoordinates.Zero;
            m_height = 1;
            Rebuild();
        }

        public void Dispose()
        {
            m_geometry.Dispose();
        }

        public void Draw(Camera camera)
        {
            if (m_visible)
            {
                m_effect.WorldMatrix = m_level.Transform;
                m_effect.ModelMatrix = Matrix4.Identity;
                m_effect.ViewMatrix = camera.Transform;
                m_effect.ProjectionMatrix = camera.CreateProjectionMatrix();
                m_effect.DiffuseColour = m_red ?
                    new Vector4(0.6f, 0.0f, 0.0f, 1.0f) :
                    new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
                m_effect.Bind();
                m_geometry.Draw();
            }
        }

        public void Rebuild()
        {
            m_geometry.Clear();

            m_geometry.AddVertex(
                new Vector3(m_position.X, m_position.Y * 0.5f, m_position.Z),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X + 1, m_position.Y * 0.5f, m_position.Z),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X, m_position.Y * 0.5f, m_position.Z + 1),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X + 1, m_position.Y * 0.5f, m_position.Z + 1),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X, (m_position.Y + m_height) * 0.5f, m_position.Z),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X + 1, (m_position.Y + m_height) * 0.5f, m_position.Z),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X, (m_position.Y + m_height) * 0.5f, m_position.Z + 1),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );
            m_geometry.AddVertex(
                new Vector3(m_position.X + 1, (m_position.Y + m_height) * 0.5f, m_position.Z + 1),
                Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.One
            );

            m_geometry.AddIndex(0);
            m_geometry.AddIndex(1);
            m_geometry.AddIndex(1);
            m_geometry.AddIndex(3);
            m_geometry.AddIndex(3);
            m_geometry.AddIndex(2);
            m_geometry.AddIndex(2);
            m_geometry.AddIndex(0);

            m_geometry.AddIndex(4);
            m_geometry.AddIndex(5);
            m_geometry.AddIndex(5);
            m_geometry.AddIndex(7);
            m_geometry.AddIndex(7);
            m_geometry.AddIndex(6);
            m_geometry.AddIndex(6);
            m_geometry.AddIndex(4);

            m_geometry.AddIndex(0);
            m_geometry.AddIndex(4);
            m_geometry.AddIndex(1);
            m_geometry.AddIndex(5);
            m_geometry.AddIndex(2);
            m_geometry.AddIndex(6);
            m_geometry.AddIndex(3);
            m_geometry.AddIndex(7);

            m_geometry.Rebuild();
        }
    }
}

