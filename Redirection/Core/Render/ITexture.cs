namespace Dan200.Core.Render
{
    public interface ITexture
    {
        int GLTexture { get; }
        int Width { get; }
        int Height { get; }
    }
}

