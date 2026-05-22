using System;
using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Compiler;

internal static class SyntaxValidation
{
    public static void ValidateProgram(AstProgram program, string sourceText, bool inheritStrictMode = false, IEnumerable<string> directEvalLexicalBindings = null)
    {
        if (program.IsAsync)
            throw new FastParseException(program.Start, "Unexpected await");

        var strictProgram = inheritStrictMode || HasUseStrictDirective(program.Statements);
        if (strictProgram && ContainsLegacyOctalLiteral(sourceText))
            throw new FastParseException(program.Start, "Unexpected legacy octal literal in strict mode");

        if (!strictProgram
            && directEvalLexicalBindings != null
            && HasDirectEvalLexicalConflict(program.Statements, directEvalLexicalBindings))
        {
            throw new FastParseException(program.Start, "Invalid declaration in direct eval code");
        }

        new StrictModeValidator(inheritStrictMode).Visit(program);
    }

    private static bool HasUseStrictDirective(IFastEnumerable<AstStatement> statements)
    {
        var enumerator = statements.GetFastEnumerator();
        while (enumerator.MoveNext(out var statement))
        {
            if (statement is not AstExpressionStatement { Expression: AstLiteral { TokenType: TokenTypes.String } literal })
                return false;

            if (literal.Start.CookedText == "use strict")
                return true;
        }

        return false;
    }

    private static bool ContainsLegacyOctalLiteral(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
            return false;

        var pool = new FastPool();
        var stream = new FastTokenStream(pool, sourceText);

        while (stream.Current.Type != TokenTypes.EOF)
        {
            var token = stream.Current;
            if (token.Type == TokenTypes.Number
                && TryGetLegacyOctalToken(token.Span.Value))
            {
                return true;
            }

            stream.Consume();
        }

        return false;
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

    private static bool HasDirectEvalLexicalConflict(IFastEnumerable<AstStatement> statements, IEnumerable<string> directEvalLexicalBindings)
    {
        var bindings = new HashSet<string>(directEvalLexicalBindings, StringComparer.Ordinal);
        if (bindings.Count == 0)
            return false;

        var enumerator = statements.GetFastEnumerator();
        while (enumerator.MoveNext(out var statement))
        {
            switch (statement)
            {
                case AstVariableDeclaration { Kind: FastVariableKind.Var } declaration:
                    if (ContainsBindingName(declaration.Declarators, bindings))
                        return true;
                    break;

                case AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true, Id: { } id } }:
                    if (bindings.Contains(id.Name.Value))
                        return true;
                    break;

                case AstExportStatement { Declaration: AstVariableDeclaration { Kind: FastVariableKind.Var } declaration }:
                    if (ContainsBindingName(declaration.Declarators, bindings))
                        return true;
                    break;

                case AstExportStatement { Declaration: AstFunctionExpression { Id: { } id } }:
                    if (bindings.Contains(id.Name.Value))
                        return true;
                    break;
            }
        }

