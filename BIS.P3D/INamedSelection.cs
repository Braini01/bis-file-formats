using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.P3D
{
    public interface INamedSelection
    {
        string Name { get; }

        string Material { get; }

        string Texture { get; }
    }
}
