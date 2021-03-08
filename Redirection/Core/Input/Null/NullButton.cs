namespace Dan200.Core.Input.Null
{
    public class NullButton : IButton
    {
        public static readonly NullButton Instance = new NullButton();

        public bool Held
        {
            get
            {
                return false;
            }
        }

        public bool Pressed
        {
            get
            {
                return false;
            }
        }

        public bool Released
        {
            get
            {
                return false;
            }
        }

        public bool Repeated
        {
            get { return false; }
        }

        private NullButton()
        {
        }
    }
}

