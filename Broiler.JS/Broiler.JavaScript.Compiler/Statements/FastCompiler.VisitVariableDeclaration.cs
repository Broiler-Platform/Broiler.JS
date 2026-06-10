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
                        list.Add(newScope ? YExpression.Assign(v.Expression, JSUndefinedBuilder.Value) : v.Expression);
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
                            list.Add(YExpression.Assign(v.Expression, initExpr));
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
                            list.Add(
                                YExpression.Block(
                                    new Sequence<YParameterExpression> { withObjectTemp.Variable, initTemp.Variable },
                                    YExpression.Assign(initTemp.Expression, initExpr),
                                    YExpression.Assign(withObjectTemp.Expression, JSContextBuilder.ResolveWithObject(key)),
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
