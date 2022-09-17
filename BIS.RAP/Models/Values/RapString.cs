using System.Text;
using Antlr4.Runtime.Misc;
using BIS.Core.Streams;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;

namespace BIS.RAP.Models.Values; 

public sealed class RapString : IRapDeserializable<ParamFileParser.LiteralStringContext>, IRapLiteral, IRapArrayEntry {
    public string Value { get; set; } = string.Empty;
    public static implicit operator RapString(string s) => new(s);
    public static implicit operator string(RapString s) => s.Value;
    public RapString(string s) => Value = s;
    public RapString() { }
    public void WriteBinarized(BinaryWriterEx writer) => writer.WriteAsciiz(Value);
    public string ToParseTree() => new StringBuilder().Append('"').Append(Value).Append('"').ToString();

    public IRapDeserializable<ParamFileParser.LiteralStringContext> ReadBinarized(BinaryReaderEx reader) {
        Value = reader.ReadAsciiz(); 
        return this;
    }

    public IRapDeserializable<ParamFileParser.LiteralStringContext> ReadParseTree(ParamFileParser.LiteralStringContext ctx) {
        Value = ctx.Start.InputStream.GetText(new Interval(ctx.Start.StartIndex, ctx.Stop.StopIndex)).TrimStart('"').TrimEnd('"');
        return this;
    }
}