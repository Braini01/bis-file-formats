using System;
using System.Collections.Generic;
using System.Linq;

namespace BIS.P3D.ODOL
{
    internal class NamedSelectionInfos : INamedSelection
    {
        private readonly LOD lod;
        private readonly NamedSelection ns;

        public NamedSelectionInfos(LOD lod, NamedSelection ns)
        {
            this.lod = lod;
            this.ns = ns;
        }

        public string Name => ns.Name;

        public string Material => OneOrNone(ns.Sections.Select(s => lod.Sections[s]).Where(s => s.MaterialIndex != -1).Select(s => lod.Materials[s.MaterialIndex].MaterialName).Distinct());

        private static string OneOrNone(IEnumerable<string> enumerable)
        {
            var list = enumerable.ToList();
            if (list.Count == 1)
            {
                return list[0];
            }
            return null;
        }

        public string Texture => OneOrNone(ns.Sections.Select(s => lod.Sections[s]).Where(s => s.TextureIndex != -1).Select(s => lod.Textures[s.TextureIndex]).Distinct());
    }
}