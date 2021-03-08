using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    // Values match SDL2 SDL_BUTTON_xxx constants
    public enum MouseButton
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 3
    }

    public static class MouseButtonExtensions
    {
        public static string GetPrompt(this MouseButton button)
        {
            if (button == MouseButton.None)
            {
                return "?";
            }

            return '[' + button.GetPromptPath() + ']';
        }

        public static string GetPromptPath(this MouseButton button)
        {
            var buttonName = button.ToString().ToLowerUnderscored();
            return "gui/prompts/mouse/" + buttonName + ".png";
        }
    }
}
