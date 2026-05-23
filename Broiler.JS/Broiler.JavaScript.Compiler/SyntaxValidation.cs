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

        protected override AstNode VisitClassProperty(AstClassProperty property)
        {
            if (property.Kind is AstPropertyKind.Method or AstPropertyKind.Constructor
                or AstPropertyKind.Get or AstPropertyKind.Set)
            {
                // Validate getter/setter parameter counts per ECMAScript spec
                if (property.Init is AstFunctionExpression func)
                {
                    var paramCount = 0;
                    var hasRest = false;
                    var en = func.Params.GetFastEnumerator();
                    while (en.MoveNext(out var param))
                    {
                        paramCount++;
                        if (param.Identifier is AstSpreadElement)
                            hasRest = true;
                    }

                    if (property.Kind == AstPropertyKind.Get && paramCount > 0)
                        throw new FastParseException(property.Start, "Getter must not have any formal parameters");

                    if (property.Kind == AstPropertyKind.Set)
                    {
                        if (paramCount != 1)
                            throw new FastParseException(property.Start, "Setter must have exactly one formal parameter");
                        if (hasRest)
                            throw new FastParseException(property.Start, "Setter function argument must not be a rest parameter");
                    }
                }

                var prev = _inMethodProperty;
                _inMethodProperty = true;
                try
                {
                    return base.VisitClassProperty(property);
                }
                finally
                {
                    _inMethodProperty = prev;
                }
            }

            return base.VisitClassProperty(property);
        }

        private bool _inMethodProperty;

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            if (IsStrictMode && IsRestrictedName(functionExpression.Id?.Name))
                throw new FastParseException(functionExpression.Start, "Invalid function name in strict mode");

            var bodyStatements = functionExpression.Body is AstBlock block ? block.Statements : Sequence<AstStatement>.Empty;
            var functionStrict = IsStrictMode || HasUseStrictDirective(bodyStatements);
            if (functionStrict && ContainsRestrictedBinding(functionExpression.Params))
                throw new FastParseException(functionExpression.Start, "Invalid parameter name in strict mode");

            // Duplicate parameter names are always forbidden in:
            // - strict mode
            // - arrow functions
            // - generators
            // - async functions
            // - method definitions (concise methods, getters, setters, constructors)
            // - functions with non-simple parameters (rest, defaults, destructuring)
            var alwaysRejectDuplicates = functionExpression.IsArrowFunction
                || functionExpression.Generator
                || functionExpression.Async
                || _inMethodProperty
                || HasNonSimpleParameters(functionExpression.Params);

            if ((functionStrict || alwaysRejectDuplicates) && ContainsDuplicateParameterNames(functionExpression.Params))
                throw new FastParseException(functionExpression.Start, "Duplicate parameter name not allowed in this context");

            var previous = IsStrictMode;
            var prevMethod = _inMethodProperty;
            IsStrictMode = functionStrict;
            _inMethodProperty = false;
            try
            {
                return base.VisitFunctionExpression(functionExpression);
            }
            finally
            {
                IsStrictMode = previous;
                _inMethodProperty = prevMethod;
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
            if (IsStrictMode)
            {
                var catchParam = tryStatement.CatchParam;
                if (catchParam is AstIdentifier catchId && IsRestrictedName(catchId.Name))
                    throw new FastParseException(tryStatement.Start, "Invalid catch parameter name in strict mode");
                if (catchParam != null && ContainsRestrictedBinding(catchParam))
                    throw new FastParseException(tryStatement.Start, "Invalid catch parameter name in strict mode");
            }

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

    private static bool HasNonSimpleParameters(IFastEnumerable<VariableDeclarator> parameters)
    {
        var enumerator = parameters.GetFastEnumerator();
        while (enumerator.MoveNext(out var parameter))
        {
            if (parameter.Init != null)  // has default value
                return true;
            if (IsNonSimpleParameter(parameter.Identifier))
                return true;
        }
        return false;
    }

    private static bool IsNonSimpleParameter(AstExpression expression)
    {
        return expression switch
        {
            AstIdentifier => false,
            AstBinaryExpression => true,   // default value
            AstSpreadElement => true,       // rest parameter
            AstArrayPattern => true,        // array destructuring
            AstObjectPattern => true,       // object destructuring
            _ => false,
        };
    }

    private static bool ContainsDuplicateParameterNames(IFastEnumerable<VariableDeclarator> parameters)
    {
        var names = new List<StringSpan>();
        CollectBindingNames(parameters, names);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!seen.Add(name.Value))
                return true;
        }
        return false;
    }

    private static void CollectBindingNames(IFastEnumerable<VariableDeclarator> parameters, List<StringSpan> names)
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
}
