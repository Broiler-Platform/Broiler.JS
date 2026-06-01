using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitBlock(AstBlock block)
    {
        int count = block.Statements.Count;
        if (count == 0)
            return YExpression.Empty;

        var blockList = new Sequence<YExpression>(count);
        var hoistingScope = block.HoistingScope;
        var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
        var lexicalBindings = CollectTopLevelLexicalBindings(block.Statements);
        
        if (hoistingScope != null)
        {
            var en = hoistingScope.GetFastEnumerator();
            while (en.MoveNext(out var v))
            {
                var isLexical = lexicalBindings.Contains(v.Value);
                var hoistToDirectEvalRoot = isDirectEvalCompilation
                    && scope.Function == null
                    && !IsStrictMode
                    && !isLexical
                    && !IsAnnexBHoistingBlocked(v);
                var variable = hoistToDirectEvalRoot
                    ? GetOrCreateDirectEvalRootVariable(v)
                    : scope.CreateVariable(v, null, true, initialize: isLexical == false);
                variable.IsLexical = isLexical;
                if (hoistToDirectEvalRoot && directEvalBindingNames != null && Array.IndexOf(directEvalBindingNames, v.Value) >= 0)
                    variable.Expression = JSContextBuilder.Index(KeyOfName(v));
            }
        }

        var se = block.Statements.GetFastEnumerator();
        while (se.MoveNext(out var stmt))
        {
            var exp = Visit(stmt);
            if (exp == null)
                continue;

            blockList.Add(CallStackItemBuilder.Step(scope.StackItem, stmt.Start.Start.Line, stmt.Start.Start.Column));
            blockList.Add(exp);
        }

        var result = Scoped(scope, blockList);

        scope.Dispose();
        return result;
    }
}
