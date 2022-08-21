using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class Keyframe
    {
        public Keyframe(BinaryReaderEx input, int version)
        {
            Time = input.ReadSingle();
            Point = input.ReadArray(i => new Vector3P(i));
        }

        public float Time { get; }

        public Vector3P[] Point { get; }

        internal void Write(BinaryWriterEx output, int version)
        {
            output.Write(Time);
            output.WriteArray(Point, (o, v) => v.Write(o));
        }
    }
}