using System.Collections.Generic;
using System.Linq;

namespace BIS.P3D.MLOD
{
    internal class NamedSelectionInfos : INamedSelection
    {
        private readonly P3DM_LOD lod;
        private readonly NamedSelectionTagg nst;

        public NamedSelectionInfos(P3DM_LOD lod, NamedSelectionTagg nst)
        {
            this.lod = lod;
            this.nst = nst;
        }

        public string Name => nst.Name;

        public string Material => OneOrNone(Faces.Select(f => f.Material).Distinct());

        public string Texture => OneOrNone(Faces.Select(f => f.Texture).Distinct());

        private IEnumerable<Face> Faces => nst.Faces.Where(b => b != 0).Select((_,i) => lod.Faces[i]);

        private static string OneOrNone(IEnumerable<string> enumerable)
        {
            var list = enumerable.ToList();
            if (list.Count == 1)
            {
                return list[0];
            }
            return null;
        }
    }
}