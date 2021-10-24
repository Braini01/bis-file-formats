using System.IO;
using BCnEncoder.Shared;
using BIS.Core.Streams;
using BIS.PAA;
using BIS.PAA.Encoder;
using PaintDotNet;

namespace PaaPdnPlugin
{
    public class PaaFileType : FileType
    {
        private readonly IFileTypeHost host;

        public PaaFileType(IFileTypeHost host)
            : base("PAA texture", new FileTypeOptions()
            {
                LoadExtensions = new[] { ".paa" },
                SaveExtensions = new[] { ".paa" },
                SupportsLayers = false
            })
        {
            this.host = host;
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
            input.Flatten(scratchSurface);

            var pixels = new ColorRgba32[scratchSurface.Width, scratchSurface.Height];

            unsafe
            {
                
                for (int y = 0; y < scratchSurface.Height; ++y)
                {
                    var src = scratchSurface.GetRowPointer(y);
                    for (int x = 0; x < scratchSurface.Width; ++x)
                    {
                        pixels[y,x] = new ColorRgba32((*src).R, (*src).G, (*src).B, (*src).A);
                        src++;
                    }
                }
            }

            using (var writer = new BinaryWriterEx(output, true))
            {
                PaaEncoder.WritePAA(writer, pixels);
            }
        }
    }
}
