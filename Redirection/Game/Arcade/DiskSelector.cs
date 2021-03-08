using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.User;
using OpenTK;
using System;
using System.Linq;

namespace Dan200.Game.Arcade
{
    public class DiskSelectEventArgs : EventArgs
    {
        public readonly ArcadeDisk Disk;
        public readonly Mod Mod;

        public DiskSelectEventArgs(ArcadeDisk disk, Mod mod)
        {
            Disk = disk;
            Mod = mod;
        }
    }

    public class DiskSelector : Element
    {
        private static Vector4 BACKGROUND_COLOUR = new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
        private static Vector4 LOCKED_COLOUR = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        private const int ROWS_PER_PAGE = 2;
        private const int COLUMNS_PER_PAGE = 3;
        private const int NUM_PER_PAGE = ROWS_PER_PAGE * COLUMNS_PER_PAGE;
        private const float DISK_SIZE = 180.0f;
        private const float DISK_PADDING = 20.0f;

        private Geometry m_geometry;
        private int m_initialDisk;
        private DiskWithMod[] m_disks;
        private bool[] m_disksUnlocked;
        private Texture[] m_diskLabels;

        private int m_page; // Current page
        private int m_numColumns; // Total number of columns
        private int m_numRows; // Total number of rows

        private int m_highlight;

        private InputPrompt m_backPrompt;
        private InputPrompt m_selectPrompt;
        private InputPrompt m_browseWorkshopPrompt;
        private Button m_previousPageButton;
        private Button m_nextPageButton;

        private int m_framesOpen;
        private bool m_closeNextFrame;

        public event EventHandler<DiskSelectEventArgs> OnSelect;
        public event EventHandler<EventArgs> OnBrowseWorkshop;
        public event EventHandler<EventArgs> OnClose;

        public DiskSelector(Screen screen, string initialDiskPath, Mod initialDiskMod, Progress progress)
        {
            m_geometry = new Geometry(Primitive.Triangles);
            m_disks = ArcadeUtils.GetAllDisks().ToArray();
            m_disksUnlocked = m_disks.Select(disk => ArcadeUtils.IsDiskUnlocked(disk.Disk, disk.Mod, progress)).ToArray();

            m_page = 0;
            if (m_disks.Length <= COLUMNS_PER_PAGE)
            {
                m_numColumns = m_disks.Length;
                m_numRows = 1;
            }
            else if (m_disks.Length < NUM_PER_PAGE)
            {
                m_numColumns = (m_disks.Length + ROWS_PER_PAGE - 1) / ROWS_PER_PAGE;
                m_numRows = ROWS_PER_PAGE;
            }
            else
            {
                m_numColumns = COLUMNS_PER_PAGE;
                m_numRows = ROWS_PER_PAGE;
            }
            m_highlight = -1;

            m_backPrompt = new InputPrompt(UIFonts.Smaller, screen.Language.Translate("menus.close"), TextAlignment.Right);
            m_backPrompt.Key = Key.Escape;
            m_backPrompt.MouseButton = MouseButton.Left;
            m_backPrompt.GamepadButton = GamepadButton.B;
            m_backPrompt.SteamControllerButton = SteamControllerButton.MenuBack;
            m_backPrompt.Anchor = Anchor.BottomRight;
            m_backPrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_backPrompt.Height);
            m_backPrompt.Parent = this;
            m_backPrompt.OnClick += delegate (object o, EventArgs args)
            {
                m_closeNextFrame = true;
            };

            m_selectPrompt = new InputPrompt(UIFonts.Smaller, screen.Language.Translate("menus.select"), TextAlignment.Left);
            m_selectPrompt.Key = Key.Return;
            m_selectPrompt.GamepadButton = GamepadButton.A;
            m_selectPrompt.SteamControllerButton = SteamControllerButton.MenuSelect;
            m_selectPrompt.Anchor = Anchor.BottomLeft;
            m_selectPrompt.LocalPosition = new Vector2(16.0f, -16.0f - m_selectPrompt.Height);
            m_selectPrompt.Parent = this;

