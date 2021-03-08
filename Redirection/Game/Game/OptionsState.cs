using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Game.Options;
using OpenTK;
using System.Linq;

namespace Dan200.Game.Game
{
    public abstract class OptionsState : MenuState
    {
        private TextMenu m_menu;
        private IOption[] m_options;

        protected OptionsState(Game game, string title, string levelPath, MenuArrangement arrangement) : base(game, title, levelPath, arrangement)
        {
            m_menu = null;
            m_options = null;
        }

        protected abstract IOption[] GetOptions();

        protected override void OnInit()
        {
            base.OnInit();

            // Create options menu (do here rather than constructor so GetOptions() acts on constructed subclass)
            m_options = GetOptions();
            {
                var menuOptions = m_options.Select(
                    (option) => option.ToString(Game.Language)
                ).ToArray();
                m_menu = new TextMenu(UIFonts.Default, menuOptions, TextAlignment.Center, MenuDirection.Vertical);
                m_menu.Anchor = Anchor.CentreMiddle;
                m_menu.LocalPosition = new Vector2(0.0f, -0.5f * m_menu.Height);
                m_menu.OnClicked += delegate (object sender, TextMenuClickedEventArgs args)
                {
                    var index = args.Index;
                    if (Dialog == null && index >= 0 && index < m_options.Length)
                    {
                        m_options[index].Click();
                        m_menu.Options[index] = m_options[index].ToString(Game.Language);
                    }
                };
            }

            // Add options menu
            Game.Screen.Elements.Add(m_menu);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            ShowSelectPrompt = Game.Screen.InputMethod != InputMethod.Mouse && (m_menu.Focus >= 0);
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_menu);
            m_menu.Dispose();
            m_menu = null;

            base.OnShutdown();
        }

        public override void OnReloadAssets()
        {
            base.OnReloadAssets();
            RefreshOptions();
        }

        public void RefreshOptions()
        {
            for (int i = 0; i < m_options.Length; ++i)
            {
                m_menu.Options[i] = m_options[i].ToString(Game.Language);
            }
        }
    }
}
