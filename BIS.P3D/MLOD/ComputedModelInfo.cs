using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BIS.Core.Math;

namespace BIS.P3D.MLOD
{
    class ComputedModelInfo : IModelInfo
    {
        internal ComputedModelInfo(MLOD mLOD)
        {
            var points = mLOD.Lods.SelectMany(l => l.Points);

            BboxMin = new Vector3P(
                points.Min(p => p.X),
                points.Min(p => p.Y),
                points.Min(p => p.Z));

            BboxMax = new Vector3P(
                points.Max(p => p.X),
                points.Max(p => p.Y),
                points.Max(p => p.Z));
        }

        public Vector3P BboxMin { get; }

        public Vector3P BboxMax { get; }
    }
}
