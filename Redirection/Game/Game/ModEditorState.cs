using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Core.Script;
using Dan200.Game.Arcade;
using Dan200.Game.Level;
using Dan200.Game.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Game.Game
{
    public class ModEditorState : OptionsState
    {
        private Mod m_mod;

        public ModEditorState(Game game, Mod mod) : base(game, "menus.mod_editor.title", "levels/empty.level", MenuArrangement.FullScreen)
        {
            m_mod = mod;
            Title = MouseButton.Left.GetPrompt() + " " + m_mod.Title;
            TitleClickable = true;
            EnableGamepad = false;
        }

        protected override IOption[] GetOptions()
        {
            var options = new List<IOption>();
            options.Add(new ActionOption("menus.mod_editor.edit_levels", EditLevels));
            options.Add(new ActionOption("menus.mod_editor.open_mod_folder", OpenModFolder));
            if (Game.Network.SupportsWorkshop)
            {
                if (m_mod.SteamWorkshopID.HasValue)
                {
                    options.Add(new ActionOption("menus.mod_editor.open_workshop", ShowInWorkshop));
                }
                options.Add(new ActionOption("menus.mod_editor.publish_mod", PublishMod));
            }
            else
            {
                options.Add(new ActionOption("menus.mod_editor.export_mod", ExportMod));
            }
            options.Add(new ActionOption("menus.mod_editor.reload_mod", ReloadMod));
            options.Add(new ActionOption("menus.mod_editor.delete_mod", DeleteMod));
            return options.ToArray();
        }

        protected override void OnTitleClicked()
        {
            RenameMod();
        }

        private void EditLevels()
        {
            if (Game.Keyboard.Keys[Key.LeftShift].Held || Game.Keyboard.Keys[Key.RightShift].Held)
            {
                CampaignSelect();
            }
            else
            {
                var campaigns = Assets.List<Campaign>("campaigns", m_mod.Assets).ToArray();
                if (campaigns.Length == 0)
                {
                    var campaign = CreateCampaign();
                    LevelSelect(campaign);
                }
                else if (campaigns.Length == 1)
                {
                    var campaign = campaigns[0];
                    LevelSelect(campaign);
                }
                else
                {
                    CampaignSelect();
                }
            }
        }

        private Campaign CreateCampaign()
        {
            // Determine a title
            var title = m_mod.Title;
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
            return Campaign.Get(assetPath);
        }

        private void CampaignSelect()
        {
            WipeToState(new CampaignSelectState(Game, m_mod));
        }

        private void LevelSelect(Campaign campaign)
        {
            int lastEditedLevelNum = campaign.Levels.Count;
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
            int page = lastEditedLevelNum / LevelSelectState.NUM_PER_PAGE;
            WipeToState(new LevelSelectState(Game, m_mod, campaign, page, -1, true));
        }

        private void OpenModFolder()
        {
            Mods.InitEditorDirectory();
            Game.Network.OpenFileBrowser(m_mod.Path);
        }

        private void ShowInWorkshop()
        {
            if (m_mod.SteamWorkshopID.HasValue)
            {
                Game.Network.OpenWorkshopItem(m_mod.SteamWorkshopID.Value);
            }
        }

        private void ExportMod()
        {
            // Update game_version
            m_mod.MinimumGameVersion = App.Info.Version;
            m_mod.SaveInfo();

            // Export the mod
            var modsPath = Path.Combine(App.SavePath, "mods");
            var outputPath = Path.Combine(modsPath, Path.GetFileName(m_mod.Path) + m_mod.Version.ToString() + ".zip");
            Mods.InitLocalDirectory();
            Mods.Export(m_mod, outputPath);

            // Inform the user
            var dialog = DialogBox.CreateQueryBox(
                Game.Screen,
                Game.Language.Translate("menus.mod_editor.export_mod"),
                Game.Language.Translate("menus.mod_exported.title", m_mod.Title),
                new string[] {
                    Game.Language.Translate( "menus.ok" )
                },
                false
            );
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // Open the export folder
                            Game.Network.OpenFileBrowser(Path.GetDirectoryName(outputPath));
                            break;
                        }
                }
            };
            ShowDialog(dialog);
        }

        private void PublishMod()
        {
            if (m_mod.SteamWorkshopID.HasValue)
            {
                // Skip confirm when updating existing mod
                ReallyPublishMod();
            }
            else
            {
                // Ask the user to confirm first
                var dialog = DialogBox.CreateQueryBox(
                    Game.Screen,
                    Game.Language.Translate("menus.reset_progress_prompt.title"),
                    Game.Language.Translate("menus.publish_mod_prompt.info", m_mod.Title),
                    new string[]
                    {
                        Game.Language.Translate("menus.yes"),
                        Game.Language.Translate("menus.no")
                    },
                    true
                );
                dialog.OnClosed += delegate (object o, DialogBoxClosedEventArgs args)
                {
                    if (args.Result == 0)
                    {
                        // YES
                        ReallyPublishMod();
                    }
                };
                ShowDialog(dialog);
            }
        }

        private string GenerateThumbnail(List<string> sourceImages)
        {
            // See how many will fit on the thumbanil
            int gridSize = 0;
            for (int i = 3; i >= 0; --i)
            {
                if (sourceImages.Count >= i * i)
                {
                    gridSize = i;
                    break;
                }
            }
            if (gridSize > 0)
            {
                // Compose
                var dst = new Bitmap(ThumbnailState.THUMBNAIL_WIDTH, ThumbnailState.THUMBNAIL_HEIGHT);
                var itemWidth = dst.Width / gridSize;
                var itemHeight = dst.Height / gridSize;
                for (int y = 0; y < gridSize; ++y)
                {
                    for (int x = 0; x < gridSize; ++x)
                    {
                        var i = x + y * gridSize;
                        using (var src = new Bitmap(sourceImages[i]))
                        {
                            if (src.Width == itemWidth && src.Height == itemHeight)
                            {
                                dst.Blit(src, x * itemWidth, y * itemHeight);
                            }
                            else
                            {
                                using (var resized = src.Resize(itemWidth, itemHeight, true, true))
                                {
                                    dst.Blit(resized, x * itemWidth, y * itemHeight);
                                }
                            }
                        }
                    }
                }

                // Save
                var tempPath = Path.Combine(App.SavePath, "editor/temp/autothumb.png");
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
                dst.Save(tempPath);
                return tempPath;
            }
            return null;
        }

        private void ReallyPublishMod()
        {
            // Update game_version
            m_mod.MinimumGameVersion = App.Info.Version;
            m_mod.SaveInfo();

            // Check every level has a thumbnail and has been completed
            foreach (var campaign in Assets.Find<Campaign>("campaigns", m_mod.Assets))
            {
                for (int i = 0; i < campaign.Levels.Count; ++i)
                {
                    var levelPath = campaign.Levels[i];
                    var levelData = LevelData.Get(levelPath);
                    if (!levelData.EverCompleted)
                    {
                        ShowDialog(DialogBox.CreateQueryBox(
                            Game.Screen,
                            Game.Language.Translate("menus.publishing_error.title"),
                            Game.Language.Translate("menus.publishing_error.level_not_completed", TranslateTitle(levelData.Title)),
                            new string[] {
                            Game.Language.Translate( "menus.ok" )
                            }, true)
                        );
                        return;
                    }

                    var levelThumbnailPath = AssetPath.ChangeExtension(levelPath, "png");
                    var levelThumbnailFullPath = Path.Combine(m_mod.Path, "assets/" + levelThumbnailPath);
                    if (!File.Exists(levelThumbnailFullPath))
                    {
                        ShowDialog(DialogBox.CreateQueryBox(
                            Game.Screen,
                            Game.Language.Translate("menus.publishing_error.title"),
                            Game.Language.Translate("menus.publishing_error.level_no_thumbnail", TranslateTitle(levelData.Title)),
                            new string[] {
                                Game.Language.Translate( "menus.ok" )
                            }, true)
                        );
                        return;
                    }
                }
            }

            // Find source images for the thumbnail
            var thumbnailSources = new List<string>();
            var manualThumbnailPath = Path.Combine(m_mod.Path, "thumbnail.png");
            if (File.Exists(manualThumbnailPath))
            {
                // Use thumbnail.png
                thumbnailSources.Add(manualThumbnailPath);
            }
            else
            {
                // Use the thumbnails from the levels
                var campaigns = Assets.List<Campaign>("campaigns", m_mod.Assets).ToArray();
                for (int i = 0; i < campaigns.Length; ++i)
                {
                    var campaign = campaigns[i];
                    for (int j = 0; j < campaign.Levels.Count; ++j)
                    {
                        var levelPath = campaign.Levels[j];
                        var levelThumbnailPath = AssetPath.ChangeExtension(levelPath, "png");
                        var levelThumbnailFullPath = Path.Combine(m_mod.Path, "assets/" + levelThumbnailPath);
                        if (File.Exists(levelThumbnailFullPath))
                        {
                            thumbnailSources.Add(levelThumbnailFullPath);
                        }
                    }
                }
            }
            if (thumbnailSources.Count == 0)
            {
                ShowDialog(DialogBox.CreateQueryBox(
                    Game.Screen,
                    Game.Language.Translate("menus.publishing_error.title"),
                    Game.Language.Translate("menus.publishing_error.no_thumbnail"),
                    new string[] {
                        Game.Language.Translate( "menus.ok" )
                    }, true)
                );
                return;
            }

            // Generate the thumbnail
            var thumbnailPath = GenerateThumbnail(thumbnailSources);

            // Determine the tags
            var tags = new HashSet<string>();
            if (Assets.Find<LuaScript>("animation", m_mod.Assets).Count() > 0)
            {
                tags.Add("Custom Animation");
            }
            if (Assets.List<Campaign>("campaigns", m_mod.Assets).Count() > 0 &&
                Assets.Find<LevelData>("levels", m_mod.Assets).Count() > 0)
            {
                tags.Add("New Levels");
            }
            if (Assets.List<Language>("languages", m_mod.Assets).Where(lang => !lang.IsEnglish).Count() > 0)
            {
                tags.Add("Localisation");
            }
            if (Assets.Find<Model>("models", m_mod.Assets).Count() > 0 ||
                Assets.Find<Sky>("skies", m_mod.Assets).Count() > 0)
            {
                tags.Add("Custom Art");
            }
            if (Assets.Find<Sound>("sound", m_mod.Assets).Count() > 0 ||
                Assets.Find<Sound>("music", m_mod.Assets).Count() > 0)
            {
                tags.Add("Custom Audio");
            }
            if (Assets.List<ArcadeDisk>("arcade", m_mod.Assets).Count() > 0)
            {
                tags.Add("Arcade Games");
            }

            // Publish
            if (m_mod.SteamWorkshopID.HasValue)
            {
                // Update existing mod
                ulong id = m_mod.SteamWorkshopID.Value;
                var promise = Game.Network.Workshop.UpdateItem(
                    id,
                    "Modified with the Redirection Mod Editor",
                    filePath: m_mod.Path,
                    previewImagePath: thumbnailPath,
                    title: m_mod.Title,
                    tags: tags.ToArray()
                );
                var progressDialog = PromiseDialogBox.Create(
                    Game.Language.Translate("menus.publishing.title"),
                    promise
                );
                progressDialog.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
                {
                    if (promise.Status == Status.Complete)
                    {
                        // Show the user the mod
                        ShowInWorkshop();

                        // Show a dialog
                        var completeDialog = DialogBox.CreateQueryBox(
                            Game.Screen,
                            Game.Language.Translate("menus.mod_published.title"),
                            promise.Result.AgreementNeeded ?
                                Game.Language.Translate("menus.mod_published.info_agreement_needed", m_mod.Title) :
                                Game.Language.Translate("menus.mod_published.info", m_mod.Title),
                            new string[] {
                                Game.Language.Translate( "menus.ok" )
                            },
                            true
                        );
                        ShowDialog(completeDialog);
                    }
                    else if (promise.Status == Status.Error)
                    {
                        // Show the user the error
                        var errorDialog = DialogBox.CreateQueryBox(
                            Game.Screen,
                            Game.Language.Translate("menus.publishing_error.title"),
                            promise.Error,
                            new string[] {
                                Game.Language.Translate( "menus.ok" )
                            },
                            true
                        );
                        ShowDialog(errorDialog);
                    }
                };
                ShowDialog(progressDialog);
            }
            else
            {
                // Create new mod
                var description = "Created with the Redirection Mod Editor";
                var promise = Game.Network.Workshop.CreateItem(
                    m_mod.Path,
                    thumbnailPath,
                    m_mod.Title,
                    description,
                    tags.ToArray(),
                    true
                );
                var progressDialog = PromiseDialogBox.Create(
                    Game.Language.Translate("menus.publishing.title"),
                    promise
                );
                progressDialog.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
                {
                    if (promise.Status == Status.Complete)
                    {
                        // Save the mod
                        ulong id = promise.Result.ID;
                        m_mod.SteamWorkshopID = id;
                        m_mod.SaveInfo();

                        // Show the user the mod
                        ShowInWorkshop();

                        // Show a dialog
                        var completeDialog = DialogBox.CreateQueryBox(
                            Game.Screen,
                            Game.Language.Translate("menus.mod_published.title"),
                            promise.Result.AgreementNeeded ?
                                Game.Language.Translate("menus.mod_published.info_agreement_needed", m_mod.Title) :
                                Game.Language.Translate("menus.mod_published.info", m_mod.Title),
                            new string[] {
                                Game.Language.Translate( "menus.ok" )
                            },
                            true
                        );
                        completeDialog.OnClosed += delegate
                        {
                            // Re-enter state so the new "show in workshop" entry appears
                            CutToState(new ModEditorState(Game, m_mod));
                        };
                        ShowDialog(completeDialog);
                    }
                    else if (promise.Status == Status.Error)
                    {
                        // Show the user the error
                        var errorDialog = DialogBox.CreateQueryBox(
                            Game.Screen,
                            Game.Language.Translate("menus.publishing_error.title"),
                            promise.Error,
                            new string[] {
                                Game.Language.Translate( "menus.ok" )
                            },
                            true
                        );
                        ShowDialog(errorDialog);
                    }
                };
                ShowDialog(progressDialog);
            }
        }

        private void ReallyDeleteMod()
        {
            Mods.Delete(m_mod);
            m_mod.AutoLoad = false;
            GoBack();
        }

        private void ReallyUnpublishThenDeleteMod()
        {
            var promise = Game.Network.Workshop.UpdateItem(
                m_mod.SteamWorkshopID.Value,
                "Deleted",
                title: m_mod.Title + " (Deleted in Editor)",
                visibility: false
            );
            var dialog = PromiseDialogBox.Create(Game.Language.Translate("menus.unpublishing.title"), promise);
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                if (promise.Status == Status.Complete)
                {
                    // Continue to delete the mod
                    ReallyDeleteMod();
                }
                else if (promise.Status == Status.Error)
                {
                    // Show the user the error
                    var errorDialog = DialogBox.CreateQueryBox(
                        Game.Screen,
                        Game.Language.Translate("menus.unpublishing_error.title"),
                        promise.Error,
                        new string[] {
                            Game.Language.Translate( "menus.ok" )
                        },
                        true
                    );
                    ShowDialog(errorDialog);
                }
            };
            ShowDialog(dialog);
        }

        private void DeleteMod()
        {
            var dialog = DialogBox.CreateQueryBox(
                Game.Screen,
                Game.Language.Translate("menus.delete_mod_prompt.title"),
                Game.Language.Translate("menus.delete_mod_prompt.info", m_mod.Title),
                new string[] {
                    Game.Language.Translate( "menus.yes" ),
                    Game.Language.Translate( "menus.no" ),
                },
                true
            );
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // YES
                            if (Game.Network.SupportsWorkshop && m_mod.SteamWorkshopID.HasValue)
                            {
                                var id = m_mod.SteamWorkshopID.Value;
                                var dialog2 = DialogBox.CreateQueryBox(
                                    Game.Screen,
                                    Game.Language.Translate("menus.unpublish_mod_prompt.title"),
                                    Game.Language.Translate("menus.unpublish_mod_prompt.info", m_mod.Title),
                                    new string[] {
                                    Game.Language.Translate( "menus.yes" ),
                                    Game.Language.Translate( "menus.no" ),
                                    Game.Language.Translate( "menus.cancel" ),
                                    },
                                    false
                                );
                                dialog2.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
                                {
                                    switch (e2.Result)
                                    {
                                        case 0:
                                            {
                                                // YES
                                                ReallyUnpublishThenDeleteMod();
                                                return;
                                            }
                                        case 1:
                                            {
                                                // NO
                                                ReallyDeleteMod();
                                                return;
                                            }
                                    }
                                };
                                ShowDialog(dialog2);
                            }
                            else
                            {
                                // Delete the mod
                                ReallyDeleteMod();
                            }
                            break;
                        }
                }
            };
            ShowDialog(dialog);
        }

        private void ReloadMod()
        {
            var mod = m_mod;
            mod.ReloadInfo();
            BlackoutToState(new ReloadSourceState(
                Game,
                delegate ()
                {
                    return new ModEditorState(Game, mod);
                },
                mod.Assets
            ));
        }

        private void RenameMod()
        {
            var textEntry = TextEntryDialogBox.Create(Game.Language.Translate("menus.name_mod_prompt.title"), m_mod.Title, "", Game.Screen.Width - 300.0f, new string[] {
                Game.Language.Translate( "menus.ok" ),
                Game.Language.Translate( "menus.cancel" )
            });
            textEntry.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
            {
                if (e2.Result == 0)
                {
                    // Get the title
                    var newTitle = (textEntry.EnteredText.Trim().Length > 0) ? textEntry.EnteredText.Trim() : "Untitled Mod";

                    // Save the title
                    var oldtitle = m_mod.Title;
                    m_mod.Title = newTitle;
                    m_mod.SaveInfo();

                    // Rename any campaigns that share the mod name
                    foreach (var campaign in Assets.List<Campaign>("campaigns", m_mod.Assets))
                    {
                        if (campaign.Title == oldtitle)
                        {
                            var fullPath = Path.Combine(m_mod.Path, "assets/" + campaign.Path);
                            var newCampaign = campaign.Copy();
                            newCampaign.Title = newTitle;
                            newCampaign.Save(fullPath);
                            Assets.Reload(campaign.Path);
                        }
                    }

                    // Update the title
                    Title = MouseButton.Left.GetPrompt() + " " + m_mod.Title;
                }
            };
            ShowDialog(textEntry);
        }

        public override void OnReloadAssets()
        {
            base.OnReloadAssets();
            Title = MouseButton.Left.GetPrompt() + " " + m_mod.Title;
        }

        protected override void GoBack()
        {
            // Go back to the startup screen
            Func<State> fnNextState = delegate ()
            {
                return new MainMenuState(Game);
            };
            if (!m_mod.AutoLoad)
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
