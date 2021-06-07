using System.Collections.Generic;

namespace WrpUtil
{
    internal class ModInfo
    {
        public string Path { get; internal set; }
        public List<PboInfo> Pbos { get; internal set; }
        public string WorkshopId { get; internal set; }
    }
}