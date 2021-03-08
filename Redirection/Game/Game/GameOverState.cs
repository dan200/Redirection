using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Network;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Game
{
    public class GameOverState : MenuState
    {
        private Mod m_mod;
        private Playthrough m_playthrough;
        private Text[] m_text;
        private TextMenu m_menu;

        public GameOverState(Game game, Mod mod, Playthrough playthrough) : base(game, "menus.game_over.title", "levels/empty.level", MenuArrangement.FullScreen)
        {
            BackPrompt = "menus.continue";

            m_mod = mod;
            m_playthrough = playthrough;

            var textItems = new List<string>();
            textItems.Add(Game.Language.Translate("menus.game_over.info", playthrough.Campaign.Title));
			if (App.Demo)
			{
				textItems.Add(Game.Language.Translate("credits.thankyou"));
			}

            var menuItems = new List<string>();
            var menuActions = new List<Action>();
            if (!App.Demo)
            {
                if (m_mod != null && Game.Network.SupportsWorkshop && m_mod.SteamWorkshopID.HasValue)
                {
                    menuItems.Add(Game.Language.Translate("menus.game_over.rate_mod", m_mod.Title));
                    menuActions.Add(RateMod);
                }
                menuItems.Add(Game.Language.Translate("menus.game_over.tweet"));
                menuActions.Add(Tweet);
                if (Game.Network.SupportsWorkshop)
                {
                    if (App.Steam)
                    {
                        menuItems.Add(Game.Language.Translate("menus.campaign_select.open_steam_workshop"));
                    }
                    else
                    {
                        menuItems.Add(Game.Language.Translate("menus.campaign_select.open_workshop"));
                    }
                    menuActions.Add(BrowseWorkshop);
                }
            }

            float yPos = -0.5f * (float)(textItems.Count + menuItems.Count) * UIFonts.Default.Height;
            m_text = new Text[textItems.Count];
            for (int i = 0; i < m_text.Length; ++i)
            {
                var text = new Text(
                    UIFonts.Default,
                    textItems[i],
                    UIColours.Text,
                    TextAlignment.Center
                );
                text.Anchor = Anchor.CentreMiddle;
                text.LocalPosition = new Vector2(0.0f, yPos);
                m_text[i] = text;
                yPos += text.Font.Height;
            }

            m_menu = new TextMenu(UIFonts.Default, menuItems.ToArray(), TextAlignment.Center, MenuDirection.Vertical);
            m_menu.Anchor = Anchor.CentreMiddle;
            m_menu.LocalPosition = new Vector2(0.0f, yPos);
            m_menu.TextColour = UIColours.Link;
            m_menu.OnClicked += delegate (object sender, TextMenuClickedEventArgs e)
            {
                if (e.Index >= 0 && e.Index < menuActions.Count)
                {
                    menuActions[e.Index].Invoke();
                }
            };
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Create GUI elements
            for (int i = 0; i < m_text.Length; ++i)
            {
                var text = m_text[i];
                Game.Screen.Elements.Add(text);
            }
            Game.Screen.Elements.Add(m_menu);

            // Set the mod as completed
            if (App.Steam && m_mod != null && m_mod.Source == ModSource.Workshop && m_mod.SteamWorkshopID.HasValue)
            {
                Game.Network.Workshop.SetItemCompleted(m_mod.SteamWorkshopID.Value);
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            ShowSelectPrompt = Game.Screen.InputMethod != InputMethod.Mouse && m_menu.Focus >= 0;
        }

        protected override void OnShutdown()
        {
            for (int i = 0; i < m_text.Length; ++i)
            {
                var text = m_text[i];
                Game.Screen.Elements.Remove(text);
                text.Dispose();
            }
            Game.Screen.Elements.Remove(m_menu);
            m_menu.Dispose();
            base.OnShutdown();
        }

        private void Tweet()
        {
            string tweet = null;
            if (m_mod != null)
            {
                var simpleTemplate = "I just completed the workshop campaign \"{0}\" in @RedirectionGame! Visit http://www.redirectiongame.com to find out more!";
                tweet = string.Format(simpleTemplate, m_playthrough.Campaign.Title);
            }
            else
            {
                tweet = string.Format("I just completed @RedirectionGame! Visit http://www.redirectiongame.com to find out more!");
            }
            Game.Network.OpenComposeTweet(tweet);
        }

        private void RateMod()
        {
            Game.Network.OpenWorkshopItem(m_mod.SteamWorkshopID.Value);
        }

        private void BrowseWorkshop()
        {
            Game.Network.OpenWorkshopHub(new string[] { "New Levels" });
        }

        protected override void GoBack()
        {
            // Go back to campaign select
            Func<State> fnNextState = delegate ()
            {
                if (App.Demo)
                {
                    return new StartScreenState(Game);
                }
                else
                {
                    return new CampaignSelectState(Game);
                }
            };
            if (m_mod != null && !m_mod.AutoLoad)
            {
                Assets.RemoveSource(m_mod.Assets);
                m_mod.Loaded = false;
                LoadToState(fnNextState);
            }
            else
            {
                WipeToState(fnNextState.Invoke());
            }
        }
    }
}

