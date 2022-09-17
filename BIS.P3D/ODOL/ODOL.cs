using System;
using System.Diagnostics;
using System.Linq;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
    public class ODOL : IReadWriteObject
    {
        public int Version { get; private set; }
        public string Prefix { get; private set; }
        public ModelInfo ModelInfo { get; private set; }
        public uint AppID { get; private set; }
        public string MuzzleFlash { get; private set; }
        public byte[] Extra { get; private set; }
        public LOD[] Lods { get; private set; }
        public Animations Animations { get; private set; }

        public void Read(BinaryReaderEx input)
        {
            if (input.ReadAscii(4) != "ODOL")
                throw new FormatException("ODOL signature expected");

            ReadContent(input);
        }

        public void Write(BinaryWriterEx output)
        {
            output.WriteAscii("ODOL", 4);
            WriteContent(output);
        }

        internal void ReadHeaderOnly(BinaryReaderEx input)
        {
            Version = input.ReadInt32();
            input.Version = Version;

            if (Version >= 44)
            {
                input.UseLZOCompression = true;
            }
            if (Version >= 64)
            {
                input.UseCompressionFlag = true;
            }
            if (Version >= 59)
            {
                AppID = input.ReadUInt32();
            }
            if (Version >= 58)
            {
                MuzzleFlash = input.ReadAsciiz();
            }

            var resolutions = input.ReadFloatArray();
            var noOfLods = resolutions.Length;

            Lods = new LOD[noOfLods];

            ModelInfo = new ModelInfo(input, Version, noOfLods);
        }

        internal void ReadContent(BinaryReaderEx input)
        {
            Version = input.ReadInt32();
            input.Version = Version;

            if (Version >= 44)
            {
                input.UseLZOCompression = true;
            }
            if (Version >= 64)
            {
                input.UseCompressionFlag = true;
            }
            if (Version >= 59)
            {
                AppID = input.ReadUInt32();
            }
            if (Version >= 58)
            {
                MuzzleFlash = input.ReadAsciiz();
            }

            var resolutions = input.ReadFloatArray();
            var noOfLods = resolutions.Length;

            Lods = new LOD[noOfLods];

            ModelInfo = new ModelInfo(input, Version, noOfLods);

            if (Version >= 30u)
            {
                var hasAnims = input.ReadBoolean();
                if (hasAnims)
                {
                    Animations = new Animations(input, Version);
                }
            }
            var lodStartAdresses = input.ReadArrayBase(r => r.ReadUInt32(), noOfLods);
            var lodEndAdresses = input.ReadArrayBase(r => r.ReadUInt32(), noOfLods);
            var permanent = input.ReadArrayBase(r => r.ReadBoolean(), noOfLods);
            var loadableLodInfo = new LoadableLodInfo[noOfLods];
            for (int m = 0; m < noOfLods; m++)
            {
                if (!permanent[m])
                {
                    loadableLodInfo[m] = new LoadableLodInfo(input, Version);
                }
            }
            for (int m = 0; m < noOfLods; m++)
            {
                input.Position = lodStartAdresses[m];
                Lods[m] = new LOD(input, resolutions[m], loadableLodInfo[m], Version);
                if (input.Position != lodEndAdresses[m])
                {
                    Trace.TraceWarning($"LOD {resolutions[m]} end mismatch. Expected={lodEndAdresses[m]} Actual={input.Position}");
                }
            }
            input.Position = lodEndAdresses.Max();
            Extra = input.ReadBytes((int)(input.BaseStream.Length - input.Position));
        }

        internal void WriteContent(BinaryWriterEx output)
        {
            output.Write(Version);

            if (Version >= 44)
            {
                output.UseLZOCompression = true;
            }
            if (Version >= 64)
            {
                output.UseCompressionFlag = true;
            }

            if (Version >= 59)
            {
                output.Write(AppID);
            }
            if (Version >= 58)
            {
                output.WriteAsciiz(MuzzleFlash);
            }

            output.WriteArray(Lods.Select(l => l.Resolution).ToArray());

            var noOfLods = Lods.Length;

            ModelInfo.Write(output, Version, noOfLods);

            if (Version >= 30u)
            {
                if (Animations != null)
                {
                    output.Write(true);
                    Animations.Write(output, Version);
                }
                else
                {
                    output.Write(false);
                }
            }
            var lodStartAdresses = new uint[noOfLods];
            var lodEndAdresses = new uint[noOfLods];
            var permanent = Lods.Select(l => l.LoadableLodInfo == null).ToArray();
            var adressesPositions = output.Position;
            output.WriteArrayBase(lodStartAdresses, (o, v) => o.Write(v));
            output.WriteArrayBase(lodEndAdresses, (o, v) => o.Write(v));
            output.WriteArrayBase(permanent, (o, v) => o.Write(v));
            foreach (var lod in Lods)
            {
                if (lod.LoadableLodInfo != null)
                {
                    lod.LoadableLodInfo.Write(output, Version);
                }
            }
            foreach (var lod in Lods.OrderByDescending(l => l.Resolution))
            {
                var m = Array.IndexOf(Lods, lod);
                lodStartAdresses[m] = (uint)output.Position;
                Lods[m].Write(output, Version);
                lodEndAdresses[m] = (uint)output.Position;
            }
            output.Write(Extra);

            output.Position = adressesPositions;
            output.WriteArrayBase(lodStartAdresses, (o, v) => o.Write(v));
            output.WriteArrayBase(lodEndAdresses, (o, v) => o.Write(v));
        }
    }
}
