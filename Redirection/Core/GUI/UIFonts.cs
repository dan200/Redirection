using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    public class UIFonts
    {
        private static string s_fontOverride = null;
        private static string s_fontName = "fonts/bankgothic64.fnt";
        private static string s_smallerFontName = "fonts/bankgothic48.fnt";
        private static string s_smallestFontName = "fonts/bankgothic32.fnt";
        private static string s_biggerFontName = "fonts/bankgothic96.fnt";

        public static string FontOverride
        {
            get
            {
                return s_fontOverride;
            }
            set
            {
                s_fontOverride = value;
                var fontName = (s_fontOverride != null) ? s_fontOverride : "bankgothic";
                s_fontName = string.Format("fonts/{0}64.fnt", fontName);
                s_smallerFontName = string.Format("fonts/{0}48.fnt", fontName);
                s_smallestFontName = string.Format("fonts/{0}32.fnt", fontName);
                s_biggerFontName = string.Format("fonts/{0}96.fnt", fontName);
            }
        }

        public static Font Default
        {
            get
            {
                return Font.Get(s_fontName);
            }
        }

        public static Font Smaller
        {
            get
            {
                return Font.Get(s_smallerFontName);
            }
        }

        public static Font Smallest
        {
            get
            {
                return Font.Get(s_smallestFontName);
            }
        }

        public static Font Bigger
        {
            get
            {
                return Font.Get(s_biggerFontName);
            }
        }
    }
}

