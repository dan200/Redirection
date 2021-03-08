using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Game.GUI;
using Dan200.Game.Level;
using OpenTK;
using System;
using System.IO;
using System.Linq;

namespace Dan200.Game.Game
{
    public class CampaignSelectState : MenuState
    {
        private bool m_editor;
        private Mod m_mod;
        private CampaignList m_campaigns;
        private Text m_footer;

        public CampaignSelectState(Game game, Mod mod = null) : base(game, "menus.campaign_select.title", "levels/empty.level", MenuArrangement.FullScreen)
        {
            m_editor = (mod != null);
            m_mod = mod;
            EnableGamepad = !m_editor;

            m_footer = new ModDownloadStatusText(UIFonts.Smaller, UIColours.Text, TextAlignment.Center);
            m_footer.Anchor = Anchor.BottomMiddle;
            m_footer.LocalPosition = new Vector2(0.0f, -2.0f * UIFonts.Smaller.Height);

            m_campaigns = new CampaignList(Game.Screen.Height * 1.3f, game, m_mod);
            m_campaigns.Anchor = Anchor.CentreMiddle;
            m_campaigns.LocalPosition = new Vector2(-0.5f * m_campaigns.Width, -0.5f * m_campaigns.Height);
            m_campaigns.OnSelection += delegate (object sender, CampaignEventArgs e)
            {
                OpenCampaign(e.Campaign, e.Mod);
            };
            m_campaigns.OnAction += delegate (object sender, CampaignActionEventArgs e)
            {
                switch (e.Action)
                {
                    case CampaignThumbnailAction.Delete:
                        {
                            DeleteCampaign(e.Campaign, e.Mod);
                            break;
                        }
                    case CampaignThumbnailAction.Edit:
                        {
                            ShowGamepadWarningThen(delegate ()
                            {
                                EditMod(e.Mod);
                            });
                            break;
                        }
                    case CampaignThumbnailAction.ShowInWorkshop:
                        {
                            ShowWorkshopInfo(e.Mod);
                            break;
                        }
                }
            };
            m_campaigns.OnBrowseWorkshop += delegate (object sender, EventArgs e)
            {
                if (!m_editor)
                {
                    BrowseWorkshop();
                }
            };
            m_campaigns.OnOpenModsFolder += delegate (object sender, EventArgs e)
            {
                if (!m_editor)
                {
                    OpenModsFolder();
                }
            };
            m_campaigns.OnCreateCampaign += delegate (object sender, EventArgs e)
            {
                if (m_editor)
                {
                    CreateCampaign();
                }
            };
        }

        protected override void OnInit()
        {
            base.OnInit();
            Game.Screen.Elements.Add(m_campaigns);
            Game.Screen.Elements.Add(m_footer);
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_campaigns);
            m_campaigns.Dispose();
            m_campaigns = null;

            Game.Screen.Elements.Remove(m_footer);
            m_footer.Dispose();
            m_footer = null;

            base.OnShutdown();
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            // Refresh GUI if mods change
            if (!m_editor && Mods.Refresh(Game.Network))
            {
                bool needsLoad = false;
                foreach (var mod in Mods.RemovedLoadedMods)
                {
                    Assets.RemoveSource(mod.Assets);
                    mod.Loaded = false;
                    needsLoad = true;
                }
                foreach (var mod in Mods.AllMods)
                {
                    if (mod.AutoLoad && !mod.Loaded)
                    {
                        Assets.AddSource(mod.Assets);
                        mod.Loaded = true;
                        needsLoad = true;
                    }
                }

                if (needsLoad)
                {
                    LoadToState(delegate ()
                   {
                       return new CampaignSelectState(Game);
                   });
                    return;
                }
                else
                {
                    m_campaigns.Refresh();
                }
            }

            // Update GUI
            ShowSelectPrompt = m_campaigns.Highlight >= 0 && (Game.Screen.InputMethod != InputMethod.Mouse);
            ShowAltSelectPrompt = m_campaigns.HighlightedAction != CampaignThumbnailAction.None && (Game.Screen.InputMethod != InputMethod.Mouse);
            switch (m_campaigns.HighlightedAction)
            {
                case CampaignThumbnailAction.Delete:
                    {
                        AltSelectPrompt = "menus.mod_select.delete";
                        break;
                    }
                case CampaignThumbnailAction.Edit:
                    {
                        AltSelectPrompt = "menus.mod_select.edit";
                        break;
                    }
                case CampaignThumbnailAction.ShowInWorkshop:
                    {
                        AltSelectPrompt = "menus.mod_editor.open_workshop";
                        break;
                    }
            }
        }

