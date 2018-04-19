using BIS.Core.Math;
using BIS.Core.Streams;
using System;
using System.IO;

namespace BIS.P3D.MLOD
{
    [Flags]
    public enum PointFlags
    {
        NONE = 0,

        ONLAND = 0x1,
        UNDERLAND = 0x2,
        ABOVELAND = 0x4,
        KEEPLAND = 0x8,
        LAND_MASK = 0xf,

        DECAL = 0x100,
        VDECAL = 0x200,
        DECAL_MASK = 0x300,

        NOLIGHT = 0x10,
        AMBIENT = 0x20,
        FULLLIGHT = 0x40,
        HALFLIGHT = 0x80,
        LIGHT_MASK = 0xf0,

        NOFOG = 0x1000,
        SKYFOG = 0x2000,
        FOG_MASK = 0x3000,

        USER_MASK = 0xff0000,
        USER_STEP = 0x010000,

        SPECIAL_MASK = 0xf000000,
        SPECIAL_HIDDEN = 0x1000000,

        ALL_FLAGS = LAND_MASK | DECAL_MASK | LIGHT_MASK | FOG_MASK | USER_MASK | SPECIAL_MASK
    }

    public class Point
    {
        public Vector3P Position;
        public PointFlags PointFlags { get; private set; }

        public Point(Vector3P pos, PointFlags flags)
        {
            Position = pos;
            PointFlags = flags;
        }

        public Point(BinaryReaderEx input)
        {
            Position = new Vector3P(input);
            PointFlags = (PointFlags)input.ReadInt32();
        }

        public new void Write(BinaryWriterEx output)
        {
            Position.Write(output);
            output.Write((int)PointFlags);
        }
    }

    public class Vertex
    {
        public int PointIndex { get; private set; }
        public int NormalIndex { get; private set; }
        public float U { get; private set; }
        public float V { get; private set; }

        public Vertex(BinaryReaderEx input)
        {
            Read(input);
        }

        public Vertex(int point, int normal, float u, float v)
        {
            PointIndex = point;
            NormalIndex = normal;
            U = u;
            V = v;
        }

        public void Read(BinaryReaderEx input)
        {
            PointIndex = input.ReadInt32();
            NormalIndex = input.ReadInt32();
            U = input.ReadSingle();
            V = input.ReadSingle();
        }

        public void Write(BinaryWriter output)
        {
            output.Write(PointIndex);
            output.Write(NormalIndex);
            output.Write(U);
            output.Write(V);
        }
    }
}
