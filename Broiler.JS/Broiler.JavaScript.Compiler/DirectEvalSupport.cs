using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.Compiler;

public static class DirectEvalSupport
{
    private sealed class TransientCodeCache : ICodeCache
    {
        // Front end and emitter under one boundary, so an eval body deep enough to need the
        // stack takes a single handoff and one short enough not to takes none. See
        // CompilationStack: without this the two guard themselves separately.
        public JSFunctionDelegate GetOrCreate(in JSCode code)
        {
            var compiler = code.Compiler;
            return ExpressionCompiler.CompilationStack.Run(
                () => compiler().CompileWithNestedLambdas(),
                code.Code.Length);
        }
    }

    private sealed class DeclaredBindingSnapshot(JSContext context, string[] names, string[] excludedNames) : IDisposable
    {
        private readonly JSContext context = context;
        private readonly Entry[] entries = names == null ? [] : CreateEntries(context, names, excludedNames);

        private sealed class Entry
        {
            public required KeyString Name;
            public required bool HadOwnProperty;
            public required JSValue PreviousValue;
        }

        private static Entry[] CreateEntries(JSContext context, string[] names, string[] excludedNames)
        {
            var seen = new HashSet<uint>();
            if (excludedNames != null)
            {
                foreach (var excludedName in excludedNames)
                {
                    if (string.IsNullOrWhiteSpace(excludedName))
                        continue;

                    seen.Add(KeyStrings.GetOrCreate(excludedName).Key);
                }
            }
            var entries = new List<Entry>(names.Length);
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var key = KeyStrings.GetOrCreate(name);
                if (!seen.Add(key.Key))
                    continue;

                var property = context.GetInternalProperty(key, false);
                entries.Add(new Entry
                {
                    Name = key,
                    HadOwnProperty = !property.IsEmpty,
                    PreviousValue = property.IsEmpty ? JSUndefined.Value : context.GetOwnPropertyValue(key)
                });
            }

            return [.. entries];
        }

