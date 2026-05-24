using System;
using System.Collections.Generic;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.Utils;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Compiler;

public partial class FastCompiler : AstMapVisitor<YExpression>
{
    private static readonly MethodInfo EnterStrictModeDisposableMethod = typeof(JSEngine)
        .InternalMethod("EnterStrictModeDisposable", typeof(bool))
        ?? throw new InvalidOperationException("JSEngine.EnterStrictModeDisposable(bool) not found");
    private static readonly MethodInfo DisposeMethod = typeof(IDisposable)
        .GetMethod(nameof(IDisposable.Dispose))
        ?? throw new InvalidOperationException("IDisposable.Dispose() not found");
    private readonly FastPool pool;

    readonly LinkedStack<FastFunctionScope> scope = new();
    private readonly Stack<FastFunctionScope> withBoundaries = new();
    private readonly string location;
    private readonly bool isDirectEvalCompilation;

    public LoopScope LoopScope => scope.Top.Loop.Top;

    private StringArray _keyStrings = new();
    private YParameterExpression scriptInfo;

    public YExpression<JSFunctionDelegate> Method { get; }

    public FastCompiler(in StringSpan code, string location = null, IList<string> argsList = null, ICodeCache codeCache = null)
    {
        pool = new FastPool();

        location = location ?? "vm.js";
        this.location = location;
        isDirectEvalCompilation = (JSEngine.Current as JSContext)?.IsCompilingDirectEval ?? false;

        // add top level...

        var parserPool = new FastPool();
        var parser = new FastParser(new FastTokenStream(parserPool, code));
        var jScript = parser.ParseProgram();
        parserPool.Dispose();
        SyntaxValidation.ValidateProgram(jScript, code.Value);
        var isStrictProgram = HasUseStrictDirective(jScript);

        using var fx = scope.Push(new FastFunctionScope(pool, null, isAsync: jScript.IsAsync));

        var lScope = fx.Context;

        if (argsList != null && jScript.HoistingScope != null)
        {
            var list = new Sequence<StringSpan>(jScript.HoistingScope.Count);
            var e = jScript.HoistingScope.GetFastEnumerator();

            while (e.MoveNext(out var a))
            {
                if (argsList.Contains(a.Value))
                    continue;

                list.Add(a);
            }

            jScript.HoistingScope = list;
        }

        scriptInfo = YExpression.Parameter(typeof(ScriptInfo));

        var args = fx.ArgumentsExpression;
        var te = ArgumentsBuilder.This(args);
        var stackItem = fx.StackItem;
        var vList = new Sequence<YParameterExpression>() { scriptInfo, lScope, stackItem };

        if (argsList != null)
        {
            int i = 0;
            foreach (var arg in argsList)
            {
                // global arguments are set here for FunctionConstructor
                fx.CreateVariable(arg, JSVariableBuilder.FromArgument(fx.ArgumentsExpression, i++, arg));
            }
        }

        var l = fx.ReturnLabel;
        var previousStrictMode = IsStrictMode;
        IsStrictMode = isStrictProgram;
        var script = Visit(jScript);
        IsStrictMode = previousStrictMode;
        if (isStrictProgram)
            script = WrapInStrictMode(script);
        var sList = new Sequence<YExpression>()
        {
            YExpression.Assign(scriptInfo, ScriptInfoBuilder.New(location,code.Value)),
            YExpression.Assign(lScope, JSContextBuilder.Current)
        };

        JSContextStackBuilder.Push(sList, lScope, stackItem, YExpression.Constant(location), StringSpanBuilder.Empty, 0, 0);
        sList.Add(ScriptInfoBuilder.Build(scriptInfo, _keyStrings));

        vList.AddRange(fx.VariableParameters);
        sList.AddRange(fx.InitList);

        // register globals..
        foreach (var v in fx.Variables)
        {
            if (v.Variable != null && v.Variable.Type == typeof(JSVariable))
            {
                if (argsList?.Contains(v.Name) ?? false)
                    continue;

                if (v.Name == "this")
                    continue;

                sList.Add(JSContextBuilder.Register(lScope, v.Variable));
            }
        }

        sList.Add(YExpression.Return(l, script.ToJSValue()));
        sList.Add(YExpression.Label(l, JSUndefinedBuilder.Value));

        script = YExpression.Block(vList, YExpression.TryFinally(YExpression.Block(sList), JSContextStackBuilder.Pop(stackItem, lScope)));

        if (jScript.IsAsync)
        {
            var g = GeneratorRewriter.Rewrite("vm", script, fx.ReturnLabel, fx.Generator, replaceArgs: fx.Arguments, replaceStackItem: fx.StackItem,
                replaceContext: fx.Context, replaceScriptInfo: scriptInfo);

            var jsf = JSAsyncFunctionBuilder.Create(JSGeneratorFunctionBuilderV2.New(g, StringSpanBuilder.New("vm"), StringSpanBuilder.New(code.Value)));
            var np = YExpression.Parameter(ArgumentsBuilder.refType, "a");

            jsf = JSFunctionBuilder.InvokeFunction(jsf, np);

            Method = YExpression.Lambda<JSFunctionDelegate>("vm", jsf, [np]);
            return;
        }

        var lambda = YExpression.Lambda<JSFunctionDelegate>("body", script, fx.Arguments);
        Method = lambda;
    }

