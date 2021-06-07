using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

        public bool HasReachedEnd => BaseStream.Position == BaseStream.Length;

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
            var str = new StringBuilder();
            for (int index = 0; index < count; ++index)
                str.Append((char)ReadByte());
            return str.ToString();
        }

        public string ReadAscii()
        {
            var n = ReadUInt16();
            return ReadAscii(n);
        }

        public string ReadAscii32()
        {
            var n = ReadUInt32();
            return ReadAscii((int)n);
        }

        public string ReadAsciiz()
        {
            var str = new StringBuilder();
            char ch;
            while ((ch = (char)ReadByte()) != 0)
                str.Append(ch);
            return str.ToString();
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
            return ReadCompressed<T>(readElement, nElements, elemSize);
        }

        public short[] ReadCompressedShortArray() => ReadCompressedArray(i => i.ReadInt16(), 2);
        public int[] ReadCompressedIntArray() => ReadCompressedArray(i => i.ReadInt32(), 4);        
        public float[] ReadCompressedFloatArray() => ReadCompressedArray(i => i.ReadSingle(), 4);
        public byte[] ReadCompressedByteArray() => ReadCompressedArray(i => i.ReadByte(), 1);

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

        public byte[] ReadCompressed(uint expectedSize, bool forceCompressed = false)
        {
            if (expectedSize == 0)
            {
                return new byte[0];
            }

            if (UseLZOCompression) return ReadLZO(expectedSize, forceCompressed);

            return ReadLZSS(expectedSize);
        }

        public byte[] ReadLZO(uint expectedSize, bool forceCompressed = false)
        {
            bool isCompressed = (expectedSize >= 1024) || forceCompressed;
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

#if DEBUG
        private void CheckLegacy(byte[] legacy, byte[] buffer, long legacyPos)
        {
            if (Position != legacyPos)
            {
                throw new IOException("Position is not the same");
            }
            if (legacy.Zip(buffer, (a,b) => a != b).Any())
            {
                throw new IOException("Bytes mismatch");
            }
        }
#endif

        public byte[] ReadLZSS(uint expectedSize, bool inPAA = false)
        {
            if (expectedSize < 1024 && !inPAA) //data is always compressed in PAAs
            {
                return ReadBytes((int)expectedSize);
            }
            else
            {
                var initialPos = BaseStream.Position;
#if DEBUG
                byte[] legacy;
                LZSS.ReadLZSS(BaseStream, out legacy, expectedSize, inPAA);

                var legacyPos = BaseStream.Position;

                BaseStream.Seek(initialPos, SeekOrigin.Begin);
#endif
                var buffer = new byte[expectedSize];
                using (var lzss = new LzssStream(BaseStream, CompressionMode.Decompress, true))
                {
                    lzss.Read(buffer, 0, (int)expectedSize);
                }
                Chesksum(inPAA, buffer); //PAAs calculate checksums with signed byte values
#if DEBUG
                CheckLegacy(legacy, buffer, legacyPos);
#endif
                return buffer;
            }
        }

        private void Chesksum(bool useSignedChecksum, byte[] buffer)
        {
            var csum = useSignedChecksum ? buffer.Sum(e => (int)(sbyte)e) : buffer.Sum(e => (int)(byte)e);
            var csData = new byte[4];
            BaseStream.Read(csData, 0, 4);
            int csr = BitConverter.ToInt32(csData, 0);
            if (csr != csum)
            {
                throw new ArgumentException("Checksum mismatch");
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

        public float[] ReadCompressedFloats(int nElements)
        {
            return ReadCompressed(r => r.ReadSingle(), nElements, 4);
        }

        public float[] ReadFloats(int nElements)
        {
            return ReadArrayBase(r => r.ReadSingle(), nElements);
        }

        public ushort[] ReadUshorts(int nElements)
        {
            return ReadArrayBase(r => r.ReadUInt16(), nElements);
        }

        public T[] ReadCompressed<T>(Func<BinaryReaderEx, T> readElement, int nElements, int elemSize)
        {
            var expectedDataSize = (uint)(nElements * elemSize);
            var stream = new BinaryReaderEx(new MemoryStream(ReadCompressed(expectedDataSize)));
            return stream.ReadArrayBase(readElement, nElements);
        }
    }
}
