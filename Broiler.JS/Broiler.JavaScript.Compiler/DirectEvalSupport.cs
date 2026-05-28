using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.Compiler;

public static class DirectEvalSupport
{
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

    public static JSValue Execute(Arguments arguments, JSValue callee, JSValue @this, CallStackItem activationOwner, bool inheritStrictMode, bool disallowArgumentsDeclaration, string[] lexicalBindings, JSVariable[] capturedBindings, string[] parameterBindings, string[] privateNamesInScope, bool useActivationBinding = false)
    {
        if (!IsDirectEval(callee))
            return callee.InvokeFunction(arguments);

        var value = arguments.Get1();
        if (!value.IsString)
            return value;

        var text = value.StringValue;
        string location = null;

        (JSEngine.Current as IJSExecutionContext)?.DispatchEvalEvent(ref text, ref location);

        if (inheritStrictMode)
            text = "\"use strict\";\n" + text;

        Validate(text, inheritStrictMode, disallowArgumentsDeclaration, lexicalBindings, parameterBindings, privateNamesInScope);
        var declaredBindings = disallowArgumentsDeclaration ? CollectProgramDeclaredBindings(text) : null;

        if (JSEngine.Current is JSContext context)
        {
            var requiresActivation = disallowArgumentsDeclaration || useActivationBinding;
            using var _ = disallowArgumentsDeclaration
                ? context.PushDirectEvalScope(capturedBindings)
                : null;
            using var ____ = requiresActivation
                ? context.PushDirectEvalActivation(activationOwner)
                : null;
            using var ___ = disallowArgumentsDeclaration
                ? new DeclaredBindingSnapshot(context, declaredBindings, ExtractBindingNames(capturedBindings))
                : null;
            using var __ = context.PushDirectEvalCompilation(requiresActivation);
            return context.Eval(text, location, @this ?? context);
        }

        return CoreScript.Evaluate(text, location);
    }

    private static string[] CollectProgramDeclaredBindings(string text)
    {
        var pool = new FastPool();
        var parser = new FastParser(new FastTokenStream(pool, text));
        var program = parser.ParseProgram();
        if (program.HoistingScope == null)
            return [];

        var bindings = new HashSet<string>(StringComparer.Ordinal);
        var hoisted = program.HoistingScope.GetFastEnumerator();
        while (hoisted.MoveNext(out var name))
            bindings.Add(name.Value);

        return [.. bindings];
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

    private static bool IsDirectEval(JSValue callee)
    {
        if (JSEngine.CurrentContext is JSContext context)
            return !context.IntrinsicEval.IsUndefined && callee.StrictEquals(context.IntrinsicEval);

        var globalEval = (JSEngine.CurrentContext as JSObject)?[KeyStrings.eval];
        return !globalEval.IsUndefined && callee.StrictEquals(globalEval);
    }

    private static void Validate(string text, bool inheritStrictMode, bool disallowArgumentsDeclaration, string[] lexicalBindings, string[] parameterBindings, string[] privateNamesInScope)
    {
        if (inheritStrictMode && ContainsStrictReservedWordUsage(text))
            throw JSEngine.NewSyntaxError("Unexpected strict mode reserved word");

        try
        {
            var pool = new FastPool();
            var parser = new FastParser(new FastTokenStream(pool, text));
            var program = parser.ParseProgram();
            SyntaxValidation.ValidateProgram(program, text, inheritStrictMode, lexicalBindings, privateNamesInScope);
            if (parameterBindings?.Length > 0
                && SyntaxValidation.ContainsDirectEvalVarConflict(program.Statements, parameterBindings))
            {
                throw new FastParseException(program.Start, "Invalid declaration in direct eval code");
            }

            var statements = program.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
            {
                if (IsRestrictedDeclaration(statement, inheritStrictMode, disallowArgumentsDeclaration))
                    throw new FastParseException(statement.Start, "Invalid declaration in direct eval code");
            }
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

        return disallowArgumentsDeclaration && name.Value.Equals("arguments");
    }
}
