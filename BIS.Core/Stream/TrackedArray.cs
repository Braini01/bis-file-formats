using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BIS.Core
{
    public class TrackedArray<T> : IReadOnlyList<T>
    {
        public TrackedArray()
        {
            Value = new T[0];
        }

        public TrackedArray(IEnumerable<T> value)
        {
            Value = value.ToArray();
        }

        internal TrackedArray(T[] value, byte[] originCompressedData)
        {
            Value = value;
            OriginCompressedData = originCompressedData;
        }

        public T this[int index] => Value[index];

        internal T[] Value { get; }

        public byte[] OriginCompressedData { get; }

        public int Count => Value.Length;

        public IEnumerator<T> GetEnumerator()
        {
            return Value.AsEnumerable<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Value.GetEnumerator();
        }
    }
}
