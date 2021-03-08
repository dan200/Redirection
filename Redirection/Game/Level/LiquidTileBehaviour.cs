using Dan200.Core.Assets;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "liquid")]
    public class LiquidTileBehaviour : TileBehaviour
    {
        private string m_texture;
        private float m_textureScale;
        private string m_animation;

        public string Texture
        {
            get
            {
                return m_texture;
            }
        }

        public string Animation
        {
            get
            {
                return m_animation;
            }
        }

        public LiquidTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_texture = kvp.GetString("texture");
            m_textureScale = kvp.GetFloat("texture_scale", 1.0f);
            m_animation = kvp.GetString("animation");
        }

        public override void OnRender(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
        }

        public override void OnRenderShadows(ILevel level, TileCoordinates coordinates, Geometry output)
        {
        }

        public override void OnRenderLiquid(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            // Render top
            var topLeft = new Vector3(coordinates.X, coordinates.Y * 0.5f + 0.5f, coordinates.Z);
            var topRight = new Vector3(coordinates.X + 1.0f, coordinates.Y * 0.5f + 0.5f, coordinates.Z);
            var bottomLeft = new Vector3(coordinates.X, coordinates.Y * 0.5f + 0.5f, coordinates.Z + 1.0f);
            var bottomRight = new Vector3(coordinates.X + 1.0f, coordinates.Y * 0.5f + 0.5f, coordinates.Z + 1.0f);
            output.AddQuad(
                topLeft,
                topRight,
                bottomLeft,
                bottomRight,
                new Quad(coordinates.X * m_textureScale, coordinates.Z * m_textureScale, m_textureScale, m_textureScale),
                Vector4.One
            );
            output.AddQuad(
                bottomLeft,
                bottomRight,
                topLeft,
                topRight,
                new Quad(coordinates.X * m_textureScale, -coordinates.Z * m_textureScale, m_textureScale, m_textureScale),
                Vector4.One
            );
        }
    }
}

