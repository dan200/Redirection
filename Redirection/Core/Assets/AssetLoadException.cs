using System;

namespace Dan200.Core.Assets
{
    public class AssetLoadException : Exception
    {
        public AssetLoadException(string assetPath, string cause) : base("Error loading asset " + assetPath + ": " + cause)
        {
        }

        public AssetLoadException(string assetPath, Exception cause) : base("Error loading asset " + assetPath + ": " + cause.Message, cause)
        {
        }
    }
}

