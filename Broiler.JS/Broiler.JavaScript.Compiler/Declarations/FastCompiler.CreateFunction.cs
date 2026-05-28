using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using System.Collections.Generic;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private int parameterInitializerDepth;

    private YExpression CreateFunction(AstFunctionExpression functionDeclaration, YExpression super = null, bool createClass = false, string className = null,
        IFastEnumerable<AstClassProperty> memberInits = null, bool forceStrictMode = false, bool hoistStatementDeclaration = true, string inferredFunctionName = null,
        bool createPrototype = true, string[] directEvalPrivateNames = null)
    {
        var node = functionDeclaration;
        var functionLength = GetExpectedArgumentCount(functionDeclaration.Params);

        // get text...

        var previousScope = scope.Top;

        // if this is an arrowFunction then override previous thisExperssion

        var previousThis = scope.Top.ThisExpression;
        if (!functionDeclaration.IsArrowFunction)
            previousThis = null;

        var functionName = functionDeclaration.Id?.Name.Value;

        // var parentScriptInfo = this.scope.Top.ScriptInfo;

        var nodeCode = node.Code;

        var code = StringSpanBuilder.New(ScriptInfoBuilder.Code(scriptInfo), nodeCode.Offset, nodeCode.Length);
        var sList = new Sequence<YExpression>();
        var bodyInits = new Sequence<YExpression>();
        var vList = new Sequence<YParameterExpression>();

        var current = scope.Top.RootScope;
        var cs = scope.Push(new FastFunctionScope(
            pool,
            functionDeclaration,
            previousThis,
            super,
            memberInits: memberInits,
            previous: functionDeclaration.IsArrowFunction ? current : null,
            directEvalPrivateNames: directEvalPrivateNames ?? previousScope.DirectEvalPrivateNames));
        {
            cs.InParameterInitializer = previousScope.InParameterInitializer;
            var lexicalScopeVar = cs.Context;

            vList.Add(cs.Context);
            vList.Add(cs.StackItem);
            sList.Add(YExpression.Assign(cs.Context, JSContextBuilder.Current));

            FastFunctionScope.VariableScope jsFVarScope = null;

            // BROILER-PATCH: For function declarations, look up name in parent scope
            // to bind the function. For function expressions, the name is local to
            // the function body and must not leak to the parent scope (ES3 §13).
            YParameterExpression fexprNameParam = null;
            if (functionName != null && functionDeclaration.IsStatement && hoistStatementDeclaration)
            {
                jsFVarScope = previousScope.GetVariable(functionName);
                if (isDirectEvalCompilation && !usesDirectEvalLocalVarEnvironment && jsFVarScope != null)
                    jsFVarScope.IsDeletable = true;
            }
            else if (functionName != null && (!functionDeclaration.IsStatement || !hoistStatementDeclaration))
            {
                // BROILER-PATCH: For function expressions, create a closure variable
                // in the parent scope that the function body captures. This variable
                // holds the function reference and is marked read-only.
                fexprNameParam = YExpression.Parameter(typeof(JSVariable), functionName);
                var fexprVarScope = new FastFunctionScope.VariableScope
                {
                    Name = functionName,
                    Expression = JSVariable.ValueExpression(fexprNameParam),
                    Create = false
                };

                cs.AddExternalVariable(functionName, fexprVarScope);
            }

            var s = cs;
            // use this to create variables...
            // var t = s.ThisExpression;
            var args = s.ArgumentsExpression;
            var stackItem = cs.StackItem;
            var r = s.ReturnLabel;

            var inheritedStrictMode = IsStrictMode || forceStrictMode || createClass;
            var isStrictFunction = inheritedStrictMode || HasUseStrictDirective(functionDeclaration.Body);
            ValidateFunctionEarlyErrors(functionDeclaration, isStrictFunction);

            var previousStrictMode = IsStrictMode;
            IsStrictMode = isStrictFunction;

            var parameterNames = new List<StringSpan>();
            CollectParameterNames(functionDeclaration.Params, parameterNames);
            foreach (var parameterName in parameterNames)
                cs.CreateVariable(parameterName, null, true, initialize: false);
            var directEvalParameterBindings = CollectDirectEvalParameterBindings(functionDeclaration, parameterNames);

            YExpression fxName;
            YExpression localFxName;
            int nameOffset;
            int nameLength;

            if (functionName != null)
            {
                var id = functionDeclaration.Id;

                fxName = StringSpanBuilder.New(ScriptInfoBuilder.Code(scriptInfo), id.Name.Offset, id.Name.Length);
                localFxName = StringSpanBuilder.New(ScriptInfoBuilder.Code(scriptInfo), id.Name.Offset, id.Name.Length);

                nameOffset = id.Name.Offset;
                nameLength = id.Name.Length;
            }
            else if (inferredFunctionName != null)
            {
                fxName = StringSpanBuilder.New(new StringSpan(inferredFunctionName));
                localFxName = StringSpanBuilder.New(new StringSpan(inferredFunctionName));

                nameOffset = 0;
                nameLength = 0;
            }
            else
            {
                fxName = StringSpanBuilder.Empty;
                localFxName = StringSpanBuilder.Empty;

                nameOffset = 0;
                nameLength = 0;
            }

            var point = node.Start.Start;

            sList.Add(YExpression.Assign(stackItem, CallStackItemBuilder.New(cs.Context, scriptInfo, nameOffset, nameLength, point.Line, point.Column)));

            var argumentElements = args;

            var pe = functionDeclaration.Params.GetFastEnumerator();
            while (pe.MoveNext(out var v, out var i))
            {
                if (v.Identifier.IsSpreadElement(out var spe))
                {
                    CreateAssignment(bodyInits, spe.Argument, ArgumentsBuilder.RestFrom(argumentElements, (uint)i), false, true,
                        suppressAnonymousFunctionNameInference: true);
                    continue;
                }

                YExpression parameterInitializer = null;
                if (v.Init != null)
                {
                    var previousDirectEvalParameterBindings = cs.CurrentDirectEvalParameterBindings;
                    var previousInParameterInitializer = cs.InParameterInitializer;
                    cs.CurrentDirectEvalParameterBindings = directEvalParameterBindings;
                    cs.InParameterInitializer = true;
                    parameterInitializerDepth++;
                    try
                    {
                        parameterInitializer = VisitExpression(v.Init);
                    }
                    finally
                    {
                        parameterInitializerDepth--;
                        cs.InParameterInitializer = previousInParameterInitializer;
                        cs.CurrentDirectEvalParameterBindings = previousDirectEvalParameterBindings;
                    }
                }

                CreateAssignment(bodyInits, v.Identifier, JSVariableBuilder.FromArgumentOptional(argumentElements, i, parameterInitializer), false, true,
                    suppressAnonymousFunctionNameInference: true);
            }

            YExpression lambdaBody = VisitStatement(functionDeclaration.Body);

            vList.AddRange(s.VariableParameters);
            sList.AddRange(s.InitList);
            sList.AddRange(bodyInits);

            if (s.MemberInits != null)
                InitMembers(sList, s);

            if (functionDeclaration.Generator)
                sList.Add(YExpression.Yield(JSUndefinedBuilder.Value));

            sList.Add(lambdaBody);

            if (createClass)
                sList.Add(YExpression.Return(r, YExpression.Coalesce(s.ThisExpression, JSExceptionBuilder.Throw("this cannot be null"))));

            sList.Add(YExpression.Label(r, JSUndefinedBuilder.Value));

            var block = YExpression.Block(
                vList,
                YExpression.TryFinally(
                    YExpression.Block(sList),
                    JSContextStackBuilder.Pop(stackItem, cs.Context)));

            // adding lexical scope pending...

            functionName = functionName ?? inferredFunctionName ?? "inline";

            static YExpression ToDelegate(YLambdaExpression e1) => e1;

            var scriptFunctionName = new FunctionName(functionName, location, point.Line, point.Column);

            YLambdaExpression lambda;
            YExpression jsf;

            if (functionDeclaration.Generator)
            {
                lambda = GeneratorRewriter.Rewrite(in scriptFunctionName, block, cs.ReturnLabel, cs.Generator, replaceArgs: cs.Arguments, replaceStackItem: cs.StackItem,
                    replaceContext: cs.Context, replaceScriptInfo: scriptInfo);

                jsf = JSGeneratorFunctionBuilderV2.New(lambda, fxName, code, functionLength, functionDeclaration.Async, primeOnInvoke: true);
            }
            else if (functionDeclaration.Async)
            {

                lambda = GeneratorRewriter.Rewrite(in scriptFunctionName, block, cs.ReturnLabel, cs.Generator, replaceArgs: cs.Arguments, replaceStackItem: cs.StackItem,
                    replaceContext: cs.Context, replaceScriptInfo: scriptInfo);

                jsf = JSAsyncFunctionBuilder.Create(JSGeneratorFunctionBuilderV2.New(lambda, fxName, code, functionLength));
            }
            else
            {
                lambda = YExpression.Lambda(typeof(JSFunctionDelegate), block, in scriptFunctionName, [cs.Arguments]);
                jsf = JSFunctionBuilder.New(ToDelegate(lambda), fxName, code, functionLength, createPrototype: createPrototype && !functionDeclaration.IsArrowFunction);
                if (!isStrictFunction)
                    jsf = JSFunctionBuilder.EnableNonStrictThis(jsf);
                else
                    jsf = JSFunctionBuilder.EnableStrictMode(jsf);

                if (withBoundaries.Count > 0 && !isDirectEvalCompilation)
                    jsf = JSFunctionBuilder.CaptureWithScopes(jsf);
            }

            IsStrictMode = previousStrictMode;

            cs.Dispose();

            if (jsFVarScope != null)
            {
                if (isDirectEvalCompilation
                    && !usesDirectEvalLocalVarEnvironment
                    && previousScope.Function == null)
                {
                    jsFVarScope.SetPostInit(YExpression.Block(
                        JSContextBuilder.EnsureCanDeclareGlobalFunction(KeyOfName(functionName)),
                        jsf));
                }
                else
                {
                    jsFVarScope.SetPostInit(jsf);
                }

                return jsFVarScope.Expression;
            }

            // BROILER-PATCH: For function expressions with a name, wrap the result
            // in a block that creates a read-only closure variable holding the
            // function reference. The function body captures this variable.
            if (fexprNameParam != null)
            {
                var isReadOnlyField = typeof(JSVariable).GetField("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
                var fexprVars = new Sequence<YParameterExpression> { fexprNameParam };

                return YExpression.Block(fexprVars,
                    YExpression.Assign(fexprNameParam, JSVariableBuilder.New(jsf, functionName)),
                    YExpression.Assign(YExpression.Field(fexprNameParam, isReadOnlyField), YExpression.Constant(true)), 
                    JSVariable.ValueExpression(fexprNameParam));
            }

            return jsf;
        }
    }

    private static int GetExpectedArgumentCount(IFastEnumerable<VariableDeclarator> parameters)
    {
        var count = 0;
        var e = parameters.GetFastEnumerator();

        while (e.MoveNext(out var parameter))
        {
            if (parameter.Identifier.IsSpreadElement(out _)
                || parameter.Init != null)
                break;

            count++;
        }

        return count;
    }

    private void InitMembers(Sequence<YExpression> sList, FastFunctionScope s)
    {
        var @this = s.ThisExpression;
        var en = s.MemberInits.GetFastEnumerator();

        while (en.MoveNext(out var member))
        {
            var name = GetName(member);
            var value = member.Init == null ? JSUndefinedBuilder.Value : Visit(member.Init);
            var init = JSObjectBuilder.AddValue(name, value, JSPropertyAttributes.ConfigurableValue);

            sList.Add(YExpression.Call(@this, init.Member as MethodInfo, init.Arguments));
        }
    }

    private static string[] CollectDirectEvalParameterBindings(AstFunctionExpression functionDeclaration, List<StringSpan> parameterNames)
    {
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameterName in parameterNames)
            bindings.Add(parameterName.Value);

        if (functionDeclaration.Body is not AstBlock body)
            return [.. bindings];

        if (!functionDeclaration.IsArrowFunction && body.HoistingScope != null)
        {
            var hoistedNames = body.HoistingScope.GetFastEnumerator();
            while (hoistedNames.MoveNext(out var hoistedName))
            {
                if (hoistedName.Equals("arguments") || hoistedName.Equals("eval"))
                    bindings.Add(hoistedName.Value);
            }
        }

        if (!functionDeclaration.IsArrowFunction)
        {
            foreach (var lexicalBinding in CollectTopLevelLexicalBindings(body.Statements))
            {
                if (lexicalBinding is "arguments" or "eval")
                    bindings.Add(lexicalBinding);
            }
        }

        return [.. bindings];
    }
}
