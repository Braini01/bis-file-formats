using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCnEncoder.Shared;
using BIS.Core.Streams;
using BIS.PAA;
using Microsoft.Toolkit.HighPerformance;
using PaintDotNet;

namespace PaaPdnPlugin
{
    public class PaaFileType : FileType
    {
        public PaaFileType(IFileTypeHost host)
            : base("PAA texture", new FileTypeOptions()
            {
                LoadExtensions = new[] { ".paa" },
                //SaveExtensions = new[] { ".paa" },
                SupportsLayers = false
            })
        {

        }

        protected override Document OnLoad(Stream input)
        {
            var paa = new PAA(input);
            var pixels = PAA.GetARGB32PixelData(paa, input);

            var doc = new Document(paa.Width, paa.Height);
            var bitmap = Layer.CreateBackgroundLayer(doc.Width, doc.Height);
            unsafe
            {
                fixed (byte* pixelsB = pixels)
                {
                    var src = pixelsB;
                    for (int y = 0; y < paa.Height; ++y)
                    {
                        var dst = bitmap.Surface.GetRowPointer(y);
                        for (int x = 0; x < paa.Width; ++x)
                        {
                            *dst = ColorBgra.FromBgra(src[0], src[1], src[2], src[3]);
                            dst++;
                            src += 4;
                        }
                    }
                }
            }
            doc.Layers.Add(bitmap);
            return doc;
        }

        protected override void OnSave(Document input, Stream output, SaveConfigToken token, Surface scratchSurface, ProgressEventHandler callback)
        {
            var enc = new BCnEncoder.Encoder.BcEncoder(CompressionFormat.Bc3); // DXT5

            byte[][] mipmaps;

            input.Flatten(scratchSurface);

            unsafe
            {
                var pixels = new ColorRgba32[scratchSurface.Width,scratchSurface.Height];
                for (int y = 0; y < scratchSurface.Height; ++y)
                {
                    var src = scratchSurface.GetRowPointer(y);
                    for (int x = 0; x < scratchSurface.Width; ++x)
                    {
                        var color = new ColorRgba32((*src).R, (*src).G, (*src).B, (*src).A);

                        pixels[y,x] = color;
                        src++;
                    }
                }
                mipmaps = enc.EncodeToRawBytes(new ReadOnlyMemory2D<ColorRgba32>(pixels));
            }

            int[] offsets = new int[16];

            offsets[0] = 128;

            for (int i = 1; i < mipmaps.Length; ++i)
            {
                offsets[i] = offsets[i - 1] + 7 + mipmaps[i - 1].Length;
            }

            using (var writer = new BinaryWriterEx(output, true))
            {
                writer.Write((ushort)0xff05);

                writer.WriteAscii("GGATCGVA", 8);
                writer.Write((uint)4);
                writer.Write((uint)0); // FIXME: AverageColor

                writer.WriteAscii("GGATCXAM", 8);
                writer.Write((uint)4);
                writer.Write((uint)0xFFFFFFFF); // FIXME: MaxColor

                writer.WriteAscii("GGATGALF", 8);
                writer.Write((uint)4);
                writer.Write((uint)2);

                writer.WriteAscii("GGATSFFO", 8);
                writer.Write((uint)offsets.Length * 4);
                foreach(var offset in offsets)
                {
                    writer.Write((uint)offset);
                }

                writer.Write((ushort)0x0000); // 2

                int index = 0;
                var size = scratchSurface.Width;
                foreach (var mipmap in mipmaps)
                {
                    if (writer.Position != offsets[index])
                    {
                        throw new Exception($"Wrong offset @{index} : {writer.Position} != {offsets[index]}");
                    }
                    writer.Write((ushort)size);
                    writer.Write((ushort)size);
                    writer.WriteUInt24((uint)mipmap.Length);
                    writer.Write(mipmap);
                    index++;
                    size = size / 2;
                }
                writer.Write((uint)0x0000);

                writer.Write((ushort)0x0000);
            }
        }
    }
}
