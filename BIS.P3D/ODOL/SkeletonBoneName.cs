using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class SkeletonBoneName
    {
        public SkeletonBoneName(BinaryReaderEx input)
        {
            BoneName = input.ReadAsciiz();
            ParentBoneName = input.ReadAsciiz();
        }

        public string BoneName { get; }
        public string ParentBoneName { get; }

        internal void Write(BinaryWriterEx output)
        {
            output.WriteAsciiz(BoneName);
            output.WriteAsciiz(ParentBoneName);
        }
    }
}