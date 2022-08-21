using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
	public class StageTexture
	{
		internal StageTexture(BinaryReaderEx input, uint version)
		{
			if (version >= 5u)
			{
				TextureFilter = input.ReadUInt32();
			}
			Texture = input.ReadAsciiz();
			if (version >= 8u)
			{
				StageID = input.ReadUInt32();
			}
			if (version >= 11u)
			{
				UseWorldEnvMap = input.ReadBoolean();
			}
		}

		public bool UseWorldEnvMap { get; }

		public string Texture { get; set; }

		public uint StageID { get; }

		public uint TextureFilter { get; }

		internal void Write(BinaryWriterEx output, uint version)
		{
			if (version >= 5u)
			{
				output.Write(TextureFilter);
			}
			output.WriteAsciiz(Texture);
			if (version >= 8u)
			{
				output.Write(StageID);
			}
			if (version >= 11u)
			{
				output.Write(UseWorldEnvMap);
			}
		}

	}
}