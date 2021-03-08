using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Game.GUI;
using Dan200.Game.Level;
using OpenTK;
using System.IO;

namespace Dan200.Game.Game
{
    public class ThumbnailState : LevelState
    {
        public const int THUMBNAIL_WIDTH = 852;
        public const int THUMBNAIL_HEIGHT = 480;
        private const float FLASH_DURATION = 1.5f;

        public enum SubState
        {
            Waiting,
            Capturing,
            CameraFlash,
            Approving,
        }

        private Text m_prompt;
        private CameraHUD m_cameraHUD;

        private SubState m_state;
        private Promise<Screenshot> m_screenshot;
        private float m_timer;

        private Mod m_mod;
        private Campaign m_campaign;
        private int m_levelIndex;
        private string m_levelSavePath;

        private bool m_modified;

        public ThumbnailState(Game game, Mod mod, Campaign campaign, int levelIndex, string levelLoadPath, string levelSavePath) : base(game, levelLoadPath, LevelOptions.Menu)
        {
            m_mod = mod;
            m_campaign = campaign;
            m_levelIndex = levelIndex;

            m_levelSavePath = levelSavePath;
            EnableGamepad = false;

            m_prompt = new Text(UIFonts.Smaller, MouseButton.Left.GetPrompt() + " " + Game.Language.Translate("menus.editor.capture_thumbnail"), UIColours.Text, TextAlignment.Center);
            m_prompt.Anchor = Anchor.BottomMiddle;
            m_prompt.LocalPosition = new Vector2(0.0f, -16.0f - m_prompt.Font.Height);

            m_cameraHUD = new CameraHUD();
            m_cameraHUD.ShowViewfinder = true;

            m_state = SubState.Waiting;
            m_timer = 0.0f;
            Level.TimeMachine.Rate = 0.0f;

            m_modified = false;
        }

        protected override void OnPreInit(State previous, Transition transition)
        {
            base.OnPreInit(previous, transition);

            // Position camera
            if (previous is ThumbnailState)
            {
                LevelState previousInGame = (LevelState)previous;
                CameraController.Pitch = previousInGame.CameraController.Pitch;
                CameraController.Yaw = previousInGame.CameraController.Yaw;
                CameraController.Distance = previousInGame.CameraController.Distance;
                CameraController.TargetDistance = previousInGame.CameraController.TargetDistance;
            }
        }

        protected override void OnInit()
        {
            base.OnInit();

            Game.Screen.Elements.Add(m_prompt);
            Game.Screen.Elements.Add(m_cameraHUD);

            CameraController.AllowUserRotate = true;
            CameraController.AllowUserZoom = true;
            EnableTimeEffects = false;
        }

        protected override string GetMusicPath(State previous, Transition transition)
        {
            return null;
        }

        private bool CheckCameraButton()
        {
            if (Game.Keyboard.Keys[Key.Return].Pressed ||
                Game.Mouse.Buttons[MouseButton.Left].Pressed)
            {
                return true;
            }
            return false;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            switch (m_state)
            {
                case SubState.Waiting:
                    {
                        // Wait for camera press
                        if (CheckCameraButton())
                        {
                            m_state = SubState.Capturing;
                            m_timer = 0.0f;
                        }
                        else if (CheckBack())
                        {
                            BackToEditor();
                        }
                        break;
                    }
                case SubState.Capturing:
                    {
                        // Take the screenshot
                        // Stop the action
                        CameraController.AllowUserZoom = false;
                        CameraController.AllowUserRotate = false;

                        // Clear the screen
                        m_cameraHUD.ShowViewfinder = false;
                        m_prompt.Visible = false;

                        // Request the screenshot
                        m_screenshot = Game.QueueScreenshot(THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT);
                        m_state = SubState.CameraFlash;
                        break;
                    }
                case SubState.CameraFlash:
                    {
                        // Wait for screenshot, then flash
                        if (m_screenshot.Status != Status.Waiting)
                        {
                            // Do the effects
                            Game.Audio.PlayMusic(null, 0.0f);
                            m_cameraHUD.Flash();
                            m_state = SubState.Approving;
                            m_timer = FLASH_DURATION;
                        }
                        break;
                    }
                case SubState.Approving:
                    {
                        // Wait for a timer, then prompt to save the screenshot
                        if (m_timer > 0.0f)
                        {
                            m_timer -= dt;
                            if (m_timer <= 0.0f)
                            {
                                // Create the texture
                                var screenshot = m_screenshot.Result;
                                var bitmap = new BitmapTexture(screenshot.Bitmap);
                                bitmap.Filter = true;

                                // Show the dialog
                                var dialog = DialogBox.CreateImageQueryBox(
                                    Game.Language.Translate("menus.create_thumbnail.confirm_prompt"),
                                    bitmap,
                                    427.0f,
                                    240.0f,
                                    new string[] {
                                    Game.Language.Translate( "menus.yes" ),
                                    Game.Language.Translate( "menus.no" ),
                                    }
                                );
                                dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
                                {
                                    // Handle the result
                                    if (e.Result == 0)
                                    {
                                        // Yes
                                        // Save the screenshot
                                        var screenshotPath = AssetPath.ChangeExtension(m_levelSavePath, "png");
                                        if (m_mod != null)
                                        {
                                            var fullPath = Path.Combine(m_mod.Path, "assets/" + screenshotPath);
                                            screenshot.Save(fullPath);
                                        }
                                        else
                                        {
                                            var fullPath = Path.Combine(App.AssetPath, "main/" + screenshotPath);
                                            screenshot.Save(fullPath);
                                        }
                                        Assets.Reload(screenshotPath);

                                        // Save the camera position
                                        Level.Info.CameraPitch = MathHelper.RadiansToDegrees(CameraController.Pitch);
                                        Level.Info.CameraYaw = MathHelper.RadiansToDegrees(CameraController.Yaw);
                                        Level.Info.CameraDistance = CameraController.Distance;
                                        m_modified = true;

                                        // Return
                                        BackToEditor();
                                    }
                                    else if (e.Result == 1)
                                    {
                                        // No
                                        TryAgain();
                                    }
                                    else
                                    {
                                        // Escape
                                        BackToEditor();
                                    }

                                    // Dispose things we no longer need
                                    bitmap.Dispose();
                                    screenshot.Dispose();
                                };
                                ShowDialog(dialog);
                            }
                        }
                        break;
                    }
            }
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_cameraHUD);
            m_cameraHUD.Dispose();
            m_cameraHUD = null;

            Game.Screen.Elements.Remove(m_prompt);
            m_prompt.Dispose();
            m_prompt = null;

            base.OnShutdown();
        }

        private void TryAgain()
        {
            CutToState(new ThumbnailState(Game, m_mod, m_campaign, m_levelIndex, LevelLoadPath, m_levelSavePath));
        }

        private string SaveLevelTemporary()
        {
            // Save temporarilly
            string assetPath = "temp/editor.level";
            var fullPath = Path.Combine(App.SavePath, "editor/" + assetPath);
            Level.Save(fullPath);
            App.Log("Saved level to " + assetPath);
            Assets.Reload(assetPath);
            return assetPath;
        }

        private void BackToEditor()
        {
            if (m_modified)
            {
                var tempPath = SaveLevelTemporary();
                CutToState(new EditorState(Game, m_mod, m_campaign, m_levelIndex, tempPath, m_levelSavePath));
            }
            else
            {
                CutToState(new EditorState(Game, m_mod, m_campaign, m_levelIndex, LevelLoadPath, m_levelSavePath));
            }
        }
    }
}

