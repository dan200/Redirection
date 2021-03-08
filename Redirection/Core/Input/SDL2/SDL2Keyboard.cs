using Dan200.Core.Util;
using Dan200.Core.Window.SDL2;
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dan200.Core.Input.SDL2
{
    public class SDL2Keyboard : IKeyboard
    {
        private SDL2Window m_window;
        private bool m_hadFocus;

        private Dictionary<Key, IButton> m_keys;
        private IReadOnlyDictionary<Key, IButton> m_keysReadOnly;

        private string m_text;
        private StringBuilder m_pendingText;

        public IReadOnlyDictionary<Key, IButton> Keys
        {
            get
            {
                return m_keysReadOnly;
            }
        }

        public string Text
        {
            get
            {
                return m_text;
            }
        }

        public SDL2Keyboard(SDL2Window window)
        {
            m_window = window;
            m_keys = new Dictionary<Key, IButton>();
            m_keysReadOnly = m_keys.ToReadOnly();
            m_text = "";
            m_pendingText = new StringBuilder();
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key != Key.None)
                {
                    m_keys.Add(key, new SimpleButton());
                }
            }
            m_hadFocus = false;
            Update();
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    {
                        // Typed text
                        if (m_window.Focus)
                        {
                            byte[] bytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
                            int length = 0;
                            unsafe
                            {
                                fixed (byte* charPtr = e.text.text)
                                {
                                    for (int i = 0; i < bytes.Length; ++i)
                                    {
                                        bytes[i] = charPtr[i];
                                        if (bytes[i] == 0)
                                        {
                                            length = i;
                                            break;
                                        }
                                    }
                                }
                            }
                            foreach (char c in Encoding.UTF8.GetString(bytes, 0, length))
                            {
                                if (!Char.IsControl(c))
                                {
                                    m_pendingText.Append(c);
                                }
                            }
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    {
                        if (m_window.Focus)
                        {
                            if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_v)
                            {
                                // Pasted text
                                SDL.SDL_Keymod pasteModifier = (SDL.SDL_GetPlatform() == "Mac OS X") ?
                                    SDL.SDL_Keymod.KMOD_GUI :
                                    SDL.SDL_Keymod.KMOD_CTRL;

                                if (((int)e.key.keysym.mod & (int)pasteModifier) != 0)
                                {
                                    m_pendingText.Append(SDL.SDL_GetClipboardText());
                                }
                            }
                            if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_BACKSPACE)
                            {
                                // Backspace
                                m_pendingText.Append('\b');
                            }
                            if (e.key.repeat != 0)
                            {
                                // Key repeats
                                var keycode = e.key.keysym.sym;
                                var key = (Key)keycode;
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var button = (SimpleButton)m_keys[key];
                                    button.Repeat();
                                }
                            }
                        }
                        break;
                    }
            }
        }

        public void Update()
        {
            // Do focus stuff
            bool focus = m_window.Focus;
            bool newlyGainedFocus = false;
            if (focus)
            {
                newlyGainedFocus = !m_hadFocus;
                m_hadFocus = true;
            }
            else
            {
                newlyGainedFocus = false;
                m_hadFocus = false;
            }

            // Get keys
            int numKeys;
            IntPtr state = SDL.SDL_GetKeyboardState(out numKeys);
            byte[] stateBytes = new byte[numKeys];
            Marshal.Copy(state, stateBytes, 0, numKeys);
            foreach (Key key in m_keys.Keys)
            {
                SDL.SDL_Keycode keycode = (SDL.SDL_Keycode)key;
                SDL.SDL_Scancode scancode = SDL.SDL_GetScancodeFromKey(keycode);
                int scancodeInt = (int)scancode;
                if (scancodeInt >= 0 && scancodeInt < numKeys)
                {
                    var simpleButton = (SimpleButton)m_keys[key];
                    bool presed = (stateBytes[scancodeInt] != 0);
                    if (newlyGainedFocus)
                    {
                        simpleButton.IgnoreCurrentPress();
                    }
                    simpleButton.Update(
                        focus && presed
                    );
                }
            }

            // Get text
            m_text = m_pendingText.ToString();
            m_pendingText.Clear();
        }
    }
}

