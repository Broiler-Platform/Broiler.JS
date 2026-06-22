using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitImportStatement(AstImportStatement importStatement)
    {
        var tempRequire = BExpression.Parameter(typeof(JSValue));
        var require = scope.Top.GetVariable("import");
        var source = VisitExpression(importStatement.Source);
        var args = ArgumentsBuilder.New(JSUndefinedBuilder.Value, source);
        var stmts = new Sequence<BExpression>
        {
            BExpression.Assign(tempRequire, BExpression.Yield(JSFunctionBuilder.InvokeFunction(require.Expression, args)))
        };

        FastFunctionScope.VariableScope imported;
        var all = importStatement.All;

        if (all != null)
        {
            imported = scope.Top.CreateVariable(all.Name);
            stmts.Add(BExpression.Assign(imported.Expression, tempRequire));
        }

        if (importStatement.Default != null)
        {
            imported = scope.Top.CreateVariable(importStatement.Default.Name);
            var prop = JSValueBuilder.Index(tempRequire, KeyOfName("default"));
            stmts.Add(BExpression.Assign(imported.Expression, prop));
        }

        if (importStatement.Members != null)
        {
            var ve = importStatement.Members.GetFastEnumerator();
            while (ve.MoveNext(out var item))
            {
                imported = scope.Top.CreateVariable(item.asName);
                var prop = JSValueBuilder.Index(tempRequire, KeyOfName(item.name));
                stmts.Add(BExpression.Assign(imported.Expression, prop));
            }
        }

        var importExp = BExpression.Block(tempRequire.AsSequence(), stmts);
        return importExp;
    }

}
