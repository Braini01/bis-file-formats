using BIS.Core;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
	public class EmbeddedMaterial
    {
        public EmbeddedMaterial(BinaryReaderEx input)
        {
			MaterialName = input.ReadAsciiz();
			Version = input.ReadUInt32();
			Emissive = new ColorP(input);
			Ambient = new ColorP(input);
			Diffuse = new ColorP(input);
			ForcedDiffuse = new ColorP(input);
			Specular = new ColorP(input);
			SpecularCopy = new ColorP(input);
			SpecularPower = input.ReadSingle();
			PixelShader = input.ReadUInt32();
			VertexShader = input.ReadUInt32();
			MainLight = input.ReadUInt32();
			FogMode = input.ReadUInt32();
			if (Version == 3u)
			{
				Unused3 = input.ReadBoolean();
			}
			if (Version >= 6u)
			{
				SurfaceFile = input.ReadAsciiz();
			}
			if (Version >= 4u)
			{
				NRenderFlags = input.ReadUInt32();
				RenderFlags = input.ReadUInt32();
			}
			if (Version > 6u) // NStages
			{
				StageTextures = new StageTexture[input.ReadUInt32()];
			}
            else
            {
				StageTextures = new StageTexture[0];
			}
			if (Version > 8u) // NTexGens
			{
				StageTransforms = new StageTransform[input.ReadUInt32()];
			}
            else
			{
				StageTransforms = new StageTransform[StageTextures.Length];
			}

			if (Version < 8u)
			{
				for (int i = 0; i < StageTextures.Length; i++)
				{
					StageTransforms[i] = new StageTransform(input);
					StageTextures[i] = new StageTexture(input, Version);
				}
			}
			else
			{
				for (int i = 0; i < StageTextures.Length; i++)
				{
					StageTextures[i] = new StageTexture(input, Version);
				}
				for (int i = 0; i < StageTransforms.Length; i++)
				{
					StageTransforms[i] = new StageTransform(input);
				}
			}
			if (Version >= 10u)
			{
				StageTI = new StageTexture(input, Version);
			}
		}

        public string MaterialName { get; set; }
        public uint Version { get; }
        public ColorP Emissive { get; }
        public ColorP Ambient { get; }
        public ColorP Diffuse { get; }
        public ColorP ForcedDiffuse { get; }
        public ColorP Specular { get; }
        public ColorP SpecularCopy { get; }
        public float SpecularPower { get; }
        public uint PixelShader { get; }
        public uint VertexShader { get; }
        public uint MainLight { get; }
        public uint FogMode { get; }
        public bool Unused3 { get; }
        public string SurfaceFile { get; set; }
        public uint NRenderFlags { get; }
        public uint RenderFlags { get; }
        public StageTexture[] StageTextures { get; }
        public StageTransform[] StageTransforms { get; }
        public StageTexture StageTI { get; }

		public void Write(BinaryWriterEx output)
        {
			output.WriteAsciiz(MaterialName);
			output.Write(Version);
			Emissive.Write(output);
			Ambient.Write(output);
			Diffuse.Write(output);
			ForcedDiffuse.Write(output);
			Specular.Write(output);
			SpecularCopy.Write(output);
			output.Write(SpecularPower);
			output.Write(PixelShader);
			output.Write(VertexShader);
			output.Write(MainLight);
			output.Write(FogMode);
			if (Version == 3u)
			{
				output.Write(Unused3);
			}
			if (Version >= 6u)
			{
				output.WriteAsciiz(SurfaceFile);
			}
			if (Version >= 4u)
			{
				output.Write(NRenderFlags);
				output.Write(RenderFlags);
			}
			if (Version > 6u) // NStages
			{
				output.Write((uint)StageTextures.Length);
			}

			if (Version > 8u) // NTexGens
			{
				output.Write((uint)StageTransforms.Length);
			}

			if (Version < 8u)
			{
				for (int i = 0; i < StageTextures.Length; i++)
				{
					StageTransforms[i].Write(output);
					StageTextures[i].Write(output, Version);
				}
			}
			else
			{
				for (int i = 0; i < StageTextures.Length; i++)
				{
					StageTextures[i].Write(output, Version);
				}
				for (int i = 0; i < StageTransforms.Length; i++)
				{
					StageTransforms[i].Write(output);
				}
			}
			if (Version >= 10u)
			{
				StageTI.Write(output, Version);
			}
		}

        public override string ToString()
        {
            return MaterialName;
        }
    }
}