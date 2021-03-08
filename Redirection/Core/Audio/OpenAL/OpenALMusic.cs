using Dan200.Core.Assets;
using Dan200.Core.Main;
using NVorbis;
using System.IO;

namespace Dan200.Core.Audio.OpenAL
{
    public class OpenALMusic : Music
    {
        private string m_path;
        private string m_pathOnDisk;
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

        public OpenALMusic(string path, IFileStore store)
        {
            m_path = path;
            Locate(store);
        }

        public override void Dispose()
        {
            m_pathOnDisk = null;
        }

        public override void Reload(IFileStore store)
        {
            OpenALAudio.Instance.StopMusic(this);
            Locate(store);
        }

        public Stream OpenForStreaming()
        {
            if (File.Exists(m_pathOnDisk))
            {
                return File.OpenRead(m_pathOnDisk);
            }
            return null;
        }

        private void Locate(IFileStore store)
        {
            if (store is FolderFileStore)
            {
                // Locate the file
                var folderStore = (FolderFileStore)store;
                var fullPath = System.IO.Path.Combine(folderStore.Path, m_path.Replace('/', System.IO.Path.DirectorySeparatorChar));
                m_pathOnDisk = fullPath;

                // Get the duration
                using (var vorbis = new VorbisReader(OpenForStreaming(), true))
                {
                    m_duration = (float)vorbis.TotalTime.TotalSeconds;
                }
            }
            else
            {
                App.Log("Error: Failed to load {0}, music cannot be loaded from ZIP files");
                m_pathOnDisk = null;
                m_duration = 0.0f;
            }
        }
    }
}

