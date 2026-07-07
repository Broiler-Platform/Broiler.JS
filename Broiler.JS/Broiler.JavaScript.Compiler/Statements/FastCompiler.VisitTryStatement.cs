using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitTryStatement(AstTryStatement tryStatement)
    {
        // Inside a generator the body is rewritten into a state machine; the script/eval completion
        // value is irrelevant there, so use the plain form (no completion tracking) to avoid
        // disturbing that rewrite — mirroring VisitIfStatement.
        if (scope.Top.Function?.Generator == true)
            return BuildTryCore(tryStatement, null);

        // A TryStatement's completion value is the value of its Block — or its Catch when the Block
        // throws — with the Finally block's value always discarded, and an empty completion replaced
        // by undefined (spec TryStatement evaluation + UpdateEmpty). Track it the same way as if /
        // with / loops: the Block's and Catch's inner statements assign to `#cv` (it is on top of the
        // completion stack while they are visited), `#cv` defaults to undefined, and is propagated to
        // the enclosing completion variables. The Finally block tracks into `#cv` too, but only an
        // ABRUPT finally (break/continue/return) keeps that value — a normal finally restores the
        // Block/Catch value (BuildTryCore → VisitFinallyBlock).
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();

        BExpression coreTry;
        using (completionScopes.Push(completionVar))
            coreTry = BuildTryCore(tryStatement, completionVar);

        return BExpression.Block(
            new Sequence<BParameterExpression> { completionVar },
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            BExpression.TailCallTransparentTryFinally(coreTry, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
    }

    // Builds the underlying try/catch/finally expression. The Block and Catch are visited under the
    // currently-active completion scope (so their values flow into the completion variable for a
    // script/eval), while the Finally block is visited with completion tracking suppressed.
    private BExpression BuildTryCore(AstTryStatement tryStatement, BParameterExpression completionVar)
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
                v.IsSimpleCatchBinding = true;
                var catchBlock = BExpression.Block(v.Variable.AsSequence(), ResetCompletion(completionVar), BExpression.Assign(v.Variable, JSVariableBuilder.NewFromException(pe.Variable, id.Name.Value)), VisitStatement(cb));
                var cbExp = BExpression.Catch(pe.Variable, catchBlock.ToJSValue());

                if (tryStatement.Finally != null)
                    return BExpression.TryCatchFinally(block.ToJSValue(), VisitFinallyBlock(tryStatement.Finally, completionVar), cbExp);

                return BExpression.TryCatch(block.ToJSValue(), cbExp);
            }
            else if (catchParam is AstArrayPattern or AstObjectPattern)
            {
                // Use a synthetic identifier for the exception, then destructure inside the catch block
                var syntheticName = new StringSpan("__catchParam__");
                var pe = this.scope.Top.CreateException(syntheticName.Value);
                using var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
                var v = this.scope.Top.CreateVariable(syntheticName, newScope: true);
                var destrList = new Sequence<BExpression>();
                // Destructure the caught exception into the pattern's bindings. Pass
                // suppressAnonymousFunctionNameInference so a per-element default that is
                // not a plain anonymous function definition (e.g. `[x = (0, function(){})]`)
                // does NOT adopt the binding name — matching the let/var declaration path
                // (a covered/comma initializer is not an AnonymousFunctionDefinition).
                CreateAssignment(destrList, catchParam, v.Expression, createVariable: true, newScope: true,
                    suppressAnonymousFunctionNameInference: true);
                // Collect all variables created in this scope (including destructured bindings)
                var vars = new Sequence<BParameterExpression>();
                var list = new Sequence<BExpression>();
                foreach (var vp in this.scope.Top.VariableParameters)
                    vars.Add(vp);
                list.Add(ResetCompletion(completionVar));
                // Initialize all variables (including JSVariable constructors for destructured bindings)
                foreach (var initExpr in this.scope.Top.InitList)
                    list.Add(initExpr);
                list.Add(BExpression.Assign(v.Variable, JSVariableBuilder.NewFromException(pe.Variable, syntheticName.Value)));
                foreach (var d in destrList)
                    list.Add(d);
                list.Add(VisitStatement(cb));
                var catchBlock = BExpression.Block(vars, list);
                var cbExp = BExpression.Catch(pe.Variable, catchBlock.ToJSValue());

                if (tryStatement.Finally != null)
                    return BExpression.TryCatchFinally(block.ToJSValue(), VisitFinallyBlock(tryStatement.Finally, completionVar), cbExp);

                return BExpression.TryCatch(block.ToJSValue(), cbExp);
            }
            else
            {
                // Optional catch binding: catch { ... }
                var pe = scope.Top.CreateException("__catchParam__");
                var catchBlock = BExpression.Block(ResetCompletion(completionVar), VisitStatement(cb));
                var cbExp = BExpression.Catch(pe.Variable, catchBlock.ToJSValue());

                if (tryStatement.Finally != null)
                    return BExpression.TryCatchFinally(block.ToJSValue(), VisitFinallyBlock(tryStatement.Finally, completionVar), cbExp);

                return BExpression.TryCatch(block.ToJSValue(), cbExp);
            }
        }

        var @finally = tryStatement.Finally;
        if (@finally != null)
            return BExpression.TryFinally(block.ToJSValue(), VisitFinallyBlock(@finally, completionVar));

        return JSUndefinedBuilder.Value;
    }

    // When Block throws and Catch handles it (ECMA-262 §14.15.5), the result is the Catch's
    // completion value; the partial completion value the Block had produced before throwing
    // (`try { 'try'; throw 'e'; } catch (e) {}` — `'try'` lived in `#cv` when the throw
    // unwound the stack) must be discarded, otherwise `eval` returns the try's value instead
    // of letting UpdateEmpty(undefined) kick in for an empty catch.
    private static BExpression ResetCompletion(BParameterExpression completionVar)
        => completionVar == null ? BExpression.Empty : BExpression.Assign(completionVar, JSUndefinedBuilder.Value);

    // A Finally block's completion value only survives when the finally completes ABRUPTLY
    // (break/continue/return): per UpdateEmpty the abrupt finally's own value (or undefined
    // when it ran no value-producing statement) replaces the Block/Catch value; a normal
    // finally discards it. Implement this with save/restore on the try statement's `#cv`:
    //   save = #cv; #cv = undefined; <finally tracks #cv>; #cv = save;
    // an abrupt finally jumps out before the trailing restore, leaving its own value in #cv,
    // while a normal finally reaches the restore and brings back the Block/Catch value. The
    // finally is pushed above a null boundary so it tracks ONLY `#cv`, never the enclosing
    // completion variables (those are reconciled afterwards by PropagateCompletion).
    private BExpression VisitFinallyBlock(AstStatement finallyStatement, BParameterExpression completionVar)
    {
        if (completionVar == null)
        {
            // Generator state-machine rewrite: completion values are irrelevant, so keep the
            // finally a completion no-op (a null boundary suppresses all TrackCompletion).
            using (completionScopes.Push(null))
                return VisitStatement(finallyStatement).ToJSValue();
        }

        using var saved = scope.Top.GetTempVariable(typeof(JSValue));
        using (completionScopes.Push(null))
        using (completionScopes.Push(completionVar))
        {
            var finallyBody = VisitStatement(finallyStatement).ToJSValue();
            return BExpression.Block(
                new Sequence<BParameterExpression> { saved.Variable },
                BExpression.Assign(saved.Variable, completionVar),
                BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
                finallyBody,
                BExpression.Assign(completionVar, saved.Variable)).ToJSValue();
        }
    }
}
