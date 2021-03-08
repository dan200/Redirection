
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Render;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.GUI
{
    public class ScrollingText : Element
    {
        private const float SCROLL_SPEED = 25.0f;

        private const float HOLD_TIME = 3.0f;
        private const float HOLD_APPEAR_TIME = 0.25f;
        private const float HOLD_WAIT_TIME = 0.5f;

        private enum ScrollingTextAnimation
        {
            Scroll,
            Hold,
        }

        private class ScrollingTextPage : IDisposable
        {
            private Screen m_screen;
            private CutsceneBorder m_border;
            private List<Element> m_lines;
            private float m_height;
            private bool m_visible;
            private ScrollingTextAnimation m_animation;

            public ScrollingTextAnimation Animation
            {
                get
                {
                    return m_animation;
                }
                set
                {
                    m_animation = value;
                }
            }

            public float Height
            {
                get
                {
                    return m_height;
                }
            }

            public bool Visible
            {
                get
                {
                    return m_visible;
                }
                set
                {
                    if (m_visible != value)
                    {
                        m_visible = value;
                        for (int i = 0; i < m_lines.Count; ++i)
                        {
                            var line = m_lines[i];
                            line.Visible = value;
                        }
                    }
                }
            }

            public ScrollingTextPage(Screen screen, CutsceneBorder border)
            {
                m_screen = screen;
                m_border = border;
                m_animation = ScrollingTextAnimation.Scroll;
                m_lines = new List<Element>();
                m_height = 0.0f;
            }

            public void Dispose()
            {
                for (int i = 0; i < m_lines.Count; ++i)
                {
                    var line = m_lines[i];
                    line.Dispose();
                }
            }

            public void AddLine(string text, Font font, Vector4 colour)
            {
                var maxWidth = m_screen.Width * 0.75f;
                var pos = 0;
                while (true)
                {
                    var partLength = font.WordWrap(text, pos, true, maxWidth);
                    var line = new Text(font, text.Substring(pos, partLength), colour, TextAlignment.Center);
                    line.Visible = m_visible;
                    line.LocalPosition = new Vector2(0.0f, m_height);
                    line.Init(m_screen);
                    m_lines.Add(line);
                    m_height += line.Height;
                    pos += partLength + Font.AdvanceWhitespace(text, pos + partLength);
                    if (pos >= text.Length)
                    {
                        break;
                    }
                }
            }

            public void AddImage(string path, bool localised)
            {
                var texture = localised ? Texture.GetLocalised(path, m_screen.Language, true) : Texture.Get(path, true);
                var width = 0.5f * (float)texture.Width;
                var height = 0.5f * (float)texture.Height;
                var image = new Image(texture, width, height);
                image.Visible = m_visible;
                image.LocalPosition = new Vector2(-0.5f * width, m_height);
                image.Init(m_screen);
                m_lines.Add(image);
                m_height += image.Height;
            }

            public void Animate(float t)
            {
                switch (m_animation)
                {
                    case ScrollingTextAnimation.Scroll:
                    default:
                        {
                            float distance = t * SCROLL_SPEED + m_border.BarHeight;
                            if (m_lines.Count > 0)
                            {
                                float baseLine = m_lines[0].LocalPosition.Y;
                                for (int j = 0; j < m_lines.Count; ++j)
                                {
                                    var element = m_lines[j];
                                    element.Anchor = Anchor.BottomMiddle;
                                    element.LocalPosition = new Vector2(
                                        element.LocalPosition.X,
                                        (element.LocalPosition.Y - baseLine) - distance
                                    );
                                }
                            }
                            break;
                        }
                    case ScrollingTextAnimation.Hold:
                        {
                            if (m_lines.Count > 0)
                            {
                                float alpha;
                                if (t < HOLD_APPEAR_TIME)
                                {
                                    alpha = (t / HOLD_APPEAR_TIME);
                                }
                                else if (t < HOLD_APPEAR_TIME + HOLD_TIME)
                                {
                                    alpha = 1.0f;
                                }
                                else if (t < HOLD_APPEAR_TIME + HOLD_TIME + HOLD_APPEAR_TIME)
                                {
                                    alpha = 1.0f - ((t - (HOLD_APPEAR_TIME + HOLD_TIME)) / HOLD_APPEAR_TIME);
                                }
                                else
                                {
                                    alpha = 0.0f;
                                }

                                float baseLine = m_lines[0].LocalPosition.Y;
                                for (int j = 0; j < m_lines.Count; ++j)
                                {
                                    var element = m_lines[j];
                                    if (element is Text)
                                    {
                                        var text = (Text)element;
                                        var colour = text.Colour;
                                        colour.W = alpha;
                                        text.Colour = colour;
                                    }
                                    else if (element is Image)
                                    {
                                        var image = (Image)element;
                                        var colour = image.Colour;
                                        colour.W = alpha;
                                        image.Colour = colour;
                                    }
                                    element.Anchor = Anchor.CentreMiddle;
                                    element.LocalPosition = new Vector2(
                                        element.LocalPosition.X,
                                        -0.5f * m_height + (element.LocalPosition.Y - baseLine)
                                    );
                                }
                            }
                            break;
                        }
                }
            }

            public bool IsFinished(float t, out float o_timeLeft)
            {
                switch (m_animation)
                {
                    case ScrollingTextAnimation.Scroll:
                    default:
                        {
                            float distance = t * SCROLL_SPEED;
                            float target = m_height + m_screen.Height - 2.0f * m_border.BarHeight;
                            if (distance >= target)
                            {
                                o_timeLeft = 0.0f;
                                return true;
                            }
                            else
                            {
                                o_timeLeft = (target - distance) / SCROLL_SPEED;
                                return false;
                            }
                        }
                    case ScrollingTextAnimation.Hold:
                        {
                            var duration = HOLD_APPEAR_TIME + HOLD_TIME + HOLD_APPEAR_TIME + HOLD_WAIT_TIME;
                            if (t >= duration)
                            {
                                o_timeLeft = 0.0f;
                                return true;
                            }
                            else
                            {
                                o_timeLeft = duration - t;
                                return false;
                            }
                        }
                }
            }

            public void RequestRebuild()
            {
                for (int i = 0; i < m_lines.Count; ++i)
                {
                    var line = m_lines[i];
                    line.RequestRebuild();
                }
            }

            public void Draw()
            {
                for (int i = 0; i < m_lines.Count; ++i)
                {
                    var line = m_lines[i];
                    line.Draw();
                }
            }
        }

        private TextAsset m_textFile;
        private CutsceneBorder m_border;
        private List<ScrollingTextPage> m_pages;
        private int m_page;
        private float m_timeOnPage;

        public bool IsFinished
        {
            get
            {
                return m_page >= m_pages.Count;
            }
        }

        public float TimeLeft
        {
            get
            {
                if (m_page < m_pages.Count)
                {
                    float timeLeft;
                    m_pages[m_page].IsFinished(m_timeOnPage, out timeLeft);
                    for (int i = m_page + 1; i < m_pages.Count; ++i)
                    {
                        float nextPageTime;
                        m_pages[i].IsFinished(0.0f, out nextPageTime);
                        timeLeft += nextPageTime;
                    }
                    return timeLeft;
                }
                return 0.0f;
            }
        }

        public ScrollingText(TextAsset textFile, CutsceneBorder border)
        {
            m_textFile = textFile;
            m_border = border;
            m_pages = new List<ScrollingTextPage>();
            m_page = 0;
            m_timeOnPage = 0.0f;
        }

        protected override void OnInit()
        {
            // Init credits
            var page = new ScrollingTextPage(Screen, m_border);
            for (int i = 0; i < m_textFile.Lines.Count; ++i)
            {
                var line = m_textFile.Lines[i];
                if (line == "---")
                {
                    // Start a new page
                    m_pages.Add(page);
                    page = new ScrollingTextPage(Screen, m_border);
                    continue;
                }

                if (line.StartsWith("!"))
                {
                    // Add an image
                    var path = line.Substring(1);
                    if (path.StartsWith("="))
                    {
                        path = line.Substring(1);
                        page.AddImage(path, true);
                    }
                    else
                    {
                        page.AddImage(path, false);
                    }
                }
                else
                {
                    // Add text
                    // Determine style
                    Font font;
                    Vector4 colour;
                    if (line.StartsWith("="))
                    {
                        font = UIFonts.Default;
                        colour = UIColours.Title;
                        line = line.Substring(1);
                    }
                    else if (line.StartsWith("-"))
                    {
                        font = UIFonts.Smaller;
                        colour = UIColours.Text;
                        line = line.Substring(1);
                    }
                    else
                    {
                        font = UIFonts.Default;
                        colour = UIColours.Text;
                    }

                    // Determine text
                    if (line.StartsWith("#", StringComparison.InvariantCulture))
                    {
                        line = Screen.Language.Translate(line.Substring(1));
                    }
                    page.AddLine(line, font, colour);
                }
            }
            m_pages.Add(page);

            // Setup animation
            for (int i = 0; i < m_pages.Count; ++i)
            {
                page = m_pages[i];
                page.Animation = ScrollingTextAnimation.Scroll; //(page.Height < Screen.Height * 0.75f) ? ScrollingTextAnimation.Hold : ScrollingTextAnimation.Scroll;
            }

            // Init scrolling
            m_timeOnPage = 0.0f;
            m_page = 0;
        }

        public override void Dispose()
        {
            base.Dispose();
            for (int i = 0; i < m_pages.Count; ++i)
            {
                var page = m_pages[i];
                page.Dispose();
            }
        }

        protected override void OnUpdate(float dt)
        {
            // Update scroll
            if (m_page < m_pages.Count)
            {
                var page = m_pages[m_page];
                float timeLeft;
                m_timeOnPage += dt;
                if (page.IsFinished(m_timeOnPage, out timeLeft))
                {
                    m_page++;
                    m_timeOnPage = 0.0f;
                }
            }

            // Update elements
            for (int i = 0; i < m_pages.Count; ++i)
            {
                var page = m_pages[i];
                if (m_page == i)
                {
                    page.Visible = true;
                    page.Animate(m_timeOnPage);
                }
                else
                {
                    page.Visible = false;
                }
            }
        }

        protected override void OnRebuild()
        {
            for (int i = 0; i < m_pages.Count; ++i)
            {
                var page = m_pages[i];
                page.RequestRebuild();
            }
        }

        protected override void OnDraw()
        {
            for (int i = 0; i < m_pages.Count; ++i)
            {
                var page = m_pages[i];
                page.Draw();
            }
        }
    }
}
