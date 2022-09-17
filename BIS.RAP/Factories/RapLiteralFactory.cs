using BIS.RAP.Generated;
using BIS.RAP.Interfaces;
using BIS.RAP.Models.Values;

namespace BIS.RAP.Factories; 

public static class RapLiteralFactory {
    public static IRapArrayEntry Create(ParamFileParser.LiteralOrArrayContext ctx) {
        if (ctx.literalArray() is { } array) return (IRapArrayEntry) new RapArray().ReadParseTree(array);
        if (ctx.literal() is { } literal) return (IRapArrayEntry) Create(literal);
        throw new Exception();
    }
    
    public static IRapLiteral Create(ParamFileParser.LiteralContext ctx) {
        if (ctx.literalString() is { } @string) return (IRapLiteral) new RapString().ReadParseTree(@string);
        if (ctx.literalFloat() is { } @float) return (IRapLiteral) new RapFloat().ReadParseTree(@float);
        if (ctx.literalInteger() is { } @int) return (IRapLiteral) new RapInteger().ReadParseTree(@int);
        throw new Exception();
    }
}