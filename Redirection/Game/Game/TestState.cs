using Dan200.Core.Input;
using Dan200.Core.Modding;
using Dan200.Game.GUI;
using Dan200.Game.User;

namespace Dan200.Game.Game
{
    public class TestState : InGameState
    {
        private Mod m_mod;
        private Campaign m_campaign;
        private int m_levelIndex;
        private string m_levelSavePath;
        private bool m_completed;

        public string LevelSavePath
        {
            get
            {
                return m_levelSavePath;
            }
        }

        public Campaign Campaign
        {
            get
            {
                return m_campaign;
            }
        }

        public Mod Mod
        {
            get
            {
                return m_mod;
            }
        }

        public int LevelIndex
        {
            get
            {
                return m_levelIndex;
            }
        }

        public TestState(Game game, Mod mod, Campaign campaign, int levelIndex, string levelLoadPath, string levelSavePath) : base(game, levelLoadPath)
        {
            m_mod = mod;
            m_campaign = campaign;
            m_levelIndex = levelIndex;
            m_levelSavePath = levelSavePath;
            m_completed = false;
        }

        protected override void Reset()
        {
            CutToState(new TestState(Game, m_mod, m_campaign, m_levelIndex, LevelLoadPath, LevelSavePath));
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (Game.Keyboard.Keys[Key.E].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                ReturnToEditor();
            }
        }

        protected override void OnMenuRequested()
        {
            // Go back to the editor
            ReturnToEditor();
        }

        protected override void OnOutroStarted(int robotsSaved, LevelCompleteDetails o_completeDetails)
        {
            // Unlock achievements
            m_completed = true;
            if (m_mod != null && m_mod.Source == ModSource.Editor)
            {
                Game.User.Progress.UnlockAchievement(Achievement.CreateLevel);
                Game.User.Progress.Save();
            }
        }

        protected override void OnOutroComplete()
        {
            // Go back to the editor
            ReturnToEditor();
        }

        private void ReturnToEditor()
        {
            var newState = new EditorState(Game, m_mod, m_campaign, m_levelIndex, LevelLoadPath, LevelSavePath);
            if (m_completed)
            {
                newState.MarkLevelCompleted();
            }
            CutToState(newState);
        }
    }
}

