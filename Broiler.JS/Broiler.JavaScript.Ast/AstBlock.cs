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

    /// <summary>
    /// True when this block was synthesized by the parser to wrap a single
    /// FunctionDeclaration appearing as an `if` clause (Annex B.3.4). Such a
    /// declaration is still an early SyntaxError in strict mode, so syntax
    /// validation must be able to tell it apart from an explicit user block.
    /// </summary>
    public bool IsSyntheticFunctionStatementBlock;

    public readonly IFastEnumerable<AstStatement> Statements;

    protected AstBlock(FastToken start, FastNodeType type, FastToken end, IFastEnumerable<AstStatement> statements) : base(start, type, end) => Statements = statements;

    public AstBlock(FastToken start, FastToken end, IFastEnumerable<AstStatement> list) : base(start, FastNodeType.Block, end) => Statements = list;

    public override string ToString() => Statements.Join("\n\t");
}
