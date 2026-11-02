// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   32
// Annotated:        32/32
// Exempt:           0
// Human-reviewed:   0/32
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  2/10 max
// Unverified:       32
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>Functions, classes, binding patterns and the conversions of the cover grammars (ECMA-262 clauses 13.15.5, 14.3.3 and 15).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=130766
// Broiler-Human:        PENDING
internal sealed partial class Parser
{
    // ---------------------------------------------------------------------------------------------------------------
    // Functions.

    /// <summary>A function, generator, async function or async generator declaration (clauses 15.2, 15.5, 15.6 and 15.8); <paramref name="plainOnly"/> where only a FunctionDeclaration stands (a labelled item, Annex B.3.3's clause).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=4BFDE8
    // Broiler-Human:        PENDING
    private Statement ParseFunctionDeclaration(bool isAsync, bool isDefault, bool plainOnly = false)
    {
        var first = Ex;
        var start = StartOf(first);
        if (isAsync)
            Advance();
        ExpectWord(Word.Function, "function", LexicalGoal.RegExp);
        var star = Op;
        var isGenerator = Eat(TokenKind.Star);
        if (plainOnly && isGenerator)
            throw Unexpected(star);

        BindingIdentifier? name = null;
        if (CanBind(Op))
            name = ParseBindingIdentifier();
        else if (!isDefault)
            throw Missing(Op, "name");

        var function = ParseFunctionRest(start, name, isAsync, isGenerator);
        return new FunctionDeclaration(function.Span, function);
    }

    /// <summary>A function, generator, async function or async generator expression (clauses 15.2, 15.5, 15.6 and 15.8).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=A49475
    // Broiler-Human:        PENDING
    private Expression ParseFunctionExpression(bool isAsync)
    {
        var start = StartOf(Ex);
        if (isAsync)
            Advance();
        ExpectWord(Word.Function, "function", LexicalGoal.RegExp);
        var isGenerator = Eat(TokenKind.Star);
        BindingIdentifier? name = null;
        if (CanBind(Op))
            name = ParseBindingIdentifier();
        var function = ParseFunctionRest(start, name, isAsync, isGenerator);
        return new FunctionExpression(function.Span, function);
    }

    /// <summary>A function's FormalParameters and FunctionBody, under the function's own [Yield] and [Await] (clause 15).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=2FA3F1
    // Broiler-Human:        PENDING
    private FunctionNode ParseFunctionRest(SourceSpan start, BindingIdentifier? name, bool isAsync, bool isGenerator)
    {
        var outerYield = inYield;
        var outerAwait = inAwait;
        inYield = isGenerator;
        inAwait = isAsync;
        var awaitMark = awaitReferences.Count;
        try
        {
            var parameters = new List<Pattern>();
            Expect(TokenKind.LeftParen);
            var trailing = ParseFormalParameterList(parameters, TokenKind.RightParen);
            Expect(TokenKind.RightParen);
            var body = ParseFunctionBody();
            return new FunctionNode(SpanFrom(start), name, Lists.Freeze(parameters), trailing, body, null, isAsync, isGenerator, isArrow: false);
        }
        finally
        {
            inYield = outerYield;
            inAwait = outerAwait;
            DropAwaitReferences(awaitMark);
        }
    }

    /// <summary>A method's parameters and body (clause 15.4): a getter's none, a setter's one, and no rest element for either.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=BFE7D1
    // Broiler-Human:        PENDING
    private FunctionNode ParseMethod(SourceSpan start, MethodKind kind, bool isAsync, bool isGenerator)
    {
        var outerYield = inYield;
        var outerAwait = inAwait;
        inYield = isGenerator;
        inAwait = isAsync;
        var awaitMark = awaitReferences.Count;
        try
        {
            var parameters = new List<Pattern>();
            var trailing = false;
            Expect(TokenKind.LeftParen);
            if (kind == MethodKind.Set)
            {
                if (Op.Kind == TokenKind.Ellipsis)
                    throw Unexpected(cur);
                parameters.Add(ParseBindingElement());
            }
            else if (kind == MethodKind.Method)
            {
                trailing = ParseFormalParameterList(parameters, TokenKind.RightParen);
            }

            Expect(TokenKind.RightParen);
            var body = ParseFunctionBody();
            return new FunctionNode(SpanFrom(start), null, Lists.Freeze(parameters), trailing, body, null, isAsync, isGenerator, isArrow: false);
        }
        finally
        {
            inYield = outerYield;
            inAwait = outerAwait;
            DropAwaitReferences(awaitMark);
        }
    }

