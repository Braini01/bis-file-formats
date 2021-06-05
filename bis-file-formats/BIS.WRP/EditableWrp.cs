using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BIS.Core.Streams;

namespace BIS.WRP
{
    public class EditableWrp : IReadWriteObject, IWrp// aka 8WVR
    {
        public EditableWrp()
        {

        }

        public EditableWrp(Stream s)
            : this(new BinaryReaderEx(s))
        {
        }

        public EditableWrp(BinaryReaderEx input)
        {
            Read(input);
        }

        public void Read(BinaryReaderEx input)
        {
            if (input.ReadAscii(4) != "8WVR")
            {
                throw new FormatException("8WVR file does not start with correct file signature");
            }

            ReadContent(input);
        }

        internal void ReadContent(BinaryReaderEx input)
        {
            LandRangeX = input.ReadInt32();
            LandRangeY = input.ReadInt32();
            TerrainRangeX = input.ReadInt32();
            TerrainRangeY = input.ReadInt32();
            CellSize = input.ReadSingle();
            Elevation = input.ReadFloats(TerrainRangeX * TerrainRangeY);
            MaterialIndex = input.ReadUshorts(LandRangeX * LandRangeY);

            var nMaterials = input.ReadInt32();
            MatNames = new string[nMaterials];
            for (int i = 0; i < nMaterials; i++)
            {
                int len;
                do
                {
                    len = input.ReadInt32();
                    if (len != 0)
                    {
                        MatNames[i] = input.ReadAscii(len);
                    }
                } while (len != 0);
            }

            while (!input.HasReachedEnd)
            {
                Objects.Add(new EditableWrpObject(input));
            }
        }

        public void Write(BinaryWriterEx output)
        {
            output.WriteAscii("8WVR", 4);
            output.Write(LandRangeX);
            output.Write(LandRangeY);
            output.Write(TerrainRangeX );
            output.Write(TerrainRangeY);
            output.Write(CellSize);
            output.WriteFloats(Elevation);
            output.WriteUshorts(MaterialIndex);
            output.Write(MatNames.Length);
            foreach (var mat in MatNames)
            {
                if (!string.IsNullOrEmpty(mat))
                {
                    output.WriteAscii32(mat);
                }
                output.WriteAscii32("");
            }
            foreach(var obj in Objects)
            {
                obj.Write(output);
            }
        }


        public int LandRangeX { get; set; }
        public int LandRangeY { get; set; }
        public int TerrainRangeX { get; set; }
        public int TerrainRangeY { get; set; }
        public float CellSize { get; set; }
        public float[] Elevation { get; set; }
        public ushort[] MaterialIndex { get; set; }
        public string[] MatNames { get; set; }
        public List<EditableWrpObject> Objects { get; set; } = new List<EditableWrpObject>();
    }
}
