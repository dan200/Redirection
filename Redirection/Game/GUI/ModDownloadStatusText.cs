using Dan200.Core.GUI;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.GUI
{
    public class ModDownloadStatusText : Text
    {
        public ModDownloadStatusText(Font font, Vector4 colour, TextAlignment alignment) : base(font, "", colour, alignment)
        {
        }

        protected override void OnInit()
        {
            base.OnInit();
            UpdateText();
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            UpdateText();
        }

        private void UpdateText()
        {
            if (Mods.PendingModCount > 0)
            {
                float progress = Mods.PendingModProgress;
                this.String = Screen.Language.TranslateCount("menus.campaign_select.mods_downloading", Mods.PendingModCount) + " (" + (int)(progress * 100.0f) + "%)"; ;
            }
            else
            {
                this.String = "";
            }
        }
    }
}

