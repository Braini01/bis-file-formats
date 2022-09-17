using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class Polygons
    {
        public Polygons(BinaryReaderEx input, int version)
        {
            var num = input.ReadInt32();
            Unused1 = input.ReadUInt32();
            Unused2 = input.ReadUInt16();
            Faces = input.ReadArrayBase(i => new Polygon(i, version), num);
        }

        public uint Unused1 { get; }
        public ushort Unused2 { get; }
        public Polygon[] Faces { get; }
        internal void Write(BinaryWriterEx output, int version)
        {
            output.Write(Faces.Length);
            output.Write(Unused1);
            output.Write(Unused2);
            output.WriteArrayBase(Faces, (o, v) => v.Write(o, version));
        }

    }
}