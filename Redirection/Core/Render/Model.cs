using Dan200.Core.Assets;
using Dan200.Core.Util;
using OpenTK;
using System.Collections.Generic;

namespace Dan200.Core.Render
{
    public class Model : IBasicAsset
    {
        public static Model Get(string path)
        {
            return Assets.Assets.Get<Model>(path);
        }

        private class Group
        {
            public string Name;
            public string MaterialName;
            public Geometry Geometry;
            public Geometry ShadowGeometry;
        }

        private string m_path;

        private string m_materialFile;
        private Dictionary<string, int> m_groupLookup;
        private List<Group> m_groups;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public int GroupCount
        {
            get
            {
                return m_groups.Count;
            }
        }

        public Model(string path, IFileStore store)
        {
            m_path = path;
            Load(store);
        }

        public int GetGroupIndex(string name)
        {
            if (m_groupLookup.ContainsKey(name))
            {
                return m_groupLookup[name];
            }
            return -1;
        }

        public string GetGroupName(int groupIndex)
        {
            return m_groups[groupIndex].Name;
        }

        public Geometry GetGroupGeometry(int groupIndex)
        {
            return m_groups[groupIndex].Geometry;
        }

        public Material GetGroupMaterial(int groupIndex)
        {
            var mtlFile = MaterialFile.Get(m_materialFile);
            var mtlName = m_groups[groupIndex].MaterialName;
            return mtlFile.GetMaterial(mtlName);
        }

        public void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Load(IFileStore store)
        {
            // Load
            var objFile = new OBJFile();
            using (var reader = store.OpenTextFile(m_path))
            {
                objFile.Parse(reader);
            }

            // Interpret
            var dir = AssetPath.GetDirectoryName(m_path);
            m_materialFile = AssetPath.Combine(dir, objFile.MTLLib);
            m_groups = new List<Group>();
            m_groupLookup = new Dictionary<string, int>();
            foreach (var objGroup in objFile.Groups)
            {
                // Skip empty groups
                if (objGroup.Faces.Count == 0)
                {
                    continue;
                }

                // Load geometry
                Geometry geometry = new Geometry(Primitive.Triangles);
                for (int i = 0; i < objGroup.Faces.Count; ++i)
                {
                    var objFace = objGroup.Faces[i];
                    if (objFace.VertexCount >= 3)
                    {
                        Vector3 posA, posB, posC;
                        Vector4 colourA, colourB, colourC;
                        Vector3? normA, normB, normC;
                        Vector2 texCoordA, texCoordB, texCoordC;
                        GetVertInfo(objFile, objFace.FirstVertex, out posA, out colourA, out texCoordA, out normA);
                        GetVertInfo(objFile, objFace.FirstVertex + 1, out posB, out colourB, out texCoordB, out normB);
                        GetVertInfo(objFile, objFace.FirstVertex + 2, out posC, out colourC, out texCoordC, out normC);

                        Vector3 faceNormal, faceTangent;
                        MathUtils.GenerateNormalAndTangent(
                            posA, posB, posC,
                            texCoordA, texCoordB, texCoordC,
                            out faceNormal, out faceTangent
                        );

                        // Add the verts
                        int firstVertIndex = geometry.VertexCount;
                        geometry.AddVertex(posA, normA.HasValue ? normA.Value : faceNormal, faceTangent, texCoordA, colourA);
                        geometry.AddVertex(posB, normB.HasValue ? normB.Value : faceNormal, faceTangent, texCoordB, colourB);
                        geometry.AddVertex(posC, normC.HasValue ? normC.Value : faceNormal, faceTangent, texCoordC, colourC);
                        for (int j = 3; j < objFace.VertexCount; ++j)
                        {
                            Vector3 posN;
                            Vector4 colourN;
                            Vector3? normN;
                            Vector2 texCoordN;
                            GetVertInfo(objFile, objFace.FirstVertex + j, out posN, out colourN, out texCoordN, out normN);
                            geometry.AddVertex(posN, normN.HasValue ? normN.Value : faceNormal, faceTangent, texCoordN, colourN);
                        }

                        // Add the indexes
                        for (int k = 2; k < objFace.VertexCount; ++k)
                        {
                            geometry.AddIndex(firstVertIndex);
                            geometry.AddIndex(firstVertIndex + k - 1);
                            geometry.AddIndex(firstVertIndex + k);
                        }
                    }
                }
                geometry.Rebuild();

                // Build shadow geometry
                var shadowGeometry = new Geometry(Primitive.Triangles);
                shadowGeometry.AddShadowGeometry(geometry, ref Matrix4.Identity);
                shadowGeometry.Rebuild();

                // Prepare the group
                var group = new Group();
                group.Name = objGroup.Name;
                group.MaterialName = objGroup.MaterialName;
                group.Geometry = geometry;
                group.ShadowGeometry = shadowGeometry;

                m_groupLookup.Add(group.Name, m_groups.Count);
                m_groups.Add(group);
            }
        }

