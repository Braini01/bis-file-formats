using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.WRP
{
    public struct ObjectId
    {
        private int id;

        public bool IsObject => ((id >> 31) & 1) > 0;
        public short ObjId => (short)(id & 0b111_1111_1111);
        public short ObjX => (short)((id >> 11) & 0b11_1111_1111);
        public short ObjZ => (short)((id >> 21) & 0b11_1111_1111);

        public int Id => id;

        public static implicit operator int(ObjectId d)
        {
            return d.id;
        }

        public static implicit operator ObjectId(int d)
        {
            var o = new ObjectId
            {
                id = d
            };
            return o;
        }
    }
}
