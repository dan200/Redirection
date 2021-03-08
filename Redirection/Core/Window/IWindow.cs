using Dan200.Core.Render;
using System;

namespace Dan200.Core.Window
{
    public interface IWindow
    {
        string Title { get; set; }
        int Width { get; }
        int Height { get; }
        bool Closed { get; }
        bool Fullscreen { get; set; }
        bool Maximised { get; }
        bool VSync { get; set; }
        bool Focus { get; }
        bool MouseFocus { get; }
        event EventHandler OnClosed;
        event EventHandler OnResized;
        void SetIcon(Bitmap bitmap);
    }
}

