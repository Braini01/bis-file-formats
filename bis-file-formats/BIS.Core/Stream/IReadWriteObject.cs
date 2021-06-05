using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.Core.Streams
{
    public interface IReadWriteObject : IReadObject
    {
        void Write(BinaryWriterEx output);
    }
}
