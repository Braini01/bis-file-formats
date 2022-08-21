using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class AnimationRTWeight
    {
        public AnimationRTWeight(BinaryReaderEx input)
        {
            Count = input.ReadInt32();
            Data = input.ReadBytes(8);
        }
        public int Count { get; }

        public byte[] Data { get; }

        internal void Write(BinaryWriterEx output)
        {
            output.Write(Count);
            output.Write(Data);
        }
    }
}