using System;
using System.Text.RegularExpressions;

namespace Dan200.Core.Assets
{
    public static class AssetPath
    {
        public static string ToSafeAssetName(this string str, bool lowercase)
        {
            var result = Regex.Replace(str, "[^a-zA-Z0-9_]+", "");
            if (lowercase)
            {
                return result.ToLowerInvariant();
            }
            return result;
        }

        public static string Combine(string path, string subPath)
        {
            while (subPath.StartsWith("../", StringComparison.InvariantCulture))
            {
                path = AssetPath.GetDirectoryName(path);
                subPath = subPath.Substring(3);
            }

            if (path.Length > 0 && subPath.Length > 0)
            {
                return path + '/' + subPath;
            }
            else if (path.Length > 0)
            {
                return path;
            }
            else
            {
                return subPath;
            }
        }

        public static string GetFileName(string path)
        {
            int slashIndex = path.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                return path.Substring(slashIndex + 1);
            }
            return path;
        }

        public static string GetDirectoryName(string path)
        {
            int slashIndex = path.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                return path.Substring(0, slashIndex);
            }
            return "";
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            string fileName = GetFileName(path);
            int dotIndex = fileName.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                return fileName.Substring(0, dotIndex);
            }
            return fileName;
        }

        public static string GetPathWithoutExtension(string path)
        {
            return Combine(GetDirectoryName(path), GetFileNameWithoutExtension(path));
        }

        public static string ChangeExtension(string path, string newExtension)
        {
            return Combine(GetDirectoryName(path), GetFileNameWithoutExtension(path) + "." + newExtension);
        }

        public static string GetExtension(string path)
        {
            string fileName = GetFileName(path);
            int dotIndex = fileName.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                return fileName.Substring(dotIndex + 1);
            }
            return "";
        }
    }
}

