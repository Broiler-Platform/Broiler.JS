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
    protected override YExpression VisitVariableDeclaration(AstVariableDeclaration variableDeclaration)
    {
        var dispose = variableDeclaration.Using;
        var async = variableDeclaration.AwaitUsing;
        var readOnlyAfterAssign = variableDeclaration.Kind == FastVariableKind.Const;
        var list = new Sequence<YExpression>();
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
                    var v = isDirectEvalCompilation && !IsStrictMode && !newScope && !usesDirectEvalLocalVarEnvironment
                        ? GetOrCreateDirectEvalRootVariable(id.Name)
                        : top.CreateVariable(id.Name, JSVariableBuilder.New(id.Name.Value), newScope);
                    if (d.Init == null)
                    {
                        // A fresh lexical binding (let/const) is initialized to undefined. A
                        // bare, already-hoisted `var x;` initializes nothing and produces no
                        // value, so its read is NOT evaluated here: for a direct-eval var whose
                        // name is an existing global accessor that read would fire the getter
                        // (test262 staging/sm/global/bug-320887).
                        if (newScope)
                            list.Add(YExpression.Assign(v.Expression, JSUndefinedBuilder.Value));
                    }
                    else
                    {
                        // NamedEvaluation: an anonymous class adopts the binding name
                        // during ClassDefinitionEvaluation (before static initializers
                        // run), so thread the name in via the hint consumed by CreateClass.
                        if (d.Init is AstClassExpression { Identifier: null })
                            anonymousClassNameHint = id.Name.Value;
                        var initExpr = Visit(d.Init);
                        if (!IsAnonymousFunctionDefinition(d.Init))
                            initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(""), YExpression.Constant(false));
                        if (newScope)
                        {
                            // The initializer may lower to a value-producing try/finally —
                            // e.g. an array-destructuring assignment `let z = [a] = [5]`,
                            // whose iterator-close runs in a finally. Assigning such a value
                            // directly into the lexical binding's value setter (a method call)
                            // emits invalid IL, since no value can cross the finally to the
                            // call. Spill into a plain local first (which the backend assigns
                            // inside the try), then store that into the binding.
                            using var lexicalInitTemp = top.GetTempVariable(typeof(JSValue));
                            list.Add(YExpression.Block(
                                new Sequence<YParameterExpression> { lexicalInitTemp.Variable },
                                YExpression.Assign(lexicalInitTemp.Expression, initExpr),
                                YExpression.Assign(v.Expression, lexicalInitTemp.Expression)));
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
                            list.Add(YExpression.Assign(v.Expression, initExpr));
                        }
                        else
                        {
                            var key = KeyOfName(id.Name);
                            using var withObjectTemp = top.GetTempVariable(typeof(JSObject));
                            using var initTemp = top.GetTempVariable(typeof(JSValue));
                            var resolveStep = YExpression.Assign(withObjectTemp.Expression, JSContextBuilder.ResolveWithObject(key));
                            var initStep = YExpression.Assign(initTemp.Expression, initExpr);
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
                                YExpression.Block(
                                    new Sequence<YParameterExpression> { withObjectTemp.Variable, initTemp.Variable },
                                    first,
                                    second,
                                    YExpression.Condition(
                                        YExpression.NotEqual(withObjectTemp.Expression, YExpression.Constant(null, typeof(JSObject))),
                                        JSContextBuilder.AssignWithObjectIdentifier(withObjectTemp.Expression, key, initTemp.Expression, IsStrictMode),
                                        YExpression.Assign(v.Expression, initTemp.Expression),
                                        typeof(JSValue))));
                        }
                    }

                    if (readOnlyAfterAssign)
                        list.Add(JSVariableBuilder.SetReadOnly(v.Variable, true));

                    if (dispose)
                    {
                        list.Add(top.Disposable.CallExpression<IJSDisposableStack, JSValue, bool>(() => (j, v, b) => 
                        j.AddDisposableResource(v, b), v.Expression, YExpression.Constant(async)));
                    }
                    break;

                case FastNodeType.ObjectPattern:
                    var objectPattern = d.Identifier as AstObjectPattern;
                    using (var temp = top.GetTempVariable())
                    {
                        if (d.Init != null)
                            list.Add(YExpression.Assign(temp.Variable, Visit(d.Init)));

                        CreateAssignment(list, objectPattern, temp.Expression, true, newScope, suppressAnonymousFunctionNameInference: true, readOnlyAfterAssign: readOnlyAfterAssign);

                        if (dispose)
                        {
                            list.Add(top.Disposable.CallExpression<IJSDisposableStack, JSValue, bool>(() => (j, v, b) => 
                            j.AddDisposableResource(v, b), temp.Variable, YExpression.Constant(async)));
                        }
                    }
                    break;

                case FastNodeType.ArrayPattern:
                    var arrayPattern = d.Identifier as AstArrayPattern;
                    using (var temp = scope.Top.GetTempVariable())
                    {
                        if (d.Init != null)
                            list.Add(YExpression.Assign(temp.Variable, Visit(d.Init)));

                        CreateAssignment(list, arrayPattern, temp.Expression, true, newScope, suppressAnonymousFunctionNameInference: true, readOnlyAfterAssign: readOnlyAfterAssign);
                        if (dispose)
                        {
                            list.Add(top.Disposable.CallExpression<IJSDisposableStack, JSValue, bool>(() => (j, v, b) => 
                            j.AddDisposableResource(v, b), temp.Variable, YExpression.Constant(async)));
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
        var r = YExpression.Block(list);
        return r;
    }
}
