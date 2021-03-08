using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Main;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.Game
{
    public class LoadState : State
    {
        private Image m_backdrop;
        private Image m_widget;

        private enum LoadStage
        {
            Startup = 0,
            LoadingNewAssets,
            UnloadingOldAssets,
            LoadingAtlases,
            LoadingAnims,
            Finalising,
            Animating,
            Finished
        }

        private LoadStage m_stage;
        private Func<State> m_nextStateFunc;
        private AssetLoadTask m_loadTask;
        private float m_timeInStage;

        public LoadState(Game game, Func<State> nextState) : base(game)
        {
            m_stage = LoadStage.Startup;
            m_nextStateFunc = nextState;
            m_loadTask = null;

            m_backdrop = new Image(Texture.Black, Game.Screen.Width, Game.Screen.Height);
            m_backdrop.Anchor = Anchor.CentreMiddle;
            m_backdrop.LocalPosition = new Vector2(-0.5f * m_backdrop.Width, -0.5f * m_backdrop.Height);

            m_widget = new Image(Texture.Get("gui/loading.png", true), GetWidgetQuad(0.0f), 64.0f, 64.0f);
            m_widget.Anchor = Anchor.CentreMiddle;
            m_widget.LocalPosition = new Vector2(-0.5f * m_widget.Width, -0.5f * m_widget.Height);

            m_timeInStage = 0.0f;
        }

        protected override void OnPreInit(State previous, Transition transition)
        {
            if (previous != null)
            {
                EnableGamepad = previous.EnableGamepad;
            }
            Game.Audio.PlayMusic(null, transition != null ? transition.Duration : 0.0f);
        }

        protected override void OnPreUpdate(float dt)
        {
        }

        protected override void OnReveal()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnInit()
        {
            Game.Sky = null;
            Game.Screen.Elements.Add(m_backdrop);
            Game.Screen.Elements.Add(m_widget);
        }

        protected virtual AssetLoadTask StartLoad()
        {
            return Assets.StartLoadAll();
        }

        protected override void OnUpdate(float dt)
        {
            // Advance state
            m_timeInStage += dt;
            switch (m_stage)
            {
                case LoadStage.LoadingNewAssets:
                    {
                        // Start asset load
                        if (m_loadTask == null)
                        {
                            m_loadTask = StartLoad();
                        }

                        // Load some assets
                        m_loadTask.LoadSome(new TimeSpan(250 * TimeSpan.TicksPerMillisecond));
                        var loadProgress = (m_loadTask.Total > 0) ? ((float)m_loadTask.Loaded / (float)m_loadTask.Total) : 1.0f;
                        SetProgress(loadProgress);

                        // Continue
                        if (m_loadTask.Remaining == 0)
                        {
                            if (m_loadTask.Total > 0)
                            {
                                App.Log("Loaded {0} assets", m_loadTask.Total);
                            }
                            NextStage();
                        }
                        break;
                    }
                case LoadStage.UnloadingOldAssets:
                    {
                        // Unload some assets
                        var loaded = Assets.Count;
                        Assets.UnloadUnsourced();
                        if (loaded > Assets.Count)
                        {
                            App.Log("Unloaded {0} assets", loaded - Assets.Count);
                        }
                        NextStage();
                        break;
                    }
                case LoadStage.LoadingAtlases:
                    {
                        TextureAtlas.Reload("models/tiles");
                        NextStage();
                        break;
                    }
                case LoadStage.LoadingAnims:
                    {
                        LuaAnimation.ReloadAll();
                        NextStage();
                        break;
                    }
                case LoadStage.Finalising:
                    {
                        Game.SelectLanguage();
                        GC.Collect();
                        NextStage();
                        break;
                    }
                case LoadStage.Animating:
                    {
                        // Loading is done. Make the robot blink
                        m_widget.Texture = Texture.Get("gui/load_complete.png", true);
                        if (m_timeInStage < 0.4f || m_timeInStage >= 0.55f)
                        {
                            m_widget.Area = new Quad(0.0f, 0.0f, 0.5f, 1.0f);
                        }
                        else
                        {
                            m_widget.Area = new Quad(0.5f, 0.0f, 0.5f, 1.0f);
                        }
                        if (m_timeInStage >= 1.0f)
                        {
                            NextStage();
                        }
                        break;
                    }
                default:
                    {
                        NextStage();
                        break;
                    }
            }
        }

        protected override void OnPopulateCamera(Camera camera)
        {
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_backdrop);
            m_backdrop.Dispose();

            Game.Screen.Elements.Remove(m_widget);
            m_widget.Dispose();
        }

        protected override void OnPostUpdate(float dt)
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

        private void NextStage()
        {
            if (m_stage < LoadStage.Finished)
            {
                m_stage++;
                m_timeInStage = 0.0f;
                if (m_stage == LoadStage.Finished)
                {
                    var nextState = m_nextStateFunc.Invoke();
                    Game.ChangeState(nextState, new BlackTransition(Game, BlackTransitionType.FromBlack, 0.0f));
                }
            }
        }

        private Quad GetWidgetQuad(float progress)
        {
            int frame = (int)(progress * 7.0f);
            int x = frame % 4;
            int y = frame / 4;
            return new Quad(
                x * 0.25f, y * 0.5f, 0.25f, 0.5f
            );
        }

        private void SetProgress(float progress)
        {
            m_widget.Area = GetWidgetQuad(progress);
        }
    }
}
