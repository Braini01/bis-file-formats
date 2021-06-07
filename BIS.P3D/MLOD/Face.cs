using BIS.Core.Streams;
using System;

namespace BIS.P3D.MLOD
{
    [Flags]
    public enum FaceFlags
    {
        DEFAULT = 0,
        NOLIGHT = 0x1,
        AMBIENT = 0x2,
        FULLLIGHT = 0x4,
        BOTHSIDESLIGHT = 0x20,
        SKYLIGHT = 0x80,
        REVERSELIGHT = 0x100000,
        FLATLIGHT = 0x200000,
        LIGHT_MASK = 0x3000a7
    }

    public class Face
    {
        public int VertexCount { get; private set; }
        public Vertex[] Vertices { get; private set; }
        public FaceFlags Flags { get; private set; }
        public string Texture { get; private set; }
        public string Material { get; private set; }

        public Face(int nVerts, Vertex[] verts, FaceFlags flags, string texture, string material)
        {
            VertexCount = nVerts;
            Vertices = verts;
            Flags = flags;
            Texture = texture;
            Material = material;
        }

        public Face(BinaryReaderEx input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            VertexCount = input.ReadInt32();
            Vertices = new Vertex[4];
            for (int i = 0; i < 4; ++i)
            {
                Vertices[i] = new Vertex(input);
            }
            Flags = (FaceFlags)input.ReadInt32();
            Texture = input.ReadAsciiz();
            Material = input.ReadAsciiz();
        }

        public void Write(BinaryWriterEx output)
        {
            output.Write(VertexCount);
            for (int i = 0; i < 4; ++i)
                if (i < Vertices.Length && Vertices[i] != null)
                    Vertices[i].Write(output);
                else
                {
                    output.Write(0);
                    output.Write(0);
                    output.Write(0);
                    output.Write(0);
                }

            output.Write((int)Flags);
            output.WriteAsciiz(Texture);
            output.WriteAsciiz(Material);
        }
    }
}
