using System.Text;
using Antlr4.Runtime.Misc;
using BIS.Core.Streams;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;
using BIS.RAP.Models.Values;

namespace BIS.RAP.Models.Declarations; 

public class RapArrayDeclaration :  IRapStatement, IRapDeserializable<ParamFileParser.ArrayDeclarationContext> {
    public string ArrayName { get; set; } = string.Empty;
    public RapArray ArrayValue { get; set; } = new();
    
    public void WriteBinarized(BinaryWriterEx writer) {
        writer.Write((byte) 2);
        writer.WriteAsciiz(ArrayName);
        ArrayValue.WriteBinarized(writer);
    }

    public string ToParseTree() => new StringBuilder(ArrayName).Append("[] = ").Append(ArrayValue.ToParseTree()).Append(';').ToString();

    public IRapDeserializable<ParamFileParser.ArrayDeclarationContext> ReadBinarized(BinaryReaderEx reader) {
        if (reader.ReadByte() != 2) throw new Exception("Expected external class.");
        ArrayName = reader.ReadAsciiz();
        ArrayValue.ReadBinarized(reader);
        return this;
    }

    public IRapDeserializable<ParamFileParser.ArrayDeclarationContext> ReadParseTree(ParamFileParser.ArrayDeclarationContext ctx) {
        if (ctx.arrayName() is not { } arrayNameCtx) throw new Exception();
        if (ctx.literalArray() is not { } literalArrayCtx) throw new Exception();
        var name = arrayNameCtx.identifier() ?? throw new Exception();
        ArrayName = ctx.Start.InputStream.GetText(new Interval(name.Start.StartIndex, name.Stop.StopIndex));
        ArrayValue.ReadParseTree(literalArrayCtx);
        return this;
    }
}