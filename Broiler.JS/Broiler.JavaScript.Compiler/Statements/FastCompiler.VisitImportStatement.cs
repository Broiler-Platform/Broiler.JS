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

        // An ImportedBinding is immutable (ES2024 16.2.1.5: the environment record's binding for an
        // imported name is created immutable). The engine seeds it once from the loaded namespace,
        // then marks it read-only so a later `x = …` throws in the strict module code rather than
        // silently overwriting the local — the same runtime TypeError a reassigned `const` gives.
        // (The spec makes assignment to an import an early SyntaxError; this engine cannot raise that
        // without whole-module scope analysis across deferred function bodies, so it matches its own
        // `const` treatment — a runtime read-only write — instead of leaving the write to succeed.)
        void SealImport(FastFunctionScope.VariableScope binding)
            => stmts.Add(JSVariableBuilder.SetReadOnly(binding.Variable, true));

        if (all != null)
        {
            imported = scope.Top.CreateVariable(all.Name);
            stmts.Add(BExpression.Assign(imported.Expression, tempRequire));
            SealImport(imported);
        }

        if (importStatement.Default != null)
        {
            imported = scope.Top.CreateVariable(importStatement.Default.Name);
            var prop = JSValueBuilder.Index(tempRequire, KeyOfName("default"));
            stmts.Add(BExpression.Assign(imported.Expression, prop));
            SealImport(imported);
        }

        if (importStatement.Members != null)
        {
            var ve = importStatement.Members.GetFastEnumerator();
            while (ve.MoveNext(out var item))
            {
                imported = scope.Top.CreateVariable(item.asName);
                var prop = JSValueBuilder.Index(tempRequire, KeyOfName(item.name));
                stmts.Add(BExpression.Assign(imported.Expression, prop));
                SealImport(imported);
            }
        }

        var importExp = BExpression.Block(tempRequire.AsSequence(), stmts);
        return importExp;
    }

}
