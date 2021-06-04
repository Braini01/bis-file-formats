using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.WRP
{
    public struct GeographyInfo
    {
        private short info;

        public byte MinWaterDepth => (byte)(info & 0b11);
        public bool Full => ((info >> 2) & 0b1) > 0;
        public bool Forest => ((info >> 3) & 0b1) > 0;
        public bool Road => ((info >> 4) & 0b1) > 0;
        public byte MaxWaterDepth => (byte)((info >> 5) & 0b11);
        public byte HowManyObjects => (byte)((info >> 7) & 0b11);
        public byte HowManyHardObjects => (byte)((info >> 9) & 0b11);
        public byte Gradient => (byte)((info >> 11) & 0b111);
        public bool SomeRoadway => ((info >> 14) & 0b1) > 0;
        public bool SomeObjects => ((info >> 15) & 0b1) > 0;


        public static implicit operator short(GeographyInfo d)
        {
            return d.info;
        }

        public static implicit operator GeographyInfo(short d)
        {
            var g = new GeographyInfo
            {
                info = d
            };
            return g;
        }
    }
}