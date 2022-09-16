using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.P3D
{
    public interface ILevelOfDetail
    {
        float Resolution { get; }
        IEnumerable<Tuple<string, string>> NamedProperties { get; }
        IEnumerable<INamedSelection> NamedSelections { get; }
        int FaceCount { get; }
        uint VertexCount { get; }
        IEnumerable<string> GetTextures();
        IEnumerable<string> GetMaterials();

        LodHashId GetModelHashId();
    }
}
