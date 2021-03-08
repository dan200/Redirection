
using Dan200.Core.Animation;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Input;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using System;

namespace Dan200.Game.Game
{
    public class StartScreenState : LevelState
    {
        private const float LOGO_GROW_TIME = 0.5f;
        private const float LOGO_SHRINK_TIME = 0.5f;

        private AnimatedCameraController m_animatedCamera;
        private CutsceneEntity m_robot;

        private Image m_logo;
        private Text m_instruction;
        private Text m_copyright;
        private float m_logoSize;

        private string GetInstructionText()
        {
            if (TimeInState >= LOGO_GROW_TIME && ((int)((TimeInState - LOGO_GROW_TIME) / 0.4f) % 3) < 2)
            {
                if (Game.Screen.InputMethod == InputMethod.SteamController)
                {
                    var controller = Game.ActiveSteamController;
                    return Game.Language.Translate("menus.startup.start_prompt", SteamControllerButton.MenuSelect.GetPrompt(controller));
                }
                else if (Game.Screen.InputMethod == InputMethod.Gamepad)
                {
                    var type = Game.ActiveGamepad.Type;
                    return Game.Language.Translate("menus.startup.start_prompt", GamepadButton.A.GetPrompt(type));
                }
                else if (Game.Screen.InputMethod == InputMethod.Keyboard)
                {
                    return Game.Language.Translate("menus.startup.start_prompt", Key.Return.GetPrompt());
                }
                else
                {
                    return Game.Language.Translate("menus.startup.start_prompt", MouseButton.Left.GetPrompt());
                }
            }
            return "";
        }

        public StartScreenState(Game game) : base(game, "levels/startscreen.level", LevelOptions.Menu)
        {
            // Create camera
            m_animatedCamera = new AnimatedCameraController(Level.TimeMachine);

            // Create GUI
            var texture = Texture.Get("gui/logo.png", true);
            m_logo = new Image(texture, Quad.UnitSquare, game.Screen.Width, game.Screen.Width * ((float)texture.Height / (float)texture.Width));
            m_logo.Anchor = Anchor.CentreMiddle;
            m_logo.LocalPosition = new Vector2(-0.5f * m_logo.Width, -0.5f * m_logo.Height - 8.0f);

            m_instruction = new Text(UIFonts.Smaller, GetInstructionText(), UIColours.Text, TextAlignment.Center);
            m_instruction.Anchor = Anchor.BottomMiddle;
            m_instruction.LocalPosition = new Vector2(0.0f, -36.0f - m_instruction.Height);

            var copyrightText = (char)169 + "2016 Daniel Ratcliffe"; // Copyright symbol
            m_copyright = new Text(UIFonts.Smallest, copyrightText, UIColours.Grey, TextAlignment.Center);
            m_copyright.Anchor = Anchor.BottomMiddle;
            m_copyright.Scale = 0.75f;
            m_copyright.LocalPosition = new Vector2(0.0f, -8.0f - m_copyright.Height);

            m_logoSize = 0.0f;
            UpdateLogoSize();
        }

        private void UpdateLogoSize()
        {
            var f = MathUtils.Ease(m_logoSize);
            var targetW = Game.Screen.Width;
            var targetH = Game.Screen.Width * ((float)m_logo.Texture.Height / (float)m_logo.Texture.Width);
            m_logo.Width = f * targetW;
            m_logo.Height = f * targetH;
            m_logo.LocalPosition = new Vector2(-0.5f * m_logo.Width, -0.5f * m_logo.Height - 16.0f);
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

        protected override string GetMusicPath(State previous, Transition transition)
        {
            if (previous is MainMenuState)
            {
                return Level.Info.MusicPath;
            }
            else
            {
                return null;
            }
        }

        protected override void OnReveal()
        {
            base.OnReveal();

            // Create robot
            m_robot = CreateEntity("models/entities/new/red_robot.obj");

            // Start animation
            StartCameraAnimation("animation/menus/startscreen/camera_initial.anim.lua");
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot.anim.lua"), false);

            // Reposition sky
            Game.Sky.ForegroundModelTransform = Matrix4.CreateTranslation(-5.0f, 5.0f, -20.0f);
        }

        protected override void OnHide()
        {
            base.OnHide();

            Game.Screen.Elements.Remove(m_logo);
            m_logo.Dispose();
            m_logo = null;
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Add GUI
            Game.Screen.Elements.Add(m_logo);
            Game.Screen.Elements.Add(m_instruction);
            Game.Screen.Elements.Add(m_copyright);

            // Start music
            Game.Audio.PlayMusic(Level.Info.MusicPath, 0.0f, true);
        }

        private bool CheckStart()
        {
            if (Dialog == null)
            {
                if (Game.ActiveSteamController != null)
                {
                    if (Game.ActiveSteamController.Buttons[SteamControllerButton.MenuSelect.GetID()].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.SteamController;
                        return true;
                    }
                }
                if (Game.ActiveGamepad != null)
                {
                    if (Game.ActiveGamepad.Buttons[GamepadButton.Start].Pressed ||
                        Game.ActiveGamepad.Buttons[GamepadButton.A].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.Gamepad;
                        return true;
                    }
                }
                if (Game.Keyboard.Keys[Key.Return].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    return true;
                }
                if (Game.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Mouse;
                    return true;
                }
            }
            return false;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            // Update logo
            m_logoSize = Math.Min(m_logoSize + dt / LOGO_GROW_TIME, 1.0f);
            UpdateLogoSize();

            // Update instruction
            m_instruction.String = GetInstructionText();

            // Check for input
            if (m_logoSize >= 1.0f && (CheckStart() || CheckBack()))
            {
                Start();
            }
        }

        protected override void OnPostUpdate(float dt)
        {
            base.OnPostUpdate(dt);

            // Update logo
            if (m_logo != null)
            {
                m_logoSize = Math.Max(m_logoSize - dt / LOGO_SHRINK_TIME, 0.0f);
                UpdateLogoSize();
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
            base.OnShutdown();

            Game.Screen.Elements.Remove(m_instruction);
            m_instruction.Dispose();
            m_instruction = null;

            Game.Screen.Elements.Remove(m_copyright);
            m_copyright.Dispose();
            m_copyright = null;
        }

        private void Start()
        {
            // Animate the camera
            StartCameraAnimation("animation/menus/startscreen/camera_frominitial.anim.lua");

            // Show startup
            CutToState(new MainMenuState(Game), 1.25f);
        }
    }
}
