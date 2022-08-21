using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    internal class Proxy
    {
        public Proxy(BinaryReaderEx input, int version)
        {
			ProxyModel = input.ReadAsciiz();
			Transformation = new Matrix4P(input);
			SequenceID = input.ReadInt32();
			NamedSelectionIndex = input.ReadInt32();
			BoneIndex = input.ReadInt32();
			if (version >= 40)
			{
				SectionIndex = input.ReadInt32();
			}
		}

        public void Write(BinaryWriterEx output, int version)
        {
			output.WriteAsciiz(ProxyModel);
			Transformation.Write(output);
			output.Write(SequenceID );
			output.Write(NamedSelectionIndex);
			output.Write(BoneIndex);
			if (version >= 40)
			{
				output.Write(SectionIndex);
			}
		}

        public int SectionIndex { get; }
        public string ProxyModel { get; }
        public Matrix4P Transformation { get; }
        public int SequenceID { get; }
        public int NamedSelectionIndex { get; }
        public int BoneIndex { get; }
    }
}