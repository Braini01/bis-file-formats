using System.Collections.Generic;
using BIS.Core;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class NamedSelection
    {
        public NamedSelection(BinaryReaderEx input, int version)
        {
            Name = input.ReadAsciiz();
            SelectedFaces = LOD.ReadCompressedVertexIndexArray(input, version);
            Unused = input.ReadInt32();
            IsSectional = input.ReadBoolean();
            Sections = input.ReadCompressedIntArrayTracked();
            SelectedVertices = LOD.ReadCompressedVertexIndexArray(input, version);
            ExpectedSize = input.ReadInt32();
            SelectedVerticesWeights = input.ReadCompressedTracked((uint)ExpectedSize);
        }

        public NamedSelection(string name, bool isSectional, IEnumerable<int> sections)
        {
            Name = name;
            Sections = new TrackedArray<int>(sections);
            SelectedFaces = new TrackedArray<int>();
            SelectedVertices = new TrackedArray<int>();
            SelectedVerticesWeights = new TrackedArray<byte>();
            IsSectional = isSectional;
        }

        public string Name { get; }
        public TrackedArray<int> SelectedFaces { get; }
        public int Unused { get; }
        public bool IsSectional { get; }
        public TrackedArray<int> Sections { get; }
        public TrackedArray<int> SelectedVertices { get; }
        public int ExpectedSize { get; }
        public TrackedArray<byte> SelectedVerticesWeights { get; }

        internal void Write(BinaryWriterEx output, int version)
        {
            output.WriteAsciiz(Name);
            LOD.WriteCompressedVertexIndexArray(output, version, SelectedFaces);
            output.Write(Unused);
            output.Write(IsSectional);
            output.WriteCompressedIntArray(Sections);
            LOD.WriteCompressedVertexIndexArray(output, version, SelectedVertices);
            output.Write(ExpectedSize);
            output.WriteCompressed(SelectedVerticesWeights);
        }

    }
}