        return false;
    }

    private sealed class StrictModeValidator : AstReduce
    {
        public StrictModeValidator(bool inheritStrictMode)
        {
            IsStrictMode = inheritStrictMode;
        }

        protected override AstNode VisitProgram(AstProgram program)
        {
            var previous = IsStrictMode;
            IsStrictMode = previous || HasUseStrictDirective(program.Statements);
            try
            {
                return base.VisitProgram(program);
            }
            finally
            {
                IsStrictMode = previous;
            }
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            if (IsStrictMode && IsRestrictedName(functionExpression.Id?.Name))
                throw new FastParseException(functionExpression.Start, "Invalid function name in strict mode");

            var bodyStatements = functionExpression.Body is AstBlock block ? block.Statements : Sequence<AstStatement>.Empty;
            var functionStrict = IsStrictMode || HasUseStrictDirective(bodyStatements);
            if (functionStrict && ContainsRestrictedBinding(functionExpression.Params))
                throw new FastParseException(functionExpression.Start, "Invalid parameter name in strict mode");

            var previous = IsStrictMode;
            IsStrictMode = functionStrict;
            try
            {
                return base.VisitFunctionExpression(functionExpression);
            }
            finally
            {
                IsStrictMode = previous;
            }
        }

        protected override AstNode VisitVariableDeclaration(AstVariableDeclaration variableDeclaration)
        {
            if (IsStrictMode && ContainsRestrictedBinding(variableDeclaration.Declarators))
                throw new FastParseException(variableDeclaration.Start, "Invalid declaration in strict mode");

            return base.VisitVariableDeclaration(variableDeclaration);
        }

        protected override AstNode VisitClassStatement(AstClassExpression classStatement)
        {
            if (IsStrictMode && IsRestrictedName(classStatement.Identifier?.Name))
                throw new FastParseException(classStatement.Start, "Invalid class name in strict mode");

            return base.VisitClassStatement(classStatement);
        }

        protected override AstNode VisitTryStatement(AstTryStatement tryStatement)
        {
            if (IsStrictMode && IsRestrictedName(tryStatement.Identifier?.Name))
                throw new FastParseException(tryStatement.Start, "Invalid catch parameter name in strict mode");

            return base.VisitTryStatement(tryStatement);
        }

        protected override AstNode VisitUnaryExpression(AstUnaryExpression unaryExpression)
        {
            if (IsStrictMode)
            {
                if ((unaryExpression.Operator == UnaryOperator.Increment || unaryExpression.Operator == UnaryOperator.Decrement)
                    && unaryExpression.Argument is AstIdentifier updateIdentifier
                    && IsRestrictedName(updateIdentifier.Name))
                {
                    throw new FastParseException(updateIdentifier.Start, "Invalid left-hand side expression for update");
                }

                if (unaryExpression.Operator == UnaryOperator.delete
                    && unaryExpression.Argument is AstIdentifier deleteIdentifier
                    && deleteIdentifier.Name != "this")
                {
                    throw new FastParseException(deleteIdentifier.Start, "Delete of an unqualified identifier in strict mode");
                }
            }

            return base.VisitUnaryExpression(unaryExpression);
        }

        protected override AstNode VisitWithStatement(AstWithStatement withStatement)
        {
            if (IsStrictMode)
                throw new FastParseException(withStatement.Start, "Strict mode code may not include a with statement");

            return base.VisitWithStatement(withStatement);
        }
    }

    private static bool ContainsRestrictedBinding(IFastEnumerable<VariableDeclarator> declarators)
    {
        var enumerator = declarators.GetFastEnumerator();
        while (enumerator.MoveNext(out var declarator))
        {
            if (ContainsRestrictedBinding(declarator.Identifier))
                return true;
        }

        return false;
    }

    private static bool ContainsRestrictedBinding(AstExpression expression)
    {
        return expression switch
        {
            AstIdentifier identifier => IsRestrictedName(identifier.Name),
            AstBinaryExpression assignment => ContainsRestrictedBinding(assignment.Left),
            AstSpreadElement spread => ContainsRestrictedBinding(spread.Argument),
            AstArrayPattern array => ContainsRestrictedBinding(array.Elements),
            AstObjectPattern @object => ContainsRestrictedBinding(@object.Properties),
            _ => false,
        };
    }

    private static bool ContainsRestrictedBinding(IFastEnumerable<AstExpression> expressions)
    {
        var enumerator = expressions.GetFastEnumerator();
        while (enumerator.MoveNext(out var expression))
        {
            if (ContainsRestrictedBinding(expression))
                return true;
        }

        return false;
    }

    private static bool ContainsRestrictedBinding(IFastEnumerable<ObjectProperty> properties)
    {
        var enumerator = properties.GetFastEnumerator();
        while (enumerator.MoveNext(out var property))
        {
            if (ContainsRestrictedBinding(property.Value))
                return true;
        }

        return false;
    }

    private static bool ContainsBindingName(IFastEnumerable<VariableDeclarator> declarators, HashSet<string> bindings)
    {
        var enumerator = declarators.GetFastEnumerator();
        while (enumerator.MoveNext(out var declarator))
        {
            if (ContainsBindingName(declarator.Identifier, bindings))
                return true;
        }

        return false;
    }

    private static bool ContainsBindingName(AstExpression expression, HashSet<string> bindings)
    {
        return expression switch
        {
            AstIdentifier identifier => bindings.Contains(identifier.Name.Value),
            AstBinaryExpression assignment => ContainsBindingName(assignment.Left, bindings),
            AstSpreadElement spread => ContainsBindingName(spread.Argument, bindings),
            AstArrayPattern array => ContainsBindingName(array.Elements, bindings),
            AstObjectPattern @object => ContainsBindingName(@object.Properties, bindings),
            _ => false,
        };
    }

    private static bool ContainsBindingName(IFastEnumerable<AstExpression> expressions, HashSet<string> bindings)
    {
        var enumerator = expressions.GetFastEnumerator();
        while (enumerator.MoveNext(out var expression))
        {
            if (ContainsBindingName(expression, bindings))
                return true;
        }

        return false;
    }

    private static bool ContainsBindingName(IFastEnumerable<ObjectProperty> properties, HashSet<string> bindings)
    {
        var enumerator = properties.GetFastEnumerator();
        while (enumerator.MoveNext(out var property))
        {
            if (ContainsBindingName(property.Value, bindings))
                return true;
        }

        return false;
    }

    private static bool IsRestrictedName(StringSpan? name)
    {
        if (name == null)
            return false;

        return name.Value.Equals("arguments") || name.Value.Equals("eval");
    }
}
