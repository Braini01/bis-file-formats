using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BIS.Core;
using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class LOD : ILevelOfDetail
	{
        internal LOD(BinaryReaderEx input, float resolution, LoadableLodInfo loadableLodInfo, int version)
        {
			var st = input.Position;

            Resolution = resolution;
            LoadableLodInfo = loadableLodInfo;
			Proxies = input.ReadArray(i => new Proxy(i, version));
			SubSkeletonsToSkeleton = input.ReadIntArray();
			SkeletonToSubSkeleton = input.ReadArray(i => new SubSkeletonIndexSet(i));
			if (version >= 50u)
			{
				VertexCount = input.ReadUInt32();
			}
			else
			{
				ClipOldFormat = input.ReadCondensedIntArrayTracked();
			}
			if (version >= 51u)
			{
				FaceArea = input.ReadSingle();
			}
			OrHints = input.ReadInt32();
            AndHints = input.ReadInt32();
			BMin = new Vector3P(input);
			BMax = new Vector3P(input);
			BCenter = new Vector3P(input);
			BRadius = input.ReadSingle();
            Textures = input.ReadStringArray();
			Materials = input.ReadArray(i => new EmbeddedMaterial(i));

			PointToVertex = ReadCompressedVertexIndexArray(input, version);
			VertexToPoint = ReadCompressedVertexIndexArray(input, version);
			Polygons = new Polygons(input, version);

			Sections = input.ReadArray(i => new Section(i, version));
			NamedSelections = input.ReadArray(i => new NamedSelection(i, version));
			NamedProperties = input.ReadArray(i => new Tuple<string, string>(i.ReadAsciiz(), i.ReadAsciiz()));


			Frames = input.ReadArray(i => new Keyframe(i, version));
			ColorTop = input.ReadInt32();
			Color = input.ReadInt32();
			Special = input.ReadInt32();
			VertexBoneRefIsSimple = input.ReadBoolean();

			var sizeOfRestDataPos = input.Position;

			var sizeOfRestData = input.ReadUInt32();

			if (version >= 50u)
			{
				Clip = input.ReadCondensedIntArrayTracked();
			}

			var uvset0 = new UVSet(input, version);
			UvSets = new UVSet[input.ReadUInt32()];
			UvSets[0] = uvset0;
			for (int i = 1; i < UvSets.Length; ++i)
			{
				UvSets[i] = new UVSet(input, version);
			}

			Vertices = input.ReadCompressedArrayTracked(i => new Vector3P(i), 12);

			if (version >= 45u)
			{
				NormalsCompressed = input.ReadCondensedArrayTracked(i => new Vector3PCompressed(i), 4);
				STCoordsCompressed = input.ReadCompressedArrayTracked(i => new Tuple<Vector3PCompressed, Vector3PCompressed>(new Vector3PCompressed(i), new Vector3PCompressed(i)), 8);
			}
			else
			{
				Normals = input.ReadCondensedArrayTracked(i => new Vector3P(i), 12);
				STCoords = input.ReadCompressedArrayTracked(i => new Tuple<Vector3P, Vector3P>(new Vector3P(i), new Vector3P(i)), 24);
			}

			VertexBoneRef = input.ReadCompressedArrayTracked(i => new AnimationRTWeight(i), 12);
			NeighborBoneRef = input.ReadCompressedArrayTracked(i => new VertexNeighborInfo(i), 32);
			if (version >= 67u)
			{
				Unknown = input.ReadUInt32();
			}
			var endOfDataPosition = input.Position;
			var sizeOfRestDataReal = endOfDataPosition - sizeOfRestDataPos - 4;
			if (sizeOfRestDataReal != sizeOfRestData)
            {
				Trace.TraceWarning($"LOD {Resolution} SizeOfRestData invalid: Expected={sizeOfRestData}, Actual={sizeOfRestDataReal}");
            }
			if (version >= 68u)
			{
				Unknown68 = input.ReadByte();
			}
		}

		internal static TrackedArray<int> ReadCompressedVertexIndexArray(BinaryReaderEx input, int version)
		{
			if (version >= 69)
			{
				return input.ReadCompressedArrayTracked(i => i.ReadInt32(), 4);
			}
			return input.ReadCompressedArrayTracked(i => (int)i.ReadUInt16(), 2);
		}

		internal static void WriteCompressedVertexIndexArray(BinaryWriterEx output, int version, TrackedArray<int> values)
		{
			if (version >= 69)
			{
				output.WriteCompressedArray(values, (o, v) => o.Write(v), 4);
			}
			else
			{
				output.WriteCompressedArray(values, (o, v) => o.Write((ushort)v), 2);
			}
		}

		public float Resolution { get; }
        internal LoadableLodInfo LoadableLodInfo { get; }
		internal Proxy[] Proxies { get; }
        public int[] SubSkeletonsToSkeleton { get; }
		internal SubSkeletonIndexSet[] SkeletonToSubSkeleton { get; }
        public uint VertexCount { get; }
        public TrackedArray<int> ClipOldFormat { get; }
        public float FaceArea { get; }
        public int OrHints { get; }
        public int AndHints { get; }
        public Vector3P BMin { get; }
        public Vector3P BMax { get; }
        public Vector3P BCenter { get; }
        public float BRadius { get; }
        public string[] Textures { get; }
		public EmbeddedMaterial[] Materials { get; }
        public TrackedArray<int> PointToVertex { get; }
        public TrackedArray<int> VertexToPoint { get; }
		internal Polygons Polygons { get; }
		public Section[] Sections { get; }
		internal NamedSelection[] NamedSelections { get; }
        public Tuple<string, string>[] NamedProperties { get; }
		internal Keyframe[] Frames { get; }
        public int ColorTop { get; }
        public int Color { get; }
        public int Special { get; }
        public bool VertexBoneRefIsSimple { get; }
        public TrackedArray<int> Clip { get; }
		internal UVSet[] UvSets { get; }
        public TrackedArray<Vector3PCompressed> NormalsCompressed { get; }
        public TrackedArray<Tuple<Vector3PCompressed, Vector3PCompressed>> STCoordsCompressed { get; }
        public TrackedArray<Vector3P> Normals { get; }
        public TrackedArray<Tuple<Vector3P, Vector3P>> STCoords { get; }
        public TrackedArray<Vector3P> Vertices { get; }
        public uint Unknown { get; }
        public byte Unknown68 { get; }
		internal TrackedArray<AnimationRTWeight> VertexBoneRef { get; }
		internal TrackedArray<VertexNeighborInfo> NeighborBoneRef { get; }

		IEnumerable<Tuple<string, string>> ILevelOfDetail.NamedProperties => NamedProperties.AsEnumerable();

		public int FaceCount => Polygons.Faces.Length;

		IEnumerable<string> ILevelOfDetail.NamedSelections => NamedSelections.Select(s => s.Name);

		internal void Write(BinaryWriterEx output, int version)
		{
			output.WriteArray(Proxies, (o,v) => v.Write(o, version));
			output.WriteArray(SubSkeletonsToSkeleton);
			output.WriteArray(SkeletonToSubSkeleton, (o, v) => v.Write(o, version));
			if (version >= 50u)
			{
				output.Write(VertexCount);
			}
			else
			{
				output.WriteCondensedIntArray(ClipOldFormat);
			}
			if (version >= 51u)
			{
				output.Write(FaceArea);
			}
			output.Write(OrHints);
			output.Write(AndHints);
			BMin.Write(output);
			BMax.Write(output);
			BCenter.Write(output);
			output.Write(BRadius);
			output.WriteArray(Textures, (o, v) => o.WriteAsciiz(v));
			output.WriteArray(Materials, (o, v) => v.Write(o));

			WriteCompressedVertexIndexArray(output, version, PointToVertex);
			WriteCompressedVertexIndexArray(output, version, VertexToPoint);
			Polygons.Write(output, version);
			output.WriteArray(Sections, (o, v) => v.Write(o, version));
			output.WriteArray(NamedSelections, (o, v) => v.Write(o, version));
			output.WriteArray(NamedProperties, (o, v) => { o.WriteAsciiz(v.Item1); o.WriteAsciiz(v.Item2); });

			output.WriteArray(Frames, (o, v) => v.Write(o, version));
			output.Write(ColorTop);
			output.Write(Color);
			output.Write(Special);
			output.Write(VertexBoneRefIsSimple);

			var sizeOfRestDataPos = output.Position;
			output.Write((uint)0);

			if (version >= 50u)
			{
				output.WriteCondensedIntArray(Clip);
			}
			UvSets[0].Write(output, version);
			output.Write(UvSets.Length);

			for (int i = 1; i < UvSets.Length; ++i)
			{
				UvSets[i].Write(output, version);
			}

			output.WriteCompressedArray(Vertices, (o, v) => v.Write(o), 12);
			
			if (version >= 45u)
			{
				output.WriteCondensedArray(NormalsCompressed, (o, v) => v.Write(o), 4);
				output.WriteCompressedArray(STCoordsCompressed, (o, v) => { v.Item1.Write(o); v.Item2.Write(o); }, 8);
			}
			else
			{
				 output.WriteCondensedArray(Normals, (o, v) => v.Write(o), 12);
				 output.WriteCompressedArray(STCoords, (o, v) => { v.Item1.Write(o); v.Item2.Write(o); }, 24);
			}
			output.WriteCompressedArray(VertexBoneRef, (o, v) => v.Write(o), 12);
			output.WriteCompressedArray(NeighborBoneRef, (o, v) => v.Write(o), 32);
			if (version >= 67u)
			{
				output.Write(Unknown);
			}

			var endOfDataPosition = output.Position;
			var sizeOfRestData = endOfDataPosition - sizeOfRestDataPos - 4;
			output.Position = sizeOfRestDataPos;
			output.Write((uint)sizeOfRestData);
			output.Position = endOfDataPosition;
			if (version >= 68u)
			{
				output.Write(Unknown68);
			}
		}

        public IEnumerable<string> GetTextures()
        {
			return Textures
				.Concat(Materials.SelectMany(m => m.StageTextures).Select(st => st.Texture))
				.Concat(Materials.Where(m => m.StageTI != null).Select(m => m.StageTI.Texture))
				.Distinct();
        }

        public IEnumerable<string> GetMaterials()
        {
			return Materials.Select(m => m.MaterialName)
				.Concat(Sections.Where(s => s.MaterialIndex != -1).Select(s => s.Material))
				.Distinct();
        }
    }
}