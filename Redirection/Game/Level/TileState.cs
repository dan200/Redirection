namespace Dan200.Game.Level
{
    public class TileState
    {
        public bool Hidden;
        public FlatDirection Direction;
        public Entity Entity;
        public Entity Occupant;
        public int SubState;

        public TileState()
        {
        }
    }
}

