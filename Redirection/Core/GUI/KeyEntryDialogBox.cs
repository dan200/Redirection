using Dan200.Core.Input;
using OpenTK;
using System;

namespace Dan200.Core.GUI
{
    public class KeyEntryDialogBox : DialogBox
    {
        // Creates a box which displays a line of text until a key is pressed
        public static KeyEntryDialogBox Create(string text)
        {
            // Calculate dimensions
            float width = 0.0f;
            width = Math.Max(width, UIFonts.Default.Measure(text, true) + LEFT_MARGIN_SIZE + RIGHT_MARGIN_SIZE + 16.0f);

            float height =
                BOTTOM_MARGIN_SIZE +
                UIFonts.Default.Height +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new KeyEntryDialogBox(width, height);

            // Populate dialog
            var title = new Text(UIFonts.Default, text, UIColours.Text, TextAlignment.Center);
            title.Anchor = dialog.Anchor;
            title.LocalPosition = dialog.LocalPosition + new Vector2(0.5f * dialog.Width, BOTTOM_MARGIN_SIZE);
            dialog.Elements.Add(title);

            return dialog;
        }

        private Key? m_result;

        public Key? Result
        {
            get
            {
                return m_result;
            }
        }

        public KeyEntryDialogBox(float width, float height) : base("", width, height)
        {
            ShowTitle = false;
            m_result = null;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            if (IsOpen && !m_result.HasValue)
            {
                var keyboard = Screen.Keyboard;
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    if (key != Key.None &&
                        keyboard.Keys[key].Pressed &&
                        key != Key.Escape &&
                        !(key >= Key.F1 && key <= Key.F12)) // F-keys are debug only, Escape is for closing
                    {
                        Screen.InputMethod = InputMethod.Keyboard;
                        m_result = key;
                        Close(0);
                        break;
                    }
                }
            }
        }
    }
}
