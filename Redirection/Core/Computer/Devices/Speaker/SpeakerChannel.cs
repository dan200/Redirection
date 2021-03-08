using System;
using System.Threading;

namespace Dan200.Core.Computer.Devices.Speaker
{
    public enum ChannelState
    {
        Playing,
        Stopped,
    }

    public unsafe class SpeakerChannel
    {
        private const float MASTER_VOLUME = 0.6f;
        private const int NOISE_BUFFER_SIZE = 4;
        private const float NOISE_BUFFER_SIZE_FLOAT = 4.0f;
        private const int SOUND_QUEUE_SIZE = 32;

        [ThreadStatic]
        private static Random s_noiseSource;

        private class SynthState
        {
            public Sound Sound;
            public int SamplesPlayed;
            public float Frequency;

            public SynthState(Sound sound)
            {
                Sound = sound;
                SamplesPlayed = 0;
                Frequency = sound.Frequency;
            }
        }

        private SynthState m_state;
        private int m_soundsCompleted;

        private Sound[] m_soundQueue;
        private int m_soundQueueStart;
        private int m_soundQueueEnd;

        private short[] m_noiseBuffer;
        private float m_phase;
        private float m_vibPhase;

        public ChannelState State
        {
            get
            {
                if (m_state != null)
                {
                    return ChannelState.Playing;
                }
                else
                {
                    return ChannelState.Stopped;
                }
            }
        }

        public int QueueSize
        {
            get
            {
                return m_soundQueueEnd - m_soundQueueStart;
            }
        }

        public SpeakerChannel()
        {
            m_state = null;
            m_soundsCompleted = 0;

            m_soundQueue = new Sound[SOUND_QUEUE_SIZE];
            m_soundQueueStart = 0;
            m_soundQueueEnd = 0;

            m_noiseBuffer = new short[NOISE_BUFFER_SIZE];
            FillNoiseBuffer(m_noiseBuffer);
            m_phase = 0.0f;
            m_vibPhase = 0.0f;
        }

        public void ForcePlay(Sound sound)
        {
            var newState = new SynthState(sound);
            if (Interlocked.Exchange(ref m_state, newState) != null)
            {
                Interlocked.Increment(ref m_soundsCompleted);
                m_soundQueueEnd = m_soundQueueStart; // Clear queue
            }
        }

        public bool PlayIfIdle(Sound sound)
        {
            var newState = new SynthState(sound);
            if (Interlocked.CompareExchange(ref m_state, newState, null) == null)
            {
                m_soundQueueEnd = m_soundQueueStart; // Clear queue
                return true;
            }
            return false;
        }

        public bool Queue(Sound sound)
        {
            if (PlayIfIdle(sound))
            {
                return true;
            }
            else if ((m_soundQueueEnd - m_soundQueueStart) < SOUND_QUEUE_SIZE)
            {
                m_soundQueue[m_soundQueueEnd % SOUND_QUEUE_SIZE] = sound;
                Interlocked.Increment(ref m_soundQueueEnd); // Enqueue
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref m_state, null) != null)
            {
                Interlocked.Increment(ref m_soundsCompleted);
                m_soundQueueEnd = m_soundQueueStart;
            }
        }

        public int GetSoundsCompleted()
        {
            return Interlocked.Exchange(ref m_soundsCompleted, 0);
        }

        public int Synth(short[] buffer, int start, int samples, int channels, int sampleRate)
        {
            if (channels == 2)
            {
                samples = Synth(buffer, start, samples, channels, sampleRate, 0);
                Copy(buffer, start, samples, channels, sampleRate, 0, 1);
                return samples;
            }
            else
            {
                return Synth(buffer, start, samples, channels, sampleRate, 0);
            }
        }

        private void Copy(short[] buffer, int start, int samples, int channels, int sampleRate, int srcChannel, int dstChannel)
        {
            for (int i = 0; i < samples; ++i)
            {
                buffer[start + i * channels + dstChannel] = buffer[start + i * channels + srcChannel];
            }
        }

