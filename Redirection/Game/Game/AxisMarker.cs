using OpenTK;
using System;

namespace Dan200.Core.Render
{
    public class AxisMarker : IDisposable
    {
        private static Vector4 X_AXIS_COLOUR = new Vector4(0.8f, 0.0f, 0.0f, 1.0f);
        private static Vector4 Y_AXIS_COLOUR = new Vector4(0.0f, 0.8f, 0.0f, 1.0f);
        private static Vector4 Z_AXIS_COLOUR = new Vector4(0.0f, 0.0f, 0.8f, 1.0f);

        private FlatEffectInstance m_effect;
        private Geometry m_geometry;

        public Matrix4 Transform;

        public AxisMarker()
        {
            m_effect = new FlatEffectInstance();
            m_geometry = new Geometry(Primitive.Lines, 6, 6);
            Transform = Matrix4.Identity;
            Rebuild();
        }

        public void Dispose()
        {
            m_geometry.Dispose();
        }

        public void Draw(Camera camera)
        {
            m_effect.WorldMatrix = Matrix4.Identity;
            m_effect.ModelMatrix = Transform;
            m_effect.ViewMatrix = camera.Transform;
            m_effect.ProjectionMatrix = camera.CreateProjectionMatrix();
            m_effect.Bind();
            m_geometry.Draw();
        }

        public void Rebuild()
        {
            m_geometry.Clear();

            // X
            m_geometry.AddVertex(Vector3.Zero, Vector3.UnitY, Vector3.UnitX, Vector2.Zero, X_AXIS_COLOUR);
            m_geometry.AddVertex(Vector3.UnitX, Vector3.UnitY, Vector3.UnitX, Vector2.Zero, X_AXIS_COLOUR);
            m_geometry.AddIndex(0);
            m_geometry.AddIndex(1);

            // Y
            m_geometry.AddVertex(Vector3.Zero, Vector3.UnitY, Vector3.UnitX, Vector2.Zero, Y_AXIS_COLOUR);
            m_geometry.AddVertex(Vector3.UnitY, Vector3.UnitY, Vector3.UnitX, Vector2.Zero, Y_AXIS_COLOUR);
            m_geometry.AddIndex(2);
            m_geometry.AddIndex(3);

            // Z
            m_geometry.AddVertex(Vector3.Zero, Vector3.UnitY, Vector3.UnitX, Vector2.Zero, Z_AXIS_COLOUR);
            m_geometry.AddVertex(-Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX, Vector2.Zero, Z_AXIS_COLOUR);
            m_geometry.AddIndex(4);
            m_geometry.AddIndex(5);

            m_geometry.Rebuild();
        }
    }
}

