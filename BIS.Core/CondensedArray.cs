using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIS.Core
{
    public class CondensedArray<T> : IEnumerable<T>
    {
        private int nElements;
        private bool isDefault;
        private T defaultValue;
        private T[] array;

        public bool IsCondensed => isDefault;
        public T DefaultValue => defaultValue;
        public int ElementCount => nElements;

        public CondensedArray(int nElements, T value)
        {
            this.nElements = nElements;
            isDefault = true;
            defaultValue = value;
            array = null;
        }

        public CondensedArray(T[] array)
        {
            nElements = array.Length;
            isDefault = false;
            defaultValue = default(T);
            this.array = array;
        }

        public T[] AsArray()
        {
            return (isDefault) ? Enumerable.Range(0,nElements).Select(_ => defaultValue).ToArray() : array;
        }

        public T this[int i]
        {
            get
            {
                if (isDefault) return defaultValue;
                else return array[i];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if(isDefault)
            {
                for (int i = 0; i < nElements; i++)
                    yield return defaultValue;
            }
            else
            {
                for (int i = 0; i < nElements; i++)
                    yield return array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
