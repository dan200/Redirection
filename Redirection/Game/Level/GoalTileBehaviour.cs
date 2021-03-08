using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "goal")]
    public class GoalTileBehaviour : TileBehaviour
    {
        private string m_colour;

        public string Colour
        {
            get
            {
                return m_colour;
            }
        }

        public GoalTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_colour = kvp.GetString("colour", "red");
        }

        public void OnOccupy(ILevel level, TileCoordinates coordinates, Robot.Robot robot)
        {
            Tile.SetSubState(level, coordinates, 1, true);
        }
    }
}
