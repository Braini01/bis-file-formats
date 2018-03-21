using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using BIS.Core.Streams;

namespace BIS.PAA
{
    public enum TexSwizzle
    {
        TSAlpha,
        TSRed,
        TSGreen,
        TSBlue,
        TSInvAlpha,
        TSInvRed,
        TSInvGreen,
        TSInvBlue,
        TSOne
    }

    public enum PAAType
    {
        DXT1 = 1,
        DXT2 = 2,
        DXT3 = 3,
        DXT4 = 4,
        DXT5 = 5,
        RGBA_5551,
        RGBA_4444,
        AI88,
        RGBA_8888,
        P8, //8bit index to palette

        UNDEFINED = -1
    }

    public class PAA
    {
        private List<Mipmap> mipmaps;
        private int[] mipmapOffsets = new int[16];

        public PAAType Type { get; private set; } = PAAType.UNDEFINED;
        public Palette Palette { get; private set; }

        public int Width => mipmaps[0].Width;
        public int Height => mipmaps[0].Height;


        public PAA(string file) : this(File.OpenRead(file), !file.EndsWith(".pac")) { }
        public PAA(Stream stream, bool isPac = false) : this(new BinaryReaderEx(stream), isPac) { }

        public PAA(BinaryReaderEx stream, bool isPac = false)
        {
            Read(stream, isPac);
        }

        public Mipmap this[int i] => mipmaps[i];

        private static PAAType MagicNumberToType(ushort magic)
        {
            switch (magic)
            {
                case 0x4444: return PAAType.RGBA_4444;
                case 0x8080: return PAAType.AI88;
                case 0x1555: return PAAType.RGBA_5551;
                case 0xff01: return PAAType.DXT1;
                case 0xff02: return PAAType.DXT2;
                case 0xff03: return PAAType.DXT3;
                case 0xff04: return PAAType.DXT4;
                case 0xff05: return PAAType.DXT5;
            }

            return PAAType.UNDEFINED;
        }

        private void Read(BinaryReaderEx input, bool isPac = false)
        {
            var magic = input.ReadUInt16();
            var type = MagicNumberToType(magic);
            if (type == PAAType.UNDEFINED)
            {
                type = (!isPac) ? PAAType.RGBA_4444 : PAAType.P8;
                input.Position -= 2;
            }
            Type = type;

            Palette = new Palette(type);
            Palette.Read(input, mipmapOffsets);
            
            mipmaps = new List<Mipmap>(16);
            int i = 0;
            while (input.ReadUInt32() != 0)
            {
                input.Position -= 4;

                Debug.Assert(input.Position == mipmapOffsets[i]);
                var mipmap = new Mipmap(input, mipmapOffsets[i++]);
                mipmaps.Add(mipmap);
            }
            if (input.ReadUInt16() != 0)
                throw new FormatException("Expected two more zero's at end of file.");
        }

        public static byte[] GetARGB32PixelData(Stream paaStream, bool isPac = false, int mipmapIndex = 0)
        {
            var paa = new PAA(paaStream, isPac);
            return GetARGB32PixelData(paa, paaStream, mipmapIndex);
        }

