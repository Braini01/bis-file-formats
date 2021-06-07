using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.Core.Streams
{
    public interface IReadObject
    {
        void Read(BinaryReaderEx input);
    }
}
