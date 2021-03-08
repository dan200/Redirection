using System;

using OpenTK;
using Dan200.Core.Main;
using System.IO;
using Dan200.Core.Util;

using OpenTK.Graphics.OpenGL;

namespace Dan200.Core.Render
{
    public class Geometry : IDisposable
    {
        private static int FLOATS_PER_VERTEX = 21;

        private Primitive m_primitiveType;
        private BeginMode m_beginMode;
        private BufferUsageHint m_bufferUsageHint;

        private int m_vertexBuffer;
        private float[] m_vertexData;
        private int m_vertexCount;

        private int m_indexBuffer;
        private short[] m_indexData;
        private int m_indexCount;

        public Primitive PrimitiveType
        {
            get
            {
                return m_primitiveType;
            }
        }

        public int VertexCount
        {
            get
            {
                return m_vertexCount;
            }
        }

        public int IndexCount
        {
            get
            {
                return m_indexCount;
            }
        }

        public Geometry(Primitive primitiveType, int vertexCountHint = 32, int indexCountHint = 32, bool dynamicHint = false)
        {
            m_primitiveType = primitiveType;
            switch (primitiveType)
            {
                case Primitive.Lines:
                    {
                        m_beginMode = BeginMode.Lines;
                        break;
                    }
                case Primitive.Triangles:
                default:
                    {
                        m_beginMode = BeginMode.Triangles;
                        break;
                    }
            }
            m_bufferUsageHint = dynamicHint ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw;

            GL.GenBuffers(1, out m_vertexBuffer);
            m_vertexData = new float[vertexCountHint * FLOATS_PER_VERTEX];
            m_vertexCount = 0;

            GL.GenBuffers(1, out m_indexBuffer);
            m_indexData = new short[indexCountHint];
            m_indexCount = 0;
        }

        ~Geometry()
        {
            if (m_vertexBuffer >= 0 || m_indexBuffer >= 0)
            {
                App.DebugLog("Warning: Undisposed Geometry");
            }
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref m_vertexBuffer);
            GL.DeleteBuffers(1, ref m_indexBuffer);
            m_vertexBuffer = -1;
            m_indexBuffer = -1;
        }

        public void Clear()
        {
            m_vertexCount = 0;
            m_indexCount = 0;
        }

