using BIS.Core.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.PBO
{
    public class FileEntry
    {
        public string FileName { get; set; }
        public int CompressedMagic { get; set; }
        public int UncompressedSize { get; set; }
        public int StartOffset { get; set; }
        public int TimeStamp { get; set; }
        public int DataSize { get; set; }

        public static int VersionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"), 0); //Vers
        public static int CompressionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("srpC"), 0); //Cprs
        public static int EncryptionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("rcnE"), 0); //Encr

        public FileEntry()
        {
            FileName = "";
            CompressedMagic = 0;
            UncompressedSize = 0;
            StartOffset = 0;
            TimeStamp = 0;
            DataSize = 0;
        }
        public FileEntry(BinaryReaderEx input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            FileName = input.ReadAsciiz();
            CompressedMagic = input.ReadInt32();
            UncompressedSize = input.ReadInt32();
            StartOffset = input.ReadInt32();
            TimeStamp = input.ReadInt32();
            DataSize = input.ReadInt32();
        }

        public void Write(BinaryWriterEx output)
        {
            output.WriteAsciiz(FileName);
            output.Write(CompressedMagic);
            output.Write(UncompressedSize);
            output.Write(StartOffset);
            output.Write(TimeStamp);
            output.Write(DataSize);
        }

        public bool IsVersion => CompressedMagic == VersionMagic && TimeStamp == 0 && DataSize == 0;
        public bool IsCompressed => CompressedMagic == CompressionMagic;
    }
}
