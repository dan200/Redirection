namespace Dan200.Core.Render
{
    public enum Primitive
    {
        Lines = 0,
        Triangles
    }

    public static class PrimitiveExtensions
    {
        public static int GetVertexCount(this Primitive primitive)
        {
            return (int)primitive + 2;
        }
    }
}