        protected override void GoBack()
        {
            if (m_editor)
            {
                // Back to the mod editor
                WipeToState(new ModEditorState(Game, m_mod));
            }
            else
            {
                // Back to the start screen
                WipeToState(new MainMenuState(Game));
            }
        }

        private void ShowGamepadWarningThen(Action fnAction)
        {
            if (Game.Screen.InputMethod != InputMethod.Keyboard && Game.Screen.InputMethod != InputMethod.Mouse)
            {
                var warning = DialogBox.CreateQueryBox(
                    Game.Screen,
                    Game.Language.Translate("menus.editor_gamepad_warning.title"),
                    Game.Language.Translate("menus.editor_gamepad_warning.info"),
                    new string[] {
                        Game.Language.Translate( "menus.ok" ),
                        Game.Language.Translate( "menus.cancel" ),
                    },
                    true
                );
                warning.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
                {
                    if (e.Result == 0)
                    {
                        EnableGamepad = false;
                        fnAction.Invoke();
                    }
                };
                ShowDialog(warning);
            }
            else
            {
                fnAction.Invoke();
            }
        }

        private void EditMod(Mod mod)
        {
            Func<State> fnNextState = delegate ()
            {
                return new ModEditorState(Game, mod);
            };
            if (!mod.Loaded)
            {
                Assets.AddSource(mod.Assets);
                mod.Loaded = true;
                LoadToState(fnNextState);
            }
            else
            {
                WipeToState(fnNextState.Invoke());
            }
        }

        private void ShowWorkshopInfo(Mod mod)
        {
            var id = mod.SteamWorkshopID.Value;
            Game.Network.OpenWorkshopItem(id);
        }

        private void BrowseWorkshop()
        {
            Game.Network.OpenWorkshopHub(new string[] { "New Levels" });
        }

        private void OpenModsFolder()
        {
            Mods.InitLocalDirectory();
            Game.Network.OpenFileBrowser(Path.Combine(App.SavePath, "mods"));
        }

        private void CreateCampaign()
        {
            string suggestedTitle = m_mod.Title;
            var count = m_campaigns.Count;
            if (count > 0)
            {
                suggestedTitle = suggestedTitle + " Act " + (count + 1);
            }

            var textEntry = TextEntryDialogBox.Create(Game.Language.Translate("menus.name_campaign_prompt.title"), suggestedTitle, "", Game.Screen.Width - 300.0f, new string[] {
                Game.Language.Translate( "menus.ok" ),
                Game.Language.Translate( "menus.cancel" )
            });
            textEntry.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
            {
                if (e2.Result == 0)
                {
                    // Determine a title
                    var title = (textEntry.EnteredText.Trim().Length > 0) ? textEntry.EnteredText.Trim() : suggestedTitle;
                    var shortTitle = title.ToSafeAssetName(true);

                    // Determine an asset path
                    int i = 2;
                    var assetPath = AssetPath.Combine("campaigns", shortTitle + ".campaign");
                    while (File.Exists(Path.Combine(m_mod.Path, "assets/" + assetPath)))
                    {
                        assetPath = AssetPath.Combine("campaigns", shortTitle + i + ".campaign");
                        ++i;
                    }

                    // Save the file out
                    var fullPath = Path.Combine(m_mod.Path, "assets/" + assetPath);
                    var newCampaign = new Campaign(assetPath);
                    newCampaign.Title = title;
                    newCampaign.Save(fullPath);

                    // Load the new campaign
                    Assets.Reload(assetPath);
                    OpenCampaign(Campaign.Get(assetPath), m_mod);
                }
            };
            ShowDialog(textEntry);
        }

