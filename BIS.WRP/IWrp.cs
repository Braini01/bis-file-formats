using System.Collections.Generic;

namespace BIS.WRP
{
    internal interface IWrp
    {
        int LandRangeX { get; }
        int LandRangeY { get; }
        int TerrainRangeX { get; }
        int TerrainRangeY { get; }
        float CellSize { get; }
        float[] Elevation { get; }
        string[] MatNames { get; }
        IReadOnlyList<ushort> MaterialIndex { get; }
        int ObjectsCount { get; }
    }
}