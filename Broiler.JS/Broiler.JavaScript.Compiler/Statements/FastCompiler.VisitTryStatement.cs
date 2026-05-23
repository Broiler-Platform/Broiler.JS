using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitTryStatement(AstTryStatement tryStatement)
    {
        var block = VisitStatement(tryStatement.Block);
        var cb = tryStatement.Catch;

        if (cb != null)
        {
            var catchParam = tryStatement.CatchParam;

            if (catchParam is AstIdentifier id)
            {
                var pe = this.scope.Top.CreateException(id.Name.Value);
                using var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
                var v = scope.CreateVariable(id.Name, newScope: true);
                var catchBlock = YExpression.Block(v.Variable.AsSequence(), YExpression.Assign(v.Variable, JSVariableBuilder.NewFromException(pe.Variable, id.Name.Value)), VisitStatement(cb));
                var cbExp = YExpression.Catch(pe.Variable, catchBlock.ToJSValue());

                if (tryStatement.Finally != null)
                    return YExpression.TryCatchFinally(block.ToJSValue(), VisitStatement(tryStatement.Finally).ToJSValue(), cbExp);

                return YExpression.TryCatch(block.ToJSValue(), cbExp);
            }
            else if (catchParam is AstArrayPattern or AstObjectPattern)
            {
                // Use a synthetic identifier for the exception, then destructure inside the catch block
                var syntheticName = new StringSpan("__catchParam__");
                var pe = this.scope.Top.CreateException(syntheticName.Value);
                using var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
                var v = this.scope.Top.CreateVariable(syntheticName, newScope: true);
                var destrList = new Sequence<YExpression>();
                // Destructure the caught exception into the pattern's bindings
                CreateAssignment(destrList, catchParam, v.Expression, createVariable: true, newScope: true);
                // Collect all variables created in this scope (including destructured bindings)
                var vars = new Sequence<YParameterExpression>();
                var list = new Sequence<YExpression>();
                foreach (var vp in this.scope.Top.VariableParameters)
                    vars.Add(vp);
                // Initialize all variables (including JSVariable constructors for destructured bindings)
                foreach (var initExpr in this.scope.Top.InitList)
                    list.Add(initExpr);
                list.Add(YExpression.Assign(v.Variable, JSVariableBuilder.NewFromException(pe.Variable, syntheticName.Value)));
                foreach (var d in destrList)
                    list.Add(d);
                list.Add(VisitStatement(cb));
                var catchBlock = YExpression.Block(vars, list);
                var cbExp = YExpression.Catch(pe.Variable, catchBlock.ToJSValue());

                if (tryStatement.Finally != null)
                    return YExpression.TryCatchFinally(block.ToJSValue(), VisitStatement(tryStatement.Finally).ToJSValue(), cbExp);

                return YExpression.TryCatch(block.ToJSValue(), cbExp);
            }
            else
            {
                // Optional catch binding: catch { ... }
                var pe = this.scope.Top.CreateException("__catchParam__");
                var catchBlock = VisitStatement(cb);
                var cbExp = YExpression.Catch(pe.Variable, catchBlock.ToJSValue());

                if (tryStatement.Finally != null)
                    return YExpression.TryCatchFinally(block.ToJSValue(), VisitStatement(tryStatement.Finally).ToJSValue(), cbExp);

                return YExpression.TryCatch(block.ToJSValue(), cbExp);
            }
        }

        var @finally = tryStatement.Finally;
        if (@finally != null)
            return YExpression.TryFinally(block.ToJSValue(), VisitStatement(@finally).ToJSValue());

        return JSUndefinedBuilder.Value;
    }
}
