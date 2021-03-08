using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Game.Game;
using Dan200.Game.User;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Analysis
{
    public class AnalysisState : State
    {
        private CampaignAnalysis m_analysis;
        private AnalysisGraph m_graph;

        private static Progress[] GetAllProgressFiles(Game.Game game)
        {
            var network = game.Network;
            var results = new List<Progress>();
            foreach (var fileName in network.LocalUser.RemoteSaveStore.ListFiles(""))
            {
                if (AssetPath.GetExtension(fileName) == "txt" &&
                    AssetPath.GetFileNameWithoutExtension(fileName).StartsWith("progress_", StringComparison.InvariantCulture))
                {
                    App.Log("Found progress file: {0}", fileName);
                    results.Add(new Progress(network, fileName));
                }
            }
            return results.ToArray();
        }

        public AnalysisState(Game.Game game) : base(game)
        {
            m_analysis = CampaignAnalysis.Analyse(
                Campaign.Get("campaigns/main.campaign"),
                GetAllProgressFiles(game)
            );

            m_graph = new AnalysisGraph(Game.Screen.Width - 64.0f, Game.Screen.Height - 64.0f, m_analysis);
            m_graph.Anchor = Anchor.CentreMiddle;
            m_graph.LocalPosition = new Vector2(-0.5f * m_graph.Width, -0.5f * m_graph.Height);
        }

        protected override void OnPreInit(State previous, Transition transition)
        {
        }

        protected override void OnReveal()
        {
        }

        protected override void OnInit()
        {
            Game.Screen.Elements.Add(m_graph);
        }

        protected override void OnPreUpdate(float dt)
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnPopulateCamera(Camera camera)
        {
        }

        protected override void OnPostUpdate(float dt)
        {
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_graph);
            m_graph.Dispose();
        }

        protected override void OnHide()
        {
        }

        protected override void OnPostShutdown()
        {
        }

        protected override void OnPreDraw()
        {
        }

        protected override void OnDraw()
        {
        }

        protected override void OnPostDraw()
        {
        }
    }
}