        public int Synth(short[] buffer, int start, int samples, int channels, int sampleRate, int channel)
        {
            var state = m_state;
            if (state == null)
            {
                return 0;
            }

            var sound = state.Sound;
            float frequency = ClampFrequency(state.Frequency);
            float slide = sound.Slide;
            float attack = sound.Attack;
            float duration = sound.Duration;
            float decay = sound.Decay;
            float amplitude = sound.Volume * MASTER_VOLUME;
            float vibFrequency = sound.VibratoFrequency;
            float vibDepth = sound.VibratoDepth;
            float duty = sound.Duty;

            float sampleRatef = (float)sampleRate;
            int firstSample = state.SamplesPlayed;

            int end = (int)(duration * (float)sampleRate);
            int outputSamples;
            if (firstSample + samples > end)
            {
                outputSamples = end - firstSample;
            }
            else
            {
                outputSamples = samples;
            }

            // Fill the buffer with the base waveform
            float phase = m_phase;
            float vibPhase = m_vibPhase;
            float vibPhaseIncrement = vibFrequency / sampleRatef;
            switch (sound.Waveform)
            {
                case Waveform.Square:
                default:
                    {
                        for (int i = 0; i < outputSamples; ++i)
                        {
                            buffer[start + i * channels + channel] = SampleSquare(phase, duty);
                            frequency += (slide / sampleRatef);
                            vibPhase = (vibPhase + vibPhaseIncrement) % 1.0f;
                            float vibbedFrequency = ClampFrequency(frequency + vibDepth * SampleSin(vibPhase));
                            phase = (phase + (vibbedFrequency / sampleRatef)) % 1.0f;
                        }
                        break;
                    }
                case Waveform.Triangle:
                    {
                        for (int i = 0; i < outputSamples; ++i)
                        {
                            buffer[start + i * channels + channel] = SampleTriangle(phase);
                            frequency += (slide / sampleRatef);
                            vibPhase = (vibPhase + vibPhaseIncrement) % 1.0f;
                            float vibbedFrequency = ClampFrequency(frequency + vibDepth * SampleSin(vibPhase));
                            phase = (phase + (vibbedFrequency / sampleRatef)) % 1.0f;
                        }
                        break;
                    }
                case Waveform.Sawtooth:
                    {
                        for (int i = 0; i < outputSamples; ++i)
                        {
                            buffer[start + i * channels + channel] = SampleSawtooth(phase);
                            frequency += (slide / sampleRatef);
                            vibPhase = (vibPhase + vibPhaseIncrement) % 1.0f;
                            float vibbedFrequency = ClampFrequency(frequency + vibDepth * SampleSin(vibPhase));
                            phase = (phase + (vibbedFrequency / sampleRatef)) % 1.0f;
                        }
                        break;
                    }
                case Waveform.Noise:
                    {
                        var noiseBuffer = m_noiseBuffer;
                        for (int i = 0; i < outputSamples; ++i)
                        {
                            buffer[start + i * channels + channel] = SampleNoise(phase, noiseBuffer);
                            frequency += (slide / sampleRatef);
                            vibPhase = (vibPhase + vibPhaseIncrement) % 1.0f;
                            float vibbedFrequency = ClampFrequency(frequency + vibDepth * SampleSin(vibPhase));
                            phase = phase + (vibbedFrequency / sampleRatef);
                            if (phase >= 1.0f)
                            {
                                FillNoiseBuffer(noiseBuffer);
                                phase %= 1.0f;
                            }
                        }
                        break;
                    }
            }

            // Shape the buffer to the envelope
            float t = (float)firstSample / sampleRatef;
            float tIncrement = 1.0f / (float)sampleRate;
            for (int i = 0; i < outputSamples; ++i)
            {
                var idx = start + i * channels + channel;
                buffer[idx] = (short)((float)buffer[idx] * amplitude);
                if (t < attack)
                {
                    var f = t / attack;
                    buffer[idx] = (short)((float)buffer[idx] * f);
                }
                if (t > (duration - decay))
                {
                    var f = 1.0f - ((t - (duration - decay)) / decay);
                    buffer[idx] = (short)((float)buffer[idx] * f);
                }
                t += tIncrement;
            }

            // Store state
            state.SamplesPlayed += outputSamples;
            state.Frequency = frequency;
            m_phase = phase;
            m_vibPhase = vibPhase;

            if (outputSamples < samples)
            {
                if (m_soundQueueEnd > m_soundQueueStart)
                {
                    // Play the next sound
                    var newState = new SynthState(m_soundQueue[m_soundQueueStart % SOUND_QUEUE_SIZE]);
                    if (Interlocked.CompareExchange(ref m_state, newState, state) == state)
                    {
                        Interlocked.Increment(ref m_soundsCompleted);
                        Interlocked.Increment(ref m_soundQueueStart);

                        // Play some of the new sound
                        int unwrittenSamples = samples - outputSamples;
                        if (unwrittenSamples > 0)
                        {
                            return outputSamples + Synth(buffer, start + outputSamples * channels, unwrittenSamples, channels, sampleRate, channel);
                        }
                    }
                }
                else if (sound.Loop)
                {
                    // Loop the sound
                    state.SamplesPlayed = 0;
                    int unwrittenSamples = samples - outputSamples;
                    if (unwrittenSamples > 0)
                    {
                        return outputSamples + Synth(buffer, start + outputSamples * channels, unwrittenSamples, channels, sampleRate, channel);
                    }
                }
                else
                {
                    // Finish the sound
                    if (Interlocked.CompareExchange(ref m_state, null, state) == state)
                    {
                        Interlocked.Increment(ref m_soundsCompleted);
                    }
                    m_phase = 0.0f;
                    m_vibPhase = 0.0f;
                }
            }

            return outputSamples;
        }

        private static float ClampFrequency(float f)
        {
            return Math.Min(Math.Max(f, 10.0f), 10000.0f);
        }

        private static float ClampAmplitude(float f)
        {
            return Math.Min(Math.Max(f, 0.0f), MASTER_VOLUME);
        }

        private static short Float2Short(float f)
        {
            return (short)(f * 32767.0f);
        }

        private static short SampleSquare(float t, float duty)
        {
            return t < duty ? short.MaxValue : short.MinValue;
        }

        private static short SampleTriangle(float t)
        {
            return Float2Short(t < 0.5f ? (-1.0f + 4.0f * t) : (1.0f - 4.0f * (t - 0.5f)));
        }

        private static short SampleSawtooth(float t)
        {
            return Float2Short(-1.0f + 2.0f * t);
        }

        private static short SampleSin(float t)
        {
            return Float2Short((float)Math.Sin(2.0 * Math.PI * t));
        }

        private static void FillNoiseBuffer(short[] noiseBuffer)
        {
            var noiseSource = s_noiseSource;
            if (noiseSource == null)
            {
                noiseSource = new Random();
                s_noiseSource = noiseSource;
            }
            for (int i = 0; i < NOISE_BUFFER_SIZE; ++i)
            {
                var r = (short)noiseSource.Next(short.MinValue, short.MaxValue);
                noiseBuffer[i] = r;
            }
        }

        private static short SampleNoise(float t, short[] noiseBuffer)
        {
            var r = (short)noiseBuffer[(int)(t * NOISE_BUFFER_SIZE_FLOAT)];
            return r;
        }
    }
}

