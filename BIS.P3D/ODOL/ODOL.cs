using System;
using System.Collections.Generic;
using System.Text;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class ODOL : IReadObject
    {
        public int Version { get; private set; }
        public string Prefix { get; private set; }
        public int NoOfLods { get; private set; }
        public ModelInfo ModelInfo { get; private set; }
        public float[] Resolutions { get; private set; }
        public uint AppID { get; private set; }
        public string MuzzleFlash { get; private set; }
        public void Read(BinaryReaderEx input)
        {
            if (input.ReadAscii(4) != "ODOL")
                throw new FormatException("ODOL signature expected");

            ReadContent(input);
        }

        internal void ReadContent(BinaryReaderEx input)
        {
            Version = input.ReadInt32();

            if (Version >= 44)
            {
                input.UseLZOCompression = true;
            }
            if (Version >= 64)
            {
                input.UseCompressionFlag = true;
            }
            if (Version >= 59)
            {
                AppID = input.ReadUInt32();
            }
            if (Version >= 58)
            {
                MuzzleFlash = input.ReadAsciiz();
            }

            Resolutions = input.ReadFloatArray();

            NoOfLods = Resolutions.Length;

            ModelInfo = new ModelInfo(input, Version, NoOfLods);
        }
    }
}
