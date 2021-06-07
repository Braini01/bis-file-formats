using System;
using System.Collections.Generic;
using BIS.Core.Streams;

namespace BIS.WRP
{
    /// <summary>
    /// Abstraction of a wrp file, binarised or editable
    /// </summary>
    public class AnyWrp : IReadObject, IWrp
    {
        private OPRW binarized;
        private EditableWrp editable;
        private IWrp wrp;

        public int LandRangeX => wrp.LandRangeX;

        public int LandRangeY => wrp.LandRangeY;

        public int TerrainRangeX => wrp.TerrainRangeX;

        public int TerrainRangeY => wrp.TerrainRangeY;

        public float CellSize => wrp.CellSize;

        public float[] Elevation => wrp.Elevation;

        public string[] MatNames => wrp.MatNames;

        public IReadOnlyList<ushort> MaterialIndex => wrp.MaterialIndex;

        public void Read(BinaryReaderEx input)
        {
            var signature = input.ReadAscii(4);
            switch (signature)
            {
                case "OPRW":
                    binarized = new OPRW();
                    binarized.ReadContent(input);
                    wrp = binarized;
                    editable = null;
                    break;
                case "8WVR":
                    editable = new EditableWrp();
                    editable.ReadContent(input);
                    wrp = editable;
                    binarized = null;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown WRP format '{signature}'");
            }
        }

        public EditableWrp GetEditableWrp()
        {
            if (editable == null)
            {
                if (binarized != null)
                {
                    editable = binarized.ToEditableWrp();
                }
            }
            return editable;
        }

    }
}
