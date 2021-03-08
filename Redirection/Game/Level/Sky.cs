using Dan200.Core.Assets;
using Dan200.Core.Render;
using OpenTK;
using System.IO;

namespace Dan200.Game.Level
{
    public class Sky : IBasicAsset
    {
        public static Sky Get(string path)
        {
            return Assets.Get<Sky>(path);
        }

        public string Path
        {
            get;
            set;
        }

        public Vector3 BackgroundColour
        {
            get;
            set;
        }

        public string BackgroundImage
        {
            get;
            set;
        }

        public Vector3 AmbientColour
        {
            get;
            set;
        }

        public Vector3 LightColour
        {
            get;
            set;
        }

        public Vector3 LightDirection
        {
            get;
            set;
        }

        public Vector3 Light2Colour
        {
            get;
            set;
        }

        public Vector3 Light2Direction
        {
            get;
            set;
        }

        public string ModelPath
        {
            get;
            set;
        }

        public string ForegroundModelPath
        {
            get;
            set;
        }

        public string AnimPath
        {
            get;
            set;
        }

        public RenderPass RenderPass
        {
            get;
            set;
        }

        public RenderPass ForegroundRenderPass
        {
            get;
            set;
        }

        public bool CastShadows
        {
            get;
            set;
        }

        public Sky(string path, IFileStore store)
        {
            Path = path;
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Load(IFileStore store)
        {
            var kvp = new KeyValuePairs();
            using (var reader = store.OpenTextFile(Path))
            {
                kvp.Load(reader);
            }

            BackgroundColour = kvp.GetColour("background_colour", Vector3.Zero);
            BackgroundImage = kvp.GetString("background_image", null);
            AmbientColour = kvp.GetColour("ambient_colour", Vector3.One);
            LightColour = kvp.GetColour("light_colour", Vector3.Zero);
            LightDirection = kvp.GetUnitVector("light_direction", -Vector3.UnitY);
            Light2Colour = kvp.GetColour("light2_colour", Vector3.Zero);
            Light2Direction = kvp.GetUnitVector("light2_direction", -Vector3.UnitY);

            ModelPath = kvp.GetString("model", null);
            ForegroundModelPath = kvp.GetString("foreground_model", null);
            AnimPath = kvp.GetString("animation", null);

            RenderPass = kvp.GetEnum("render_pass", RenderPass.Opaque);
            ForegroundRenderPass = kvp.GetEnum("foreground_render_pass", RenderPass.Opaque);
            CastShadows = kvp.GetBool("cast_shadows", false);
        }

        private void Unload()
        {
        }

        public void Save(TextWriter writer)
        {
            var kvp = new KeyValuePairs();

            kvp.SetColour("background_colour", BackgroundColour);
            kvp.Set("background_image", BackgroundImage);
            kvp.SetColour("ambient_colour", AmbientColour);
            kvp.SetColour("light_colour", LightColour);
            kvp.SetVector("light_direction", LightDirection);
            kvp.SetColour("light2_colour", Light2Colour);
            kvp.SetVector("light2_direction", Light2Direction);

            kvp.Set("model", ModelPath);
            kvp.Set("foreground_model", ModelPath);
            kvp.Set("animation", AnimPath);

            kvp.Set("render_pass", RenderPass);
            kvp.Set("foreground_render_pass", ForegroundRenderPass);
            kvp.Set("cast_shaows", CastShadows);

            kvp.Save(writer);
        }
    }
}