            m_browseWorkshopPrompt = new InputPrompt(UIFonts.Smaller, screen.Language.Translate("menus.arcade.browse_workshop"), TextAlignment.Right);
            m_browseWorkshopPrompt.Key = Key.LeftCtrl;
            m_browseWorkshopPrompt.GamepadButton = GamepadButton.Y;
            m_browseWorkshopPrompt.SteamControllerButton = SteamControllerButton.MenuAltSelect;
            m_browseWorkshopPrompt.Anchor = Anchor.BottomRight;
            m_browseWorkshopPrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_selectPrompt.Height - m_browseWorkshopPrompt.Height);
            m_browseWorkshopPrompt.Parent = this;

            m_previousPageButton = new Button(Texture.Get("gui/arrows.png", true), 32.0f, 32.0f);
            m_previousPageButton.Region = new Quad(0.0f, 0.5f, 0.5f, 0.5f);
            m_previousPageButton.HighlightRegion = m_previousPageButton.Region;
            m_previousPageButton.DisabledRegion = m_previousPageButton.Region;
            m_previousPageButton.ShortcutButton = GamepadButton.LeftBumper;
            m_previousPageButton.AltShortcutButton = GamepadButton.LeftTrigger;
            m_previousPageButton.ShortcutSteamControllerButton = SteamControllerButton.MenuPreviousPage;
            m_previousPageButton.Colour = UIColours.Title;
            m_previousPageButton.HighlightColour = UIColours.White;
            m_previousPageButton.DisabledColour = m_previousPageButton.Colour;
            m_previousPageButton.Anchor = Anchor.CentreMiddle;
            m_previousPageButton.LocalPosition = new Vector2(
                -0.5f * (float)COLUMNS_PER_PAGE * (DISK_SIZE + DISK_PADDING) - m_previousPageButton.Width,
                -0.5f * m_previousPageButton.Height
            );
            m_previousPageButton.Parent = this;
            m_previousPageButton.OnClicked += delegate (object o, EventArgs e)
            {
                PreviousPage();
            };

            m_nextPageButton = new Button(Texture.Get("gui/arrows.png", true), 32.0f, 32.0f);
            m_nextPageButton.Region = new Quad(0.0f, 0.0f, 0.5f, 0.5f);
            m_nextPageButton.HighlightRegion = m_nextPageButton.Region;
            m_nextPageButton.DisabledRegion = m_nextPageButton.Region;
            m_nextPageButton.ShortcutButton = GamepadButton.RightBumper;
            m_nextPageButton.AltShortcutButton = GamepadButton.RightTrigger;
            m_nextPageButton.ShortcutSteamControllerButton = SteamControllerButton.MenuNextPage;
            m_nextPageButton.Colour = UIColours.Title;
            m_nextPageButton.HighlightColour = UIColours.White;
            m_nextPageButton.DisabledColour = m_nextPageButton.Colour;
            m_nextPageButton.Anchor = Anchor.CentreMiddle;
            m_nextPageButton.LocalPosition = new Vector2(
                0.5f * (float)COLUMNS_PER_PAGE * (DISK_SIZE + DISK_PADDING),
                -0.5f * m_previousPageButton.Height
            );
            m_nextPageButton.Parent = this;
            m_nextPageButton.OnClicked += delegate (object o, EventArgs e)
            {
                NextPage();
            };

            // Load labels
            m_diskLabels = new Texture[m_disks.Length];
            for (int i = 0; i < m_disks.Length; ++i)
            {
                var disk = m_disks[i];
                var labelPath = AssetPath.ChangeExtension(disk.Disk.Path, "png");
                if (disk.Mod != null)
                {
                    if (disk.Mod.Assets.CanLoad(labelPath))
                    {
                        m_diskLabels[i] = disk.Mod.Assets.Load<Texture>(labelPath);
                        m_diskLabels[i].Filter = false;
                    }
                }
                else
                {
                    m_diskLabels[i] = Texture.Get(labelPath, false);
                }
            }

