using Dan200.Core.Main;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading;

namespace Dan200.Core.Audio.OpenAL
{
    public class OpenALCustomPlayback : ICustomPlayback, IDisposable
    {
        private const int SAMPLES_PER_BUFFER = 512;
        private const int NUM_BUFFERS = 3;
        private const int UPDATE_INTERVAL_MILLIS = 10;

        private ICustomAudioSource m_audioSource;
        private int m_channels;
        private int m_sampleRate;
        private uint m_source;
        private uint[] m_buffers;

        private object m_lock;
        private float m_volume;
        private bool m_stopped;

        public ICustomAudioSource Source
        {
            get
            {
                return m_audioSource;
            }
        }

        public int Channels
        {
            get
            {
                return m_channels;
            }
        }

        public int SampleRate
        {
            get
            {
                return m_sampleRate;
            }
        }

        public float Volume
        {
            get
            {
                return m_volume;
            }
            set
            {
                if (!m_stopped && m_volume != value)
                {
                    m_volume = value;
                    UpdateVolume();
                }
            }
        }

        public bool Stopped
        {
            get
            {
                return m_stopped;
            }
        }

        public OpenALCustomPlayback(ICustomAudioSource source, int channels, int sampleRate)
        {
            m_audioSource = source;
            m_channels = channels;
            m_sampleRate = sampleRate;
            m_volume = 1.0f;
            m_lock = new object();
            m_stopped = false;

            // Create the buffers
            m_buffers = new uint[NUM_BUFFERS];
            for (int i = 0; i < m_buffers.Length; ++i)
            {
                AL.GenBuffer(out m_buffers[i]);
                App.CheckOpenALError();
                if (OpenALAudio.Instance.XRam.IsInitialized)
                {
                    OpenALAudio.Instance.XRam.SetBufferMode(1, ref m_buffers[i], XRamExtension.XRamStorage.Hardware);
                }
            }

            // Create the source
            AL.GenSource(out m_source);
            UpdateVolume();
            App.CheckOpenALError();

            // Start the background thread
            var thread = new Thread(Run);
            thread.Start();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Update(float dt)
        {
        }

        public void Stop()
        {
            lock (m_lock)
            {
                if (!m_stopped)
                {
                    m_stopped = true;

                    AL.DeleteSource((int)m_source);
                    App.CheckOpenALError();

                    AL.DeleteBuffers(m_buffers);
                    App.CheckOpenALError();
                }
            }
        }

        private void Run()
        {
            try
            {
                App.DebugLog("Started streaming custom audio @ {0}Hz", m_sampleRate);

                // Get number of channels and sample rate
                var channels = m_channels;
                var format = (channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
                var sampleRate = m_sampleRate;

                // Start reading
                int nextBufferIndex = 0;
                var shortBuffer = new short[SAMPLES_PER_BUFFER * channels];

                // Loop until the we stop
                while (true)
                {
                    // Get the queue state
                    int queued, processed;
                    lock (m_lock)
                    {
                        if (m_stopped)
                        {
                            break;
                        }

                        AL.GetSource(m_source, ALGetSourcei.BuffersQueued, out queued);
                        AL.GetSource(m_source, ALGetSourcei.BuffersProcessed, out processed);
                        App.CheckOpenALError(true);

                        // Dequeued processed buffers
                        if (processed > 0)
                        {
                            AL.SourceUnqueueBuffers((int)m_source, processed);
                            App.CheckOpenALError(true);
                            queued -= processed;
                            processed = 0;
                        }
                    }

                    // Queue some samples
                    while (queued < m_buffers.Length)
                    {
                        // Generate the samples
                        m_audioSource.GenerateSamples(this, shortBuffer, 0, SAMPLES_PER_BUFFER);

                        // Pick an OpenAL buffer to put the samples in
                        var alBuffer = m_buffers[nextBufferIndex];
                        nextBufferIndex = (nextBufferIndex + 1) % m_buffers.Length;

                        lock (m_lock)
                        {
                            if (m_stopped)
                            {
                                break;
                            }

                            // Put the samples into the OpenAL buffer
                            AL.BufferData((int)alBuffer, format, shortBuffer, SAMPLES_PER_BUFFER * channels * sizeof(short), sampleRate);
                            App.CheckOpenALError(true);

                            // Add the buffer to the source's queue
                            AL.SourceQueueBuffer((int)m_source, (int)alBuffer);
                            App.CheckOpenALError(true);
                        }

                        queued++;
                    }

                    // Play the source if it's not playing already
                    lock (m_lock)
                    {
                        if (m_stopped)
                        {
                            break;
                        }

                        var state = AL.GetSourceState(m_source);
                        if (state != ALSourceState.Playing)
                        {
                            if (state != ALSourceState.Initial)
                            {
                                App.Log("Error: Buffer overrun detected. Resuming custom audio");
                            }
                            AL.SourcePlay(m_source);
                            App.CheckOpenALError(true);
                        }
                    }

                    // Sleep
                    Thread.Sleep(UPDATE_INTERVAL_MILLIS);
                }
                App.DebugLog("Stopped streaming custom audio");
            }
            catch (Exception e)
            {
                App.Log("Error streaming custom audio: Threw {0}: {1}", e.GetType().FullName, e.Message);
                App.Log(e.StackTrace);
            }
            finally
            {
                if (!m_stopped)
                {
                    // Wait for complete
                    while (true)
                    {
                        lock (m_lock)
                        {
                            if (m_stopped)
                            {
                                break;
                            }

                            var state = AL.GetSourceState(m_source);
                            if (state != ALSourceState.Playing)
                            {
                                break;
                            }
                        }
                        Thread.Sleep(UPDATE_INTERVAL_MILLIS);
                    }

                    // Stop
                    Stop();
                }
            }
        }

        public void UpdateVolume()
        {
            lock (m_lock)
            {
                if (!m_stopped)
                {
                    var globalVolume = OpenALAudio.Instance.EnableSound ? OpenALAudio.Instance.SoundVolume : 0.0f;
                    var localVolume = m_volume;
                    AL.Source(m_source, ALSourcef.Gain, globalVolume * localVolume);
                    App.CheckOpenALError();
                }
            }
        }
    }
}

