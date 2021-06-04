using System;
using System.Globalization;

using BIS.Core.Streams;

namespace BIS.Core.Math
{
    public class Vector3P
    {
        private float[] xyz;

        public float X
        {
            get { return xyz[0]; }
            set { xyz[0] = value; }
        }

        public float Y
        {
            get { return xyz[1]; }
            set { xyz[1] = value; }
        }

        public float Z
        {
            get { return xyz[2]; }
            set { xyz[2] = value; }
        }

        public Vector3P() : this(0f) { }

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
            X = (float)(x * scaleFactor);
            Y = (float)(y * scaleFactor);
            Z = (float)(z * scaleFactor);
        }

        public Vector3P(float x, float y, float z)
        {
            xyz = new float[] { x, y, z };
        }

        public double Length => System.Math.Sqrt(X * X + Y * Y + Z * Z);

        public float this[int i]
        {
            get
            {
                return xyz[i];
            }

            set
            {
                xyz[i] = value;
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
            Vector3P p = obj as Vector3P;
            if (p == null)
            {
                return false;
            }

            return base.Equals(obj) && Equals(p);
        }

        //ToDo:
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(Vector3P other)
        {
            Func<float, float, bool> nearlyEqual = (f1, f2) => System.Math.Abs(f1 - f2) < 0.05;

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

    public class Vector3PCompressed
    {
        private int value;
        private const float scaleFactor = -1.0f / 511.0f;

        public float X
        {
            get
            {
                int x = value & 0x3FF;
                if (x > 511) x -= 1024;
                return x * scaleFactor;
            }
        }

        public float Y
        {
            get
            {
                int y = (value >> 10) & 0x3FF;
                if (y > 511) y -= 1024;
                return y * scaleFactor;
            }
        }

        public float Z
        {
            get
            {
                int z = (value >> 20) & 0x3FF;
                if (z > 511) z -= 1024;
                return z * scaleFactor;
            }
        }

        public static implicit operator Vector3P(Vector3PCompressed src)
        {
            int x = src.value & 0x3FF;
            int y = (src.value >> 10) & 0x3FF;
            int z = (src.value >> 20) & 0x3FF;
            if (x > 511) x -= 1024;
            if (y > 511) y -= 1024;
            if (z > 511) z -= 1024;

            return new Vector3P(x * scaleFactor, y *  scaleFactor, z * scaleFactor);
        }

        public static implicit operator int(Vector3PCompressed src)
        {
            return src.value;
        }

        public static implicit operator Vector3PCompressed(int src)
        {
            return new Vector3PCompressed(src);
        }

        public Vector3PCompressed(int value)
        {
            this.value = value;
        }
        public Vector3PCompressed(BinaryReaderEx input)
        {
            value = input.ReadInt32();
        }
    }
}
