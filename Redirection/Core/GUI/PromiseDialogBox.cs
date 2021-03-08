
using Dan200.Core.Async;
using OpenTK;
using System;

namespace Dan200.Core.GUI
{
    public class PromiseDialogBox : DialogBox
    {
        // Creates a box which displays a line of text until a promise completes
        public static PromiseDialogBox Create(string text, Promise promise)
        {
            // Calculate dimensions
            float width = 0.0f;
            width = Math.Max(width, UIFonts.Default.Measure(text, true) + LEFT_MARGIN_SIZE + RIGHT_MARGIN_SIZE + 16.0f);

            float height =
                BOTTOM_MARGIN_SIZE +
                UIFonts.Default.Height +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new PromiseDialogBox(width, height, promise);

            // Populate dialog
            var title = new Text(UIFonts.Default, text, UIColours.Text, TextAlignment.Center);
            title.Anchor = dialog.Anchor;
            title.LocalPosition = dialog.LocalPosition + new Vector2(0.5f * dialog.Width, BOTTOM_MARGIN_SIZE);
            dialog.Elements.Add(title);

            return dialog;
        }

        private Promise m_promise;
        private float m_timeOpen;

        public PromiseDialogBox(float width, float height, Promise promise) : base("", width, height)
        {
            ShowTitle = false;
            AllowUserClose = false;
            m_promise = promise;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            m_timeOpen += dt;
            if (m_promise.Status != Status.Waiting && m_timeOpen >= 0.6f)
            {
                Close(0);
            }
        }
    }
}
