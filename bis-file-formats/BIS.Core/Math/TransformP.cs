using System;
using System.Collections.Generic;
using System.Text;
using BIS.Core.Streams;

namespace BIS.Core.Math
{
    public class TransformP
    {
        private readonly QuaternionP quaternion;
        private readonly Vector3P vector;

        public TransformP(QuaternionP quaternion, Vector3P vector)
        {
            this.quaternion = quaternion;
            this.vector = vector;
        }

        public static TransformP Read(BinaryReaderEx input)
        {
            var quaternion = QuaternionP.ReadCompressed(input);
            var x = new ShortFloat(input.ReadUInt16());
            var y = new ShortFloat(input.ReadUInt16());
            var z = new ShortFloat(input.ReadUInt16());
            var vector = new Vector3P((float)x.DoubleValue, (float)y.DoubleValue, (float)z.DoubleValue);

            return new TransformP(quaternion, vector);
        }
    }
}
