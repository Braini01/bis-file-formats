using System;
using System.Collections.Generic;
using System.Text;
using BIS.Core.Math;

namespace BIS.P3D
{
    public interface IModelInfo
    {
        /// <summary>
        /// Minimum coordinates of bounding box
        /// </summary>
        Vector3P BboxMin { get; }

        /// <summary>
        /// Maximum coordinates of bounding box
        /// </summary>
        Vector3P BboxMax { get; }
    }
}