        public void Dispose()
        {
            foreach (var entry in entries)
            {
                if (entry.HadOwnProperty)
                    context.SetOwnPropertyValue(entry.Name, entry.PreviousValue);
                else
                    context.Delete(entry.Name);
            }
        }
    }

    public static JSValue Execute(Arguments arguments, JSValue callee, JSValue @this, FrameToken activationOwner, bool inheritStrictMode, bool disallowArgumentsDeclaration, string[] lexicalBindings, JSVariable[] capturedBindings, JSVariable[] shadowedBindings, string[] capturedLexicalBindingNames, string[] parameterBindings, string[] privateNamesInScope, bool allowSuperProperty, bool allowSuperCall, bool useActivationBinding = false, JSValue directEvalSuper = null, bool inFieldInitializer = false, bool rejectNewTarget = false, JSValue directEvalSuperConstructor = null, JSVariable directEvalThisBinding = null, string[] evalVarEnvNames = null, JSValue directEvalNewTarget = null, bool tailCall = false)
    {
        if (!IsDirectEval(callee))
        {
            // An `eval(...)` whose callee is not %eval% is an ordinary call. When it
            // sits in a proper-tail-call position (BROILER_SCRIPT_HOST), the IL
            // generator forces tailCall=true so we hand a JSTailCall sentinel back to
            // the enclosing trampoline instead of recursing through InvokeFunction —
            // otherwise deep `return eval(n-1)` self-recursion overflows the stack
            // (test262 tco-non-eval-global/with/function-dynamic, #648 problem 2).
            return tailCall
                ? new JSTailCall(callee, arguments)
                : callee.InvokeFunction(arguments);
        }

        var value = arguments.Get1();
        if (!value.IsString)
            return value;

        var text = value.StringValue;
        string location = null;

        (JSEngine.Current as IJSExecutionContext)?.DispatchEvalEvent(ref text, ref location);

        if (inheritStrictMode)
            text = "\"use strict\";\n" + text;

        var program = Validate(text, inheritStrictMode, disallowArgumentsDeclaration, lexicalBindings, parameterBindings, privateNamesInScope, allowSuperProperty, allowSuperCall, rejectArguments: inFieldInitializer, rejectNewTarget: rejectNewTarget);

        // Nothing to run: PerformEval owes an empty StatementList the undefined completion,
        // and a body with no statements declares nothing, so none of the scope, activation
        // and binding-snapshot scaffolding below can be observed either.
        if (program.Statements.Count == 0)
            return JSUndefined.Value;

        // Nothing to compile either. A body whose every statement is a literal expression —
        // `eval("/" + character + "/")`, `eval("0")` — declares nothing, reads no binding and calls
        // nothing, so its completion value is that last literal and the compile is pure overhead.
        // And it is nearly all of the cost: emitting and JITting one throwaway method is ~2.6 ms in
        // a Release build against ~13 µs to parse the same text. test262 runs exactly that loop
        // 65 536 times, once per BMP code unit (language/literals/regexp/S7.8.5_A*_T2, annexB
        // RegExp-{leading,trailing}-escape-BMP), where it decides between four CPU-minutes and two
        // seconds — and the runner caps a test at 30 CPU-seconds.
        if (TryEvaluateLiteralProgram(program, inheritStrictMode, out var literalCompletion))
            return literalCompletion;

        // The program Validate already parsed, rather than a second parse of the same text.
        var declaredBindings = disallowArgumentsDeclaration ? CollectProgramDeclaredBindings(program) : null;

        if (JSEngine.Current is JSContext context)
        {
            var requiresActivation = disallowArgumentsDeclaration || useActivationBinding;
            using var _ = capturedBindings?.Length > 0
                ? context.PushDirectEvalScope(capturedBindings, shadowedBindings)
                : null;
            // Pushed for every direct eval (even with no names) so the innermost var-env-name scope
            // always corresponds to the currently running eval (see IsImmediateEvalVarEnvName).
            using var varEnvNameScope = context.PushDirectEvalVarEnvNames(evalVarEnvNames);
            using var lexicalBindingScope = capturedLexicalBindingNames?.Length > 0
                ? context.PushDirectEvalLexicalBindingNames(capturedLexicalBindingNames)
                : null;
            using var ____ = requiresActivation
                ? context.PushDirectEvalActivation(activationOwner)
                : null;
            using var ___ = disallowArgumentsDeclaration
                ? new DeclaredBindingSnapshot(context, declaredBindings, ExtractBindingNames(capturedBindings))
                : null;
            using var superScope = allowSuperProperty
                ? context.PushDirectEvalSuper(directEvalSuper)
                : null;
            // Share the derived constructor's superclass constructor and `this` binding
            // so a `super(...)` inside the eval initializes the constructor's own `this`.
            using var superCallScope = directEvalThisBinding != null
                ? context.PushDirectEvalSuperCall(directEvalSuperConstructor, directEvalThisBinding)
                : null;
            // Thread the caller's new.target so `new.target` at the eval's top level
            // (and a nested eval) observes it. Pushed unconditionally so a nested
            // direct eval inherits the same value rather than reading undefined.
            using var newTargetScope = context.PushDirectEvalNewTarget(directEvalNewTarget, rejectNewTarget);
            using var __ = context.PushDirectEvalCompilation(requiresActivation, privateNamesInScope);
            // The completion value of the eval body is a real value, not a tail
            // call of the surrounding function: eval is a syntactic boundary. Under
            // BROILER_SCRIPT_HOST the trailing expression may be emitted as a
            // JSTailCall sentinel (proper tail call); force it here so the call
            // actually runs even when the eval result is discarded.
            var result = JSTailCall.Resolve(
                CoreScript.Compile(text, location, null, new TransientCodeCache())(new Arguments(@this ?? context)));
            if (declaredBindings?.Length > 0 && capturedBindings?.Length > 0)
            {
                foreach (var declaredBinding in declaredBindings)
                {
                    if (string.IsNullOrWhiteSpace(declaredBinding))
                        continue;

                    foreach (var capturedBinding in capturedBindings)
                    {
                        if (capturedBinding == null || !capturedBinding.Name.Equals(declaredBinding))
                            continue;

                        // The eval's declarations live in its VARIABLE environment. Reading them
                        // back through the `with`-aware ResolveIdentifier let an enclosing
                        // `with (o)` whose object happens to own the name answer with o[name],
                        // which then overwrote the caller's binding — B.3.3.3 sets the function on
                        // the nearest VariableEnvironment and must neither touch nor consult o.
                        capturedBinding.Value = context.ResolveIdentifierWithoutWithScopes(KeyStrings.GetOrCreate(declaredBinding));
                        break;
                    }
                }
            }

            return result;
        }

        return CoreScript.Evaluate(text, location);
    }


    private static void ValidateDirectEvalEarlyErrors(AstProgram program, bool allowSuperProperty, bool allowSuperCall, bool rejectArguments, bool rejectNewTarget)
        => new DirectEvalEarlyErrorValidator(allowSuperProperty, allowSuperCall, rejectArguments, rejectNewTarget).Visit(program);

    /// <summary>
    /// Enforces the PerformEval early-error rules that depend on the syntactic
    /// position of the eval call. Recursion stops at non-arrow function
    /// boundaries (only arrow bodies are descended into) so that, like the spec
    /// productions Contains SuperCall / ContainsArguments / new.target, the
    /// checks apply to the eval's own statement list and any nested arrows but
    /// not to nested ordinary functions, which establish their own bindings.
    /// </summary>
    /// <remarks>
    /// <paramref name="rejectArguments"/> implements "Syntax Error if
    /// ContainsArguments" for a direct eval inside a class field initializer.
    /// <paramref name="rejectNewTarget"/> rejects <c>new.target</c> appearing at
    /// the top level of an indirect eval (global code), where it is not allowed.
    /// </remarks>
    private sealed class DirectEvalEarlyErrorValidator(bool allowSuperProperty, bool allowSuperCall, bool rejectArguments, bool rejectNewTarget) : AstReduce
    {
        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstSuper)
            {
                if (!allowSuperCall)
                    throw new FastParseException(callExpression.Start, "Unexpected super call in eval code");

                VisitArguments(callExpression.Arguments);
                return callExpression;
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitMemberExpression(AstMemberExpression memberExpression)
        {
            if (memberExpression.Object is AstSuper)
            {
                if (!allowSuperProperty)
                    throw new FastParseException(memberExpression.Start, "Unexpected super property in eval code");

                if (memberExpression.Computed)
                    Visit(memberExpression.Property);

                return memberExpression;
            }

            // Only a computed property is an expression position; a static
            // property name such as `obj.arguments` is not an IdentifierReference
            // and must not be treated as one by the ContainsArguments check.
            Visit(memberExpression.Object);
            if (memberExpression.Computed)
                Visit(memberExpression.Property);

            return memberExpression;
        }

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            if (rejectArguments && identifier.Name.Equals("arguments"))
                throw new FastParseException(identifier.Start, "Unexpected arguments in eval code inside a class field initializer");

            return identifier;
        }

        protected override AstNode VisitMeta(AstMeta astMeta)
        {
            if (rejectNewTarget
                && astMeta.Identifier.Name.Equals("new")
                && astMeta.Property.Name.Equals("target"))
            {
                throw new FastParseException(astMeta.Start, "Unexpected new.target in eval code");
            }

            return astMeta;
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            Visit(functionExpression.Id);
            var parameters = functionExpression.Params.GetFastEnumerator();
            while (parameters.MoveNext(out var parameter))
                VisitVariableDeclarator(parameter);

            if (functionExpression.IsArrowFunction)
                Visit(functionExpression.Body);

            return functionExpression;
        }

        private void VisitArguments(IFastEnumerable<AstExpression> arguments)
        {
            var enumerator = arguments.GetFastEnumerator();
            while (enumerator.MoveNext(out var argument))
                Visit(argument);
        }
    }

    private static string[] CollectProgramDeclaredBindings(AstProgram program)
    {
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        CollectDeclaredBindings(program.Statements, bindings);

        return [.. bindings];
    }

    private static void CollectDeclaredBindings(IFastEnumerable<AstStatement> statements, HashSet<string> bindings)
    {
        if (statements == null)
            return;

        var enumerator = statements.GetFastEnumerator();
        while (enumerator.MoveNext(out var statement))
            CollectDeclaredBindings(statement, bindings);
    }

    private static void CollectDeclaredBindings(AstStatement statement, HashSet<string> bindings)
    {
        switch (statement)
        {
            case AstBlock block:
                CollectDeclaredBindings(block.Statements, bindings);
                break;

            case AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true, Id: { } id } }:
                bindings.Add(id.Name.Value);
                break;

            case AstIfStatement ifStatement:
                CollectDeclaredBindings(ifStatement.True, bindings);
                if (ifStatement.False != null)
                    CollectDeclaredBindings(ifStatement.False, bindings);
                break;

            case AstTryStatement tryStatement:
                CollectDeclaredBindings(tryStatement.Block, bindings);
                if (tryStatement.Catch != null)
                    CollectDeclaredBindings(tryStatement.Catch, bindings);
                if (tryStatement.Finally != null)
                    CollectDeclaredBindings(tryStatement.Finally, bindings);
                break;

            case AstSwitchStatement switchStatement:
                var cases = switchStatement.Cases.GetFastEnumerator();
                while (cases.MoveNext(out var @case))
                    CollectDeclaredBindings(@case.Statements, bindings);
                break;

            case AstWhileStatement whileStatement:
                CollectDeclaredBindings(whileStatement.Body, bindings);
                break;

            case AstDoWhileStatement doWhileStatement:
                CollectDeclaredBindings(doWhileStatement.Body, bindings);
                break;

            case AstForStatement forStatement:
                CollectDeclaredBindings(forStatement.Body, bindings);
                break;

            case AstForInStatement forInStatement:
                CollectDeclaredBindings(forInStatement.Body, bindings);
                break;

            case AstForOfStatement forOfStatement:
                CollectDeclaredBindings(forOfStatement.Body, bindings);
                break;

            case AstLabeledStatement labeledStatement:
                CollectDeclaredBindings(labeledStatement.Body, bindings);
                break;
        }
    }

    private static string[] ExtractBindingNames(JSVariable[] bindings)
    {
        if (bindings == null || bindings.Length == 0)
            return [];

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            if (binding == null || binding.Name.IsEmpty)
                continue;

            names.Add(binding.Name.Value);
        }

        return [.. names];
    }

    /// <summary>
    /// Reports whether every statement of <paramref name="program"/> is an expression statement over
    /// a literal, and if so hands back the completion value: the last of those literals. Such a body
    /// has no declarations, no references and no calls, so evaluating it is exactly building that one
    /// value — see the call site for why that is worth short-circuiting.
    /// </summary>
    /// <param name="injectedStrictDirective">
    /// True when <see cref="Execute"/> prefixed the source with its own <c>"use strict";</c>. That
    /// line is not part of the program the caller wrote and its value must not become the completion
    /// value, so it is skipped; a body that is nothing else falls through to the ordinary path.
    /// </param>
    private static bool TryEvaluateLiteralProgram(AstProgram program, bool injectedStrictDirective, out JSValue value)
    {
        value = JSUndefined.Value;

        var statements = program.Statements.GetFastEnumerator();
        var first = true;
        var any = false;
        while (statements.MoveNext(out var statement))
        {
            if (first)
            {
                first = false;
                if (injectedStrictDirective)
                    continue;
            }

            if (statement is not AstExpressionStatement expressionStatement
                || expressionStatement.Expression is not AstLiteral literal
                || !TryCreateLiteralValue(literal, out var literalValue))
            {
                value = JSUndefined.Value;
                return false;
            }

            value = literalValue;
            any = true;
        }

        // Reaching here with nothing found means the injected directive was the only statement, so
        // the caller wrote a body with no statements of its own — PerformEval owes that the
        // undefined completion, not the text of a directive it never wrote.
        return any || injectedStrictDirective;
    }

    /// <summary>
    /// Builds the value of a literal the way <c>FastCompiler.VisitLiteral</c> would, without
    /// generating code for it. BigInt and Decimal literals are deliberately not handled — they
    /// would need their own runtime factories and no measured workload evaluates them this way.
    /// </summary>
    private static bool TryCreateLiteralValue(AstLiteral literal, out JSValue value)
    {
        switch (literal.TokenType)
        {
            case TokenTypes.True:
                value = JSValue.BooleanTrue;
                return true;

            case TokenTypes.False:
                value = JSValue.BooleanFalse;
                return true;

            case TokenTypes.Null:
                value = JSValue.NullValue;
                return true;

            case TokenTypes.Number when JSNumberBuilder.Create is { } number:
                value = number(literal.NumericValue);
                return true;

            case TokenTypes.String when JSStringBuilder.Create is { } text:
                value = text(literal.StringValue);
                return true;

            case TokenTypes.RegExLiteral when JSRegExpBuilder.Create is { } regexp:
                var (pattern, flags) = literal.Regex;
                value = regexp(pattern, flags);
                return true;
        }

        value = JSUndefined.Value;
        return false;
    }

    private static bool IsDirectEval(JSValue callee)
    {
        if (JSEngine.CurrentContext is JSContext context)
            return !context.IntrinsicEval.IsUndefined && callee.StrictEquals(context.IntrinsicEval);

        var globalEval = (JSEngine.CurrentContext as JSObject)?[KeyStrings.eval];
        return !globalEval.IsUndefined && callee.StrictEquals(globalEval);
    }

    /// <summary>
    /// Runs the indirect-eval early errors over <paramref name="text"/> and reports whether
    /// its body has any statements at all, so an inert body can skip compilation the same
    /// way <see cref="Execute"/> does.
    /// </summary>
    public static bool ValidateIndirectEval(string text)
        => Validate(text, false, false, null, null, null, false, false, rejectNewTarget: true)
            .Statements.Count > 0;

    /// <summary>
    /// Enforces the eval early errors and returns the program they were checked against, so
    /// the caller can reuse that parse rather than paying for another one.
    /// </summary>
    private static AstProgram Validate(string text, bool inheritStrictMode, bool disallowArgumentsDeclaration, string[] lexicalBindings, string[] parameterBindings, string[] privateNamesInScope, bool allowSuperProperty, bool allowSuperCall, bool rejectArguments = false, bool rejectNewTarget = false)
    {
        if (inheritStrictMode && ContainsStrictReservedWordUsage(text))
            throw JSEngine.NewSyntaxError("Unexpected strict mode reserved word");

        try
        {
            var pool = new FastPool();
            var parser = new FastParser(new FastTokenStream(pool, text));
            var program = parser.ParseProgram();
            ValidateDirectEvalEarlyErrors(program, allowSuperProperty, allowSuperCall, rejectArguments, rejectNewTarget);
            SyntaxValidation.ValidateProgram(program, text, inheritStrictMode, lexicalBindings, privateNamesInScope);
            if (parameterBindings?.Length > 0
                && SyntaxValidation.ContainsDirectEvalVarConflict(program.Statements, parameterBindings))
            {
                throw new FastParseException(program.Start, "Invalid declaration in direct eval code");
            }

            // `var arguments` (and likewise a function/class/catch binding named
            // "arguments") is an early error only when the direct eval is in a
            // PARAMETER INITIALIZER — signalled here by a non-empty parameterBindings,
            // which is populated solely while a parameter initializer is compiled. In
            // the function body it is allowed in non-strict mode (test262 12.2.1-11);
            // strict mode is still rejected via the inheritStrictMode path.
            var inParameterInitializer = parameterBindings?.Length > 0;
            var statements = program.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
            {
                if (IsRestrictedDeclaration(statement, inheritStrictMode, disallowArgumentsDeclaration && inParameterInitializer))
                    throw new FastParseException(statement.Start, "Invalid declaration in direct eval code");
            }

            return program;
        }
        catch (FastParseException ex)
        {
            throw JSEngine.NewSyntaxError(ex.Message);
        }
    }

    private static bool ContainsStrictReservedWordUsage(string text)
    {
        var pool = new FastPool();
        var stream = new FastTokenStream(pool, text);
        FastToken previous = null;

        while (stream.Current.Type != TokenTypes.EOF)
        {
            var token = stream.Current;
            if (token.IsKeyword
                && IsStrictReservedWord(token.Keyword)
                && previous?.Type is not (TokenTypes.Dot or TokenTypes.QuestionDot))
            {
                var nextType = stream.Next?.Type ?? TokenTypes.EOF;
                if (nextType is TokenTypes.Assign
                    or TokenTypes.Increment
                    or TokenTypes.Decrement
                    or TokenTypes.AssignAdd
                    or TokenTypes.AssignSubtract
                    or TokenTypes.AssignMultiply
                    or TokenTypes.AssignDivide
                    or TokenTypes.AssignMod
                    or TokenTypes.AssignBitwideAnd
                    or TokenTypes.AssignBitwideOr
                    or TokenTypes.AssignCoalesce
                    or TokenTypes.AssignBooleanAnd
                    or TokenTypes.AssignBooleanOr
                    or TokenTypes.AssignLeftShift
                    or TokenTypes.AssignPower
                    or TokenTypes.AssignRightShift
                    or TokenTypes.AssignUnsignedRightShift
                    or TokenTypes.AssignXor)
                {
                    return true;
                }
            }

            if (token.Type != TokenTypes.LineTerminator)
                previous = token;

            stream.Consume();
        }

        return false;
    }

    private static bool IsStrictReservedWord(FastKeywords keyword)
        => keyword is FastKeywords.@implements
            or FastKeywords.@interface
            or FastKeywords.@package
            or FastKeywords.@private
            or FastKeywords.@protected
            or FastKeywords.@public
            or FastKeywords.@static
            or FastKeywords.@yield;

    private static bool IsRestrictedDeclaration(AstStatement statement, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        return statement switch
        {
            AstVariableDeclaration declaration => ContainsRestrictedDeclarator(declaration.Declarators, inheritStrictMode, disallowArgumentsDeclaration),
            AstExpressionStatement { Expression: AstFunctionExpression function } => IsRestrictedName(function.Id?.Name, inheritStrictMode, disallowArgumentsDeclaration),
            AstExpressionStatement { Expression: AstClassExpression @class } => IsRestrictedName(@class.Identifier?.Name, inheritStrictMode, disallowArgumentsDeclaration),
            AstTryStatement @try => @try.CatchParam is AstIdentifier catchId
                ? IsRestrictedName(catchId.Name, inheritStrictMode, disallowArgumentsDeclaration)
                : @try.CatchParam != null && ContainsRestrictedBinding(@try.CatchParam, inheritStrictMode, disallowArgumentsDeclaration),
            AstExportStatement { Declaration: AstVariableDeclaration declaration } => ContainsRestrictedDeclarator(declaration.Declarators, inheritStrictMode, disallowArgumentsDeclaration),
            AstExportStatement { Declaration: AstFunctionExpression function } => IsRestrictedName(function.Id?.Name, inheritStrictMode, disallowArgumentsDeclaration),
            AstExportStatement { Declaration: AstClassExpression @class } => IsRestrictedName(@class.Identifier?.Name, inheritStrictMode, disallowArgumentsDeclaration),
            _ => false,
        };
    }

    private static bool ContainsRestrictedDeclarator(IFastEnumerable<VariableDeclarator> declarators, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        var enumerator = declarators.GetFastEnumerator();
        while (enumerator.MoveNext(out var declarator))
        {
            if (ContainsRestrictedBinding(declarator.Identifier, inheritStrictMode, disallowArgumentsDeclaration))
                return true;
        }

        return false;
    }

    private static bool ContainsRestrictedBinding(AstExpression expression, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        return expression switch
        {
            AstIdentifier identifier => IsRestrictedName(identifier.Name, inheritStrictMode, disallowArgumentsDeclaration),
            AstBinaryExpression assignment => ContainsRestrictedBinding(assignment.Left, inheritStrictMode, disallowArgumentsDeclaration),
            AstSpreadElement spread => ContainsRestrictedBinding(spread.Argument, inheritStrictMode, disallowArgumentsDeclaration),
            AstArrayPattern array => ContainsRestrictedBinding(array.Elements, inheritStrictMode, disallowArgumentsDeclaration),
            AstObjectPattern @object => ContainsRestrictedBinding(@object.Properties, inheritStrictMode, disallowArgumentsDeclaration),
            _ => false,
        };
    }

    private static bool ContainsRestrictedBinding(IFastEnumerable<AstExpression> expressions, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        var enumerator = expressions.GetFastEnumerator();
        while (enumerator.MoveNext(out var expression))
        {
            if (ContainsRestrictedBinding(expression, inheritStrictMode, disallowArgumentsDeclaration))
                return true;
        }

        return false;
    }

    private static bool ContainsRestrictedBinding(IFastEnumerable<ObjectProperty> properties, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        var enumerator = properties.GetFastEnumerator();
        while (enumerator.MoveNext(out var property))
        {
            if (ContainsRestrictedBinding(property.Value, inheritStrictMode, disallowArgumentsDeclaration))
                return true;
        }

        return false;
    }

    private static bool IsRestrictedName(StringSpan? name, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        if (name == null)
            return false;

        if (inheritStrictMode && (name.Value.Equals("arguments") || name.Value.Equals("eval")))
            return true;

        // `var arguments` is rejected only when the direct eval is in a parameter
        // initializer (disallowArgumentsDeclaration); in the function body it is
        // allowed (test262 12.2.1-11), where it just refers to the arguments object.
        return disallowArgumentsDeclaration && name.Value.Equals("arguments");
    }
}
