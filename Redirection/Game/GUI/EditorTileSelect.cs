using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class EditorTileSelect : Element
    {
        private const int SIDEBAR_ROWS = 6;
        private static string[] DEFAULT_SIDEBAR = new string[] {
            "tiles/ship/step.tile",
            "tiles/ship/half_machine.tile",
            "tiles/new/blue_spawn.tile",
            "tiles/classic/bluegoal.tile",
            "tiles/new/red_spawn.tile",
            "tiles/classic/redgoal.tile",
        };
        private static FlatDirection[] DEFAULT_SIDEBAR_DIRECTION = new FlatDirection[] {
            FlatDirection.North,
            FlatDirection.North,
            FlatDirection.South,
            FlatDirection.North,
            FlatDirection.South,
            FlatDirection.North,
        };

        private Game.Game m_game;
        private Settings m_settings;
        private Language m_language;

        private Texture m_texture;
        private Geometry m_geometry;

        private TileList m_tileList;
        private Level.Level[] m_sidebarLevels;
        private Level.Level[] m_paletteLevels;

        private int m_dragStart;
        private int m_dragStartRight;
        private int m_hover;
        private int m_selection;

        private int m_paletteDragStart;
        private int m_paletteDragStartRight;
        private int m_palettePage;
        private bool m_paletteOpen;
        private int m_paletteHover;

        private Text m_palettePageText;
        private Text m_paletteHoverText;

        public Tile SelectedTile
        {
            get
            {
                var level = m_sidebarLevels[m_selection];
                return level.Tiles[TileCoordinates.Zero];
            }
        }

        public FlatDirection SelectedTileDirection
        {
            get
            {
                var level = m_sidebarLevels[m_selection];
                return level.Tiles[TileCoordinates.Zero].GetDirection(level, TileCoordinates.Zero);
            }
        }

        public bool IsFullscreen
        {
            get
            {
                return m_paletteOpen;
            }
        }

        public EditorTileSelect(Game.Game game, Settings settings, Language language)
        {
            m_game = game;
            m_settings = settings;
            m_language = language;

            m_texture = Texture.Get("gui/tileselect.png", true);
            int numQuads = SIDEBAR_ROWS + 2;
            m_geometry = new Geometry(Primitive.Triangles, numQuads * 4, numQuads * 6);

            m_tileList = new TileList(
                "tiles"
            );
            m_sidebarLevels = new Level.Level[SIDEBAR_ROWS];
            m_paletteLevels = new Level.Level[m_tileList.Count];

            m_dragStart = -1;
            m_dragStartRight = -1;
            m_hover = -1;
            m_selection = m_settings.EditorSelection;
            m_palettePage = m_settings.EditorPage;

            m_paletteDragStart = -1;
            m_paletteDragStartRight = -1;
            m_paletteHover = -1;
            m_palettePage = 0;
            m_paletteOpen = false;

            var pallete = m_settings.EditorPalette;
            var directions = m_settings.EditorDirections;
            for (int i = 0; i < m_sidebarLevels.Length; ++i)
            {
                m_sidebarLevels[i] = new Level.Level(0, 0, 0, 1, 1, 1);
                m_sidebarLevels[i].Info.InEditor = true;

                var tilePath = (i < pallete.Length) ? pallete[i] : DEFAULT_SIDEBAR[i];
                var tile = Assets.Exists<Tile>(tilePath) ? Tile.Get(tilePath) : Tile.Get(DEFAULT_SIDEBAR[i]);
                var direction = (i < directions.Length) ? directions[i] : DEFAULT_SIDEBAR_DIRECTION[i];
                m_sidebarLevels[i].Tiles.SetTile(
                    TileCoordinates.Zero,
                    tile,
                    direction,
                    false
                );
            }

            var selectedDirection = SelectedTileDirection;
            for (int i = 0; i < m_paletteLevels.Length; ++i)
            {
                m_paletteLevels[i] = new Level.Level(0, 0, 0, 1, 1, 1);
                m_paletteLevels[i].Info.InEditor = true;
                m_paletteLevels[i].Tiles.SetTile(
                    TileCoordinates.Zero,
                    m_tileList[i],
                    selectedDirection,
                    false
                );
            }

            m_palettePageText = new Text(UIFonts.Smaller, "", UIColours.Text, TextAlignment.Right);
            m_palettePageText.Visible = false;
            m_palettePageText.Anchor = Anchor.BottomRight;
            m_palettePageText.LocalPosition = new Vector2(
                -16.0f,
                -16.0f - m_palettePageText.Font.Height
            );

            m_paletteHoverText = new Text(UIFonts.Smaller, "", UIColours.Text, TextAlignment.Right);
            m_paletteHoverText.Visible = false;
            m_paletteHoverText.Anchor = Anchor.BottomRight;
            m_paletteHoverText.LocalPosition = new Vector2(
                -16.0f,
                -16.0f - m_palettePageText.Font.Height - m_paletteHoverText.Font.Height
            );
        }

        public override void Dispose()
        {
            m_geometry.Dispose();
            for (int i = 0; i < m_sidebarLevels.Length; ++i)
            {
                m_sidebarLevels[i].Dispose();
            }
            for (int i = 0; i < m_paletteLevels.Length; ++i)
            {
                m_paletteLevels[i].Dispose();
            }

            Screen.Elements.Remove(m_paletteHoverText);
            m_paletteHoverText.Dispose();
            m_paletteHoverText = null;

            Screen.Elements.Remove(m_palettePageText);
            m_palettePageText.Dispose();
            m_palettePageText = null;
        }

        protected override void OnInit()
        {
            Screen.Elements.Add(m_paletteHoverText);
            Screen.Elements.Add(m_palettePageText);
        }

        protected override void OnUpdate(float dt)
        {
            // Update levels
            for (int i = 0; i < m_sidebarLevels.Length; ++i)
            {
                m_sidebarLevels[i].Update(dt);
            }
            for (int i = 0; i < m_paletteLevels.Length; ++i)
            {
                m_paletteLevels[i].Update(dt);
            }

            // Update keyboard
            if (Screen.ModalDialog == this || Screen.ModalDialog == null)
            {
                if (Screen.Keyboard.Keys[Key.Left].Pressed)
                {
                    if (m_paletteOpen)
                    {
                        SelectPreviousPage();
                        RequestRebuild();
                    }
                    else
                    {
                        SelectPreviousTileRotation();
                    }
                }
                if (Screen.Keyboard.Keys[Key.Right].Pressed)
                {
                    if (m_paletteOpen)
                    {
                        SelectNextPage();
                        RequestRebuild();
                    }
                    else
                    {
                        SelectNextTileRotation();
                    }
                }
                if (Screen.Keyboard.Keys[Key.Up].Pressed)
                {
                    m_selection = (m_selection + SIDEBAR_ROWS - 1) % SIDEBAR_ROWS;
                    StoreSettings();
                    SetTileRotation(SelectedTileDirection);
                    RequestRebuild();
                }
                if (Screen.Keyboard.Keys[Key.Down].Pressed)
                {
                    m_selection = (m_selection + 1) % SIDEBAR_ROWS;
                    StoreSettings();
                    SetTileRotation(SelectedTileDirection);
                    RequestRebuild();
                }
                for (int i = 0; i < SIDEBAR_ROWS; ++i)
                {
                    var key = (Key)(Key.One + i);
                    if (Screen.Keyboard.Keys[key].Pressed)
                    {
                        m_selection = i;
                        StoreSettings();
                        SetTileRotation(SelectedTileDirection);
                        RequestRebuild();
                    }
                }
                if (Screen.Keyboard.Keys[Key.Escape].Pressed)
                {
                    m_paletteOpen = false;
                    RequestRebuild();
                }
            }

            // Update hover
            int sidebarMouseIndex = TestSidebar();
            if (sidebarMouseIndex != m_hover)
            {
                m_hover = sidebarMouseIndex;
                RequestRebuild();
            }
            int paletteMouseIndex = TestPalette();
            if (m_paletteHover != paletteMouseIndex)
            {
                m_paletteHover = paletteMouseIndex;
                if (m_paletteOpen)
                {
                    RequestRebuild();
                }
            }

            if (Screen.ModalDialog == this || Screen.ModalDialog == null)
            {
                // Update click
                if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    m_dragStart = m_hover;
                    m_paletteDragStart = m_paletteHover;
                }
                if (Screen.Mouse.Buttons[MouseButton.Left].Released)
                {
                    if (m_hover >= 0 && m_hover == m_dragStart)
                    {
                        m_selection = m_hover;
                        m_paletteOpen = false;
                        SetTileRotation(SelectedTileDirection);
                        StoreSettings();
                        RequestRebuild();
                    }
                    if (m_paletteOpen && m_paletteHover >= 0 && m_paletteHover == m_paletteDragStart)
                    {
                        var palleteLevel = m_paletteLevels[m_paletteHover];
                        var sideBarLevel = m_sidebarLevels[m_selection];
                        sideBarLevel.Tiles.SetTile(
                            TileCoordinates.Zero,
                            palleteLevel.Tiles[TileCoordinates.Zero],
                            sideBarLevel.Tiles[TileCoordinates.Zero].GetDirection(sideBarLevel, TileCoordinates.Zero),
                            false
                        );
                        sideBarLevel.Tiles.Compress();
                        m_paletteOpen = false;
                        StoreSettings();
                        RequestRebuild();
                    }
                }
                if (Screen.Mouse.Buttons[MouseButton.Right].Pressed)
                {
                    m_dragStartRight = m_hover;
                    m_paletteDragStartRight = m_paletteHover;
                }
                if (Screen.Mouse.Buttons[MouseButton.Right].Released)
                {
                    if (m_hover >= 0 && m_hover == m_dragStartRight)
                    {
                        m_selection = m_hover;
                        m_paletteOpen = true;
                        SetTileRotation(SelectedTileDirection);
                        StoreSettings();
                        RequestRebuild();
                    }
                    if (m_paletteOpen && m_paletteHover >= 0 && m_paletteHover == m_paletteDragStartRight)
                    {
                        var palleteLevel = m_paletteLevels[m_paletteHover];
                        var sideBarLevel = m_sidebarLevels[m_selection];
                        sideBarLevel.Tiles.SetTile(
                            TileCoordinates.Zero,
                            palleteLevel.Tiles[TileCoordinates.Zero],
                            sideBarLevel.Tiles[TileCoordinates.Zero].GetDirection(sideBarLevel, TileCoordinates.Zero),
                            false
                        );
                        sideBarLevel.Tiles.Compress();
                        m_paletteOpen = true;
                        StoreSettings();
                        RequestRebuild();
                    }
                }
            }
            else
            {
                m_dragStart = -1;
                m_dragStartRight = -1;
                m_paletteDragStart = -1;
                m_paletteDragStartRight = -1;
            }

            // Update hover text
            if (m_paletteOpen && m_paletteHover >= 0)
            {
                m_paletteHoverText.Visible = true;
                m_paletteHoverText.String = m_paletteLevels[m_paletteHover].Tiles[TileCoordinates.Zero].Path;
            }
            else
            {
                m_paletteHoverText.Visible = false;
            }

            // Update page text
            if (m_paletteOpen)
            {
                int numColumns = (int)((Screen.Width - 85.0f) / 80.0f);
                int numRows = 5;
                int numPerPage = numColumns * numRows;
                int numPages = ((m_paletteLevels.Length - 1) / numPerPage) + 1;
                m_palettePageText.Visible = true;
                m_palettePageText.String = "[gui/prompts/keyboard/left.png] " + m_language.Translate("menus.editor.palette_page", m_palettePage + 1, numPages) + " [gui/prompts/keyboard/right.png]";
            }
            else
            {
                m_palettePageText.Visible = false;
            }
        }

        public void PickTile(Tile tile, FlatDirection direction)
        {
            // Try to switch to an existing slot first
            for (int i = 0; i < m_sidebarLevels.Length; ++i)
            {
                var level = m_sidebarLevels[i];
                if (level.Tiles[TileCoordinates.Zero] == tile)
                {
                    m_selection = i;
                    SetTileRotation(direction);
                    StoreSettings();
                    RequestRebuild();
                    return;
                }
            }

            // Otherwise, change the current slot
            var sideBarLevel = m_sidebarLevels[m_selection];
            sideBarLevel.Tiles.SetTile(
                TileCoordinates.Zero,
                tile,
                direction,
                false
            );
            sideBarLevel.Tiles.Compress();
            SetTileRotation(direction);
            StoreSettings();
        }

        public void ReloadAssets()
        {
            // Reload sidebar
            for (int i = 0; i < m_sidebarLevels.Length; ++i)
            {
                m_sidebarLevels[i].Tiles.RequestRebuild();
            }

            // Reload pallette
            m_tileList.Reload();
            var selectedDirection = SelectedTileDirection;
            for (int i = 0; i < m_paletteLevels.Length; ++i)
            {
                m_paletteLevels[i].Dispose();
            }
            m_paletteLevels = new Dan200.Game.Level.Level[m_tileList.Count];
            for (int i = 0; i < m_paletteLevels.Length; ++i)
            {
                m_paletteLevels[i] = new Level.Level(0, 0, 0, 1, 1, 1);
                m_paletteLevels[i].Info.InEditor = true;
                m_paletteLevels[i].Tiles.SetTile(
                    TileCoordinates.Zero,
                    m_tileList[i],
                    selectedDirection,
                    false
                );
            }
        }

        private void SetTileRotation(FlatDirection direction)
        {
            var sideBarLevel = m_sidebarLevels[m_selection];
            sideBarLevel.Tiles.SetTile(
                TileCoordinates.Zero,
                sideBarLevel.Tiles[TileCoordinates.Zero],
                direction,
                false
            );

            for (int i = 0; i < m_paletteLevels.Length; ++i)
            {
                var level = m_paletteLevels[i];
                level.Tiles.SetTile(
                    TileCoordinates.Zero,
                    level.Tiles[TileCoordinates.Zero],
                    direction
                );
            }
        }

        private void SelectPreviousTileRotation()
        {
            SetTileRotation(SelectedTileDirection.RotateLeft());
            StoreSettings();
        }

        private void SelectNextTileRotation()
        {
            SetTileRotation(SelectedTileDirection.RotateRight());
            StoreSettings();
        }

        private void SelectPreviousPage()
        {
            int numColumns = (int)((Screen.Width - 85.0f) / 80.0f);
            int numRows = 5;
            int numPerPage = numColumns * numRows;
            int numPages = ((m_paletteLevels.Length - 1) / numPerPage) + 1;
            m_palettePage = (m_palettePage + numPages - 1) % numPages;
            StoreSettings();
        }

        private void SelectNextPage()
        {
            int numColumns = (int)((Screen.Width - 85.0f) / 80.0f);
            int numRows = 5;
            int numPerPage = numColumns * numRows;
            int numPages = ((m_paletteLevels.Length - 1) / numPerPage) + 1;
            m_palettePage = (m_palettePage + 1) % numPages;
            StoreSettings();
        }

        private bool IsObscured()
        {
            return Screen.ModalDialog is DialogBox && ((DialogBox)Screen.ModalDialog).Width > (Screen.Width - 160.0f);
        }

        protected override void OnDraw()
        {
            if (IsObscured())
            {
                return;
            }

            // Draw the background
            Screen.Effect.Colour = Vector4.One;
            Screen.Effect.Texture = m_texture;
            Screen.Effect.Bind();
            m_geometry.Draw();
        }

        private void SetLevelTransform(Level.Level level, Vector2 screenPosition)
        {
            float x = (screenPosition.X / (0.5f * Screen.Width)) - 1.0f;
            float y = (screenPosition.Y / (0.5f * Screen.Height)) - 1.0f;
            Vector3 dirCS = new Vector3(
                (float)(Math.Tan(0.5f * m_game.Camera.FOV)) * (x * m_game.Camera.AspectRatio),
                -(float)(Math.Tan(0.5f * m_game.Camera.FOV)) * y,
                -1.0f
            );
            dirCS.Normalize();

            Matrix4 cameraTransInv = m_game.Camera.Transform;
            MathUtils.FastInvert(ref cameraTransInv);

            var posWS = Vector3.TransformPosition(Vector3.Zero, cameraTransInv);
            var dirWS = Vector3.TransformVector(dirCS, cameraTransInv);

            level.Transform = Matrix4.CreateTranslation(
                posWS + 22.0f * dirWS - new Vector3(0.5f, 0.25f * level.Tiles.Height, 0.5f)
            );
        }

        private void UpdateLighting(Level.Level level)
        {
            if (m_game.Sky != null)
            {
                level.Lights.AmbientLight.Colour = m_game.Sky.AmbientColour;
                level.Lights.SkyLight.Active = (m_game.Sky.LightColour.LengthSquared > 0.0f);
                level.Lights.SkyLight.Colour = m_game.Sky.LightColour;
                level.Lights.SkyLight.Direction = m_game.Sky.LightDirection;
                level.Lights.SkyLight2.Active = (m_game.Sky.Light2Colour.LengthSquared > 0.0f);
                level.Lights.SkyLight2.Colour = m_game.Sky.Light2Colour;
                level.Lights.SkyLight2.Direction = m_game.Sky.Light2Direction;
            }
        }

        protected override void OnDraw3D()
        {
            if (IsObscured())
            {
                return;
            }

            // Draw the sidebar
            for (int i = 0; i < m_sidebarLevels.Length; ++i)
            {
                var level = m_sidebarLevels[i];
                SetLevelTransform(level, new Vector2(40.0f, 40.0f + (i * 80.0f)));
                UpdateLighting(level);
                level.Draw(m_game.Camera, drawShadows: false);
            }

            // Draw the palette
            if (m_paletteOpen)
            {
                int numColumns = (int)((Screen.Width - 85.0f) / 80.0f);
                int numRows = 5;
                if (numColumns > 0)
                {
                    int numPerPage = numColumns * numRows;
                    int start = m_palettePage * numPerPage;
                    int end = Math.Min(m_paletteLevels.Length, (m_palettePage + 1) * numPerPage);
                    for (int i = start; i < end; ++i)
                    {
                        var level = m_paletteLevels[i];
                        int x = ((i - start) % numColumns);
                        int y = ((i - start) / numColumns);
                        SetLevelTransform(level, new Vector2(125.0f + (x * 80.0f), 40.0f + (y * 80.0f)));
                        UpdateLighting(level);
                        level.Draw(m_game.Camera, drawShadows: false);
                    }
                }
            }
        }

        protected override void OnRebuild()
        {
            m_geometry.Clear();
            if (m_paletteOpen)
            {
                m_geometry.Add2DQuad(
                    new Vector2(0.0f, 0.0f),
                    new Vector2(Screen.Width, Screen.Height),
                    new Quad(0.5f, 0.5f, 0.5f, 0.5f)
                );

                int numColumns = (int)((Screen.Width - 85.0f) / 80.0f);
                int numRows = 5;
                if (numColumns > 0)
                {
                    int numPerPage = numColumns * numRows;
                    int start = m_palettePage * numPerPage;

                    int paletteSelect = m_tileList.GetTileIndex(SelectedTile);
                    if (paletteSelect >= 0)
                    {
                        int x = (paletteSelect - start) % numColumns;
                        int y = (paletteSelect - start) / numColumns;
                        if (y >= 0 && y < numRows)
                        {
                            m_geometry.Add2DQuad(
                                new Vector2(85.0f + x * 80.0f, y * 80.0f),
                                new Vector2(85.0f + x * 80.0f + 80.0f, y * 80.0f + 80.0f),
                                new Quad(0.0f, 0.5f, 0.5f * (80.0f / 128.0f), 0.5f * (80.0f / 128.0f))
                            );
                        }
                    }
                    if (m_paletteHover >= 0 && m_paletteHover != paletteSelect)
                    {
                        int x = (m_paletteHover - start) % numColumns;
                        int y = (m_paletteHover - start) / numColumns;
                        if (y >= 0 && y < numRows)
                        {
                            m_geometry.Add2DQuad(
                                new Vector2(85.0f + x * 80.0f, y * 80.0f),
                                new Vector2(85.0f + x * 80.0f + 80.0f, y * 80.0f + 80.0f),
                                new Quad(0.5f, 0.0f, 0.5f * (80.0f / 128.0f), 0.5f * (80.0f / 128.0f))
                            );
                        }
                    }
                }
            }
            for (int i = 0; i < SIDEBAR_ROWS; ++i)
            {
                if (i == m_selection)
                {
                    m_geometry.Add2DQuad(
                        new Vector2(0.0f, i * 80.0f),
                        new Vector2(96.0f, i * 80.0f + 128.0f),
                        new Quad(0.0f, 0.5f, 0.375f, 0.5f)
                    );
                }
                else if (i == m_hover)
                {
                    m_geometry.Add2DQuad(
                        new Vector2(0.0f, i * 80.0f),
                        new Vector2(96.0f, i * 80.0f + 128.0f),
                        new Quad(0.5f, 0.0f, 0.375f, 0.5f)
                    );
                }
                else
                {
                    m_geometry.Add2DQuad(
                        new Vector2(0.0f, i * 80.0f),
                        new Vector2(96.0f, i * 80.0f + 128.0f),
                        new Quad(0.0f, 0.0f, 0.375f, 0.5f)
                    );
                }
            }
            m_geometry.Rebuild();
        }

        public bool TestMouse()
        {
            return m_paletteOpen || Screen.MousePosition.X < 83.0f;
        }

        private int TestSidebar()
        {
            if (Screen.ModalDialog == this || Screen.ModalDialog == null)
            {
                for (int i = 0; i < SIDEBAR_ROWS; ++i)
                {
                    float localX = Screen.MousePosition.X;
                    float localY = Screen.MousePosition.Y - (i * 80.0f);
                    if (localX >= 0.0f && localX < 80.0f &&
                        localY >= 0.0f && localY < 80.0f)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private int TestPalette()
        {
            if (Screen.ModalDialog == this || Screen.ModalDialog == null)
            {
                int numColumns = (int)((Screen.Width - 85.0f) / 80.0f);
                int numRows = 5;
                if (numColumns > 0)
                {
                    int numPerPage = numColumns * numRows;
                    int start = m_palettePage * numPerPage;
                    int end = Math.Min(m_paletteLevels.Length, (m_palettePage + 1) * numPerPage);
                    for (int i = start; i < end; ++i)
                    {
                        int x = ((i - start) % numColumns);
                        int y = ((i - start) / numColumns);
                        float localX = Screen.MousePosition.X - (85.0f + x * 80.0f);
                        float localY = Screen.MousePosition.Y - (y * 80.0f);
                        if (localX >= 0.0f && localX < 80.0f &&
                            localY >= 0.0f && localY < 80.0f)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        private void StoreSettings()
        {
            var palette = new string[SIDEBAR_ROWS];
            var directions = new FlatDirection[SIDEBAR_ROWS];
            for (int i = 0; i < SIDEBAR_ROWS; ++i)
            {
                var level = m_sidebarLevels[i];
                var tile = level.Tiles[TileCoordinates.Zero];
                palette[i] = tile.Path;
                directions[i] = tile.GetDirection(level, TileCoordinates.Zero);
            }
            m_settings.EditorPalette = palette;
            m_settings.EditorDirections = directions;
            m_settings.EditorSelection = m_selection;
            m_settings.EditorPage = m_palettePage;
            m_settings.Save();
        }
    }
}

