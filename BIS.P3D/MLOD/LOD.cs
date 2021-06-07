using BIS.Core.Math;
using BIS.Core.Streams;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BIS.P3D.MLOD
{
    public class P3DM_LOD
    {
        private int Flags { get; set; }
        public int Version { get; private set; }
        public Point[] Points { get; private set; }
        public Vector3P[] Normals { get; private set; }
        public Face[] Faces { get; private set; }
        public LinkedList<Tagg> Taggs { get; private set; }
        public float Resolution { get; private set; }

        public P3DM_LOD(BinaryReaderEx input)
        {
            Read(input);
        }

        public P3DM_LOD(float resolution, Point[] points, Vector3P[] normals, Face[] faces, IEnumerable<Tagg> taggs)
        {
            Version = 0x100;
            Points = points;
            Normals = normals;
            Faces = faces;
            Taggs = new LinkedList<Tagg>(taggs);
            Resolution = resolution;
        }

        public void Read(BinaryReaderEx input)
        {
            if (input.ReadAscii(4) != "P3DM")
                throw new ArgumentException("Only P3DM LODs are supported");

            var headerSize = input.ReadInt32();
            Version = input.ReadInt32();

            if (headerSize != 28 || Version != 0x100)
                throw new ArgumentOutOfRangeException("Unknown P3DM version");
            var nPoints = input.ReadInt32();
            var nNormals = input.ReadInt32();
            var nFaces = input.ReadInt32();

            Flags = input.ReadInt32();
            Points = new Point[nPoints];
            Normals = new Vector3P[nNormals];
            Faces = new Face[nFaces];
            for (int i = 0; i < nPoints; ++i)
            {
                Points[i] = new Point(input);
            }
            for (int i = 0; i < nNormals; ++i)
            {
                Normals[i] = new Vector3P(input);
            }
            for (int i = 0;  i < nFaces; ++i)
            {
                Faces[i] = new Face(input);
            }

            if (input.ReadAscii(4) != "TAGG")
                throw new FormatException("TAGG expected");

            Taggs = new LinkedList<Tagg>();
            Tagg mlodTagg;
            do
            {
                mlodTagg = Tagg.ReadTagg(input, nPoints, Faces);
                Taggs.AddLast(mlodTagg);
            }
            while (!(mlodTagg is EOFTagg));

            Resolution = input.ReadSingle();
        }

        public void Write(BinaryWriterEx output)
        {
            var nPoints = Points.Length;
            var nNormals = Normals.Length;
            var nFaces = Faces.Length;

            output.WriteAscii("P3DM", 4);
            output.Write(28); //headerSize
            output.Write(Version);
            output.Write(nPoints);
            output.Write(nNormals);
            output.Write(nFaces);
            output.Write(Flags);
            for (int i = 0; i < nPoints; ++i)
                Points[i].Write(output);
            for (int index = 0; index < nNormals; ++index)
                Normals[index].Write(output);
            for (int index = 0; index < nFaces; ++index)
                Faces[index].Write(output);
            output.WriteAscii("TAGG", 4);
            foreach (Tagg tagg in Taggs)
                tagg.Write(output);
            output.Write(Resolution);
        }
    }
}
