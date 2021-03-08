namespace Dan200.Core.Computer.Devices.GPU
{
    public class Palette
    {
        public readonly object Lock;
        private readonly ChangeListener m_changeListener;
        private readonly uint[] Colors;

        public int Size
        {
            get
            {
                return Colors.Length;
            }
        }

        public int Version
        {
            get
            {
                return m_changeListener.Version;
            }
        }

        public uint this[int index]
        {
            get
            {
                return Colors[index];
            }
            set
            {
                Colors[index] = value;
                Change();
            }
        }

        public Palette(uint[] colors)
        {
            Lock = new object();
            m_changeListener = new ChangeListener();
            Colors = colors;
        }

        public Palette Copy()
        {
            uint[] copy = new uint[Colors.Length];
            for (int i = 0; i < copy.Length; ++i)
            {
                copy[i] = Colors[i];
            }
            return new Palette(copy);
        }

        public void Change()
        {
            m_changeListener.Change();
        }
    }
}

