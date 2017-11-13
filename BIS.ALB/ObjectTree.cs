using BIS.Core.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIS.ALB
{
    public class ObjectTreeNode
    {
        public sbyte NodeType { get; }
        public MapArea Area { get; }
        public int Level { get; }
        public byte[] Color { get; }
        public byte flags;

        public ObjectTreeNode[] Childs;

        public ObjectTreeLeaf[] Objects;

        public ObjectTreeNode(BinaryReaderEx input)
        {
            NodeType = input.ReadSByte();

            Area = new MapArea(input);

            Level = input.ReadInt32();
            Color = Enumerable.Range(0, 4).Select(_ => input.ReadByte()).ToArray();
            flags = input.ReadByte();

            if (NodeType == 16)
            {
                Objects = new ObjectTreeLeaf[4];
                var isChild = flags;
                for (int i = 0; i < 4; i++)
                {
                    if ((isChild & 1) == 1) Objects[i] = new ObjectTreeLeaf(input);
                    isChild >>= 1;
                }
            }
            else
            {
                Childs = new ObjectTreeNode[4];
                var isChild = flags;
                for (int i = 0; i < 4; i++)
                {
                    if ((isChild & 1) == 1) Childs[i] = new ObjectTreeNode(input);
                    isChild >>= 1;
                }
            }
        }
    }

    public class ObjectTreeLeaf
    {
        public MapArea Area { get; }
        public byte[] Color { get; }

        //it's currently not clear what object hash is stored here; maybe the one covering the most area
        public int HashValue { get; }
        public int ObjectTypeCount { get; }
        public int[] ObjectTypeHashes { get; }
        public ObjectInfo[][] ObjectInfos { get; }

        public ObjectTreeLeaf(BinaryReaderEx input)
        {
            Area = new MapArea(input);
            Color = input.ReadBytes(4);
            HashValue = input.ReadInt32();
            ObjectTypeCount = input.ReadInt32();

            ObjectTypeHashes = new int[ObjectTypeCount];
            ObjectInfos = new ObjectInfo[ObjectTypeCount][];
            for(int curObjType = 0; curObjType < ObjectTypeCount; curObjType++)
            {
                var nObjects = input.ReadInt32();
                ObjectTypeHashes[curObjType] = input.ReadInt32();
                ObjectInfos[curObjType] = new ObjectInfo[nObjects];
                for (int obj = 0; obj < nObjects; obj++)
                {
                    ObjectInfos[curObjType][obj] = new ObjectInfo(input);
                }
            }
        }

        public override string ToString()
        {
            var node = $"{Area};{HashValue}:";
            var sb = new StringBuilder(node);
            sb.AppendLine();
            for(int i=0;i < ObjectTypeCount; i++)
            {
                var objType = ObjectTypeHashes[i];
                foreach(var objinfo in ObjectInfos[i])
                {
                    sb.AppendLine($"    {objType};{objinfo}");
                }
            }

            return sb.ToString();
        }
    }

    public class ObjectInfo
    {
        public double X { get; }
        public double Y { get; }
        public float Yaw { get; }
        public float Pitch { get; }
        public float Roll { get; }
        public float Scale { get; }
        public float RelativeElevation { get; }
        public int ID { get; }

        public ObjectInfo(BinaryReaderEx input)
        {
            X = input.ReadDouble();
            Y = input.ReadDouble();
            Yaw = input.ReadSingle();
            Pitch = input.ReadSingle();
            Roll = input.ReadSingle();
            Scale = input.ReadSingle();
            RelativeElevation = input.ReadSingle();
            ID = input.ReadInt32();
        }

        public override string ToString()
        {
            return $"{X:0.###};{Y:0.###};{Yaw:0.###};{Pitch:0.###};{Roll:0.###};{Scale:0.###};{RelativeElevation:0.###};{ID}";
        }
    }
}
