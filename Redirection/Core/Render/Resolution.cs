namespace Dan200.Core.Render
{
    public struct Resolution
    {
        public static Resolution[] StandardResolutions = new Resolution[] {
            new Resolution( 854, 480 ),
            new Resolution( 1280, 720 ),
            new Resolution( 1600, 900 ),
            new Resolution( 1920, 1080 ),
            new Resolution( 2560, 1440 ),
            new Resolution( 4096, 2160 ),
        };

        private static string[] s_standardResolutionNames = new string[] {
            "480p",
            "720p",
            "900p",
            "1080p",
            "1440p",
            "2160p"
        };

        public string Name
        {
            get
            {
                for (int i = 0; i < StandardResolutions.Length; ++i)
                {
                    if (StandardResolutions[i].Equals(this))
                    {
                        return s_standardResolutionNames[i];
                    }
                }
                return string.Format("{0}x{1}", Width, Height);
            }
        }

        public readonly int Width;
        public readonly int Height;

        public Resolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override bool Equals(object other)
        {
            if (other != null && other is Resolution)
            {
                return Equals((Resolution)other);
            }
            return false;
        }

        public bool Equals(Resolution other)
        {
            return other.Width == Width && other.Height == Height;
        }

        public override int GetHashCode()
        {
            return ((Width << 16) + Height);
        }

        public static bool operator ==(Resolution a, Resolution b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Resolution a, Resolution b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

