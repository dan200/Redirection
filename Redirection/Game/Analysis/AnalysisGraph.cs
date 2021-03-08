using Dan200.Core.GUI;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Analysis
{
    public class AnalysisGraph : Element
    {
        private Geometry m_geometry;
        private float m_width;
        private float m_height;
        private CampaignAnalysis m_analysis;

        public float Width
        {
            get
            {
                return m_width;
            }
            set
            {
                m_width = value;
                RequestRebuild();
            }
        }

        public float Height
        {
            get
            {
                return m_height;
            }
            set
            {
                m_height = value;
                RequestRebuild();
            }
        }

        public AnalysisGraph(float width, float height, CampaignAnalysis analysis)
        {
            m_width = width;
            m_height = height;
            m_analysis = analysis;
            m_geometry = new Geometry(Primitive.Lines);
        }

        protected override void OnInit()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild()
        {
            var position = Position;
            m_geometry.Clear();

            // Determine the Y scale
            var maxPlaytime = 1.0f;
            foreach (var analysis in m_analysis.Levels)
            {
                if (analysis.MaxPlaytime > maxPlaytime)
                {
                    maxPlaytime = analysis.MaxPlaytime;
                }
            }

            // Add the Y scale lines
            for (float time = 0.0f; time <= maxPlaytime; time += 60.0f)
            {
                float h = time / maxPlaytime;
                m_geometry.Add2DLine(
                    position + new Vector2(0.0f, (1.0f - h) * Height),
                    position + new Vector2(Width, (1.0f - h) * Height),
                    new Vector4(0.2f, 0.2f, 0.2f, 1.0f)
                );
            }

            // Add the values
            float lastCompletionHeight = 0.0f;
            float lastPlaytimeHeight = 0.0f;
            float lastX = 0.0f;

            for (int i = 0; i < m_analysis.Levels.Length; ++i)
            {
                var analysis = m_analysis.Levels[i];
                var x = ((float)i / (float)m_analysis.Levels.Length) * Width;

                m_geometry.Add2DLine(
                    position + new Vector2(x, 0.0f),
                    position + new Vector2(x, Height),
                    new Vector4(0.2f, 0.2f, 0.2f, 1.0f)
                );

                var competionHeight = analysis.PercentCompleted / 100.0f;
                if (i > 0)
                {
                    m_geometry.Add2DLine(
                        position + new Vector2(lastX, (1.0f - lastCompletionHeight) * Height),
                        position + new Vector2(x, (1.0f - competionHeight) * Height),
                        UIColours.Blue
                    );
                }
                lastCompletionHeight = competionHeight;

                var playtimeHeight = analysis.MedianPlaytime / maxPlaytime;
                if (i > 0)
                {
                    m_geometry.Add2DLine(
                        position + new Vector2(lastX, (1.0f - lastPlaytimeHeight) * Height),
                        position + new Vector2(x, (1.0f - playtimeHeight) * Height),
                        UIColours.Red
                    );
                }
                lastPlaytimeHeight = playtimeHeight;

                lastX = x;
            }

            // Add the axes
            m_geometry.Add2DLine(position, position + new Vector2(0.0f, Height), UIColours.White);
            m_geometry.Add2DLine(position + new Vector2(0.0f, Height), position + new Vector2(Width, Height), UIColours.White);

            m_geometry.Rebuild();
        }

        protected override void OnDraw()
        {
            // Draw the graph
            Screen.Effect.Texture = Texture.White;
            Screen.Effect.Colour = UIColours.White;
            Screen.Effect.Bind();
            m_geometry.Draw();
        }
    }
}

