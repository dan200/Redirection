using Dan200.Core.Assets;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Render
{
    public class MaterialFile : IBasicAsset
    {
        public static MaterialFile Get(string path)
        {
            return Assets.Assets.Get<MaterialFile>(path);
        }

        private string m_path;
        private Dictionary<string, Material> m_materials;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public MaterialFile(string path, IFileStore store)
        {
            m_path = path;
            m_materials = new Dictionary<string, Material>();
            Load(store);
        }

        public Material GetMaterial(string name)
        {
            Material result;
            if (m_materials.TryGetValue(name, out result))
            {
                return result;
            }
            if (m_materials.TryGetValue("default", out result))
            {
                return result;
            }
            return null;
        }

        public void Reload(IFileStore store)
        {
            m_materials.Clear();
            Load(store);
        }

        public void Dispose()
        {
        }

        private void Load(IFileStore store)
        {
            // Load the material infos
            var dir = AssetPath.GetDirectoryName(m_path);
            Material currentMaterial = null;
            using (var reader = store.OpenTextFile(m_path))
            {
                // For each line:
                string line;
                var whitespace = new char[] { ' ', '\t' };
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
                        case "newmtl":
                            {
                                var name = parts[1];
                                currentMaterial = new Material();
                                m_materials.Add(name, currentMaterial);
                                break;
                            }
                        case "map_ka":
                            {
                                var path = AssetPath.Combine(dir, parts[1]);
                                currentMaterial.EmissiveColour = Vector3.One;
                                currentMaterial.EmissiveTexture = path;
                                break;
                            }
                        case "map_kd":
                            {
                                var path = AssetPath.Combine(dir, parts[1]);
                                currentMaterial.DiffuseColour = Vector4.One;
                                currentMaterial.DiffuseTexture = path;
                                break;
                            }
                        case "map_ks":
                            {
                                var path = AssetPath.Combine(dir, parts[1]);
                                currentMaterial.SpecularColour = Vector3.One;
                                currentMaterial.SpecularTexture = path;
                                break;
                            }
                        case "map_bump":
                        case "bump":
                            {
                                var path = AssetPath.Combine(dir, parts[1]);
                                currentMaterial.NormalTexture = path;
                                break;
                            }
                    }
                }
            }

            // Add missing material paths
            foreach (var material in m_materials.Values)
            {
                if (material.EmissiveTexture == null)
                {
                    material.EmissiveColour = Vector3.One;
                    material.EmissiveTexture = "black.png";
                    if (material.DiffuseTexture != null)
                    {
                        var potentialPath = AssetPath.ChangeExtension(material.DiffuseTexture, "emit.png");
                        if (store.FileExists(potentialPath))
                        {
                            material.EmissiveTexture = potentialPath;
                        }
                    }
                }

                if (material.SpecularTexture == null)
                {
                    material.SpecularColour = Vector3.One;
                    material.SpecularTexture = "black.png";
                    if (material.DiffuseTexture != null)
                    {
                        var potentialPath = AssetPath.ChangeExtension(material.DiffuseTexture, "spec.png");
                        if (store.FileExists(potentialPath))
                        {
                            material.SpecularTexture = potentialPath;
                        }
                    }
                }

                if (material.NormalTexture == null)
                {
                    material.NormalTexture = "flat.png";
                    if (material.DiffuseTexture != null)
                    {
                        var potentialPath = AssetPath.ChangeExtension(material.DiffuseTexture, "norm.png");
                        if (store.FileExists(potentialPath))
                        {
                            material.NormalTexture = potentialPath;
                        }
                    }
                }

                if (material.DiffuseTexture == null)
                {
                    material.DiffuseColour = Vector4.One;
                    material.DiffuseTexture = "white.png";
                }
            }
        }
    }
}
