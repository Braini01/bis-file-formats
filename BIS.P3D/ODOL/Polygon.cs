using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class Polygon
    {
        public Polygon(BinaryReaderEx input, int version)
        {
            var length = input.ReadByte();
            if (version >= 69)
            {
                VertexIndices = input.ReadArrayBase(i => i.ReadInt32(), length);
            }
            else
            {
                VertexIndices = input.ReadArrayBase(i => (int)i.ReadUInt16(), length);
            }
        }

        public int[] VertexIndices { get; }

        internal void Write(BinaryWriterEx output, int version)
        {
            output.Write((byte)VertexIndices.Length);

            if (version >= 69)
            {
                output.WriteArrayBase(VertexIndices, (o,v) => o.Write(v));
            }
            else
            {
                output.WriteArrayBase(VertexIndices, (o, v) => o.Write((ushort)v));
            }
        }

    }
}