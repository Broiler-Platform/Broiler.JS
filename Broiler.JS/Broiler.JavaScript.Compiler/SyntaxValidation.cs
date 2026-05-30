using System;
using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

internal static class SyntaxValidation
{
public static void ValidateProgram(
    AstProgram program,
    string sourceText,
    bool inheritStrictMode = false,
    IEnumerable<string> directEvalLexicalBindings = null,
    IEnumerable<string> directEvalPrivateNames = null)
    {
        if (program.IsAsync && !CoreScript.AllowTopLevelAwait)
            throw new FastParseException(program.Start, "Unexpected await");

        var strictProgram = inheritStrictMode || HasUseStrictDirective(program.Statements);
        if (strictProgram && ContainsLegacyOctalLiteral(sourceText))
            throw new FastParseException(program.Start, "Unexpected legacy octal literal in strict mode");

        if (!strictProgram
            && directEvalLexicalBindings != null
            && ContainsDirectEvalVarConflict(program.Statements, directEvalLexicalBindings))
        {
            throw new FastParseException(program.Start, "Invalid declaration in direct eval code");
        }

        new ControlFlowValidator().Visit(program);
        new StrictModeValidator(inheritStrictMode, directEvalPrivateNames).Visit(program);
    }

    internal static bool IsUseStrictDirectiveLiteral(AstLiteral literal)
        => literal.TokenType == TokenTypes.String
            && (literal.Start.Span.Value == "\"use strict\"" || literal.Start.Span.Value == "'use strict'");

