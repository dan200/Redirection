using System;
using System.Collections.Generic;

using OpenTK;
using Dan200.Core.Input;
using Dan200.Game.GUI;
using Dan200.Game.Input;

using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    public class DialogBoxClosedEventArgs : EventArgs
    {
        public readonly int Result;

        public DialogBoxClosedEventArgs(int result)
        {
            Result = result;
        }
    }

    public class DialogBox : Box
    {
        public const float GROW_TIME = 0.3f;

        public const float TOP_MARGIN_SIZE = 36.0f;
        public const float BOTTOM_MARGIN_SIZE = 8.0f;
        public const float LEFT_MARGIN_SIZE = 8.0f;
        public const float RIGHT_MARGIN_SIZE = 8.0f;

        private static Vector4 BACKGROUND_COLOUR = new Vector4(0.0f, 0.0f, 0.0f, 0.5f);

        // Creates a simple YES/NO, OK/CANCEL or just plain OK type box
        public static DialogBox CreateQueryBox(Screen screen, string title, string info, string[] options, bool important)
        {
            // Calculate dimensions
            float width = 256.0f;
            width = Math.Max(width, UIFonts.Smaller.Measure(title, true) + 72.0f);

            // Word wrap
            var infoFont = UIFonts.Smaller;
            List<string> infoLines = new List<string>();
            var maxInfoWidth = screen.Width - 64.0f - LEFT_MARGIN_SIZE - RIGHT_MARGIN_SIZE;
            int pos = 0;
            while (pos < info.Length)
            {
                var lineLength = infoFont.WordWrap(info, pos, true, maxInfoWidth);
                var line = info.Substring(pos, lineLength);
                infoLines.Add(line);
                width = Math.Max(width, infoFont.Measure(line, true) + LEFT_MARGIN_SIZE + RIGHT_MARGIN_SIZE + 16.0f);
                pos += lineLength + Font.AdvanceWhitespace(info, pos + lineLength);
            }

            float height =
                TOP_MARGIN_SIZE +
                infoLines.Count * infoFont.Height +
                3.0f +
                UIFonts.Smaller.Height +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new DialogBox(title, width, height);
            dialog.Important = important;

            // Populate dialog
            float yPos = -0.5f * height + TOP_MARGIN_SIZE;
            for (int i = 0; i < infoLines.Count; ++i)
            {
                var infoText = new Text(infoFont, infoLines[i], UIColours.Text, TextAlignment.Center);
                infoText.Anchor = Anchor.CentreMiddle;
                infoText.LocalPosition = new Vector2(0.0f, yPos);
                dialog.Elements.Add(infoText);
                yPos += infoText.Font.Height;
            }
            yPos += 3.0f;

            var optionsMenu = new TextMenu(UIFonts.Smaller, options, TextAlignment.Center, MenuDirection.Horizontal);
            optionsMenu.ShowBackground = true;
            optionsMenu.Anchor = Anchor.CentreMiddle;
            optionsMenu.LocalPosition = new Vector2(0.0f, yPos);
            optionsMenu.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                if (e.Index >= 0)
                {
                    dialog.Close(e.Index);
                }
            };
            dialog.Elements.Add(optionsMenu);

            return dialog;
        }

        // Creates a simple YES/NO, OK/CANCEL or just plain OK type box
        public static DialogBox CreateImageQueryBox(string title, ITexture imageTexture, float imageWidth, float imageHeight, string[] options)
        {
            // Calculate dimensions
            float width = 256.0f;
            width = Math.Max(width, UIFonts.Smaller.Measure(title, true) + 72.0f);
            width = Math.Max(width, LEFT_MARGIN_SIZE + imageWidth + 6.0f + RIGHT_MARGIN_SIZE);

            float height =
                TOP_MARGIN_SIZE +
                imageHeight +
                10.0f +
                UIFonts.Smaller.Height +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new DialogBox(title, width, height);

            // Populate dialog
            float yPos = -0.5f * height + TOP_MARGIN_SIZE;

            var imageBox = new Box(Core.Render.Texture.Get("gui/inset_border.png", true), imageWidth + 6.0f, imageHeight + 6.0f);
            imageBox.Anchor = Anchor.CentreMiddle;
            imageBox.LocalPosition = new Vector2(-0.5f * imageWidth - 3.0f, yPos);
            dialog.Elements.Add(imageBox);

            var image = new Image(imageTexture, imageWidth, imageHeight);
            image.Anchor = Anchor.CentreMiddle;
            image.LocalPosition = new Vector2(-0.5f * imageWidth, yPos + 3.0f);
            dialog.Elements.Add(image);
            yPos += imageHeight;
            yPos += 10.0f;

            var optionsMenu = new TextMenu(UIFonts.Smaller, options, TextAlignment.Center, MenuDirection.Horizontal);
            optionsMenu.ShowBackground = true;
            optionsMenu.Anchor = Anchor.CentreMiddle;
            optionsMenu.LocalPosition = new Vector2(0.0f, yPos);
            optionsMenu.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                if (e.Index >= 0)
                {
                    dialog.Close(e.Index);
                }
            };
            dialog.Elements.Add(optionsMenu);

            return dialog;
        }

        // Creates a vertical list of options
        public static DialogBox CreateMenuBox(string title, string[] options, bool important)
        {
            // Calculate dimensions
            float width = 300.0f;
            width = Math.Max(width, UIFonts.Smaller.Measure(title, true) + 72.0f);
            for (int i = 0; i < options.Length; ++i)
            {
                var textLength = UIFonts.Default.Measure(options[i], true);
                width = Math.Max(width, LEFT_MARGIN_SIZE + TextMenu.MARGIN + textLength + TextMenu.MARGIN + RIGHT_MARGIN_SIZE);
            }

            float height =
                TOP_MARGIN_SIZE +
                ((float)options.Length * UIFonts.Default.Height) +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new DialogBox(title, width, height);
            dialog.Important = important;

            // Populate dialog
            float yPos = -0.5f * height + TOP_MARGIN_SIZE;
            var optionsMenu = new TextMenu(UIFonts.Default, options, TextAlignment.Center, MenuDirection.Vertical);
            optionsMenu.ShowBackground = true;
            optionsMenu.MinimumWidth = (width - LEFT_MARGIN_SIZE - RIGHT_MARGIN_SIZE);
            optionsMenu.Anchor = Anchor.CentreMiddle;
            optionsMenu.LocalPosition = new Vector2(0.0f, yPos);
            optionsMenu.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                if (e.Index >= 0)
                {
                    dialog.Close(e.Index);
                }
                else
                {
                    dialog.Close(-1);
                }
            };
            dialog.Elements.Add(optionsMenu);

            return dialog;
        }

        private Geometry m_bgGeometry;

        private float m_growth;
        private InputPrompt m_acceptPrompt;
        private InputPrompt m_closePrompt;
        private Text m_title;
        private List<Element> m_elements;
        private bool m_allowUserClose;
        private bool m_important;
        private bool m_disposed;

        private enum State
        {
            Closed,
            Opening,
            Open,
            Closing,
        }
        private State m_state;
        private int m_result;

        public bool AllowUserClose
        {
            get
            {
                return m_allowUserClose;
            }
            set
            {
                m_allowUserClose = value;
                m_closePrompt.Visible = value;
            }
        }

        public bool ShowAcceptPrompt
        {
            get
            {
                return m_acceptPrompt.Visible;
            }
            set
            {
                m_acceptPrompt.Visible = value;
            }
        }

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

        public bool ShowTitle
        {
            get
            {
                return m_title.Visible;
            }
            set
            {
                m_title.Visible = value;
                UpdateTexture();
            }
        }

        public bool Important
        {
            get
            {
                return m_important;
            }
            set
            {
                m_important = value;
                UpdateTexture();
            }
        }

        public bool Disposed
        {
            get
            {
                return m_disposed;
            }
        }

        public bool PauseWorld = true;
        public bool BlockInput = true;
        public bool FadeWorld = true;

        public bool IsOpen
        {
            get
            {
                return m_state == State.Open;
            }
        }

        public bool IsClosed
        {
            get
            {
                return m_state == State.Closed;
            }
        }

        public bool IsClosing
        {
            get
            {
                return m_state == State.Closing;
            }
        }

        public class ElementSet
        {
            private DialogBox m_owner;

            public ElementSet(DialogBox owner)
            {
                m_owner = owner;
            }

            public void Add(Element element)
            {
                m_owner.m_elements.Add(element);
                element.Parent = m_owner;
                if (m_owner.Screen != null)
                {
                    element.Init(m_owner.Screen);
                }
            }

            public void Remove(Element element)
            {
                if (m_owner.m_elements.Contains(element))
                {
                    m_owner.m_elements.Remove(element);
                    element.Parent = null;
                }
            }

            public void Clear()
            {
                m_owner.m_elements.Clear();
            }
        }

        public ElementSet Elements
        {
            get
            {
                return new ElementSet(this);
            }
        }

        public EventHandler<DialogBoxClosedEventArgs> OnClosed;

        public DialogBox(string title, float width, float height) : this(title, Anchor.CentreMiddle, -0.5f * width, -0.5f * height, width, height)
        {
        }

        public DialogBox(string title, Anchor anchor, float x, float y, float width, float height) : base(Core.Render.Texture.Get("gui/dialog.png", true), width, height)
        {
            m_bgGeometry = new Geometry(Primitive.Triangles, 4, 6);
            m_elements = new List<Element>();

            m_title = new Text(UIFonts.Smaller, title, UIColours.Text, TextAlignment.Left);
            //m_title.Style = TextStyle.UpperCase;
            m_title.Anchor = anchor;
            m_title.LocalPosition = new Vector2(x + 24.0f, y + 16.0f - 0.5f * m_title.Font.Height);
            m_elements.Add(m_title);

            m_closePrompt = new InputPrompt(UIFonts.Smaller, "", TextAlignment.Right);
            m_closePrompt.Key = Key.Escape;
            m_closePrompt.MouseButton = MouseButton.Left;
            m_closePrompt.GamepadButton = GamepadButton.B;
            m_closePrompt.SteamControllerButton = SteamControllerButton.MenuBack;
            m_closePrompt.Parent = this;
            m_closePrompt.Visible = true;
            m_closePrompt.OnClick += delegate (object sender, EventArgs e)
            {
                if (m_state == State.Open && m_allowUserClose)
                {
                    Close(-1);
                }
            };
            m_allowUserClose = true;

            m_acceptPrompt = new InputPrompt(UIFonts.Smaller, "", TextAlignment.Left);
            m_acceptPrompt.Key = Key.Return;
            m_acceptPrompt.GamepadButton = GamepadButton.A;
            m_acceptPrompt.SteamControllerButton = SteamControllerButton.MenuSelect;
            m_acceptPrompt.Parent = this;
            m_acceptPrompt.Visible = false;

            m_state = State.Opening;
            m_growth = 0.0f;
            m_result = -1;
            m_important = false;

            Anchor = anchor;
            LocalPosition = new Vector2(x, y);
        }

        public void Open()
        {
            if (m_state != State.Open && m_state != State.Opening)
            {
                // Open form
                m_state = State.Opening;
                m_result = -1;
                PlayGrowSound();
            }
        }

        public void Close(int result)
        {
            if (m_state != State.Closed && m_state != State.Closing)
            {
                // Close form
                m_state = State.Closing;
                m_result = result;
                PlayShrinkSound();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            // Dispose elements
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].Dispose();
            }
            m_closePrompt.Dispose();
            m_acceptPrompt.Dispose();

            // Dispose self
            m_bgGeometry.Dispose();

            if (Screen != null && Screen.ModalDialog == this)
            {
                Screen.ModalDialog = null;
            }

            m_disposed = true;
        }

        protected override void OnInit()
        {
            Screen.ModalDialog = this;
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].Init(Screen);
            }

            m_closePrompt.String = Screen.Language.Translate("menus.close");
            m_closePrompt.Init(Screen);

            m_acceptPrompt.String = Screen.Language.Translate("menus.select");
            m_acceptPrompt.Init(Screen);

            PlayGrowSound();
        }

        private void PlayGrowSound()
        {
            //Screen.Audio.PlaySound("sound/menu_grow.wav");
        }

        private void PlayShrinkSound()
        {
            //Screen.Audio.PlaySound("sound/menu_shrink.wav");
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
            if (Screen.Keyboard.Keys[Key.Escape].Pressed)
            {
                if (Screen.InputMethod != InputMethod.Mouse)
                {
                    Screen.InputMethod = InputMethod.Keyboard;
                }
                return true;
            }
            return false;
        }

        protected override void OnUpdate(float dt)
        {
            // Check input
            int numMenus = 0;
            int numFocusedMenus = 0;
            for (int i = 0; i < m_elements.Count; ++i)
            {
                var element = m_elements[i];
                if (element is TextMenu)
                {
                    var textMenu = (TextMenu)element;
                    numMenus++;
                    if (textMenu.Focus >= 0)
                    {
                        numFocusedMenus++;
                    }
                }
            }
            if (numMenus > 0)
            {
                ShowAcceptPrompt = Screen.InputMethod != InputMethod.Mouse && (numFocusedMenus > 0);
            }
            if (m_state == State.Open && m_allowUserClose && CheckClose())
            {
                Close(-1);
            }

            // Animate
            if (m_state == State.Opening)
            {
                // Grow
                m_growth += dt / GROW_TIME;
                if (m_growth >= 1.0f)
                {
                    m_growth = 1.0f;
                    m_state = State.Open;
                }
                RequestRebuild();
            }

            if (m_state == State.Open)
            {
                // Update
                for (int i = 0; i < m_elements.Count; ++i)
                {
                    m_elements[i].Update(dt);
                }
                m_closePrompt.Update(dt);
                m_acceptPrompt.Update(dt);
            }

            if (m_state == State.Closing)
            {
                // Shrink
                m_growth -= dt / GROW_TIME;
                if (m_growth < 0.0f)
                {
                    m_growth = 0.0f;
                    m_state = State.Closed;
                    if (OnClosed != null)
                    {
                        OnClosed.Invoke(this, new DialogBoxClosedEventArgs(m_result));
                    }
                    if (Screen.ModalDialog == this)
                    {
                        Screen.ModalDialog = null;
                    }
                }
                else
                {
                    RequestRebuild();
                }
            }
        }

        protected override void OnDraw()
        {
            if (m_state != State.Closed)
            {
                if (FadeWorld)
                {
                    // Draw background
                    Screen.Effect.Colour = BACKGROUND_COLOUR;
                    Screen.Effect.Texture = Core.Render.Texture.White;
                    Screen.Effect.Bind();
                    m_bgGeometry.Draw();
                }

                // Draw self
                base.OnDraw();

                // Draw elements
                if (m_state == State.Open)
                {
                    for (int i = 0; i < m_elements.Count; ++i)
                    {
                        m_elements[i].Draw();
                    }
                    m_closePrompt.Draw();
                    m_acceptPrompt.Draw();
                }
            }
        }

        protected override void OnRebuild()
        {
            m_closePrompt.Anchor = Anchor;
            m_closePrompt.LocalPosition = LocalPosition + new Vector2(Width, Height + 0.25f * m_closePrompt.Font.Height);
            m_closePrompt.RequestRebuild();

            m_acceptPrompt.Anchor = Anchor;
            m_acceptPrompt.LocalPosition = LocalPosition + new Vector2(0.0f, Height + 0.25f * m_acceptPrompt.Font.Height);
            m_acceptPrompt.RequestRebuild();

            // Rebuild elements
            for (int i = 0; i < m_elements.Count; ++i)
            {
                m_elements[i].RequestRebuild();
            }

            // Rebuild background
            m_bgGeometry.Clear();
            m_bgGeometry.Add2DQuad(Vector2.Zero, new Vector2(Screen.Width, Screen.Height));
            m_bgGeometry.Rebuild();

            // Determine current size
            float minWidth = Math.Min(80.0f, Width);
            float currentWidth = minWidth + (m_growth * (Width - minWidth));
            float minHeight = Math.Min(64.0f, Height);
            float currentHeight = minHeight + (m_growth * (Height - minHeight));

            // Rebuild self
            if (m_state != State.Closed)
            {
                float startX = (Width - currentWidth) * 0.5f;
                float startY = (Height - currentHeight) * 0.5f;
                float endX = startX + currentWidth;
                float endY = startY + currentHeight;
                RebuildBoxGeometry(startX, endX, startY, endY);
            }
            else
            {
                ClearBoxGeometry();
            }
        }

        private void UpdateTexture()
        {
            if (m_title.Visible)
            {
                if (m_important)
                {
                    //Texture = Core.Render.Texture.Get("gui/dialog_important.png", true);
                    Texture = Core.Render.Texture.Get("gui/dialog.png", true);
                }
                else
                {
                    Texture = Core.Render.Texture.Get("gui/dialog.png", true);
                }
            }
            else
            {
                Texture = Core.Render.Texture.Get("gui/simple_dialog.png", true);
            }
            RequestRebuild();
        }
    }
}
