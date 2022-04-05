using System;

namespace FileShare
{
    [Serializable]
    public class SharedFile
    {
        public string Creator { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public byte[] FileBytes { get; set; }

        public SharedFile(string creator, string filename, byte[] fileBytes)
        {
            Creator = creator;
            FileName = filename;
            FileBytes = fileBytes;
        }

        public void SetPath(string path)
        {
            FilePath = path;
        }

        public override string ToString()
        {
            return Creator + ": " + FileName;
        }
    }
}
