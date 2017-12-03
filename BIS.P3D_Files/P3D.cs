using BIS.Core.Math;
using BIS.Core.Streams;
using System;
using System.IO;
using System.Linq;

namespace BIS.P3D
{
    //public abstract class P3D_LOD
    //{
    //    public float Resolution { get; protected set; }

    //    public string Name
    //    { 
    //        get { return Resolution.GetLODName(); }
    //    }

    //    public abstract Vector3P[] Points
    //    {
    //        get;
    //    }

    //    public abstract Vector3P[] NormalVectors
    //    {
    //        get;
    //    }

    //    public abstract string[] Textures
    //    {
    //        get;
    //    }

    //    public abstract string[] MaterialNames
    //    {
    //        get;
    //    }
    //}
   
    public static class P3D
    {
        //public int Version { get; protected set; }

        //public static P3D GetInstance(string fileName)
        //{
        //    return GetInstance(File.OpenRead(fileName));
        //}

        //public static P3D GetInstance(Stream stream)
        //{
        //    var binaryReader = new BinaryReaderEx(stream);
        //    var sig = binaryReader.ReadAscii(4);
        //    stream.Position -= 4;
        //    if (sig == "ODOL")
        //        return new ODOL.ODOL(stream);
        //    if (sig == "MLOD")
        //        return new MLOD.MLOD(stream);
        //    else
        //        throw new FormatException("Neither MLOD nor ODOL signature detected");
        //}

        public static bool IsODOL(string filePath)
        {
            return IsODOL(File.OpenRead(filePath));
        }

        public static bool IsODOL(Stream stream)
        {
            bool result = false;
            if (stream.ReadByte() == 'O'
            && stream.ReadByte() == 'D'
            && stream.ReadByte() == 'O'
            && stream.ReadByte() == 'L')
                result = true; ;

            stream.Position = 0;

            return result;
        }
        public static bool IsMLOD(string filePath)
        {
            return IsMLOD(File.OpenRead(filePath));
        }

        public static bool IsMLOD(Stream stream)
        {
            bool result = false;
            if (stream.ReadByte() == 'M'
            && stream.ReadByte() == 'L'
            && stream.ReadByte() == 'O'
            && stream.ReadByte() == 'D')
                result = true; ;

            stream.Position = 0;

            return result;
        }

        //public abstract P3D_LOD[] LODs { get; }

        //public virtual P3D_LOD GetLOD(float resolution)
        //{
        //    return LODs.FirstOrDefault(lod => lod.Resolution == resolution);
        //}

        //public abstract float Mass
        //{
        //    get;
        //}
    }
}
