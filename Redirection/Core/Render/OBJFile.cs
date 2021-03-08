using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Dan200.Core.Render
{
    public class OBJFile
    {
        public struct OBJVert
        {
            public int PositionIndex;
            public int TexCoordIndex;
            public int NormalIndex;
        }

        public struct OBJFace
        {
            public int FirstVertex;
            public int VertexCount;
        }

        public class OBJGroup
        {
            public string Name;
            public List<OBJFace> Faces;
            public string MaterialName;

            public OBJGroup(string name)
            {
                Name = name;
                Faces = new List<OBJFace>();
                MaterialName = null;
            }
        }

        public List<Vector3> Positions;
        public List<Vector4> Colours;
        public List<Vector2> TexCoords;
        public List<Vector3> Normals;
        public List<OBJVert> Verts;
        public List<OBJGroup> Groups;
        public string MTLLib;

        public OBJFile()
        {
            Positions = new List<Vector3>();
            Colours = new List<Vector4>();
            TexCoords = new List<Vector2>();
            Normals = new List<Vector3>();
            Verts = new List<OBJVert>();
            Groups = new List<OBJGroup>();
            MTLLib = null;
        }

        public void Parse(TextReader reader)
        {
            // For each line:
            string line;
            var whitespace = new char[] { ' ', '\t' };
            var slash = new char[] { '/' };
            OBJGroup currentGroup = null;
            while ((line = reader.ReadLine()) != null)
            {
                // Strip comment
                var commentIdx = line.IndexOf('#');
                if (commentIdx >= 0)
                {
                    line = line.Substring(0, commentIdx);
                }

                // Segment
                var parts = line.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }

                // Parse
                var type = parts[0].ToLowerInvariant();
                switch (type)
                {
                    case "mtllib":
                        {
                            var path = parts[1];
                            MTLLib = path;
                            break;
                        }
                    case "o":
                    case "g":
                        {
                            var name = parts[1];
                            currentGroup = new OBJGroup(name);
                            Groups.Add(currentGroup);
                            break;
                        }
                    case "v":
                        {
                            var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            var y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            var z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            Positions.Add(new Vector3(x, y, z));
                            if (parts.Length > 4)
                            {
                                var r = float.Parse(parts[4], CultureInfo.InvariantCulture);
                                var g = float.Parse(parts[5], CultureInfo.InvariantCulture);
                                var b = float.Parse(parts[6], CultureInfo.InvariantCulture);
                                var a = (parts.Length > 7) ?
                                    float.Parse(parts[7], CultureInfo.InvariantCulture) :
                                    1.0f;
                                Colours.Add(new Vector4(r, g, b, a));
                            }
                            else
                            {
                                Colours.Add(Vector4.One);
                            }
                            break;
                        }
                    case "vt":
                        {
                            var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            var y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            TexCoords.Add(new Vector2(x, y));
                            break;
                        }
                    case "vn":
                        {
                            var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            var y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            var z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            Normals.Add(new Vector3(x, y, z));
                            break;
                        }
                    case "usemtl":
                        {
                            var name = parts[1];
                            currentGroup.MaterialName = name;
                            break;
                        }
                    case "f":
                        {
                            var face = new OBJFace();
                            face.FirstVertex = Verts.Count;
                            for (int i = 1; i < parts.Length; ++i)
                            {
                                var part = parts[i];
                                var subParts = part.Split(slash, StringSplitOptions.None);
                                var vert = new OBJVert();
                                if (subParts.Length > 0)
                                {
                                    int posIndex;
                                    if (int.TryParse(subParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out posIndex))
                                    {
                                        vert.PositionIndex = posIndex;
                                    }
                                }
                                if (subParts.Length > 1)
                                {
                                    int texCoordIndex;
                                    if (int.TryParse(subParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out texCoordIndex))
                                    {
                                        vert.TexCoordIndex = texCoordIndex;
                                    }
                                }
                                if (subParts.Length > 2)
                                {
                                    int normalIndex;
                                    if (int.TryParse(subParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out normalIndex))
                                    {
                                        vert.NormalIndex = normalIndex;
                                    }
                                }
                                Verts.Add(vert);
                                face.VertexCount++;
                            }
                            currentGroup.Faces.Add(face);
                            break;
                        }
                }
            }
        }
    }
}