        private void Unload()
        {
            for (int i = 0; i < m_groups.Count; ++i)
            {
                var group = m_groups[i];
                group.Geometry.Dispose();
                group.ShadowGeometry.Dispose();
            }
            m_groups = null;
        }

        private ITexture GetModelTexture(string path)
        {
            var tex = Texture.Get(path, false);
            tex.Wrap = true;
            return tex;
        }

        public void DrawGroup(ModelEffectInstance effect, int i, Matrix4 transform, Vector2 uvOffset, Vector2 uvScale, Vector4 colour, ITexture customEmissiveTexture = null)
        {
            var group = m_groups[i];
            var material = GetGroupMaterial(i);

            effect.ModelMatrix = transform;
            effect.UVOffset = uvOffset;
            effect.UVScale = uvScale;
            effect.DiffuseColour = material.DiffuseColour.Mul(colour);
            effect.DiffuseTexture = GetModelTexture(material.DiffuseTexture);
            effect.SpecularColour = material.SpecularColour;
            effect.SpecularTexture = GetModelTexture(material.SpecularTexture);
            effect.NormalTexture = GetModelTexture(material.NormalTexture);
            effect.EmissiveColour = material.EmissiveColour;
            if (customEmissiveTexture != null)
            {
                effect.EmissiveTexture = customEmissiveTexture;
            }
            else
            {
                effect.EmissiveTexture = GetModelTexture(material.EmissiveTexture);
            }
            effect.Bind();
            group.Geometry.Draw();

            /*
            // Wireframe test code
            effect.ModelMatrix = transform;
            effect.DiffuseColour = Vector4.One;
            effect.DiffuseTexture = Texture.White;
            effect.SpecularColour = Vector3.One;
            effect.SpecularTexture = Texture.White;
            effect.NormalTexture = Texture.Flat;
            effect.Bind();
            using( var wireframe = group.Geometry.ToWireframe( lines:false, normals:true, tangents:true, binormals:true ) )
            {
                wireframe.Draw();
            }
            */
        }

        public void Draw(ModelEffectInstance effect, Matrix4[] transforms, bool[] visibility, Vector2[] uvOffset, Vector2[] uvScale, Vector4[] colour)
        {
            for (int i = 0; i < m_groups.Count; ++i)
            {
                if (visibility[i])
                {
                    DrawGroup(effect, i, transforms[i], uvOffset[i], uvScale[i], colour[i]);
                }
            }
        }

        public void DrawShadows(ShadowEffectInstance effect, Matrix4[] transforms, bool[] visibility)
        {
            for (int i = 0; i < m_groups.Count; ++i)
            {
                var group = m_groups[i];
                if (visibility[i])
                {
                    effect.ModelMatrix = transforms[i];
                    effect.Bind();
                    group.ShadowGeometry.Draw();
                }
            }
        }

        private void GetVertInfo(OBJFile file, int vertexIndex, out Vector3 o_pos, out Vector4 o_colour, out Vector2 o_texCoord, out Vector3? o_normal)
        {
            var vertex = file.Verts[vertexIndex];
            var objPos = file.Positions[vertex.PositionIndex - 1];
            o_pos = objPos;

            var objColour = file.Colours[vertex.PositionIndex - 1];
            o_colour = objColour;

            if (vertex.TexCoordIndex > 0)
            {
                var objTexCoord = file.TexCoords[vertex.TexCoordIndex - 1];
                o_texCoord = new Vector2(objTexCoord.X, 1.0f - objTexCoord.Y);
            }
            else
            {
                o_texCoord = new Vector2(0.5f, 0.5f);
            }

            if (vertex.NormalIndex > 0)
            {
                var objNormal = file.Normals[vertex.NormalIndex - 1];
                o_normal = objNormal;
                o_normal.Value.Normalize();
            }
            else
            {
                o_normal = null;
            }
        }
    }
}
