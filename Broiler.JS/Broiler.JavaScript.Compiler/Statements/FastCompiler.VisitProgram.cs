using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private YExpression Scoped(FastFunctionScope scope, IFastEnumerable<YExpression> body)
    {
        var list = new Sequence<YExpression>();
        list.AddRange(scope.InitList);
        list.AddRange(body);

        if (scope.VariableParameters.Any() && !list.Any())
            throw new InvalidOperationException();

        if (!list.Any())
            return YExpression.Empty;

        var r = YExpression.Block(scope.VariableParameters.AsSequence(), list);

        if (scope.HasDisposable)
        {
            list =
            [
                // create new disposable via factory delegate ...
                YExpression.Assign(scope.Disposable,
                    NewLambdaExpression.StaticCallExpression<IJSDisposableStack>(() => () => IJSDisposableStack.New()))
            ];

            var d = scope.Disposable;
            var dispose = d.CallExpression<IJSDisposableStack, JSValue>(() => (j) => j.Dispose());
            if (scope.Function.Async)
            {
                // we will move everything inside await dispose...
                list.Add(YExpression.TryFinally(r, YExpression.Yield(dispose)));
            }
            else
            {
                list.Add(YExpression.TryFinally(r, dispose));
            }

            return YExpression.Block(new Sequence<YParameterExpression> { scope.Disposable }, list);
        }

        return r;
    }


    protected override YExpression VisitProgram(AstProgram program)
    {
        var blockList = new Sequence<YExpression>(program.Statements.Count);
        ref var hoistingScope = ref program.HoistingScope;
        var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
        var lexicalBindings = CollectTopLevelLexicalBindings(program.Statements);

        if (hoistingScope != null)
        {
            var en = hoistingScope.GetFastEnumerator();
            var top = this.scope.Top;
            var isDirectEvalProgramScope = isDirectEvalCompilation && top.Function == null;
        
            while (en.MoveNext(out var v))
            {
                if (lexicalBindings.Contains(v.Value))
                {
                    scope.CreateVariable(v, null, true, initialize: false);
                    continue;
                }

                if (isDirectEvalProgramScope && (IsStrictMode || usesDirectEvalLocalVarEnvironment))
                {
                    // Strict eval, and non-strict eval inside a function var
                    // environment, keep vars local to the eval scope.
                    var localVariable = scope.CreateVariable(v, null, true);
                    localVariable.IsLexical = false;
                    localVariable.IsDeletable = !IsStrictMode && isDirectEvalProgramScope;
                    if (usesDirectEvalLocalVarEnvironment && !IsStrictMode)
                    {
                        var currentValue = JSContextBuilder.Index(KeyOfName(v));
                        localVariable.SetInit(JSVariableBuilder.New(currentValue, v.Value));
                        localVariable.SetPostInit(JSContextBuilder.RegisterDirectEvalVariable(localVariable.Variable));
                    }
                    continue;
                }

                var isDirectEvalLexicalBinding = directEvalLexicalBindingNames?.Contains(v.Value) ?? false;
                var g = isDirectEvalProgramScope
                    ? JSContextBuilder.Index(KeyOfName(v))
                    : JSValueBuilder.Index(top.Context, KeyOfName(v));
                var vs = scope.CreateVariable(v, null, true);
                vs.IsLexical = false;
                vs.IsDeletable = isDirectEvalProgramScope;
                if (isDirectEvalProgramScope && isDirectEvalLexicalBinding)
                    vs.SkipRegistration = true;
                scope.Parent?.AddExternalVariable(v, vs);

                if (isDirectEvalProgramScope)
                {
                    if (!isDirectEvalLexicalBinding)
                        vs.Expression = JSContextBuilder.Index(KeyOfName(v));
                }
                else
                    vs.Expression = JSVariableBuilder.Property(vs.Variable);
                vs.SetInit(JSVariableBuilder.New(g, v.Value));
            }
        }

        var se = program.Statements.GetFastEnumerator();
        while (se.MoveNext(out var stmt))
        {
            var exp = Visit(stmt);
            if (exp == null)
                continue;

            blockList.Add(CallStackItemBuilder.Step(scope.StackItem, stmt.Start.Start.Line, stmt.Start.Start.Column));
            blockList.Add(exp);
        }

        var r = Scoped(scope, blockList);

        scope.Dispose();
        return r;
    }

    private FastFunctionScope.VariableScope GetOrCreateDirectEvalRootVariable(in StringSpan name)
    {
        var top = scope.Top;
        while (top.Parent != null && top.Parent.Function == top.Function)
            top = top.Parent;

        var isLexicalDirectEvalBinding = directEvalLexicalBindingNames?.Contains(name.Value) == true;
        var existing = isLexicalDirectEvalBinding
            ? top.TryGetOwnVariable(name, out var ownVariable) ? ownVariable : null
            : top.GetVariable(name);
        if (existing != null)
        {
            existing.IsDeletable = true;
            return existing;
        }

        var globalValue = JSContextBuilder.Index(KeyOfName(name));
        var variable = top.CreateVariable(name, null, true);
        variable.IsLexical = false;
        variable.IsDeletable = true;
        if (isLexicalDirectEvalBinding)
            variable.SkipRegistration = true;
        top.Parent?.AddExternalVariable(name, variable);
        variable.Expression = isLexicalDirectEvalBinding
            ? JSVariable.ValueExpression(variable.Variable)
            : directEvalBindingNames?.Contains(name.Value) == true
                ? JSContextBuilder.Index(KeyOfName(name))
                : JSValueBuilder.Index(top.RootScope.Context, KeyOfName(name));
        variable.SetInit(JSVariableBuilder.New(globalValue, name.Value));
        return variable;
    }

    private static HashSet<string> CollectTopLevelLexicalBindings(IFastEnumerable<AstStatement> statements)
    {
        var lexicalBindings = new HashSet<string>(StringComparer.Ordinal);
        var enumerator = statements.GetFastEnumerator();

        while (enumerator.MoveNext(out var statement))
        {
            switch (statement)
            {
                case AstVariableDeclaration { Kind: FastVariableKind.Let or FastVariableKind.Const } declaration:
                    var declarators = declaration.Declarators.GetFastEnumerator();
                    while (declarators.MoveNext(out var declarator))
                        CollectBindingNames(declarator.Identifier, lexicalBindings);
                    break;

                case AstExpressionStatement { Expression: AstClassExpression { Identifier: { } identifier } }:
                    lexicalBindings.Add(identifier.Name.Value);
                    break;
            }
        }

        return lexicalBindings;
    }

    private static void CollectBindingNames(AstExpression expression, HashSet<string> names)
    {
        switch (expression)
        {
            case AstIdentifier identifier:
                names.Add(identifier.Name.Value);
                break;

            case AstBinaryExpression assignment:
                CollectBindingNames(assignment.Left, names);
                break;

            case AstSpreadElement spread:
                CollectBindingNames(spread.Argument, names);
                break;

            case AstArrayPattern array:
                var elements = array.Elements.GetFastEnumerator();
                while (elements.MoveNext(out var element))
                {
                    if (element != null)
                        CollectBindingNames(element, names);
                }
                 break;

            case AstObjectPattern @object:
                var properties = @object.Properties.GetFastEnumerator();
                while (properties.MoveNext(out var property))
                    CollectBindingNames(property.Value, names);
                break;
        }
    }
}
