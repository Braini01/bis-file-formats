using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace BIS.Core
{
    public class QuadTree<TLeaf> : IEnumerable<object> where TLeaf: QuadTreeLeaf<TLeaf>, new()
    {           
        /// <summary>
        /// how many elements exist in X-dimension
        /// </summary>
        private int sizeX;
        /// <summary>
        /// how many elements exist in Y-dimension
        /// </summary>
        private int sizeY;

        /// <summary>
        /// how many virtual elements exist in X-dimension
        /// </summary>
        private int sizeTotalX;
        /// <summary>
        /// how many virtual elements exist in Y-dimension
        /// </summary>
        private int sizeTotalY;

        private const int logSizeX = 2;
        private const int logSizeY = 2;

        private int logSizeTotalX;
        private int logSizeTotalY;

        private int itemLogSizeX;
        private int itemLogSizeY;

        /// <summary>
        /// root of the quadTree that represents the whole area
        /// </summary>
        private QuadTreeNode<TLeaf> root;

        /// <summary>
        /// tells if the root is a leafNode or a tree
        /// </summary>
        private bool flag;

        private IEnumerable<object> allElementsEnumeration;

        public int SizeX { get { return sizeX; } }
        public int SizeY { get { return sizeY; } }


        public QuadTree(int sizeX, int sizeY)
        {
            CalculateDimensions(sizeX, sizeY);
            allElementsEnumeration = from y in Enumerable.Range(0, SizeY)
                                     from x in Enumerable.Range(0, SizeX)
                                     select Get(x, y);
        }

        public void Read(BinaryReader input)
        {
            flag = input.ReadBoolean();
            if (flag)
            {
                root = new QuadTreeNode<TLeaf>();
                root.Read(input);
            }
            else
            {
                root = new TLeaf();
                ((TLeaf)root).Value = input.ReadBytes(4);
            }
        }

        public int Skip(BinaryReader input)
        {
            var sPos = input.BaseStream.Position;

            var isPresent = input.ReadBoolean();
            if (isPresent)
            {
                SkipNode(input);
            }
            else
            {
                input.ReadInt32();
            }

            return (int)(input.BaseStream.Position - sPos);
        }

        protected void SkipNode(BinaryReader input)
        {
            var flags = input.ReadUInt16();
            for (int index = 0; index < 16; ++index)
            {
                if ((flags & 1) == 1)
                {
                    SkipNode(input);
                }
                else
                {
                    input.BaseStream.Position += 4;
                }
                flags >>= 1;
            }
        }

        public object Get(int x, int y)
        {
            if (x < 0 || x >= sizeX) 
                throw new ArgumentOutOfRangeException("x");
            if (y < 0 || y >= sizeY) 
                throw new ArgumentOutOfRangeException("y");

            uint shiftedX = (uint)(x << (8 * sizeof(int) - logSizeTotalX)); // make highest bits accessible on left side
            uint shiftedY = (uint)(y << (8 * sizeof(int) - logSizeTotalY)); // make highest bits accessible on left side
            if (flag)
                return root.Get(x, y, shiftedX, shiftedY);
            else
                return ((TLeaf)root).Get(x, y);
        }

        private void CalculateDimensions(int x, int y)
        {
            sizeX = x;
            sizeY = y;

            x--;
            logSizeTotalX = 0;
            while (x != 0)
            {
                logSizeTotalX++;
                x >>= 1;
            }

            y--;
            logSizeTotalY = 0;
            while (y != 0)
            {
                logSizeTotalY++;
                y >>= 1;
            }

            if (typeof(TLeaf) == typeof(QuadTreeByteLeaf))
            {
                itemLogSizeX = 1;
                itemLogSizeY = 1;
            }
            else if (typeof(TLeaf) == typeof(QuadTreeShortLeaf))
            {
                itemLogSizeX = 1;
                itemLogSizeY = 0;
            }
            else if (typeof(TLeaf) == typeof(QuadTreeIntLeaf))
            {
                itemLogSizeX = 0;
                itemLogSizeY = 0;
            }
            else throw new ArgumentException("Unknown QuadTreeLeafType");
            
            // optimize _logSizeTotalX, _logSizeTotalY
            int numLevelsX = (logSizeTotalX - itemLogSizeX + logSizeX - 1) / logSizeX;
            int numLevelsY = (logSizeTotalY - itemLogSizeY + logSizeY - 1) / logSizeY;

            int numLevels = numLevelsX > numLevelsY ? numLevelsX : numLevelsY;

            logSizeTotalX = numLevels * logSizeX + itemLogSizeX;
            logSizeTotalY = numLevels * logSizeY + itemLogSizeY;
            sizeTotalX = 1 << logSizeTotalX;
            sizeTotalY = 1 << logSizeTotalY;

            // check if quad tree is well-formed
            Debug.Assert(sizeTotalX >= sizeX);
            Debug.Assert(sizeTotalY >= sizeY);
        }

        public IEnumerator<object> GetEnumerator()
        {
            return allElementsEnumeration.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return allElementsEnumeration.GetEnumerator();
        }
    }

    public class QuadTreeNode<TLeaf> where TLeaf : QuadTreeLeaf<TLeaf>, new()
    {
        private const int logSizeX = 2;
        private const int logSizeY = 2;

        /// <summary>
        /// bits determine if subTree is a leaf or not
        /// </summary>
        private short flag;
        private QuadTreeNode<TLeaf>[] subTrees = new QuadTreeNode<TLeaf>[16];

        public object Get(int x, int y, uint shiftedX, uint shiftedY)
        {
            // use (logSize) highest bits of shiftedX, shiftedX as indices
            uint indexX = shiftedX >> (8 * sizeof(int) - logSizeX);
            uint indexY = shiftedY >> (8 * sizeof(int) - logSizeY);
            // 2D to 1D array conversion
            int index = (int)((indexY << logSizeX) + indexX);
            if ((flag & (1 << index)) != 0)
            {
                // move shiftedX, shiftedY to make next bits available
                return subTrees[index].Get(x, y, shiftedX << logSizeX, shiftedY << logSizeY);
            }
            else
            {
                //ToDo: adapt for different T; currently useable for 4-byte values
                var itemLogSizeX = 0;
                var itemLogSizeY = 0;

                // mask only lowest bits from the original x, y
                int maskX = (1 << itemLogSizeX) - 1;
                int maskY = (1 << itemLogSizeY) - 1;
                return ((TLeaf)subTrees[index]).Get(x & maskX, y & maskY);
            }
        }

        public void Read(BinaryReader input)
        {
            flag = input.ReadInt16();
            var bitMask = flag;
            for (int i = 0; i < 16; i++)
            {
                if ((bitMask & 1) == 1)
                {
                    subTrees[i] = new QuadTreeNode<TLeaf>();
                    subTrees[i].Read(input);
                }
                else
                {
                    subTrees[i] = new TLeaf();
                    ((TLeaf)subTrees[i]).Value = input.ReadBytes(4);
                }
                bitMask >>= 1;
            }
        }
    }

    public abstract class QuadTreeLeaf<TLeaf> : QuadTreeNode<TLeaf> where TLeaf : QuadTreeLeaf<TLeaf>, new()
    {
        protected byte[] value = new byte[4];

        public byte[] Value { get { return value; } set { this.value = value; } }
        public abstract object Get(int x, int y);
    }

    public class QuadTreeIntLeaf : QuadTreeLeaf<QuadTreeIntLeaf>
    {
        public override object Get(int x, int y)
        {
            Debug.Assert(x == 0);
            Debug.Assert(y == 0);
            return BitConverter.ToInt32(value, 0);
        }
    }

    public class QuadTreeShortLeaf : QuadTreeLeaf<QuadTreeShortLeaf>
    {
        public override object Get(int x, int y)
        {
            Debug.Assert(x == 0 || x == 1);
            Debug.Assert(y == 0);
            return BitConverter.ToInt16(value, x * 2);
        }
    }

    public class QuadTreeByteLeaf : QuadTreeLeaf<QuadTreeByteLeaf>
    {
        public override object Get(int x, int y)
        {
            Debug.Assert(x == 0 || x == 1);
            Debug.Assert(y == 0 || y == 1);
            return value[(y << 1) + x];
        }
    }
}
