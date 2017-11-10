using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

using BIS.Core.Compression;

namespace BIS.Core.Streams
{
    public class BinaryReaderEx : BinaryReader
    {
        public bool UseCompressionFlag { get; set; }
        public bool UseLZOCompression { get; set; }

        //used to store file format versions (e.g. ODOL v60)
        public int Version { get; set; }

        public long Position
        {
            get
            {
                return BaseStream.Position;
            }
            set
            {
                BaseStream.Position = value;
            }
        }

        public BinaryReaderEx(Stream stream): base(stream)
        {
            UseCompressionFlag = false;
        }

        public uint ReadUInt24()
        {
            return (uint)(ReadByte() + (ReadByte() << 8) + (ReadByte() << 16));
        }

        public string ReadAscii(int count)
        {
            string str = "";
            for (int index = 0; index < count; ++index)
                str = str + (char)ReadByte();
            return str;
        }

        public string ReadAscii()
        {
            var n = ReadUInt16();
            return ReadAscii(n);
        }

        public string ReadAsciiz()
        {
            string str = "";
            char ch;
            while ((ch = (char)ReadByte()) != 0)
                str = str + ch;
            return str;
        }

        #region SimpleArray
        public T[] ReadArrayBase<T>(Func<BinaryReaderEx, T> readElement, int size)
        {
            var array = new T[size];
            for (int i = 0; i < size; i++)
                array[i] = readElement(this);

            return array;
        }

        public T[] ReadArray<T>(Func<BinaryReaderEx, T> readElement) => ReadArrayBase(readElement, ReadInt32());
        public float[] ReadFloatArray() => ReadArray(i => i.ReadSingle());
        public int[] ReadIntArray() => ReadArray(i => i.ReadInt32());
        public string[] ReadStringArray() => ReadArray(i => i.ReadAsciiz());

        #endregion

        #region CompressedArray
        public T[] ReadCompressedArray<T>(Func<BinaryReaderEx, T> readElement, int elemSize)
        {
            int nElements = ReadInt32();
            var expectedDataSize = (uint)(nElements * elemSize);
            var stream = new BinaryReaderEx(new MemoryStream(ReadCompressed(expectedDataSize)));

            return stream.ReadArrayBase(readElement, nElements);
        }

        public T[] ReadCompressedArray<T>(Func<BinaryReaderEx, T> readElement) => ReadCompressedArray(readElement, Marshal.SizeOf(typeof(T)));
        public short[] ReadCompressedShortArray() => ReadCompressedArray(i => i.ReadInt16());
        public int[] ReadCompressedIntArray() => ReadCompressedArray(i => i.ReadInt32());        
        public float[] ReadCompressedFloatArray() => ReadCompressedArray(i => i.ReadSingle());

        #endregion

        #region CondensedArray

        public T[] ReadCondensedArray<T>(Func<BinaryReaderEx, T> readElement, int sizeOfT)
        {
            int size = ReadInt32();
            T[] result = new T[size];
            bool defaultFill = ReadBoolean();
            if (defaultFill)
            {
                var defaultValue = readElement(this);
                for (int i = 0; i < size; i++)
                    result[i] = defaultValue;

                return result;
            }

            var expectedDataSize = (uint)(size * sizeOfT);
            using (var stream = new BinaryReaderEx(new MemoryStream(ReadCompressed(expectedDataSize))))
            {
                result = stream.ReadArrayBase(readElement, size);
            }

            return result;
        }

        public int[] ReadCondensedIntArray() => ReadCondensedArray(i => i.ReadInt32(), 4);
        #endregion

        public int ReadCompactInteger()
        {
            int val = ReadByte();
            if ((val & 0x80) != 0)
            {
                int extra = ReadByte();
                val += (extra - 1) * 0x80;
            }
            return val;
        }

        public byte[] ReadCompressed(uint expectedSize)
        {
            if (expectedSize == 0)
            {
                return new byte[0];
            }

            if (UseLZOCompression) return ReadLZO(expectedSize);

            return ReadLZSS(expectedSize);
        }

        public byte[] ReadLZO(uint expectedSize)
        {
            bool isCompressed = (expectedSize >= 1024);
            if (UseCompressionFlag)
            {
                isCompressed = ReadBoolean();
            }

            if (!isCompressed)
            {
                return ReadBytes((int)expectedSize);
            }

            return LZO.ReadLZO(BaseStream, expectedSize);
        }

        public byte[] ReadLZSS(uint expectedSize, bool inPAA = false)
        {
            if (expectedSize < 1024 && !inPAA) //data is always compressed in PAAs
            {
                return ReadBytes((int)expectedSize);
            }
            else
            {
                var dst = new byte[expectedSize];
                LZSS.ReadLZSS(BaseStream, out dst, expectedSize, inPAA); //PAAs calculate checksums with signed byte values
                return dst;
            }
        }

        public byte[] ReadCompressedIndices(int bytesToRead, uint expectedSize)
        {
            var result = new byte[expectedSize];
            int outputI = 0;
            for(int i=0;i<bytesToRead;i++)
            {
                var b = ReadByte();
                if( (b & 128) != 0 )
                {
                    byte n = (byte)(b - 127);
                    byte value = ReadByte();
                    for (int j = 0; j < n; j++)
                        result[outputI++] = value;
                }
                else
                {
                    for (int j = 0; j < b + 1; j++)
                        result[outputI++] = ReadByte();
                }
            }

            Debug.Assert(outputI == expectedSize);

            return result;
        }
    }
}