        public static byte[] GetARGB32PixelData(PAA paa, Stream paaStream, int mipmapIndex = 0)
        {
            var input = new BinaryReaderEx(paaStream);

            Mipmap mipmap = paa[mipmapIndex];
            var rawData = mipmap.GetRawPixelData(input, paa.Type);

            switch (paa.Type)
            {
                case PAAType.RGBA_8888:
                case PAAType.P8:
                    return PixelFormatConversion.P8ToARGB32(rawData, paa.Palette);
                case PAAType.DXT1:
                case PAAType.DXT2:
                case PAAType.DXT3:
                case PAAType.DXT4:
                case PAAType.DXT5:
                    return PixelFormatConversion.DXTToARGB32(rawData, mipmap.Width, mipmap.Height, (int)paa.Type);
                case PAAType.RGBA_4444:
                    return PixelFormatConversion.ARGB16ToARGB32(rawData);
                case PAAType.RGBA_5551:
                    return PixelFormatConversion.ARGB1555ToARGB32(rawData);
                case PAAType.AI88:
                    return PixelFormatConversion.AI88ToARGB32(rawData);
                default:
                    throw new Exception($"Cannot retrieve pixel data from this PaaType: {paa.Type}");
            }


            //only for PAA and if it is no PI8 format
            if (_argb)
            {
                TexSwizzle invSwizzle[4] = { TSAlpha, TSRed, TSGreen, TSBlue };
                InvertSwizzle(invSwizzle, swizzle, 0);
                InvertSwizzle(invSwizzle, swizzle, 1);
                InvertSwizzle(invSwizzle, swizzle, 2);
                InvertSwizzle(invSwizzle, swizzle, 3);
                ChannelSwizzle(invSwizzle);
            }

        }

        public static void InvertSwizzle(TexSwizzle[] invSwizzle, TexSwizzle[] swizzle, int ch)
        {
            TexSwizzle swiz = TexSwizzle.TSAlpha + ch;
            if (swizzle[ch] >= TexSwizzle.TSInvAlpha && swizzle[ch] <= TexSwizzle.TSInvBlue)
            {
                invSwizzle[swizzle[ch] - TexSwizzle.TSInvAlpha] = TexSwizzle.TSInvAlpha - TexSwizzle.TSAlpha + swiz;
            }
            else if (swizzle[ch] <= TexSwizzle.TSBlue)
            {
                invSwizzle[(int)swizzle[ch]] = swiz;
            }
        }

        public static void CheckInvSwizzle(TexSwizzle swiz, out int offset, out int mulA, out int addA)
        {
            if (swiz == TexSwizzle.TSOne)
            {
                // one - ignore input (mul by 0) and set it to one (add 255)
                mulA = 0;
                addA = 255;
                offset = 0;
                return;
            }
            mulA = 1;
            addA = 0;
            switch (swiz)
            {
                case TexSwizzle.TSInvAlpha: swiz = TexSwizzle.TSAlpha; mulA = -1; addA = 255; break;
                case TexSwizzle.TSInvRed: swiz = TexSwizzle.TSRed; mulA = -1; addA = 255; break;
                case TexSwizzle.TSInvGreen: swiz = TexSwizzle.TSGreen; mulA = -1; addA = 255; break;
                case TexSwizzle.TSInvBlue: swiz = TexSwizzle.TSBlue; mulA = -1; addA = 255; break;
            }
            offset = swiz < TexSwizzle.TSOne ? 24 - (int)swiz * 8 : 0;
        }

        public void ChannelSwizzle(TexSwizzle[] channelSwizzle, byte[] argbPixels, int w, int h)
        {
            if (channelSwizzle[0] == TexSwizzle.TSAlpha && channelSwizzle[1] == TexSwizzle.TSRed &&
              channelSwizzle[2] == TexSwizzle.TSGreen && channelSwizzle[3] == TexSwizzle.TSBlue)
            {
                return;
            }

            int nPixel = w * h;
            CheckInvSwizzle(channelSwizzle[0], out int aOffset, out int mulA, out int addA);
            CheckInvSwizzle(channelSwizzle[1], out int rOffset, out int mulR, out int addR);
            CheckInvSwizzle(channelSwizzle[2], out int gOffset, out int mulG, out int addG);
            CheckInvSwizzle(channelSwizzle[3], out int bOffset, out int mulB, out int addB);
  
            while (--nPixel>=0)
            {
               
                int p = argbPixels;
                int a = (p >> aOffset) & 0xff;
                int r = (p >> rOffset) & 0xff;
                int g = (p >> gOffset) & 0xff;
                int b = (p >> bOffset) & 0xff;
                *pix++ = MakeARGB(a* mulA+addA, r* mulR+addR, g* mulG+addG, b* mulB+addB);
            }
        }


    }
}