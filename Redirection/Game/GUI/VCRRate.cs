namespace Dan200.Game.GUI
{
    public enum VCRRate
    {
        Rewind,
        Pause,
        Play,
        FastForward,
    }

    public static partial class VCRRateExtensions
    {
        private static float[] s_speeds = new float[] {
            -5.0f, 0.0f, 1.0f, 5.0f
        };

        public static float ToFloat(this VCRRate rate)
        {
            return s_speeds[(int)rate];
        }
    }
}