    private static bool HasUseStrictDirective(AstStatement body)
    {
        if (body is not AstBlock block)
            return false;

        var statements = block.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            if (statement is not AstExpressionStatement { Expression: var expression })
                return false;

            if (!expression.IsStringLiteral(out var literal))
                return false;

            if (literal == "use strict")
                return true;
        }

        return false;
    }

    private static YExpression WrapInStrictMode(YExpression body)
    {
        var strictScope = YExpression.Parameter(typeof(IDisposable), "#strictScope");
        return YExpression.Block(
            new Sequence<YParameterExpression> { strictScope },
            YExpression.Assign(strictScope, YExpression.Call(null, EnterStrictModeDisposableMethod, YExpression.Constant(true))),
            YExpression.TryFinally(body, YExpression.Call(strictScope, DisposeMethod)));
    }

    private YExpression VisitExpression(AstExpression exp) => Visit(exp);

    private YExpression VisitStatement(AstStatement exp) => Visit(exp);

    protected override YExpression VisitClassStatement(AstClassExpression classStatement) => CreateClass(classStatement.Identifier, classStatement.Base, classStatement);

    protected override YExpression VisitContinueStatement(AstContinueStatement continueStatement)
    {
        string name = continueStatement.Label?.Name.Value;
        if (name != null)
        {
            var target = LoopScope.Get(name);
            return target == null ? throw JSEngine.NewSyntaxError($"No label found for {name}") : YExpression.Continue(target.Break);
        }

        return YExpression.Continue(scope.Top.Loop.Top.Continue);
    }

    protected override YExpression VisitDebuggerStatement(AstDebuggerStatement debuggerStatement) => JSDebuggerBuilder.RaiseBreak();

    protected override YExpression VisitEmptyExpression(AstEmptyExpression emptyExpression) => YExpression.Empty;

    protected override YExpression VisitExpressionStatement(AstExpressionStatement expressionStatement)
    {
        var result = Visit(expressionStatement.Expression);

        var completionVar = scope.Top.Loop.Top?.FindCompletionVariable();
        if (completionVar != null)
        {
            result = YExpression.Block(YExpression.Assign(completionVar, result), completionVar);
        }

        if (IsStrictMode
            || scope.Top == scope.Top.RootScope
            || expressionStatement.Expression is not AstFunctionExpression { IsStatement: true, Id: { } id })
        {
            return result;
        }

        var currentBinding = scope.Top.GetVariable(id.Name);
        if (isDirectEvalCompilation && scope.Top.RootScope.Function == null)
            currentBinding ??= GetOrCreateDirectEvalRootVariable(id.Name);
        if (currentBinding == null)
            return result;

        if (isDirectEvalCompilation && scope.Top.RootScope.Function == null)
            currentBinding.IsDeletable = true;

        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, result)
        };

        AppendAnnexBOuterBindingAssignments(statements, currentBinding, id.Name, temp.Variable);
        statements.Add(temp.Variable);

        return YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, statements);
    }

    private YExpression VisitRuntimeFunctionDeclaration(AstFunctionExpression functionDeclaration)
    {
        var currentBinding = scope.Top.GetVariable(functionDeclaration.Id!.Name);
        if (currentBinding == null && isDirectEvalCompilation && !IsStrictMode && scope.Top.RootScope.Function == null)
            currentBinding = GetOrCreateDirectEvalRootVariable(functionDeclaration.Id.Name);
        else if (currentBinding != null && isDirectEvalCompilation && !IsStrictMode && scope.Top.RootScope.Function == null)
            currentBinding.IsDeletable = true;
        var result = CreateFunction(functionDeclaration, hoistStatementDeclaration: false);

        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var variables = new Sequence<YParameterExpression> { temp.Variable };
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, result),
            YExpression.Assign(currentBinding.Expression, temp.Variable)
        };

        AppendAnnexBOuterBindingAssignments(statements, currentBinding, functionDeclaration.Id.Name, temp.Variable);

        statements.Add(temp.Variable);
        return YExpression.Block(variables, statements);
    }

    private void AppendAnnexBOuterBindingAssignments(Sequence<YExpression> statements, FastFunctionScope.VariableScope currentBinding, in StringSpan name, YExpression value)
    {
        if (IsStrictMode)
            return;

        // Per B.3.3.3 step ii: skip Annex B hoisting when replacing the
        // FunctionDeclaration with a VariableStatement would produce an
        // early error (e.g. name conflicts with a destructured CatchParameter
        // per B.3.5, or with an enclosing lexical binding).
        if (IsAnnexBHoistingBlocked(name))
            return;

        if (scope.Top != scope.Top.RootScope)
        {
            var outerBinding = GetAnnexBOuterBinding(name, currentBinding);
            if (outerBinding != null && outerBinding != currentBinding)
                statements.Add(YExpression.Assign(outerBinding.Expression, value));
        }

        if (scope.Top.Function == null)
            statements.Add(JSContextBuilder.AssignIdentifier(KeyOfName(name), value));
    }

    private bool IsAnnexBHoistingBlocked(in StringSpan name)
    {
        // Per B.3.3.3 step ii and B.3.5: Annex B var-hoisting is blocked when
        // a lexical binding with the same name exists in an enclosing scope
        // (e.g. a destructured CatchParameter or a let/const/class binding).
        var parent = scope.Top.Parent;
        while (parent != null && parent.Function == scope.Top.Function)
        {
            if (parent.TryGetOwnVariable(name, out var variable) && variable.IsLexical)
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    private FastFunctionScope.VariableScope GetAnnexBOuterBinding(in StringSpan name, FastFunctionScope.VariableScope currentBinding)
    {
        var parent = scope.Top.Parent;
        while (parent != null && parent.Function == scope.Top.Function)
        {
            if (parent.TryGetOwnVariable(name, out var variable) && variable != currentBinding)
                return variable;

            parent = parent.Parent;
        }

        if (scope.Top.RootScope.TryGetOwnVariable(name, out var rootVariable))
            return rootVariable;

        if (scope.Top.Function == null)
        {
            var globalVariable = scope.Top.RootScope.CreateVariable(name, null, true);
            globalVariable.Expression = JSVariableBuilder.Property(globalVariable.Variable);
            globalVariable.SetInit(JSVariableBuilder.New(JSValueBuilder.Index(scope.Top.RootScope.Context, KeyOfName(name)), name.Value));
            return globalVariable;
        }

        return scope.Top.RootScope.CreateVariable(name, null, true);
    }

    protected override YExpression VisitFunctionExpression(AstFunctionExpression functionExpression) => CreateFunction(functionExpression);

    protected override YExpression VisitSpreadElement(AstSpreadElement spreadElement) => throw new NotImplementedException();

    protected override YExpression VisitThrowStatement(AstThrowStatement throwStatement) => JSExceptionBuilder.Throw(VisitExpression(throwStatement.Argument));

    protected override YExpression VisitYieldExpression(AstYieldExpression yieldExpression)
    {
        var target = yieldExpression.Argument == null ? JSUndefinedBuilder.Value : VisitExpression(yieldExpression.Argument);
        return YExpression.Yield(target, yieldExpression.Delegate);
    }
}
