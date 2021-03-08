using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Game.Game;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dan200.Game.GUI
{
    public class CampaignEventArgs : EventArgs
    {
        public readonly Campaign Campaign;
        public readonly Mod Mod;

        public CampaignEventArgs(Campaign campaign, Mod mod)
        {
            Campaign = campaign;
            Mod = mod;
        }
    }

    public class CampaignActionEventArgs : CampaignEventArgs
    {
        public readonly CampaignThumbnailAction Action;

        public CampaignActionEventArgs(Campaign campaign, Mod mod, CampaignThumbnailAction action) : base(campaign, mod)
        {
            Action = action;
        }
    }

    public class CampaignList : Element
    {
        private const int ENTRIES_PER_PAGE = 3;
        private const float MARGIN_HEIGHT = 8.0f;

        private Game.Game m_game;
        private bool m_editor;
        private int m_offset;
        private int m_count;
        private Mod m_mod;

        private List<CampaignThumbnail> m_campaignThumbnails;
        private BoxButton m_actionButton;
        private float m_width;
        private int m_highlight;

        private Button m_upButton;
        private Button m_downButton;

        public float Width
        {
            get
            {
                return m_width;
            }
        }

        public float Height
        {
            get
            {
                return
                    ENTRIES_PER_PAGE * (16.0f + 2.0f * UIFonts.Default.Height + MARGIN_HEIGHT) +
                    36.0f;
            }
        }

        public int Count
        {
            get
            {
                return m_count;
            }
        }

        public int Highlight
        {
            get
            {
                return m_highlight;
            }
        }

        public CampaignThumbnailAction HighlightedAction
        {
            get
            {
                if (m_highlight >= m_offset && m_highlight < Math.Min(m_offset + ENTRIES_PER_PAGE, m_count))
                {
                    return m_campaignThumbnails[m_highlight - m_offset].Action;
                }
                return CampaignThumbnailAction.None;
            }
        }

        public int Offset
        {
            get
            {
                return m_offset;
            }
            set
            {
                if (m_offset != value)
                {
                    m_offset = value;
                    Refresh();
                }
            }
        }

        public event EventHandler<CampaignEventArgs> OnSelection;
        public event EventHandler<CampaignActionEventArgs> OnAction;
        public event EventHandler<EventArgs> OnBrowseWorkshop;
        public event EventHandler<EventArgs> OnOpenModsFolder;
        public event EventHandler<EventArgs> OnCreateCampaign;

        public CampaignList(float width, Game.Game game, Mod mod = null)
        {
            m_game = game;
            m_editor = (mod != null);
            m_mod = mod;
            m_offset = 0;

            m_width = width;
            m_campaignThumbnails = new List<CampaignThumbnail>();

            m_actionButton = new BoxButton(m_width, 36.0f);
            m_actionButton.LocalPosition = new Vector2(0.0f, 0.0f);
            if (m_editor)
            {
                m_actionButton.Text = m_game.Screen.Language.Translate("menus.mod_select.create_new");
                m_actionButton.OnClicked += delegate (object sender, EventArgs e)
                {
                    FireOnCreateCampaign();
                };
            }
            else
            {
                if (App.Steam)
                {
                    m_actionButton.Text = m_game.Screen.Language.Translate("menus.campaign_select.open_steam_workshop");
                    m_actionButton.OnClicked += delegate (object sender, EventArgs e)
                    {
                        FireOnBrowseWorkshop();
                    };
                }
                else
                {
                    m_actionButton.Text = m_game.Screen.Language.Translate("menus.campaign_select.open_mod_directory");
                    m_actionButton.OnClicked += delegate (object sender, EventArgs e)
                    {
                        FireOnOpenModsFolder();
                    };
                }
            }

            m_upButton = new Button(Texture.Get("gui/arrows.png", true), 32.0f, 32.0f);
            m_upButton.Region = new Quad(0.5f, 0.0f, 0.5f, 0.5f);
            m_upButton.HighlightRegion = m_upButton.Region;
            m_upButton.DisabledRegion = m_upButton.Region;
            m_upButton.LocalPosition = new Vector2(m_width + MARGIN_HEIGHT, 0.0f);
            m_upButton.Colour = UIColours.Title;
            m_upButton.HighlightColour = UIColours.White;
            m_upButton.OnClicked += delegate (object sender, EventArgs e)
            {
                ScrollUp();
            };

            m_downButton = new Button(Texture.Get("gui/arrows.png", true), 32.0f, 32.0f);
            m_downButton.Region = new Quad(0.5f, 0.5f, 0.5f, 0.5f);
            m_downButton.HighlightRegion = m_downButton.Region;
            m_downButton.DisabledRegion = m_downButton.Region;
            m_downButton.LocalPosition = new Vector2(m_width + MARGIN_HEIGHT, Height - m_downButton.Height);
            m_downButton.Colour = UIColours.Title;
            m_downButton.HighlightColour = UIColours.White;
            m_downButton.OnClicked += delegate (object sender, EventArgs e)
            {
                ScrollDown();
            };

            m_highlight = -1;
            Refresh();
        }

        private void ScrollUp()
        {
            if (m_offset > 0)
            {
                m_offset--;
                Refresh();
            }
        }

        private void ScrollDown()
        {
            if (m_offset < m_count - ENTRIES_PER_PAGE)
            {
                m_offset++;
                Refresh();
            }
        }

        public void Refresh()
        {
            // Unhighlight
            int oldHighlight = m_highlight;
            int oldCount = m_count;
            if (Screen != null)
            {
                SetHighlight(-1);
            }

            // Dispose old elements
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                thumbnail.Dispose();
            }
            m_campaignThumbnails.Clear();

            // Get campaign list
            var campaigns = AllCampaigns();
            m_count = campaigns.Count;

            // Add new elements
            var yPos = 0.0f;
            for (int i = m_offset; i < Math.Min(campaigns.Count, m_offset + ENTRIES_PER_PAGE); ++i)
            {
                var campaign = campaigns[i].Campaign;
                var mod = campaigns[i].Mod;
                var thumbnail = new CampaignThumbnail(m_width, 16.0f + 2.0f * UIFonts.Default.Height);
                thumbnail.Anchor = Anchor;
                thumbnail.LocalPosition = LocalPosition + new Vector2(0.0f, yPos);

                // Set icon
                if (mod != null)
                {
                    var icon = mod.LoadIcon(true);
                    if (icon != null)
                    {
                        thumbnail.Icon = icon;
                        thumbnail.DisposeIcon = true;
                    }
                    else
                    {
                        thumbnail.Icon = Texture.Get("gui/blue_icon.png", true);
                    }
                }
                else
                {
                    thumbnail.Icon = Texture.Get("gui/red_icon.png", true);
                }

                // Set title, info and action
                int robotsRescued, totalRobots;
                bool allLevelsCompleted = ProgressUtils.IsCampaignCompleted(campaign, mod, m_game.User);
                robotsRescued = ProgressUtils.CountRobotsRescued(campaign, mod, m_game.User, out totalRobots);

                thumbnail.Title = campaign.Title;
                if (m_editor)
                {
                    thumbnail.Info = "[gui/red_robot.png] " + totalRobots;
                    thumbnail.Action = CampaignThumbnailAction.Delete;
                }
                else
                {
                    if (allLevelsCompleted)
                    {
                        thumbnail.Info = "[gui/red_robot.png] " + robotsRescued + " [gui/completed.png]";
                    }
                    else
                    {
                        thumbnail.Info = "[gui/red_robot.png] " + robotsRescued;
                    }
                    if (mod != null && mod.Source == ModSource.Editor)
                    {
                        thumbnail.Action = CampaignThumbnailAction.Edit;
                    }
                    else if (mod != null && mod.Source == ModSource.Workshop)
                    {
                        thumbnail.Action = CampaignThumbnailAction.ShowInWorkshop;
                    }
                }

                thumbnail.OnClicked += delegate (object sender, EventArgs e)
                {
                    FireOnSelection(campaign, mod);
                };
                thumbnail.OnActionClicked += delegate (object sender, EventArgs e)
                {
                    FireOnAction(campaign, mod, thumbnail.Action);
                };

                m_campaignThumbnails.Add(thumbnail);
                yPos += thumbnail.Height + MARGIN_HEIGHT;
            }
            m_actionButton.Anchor = Anchor;
            m_actionButton.LocalPosition = LocalPosition + new Vector2(0.0f, yPos);

            if (Screen != null)
            {
                // Init new elements
                for (int i = 0; i < m_campaignThumbnails.Count; ++i)
                {
                    var thumbnail = m_campaignThumbnails[i];
                    thumbnail.Init(Screen);
                }

                // Set highlight
                if (oldHighlight == oldCount)
                {
                    SetHighlight(m_count);
                }
                else if (oldHighlight >= 0 && oldHighlight < m_count)
                {
                    SetHighlight(oldHighlight);
                }
                else
                {
                    SetHighlight(-1);
                }
            }

            m_upButton.Visible = m_offset > 0;
            m_downButton.Visible = (m_offset + ENTRIES_PER_PAGE) < m_count;
        }

        private void FireOnSelection(Campaign campaign, Mod mod)
        {
            if (OnSelection != null)
            {
                OnSelection.Invoke(this, new CampaignEventArgs(campaign, mod));
            }
        }

        private void FireOnAction(Campaign campaign, Mod mod, CampaignThumbnailAction action)
        {
            if (OnAction != null)
            {
                OnAction.Invoke(this, new CampaignActionEventArgs(campaign, mod, action));
            }
        }

        private void FireOnBrowseWorkshop()
        {
            if (OnBrowseWorkshop != null)
            {
                OnBrowseWorkshop.Invoke(this, EventArgs.Empty);
            }
        }

        private void FireOnOpenModsFolder()
        {
            if (OnOpenModsFolder != null)
            {
                OnOpenModsFolder.Invoke(this, EventArgs.Empty);
            }
        }

        private void FireOnCreateCampaign()
        {
            if (OnCreateCampaign != null)
            {
                OnCreateCampaign.Invoke(this, EventArgs.Empty);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                thumbnail.Dispose();
            }
            m_actionButton.Dispose();
            m_upButton.Dispose();
            m_downButton.Dispose();
        }

        private int TestMouse()
        {
            var mousePos = Screen.MousePosition;
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                if (mousePos.X >= thumbnail.Position.X && mousePos.X < thumbnail.Position.X + thumbnail.Width &&
                    mousePos.Y >= thumbnail.Position.Y && mousePos.Y < thumbnail.Position.Y + thumbnail.Height)
                {
                    return m_offset + i;
                }
            }
            if (mousePos.X >= m_actionButton.Position.X && mousePos.X < m_actionButton.Position.X + m_actionButton.Width &&
                mousePos.Y >= m_actionButton.Position.Y && mousePos.Y < m_actionButton.Position.Y + m_actionButton.Height)
            {
                return m_count;
            }
            return -1;
        }

        private void ShowAndHighlight(int highlight)
        {
            if (highlight < 0 ||
                (highlight >= m_offset && highlight < m_offset + m_campaignThumbnails.Count) ||
                highlight == m_count)
            {
                SetHighlight(highlight);
            }
            else if (highlight < m_offset)
            {
                m_offset = highlight;
                Refresh();
                SetHighlight(highlight);
            }
            else if (highlight >= m_offset + m_campaignThumbnails.Count)
            {
                m_offset = highlight - ENTRIES_PER_PAGE + 1;
                Refresh();
                SetHighlight(highlight);
            }
        }

        private void SetHighlight(int highlight)
        {
            if (m_highlight >= 0)
            {
                if (m_highlight < m_count)
                {
                    if (m_highlight >= m_offset && m_highlight < m_offset + m_campaignThumbnails.Count)
                    {
                        m_campaignThumbnails[m_highlight - m_offset].Highlight = false;
                    }
                }
                else
                {
                    m_actionButton.Highlight = false;
                }
            }
            if (highlight >= 0 && highlight <= m_count)
            {
                m_highlight = highlight;
            }
            else
            {
                m_highlight = -1;
            }
            if (m_highlight >= 0)
            {
                if (m_highlight < m_count)
                {
                    if (m_highlight >= m_offset && m_highlight < m_offset + m_campaignThumbnails.Count)
                    {
                        m_campaignThumbnails[m_highlight - m_offset].Highlight = true;
                    }
                }
                else
                {
                    m_actionButton.Highlight = true;
                }
            }
        }

        protected override void OnInit()
        {
            // Init highlight
            if (Screen.InputMethod != InputMethod.Mouse)
            {
                SetHighlight(0);
            }

            // Init elements
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                thumbnail.Init(Screen);
            }
            m_actionButton.Init(Screen);
            m_upButton.Init(Screen);
            m_downButton.Init(Screen);
        }

        protected override void OnUpdate(float dt)
        {
            // Update elements
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                thumbnail.Update(dt);
            }
            m_actionButton.Update(dt);
            m_upButton.Update(dt);
            m_downButton.Update(dt);

            if (Screen.ModalDialog == Parent)
            {
                // Update input
                if (Screen.Mouse.DX != 0 || Screen.Mouse.DY != 0)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    var mouseHighlight = TestMouse();
                    if (mouseHighlight != m_highlight)
                    {
                        SetHighlight(mouseHighlight);
                        if (mouseHighlight >= 0)
                        {
                            PlayHighlightSound();
                        }
                    }
                }

                if (Screen.CheckUp())
                {
                    var target = (m_highlight >= 0) ? Math.Max(m_highlight - 1, 0) : m_count;
                    ShowAndHighlight(target);
                    PlayHighlightSound();
                }

                if (Screen.CheckDown())
                {
                    var target = (m_highlight >= 0) ? Math.Min(m_highlight + 1, m_count) : 0;
                    ShowAndHighlight(target);
                    PlayHighlightSound();
                }

                if (Screen.Mouse.Wheel < 0)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    ScrollDown();
                    SetHighlight(TestMouse());
                }
                else if (Screen.Mouse.Wheel > 0)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    ScrollUp();
                    SetHighlight(TestMouse());
                }
            }
            else
            {
                // Unfocus
                if (m_highlight >= 0)
                {
                    SetHighlight(-1);
                }
            }
        }

        protected override void OnRebuild()
        {
            float yPos = 0.0f;
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                thumbnail.Anchor = Anchor;
                thumbnail.LocalPosition = LocalPosition + new Vector2(0.0f, yPos);
                thumbnail.RequestRebuild();
                yPos += thumbnail.Height + MARGIN_HEIGHT;
            }

            m_actionButton.Anchor = Anchor;
            m_actionButton.LocalPosition = LocalPosition + new Vector2(0.0f, yPos);
            m_actionButton.RequestRebuild();

            m_upButton.Anchor = Anchor;
            m_upButton.LocalPosition = LocalPosition + new Vector2(m_width + MARGIN_HEIGHT, 0.0f);
            m_upButton.RequestRebuild();

            m_downButton.Anchor = Anchor;
            m_downButton.LocalPosition = LocalPosition + new Vector2(m_width + MARGIN_HEIGHT, Height - m_downButton.Height);
            m_downButton.RequestRebuild();
        }

        protected override void OnDraw()
        {
            for (int i = 0; i < m_campaignThumbnails.Count; ++i)
            {
                var thumbnail = m_campaignThumbnails[i];
                thumbnail.Draw();
            }
            m_actionButton.Draw();
            m_upButton.Draw();
            m_downButton.Draw();
        }

        private struct ModCampaign
        {
            public readonly Mod Mod;
            public readonly Campaign Campaign;

            public ModCampaign(Mod mod, Campaign campaign)
            {
                Mod = mod;
                Campaign = campaign;
            }
        }

        private List<ModCampaign> AllCampaigns()
        {
            var campaigns = new List<ModCampaign>();
            if (m_mod != null)
            {
                // Find campaigns in the mod
                foreach (var campaign in Assets.List<Campaign>("campaigns", m_mod.Assets))
                {
                    campaigns.Add(new ModCampaign(m_mod, campaign));
                }
            }
            else
            {
                // Find campaigns in the base game
                foreach (var campaign in Assets.List<Campaign>("campaigns"))
                {
					if (campaign.Levels.Count > 0 && !campaign.Hidden &&
                        !Assets.GetSources(campaign.Path).Where(source => source.Mod != null).Any())
                    {
                        campaigns.Add(new ModCampaign(null, campaign));
                    }
                }

                // Find campaigns in mods
                foreach (var mod in Mods.AllMods)
                {
                    foreach (var campaign in mod.Assets.LoadAll<Campaign>("campaigns"))
                    {
						if (campaign.Levels.Count > 0 && !campaign.Hidden )
                        {
                            campaigns.Add(new ModCampaign(mod, campaign));
                        }
                    }
                }
            }
            return campaigns;
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