    private static bool HasUseStrictDirective(IFastEnumerable<AstStatement> statements)
    {
        var enumerator = statements.GetFastEnumerator();
        while (enumerator.MoveNext(out var statement))
        {
            if (statement is not AstExpressionStatement { Expression: AstLiteral { TokenType: TokenTypes.String } literal })
                return false;

            if (IsUseStrictDirectiveLiteral(literal))
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

            if (token.Type == TokenTypes.String
                && ContainsLegacyOctalEscapeInString(token.Span.Value))
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

    /// <summary>
    /// Checks whether the raw source text of a string literal token contains
    /// a legacy octal escape sequence such as <c>\1</c>, <c>\00</c> or <c>\010</c>.
    /// The bare <c>\0</c> (null escape) followed by a non-octal digit is allowed.
    /// </summary>
    internal static bool ContainsLegacyOctalEscapeInString(string rawSource)
    {
        if (string.IsNullOrEmpty(rawSource) || rawSource.Length < 3)
            return false;

        // Scan between the opening and closing quote characters.
        for (var i = 1; i < rawSource.Length - 1; i++)
        {
            if (rawSource[i] != '\\')
                continue;

            i++; // advance past backslash
            if (i >= rawSource.Length - 1)
                break;

            var ch = rawSource[i];

            // \1 through \7 are always legacy octal escapes
            if (ch >= '1' && ch <= '7')
                return true;

            // \8 and \9 are NonOctalDecimalEscapeSequence, forbidden in strict mode
            if (ch == '8' || ch == '9')
                return true;

            // \0 followed by a decimal digit (0-9) is a legacy octal escape:
            // \00..\07 are LegacyOctalEscapeSequence (ZeroToThree OctalDigit)
            // \08, \09 are LegacyOctalEscapeSequence (0 [lookahead ∈ {8, 9}])
            if (ch == '0'
                && i + 1 < rawSource.Length - 1
                && rawSource[i + 1] >= '0' && rawSource[i + 1] <= '9')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsDirectEvalVarConflict(IFastEnumerable<AstStatement> statements, IEnumerable<string> directEvalLexicalBindings)
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


    private sealed class ControlFlowValidator : AstReduce
    {
        private int loopDepth;
        private int switchDepth;
        private readonly Stack<HashSet<string>> breakLabels = new();
        private readonly Stack<HashSet<string>> continueLabels = new();

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            Visit(functionExpression.Id);
            var parameters = functionExpression.Params.GetFastEnumerator();
            while (parameters.MoveNext(out var parameter))
                VisitVariableDeclarator(parameter);

            var previousLoopDepth = loopDepth;
            var previousSwitchDepth = switchDepth;
            var previousBreakLabels = breakLabels.ToArray();
            var previousContinueLabels = continueLabels.ToArray();
            breakLabels.Clear();
            continueLabels.Clear();
            loopDepth = 0;
            switchDepth = 0;
            try
            {
                Visit(functionExpression.Body);
            }
            finally
            {
                loopDepth = previousLoopDepth;
                switchDepth = previousSwitchDepth;
                RestoreLabels(breakLabels, previousBreakLabels);
                RestoreLabels(continueLabels, previousContinueLabels);
            }

            return functionExpression;
        }

        protected override AstNode VisitBreakStatement(AstBreakStatement breakStatement)
        {
            var label = breakStatement.Label?.Name.Value;
            if (label != null)
            {
                if (!HasLabel(breakLabels, label))
                    throw new FastParseException(breakStatement.Start, $"No label found for {label}");
                return breakStatement;
            }

            if (loopDepth == 0 && switchDepth == 0)
                throw new FastParseException(breakStatement.Start, "Illegal break statement");

            return breakStatement;
        }

        protected override AstNode VisitContinueStatement(AstContinueStatement continueStatement)
        {
            var label = continueStatement.Label?.Name.Value;
            if (label != null)
            {
                if (!HasLabel(continueLabels, label))
                    throw new FastParseException(continueStatement.Start, $"No label found for {label}");
                return continueStatement;
            }

            if (loopDepth == 0)
                throw new FastParseException(continueStatement.Start, "Illegal continue statement");

            return continueStatement;
        }

        protected override AstNode VisitLabeledStatement(AstLabeledStatement labeledStatement)
        {
            var label = labeledStatement.Label.Span.Value;
            var canContinue = labeledStatement.Body.Type is FastNodeType.WhileStatement
                or FastNodeType.DoWhileStatement
                or FastNodeType.ForStatement
                or FastNodeType.ForInStatement
                or FastNodeType.ForOfStatement;

            PushLabel(breakLabels, label);
            if (canContinue)
                PushLabel(continueLabels, label);

            try
            {
                return base.VisitLabeledStatement(labeledStatement);
            }
            finally
            {
                if (canContinue)
                    continueLabels.Pop();
                breakLabels.Pop();
            }
        }

        protected override AstNode VisitWhileStatement(AstWhileStatement whileStatement, string label = null)
            => VisitLoop(() => base.VisitWhileStatement(whileStatement, label));

        protected override AstNode VisitDoWhileStatement(AstDoWhileStatement doWhileStatement, string label = null)
            => VisitLoop(() => base.VisitDoWhileStatement(doWhileStatement, label));

        protected override AstNode VisitForStatement(AstForStatement forStatement, string label = null)
            => VisitLoop(() => base.VisitForStatement(forStatement, label));

        protected override AstNode VisitForInStatement(AstForInStatement forInStatement, string label = null)
            => VisitLoop(() => base.VisitForInStatement(forInStatement, label));

        protected override AstNode VisitForOfStatement(AstForOfStatement forOfStatement, string label = null)
            => VisitLoop(() => base.VisitForOfStatement(forOfStatement, label));

        protected override AstNode VisitSwitchStatement(AstSwitchStatement switchStatement)
        {
            Visit(switchStatement.Target);
            switchDepth++;
            try
            {
                var cases = switchStatement.Cases.GetFastEnumerator();
                while (cases.MoveNext(out var @case))
                {
                    Visit(@case.Test);
                    var statements = @case.Statements.GetFastEnumerator();
                    while (statements.MoveNext(out var statement))
                        Visit(statement);
                }
            }
            finally
            {
                switchDepth--;
            }

            return switchStatement;
        }



        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstSuper)
            {
                var arguments = callExpression.Arguments.GetFastEnumerator();
                while (arguments.MoveNext(out var argument))
                    Visit(argument);

                return callExpression;
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitMemberExpression(AstMemberExpression memberExpression)
        {
            if (memberExpression.Object is AstSuper)
            {
                if (memberExpression.Computed)
                    Visit(memberExpression.Property);

                return memberExpression;
            }

            return base.VisitMemberExpression(memberExpression);
        }

        private AstNode VisitLoop(Func<AstNode> visit)
        {
            loopDepth++;
            try
            {
                return visit();
            }
            finally
            {
                loopDepth--;
            }
        }

        private static void PushLabel(Stack<HashSet<string>> labels, string label)
            => labels.Push(new HashSet<string>(StringComparer.Ordinal) { label });

        private static void RestoreLabels(Stack<HashSet<string>> labels, HashSet<string>[] snapshot)
        {
            labels.Clear();
            for (var i = snapshot.Length - 1; i >= 0; i--)
                labels.Push(snapshot[i]);
        }

        private static bool HasLabel(Stack<HashSet<string>> labels, string label)
        {
            foreach (var scope in labels)
            {
                if (scope.Contains(label))
                    return true;
            }

            return false;
        }
    }

    private sealed class StrictModeValidator : AstReduce
    {
        private readonly Stack<HashSet<string>> privateNameScopes = new();

        public StrictModeValidator(bool inheritStrictMode, IEnumerable<string> directEvalPrivateNames)
        {
            IsStrictMode = inheritStrictMode;
            if (directEvalPrivateNames != null)
                privateNameScopes.Push(new HashSet<string>(directEvalPrivateNames, StringComparer.Ordinal));
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
            if (property.IsPrivate && !HasPrivateName(property.Key as AstIdentifier))
                throw new FastParseException(property.Start, "Private name is not declared in an enclosing class");

            if (property.Kind is AstPropertyKind.Method or AstPropertyKind.Constructor
                or AstPropertyKind.Get or AstPropertyKind.Set)
            {
                if (property.IsStatic && IsEscapedKeyword(property.Start, "static"))
                    throw new FastParseException(property.Start, "Keyword must not contain escaped characters");

                if (property.Kind == AstPropertyKind.Get && IsEscapedKeyword(property.Start, "get"))
                    throw new FastParseException(property.Start, "Keyword must not contain escaped characters");

                if (property.Kind == AstPropertyKind.Set && IsEscapedKeyword(property.Start, "set"))
                    throw new FastParseException(property.Start, "Keyword must not contain escaped characters");

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
            var bodyStatements = functionExpression.Body is AstBlock block ? block.Statements : Sequence<AstStatement>.Empty;
            var functionStrict = IsStrictMode || HasUseStrictDirective(bodyStatements);
            if (functionStrict && IsRestrictedName(functionExpression.Id?.Name))
                throw new FastParseException(functionExpression.Start, "Invalid function name in strict mode");

            if (functionStrict && ContainsRestrictedBinding(functionExpression.Params))
                throw new FastParseException(functionExpression.Start, "Invalid parameter name in strict mode");

            if (functionExpression.Generator && ContainsYieldBinding(functionExpression.Params))
                throw new FastParseException(functionExpression.Start, "Invalid generator parameter name");

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

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            Visit(declarator.Init);
            return declarator;
        }

        protected override AstNode VisitClassStatement(AstClassExpression classStatement)
        {
            if (IsStrictMode && IsRestrictedName(classStatement.Identifier?.Name))
                throw new FastParseException(classStatement.Start, "Invalid class name in strict mode");

            Visit(classStatement.Identifier);
            Visit(classStatement.Base);

            privateNameScopes.Push(CollectPrivateNames(classStatement.Members));
            try
            {
                var members = classStatement.Members.GetFastEnumerator();
                while (members.MoveNext(out var member))
                    VisitClassProperty(member);
            }
            finally
            {
                privateNameScopes.Pop();
            }

            return classStatement;
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

        protected override AstNode VisitBinaryExpression(AstBinaryExpression binaryExpression)
        {
            if (binaryExpression.Operator == TokenTypes.Assign
                && ContainsInvalidParenthesizedPattern(binaryExpression.Left))
            {
                throw new FastParseException(binaryExpression.Left.Start, "Invalid parenthesized destructuring pattern");
            }

            if (IsStrictMode
                && binaryExpression.Operator > TokenTypes.BeginAssignTokens
                && binaryExpression.Operator < TokenTypes.EndAssignTokens
                && binaryExpression.Left is AstIdentifier assignTarget
                && IsRestrictedName(assignTarget.Name))
            {
                throw new FastParseException(assignTarget.Start, "Assignment to eval or arguments is not allowed in strict mode");
            }

            return base.VisitBinaryExpression(binaryExpression);
        }

        protected override AstNode VisitLiteral(AstLiteral literal)
        {
            if (IsStrictMode
                && literal.TokenType == TokenTypes.String
                && ContainsLegacyOctalEscapeInString(literal.Start.Span.Value))
            {
                throw new FastParseException(literal.Start, "Octal escape sequences are not allowed in strict mode");
            }

            return base.VisitLiteral(literal);
        }

        protected override AstNode VisitWithStatement(AstWithStatement withStatement)
        {
            if (IsStrictMode)
                throw new FastParseException(withStatement.Start, "Strict mode code may not include a with statement");

            return base.VisitWithStatement(withStatement);
        }

        protected override AstNode VisitIfStatement(AstIfStatement ifStatement)
        {
            if (IsStrictMode)
            {
                ThrowIfFunctionDeclarationBody(ifStatement.True);
                ThrowIfFunctionDeclarationBody(ifStatement.False);
            }
            else
            {
                ThrowIfLabeledFunctionInBody(ifStatement.True);
                ThrowIfLabeledFunctionInBody(ifStatement.False);
            }

            return base.VisitIfStatement(ifStatement);
        }

        protected override AstNode VisitWhileStatement(AstWhileStatement whileStatement, string label = null)
        {
            if (IsStrictMode)
                ThrowIfFunctionDeclarationBody(whileStatement.Body);
            else
                ThrowIfLabeledFunctionInBody(whileStatement.Body);

            return base.VisitWhileStatement(whileStatement, label);
        }

        protected override AstNode VisitDoWhileStatement(AstDoWhileStatement doWhileStatement, string label = null)
        {
            if (IsStrictMode)
                ThrowIfFunctionDeclarationBody(doWhileStatement.Body);
            else
                ThrowIfLabeledFunctionInBody(doWhileStatement.Body);

            return base.VisitDoWhileStatement(doWhileStatement, label);
        }

        protected override AstNode VisitForStatement(AstForStatement forStatement, string label = null)
        {
            if (IsStrictMode)
                ThrowIfFunctionDeclarationBody(forStatement.Body);
            else
                ThrowIfLabeledFunctionInBody(forStatement.Body);

            return base.VisitForStatement(forStatement, label);
        }

        protected override AstNode VisitForInStatement(AstForInStatement forInStatement, string label = null)
        {
            if (IsStrictMode)
                ThrowIfFunctionDeclarationBody(forInStatement.Body);
            else
                ThrowIfLabeledFunctionInBody(forInStatement.Body);

            return base.VisitForInStatement(forInStatement, label);
        }

        protected override AstNode VisitForOfStatement(AstForOfStatement forOfStatement, string label = null)
        {
            if (IsStrictMode)
                ThrowIfFunctionDeclarationBody(forOfStatement.Body);
            else
                ThrowIfLabeledFunctionInBody(forOfStatement.Body);

            return base.VisitForOfStatement(forOfStatement, label);
        }

        protected override AstNode VisitLabeledStatement(AstLabeledStatement labeledStatement)
        {
            if (IsStrictMode)
            {
                if (IsRestrictedName(GetTokenValue(labeledStatement.Label)))
                    throw new FastParseException(labeledStatement.Label, "Invalid label name in strict mode");

                ThrowIfFunctionDeclarationBody(labeledStatement.Body);
            }

            return base.VisitLabeledStatement(labeledStatement);
        }

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            if (IsPrivateName(identifier) && !HasPrivateName(identifier))
                throw new FastParseException(identifier.Start, "Private name is not declared in an enclosing class");

            return base.VisitIdentifier(identifier);
        }



        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstSuper)
            {
                var arguments = callExpression.Arguments.GetFastEnumerator();
                while (arguments.MoveNext(out var argument))
                    Visit(argument);

                return callExpression;
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitMemberExpression(AstMemberExpression memberExpression)
        {
            if (memberExpression.Object is AstSuper)
            {
                if (memberExpression.Computed)
                    Visit(memberExpression.Property);

                return memberExpression;
            }

            if (!memberExpression.Computed
                && memberExpression.Property is AstIdentifier identifier
                && IsPrivateName(identifier)
                && !HasPrivateName(identifier))
            {
                throw new FastParseException(identifier.Start, "Private name is not declared in an enclosing class");
            }

            return base.VisitMemberExpression(memberExpression);
        }

        private static void ThrowIfFunctionDeclarationBody(AstStatement body)
        {
            if (body is AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true } func })
                throw new FastParseException(func.Start, "In strict mode code, functions can only be declared at top level or inside a block");
        }

        private static void ThrowIfLabeledFunctionInBody(AstStatement body)
        {
            // Unwrap nested labels: label1: label2: ... function f() {} is invalid
            // inside control flow bodies (if/while/for/do), even in sloppy mode.
            // Bare function declarations without labels are allowed per Annex B.
            if (body is not AstLabeledStatement)
                return;

            var current = body;
            while (current is AstLabeledStatement labeled)
            {
                current = labeled.Body;
            }

            if (current is AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true } func })
                throw new FastParseException(func.Start, "In strict mode code, functions can only be declared at top level or inside a block");
        }

        private static HashSet<string> CollectPrivateNames(IFastEnumerable<AstClassProperty> members)
        {
            var privateNames = new HashSet<string>(StringComparer.Ordinal);
            var enumerator = members.GetFastEnumerator();
            while (enumerator.MoveNext(out var member))
            {
                if (member.IsPrivate && member.Key is AstIdentifier identifier)
                    privateNames.Add(identifier.Name.Value);
            }

            return privateNames;
        }

        private bool HasPrivateName(AstIdentifier identifier)
            => identifier != null
                && privateNameScopes.Count > 0
                && privateNameScopes.Peek().Contains(identifier.Name.Value);

        private static bool IsPrivateName(AstIdentifier identifier)
            => identifier != null && identifier.Name.Value.StartsWith("#", StringComparison.Ordinal);
    }

