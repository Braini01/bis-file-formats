using Antlr4.Runtime;
using BIS.Core.Streams;

namespace BIS.RAP.Interfaces; 

public interface IRapDeserializable<in T> : IRapSerializable where T : ParserRuleContext {
    public IRapDeserializable<T> ReadBinarized(BinaryReaderEx reader);
    public IRapDeserializable<T> ReadParseTree(T ctx);
}