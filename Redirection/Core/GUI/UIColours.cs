using OpenTK;

namespace Dan200.Core.GUI
{
    public class UIColours
    {
        public static Vector4 White = Vector4.One;
        public static Vector4 Grey = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        public static Vector4 Red = new Vector4(0.88f, 0.3f, 0.3f, 1.0f);
        public static Vector4 Blue = new Vector4(0.23f, 0.39f, 0.71f, 1.0f);

        public static Vector4 Title = Blue;
        public static Vector4 Text = White;
        public static Vector4 Link = Blue;
        public static Vector4 Important = Red;
        public static Vector4 Hover = Red;
        public static Vector4 Disabled = Grey;
    }
}

