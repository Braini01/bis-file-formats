using BIS.PAA;
using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Linq;

namespace PAA2PNG
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
                       var ext = Path.GetExtension(o.Source);
                       var isPAA = ext.Equals(".paa", System.StringComparison.OrdinalIgnoreCase);
                       var isPAC = ext.Equals(".pac", System.StringComparison.OrdinalIgnoreCase);
                       if (!isPAA && !isPAC)
                       {
                           Console.Error.WriteLine($"File '{o.Source}' is not a PAA or a PAC.");
                           return 2;
                       }

                       if (Path.GetFileNameWithoutExtension(o.Source).Contains("*"))
                       {
                           var files = Directory.GetFiles(Path.GetDirectoryName(o.Source), Path.GetFileName(o.Source));

                           foreach (var file in files)
                           {
                               var target = string.IsNullOrEmpty(o.Target) ? 
                                   Path.ChangeExtension(file, ".png") : 
                                   Path.Combine(o.Target, Path.ChangeExtension(Path.GetFileName(file), ".png"));

                               Convert(isPAC, file, target);
                           }
                       }
                       else
                       {
                           if (!File.Exists(o.Source))
                           {
                               Console.Error.WriteLine($"File '{o.Source}' does not exists.");
                               return 1;
                           }
                           var target = string.IsNullOrEmpty(o.Target) ?
                             Path.ChangeExtension(o.Source, ".png") :
                             o.Target;
                           Convert(isPAC, o.Source, target);
                       }
                       return 0;
                   },
                   e => 3);
        }

        private static void Convert(bool isPAC, string source, string target)
        {
            Console.WriteLine($"{source} -> {target}");
            using (var paaStream = File.OpenRead(source))
            {
                var paa = new PAA(paaStream, isPAC);
                var pixels = PAA.GetARGB32PixelData(paa, paaStream);
                using (var image = Image.LoadPixelData<Bgra32>(pixels, paa.Width, paa.Height))
                {
                    image.SaveAsPng(target);
                }
            }
        }
    }
}
