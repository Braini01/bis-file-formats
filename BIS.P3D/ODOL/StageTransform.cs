using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class StageTransform
    {
        internal StageTransform(BinaryReaderEx input)
        {
            UvSource = input.ReadUInt32();
            Transformation = new Matrix4P(input);
        }

        public uint UvSource { get; }

        public Matrix4P Transformation { get; }

        internal void Write(BinaryWriterEx output)
        {
            output.Write(UvSource);
            Transformation.Write(output);
        }
    }
}