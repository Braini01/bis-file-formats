using System.Text;
using Antlr4.Runtime.Misc;
using BIS.Core.Streams;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;

namespace BIS.RAP.Models.Statements; 

public class RapExternalClassStatement : IRapStatement, IRapDeserializable<ParamFileParser.ExternalClassDeclarationContext> {
    public string Classname { get; set; } = string.Empty;
    
    public void WriteBinarized(BinaryWriterEx writer) {
        writer.Write((byte) 3);
        writer.WriteAsciiz(Classname);
    }

    public string ToParseTree() => new StringBuilder("class ").Append(Classname).Append(';').ToString();

    public IRapDeserializable<ParamFileParser.ExternalClassDeclarationContext> ReadBinarized(BinaryReaderEx reader) {
        if (reader.ReadByte() != 3) throw new Exception("Expected external class.");
        Classname = reader.ReadAsciiz();
        return this;
    }

    public IRapDeserializable<ParamFileParser.ExternalClassDeclarationContext> ReadParseTree(ParamFileParser.ExternalClassDeclarationContext ctx) {
        if (ctx.classname is not { } classname) throw new Exception();
        Classname = ctx.Start.InputStream.GetText(new Interval(classname.Start.StartIndex, classname.Stop.StopIndex));
        return this;
    }
}