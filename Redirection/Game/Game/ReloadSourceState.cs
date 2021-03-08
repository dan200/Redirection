using Dan200.Core.Assets;
using System;

namespace Dan200.Game.Game
{
    public class ReloadSourceState : LoadState
    {
        private IAssetSource m_source;

        public ReloadSourceState(Game game, Func<State> nextState, IAssetSource source) : base(game, nextState)
        {
            m_source = source;
        }

        protected override AssetLoadTask StartLoad()
        {
            return Assets.StartReloadSource(m_source);
        }
    }
}
