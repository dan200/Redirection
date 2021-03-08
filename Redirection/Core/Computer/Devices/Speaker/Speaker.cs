using System;

namespace Dan200.Core.Computer.Devices.Speaker
{
    public delegate void SoundCompleteDelegate(int channel);

    public unsafe class Speaker
    {
        private SpeakerChannel[] m_channels;

        public int Channels
        {
            get
            {
                return m_channels.Length;
            }
        }

        public event SoundCompleteDelegate OnSoundComplete;

        public Speaker(int channels)
        {
            m_channels = new SpeakerChannel[channels];
            for (int i = 0; i < m_channels.Length; ++i)
            {
                m_channels[i] = new SpeakerChannel();
            }
        }

        public void Update()
        {
            for (int i = 0; i < m_channels.Length; ++i)
            {
                var ch = m_channels[i];
                var completed = ch.GetSoundsCompleted();
                for (int j = 0; j < completed; ++j)
                {
                    OnSoundComplete(i);
                }
            }
        }

        public int Play(Sound sound)
        {
            for (int i = 0; i < m_channels.Length; ++i)
            {
                var ch = m_channels[i];
                if (ch.PlayIfIdle(sound))
                {
                    return i;
                }
            }
            return -1;
        }

        public int Play(Sound sound, int channel)
        {
            if (channel >= 0 && channel < m_channels.Length)
            {
                var ch = m_channels[channel];
                ch.ForcePlay(sound);
                return channel;
            }
            return -1;
        }

        public ChannelState GetChannelState(int channel, out int o_queuedSounds)
        {
            if (channel >= 0 && channel < m_channels.Length)
            {
                var ch = m_channels[channel];
                if (ch.State == ChannelState.Playing)
                {
                    o_queuedSounds = ch.QueueSize;
                    return ChannelState.Playing;
                }
            }
            o_queuedSounds = 0;
            return ChannelState.Stopped;
        }

        public int Queue(Sound sound, int channel)
        {
            if (channel >= 0 && channel < m_channels.Length)
            {
                var ch = m_channels[channel];
                if (ch.Queue(sound))
                {
                    return channel;
                }
            }
            return -1;
        }

        public void Stop(int channel)
        {
            if (channel >= 0 && channel < m_channels.Length)
            {
                var ch = m_channels[channel];
                ch.Stop();
            }
        }

        public void Stop()
        {
            for (int i = 0; i < m_channels.Length; ++i)
            {
                var ch = m_channels[i];
                ch.Stop();
            }
        }

        private short[] m_tempBuffer;

        private short AddShort(short a, short b)
        {
            int sum = a + b;
            if (sum < short.MinValue)
            {
                return short.MinValue;
            }
            else if (sum > short.MaxValue)
            {
                return short.MaxValue;
            }
            else
            {
                return (short)sum;
            }
        }

        public void Fill(short[] buffer, int start, int samples, int channels, int sampleRate)
        {
            if (m_tempBuffer == null || m_tempBuffer.Length < (samples * channels))
            {
                m_tempBuffer = new short[samples * channels];
            }

            // Fill in the first channel
            int samplesWritten = m_channels[0].Synth(buffer, start, samples, channels, sampleRate);
            if (samplesWritten < samples)
            {
                FillSilence(buffer, start + samplesWritten * channels, samples - samplesWritten, channels, sampleRate);
            }

            // Fill in remaining channels
            for (int i = 1; i < m_channels.Length; ++i)
            {
                var ch = m_channels[i];
                samplesWritten = ch.Synth(m_tempBuffer, 0, samples, channels, sampleRate);
                for (int j = 0; j < samplesWritten * channels; ++j)
                {
                    buffer[start + j] = AddShort(buffer[start + j], m_tempBuffer[j]);
                }
            }
        }

        private void FillSilence(short[] buffer, int start, int samples, int channels, int sampleRate)
        {
            Array.Clear(buffer, start, samples * channels);
        }

        private void CompleteSound(int channel)
        {
            if (OnSoundComplete != null)
            {
                OnSoundComplete.Invoke(channel);
            }
        }
    }
}

