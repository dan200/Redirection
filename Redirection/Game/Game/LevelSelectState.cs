using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.Level;
using OpenTK;
using System;
using System.IO;
using System.Linq;

namespace Dan200.Game.Game
{
    public class LevelSelectState : MenuState
    {
        public static int NUM_COLUMNS = 3;
        public static int NUM_ROWS = 2;
        public static int NUM_PER_PAGE = NUM_COLUMNS * NUM_ROWS;

        private static float IMAGE_HEIGHT = 126.0f;
        private static float IMAGE_WIDTH = IMAGE_HEIGHT * (16.0f / 9.0f);
        private static float IMAGE_MARGIN = 16.0f;

        private bool m_editor;
        private Mod m_mod;
        private Campaign m_campaign;
        private int m_page;

        private Text m_subtitle;
        private Button m_nextPageButton;
        private Button m_previousPageButton;
        private LevelThumbnail[] m_thumbnails;
        private int m_highlight;

        public LevelSelectState(Game game, Mod mod, Campaign campaign, int page, int highlight, bool editor, int justCompleted = -1) : base(game, "menus.level_select.title", "levels/empty.level", MenuArrangement.FullScreen)
        {
            m_mod = mod;
            m_campaign = campaign;
            m_page = page;
            m_editor = editor;
            m_highlight = highlight;
            EnableGamepad = !m_editor;

            // Setup title
            if (m_editor)
            {
                TitleClickable = true;
                Title = MouseButton.Left.GetPrompt() + " " + m_campaign.Title;
            }
            else
            {
                Title = m_campaign.Title;
            }

            // Create subtitle
            {
                m_subtitle = new Text(UIFonts.Default, Game.Language.Translate("menus.level_select.subtitle"), UIColours.Text, TextAlignment.Center);
                m_subtitle.Anchor = Anchor.TopMiddle;
                m_subtitle.LocalPosition = new Vector2(0.0f, 32.0f + UIFonts.Default.Height);
            }

            // Create buttons
            m_previousPageButton = new Button(Texture.Get("gui/arrows.png", true), 32.0f, 32.0f);
            m_previousPageButton.Region = new Quad(0.0f, 0.5f, 0.5f, 0.5f);
            m_previousPageButton.HighlightRegion = m_previousPageButton.Region;
            m_previousPageButton.DisabledRegion = m_previousPageButton.Region;
            m_previousPageButton.ShortcutButton = GamepadButton.LeftBumper;
            m_previousPageButton.AltShortcutButton = GamepadButton.LeftTrigger;
            m_previousPageButton.ShortcutSteamControllerButton = SteamControllerButton.MenuPreviousPage;
            m_previousPageButton.Anchor = Anchor.TopMiddle;
            m_previousPageButton.Colour = UIColours.Title;
            m_previousPageButton.HighlightColour = UIColours.White;
            m_previousPageButton.LocalPosition = new Vector2(
                -240.0f - 0.5f * m_previousPageButton.Width,
                32.0f + UIFonts.Default.Height - 0.5f * m_previousPageButton.Height
            );
            m_previousPageButton.OnClicked += delegate (object sender, EventArgs e)
            {
                if (Dialog == null)
                {
                    PreviousPage();
                }
            };
            m_previousPageButton.Visible = (m_page > 0);

            // Next
            m_nextPageButton = new Button(Texture.Get("gui/arrows.png", true), 32.0f, 32.0f);
            m_nextPageButton.Region = new Quad(0.0f, 0.0f, 0.5f, 0.5f);
            m_nextPageButton.HighlightRegion = m_nextPageButton.Region;
            m_nextPageButton.DisabledRegion = m_nextPageButton.Region;
            m_nextPageButton.ShortcutButton = GamepadButton.RightBumper;
            m_nextPageButton.AltShortcutButton = GamepadButton.RightTrigger;
            m_nextPageButton.ShortcutSteamControllerButton = SteamControllerButton.MenuNextPage;
            m_nextPageButton.Anchor = Anchor.TopMiddle;
            m_nextPageButton.Colour = UIColours.Title;
            m_nextPageButton.HighlightColour = UIColours.White;
            m_nextPageButton.LocalPosition = new Vector2(
                240.0f - 0.5f * m_nextPageButton.Width,
                32.0f + UIFonts.Default.Height - 0.5f * m_nextPageButton.Height
            );
            m_nextPageButton.OnClicked += delegate (object sender, EventArgs e)
            {
                if (Dialog == null)
                {
                    NextPage();
                }
            };
            m_nextPageButton.Visible = IsNextPageUnlocked();

            // Create thumbnails
            {
                var firstLevel = page * NUM_PER_PAGE;
                var lastLevel = Math.Min(firstLevel + NUM_PER_PAGE, m_campaign.Levels.Count + (m_editor ? 1 : 0)) - 1;
                m_thumbnails = new LevelThumbnail[lastLevel - firstLevel + 1];

                float xStart = (NUM_COLUMNS * IMAGE_WIDTH + (NUM_COLUMNS - 1) * IMAGE_MARGIN) * -0.5f;
                float yStart = (NUM_ROWS * IMAGE_HEIGHT + (NUM_ROWS - 1) * IMAGE_MARGIN) * -0.5f + 16.0f;
                for (int i = firstLevel; i <= lastLevel; ++i)
                {
                    var levelPath = (i < m_campaign.Levels.Count) ? m_campaign.Levels[i] : "NEW";
                    var levelIndex = i;

                    var pos = i - firstLevel;
                    var thumbnail = new LevelThumbnail(levelPath, IMAGE_WIDTH, IMAGE_HEIGHT, Game.Language);
                    thumbnail.Anchor = Anchor.CentreMiddle;
                    thumbnail.LocalPosition = new Vector2(
                        xStart + (pos % NUM_COLUMNS) * (IMAGE_WIDTH + IMAGE_MARGIN),
                        yStart + (pos / NUM_COLUMNS) * (IMAGE_HEIGHT + IMAGE_MARGIN)
                    );
                    thumbnail.Completed = !m_editor && Game.User.Progress.IsLevelCompleted(LevelData.Get(levelPath).ID);
                    thumbnail.JustCompleted = thumbnail.Completed && (i == justCompleted);
                    thumbnail.Locked = !IsLevelUnlocked(campaign, mod, levelIndex);
                    thumbnail.JustUnlocked = !thumbnail.Locked && !thumbnail.Completed && WasJustUnlocked(campaign, mod, i, justCompleted);
                    thumbnail.CanDelete = m_editor && levelIndex != m_campaign.Levels.Count;
                    thumbnail.CanMoveLeft = m_editor && levelIndex > 0 && levelIndex != m_campaign.Levels.Count;
                    thumbnail.CanMoveRight = m_editor && levelIndex < m_campaign.Levels.Count - 1;

                    thumbnail.OnClicked += delegate (object sender, EventArgs e)
                    {
                        if (Dialog == null)
                        {
                            PlayOrEditLevel(levelIndex);
                        }
                    };

                    thumbnail.OnDeleteClicked += delegate (object sender, EventArgs e)
                    {
                        if (Dialog == null)
                        {
                            DeleteLevel(TranslateTitle(thumbnail.LevelTitle), levelIndex);
                        }
                    };

                    thumbnail.OnMoveLeftClicked += delegate (object sender, EventArgs e)
                    {
                        if (Dialog == null)
                        {
                            SwapLevels(levelIndex, levelIndex - 1);
                            CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, -1, m_editor));
                        }
                    };

                    thumbnail.OnMoveRightClicked += delegate (object sender, EventArgs e)
                    {
                        if (Dialog == null)
                        {
                            SwapLevels(levelIndex, levelIndex + 1);
                            CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, -1, m_editor));
                        }
                    };

