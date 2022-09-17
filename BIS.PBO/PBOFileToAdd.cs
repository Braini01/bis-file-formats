using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BIS.PBO
{
    public class PBOFileToAdd : IPBOFileEntry
    {
        private readonly FileInfo file;

        public PBOFileToAdd(FileInfo file, string pboFileName)
        {
            FileName = pboFileName;
            this.file = file;
        }

        public string FileName { get; }

        public int Size => (int)file.Length;

        public int TimeStamp => (int)file.LastWriteTimeUtc.Subtract(PBO.Epoch).TotalSeconds;

        public bool IsCompressed => false;

        public int DiskSize => Size;

        public Stream OpenRead()
        {
            return file.Open(FileMode.Open, FileAccess.Read);
        }
    }
}
