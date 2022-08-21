using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BIS.Core.Streams;
using BIS.P3D.MLOD;
using CommandLine;

namespace P3dUtil
{
    class Program
    {
        [Verb("template", HelpText = "Generate P3D files based on a template definition.")]
        class TemlateOptions
        {
            [Value(0, MetaName = "template", HelpText = "Template definition JSON file.", Required = true)]
            public string TemplateDefinition { get; set; }

            [Option('n', "no-backup", Required = false, HelpText = "Do not generate a backup files (.p3d.bak).")]
            public bool NoBackup { get; set; }
        }

        [Verb("replace-path", HelpText = "Replace a path in all texture and material reference of a P3D file. (MLOD or ODOL)")]
        class ReplaceOptions
        {
            [Value(0, MetaName = "model", HelpText = "P3D file(s) (can be a pattern)", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "old-path", HelpText = "Old path to be replaced.", Required = true)]
            public string OldPath { get; set; }

            [Value(2, MetaName = "new-path", HelpText = "New path to use.", Required = true)]
            public string NewPath { get; set; }

            [Option('n', "no-backup", Required = false, HelpText = "Do not generate a backup file (.p3d.bak).")]
            public bool NoBackup { get; set; }

            [Option('r', "recursive", Required = false, HelpText = "If model is a pattern, do a recursive file search.")]
            public bool IsRecursive { get; set; }
        }


