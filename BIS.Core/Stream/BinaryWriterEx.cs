using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using BIS.Core.Compression;
using BIS.Core.Math;

namespace BIS.Core.Streams
{
    public class BinaryWriterEx : BinaryWriter
    {
        public bool UseCompressionFlag { get; set; }
        public bool UseLZOCompression { get; set; }
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


        public BinaryWriterEx(Stream dstStream) : base(dstStream, Encoding.ASCII) { }

        public BinaryWriterEx(Stream dstStream, bool leaveOpen): base(dstStream, Encoding.ASCII, leaveOpen) {}

        public void WriteAscii(string text, uint len)
        {
            Write(text.ToCharArray());
            uint num = (uint)(len - text.Length);
            for (int index = 0; index < num; ++index)
                Write(char.MinValue); //ToDo: check encoding, should always write one byte and never two or more
        }

        public void WriteAscii32(string text)
        {
            Write(text.Length);
            Write(text.ToCharArray());
        }

        public void WriteAsciiz(string text)
        {
            Write(text.ToCharArray());
            Write(char.MinValue);
        }


        public void WriteArray<T>(T[] array, Action<BinaryWriterEx, T> write)
        {
            Write(array.Length);
            WriteArrayBase(array, write);
        }

        public void WriteArray(float[] array)
        {
            WriteArray(array, (w, f) => w.Write(f));
        }

        public void WriteArray(int[] array)
        {
            WriteArray(array, (w, f) => w.Write(f));
        }

        public void WriteArrayBase<T>(T[] array, Action<BinaryWriterEx, T> write)
        {
            foreach (var item in array)
            {
                write(this, item);
            }
        }

        public void WriteArrayBase(float[] array)
        {
            WriteArrayBase(array, (w, f) => w.Write(f));
        }

        public void WriteArrayBase(int[] array)
        {
            WriteArrayBase(array, (w, f) => w.Write(f));
        }

        public void WriteCompressedFloatArray(float[] array)
        {
            WriteCompressedArray(array, (w, v) => w.Write(v), 4);
        }

        public void WriteCompressedIntArray(int[] array)
        {
            WriteCompressedArray(array, (w, v) => w.Write(v), 4);
        }

        public void WriteCompressedIntArray(TrackedArray<int> array)
        {
            WriteCompressedArray(array, (w, v) => w.Write(v), 4);
        }

        public void WriteCompressedArray<T>(TrackedArray<T> array, Action<BinaryWriterEx, T> write, int size, bool forceCompressed = false)
        {
            if (array.OriginCompressedData != null)
            {
                Write(array.OriginCompressedData);
            }
            else
            {
                WriteCompressedArray(array.Value, write, size, forceCompressed);
            }
        }

        public void WriteCompressedArray<T>(T[] array, Action<BinaryWriterEx, T> write, int size, bool forceCompressed = false)
        {
            var mem = new MemoryStream();
            using (var writer = new BinaryWriterEx(mem))
            {
                writer.WriteArrayBase(array, write);
            }
            Write(array.Length);
            var bytes = mem.ToArray();
            if (array.Length * size != bytes.Length)
            {
                throw new InvalidOperationException();
            }
            WriteCompressed(bytes, forceCompressed);
        }

        public void WriteCompressed(TrackedArray<byte> bytes, bool forceCompressed = false)
        {
            if (bytes.OriginCompressedData != null)
            {
                Write(bytes.OriginCompressedData);
            }
            else
            {
                WriteCompressed(bytes.Value, forceCompressed);
            }
        }

        public void WriteCompressed(byte[] bytes, bool forceCompressed = false)
        {
            if (bytes.Length == 0)
            {
                return;
            }
            if (UseLZOCompression)
            {
                WriteLZO(bytes, forceCompressed);
            }
            else
            {
                WriteLZSS(bytes);
            }
        }


        public void WriteLZO(byte[] bytes, bool forceCompressed = false)
        {
            if (bytes.Length < 1024 && !forceCompressed)
            {
                if (UseCompressionFlag)
                {
                    Write((byte)0);
                }
                Write(bytes);
            }
            else
            {
                if (UseCompressionFlag)
                {
                    Write((byte)2);
                }
                Write(MiniLZO.MiniLZO.Compress(bytes));
            }
        }

        public void WriteLZSS(byte[] bytes, bool inPAA = false)
        {
            if (bytes.Length < 1024 && !inPAA) //data is always compressed in PAAs
            {
                Write(bytes);
            }
            else
            {
                var csum = inPAA ? bytes.Sum(e => (int)(sbyte)e) : bytes.Sum(e => (int)(byte)e);
                using (var lzss = new LzssStream(BaseStream, System.IO.Compression.CompressionMode.Compress, true))
                {
                    lzss.Write(bytes, 0, bytes.Length);
                }
                Write(BitConverter.GetBytes(csum));
            }
            
        }
        
        public void WriteCompressedInt(int data) {
            do {
                var current = data % 0x80;
                data = (int) System.Math.Floor((decimal)data / 0x80);
                if (data is not 0) current = current | 0x80; 
                Write((byte) current);
            } while (data > 0x7F);

            if (data is not 0) Write((byte) data);
        }


        public void WriteUInt24(uint length)
        {
            Write((byte)(length & 0xFF));
            Write((byte)((length >> 8) & 0xFF));
            Write((byte)((length >> 16) & 0xFF));
        }

        public void WriteFloats(float[] elements)
        {
            WriteArrayBase(elements, (r, e) => r.Write(e));
        }

        public void WriteUshorts(ushort[] elements)
        {
            WriteArrayBase(elements, (r,e) => r.Write(e));
        }
        public void Write(Vector3 value)
        {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }

        public void WriteCondensedIntArray(int[] value)
        {
            WriteCondensedArray(value, (o, v) => o.Write(v), 4);
        }

        public void WriteCondensedIntArray(TrackedArray<int> value)
        {
            WriteCondensedArray(value, (o, v) => o.Write(v), 4);
        }

        public void WriteCondensedArray<T>(TrackedArray<T> array, Action<BinaryWriterEx, T> write, int sizeOf)
            where T : IEquatable<T>
        {
            if (array.OriginCompressedData != null)
            {
                Write(array.OriginCompressedData);
            }
            else
            {
                WriteCondensedArray(array.Value, write, sizeOf);
            }
        }

        public void WriteCondensedArray<T>(T[] array, Action<BinaryWriterEx, T> write, int sizeOf) 
            where T : IEquatable<T>
        {

            var distinct = array.Distinct().ToList();
            if (distinct.Count == 1)
            {
                Write(array.Length);
                Write(true);
                write(this, distinct[0]);
            }
            else
            {

                var mem = new MemoryStream();
                using (var writer = new BinaryWriterEx(mem))
                {
                    writer.WriteArrayBase(array, write);
                }
                var bytes = mem.ToArray();
                if (array.Length * sizeOf != bytes.Length)
                {
                    throw new InvalidOperationException();
                }
                Write(array.Length);
                Write(false);
                WriteCompressed(mem.ToArray());
            }
        }
    }
}
