using System;
using System.IO;
using System.Threading.Tasks;
using BCnEncoder.Shared;
using BIS.PAA.Encoder;
using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Image2PAA
{
    class Program
    {
        public class Options
        {
            [Value(0, MetaName = "source", HelpText = "Source file.", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target file.", Required = false)]
            public string Target { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args)
                   .MapResult(o =>
                   {
                       if (Path.GetFileNameWithoutExtension(o.Source).Contains("*"))
                       {
                           var files = Directory.GetFiles(Path.GetDirectoryName(o.Source), Path.GetFileName(o.Source));

                           Parallel.ForEach(files, file =>
                           {
                               var target = string.IsNullOrEmpty(o.Target) ?
                                  Path.ChangeExtension(file, ".paa") :
                                  Path.Combine(o.Target, Path.ChangeExtension(Path.GetFileName(file), ".paa"));
                               Convert(file, target);
                           });
                       }
                       else
                       {
                           if (!File.Exists(o.Source))
                           {
                               Console.Error.WriteLine($"File '{o.Source}' does not exists.");
                               return 1;
                           }
                           var target = string.IsNullOrEmpty(o.Target) ?
                             Path.ChangeExtension(o.Source, ".paa") :
                             o.Target;
                           Convert(o.Source, target);
                       }
                       return 0;
                   },
                   e => 3);
        }

        private static void Convert(string source, string target)
        {
            Console.WriteLine($"{source} -> {target}");
            using (var paaStream = File.OpenRead(source))
            {
                using (var img = Image.Load<Rgba32>(source))
                {
                    var targetPixels = new ColorRgba32[img.Height, img.Width];
                    for (int y = 0; y < img.Height; ++y)
                    {
                        for (int x = 0; x < img.Width; ++x)
                        {
                            var srcPixel = img[x, y];
                            targetPixels[y, x] = new ColorRgba32(srcPixel.R, srcPixel.G, srcPixel.B, srcPixel.A);
                        }
                    }
                    PaaEncoder.WritePAA(target, targetPixels);
                }
            }
        }
    }
}
