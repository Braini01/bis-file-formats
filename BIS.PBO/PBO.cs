using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BIS.Core.Streams;
using BIS.Core;

namespace BIS.PBO
{
    public class PBO
    {
        private static FileEntry VersionEntry;
        private static FileEntry EmptyEntry;

        private FileStream pboFileStream;

        public string PBOFilePath { get; private set; }

        public FileStream PBOFileStream
        {
            get
            {
                pboFileStream = pboFileStream ?? File.OpenRead(PBOFilePath);
                return pboFileStream;
            }
        }
        public LinkedList<FileEntry> FileEntries { get; } = new LinkedList<FileEntry>();
        public LinkedList<string> Properties { get; } = new LinkedList<string>();
        public int DataOffset { get; private set; }
        public string Prefix { get; private set; }
 
        public string FileName => Path.GetFileName(PBOFilePath);

        static PBO()
        {
            VersionEntry = new FileEntry
            {
                CompressedMagic = FileEntry.VersionMagic,
                FileName = ""
            };

            EmptyEntry = new FileEntry();
        }

        public PBO(string fileName, bool keepStreamOpen = false)
        {
            PBOFilePath = fileName;
            var input = new BinaryReaderEx(PBOFileStream);
            ReadHeader(input);
            if (!keepStreamOpen)
            {
                pboFileStream.Close();
                pboFileStream = null;
            }
        }

        private void ReadHeader(BinaryReaderEx input)
        {
            int curOffset = 0;
            FileEntry pboEntry;
            do
            {
                pboEntry = new FileEntry(input)
                {
                    StartOffset = curOffset
                };

                curOffset += pboEntry.DataSize;

                if (pboEntry.IsVersion)
                {
                    string name;
                    string value;
                    do
                    {
                        name = input.ReadAsciiz();
                        if (name == "") break;
                        Properties.AddLast(name);

                        value = input.ReadAsciiz();
                        Properties.AddLast(value);

                        if (name == "prefix")
                            Prefix = value;
                    }
                    while (name != "");

                    if (Properties.Count % 2 != 0)
                        throw new Exception("metaData count is not even.");
                }
                else if (pboEntry.FileName != "")
                    FileEntries.AddLast(pboEntry);
            }
            while (pboEntry.FileName != "" || FileEntries.Count == 0);

            DataOffset = (int)input.Position;
        }

        private byte[] GetFileData(FileEntry entry)
        {
            PBOFileStream.Position = DataOffset + entry.StartOffset;
            byte[] bytes;
            if (entry.CompressedMagic == 0)
            {
                bytes = new byte[entry.DataSize];
                PBOFileStream.Read(bytes, 0, entry.DataSize);
            }
            else
            {
                if (!entry.IsCompressed)
                    throw new Exception("Unexpected packingMethod");

                var br = new BinaryReaderEx(PBOFileStream);
                bytes = br.ReadLZSS((uint)entry.UncompressedSize);
            }

            return bytes;
        }

        public void ExtractFile(FileEntry entry, string dst)
        {
            ExtractFiles(Methods.Yield(entry), dst);
        }

        public void ExtractFiles(IEnumerable<FileEntry> entries, string dst, bool keepStreamOpen = false)
        {
            foreach (var entry in entries.OrderBy(e => e.StartOffset))
            {
                if (entry.DataSize <= 0) continue;
                string path = Path.Combine(dst, entry.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, GetFileData(entry));
            }

            if (!keepStreamOpen)
            {
                pboFileStream.Close();
                pboFileStream = null;
            }
        }

        public void ExtractAllFiles(string directory)
        {
            var dstPath = Path.Combine(directory, Prefix);
            ExtractFiles(FileEntries, dstPath);
        }

        public MemoryStream GetFileEntryStream(FileEntry entry)
        {
            return GetFileEntryStreams(Methods.Yield(entry)).First();
        }

        public IEnumerable<MemoryStream> GetFileEntryStreams(IEnumerable<FileEntry> entries, bool keepStreamOpen = false)
        {
            foreach (var entry in entries)
            {
                if (entry.DataSize <= 0) continue;
                yield return new MemoryStream(GetFileData(entry), false);
            }

            if (!keepStreamOpen)
            {
                pboFileStream.Close();
                pboFileStream = null;
            }
        }

        private void WriteBasicHeader(BinaryWriterEx output)
        {
            WriteBasicHeader(output, FileEntries);
        }

        private static void WriteBasicHeader(BinaryWriterEx output, IEnumerable<FileEntry> fileEntries)
        {
            foreach (var entry in fileEntries)
            {
                entry.Write(output);
            }

            EmptyEntry.Write(output);
        }

        private void WriteProperties(BinaryWriterEx output)
        {
            WriteProperties(output, Properties);
        }

        private static void WriteProperties(BinaryWriterEx output, IEnumerable<string> properties)
        {
            //create starting entry
            VersionEntry.Write(output);

            foreach (var e in properties)
            {
                output.WriteAsciiz(e);
            }
            output.Write((byte)0); //empty string
        }

        private void WriteHeader(BinaryWriterEx output)
        {
            WriteProperties(output);
            WriteBasicHeader(output);
        }

        public static IEnumerable<KeyValuePair<FileEntry, PBO>> GetAllNonEmptyFileEntries(string path)
        {
            var allPBOs = Directory.GetFiles(path, "*.pbo", SearchOption.AllDirectories);

            foreach (var pboPath in allPBOs)
            {
                var pbo = new PBO(pboPath);
                foreach (var entry in pbo.FileEntries)
                {
                    if (entry.DataSize > 0)
                        yield return new KeyValuePair<FileEntry, PBO>(entry, pbo);
                }
            }
        }
    }
}
