using System;
using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static void ValidateFunctionEarlyErrors(AstFunctionExpression functionDeclaration, bool isStrictFunction)
    {
        if (!isStrictFunction)
            return;

        if (HasUseStrictDirective(functionDeclaration.Body) && !HasSimpleParameterList(functionDeclaration.Params))
            throw new FastParseException(functionDeclaration.Start, "Illegal 'use strict' directive in function with non-simple parameter list");

        if (ContainsLegacyOctalLiteral(functionDeclaration.Body))
            throw new FastParseException(functionDeclaration.Start, "Unexpected legacy octal literal in strict mode");

        var parameterNames = new List<StringSpan>();
        CollectParameterNames(functionDeclaration.Params, parameterNames);

        foreach (var parameterName in parameterNames)
        {
            if (parameterName.Equals("arguments") || parameterName.Equals("eval"))
                throw new FastParseException(functionDeclaration.Start, "Unexpected eval or arguments in strict mode");
        }
    }

    private static bool HasSimpleParameterList(IFastEnumerable<VariableDeclarator> parameters)
    {
        var enumerator = parameters.GetFastEnumerator();
        while (enumerator.MoveNext(out var parameter))
        {
            if (parameter.Init != null || parameter.Identifier.Type != FastNodeType.Identifier)
                return false;
        }

        return true;
    }

    private static void CollectParameterNames(IFastEnumerable<VariableDeclarator> parameters, List<StringSpan> names)
    {
        var enumerator = parameters.GetFastEnumerator();
        while (enumerator.MoveNext(out var parameter))
            CollectBindingNames(parameter.Identifier, names);
    }

    private static void CollectBindingNames(AstExpression expression, List<StringSpan> names)
    {
        switch (expression)
        {
            case AstIdentifier identifier:
                names.Add(identifier.Name);
                return;
            case AstBinaryExpression assignment:
                CollectBindingNames(assignment.Left, names);
                return;
            case AstSpreadElement spread:
                CollectBindingNames(spread.Argument, names);
                return;
            case AstArrayPattern arrayPattern:
            {
                var enumerator = arrayPattern.Elements.GetFastEnumerator();
                while (enumerator.MoveNext(out var element))
                    CollectBindingNames(element, names);
                return;
            }
            case AstObjectPattern objectPattern:
            {
                var enumerator = objectPattern.Properties.GetFastEnumerator();
                while (enumerator.MoveNext(out var property))
                    CollectBindingNames(property.Value, names);
                return;
            }
        }
    }

    private static bool ContainsLegacyOctalLiteral(AstStatement body)
    {
        var found = false;
        new LegacyOctalLiteralDetector(() => found = true).Visit(body);
        return found;
    }

    private sealed class LegacyOctalLiteralDetector(Action onFound) : AstReduce
    {
        private readonly Action onFound = onFound;

        protected override AstNode VisitLiteral(AstLiteral literal)
        {
            if (literal.TokenType == TokenTypes.Number
                && TryGetLegacyOctalToken(literal.Start.Span.Value))
            {
                onFound();
            }

            return literal;
        }
    }

    private static bool TryGetLegacyOctalToken(string tokenText)
    {
        if (string.IsNullOrEmpty(tokenText) || tokenText.Length < 2 || tokenText[0] != '0')
            return false;

        var second = tokenText[1];
        if (second is 'x' or 'X' or 'b' or 'B' or 'o' or 'O' or '.')
            return false;

        return second is >= '0' and <= '7';
    }
}
