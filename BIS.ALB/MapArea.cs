using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BIS.ALB
{
    public class MapArea
    {
        public double X1;
        public double Y1;
        public double X2;
        public double Y2;

        public double Width => X2 - X1;
        public double Height => Y2 - Y1;

        public MapArea(BinaryReader input, bool readDouble = true)
        {
            if (readDouble)
            {
                X1 = input.ReadDouble();
                Y1 = input.ReadDouble();
                X2 = input.ReadDouble();
                Y2 = input.ReadDouble();
            }
            else
            {
                X1 = input.ReadSingle();
                Y1 = input.ReadSingle();
                X2 = input.ReadSingle();
                Y2 = input.ReadSingle();
            }
        }

        public override string ToString()
        {
            return $"{X1:0.###};{Y1:0.###};{X2:0.###};{Y2:0.###}";
        }
    }
}
