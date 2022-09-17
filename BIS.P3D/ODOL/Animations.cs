using System;
using BIS.Core.Math;
using BIS.Core.Streams;

namespace BIS.P3D.ODOL
{
	public class Animations
    {
        internal Animations(BinaryReaderEx input, int version)
        {
			AnimationClasses = input.ReadArray(i => new AnimationClass(i, version));
			Bones2Anims = input.ReadArray(i => i.ReadArray(j => j.ReadArray(k => k.ReadUInt32())));

			Anims2Bones = new int[Bones2Anims.Length][];
			AxisData = new Vector3P[Bones2Anims.Length][][];
			for (int j = 0; j < Bones2Anims.Length; j++)
			{
				Anims2Bones[j] = new int[AnimationClasses.Length];
				AxisData[j] = new Vector3P[AnimationClasses.Length][];
				for (int k = 0; k < AnimationClasses.Length; k++)
				{
					Anims2Bones[j][k] = input.ReadInt32();
					if (Anims2Bones[j][k] != -1 
						&& AnimationClasses[k].AnimType != 8 
						&& AnimationClasses[k].AnimType != 9)
					{
						AxisData[j][k] = new Vector3P[] { new Vector3P(input), new Vector3P(input) };
					}
				}
			}
		}

        public AnimationClass[] AnimationClasses { get; }
        public uint[][][] Bones2Anims { get; }
        public int[][] Anims2Bones { get; }
        public Vector3P[][][] AxisData { get; }

        internal void Write(BinaryWriterEx output, int version)
        {
			output.WriteArray(AnimationClasses, (w, v) => v.Write(w, version));
			output.WriteArray(Bones2Anims, (w1, v1) => w1.WriteArray(v1, (w2,v2) => w2.WriteArray(v2, (w3, v3) => w3.Write(v3))));
			for (int j = 0; j < Bones2Anims.Length; j++)
			{
				for (int k = 0; k < AnimationClasses.Length; k++)
				{
					output.Write(Anims2Bones[j][k]);
					if (Anims2Bones[j][k] != -1
						&& AnimationClasses[k].AnimType != 8
						&& AnimationClasses[k].AnimType != 9)
					{
						AxisData[j][k][0].Write(output);
						AxisData[j][k][1].Write(output);
					}
				}
			}
		}
    }
}