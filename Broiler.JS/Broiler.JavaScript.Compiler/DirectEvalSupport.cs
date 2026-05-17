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

namespace Broiler.JavaScript.Compiler;

public static class DirectEvalSupport
{
    public static JSValue Execute(Arguments arguments, bool inheritStrictMode, bool disallowArgumentsDeclaration, JSVariable[] capturedBindings)
    {
        var value = arguments.Get1();
        if (!value.IsString)
            return value;

        var text = value.StringValue;
        string location = null;

        (JSEngine.Current as IJSExecutionContext)?.DispatchEvalEvent(ref text, ref location);

        if (inheritStrictMode)
            text = "\"use strict\";\n" + text;

        if (inheritStrictMode || disallowArgumentsDeclaration)
            Validate(text, inheritStrictMode, disallowArgumentsDeclaration);

        if (JSEngine.Current is JSContext context)
        {
            using var _ = context.PushDirectEvalScope(capturedBindings);
            return context.Eval(text, location, context);
        }

        return CoreScript.Evaluate(text, location);
    }

    private static void Validate(string text, bool inheritStrictMode, bool disallowArgumentsDeclaration)
    {
        if (inheritStrictMode && ContainsStrictReservedWordUsage(text))
            throw JSEngine.NewSyntaxError("Unexpected strict mode reserved word");

        try
        {
            var pool = new FastPool();
            var parser = new FastParser(new FastTokenStream(pool, text));
            var program = parser.ParseProgram();
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
                && previous?.Type is not TokenTypes.Dot and not TokenTypes.QuestionDot)
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
