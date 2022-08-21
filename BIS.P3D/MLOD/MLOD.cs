using BIS.Core.Streams;
using BIS.P3D.ODOL;
using System;
using System.IO;

namespace BIS.P3D.MLOD
{
    public class MLOD : IReadWriteObject
    {
        public int Version { get; private set; }

        public P3DM_LOD[] Lods { get; private set; }

        public IModelInfo ModelInfo => new ComputedModelInfo(this);

        public MLOD(string fileName) : this(File.OpenRead(fileName)) 
        {
        }

        public MLOD(Stream stream)
        {
            Read(new BinaryReaderEx(stream));
        }

        public MLOD(P3DM_LOD[] lods)
        {
            Version = 257;
            Lods = lods;
        }

        internal MLOD()
        {
        }

        public void Read(BinaryReaderEx input)
        {
            if (input.ReadAscii(4) != "MLOD")
                throw new FormatException("MLOD signature expected");

            ReadContent(input);
        }

        internal void ReadContent(BinaryReaderEx input)
        {
            Version = input.ReadInt32();
            if (Version != 257)
                throw new ArgumentException("Unknown MLOD version");

            Lods = input.ReadArray(inp => new P3DM_LOD(inp));
        }

        public void Write(BinaryWriterEx output)
        {
            output.WriteAscii("MLOD", 4);
            output.Write(Version);
            output.Write(Lods.Length);
            for (int index = 0; index < Lods.Length; ++index)
                Lods[index].Write(output);
        }

        public void WriteToFile(string file, bool allowOverwriting=false)
        {
            var mode = (allowOverwriting) ? FileMode.Create : FileMode.CreateNew;

            var fs = new FileStream(file, mode);
            using (var output = new BinaryWriterEx(fs))
            {
                Write(output);
            }
        }

        public MemoryStream WriteToMemory()
        {
            var memStream = new MemoryStream(100000);
            var outStream = new BinaryWriterEx(memStream);
            Write(outStream);
            outStream.Position = 0;
            return memStream;
        }

        public void WriteToStream(Stream stream)
        {
            var output = new BinaryWriterEx(stream);
            Write(output);
        }
    }
}