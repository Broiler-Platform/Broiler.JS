#nullable enable

using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using AstDeclarator = Broiler.JavaScript.Ast.VariableDeclarator;

namespace Broiler.JavaScript.Parser.Bridge;

internal sealed partial class BridgeTreeBuilder
{
    // ---------------------------------------------------------------------------------------------------------------
    // Functions.

    private AstFunctionExpression FunctionStatement(FunctionNode function, bool ifClause)
        => Function(function, isStatement: true, ifClause);

    /// <summary>
    /// A function declaration's or expression's function, as the old parser's FunctionExpression built it: its name
    /// declared where it is a statement (Annex B's hoisting registered first), its parameters bound in the function's
    /// scope, its body a block of its own.
    /// </summary>
    private AstFunctionExpression Function(FunctionNode function, bool isStatement, bool ifClause)
    {
        AstIdentifier? id = null;
        if (function.Name is { } name)
        {
            id = Name(name);
            if (isStatement)
                scopes.DeclareFunction(scopes.NameSpan(name, name.Name), plain: !function.IsAsync && !function.IsGenerator, ifClause);
        }

        scopes.EnterFunction(isArrow: false);
        var parameters = Parameters(function.Parameters);
        var body = Body(function, function.Name is null ? function.Span.Offset : EndOf(function.Name));
        scopes.LeaveFunction();
        return new AstFunctionExpression(Start(function), End(function), false, function.IsAsync, function.IsGenerator, id!, parameters, body, isStatement);
    }

    /// <summary>
    /// A method's function: an object literal's or a class's method, getter or setter, whose name ends at
    /// <paramref name="keyEnd"/>.
    /// </summary>
    private AstFunctionExpression Method(FunctionNode function, int keyEnd)
    {
        scopes.EnterFunction(isArrow: false);
        var parameters = Parameters(function.Parameters);
        var body = Body(function, System.Math.Max(function.Span.Offset, keyEnd));
        scopes.LeaveFunction();
        return new AstFunctionExpression(Start(function), End(function), false, function.IsAsync, function.IsGenerator, null!, parameters, body);
    }

    /// <summary>
    /// An arrow function, as the old parser built it from its cover: its parameters the cover's conversion and never
    /// declared, its expression body a return statement over the expression.
    /// </summary>
    private AstFunctionExpression Arrow(FunctionNode function)
    {
        scopes.EnterFunction(isArrow: true);
        var parameters = new Sequence<AstDeclarator>(function.Parameters.Count);
        foreach (var parameter in function.Parameters)
        {
            parameters.Add(parameter switch
            {
                AssignmentPattern assignment => new AstDeclarator(CoverPattern(assignment.Target), Expression(assignment.Initializer)),
                _ => new AstDeclarator(CoverPattern(parameter)),
            });
        }

        // The old parser read a parenthesised head as a parenthesised expression, and its one parameter, where it is a
        // name, kept the mark.
        if (parameters.Count == 1 && function.Parameters[0] is BindingIdentifier && HeadIsParenthesized(function))
            parameters[0].Identifier.WasParenthesized = true;

        AstStatement body;
        if (function.ExpressionBody is { } expression)
        {
            var value = Expression(expression);
            body = new AstReturnStatement(value.Start, value.End, value);
        }
        else
        {
            body = Body(function, function.Span.Offset);
        }

        scopes.LeaveFunction();
        return new AstFunctionExpression(Start(function), End(function), true, function.IsAsync, false, null!, parameters, body);
    }

    private bool HeadIsParenthesized(FunctionNode arrow)
    {
        var at = tokens.SkipTrivia(arrow.Span.Offset);
        if (arrow.IsAsync && string.CompareOrdinal(text, at, "async", 0, 5) == 0)
            at = tokens.SkipTrivia(at + 5);
        return at < tokens.EndOffset && text[at] == '(';
    }

