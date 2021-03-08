using Dan200.Core.Main;
using Dan200.Game.Options;
using System.Collections.Generic;

namespace Dan200.Game.Game
{
    public class InputOptionsState : RobotOptionsState
    {
        public InputOptionsState(Game game) : base(game, "menus.input_options.title")
        {
        }

        protected override IOption[] GetOptions()
        {
            var options = new List<IOption>();
            options.Add(new ActionOption("menus.input_options.keyboard", Keyboard));
            options.Add(new ActionOption("menus.input_options.gamepad", Gamepad));
            if (App.Steam)
            {
                options.Add(new ActionOption("menus.input_options.steam_controller", SteamController));
            }
            return options.ToArray();
        }

        private void Keyboard()
        {
            FuzzToState(new KeyboardOptionsState(Game));
        }

        private void Gamepad()
        {
            FuzzToState(new GamepadOptionsState(Game));
        }

        private void SteamController()
        {
            FuzzToState(new SteamControllerOptionsState(Game));
        }

        protected override void GoBack()
        {
            FuzzToState(new MainOptionsState(Game));
        }
    }
}

