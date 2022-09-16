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

            var pair = mLOD.Lods.SelectMany(l => l.NamedProperties.Where(n => string.Equals(n.Item1, "map", StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
            if ( pair != null )
            {
                MapType = DecodeMapType(pair.Item2);
            }
            else
            {
                MapType = MapType.Unkwown;
            }
            pair = mLOD.Lods.SelectMany(l => l.NamedProperties.Where(n => string.Equals(n.Item1, "class", StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
            if (pair != null)
            {
                Class = pair.Item2;
            }
        }

        private static MapType DecodeMapType(string value)
        {
            switch (value.ToLowerInvariant().Trim())
            {
                case "tree": 
                    return MapType.Tree;
                case "small tree":
                    return MapType.SmallTree;
                case "building":
                    return MapType.Building;
                case "house":
                    return MapType.House;
                case "wall":
                    return MapType.Wall;
                case "fence":
                    return MapType.Fence;
                case "rock":
                    return MapType.Rock;
            }
            return MapType.Unkwown;
        }

        public Vector3P BboxMin { get; }

        public Vector3P BboxMax { get; }

        public MapType MapType { get; }

        public string Class { get; }
    }
}
