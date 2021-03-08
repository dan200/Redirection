using Dan200.Game.Options;
using Dan200.Game.User;

namespace Dan200.Game.Game
{
    public class KeyboardOptionsState : RobotOptionsState
    {
        public KeyboardOptionsState(Game game) : base(game, "menus.keyboard_input_options.title")
        {
        }

        protected override IOption[] GetOptions()
        {
            return new IOption[]
            {
                new KeyBindOption( Game, this, Bind.Play ),
                new KeyBindOption( Game, this, Bind.Rewind ),
                new KeyBindOption( Game, this, Bind.FastForward ),
            };
        }

        protected override void GoBack()
        {
            FuzzToState(new InputOptionsState(Game));
        }
    }
}

