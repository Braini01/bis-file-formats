using BIS.Core.Math;
using BIS.Core.Streams;
using System;

namespace BIS.P3D.MLOD
{
    public abstract class Tagg
    {
        public string Name { get; set; }
        public uint DataSize { get; set; }

        protected Tagg(uint dataSize, string taggName)
        {
            Name = taggName;
            DataSize = dataSize;
        }

        protected Tagg(BinaryReaderEx input)
        {
            if (!input.ReadBoolean())
                throw new FormatException("Deactivated Tagg?");

            Name = input.ReadAsciiz();
            DataSize = input.ReadUInt32();
        }

        protected void WriteHeader(BinaryWriterEx output)
        {
            output.Write(true);
            output.WriteAsciiz(Name);
            output.Write(DataSize);
        }

        public abstract void Write(BinaryWriterEx output);

        public static Tagg ReadTagg(BinaryReaderEx input, int nPoints, Face[] faces)
        {
            if (!input.ReadBoolean())
                throw new Exception("Deactivated Tagg?");
            var taggName = input.ReadAsciiz();
            input.Position -= taggName.Length + 2;

            switch (taggName)
            {
                case "#SharpEdges#":
                    return new SharpEdgesTagg(input);
                case "#Property#":
                    return new PropertyTagg(input);
                case "#Mass#":
                    return new MassTagg(input);
                case "#UVSet#":
                    return new UVSetTagg(input, faces);
                case "#Lock#":
                    return new LockTagg(input, nPoints, faces.Length);
                case "#Selected#":
                    return new SelectedTagg(input, nPoints, faces.Length);
                case "#Animation#":
                    return new AnimationTagg(input);
                case "#EndOfFile#":
                    return new EOFTagg(input);
                default:
                    return new NamedSelectionTagg(input, nPoints, faces.Length);
            }
        }
    }

    public class AnimationTagg : Tagg
    {
        public float FrameTime { get; set; }
        public Vector3P[] FramePoints { get; set; }

        public AnimationTagg(float frameTime, Vector3P[] framePoints) : base((uint)(framePoints.Length * 4 + 4), "#Animation#")
        {
            FrameTime = frameTime;
            FramePoints = framePoints;
        }

        public AnimationTagg(BinaryReaderEx input) : base(input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            var num = (DataSize - 4) / 12;
            FrameTime = input.ReadSingle();
            FramePoints = new Vector3P[num];
            for (int i = 0; i < num; ++i)
                FramePoints[i] = new Vector3P(input);
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            output.Write(FrameTime);
            for (int index = 0; index < FramePoints.Length; ++index)
                FramePoints[index].Write(output);
        }
    }

    public class LockTagg : Tagg
    {
        public bool[] LockedPoints { get; private set; }
        public bool[] LockedFaces { get; private set; }

        public LockTagg(BinaryReaderEx input, int nPoints, int nFaces) : base(input)
        {
            Read(input, nPoints, nFaces);
        }

        public void Read(BinaryReaderEx input, int nPoints, int nFaces)
        {
            LockedPoints = new bool[nPoints];
            for (int index = 0; index < nPoints; ++index)
                LockedPoints[index] = input.ReadBoolean();
            LockedFaces = new bool[nFaces];
            for (int index = 0; index < nFaces; ++index)
                LockedFaces[index] = input.ReadBoolean();
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            for (int index = 0; index < LockedPoints.Length; ++index)
                output.Write(LockedPoints[index]);
            for (int index = 0; index < LockedFaces.Length; ++index)
                output.Write(LockedFaces[index]);
        }
    }

    public class MassTagg : Tagg
    {
        public float[] Mass { get; set; }

        public MassTagg(float[] mass): base((uint)(mass.Length * 4), "#Mass#")
        {
            Mass = mass;
        }

        public MassTagg(BinaryReaderEx input): base(input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            uint num = DataSize / 4;
            Mass = new float[num];
            for (int index = 0; index < num; ++index)
                Mass[index] = input.ReadSingle();
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            uint num = DataSize / 4;
            for (int index = 0; index < num; ++index)
                output.Write(Mass[index]);
        }
    }

    public class NamedSelectionTagg : Tagg
    {
        public byte[] Points { get; set; }
        public byte[] Faces { get; set; }

        public NamedSelectionTagg(string name, byte[] points, byte[] faces) : base((uint)(points.Length + faces.Length), name)
        {
            Points = points;
            Faces = faces;
        }