        private void OpenCampaign(Campaign campaign, Mod mod)
        {
            // Set the campaign as played
            if (App.Steam && mod != null && mod.Source == ModSource.Workshop && mod.SteamWorkshopID.HasValue)
            {
                Game.Network.Workshop.SetItemPlayed(mod.SteamWorkshopID.Value);
            }

            // Load the campaign
            bool editor = m_editor;
            Func<State> fnNextState = delegate ()
            {
                // Get the campaign again (it might have only just been loaded)
                campaign = Campaign.Get(campaign.Path);

                // Open the level select screen
                // Choose an appropriate page to open onto
                if (editor)
                {
                    // Open to the last edited level
                    int lastEditedLevelNum = -1;
                    var lastEditedLevel = Game.User.Progress.LastEditedLevel;
                    if (lastEditedLevel != 0)
                    {
                        for (int i = 0; i < campaign.Levels.Count; ++i)
                        {
                            var levelPath = campaign.Levels[i];
                            var levelData = LevelData.Get(levelPath);
                            if (levelData.ID == lastEditedLevel)
                            {
                                lastEditedLevelNum = i;
                                break;
                            }
                        }
                    }

                    int level = (lastEditedLevelNum >= 0) ? lastEditedLevelNum : campaign.Levels.Count;
                    int page = level / LevelSelectState.NUM_PER_PAGE;
                    return new LevelSelectState(Game, mod, campaign, page, -1, true);
                }
                else
                {
                    // Open to the first unplayed level
                    int firstIncompleteLevel = -1;
                    for (int i = 0; i < campaign.Levels.Count; ++i)
                    {
                        var levelPath = campaign.Levels[i];
                        var levelData = LevelData.Get(levelPath);
                        if (!Game.User.Progress.IsLevelCompleted(levelData.ID))
                        {
                            firstIncompleteLevel = i;
                            break;
                        }
                    }
                    if (firstIncompleteLevel == 0 || campaign.Levels.Count == 1)
                    {
                        var introPath = LevelData.Get(campaign.Levels[0]).Intro;
                        if (introPath != null)
                        {
                            return new CutsceneState(Game, mod, introPath, CutsceneContext.LevelIntro, new Playthrough(campaign, 0));
                        }
                        else
                        {
                            return new CampaignState(Game, mod, new Playthrough(campaign, 0));
                        }
                    }
                    else
                    {
                        int level = (firstIncompleteLevel >= 0) ? firstIncompleteLevel : 0;
                        int page = level / LevelSelectState.NUM_PER_PAGE;
                        return new LevelSelectState(Game, mod, campaign, page, -1, false);
                    }
                }
            };

            if (mod != null && !mod.Loaded)
            {
                Assets.AddSource(mod.Assets);
                mod.Loaded = true;
                LoadToState(fnNextState);
            }
            else
            {
                WipeToState(fnNextState.Invoke());
            }
        }

        private void DeleteCampaign(Campaign campaign, Mod mod)
        {
            var dialog = DialogBox.CreateQueryBox(
                Game.Screen,
                Game.Language.Translate("menus.delete_mod_prompt.title"),
                Game.Language.Translate("menus.delete_mod_prompt.info", campaign.Title),
                new string[] {
                    Game.Language.Translate( "menus.yes" ),
                    Game.Language.Translate( "menus.no" ),
                },
                true
            );
            dialog.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // YES
                            // Delete the campaign
                            var assetPath = campaign.Path;
                            var fullPath = Path.Combine(mod.Path, "assets/" + assetPath);
                            File.Delete(fullPath);

                            // Delete the levels and their thumbnails
                            for (int i = 0; i < campaign.Levels.Count; ++i)
                            {
                                var levelPath = campaign.Levels[i];
                                var fullLevelPath = Path.Combine(mod.Path, "assets/" + levelPath);
                                if (File.Exists(fullLevelPath))
                                {
                                    File.Delete(fullLevelPath);
                                }

                                var thumbnailPath = AssetPath.ChangeExtension(levelPath, "png");
                                var fullThumbnailPath = AssetPath.Combine(mod.Path, "assets/" + thumbnailPath);
                                if (File.Exists(fullThumbnailPath))
                                {
                                    File.Delete(fullThumbnailPath);
                                }
                            }

                            // Unload the campaign
                            Assets.Reload(assetPath);
                            var sources = Assets.GetSources(assetPath);
                            if (sources.Count == 0 || (sources.Count == 1 && sources.First() == m_mod.Assets))
                            {
                                Assets.Unload(assetPath);
                            }
                            m_campaigns.Refresh();
                            break;
                        }
                }
            };
            ShowDialog(dialog);
        }
    }
}

