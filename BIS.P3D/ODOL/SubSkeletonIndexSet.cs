using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class SubSkeletonIndexSet
    {
        public SubSkeletonIndexSet(BinaryReaderEx input)
        {
            SubSkeletons = input.ReadIntArray();
        }

        public int[] SubSkeletons { get; }

		public void Write(BinaryWriterEx output, int version)
		{
			output.WriteArray(SubSkeletons);
		}
	}
}