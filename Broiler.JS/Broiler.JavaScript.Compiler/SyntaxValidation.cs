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
            // It is an early SyntaxError for a LabelledStatement to use a label that is
            // already in the enclosing label set (`x: x: ;`, `a: { a: ; }`). The label
            // set propagates through blocks/loops/switch but resets at function
            // boundaries, exactly as breakLabels is maintained here.
            if (HasLabel(breakLabels, label))
                throw new FastParseException(labeledStatement.Start, $"Label '{label}' has already been declared");

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

            ValidateRestParameter(functionExpression.Params, functionExpression);

            var previous = IsStrictMode;
            var prevMethod = _inMethodProperty;
            var prevBody = _functionBodyBlock;
            IsStrictMode = functionStrict;
            _inMethodProperty = false;
            // The function body's own block is a var-scoped environment: top-level
            // FunctionDeclarations in it are var-declared, so duplicates are allowed
            // (even in strict mode). Record it so VisitBlock skips the lexical
            // duplicate-FunctionDeclaration check for it (but still checks nested
            // blocks, which are genuine lexical scopes).
            _functionBodyBlock = functionExpression.Body as AstBlock;
            try
            {
                return base.VisitFunctionExpression(functionExpression);
            }
            finally
            {
                IsStrictMode = previous;
                _inMethodProperty = prevMethod;
                _functionBodyBlock = prevBody;
            }
        }

        private AstBlock _functionBodyBlock;

        // A BindingRestElement (`...a`) must be the LAST formal parameter and may not
        // carry a default initializer: `function f(...a, b)`, `function f(...a, ...b)`
        // and `function f(...a = 1)` are early SyntaxErrors (FormalParameters /
        // FunctionRestParameter). Setters are validated separately (they reject any
        // rest parameter outright).
        private static void ValidateRestParameter(IFastEnumerable<VariableDeclarator> parameters, AstNode node)
        {
            if (parameters == null)
                return;

            var en = parameters.GetFastEnumerator();
            var seenRest = false;
            while (en.MoveNext(out var param))
            {
                if (seenRest)
                    throw new FastParseException(node.Start, "Rest parameter must be last formal parameter");

                if (param.Identifier is AstSpreadElement)
                {
                    seenRest = true;
                    if (param.Init != null)
                        throw new FastParseException(node.Start, "Rest parameter may not have a default initializer");
                }
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

        protected override AstNode VisitObjectLiteral(AstObjectLiteral objectLiteral)
        {
            // Annex B.3.1 / 13.2.5.1: an object literal may not contain more than one
            // `__proto__: value` data property obtained from a colon PropertyDefinition.
            // Shorthand (`{__proto__}`), methods, accessors and computed keys are
            // exempt — only the literal `__proto__ : AssignmentExpression` form counts.
            var seenProtoSetter = false;
            var members = objectLiteral.Properties.GetFastEnumerator();
            while (members.MoveNext(out var node))
            {
                // A CoverInitializedName (`{ id = expr }`) is only legal when the
                // object literal is reinterpreted as an assignment/binding pattern.
                // A genuine destructuring target is parsed as an ObjectPattern, so any
                // object *literal* that still carries `UsesAssign` is being used as a
                // value (e.g. `({a = 0})`, `[{a = 0}.x] = []`) — a SyntaxError.
                if (node is AstClassProperty { Kind: AstPropertyKind.Data, UsesAssign: true } coverInit)
                    throw new FastParseException(coverInit.Start, "Invalid shorthand property initializer in object literal");

                if (node is not AstClassProperty { Kind: AstPropertyKind.Data, UsesColon: true, Computed: false } property
                    || !IsProtoName(property.Key))
                {
                    continue;
                }

                if (seenProtoSetter)
                    throw new FastParseException(property.Start, "Duplicate __proto__ fields are not allowed in object literals");

                seenProtoSetter = true;
            }

            return base.VisitObjectLiteral(objectLiteral);
        }

        // The PropertyName `__proto__`, written either as an IdentifierName or a
        // StringLiteral (but not a computed key), designates the prototype setter.
        private static bool IsProtoName(AstExpression key)
            => key switch
            {
                AstIdentifier identifier => identifier.Name.Value == "__proto__",
                AstLiteral { TokenType: TokenTypes.String } literal => literal.StringValue == "__proto__",
                _ => false,
            };

        protected override AstNode VisitClassStatement(AstClassExpression classStatement)
        {
            // A class definition is always strict mode code — its name binding, heritage
            // expression, and every element (method/getter/setter/constructor body, field
            // initializer, computed key) are validated under strict mode regardless of the
            // surrounding context. (Object-literal concise methods, by contrast, are not.)
            var previousStrict = IsStrictMode;
            IsStrictMode = true;
            try
            {
                if (IsRestrictedName(classStatement.Identifier?.Name))
                    throw new FastParseException(classStatement.Start, "Invalid class name in strict mode");

                Visit(classStatement.Identifier);
                Visit(classStatement.Base);

                ValidateClassEarlyErrors(classStatement);

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
            finally
            {
                IsStrictMode = previousStrict;
            }
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
                if (unaryExpression.Operator == UnaryOperator.Increment || unaryExpression.Operator == UnaryOperator.Decrement)
                {
                    if (unaryExpression.Argument is AstIdentifier updateIdentifier
                        && IsRestrictedName(updateIdentifier.Name))
                    {
                        throw new FastParseException(updateIdentifier.Start, "Invalid left-hand side expression for update");
                    }

                    if (unaryExpression.Argument is AstCallExpression)
                        throw new FastParseException(unaryExpression.Argument.Start, "Invalid left-hand side expression for update");
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
                && binaryExpression.Operator < TokenTypes.EndAssignTokens)
            {
                if (binaryExpression.Left is AstIdentifier assignTarget
                    && IsRestrictedName(assignTarget.Name))
                {
                    throw new FastParseException(assignTarget.Start, "Assignment to eval or arguments is not allowed in strict mode");
                }

                // A destructuring assignment target — e.g. the shorthand
                // `({ implements } = 'foo')` or `[yield] = x` — also has AssignmentTarget
                // identifiers, which in strict mode may not be eval/arguments or a
                // strict-reserved word. The simple-target check above misses these because
                // the left-hand side is an Object/Array pattern, not a bare identifier.
                if (binaryExpression.Left is AstObjectPattern or AstArrayPattern
                    && ContainsRestrictedBinding(binaryExpression.Left))
                {
                    throw new FastParseException(binaryExpression.Left.Start, "Assignment to eval, arguments, or a reserved word is not allowed in strict mode");
                }
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

        protected override AstNode VisitBlock(AstBlock block)
        {
            // Annex B 3.3.4 (duplicate block-nested FunctionDeclarations of the same
            // name) is a sloppy-mode-only allowance: in strict mode they are a
            // duplicate lexical declaration. The parser permits them (the Function
            // kind), so reject them here, where the strictness is known. The function
            // body block itself is var-scoped (skipped via _functionBodyBlock).
            if (IsStrictMode && block != _functionBodyBlock)
            {
                HashSet<string> functionNames = null;
                CheckDuplicateFunctionDeclarations(block.Statements, ref functionNames);
            }

            return base.VisitBlock(block);
        }

        protected override AstNode VisitSwitchStatement(AstSwitchStatement switchStatement)
        {
            // All case clauses of a switch share one lexical block scope, so a
            // strict-mode duplicate FunctionDeclaration check spans every clause.
            if (IsStrictMode)
            {
                HashSet<string> functionNames = null;
                var cases = switchStatement.Cases.GetFastEnumerator();
                while (cases.MoveNext(out var @case))
                {
                    CheckDuplicateFunctionDeclarations(@case.Statements, ref functionNames);

                    // A labelled FunctionDeclaration is an early SyntaxError in strict mode
                    // in every context, including a switch CaseClause. The base visitor's
                    // VisitCase is a no-op (it does not descend into case bodies), so the
                    // VisitLabeledStatement check below never reaches a case-nested labelled
                    // function — apply it explicitly here. (test262 staging/sm/
                    // lexical-environment/block-scoped-functions-annex-b-label.)
                    var caseStatements = @case.Statements.GetFastEnumerator();
                    while (caseStatements.MoveNext(out var caseStatement))
                        ThrowIfLabeledFunctionInBody(caseStatement);
                }
            }

            return base.VisitSwitchStatement(switchStatement);
        }

        private static void CheckDuplicateFunctionDeclarations(
            IFastEnumerable<AstStatement> statements, ref HashSet<string> functionNames)
        {
            var e = statements.GetFastEnumerator();
            while (e.MoveNext(out var statement))
            {
                if (statement is not AstExpressionStatement
                    { Expression: AstFunctionExpression { IsStatement: true, Id: { } id } })
                    continue;

                functionNames ??= new HashSet<string>(StringComparer.Ordinal);
                if (!functionNames.Add(id.Name.Value))
                    throw new FastParseException(id.Start,
                        $"{id.Name} is already defined in current scope at {id.Start.Start}");
            }
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
            {
                ThrowIfFunctionDeclarationBody(forInStatement.Body);

                // Annex B 3.5 tolerates a for-in `var` head with an initializer
                // (`for (var x = init in obj)`) only in non-strict code. In strict
                // mode it is an early SyntaxError. (let/const initializers and
                // destructuring-pattern initializers are rejected by the parser in
                // every mode.)
                if (forInStatement.Init is AstVariableDeclaration { Kind: FastVariableKind.Var } declaration)
                {
                    var en = declaration.Declarators.GetFastEnumerator();
                    while (en.MoveNext(out var d))
                    {
                        if (d.Init != null)
                            throw new FastParseException(declaration.Start, "for-in loop variable declaration may not have an initializer in strict mode");
                    }
                }
            }
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
            // The parser wraps a single FunctionDeclaration `if` clause in a
            // synthetic block (Annex B.3.4); in strict mode that is still an early
            // error, so look through the wrapper as well as the bare statement.
            if (body is AstBlock { IsSyntheticFunctionStatementBlock: true } block)
            {
                var en = block.Statements.GetFastEnumerator();
                body = en.MoveNext(out var first) ? first : null;
            }

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

        // ClassBody early errors (ECMAScript 15.7.1):
        //   * at most one ClassElement may be the constructor;
        //   * PrivateBoundIdentifiers may not contain duplicate entries, unless a
        //     name is used exactly once for a getter and once for a setter and in no
        //     other element. A getter/setter pair must also share the same static
        //     placement (a static getter and an instance setter — or vice versa — do
        //     not form a valid accessor pair, matching V8/SpiderMonkey).
        private sealed class PrivateNameUsage
        {
            public bool HasGet;
            public bool HasSet;
            public bool HasOther;   // field, method, or any non-accessor element
            public bool GetStatic;
            public bool SetStatic;
        }

        private static void ValidateClassEarlyErrors(AstClassExpression classStatement)
        {
            var seenConstructor = false;
            Dictionary<string, PrivateNameUsage> privateNames = null;

            var members = classStatement.Members.GetFastEnumerator();
            while (members.MoveNext(out var member))
            {
                if (member.Kind == AstPropertyKind.Constructor)
                {
                    if (seenConstructor)
                        throw new FastParseException(member.Start, "A class may only have one constructor");

                    seenConstructor = true;
                }

                if (!member.Computed)
                {
                    var elementName = member.Key switch
                    {
                        AstIdentifier nameIdentifier => nameIdentifier.Name.Value,
                        AstLiteral { TokenType: TokenTypes.String } literal => literal.StringValue,
                        _ => null,
                    };

                    // A static class element named "prototype" (method/accessor/field) is an
                    // early error. Computed keys are exempt (checked at runtime).
                    if (member.IsStatic && elementName == "prototype")
                        throw new FastParseException(member.Start, "Classes may not have a static property named 'prototype'");

                    if (elementName == "constructor")
                    {
                        // A class field named "constructor" (static or not) is an early error.
                        if (member.Kind == AstPropertyKind.Data)
                            throw new FastParseException(member.Start, "Classes may not have a field named 'constructor'");

                        // The prototype constructor must be a plain method: a non-static
                        // generator/async method named "constructor" is an early error.
                        if (!member.IsStatic
                            && member.Init is AstFunctionExpression { Generator: true } or AstFunctionExpression { Async: true })
                            throw new FastParseException(member.Start, "Class constructor may not be a generator or async method");
                    }
                }

                if (!member.IsPrivate || member.Key is not AstIdentifier identifier)
                    continue;

                privateNames ??= new Dictionary<string, PrivateNameUsage>(StringComparer.Ordinal);
                if (!privateNames.TryGetValue(identifier.Name.Value, out var usage))
                {
                    usage = new PrivateNameUsage();
                    privateNames[identifier.Name.Value] = usage;
                }

                var duplicate = member.Kind switch
                {
                    // A getter may join only a single, same-placement setter.
                    AstPropertyKind.Get => usage.HasGet || usage.HasOther
                        || (usage.HasSet && usage.SetStatic != member.IsStatic),
                    AstPropertyKind.Set => usage.HasSet || usage.HasOther
                        || (usage.HasGet && usage.GetStatic != member.IsStatic),
                    // A field/method cannot share its name with anything else.
                    _ => usage.HasGet || usage.HasSet || usage.HasOther,
                };

                if (duplicate)
                    throw new FastParseException(member.Start, $"Duplicate private name #{identifier.Name.Value}");

                switch (member.Kind)
                {
                    case AstPropertyKind.Get:
                        usage.HasGet = true;
                        usage.GetStatic = member.IsStatic;
                        break;
                    case AstPropertyKind.Set:
                        usage.HasSet = true;
                        usage.SetStatic = member.IsStatic;
                        break;
                    default:
                        usage.HasOther = true;
                        break;
                }
            }
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
        {
            if (identifier == null)
                return false;

            foreach (var scope in privateNameScopes)
            {
                if (scope.Contains(identifier.Name.Value))
                    return true;
            }

            return false;
        }

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
