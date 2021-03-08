using Dan200.Core.Assets;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Level;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Game.User
{
    public class Settings
    {
        private KeyValuePairFile m_kvp;
        private Dictionary<Bind, Key> m_cachedKeyBinds;

        public bool EnableSound
        {
            get
            {
                return m_kvp.GetBool("audio.sound_enabled");
            }
            set
            {
                m_kvp.Set("audio.sound_enabled", value);
            }
        }

        public float SoundVolume
        {
            get
            {
                return m_kvp.GetFloat("audio.sound_volume");
            }
            set
            {
                m_kvp.Set("audio.sound_volume", value);
            }
        }

        public bool EnableMusic
        {
            get
            {
                return m_kvp.GetBool("audio.music_enabled");
            }
            set
            {
                m_kvp.Set("audio.music_enabled", value);
            }
        }

        public float MusicVolume
        {
            get
            {
                return m_kvp.GetFloat("audio.music_volume");
            }
            set
            {
                m_kvp.Set("audio.music_volume", value);
            }
        }

        public string Language
        {
            get
            {
                return m_kvp.GetString("text.language");
            }
            set
            {
                m_kvp.Set("text.language", value);
            }
        }

        public bool Fullscreen
        {
            get
            {
                return m_kvp.GetBool("video.fullscreen");
            }
            set
            {
                m_kvp.Set("video.fullscreen", value);
            }
        }

        public int FullscreenWidth
        {
            get
            {
                return m_kvp.GetInteger("video.fullscreen_width");
            }
            set
            {
                m_kvp.Set("video.fullscreen_width", value);
            }
        }

        public int FullscreenHeight
        {
            get
            {
                return m_kvp.GetInteger("video.fullscreen_height");
            }
            set
            {
                m_kvp.Set("video.fullscreen_height", value);
            }
        }

        public bool VSync
        {
            get
            {
                return m_kvp.GetBool("video.vsync");
            }
            set
            {
                m_kvp.Set("video.vsync", value);
            }
        }

        public int WindowWidth
        {
            get
            {
                return m_kvp.GetInteger("video.window_width");
            }
            set
            {
                m_kvp.Set("video.window_width", value);
            }
        }

        public int WindowHeight
        {
            get
            {
                return m_kvp.GetInteger("video.window_height");
            }
            set
            {
                m_kvp.Set("video.window_height", value);
            }
        }

        public bool WindowMaximised
        {
            get
            {
                return m_kvp.GetBool("video.window_maximised");
            }
            set
            {
                m_kvp.Set("video.window_maximised", value);
            }
        }

        public float Gamma
        {
            get
            {
                return m_kvp.GetFloat("video.gamma");
            }
            set
            {
                m_kvp.Set("video.gamma", value);
            }
        }

        public bool Shadows
        {
            get
            {
                return m_kvp.GetBool("video.shadows");
            }
            set
            {
                m_kvp.Set("video.shadows", value);
            }
        }

        public bool FancyRewind
        {
            get
            {
                return m_kvp.GetBool("video.fancy_rewind");
            }
            set
            {
                m_kvp.Set("video.fancy_rewind", value);
            }
        }

        public AntiAliasingMode AAMode
        {
            get
            {
                return m_kvp.GetEnum("video.aamode", AntiAliasingMode.None);
            }
            set
            {
                m_kvp.Set("video.aamode", value);
            }
        }

        public bool EnableGamepad
        {
            get
            {
                return m_kvp.GetBool("input.gamepad_enabled");
            }
            set
            {
                m_kvp.Set("input.gamepad_enabled", value);
            }
        }

        public bool EnableGamepadRumble
        {
            get
            {
                return m_kvp.GetBool("input.gamepad_rumble_enabled");
            }
            set
            {
                m_kvp.Set("input.gamepad_rumble_enabled", value);
            }
        }

        public GamepadType GamepadPromptType
        {
            get
            {
                return m_kvp.GetEnum("input.gamepad_prompt_type", GamepadType.Unknown);
            }
            set
            {
                if (value != GamepadType.Unknown)
                {
                    m_kvp.Set("input.gamepad_prompt_type", value);
                }
                else
                {
                    m_kvp.Remove("input.gamepad_prompt_type");
                }
            }
        }

        public bool EnableSteamController
        {
            get
            {
                return m_kvp.GetBool("input.steam_controller_enabled");
            }
            set
            {
                m_kvp.Set("input.steam_controller_enabled", value);
            }
        }

        public string[] EditorPalette
        {
            get
            {
                return m_kvp.GetString("editor.palette", "").Split(',');
            }
            set
            {
                m_kvp.Set("editor.palette", string.Join(",", value));
            }
        }

        public FlatDirection[] EditorDirections
        {
            get
            {
                var strings = m_kvp.GetString("editor.directions", "").Split(',');
                return strings.Select(delegate (string dirString)
               {
                   FlatDirection direction;
                   if (Enum.TryParse(dirString, out direction))
                   {
                       return direction;
                   }
                   return FlatDirection.North;
               }).ToArray();
            }
            set
            {
                m_kvp.Set(
                    "editor.directions",
                    string.Join(",", value.Select((dir) => dir.ToString()))
                );
            }
        }

        public int EditorSelection
        {
            get
            {
                return m_kvp.GetInteger("editor.selection", 0);
            }
            set
            {
                m_kvp.Set("editor.selection", value);
            }
        }

        public int EditorPage
        {
            get
            {
                return m_kvp.GetInteger("editor.page", 0);
            }
            set
            {
                m_kvp.Set("editor.page", value);
            }
        }

        public bool DisableFPSWarning
        {
            get
            {
                return m_kvp.GetBool("video.disable_fps_warning");
            }
            set
            {
                m_kvp.Set("video.disable_fps_warning", value);
            }
        }

        public Guid ArcadeGUID
        {
            get
            {
                return m_kvp.GetGUID("arcade.system_guid", Guid.Empty);
            }
            set
            {
                m_kvp.SetGUID("arcade.system_guid", value);
            }
        }

        public Settings()
        {
            var settingsPath = Path.Combine(App.SavePath, "settings.txt");
            m_kvp = new KeyValuePairFile(settingsPath);
            m_cachedKeyBinds = new Dictionary<Bind, Key>();
            EnsureDefaults();
            m_kvp.SaveIfModified();
        }

        public void Reset()
        {
            m_kvp.Clear();
            m_kvp.Comment = null;
            m_cachedKeyBinds.Clear();
            EnsureDefaults();
        }

        private void EnsureDefaults()
        {
            if (m_kvp.Comment == null)
            {
                m_kvp.Comment = "Game settings";
            }
            m_kvp.Ensure("audio.music_enabled", true);
            m_kvp.Ensure("audio.sound_enabled", true);
            m_kvp.Ensure("audio.music_volume", 8.0f);
            m_kvp.Ensure("audio.sound_volume", 8.0f);
            m_kvp.Ensure("text.language", "system");
            m_kvp.Ensure("video.disable_fps_warning", false);
            m_kvp.Ensure("video.fullscreen", App.Debug ? false : true);
            m_kvp.Ensure("video.fullscreen_width", 1920);
            m_kvp.Ensure("video.fullscreen_height", 1080);
            m_kvp.Ensure("video.window_width", 910);
            m_kvp.Ensure("video.window_height", 540);
            m_kvp.Ensure("video.window_maximised", false);
            m_kvp.Ensure("video.gamma", 1.1f);
            m_kvp.Ensure("video.shadows", true);
            m_kvp.Ensure("video.aamode", AntiAliasingMode.FXAA);
            m_kvp.Ensure("video.vsync", true);
            m_kvp.Ensure("video.fancy_rewind", true);
            m_kvp.Ensure("input.gamepad_enabled", true);
            m_kvp.Ensure("input.gamepad_rumble_enabled", true);
            m_kvp.Ensure("input.steam_controller_enabled", true);
            foreach (Bind bind in Enum.GetValues(typeof(Bind)))
            {
                var defaultKey = bind.GetDefaultKey();
                if (defaultKey != Key.None)
                {
                    var k = "input.keyboard." + bind.ToString().ToLowerUnderscored();
                    var v = m_kvp.Ensure(k, defaultKey);
                    m_cachedKeyBinds.Add(bind, v);
                }
            }
        }

        public void SetKeyBind(Bind bind, Key key)
        {
            if (bind.GetDefaultKey() != Key.None)
            {
                var k = "input.keyboard." + bind.ToString().ToLowerUnderscored();
                m_kvp.Set(k, key);
                m_cachedKeyBinds[bind] = key;
            }
        }

        public Key GetKeyBind(Bind bind)
        {
            if (bind.GetDefaultKey() != Key.None)
            {
                return m_cachedKeyBinds[bind];
            }
            return Key.None;
        }

        public GamepadButton GetPadBind(Bind bind)
        {
            return bind.GetDefaultPadButton();
        }

        public MouseButton GetMouseBind(Bind bind)
        {
            return bind.GetDefaultMouseButton();
        }

        public void Save()
        {
            m_kvp.SaveIfModified();
        }
    }
}
