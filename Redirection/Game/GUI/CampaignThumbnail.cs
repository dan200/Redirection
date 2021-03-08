using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public enum CampaignThumbnailAction
    {
        None,
        Delete,
        Edit,
        ShowInWorkshop
    }

    public class CampaignThumbnail : Box
    {
        private Box m_iconBorder;
        private Image m_titleCover;
        private Image m_titleCover2;
        private Image m_icon;
        private Text m_title;
        private Text m_info;
        private bool m_highlight;

        private CampaignThumbnailAction m_action;
        private Image m_actionButton;
        private bool m_actionHover;

        public ITexture Icon
        {
            get
            {
                return m_icon.Texture;
            }
            set
            {
                m_icon.Texture = value;
            }
        }

        public bool DisposeIcon;

        public string Title
        {
            get
            {
                return m_title.String;
            }
            set
            {
                m_title.String = value;
            }
        }

        public string Info
        {
            get
            {
                return m_info.String;
            }
            set
            {
                m_info.String = value;
            }
        }

        public CampaignThumbnailAction Action
        {
            get
            {
                return m_action;
            }
            set
            {
                m_action = value;
                RequestRebuild();
            }
        }

        public bool Highlight
        {
            get
            {
                return m_highlight;
            }
            set
            {
                if (m_highlight != value)
                {
                    m_highlight = value;
                    RequestRebuild();
                    UpdateTexture();
                }
            }
        }

        public event EventHandler<EventArgs> OnClicked;
        public event EventHandler<EventArgs> OnActionClicked;

        public CampaignThumbnail(float width, float height) : base(Core.Render.Texture.Get("gui/dialog.png", true), width, height)
        {
            var iconSize = height - 6.0f - 2.0f * DialogBox.BOTTOM_MARGIN_SIZE;
            m_icon = new Image(Core.Render.Texture.Get("gui/blue_icon.png", true), iconSize, iconSize);
            m_iconBorder = new Box(Core.Render.Texture.Get("gui/inset_border.png", true), m_icon.Width + 6.0f, m_icon.Height + 6.0f);
            m_titleCover = new Image(Core.Render.Texture.Get("gui/dialog_title_cover.png", true), new Quad(0.0f, 0.0f, 0.5f, 1.0f), 32.0f, 32.0f);
            m_titleCover2 = new Image(Core.Render.Texture.Get("gui/dialog_title_cover.png", true), new Quad(0.5f, 0.0f, 0.5f, 1.0f), 32.0f, 32.0f);
            m_titleCover2.Stretch = true;

            m_title = new Text(UIFonts.Smaller, "", UIColours.Text, TextAlignment.Left);
            m_title.Style = TextStyle.UpperCase;

            m_info = new Text(UIFonts.Default, "", UIColours.Text, TextAlignment.Left);
            m_info.Style = TextStyle.Default;

            m_action = CampaignThumbnailAction.None;
            m_actionButton = new Image(Core.Render.Texture.White, 32.0f, 32.0f);
            m_actionButton.Visible = false;
            m_actionHover = false;
        }

        protected override void OnInit()
        {
            base.OnInit();
            m_iconBorder.Init(Screen);
            m_titleCover.Init(Screen);
            m_titleCover2.Init(Screen);
            m_icon.Init(Screen);
            m_title.Init(Screen);
            m_info.Init(Screen);
            m_actionButton.Init(Screen);
        }

        private void FireOnClicked()
        {
            if (OnClicked != null)
            {
                OnClicked.Invoke(this, EventArgs.Empty);
            }
        }

        private void FireOnActionClicked()
        {
            if (OnActionClicked != null)
            {
                OnActionClicked.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateTexture()
        {
            Texture = m_highlight ?
                Core.Render.Texture.Get("gui/dialog_hover.png", true) :
                Core.Render.Texture.Get("gui/dialog.png", true);
            m_titleCover.Texture = m_highlight ?
                Core.Render.Texture.Get("gui/dialog_title_cover_hover.png", true) :
                Core.Render.Texture.Get("gui/dialog_title_cover.png", true);
            m_titleCover2.Texture =
                m_titleCover.Texture;
            Colour = UIColours.White;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            m_iconBorder.Update(dt);
            m_titleCover.Update(dt);
            m_titleCover2.Update(dt);
            m_icon.Update(dt);
            m_title.Update(dt);
            m_info.Update(dt);
            m_actionButton.Update(dt);

            bool actionHover =
                m_action != CampaignThumbnailAction.None &&
                Screen.ModalDialog == Parent &&
                Screen.MousePosition.X >= m_actionButton.Position.X &&
                Screen.MousePosition.X < m_actionButton.Position.X + m_actionButton.Width &&
                Screen.MousePosition.Y >= m_actionButton.Position.Y &&
                Screen.MousePosition.Y < m_actionButton.Position.Y + m_actionButton.Height;
            if (actionHover != m_actionHover)
            {
                m_actionHover = actionHover;
                RequestRebuild();
                if (m_actionHover)
                {
                    PlayHighlightSound();
                }
            }

            if (Highlight && Screen.ModalDialog == Parent)
            {
                if (Screen.CheckSelect())
                {
                    FireOnClicked();
                    PlaySelectSound();
                }
                else if (Screen.CheckAltSelect() && m_action != CampaignThumbnailAction.None)
                {
                    FireOnActionClicked();
                    PlaySelectSound();
                }
                else if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    if (m_actionHover)
                    {
                        FireOnActionClicked();
                    }
                    else
                    {
                        FireOnClicked();
                    }
                    PlaySelectSound();
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            m_iconBorder.Dispose();

            var icon = m_icon.Texture;
            m_icon.Dispose();
            if (DisposeIcon && icon is IDisposable)
            {
                ((IDisposable)icon).Dispose();
            }

            m_titleCover.Dispose();
            m_titleCover2.Dispose();
            m_title.Dispose();
            m_info.Dispose();
            m_actionButton.Dispose();
        }

        protected override void OnDraw()
        {
            base.OnDraw();

            m_titleCover.Draw();
            m_titleCover2.Draw();
            m_iconBorder.Draw();
            m_icon.Draw();
            m_title.Draw();
            m_info.Draw();
            m_actionButton.Draw();
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();

            m_icon.Anchor = Anchor;
            m_icon.LocalPosition = LocalPosition + new Vector2(Width - DialogBox.RIGHT_MARGIN_SIZE - 6.0f - 3.0f - m_icon.Width, DialogBox.BOTTOM_MARGIN_SIZE + 3.0f);
            m_icon.Visible = true;
            m_icon.RequestRebuild();

            m_iconBorder.Anchor = Anchor;
            m_iconBorder.LocalPosition = m_icon.LocalPosition - new Vector2(3.0f, 3.0f);
            m_iconBorder.Visible = m_icon.Visible;
            m_icon.RequestRebuild();

            m_titleCover.Anchor = Anchor;
            m_titleCover.LocalPosition = new Vector2(m_icon.LocalPosition.X - 3.0f - 29.0f, LocalPosition.Y);
            m_titleCover.RequestRebuild();

            m_titleCover2.Anchor = Anchor;
            m_titleCover2.LocalPosition = m_titleCover.LocalPosition + new Vector2(32.0f, 0.0f);
            m_titleCover2.Width = (LocalPosition.X + Width - 16.0f) - m_titleCover2.LocalPosition.X;
            m_titleCover.RequestRebuild();

            m_title.Anchor = Anchor;
            m_title.LocalPosition = LocalPosition + new Vector2(24.0f, 16.0f - 0.5f * m_title.Font.Height);
            m_title.RequestRebuild();

            m_info.Anchor = Anchor;
            m_info.LocalPosition = LocalPosition + new Vector2(16.0f, DialogBox.TOP_MARGIN_SIZE + 0.5f * (Height - DialogBox.TOP_MARGIN_SIZE - DialogBox.BOTTOM_MARGIN_SIZE) - 0.5f * m_info.Font.Height);
            m_info.RequestRebuild();

            bool actionHighlight = m_highlight && (m_actionHover || Screen.InputMethod != InputMethod.Mouse);
            m_actionButton.Anchor = Anchor;
            m_actionButton.LocalPosition = new Vector2(
                m_iconBorder.LocalPosition.X - 6.0f - m_actionButton.Width,
                LocalPosition.Y + Height - DialogBox.BOTTOM_MARGIN_SIZE - m_actionButton.Height
            );
            switch (m_action)
            {
                case CampaignThumbnailAction.None:
                default:
                    {
                        m_actionButton.Visible = false;
                        break;
                    }
                case CampaignThumbnailAction.Delete:
                    {
                        m_actionButton.Visible = true;
                        m_actionButton.Texture = Core.Render.Texture.Get("gui/trash.png", true);
                        break;
                    }
                case CampaignThumbnailAction.Edit:
                    {
                        m_actionButton.Visible = true;
                        m_actionButton.Texture = Core.Render.Texture.Get("gui/edit.png", true);
                        break;
                    }
                case CampaignThumbnailAction.ShowInWorkshop:
                    {
                        m_actionButton.Visible = true;
                        m_actionButton.Texture = Core.Render.Texture.Get("gui/info.png", true);
                        break;
                    }
            }
            m_actionButton.Area = actionHighlight ? new Quad(0.5f, 0.0f, 0.5f, 1.0f) : new Quad(0.0f, 0.0f, 0.5f, 1.0f);
            m_actionButton.RequestRebuild();
        }

        private void PlaySelectSound()
        {
            Screen.Audio.PlaySound("sound/menu_select.wav");
        }

        private void PlayHighlightSound()
        {
            Screen.Audio.PlaySound("sound/menu_highlight.wav");
        }
    }
}
