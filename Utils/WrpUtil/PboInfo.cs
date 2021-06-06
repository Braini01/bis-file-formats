using System.Collections.Generic;

namespace WrpUtil
{
    internal class PboInfo
    {
        public string Path { get; internal set; }
        public HashSet<string> Files { get; internal set; }
        public ModInfo Mod { get; internal set; }
    }
}