        public void AddVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord, Vector4 colour)
        {
            AddVertex(position, normal, tangent, texCoord, texCoord, texCoord, texCoord, colour);
        }

        public void AddVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord0, Vector2 texCoord1, Vector2 texCoord2, Vector2 texCoord3, Vector4 colour)
        {
            int index = m_vertexCount * FLOATS_PER_VERTEX;
            if (index >= m_vertexData.Length)
            {
                Array.Resize(ref m_vertexData, Math.Max(m_vertexData.Length * 2, 32 * FLOATS_PER_VERTEX));
            }
            m_vertexData[index] = position.X;
            m_vertexData[index + 1] = position.Y;
            m_vertexData[index + 2] = position.Z;
            m_vertexData[index + 3] = normal.X;
            m_vertexData[index + 4] = normal.Y;
            m_vertexData[index + 5] = normal.Z;
            m_vertexData[index + 6] = texCoord0.X;
            m_vertexData[index + 7] = texCoord0.Y;
            m_vertexData[index + 8] = texCoord1.X;
            m_vertexData[index + 9] = texCoord1.Y;
            m_vertexData[index + 10] = colour.X;
            m_vertexData[index + 11] = colour.Y;
            m_vertexData[index + 12] = colour.Z;
            m_vertexData[index + 13] = colour.W;
            m_vertexData[index + 14] = tangent.X;
            m_vertexData[index + 15] = tangent.Y;
            m_vertexData[index + 16] = tangent.Z;
            m_vertexData[index + 17] = texCoord2.X;
            m_vertexData[index + 18] = texCoord2.Y;
            m_vertexData[index + 19] = texCoord3.X;
            m_vertexData[index + 20] = texCoord3.Y;
            m_vertexCount++;
        }

        public void GetVertexPosition(int vertexIndex, out Vector3 position)
        {
            int index = vertexIndex * FLOATS_PER_VERTEX;
            position = new Vector3(
                m_vertexData[index],
                m_vertexData[index + 1],
                m_vertexData[index + 2]
            );
        }

        public void GetVertex(int vertexIndex, out Vector3 o_position, out Vector3 o_normal, out Vector3 o_tangent)
        {
            int index = vertexIndex * FLOATS_PER_VERTEX;
            o_position = new Vector3(
                m_vertexData[index],
                m_vertexData[index + 1],
                m_vertexData[index + 2]
            );
            o_normal = new Vector3(
                m_vertexData[index + 3],
                m_vertexData[index + 4],
                m_vertexData[index + 5]
            );
            o_tangent = new Vector3(
                m_vertexData[index + 14],
                m_vertexData[index + 15],
                m_vertexData[index + 16]
            );
        }

        public void GetVertex(int vertexIndex, out Vector3 o_position, out Vector3 o_normal, out Vector3 o_tangent, out Vector2 o_texCoord0, out Vector2 o_texCoord1, out Vector2 o_texCoord2, out Vector2 o_texCoord3, out Vector4 o_colour)
        {
            int index = vertexIndex * FLOATS_PER_VERTEX;
            GetVertex(vertexIndex, out o_position, out o_normal, out o_tangent);
            o_texCoord0 = new Vector2(
                m_vertexData[index + 6],
                m_vertexData[index + 7]
            );
            o_texCoord1 = new Vector2(
                m_vertexData[index + 8],
                m_vertexData[index + 9]
            );
            o_colour = new Vector4(
                m_vertexData[index + 10],
                m_vertexData[index + 11],
                m_vertexData[index + 12],
                m_vertexData[index + 13]
            );
            o_texCoord2 = new Vector2(
                m_vertexData[index + 17],
                m_vertexData[index + 18]
            );
            o_texCoord3 = new Vector2(
                m_vertexData[index + 19],
                m_vertexData[index + 20]
            );
        }

        public void AddIndex(int index)
        {
            if (m_indexCount >= m_indexData.Length)
            {
                Array.Resize(ref m_indexData, Math.Max(m_indexData.Length * 2, 256));
            }
            m_indexData[m_indexCount] = (short)index;
            m_indexCount++;
        }

        public int GetIndex(int indexIndex)
        {
            return m_indexData[indexIndex];
        }

        public void Rebuild()
        {
            if (m_vertexData.Length > m_vertexCount * FLOATS_PER_VERTEX)
            {
                Array.Resize(ref m_vertexData, m_vertexCount * FLOATS_PER_VERTEX);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(m_vertexCount * FLOATS_PER_VERTEX * sizeof(float)), m_vertexData, m_bufferUsageHint);

            if (m_indexData.Length > m_indexCount)
            {
                Array.Resize(ref m_indexData, m_indexCount);
            }
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(m_indexCount * sizeof(short)), m_indexData, m_bufferUsageHint);
        }

        private String WrapAttribName(String s)
        {
            return s;
        }

        public void Draw()
        {
            DrawRange(0, IndexCount);
        }

        public void DrawRange(int startIndex, int indexCount)
        {
            if (startIndex < 0 || indexCount < 0 || startIndex + indexCount > m_indexCount)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (indexCount == 0)
            {
                return;
            }

            var effect = EffectInstance.Current.Effect;
            int positionLocation = effect.GetAttributeLocation("position");
            int normalLocation = effect.GetAttributeLocation("normal");
            int texCoord0Location = effect.GetAttributeLocation("texCoord0");
            int texCoord1Location = effect.GetAttributeLocation("texCoord1");
            int colourLocation = effect.GetAttributeLocation("colour");
            int tangentLocation = effect.GetAttributeLocation("tangent");
            int texCoord2Location = effect.GetAttributeLocation("texCoord2");
            int texCoord3Location = effect.GetAttributeLocation("texCoord3");

            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vertexBuffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_indexBuffer);

            if (positionLocation >= 0)
            {
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(0));
                GL.EnableVertexAttribArray(positionLocation);
            }
            if (normalLocation >= 0)
            {
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, true, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(3 * sizeof(float)));
                GL.EnableVertexAttribArray(normalLocation);
            }
            if (texCoord0Location >= 0)
            {
                GL.VertexAttribPointer(texCoord0Location, 2, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(6 * sizeof(float)));
                GL.EnableVertexAttribArray(texCoord0Location);
            }
            if (texCoord1Location >= 0)
            {
                GL.VertexAttribPointer(texCoord1Location, 2, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(8 * sizeof(float)));
                GL.EnableVertexAttribArray(texCoord1Location);
            }
            if (colourLocation >= 0)
            {
                GL.VertexAttribPointer(colourLocation, 4, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(10 * sizeof(float)));
                GL.EnableVertexAttribArray(colourLocation);
            }
            if (tangentLocation >= 0)
            {
                GL.VertexAttribPointer(tangentLocation, 3, VertexAttribPointerType.Float, true, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(14 * sizeof(float)));
                GL.EnableVertexAttribArray(tangentLocation);
            }
            if (texCoord2Location >= 0)
            {
                GL.VertexAttribPointer(texCoord2Location, 2, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(17 * sizeof(float)));
                GL.EnableVertexAttribArray(texCoord2Location);
            }
            if (texCoord3Location >= 0)
            {
                GL.VertexAttribPointer(texCoord3Location, 2, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), (IntPtr)(19 * sizeof(float)));
                GL.EnableVertexAttribArray(texCoord3Location);
            }

            GL.DrawElements(m_beginMode, indexCount, DrawElementsType.UnsignedShort, startIndex * sizeof(ushort));
            RenderStats.AddDrawCall(indexCount / m_primitiveType.GetVertexCount());

            if (positionLocation >= 0)
            {
                GL.DisableVertexAttribArray(positionLocation);
            }
            if (normalLocation >= 0)
            {
                GL.DisableVertexAttribArray(normalLocation);
            }
            if (texCoord0Location >= 0)
            {
                GL.DisableVertexAttribArray(texCoord0Location);
            }
            if (texCoord1Location >= 0)
            {
                GL.DisableVertexAttribArray(texCoord1Location);
            }
            if (colourLocation >= 0)
            {
                GL.DisableVertexAttribArray(colourLocation);
            }
            if (tangentLocation >= 0)
            {
                GL.DisableVertexAttribArray(tangentLocation);
            }
            if (texCoord2Location >= 0)
            {
                GL.DisableVertexAttribArray(texCoord2Location);
            }
            if (texCoord3Location >= 0)
            {
                GL.DisableVertexAttribArray(texCoord3Location);
            }
            App.CheckOpenGLError();
        }
    }

    public static class GeometryExtensions
    {
        public static void Add2DQuad(
            this Geometry geometry,
            Vector2 topLeft,
            Vector2 bottomRight
        )
        {
            geometry.Add2DQuad(
                topLeft, bottomRight,
                Quad.UnitSquare,
                Vector4.One
            );
        }

        public static void Add2DQuad(
            this Geometry geometry,
            Vector2 topLeft,
            Vector2 bottomRight,
            Vector4 colour
        )
        {
            geometry.Add2DQuad(
                topLeft, bottomRight,
                Quad.UnitSquare,
                colour
            );
        }

        public static void Add2DQuad(
            this Geometry geometry,
            Vector2 topLeft,
            Vector2 bottomRight,
            Quad texCoords
        )
        {
            geometry.Add2DQuad(
                topLeft, bottomRight,
                texCoords,
                Vector4.One
            );
        }

        public static void Add2DQuad(
            this Geometry geometry,
            Vector2 topLeft,
            Vector2 bottomRight,
            Quad texCoords,
            Vector4 colour
        )
        {
            geometry.AddQuad(
                new Vector3(topLeft.X, topLeft.Y, 0.0f),
                new Vector3(bottomRight.X, topLeft.Y, 0.0f),
                new Vector3(topLeft.X, bottomRight.Y, 0.0f),
                new Vector3(bottomRight.X, bottomRight.Y, 0.0f),
                texCoords,
                colour
            );
        }

        public static void Add2DNineSlice(
            this Geometry geometry,
            Vector2 topLeft,
            Vector2 bottomRight,
            float leftMargin,
            float topMargin,
            float rightMargin,
            float bottomMargin
        )
        {
            geometry.Add2DNineSlice(
                topLeft,
                bottomRight,
                leftMargin,
                topMargin,
                rightMargin,
                bottomMargin,
                Vector4.One
            );
        }

        public static void Add2DNineSlice(
            this Geometry geometry,
            Vector2 topLeft,
            Vector2 bottomRight,
            float leftMargin,
            float topMargin,
            float rightMargin,
            float bottomMargin,
            Vector4 colour
        )
        {
            var topRight = new Vector2(bottomRight.X, topLeft.Y);
            var bottomLeft = new Vector2(topLeft.X, bottomRight.Y);

            // TOP
            geometry.Add2DQuad(topLeft + new Vector2(0.0f, 0.0f), topLeft + new Vector2(leftMargin, topMargin), new Quad(0.0f, 0.0f, 0.25f, 0.25f), colour);
            /*
            geometry.Add2DQuad(topLeft + new Vector2(leftMargin, 0.0f), topRight + new Vector2(-rightMargin, topMargin), new Quad(0.25f, 0.0f, 0.5f, 0.25f), colour);
            geometry.Add2DQuad(topRight + new Vector2(-rightMargin, 0.0f), topRight + new Vector2(0.0f, topMargin), new Quad(0.75f, 0.0f, 0.25f, 0.25f), colour);
            */
            // Hack to make the dialog box title area fit
            geometry.Add2DQuad(topLeft + new Vector2(leftMargin, 0.0f), topRight + new Vector2(-1.5f * rightMargin, topMargin), new Quad(0.25f, 0.0f, 0.375f, 0.25f), colour);
            geometry.Add2DQuad(topRight + new Vector2(-1.5f * rightMargin, 0.0f), topRight + new Vector2(0.0f, topMargin), new Quad(0.625f, 0.0f, 0.375f, 0.25f), colour);

            // MIDDLE
            geometry.Add2DQuad(topLeft + new Vector2(0.0f, topMargin), bottomLeft + new Vector2(leftMargin, -bottomMargin), new Quad(0.0f, 0.25f, 0.25f, 0.5f));
            geometry.Add2DQuad(topLeft + new Vector2(leftMargin, topMargin), bottomRight + new Vector2(-rightMargin, -bottomMargin), new Quad(0.25f, 0.25f, 0.5f, 0.5f), colour);
            geometry.Add2DQuad(topRight + new Vector2(-rightMargin, topMargin), bottomRight + new Vector2(0.0f, -bottomMargin), new Quad(0.75f, 0.25f, 0.25f, 0.5f), colour);

            // BOTTOM
            geometry.Add2DQuad(bottomLeft + new Vector2(0.0f, -bottomMargin), bottomLeft + new Vector2(leftMargin, 0.0f), new Quad(0.0f, 0.75f, 0.25f, 0.25f));
            geometry.Add2DQuad(bottomLeft + new Vector2(leftMargin, -bottomMargin), bottomRight + new Vector2(-rightMargin, 0.0f), new Quad(0.25f, 0.75f, 0.5f, 0.25f), colour);
            geometry.Add2DQuad(bottomRight + new Vector2(-rightMargin, -bottomMargin), bottomRight + new Vector2(0.0f, 0.0f), new Quad(0.75f, 0.75f, 0.25f, 0.25f), colour);
        }

        public static void Add2DLine(
            this Geometry geometry,
            Vector2 start,
            Vector2 end,
            Vector4 colour
        )
        {
            AddLine(geometry, new Vector3(start.X, start.Y, 0.0f), new Vector3(end.X, end.Y, 0.0f), colour);
        }

        public static void AddLine(
            this Geometry geometry,
            Vector3 start,
            Vector3 end,
            Vector4 colour
        )
        {
            var firstVertex = geometry.VertexCount;
            geometry.AddVertex(start, Vector3.UnitZ, Vector3.UnitX, Vector2.Zero, colour);
            geometry.AddVertex(end, Vector3.UnitZ, Vector3.UnitX, Vector2.Zero, colour);
            geometry.AddIndex(firstVertex);
            geometry.AddIndex(firstVertex + 1);
        }

        public static void AddQuad(
            this Geometry geometry,
            Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight,
            Quad texCoords,
            Vector4 colour
        )
        {
            Vector3 normal, tangent;
            MathUtils.GenerateNormalAndTangent(
                bottomLeft, topRight, topLeft,
                texCoords.BottomLeft, texCoords.TopRight, texCoords.TopLeft,
                out normal, out tangent
            );

            int firstVertex = geometry.VertexCount;
            geometry.AddVertex(topLeft, normal, tangent, texCoords.TopLeft, colour);
            geometry.AddVertex(topRight, normal, tangent, texCoords.TopRight, colour);
            geometry.AddVertex(bottomLeft, normal, tangent, texCoords.BottomLeft, colour);
            geometry.AddVertex(bottomRight, normal, tangent, texCoords.BottomRight, colour);
            geometry.AddIndex(firstVertex + 2);
            geometry.AddIndex(firstVertex + 1);
            geometry.AddIndex(firstVertex + 0);
            geometry.AddIndex(firstVertex + 1);
            geometry.AddIndex(firstVertex + 2);
            geometry.AddIndex(firstVertex + 3);
        }

        public static void AddQuad(
            this Geometry geometry,
            Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight,
            Vector2 topLeftTexCoord, Vector2 topRightTexCoord, Vector2 bottomLeftTexCoord, Vector2 bottomRightTexCoord,
            Vector4 colour
        )
        {
            Vector3 normal, tangent;
            MathUtils.GenerateNormalAndTangent(
                bottomLeft, topRight, topLeft,
                bottomLeftTexCoord, topRightTexCoord, topLeftTexCoord,
                out normal, out tangent
            );

            int firstVertex = geometry.VertexCount;
            geometry.AddVertex(topLeft, normal, tangent, topLeftTexCoord, colour);
            geometry.AddVertex(topRight, normal, tangent, topRightTexCoord, colour);
            geometry.AddVertex(bottomLeft, normal, tangent, bottomLeftTexCoord, colour);
            geometry.AddVertex(bottomRight, normal, tangent, bottomRightTexCoord, colour);
            geometry.AddIndex(firstVertex + 2);
            geometry.AddIndex(firstVertex + 1);
            geometry.AddIndex(firstVertex + 0);
            geometry.AddIndex(firstVertex + 1);
            geometry.AddIndex(firstVertex + 2);
            geometry.AddIndex(firstVertex + 3);
        }

        public static void AddQuad(
            this Geometry geometry,
            Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight,
            Quad texCoords0, Quad texCoords1, Quad texCoords2, Quad texCoords3,
            Vector4 colour
        )
        {
            Vector3 normal, tangent;
            MathUtils.GenerateNormalAndTangent(
                bottomLeft, topRight, topLeft,
                texCoords0.BottomLeft, texCoords0.TopRight, texCoords0.TopLeft,
                out normal, out tangent
            );

            int firstVertex = geometry.VertexCount;
            geometry.AddVertex(topLeft, normal, tangent, texCoords0.TopLeft, texCoords1.TopLeft, texCoords2.TopLeft, texCoords3.TopLeft, colour);
            geometry.AddVertex(topRight, normal, tangent, texCoords0.TopRight, texCoords1.TopRight, texCoords2.TopRight, texCoords3.TopRight, colour);
            geometry.AddVertex(bottomLeft, normal, tangent, texCoords0.BottomLeft, texCoords1.BottomLeft, texCoords2.BottomLeft, texCoords3.BottomLeft, colour);
            geometry.AddVertex(bottomRight, normal, tangent, texCoords0.BottomRight, texCoords1.BottomRight, texCoords2.BottomRight, texCoords3.BottomRight, colour);
            geometry.AddIndex(firstVertex + 2);
            geometry.AddIndex(firstVertex + 1);
            geometry.AddIndex(firstVertex + 0);
            geometry.AddIndex(firstVertex + 1);
            geometry.AddIndex(firstVertex + 2);
            geometry.AddIndex(firstVertex + 3);
        }

        public static void AddGeometry(
            this Geometry geometry,
            Geometry otherGeometry,
            ref Matrix4 transform,
            Quad diffuseTextureArea,
            Quad specularTextureArea,
            Quad normalTextureArea,
            Quad emissiveTextureArea
        )
        {
            geometry.AddGeometry(otherGeometry, ref transform, diffuseTextureArea, specularTextureArea, normalTextureArea, emissiveTextureArea, false, false, false, false, false, false);
        }

        public static void AddGeometry(
            this Geometry geometry,
            Geometry otherGeometry,
            ref Matrix4 transform,
            Quad diffuseTextureArea,
            Quad specularTextureArea,
            Quad normalTextureArea,
            Quad emissiveTextureArea,
            bool cullLeft,
            bool cullRight,
            bool cullTop,
            bool cullBottom,
            bool cullFront,
            bool cullBack
        )
        {
            // Add or cull all the indices
            int firstVertex = geometry.VertexCount;
            for (int i = 0; i < otherGeometry.IndexCount; i += 3)
            {
                int index0 = otherGeometry.GetIndex(i);
                int index1 = otherGeometry.GetIndex(i + 1);
                int index2 = otherGeometry.GetIndex(i + 2);

                Vector3 pos0, pos1, pos2;
                otherGeometry.GetVertexPosition(index0, out pos0);
                otherGeometry.GetVertexPosition(index1, out pos1);
                otherGeometry.GetVertexPosition(index2, out pos2);
                if (cullRight && pos0.X <= 0.0f && pos1.X <= 0.0f && pos2.X <= 0.0f)
                {
                    continue;
                }
                if (cullLeft && pos0.X >= 1.0f && pos1.X >= 1.0f && pos2.X >= 1.0f)
                {
                    continue;
                }
                if (cullBottom && pos0.Y <= 0.0f && pos1.Y <= 0.0f && pos2.Y <= 0.0f)
                {
                    continue;
                }
                if (cullTop && pos0.Y >= 0.5f && pos1.Y >= 0.5f && pos2.Y >= 0.5f)
                {
                    continue;
                }
                if (cullBack && pos0.Z <= 0.0f && pos1.Z <= 0.0f && pos2.Z <= 0.0f)
                {
                    continue;
                }
                if (cullFront && pos0.Z >= 1.0f && pos1.Z >= 1.0f && pos2.Z >= 1.0f)
                {
                    continue;
                }

                geometry.AddIndex(index0 + firstVertex);
                geometry.AddIndex(index1 + firstVertex);
                geometry.AddIndex(index2 + firstVertex);
            }

            // Add and transform all the verts
            var transformInv = MathUtils.FastInverted(transform);
            for (int i = 0; i < otherGeometry.VertexCount; ++i)
            {
                Vector3 position, normal, tangent;
                Vector2 texCoord0, texCoord1, texCoord2, texCoord3;
                Vector4 colour;
                otherGeometry.GetVertex(i, out position, out normal, out tangent, out texCoord0, out texCoord1, out texCoord2, out texCoord3, out colour);

                Vector3 transformedPos;
                Vector3.TransformPosition(ref position, ref transform, out transformedPos);

                Vector3 transformedNormal;
                Vector3.TransformNormalInverse(ref normal, ref transformInv, out transformedNormal);

                Vector3 transformedTangent;
                Vector3.TransformNormalInverse(ref tangent, ref transformInv, out transformedTangent);

                geometry.AddVertex(
                    transformedPos,
                    transformedNormal,
                    transformedTangent,
                    diffuseTextureArea.Interpolate(texCoord0.X, texCoord0.Y),
                    specularTextureArea.Interpolate(texCoord1.X, texCoord1.Y),
                    normalTextureArea.Interpolate(texCoord2.X, texCoord2.Y),
                    emissiveTextureArea.Interpolate(texCoord3.X, texCoord3.Y),
                    colour
                );
            }
        }

        public static void AddShadowGeometry(
            this Geometry geometry,
            Geometry otherGeometry,
            ref Matrix4 transform
        )
        {
            geometry.AddShadowGeometry(otherGeometry, ref transform, false, false, false, false, false, false);
        }

        public static void AddShadowGeometry(
            this Geometry geometry,
            Geometry otherGeometry,
            ref Matrix4 transform,
            bool cullLeft,
            bool cullRight,
            bool cullTop,
            bool cullBottom,
            bool cullFront,
            bool cullBack
        )
        {
            // For each triangle:
            var transformInv = MathUtils.FastInverted(transform);
            for (int i = 0; i < otherGeometry.IndexCount; i += 3)
            {
                // Get positions
                int index0 = otherGeometry.GetIndex(i);
                int index1 = otherGeometry.GetIndex(i + 1);
                int index2 = otherGeometry.GetIndex(i + 2);

                Vector3 pos0, pos1, pos2;
                otherGeometry.GetVertexPosition(index0, out pos0);
                otherGeometry.GetVertexPosition(index1, out pos1);
                otherGeometry.GetVertexPosition(index2, out pos2);

                // Perform culling
                if (cullRight && pos0.X <= 0.0f && pos1.X <= 0.0f && pos2.X <= 0.0f)
                {
                    continue;
                }
                if (cullLeft && pos0.X >= 1.0f && pos1.X >= 1.0f && pos2.X >= 1.0f)
                {
                    continue;
                }
                if (cullBottom && pos0.Y <= 0.0f && pos1.Y <= 0.0f && pos2.Y <= 0.0f)
                {
                    continue;
                }
                if (cullTop && pos0.Y >= 0.5f && pos1.Y >= 0.5f && pos2.Y >= 0.5f)
                {
                    continue;
                }
                if (cullBack && pos0.Z <= 0.0f && pos1.Z <= 0.0f && pos2.Z <= 0.0f)
                {
                    continue;
                }
                if (cullFront && pos0.Z >= 1.0f && pos1.Z >= 1.0f && pos2.Z >= 1.0f)
                {
                    continue;
                }

                // Transform to world space
                var faceNormal = Vector3.Cross(pos1 - pos2, pos1 - pos0);
                var faceTangent = pos1 - pos0;

                Vector3 faceNormalWorld;
                Vector3.TransformNormalInverse(ref faceNormal, ref transformInv, out faceNormalWorld);

                Vector3 faceTangentWorld;
                Vector3.TransformNormalInverse(ref faceTangent, ref transformInv, out faceTangentWorld);

                Vector3 pos0World, pos1World, pos2World;
                Vector3.TransformPosition(ref pos0, ref transform, out pos0World);
                Vector3.TransformPosition(ref pos1, ref transform, out pos1World);
                Vector3.TransformPosition(ref pos2, ref transform, out pos2World);

                Vector3 pos0Pushed = pos0World;
                Vector3 pos1Pushed = pos1World;
                Vector3 pos2Pushed = pos2World;

                int firstVertex = geometry.VertexCount;
                geometry.AddVertex(pos2World, faceNormalWorld, faceTangentWorld, Vector2.Zero, Vector4.One);
                geometry.AddVertex(pos1World, faceNormalWorld, faceTangentWorld, Vector2.Zero, Vector4.One);
                geometry.AddVertex(pos0World, faceNormalWorld, faceTangentWorld, Vector2.Zero, Vector4.One);

                int firstPushedVertex = geometry.VertexCount;
                geometry.AddVertex(pos2Pushed, faceNormalWorld, faceTangentWorld, Vector2.One, Vector4.One);
                geometry.AddVertex(pos1Pushed, faceNormalWorld, faceTangentWorld, Vector2.One, Vector4.One);
                geometry.AddVertex(pos0Pushed, faceNormalWorld, faceTangentWorld, Vector2.One, Vector4.One);

                // Generate the shadow volume
                // Start cap:
                geometry.AddIndex(firstVertex + 1);
                geometry.AddIndex(firstVertex + 2);
                geometry.AddIndex(firstVertex);

                // End cap:
                geometry.AddIndex(firstPushedVertex + 2);
                geometry.AddIndex(firstPushedVertex + 1);
                geometry.AddIndex(firstPushedVertex);

                // Sides:
                geometry.AddIndex(firstPushedVertex + 0);
                geometry.AddIndex(firstVertex + 1);
                geometry.AddIndex(firstVertex + 0);
                geometry.AddIndex(firstVertex + 1);
                geometry.AddIndex(firstPushedVertex + 0);
                geometry.AddIndex(firstPushedVertex + 1);

                geometry.AddIndex(firstPushedVertex + 1);
                geometry.AddIndex(firstVertex + 2);
                geometry.AddIndex(firstVertex + 1);
                geometry.AddIndex(firstVertex + 2);
                geometry.AddIndex(firstPushedVertex + 1);
                geometry.AddIndex(firstPushedVertex + 2);

                geometry.AddIndex(firstPushedVertex + 2);
                geometry.AddIndex(firstVertex + 0);
                geometry.AddIndex(firstVertex + 2);
                geometry.AddIndex(firstVertex + 0);
                geometry.AddIndex(firstPushedVertex + 2);
                geometry.AddIndex(firstPushedVertex + 0);
            }
        }

        public static void AddShellGeometry(
            this Geometry geometry,
            Geometry otherGeometry,
            float push
        )
        {
            // Extrude all the verts
            int firstVertex = geometry.VertexCount;
            for (int i = 0; i < otherGeometry.VertexCount; ++i)
            {
                Vector3 position;
                Vector3 normal;
                Vector3 tangent;
                Vector2 texCoord0;
                Vector2 texCoord1;
                Vector2 texCoord2;
                Vector2 texCoord3;
                Vector4 colour;
                otherGeometry.GetVertex(i, out position, out normal, out tangent, out texCoord0, out texCoord1, out texCoord2, out texCoord3, out colour);
                position += push * normal;
                normal = -normal;
                geometry.AddVertex(position, normal, tangent, texCoord0, texCoord1, texCoord2, texCoord3, colour);
            }

            // Invert all the faces
            for (int i = 0; i < otherGeometry.IndexCount; i += 3)
            {
                var i0 = otherGeometry.GetIndex(i);
                var i1 = otherGeometry.GetIndex(i + 1);
                var i2 = otherGeometry.GetIndex(i + 2);
                geometry.AddIndex(firstVertex + i0);
                geometry.AddIndex(firstVertex + i2);
                geometry.AddIndex(firstVertex + i1);
            }
        }

        public static Geometry ToWireframe(
            this Geometry geometry,
            bool lines = true, bool normals = false, bool tangents = false, bool binormals = false
        )
        {
            if (geometry.PrimitiveType == Primitive.Lines)
            {
                return geometry;
            }

            var result = new Geometry(Primitive.Lines);

            if (lines)
            {
                // Add triangles
                for (int i = 0; i < geometry.VertexCount; i++)
                {
                    Vector3 p1, n1, t1;
                    geometry.GetVertex(i, out p1, out n1, out t1);
                    result.AddVertex(p1, n1, t1, Vector2.Zero, Vector4.One);
                }

                for (int i = 0; i < geometry.IndexCount; i += 3)
                {
                    int index0 = geometry.GetIndex(i);
                    int index1 = geometry.GetIndex(i + 1);
                    int index2 = geometry.GetIndex(i + 2);
                    result.AddIndex(index0);
                    result.AddIndex(index1);
                    result.AddIndex(index1);
                    result.AddIndex(index2);
                    result.AddIndex(index2);
                    result.AddIndex(index0);
                }
            }

            // Add normals and tangents
            for (int i = 0; i < geometry.VertexCount; i++)
            {
                Vector3 p1, n1, t1;
                geometry.GetVertex(i, out p1, out n1, out t1);

                if (tangents)
                {
                    result.AddVertex(p1, n1, t1, Vector2.Zero, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    result.AddVertex(p1 + 0.3f * t1, n1, t1, Vector2.Zero, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    result.AddIndex(result.VertexCount - 2);
                    result.AddIndex(result.VertexCount - 1);
                }

                if (normals)
                {
                    result.AddVertex(p1, n1, t1, Vector2.Zero, new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
                    result.AddVertex(p1 + 0.3f * n1, n1, t1, Vector2.Zero, new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
                    result.AddIndex(result.VertexCount - 2);
                    result.AddIndex(result.VertexCount - 1);
                }

                if (binormals)
                {
                    var b1 = Vector3.Cross(t1, n1);
                    result.AddVertex(p1, n1, t1, Vector2.Zero, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                    result.AddVertex(p1 + 0.3f * b1, n1, t1, Vector2.Zero, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                    result.AddIndex(result.VertexCount - 2);
                    result.AddIndex(result.VertexCount - 1);
                }
            }

            result.Rebuild();
            return result;
        }

        public static void ExportToOBJ(
            this Geometry geometry,
            string name,
            TextWriter writer
        )
        {
            writer.WriteLine("# OBJ File");
            writer.WriteLine("mtllib " + name + ".mtl");
            writer.WriteLine("o " + name);
            for (int i = 0; i < geometry.VertexCount; ++i)
            {
                Vector3 position, normal, tangent;
                geometry.GetVertex(i, out position, out normal, out tangent);
                writer.WriteLine("v " + position.X + " " + position.Y + " " + position.Z);
            }
            for (int i = 0; i < geometry.VertexCount; ++i)
            {
                Vector3 position, normal, tangent;
                Vector2 texCoord0, texCoord1, texCoord2, texCoord3;
                Vector4 colour;
                geometry.GetVertex(i, out position, out normal, out tangent, out texCoord0, out texCoord1, out texCoord2, out texCoord3, out colour);
                writer.WriteLine("vt " + texCoord0.X + " " + (1.0f - texCoord0.Y));
            }
            writer.WriteLine("usemtl " + name);
            writer.WriteLine("s off");
            int stride = geometry.PrimitiveType.GetVertexCount();
            for (int i = 0; i < geometry.IndexCount; i += stride)
            {
                writer.Write("f ");
                for (int j = 0; j < stride; ++j)
                {
                    int vert = geometry.GetIndex(i + j);
                    writer.Write((vert + 1) + "/" + (vert + 1));
                    if (j < stride - 1)
                    {
                        writer.Write(" ");
                    }
                }
                writer.WriteLine();
            }
        }
    }
}

