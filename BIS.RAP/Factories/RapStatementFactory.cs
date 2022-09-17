using BIS.RAP.Generated;
using BIS.RAP.Interfaces;
using BIS.RAP.Models.Declarations;
using BIS.RAP.Models.Statements;

namespace BIS.RAP.Factories; 

public static class RapStatementFactory {
    public static IRapStatement Create(ParamFileParser.StatementContext ctx) {
        if (ctx.classDeclaration() is { } @class) return (IRapStatement) new RapClassDeclaration().ReadParseTree(@class);
        if (ctx.externalClassDeclaration() is { } external) return (IRapStatement) new RapExternalClassStatement().ReadParseTree(external);
        if (ctx.tokenDeclaration() is { } var) return (IRapStatement) new RapVariableDeclaration().ReadParseTree(var);
        if (ctx.arrayAppension() is { } appension) return (IRapStatement) new RapAppensionStatement().ReadParseTree(appension);
        if (ctx.arrayDeclaration() is { } array) return (IRapStatement) new RapArrayDeclaration().ReadParseTree(array);
        if (ctx.deleteStatement() is { } delete) return (IRapStatement) new RapDeleteStatement().ReadParseTree(delete);
        throw new NotSupportedException();
    }
}