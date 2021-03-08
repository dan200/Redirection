using OpenTK;
using System;
using System.Text.RegularExpressions;

namespace Dan200.Core.GUI
{
    public class TextEntryDialogBox : DialogBox
    {
        // Creates a box for entering one line of text
        public static TextEntryDialogBox Create(string title, string defaultText, string textRegex, float textWidth, string[] options)
        {
            // Calculate dimensions
            float width = 256.0f;
            width = Math.Max(width, UIFonts.Smaller.Measure(title, true) + 72.0f);
            width = Math.Max(width, textWidth + LEFT_MARGIN_SIZE + RIGHT_MARGIN_SIZE);

            float height =
                TOP_MARGIN_SIZE +
                1.2f * UIFonts.Default.Height +
                4.0f +
                UIFonts.Smaller.Height +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new TextEntryDialogBox(title, width, height);

            // Populate dialog
            float yPos = -0.5f * height + TOP_MARGIN_SIZE;

            var textBox = new TextBox(width - LEFT_MARGIN_SIZE - RIGHT_MARGIN_SIZE, 1.2f * UIFonts.Default.Height);
            textBox.Anchor = Anchor.CentreMiddle;
            textBox.LocalPosition = new Vector2(-0.5f * textBox.Width, yPos);
            textBox.Text = defaultText;
            textBox.Focus = true;
            dialog.Elements.Add(textBox);
            yPos += textBox.Height + 4.0f;

            var optionsMenu = new TextMenu(UIFonts.Smaller, options, TextAlignment.Center, MenuDirection.Horizontal);
            optionsMenu.ShowBackground = true;
            optionsMenu.Anchor = Anchor.CentreMiddle;
            optionsMenu.Parent = dialog;
            optionsMenu.LocalPosition = new Vector2(0.0f, yPos);
            optionsMenu.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                switch (e.Index)
                {
                    case 0:
                        {
                            if (Regex.IsMatch(textBox.Text, textRegex))
                            {
                                dialog.EnteredText = textBox.Text;
                                dialog.Close(e.Index);
                            }
                            break;
                        }
                    default:
                        {
                            dialog.Close(e.Index);
                            break;
                        }
                }
            };
            dialog.Elements.Add(optionsMenu);

            return dialog;
        }

        public string EnteredText;

        public TextEntryDialogBox(string title, float width, float height) : base(title, width, height)
        {
            EnteredText = null;
        }
    }
}