    // A function's body, a block that begins at its opening brace: the brace after the parameter list's `)` (and an
    // arrow's `=>`), the list found after the last parameter or, where there is none, at the first `(` at or after
    // `from`, an offset past anything before the list that can hold a parenthesis (a method's computed name).
    private AstBlock Body(FunctionNode function, int from)
    {
        scopes.EnterBody();
        var statements = StatementList(function.Body);
        var annexB = scopes.LeaveBody();
        return new AstBlock(BodyOpen(function, from), End(function), statements)
        {
            HoistingScope = scopes.Hoisting(function),
            AnnexBFunctionNames = annexB,
        };
    }

    private FastToken BodyOpen(FunctionNode function, int from)
    {
        var end = tokens.EndOffset;
        int at;
        if (function.Parameters.Count > 0)
        {
            at = tokens.SkipTrivia(EndOf(function.Parameters[function.Parameters.Count - 1]));
        }
        else
        {
            at = tokens.SkipTrivia(from);
            while (at < end && text[at] != '(')
                at = tokens.SkipTrivia(at + 1);
            if (at < end)
                at = tokens.SkipTrivia(at + 1);
        }

        if (at < end && text[at] == ',')
            at = tokens.SkipTrivia(at + 1);
        if (at < end && text[at] == ')')
            at = tokens.SkipTrivia(at + 1);
        if (at + 1 < end && text[at] == '=' && text[at + 1] == '>')
            at += 2;
        return OpenBrace(at);
    }

    // The FormalParameters, as the old parser's Parameters read them: each a binding pattern declared in the
    // function's scope, with its initializer where one is written.
    private Sequence<AstDeclarator> Parameters(IReadOnlyList<Pattern> parameters)
    {
        var list = new Sequence<AstDeclarator>(parameters.Count);
        foreach (var parameter in parameters)
        {
            list.Add(parameter is AssignmentPattern assignment
                ? new AstDeclarator(BindingPattern(assignment.Target, FastVariableKind.Var), Expression(assignment.Initializer))
                : new AstDeclarator(BindingPattern(parameter, FastVariableKind.Var)));
        }

        return list;
    }

    private AstIdentifier Name(BindingIdentifier name)
        => new(tokens.Name(name.Span.Offset, name.Span.Length, name.Name, name.IsEscaped));

    // ---------------------------------------------------------------------------------------------------------------
    // Patterns.

    /// <summary>A binding pattern, as the old parser's AssignmentLeftPattern read it, its names declared as <paramref name="kind"/>.</summary>
    private AstExpression BindingPattern(Pattern pattern, FastVariableKind kind)
    {
        switch (pattern)
        {
            case BindingIdentifier identifier:
            {
                var node = Name(identifier);
                scopes.Declare(identifier.Name, kind);
                return node;
            }

            case AssignmentPattern assignment:
            {
                var target = BindingPattern(assignment.Target, kind);
                return new AstBinaryExpression(target, TokenTypes.Assign, Expression(assignment.Initializer));
            }

            case RestElement rest:
            {
                var token = Start(rest);
                var argument = BindingPattern(rest.Argument, kind);
                return new AstSpreadElement(token, argument.End, argument);
            }

            case ArrayPattern array:
            {
                // The pattern begins at its `[`, where its text begins (the old parser began it at the token after
                // the bracket, a line terminator's where one follows); an elision is at its comma, as the old parser
                // had it.
                var begin = Start(array);
                var cursor = array.Span.Offset + 1;
                var list = new Sequence<AstExpression>(array.Elements.Count);
                foreach (var element in array.Elements)
                {
                    if (element is null)
                    {
                        var comma = tokens.After(cursor);
                        list.Add(new AstEmptyExpression(comma));
                        cursor = comma.Span.Offset + 1;
                        continue;
                    }

                    list.Add(BindingPattern(element, kind));
                    cursor = tokens.SkipTrivia(EndOf(element));
                    if (cursor < tokens.EndOffset && text[cursor] == ',')
                        cursor++;
                }

                return new AstArrayPattern(begin, End(array), list);
            }

            case ObjectPattern @object:
            {
                var begin = Start(@object);
                var list = new Sequence<ObjectProperty>(@object.Properties.Count);
                foreach (var property in @object.Properties)
                {
                    if (property is RestElement rest)
                    {
                        var spread = (AstSpreadElement)BindingPattern(rest, kind);
                        list.Add(new ObjectProperty(spread, spread, null!, computed: false));
                        continue;
                    }

                    var entry = (PatternProperty)property;
                    var (key, computed) = BindingKey(entry.Key);
                    var value = entry.Value is AssignmentPattern assignment ? assignment.Target : entry.Value;

                    AstExpression target;
                    if (entry.IsShorthand)
                    {
                        target = key;
                        if (value is BindingIdentifier shorthand)
                            scopes.Declare(shorthand.Name, kind);
                    }
                    else
                    {
                        target = BindingPattern(value, kind);
                    }

                    var initializer = entry.Value is AssignmentPattern withInitializer ? Expression(withInitializer.Initializer) : null;
                    list.Add(new ObjectProperty(key, target, initializer!, computed: computed));
                }

                return new AstObjectPattern(begin, End(@object), list);
            }

            default:
                throw Unsupported(pattern);
        }
    }

