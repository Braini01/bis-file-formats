using System;
using System.IO;

namespace BIS.Core.Math
{
    public class Quaternion
    {
        private System.Numerics.Quaternion quaternion;

        public float X => quaternion.X;
        public float Y => quaternion.Y;
        public float Z => quaternion.Z;
        public float W => quaternion.W;

        public static Quaternion ReadCompressed(BinaryReader input)
        {
            var x = (float)(-input.ReadInt16() / 16384d);
            var y = (float)(input.ReadInt16() / 16384d);
            var z = (float)(-input.ReadInt16() / 16384d);
            var w = (float)(input.ReadInt16() / 16384d);

            return new Quaternion(x, y, z, w);
        }

        public Quaternion() 
            : this(System.Numerics.Quaternion.Identity)
        {
        }

        public Quaternion(float x, float y, float z, float w)
            : this(new System.Numerics.Quaternion(x, y, z, w))
        {

        }

        public Quaternion(System.Numerics.Quaternion quaternion)
        {
            this.quaternion = quaternion;
        }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.quaternion * b.quaternion);
        }

        public Quaternion Inverse
        {
            get
            {
                Normalize();
                return Conjugate;
            }
        }

        public Quaternion Conjugate => new Quaternion(System.Numerics.Quaternion.Conjugate(quaternion));

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
        public Matrix3P AsRotationMatrix()
        {
            // var matrix = System.Numerics.Matrix4x4.CreateFromQuaternion(quaternion);

            var rotMatrix = new Matrix3P();

            double xy = X * Y;
            double wz = W * Z;
            double wx = W * X;
            double wy = W * Y;
            double xz = X * Z;
            double yz = Y * Z;
            double zz = Z * Z;
            double yy = Y * Y;
            double xx = X * X;
            rotMatrix[0, 0] = (float)(1 - 2 * (yy + zz));	//1-2y2-2z2// need .997
            rotMatrix[0, 1] = (float)(2 * (xy - wz));			//2xy-2wz     -0.033  
            rotMatrix[0, 2] = (float)(2 * (xz + wy));   ////  2xz+2wy//0.063
            rotMatrix[1, 0] = (float)(2 * (xy + wz));     //2xy+2wz  0.024      
            rotMatrix[1, 1] = (float)(1 - 2 * (xx + zz)); //1-2x2-2z2
            rotMatrix[1, 2] = (float)(2 * (yz - wx));        //2yz+2wx////////////////
            rotMatrix[2, 0] = (float)(2 * (xz - wy));     //2xz-2wy
            rotMatrix[2, 1] = (float)(2 * (yz + wx));		//2yz-2wx/////////
            rotMatrix[2, 2] = (float)(1 - 2 * (xx + yy));   //1-2x2-2y2

            return rotMatrix;
        }
    }
}
