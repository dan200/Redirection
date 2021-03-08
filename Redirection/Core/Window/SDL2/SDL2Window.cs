using System;
using OpenTK.Graphics;
using SDL2;
using Dan200.Core.Render;

#if OPENGLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

using Dan200.Core.Main;

namespace Dan200.Core.Window.SDL2
{
    public class SDL2Window : IWindow, IDisposable
    {
        private IntPtr m_window;
        private IntPtr m_openGLContext;
        private bool m_vsync;

        private int m_width;
        private int m_height;
        private bool m_closed;

        public string Title
        {
            get
            {
                return SDL.SDL_GetWindowTitle(m_window);
            }
            set
            {
                SDL.SDL_SetWindowTitle(m_window, value);
            }
        }

        public int Width
        {
            get
            {
                return m_width;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public bool Closed
        {
            get
            {
                return m_closed;
            }
        }

        public bool Fullscreen
        {
            get
            {
                uint flags = SDL.SDL_GetWindowFlags(m_window);
                return (flags &
                    ((uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN | (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP)
                ) != 0;
            }
            set
            {
                App.Log(value ? "Entering fullscreen" : "Exiting fullscreen");
                App.CheckSDLResult("SDL_SetWindowFullscreen", SDL.SDL_SetWindowFullscreen(m_window, value ?
                    (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 0
                ));
            }
        }

        public bool Maximised
        {
            get
            {
                uint flags = SDL.SDL_GetWindowFlags(m_window);
                return (flags & (uint)SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
            }
        }

        public bool VSync
        {
            get
            {
                return m_vsync;
            }
            set
            {
                if (m_vsync != value)
                {
                    m_vsync = value;
                    VSyncChanged();
                }
            }
        }

        public bool Focus
        {
            get
            {
                return (SDL.SDL_GetKeyboardFocus() == m_window);
            }
        }

        public bool MouseFocus
        {
            get
            {
                return (SDL.SDL_GetMouseFocus() == m_window && SDL.SDL_GetKeyboardFocus() == m_window);
            }
        }

        public event EventHandler OnClosed;
        public event EventHandler OnResized;

        public SDL2Window(string title, int width, int height, bool fullscreen, bool maximised, bool vsync)
        {
            // Setup window
            if (fullscreen)
            {
                App.Log("Creating fullscreen window");
            }
            else
            {
                App.Log("Creating window sized {0}x{1}", width, height);
            }
            m_window = SDL.SDL_CreateWindow(
                title,
                SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                width, height,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN |
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                (fullscreen ? SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 0) |
                (maximised ? SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED : 0)
            );
            App.CheckSDLResult("SDL_CreateWindow", m_window);

            // Create OpenGL context
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 2);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            m_openGLContext = SDL.SDL_GL_CreateContext(m_window);
            App.CheckSDLResult("SDL_GL_CreateContext", m_openGLContext);

            // Prepare OpenGL for use
            GraphicsContext.CurrentContext = m_openGLContext;
            GL.LoadAll( SDL.SDL_GL_GetProcAddress );
            SDL.SDL_ClearError(); // For some reason an SDL2 error is raised during GL.LoadAll()
            MakeCurrent();

            // Get OpenGL info
            App.Log("GL Vendor: " + GL.GetString(StringName.Vendor));
            App.Log("GL Version: " + GL.GetString(StringName.Version));
            App.Log("GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));

            // Cache some things
            SDL.SDL_GetWindowSize(m_window, out m_width, out m_height);
            m_closed = false;

            // Configure the new context
            m_vsync = vsync;
            VSyncChanged();
        }

        private void VSyncChanged()
        {
            if (m_vsync)
            {
                if (SDL.SDL_GL_SetSwapInterval(-1) < 0)
                {
                    App.CheckSDLResult("SDL_GL_SetSwapInterval", SDL.SDL_GL_SetSwapInterval(1));
                    App.Log("VSync enabled");
                }
                else
                {
                    App.Log("Adaptive VSync enabled");
                }
            }
            else
            {
                App.CheckSDLResult("SDL_GL_SetSwapInterval", SDL.SDL_GL_SetSwapInterval(0));
                App.Log("VSync disabled");
            }
        }

        public void Dispose()
        {
            Close();
            SDL.SDL_DestroyWindow(m_window);
        }

        public void SetIcon(Bitmap bitmap)
        {
            SDL.SDL_SetWindowIcon(m_window, bitmap.SDLSurface);
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    {
                        if (e.window.windowID == SDL.SDL_GetWindowID(m_window))
                        {
                            switch (e.window.windowEvent)
                            {
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                                    {
                                        Resize();
                                        break;
                                    }
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    {
                                        Close();
                                        break;
                                    }
                            }
                        }
                        break;
                    }
            }
        }

        public void MakeCurrent()
        {
            App.CheckSDLResult("SDL_GL_MakeCurrent", SDL.SDL_GL_MakeCurrent(m_window, m_openGLContext));
        }

        public void SwapBuffers()
        {
            GL.Flush();
            SDL.SDL_GL_SwapWindow(m_window);
        }

        private void Resize()
        {
            SDL.SDL_GetWindowSize(m_window, out m_width, out m_height);
            OnResized.Invoke(this, new EventArgs());
        }

        private void Close()
        {
            m_closed = true;
            OnClosed.Invoke(this, new EventArgs());
        }
    }
}
