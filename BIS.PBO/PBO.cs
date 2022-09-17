using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BIS.Core;
using BIS.Core.Streams;

namespace BIS.PBO
{
    public class PBO : IDisposable
    {
        public static DateTime Epoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

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
        public List<IPBOFileEntry> Files { get; } = new List<IPBOFileEntry>();

        [Obsolete]
        public LinkedList<FileEntry> FileEntries { get; } = new LinkedList<FileEntry>();

        [Obsolete]
        public LinkedList<string> Properties { get; } = new LinkedList<string>();
        public List<KeyValuePair<string,string>> PropertiesPairs { get; } = new List<KeyValuePair<string, string>>();
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

        public PBO()
        {

        }

        private void ReadHeader(BinaryReaderEx input)
        {
            int curOffset = 0;
            FileEntry pboEntry;
#pragma warning disable CS0612 // Le type ou le membre est obsolète
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

                        PropertiesPairs.Add(new KeyValuePair<string, string>(name, value));

                        if (name == "prefix")
                            Prefix = value;
                    }
                    while (name != "");

                    if (Properties.Count % 2 != 0)
                        throw new Exception("metaData count is not even.");
                }
                else if (pboEntry.FileName != "")
                {
                    FileEntries.AddLast(pboEntry);
                    Files.Add(new PBOFileExisting(pboEntry, this));
                }
            }
            while (pboEntry.FileName != "" || Files.Count == 0);
#pragma warning restore CS0612 // Le type ou le membre est obsolète

            DataOffset = (int)input.Position;

            if (Prefix == null)
            {
                Prefix = Path.GetFileNameWithoutExtension(PBOFilePath);
            }
        }

        private byte[] GetFileData(FileEntry entry)
        {
            byte[] bytes;
            lock (this)
            {
                PBOFileStream.Position = DataOffset + entry.StartOffset;
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
            }

            return bytes;
        }

        [Obsolete]
        public void ExtractFile(FileEntry entry, string dst)
        {
            ExtractFiles(Methods.Yield(entry), dst);
        }

        [Obsolete]
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

        public void ExtractFiles(IEnumerable<IPBOFileEntry> entries, string target)
        {
            foreach (var entry in entries)
            {
                var path = Path.Combine(target, entry.FileName);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using(var targetFile = File.Create(path))
                {
                    using(var source = entry.OpenRead())
                    {
                        source.CopyTo(targetFile);
                    }
                }
            }
        }

        public void ExtractAllFiles(string directory)
        {
            ExtractFiles(Files, Path.Combine(directory, Prefix));
        }

        public MemoryStream GetFileEntryStream(FileEntry entry)
        {
            return new MemoryStream(GetFileData(entry), false);
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

        private static void WriteBasicHeader(BinaryWriterEx output, IEnumerable<FileEntry> fileEntries)
        {
            foreach (var entry in fileEntries)
            {
                entry.Write(output);
            }

            EmptyEntry.Write(output);
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

        public void Save()
        {
            if (string.IsNullOrEmpty(PBOFilePath))
            {
                throw new InvalidOperationException("PBO is not bound to a file, please use SaveTo() instead.");
            }
            SaveTo(PBOFilePath);
        }

        public void SaveTo(string targetFile)
        {
            if (PBOFilePath == null)
            {
                SaveToInternal(targetFile, true);
                PBOFilePath = targetFile;
                return;
            }
            if (string.Equals(Path.GetFullPath(targetFile), Path.GetFullPath(PBOFilePath), StringComparison.OrdinalIgnoreCase))
            {
                var temp = Path.GetTempFileName();
                SaveToInternal(temp, true);
                if (pboFileStream != null)
                {
                    pboFileStream.Close();
                    pboFileStream = null;
                }
                File.Copy(temp, targetFile, true);
                return;
            }
            SaveToInternal(targetFile, false);
        }

        private void SaveToInternal(string targetFile, bool isReplaceSelf)
        {
            var entries = Files.Select(e => new FileEntry() { 
                FileName = e.FileName, 
                TimeStamp = e.TimeStamp,
                DataSize = e.Size, 
                UncompressedSize = 0, 
                CompressedMagic = 0 
            }).ToList();

            var offset = 0;
            foreach(var entry in entries)
            {
                entry.StartOffset = offset;
                offset += entry.DataSize;
            }

            using(var target = File.Create(targetFile))
            {
                using (var output = new BinaryWriterEx(target, true))
                {
                    WriteProperties(output, PropertiesPairs.SelectMany(p => new[] { p.Key, p.Value }));
                    WriteBasicHeader(output, entries);
                }
                foreach(var file in Files)
                {
                    using (var source = file.OpenRead())
                    {
                        source.CopyTo(target);
                    }
                }
                target.Position = 0;
                byte[] hash;
                using (var sha1 = new SHA1Managed())
                {
                    hash = sha1.ComputeHash(target);
                }
                target.WriteByte(0x0);
                target.Write(hash, 0, 20);
            }

            if (isReplaceSelf)
            {
                Files.Clear();
                Files.AddRange(entries.Select(e => new PBOFileExisting(e, this)));

#pragma warning disable CS0612 // Le type ou le membre est obsolète
                FileEntries.Clear();
                foreach (var entry in entries)
                {
                    FileEntries.AddLast(entry);
                }
#pragma warning restore CS0612 // Le type ou le membre est obsolète
            }
        }

        [Obsolete]
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

        public void Dispose()
        {
            if (pboFileStream != null)
            {
                pboFileStream.Close();
                pboFileStream = null;
            }
        }
    }
}
