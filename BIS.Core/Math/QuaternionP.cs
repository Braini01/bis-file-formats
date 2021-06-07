using System;
using System.IO;
using System.Numerics;

namespace BIS.Core.Math
{
    public class QuaternionP
    {
        private System.Numerics.Quaternion quaternion;

        public float X => quaternion.X;
        public float Y => quaternion.Y;
        public float Z => quaternion.Z;
        public float W => quaternion.W;

        public static QuaternionP ReadCompressed(BinaryReader input)
        {
            var x = (float)(input.ReadInt16() / 16384d);
            var y = (float)(input.ReadInt16() / 16384d);
            var z = (float)(input.ReadInt16() / 16384d);
            var w = (float)(input.ReadInt16() / 16384d);

            return new QuaternionP(x, y, z, w);
        }

        public QuaternionP() 
            : this(System.Numerics.Quaternion.Identity)
        {
        }

        public QuaternionP(float x, float y, float z, float w)
            : this(new System.Numerics.Quaternion(x, y, z, w))
        {

        }

        public QuaternionP(System.Numerics.Quaternion quaternion)
        {
            this.quaternion = quaternion;
        }

        public static QuaternionP operator *(QuaternionP a, QuaternionP b)
        {
            return new QuaternionP(a.quaternion * b.quaternion);
        }

        public QuaternionP Inverse
        {
            get
            {
                Normalize();
                return Conjugate;
            }
        }

        public QuaternionP Conjugate => new QuaternionP(System.Numerics.Quaternion.Conjugate(quaternion));

        public void Normalize()
        {
            quaternion = System.Numerics.Quaternion.Normalize(quaternion);
        }

        public Vector3P Transform(Vector3P xyz)
        {
            return new Vector3P(System.Numerics.Vector3.Transform(xyz.Vector3, quaternion));
        }

        /// <summary>
        /// for unit quaternions only?
        /// </summary>
        /// <returns></returns>
        public Matrix4P ToRotationMatrix()
        {
            return new Matrix4P(Matrix4x4.CreateFromQuaternion(quaternion)); 
        }
    }
}
