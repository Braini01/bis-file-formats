using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace BIS.PAA
{
    public static class PixelFormatConversion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetColor(byte[] img, int offset, byte a, byte r, byte g, byte b)
        {
            img[offset] = b;
            img[offset + 1] = g;
            img[offset + 2] = r;
            img[offset + 3] = a;
        }

        public static byte[] ARGB16ToARGB32(byte[] src)
        {
            var dst = new byte[src.Length * 2];
            var nPixel = src.Length / 2;
            for (int index = 0; index < nPixel; ++index)
            {
                byte hbyte = src[index * 2 + 1];
                byte lbyte = src[index * 2];
                byte lhbyte = (byte)(hbyte & 0x0F);
                byte hhbyte = (byte)((hbyte & 0xF0) >> 4);
                byte llbyte = (byte)(lbyte & 0x0F);
                byte hlbyte = (byte)((lbyte & 0xF0) >> 4);
                byte b = (byte)(lhbyte * byte.MaxValue / 15);
                byte a = (byte)(hhbyte * byte.MaxValue / 15);
                byte r = (byte)(llbyte * byte.MaxValue / 15);
                byte g = (byte)(hlbyte * byte.MaxValue / 15);

                SetColor(dst, index * 4, a, r, g, b);
            }

            return dst;
        }

        public static byte[] ARGB1555ToARGB32(byte[] src)
        {
            var dst = new byte[src.Length * 2];
            var nPixel = src.Length / 2;
            for (int index = 0; index < nPixel; ++index)
            {
                ushort s = BitConverter.ToUInt16(src, index * 2);
                bool abit = ((s & 0x8000) >> 15) == 1;
                byte b5bit = (byte)((s & 0x7C00) >> 10);
                byte g5bit = (byte)((s & 0x03E0) >> 5);
                byte r5bit = (byte)(s & 0x001F);
                byte b = (byte)(b5bit * byte.MaxValue / 31);
                byte a = (abit) ? byte.MaxValue : (byte)0;
                byte r = (byte)(r5bit * byte.MaxValue / 31);
                byte g = (byte)(g5bit * byte.MaxValue / 31);

                SetColor(dst, index * 4, a, r, g, b);
            }

            return dst;
        }

        public static byte[] AI88ToARGB32(byte[] src)
        {
            var dst = new byte[src.Length * 2];
            var nPixel = src.Length / 2;
            for (int index = 0; index < nPixel; ++index)
            {
                byte grey = src[index * 2];
                byte alpha = src[index * 2 + 1];

                SetColor(dst, index * 4, alpha, grey, grey, grey);
            }

            return dst;
        }

        public static byte[] P8ToARGB32(byte[] src, Palette palette)
        {
            var dst = new byte[src.Length * 4];
            var colors = palette.Colors;
            var nPixel = src.Length;
            for (int index = 0; index < nPixel; ++index)
            {
                var color = colors[src[index]];
                SetColor(dst, index * 4, color.A8, color.R8, color.G8, color.B8);
            }

            return dst;
        }

        internal static byte[] DXTToARGB32(byte[] imageData, int width, int height, int dxtType)
        {
            using (MemoryStream imageStream = new MemoryStream(imageData))
                return DXTToARGB32(imageStream, width, height, dxtType);
        }

        internal static byte[] DXTToARGB32(Stream imageStream, int width, int height, int dxtType)
        {
            Action<BinaryReader, int, int, int, int, int, byte[]> DecompressDXTBlock;
            switch (dxtType)
            {
                case 1:
                    DecompressDXTBlock = DecompressDxt1Block;
                    break;
                case 2:
                case 3:
                    DecompressDXTBlock = DecompressDxt3Block;
                    break;
                case 4:
                case 5:
                    DecompressDXTBlock = DecompressDxt5Block;
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(dxtType), dxtType, "Value needs to be within interval 1-5");
            }

            byte[] imageData = new byte[width * height * 4];
            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDXTBlock(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        private static void DecompressDxt1Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    if (c0 > c1)
                    {
                        switch (index)
                        {
                            case 0:
                                r = r0;
                                g = g0;
                                b = b0;
                                break;
                            case 1:
                                r = r1;
                                g = g1;
                                b = b1;
                                break;
                            case 2:
                                r = (byte)((2 * r0 + r1) / 3);
                                g = (byte)((2 * g0 + g1) / 3);
                                b = (byte)((2 * b0 + b1) / 3);
                                break;
                            case 3:
                                r = (byte)((r0 + 2 * r1) / 3);
                                g = (byte)((g0 + 2 * g1) / 3);
                                b = (byte)((b0 + 2 * b1) / 3);
                                break;
                        }
                    }
                    else
                    {
                        switch (index)
                        {
                            case 0:
                                r = r0;
                                g = g0;
                                b = b0;
                                break;
                            case 1:
                                r = r1;
                                g = g1;
                                b = b1;
                                break;
                            case 2:
                                r = (byte)((r0 + r1) / 2);
                                g = (byte)((g0 + g1) / 2);
                                b = (byte)((b0 + b1) / 2);
                                break;
                            case 3:
                                r = 0;
                                g = 0;
                                b = 0;
                                a = 0;
                                break;
                        }
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        SetColor(imageData, offset, a, r, g, b);
                    }
                }
            }
        }

        private static void DecompressDxt3Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            byte a0 = imageReader.ReadByte();
            byte a1 = imageReader.ReadByte();
            byte a2 = imageReader.ReadByte();
            byte a3 = imageReader.ReadByte();
            byte a4 = imageReader.ReadByte();
            byte a5 = imageReader.ReadByte();
            byte a6 = imageReader.ReadByte();
            byte a7 = imageReader.ReadByte();

            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            int alphaIndex = 0;
            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 0;

                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    switch (alphaIndex)
                    {
                        case 0:
                            a = (byte)((a0 & 0x0F) | ((a0 & 0x0F) << 4));
                            break;
                        case 1:
                            a = (byte)((a0 & 0xF0) | ((a0 & 0xF0) >> 4));
                            break;
                        case 2:
                            a = (byte)((a1 & 0x0F) | ((a1 & 0x0F) << 4));
                            break;
                        case 3:
                            a = (byte)((a1 & 0xF0) | ((a1 & 0xF0) >> 4));
                            break;
                        case 4:
                            a = (byte)((a2 & 0x0F) | ((a2 & 0x0F) << 4));
                            break;
                        case 5:
                            a = (byte)((a2 & 0xF0) | ((a2 & 0xF0) >> 4));
                            break;
                        case 6:
                            a = (byte)((a3 & 0x0F) | ((a3 & 0x0F) << 4));
                            break;
                        case 7:
                            a = (byte)((a3 & 0xF0) | ((a3 & 0xF0) >> 4));
                            break;
                        case 8:
                            a = (byte)((a4 & 0x0F) | ((a4 & 0x0F) << 4));
                            break;
                        case 9:
                            a = (byte)((a4 & 0xF0) | ((a4 & 0xF0) >> 4));
                            break;
                        case 10:
                            a = (byte)((a5 & 0x0F) | ((a5 & 0x0F) << 4));
                            break;
                        case 11:
                            a = (byte)((a5 & 0xF0) | ((a5 & 0xF0) >> 4));
                            break;
                        case 12:
                            a = (byte)((a6 & 0x0F) | ((a6 & 0x0F) << 4));
                            break;
                        case 13:
                            a = (byte)((a6 & 0xF0) | ((a6 & 0xF0) >> 4));
                            break;
                        case 14:
                            a = (byte)((a7 & 0x0F) | ((a7 & 0x0F) << 4));
                            break;
                        case 15:
                            a = (byte)((a7 & 0xF0) | ((a7 & 0xF0) >> 4));
                            break;
                    }
                    ++alphaIndex;

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;
                            break;
                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;
                            break;
                        case 2:
                            r = (byte)((2 * r0 + r1) / 3);
                            g = (byte)((2 * g0 + g1) / 3);
                            b = (byte)((2 * b0 + b1) / 3);
                            break;
                        case 3:
                            r = (byte)((r0 + 2 * r1) / 3);
                            g = (byte)((g0 + 2 * g1) / 3);
                            b = (byte)((b0 + 2 * b1) / 3);
                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        SetColor(imageData, offset, a, r, g, b);
                    }
                }
            }
        }

        private static void DecompressDxt5Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            byte alpha0 = imageReader.ReadByte();
            byte alpha1 = imageReader.ReadByte();

            ulong alphaMask = imageReader.ReadByte();
            alphaMask += (ulong)imageReader.ReadByte() << 8;
            alphaMask += (ulong)imageReader.ReadByte() << 16;
            alphaMask += (ulong)imageReader.ReadByte() << 24;
            alphaMask += (ulong)imageReader.ReadByte() << 32;
            alphaMask += (ulong)imageReader.ReadByte() << 40;

            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    uint alphaIndex = (uint)((alphaMask >> 3 * (4 * blockY + blockX)) & 0x07);
                    if (alphaIndex == 0)
                    {
                        a = alpha0;
                    }
                    else if (alphaIndex == 1)
                    {
                        a = alpha1;
                    }
                    else if (alpha0 > alpha1)
                    {
                        a = (byte)(((8 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 7);
                    }
                    else if (alphaIndex == 6)
                    {
                        a = 0;
                    }
                    else if (alphaIndex == 7)
                    {
                        a = 0xff;
                    }
                    else
                    {
                        a = (byte)(((6 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 5);
                    }

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;
                            break;
                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;
                            break;
                        case 2:
                            r = (byte)((2 * r0 + r1) / 3);
                            g = (byte)((2 * g0 + g1) / 3);
                            b = (byte)((2 * b0 + b1) / 3);
                            break;
                        case 3:
                            r = (byte)((r0 + 2 * r1) / 3);
                            g = (byte)((g0 + 2 * g1) / 3);
                            b = (byte)((b0 + 2 * b1) / 3);
                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        SetColor(imageData, offset, a, r, g, b);
                    }
                }
            }
        }

        private static void ConvertRgb565ToRgb888(ushort color, out byte r, out byte g, out byte b)
        {
            int temp;

            temp = (color >> 11) * 255 + 16;
            r = (byte)((temp / 32 + temp) / 32);
            temp = ((color & 0x07E0) >> 5) * 255 + 32;
            g = (byte)((temp / 64 + temp) / 64);
            temp = (color & 0x001F) * 255 + 16;
            b = (byte)((temp / 32 + temp) / 32);
        }
    }
}
