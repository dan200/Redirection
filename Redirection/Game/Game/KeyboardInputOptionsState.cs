using Dan200.Game.Options;
using Dan200.Game.User;

namespace Dan200.Game.Game
{
    public class KeyboardInputOptionsState : OptionsState
    {
        public KeyboardInputOptionsState(Game game) : base(game, "menus.keyboard_input_options.title", "levels/options.level")
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
            SlideToState(new InputOptionsState(Game), SlideDirection.Left);
        }
    }
}