    private static bool ContainsYieldBinding(IFastEnumerable<VariableDeclarator> declarators)
    {
        var enumerator = declarators.GetFastEnumerator();
        while (enumerator.MoveNext(out var declarator))
        {
            if (ContainsYieldBinding(declarator.Identifier))
                return true;
        }

        return false;
    }

    private static bool ContainsYieldBinding(AstExpression expression)
    {
        return expression switch
        {
            AstIdentifier identifier => identifier.Name.Value == "yield",
            AstBinaryExpression assignment => ContainsYieldBinding(assignment.Left),
            AstSpreadElement spread => ContainsYieldBinding(spread.Argument),
            AstArrayPattern array => ContainsYieldBinding(array.Elements),
            AstObjectPattern @object => ContainsYieldBinding(@object.Properties),
            _ => false,
        };
    }

    private static bool ContainsYieldBinding(IFastEnumerable<AstExpression> expressions)
    {
        var enumerator = expressions.GetFastEnumerator();
        while (enumerator.MoveNext(out var expression))
        {
            if (ContainsYieldBinding(expression))
                return true;
        }

        return false;
    }

    private static bool ContainsYieldBinding(IFastEnumerable<ObjectProperty> properties)
    {
        var enumerator = properties.GetFastEnumerator();
        while (enumerator.MoveNext(out var property))
        {
            if (ContainsYieldBinding(property.Value))
                return true;
        }

        return false;
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

        var v = name.Value;
        return v.Equals("arguments") || v.Equals("eval")
            || v.Equals("let") || v.Equals("static")
            || v.Equals("yield")
            || v.Equals("implements") || v.Equals("interface")
            || v.Equals("package") || v.Equals("private")
            || v.Equals("protected") || v.Equals("public");
    }

