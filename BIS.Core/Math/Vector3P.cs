using System;
using System.Globalization;
using System.Numerics;
using BIS.Core.Streams;

namespace BIS.Core.Math
{
    public class Vector3P : IEquatable<Vector3P>
    {
        private Vector3 xyz;

        public float X
        {
            get { return xyz.X; }
            set { xyz.X = value; }
        }

        public float Y
        {
            get { return xyz.Y; }
            set { xyz.Y = value; }
        }

        public float Z
        {
            get { return xyz.Z; }
            set { xyz.Z = value; }
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
            xyz = new Vector3( x, y, z );
        }

        public Vector3P(Vector3 xyz)
        {
            this.xyz = xyz;
        }

        public float Length => xyz.Length();

        public Vector3 Vector3 => xyz;

        public float this[int i]
        {
            get
            {
                switch(i)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(i));
                }
            }

            set
            {
                switch (i)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(i));
                }
            }
        }

        public static Vector3P operator -(Vector3P a)
        {
            return new Vector3P(-a.xyz);
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
            return new Vector3P(a.xyz * b);
        }

        //Scalarproduct
        public static float operator *(Vector3P a, Vector3P b)
        {
            return Vector3.Dot(a.xyz, b.xyz);
        }

        public static Vector3P operator +(Vector3P a, Vector3P b)
        {
            return new Vector3P(a.xyz + b.xyz);
        }

        public static Vector3P operator -(Vector3P a, Vector3P b)
        {
            return new Vector3P(a.xyz - b.xyz);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Vector3P);
        }

        public override int GetHashCode()
        {
            return xyz.GetHashCode();
        }

        public bool Equals(Vector3P other)
        {
            if (other != null)
            {
                return xyz.Equals(other.xyz);
            }
            return false;
        }

        private static bool NearlyEquals(float f1, float f2)
        {
            return System.Math.Abs(f1 - f2) < 0.05;
        }

        public bool NearlyEquals(Vector3P other)
        {
            if (other != null)
            {
                return NearlyEquals(X, other.X) && NearlyEquals(Y, other.Y) && NearlyEquals(Z, other.Z);
            }
            return false;
        }

        public override string ToString()
        {
            return "{" + X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture) + "," + Z.ToString(CultureInfo.InvariantCulture) + "}";
        }

        public float Distance(Vector3P v)
        {
            return Vector3.Distance(xyz, v.xyz);
        }

        public void Normalize()
        {
            xyz = Vector3.Normalize(xyz);
        }


        public static Vector3P CrossProduct(Vector3P a, Vector3P b)
        {
            return new Vector3P(Vector3.Cross(a.xyz, b.xyz));
        }
    }
}
