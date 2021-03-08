using OpenTK;

namespace Dan200.Core.Render
{
    public class Material
    {
        public Vector3 EmissiveColour;
        public string EmissiveTexture;
        public Vector4 DiffuseColour;
        public string DiffuseTexture;
        public Vector3 SpecularColour;
        public string SpecularTexture;
        public string NormalTexture;
        public bool CastShadows;

        public Material()
        {
            EmissiveColour = Vector3.Zero;
            EmissiveTexture = null;
            DiffuseColour = Vector4.One;
            DiffuseTexture = null;
            SpecularColour = Vector3.Zero;
            SpecularTexture = null;
            NormalTexture = null;
        }
    }
}