    private static bool IsRestrictedName(string? name)
        => !string.IsNullOrEmpty(name) && IsRestrictedName(new StringSpan(name));

    private static string GetTokenValue(FastToken token)
        => token.CookedText ?? token.Span.Value;

    private static bool IsEscapedKeyword(FastToken token, string keyword)
        => token.CookedText == keyword && token.Span.Value != keyword;

    private static bool ContainsInvalidParenthesizedPattern(AstExpression expression, bool withinPattern = false)
    {
        return expression switch
        {
            AstArrayPattern arrayPattern => IsParenthesized(arrayPattern) || ContainsInvalidParenthesizedPattern(arrayPattern.Elements, true),
            AstObjectPattern objectPattern => IsParenthesized(objectPattern) || ContainsInvalidParenthesizedPattern(objectPattern.Properties, true),
            AstBinaryExpression { Operator: TokenTypes.Assign } assignment =>
                (withinPattern && IsParenthesized(assignment)) || ContainsInvalidParenthesizedPattern(assignment.Left, true),
            AstSpreadElement spread => ContainsInvalidParenthesizedPattern(spread.Argument, true),
            _ => false,
        };
    }

    private static bool ContainsInvalidParenthesizedPattern(IFastEnumerable<AstExpression> expressions, bool withinPattern)
    {
        var enumerator = expressions.GetFastEnumerator();
        while (enumerator.MoveNext(out var expression))
        {
            if (expression != null && ContainsInvalidParenthesizedPattern(expression, withinPattern))
                return true;
        }

        return false;
    }

    private static bool ContainsInvalidParenthesizedPattern(IFastEnumerable<ObjectProperty> properties, bool withinPattern)
    {
        var enumerator = properties.GetFastEnumerator();
        while (enumerator.MoveNext(out var property))
        {
            if (withinPattern
                && property.Init != null
                && property.Value.Start.Previous?.Type == TokenTypes.BracketStart
                && property.Value.End.Next?.Type == TokenTypes.Assign)
            {
                return true;
            }

            if (ContainsInvalidParenthesizedPattern(property.Value, withinPattern))
                return true;
        }

        return false;
    }

    private static bool IsParenthesized(AstNode node)
        => node.Start.Previous?.Type == TokenTypes.BracketStart
            && node.End.Next?.Type == TokenTypes.BracketEnd;

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
