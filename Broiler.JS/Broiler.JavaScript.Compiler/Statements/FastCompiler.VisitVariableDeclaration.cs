using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitVariableDeclaration(AstVariableDeclaration variableDeclaration)
    {
        var dispose = variableDeclaration.Using;
        var async = variableDeclaration.AwaitUsing;
        var readOnlyAfterAssign = variableDeclaration.Kind == FastVariableKind.Const;
        var list = new Sequence<BExpression>();
        var top = scope.Top;
        // Record that this scope's disposal must be awaited (an `await using` resource).
        // A sync-only `using` scope disposes synchronously and must not be Yield-wrapped.
        if (dispose && async)
            top.HasAsyncDisposable = true;
        var newScope = variableDeclaration.Kind == FastVariableKind.Const || variableDeclaration.Kind == FastVariableKind.Let;
        var ed = variableDeclaration.Declarators.GetFastEnumerator();
        while (ed.MoveNext(out var d))
        {
            switch (d.Identifier.Type)
            {
                case FastNodeType.Identifier:
                    var id = d.Identifier as AstIdentifier;
                    // Only the eval program's OWN top-level `var` belongs to the eval
                    // var environment. A `var` inside a function the eval code declares
                    // is an ordinary function local, and routing it to the eval root
                    // binding broke it twice over: the hoisted local (VisitBlock) is what
                    // reads resolve to, so the initializer store went to a *different*
                    // binding — leaving `eval("(function(){ var x = 42; return x; })()")`
                    // 0 (numeric local) or undefined — and that binding is indexed off the
                    // global object, so the value leaked out as a global `x`. Every other
                    // direct-eval root-hoisting site guards on the same `Function == null`
                    // (VisitBlock, VisitSwitchStatement, VisitProgram); this one did not.
                    var v = isDirectEvalCompilation && top.Function == null && !IsStrictMode && !newScope && !usesDirectEvalLocalVarEnvironment
                        ? GetOrCreateDirectEvalRootVariable(id.Name)
                        : top.CreateVariable(id.Name, JSVariableBuilder.New(id.Name.Value), newScope);
                    if (d.Init == null)
                    {
                        // A fresh lexical binding (let/const) is initialized to undefined. A
                        // bare, already-hoisted `var x;` initializes nothing and produces no
                        // value, so its read is NOT evaluated here: for a direct-eval var whose
                        // name is an existing global accessor that read would fire the getter
                        // (test262 staging/sm/global/bug-320887).
                        // A numeric local never reaches here: the analysis rejects a declarator
                        // with no initializer, since the binding would be observably undefined.
                        if (newScope && v.NumericStorage == null)
                            list.Add(AssignToVariable(v, JSUndefinedBuilder.Value));
                    }
                    else
                    {
                        // NamedEvaluation: an anonymous class adopts the binding name
                        // during ClassDefinitionEvaluation (before static initializers
                        // run), so thread the name in via the hint consumed by CreateClass.
                        if (d.Init is AstClassExpression { Identifier: null })
                            anonymousClassNameHint = id.Name.Value;
                        var initExpr = VisitConsumedBy(d.Init, NumberBoxingConversionSite.GuardedTreeRootIntoLocal);
                        if (!IsAnonymousFunctionDefinition(d.Init))
                            initExpr = BExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, BExpression.Constant(""), BExpression.Constant(false));
                        if (v.NumericStorage != null)
                        {
                            // Tested BEFORE the lexical branch below, not after. A numeric
                            // local's Expression is a BOXING READ of its raw double, so
                            // assigning through it is an assignment to a method call — which
                            // the IL backend rejects with a NotImplementedException that kills
                            // compilation of the whole script. A numeric local also exists only
                            // in a function with no `with` and no direct eval, so neither the
                            // lexical spill nor the with-object resolution below can apply to
                            // it (docs/performance-roadmap.md item 3-3).
                            list.Add(AssignToVariable(v, initExpr));
                        }
                        else if (newScope)
                        {
                            // The initializer may lower to a value-producing try/finally —
                            // e.g. an array-destructuring assignment `let z = [a] = [5]`,
                            // whose iterator-close runs in a finally. Assigning such a value
                            // directly into the lexical binding's value setter (a method call)
                            // emits invalid IL, since no value can cross the finally to the
                            // call. Spill into a plain local first (which the backend assigns
                            // inside the try), then store that into the binding.
                            using var lexicalInitTemp = top.GetTempVariable(typeof(JSValue));
                            list.Add(BExpression.Block(
                                new Sequence<BParameterExpression> { lexicalInitTemp.Variable },
                                BExpression.Assign(lexicalInitTemp.Expression, initExpr),
                                AssignToVariable(v, lexicalInitTemp.Expression)));
                        }
                        else if (withBoundaries.Count > 0
                            && TryGetStaticIdentifierVariable(id, out var staticVar) && staticVar != null)
                        {
                            // The name statically resolves to a local declared inside the
                            // active `with` boundary, so the local shadows any same-named
                            // with-object property. Reads resolve to this local
                            // (TryGetStaticIdentifierVariable), so the initializer must too —
                            // otherwise a `var x = init` whose name collides with a
                            // with-object property would store into the object and leave the
                            // local undefined.
                            list.Add(AssignToVariable(v, initExpr));
                        }
                        else
                        {
                            var key = KeyOfName(id.Name);
                            using var withObjectTemp = top.GetTempVariable(typeof(JSObject));
                            using var initTemp = top.GetTempVariable(typeof(JSValue));
                            var resolveStep = BExpression.Assign(withObjectTemp.Expression, JSContextBuilder.ResolveWithObject(key));
                            var initStep = BExpression.Assign(initTemp.Expression, initExpr);
                            // Inside a `with`, ResolveBinding (which with-object, if any, holds
                            // the name) happens BEFORE the Initializer runs, per VariableDeclaration
                            // semantics: the reference is resolved, then the Initializer runs, then
                            // PutValue stores into that already-resolved reference. So
                            // `with (o) { var x = delete o.x; }` resolves the target to o.x first,
                            // and the later assignment re-creates o.x even though the initializer
                            // deleted it. Outside any `with` (the common global/function-scope var),
                            // keep the original init-then-resolve order — ResolveWithObject only
                            // matters at global scope there, and reordering it is a needless change.
                            var first = withBoundaries.Count > 0 ? resolveStep : initStep;
                            var second = withBoundaries.Count > 0 ? initStep : resolveStep;
                            list.Add(
                                BExpression.Block(
                                    new Sequence<BParameterExpression> { withObjectTemp.Variable, initTemp.Variable },
                                    first,
                                    second,
                                    BExpression.Condition(
                                        BExpression.NotEqual(withObjectTemp.Expression, BExpression.Constant(null, typeof(JSObject))),
                                        JSContextBuilder.AssignWithObjectIdentifier(withObjectTemp.Expression, key, initTemp.Expression, IsStrictMode),
                                        AssignToVariable(v, initTemp.Expression),
                                        typeof(JSValue))));
                        }
                    }

                    // Read-only is a property of the JSVariable cell, and a numeric local has
                    // none. It needs none either: NumericLocalAnalysis rejects a const that is
                    // written anywhere, so a name that reached a raw double has no assignment
                    // whose TypeError could go missing.
                    if (readOnlyAfterAssign && v.NumericStorage == null)
                        list.Add(JSVariableBuilder.SetReadOnly(v.Variable, true));

                    if (dispose)
                    {
                        list.Add(top.Disposable.CallExpression<IJSDisposableStack, JSValue, bool>(() => (j, v, b) => 
                        j.AddDisposableResource(v, b), v.Expression, BExpression.Constant(async)));
                    }
                    break;

                case FastNodeType.ObjectPattern:
                    var objectPattern = d.Identifier as AstObjectPattern;
                    using (var temp = top.GetTempVariable())
                    {
                        if (d.Init != null)
                            list.Add(BExpression.Assign(temp.Variable, Visit(d.Init)));

                        CreateAssignment(list, objectPattern, temp.Expression, true, newScope, suppressAnonymousFunctionNameInference: true, readOnlyAfterAssign: readOnlyAfterAssign);

                        if (dispose)
                        {
                            list.Add(top.Disposable.CallExpression<IJSDisposableStack, JSValue, bool>(() => (j, v, b) => 
                            j.AddDisposableResource(v, b), temp.Variable, BExpression.Constant(async)));
                        }
                    }
                    break;

                case FastNodeType.ArrayPattern:
                    var arrayPattern = d.Identifier as AstArrayPattern;
                    using (var temp = scope.Top.GetTempVariable())
                    {
                        if (d.Init != null)
                            list.Add(BExpression.Assign(temp.Variable, Visit(d.Init)));

                        CreateAssignment(list, arrayPattern, temp.Expression, true, newScope, suppressAnonymousFunctionNameInference: true, readOnlyAfterAssign: readOnlyAfterAssign);
                        if (dispose)
                        {
                            list.Add(top.Disposable.CallExpression<IJSDisposableStack, JSValue, bool>(() => (j, v, b) => 
                            j.AddDisposableResource(v, b), temp.Variable, BExpression.Constant(async)));
                        }
                    }
                    break;

                default:
                    throw new FastParseException(d.Identifier.Start, $"Invalid pattern {d.Identifier.Type}");
            }
        }

        if (list.Count == 1)
        {
            var e = list[0];
            return e;
        }
        var r = BExpression.Block(list);
        return r;
    }
}
