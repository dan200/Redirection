using Dan200.Core.Assets;
using Dan200.Core.Main;
using OpenTK.Audio.OpenAL;
using System;
using System.IO;
using System.Text;

namespace Dan200.Core.Audio.OpenAL
{
    public class OpenALSound : Sound
    {
        private string m_path;
        private uint m_buffer;
        private float m_duration;

        public override string Path
        {
            get
            {
                return m_path;
            }
        }

        public override float Duration
        {
            get
            {
                return m_duration;
            }
        }

        public uint ALBuffer
        {
            get
            {
                return m_buffer;
            }
        }

        public OpenALSound(string path, IFileStore store)
        {
            m_path = path;
            Load(store);
        }

        public override void Dispose()
        {
            Unload();
        }

        public override void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        private void Load(IFileStore store)
        {
            // Create the buffer
            AL.GenBuffer(out m_buffer);
            if (OpenALAudio.Instance.XRam.IsInitialized)
            {
                OpenALAudio.Instance.XRam.SetBufferMode(1, ref m_buffer, XRamExtension.XRamStorage.Hardware);
            }

            // Load the file
            byte[] soundData;
            int channels, bitsPerSample, sampleRate;
            using (var stream = store.OpenFile(m_path))
            {
                soundData = LoadWave(stream, out channels, out bitsPerSample, out sampleRate);
            }

            // Measure the sound
            int samples = (soundData.Length * 8) / (bitsPerSample * channels);
            m_duration = (float)samples / (float)sampleRate;

            // Load the sound
            var soundFormat = GetSoundFormat(channels, bitsPerSample);
            AL.BufferData((int)m_buffer, soundFormat, soundData, soundData.Length, sampleRate);
            App.CheckOpenALError();
        }

        private void Unload()
        {
            OpenALAudio.Instance.StopSound(this);
            AL.DeleteBuffer(ref m_buffer);
            App.CheckOpenALError();
        }

        private static byte[] LoadWave(Stream stream, out int o_channels, out int o_bitsPerSample, out int o_sampleRate)
        {
            var reader = new BinaryReader(stream, Encoding.ASCII);

            // RIFF header
            string signature = new string(reader.ReadChars(4));
            if (signature != "RIFF")
            {
                throw new NotSupportedException("Specified stream is not a wave file.");
            }

#pragma warning disable 0219 // Unused variable
            int riff_chunck_size = reader.ReadInt32();
#pragma warning restore 0219 // Unused variable

            string format = new string(reader.ReadChars(4));
            if (format != "WAVE")
            {
                throw new NotSupportedException("Specified stream is not a wave file.");
            }

            // WAVE header
            string format_signature = new string(reader.ReadChars(4));
            if (format_signature != "fmt ")
            {
                throw new NotSupportedException("Specified wave file is not supported.");
            }

#pragma warning disable 0219 // Unused variable
            int format_chunk_size = reader.ReadInt32();
            int audio_format = reader.ReadInt16();
            int num_channels = reader.ReadInt16();
            int sample_rate = reader.ReadInt32();
            int byte_rate = reader.ReadInt32();
            int block_align = reader.ReadInt16();
            int bits_per_sample = reader.ReadInt16();
#pragma warning restore 0219 // Unused variable

            string data_signature = new string(reader.ReadChars(4));
            if (data_signature != "data")
            {
                throw new NotSupportedException("Specified wave file is not supported.");
            }

            int data_chunk_size = reader.ReadInt32();
            o_channels = num_channels;
            o_bitsPerSample = bits_per_sample;
            o_sampleRate = sample_rate;
            return reader.ReadBytes((int)data_chunk_size);
        }

        private static ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1:
                    {
                        return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                    }
                case 2:
                    {
                        return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                    }
                default:
                    {
                        throw new NotSupportedException("The specified sound format is not supported.");
                    }
            }
        }
    }
}

