namespace Dan200.Core.Computer.Devices.Speaker
{
    public enum Waveform
    {
        Square,
        Triangle,
        Sawtooth,
        Noise
    }

    public class Sound
    {
        public Waveform Waveform = Waveform.Square;
        public float Volume = 1.0f;
        public float Duty = 0.5f;

        public float Attack = 0.0f;
        public float Duration = 1.0f;
        public float Decay = 0.0f;

        public float Frequency = 440.0f;
        public float Slide = 0.0f;

        public float VibratoFrequency = 0.0f;
        public float VibratoDepth = 0.0f;

        public bool Loop = false;

        public Sound()
        {
        }
    }
}

