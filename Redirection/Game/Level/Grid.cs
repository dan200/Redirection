using Dan200.Core.Render;
using Dan200.Core.Utils;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public class Grid : IDisposable
    {
        private static Vector4 COLOUR = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
        private static Vector4 BORDER_COLOUR = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
        private static Vector4 X_AXIS_COLOUR = new Vector4(0.8f, 0.0f, 0.0f, 1.0f);
        private static Vector4 Z_AXIS_COLOUR = new Vector4(0.0f, 0.0f, 0.8f, 1.0f);

        private FlatEffectInstance m_effect;
        private Geometry m_geometry;
        private Level m_level;

        private int m_height;

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

        public Grid(Level level)
        {
            m_effect = new FlatEffectInstance();
            m_geometry = new Geometry(Primitive.Lines);
            m_level = level;
            m_height = 0;
            Rebuild();
        }

        public void Dispose()
        {
            m_geometry.Dispose();
        }

        public bool Raycast(Ray ray, out TileCoordinates o_hitCoords, out Direction o_hitDirection, out float o_hitDistance)
        {
            // Get world space ray
            Ray rayLS = ray.ToLocal(m_level.Transform);
            if (rayLS.Direction.Y != 0.0f)
            {
                // Test ray against the grid
                float surfaceHeight = (float)(m_level.Tiles.MinY + m_height) * 0.5f;
                float yDistanceToSurface = surfaceHeight - rayLS.Origin.Y;
                float distToSurface = yDistanceToSurface / rayLS.Direction.Y;
                if (distToSurface >= 0 && distToSurface <= rayLS.Length)
                {
                    Vector3 intersectPos = rayLS.Origin + rayLS.Direction * distToSurface;
                    if (yDistanceToSurface <= 0.0f)
                    {
                        o_hitCoords = new TileCoordinates(
                            (int)Math.Floor(intersectPos.X),
                            m_level.Tiles.MinY + m_height - 1,
                            (int)Math.Floor(intersectPos.Z)
                        );
                        o_hitDirection = Direction.Up;
                        o_hitDistance = distToSurface;
                        return true;
                    }
                    else
                    {
                        o_hitCoords = new TileCoordinates(
                            (int)Math.Floor(intersectPos.X),
                            m_level.Tiles.MinY + m_height,
                            (int)Math.Floor(intersectPos.Z)
                        );
                        o_hitDirection = Direction.Down;
                        o_hitDistance = distToSurface;
                        return true;
                    }
                }
            }
            o_hitCoords = default(TileCoordinates);
            o_hitDirection = default(Direction);
            o_hitDistance = default(float);
            return false;
        }

        public void Draw(Camera camera)
        {
            m_effect.WorldMatrix = m_level.Transform;
            m_effect.ModelMatrix = Matrix4.Identity;
            m_effect.ViewMatrix = camera.Transform;
            m_effect.ProjectionMatrix = camera.CreateProjectionMatrix();
            m_effect.Bind();
            m_geometry.Draw();
        }

        public void Rebuild()
        {
            float height = 0.5f * (float)(m_level.Tiles.MinY + m_height);
            m_geometry.Clear();

            // Create grid
            int minX = m_level.Tiles.MinX - 5;
            int maxX = m_level.Tiles.MaxX + 5;
            int minZ = m_level.Tiles.MinZ - 5;
            int maxZ = m_level.Tiles.MaxZ + 5;
            for (int x = minX; x <= maxX; ++x)
            {
                int firstVertex = m_geometry.VertexCount;
                var colour = (x == 0) ? Z_AXIS_COLOUR : COLOUR;
                if (x == m_level.Tiles.MinX || x == m_level.Tiles.MaxX)
                {
                    m_geometry.AddVertex(new Vector3(x, height, minZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(x, height, m_level.Tiles.MinZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(x, height, m_level.Tiles.MinZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, BORDER_COLOUR);
                    m_geometry.AddVertex(new Vector3(x, height, m_level.Tiles.MaxZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, BORDER_COLOUR);
                    m_geometry.AddVertex(new Vector3(x, height, m_level.Tiles.MaxZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(x, height, maxZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddIndex(firstVertex);
                    m_geometry.AddIndex(firstVertex + 1);
                    m_geometry.AddIndex(firstVertex + 2);
                    m_geometry.AddIndex(firstVertex + 3);
                    m_geometry.AddIndex(firstVertex + 4);
                    m_geometry.AddIndex(firstVertex + 5);
                }
                else
                {
                    m_geometry.AddVertex(new Vector3(x, height, minZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(x, height, maxZ), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddIndex(firstVertex);
                    m_geometry.AddIndex(firstVertex + 1);
                }
            }
            for (int z = minZ; z <= maxZ; ++z)
            {
                int firstVertex = m_geometry.VertexCount;
                var colour = (z == 0) ? X_AXIS_COLOUR : COLOUR;
                if (z == m_level.Tiles.MinZ || z == m_level.Tiles.MaxZ)
                {
                    m_geometry.AddVertex(new Vector3(minX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(m_level.Tiles.MinX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(m_level.Tiles.MinX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, BORDER_COLOUR);
                    m_geometry.AddVertex(new Vector3(m_level.Tiles.MaxX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, BORDER_COLOUR);
                    m_geometry.AddVertex(new Vector3(m_level.Tiles.MaxX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(maxX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddIndex(firstVertex);
                    m_geometry.AddIndex(firstVertex + 1);
                    m_geometry.AddIndex(firstVertex + 2);
                    m_geometry.AddIndex(firstVertex + 3);
                    m_geometry.AddIndex(firstVertex + 4);
                    m_geometry.AddIndex(firstVertex + 5);
                }
                else
                {
                    m_geometry.AddVertex(new Vector3(minX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddVertex(new Vector3(maxX, height, z), Vector3.UnitY, Vector3.UnitX, Vector2.Zero, colour);
                    m_geometry.AddIndex(firstVertex);
                    m_geometry.AddIndex(firstVertex + 1);
                }
            }

            m_geometry.Rebuild();
        }
    }
}

