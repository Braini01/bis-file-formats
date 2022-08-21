using System.Globalization;
using System.Diagnostics;

using BIS.Core.Streams;

namespace BIS.Core
{
    public struct ColorP
    {
        public float Red { get; private set; }
        public float Green { get; private set; }
        public float Blue { get; private set; }
        public float Alpha { get; private set; }

        public ColorP(float r, float g, float b, float a)
        {
            Red = r;
            Green = g;
            Blue = b;
            Alpha = a;
        }
        public ColorP(BinaryReaderEx input)
        {
            Red = input.ReadSingle();
            Green = input.ReadSingle();
            Blue = input.ReadSingle();
            Alpha = input.ReadSingle();
        }

        public void Read(BinaryReaderEx input)
        {
            Red = input.ReadSingle();
            Green = input.ReadSingle();
            Blue = input.ReadSingle();
            Alpha = input.ReadSingle();
        }

        public void Write(BinaryWriterEx output)
        {
            output.Write(Red);
            output.Write(Green);
            output.Write(Blue);
            output.Write(Alpha);
        }

        public override string ToString()
        {
            CultureInfo cultureInfo = new CultureInfo("en-GB");
            return "{" + Red.ToString(cultureInfo.NumberFormat) + "," + Green.ToString(cultureInfo.NumberFormat) + "," + this.Blue.ToString(cultureInfo.NumberFormat) + "," + this.Alpha.ToString(cultureInfo.NumberFormat) + "}";
        }
    }

    public struct PackedColor
    {
        private uint value;

        public byte A8 => (byte)((value >> 24) & 0xff);
        public byte R8 => (byte)((value >> 16) & 0xff);
        public byte G8 => (byte)((value >>  8) & 0xff);
        public byte B8 => (byte)((value      ) & 0xff);

        public PackedColor(uint value)
        {
            this.value = value;
        }

        public void Write(BinaryWriterEx output)
        {
            output.Write(value);
        }

        public PackedColor(byte r, byte g, byte b, byte a=255)
        {
            value = PackColor(r, g, b, a);
        }

        public PackedColor(float r, float g, float b, float a)
        {
            Debug.Assert(r <= 1.0f && r >= 0 && !float.IsNaN(r));
            Debug.Assert(g <= 1.0f && g >= 0 && !float.IsNaN(g));
            Debug.Assert(b <= 1.0f && b >= 0 && !float.IsNaN(b));
            Debug.Assert(a <= 1.0f && a >= 0 && !float.IsNaN(a));

            byte r8 = (byte)(r * 255);
            byte g8 = (byte)(g * 255);
            byte b8 = (byte)(b * 255);
            byte a8 = (byte)(a * 255);

            value = PackColor(r8, g8, b8, a8);
        }

        internal static uint PackColor(byte r, byte g, byte b, byte a)
        {
            return (uint)(a << 24 | r << 16 | g << 8) | b;
        }
    }
}
