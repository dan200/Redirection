using System;

namespace Dan200.Core.Assets
{
    public interface IAsset : IDisposable
    {
        string Path { get; }
    }
}
