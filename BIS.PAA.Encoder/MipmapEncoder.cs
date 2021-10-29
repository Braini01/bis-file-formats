using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.PAA.Encoder
{
    internal class MipmapEncoder
    {
        public MipmapEncoder(byte[] mipmap, int width, int height, int offset)
        {
            PaaData = mipmap;
            Width = (ushort)width;
            WidthEncoded = Width;
            Height = (ushort)height;
            Offset = offset;

            if (Width >= 256 || Height >= 256)
            {
                PaaData = MiniLZO.MiniLZO.Compress(PaaData); // Less efficient than BI's ImageToPAA LZO lib, but works
                WidthEncoded = (ushort)(Width | 0x8000);
            }
        }

        public ushort Width { get; }
        public ushort WidthEncoded { get; }
        public ushort Height { get; }
        public int Offset { get; }
        public byte[] PaaData { get; }
        public int MipmapEntrySize => PaaData.Length + 7;
    }
}
