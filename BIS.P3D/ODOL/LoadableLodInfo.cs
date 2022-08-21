using System.Drawing;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class LoadableLodInfo
    {
        public LoadableLodInfo(BinaryReaderEx input, int version)
        {
			NumberOfFaces = input.ReadInt32();
			Color = input.ReadUInt32();
			Special = input.ReadInt32();
			OrHints = input.ReadUInt32();
			if (version >= 39)
			{
				HasSkeleton = input.ReadBoolean();
			}
			if (version >= 51)
			{
				NumberOfVertices = input.ReadInt32();
				FaceArea = input.ReadSingle();
			}
		}

		public void Write(BinaryWriterEx output, int version)
        {
			output.Write(NumberOfFaces);
			output.Write(Color);
			output.Write(Special);
			output.Write(OrHints);
			if (version >= 39)
			{
				output.Write(HasSkeleton);
			}
			if (version >= 51)
			{
				output.Write(NumberOfVertices);
				output.Write(FaceArea);
			}
		}

        public int NumberOfVertices { get; }
        public float FaceArea { get; }
        public bool HasSkeleton { get; }
        public int Special { get; }
        public uint OrHints { get; }
        public int NumberOfFaces { get; }
        public uint Color { get; }
    }
}