
using Dan200.Core.Animation;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.Game
{
    public enum MenuArrangement
    {
        FullScreen,
        RobotScreen
    }

    public abstract class MenuState : LevelState
    {
        private AnimatedCameraController m_animatedCamera;

        private string m_unlocalisedTitle;
        private string m_unlocalisedBack;
        private string m_unlocalisedAltSelect;
        private TextMenu m_titleMenu;
        private InputPrompt m_backPrompt;
        private InputPrompt m_selectPrompt;
        private InputPrompt m_altSelectPrompt;

        public string Title
        {
            get
            {
                return m_titleMenu.Options[0];
            }
            set
            {
                m_unlocalisedTitle = null;
                m_titleMenu.Options[0] = value;
            }
        }

        public bool TitleClickable
        {
            get
            {
                return m_titleMenu.Enabled;
            }
            set
            {
                m_titleMenu.Enabled = value;
            }
        }

        public string BackPrompt
        {
            get
            {
                return m_unlocalisedBack;
            }
            set
            {
                if (m_unlocalisedBack != value)
                {
                    m_unlocalisedBack = value;
                    m_backPrompt.String = Game.Language.Translate(m_unlocalisedBack);
                }
            }
        }

        public bool ShowSelectPrompt
        {
            get
            {
                return m_selectPrompt.Visible;
            }
            set
            {
                m_selectPrompt.Visible = value;
            }
        }

        public bool ShowAltSelectPrompt
        {
            get
            {
                return m_altSelectPrompt.Visible;
            }
            set
            {
                m_altSelectPrompt.Visible = value;
            }
        }

        public string AltSelectPrompt
        {
            get
            {
                return m_unlocalisedAltSelect;
            }
            set
            {
                if (m_unlocalisedAltSelect != value)
                {
                    m_unlocalisedAltSelect = value;
                    m_altSelectPrompt.String = Game.Language.Translate(m_unlocalisedAltSelect);
                }
            }
        }

        protected MenuState(Game game, string title, string level, MenuArrangement arrangement) : base(game, level, LevelOptions.Menu)
        {
            m_unlocalisedTitle = title;
            m_unlocalisedBack = "menus.back";
            m_unlocalisedAltSelect = "menus.select";

            // Create camera
            m_animatedCamera = new AnimatedCameraController(Level.TimeMachine);

            // Create title menu
            {
                m_titleMenu = new TextMenu(UIFonts.Default, new string[] {
                    Game.Language.Translate( title ),
                }, TextAlignment.Center, MenuDirection.Vertical);
                if (arrangement == MenuArrangement.RobotScreen)
                {
                    m_titleMenu.Anchor = Anchor.TopMiddle;
                    m_titleMenu.LocalPosition = new Vector2(0.0f, 40.0f);
                }
                else
                {
                    m_titleMenu.Anchor = Anchor.TopMiddle;
                    m_titleMenu.LocalPosition = new Vector2(0.0f, 32.0f);
                }
                m_titleMenu.TextColour = UIColours.Title;
                m_titleMenu.Enabled = false;
                m_titleMenu.MouseOnly = true;
                m_titleMenu.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
                {
                    if (Dialog == null)
                    {
                        OnTitleClicked();
                    }
                };
            }

            // Create prompts
            {
                m_backPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate(m_unlocalisedBack), TextAlignment.Right);
                m_backPrompt.Key = Key.Escape;
                m_backPrompt.MouseButton = MouseButton.Left;
                m_backPrompt.GamepadButton = GamepadButton.B;
                m_backPrompt.SteamControllerButton = SteamControllerButton.MenuBack;
                if (arrangement == MenuArrangement.RobotScreen)
                {
                    m_backPrompt.Anchor = Anchor.BottomMiddle;
                    m_backPrompt.LocalPosition = new Vector2(225.0f, -40.0f - m_backPrompt.Font.Height);
                }
                else
                {
                    m_backPrompt.Anchor = Anchor.BottomRight;
                    m_backPrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_backPrompt.Font.Height);
                }
                m_backPrompt.OnClick += delegate (object sender, EventArgs e)
                {
                    GoBack();
                };
            }
            {
                m_selectPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate("menus.select"), TextAlignment.Left);
                m_selectPrompt.Key = Key.Return;
                m_selectPrompt.GamepadButton = GamepadButton.A;
                m_selectPrompt.SteamControllerButton = SteamControllerButton.MenuSelect;
                if (arrangement == MenuArrangement.RobotScreen)
                {
                    m_selectPrompt.Anchor = Anchor.BottomMiddle;
                    m_selectPrompt.LocalPosition = new Vector2(-224.0f, -40.0f - m_selectPrompt.Font.Height);
                }
                else
                {
                    m_selectPrompt.Anchor = Anchor.BottomLeft;
                    m_selectPrompt.LocalPosition = new Vector2(16.0f, -16.0f - m_selectPrompt.Font.Height);
                }
                m_selectPrompt.Visible = false;
            }
            {
                m_altSelectPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate(m_unlocalisedAltSelect), TextAlignment.Left);
                m_altSelectPrompt.Key = Key.Tab;
                m_altSelectPrompt.GamepadButton = GamepadButton.Y;
                m_altSelectPrompt.SteamControllerButton = SteamControllerButton.MenuAltSelect;
                m_altSelectPrompt.Anchor = m_selectPrompt.Anchor;
                m_altSelectPrompt.LocalPosition = m_selectPrompt.LocalPosition + new Vector2(0.0f, -m_altSelectPrompt.Font.Height);
                m_altSelectPrompt.Visible = false;
            }
        }

        protected void StartCameraAnimation(string animPath)
        {
            m_animatedCamera.Play(LuaAnimation.Get(animPath));
        }

        protected CutsceneEntity CreateEntity(string modelPath)
        {
            var entity = new CutsceneEntity(Model.Get(modelPath), RenderPass.Opaque);
            Level.Entities.Add(entity);
            return entity;
        }

        protected virtual void OnTitleClicked()
        {
        }

        protected abstract void GoBack();

        protected override void OnInit()
        {
            base.OnInit();
            Game.Screen.Elements.Add(m_titleMenu);
            Game.Screen.Elements.Add(m_backPrompt);
            Game.Screen.Elements.Add(m_selectPrompt);
            Game.Screen.Elements.Add(m_altSelectPrompt);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (CheckBack())
            {
                GoBack();
            }
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            // Sample the animation
            m_animatedCamera.Populate(camera);

            // Transform from level to world space
            MathUtils.FastInvert(ref camera.Transform);
            camera.Transform = camera.Transform * Level.Transform;
            MathUtils.FastInvert(ref camera.Transform);
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_titleMenu);
            m_titleMenu.Dispose();

            Game.Screen.Elements.Remove(m_backPrompt);
            m_backPrompt.Dispose();

            Game.Screen.Elements.Remove(m_selectPrompt);
            m_selectPrompt.Dispose();

            Game.Screen.Elements.Remove(m_altSelectPrompt);
            m_altSelectPrompt.Dispose();

            base.OnShutdown();
        }

        public override void OnReloadAssets()
        {
            base.OnReloadAssets();
            if (m_unlocalisedTitle != null)
            {
                m_titleMenu.Options[0] = Game.Language.Translate(m_unlocalisedTitle);
            }
            m_backPrompt.String = Game.Language.Translate(m_unlocalisedBack);
            m_selectPrompt.String = Game.Language.Translate("menus.select");
            m_altSelectPrompt.String = Game.Language.Translate(m_unlocalisedAltSelect);
        }
    }
}
