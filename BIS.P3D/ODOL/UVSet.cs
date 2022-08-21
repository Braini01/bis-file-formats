using BIS.Core;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
	internal class UVSet
	{
		public UVSet(BinaryReaderEx input, int version)
		{
			if (version >= 45u)
			{
				MinU = input.ReadSingle();
				MinV = input.ReadSingle();
				MaxU = input.ReadSingle();
				MaxV = input.ReadSingle();
			}
			NVertices = input.ReadUInt32();
			DefaultFill = input.ReadBoolean();
			var num = (version >= 45u) ? 4u : 8u;
			if (DefaultFill)
			{
				DefaultValue = input.ReadBytes((int)num);
			}
			else
			{
				UvData = input.ReadCompressedTracked(NVertices * num);
			}
		}

		public uint NVertices { get; }
		public TrackedArray<byte> UvData { get; }
		public byte[] DefaultValue { get; }
		public bool DefaultFill { get; }
		public float MinU { get; }
		public float MinV { get; }
		public float MaxU { get; }
		public float MaxV { get; }

		internal void Write(BinaryWriterEx output, int version)
		{
			if (version >= 45u)
			{
				output.Write(MinU);
				output.Write(MinV);
				output.Write(MaxU);
				output.Write(MaxV);
			}
			output.Write(NVertices);
			output.Write(DefaultFill);
			var num = (version >= 45u) ? 4u : 8u;
			if (DefaultFill)
			{
				output.Write(DefaultValue);
			}
			else
			{
				output.WriteCompressed(UvData);
			}
		}
	}
}