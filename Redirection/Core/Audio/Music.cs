using Dan200.Core.Assets;

namespace Dan200.Core.Audio
{
    public abstract class Music : IBasicAsset
    {
        public static Music Get(string path)
        {
            return Assets.Assets.Get<Music>(path);
        }

        public abstract string Path { get; }
        public abstract float Duration { get; }
        public abstract void Reload(IFileStore store);
        public abstract void Dispose();
    }
}