        [Verb("uv-transform", HelpText = "Transform UV of a texture : Utarget = Usource * u-mul + u-add ; Vtarget = Vsource * v-mul + v-add.")]
        class UvTransformOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source file", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target file")]
            public string Target { get; set; }

            [Option("texture", Required = false, HelpText = "Texture")]
            public string Texture { get; set; }

            [Option("u-add", Required = false, HelpText = "Shift on U")]
            public float UAdd { get; set; } = 0f;

            [Option("u-mul", Required = false, HelpText = "Factor on original U")]
            public float UMul { get; set; } = 1f;

            [Option("v-add", Required = false, HelpText = "Shift on V")]
            public float VAdd { get; set; } = 0f;

            [Option("v-mul", Required = false, HelpText = "Factor on original V")]
            public float VMul { get; set; } = 1f;
        }

        [Verb("rm-faces", HelpText = "Removes faces with specified texture that have a point with Y between y-from and y-top.")]
        class RemoveFacesOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source file", Required = true)]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target file")]
            public string Target { get; set; }

            [Option("texture", Required = false, HelpText = "Only faces with this textures")]
            public string Texture { get; set; }

            [Option("y-from", Required = false, HelpText = "Y From")]
            public float YFrom { get; set; } = float.MinValue;

            [Option("y-to", Required = false, HelpText = "Y To")]
            public float YTo { get; set; } = float.MaxValue;
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<TemlateOptions, ReplaceOptions, UvTransformOptions, RemoveFacesOptions>(args)
              .MapResult(
                (TemlateOptions opts) => Templating(opts),
                (ReplaceOptions opts) => Replace(opts),
                (UvTransformOptions opts) => UvTransform(opts),
                (RemoveFacesOptions opts) => RemoveFaces(opts),
                errs => 1);
        }

        private static int RemoveFaces(RemoveFacesOptions opts)
        {
            if (string.IsNullOrEmpty(opts.Target))
            {
                opts.Target = opts.Source;
            }
            Console.WriteLine($"Process '{opts.Source}'...");
            var p3d = new MLOD(opts.Source);

            
            foreach (var lod in p3d.Lods)
            {
                var faceIndex = 0;
                var facesIndexToRemove = new List<int>();
                foreach (var face in lod.Faces)
                {
                    if ((string.IsNullOrEmpty(opts.Texture) || face.Texture.Contains(opts.Texture, StringComparison.OrdinalIgnoreCase)) &&
                        face.RealVertices.Select(v => lod.Points[v.PointIndex]).Any(p => p.Y >= opts.YFrom && p.Y <= opts.YTo))
                    {
                        facesIndexToRemove.Add(faceIndex);
                    }
                    faceIndex++;
                }
                Console.WriteLine($"  LOD #{lod.Resolution}, {facesIndexToRemove.Count} faces to remove (on {lod.Faces.Length})");

                if (facesIndexToRemove.Count > 0)
                {
                    lod.Faces = lod.Faces.Where((_, i) => !facesIndexToRemove.Contains(i)).ToArray();
                    foreach (var uvset in lod.Taggs.OfType<UVSetTagg>())
                    {
                        uvset.FaceUVs = uvset.FaceUVs.Where((_, i) => !facesIndexToRemove.Contains(i)).ToArray();
                        uvset.UpdateDataSize();
                    }
                    var sel = lod.Taggs.OfType<SelectedTagg>().FirstOrDefault();
                    if (sel != null)
                    {
                        sel.Faces = sel.Faces.Where((_, i) => !facesIndexToRemove.Contains(i)).ToArray();
                        sel.UpdateDataSize();
                    }
                    var lck = lod.Taggs.OfType<LockTagg>().FirstOrDefault();
                    if (lck != null)
                    {
                        lck.LockedFaces = lck.LockedFaces.Where((_, i) => !facesIndexToRemove.Contains(i)).ToArray();
                        lck.UpdateDataSize();
                    }
                    foreach (var select in lod.Taggs.OfType<NamedSelectionTagg>())
                    {
                        select.Faces = select.Faces.Where((_, i) => !facesIndexToRemove.Contains(i)).ToArray();
                        select.UpdateDataSize();
                    }
                }
            }

            Console.WriteLine($"  Save to '{opts.Target}'...");
            p3d.WriteToFile(opts.Target, true);
            Console.WriteLine("  Done");
            return 0;
        }

        private static int UvTransform(UvTransformOptions opts)
        {
            if ( string.IsNullOrEmpty(opts.Target))
            {
                opts.Target = opts.Source;
            }
            Console.WriteLine($"Process '{opts.Source}'...");
            var p3d = new MLOD(opts.Source);
            foreach (var lod in p3d.Lods)
            {
                var uvsetTaggs = lod.Taggs.OfType<UVSetTagg>().ToList();
                var faceIndex = 0;
                foreach (var face in lod.Faces)
                {
                    if (string.IsNullOrEmpty(opts.Texture) || face.Texture.Contains(opts.Texture, StringComparison.OrdinalIgnoreCase))
                    {
                        var vertexIndex = 0;
                        foreach (var vert in face.Vertices.Take(face.VertexCount))
                        {
                            vert.U = opts.UMul * vert.U + opts.UAdd;
                            vert.V = opts.VMul * vert.V + opts.VAdd;
                            foreach (var uvset in uvsetTaggs)
                            {
                                uvset.FaceUVs[faceIndex][vertexIndex, 0] = vert.U;
                                uvset.FaceUVs[faceIndex][vertexIndex, 1] = vert.V;
                            }
                            vertexIndex++;
                        }
                    }
                    faceIndex++;
                }
            }
            Console.WriteLine($"  Save to '{opts.Target}'...");
            p3d.WriteToFile(opts.Target, true);
            Console.WriteLine("  Done");
            return 0;
        }

        private static int Templating(TemlateOptions opts)
        {
            var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(opts.TemplateDefinition));
            var definition = JsonSerializer.Deserialize<TemlateDefinition>(File.ReadAllText(opts.TemplateDefinition), new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip, PropertyNameCaseInsensitive = true });

            var backup = opts.NoBackup ? false : definition.Backup ?? true;

            if (string.Equals(definition.Mode, "per-texture", StringComparison.OrdinalIgnoreCase))
            {
                PerTextureTemplating(documentDirectory, definition, backup);
            }
            return 0;
        }

        private static void PerTextureTemplating(string directory, TemlateDefinition definition, bool backup)
        {
            definition.TemplateFile = Path.Combine(directory, definition.TemplateFile);
            definition.TextureBaseDirectory = Path.Combine(directory, definition.TextureBaseDirectory);
            definition.TexturePattern = definition.TexturePattern ?? "*.paa";
            definition.TextureNameFilter = definition.TextureNameFilter ?? definition.TexturePattern.Replace("*", "");
            definition.TextureBaseGamePath = definition.TextureBaseGamePath ?? "";

            Console.WriteLine($"TemplateFile         = '{definition.TemplateFile}'");
            Console.WriteLine($"TextureBaseDirectory = '{definition.TextureBaseDirectory}'");
            Console.WriteLine($"TextureBaseGamePath  = '{definition.TextureBaseGamePath}'");
            Console.WriteLine($"TexturePattern       = '{definition.TexturePattern}'");
            Console.WriteLine($"TextureNameFilter    = '{definition.TextureNameFilter}'");


            if (definition.TemplateFile.Contains('*'))
            {
                var templateFiles = Directory.GetFiles(Path.GetDirectoryName(definition.TemplateFile), Path.GetFileName(definition.TemplateFile));
                foreach (var templateFile in templateFiles)
                {
                    Console.WriteLine($"Template '{templateFile}'");
                    PerTextureTemplate(definition, backup, Path.GetFileNameWithoutExtension(templateFile) + "_", templateFile, templateFiles);
                }
            }
            else
            {

                PerTextureTemplate(definition, backup, string.Empty, definition.TemplateFile, new[] { definition.TemplateFile });
            }


        }

        private static void PerTextureTemplate(TemlateDefinition definition, bool backup, string prefix, string templateFile, string[] templateFiles)
        {
            foreach (var file in Directory.GetFiles(definition.TextureBaseDirectory, definition.TexturePattern, SearchOption.AllDirectories))
            {
                var p3d = file.Replace(definition.TextureNameFilter, ".p3d", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(prefix))
                {
                    p3d = Path.Combine(Path.GetDirectoryName(p3d), prefix + Path.GetFileName(p3d));
                }
                if (!templateFiles.Contains(p3d, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  Process '{p3d}'...");
                    if (File.Exists(p3d) && backup)
                    {
                        BackupFile(p3d);
                    }
                    var newTexture = file.Replace(definition.TextureBaseDirectory, definition.TextureBaseGamePath, StringComparison.OrdinalIgnoreCase);
                    var template = new MLOD(templateFile);
                    var changes = 0;
                    foreach (var lod in template.Lods)
                    {
                        foreach (var face in lod.Faces)
                        {
                            if (string.Equals(face.Texture, definition.InitialTexture, StringComparison.OrdinalIgnoreCase))
                            {
                                face.Texture = newTexture;
                                changes++;
                            }
                        }
                    }
                    Console.WriteLine($"  '{definition.InitialTexture}' -> '{newTexture}' (x{changes})");
                    template.WriteToFile(p3d, true);
                }
            }
        }

        private static int Replace(ReplaceOptions opts)
        {
            if (Path.GetFileNameWithoutExtension(opts.Source).Contains("*"))
            {
                var files = Directory.GetFiles(Path.GetDirectoryName(opts.Source), Path.GetFileName(opts.Source), opts.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    Replace(file, opts.OldPath, opts.NewPath, !opts.NoBackup);
                }
            }
            else
            {
                if (!File.Exists(opts.Source))
                {
                    Console.Error.WriteLine($"File '{opts.Source}' does not exists.");
                    return 1;
                }
                Replace(opts.Source, opts.OldPath, opts.NewPath, !opts.NoBackup);
            }
            return 0;
        }
        private static void Replace(string file, string oldPath, string newPath, bool backup)
        {
            Console.WriteLine($"Process '{file}'...");
            if (backup)
            {
                BackupFile(file);
            }

            var reportedReplaces = new HashSet<string>();

            var p3d = StreamHelper.Read<BIS.P3D.P3D>(file);

            if (p3d.MLOD != null)
            {
                foreach (var lod in p3d.MLOD.Lods)
                {
                    foreach (var face in lod.Faces)
                    {
                        face.Material = Replace(face.Material, oldPath, newPath, reportedReplaces);
                        face.Texture = Replace(face.Texture, oldPath, newPath, reportedReplaces);
                    }
                }
                p3d.MLOD.Write(file);
            }
            else
            {
                foreach (var lod in p3d.ODOL.Lods)
                {
                    for (int i = 0; i < lod.Textures.Length; ++i)
                    {
                        lod.Textures[i] = Replace(lod.Textures[i], oldPath, newPath, reportedReplaces);
                    }
                    foreach (var mat in lod.Materials)
                    {
                        mat.MaterialName = Replace(mat.MaterialName, oldPath, newPath, reportedReplaces);
                        mat.SurfaceFile = Replace(mat.SurfaceFile, oldPath, newPath, reportedReplaces);
                        foreach (var tex in mat.StageTextures)
                        {
                            tex.Texture = Replace(tex.Texture, oldPath, newPath, reportedReplaces);
                        }
                        if (mat.StageTI != null)
                        {
                            mat.StageTI.Texture = Replace(mat.StageTI.Texture, oldPath, newPath, reportedReplaces);
                        }
                    }
                    foreach (var sec in lod.Sections)
                    {
                        sec.Material = Replace(sec.Material, oldPath, newPath, reportedReplaces);
                    }
                }
                p3d.ODOL.Write(file);
            }

            Console.WriteLine("  Done");
        }


        private static string Replace(string value, string oldPath, string newPath, HashSet<string> reportedReplaces)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            var newValue = value.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
            if (newValue != value && !reportedReplaces.Contains(value))
            {
                Console.WriteLine($"  '{value}' -> '{newValue}'");
                reportedReplaces.Add(value);
            }
            return newValue;
        }

        private static void BackupFile(string file)
        {
            int backupNum = 0;
            string backupPath = Path.ChangeExtension(file, ".p3d.bak");
            while (File.Exists(backupPath))
            {
                backupNum++;
                backupPath = Path.ChangeExtension(file, ".p3d.bak" + backupNum);
            }
            File.Copy(file, backupPath);
            Console.WriteLine($"  Backup to '{backupPath}'");
        }
    }
}
