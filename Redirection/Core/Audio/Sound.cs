using Dan200.Core.Assets;

namespace Dan200.Core.Audio
{
    public abstract class Sound : IBasicAsset
    {
        public static Sound Get(string path)
        {
            return Assets.Assets.Get<Sound>(path);
        }

        public abstract string Path { get; }
        public abstract float Duration { get; }
        public abstract void Reload(IFileStore store);
        public abstract void Dispose();
    }
}