        public NamedSelectionTagg(BinaryReaderEx input, int nPoints, int nFaces) : base(input)
        {
            Read(input, nPoints, nFaces);
        }

        public void Read(BinaryReaderEx input, int nPoints, int nFaces)
        {
            Points = new byte[nPoints];
            for (int index = 0; index < nPoints; ++index)
                Points[index] = input.ReadByte();
            Faces = new byte[nFaces];
            for (int index = 0; index < nFaces; ++index)
                Faces[index] = input.ReadByte();
        }
        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            for (int index = 0; index < Points.Length; ++index)
                output.Write(Points[index]);
            for (int index = 0; index < Faces.Length; ++index)
                output.Write(Faces[index]);
        }
    }

    public class PropertyTagg : Tagg
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }

        public PropertyTagg(string prop, string val) : base(128,"#Property#")
        {
            PropertyName = prop;
            Value = val;
        }

        public PropertyTagg(BinaryReaderEx input) : base(input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            PropertyName = input.ReadAscii(64);
            Value = input.ReadAscii(64);
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            output.WriteAscii(PropertyName, 64);
            output.WriteAscii(Value, 64);
        }
    }
    public class SelectedTagg : Tagg
    {
        public byte[] WeightedPoints { get; set; }
        public byte[] Faces { get; set; }

        public SelectedTagg(BinaryReaderEx input, int nPoints, int nFaces) : base(input)
        {
            Read(input, nPoints, nFaces);
        }

        public void Read(BinaryReaderEx input, int nPoints, int nFaces)
        {
            WeightedPoints = new byte[nPoints];
            for (int index = 0; index < nPoints; ++index)
                WeightedPoints[index] = input.ReadByte();
            Faces = new byte[nFaces];
            for (int index = 0; index < nFaces; ++index)
                Faces[index] = input.ReadByte();
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            for (int index = 0; index < WeightedPoints.Length; ++index)
                output.Write(WeightedPoints[index]);
            for (int index = 0; index < Faces.Length; ++index)
                output.Write(Faces[index]);
        }
    }
    public class SharpEdgesTagg : Tagg
    {
        public int[,] PointIndices { get; private set; }

        public SharpEdgesTagg(BinaryReaderEx input) : base(input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            var num = DataSize / 8;
            PointIndices = new int[num, 2];
            for (int index = 0; index < num; ++index)
            {
                PointIndices[index, 0] = input.ReadInt32();
                PointIndices[index, 1] = input.ReadInt32();
            }
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            var num = DataSize / 8;
            for (int index = 0; index < num; ++index)
            {
                output.Write(PointIndices[index, 0]);
                output.Write(PointIndices[index, 1]);
            }
        }
    }

    public class UVSetTagg : Tagg
    {
        public int UvSetNr { get; set; }
        public float[][,] FaceUVs { get; set; }

        public UVSetTagg(uint dataSize, int uvNr, float[][,] uvs): base(dataSize, "#UVSet#")
        {
            UvSetNr = uvNr;
            FaceUVs = uvs;
        }

        public UVSetTagg(BinaryReaderEx input, Face[] faces) : base(input)
        {
            Read(input, faces);
        }

        public void Read(BinaryReaderEx input, Face[] faces)
        {
            UvSetNr = input.ReadInt32();
            FaceUVs = new float[faces.Length][,];
            for (int i = 0; i < faces.Length; ++i)
            {
                FaceUVs[i] = new float[faces[i].VertexCount, 2];
                for (int j = 0; j < faces[i].VertexCount; ++j)
                {
                    FaceUVs[i][j, 0] = input.ReadSingle();
                    FaceUVs[i][j, 1] = input.ReadSingle();
                }
            }
        }

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
            output.Write(UvSetNr);
            for (int i = 0; i < FaceUVs.Length; ++i)
            {
                for (int j = 0; j < FaceUVs[i].Length / 2; ++j)
                {
                    output.Write(FaceUVs[i][j, 0]);
                    output.Write(FaceUVs[i][j, 1]);
                }
            }
        }
    }

    public class EOFTagg : Tagg
    {
        public EOFTagg(): base(0, "#EndOfFile#") {}

        public EOFTagg(BinaryReaderEx input) : base(input) {}

        public override void Write(BinaryWriterEx output)
        {
            WriteHeader(output);
        }
    }
}