    // A binding property's name, as AssignmentLeftPattern's ReadObjectPattern read it: a name is the name's token, a
    // keyword literal (`true`, `null`, `in` ...) its string token, a literal a literal, a computed name its expression.
    private (AstExpression Key, bool Computed) BindingKey(SyntaxNode key)
    {
        switch (key)
        {
            case IdentifierName name:
            {
                var token = tokens.Name(name.Span.Offset, name.Span.Length, name.Name, name.IsEscaped);
                return (new AstIdentifier(token.Type == TokenTypes.Identifier ? token : BridgeTokens.AsString(token)), false);
            }

            case StringLiteral @string:
                return (String(@string), false);
            case NumericLiteral number:
                return (Number(number), false);
            case ComputedPropertyName computed:
                return (Expression(computed.Expression), true);
            default:
                throw Unsupported(key);
        }
    }

    /// <summary>
    /// An assignment pattern, a for-in or for-of head's target, or an arrow's parameter, as the old parser converted
    /// the cover it read (<c>ExpressionPatternExtensions.ToPattern</c>): the literal's tokens kept, an elision at the
    /// literal's start, an object entry's key and value as the literal held them; nothing declared.
    /// </summary>
    private AstExpression CoverPattern(Pattern pattern)
    {
        switch (pattern)
        {
            case TargetPattern target:
                return Expression(target.Target);
            case BindingIdentifier identifier:
                return Name(identifier);
            case AssignmentPattern assignment:
            {
                var target = CoverPattern(assignment.Target);
                return new AstBinaryExpression(target, TokenTypes.Assign, Expression(assignment.Initializer));
            }

            case RestElement rest:
            {
                var token = Start(rest);
                var argument = CoverPattern(rest.Argument);
                return new AstSpreadElement(token, argument.End, argument);
            }

            case ArrayPattern array:
            {
                var start = Start(array);
                var list = new Sequence<AstExpression>(array.Elements.Count);
                foreach (var element in array.Elements)
                    list.Add(element is null ? new AstEmptyExpression(start) : CoverPattern(element));
                return new AstArrayPattern(start, End(array), list);
            }

            case ObjectPattern @object:
            {
                var start = Start(@object);
                var list = new Sequence<ObjectProperty>(@object.Properties.Count);
                foreach (var property in @object.Properties)
                {
                    if (property is RestElement rest)
                    {
                        list.Add(new ObjectProperty(null!, CoverPattern(rest.Argument), null!, spread: true));
                        continue;
                    }

                    var entry = (PatternProperty)property;
                    var (key, computed, _) = Key(entry.Key);
                    var value = entry.Value is AssignmentPattern assignment ? assignment.Target : entry.Value;
                    var target = entry.IsShorthand ? key : CoverPattern(value);
                    var initializer = entry.Value is AssignmentPattern withInitializer ? Expression(withInitializer.Initializer) : null;
                    list.Add(new ObjectProperty(key, target, initializer!, computed: computed));
                }

                return new AstObjectPattern(start, End(@object), list);
            }

            default:
                throw Unsupported(pattern);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Classes.

    /// <summary>
    /// A class, as the old parser's ClassExpression built it: its elements <see cref="AstClassProperty"/>s, its name
    /// declared (as a lexical binding) once its body is read, where it is a declaration. Decorators leave no node, as
    /// the old parser read and dropped them.
    /// </summary>
    private AstClassExpression Class(ClassNode @class, bool isDeclaration)
    {
        var begin = @class.Decorators.Count > 0
            ? tokens.After(EndOf(@class.Decorators[@class.Decorators.Count - 1]))
            : Start(@class);
        var identifier = @class.Name is null ? null : Name(@class.Name);
        var heritage = @class.Heritage is null ? null : Expression(@class.Heritage);
        var members = new Sequence<AstClassProperty>(@class.Elements.Count);
        foreach (var element in @class.Elements)
            members.Add(Element(element));

        if (isDeclaration && @class.Name is { } name)
            scopes.Declare(name.Name, FastVariableKind.Let);
        return new AstClassExpression(begin, End(@class), identifier!, heritage!, members, isDeclaration);
    }

    private AstClassProperty Element(SyntaxNode element)
    {
        switch (element)
        {
            case MethodDefinition method:
            {
                // The name first, so that the element's first token, where the name begins it, is the name's own.
                var (key, computed, isPrivate) = Key(method.Key);
                var current = ElementStart(method, method.Decorators);
                var function = Method(method.Function, EndOf(method.Key));
                var isConstructor = !computed && !isPrivate && !method.IsStatic && method.MethodKind == MethodKind.Method
                    && method.Key is IdentifierName { Name: "constructor" } or StringLiteral { Value: "constructor" };
                var kind = method.MethodKind switch
                {
                    MethodKind.Get => AstPropertyKind.Get,
                    MethodKind.Set => AstPropertyKind.Set,
                    _ => isConstructor ? AstPropertyKind.Constructor : AstPropertyKind.Method,
                };
                return new AstClassProperty(current, function.End, kind, isPrivate, method.IsStatic, key, computed, function);
            }

            case FieldDefinition field:
            {
                var (key, computed, isPrivate) = Key(field.Key);
                var current = ElementStart(field, field.Decorators);
                if (field.Value is null)
                    return new AstClassProperty(current, tokens.EndingAt(EndOf(field.Key)), AstPropertyKind.Data, isPrivate, field.IsStatic, key, computed, null!,
                        isAutoAccessor: field.IsAccessor);

                // The element ends where its initializer's text ends, a closing parenthesis included.
                var value = Expression(field.Value);
                var valueEnd = End(field.Value);
                return field.IsAccessor
                    ? new AstClassProperty(current, valueEnd, AstPropertyKind.Data, isPrivate, field.IsStatic, key, computed, value, isAutoAccessor: true)
                    : new AstClassProperty(current, valueEnd, AstPropertyKind.Data, isPrivate, field.IsStatic, key, computed, value, usesAssign: true);
            }

            case StaticBlock block:
            {
                var current = Start(block);
                scopes.EnterStaticBlock();
                var statements = StatementList(block.Body);
                scopes.LeaveBlock();
                var end = End(block);
                var body = new AstBlock(OpenBrace(tokens.SkipTrivia(block.Span.Offset) + "static".Length), end, statements)
                {
                    HoistingScope = scopes.Hoisting(block),
                    AnnexBFunctionNames = Sequence<StringSpan>.Empty,
                };
                var function = new AstFunctionExpression(current, end, false, false, false, null!, Sequence<AstDeclarator>.Empty, body);
                return new AstClassProperty(current, end, AstPropertyKind.Init, isPrivate: false, isStatic: true, propertyName: null!, computed: false, init: function);
            }

            default:
                throw Unsupported(element);
        }
    }

    // A class element begins at its first token after its decorators, which the old parser read and dropped.
    private FastToken ElementStart(SyntaxNode element, IReadOnlyList<Decorator> decorators)
        => decorators.Count > 0 ? tokens.After(EndOf(decorators[decorators.Count - 1])) : Start(element);
}
