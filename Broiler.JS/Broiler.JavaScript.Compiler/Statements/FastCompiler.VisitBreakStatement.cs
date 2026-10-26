using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;


namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitBreakStatement(AstBreakStatement breakStatement)
    {
        string name = breakStatement.Label?.Name.Value;
        
        if (name != null)
        {
            var target = LoopScope.Get(name);
            return target == null ? throw JSEngine.NewSyntaxError($"No label found for {name}") : BExpression.Break(target.Break);
        }

        // An unlabeled `break` leaves the innermost loop or switch, never an enclosing
        // labelled block: `while (…) L: { break; }` exits the while.
        var ls = LoopScope.GetUnlabeledBreak();
        if (ls.IsSwitch)
            return BExpression.Goto(ls.Break);

        return BExpression.Break(ls.Break);
    }
}
