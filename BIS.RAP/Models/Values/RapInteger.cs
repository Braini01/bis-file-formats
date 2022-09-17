using Antlr4.Runtime.Misc;
using BIS.Core.Streams;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;

namespace BIS.RAP.Models.Values; 

public sealed class RapInteger : IRapDeserializable<ParamFileParser.LiteralIntegerContext>, IRapLiteral, IRapArrayEntry {
    public int Value { get; set; } = 0;
    public static implicit operator RapInteger (int s) => new() { Value = s };
    public static implicit operator int (RapInteger s) => s.Value;
    public RapInteger(int value) => Value = value;
    public RapInteger() { }
    public void WriteBinarized(BinaryWriterEx writer) => writer.Write(Value);
    public string ToParseTree() => Value.ToString();
    public IRapDeserializable<ParamFileParser.LiteralIntegerContext> ReadParseTree(ParamFileParser.LiteralIntegerContext ctx) { Value = int.Parse(ctx.Start.InputStream.GetText(new Interval(ctx.Start.StartIndex, ctx.Stop.StopIndex))); return this; }
    public IRapDeserializable<ParamFileParser.LiteralIntegerContext> ReadBinarized(BinaryReaderEx reader) { Value = reader.ReadInt32(); return this; }
}