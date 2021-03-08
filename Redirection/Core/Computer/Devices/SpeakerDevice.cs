using Dan200.Core.Computer.Devices.Speaker;
using Dan200.Core.Lua;
using System;

namespace Dan200.Core.Computer.Devices
{
    public class SpeakerDevice : Device
    {
        private string m_description;
        private Speaker.Speaker m_speaker;
        private Computer m_computer;

        public override string Type
        {
            get
            {
                return "speaker";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public SpeakerDevice(string description, int channels)
        {
            m_description = description;
            m_speaker = new Speaker.Speaker(channels);
            m_speaker.OnSoundComplete += delegate (int channel)
            {
                if (m_computer != null)
                {
                    m_computer.Events.Queue("sound_complete", new LuaArgs(channel + 1));
                }
            };
        }

        public void Fill(short[] buffer, int start, int samples, int channels, int sampleRate)
        {
            m_speaker.Fill(buffer, start, samples, channels, sampleRate);
        }

        public override void Attach(Computer computer)
        {
            m_computer = computer;
        }

        public override DeviceUpdateResult Update(TimeSpan dt)
        {
            m_speaker.Update();
            return DeviceUpdateResult.Continue;
        }

        public override void Detach()
        {
            m_computer = null;
            m_speaker.Stop();
        }

        [LuaMethod]
        public LuaArgs getNumChannels(LuaArgs args)
        {
            return new LuaArgs(m_speaker.Channels);
        }

        private Sound parseSound(LuaArgs args, int index)
        {
            LuaTable table = args.GetTable(index);
            string waveform = table.IsNil("waveform") ? "square" : table.GetString("waveform");
            float volume = table.IsNil("volume") ? 1.0f : table.GetFloat("volume");
            float duty = table.IsNil("duty") ? 0.5f : table.GetFloat("duty");
            float duration = table.GetFloat("duration");
            float attack = table.IsNil("attack") ? 0.0f : table.GetFloat("attack");
            float decay = table.IsNil("decay") ? 0.0f : table.GetFloat("decay");
            float frequency = table.GetFloat("frequency");
            float slide = table.IsNil("slide") ? 0.0f : table.GetFloat("slide");
            float vibratoDepth = table.IsNil("vibrato_depth") ? 0.0f : table.GetFloat("vibrato_depth");
            float vibratoFrequency = table.IsNil("vibrato_frequency") ? 0.0f : table.GetFloat("vibrato_frequency");
            bool loop = table.IsNil("loop") ? false : table.GetBool("loop");

            var sound = new Sound();
            sound.Waveform = ParseWaveform(waveform);
            sound.Volume = Clamp(volume, 0.0f, 1.0f);
            sound.Duty = Clamp(duty, 0.0f, 1.0f);
            sound.Attack = Math.Max(attack, 0.0f);
            sound.Duration = Math.Max(duration, 0.0f);
            sound.Decay = Math.Max(decay, 0.0f);
            sound.Frequency = Math.Max(frequency, 0.0f);
            sound.Slide = slide;
            sound.VibratoDepth = Math.Max(vibratoDepth, 0.0f);
            sound.VibratoFrequency = Math.Max(vibratoFrequency, 0.0f);
            sound.Loop = loop;

            return sound;
        }

        [LuaMethod]
        public LuaArgs play(LuaArgs args)
        {
            var sound = parseSound(args, 0);
            if (args.IsNil(1))
            {
                int channel = m_speaker.Play(sound);
                if (channel >= 0)
                {
                    return new LuaArgs(channel);
                }
            }
            else
            {
                int channel = args.GetInt(1);
                channel = m_speaker.Play(sound, channel);
                if (channel >= 0)
                {
                    return new LuaArgs(channel);
                }
            }
            return LuaArgs.Nil;
        }

        [LuaMethod]
        public LuaArgs getChannelState(LuaArgs args)
        {
            int channel = args.GetInt(0);
            int queuedSounds;
            var state = m_speaker.GetChannelState(channel, out queuedSounds);
            return new LuaArgs(
                state.ToString().ToLowerInvariant(),
                queuedSounds
            );
        }

        [LuaMethod]
        public LuaArgs queue(LuaArgs args)
        {
            var sound = parseSound(args, 0);
            int channel = args.GetInt(1);
            channel = m_speaker.Queue(sound, channel);
            if (channel >= 0)
            {
                return new LuaArgs(channel);
            }
            else
            {
                return LuaArgs.Nil;
            }
        }

        [LuaMethod]
        public LuaArgs stop(LuaArgs args)
        {
            if (args.IsNil(0))
            {
                m_speaker.Stop();
            }
            else
            {
                int channel = args.GetInt(0);
                m_speaker.Stop(channel);
            }
            return LuaArgs.Empty;
        }

        private Waveform ParseWaveform(string str)
        {
            switch (str)
            {
                case "square":
                    {
                        return Waveform.Square;
                    }
                case "triangle":
                    {
                        return Waveform.Triangle;
                    }
                case "sawtooth":
                    {
                        return Waveform.Sawtooth;
                    }
                case "noise":
                    {
                        return Waveform.Noise;
                    }
                default:
                    {
                        throw new LuaError("Unsupported waveform");
                    }
            }
        }

        private float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
