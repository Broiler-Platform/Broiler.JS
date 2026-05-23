using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

public class AstTryStatement(FastToken token, FastToken previousToken, AstStatement body, AstExpression catchParam, AstStatement @catch, AstStatement @finally) :
    AstStatement(token, FastNodeType.TryStatement, previousToken)
{
    public readonly AstStatement Block = body;
    public readonly AstExpression CatchParam = catchParam;
    public readonly AstStatement Catch = @catch;
    public readonly AstStatement Finally = @finally;
}
