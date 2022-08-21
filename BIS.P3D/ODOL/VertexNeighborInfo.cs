using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class VertexNeighborInfo
    {
        public VertexNeighborInfo(BinaryReaderEx input)
        {
			PosA = input.ReadUInt16();
			Unused1 = input.ReadBytes(2);
			RtwA = new AnimationRTWeight(input);
			PosB = input.ReadUInt16();
			Unused2 = input.ReadBytes(2);
			RtwB = new AnimationRTWeight(input);
		}

        public ushort PosA { get; }
        public byte[] Unused1 { get; }
        public AnimationRTWeight RtwA { get; }
        public ushort PosB { get; }
        public byte[] Unused2 { get; }
        public AnimationRTWeight RtwB { get; }

        internal void Write(BinaryWriterEx output)
        {
            output.Write(PosA);
            output.Write(Unused1);
            RtwA.Write(output);
            output.Write(PosB);
            output.Write(Unused2);
            RtwB.Write(output);
        }

    }
}