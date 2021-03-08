using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Util;
using Dan200.Game.Game;
using Dan200.Game.User;

namespace Dan200.Game.Options
{
    public class KeyBindOption : IOption
    {
        private Game.Game m_game;
        private OptionsState m_menu;
        private Bind m_binding;
        private bool m_awaitingPress;

        public KeyBindOption(Game.Game game, OptionsState menu, Bind binding)
        {
            m_game = game;
            m_menu = menu;
            m_binding = binding;
        }

        public string ToString(Language language)
        {
            var name = language.Translate("inputs." + m_binding.ToString().ToLowerUnderscored() + ".name");
            var value = m_awaitingPress ?
                "[gui/prompts/keyboard/blank.png]" :
                m_game.User.Settings.GetKeyBind(m_binding).GetPrompt();
            return name + " - " + value;
        }

        public void Click()
        {
            m_awaitingPress = true;

            var dialog = KeyEntryDialogBox.Create(m_game.Language.Translate("menus.key_entry.info"));
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                m_awaitingPress = false;
                if (e.Result >= 0 && dialog.Result.HasValue)
                {
                    m_game.User.Settings.SetKeyBind(m_binding, dialog.Result.Value);
                    m_game.User.Settings.Save();
                }
                m_menu.RefreshOptions();
            };
            m_menu.ShowDialog(dialog);
        }
    }
}
