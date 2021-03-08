using Dan200.Core.Network;

namespace Dan200.Game.User
{
    public class User
    {
        public Settings Settings
        {
            get;
            private set;
        }

        public Progress Progress
        {
            get;
            private set;
        }

        public User(INetwork network)
        {
            Settings = new Settings();
            Progress = new Progress(network, "progress.txt");
        }
    }
}
