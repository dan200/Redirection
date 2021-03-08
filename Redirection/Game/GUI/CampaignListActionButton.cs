using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class BoxButton : Box
    {
        private Text m_text;
        private ITexture m_texture;
        private ITexture m_highlightTexture;
        private bool m_highlight;

        public string Text
        {
            get
            {
                return m_text.String;
            }
            set
            {
                m_text.String = value;
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
                    Texture = m_highlight ? m_highlightTexture : m_texture;
                }
            }
        }

        public event EventHandler<EventArgs> OnClicked;

        public BoxButton(float width, float height, ITexture texture, ITexture highlightTexture) : base(texture, width, height)
        {
            m_texture = texture;
            m_highlightTexture = highlightTexture;
            m_text = new Text(UIFonts.Smaller, "", UIColours.Text, TextAlignment.Center);
            m_text.Style = TextStyle.UpperCase;
        }

        public BoxButton(float width, float height) : this(width, height, Core.Render.Texture.Get("gui/button.png", true), Core.Render.Texture.Get("gui/button_hover.png", true))
        {
        }

        protected override void OnInit()
        {
            base.OnInit();
            m_text.Init(Screen);
        }

        private void FireOnClicked()
        {
            if (OnClicked != null)
            {
                OnClicked.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            m_text.Update(dt);

            if (Highlight && Screen.ModalDialog == Parent)
            {
                if (Screen.CheckSelect())
                {
                    FireOnClicked();
                    PlaySelectSound();
                }
                else if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    FireOnClicked();
                    PlaySelectSound();
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            m_text.Dispose();
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            m_text.Draw();
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();
            m_text.Anchor = Anchor;
            m_text.LocalPosition = LocalPosition + new Vector2(Width * 0.5f, (Height - m_text.Font.Height) * 0.5f);
            m_text.RequestRebuild();
        }

        private void PlaySelectSound()
        {
            Screen.Audio.PlaySound("sound/menu_select.wav");
        }
    }
}
