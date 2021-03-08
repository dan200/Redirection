using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.Computer;
using Dan200.Core.Computer.Devices;
using Dan200.Core.Computer.Devices.GPU;
using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Game.Arcade
{
    public class RobotDevices : IDevicePort, ICustomAudioSource
    {
        private const int DISPLAY_WIDTH = 64;
        private const int DISPLAY_HEIGHT = 64;
        private const int SPEAKER_CHANNELS = 2;
        private const int HDD_CAPACITY = 2000000;

        private ClockDevice m_clock;
        private LuaCPUDevice m_cpu;
        private ROMDevice m_rom;
        private GPUDevice m_gpu;
        private DisplayDevice m_display;
        private SpeakerDevice m_speaker;
        private GamepadDevice m_gamepad;
        private KeyboardDevice m_keyboard;
        private HardDriveDevice m_hardDrive;
        private DiskDriveDevice m_diskDrive;
        private ScoreDevice m_score;

        public DisplayDevice Display
        {
            get
            {
                return m_display;
            }
        }

        public DiskDriveDevice DiskDrive
        {
            get
            {
                return m_diskDrive;
            }
        }

        public SpeakerDevice Speaker
        {
            get
            {
                return m_speaker;
            }
        }

        public KeyboardDevice Keyboard
        {
            get
            {
                return m_keyboard;
            }
        }

        public GamepadDevice Gamepad
        {
            get
            {
                return m_gamepad;
            }
        }

        public ScoreDevice Score
        {
            get
            {
                return m_score;
            }
        }

        public IEnumerable<Device> Devices
        {
            get
            {
                yield return m_clock;
                yield return m_cpu;
                yield return m_rom;
                yield return m_gpu;
                yield return m_display;
                yield return m_speaker;
                yield return m_hardDrive;
                yield return m_diskDrive;
                yield return m_keyboard;
                yield return m_gamepad;
                yield return m_score;
            }
        }

        public RobotDevices()
        {
            var now = DateTime.UtcNow;
            var timeOffset = (now.AddYears(152) - now).TotalSeconds;
            m_clock = new ClockDevice("RoboSoft Basic Realtime Clock", timeOffset);
            m_cpu = new LuaCPUDevice("RoboSoft Basic Lua 5.3 CPU");
            m_rom = new ROMDevice("RoboSoft BIOS ROM", new AssetMount("rom", Assets.Sources.Where(source => source.Mod == null), "arcade/rom"));
            m_gpu = new GPUDevice("RoboSoft Basic GPU");
            m_display = new DisplayDevice("RoboSoft Basic Monochrome Display", DISPLAY_WIDTH, DISPLAY_HEIGHT, new Palette(new uint[] {
                0x000000ff, 0xffffffff
            }));
            m_speaker = new SpeakerDevice("RoboSoft Basic Internal Speaker", SPEAKER_CHANNELS);

            var hddPath = Path.Combine(App.SavePath, "arcade/hdd");
            m_hardDrive = new HardDriveDevice("RoboSoft Basic Hard Drive", new FileMount("hdd", hddPath, HDD_CAPACITY, false));
            m_diskDrive = new DiskDriveDevice("RoboSoft Basic Disk Drive");
            m_keyboard = new KeyboardDevice("RoboSoft Basic Keyboard");
            m_gamepad = new GamepadDevice("RoboSoft Basic 2-Button Gamepad", 2, 2);
            m_score = new ScoreDevice("RoboSoft Basic Highscore RAM");
        }

        public void GenerateSamples(ICustomPlayback playback, short[] data, int start, int numSamples)
        {
            m_speaker.Fill(data, start, numSamples, playback.Channels, playback.SampleRate);
        }
    }
}
