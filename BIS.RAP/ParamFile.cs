using Antlr4.Runtime;
using BIS.Core.Streams;
using BIS.RAP.Factories;
using BIS.RAP.Generated;
using BIS.RAP.Interfaces;
using BIS.RAP.Models.Declarations;
using BIS.RAP.Models.Statements;

namespace BIS.RAP; 

public class ParamFile : IRapDeserializable<ParamFileParser.ComputationalStartContext> {
    public List<IRapStatement> Statements { get; set; } = new();
    
    public void WriteBinarized(BinaryWriterEx writer) {
        Action<RapClassDeclaration> saveChildClasses = null!;
        saveChildClasses = globalClass => {
            globalClass.BinaryOffset = (uint) writer.BaseStream.Position;
            writer.BaseStream.Position = globalClass.BinaryOffsetPosition;
            writer.Write(BitConverter.GetBytes(globalClass.BinaryOffset), 0, 4);
            writer.BaseStream.Position = globalClass.BinaryOffset;
            writer.WriteAsciiz(globalClass.ParentClassname ?? string.Empty);
            writer.WriteCompressedInt(globalClass.Statements.Count);
            globalClass.Statements.Where(s => s is RapExternalClassStatement).ToList().ForEach(s => s.WriteBinarized(writer));
            globalClass.Statements.Where(s => s is RapClassDeclaration).ToList().ForEach(s => s.WriteBinarized(writer));
            globalClass.Statements.Where(s => s is RapDeleteStatement).ToList().ForEach(s => s.WriteBinarized(writer));
            globalClass.Statements.Where(s => s is RapVariableDeclaration).ToList().ForEach(s => s.WriteBinarized(writer));
            globalClass.Statements.Where(s => s is RapArrayDeclaration).ToList().ForEach(s => s.WriteBinarized(writer));
            globalClass.Statements.Where(s => s is RapAppensionStatement).ToList().ForEach(s => s.WriteBinarized(writer));
            globalClass.Statements.Where(s => s is RapClassDeclaration).ToList().ForEach(s => saveChildClasses((RapClassDeclaration)s));
        };
        
        writer.Write(new byte[] {0x00, (byte) 'r', (byte) 'a', (byte) 'P'});
        writer.Write((uint) 0);
        writer.Write((uint) 8);
        var enumOffsetPosition = writer.BaseStream.Position;
        writer.Write((uint) 999999); //Write Enum offset. will be changed later
        writer.WriteAsciiz(string.Empty);
        writer.WriteCompressedInt(Statements.Count);
        foreach (var statement in Statements) statement.WriteBinarized(writer);
        foreach (var rapStatement in Statements.Where(s => s is RapClassDeclaration)) saveChildClasses((RapClassDeclaration)rapStatement);
        var enumOffset = (uint) writer.BaseStream.Position;
        writer.BaseStream.Position = enumOffsetPosition;
        writer.Write(BitConverter.GetBytes(enumOffset), 0, 4);
        writer.BaseStream.Position = enumOffset;
        writer.Write((uint) 0);
    }

    public string ToParseTree() => string.Join("\n", Statements.Select(s => s.ToParseTree()));

