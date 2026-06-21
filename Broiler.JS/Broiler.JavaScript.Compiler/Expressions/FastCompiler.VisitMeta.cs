using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitMeta(AstMeta astMeta)
    {
        // only new.target is supported....
        if (!(astMeta.Identifier.Name.Equals("new") && astMeta.Property.Name.Equals("target")))
            throw JSEngine.NewSyntaxError($"{astMeta.Identifier.Name}.{astMeta.Property} not supported");

        // new.target is only legal inside an ordinary (non-arrow) function — a function,
        // method, constructor, or accessor — or a class element initializer. At the
        // program/script top level (including a top-level arrow, which only inherits an
        // enclosing ordinary function's binding) it is an early SyntaxError. Direct eval
        // validates its own new.target placement (DirectEvalSupport), so it is exempt.
        if (!isDirectEvalCompilation && !inMemberInitializer && !EnclosedByOrdinaryFunction(scope.Top))
            throw new FastParseException(astMeta.Start, "new.target expression is not allowed here");

        // Inside a function, new.target resolves to the lexically captured cell
        // (which an arrow function inherits from its enclosing ordinary function).
        // At the program/root level there is no cell, so read the live value — except
        // in a direct eval, whose top-level new.target shares the caller's [[NewTarget]]
        // threaded in by PerformEval (a function declared inside the eval still gets its
        // own cell and so keeps using NewTargetExpression).
        if (scope.Top.NewTargetExpression != null)
            return scope.Top.NewTargetExpression;

        return isDirectEvalCompilation ? JSContextBuilder.DirectEvalNewTarget : JSContextBuilder.NewTarget();
    }
}
