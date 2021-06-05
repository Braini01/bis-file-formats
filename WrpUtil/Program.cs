using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using BIS.Core.Config;
using BIS.Core.Streams;
using BIS.WRP;
using CommandLine;

namespace WrpUtil
{

    class Program
    {
        [Verb("convert", HelpText = "Convert to editable WRP.")]
        class ConvertOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source file.")]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target file.")]
            public string Target { get; set; }
        }

        [Verb("merge", HelpText = "Merge data from two editable WRP.")]
        class MergeOptions
        {
            [Value(0, MetaName = "master", HelpText = "Master source file, its terrain definition is kept.")]
            public string Master { get; set; }

            [Value(1, MetaName = "objects", HelpText = "Objects source file, its objects are kept.")]
            public string ToMerge { get; set; }

            [Value(2, MetaName = "target", HelpText = "Target file.")]
            public string Target { get; set; }
        }

        [Verb("strip", HelpText = "Strip objects from a WRP, keep only terrain.")]
        class StripOptions
        {
            [Value(0, MetaName = "source", HelpText = "Source file.")]
            public string Source { get; set; }

            [Value(1, MetaName = "target", HelpText = "Target file.")]
            public string Target { get; set; }
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<ConvertOptions, MergeOptions, StripOptions>(args)
              .MapResult(
                (ConvertOptions opts) => Convert(opts),
                (MergeOptions opts) => Merge(opts),
                (StripOptions opts) => Strip(opts),
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
            var idShift = objects.Max(o => o.ObjectID) + 1;
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
    }
}
