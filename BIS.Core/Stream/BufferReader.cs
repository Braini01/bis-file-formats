using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;
using BIS.Core.Compression;
using System.Runtime.InteropServices;

namespace BIS.Core.Streams
{
    public class BufferReader
    {
        private byte[] buffer;
        private int pos;

        public bool UseLZOCompression { get; set; }
        public bool UseCompressionFlag { get; set; }
        public int Version { get; set; }


        public BufferReader(byte[] buffer)
        {
            this.buffer = buffer;
            pos = 0;
        }

        public BufferReader(ReadOnlySpan<byte> bufferSpan)
        {
            this.buffer = bufferSpan.ToArray();
            pos = 0;
        }

        public ReadOnlySpan<byte> ReadSpan(int len)
        {
            var span = buffer.AsSpan().Slice(pos, len);
            pos += len;
            return span;
        }

        public T Read<T>() where T : struct
        {
            int elemSize = Marshal.SizeOf(typeof(T));
            var result = MemoryMarshal.Read<T>(buffer.AsSpan(pos, elemSize));
            pos += elemSize;
            return result;
        }

        #region Compression
        public ReadOnlySpan<byte> ReadCompressed(int expectedSize)
        {
            if (expectedSize == 0)
            {
                return new byte[0];
            }

            if (UseLZOCompression) return ReadLZO(expectedSize);

            return ReadLZSS(expectedSize);
        }
        public ReadOnlySpan<byte> ReadLZO(int expectedSize)
        {
            bool isCompressed = (expectedSize >= 1024);
            if (UseCompressionFlag)
            {
                isCompressed = buffer[pos++] != 0;
            }

            if (!isCompressed)
            {
                return ReadSpan(expectedSize);
            }

            var output = LZO.Decompress(buffer.AsSpan(pos), expectedSize, out int bytesRead);
            pos += bytesRead;
            return output;
        }
        public ReadOnlySpan<byte> ReadLZSS(int expectedSize, bool inPAA = false)
        {
            if (expectedSize < 1024 && !inPAA) //data is always compressed in PAAs
            {
                return ReadSpan(expectedSize);
            }
            else
            {
                var dst = LZSS.ReadLZSS(buffer.AsSpan(pos), expectedSize, inPAA, out int bytesRead);
                pos += bytesRead;
                return dst;
            }
        }
        #endregion

        public T[] ReadCompressedArray<T>() where T: struct
        {
            int nElements = BitConverter.ToInt32(buffer, pos);
            pos += 4;
            int elemSize = Marshal.SizeOf(typeof(T));
            var expectedDataSize = nElements * elemSize;
            var decompressedSpan = ReadCompressed(expectedDataSize);

            return MemoryMarshal.Cast<byte, T>(decompressedSpan).ToArray();
        }

        public CondensedArray<T> ReadCondensedArray<T>() where T : struct
        {
            int nElements = BitConverter.ToInt32(buffer, pos);
            pos += 4;
            bool defaultFill = buffer[pos++] != 0;
            var elemSize = Marshal.SizeOf(typeof(T));
            if (defaultFill)
            {
                var defaultValue = MemoryMarshal.Read<T>(buffer.AsSpan(pos, elemSize));
                pos += elemSize;

                return new CondensedArray<T>(nElements, defaultValue);
            }

            var expectedDataSize = nElements * elemSize;
            var decompressedSpan = ReadCompressed(expectedDataSize);
            var data = MemoryMarshal.Cast<byte, T>(decompressedSpan).ToArray();
            return new CondensedArray<T>(data);
        }
    }
}