                    m_thumbnails[pos] = thumbnail;
                }
            }
        }

        private bool WasJustUnlocked(Campaign campaign, Mod mod, int levelIndex, int justCompletedLevel)
        {
            return
                justCompletedLevel >= 0 &&
                IsLevelUnlocked(campaign, mod, levelIndex) &&
                !IsLevelUnlocked(campaign, mod, levelIndex, justCompletedLevel);
        }

        private bool IsLevelUnlocked(Campaign campaign, Mod mod, int levelIndex, int ignoreLevel = -1)
        {
            return m_editor || ProgressUtils.IsLevelUnlocked(campaign, mod, levelIndex, Game.User, ignoreLevel);
        }


        protected override void OnTitleClicked()
        {
            RenameCampaign();
        }

        protected override void OnInit()
        {
            base.OnInit();
            Game.Screen.Elements.Add(m_subtitle);
            Game.Screen.Elements.Add(m_nextPageButton);
            Game.Screen.Elements.Add(m_previousPageButton);
            for (int i = 0; i < m_thumbnails.Length; ++i)
            {
                var thumb = m_thumbnails[i];
                Game.Screen.Elements.Add(thumb);
            }
            if (m_highlight >= 0)
            {
                // Select specified unlocked level
                m_highlight = Math.Min(m_highlight, m_thumbnails.Length - 1);
                if (!IsLocked(m_highlight))
                {
                    SetHighlight(m_highlight);
                }
                else if (m_highlight == 0)
                {
                    // Select first unlocked level
                    while (m_highlight < m_thumbnails.Length && IsLocked(m_highlight))
                    {
                        ++m_highlight;
                    }
                    if (m_highlight < m_thumbnails.Length)
                    {
                        SetHighlight(m_highlight);
                    }
                    else
                    {
                        m_highlight = -1;
                    }
                }
                else if (m_highlight == m_thumbnails.Length - 1)
                {
                    // Select last unlocked level
                    while (m_highlight >= 0 && IsLocked(m_highlight))
                    {
                        --m_highlight;
                    }
                    if (m_highlight >= 0)
                    {
                        SetHighlight(m_highlight);
                    }
                    else
                    {
                        m_highlight = -1;
                    }
                }
            }
            else if (Game.Screen.InputMethod != InputMethod.Mouse)
            {
                // Select first unlocked level
                int first = 0;
                while (first < m_thumbnails.Length && IsLocked(first))
                {
                    ++first;
                }
                if (first < m_thumbnails.Length)
                {
                    SetHighlight(first);
                }
            }
            else
            {
                // Select mouseovered unlocked level
                SetHighlight(TestMouse());
            }
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();

            Game.Screen.Elements.Remove(m_subtitle);
            m_subtitle.Dispose();

            Game.Screen.Elements.Remove(m_nextPageButton);
            m_nextPageButton.Dispose();

            Game.Screen.Elements.Remove(m_previousPageButton);
            m_previousPageButton.Dispose();

            for (int i = 0; i < m_thumbnails.Length; ++i)
            {
                var thumb = m_thumbnails[i];
                Game.Screen.Elements.Remove(thumb);
                thumb.Dispose();
            }
        }

        private bool PreviousPage()
        {
            if (m_page > 0)
            {
                CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page - 1, Game.Screen.InputMethod == InputMethod.Mouse ? -1 : NUM_PER_PAGE - 1, m_editor));
                return true;
            }
            return false;
        }

        private bool IsNextPageUnlocked()
        {
            int lastLevel = m_editor ? m_campaign.Levels.Count : m_campaign.Levels.Count - 1;
            int lastPage = lastLevel / NUM_PER_PAGE;
            if (lastPage > m_page)
            {
                if (m_editor)
                {
                    return true;
                }
                else
                {
                    int nextPage = m_page + 1;
                    int nextLevel = nextPage * NUM_PER_PAGE;
                    return IsLevelUnlocked(m_campaign, m_mod, nextLevel);
                }
            }
            return false;
        }

        private bool NextPage()
        {
            if (IsNextPageUnlocked())
            {
                CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page + 1, Game.Screen.InputMethod == InputMethod.Mouse ? -1 : 0, m_editor));
                return true;
            }
            return false;
        }

        private bool IsLocked(int highlight)
        {
            return m_thumbnails[highlight].Locked;
        }

        private void SetHighlight(int highlight)
        {
            if (m_highlight >= 0)
            {
                m_thumbnails[m_highlight].Highlight = false;
            }
            m_highlight = highlight;
            ShowSelectPrompt = highlight >= 0 && Game.Screen.InputMethod != InputMethod.Mouse;
            ShowAltSelectPrompt = highlight >= 0 && Game.Screen.InputMethod != InputMethod.Mouse & m_thumbnails[m_highlight].CanDelete;
            AltSelectPrompt = "menus.mod_select.delete";
            if (highlight >= 0)
            {
                m_thumbnails[m_highlight].Highlight = true;
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            if (App.Demo && Game.Keyboard.Keys[Key.R].Pressed)
            {
                // Reset
                WipeToState(new StartScreenState(Game));
            }

            // Update UI
            if (Game.Screen.ModalDialog == null)
            {
                if (Game.Mouse.DX != 0 || Game.Mouse.DY != 0)
                {
                    Game.Screen.InputMethod = InputMethod.Mouse;
                    int highlight = TestMouse();
                    if (highlight != m_highlight)
                    {
                        SetHighlight(highlight);
                        if (highlight >= 0)
                        {
                            PlayHighlightSound();
                        }
                    }
                }

                var swap = (Game.Keyboard.Keys[Key.LeftShift].Held || Game.Keyboard.Keys[Key.RightShift].Held) && m_editor;
                if (Game.Screen.CheckUp())
                {
                    if (m_highlight >= 0)
                    {
                        // Select the unlocked level above
                        int newHighlight = m_highlight - NUM_COLUMNS;
                        while (newHighlight >= 0 && IsLocked(newHighlight))
                        {
                            newHighlight -= NUM_COLUMNS;
                        }
                        if (newHighlight >= 0)
                        {
                            if (swap)
                            {
                                var oldIndex = m_page * NUM_PER_PAGE + m_highlight;
                                var newIndex = m_page * NUM_PER_PAGE + newHighlight;
                                if (oldIndex < m_campaign.Levels.Count && newIndex < m_campaign.Levels.Count)
                                {
                                    SwapLevels(oldIndex, newIndex);
                                    CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, newHighlight, m_editor));
                                    PlayHighlightSound();
                                }
                            }
                            else
                            {
                                SetHighlight(newHighlight);
                                PlayHighlightSound();
                            }
                        }
                    }
                    else
                    {
                        // Select the last unlocked level
                        int newHighlight = m_thumbnails.Length - 1;
                        while (newHighlight >= 0 && IsLocked(newHighlight))
                        {
                            --newHighlight;
                        }
                        if (newHighlight >= 0)
                        {
                            SetHighlight(newHighlight);
                            PlayHighlightSound();
                        }
                    }
                }
                if (Game.Screen.CheckDown())
                {
                    if (m_highlight >= 0)
                    {
                        // Select the unlocked level below
                        int newHighlight = m_highlight + NUM_COLUMNS;
                        while (newHighlight < m_thumbnails.Length && IsLocked(newHighlight))
                        {
                            newHighlight += NUM_COLUMNS;
                        }
                        if (newHighlight < m_thumbnails.Length)
                        {
                            if (swap)
                            {
                                var oldIndex = m_page * NUM_PER_PAGE + m_highlight;
                                var newIndex = m_page * NUM_PER_PAGE + newHighlight;
                                if (oldIndex < m_campaign.Levels.Count && newIndex < m_campaign.Levels.Count)
                                {
                                    SwapLevels(oldIndex, newIndex);
                                    CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, newHighlight, m_editor));
                                    PlayHighlightSound();
                                }
                            }
                            else
                            {
                                SetHighlight(newHighlight);
                                PlayHighlightSound();
                            }
                        }
                    }
                    else
                    {
                        // Select the first unlocked level
                        int first = 0;
                        while (first < m_thumbnails.Length && IsLocked(first))
                        {
                            ++first;
                        }
                        if (first < m_thumbnails.Length)
                        {
                            SetHighlight(first);
                            PlayHighlightSound();
                        }
                    }
                }
                if (Game.Screen.CheckLeft())
                {
                    if (m_highlight >= 0)
                    {
                        // Select the previous unlocked level
                        int newHighlight = m_highlight - 1;
                        while (newHighlight >= 0 && IsLocked(newHighlight))
                        {
                            --newHighlight;
                        }
                        if (newHighlight >= 0)
                        {
                            if (swap)
                            {
                                var oldIndex = m_page * NUM_PER_PAGE + m_highlight;
                                var newIndex = m_page * NUM_PER_PAGE + newHighlight;
                                if (oldIndex < m_campaign.Levels.Count && newIndex < m_campaign.Levels.Count)
                                {
                                    SwapLevels(oldIndex, newIndex);
                                    CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, newHighlight, m_editor));
                                    PlayHighlightSound();
                                }
                            }
                            else
                            {
                                SetHighlight(newHighlight);
                                PlayHighlightSound();
                            }
                        }
                        else
                        {
                            if (m_page > 0)
                            {
                                if (swap)
                                {
                                    var oldIndex = m_page * NUM_PER_PAGE + m_highlight;
                                    var newIndex = (m_page - 1) * NUM_PER_PAGE + (NUM_PER_PAGE - 1);
                                    if (oldIndex < m_campaign.Levels.Count && newIndex < m_campaign.Levels.Count)
                                    {
                                        SwapLevels(oldIndex, newIndex);
                                        PreviousPage();
                                        PlayHighlightSound();
                                    }
                                }
                                else
                                {
                                    PreviousPage();
                                    PlayHighlightSound();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Select the last unlocked level
                        int last = m_thumbnails.Length - 1;
                        while (last >= 0 && IsLocked(last))
                        {
                            --last;
                        }
                        if (last >= 0)
                        {
                            SetHighlight(last);
                            PlayHighlightSound();
                        }
                        else
                        {
                            if (PreviousPage())
                            {
                                PlayHighlightSound();
                            }
                        }
                    }
                }
                if (Game.Screen.CheckRight())
                {
                    if (m_highlight >= 0)
                    {
                        // Select the next unlocked level
                        int newHighlight = m_highlight + 1;
                        while (newHighlight < m_thumbnails.Length && IsLocked(newHighlight))
                        {
                            ++newHighlight;
                        }
                        if (newHighlight < m_thumbnails.Length)
                        {
                            if (swap)
                            {
                                var oldIndex = m_page * NUM_PER_PAGE + m_highlight;
                                var newIndex = m_page * NUM_PER_PAGE + newHighlight;
                                if (oldIndex < m_campaign.Levels.Count && newIndex < m_campaign.Levels.Count)
                                {
                                    SwapLevels(oldIndex, newIndex);
                                    CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, newHighlight, m_editor));
                                    PlayHighlightSound();
                                }
                            }
                            else
                            {
                                SetHighlight(newHighlight);
                                PlayHighlightSound();
                            }
                        }
                        else
                        {
                            if (IsNextPageUnlocked())
                            {
                                if (swap)
                                {
                                    var oldIndex = m_page * NUM_PER_PAGE + m_highlight;
                                    var newIndex = (m_page + 1) * NUM_PER_PAGE;
                                    if (oldIndex < m_campaign.Levels.Count && newIndex < m_campaign.Levels.Count)
                                    {
                                        SwapLevels(oldIndex, newIndex);
                                        NextPage();
                                        PlayHighlightSound();
                                    }
                                }
                                else
                                {
                                    NextPage();
                                    PlayHighlightSound();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Select the first unlocked level
                        int first = 0;
                        while (first < m_thumbnails.Length && IsLocked(first))
                        {
                            ++first;
                        }
                        if (first < m_thumbnails.Length)
                        {
                            SetHighlight(first);
                        }
                        else
                        {
                            if (NextPage())
                            {
                                PlayHighlightSound();
                            }
                        }
                    }
                }
                if (Game.Screen.CheckSelect())
                {
                    if (m_highlight >= 0)
                    {
                        // Enter the highlighted level
                        int firstLevel = m_page * NUM_PER_PAGE;
                        PlayOrEditLevel(firstLevel + m_highlight);
                        PlaySelectSound();
                    }
                }
                if (Game.Screen.CheckAltSelect())
                {
                    if (m_editor && m_highlight >= 0 && m_thumbnails[m_highlight].CanDelete)
                    {
                        // Delete the highlighted level
                        int firstLevel = m_page * NUM_PER_PAGE;
                        DeleteLevel(TranslateTitle(m_thumbnails[m_highlight].LevelTitle), firstLevel + m_highlight);
                        PlaySelectSound();
                    }
                }
            }

            // Update subtitle
            if (m_highlight >= 0)
            {
                var thumbnail = m_thumbnails[m_highlight];
                if (thumbnail.MouseOverDelete)
                {
                    m_subtitle.String = Game.Language.Translate("menus.mod_select.delete");
                }
                else if (thumbnail.MouseOverMoveLeft)
                {
                    m_subtitle.String = Game.Language.Translate("menus.level_select.move_back");
                }
                else if (thumbnail.MouseOverMoveRight)
                {
                    m_subtitle.String = Game.Language.Translate("menus.level_select.move_forward");
                }
                else
                {
                    m_subtitle.String = TranslateTitle(thumbnail.LevelTitle);
                }
            }
            else
            {
                m_subtitle.String = Game.Language.Translate("menus.level_select.subtitle");
            }

            // Update debug shortcuts
            if (Dialog == null && !m_editor)
            {
                if ((App.Debug || (m_mod != null && m_mod.Source == ModSource.Editor)) && Game.Keyboard.Keys[Key.E].Pressed)
                {
                    CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, -1, true));
                }
            }
        }

        private void RenameCampaign()
        {
            var textEntry = TextEntryDialogBox.Create(Game.Language.Translate("menus.name_campaign_prompt.title"), m_campaign.Title, "", Game.Screen.Width - 300.0f, new string[] {
                Game.Language.Translate( "menus.ok" ),
                Game.Language.Translate( "menus.cancel" )
            });
            textEntry.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
            {
                if (e2.Result == 0)
                {
                    // Get the title
                    var title = (textEntry.EnteredText.Trim().Length > 0) ? textEntry.EnteredText.Trim() : "Untitled Campaign";

                    // Add the title to the campaign
                    var oldtitle = m_campaign.Title;
                    var newCampaign = m_campaign.Copy();
                    newCampaign.Title = title;

                    // Save the campaign
                    if (m_mod != null)
                    {
                        var fullCampaignPath = Path.Combine(m_mod.Path, "assets/" + m_campaign.Path);
                        newCampaign.Save(fullCampaignPath);

                        // Rename the mod too if it has the same name
                        if (m_mod.Title == oldtitle)
                        {
                            m_mod.Title = title;
                            m_mod.SaveInfo();
                        }
                    }
                    else
                    {
                        var fullCampaignPath = Path.Combine(App.AssetPath, "main/" + m_campaign.Path);
                        newCampaign.Save(fullCampaignPath);
                    }

                    // Reload the campaign
                    Assets.Reload(m_campaign.Path);
                    Title = MouseButton.Left.GetPrompt() + " " + m_campaign.Title;
                }
            };
            ShowDialog(textEntry);
        }

        private void EditLevel(int level)
        {
            var levelPath = m_campaign.Levels[level];
            var levelData = LevelData.Get(levelPath);
            Game.User.Progress.LastEditedLevel = levelData.ID;
            Game.User.Progress.Save();
            if (Assets.Exists<LevelData>(levelPath))
            {
                WipeToState(new EditorState(Game, m_mod, m_campaign, level, levelPath, levelPath));
            }
            else
            {
                WipeToState(new EditorState(Game, m_mod, m_campaign, level, "levels/template.level", levelPath));
            }
        }

        private void CreateLevel()
        {
            // Determine where to create the new level
            var assetsPath = m_mod != null ? Path.Combine(m_mod.Path, "assets") : Path.Combine(App.AssetPath, "main");
            var shortCampaignTitle = AssetPath.GetFileNameWithoutExtension(m_campaign.Path);

            int i = 1;
            var levelAssetPath = "levels/" + shortCampaignTitle + "/" + i + ".level";
            while (File.Exists(Path.Combine(assetsPath, levelAssetPath)))
            {
                levelAssetPath = "levels/" + shortCampaignTitle + "/" + i + ".level";
                ++i;
            }

            // Add the level to the campaign
            var newCampaign = m_campaign.Copy();
            newCampaign.Levels.Add(levelAssetPath);

            // Save the campaign
            var fullCampaignPath = Path.Combine(assetsPath, m_campaign.Path);
            newCampaign.Save(fullCampaignPath);

            // Create the Level
            var fullLevelPath = Path.Combine(assetsPath, levelAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullLevelPath));
            File.Copy(Path.Combine(App.AssetPath, "base/levels/template.level"), fullLevelPath);

            // Modify the ID
            {
                var kvp = new KeyValuePairFile(fullLevelPath);
                kvp.Set("id", MathUtils.GenerateLevelID(levelAssetPath));
                kvp.Save();
            }

            // Reload and edit the level
            Assets.Reload(levelAssetPath);
            Assets.Reload(m_campaign.Path);

            EditLevel(m_campaign.Levels.Count - 1);
        }

        private void DeleteLevel(string levelTitle, int levelIndex)
        {
            var dialog = DialogBox.CreateQueryBox(
                Game.Screen,
                Game.Language.Translate("menus.delete_mod_prompt.title"),
                Game.Language.Translate("menus.delete_mod_prompt.info", levelTitle),
                new string[] {
                    Game.Language.Translate( "menus.yes" ),
                    Game.Language.Translate( "menus.no" ),
                },
                true
            );
            dialog.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // YES
                            ReallyDeleteLevel(levelIndex);
                            break;
                        }
                }
            };
            ShowDialog(dialog);
        }

        private void ReallyDeleteLevel(int levelIndex)
        {
            // Remove the level from the campaign
            var levelPath = m_campaign.Levels[levelIndex];
            var newCampaign = m_campaign.Copy();
            newCampaign.Levels.RemoveAll((s) => (s == levelPath));

            // Save the campaign
            var assetsPath = (m_mod != null) ? Path.Combine(m_mod.Path, "assets") : Path.Combine(App.AssetPath, "main");
            var fullCampaignPath = Path.Combine(assetsPath, m_campaign.Path);
            newCampaign.Save(fullCampaignPath);

            // Delete the Level
            var fullLevelPath = Path.Combine(assetsPath, levelPath);
            if (File.Exists(fullLevelPath))
            {
                File.Delete(fullLevelPath);
            }

            // Delete the Thumbnail
            var thumbnailPath = AssetPath.ChangeExtension(levelPath, "png");
            var fullThumbnailPath = Path.Combine(assetsPath, thumbnailPath);
            if (File.Exists(fullThumbnailPath))
            {
                File.Delete(fullThumbnailPath);
            }

            // Reopen the level select screen
            Assets.Reload(m_campaign.Path);
            CutToState(new LevelSelectState(Game, m_mod, m_campaign, m_page, -1, m_editor));
        }

        private void SwapLevels(int level1Index, int level2Index)
        {
            // Swap the levels from the campaign
            var newCampaign = m_campaign.Copy();
            var temp = newCampaign.Levels[level1Index];
            newCampaign.Levels[level1Index] = newCampaign.Levels[level2Index];
            newCampaign.Levels[level2Index] = temp;

            // Save the campaign
            var assetsPath = (m_mod != null) ? Path.Combine(m_mod.Path, "assets") : Path.Combine(App.AssetPath, "main");
            var fullCampaignPath = Path.Combine(assetsPath, m_campaign.Path);
            newCampaign.Save(fullCampaignPath);

            // Reload the campaign
            Assets.Reload(m_campaign.Path);
        }

        private void PlayOrEditLevel(int levelIndex)
        {
            if (m_editor)
            {
                if (levelIndex < m_campaign.Levels.Count)
                {
                    EditLevel(levelIndex);
                }
                else
                {
                    CreateLevel();
                }
            }
            else
            {
                PlayLevel(levelIndex);
            }
        }

        private void PlayLevel(int level)
        {
            var playthrough = new Playthrough(m_campaign, level);
            var introPath = LevelData.Get(m_campaign.Levels[level]).Intro;
            if (introPath != null)
            {
                // Go to the cutscene
                WipeToState(new CutsceneState(Game, m_mod, introPath, CutsceneContext.LevelIntro, playthrough));
            }
            else
            {
                // Go to the level
                WipeToState(new CampaignState(Game, m_mod, playthrough));
            }
        }

        protected override void GoBack()
        {
            if (m_editor && m_mod != null)
            {
                // Back to the editor campaign select (keep mod loaded)
                var campaigns = Assets.List<Campaign>("campaigns", m_mod.Assets).ToArray();
                if (campaigns.Length > 1)
                {
                    WipeToState(new CampaignSelectState(Game, m_mod));
                }
                else
                {
                    WipeToState(new ModEditorState(Game, m_mod));
                }
            }
            else
            {
                // Back to the regular campaign select (unload mod)
                Func<State> fnNextState = delegate ()
                {
                    if (App.Demo)
                    {
                        return new StartScreenState(Game);
                    }
                    else
                    {
                        return new CampaignSelectState(Game);
                    }
                };
                if (m_mod != null && !m_mod.AutoLoad)
                {
                    Assets.RemoveSource(m_mod.Assets);
                    m_mod.Loaded = false;
                    LoadToState(fnNextState);
                }
                else
                {
                    WipeToState(fnNextState.Invoke());
                }
            }
        }

        private int TestMouse()
        {
            if (Dialog == null)
            {
                var mousePos = Game.Screen.MousePosition;
                float halfMargin = IMAGE_MARGIN * 0.5f;
                for (int i = 0; i < m_thumbnails.Length; ++i)
                {
                    var thumbnail = m_thumbnails[i];
                    if (!thumbnail.Locked &&
                        mousePos.X >= thumbnail.Position.X - halfMargin &&
                        mousePos.X < thumbnail.Position.X + thumbnail.Width + halfMargin &&
                        mousePos.Y >= thumbnail.Position.Y - halfMargin &&
                        mousePos.Y < thumbnail.Position.Y + thumbnail.Height + halfMargin)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private void PlayHighlightSound()
        {
            Game.Screen.Audio.PlaySound("sound/menu_highlight.wav");
        }

        private void PlaySelectSound()
        {
            Game.Screen.Audio.PlaySound("sound/menu_select.wav");
        }
    }
}

