using BIS.Core.Math;
using BIS.Core.Streams;
using System;
using System.Linq;

namespace BIS.RTM
{
    public class RTMB
    {
        // version 3 - LZO compression used for compressed arrays
        // version 4 - animation metadata

        public int Version { get; private set; }
        public bool Reversed { get; private set; }
        public Vector3P Step { get; private set; }
        public int PreloadCount { get; private set; }
        public string[] BoneNames { get; private set; }
        public string[] MetaDataValues { get; private set; }
        public AnimKeyStone[] AnimKeyStones { get; private set; }
        public float[] PhaseTimes { get; private set; }
        public TransformP[][] Phases { get; private set; }

        private void Read(BinaryReaderEx input)
        {
            if ("BMTR" != input.ReadAscii(4))
                throw new FormatException();

            Version = input.ReadInt32();
            input.Version = Version;
            if (Version >= 3) input.UseLZOCompression = true;
            if (Version >= 5) input.UseCompressionFlag = true;

            Reversed = input.ReadBoolean();
            Step = new Vector3P(input);
            var nPhases = input.ReadInt32();
            PreloadCount = input.ReadInt32();
            var nAnimatedBones = input.ReadInt32();
            BoneNames = input.ReadStringArray();

            //metadata
            if (Version >= 4)
            {
                MetaDataValues = input.ReadStringArray();
                AnimKeyStones = input.ReadArray(inp => new AnimKeyStone(inp));
            }

            PhaseTimes = input.ReadCompressedFloatArray();
            Phases = Enumerable.Range(0, nPhases).Select(_ => input.ReadCompressedArray(inp => TransformP.Read(inp), 14)).ToArray();
        }
    }
}
