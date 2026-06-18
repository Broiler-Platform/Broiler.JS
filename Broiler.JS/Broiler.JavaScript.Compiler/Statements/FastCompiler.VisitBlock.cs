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

        // Annex B 3.3 (sloppy mode): consume the function-scope var bindings handed
        // down from CreateFunction for this function body block. Cleared immediately
        // so nested blocks do not inherit them.
        var annexBFunctionNames = pendingAnnexBFunctionNames;
        pendingAnnexBFunctionNames = null;
        if (annexBFunctionNames != null)
        {
            var annexBEn = annexBFunctionNames.GetFastEnumerator();
            while (annexBEn.MoveNext(out var annexBName))
            {
                if (lexicalBindings.Contains(annexBName.Value))
                    continue;

                // Mark as a var (non-lexical) binding so the declaration site's
                // Annex B outer-binding assignment is not blocked by
                // IsAnnexBHoistingBlocked.
                var created = scope.CreateVariable(annexBName, null, true, initialize: true);
                created.IsLexical = false;
            }
        }

        if (hoistingScope != null)
        {
            var en = hoistingScope.GetFastEnumerator();
            while (en.MoveNext(out var v))
            {
                var isLexical = lexicalBindings.Contains(v.Value);

                // A `var arguments` in an ordinary (non-arrow) function shares the
                // function's own `arguments` binding rather than a fresh block-scope
                // var — the binding is initialized to the arguments object, and a
                // later `var arguments = x` (which resolves up to this same binding)
                // overrides it. Without this the declaration created a separate slot
                // while reads kept resolving to the materialized arguments object
                // (test262 language/statements/function/S13_A15_T2).
                if (!isLexical
                    && v.Equals("arguments")
                    && scope.RootScope.Function is { IsArrowFunction: false })
                {
                    MaterializeArgumentsBinding();
                    continue;
                }

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
