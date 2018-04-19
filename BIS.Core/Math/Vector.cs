using System;
using System.Globalization;
using System.Runtime.InteropServices;
using BIS.Core.Streams;

namespace BIS.Core.Math
{
    public struct Vector3P
    {
        private float x;
        private float y;
        private float z;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        public Vector3P(float val) : this(val, val, val) { }

        public Vector3P(BinaryReaderEx input) : this(input.ReadSingle(), input.ReadSingle(), input.ReadSingle()) { }

        public Vector3P(int compressed) : this()
        {
            const double scaleFactor = -1.0 / 511;
            int x = compressed & 0x3FF;
            int y = (compressed >> 10) & 0x3FF;
            int z = (compressed >> 20) & 0x3FF;
            if (x > 511) x -= 1024;
            if (y > 511) y -= 1024;
            if (z > 511) z -= 1024;
            this.x = (float)(x * scaleFactor);
            this.y = (float)(y * scaleFactor);
            this.z = (float)(z * scaleFactor);
        }

        public Vector3P(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public double Length => System.Math.Sqrt(x * x + y * y + z * z);

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;

                    default: throw new ArgumentOutOfRangeException(nameof(i), i, "Index to Vector3P has to be 0, 1 or 2");
                }
            }

            set
            {
                switch (i)
                {
                    case 0: x = value;return;
                    case 1: y = value;return;
                    case 2: z = value;return;

                    default: throw new ArgumentOutOfRangeException(nameof(i), i, "Index to Vector3P has to be 0, 1 or 2");
                }
            }
        }

        public static Vector3P operator -(Vector3P a)
        {
            return new Vector3P(-a.X, -a.Y, -a.Z);
        }

        public void Write(BinaryWriterEx output)
        {
            output.Write(X);
            output.Write(Y);
            output.Write(Z);
        }

        //Scalarmultiplication
        public static Vector3P operator *(Vector3P a, float b)
        {
            return new Vector3P(a.X * b, a.Y * b, a.Z * b);
        }

        //Scalarproduct
        public static float operator *(Vector3P a, Vector3P b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector3P operator +(Vector3P a, Vector3P b)
        {
            return new Vector3P(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3P operator -(Vector3P a, Vector3P b)
        {
            return new Vector3P(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3P v)
            {
                return base.Equals(obj) && Equals(v);
            }

            return false;
        }

        //ToDo:
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(Vector3P other)
        {
            bool nearlyEqual(float f1, float f2) => System.Math.Abs(f1 - f2) < 0.05;

            return ( nearlyEqual(X, other.X) && nearlyEqual(Y, other.Y) && nearlyEqual(Z, other.Z));
        }

        public override string ToString()
        {
            CultureInfo cultureInfo = new CultureInfo("en-GB");
            return "{" + X.ToString(cultureInfo.NumberFormat) + "," + Y.ToString(cultureInfo.NumberFormat) + "," + Z.ToString(cultureInfo.NumberFormat) + "}";
        }

        public float Distance(Vector3P v)
        {
            var d = this - v;
            return (float)System.Math.Sqrt( d.X * d.X + d.Y * d.Y + d.Z * d.Z );
        }

        public void Normalize()
        {
            float l = (float)Length;
            X /= l;
            Y /= l;
            Z /= l;
        }

        public static Vector3P CrossProduct(Vector3P a, Vector3P b)
        {
            var x = a.Y * b.Z - a.Z * b.Y;
            var y = a.Z * b.X - a.X * b.Z;
            var z = a.X * b.Y - a.Y * b.X;

            return new Vector3P(x, y, z);
        }
    }

    public struct Vector3PCompressed
    {
        private const float ScaleFactor = -1.0f / 511.0f;

        private int xyz;
        

        //public float X
        //{
        //    get
        //    {
        //        int x = value & 0x3FF;
        //        if (x > 511) x -= 1024;
        //        return x * ScaleFactor;
        //    }
        //}

        //public float Y
        //{
        //    get
        //    {
        //        int y = (value >> 10) & 0x3FF;
        //        if (y > 511) y -= 1024;
        //        return y * ScaleFactor;
        //    }
        //}

        //public float Z
        //{
        //    get
        //    {
        //        int z = (value >> 20) & 0x3FF;
        //        if (z > 511) z -= 1024;
        //        return z * ScaleFactor;
        //    }
        //}

        public static implicit operator Vector3P(Vector3PCompressed src)
        {
            int x = src.xyz & 0x3FF;
            int y = (src.xyz >> 10) & 0x3FF;
            int z = (src.xyz >> 20) & 0x3FF;
            if (x > 511) x -= 1024;
            if (y > 511) y -= 1024;
            if (z > 511) z -= 1024;

            return new Vector3P(x * ScaleFactor, y *  ScaleFactor, z * ScaleFactor);
        }

        public static implicit operator int(Vector3PCompressed src)
        {
            return src.xyz;
        }

        public static implicit operator Vector3PCompressed(int src)
        {
            return new Vector3PCompressed(src);
        }

        public Vector3PCompressed(int value)
        {
            xyz = value;
        }
        public Vector3PCompressed(BinaryReaderEx input)
        {
            xyz = input.ReadInt32();
        }
    }
}
