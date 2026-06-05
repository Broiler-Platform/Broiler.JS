using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using System.Collections.Generic;
using System;
using System.Linq;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    class SwitchInfo(FastPool.Scope scope)
    {
        public Sequence<YExpression> Tests = [];
        public Sequence<YExpression> Body;
        public readonly YLabelTarget Label = YExpression.Label("case-start");
    }

    protected override YExpression VisitSwitchStatement(AstSwitchStatement switchStatement)
    {
        bool allStrings = true;
        bool allNumbers = true;
        bool allIntegers = true;

        var switchLexicalScope = this.scope.Push(new FastFunctionScope(this.scope.Top));
        var lexicalBindings = CollectSwitchLexicalBindings(switchStatement.Cases);
        var hoistingScope = switchStatement.HoistingScope;
        if (hoistingScope != null)
        {
            var hoisted = hoistingScope.GetFastEnumerator();
            while (hoisted.MoveNext(out var name))
            {
                // Only genuine let/const/class names are lexical bindings of the
                // CaseBlock. An Annex B case-body FunctionDeclaration is var-like
                // (it hoists to the enclosing function/program scope), so marking
                // it lexical would wrongly make it block its own Annex B hoisting
                // via IsAnnexBHoistingBlocked.
                var isLexical = lexicalBindings.Contains(name.Value);
                var variable = switchLexicalScope.CreateVariable(name, null, true, initialize: isLexical == false);
                variable.IsLexical = isLexical;
            }
        }

        var switchPoolScope = pool.NewScope();

        try
        {
            Sequence<YExpression> defBody = null;
            var @continue = this.scope.Top.Loop?.Top?.Continue;
            var @break = YExpression.Label();
            var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
            var outerCompletionVars = GetCompletionVariables();
            var ls = new LoopScope(@break, @continue, true) { CompletionVariable = completionVar };
            var cases = new Sequence<SwitchInfo>(switchStatement.Cases.Count + 2);
            var functionInitializers = new Sequence<YExpression>();
            using var completion = completionScopes.Push(completionVar);
            using var bt = this.scope.Top.Loop.Push(ls);
            SwitchInfo lastCase = new(switchPoolScope);
            var casesEn = switchStatement.Cases.GetFastEnumerator();

            while (casesEn.MoveNext(out var c))
            {
                var body = new Sequence<YExpression>(c.Statements.Count);
                var en = c.Statements.GetFastEnumerator();

                while (en.MoveNext(out var es))
                {
                    switch (es)
                    {
                        case AstExpressionStatement { Expression: AstFunctionExpression { Id: { } } functionDeclaration }:
                            functionInitializers.Add(CreateSwitchFunctionInitializer(functionDeclaration));
                            break;

                        case AstStatement stmt:
                            body.Add(TrackCompletion(VisitStatement(stmt)));
                            break;

                        default:
                            throw new FastParseException(es.Start, $"Invalid statement {es.Type}");
                    }
                }

                if (c.Test == null)
                {
                    defBody = body;
                    lastCase = new SwitchInfo(switchPoolScope);

                    continue;
                }

                YExpression test = null;
                switch (c.Test.Type)
                {
                    case FastNodeType.UnaryExpression:
                        var unary = c.Test as AstUnaryExpression;
                        var isTestSet = false;

                        switch (unary.Operator)
                        {
                            case UnaryOperator.Plus:
                            case UnaryOperator.Minus:
                                if (unary.Argument.Type == FastNodeType.Literal)
                                {
                                    var l = unary.Argument as AstLiteral;

                                    if (l.TokenType == TokenTypes.Number)
                                    {
                                        var n = l.NumericValue;
                                        if ((n % 1) != 0)
                                            allIntegers = false;

                                        var ln = l.NumericValue;
                                        if (unary.Operator == UnaryOperator.Minus)
                                            ln = -ln;

                                        test = YExpression.Constant(ln);
                                        isTestSet = true;
                                        break;
                                    }
                                }

                                break;
                        }

                        if (!isTestSet)
                        {
                            test = VisitExpression(c.Test);
                            allNumbers = false;
                            allStrings = false;
                            allIntegers = false;
                        }

                        break;

                    case FastNodeType.Literal:
                        var literal = c.Test as AstLiteral;

                        switch (literal.TokenType)
                        {
                            case TokenTypes.String:
                                allNumbers = false;
                                // allStrings = allStrings && true ;
                                test = YExpression.Constant(literal.StringValue);
                                break;

                            case TokenTypes.Number:
                                var n = literal.NumericValue;
                                if ((n % 1) != 0)
                                    allIntegers = false;

                                test = YExpression.Constant(literal.NumericValue);
                                break;

                            case TokenTypes.True:
                                allNumbers = false;
                                allStrings = false;
                                allIntegers = false;
                                test = JSBooleanBuilder.True;
                                break;

                            case TokenTypes.False:
                                allNumbers = false;
                                allStrings = false;
                                allIntegers = false;
                                test = JSBooleanBuilder.False;
                                break;

                            default:
                                // null / bigint / regexp / etc. literal case:
                                // evaluate it and compare via strict-equals like a
                                // general expression case.
                                test = VisitExpression(c.Test);
                                allNumbers = false;
                                allStrings = false;
                                allIntegers = false;
                                break;
                        }

                        break;
                    default:
                        test = VisitExpression(c.Test);
                        allNumbers = false;
                        allStrings = false;
                        allIntegers = false;

                        break;
                }

                lastCase.Tests.Add(test);

                if (body.Count > 0)
                {
                    cases.Add(lastCase);
                    body.Insert(0, YExpression.Label(lastCase.Label));
                    lastCase.Body = body;
                    lastCase = new SwitchInfo(switchPoolScope);
                }
            }

            if (lastCase.Tests.Any())
            {
                lastCase.Body =
                [
                    YExpression.Label(lastCase.Label),
                    YExpression.Empty
                ];
                cases.Add(lastCase);
            }

            System.Reflection.MethodInfo equalsMethod = null;

            SwitchInfo last = null;
            foreach (var @case in cases)
            {
                // if last one is not break statement... make it fall through...
                last?.Body.Add(YExpression.Goto(@case.Label));
                last = @case;

                if (allNumbers)
                {
                    if (allIntegers)
                    {
                        @case.Tests = @case.Tests.ConvertToInteger(switchPoolScope);
                    }
                    else
                    {
                        // convert every case to double..
                        @case.Tests = @case.Tests.ConvertToNumber(switchPoolScope);
                    }
                }
                else
                {
                    if (allStrings)
                    {
                        // force everything to string if it isn't
                        @case.Tests = @case.Tests.ConvertToString(switchPoolScope);
                    }
                    else
                    {
                        @case.Tests = @case.Tests.ConvertToJSValue(switchPoolScope);
                        // SwitchStatement uses the Strict Equality Comparison (===),
                        // so e.g. `case null` must not match an `undefined`
                        // discriminant.
                        equalsMethod = JSValueBuilder.StaticStrictEquals;
                    }
                }
            }

            var testTarget = VisitExpression(switchStatement.Target);
            if (allNumbers)
            {
                if (allIntegers)
                {
                    testTarget = JSValueBuilder.IntValue(testTarget);
                }
                else
                {
                    testTarget = JSValueBuilder.DoubleValue(testTarget);
                }
            }
            else
            {
                if (allStrings)
                {
                    testTarget = ObjectBuilder.ToString(testTarget);
                }
                else
                {

                }
            }

            YExpression d = null;
            var lastLine = switchStatement.Start.Start.Line;

            if (defBody != null)
            {
                var defLabel = YExpression.Label($"default-start-{lastLine}");
                last?.Body.Add(YExpression.Goto(defLabel));

                defBody.Insert(0, YExpression.Label(defLabel));
                d = YExpression.Block(defBody);
            }

            var statements = new Sequence<YExpression>
            {
                YExpression.Assign(completionVar, JSUndefinedBuilder.Value)
            };
            if (functionInitializers.Any())
                statements.Add(YExpression.Block(functionInitializers));
            statements.Add(
                YExpression.TailCallTransparentTryFinally(
                    YExpression.Switch(testTarget, d.ToJSValue() ?? JSUndefinedBuilder.Value, equalsMethod, [.. cases.Select(x =>
                    YExpression.SwitchCase(YExpression.Block(x.Body).ToJSValue(), x.Tests))]),
                    PropagateCompletion(completionVar, outerCompletionVars)));
            statements.Add(YExpression.Label(@break));
            statements.Add(completionVar);

            var r = YExpression.Block(new Sequence<YParameterExpression> { completionVar }, statements);
            return Scoped(switchLexicalScope, new Sequence<YExpression> { r });
        }
        finally
        {
            switchPoolScope.Dispose();
            switchLexicalScope.Dispose();
        }
    }

    private YExpression CreateSwitchFunctionInitializer(AstFunctionExpression functionDeclaration)
    {
        var currentBinding = scope.Top.GetVariable(functionDeclaration.Id!.Name)!;
        var result = CreateFunction(functionDeclaration, hoistStatementDeclaration: false);

        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var variables = new Sequence<YParameterExpression> { temp.Variable };
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, result),
            YExpression.Assign(currentBinding.Expression, temp.Variable)
        };

        AppendAnnexBOuterBindingAssignments(statements, currentBinding, functionDeclaration.Id.Name, temp.Variable);
        return YExpression.Block(variables, statements);
    }

    private static HashSet<string> CollectSwitchLexicalBindings(IFastEnumerable<Case> cases)
    {
        var lexicalBindings = new HashSet<string>(StringComparer.Ordinal);
        var casesEn = cases.GetFastEnumerator();

        while (casesEn.MoveNext(out var @case))
        {
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
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
        }

        return lexicalBindings;
    }
}