    public IRapDeserializable<ParamFileParser.ComputationalStartContext> ReadBinarized(BinaryReaderEx reader) {
        var bits = reader.ReadBytes(4);
        if (!(bits[0] == '\0' && bits[1] == 'r' && bits[2] == 'a' && bits[3] == 'P')) throw new Exception("Invalid header.");
        if(reader.ReadUInt32() != 0 || reader.ReadUInt32() != 8) throw new Exception("Expected bytes 0 and 8.");
        var enumOffset = reader.ReadUInt32();
        Action<RapClassDeclaration> loadChildClasses = null!;
        loadChildClasses = (child) => {
            Action<RapClassDeclaration> addEntryToClass = (childClass) => {
                var entryType = reader.PeekChar();
                switch (entryType) {
                    case 0:
                        childClass.Statements.Add((IRapStatement)new RapClassDeclaration().ReadBinarized(reader));
                        return;
                    case 1:
                        childClass.Statements.Add((IRapStatement)new RapVariableDeclaration().ReadBinarized(reader));
                        return;
                    case 2:
                        childClass.Statements.Add((IRapStatement)new RapArrayDeclaration().ReadBinarized(reader));
                        return;
                    case 3:
                        childClass.Statements.Add((IRapStatement)new RapExternalClassStatement().ReadBinarized(reader));
                        return;
                    case 4:
                        childClass.Statements.Add((IRapStatement)new RapDeleteStatement().ReadBinarized(reader));
                        return;
                    case 5:
                        childClass.Statements.Add((IRapStatement)new RapAppensionStatement().ReadBinarized(reader));
                        return;
                    default: throw new Exception();
                }
            };
            reader.BaseStream.Position = child.BinaryOffset;
            var parent = reader.ReadAsciiz();
            child.ParentClassname = (parent == string.Empty) ? null : parent;
            var entryCount = reader.ReadCompressedInteger();
            for (var i = 0; i < entryCount; ++i) addEntryToClass(child);

            child.Statements.Where(s => s is RapClassDeclaration).ToList()
                .ForEach(c => loadChildClasses((RapClassDeclaration)c));
        };
        reader.ReadAsciiz();
        var parentEntryCount = reader.ReadCompressedInteger();

        for (var i = 0; i < parentEntryCount; ++i) {
            switch (reader.PeekChar()) {
                case 0:
                    Statements.Add((IRapStatement) new RapClassDeclaration().ReadBinarized(reader));
                    break;
                case 1:
                    Statements.Add((IRapStatement) new RapVariableDeclaration().ReadBinarized(reader));
                    break;
                case 2:
                    Statements.Add((IRapStatement) new RapArrayDeclaration().ReadBinarized(reader));
                    break;
                case 3:
                    Statements.Add((IRapStatement) new RapExternalClassStatement().ReadBinarized(reader));
                    break;
                case 4:
                    Statements.Add((IRapStatement) new RapDeleteStatement().ReadBinarized(reader));
                    break;
                case 5:
                    Statements.Add((IRapStatement) new RapAppensionStatement().ReadBinarized(reader));
                    break;
                default: throw new NotSupportedException();
            }
        }
        if(!(parentEntryCount > 0)) Console.WriteLine("No parent classes were found.");
        var funcCtx = Statements.Where(s => s is RapClassDeclaration).ToList();
        funcCtx.ForEach(c => loadChildClasses((RapClassDeclaration) c));
        if(!(funcCtx.Count > 0)) Console.WriteLine("No child classes were found.");
        //TODO: Read Enums
        return this;
    }

    public void WriteToFile(string filePath, bool binarized = true) => WriteToStream(binarized).WriteTo(File.OpenWrite(filePath));

    public MemoryStream WriteToStream(bool binarized = true) {
        var fs = new MemoryStream();
        var writer = new BinaryWriterEx(fs);
        if(binarized) WriteBinarized(writer);
        else foreach (var c in ToParseTree()) writer.Write(c);
        return fs;
    }

    public static bool TryOpenStream(Stream stream, out ParamFile? paramFile) {
        var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);
        using (var reader = new BinaryReaderEx(memStream)) {
            var bits = reader.ReadBytes(4);
            reader.BaseStream.Position -= 4;

            if (bits[0] == '\0' && bits[1] == 'r' && bits[2] == 'a' && bits[3] == 'P') {
                paramFile = (ParamFile)new ParamFile().ReadBinarized(reader);
                return true;
            }

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.Close();
        }
        return TryParseParamFile(stream, out paramFile);
    }

    public static bool TryOpenFile(string filePath, ParamFile? paramFile) => TryOpenStream(File.OpenRead(filePath), out paramFile);

    private static bool TryParseBinarizedParamFile(Stream stream, out ParamFile? paramFile) {
        try {
            using var reader = new BinaryReaderEx(stream);
            paramFile = (ParamFile) new ParamFile().ReadBinarized(reader);
        }
        catch {
            //
        }
        paramFile = null;
        return false;
    }

    private static bool TryParseParamFile(Stream stream, out ParamFile? paramFile) {
        try {
            byte[]? streamData = null;
            var memStream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var lexer = new ParamFileLexer(CharStreams.fromStream(memStream));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ParamFileParser(tokens);
            
            var computationalStart = parser.computationalStart();
            
            if (parser.NumberOfSyntaxErrors != 0) {
                paramFile = null;
                return false;
            }

            paramFile = (ParamFile) new ParamFile().ReadParseTree(computationalStart);
            return true;
        } catch (Exception e) {
            paramFile = null;
            return false;
        }
    }

    public IRapDeserializable<ParamFileParser.ComputationalStartContext> ReadParseTree(ParamFileParser.ComputationalStartContext ctx) {
        if (ctx.statement() is { } statements) Statements.AddRange(statements.Select(RapStatementFactory.Create));
        return this;
    }
}