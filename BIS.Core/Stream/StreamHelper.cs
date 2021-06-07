using System.IO;

namespace BIS.Core.Streams
{
    public static class StreamHelper
    {
        private static MemoryStream MakeBuffer(Stream stream)
        {
            var ms = new MemoryStream((int)stream.Length);
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        public static T Read<T> (Stream input) where T : IReadObject, new()
        {
            var o = new T();
            o.Read(new BinaryReaderEx(MakeBuffer(input)));
            return o;
        }

        public static T Read<T>(string filename) where T : IReadObject, new()
        {
            using(var input = File.OpenRead(filename))
            {
                return Read<T>(input);
            }
        }

        public static T ReadNoBuffer<T>(Stream input) where T : IReadObject, new()
        {
            var o = new T();
            o.Read(new BinaryReaderEx(MakeBuffer(input)));
            return o;
        }

        public static T ReadNoBuffer<T>(string filename) where T : IReadObject, new()
        {
            using (var input = File.OpenRead(filename))
            {
                return Read<T>(input);
            }
        }

        public static void Write<T>(this T value, string filename) where T : IReadWriteObject
        {
            using (var output = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                Write<T>(value, output);
            }
        }

        public static void Write<T>(this T value, Stream stream) where T : IReadWriteObject
        {
            value.Write(new BinaryWriterEx(stream));
        }
    }
}