    /// <summary>
    /// A FormalParameterList (clause 15.1) up to <paramref name="end"/>: binding elements, a rest element last, a trailing comma
    /// where none follows a rest element. Answers whether a trailing comma closes it.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=38694E
    // Broiler-Human:        PENDING
    private bool ParseFormalParameterList(List<Pattern> into, TokenKind end)
    {
        while (Ex.Kind != end)
        {
            var token = cur;
            if (token.Kind == TokenKind.Ellipsis)
            {
                Advance();
                var target = ParseBindingTarget();
                into.Add(new RestElement(SpanFrom(StartOf(token)), target));

                // A rest parameter is the last: what follows it is a token no production admits there (EX0008).
                if (Op.Kind != end)
                    throw Unexpected(cur);
                return false;
            }

            into.Add(ParseBindingElement());
            if (Op.Kind == end)
                return false;
            Expect(TokenKind.Comma);
            if (Ex.Kind == end)
                return true;
        }

        return false;
    }

    /// <summary>A FunctionBody between braces (clause 15.2): [Return] on, its directive prologue first, its strictness its own.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=671DF8
    // Broiler-Human:        PENDING
    private IReadOnlyList<Statement> ParseFunctionBody()
    {
        var outerReturn = inReturn;
        var outerStrict = strict;
        var outerStaticBlock = inStaticBlock;
        functionDepth++;
        inReturn = true;
        inStaticBlock = false;
        try
        {
            Expect(TokenKind.LeftBrace);
            var body = new List<Statement>();
            ParsePrologue(body, TokenKind.RightBrace);
            while (Ex.Kind != TokenKind.RightBrace)
            {
                if (cur.Kind == TokenKind.EndOfInput)
                    throw Missing(cur, "}");
                body.Add(ParseStatementListItem());
            }

            Advance();
            return Lists.Freeze(body);
        }
        finally
        {
            functionDepth--;
            inReturn = outerReturn;
            inStaticBlock = outerStaticBlock;
            strict = outerStrict;
        }
    }

    /// <summary>
    /// An arrow function's <c>=&gt;</c> and ConciseBody (clauses 15.3 and 15.9), its parameters already converted: an
    /// expression body under [~Yield] and the arrow's own [Await], or a FunctionBody.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=19D7C2
    // Broiler-Human:        PENDING
    private Expression ParseArrowFunction(SourceSpan start, IReadOnlyList<Pattern> parameters, bool trailing, bool isAsync, int mark, bool noIn = false)
    {
        CheckCoverInitialized(mark);
        Expect(TokenKind.Arrow);
        var outerYield = inYield;
        var outerAwait = inAwait;
        inYield = false;
        inAwait = isAsync;
        var awaitMark = awaitReferences.Count;
        try
        {
            IReadOnlyList<Statement> body;
            Expression? expressionBody = null;
            if (Ex.Kind == TokenKind.LeftBrace)
            {
                body = ParseFunctionBody();
            }
            else
            {
                functionDepth++;
                try
                {
                    body = System.Array.Empty<Statement>();
                    expressionBody = ParseAssignment(noIn);
                }
                finally
                {
                    functionDepth--;
                }
            }

            var function = new FunctionNode(SpanFrom(start), null, Lists.Freeze(new List<Pattern>(parameters)), trailing, body, expressionBody, isAsync, isGenerator: false, isArrow: true);
            return new ArrowFunctionExpression(function.Span, function);
        }
        finally
        {
            inYield = outerYield;
            inAwait = outerAwait;
            DropAwaitReferences(awaitMark);
        }
    }

