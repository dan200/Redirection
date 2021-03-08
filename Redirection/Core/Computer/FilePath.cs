using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Dan200.Core.Computer
{
    public struct FilePath
    {
        public static FilePath Empty = new FilePath(null);

        public static FilePath Combine(FilePath a, string b)
        {
            return Combine(a, new FilePath(b));
        }

        public static FilePath Combine(FilePath a, FilePath b)
        {
            if (b.IsRooted)
            {
                return b;
            }
            else if (a.IsRoot)
            {
                return b.Root();
            }
            else if (a.IsEmpty)
            {
                return b;
            }
            else if (b.IsEmpty)
            {
                return a;
            }
            else if (b.IsBackPath)
            {
                return new FilePath(a.Path + '/' + b.Path);
            }
            else
            {
                return ConstructUnsafe(a.Path + '/' + b.Path);
            }
        }

        private string m_path;

        public string Path
        {
            get
            {
                return (m_path != null) ? m_path : "";
            }
        }

        public bool IsRoot
        {
            get
            {
                return Path.Equals("/", StringComparison.InvariantCulture);
            }
        }

        public bool IsRooted
        {
            get
            {
                return Path.StartsWith("/", StringComparison.InvariantCulture);
            }
        }

        public bool IsEmpty
        {
            get
            {
                return Path.Length == 0;
            }
        }

        public bool IsBackPath
        {
            get
            {
                return
                    Path.Equals("..", StringComparison.InvariantCulture) ||
                    Path.Equals("/..", StringComparison.InvariantCulture) ||
                    Path.StartsWith("../", StringComparison.InvariantCulture) ||
                    Path.StartsWith("/../", StringComparison.InvariantCulture);
            }
        }

        public FilePath(string path)
        {
            if (path != null)
            {
                m_path = Sanitize(path);
            }
            else
            {
                m_path = null;
            }
        }

        private static FilePath ConstructUnsafe(string path)
        {
            FilePath result;
            result.m_path = path;
            return result;
        }

        public FilePath Root()
        {
            if (Path.StartsWith("/", StringComparison.InvariantCulture))
            {
                return this;
            }
            else
            {
                return ConstructUnsafe('/' + Path);
            }
        }

        public FilePath UnRoot()
        {
            if (Path.StartsWith("/", StringComparison.InvariantCulture))
            {
                return ConstructUnsafe(Path.Substring(1));
            }
            else
            {
                return this;
            }
        }

        public string GetName()
        {
            int lastSlash = Path.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                return Path.Substring(lastSlash + 1);
            }
            else
            {
                return m_path;
            }
        }

        public FilePath GetDir()
        {
            int lastSlash = Path.LastIndexOf('/');
            if (lastSlash == 0)
            {
                return ConstructUnsafe("/"); // Path is rooted
            }
            else if (lastSlash >= 0)
            {
                return ConstructUnsafe(Path.Substring(0, lastSlash));
            }
            else
            {
                return FilePath.Empty;
            }
        }

        public string GetExtension()
        {
            string name = GetName();
            int lastDot = name.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return name.Substring(lastDot + 1);
            }
            else
            {
                return "";
            }
        }

        public string GetNameWithoutExtension()
        {
            string name = GetName();
            int lastDot = name.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return name.Substring(0, lastDot);
            }
            else
            {
                return name;
            }
        }

        public bool Matches(FilePath wildcard)
        {
            var dir = GetDir();
            var otherDir = wildcard.GetDir();
            if (dir == otherDir)
            {
                var name = GetName();
                var namePattern = wildcard.GetName();
                var nameRegex = new Regex("^" + Regex.Escape(namePattern).Replace("\\*", ".*") + "$");
                return nameRegex.IsMatch(name);
            }
            return false;
        }

        public bool IsParentOf(FilePath other)
        {
            if (other.IsBackPath || this.IsRooted != other.IsRooted)
            {
                return false;
            }

            if (this == other)
            {
                return true;
            }
            else if (this.IsEmpty || this.IsRoot)
            {
                return true;
            }
            else if (other.Path.StartsWith(this.Path + '/', StringComparison.InvariantCulture))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public FilePath ToLocal(FilePath parent)
        {
            if (!parent.IsParentOf(this))
            {
                throw new InvalidOperationException();
            }
            var local = this.Path.Substring(parent.Path.Length);
            if (local.StartsWith("/", StringComparison.InvariantCulture))
            {
                return ConstructUnsafe(local.Substring(1));
            }
            else
            {
                return ConstructUnsafe(local);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is FilePath)
            {
                return Equals((FilePath)obj);
            }
            return false;
        }

        public bool Equals(FilePath o)
        {
            return o.Path.Equals(Path, StringComparison.InvariantCulture);
        }

        public static bool operator ==(FilePath a, FilePath b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FilePath a, FilePath b)
        {
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public override string ToString()
        {
            return Path;
        }

        private static char[] s_slashes = new char[] {
            '/', '\\'
        };

        private static string Sanitize(string path)
        {
            // Split the path
            bool rooted =
                path.StartsWith("/", StringComparison.InvariantCulture) ||
                path.StartsWith("\\", StringComparison.InvariantCulture);
            string[] parts = path.Split(s_slashes, StringSplitOptions.RemoveEmptyEntries);

            // Recombine with '.' and redundant '..' removed and slashes normalised
            int outputLength = 0;
            string[] outputParts = new string[parts.Length];
            for (int i = 0; i < parts.Length; ++i)
            {
                var part = parts[i];
                if (part == "." || path == "...")
                {
                    // "." is redundant
                    continue;
                }
                else if (part == "..")
                {
                    // ".." can cancel out the last folder entered
                    if (outputLength > 0)
                    {
                        var top = outputParts[outputLength - 1];
                        if (top != "..")
                        {
                            outputLength--;
                        }
                        else
                        {
                            outputParts[outputLength++] = "..";
                        }
                    }
                    else
                    {
                        outputParts[outputLength++] = "..";
                    }
                }
                else
                {
                    outputParts[outputLength++] = part;
                }
            }

            var resultBuilder = new StringBuilder();
            if (rooted)
            {
                resultBuilder.Append("/");
            }
            for (int i = 0; i < outputLength; ++i)
            {
                var part = outputParts[i];
                if (i > 0)
                {
                    resultBuilder.Append("/");
                }
                resultBuilder.Append(part);
            }
            return resultBuilder.ToString();
        }
    }
}

