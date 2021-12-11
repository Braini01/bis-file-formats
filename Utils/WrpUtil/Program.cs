using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BIS.Core.Config;
using BIS.Core.Streams;
using BIS.P3D;
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

        [Verb("stats", HelpText = "Stats from a WRP file.")]
        class StatsOptions
        {
            [Value(0, MetaName = "source", HelpText = "WRP file.", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "If specified, target CSV file with object count per model.", Required = false)]
            public string Target { get; set; }
        }

        [Verb("edit", HelpText = "Mass edit a WRP file.")]
        class MassEditOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source WRP file.", Required = true)]
            public string Source { get; set; }

            [Value(2, MetaName = "definition", HelpText = "CSV file with edit to proceed.", Required = true)]
            public string Definition { get; set; }

            [Value(3, MetaName = "target", HelpText = "Target WRP file.", Required = true)]
            public string Target { get; set; }

            [Option('z', "fix-alt", Required = false, HelpText = "Automaticly fix Z position if models heights are differents.")]
            public bool FixAltitude { get; set; }
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<ConvertOptions, MergeOptions, StripOptions, DependenciesOptions, StatsOptions, MassEditOptions>(args)
              .MapResult(
                (ConvertOptions opts) => Convert(opts),
                (MergeOptions opts) => Merge(opts),
                (StripOptions opts) => Strip(opts),
                (DependenciesOptions opts) => Dependencies(opts),
                (StatsOptions opts) => Stats(opts),
                (MassEditOptions opts) => MassEdit(opts),
                errs => 1);
        }

        private static int MassEdit(MassEditOptions opts)
        {
            Console.WriteLine($"Read '{opts.Source}'");
            var source = StreamHelper.Read<AnyWrp>(opts.Source);
            var editable = source.GetEditableWrp();
            var editedMetarial = new Dictionary<string, string>();

            Console.WriteLine($"Process '{opts.Definition}'");
            foreach (var line in File.ReadAllLines(opts.Definition))
            {
                var items = line.Split(';');
                switch(items[0].ToUpperInvariant())
                {
                    case "REPLACE":
                        Replace(editable, items[1], items[2], items.Length > 3 ? items[3] : null, opts.FixAltitude);
                        break;
                    case "REDUCE":
                        Reduce(editable, items[1], double.Parse(items[2], CultureInfo.InvariantCulture));
                        break;
                    case "RE-MATERIAL":
                        ReMaterial(editable, editedMetarial, items[1], items[2]);
                        break;
                }
            }

            editable.Objects = editable.Objects.Where(o => o != null).ToList();

            if (editedMetarial.Count > 0)
            {
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(opts.Target), "data", "layers"));
                Directory.CreateDirectory(target);
                Console.WriteLine($"Generate new materials in {target}");
                foreach (var pair in editedMetarial)
                {
                    File.WriteAllText(Path.Combine(target, Path.GetFileName(pair.Key)), pair.Value);
                }
                for(int i = 0; i < editable.MatNames.Length; ++i)
                {
                    if (editable.MatNames[i] != null)
                    {
                        editable.MatNames[i] = Path.Combine(target, Path.GetFileName(editable.MatNames[i])).Replace("P:\\", "", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            Console.WriteLine($"Write '{opts.Target}'");
            editable.Write(opts.Target);

            Console.WriteLine("Done");
            return 0;
        }

        private static void ReMaterial(EditableWrp editable, Dictionary<string, string> editedMetarial, string initial, string replacement)
        {
            if (editedMetarial.Count == 0)
            {
                foreach (var mat in editable.MatNames.Where(m => m != null))
                {
                    editedMetarial[mat] = ReadMaterial(mat);
                }
            }
            foreach(var mat in editable.MatNames)
            {
                if (mat != null && editedMetarial.ContainsKey(mat))
                {
                    editedMetarial[mat] = editedMetarial[mat].Replace(initial, replacement, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private static string ReadMaterial(string m)
        {
            var physical = Path.Combine("P:", m);
            try
            {
                return StreamHelper.Read<ParamFile>(physical).ToString();
            }
            catch(ArgumentException) // if ParamFile is not binarized/rapped
            {
                return File.ReadAllText(physical);
            }
        }

        private static void Reduce(EditableWrp editable, string model, double removeRatio)
        {
            var rnd = new Random(model.GetHashCode());
            for (int i = 0; i < editable.Objects.Count; ++i)
            {
                var obj = editable.Objects[i];
                if (obj != null &&
                    string.Equals(model, model, StringComparison.OrdinalIgnoreCase) &&
                    (removeRatio == 1 || rnd.NextDouble() <= removeRatio))
                {
                    editable.Objects[i] = null;
                }
            }
        }

        private static void Replace(EditableWrp editable, string initial, string replacement, string altShiftString, bool autoFixAltitude)
        {
            float altShift = 0;
            if ((string.IsNullOrEmpty(altShiftString) || !float.TryParse(altShiftString, NumberStyles.Any, CultureInfo.InvariantCulture, out altShift)) && autoFixAltitude)
            {
                var oldModel = StreamHelper.Read<P3D>(Path.Combine("P:", initial)).ModelInfo;
                var newModel = StreamHelper.Read<P3D>(Path.Combine("P:", replacement)).ModelInfo;
                altShift = oldModel.BboxMin.Y - newModel.BboxMin.Y;
                Console.WriteLine($"  '{initial}'->'{replacement}' altShift={altShift:0.00} (computed)");
            }
            else
            {
                Console.WriteLine($"  '{initial}'->'{replacement}' altShift={altShift:0.00}");
            }
            var changes = 0;
            foreach (var obj in editable.Objects)
            {
                if (string.Equals(obj.Model, initial, StringComparison.OrdinalIgnoreCase))
                {
                    obj.Model = replacement;
                    if (altShift != 0)
                    {
                        if (obj.Transform.AltitudeScale != 1f)
                        {
                            obj.Transform.Altitude += altShift * obj.Transform.AltitudeScale;
                        }
                        else
                        {
                            obj.Transform.Altitude += altShift;
                        }
                    }
                    changes++;
                }
            }
            Console.WriteLine($"  -> {changes} changes");
            Console.WriteLine();
        }

        private static int Stats(StatsOptions opts)
        {
            Console.WriteLine($"WRP '{opts.Source}'");
            var source = StreamHelper.Read<AnyWrp>(opts.Source);
            Console.WriteLine($"CellSize      = {source.CellSize}");
            Console.WriteLine($"LandRange     = {source.LandRangeX}x{source.LandRangeY}");
            Console.WriteLine($"TerrainRange  = {source.TerrainRangeX}x{source.TerrainRangeY}");
            Console.WriteLine($"ObjectsCount  = {source.ObjectsCount}");
            Console.WriteLine($"MaterialNames = {source.MatNames.Length}");
            Console.WriteLine($"MaterialIndex = {source.MaterialIndex.Count}");
            if (!string.IsNullOrEmpty(opts.Target))
            {
                var data = source.GetEditableWrp().GetNonDummyObjects().GroupBy(o => o.Model).Select(g => new { Object = g.Key, Count = g.Count() }).ToList();
                File.WriteAllLines(opts.Target, data.OrderByDescending(d => d.Count).Select(d => $"{d.Object};{d.Count}"));
            }
            return 0;
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
