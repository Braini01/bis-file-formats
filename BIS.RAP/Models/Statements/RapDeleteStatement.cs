using System.Text;
using Antlr4.Runtime.Misc;
using BIS.Core.Streams;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;

namespace BIS.RAP.Models.Statements; 

public class RapDeleteStatement : IRapStatement, IRapDeserializable<ParamFileParser.DeleteStatementContext> {
    private string Target { get; set; } = string.Empty;

    public void WriteBinarized(BinaryWriterEx writer) {
        writer.Write((byte) 4);
        writer.WriteAsciiz(Target);
    }

    public string ToParseTree() => new StringBuilder("delete ").Append(Target).Append(';').ToString();

    public IRapDeserializable<ParamFileParser.DeleteStatementContext> ReadBinarized(BinaryReaderEx reader) {
        if (reader.ReadByte() != 4) throw new Exception("Expected delete statement.");
        Target = reader.ReadAsciiz();
        return this;
    }

    public IRapDeserializable<ParamFileParser.DeleteStatementContext> ReadParseTree(ParamFileParser.DeleteStatementContext ctx) {
        if (ctx.identifier() is not { } identifier) throw new Exception("Nothing was given to delete.");
        Target = ctx.Start.InputStream.GetText(new Interval(identifier.Start.StartIndex, identifier.Stop.StopIndex));
        return this;
    }
}