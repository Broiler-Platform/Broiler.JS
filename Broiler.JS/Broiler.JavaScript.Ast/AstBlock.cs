using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast;


public class AstBlock : AstStatement
{
    public IFastEnumerable<StringSpan>? HoistingScope;

    /// <summary>
    /// Names of block-nested function declarations that must additionally be
    /// var-hoisted to this function/program body scope per Annex B 3.3 (sloppy
    /// mode only). The compiler creates the function-scope binding at entry,
    /// initialized to undefined, so reads before the declaration resolve.
    /// </summary>
    public IFastEnumerable<StringSpan>? AnnexBFunctionNames;

    public readonly IFastEnumerable<AstStatement> Statements;

    protected AstBlock(FastToken start, FastNodeType type, FastToken end, IFastEnumerable<AstStatement> statements) : base(start, type, end) => Statements = statements;

    public AstBlock(FastToken start, FastToken end, IFastEnumerable<AstStatement> list) : base(start, FastNodeType.Block, end) => Statements = list;

    public override string ToString() => Statements.Join("\n\t");
}
