using System.Text;
using BIS.Core.Streams;
using BIS.RAP.Factories;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;

namespace BIS.RAP.Models.Values; 

public sealed class RapArray : IRapDeserializable<ParamFileParser.LiteralArrayContext>, IRapArrayEntry {
    public List<IRapArrayEntry> Entries { get; set; } = new();
    
    public void WriteBinarized(BinaryWriterEx writer) {
        writer.WriteCompressedInt(Entries.Count);

        foreach (var entry in Entries) {
            switch (entry) {
                case RapString @string: {
                    writer.Write((byte) 0);
                    @string.WriteBinarized(writer);
                    continue;
                };
                case RapFloat @float: {
                    writer.Write((byte) 1);
                    @float.WriteBinarized(writer);
                    continue;
                };
                case RapInteger @int: {
                    writer.Write((byte) 2);
                    @int.WriteBinarized(writer);
                    continue;
                };
                case RapArray array: {
                    writer.Write((byte) 3);
                    array.WriteBinarized(writer);
                    continue;
                };
                default: {
                    throw new NotSupportedException();
                };
            }
        }
    }

    public string ToParseTree() => new StringBuilder("{").Append(string.Join(", ", Entries.Select(v => v.ToParseTree()))).Append('}').ToString();

    public IRapDeserializable<ParamFileParser.LiteralArrayContext> ReadBinarized(BinaryReaderEx reader) {
        Entries = new List<IRapArrayEntry>(reader.ReadCompressedInteger());
        for (var i = 0; i < Entries.Capacity; ++i) {
            switch (reader.ReadByte()) {
                case 0: { // String
                    Entries.Add((IRapArrayEntry) new RapString().ReadBinarized(reader));
                    break;
                };
                case 1: { // Float
                    Entries.Add((IRapArrayEntry) new RapFloat().ReadBinarized(reader));
                    break;
                };
                case 2: { // Integer
                    Entries.Add((IRapArrayEntry) new RapInteger().ReadBinarized(reader));
                    break;
                };
                case 3: { // Child Array
                    Entries.Add((IRapArrayEntry) new RapArray().ReadBinarized(reader));
                    break;
                };
                case 4: // Variable
                default: {
                    throw new Exception();
                };
            }
        }
        return this;
    }

    public IRapDeserializable<ParamFileParser.LiteralArrayContext> ReadParseTree(ParamFileParser.LiteralArrayContext ctx) {
        Entries.AddRange(ctx.literalOrArray().ToList().Select(RapLiteralFactory.Create));
        return this;
    }
}