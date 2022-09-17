using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using BIS.Core.Compression;

namespace BIS.Core.Streams
{
    public class BinaryReaderEx : BinaryReader
    {
        public bool UseCompressionFlag { get; set; }
        public bool UseLZOCompression { get; set; }
        public bool AllowArrayTracking { get; set; } = true;

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
        
        public int ReadCompressedInteger() 
        {
            var value = 0;
            for (var i = 0;; ++i)
            {
                var v = ReadByte();
                value |= v & 0x7F << (7 * i);
                if((v & 0x80) == 0) break;
            }
            return value;
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

        public string ReadUTF8z()
        {
            var str = new List<byte>();
            byte ch;
            while ((ch = ReadByte()) != 0)
                str.Add(ch);
            return Encoding.UTF8.GetString(str.ToArray());
        }

        public TrackedArray<T> ReadTracked<T>(Func<BinaryReaderEx, T[]> read)
        {
            if (!AllowArrayTracking)
            {
                return new TrackedArray<T>(read(this), null);
            }

            var startPosition = Position;
            var value = read(this);
            var endPosition = Position;
            Position = startPosition;
            var bytes = ReadBytes((int)(endPosition - startPosition));
            Position = endPosition;
            return new TrackedArray<T>(value, bytes);
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

        public TrackedArray<T> ReadCompressedArrayTracked<T>(Func<BinaryReaderEx, T> readElement, int elemSize)
        {
            return ReadTracked(r => r.ReadCompressedArray(readElement, elemSize));
        }

        public short[] ReadCompressedShortArray() => ReadCompressedArray(i => i.ReadInt16(), 2);
        public int[] ReadCompressedIntArray() => ReadCompressedArray(i => i.ReadInt32(), 4);        
        public float[] ReadCompressedFloatArray() => ReadCompressedArray(i => i.ReadSingle(), 4);
        public byte[] ReadCompressedByteArray() => ReadCompressedArray(i => i.ReadByte(), 1);

        public TrackedArray<int> ReadCompressedIntArrayTracked()
        {
            return ReadTracked(r => r.ReadCompressedIntArray());
        }
        public TrackedArray<float> ReadCompressedFloatArrayTracked()
        {
            return ReadTracked(r => r.ReadCompressedFloatArray());
        }

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


        public TrackedArray<T> ReadCondensedArrayTracked<T>(Func<BinaryReaderEx, T> readElement, int elemSize)
        {
            return ReadTracked(r => r.ReadCondensedArray(readElement, elemSize));
        }

        public TrackedArray<int> ReadCondensedIntArrayTracked()
        {
            return ReadTracked(r => r.ReadCondensedIntArray());
        }

        #endregion

        public int ReadCompactInteger()
        {
            int result = 0;
            int i = 0;
            bool end;
            do
            {
                int b = ReadByte();
                result |= (b & 0x7f) << (i * 7);
                end = b < 0x80;
                i++;
            } while (!end);
            return result;
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

        public TrackedArray<byte> ReadCompressedTracked(uint expectedSize, bool forceCompressed = false)
        {
            return ReadTracked(r => r.ReadCompressed(expectedSize, forceCompressed));
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

        public byte[] ReadLZSS(uint expectedSize, bool inPAA = false)
        {
            if (expectedSize < 1024 && !inPAA) //data is always compressed in PAAs
            {
                return ReadBytes((int)expectedSize);
            }
            else
            {
                // XXX: Needs testing
                //var buffer = new byte[expectedSize];
                //using (var lzss = new LzssStream(BaseStream, CompressionMode.Decompress, true))
                //{
                //    lzss.Read(buffer, 0, (int)expectedSize);
                //}
                //Chesksum(inPAA, buffer); //PAAs calculate checksums with signed byte values
                byte[] buffer;
                LZSS.ReadLZSS(BaseStream, out buffer, expectedSize, inPAA);
                return buffer;
            }
        }

        // XXX: Needs testing
        //private void Chesksum(bool useSignedChecksum, byte[] buffer)
        //{
        //    var csum = useSignedChecksum ? buffer.Sum(e => (int)(sbyte)e) : buffer.Sum(e => (int)(byte)e);
        //    var csData = new byte[4];
        //    BaseStream.Read(csData, 0, 4);
        //    int csr = BitConverter.ToInt32(csData, 0);
        //    if (csr != csum)
        //    {
        //        throw new ArgumentException("Checksum mismatch");
        //    }
        //}

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

        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Vector3 ReadVector3Compressed()
        {
            var value = ReadUInt32();
            double scaleFactor = -1.0 / 511;
            uint x = value & 0x3FF;
            uint y = (value >> 10) & 0x3FF;
            uint z = (value >> 20) & 0x3FF;
            if (x > 511) { x -= 1024; }
            if (y > 511) { y -= 1024; }
            if (z > 511) { z -= 1024; }
            return new Vector3((float)(x * scaleFactor), (float)(y * scaleFactor), (float)(z * scaleFactor));
        }
    }
}
