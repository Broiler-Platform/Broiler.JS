using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

public class AstStatement(FastToken start, FastNodeType type, FastToken end) : AstNode(start, type, end, isStatement: true)
{
    /// <summary>
    /// Whether the compiler's for-loop pass added this statement to a loop it rewrites: the per-iteration
    /// declaration, the test and the guarded update it moves into the body, the copy back into the carriers,
    /// or the <c>try</c> that runs that copy on a <c>continue</c>. Such a statement is not in the source, so it
    /// takes no part in the completion value, which ForBodyEvaluation takes from the body's statements alone:
    /// where a completion value is tracked, the compiler compiles it so that it neither sets that value nor
    /// yields one, and the body inside the <c>try</c> sets it as it would without the <c>try</c>. Internal, as
    /// <see cref="AstForStatement.ContainsContinue"/> is.
    /// </summary>
    internal bool IsLoopBookkeeping;
}
