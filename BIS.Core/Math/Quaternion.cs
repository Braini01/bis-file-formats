using System;

namespace BIS.Core.Math
{
    public class Quaternion
    {
        private float x, y, z, w;

        public float X => x;
        public float Y => y;
        public float Z => z;
        public float W => w;

        public static Quaternion ReadCompressed(byte[] data)
        {
            var x = (float)(-BitConverter.ToInt16(data, 0) / 16384d);
            var y = (float)(BitConverter.ToInt16(data, 2) / 16384d);
            var z = (float)(-BitConverter.ToInt16(data, 4) / 16384d);
            var w = (float)(BitConverter.ToInt16(data, 6) / 16384d);

            return new Quaternion(x, y, z, w);
        }

        public Quaternion()
        {
            w = 1f;
            x = 0f;
            y = 0f;
            z = 0f;
        }

        public Quaternion(float x, float y, float z, float w)
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            var w = (a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
            var x = (a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y);
            var y = (a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x);
            var z = (a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w);
            return new Quaternion(x, y, z, w);
        }

        public Quaternion Inverse
        {
            get
            {
                Normalize();
                return Conjugate;
            }
        }

        public Quaternion Conjugate => new Quaternion(-x, -y, -z, w);

        public void Normalize()
        {
            float n = (float)(1 / System.Math.Sqrt(x * x + y * y + z * z + w * w));
            x *= n;
            y *= n;
            z *= n;
            w *= n;
        }

        public Vector3P Transform(Vector3P xyz)
        {
            var vQ = new Quaternion(xyz.X, xyz.Y, xyz.Z, 0);
            var vQnew = this * vQ * Inverse;
            return new Vector3P(vQnew.x, vQnew.y, vQnew.z);
        }

        /// <summary>
        /// for unit quaternions only?
        /// </summary>
        /// <returns></returns>
        public float[,] AsRotationMatrix()
        {
            var rotMatrix = new float[3, 3];

            double xy = x * y;
            double wz = w * z;
            double wx = w * x;
            double wy = w * y;
            double xz = x * z;
            double yz = y * z;
            double zz = z * z;
            double yy = y * y;
            double xx = x * x;
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
