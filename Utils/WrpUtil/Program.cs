using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BIS.Core.Streams;
using BIS.PBO;
using BIS.WRP;
using CommandLine;

namespace WrpUtil
{

    class Program
    {
        [Verb("convert", HelpText = "Convert to editable WRP.")]
        class ConvertOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source file.", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target file.", Required = true)]
            public string Target { get; set; }
        }

        [Verb("merge", HelpText = "Merge data from two editable WRP.")]
        class MergeOptions
        {
            [Value(0, MetaName = "master", HelpText = "Master source file, its terrain definition is kept.", Required = true)]
            public string Master { get; set; }

            [Value(1, MetaName = "objects", HelpText = "Objects source file, its objects are kept.", Required = true)]
            public string ToMerge { get; set; }

            [Value(2, MetaName = "target", HelpText = "Target file.", Required = true)]
            public string Target { get; set; }
        }

        [Verb("strip", HelpText = "Strip objects from a WRP, keep only terrain.")]
        class StripOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source WRP file.", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target WRP file.", Required = true)]
            public string Target { get; set; }
        }

        [Verb("dependencies", HelpText = "Compute dependencies of a WRP.")]
        class DependenciesOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source WRP file.", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "report", HelpText = "Report text file.", Required = false)]
            public string ReportFile { get; set; }

            [Option('m', "mods", Required = false, HelpText = "Base path of mods directory (by default !Workshop of Arma installation directory).")]
            public string ModsBasePath { get; set; }
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<ConvertOptions, MergeOptions, StripOptions, DependenciesOptions>(args)
              .MapResult(
                (ConvertOptions opts) => Convert(opts),
                (MergeOptions opts) => Merge(opts),
                (StripOptions opts) => Strip(opts),
                (DependenciesOptions opts) => Dependencies(opts),
                errs => 1);
        }


        private static int Convert(ConvertOptions opts)
        {
            Console.WriteLine($"Read WRP from '{opts.Source}'");
            var source = StreamHelper.Read<AnyWrp>(opts.Source);

            Console.WriteLine("Convert");
            var editable = source.GetEditableWrp();

            Console.WriteLine($"Write to '{opts.Target}'");
            editable.Write(opts.Target);

            Console.WriteLine("Done");
            return 0;
        }

        private static int Merge(MergeOptions opts)
        {
            Console.WriteLine($"Read WRP from '{opts.Master}'");
            var master = StreamHelper.Read<AnyWrp>(opts.Master).GetEditableWrp();

            Console.WriteLine($"Read WRP from '{opts.ToMerge}'");
            var tomerge = StreamHelper.Read<AnyWrp>(opts.ToMerge).GetEditableWrp().GetNonDummyObjects();

            Console.WriteLine("Merge");
            var objects = master.GetNonDummyObjects().ToList();
            var idShift = objects.Count > 0 ? objects.Max(o => o.ObjectID) + 1 : 0;
            objects.AddRange(tomerge.Select(o => new EditableWrpObject() { Model = o.Model, ObjectID = o.ObjectID + idShift, Transform = o.Transform }));
            objects.Add(EditableWrpObject.Dummy);
            master.Objects = objects;

            Console.WriteLine($"Write to '{opts.Target}'");
            master.Write(opts.Target);

            Console.WriteLine("Done");
            return 0;
        }

        private static int Strip(StripOptions opts)
        {
            Console.WriteLine($"Read WRP from '{opts.Source}'");
            var source = StreamHelper.Read<AnyWrp>(opts.Source);

            Console.WriteLine("Convert");
            var editable = source.GetEditableWrp();
            editable.Objects = new List<EditableWrpObject>() { EditableWrpObject.Dummy };

            Console.WriteLine($"Write to '{opts.Target}'");
            editable.Write(opts.Target);

            Console.WriteLine("Done");
            return 0;
        }

        private static int Dependencies(DependenciesOptions opts)
        {
            if (string.IsNullOrEmpty(opts.ModsBasePath))
            {
                opts.ModsBasePath = @"C:\Program Files (x86)\Steam\steamapps\common\Arma 3\!Workshop";
            }
            Console.WriteLine($"Build index of mods pbo and files from '{opts.ModsBasePath}'");
            var mods = Directory.GetDirectories(opts.ModsBasePath);
            var modsData = new List<ModInfo>();
            foreach (var mod in mods)
            {
                var path = Path.Combine(mod, "addons");
                if (Directory.Exists(path))
                {
                    var infos = new ModInfo();
                    infos.Path = mod;
                    infos.Pbos = new List<PboInfo>();
                    var allPBOs = Directory.GetFiles(Path.Combine(mod, "addons"), "*.pbo");
                    foreach (var pboPath in allPBOs)
                    {
                        var pbo = new PBO(pboPath);
                        var pboInfos = new PboInfo();
                        pboInfos.Mod = infos;
                        pboInfos.Path = pboPath;
                        pboInfos.Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var entry in pbo.FileEntries)
                        {
                            if (string.Equals(Path.GetExtension(entry.FileName), ".p3d", StringComparison.OrdinalIgnoreCase))
                            {
                                pboInfos.Files.Add(Path.Combine(pbo.Prefix, entry.FileName));
                            }
                        }
                        if (pboInfos.Files.Count > 0)
                        {
                            infos.Pbos.Add(pboInfos);
                        }
                    }
                    if (infos.Pbos.Count > 0)
                    {
                        infos.WorkshopId = GetWorkshopId(mod);
                        modsData.Add(infos);
                    }
                }
            }

            var allPbos = modsData.SelectMany(m => m.Pbos);

            Console.WriteLine($"Read WRP from '{opts.Source}'");
            var source = StreamHelper.Read<AnyWrp>(opts.Source);

            Console.WriteLine("Compute model list");
            var models = source.GetEditableWrp().GetNonDummyObjects().Select(e => e.Model).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);

            var usedPbo = new HashSet<PboInfo>();
            foreach(var model in models)
            {
                if (!model.StartsWith("a3\\"))
                {
                    var pbo = allPbos.FirstOrDefault(p => p.Files.Contains(model));
                    if (pbo != null)
                    {
                        usedPbo.Add(pbo);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Model '{model}' was not found.");
                    }
                }
            }

            var usedMods = usedPbo.GroupBy(p => p.Mod).Select(m => new ModInfo()
            {
                Path = m.Key.Path,
                WorkshopId = m.Key.WorkshopId,
                Pbos = m.Select(p => new PboInfo()
                {
                    Path = p.Path,
                    Files = p.Files.Where(f => models.Contains(f)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                }).ToList()
            }).ToList();

            if (string.IsNullOrEmpty(opts.ReportFile))
            {
                opts.ReportFile = Path.ChangeExtension(opts.Source, ".txt");
            }

            Console.WriteLine($"Write full report to '{opts.ReportFile}'");
            using (var writer = new StreamWriter(opts.ReportFile, false))
            {
                foreach (var mod in usedMods)
                {
                    Console.WriteLine($"  Depends on '{Path.GetFileName(mod.Path)}' (Workshop #{mod.WorkshopId})");
                    writer.WriteLine($"Depends on '{Path.GetFileName(mod.Path)}'");
                    writer.WriteLine($"   Workshop #{mod.WorkshopId}");
                    writer.WriteLine($"   '{mod.Path}')");
                    foreach (var pbo in mod.Pbos)
                    {
                        writer.WriteLine($"  Content from '{Path.GetFileName(pbo.Path)}'");
                        foreach(var file in pbo.Files)
                        {
                            writer.WriteLine($"    '{file}'");
                        }
                        writer.WriteLine();
                    }
                    writer.WriteLine();
                    writer.WriteLine();
                }

                writer.WriteLine($"Project drive minimal setup (using bankrev)");
                foreach (var mod in usedMods)
                {
                    foreach (var pbo in mod.Pbos)
                    {
                        writer.WriteLine($@"  bankrev -f ""P:"" -prefix ""{pbo.Path}"" ");
                    }
                }
            }

            Console.WriteLine("Done");
            return 0;
        }

        private static readonly Regex IdRegex = new Regex(@"publishedid\s*=\s*([0-9]+);", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static string GetWorkshopId(string mod)
        {
            var infos = Path.Combine(mod, "meta.cpp");
            if (File.Exists(infos))
            {
                var match = IdRegex.Match(File.ReadAllText(infos));
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return "";
        }
    }
}
