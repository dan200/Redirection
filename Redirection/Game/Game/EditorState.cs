using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Network;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using Dan200.Game.Level;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Game.Game
{
    public class EditorState : LevelState
    {
        private const string GUIDE_URL = "http://steamcommunity.com/sharedfiles/filedetails/?id=760371525";
        private const float STATUS_TIMEOUT = 3.0f;

        private Mod m_mod;
        private Campaign m_campaign;
        private int m_levelIndex;

        private string LevelSavePath;
        private EditorTileSelect m_tileSelect;
        private Button m_menuButton;
        private Button m_saveButton;
        private Button m_testButton;
        private Grid m_grid;
        private TileOutline m_outline;
        private bool m_modified;

        private TilePreview m_previewEntity;
        private List<TilePreview> m_pendingChanges;

        private TextMenu m_titleText;
        private Text m_statusText;
        private float m_statusTimeout;

        public EditorState(Game game, Mod mod, Campaign campaign, int levelIndex, string levelLoadPath, string levelSavePath) : base(game, levelLoadPath, LevelOptions.Editor)
        {
            m_mod = mod;
            m_campaign = campaign;
            m_levelIndex = levelIndex;
            EnableGamepad = false;

            LevelSavePath = levelSavePath;
            ShowPlacementsLeft = true;
            m_modified = (levelLoadPath != levelSavePath);

            // Load Menu
            // Test button
            m_testButton = new Button(Texture.Get("gui/play.png", true), 48.0f, 48.0f);
            m_testButton.Region = new Quad(0.0f, 0.0f, 0.375f, 0.375f);
            m_testButton.HighlightRegion = new Quad(0.5f, 0.0f, 0.375f, 0.375f);
            m_testButton.DisabledRegion = new Quad(0.0f, 0.5f, 0.375f, 0.375f);
            m_testButton.ShortcutKey = Key.T;
            m_testButton.ShowShortcutPrompt = true;
            m_testButton.Anchor = Anchor.BottomRight;
            m_testButton.LocalPosition = new Vector2(-16.0f - m_testButton.Width, -16.0f - m_testButton.Height);
            m_testButton.Visible = true;
            m_testButton.OnClicked += delegate (object o, EventArgs args)
            {
                if (Dialog == null && !m_tileSelect.IsFullscreen)
                {
                    TestLevel();
                }
            };

            // Save button
            m_saveButton = new Button(Texture.Get("gui/save.png", true), 48.0f, 48.0f);
            m_saveButton.Region = new Quad(0.0f, 0.0f, 0.375f, 0.375f);
            m_saveButton.HighlightRegion = new Quad(0.5f, 0.0f, 0.375f, 0.375f);
            m_saveButton.DisabledRegion = new Quad(0.0f, 0.5f, 0.375f, 0.375f);
            m_saveButton.ShortcutKey = Key.S;
            m_saveButton.ShowShortcutPrompt = true;
            m_saveButton.Anchor = Anchor.BottomRight;
            m_saveButton.LocalPosition = new Vector2(-16.0f - m_testButton.Width - 8.0f - m_saveButton.Width, -16.0f - m_saveButton.Height);
            m_saveButton.Visible = true;
            m_saveButton.OnClicked += delegate (object o, EventArgs args)
            {
                if (Dialog == null && !m_tileSelect.IsFullscreen)
                {
                    SaveLevel();
                }
            };

            // Menu button
            m_menuButton = new Button(Texture.Get("gui/menu.png", true), 48.0f, 48.0f);
            m_menuButton.Region = new Quad(0.0f, 0.0f, 0.375f, 0.375f);
            m_menuButton.HighlightRegion = new Quad(0.5f, 0.0f, 0.375f, 0.375f);
            m_menuButton.DisabledRegion = new Quad(0.0f, 0.5f, 0.375f, 0.375f);
            m_menuButton.ShortcutKey = Key.Escape;
            m_menuButton.ShowShortcutPrompt = true;
            m_menuButton.Anchor = Anchor.BottomRight;
            m_menuButton.LocalPosition = new Vector2(-16.0f - m_menuButton.Width - 8.0f - m_saveButton.Width - 8.0f - m_menuButton.Width, -16.0f - m_testButton.Height);
            m_menuButton.Visible = true;
            m_menuButton.OnClicked += delegate (object o, EventArgs args)
            {
                if (Dialog == null && !m_tileSelect.IsFullscreen)
                {
                    ShowPauseMenu();
                }
            };

            // Load clickable title
            m_titleText = new TextMenu(UIFonts.Default, new string[] { "" }, TextAlignment.Right, MenuDirection.Vertical);
            m_titleText.Anchor = Anchor.TopRight;
            m_titleText.MouseOnly = true;
            m_titleText.LocalPosition = new Vector2(-32.0f, 16.0f);
            m_titleText.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                if (Dialog == null && !m_tileSelect.IsFullscreen)
                {
                    var textEntry = TextEntryDialogBox.Create(
                        Game.Language.Translate("menus.name_level_prompt.title"),
                        Level.Info.Title, "",
                        Game.Screen.Width - 300.0f, new string[] {
                            Game.Language.Translate( "menus.ok" ),
                            Game.Language.Translate( "menus.cancel" )
                        }
                    );
                    textEntry.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
                    {
                        if (e2.Result == 0)
                        {
                            var title = (textEntry.EnteredText.Trim().Length > 0) ? textEntry.EnteredText.Trim() : "Untitled";
                            Level.Info.Title = title;
                            m_titleText.Options[0] = MouseButton.Left.GetPrompt() + " " + TranslateTitle(title);
                            m_modified = true;
                        }
                    };
                    ShowDialog(textEntry);
                }
            };

            // Load status
            m_statusText = new Text(UIFonts.Smaller, "", UIColours.Text, TextAlignment.Left);
            m_statusText.Anchor = Anchor.BottomLeft;
            m_statusText.LocalPosition = new Vector2(80.0f + 16.0f, -16.0f - m_statusText.Font.Height);
            m_statusTimeout = 0.0f;

            // Load tile select
            m_tileSelect = new EditorTileSelect(Game, Game.User.Settings, Game.Language);
        }

        public void MarkLevelCompleted()
        {
            if (!Level.Info.EverCompleted)
            {
                Level.Info.EverCompleted = true;
                m_modified = true;
            }
        }

        protected override void OnPreInit(State previous, Transition transition)
        {
            base.OnPreInit(previous, transition);

            // Position camera
            if (previous is InGameState || previous is EditorState || previous is ThumbnailState)
            {
                LevelState previousInGame = (LevelState)previous;
                CameraController.Focus = previousInGame.CameraController.Focus;
                CameraController.Pitch = previousInGame.CameraController.Pitch;
                CameraController.Yaw = previousInGame.CameraController.Yaw;
                CameraController.Distance = previousInGame.CameraController.Distance;
                CameraController.TargetDistance = previousInGame.CameraController.TargetDistance;
            }
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Enable camera control
            CameraController.AllowUserRotate = true;
            CameraController.AllowUserInvert = true;
            CameraController.AllowUserZoom = true;
            CameraController.AllowUserPan = true;

            // Load grid
            m_grid = new Grid(Level);
            m_outline = new TileOutline(Level);

            // Setup preview
            m_previewEntity = new TilePreview();
            Level.Entities.Add(m_previewEntity);

            // Setup change queue
            m_pendingChanges = new List<TilePreview>();

            // Add GUI elements
            Game.Screen.Elements.Add(m_menuButton);
            Game.Screen.Elements.Add(m_saveButton);
            Game.Screen.Elements.Add(m_testButton);

            m_titleText.Options[0] = MouseButton.Left.GetPrompt() + " " + TranslateTitle(Level.Info.Title);
            Game.Screen.Elements.Add(m_titleText);
            Game.Screen.Elements.Add(m_statusText);
            Game.Screen.Elements.Add(m_tileSelect);

            if (!Game.User.Progress.UsedLevelEditor)
            {
                // Do first time tutorial
                var dialog = DialogBox.CreateQueryBox(
                    Game.Screen,
                    Game.Language.Translate("menus.editor.title"),
                    Game.Language.Translate("menus.editor.first_time_prompt"),
                    new string[] {
                        Game.Language.Translate("menus.yes"),
                        Game.Language.Translate("menus.no"),
                    },
                    false
                );
                dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs args)
                {
                    if (args.Result == 0)
                    {
                        // YES
                        ShowGuide();
                    }
                };
                ShowDialog(dialog);

                // Supress it in future
                Game.User.Progress.UsedLevelEditor = true;
                Game.User.Progress.Save();
            }
        }

        protected override string GetMusicPath(State previous, Transition transition)
        {
            return null;
        }

        private bool PickTile(out Tile o_tile, out FlatDirection o_pickedDirection)
        {
            if (Dialog == null &&
                !m_menuButton.TestMouse() &&
                !m_saveButton.TestMouse() &&
                !m_testButton.TestMouse() &&
                m_titleText.TestMouse() < 0 &&
                !m_tileSelect.TestMouse())
            {
                var ray = BuildRay(Game.Screen.MousePosition, 100.0f);

                // Raycast against level
                TileCoordinates levelHitCoords;
                Direction levelHitSide;
                float levelHitDistance;
                bool levelHit = Level.RaycastTiles(ray, out levelHitCoords, out levelHitSide, out levelHitDistance);
                if (levelHit)
                {
                    var baseCoords = Level.Tiles[levelHitCoords].GetBase(Level, levelHitCoords);
                    o_tile = Level.Tiles[baseCoords];
                    o_pickedDirection = o_tile.GetDirection(Level, baseCoords);
                    return true;
                }
            }
            o_tile = default(Tile);
            o_pickedDirection = default(FlatDirection);
            return false;
        }

        private bool GetPotentialPlacement(out TileCoordinates o_position, out Tile o_tile, out FlatDirection o_direction, TileOutline previewOutline)
        {
            if (Dialog == null &&
                !m_menuButton.TestMouse() &&
                !m_testButton.TestMouse() &&
                !m_saveButton.TestMouse() &&
                m_titleText.TestMouse() < 0 &&
                !m_tileSelect.TestMouse())
            {
                var ray = BuildRay(Game.Screen.MousePosition, 100.0f);

                // Raycast against level
                TileCoordinates levelHitCoords;
                Direction levelHitSide;
                float levelHitDistance;
                bool levelHit = Level.RaycastTiles(ray, out levelHitCoords, out levelHitSide, out levelHitDistance);

                // Raycast against grid
                TileCoordinates gridHitCoords;
                Direction gridHitSide;
                float gridHitDistance;
                bool gridHit = m_grid.Raycast(ray, out gridHitCoords, out gridHitSide, out gridHitDistance);

                // Determine the closest hit result (if any)
                TileCoordinates hitCoords;
                Direction hitSide;
                bool hit = false;
                if (gridHit && !levelHit)
                {
                    hitCoords = gridHitCoords;
                    hitSide = gridHitSide;
                    hit = true;
                }
                else if (levelHit)
                {
                    hitCoords = levelHitCoords;
                    hitSide = levelHitSide;
                    hit = true;
                }
                else
                {
                    hitCoords = default(TileCoordinates);
                    hitSide = default(Direction);
                    hit = false;
                }

                // Determine where to place or delete
                if (hit)
                {
                    var baseCoords = Level.Tiles[hitCoords].GetBase(Level, hitCoords);
                    if (Game.Keyboard.Keys[Key.LeftShift].Held || Game.Keyboard.Keys[Key.RightShift].Held)
                    {
                        // Deleting tile
                        o_tile = Tiles.Air;
                        o_direction = FlatDirection.North;
                        o_position = baseCoords;
                        if (previewOutline != null)
                        {
                            previewOutline.Visible = levelHit;
                            previewOutline.Position = baseCoords;
                            previewOutline.Height = Level.Tiles[baseCoords].Height;
                            previewOutline.Red = true;
                        }
                    }
                    else if (levelHit && Game.Keyboard.Keys[Key.LeftCtrl].Held || Game.Keyboard.Keys[Key.RightCtrl].Held)
                    {
                        // Replacing tile
                        o_tile = m_tileSelect.SelectedTile;
                        o_direction = m_tileSelect.SelectedTileDirection;
                        o_position = baseCoords;
                        if (previewOutline != null)
                        {
                            previewOutline.Visible = false;
                        }
                    }
                    else
                    {
                        // Adding tile
                        o_tile = m_tileSelect.SelectedTile;
                        o_direction = m_tileSelect.SelectedTileDirection;
                        if (hitSide == Direction.Down)
                        {
                            o_position = hitCoords.Move(hitSide, o_tile.Height);
                        }
                        else
                        {
                            o_position = hitCoords.Move(hitSide);
                        }
                        if (previewOutline != null)
                        {
                            previewOutline.Visible = levelHit;
                            previewOutline.Position = baseCoords;
                            previewOutline.Height = Level.Tiles[baseCoords].Height;
                            previewOutline.Red = false;
                        }
                    }
                    return true;
                }
            }

            o_position = default(TileCoordinates);
            o_direction = default(FlatDirection);
            o_tile = default(Tile);
            if (previewOutline != null)
            {
                previewOutline.Visible = false;
            }
            return false;
        }

        private void QueuePlacement()
        {
            // Determine where to place
            TileCoordinates position;
            Tile tile;
            FlatDirection direction;
            if (GetPotentialPlacement(out position, out tile, out direction, null))
            {
                if (m_pendingChanges.Count == 0)
                {
                    // Before creating pending entities, hide the cursor one. Prevents confusion with visibility
                    m_previewEntity.Visible = false;
                }
                else
                {
                    // Check we don't already have an overlapping entry queued
                    foreach (var existing in m_pendingChanges)
                    {
                        if (existing.Location.X == position.X &&
                            existing.Location.Z == position.Z &&
                            (existing.Location.Y < position.Y + tile.Height &&
                             position.Y < existing.Location.Y + existing.Tile.Height))
                        {
                            return;
                        }
                    }
                }

                // Setup the new tile preview, or change an existing one
                var entity = new TilePreview();
                entity.Location = position;
                entity.Visible = true;
                entity.SetTile(tile, direction);
                Level.Entities.Add(entity);
                m_pendingChanges.Add(entity);
            }
        }

        private void CommitPlacements()
        {
            // Take all the preview tiles and add them to the level in one go
            if (m_pendingChanges.Count > 0)
            {
                // Make the changes
                var tiles = Level.Tiles;
                var entities = Level.Entities;
                foreach (var entry in m_pendingChanges)
                {
                    // Replace a tile
                    var position = entry.Location;
                    var tile = entry.Tile;
                    var direction = entry.Direction;
                    for (int i = 0; i < tile.Height; ++i)
                    {
                        var above = position.Move(Direction.Up, i);
                        Level.Tiles.GetTile(above).Clear(Level, above, true);
                    }
                    tiles.SetTile(position, tile, direction, true);
                    entities.Remove(entry);
                }
                m_pendingChanges.Clear();
                tiles.Compress();

                // Update the grid and camera
                m_grid.Rebuild();
                UpdateCameraBounds();
                Level.Info.EverCompleted = false;
                m_modified = true;
            }
        }

        private void ShowPauseMenu()
        {
            // Editor pause menu
            string[] options = new string[] {
                Game.Language.Translate( "menus.editor.show_guide" ),
                Game.Language.Translate( "menus.editor.capture_thumbnail" ),
                Game.Language.Translate( "menus.editor.edit_script" ),
                Game.Language.Translate( "menus.editor.reload" ),
                Game.Language.Translate( "menus.editor.go_back" )
            };

            var pauseMenu = DialogBox.CreateMenuBox(Game.Language.Translate("menus.editor.title"), options, false);
            pauseMenu.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // Show guide
                            ShowGuide();
                            break;
                        }
                    case 1:
                        {
                            // Create thumbnail
                            if (m_modified)
                            {
                                var tempPath = SaveLevelTemporary();
                                CutToState(new ThumbnailState(Game, m_mod, m_campaign, m_levelIndex, tempPath, LevelSavePath));
                            }
                            else
                            {
                                CutToState(new ThumbnailState(Game, m_mod, m_campaign, m_levelIndex, LevelSavePath, LevelSavePath));
                            }
                            break;
                        }
                    case 2:
                        {
                            // Edit Script
                            EditScript();
                            break;
                        }
                    case 3:
                        {
                            // Reload level
                            ReloadLevel();
                            break;
                        }
                    case 4:
                        {
                            // Back to Menu
                            if (m_modified)
                            {
                                // Modified, so prompt for save first
                                var promptMenu = DialogBox.CreateQueryBox(
                                    Game.Screen,
                                    Game.Language.Translate("menus.save_prompt.title"),
                                    Game.Language.Translate("menus.save_prompt.info"),
                                    new string[] {
                                    Game.Language.Translate( "menus.yes" ),
                                    Game.Language.Translate( "menus.no" ),
                                    },
                                    true
                                );
                                promptMenu.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
                                {
                                    switch (e2.Result)
                                    {
                                        case 0:
                                            {
                                            // YES
                                            SaveLevel();
                                                BackToMenu();
                                                break;
                                            }
                                        case 1:
                                            {
                                            // NO
                                            BackToMenu();
                                                break;
                                            }
                                    }
                                };
                                ShowDialog(promptMenu);
                            }
                            else
                            {
                                // Unmodified, so don't prompt:
                                BackToMenu();
                            }
                            break;
                        }
                }
            };
            ShowDialog(pauseMenu);
        }

        private void EditScript()
        {
            // Assign a script to the level if there isn't one
            if (Level.Info.ScriptPath == null)
            {
                var levelPath = LevelSavePath;
                var scriptPath = AssetPath.ChangeExtension(levelPath, "lua");
                if (scriptPath.IndexOf("levels/") == 0)
                {
                    scriptPath = "scripts/" + scriptPath.Substring("levels/".Length);
                }
                Level.Info.ScriptPath = scriptPath;
                m_modified = true;

                SetStatus(Game.Language.Translate("menus.editor.script_changed", scriptPath));
            }

            // Create the script if it doesn't exist
            var fullScriptPath = (m_mod != null) ?
                Path.Combine(m_mod.Path, "assets/" + Level.Info.ScriptPath) :
                Path.Combine(App.AssetPath, "main/" + Level.Info.ScriptPath);
            if (!File.Exists(fullScriptPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullScriptPath));
                File.Copy(
                    Path.Combine(App.AssetPath, "base/scripts/template.lua"),
                    fullScriptPath
                );
                Assets.Reload(Level.Info.ScriptPath);
            }

            // Edit the script
            Game.Network.OpenTextEditor(fullScriptPath);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            // Input
            if (Dialog == null && !m_tileSelect.IsFullscreen)
            {
                if (Game.Keyboard.Keys[Key.Z].Pressed &&
                    ((Game.Keyboard.Keys[Key.LeftCtrl].Held || Game.Keyboard.Keys[Key.RightCtrl].Held) ||
                     (App.Platform == Platform.OSX && (Game.Keyboard.Keys[Key.LeftGUI].Held || Game.Keyboard.Keys[Key.LeftGUI].Held))))
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    Undo();
                }
                if (Game.Keyboard.Keys[Key.Plus].Pressed ||
                    Game.Keyboard.Keys[Key.NumpadPlus].Pressed ||
                    Game.Keyboard.Keys[Key.Equals].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    Level.Info.TotalPlacements = Level.Info.TotalPlacements + 1;
                    Level.Info.PlacementsLeft = Level.Info.TotalPlacements;
                    m_modified = true;
                }
                if (Game.Keyboard.Keys[Key.Minus].Pressed ||
                    Game.Keyboard.Keys[Key.NumpadMinus].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    if (Level.Info.TotalPlacements > 0)
                    {
                        Level.Info.TotalPlacements = Level.Info.TotalPlacements - 1;
                        Level.Info.PlacementsLeft = Level.Info.TotalPlacements;
                        Level.Info.EverCompleted = false;
                        m_modified = true;
                    }
                }
                if (Game.Keyboard.Keys[Key.NumpadZero].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    var oldPlacements = Level.Info.TotalPlacements;
                    if (oldPlacements != 0)
                    {
                        Level.Info.TotalPlacements = 0;
                        Level.Info.PlacementsLeft = Level.Info.TotalPlacements;
                        if (oldPlacements > Level.Info.TotalPlacements)
                        {
                            Level.Info.EverCompleted = false;
                        }
                        m_modified = true;
                    }
                }
                for (int i = 1; i <= 9; ++i)
                {
                    var key = (Key)(Key.NumpadOne + (i - 1));
                    if (Game.Keyboard.Keys[key].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.Keyboard;
                        var oldPlacements = Level.Info.TotalPlacements;
                        if (oldPlacements != i)
                        {
                            Level.Info.TotalPlacements = i;
                            Level.Info.PlacementsLeft = Level.Info.TotalPlacements;
                            if (oldPlacements > Level.Info.TotalPlacements)
                            {
                                Level.Info.EverCompleted = false;
                            }
                            m_modified = true;
                        }
                    }
                }
                if (Game.Keyboard.Keys[Key.O].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    // Cycle items
                    // Find all possible items
                    var items = Assets.Find<Tile>("tiles").Where(delegate (Tile t)
                    {
                        return t.AllowPlacement && t.Behaviour is SpawnTileBehaviour && ((SpawnTileBehaviour)t.Behaviour).Immobile;
                    }).ToArray();
                    if (items.Length > 0)
                    {
                        // Find the current item
                        int currentItemIndex = -1;
                        for (int i = 0; i < items.Length; ++i)
                        {
                            var item = items[i];
                            if (item.Path == Level.Info.ItemPath)
                            {
                                currentItemIndex = i;
                                break;
                            }
                        }

                        // Choose the next item
                        int nextItemIndex = (currentItemIndex + 1) % items.Length;
                        var nextItem = items[nextItemIndex];
                        Level.Info.ItemPath = nextItem.Path;
                        m_modified = true;

                        // Set status
                        SetStatus(Game.Language.Translate("menus.editor.item_changed", nextItem.Path));
                    }
                }
                if (Game.Keyboard.Keys[Key.K].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    // Cycle skies
                    // Find all possible skies
                    var skies = Assets.Find<Sky>("skies").ToArray();
                    if (skies.Length > 0)
                    {
                        // Find the current sky
                        int currentSkyIndex = -1;
                        for (int i = 0; i < skies.Length; ++i)
                        {
                            var sky = skies[i];
                            if (sky.Path == Level.Info.SkyPath)
                            {
                                currentSkyIndex = i;
                                break;
                            }
                        }

                        // Choose the sky
                        int nextSkyIndex = (currentSkyIndex + 1) % skies.Length;
                        var nextSky = skies[nextSkyIndex];
                        Level.Info.SkyPath = nextSky.Path;
                        Game.Sky = new SkyInstance(nextSky);
                        Level.Sky = Game.Sky;
                        m_modified = true;

                        SetStatus(Game.Language.Translate("menus.editor.sky_changed", nextSky.Path));
                    }
                }
                if (Game.Keyboard.Keys[Key.M].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    // Cycle music
                    // Find all possible music
                    var musics = Assets.Find<Music>("music").Where(delegate (Music m)
                   {
                       return !m.Path.EndsWith("_outro.ogg");
                   }).ToArray();
                    if (musics.Length > 0)
                    {
                        // Find the current sky
                        int currentMusicIndex = -1;
                        for (int i = 0; i < musics.Length; ++i)
                        {
                            var music = musics[i];
                            if (music.Path == Level.Info.MusicPath)
                            {
                                currentMusicIndex = i;
                                break;
                            }
                        }

                        // Choose the music
                        int nextMusicIndex = (currentMusicIndex + 1) % musics.Length;
                        var nextMusic = musics[nextMusicIndex];
                        Level.Info.MusicPath = nextMusic.Path;
                        Game.Audio.PlayMusic(nextMusic.Path, 0.0f, false);
                        m_modified = true;

                        SetStatus(Game.Language.Translate("menus.editor.music_changed", nextMusic.Path));
                    }
                }
                if (Game.Keyboard.Keys[Key.R].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    // Change the random number seed
                    Level.Info.RandomSeed = Level.Info.RandomSeed ^ Environment.TickCount;
                    Level.Tiles.RequestRebuild();
                    m_modified = true;

                    SetStatus(Game.Language.Translate("menus.editor.seed_changed", Level.Info.RandomSeed));
                }
                if (Game.Keyboard.Keys[Key.I].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    // Change the level ID
                    Level.Info.ID = MathUtils.GenerateLevelID(LevelSavePath);
                    m_modified = true;

                    SetStatus(Game.Language.Translate("menus.editor.id_changed", Level.Info.ID));
                }
            }

            // Picking
            {
                Tile pickedTile;
                FlatDirection pickedDirection;
                if ((Game.Mouse.Buttons[MouseButton.Middle].Pressed || Game.Keyboard.Keys[Key.P].Pressed) &&
                    PickTile(out pickedTile, out pickedDirection))
                {
                    Game.Screen.InputMethod = InputMethod.Mouse;
                    m_tileSelect.PickTile(pickedTile, pickedDirection);
                }
            }

            // Preview
            {
                TileCoordinates position;
                Tile tile;
                FlatDirection direction;
                bool placement = GetPotentialPlacement(out position, out tile, out direction, m_outline);
                if (m_pendingChanges.Count == 0)
                {
                    if (placement)
                    {
                        m_previewEntity.Visible = true;
                        m_previewEntity.Location = position;
                        m_previewEntity.SetTile(tile, direction);
                    }
                    else
                    {
                        m_previewEntity.Visible = false;
                    }
                }
            }

            // Dragging
            if (Game.Mouse.Buttons[MouseButton.Left].Held)
            {
                Game.Screen.InputMethod = InputMethod.Mouse;
                QueuePlacement();
            }
            if (Game.Mouse.Buttons[MouseButton.Left].Released)
            {
                Game.Screen.InputMethod = InputMethod.Mouse;
                CommitPlacements();
            }

            // Status text
            if (m_statusTimeout > 0.0f)
            {
                m_statusTimeout -= dt;
                if (m_statusTimeout <= 0.0f)
                {
                    m_statusText.String = "";
                }
            }
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            ShowPlacementsLeft = !m_tileSelect.IsFullscreen;
            m_grid.Draw(Game.Camera);
            m_outline.Draw(Game.Camera);
        }

        protected override void OnShutdown()
        {
            // Disable camera control
            CameraController.AllowUserRotate = false;
            CameraController.AllowUserZoom = false;
            CameraController.AllowUserPan = false;

            // Remove GUI elements
            Game.Screen.Elements.Remove(m_menuButton);
            m_menuButton.Dispose();
            m_menuButton = null;

            Game.Screen.Elements.Remove(m_saveButton);
            m_saveButton.Dispose();
            m_saveButton = null;

            Game.Screen.Elements.Remove(m_testButton);
            m_testButton.Dispose();
            m_saveButton = null;

            Game.Screen.Elements.Remove(m_titleText);
            m_titleText.Dispose();
            m_titleText = null;

            Game.Screen.Elements.Remove(m_statusText);
            m_statusText.Dispose();
            m_statusText = null;

            Game.Screen.Elements.Remove(m_tileSelect);
            m_tileSelect.Dispose();
            m_tileSelect = null;

            // Dispose things
            m_grid.Dispose();
            m_outline.Dispose();

            base.OnShutdown();
        }

        private void Undo()
        {
            if (Level.Tiles.Undo())
            {
                Level.Tiles.Compress();
                m_grid.Rebuild();
                UpdateCameraBounds();
                Level.Info.EverCompleted = false;
                m_modified = true;
            }
        }

        private void SaveLevel()
        {
            // Save
            string assetPath = LevelSavePath;
            if (Level.Info.ID == 0 && assetPath != "levels/template.level")
            {
                Level.Info.ID = MathUtils.GenerateLevelID(assetPath);
            }
            if (m_mod != null)
            {
                var fullPath = Path.Combine(m_mod.Path, "assets/" + assetPath);
                Level.Save(fullPath);
                App.Log("Saved level to " + assetPath);
            }
            else
            {
                var fullPath = Path.Combine(App.AssetPath, "main/" + assetPath);
                Level.Save(fullPath);
                App.Log("Saved level to " + assetPath);
            }
            Assets.Reload(assetPath);
            SetStatus(Game.Language.Translate("menus.editor.saved", assetPath));
            m_modified = false;
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

        private void TestLevel()
        {
            if (Level.Info.ScriptPath != null)
            {
                Assets.Reload(Level.Info.ScriptPath);
            }
            if (m_modified)
            {
                var tempPath = SaveLevelTemporary();
                CutToState(new TestState(Game, m_mod, m_campaign, m_levelIndex, tempPath, LevelSavePath));
            }
            else
            {
                CutToState(new TestState(Game, m_mod, m_campaign, m_levelIndex, LevelSavePath, LevelSavePath));
            }
        }

        private void ReloadLevel()
        {
            Assets.Reload(LevelSavePath);
            if (Level.Info.ScriptPath != null)
            {
                Assets.Reload(Level.Info.ScriptPath);
            }
            if (Assets.Exists<LevelData>(LevelSavePath))
            {
                CutToState(new EditorState(Game, m_mod, m_campaign, m_levelIndex, LevelSavePath, LevelSavePath));
            }
            else
            {
                CutToState(new EditorState(Game, m_mod, m_campaign, m_levelIndex, "levels/template.level", LevelSavePath));
            }
        }

        public override void OnReloadAssets()
        {
            base.OnReloadAssets();
            m_tileSelect.ReloadAssets();
        }

        private void SetStatus(string statusText)
        {
            App.Log(statusText, LogLevel.User);
            m_statusText.String = statusText;
            m_statusTimeout = STATUS_TIMEOUT;
        }

        private void ShowGuide()
        {
            Game.Network.OpenWebBrowser(GUIDE_URL, WebBrowserType.Overlay);
        }

        private void BackToMenu()
        {
            if (m_campaign != null)
            {
                int page = 0;
                for (int i = 0; i < m_campaign.Levels.Count; ++i)
                {
                    if (m_campaign.Levels[i] == LevelSavePath)
                    {
                        page = i / LevelSelectState.NUM_PER_PAGE;
                        break;
                    }
                }
                WipeToState(new LevelSelectState(Game, m_mod, m_campaign, page, -1, editor: true));
            }
            else if (m_mod != null)
            {
                WipeToState(new ModEditorState(Game, m_mod));
            }
            else
            {
                WipeToState(new MainMenuState(Game));
            }
        }
    }
}
