using System.Text;
using Antlr4.Runtime.Misc;
using BIS.Core.Streams;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;
using BIS.RAP.Models.Values;

namespace BIS.RAP.Models.Statements; 

public class RapAppensionStatement : IRapStatement, IRapDeserializable<ParamFileParser.ArrayAppensionContext> {
    public string Target { get; set; } = string.Empty;
    public RapArray Array { get; set; } = new();
    
    public void WriteBinarized(BinaryWriterEx writer) {
        writer.Write((byte) 5);
        writer.Write((int) 1);
        writer.WriteAsciiz(Target);
        Array.WriteBinarized(writer);
    }

    public string ToParseTree() => new StringBuilder(Target).Append("[] += ").Append(Array.ToParseTree()).Append(';').ToString();

    public IRapDeserializable<ParamFileParser.ArrayAppensionContext> ReadBinarized(BinaryReaderEx reader) {
        if (reader.ReadByte() != 5) throw new Exception("Expected array appension.");
        if (reader.ReadInt32() != 1) throw new Exception("Expected array appension. (1)");
        Target = reader.ReadAsciiz();
        Array.ReadBinarized(reader);
        return this;
    }

    public IRapDeserializable<ParamFileParser.ArrayAppensionContext> ReadParseTree(ParamFileParser.ArrayAppensionContext ctx) {
        if (ctx.arrayName() is not { } arrayNameCtx) throw new Exception();
        if (ctx.literalArray() is not { } literalArrayCtx) throw new Exception();
        Target = ctx.Start.InputStream.GetText(new Interval(arrayNameCtx.identifier().Start.StartIndex, arrayNameCtx.identifier().Stop.StopIndex));
        Array.ReadParseTree(literalArrayCtx);
        return this;
    }
}