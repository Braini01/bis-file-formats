using System.IO;

namespace BIS.Core.Streams
{
    public class BinaryWriterEx : BinaryWriter
    {
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

        public BinaryWriterEx(Stream dstStream): base(dstStream){}

        public void WriteAscii(string text, uint len)
        {
            Write(text.ToCharArray());
            uint num = (uint)(len - text.Length);
            for (int index = 0; index < num; ++index)
                Write(char.MinValue); //ToDo: check encoding, should always write one byte and never two or more
        }

        public void WriteAsciiz(string text)
        {
            Write(text.ToCharArray());
            Write(char.MinValue);
        }
    }
}
