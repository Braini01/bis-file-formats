using System;
using BIS.Core.Streams;

namespace BIS.Core.Math
{
    /// <summary>
    /// Layout:
    /// [m11, m12, m13, 0]
    /// [m21, m22, m23, 0]
    /// [m31, m32, m33, 0]
    /// [m41, m42, m43, 1]
    /// </summary>
    public class Matrix4P
    {
        private System.Numerics.Matrix4x4 matrix;

        public Matrix4P(BinaryReaderEx input) : this(
            new System.Numerics.Matrix4x4(
                input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), 0f,
                input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), 0f,
                input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), 0f,
                input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), 1f)
            ) { 
        }

        public Matrix4P(System.Numerics.Matrix4x4 matrix)
        {
            this.matrix = matrix;
        }

        public static Matrix4P operator *(Matrix4P a, Matrix4P b)
        {
            return new Matrix4P(a.matrix * b.matrix);
        }

        public void Write(BinaryWriterEx output)
        {
            if (matrix.M14 != 0f || matrix.M24 != 0f || matrix.M34 != 0f || matrix.M44 != 1f)
            {
                throw new InvalidOperationException();
            }
            output.Write(matrix.M11);
            output.Write(matrix.M12);
            output.Write(matrix.M13);
            output.Write(matrix.M21);
            output.Write(matrix.M22);
            output.Write(matrix.M23);
            output.Write(matrix.M31);
            output.Write(matrix.M32);
            output.Write(matrix.M33);
            output.Write(matrix.M41);
            output.Write(matrix.M42);
            output.Write(matrix.M43);
        }

        public System.Numerics.Matrix4x4 Matrix 
        { 
            get { return matrix; } 
            set { matrix = value; } 
        }

        public float Altitude
        {
            get { return matrix.M42; }
            set { matrix.M42 = value; }
        }

        public float AltitudeScale
        {
            get { return matrix.M22; }
            set { matrix.M22 = value; }
        }

        public override string ToString()
        {
            return matrix.ToString();
        }
    }
}