    /// <summary>
    /// An arrow function from the cover an AssignmentExpression read (clauses 13.2.10 and 15.9): the parenthesised list's or
    /// the async call's elements converted into ArrowFormalParameters, refused where the cover does not cover them
    /// (catalogue rows EE0087 and EE0155 at stage 4d).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=B842AB
    // Broiler-Human:        PENDING
    private Expression ParseArrowFromCover(SourceSpan start, Expression cover, int mark, bool noIn)
    {
        var parameters = new List<Pattern>();
        bool trailing;
        bool isAsync;
        if (cover is ArrowParametersCover list)
        {
            foreach (var item in list.Items)
                parameters.Add(Converting(Rows.EE0087, () => ToBindingElement(item)));
            if (list.Rest is not null)
                parameters.Add(list.Rest);
            trailing = list.TrailingComma;
            isAsync = false;
            mark = list.Mark;
        }
        else
        {
            var head = (AsyncArrowHead)cover;
            var arguments = head.Call.Arguments;
            var bound = new List<Identifier>();
            awaitBound = bound;
            try
            {
                for (var i = 0; i < arguments.Count; i++)
                {
                    if (arguments[i] is SpreadElement spread)
                    {
                        if (i != arguments.Count - 1 || head.Call.TrailingComma)
                            throw Refuse(Rows.EE0155, spread.Span, string.Empty);
                        parameters.Add(new RestElement(spread.Span, Converting(Rows.EE0155, () => ToBindingTarget(spread.Argument))));
                    }
                    else
                    {
                        var argument = arguments[i];
                        parameters.Add(Converting(Rows.EE0155, () => ToBindingElement(argument)));
                    }
                }
            }
            finally
            {
                awaitBound = null;
            }

            // The head is ArrowFormalParameters[~Yield, +Await]: an `await` the cover read as an IdentifierReference that
            // its parameters do not bind (an initialiser's, a computed key's, a nested cover's) can only begin an
            // AwaitExpression there, so the cover covers no AsyncArrowHead (catalogue row EE0155 at stage 4d).
            for (var i = head.AwaitMark; i < awaitReferences.Count; i++)
            {
                var reference = awaitReferences[i];
                if (!bound.Exists(b => ReferenceEquals(b, reference)))
                    throw Refuse(Rows.EE0155, reference.Span, "await");
            }

            trailing = head.Call.TrailingComma;
            isAsync = true;
            mark = head.Mark;
        }

        return ParseArrowFunction(start, parameters, trailing, isAsync, mark, noIn);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Binding patterns (clause 14.3.3).

    /// <summary>A BindingElement: a target with its Initializer where <c>=</c> follows.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=231E31
    // Broiler-Human:        PENDING
    private Pattern ParseBindingElement()
    {
        var start = StartOf(Op);
        var target = ParseBindingTarget();
        if (Op.Kind != TokenKind.Equals)
            return target;
        Advance();
        var initializer = ParseAssignment(noIn: false);
        return new AssignmentPattern(SpanFrom(start), target, initializer);
    }

    /// <summary>A BindingIdentifier or a BindingPattern.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=3F66AA
    // Broiler-Human:        PENDING
    private Pattern ParseBindingTarget()
    {
        var token = Op;
        return token.Kind is TokenKind.LeftBracket or TokenKind.LeftBrace ? ParseBindingPattern() : ParseBindingIdentifier();
    }

    /// <summary>A BindingIdentifier (clause 13.1).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=6EBE42
    // Broiler-Human:        PENDING
    private BindingIdentifier ParseBindingIdentifier()
    {
        var token = Op;
        if (!CanBind(token))
            throw Unexpected(token);
        Advance();
        return new BindingIdentifier(SpanOf(token), token.Value!, token.IsEscaped);
    }

    /// <summary>An ArrayBindingPattern or ObjectBindingPattern, charged one level of nesting as Broiler.VM's parser charges it.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=8C8C6D
    // Broiler-Human:        PENDING
    private Pattern ParseBindingPattern()
    {
        var open = Op;
        EnterNesting(open);
        try
        {
            var start = StartOf(Advance());
            if (open.Kind == TokenKind.LeftBracket)
            {
                var elements = new List<Pattern?>();
                var trailing = false;
                while (true)
                {
                    var token = Op;
                    if (token.Kind == TokenKind.RightBracket)
                        break;
                    if (token.Kind == TokenKind.Comma)
                    {
                        Advance();
                        elements.Add(null);
                        continue;
                    }

                    if (token.Kind == TokenKind.Ellipsis)
                    {
                        Advance();
                        var target = ParseBindingTarget();
                        elements.Add(new RestElement(SpanFrom(StartOf(token)), target));
                        if (Op.Kind != TokenKind.RightBracket)
                            throw Unexpected(cur); // a rest element is the last (EX0008)
                        break;
                    }

                    elements.Add(ParseBindingElement());
                    if (Op.Kind == TokenKind.RightBracket)
                        break;
                    Expect(TokenKind.Comma);
                    if (Op.Kind == TokenKind.RightBracket)
                        trailing = true;
                }

                Advance();
                return new ArrayPattern(SpanFrom(start), Lists.Freeze(elements), trailing);
            }

            var properties = new List<SyntaxNode>();
            var trailingComma = false;
            while (true)
            {
                var token = Op;
                if (token.Kind == TokenKind.RightBrace)
                    break;
                var propertyStart = StartOf(token);
                if (token.Kind == TokenKind.Ellipsis)
                {
                    Advance();
                    var rest = ParseBindingIdentifier();
                    properties.Add(new RestElement(SpanFrom(propertyStart), rest));
                    if (Op.Kind != TokenKind.RightBrace)
                        throw Unexpected(cur); // a rest property is the last (EX0008)
                    break;
                }

                if (token.Kind == TokenKind.IdentifierName && PeekAfter(LexicalGoal.Div, LexicalGoal.Div).Kind != TokenKind.Colon)
                {
                    // A SingleNameBinding: the name is the key and the target.
                    var name = ParseBindingIdentifier();
                    Pattern value = name;
                    if (Op.Kind == TokenKind.Equals)
                    {
                        Advance();
                        var initializer = ParseAssignment(noIn: false);
                        value = new AssignmentPattern(SpanFrom(propertyStart), name, initializer);
                    }

                    var key = new IdentifierName(name.Span, name.Name, name.IsEscaped);
                    properties.Add(new PatternProperty(SpanFrom(propertyStart), key, value, isShorthand: true));
                }
                else
                {
                    var key = ParsePropertyName(allowPrivate: false);
                    Expect(TokenKind.Colon);
                    var value = ParseBindingElement();
                    properties.Add(new PatternProperty(SpanFrom(propertyStart), key, value, isShorthand: false));
                }

                if (Op.Kind == TokenKind.RightBrace)
                    break;
                Expect(TokenKind.Comma);
                if (Op.Kind == TokenKind.RightBrace)
                    trailingComma = true;
            }

            Advance();
            return new ObjectPattern(SpanFrom(start), Lists.Freeze(properties), trailingComma);
        }
        finally
        {
            ExitNesting();
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Conversions of the covers (clauses 13.2.10, 13.15.5 and 15.3): what was read is converted, never read again.

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=2E38E7
    // Broiler-Human:        PENDING
    private BindingIdentifier ToBindingIdentifier(Identifier identifier)
    {
        awaitBound?.Add(identifier);
        return new BindingIdentifier(identifier.Span, identifier.Name, identifier.IsEscaped);
    }

    /// <summary>A cover's element as a BindingElement of ArrowFormalParameters.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=2E1091
    // Broiler-Human:        PENDING
    private Pattern ToBindingElement(Expression expression)
    {
        if (expression is AssignmentExpression { Operator: AssignmentOperator.Assign } assignment)
            return new AssignmentPattern(assignment.Span, ToBindingTarget(assignment.Target), assignment.Value);
        return ToBindingTarget(expression);
    }

    /// <summary>A cover's target as a BindingIdentifier or BindingPattern: a name, a literal, or an assignment pattern the parse already converted.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15.3 ArrowFormalParameters by conversion; IP=Low; Security=High; Resources=2; Fingerprint=C25A33
    // Broiler-Falsified-If: a cover element that covers no binding is converted, or one that does is refused
    // Broiler-Human:        PENDING
    private Pattern ToBindingTarget(SyntaxNode node)
    {
        switch (node)
        {
            case Identifier identifier:
                return ToBindingIdentifier(identifier);
            case ArrayLiteral array:
                return ToArrayBindingPattern(array);
            case ObjectLiteral @object:
                coverInitialized.Remove(@object);
                return ToObjectBindingPattern(@object);
            case ArrayPattern or ObjectPattern:
                return ToBindingFromAssignmentPattern((Pattern)node);
            default:
                throw UnexpectedAt(node.Span);
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=BF0CE3
    // Broiler-Human:        PENDING
    private ArrayPattern ToArrayBindingPattern(ArrayLiteral array)
    {
        var elements = new List<Pattern?>();
        for (var i = 0; i < array.Elements.Count; i++)
        {
            var element = array.Elements[i];
            if (element is null)
            {
                elements.Add(null);
            }
            else if (element is SpreadElement spread)
            {
                if (i != array.Elements.Count - 1 || array.TrailingComma)
                    throw UnexpectedAt(spread.Span);
                elements.Add(new RestElement(spread.Span, ToBindingTarget(spread.Argument)));
            }
            else
            {
                elements.Add(ToBindingElement(element));
            }
        }

        return new ArrayPattern(array.Span, Lists.Freeze(elements), array.TrailingComma);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=C49046
    // Broiler-Human:        PENDING
    private ObjectPattern ToObjectBindingPattern(ObjectLiteral @object)
    {
        var properties = new List<SyntaxNode>();
        for (var i = 0; i < @object.Properties.Count; i++)
        {
            var property = @object.Properties[i];
            if (property is SpreadElement spread)
            {
                if (i != @object.Properties.Count - 1 || @object.TrailingComma || spread.Argument is not Identifier name)
                    throw UnexpectedAt(spread.Span);
                properties.Add(new RestElement(spread.Span, ToBindingIdentifier(name)));
                continue;
            }

            var definition = (PropertyDefinition)property;
            switch (definition.PropertyKind)
            {
                case PropertyKind.Init:
                    properties.Add(new PatternProperty(definition.Span, definition.Key, ToBindingElement(definition.Value), isShorthand: false));
                    break;
                case PropertyKind.Shorthand:
                    properties.Add(new PatternProperty(definition.Span, definition.Key, ToBindingElement(definition.Value), isShorthand: true));
                    break;
                default:
                    throw UnexpectedAt(definition.Span);
            }
        }

        return new ObjectPattern(@object.Span, Lists.Freeze(properties), @object.TrailingComma);
    }

    /// <summary>An assignment pattern the parse converted, read again as a binding pattern: its targets must be names.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=09E1F2
    // Broiler-Human:        PENDING
    private Pattern ToBindingFromAssignmentPattern(Pattern pattern)
    {
        switch (pattern)
        {
            case TargetPattern { Target: Identifier identifier }:
                return ToBindingIdentifier(identifier);
            case AssignmentPattern assignment:
                return new AssignmentPattern(assignment.Span, ToBindingFromAssignmentPattern(assignment.Target), assignment.Initializer);
            case RestElement rest:
                return new RestElement(rest.Span, ToBindingFromAssignmentPattern(rest.Argument));
            case ArrayPattern array:
            {
                var elements = new List<Pattern?>();
                foreach (var element in array.Elements)
                    elements.Add(element is null ? null : ToBindingFromAssignmentPattern(element));
                return new ArrayPattern(array.Span, Lists.Freeze(elements), array.TrailingComma);
            }

            case ObjectPattern @object:
            {
                var properties = new List<SyntaxNode>();
                foreach (var property in @object.Properties)
                {
                    if (property is RestElement { Argument: TargetPattern { Target: Identifier name } } rest)
                        properties.Add(new RestElement(rest.Span, ToBindingIdentifier(name)));
                    else if (property is PatternProperty entry)
                        properties.Add(new PatternProperty(entry.Span, entry.Key, ToBindingFromAssignmentPattern(entry.Value), entry.IsShorthand));
                    else
                        throw UnexpectedAt(property.Span);
                }

                return new ObjectPattern(@object.Span, Lists.Freeze(properties), @object.TrailingComma);
            }

            default:
                throw UnexpectedAt(pattern.Span);
        }
    }

    /// <summary>
    /// An assignment's or a for head's target (clauses 13.15.5 and 14.7.5): an array or object literal converted to an
    /// AssignmentPattern, or a LeftHandSideExpression as written.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=392499
    // Broiler-Human:        PENDING
    private SyntaxNode ToAssignmentTarget(Expression expression, bool forHead)
    {
        if (expression is ArrayLiteral or ObjectLiteral)
            return ToAssignmentPattern(expression);
        if (IsLeftHandSide(expression))
            return expression;
        throw UnexpectedAt(expression.Span);
    }

    /// <summary>An array or object literal converted to an ArrayAssignmentPattern or ObjectAssignmentPattern (clause 13.15.5), its CoverInitializedNames taken.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 AssignmentPattern by conversion; IP=Low; Security=High; Resources=2; Fingerprint=BCA81B
    // Broiler-Falsified-If: a literal that covers no AssignmentPattern is converted, or one that does is refused
    // Broiler-Human:        PENDING
    private Pattern ToAssignmentPattern(Expression literal)
    {
        if (literal is ArrayLiteral array)
        {
            var elements = new List<Pattern?>();
            for (var i = 0; i < array.Elements.Count; i++)
            {
                var element = array.Elements[i];
                if (element is null)
                {
                    elements.Add(null);
                }
                else if (element is SpreadElement spread)
                {
                    if (i != array.Elements.Count - 1 || array.TrailingComma || spread.Argument is AssignmentExpression)
                        throw UnexpectedAt(spread.Span);
                    elements.Add(new RestElement(spread.Span, ToTargetPattern(spread.Argument)));
                }
                else
                {
                    elements.Add(ToAssignmentElement(element));
                }
            }

            return new ArrayPattern(array.Span, Lists.Freeze(elements), array.TrailingComma);
        }

        var @object = (ObjectLiteral)literal;
        coverInitialized.Remove(@object);
        var properties = new List<SyntaxNode>();
        for (var i = 0; i < @object.Properties.Count; i++)
        {
            var property = @object.Properties[i];
            if (property is SpreadElement spread)
            {
                // AssignmentRestProperty: its target is no pattern (catalogue row EE0037).
                if (spread.Argument is ArrayLiteral or ObjectLiteral && i == @object.Properties.Count - 1 && !@object.TrailingComma)
                    throw Refuse(Rows.EE0037, spread.Argument.Span, string.Empty);
                if (i != @object.Properties.Count - 1 || @object.TrailingComma || spread.Argument is ArrayLiteral or ObjectLiteral || !IsLeftHandSide(spread.Argument))
                    throw UnexpectedAt(spread.Span);
                properties.Add(new RestElement(spread.Span, new TargetPattern(spread.Argument.Span, spread.Argument)));
                continue;
            }

            var definition = (PropertyDefinition)property;
            switch (definition.PropertyKind)
            {
                case PropertyKind.Init:
                case PropertyKind.Shorthand:
                    properties.Add(new PatternProperty(definition.Span, definition.Key, ToAssignmentElement(definition.Value), definition.PropertyKind == PropertyKind.Shorthand));
                    break;
                default:
                    throw UnexpectedAt(definition.Span);
            }
        }

        return new ObjectPattern(@object.Span, Lists.Freeze(properties), @object.TrailingComma);
    }

    /// <summary>An AssignmentElement: a DestructuringAssignmentTarget with its Initializer where one is written.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=BD3C1B
    // Broiler-Human:        PENDING
    private Pattern ToAssignmentElement(Expression expression)
    {
        if (expression is AssignmentExpression assignment)
        {
            if (assignment.Operator != AssignmentOperator.Assign)
                throw UnexpectedAt(assignment.Span);
            var target = assignment.Target is Pattern pattern ? pattern : ToTargetPattern((Expression)assignment.Target);
            return new AssignmentPattern(assignment.Span, target, assignment.Value);
        }

        return ToTargetPattern(expression);
    }

    /// <summary>A DestructuringAssignmentTarget: a nested literal converted, or a LeftHandSideExpression (whose validity is stage 4d's).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=02FD5C
    // Broiler-Human:        PENDING
    private Pattern ToTargetPattern(Expression expression)
    {
        // A DestructuringAssignmentTarget that is a literal must cover an AssignmentPattern (catalogue row EE0038).
        if (expression is ArrayLiteral or ObjectLiteral)
            return Converting(Rows.EE0038, () => ToAssignmentPattern(expression));
        if (IsLeftHandSide(expression))
            return new TargetPattern(expression.Span, expression);
        throw UnexpectedAt(expression.Span);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Classes (clause 15.7): all of a class is strict code.

    /// <summary>A ClassDeclaration (clause 15.7), its name optional only after <c>export default</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=B7F477
    // Broiler-Human:        PENDING
    private Statement ParseClassDeclaration(IReadOnlyList<Decorator> decorators, bool isDefault)
    {
        var start = decorators.Count > 0 ? decorators[0].Span : StartOf(Ex);
        ExpectWord(Word.Class, "class", LexicalGoal.RegExp);
        var outerStrict = strict;
        strict = true;
        try
        {
            BindingIdentifier? name = null;
            if (CanBind(Op))
                name = ParseBindingIdentifier();
            else if (!isDefault)
                throw Missing(Op, "name");
            var @class = ParseClassTail(start, decorators, name);
            return new ClassDeclaration(@class.Span, @class);
        }
        finally
        {
            strict = outerStrict;
        }
    }

    /// <summary>A ClassExpression (clause 15.7).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=CF8560
    // Broiler-Human:        PENDING
    private Expression ParseClassExpression(IReadOnlyList<Decorator> decorators)
    {
        var start = decorators.Count > 0 ? decorators[0].Span : StartOf(Ex);
        ExpectWord(Word.Class, "class", LexicalGoal.RegExp);
        var outerStrict = strict;
        strict = true;
        try
        {
            BindingIdentifier? name = null;
            if (CanBind(Op))
                name = ParseBindingIdentifier();
            var @class = ParseClassTail(start, decorators, name);
            return new ClassExpression(@class.Span, @class);
        }
        finally
        {
            strict = outerStrict;
        }
    }

    /// <summary>
    /// A ClassTail: the ClassHeritage's LeftHandSideExpression and the ClassBody. The heritage is charged one level of
    /// nesting (decision D14): a class expression there recurses through it with no statement or assignment between, so
    /// <c>class extends class extends …</c> would otherwise reach the stack uncharged, a gap Broiler.VM's parser has too.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15.7 ClassHeritage, JSD-0083 D14 nesting bound; IP=Low; Security=High; Resources=2; Fingerprint=735E95
    // Broiler-Falsified-If: a chain of class heritages nested past the request's bound reaches the stack rather than being refused with the nesting diagnostic
    // Broiler-Human:        PENDING
    private ClassNode ParseClassTail(SourceSpan start, IReadOnlyList<Decorator> decorators, BindingIdentifier? name)
    {
        Expression? heritage = null;
        if (IsWord(Op, Word.Extends))
        {
            var keyword = Advance();
            EnterNesting(keyword);
            try
            {
                heritage = ParseLeftHandSide();
            }
            finally
            {
                ExitNesting();
            }

            if (ReferenceEquals(heritage, arrowCover) || ReferenceEquals(heritage, primaryArrow))
                throw UnexpectedAt(heritage.Span);
        }

        Expect(TokenKind.LeftBrace);
        var elements = new List<SyntaxNode>();
        while (true)
        {
            var token = Op;
            if (token.Kind == TokenKind.RightBrace)
                break;
            if (token.Kind == TokenKind.Semicolon)
            {
                Advance();
                continue;
            }

            elements.Add(ParseClassElement());
        }

        Advance();
        return new ClassNode(SpanFrom(start), decorators, name, heritage, Lists.Freeze(elements));
    }

    /// <summary>Whether a token can begin a PropertyName or ClassElementName (clauses 13.2.5 and 15.7): a name, a string, a number, <c>[</c> or a private name.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=918C42
    // Broiler-Human:        PENDING
    private static bool StartsElementName(Token token) => token.Kind is TokenKind.IdentifierName or TokenKind.StringLiteral
        or TokenKind.NumericLiteral or TokenKind.LeftBracket or TokenKind.PrivateIdentifier;

    /// <summary>Whether a token, after a modifier word, means the word is the element's name: <c>(</c>, <c>=</c>, <c>;</c> or <c>}</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=0F5B20
    // Broiler-Human:        PENDING
    private static bool EndsElementName(Token token) => token.Kind is TokenKind.LeftParen or TokenKind.Equals or TokenKind.Semicolon or TokenKind.RightBrace or TokenKind.EndOfInput;

    /// <summary>
    /// One ClassElement (clause 15.7): a method, accessor, generator or async method, a field (an auto-accessor under its
    /// flag), or a static block, <c>static</c> where written; <c>get</c>, <c>set</c>, <c>static</c>, <c>async</c> and
    /// <c>accessor</c> are names where a name's end follows them.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=ECC2DD
    // Broiler-Human:        PENDING
    private SyntaxNode ParseClassElement()
    {
        var first = Op;
        var start = StartOf(first);
        IReadOnlyList<Decorator> decorators = System.Array.Empty<Decorator>();
        if (first.Kind == TokenKind.At && (proposals & ProposalFlags.Decorators) != 0)
            decorators = ParseDecorators();

        var token = Op;
        var isStatic = false;
        if (IsWord(token, Word.Static))
        {
            var next = PeekAfter(LexicalGoal.Div, LexicalGoal.Div);
            if (next.Kind == TokenKind.LeftBrace && decorators.Count == 0)
                return ParseStaticBlock(start);
            if (!EndsElementName(next))
            {
                isStatic = true;
                Advance();
                token = Op;
            }
        }

        var memberStart = StartOf(token);

        // `accessor` followed by an element's name on its line is an auto-accessor, whose proposal the request does not
        // admit here (catalogue row EP0010, decision D17).
        if (IsWord(token, Word.Accessor) && (proposals & ProposalFlags.AutoAccessors) == 0)
        {
            var next = PeekAfter(LexicalGoal.Div, LexicalGoal.Div);
            if (StartsElementName(next) && !EndsElementName(next) && !next.NewLineBefore)
                throw Refuse(Rows.EP0010, SpanOf(token), "accessor");
        }

        if (IsWord(token, Word.Accessor) && (proposals & ProposalFlags.AutoAccessors) != 0)
        {
            var next = PeekAfter(LexicalGoal.Div, LexicalGoal.Div);
            if (!EndsElementName(next) && !next.NewLineBefore)
            {
                Advance();
                var accessorKey = ParsePropertyName(allowPrivate: true);
                var accessorValue = ParseFieldInitializer();
                ConsumeSemicolon();
                return new FieldDefinition(SpanFrom(start), decorators, isStatic, isAccessor: true, accessorKey, accessorValue);
            }
        }

        if (token.Kind == TokenKind.Star)
        {
            Advance();
            var key = ParsePropertyName(allowPrivate: true);
            var function = ParseMethod(memberStart, MethodKind.Method, isAsync: false, isGenerator: true);
            return new MethodDefinition(SpanFrom(start), decorators, isStatic, MethodKind.Method, key, function);
        }

        if (IsWord(token, Word.Async))
        {
            var next = PeekAfter(LexicalGoal.Div, LexicalGoal.Div);
            if (!EndsElementName(next) && !next.NewLineBefore)
            {
                Advance();
                var isGenerator = Eat(TokenKind.Star);
                var key = ParsePropertyName(allowPrivate: true);
                var function = ParseMethod(memberStart, MethodKind.Method, isAsync: true, isGenerator);
                return new MethodDefinition(SpanFrom(start), decorators, isStatic, MethodKind.Method, key, function);
            }
        }

        if (IsWord(token, Word.Get) || IsWord(token, Word.Set))
        {
            var next = PeekAfter(LexicalGoal.Div, LexicalGoal.Div);
            if (StartsElementName(next))
            {
                Advance();
                var kind = token.Word == Word.Get ? MethodKind.Get : MethodKind.Set;
                var key = ParsePropertyName(allowPrivate: true);
                var function = ParseMethod(memberStart, kind, isAsync: false, isGenerator: false);
                return new MethodDefinition(SpanFrom(start), decorators, isStatic, kind, key, function);
            }
        }

        var name = ParsePropertyName(allowPrivate: true);
        if (Op.Kind == TokenKind.LeftParen)
        {
            var function = ParseMethod(memberStart, MethodKind.Method, isAsync: false, isGenerator: false);
            return new MethodDefinition(SpanFrom(start), decorators, isStatic, MethodKind.Method, name, function);
        }

        var value = ParseFieldInitializer();
        ConsumeSemicolon();
        return new FieldDefinition(SpanFrom(start), decorators, isStatic, isAccessor: false, name, value);
    }

    /// <summary>
    /// A field's Initializer (clause 15.7), read where its value is evaluated later, outside the top level, as a function
    /// boundary: under [~Yield, ~Await], the reading both engines and test262 take (<c>x = await;</c> names <c>await</c>
    /// inside an async function, and <c>x = await 1</c> is refused), where ES2026's production reads
    /// <c>Initializer[+In, ?Yield, ?Await]</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=F82313
    // Broiler-Human:        PENDING
    private Expression? ParseFieldInitializer()
    {
        if (Op.Kind != TokenKind.Equals)
            return null;
        Advance();
        var outerYield = inYield;
        var outerAwait = inAwait;
        inYield = false;
        inAwait = false;
        var awaitMark = awaitReferences.Count;
        functionDepth++;
        try
        {
            return ParseAssignment(noIn: false);
        }
        finally
        {
            functionDepth--;
            inYield = outerYield;
            inAwait = outerAwait;
            DropAwaitReferences(awaitMark);
        }
    }

    /// <summary>A ClassStaticBlock (clause 15.7): its statements under [~Yield, +Await, ~Return].</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=E08310
    // Broiler-Human:        PENDING
    private StaticBlock ParseStaticBlock(SourceSpan start)
    {
        Advance();
        Expect(TokenKind.LeftBrace);
        var outerYield = inYield;
        var outerAwait = inAwait;
        var outerReturn = inReturn;
        var outerStaticBlock = inStaticBlock;
        inYield = false;
        inAwait = true;
        inReturn = false;
        inStaticBlock = true;
        functionDepth++;
        try
        {
            var body = new List<Statement>();
            while (Ex.Kind != TokenKind.RightBrace)
            {
                if (cur.Kind == TokenKind.EndOfInput)
                    throw Missing(cur, "}");
                body.Add(ParseStatementListItem());
            }

            Advance();
            return new StaticBlock(SpanFrom(start), Lists.Freeze(body));
        }
        finally
        {
            inYield = outerYield;
            inAwait = outerAwait;
            inReturn = outerReturn;
            inStaticBlock = outerStaticBlock;
            functionDepth--;
        }
    }

    /// <summary>Decorators, under the decorators proposal's flag (decision D17): <c>@</c> and a member chain, its call, or a parenthesised expression.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15 functions classes, s13.15.5 conversions; IP=Low; Security=Medium; Resources=2; Fingerprint=A0D4B0
    // Broiler-Human:        PENDING
    private IReadOnlyList<Decorator> ParseDecorators()
    {
        var list = new List<Decorator>();
        while (Op.Kind == TokenKind.At)
        {
            var start = StartOf(Advance());
            Expression expression;
            var token = Op;
            if (token.Kind == TokenKind.LeftParen)
            {
                Advance();
                var inner = ParseExpression(noIn: false);
                Expect(TokenKind.RightParen);
                expression = new ParenthesizedExpression(SpanFrom(StartOf(token)), inner);
            }
            else
            {
                if (!IsIdentifierReference(token))
                    throw Unexpected(token);
                Advance();
                expression = NoteAwaitReference(new Identifier(SpanOf(token), token.Value!, token.IsEscaped));

                // The member chain and its call are a chain like ParseChainLinks's: each link charged one level of tree
                // depth before it is built (decision D14), so a chain past ParseLimits.MaximumTreeDepth is refused.
                var saved = treeDepth;
                try
                {
                    while (Op.Kind == TokenKind.Dot)
                    {
                        Advance();
                        var member = ParseMemberName();
                        var span = SpanFrom(expression.Span);
                        expression = new MemberExpression(span, expression, member, isComputed: false, isOptional: false);
                        Charge(expression, saved, span);
                    }

                    if (Op.Kind == TokenKind.LeftParen)
                    {
                        var (arguments, trailing) = ParseArguments(defer: false);
                        var span = SpanFrom(expression.Span);
                        expression = new CallExpression(span, expression, arguments, trailing, isOptional: false);
                        Charge(expression, saved, span);
                    }
                }
                finally
                {
                    treeDepth = saved;
                }
            }

            list.Add(new Decorator(SpanFrom(start), expression));
        }

        return Lists.Freeze(list);
    }
}
