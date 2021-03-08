using SDL2;
using System;

namespace Dan200.Core.Main
{
    public interface IGame : IDisposable
    {
        bool Over { get; }
        void HandleEvent(ref SDL.SDL_Event e);
        void Update(float dt);
        void Render();
    }
}
