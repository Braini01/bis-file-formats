using BIS.Core.Math;
using BIS.Core.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.WRP
{
    public class Object
    {
        public int ObjectID { get; }
        public int ModelIndex { get; } // into the [[#Models|models path name list]] (1 based)
        public Matrix4P Transform { get; }
        public int ShapeParam { get; }

        public Object(BinaryReaderEx input)
        {
            ObjectID = input.ReadInt32();
            ModelIndex = input.ReadInt32();
            Transform = new Matrix4P(input);
            if (input.Version >= 14)
                ShapeParam = input.ReadInt32();
        }
    }
}
