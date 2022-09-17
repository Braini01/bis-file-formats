using System;
using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
	public class AnimationClass
    {
        internal AnimationClass(BinaryReaderEx input, int version)
        {
			AnimType = input.ReadUInt32();
			AnimName = input.ReadAsciiz();
			AnimSource = input.ReadAsciiz();
			MinPhase = input.ReadSingle();
			MaxPhase = input.ReadSingle();
			MinValue = input.ReadSingle();
			MaxValue = input.ReadSingle();
			if (version >= 56)
			{
				AnimPeriod = input.ReadSingle();
				InitPhase = input.ReadSingle();
			}
			SourceAddress = input.ReadUInt32();

			switch (AnimType)
			{
				case 0:
				case 1:
				case 2:
				case 3:
					Angle0 = input.ReadSingle();
					Angle1 = input.ReadSingle();
					return;
				case 4:
				case 5:
				case 6:
				case 7:
					Offset0 = input.ReadSingle();
					Offset1 = input.ReadSingle();
					return;
				case 8:
					AxisPos = new Vector3P(input);
					AxisDir = new Vector3P(input);
					Angle = input.ReadSingle();
					AxisOffset = input.ReadSingle();
					return;
				case 9:
					HideValue = input.ReadSingle();
					if (version >= 55)
					{
						Unused55 = input.ReadSingle();
						return;
					}
					return;
				default:
					throw new Exception("Unknown AnimType encountered: " + AnimType);
			}
		}

        public uint AnimType { get; }
        public string AnimName { get; }
        public string AnimSource { get; }
        public float MinPhase { get; }
        public float MaxPhase { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public float AnimPeriod { get; }
        public float InitPhase { get; }
        public uint SourceAddress { get; }
        public float Angle0 { get; }
        public float Angle1 { get; }
        public float Offset0 { get; }
        public float Offset1 { get; }
        public Vector3P AxisPos { get; }
        public Vector3P AxisDir { get; }
        public float Angle { get; }
        public float AxisOffset { get; }
        public float HideValue { get; }
        public float Unused55 { get; }

		internal void Write(BinaryWriterEx output, int version)
		{
			output.Write(AnimType);
			output.WriteAsciiz(AnimName);
			output.WriteAsciiz(AnimSource);
			output.Write(MinPhase);
			output.Write(MaxPhase);
			output.Write(MinValue);
			output.Write(MaxValue);
			if (version >= 56)
			{
				output.Write(AnimPeriod);
				output.Write(InitPhase);
			}
			output.Write(SourceAddress);

			switch (AnimType)
			{
				case 0:
				case 1:
				case 2:
				case 3:
					output.Write(Angle0);
					output.Write(Angle1);
					return;
				case 4:
				case 5:
				case 6:
				case 7:
					output.Write(Offset0);
					output.Write(Offset1);
					return;
				case 8:
					AxisPos.Write(output);
					AxisDir.Write(output);
					output.Write(Angle);
					output.Write(AxisOffset);
					return;
				case 9:
					output.Write(HideValue);
					if (version >= 55)
					{
						output.Write(Unused55);
						return;
					}
					return;
				default:
					throw new Exception("Unknown AnimType encountered: " + AnimType);
			}
		}
	}
}