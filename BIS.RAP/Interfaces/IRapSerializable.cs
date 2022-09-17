using BIS.Core.Streams;

namespace BIS.RAP.Interfaces; 

public interface IRapSerializable {
    public void WriteBinarized(BinaryWriterEx writer);
    public string ToParseTree();
}