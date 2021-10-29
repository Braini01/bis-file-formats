using System;
using System.Collections.Generic;
using System.IO;
using BCnEncoder.Shared;
using BIS.Core.Streams;
using Microsoft.Toolkit.HighPerformance;

namespace BIS.PAA.Encoder
{
    public static class PaaEncoder
    {
        public static void WritePAA(string file, ColorRgba32[,] image, PAAType type = PAAType.UNDEFINED, PAAFlags flags = PAAFlags.InterpolatedAlpha)
        {
            using (var writer = new BinaryWriterEx(File.Create(file)))
            {
                WritePAA(writer, image, type, flags);
            }
        }

        public static void WritePAA(BinaryWriterEx writer, ColorRgba32[,] image, PAAType type = PAAType.UNDEFINED, PAAFlags flags = PAAFlags.InterpolatedAlpha)
        {
            var max = new ColorRgba32(0, 0, 0, 0);
            ulong sumR = 0;
            ulong sumG = 0;
            ulong sumB = 0;
            ulong sumA = 0;
            byte minA = 255;
            ulong totalPixels = 0;
            for (int y = 0; y < image.GetLength(0); ++y)
            {
                for (int x = 0; x < image.GetLength(1); ++x)
                {
                    var color = image[y,x];
                    max.r = Math.Max(color.r, max.r);
                    max.g = Math.Max(color.g, max.g);
                    max.b = Math.Max(color.b, max.b);
                    max.a = Math.Max(color.a, max.a);
                    minA = Math.Min(color.a, minA);
                    if (color.a > 128)
                    {
                        sumR += color.r;
                        sumG += color.g;
                        sumB += color.b;
                        sumA += color.a;
                        totalPixels++;
                    }
                }
            }
            var avg = new ColorRgba32((byte)(sumR / totalPixels),(byte)(sumG / totalPixels),(byte)(sumB / totalPixels),(byte)(sumA / totalPixels));

            if (type == PAAType.UNDEFINED)
            {
                if (minA == 255)
                {
                    type = PAAType.DXT1;
                }
                else
                {
                    type = PAAType.DXT5;
                }
            }

            WritePAA(writer, new ReadOnlyMemory2D<ColorRgba32>(image), max, avg, type, flags);
        }

        public static void WritePAA(BinaryWriterEx writer, ReadOnlyMemory2D<ColorRgba32> image, ColorRgba32 max, ColorRgba32 avg, PAAType type = PAAType.DXT5, PAAFlags flags = PAAFlags.InterpolatedAlpha)
        {
            if (type != PAAType.DXT5 && type != PAAType.DXT1)
            {
                throw new NotSupportedException();
            }

            var enc = new BCnEncoder.Encoder.BcEncoder(type == PAAType.DXT5 ? CompressionFormat.Bc3 : CompressionFormat.Bc1);

            var mipmaps = new List<MipmapEncoder>();

            var width = image.Width;
            var height = image.Width;
            var offset = type == PAAType.DXT5 ? 128 : 112;
            foreach (var mipmap in enc.EncodeToRawBytes(image))
            {
                if (width > 2 || height > 2)
                {
                    var menc = new MipmapEncoder(mipmap, width, height, offset);
                    offset += menc.MipmapEntrySize;
                    mipmaps.Add(menc);
                    width = width / 2;
                    height = height / 2;
                }
            }

            writer.Write(type == PAAType.DXT5 ? (ushort)0xff05 : (ushort)0xff01);

            writer.WriteAscii("GGATCGVA", 8);
            writer.Write((uint)4);
            writer.Write(avg.r);
            writer.Write(avg.g);
            writer.Write(avg.b);
            writer.Write(avg.a);

            writer.WriteAscii("GGATCXAM", 8);
            writer.Write((uint)4);
            writer.Write(max.r);
            writer.Write(max.g);
            writer.Write(max.b);
            writer.Write(max.a);

            if (type == PAAType.DXT5)
            {
                writer.WriteAscii("GGATGALF", 8);
                writer.Write((uint)4);
                writer.Write((uint)flags);
            }

            writer.WriteAscii("GGATSFFO", 8);
            writer.Write(16 * 4);
            for (int i = 0; i < 16; ++i )
            {
                if (i < mipmaps.Count)
                {
                    writer.Write((uint)mipmaps[i].Offset);
                }
                else
                {
                    writer.Write((uint)0);
                }
            }

            writer.Write((ushort)0x0000);

            int index = 0;
            foreach (var mipmap in mipmaps)
            {
                if (writer.Position != mipmap.Offset)
                {
                    throw new Exception($"Wrong offset @{mipmap.Width} : {writer.Position} != { mipmap.Offset}");
                }
                writer.Write(mipmap.WidthEncoded);
                writer.Write(mipmap.Height);
                writer.WriteUInt24((uint)mipmap.PaaData.Length);
                writer.Write(mipmap.PaaData);
                index++;
                width = width / 2;
                height = height / 2;
            }
            writer.Write((uint)0x0000);
            writer.Write((ushort)0x0000);
        }
    }
}