            m_framesOpen = 0;
            m_closeNextFrame = false;

            // Determine initial disk index
            m_initialDisk = -1;
            if (initialDiskPath != null && m_disks.Length > 0)
            {
                for (int i = 0; i < m_disks.Length; ++i)
                {
                    var disk = m_disks[i];
                    if (disk.Disk.Path == initialDiskPath &&
                        disk.Mod == initialDiskMod &&
                        m_disksUnlocked[i] )
                    {
                        m_initialDisk = i;
                        break;
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
            m_backPrompt.Dispose();
            m_selectPrompt.Dispose();
            m_browseWorkshopPrompt.Dispose();
            m_nextPageButton.Dispose();
            m_previousPageButton.Dispose();

            // Dispose labels
            for (int i = 0; i < m_disks.Length; ++i)
            {
                var disk = m_disks[i];
                if (disk.Mod != null && m_diskLabels[i] != null)
                {
                    m_diskLabels[i].Dispose();
                }
            }
        }

        protected override void OnInit()
        {
            // Init self
            Screen.ModalDialog = this;

            // Initialise highlight
            var initialDisk = m_initialDisk;
            if (initialDisk < 0 && m_disks.Length > 0)
            {
                for( int i=0; i<m_disks.Length; ++i )
                {
                    if( m_disksUnlocked[i] )
                    {
                        initialDisk = i;
                        break;
                    }
                }
            }
            m_page = initialDisk / NUM_PER_PAGE;
            if (Screen.InputMethod != InputMethod.Mouse)
            {
                m_highlight = initialDisk;
            }
            else
            {
                m_highlight = TestMouse();
            }

            var lastPage = (m_disks.Length - 1) / NUM_PER_PAGE;
            m_previousPageButton.Visible = m_page > 0;
            m_nextPageButton.Visible = m_page < lastPage;

            // Init children
            m_backPrompt.Init(Screen);
            m_selectPrompt.Init(Screen);
            m_browseWorkshopPrompt.Init(Screen);
            m_previousPageButton.Init(Screen);
            m_nextPageButton.Init(Screen);
        }

        private void SetHighlight(int highlight)
        {
            if (m_highlight != highlight)
            {
                m_highlight = highlight;
                if (highlight >= 0)
                {
                    var page = highlight / NUM_PER_PAGE;
                    if (page != m_page)
                    {
                        var lastPage = (m_disks.Length - 1) / NUM_PER_PAGE;
                        m_page = page;
                        m_previousPageButton.Visible = m_page > 0;
                        m_nextPageButton.Visible = m_page < lastPage;
                    }
                    PlayHighlightSound();
                }
                RequestRebuild();
            }
        }

        private int GetFirstUnlockedDiskOnPage()
        {
            int first = m_page * NUM_PER_PAGE;
            int last = Math.Min(first + NUM_PER_PAGE, m_disks.Length);
            for (int i = first; i < last; ++i)
            {
                if (m_disksUnlocked[i])
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetLastUnlockedDiskOnPage()
        {
            int first = m_page * NUM_PER_PAGE;
            int last = Math.Min(first + NUM_PER_PAGE, m_disks.Length);
            for (int i = last -1; i >= first; --i)
            {
                if (m_disksUnlocked[i])
                {
                    return i;
                }
            }
            return -1;
        }

        private void PreviousPage()
        {
            if (m_page > 0)
            {
                var lastPage = (m_disks.Length - 1) / NUM_PER_PAGE;
                m_page = m_page - 1;
                m_previousPageButton.Visible = m_page > 0;
                m_nextPageButton.Visible = m_page < lastPage;
                if (Screen.InputMethod != InputMethod.Mouse)
                {
                    m_highlight = GetFirstUnlockedDiskOnPage();
                }
                else
                {
                    m_highlight = TestMouse();
                }
                RequestRebuild();
            }
        }

        private void NextPage()
        {
            var lastPage = (m_disks.Length - 1) / NUM_PER_PAGE;
            if (m_page < lastPage)
            {
                m_page = m_page + 1;
                m_previousPageButton.Visible = m_page > 0;
                m_nextPageButton.Visible = m_page < lastPage;
                if (Screen.InputMethod != InputMethod.Mouse)
                {
                    m_highlight = GetFirstUnlockedDiskOnPage();
                }
                else
                {
                    m_highlight = TestMouse();
                }
                RequestRebuild();
            }
        }

        protected override void OnUpdate(float dt)
        {
            // Update children
            m_backPrompt.Update(dt);
            m_selectPrompt.Update(dt);
            m_browseWorkshopPrompt.Update(dt);
            m_nextPageButton.Update(dt);
            m_previousPageButton.Update(dt);

            if (m_closeNextFrame)
            {
                // Hack hack
                FireOnClose();
            }
            else if (m_framesOpen > 0)
            {
                // Update self
                if (m_disks.Length > 0)
                {
                    // Navigate disks
                    if (Screen.Mouse.DX != 0 || Screen.Mouse.DY != 0)
                    {
                        SetHighlight(TestMouse());
                    }
                    if (Screen.CheckLeft())
                    {
                        if (m_highlight >= 0)
                        {
                            var newHighlight = m_highlight - 1;
                            while(newHighlight >= 0)
                            {
                                if (m_disksUnlocked[newHighlight])
                                {
                                    SetHighlight(newHighlight);
                                    break;
                                }
                                else
                                {
                                    newHighlight--;
                                }
                            }
                        }
                        else
                        {
                            SetHighlight(GetLastUnlockedDiskOnPage());
                        }
                    }
                    if (Screen.CheckRight())
                    {
                        if (m_highlight >= 0)
                        {
                            var newHighlight = m_highlight + 1;
                            while (newHighlight < m_disks.Length)
                            {
                                if( m_disksUnlocked[newHighlight] )
                                {
                                    SetHighlight(newHighlight);
                                    break;
                                }
                                else
                                {
                                    newHighlight++;
                                }
                            }
                        }
                        else
                        {
                            SetHighlight(GetFirstUnlockedDiskOnPage());
                        }
                    }
                    if (Screen.CheckUp())
                    {
                        if (m_highlight >= 0)
                        {
                            int newHighlight = m_highlight - COLUMNS_PER_PAGE;
                            if (newHighlight >= (m_page * NUM_PER_PAGE) &&
                                m_disksUnlocked[newHighlight])
                            {
                                SetHighlight(newHighlight);
                            }
                        }
                        else
                        {
                            SetHighlight(GetLastUnlockedDiskOnPage());
                        }
                    }
                    if (Screen.CheckDown())
                    {
                        if (m_highlight >= 0)
                        {
                            int newHighlight = m_highlight + COLUMNS_PER_PAGE;
                            if (newHighlight < Math.Min(m_disks.Length, (m_page + 1) * NUM_PER_PAGE) &&
                                m_disksUnlocked[newHighlight])
                            {
                                SetHighlight(newHighlight);
                            }
                        }
                        else
                        {
                            SetHighlight(GetFirstUnlockedDiskOnPage());
                        }
                    }
                }

                // Check select
                if (m_highlight >= 0 && CheckSelect())
                {
                    PlaySelectSound();
                    FireOnSelect(m_disks[m_highlight].Disk, m_disks[m_highlight].Mod);
                    m_closeNextFrame = true;
                }
                else if (App.Steam && CheckBrowseWorkshop())
                {
                    FireOnBrowseWorkshop();
                    m_closeNextFrame = true;
                }
                else if (CheckClose())
                {
                    m_closeNextFrame = true;
                }
            }

            ++m_framesOpen;
        }

        private bool CheckSelect()
        {
            if (Screen.CheckSelect())
            {
                return true;
            }
            else if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
            {
                Screen.InputMethod = InputMethod.Mouse;
                return true;
            }
            return false;
        }

        private bool CheckClose()
        {
            if (Screen.SteamController != null)
            {
                if (Screen.SteamController.Buttons[SteamControllerButton.MenuBack.GetID()].Pressed ||
                    Screen.SteamController.Buttons[SteamControllerButton.MenuToGame.GetID()].Pressed)
                {
                    Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Screen.Gamepad != null)
            {
                if (Screen.Gamepad.Buttons[GamepadButton.Back].Pressed ||
                    Screen.Gamepad.Buttons[GamepadButton.B].Pressed ||
                    Screen.Gamepad.Buttons[GamepadButton.Start].Pressed)
                {
                    Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Screen.Keyboard.Keys[Key.Escape].Pressed ||
                Screen.Keyboard.Keys[Key.Tab].Pressed)
            {
                Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private int TestMouse()
        {
            var mousePos = Screen.MousePosition;
            var numVisibleRows = Math.Min(m_numRows, ROWS_PER_PAGE);
            var ox = 0.5f * (Screen.Width - (float)m_numColumns * DISK_SIZE - ((float)m_numColumns - 1) * DISK_PADDING);
            var oy = 0.5f * (Screen.Height - (float)numVisibleRows * DISK_SIZE - ((float)numVisibleRows - 1) * DISK_PADDING);
            var first = m_page * NUM_PER_PAGE;
            for (int i = first; i < Math.Min(m_disks.Length, first + NUM_PER_PAGE); ++i)
            {
                if (m_disksUnlocked[i])
                {
                    var x = (i - first) % m_numColumns;
                    var y = (i - first) / m_numColumns;
                    var start = new Vector2(
                        ox + (float)x * (DISK_SIZE + DISK_PADDING),
                        oy + (float)y * (DISK_SIZE + DISK_PADDING)
                    );
                    var localPos = mousePos - start;
                    if (localPos.X >= 0.0f && localPos.Y >= 0.0f &&
                        localPos.X < DISK_SIZE && localPos.Y < DISK_SIZE)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private bool CheckBrowseWorkshop()
        {
            if (Screen.SteamController != null)
            {
                if (Screen.SteamController.Buttons[SteamControllerButton.MenuAltSelect.GetID()].Pressed)
                {
                    Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Screen.Gamepad != null)
            {
                if (Screen.Gamepad.Buttons[GamepadButton.Y].Pressed)
                {
                    Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Screen.Keyboard.Keys[Key.LeftCtrl].Pressed ||
                Screen.Keyboard.Keys[Key.RightCtrl].Pressed)
            {
                Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        protected override void OnRebuild()
        {
            // Rebuild children:
            m_backPrompt.RequestRebuild();
            m_browseWorkshopPrompt.RequestRebuild();
            m_selectPrompt.RequestRebuild();

            // Rebuild self:
            m_geometry.Clear();

            // Rebuild background
            m_geometry.Add2DQuad(Vector2.Zero, new Vector2(Screen.Width, Screen.Height));

            // Rebuild disks
            var numVisibleRows = Math.Min(m_numRows, ROWS_PER_PAGE);
            var ox = 0.5f * (Screen.Width - (float)m_numColumns * DISK_SIZE - ((float)m_numColumns - 1) * DISK_PADDING);
            var oy = 0.5f * (Screen.Height - (float)numVisibleRows * DISK_SIZE - ((float)numVisibleRows - 1) * DISK_PADDING);
            for (int i = 0; i < NUM_PER_PAGE; ++i)
            {
                var x = i % m_numColumns;
                var y = i / m_numColumns;
                var start = new Vector2(
                    ox + (float)x * (DISK_SIZE + DISK_PADDING),
                    oy + (float)y * (DISK_SIZE + DISK_PADDING)
                );
                m_geometry.Add2DQuad(
                    start,
                    start + new Vector2(DISK_SIZE, DISK_SIZE)
                );
                m_geometry.Add2DQuad(
                    start + new Vector2(13.0f / 66.0f, 35.0f / 66.0f) * DISK_SIZE,
                    start + new Vector2(53.0f / 66.0f, 63.0f / 66.0f) * DISK_SIZE
                );
                m_geometry.Add2DQuad(
                    start,
                    start + new Vector2(DISK_SIZE, DISK_SIZE)
                );
            }

            m_geometry.Rebuild();
        }

        protected override void OnDraw()
        {
            // Draw self:
            // Draw background
            Screen.Effect.Colour = BACKGROUND_COLOUR;
            Screen.Effect.Texture = Texture.White;
            Screen.Effect.Bind();
            m_geometry.DrawRange(0, 6);

            // Draw disks
            var first = m_page * NUM_PER_PAGE;
            for (int i = first; i < Math.Min(m_disks.Length, first + NUM_PER_PAGE); ++i)
            {
                // Draw disk
                var unlocked = m_disksUnlocked[i];
                Screen.Effect.Colour = unlocked ? Vector4.One : LOCKED_COLOUR;
                Screen.Effect.Texture = (i == m_highlight) ?
                    Texture.Get("gui/floppy_highlight.png", false) :
                    Texture.Get("gui/floppy.png", false);
                Screen.Effect.Bind();
                m_geometry.DrawRange(6 + (i - first) * 18, 6);

                // Draw label
                if (m_diskLabels[i] != null)
                {
                    Screen.Effect.Colour = unlocked ? Vector4.One : LOCKED_COLOUR;
                    Screen.Effect.Texture = m_diskLabels[i];
                    Screen.Effect.Bind();
                    m_geometry.DrawRange(6 + (i - first) * 18 + 6, 6);
                }

                // Draw padlock
                if( !m_disksUnlocked[i] )
                {
                    Screen.Effect.Colour = Vector4.One;
                    Screen.Effect.Texture = Texture.Get("gui/floppy_locked.png", false);
                    Screen.Effect.Bind();
                    m_geometry.DrawRange(6 + (i - first) * 18 + 12, 6);
                }
            }

            // Draw children:
            m_backPrompt.Draw();
            if (App.Steam)
            {
                m_browseWorkshopPrompt.Draw();
            }
            if (Screen.InputMethod != InputMethod.Mouse && m_highlight >= 0)
            {
                if (m_highlight == m_initialDisk)
                {
                    m_selectPrompt.String = Screen.Language.Translate("menus.arcade.reset");
                }
                else
                {
                    m_selectPrompt.String = Screen.Language.Translate("menus.select");
                }
                m_selectPrompt.Draw();
            }
            m_nextPageButton.Draw();
            m_previousPageButton.Draw();
        }

        private void FireOnSelect(ArcadeDisk disk, Mod mod)
        {
            if (OnSelect != null)
            {
                OnSelect.Invoke(this, new DiskSelectEventArgs(disk, mod));
            }
        }

        private void FireOnBrowseWorkshop()
        {
            if (OnBrowseWorkshop != null)
            {
                OnBrowseWorkshop.Invoke(this, EventArgs.Empty);
            }
        }

        private void FireOnClose()
        {
            if (OnClose != null)
            {
                OnClose.Invoke(this, EventArgs.Empty);
            }
        }

        private void PlayHighlightSound()
        {
            Screen.Audio.PlaySound("sound/menu_highlight.wav");
        }

        private void PlaySelectSound()
        {
            Screen.Audio.PlaySound("sound/menu_select.wav");
        }
    }
}
