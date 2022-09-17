using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class Skeleton
    {
        internal Skeleton(BinaryReaderEx input, int version, int noOfLods)
        {
            SkeletonName = input.ReadAsciiz();
            if (!string.IsNullOrEmpty(SkeletonName))
            {
                if (version >= 23)
                {
                    IsDiscrete = input.ReadBoolean();
                }
                SkeletonBoneNames = input.ReadArray(i => new SkeletonBoneName(i));
                if (version > 40)
                {
                    PivotsNameObsolete = input.ReadAsciiz();
                }
            }
        }

        public string SkeletonName { get; }

        public bool IsDiscrete { get; }

        public SkeletonBoneName[] SkeletonBoneNames { get; }

        public string PivotsNameObsolete { get; }

        internal void Write(BinaryWriterEx output, int version, int noOfLods)
        {
            output.WriteAsciiz(SkeletonName);
            if (!string.IsNullOrEmpty(SkeletonName))
            {
                if (version >= 23)
                {
                    output.Write(IsDiscrete);
                }
                output.WriteArray(SkeletonBoneNames, (w, v) => v.Write(w));
                if (version > 40)
                {
                    output.WriteAsciiz(PivotsNameObsolete);
                }
            }
        }
    }
}