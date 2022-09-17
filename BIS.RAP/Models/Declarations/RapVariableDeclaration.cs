using System.Text;
using BIS.Core.Streams;
using BIS.RAP.Factories;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;
using BIS.RAP.Models.Values;

namespace BIS.RAP.Models.Declarations; 

public class RapVariableDeclaration : IRapStatement, IRapDeserializable<ParamFileParser.TokenDeclarationContext> {
    public string VariableName { get; set; } = string.Empty;
    public IRapLiteral VariableValue { get; set; } = new RapString();

    public void WriteBinarized(BinaryWriterEx writer) {
        writer.Write((byte) 1);
        switch (VariableValue) {
            case RapString @string:
                writer.Write((byte) 0);
                writer.WriteAsciiz(VariableName);
                @string.WriteBinarized(writer);
                break;
            case RapFloat @float:
                writer.Write((byte) 1);
                writer.WriteAsciiz(VariableName);
                @float.WriteBinarized(writer);
                break;
            case RapInteger @int:
                writer.Write((byte) 2);
                writer.WriteAsciiz(VariableName);
                @int.WriteBinarized(writer);
                break;
            default: throw new NotSupportedException();
        }
    }

    public string ToParseTree() => new StringBuilder(VariableName).Append(" = ").Append(VariableValue.ToParseTree()).Append(';').ToString();

    public IRapDeserializable<ParamFileParser.TokenDeclarationContext> ReadBinarized(BinaryReaderEx reader) {
        if (reader.ReadByte() != 1) throw new Exception("Expected token.");
        var valType = reader.ReadByte();
        VariableName = reader.ReadAsciiz();
        switch (valType) {
            case 0:
                VariableValue = (IRapLiteral) new RapString().ReadBinarized(reader);
                return this;
            case 1:
                VariableValue = (IRapLiteral) new RapFloat().ReadBinarized(reader);
                return this;
            case 2:
                VariableValue = (IRapLiteral) new RapInteger().ReadBinarized(reader);
                return this;
            default: throw new Exception();
        }
    }

    public IRapDeserializable<ParamFileParser.TokenDeclarationContext> ReadParseTree(ParamFileParser.TokenDeclarationContext ctx) {
        if (ctx.identifier() is not { } identifier) throw new Exception();
        if (ctx.value is not { } value) throw new Exception();
        VariableName = ctx.identifier().GetText();
        VariableValue = RapLiteralFactory.Create(value);
        return this;
    }
}