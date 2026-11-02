// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   47
// Annotated:        47/47
// Exempt:           8
// Human-reviewed:   0/47
// IP risk:          Low
// Security risk:    High
// Criteria:         5/5
// Resource impact:  2/10 max
// Unverified:       47
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>Expressions (ECMA-262 clause 13), with the cover grammars read once and converted.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=130766
// Broiler-Human:        PENDING
internal sealed partial class Parser
{
    // The [In] parameter of the AssignmentExpression being read, which an async arrow read whole as a primary takes.
    private bool assignmentNoIn;

    /// <summary>The precedence of <c>in</c>, <c>instanceof</c> and the four comparisons.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Low; Resources=0; Fingerprint=2AB4A2
    // Broiler-Human:        PENDING
    private const int Relational = 8;

    /// <summary>The precedence of <c>**</c>, the one right-associative binary operator.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Low; Resources=0; Fingerprint=6FC2D1
    // Broiler-Human:        PENDING
    private const int Exponent = 12;

    /// <summary>An Expression (clause 13.16): AssignmentExpressions joined by commas.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=DCA6DD
    // Broiler-Human:        PENDING
    private Expression ParseExpression(bool noIn, bool defer = false)
    {
        var first = ParseAssignment(noIn, defer);
        if (Op.Kind != TokenKind.Comma)
            return first;

        var list = new List<Expression> { first };
        while (Eat(TokenKind.Comma))
            list.Add(ParseAssignment(noIn, defer));
        return new SequenceExpression(SpanFrom(first.Span), Lists.Freeze(list));
    }

    /// <summary>An Expression whose CoverInitializedNames the caller converts or refuses: a for statement's head.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=30F070
    // Broiler-Human:        PENDING
    private Expression ParseExpressionCover(bool noIn) => ParseExpression(noIn, defer: true);

    /// <summary>
    /// An AssignmentExpression (clause 13.15), charged one level of nesting: a YieldExpression, an arrow function converted
    /// from the identifier or cover just read, an assignment whose array or object literal target is converted to a
    /// pattern, or a ConditionalExpression. With <paramref name="defer"/>, a CoverInitializedName read here and not
    /// converted is left to the caller, whose literal may yet become a pattern.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15 s15.3 arrows by conversion; IP=Low; Security=High; Resources=2; Fingerprint=07ED58
    // Broiler-Falsified-If: an arrow is read where its cover was not the whole left operand, a target that is no LeftHandSideExpression is admitted, or a CoverInitializedName no conversion takes is admitted
    // Broiler-Human:        PENDING
    private Expression ParseAssignment(bool noIn, bool defer = false)
    {
        var token = Ex;
        EnterNesting(token);
        try
        {
            if (inYield && IsWord(token, Word.Yield))
                return ParseYield(noIn);

            var mark = coverInitialized.Count;
            var outerNoIn = assignmentNoIn;
            assignmentNoIn = noIn;
            var outerCover = arrowCover;
            var outerArrow = primaryArrow;
            arrowCover = null;
            primaryArrow = null;
            var left = ParseConditional(noIn);
            var cover = arrowCover;
            var arrow = primaryArrow;
            arrowCover = outerCover;
            primaryArrow = outerArrow;
            assignmentNoIn = outerNoIn;

            if (arrow is not null)
            {
                if (!ReferenceEquals(arrow, left))
                    throw UnexpectedAt(arrow.Span);
                return left;
            }

            var next = Op;
            if (next.Kind == TokenKind.Arrow)
            {
                if (next.NewLineBefore)
                    throw Unexpected(next);
                if (left is Identifier identifier)
                {
                    // ArrowParameters : BindingIdentifier, which `await` may be under [+Await] too (an early error, stage 4d).
                    ForgetAwaitReference(identifier);
                    return ParseArrowFunction(StartOf(token), new[] { (Pattern)ToBindingIdentifier(identifier) }, false, isAsync: false, mark, noIn);
                }
                if (cover is not null && ReferenceEquals(cover, left))
                    return ParseArrowFromCover(StartOf(token), cover, mark, noIn);
                throw Unexpected(next);
            }

            if (AssignmentOperatorOf(next.Kind) is { } op)
            {
                SyntaxNode target;
                if (op == AssignmentOperator.Assign && left is ArrayLiteral or ObjectLiteral)
                    target = ToAssignmentPattern(left);
                else if (IsLeftHandSide(left))
                    target = left;
                else
                    throw Unexpected(next);

                Advance();
                var value = ParseAssignment(noIn);
                var assignment = new AssignmentExpression(SpanFrom(left.Span), op, target, value);
                if (!defer)
                    CheckCoverInitialized(mark);
                return assignment;
            }

            if (!defer)
                CheckCoverInitialized(mark);
            return left;
        }
        finally
        {
            ExitNesting();
        }
    }

    /// <summary>Refuses the first CoverInitializedName read since <paramref name="mark"/> that no conversion took (catalogue row EE0017 at stage 4d).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.2.5 CoverInitializedName; IP=Low; Security=High; Resources=2; Fingerprint=BD658C
    // Broiler-Falsified-If: a CoverInitializedName that no conversion took is admitted
    // Broiler-Human:        PENDING
    private void CheckCoverInitialized(int mark)
    {
        if (coverInitialized.Count > mark)
            throw Unexpected(coverInitializedAt[coverInitialized[mark]]);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=E308C4
    // Broiler-Human:        PENDING
    private static AssignmentOperator? AssignmentOperatorOf(TokenKind kind) => kind switch
    {
        TokenKind.Equals => AssignmentOperator.Assign,
        TokenKind.StarEquals => AssignmentOperator.MultiplyAssign,
        TokenKind.SlashEquals => AssignmentOperator.DivideAssign,
        TokenKind.PercentEquals => AssignmentOperator.RemainderAssign,
        TokenKind.PlusEquals => AssignmentOperator.AddAssign,
        TokenKind.MinusEquals => AssignmentOperator.SubtractAssign,
        TokenKind.LeftShiftEquals => AssignmentOperator.LeftShiftAssign,
        TokenKind.RightShiftEquals => AssignmentOperator.SignedRightShiftAssign,
        TokenKind.UnsignedRightShiftEquals => AssignmentOperator.UnsignedRightShiftAssign,
        TokenKind.AmpersandEquals => AssignmentOperator.BitwiseAndAssign,
        TokenKind.CaretEquals => AssignmentOperator.BitwiseXorAssign,
        TokenKind.BarEquals => AssignmentOperator.BitwiseOrAssign,
        TokenKind.StarStarEquals => AssignmentOperator.ExponentAssign,
        TokenKind.AmpersandAmpersandEquals => AssignmentOperator.AndAssign,
        TokenKind.BarBarEquals => AssignmentOperator.OrAssign,
        TokenKind.QuestionQuestionEquals => AssignmentOperator.CoalesceAssign,
        _ => null,
    };

    /// <summary>
    /// Whether an expression is a LeftHandSideExpression (clause 13.3): a NewExpression, CallExpression or OptionalExpression,
    /// which a primary expression is. Whether it is a valid target is a static-semantic rule (stage 4d).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=60433A
    // Broiler-Human:        PENDING
    private static bool IsLeftHandSide(Expression expression) => expression.Kind is SyntaxKind.Identifier or SyntaxKind.ThisExpression
        or SyntaxKind.NullLiteral or SyntaxKind.BooleanLiteral or SyntaxKind.NumericLiteral or SyntaxKind.StringLiteral
        or SyntaxKind.RegularExpressionLiteral or SyntaxKind.TemplateLiteral or SyntaxKind.ArrayLiteral or SyntaxKind.ObjectLiteral
        or SyntaxKind.FunctionExpression or SyntaxKind.ClassExpression or SyntaxKind.MemberExpression or SyntaxKind.CallExpression
        or SyntaxKind.ChainExpression or SyntaxKind.NewExpression or SyntaxKind.MetaProperty or SyntaxKind.ImportCall
        or SyntaxKind.TaggedTemplateExpression
        || (expression.Kind == SyntaxKind.ParenthesizedExpression && expression is ParenthesizedExpression);

    /// <summary>A YieldExpression (clause 15.5): its operand on the same line where one is written, <c>yield*</c> with one always.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=5207A7
    // Broiler-Human:        PENDING
    private Expression ParseYield(bool noIn)
    {
        var keyword = Advance();
        var start = StartOf(keyword);
        var token = Ex;
        if (token.NewLineBefore)
            return new YieldExpression(SpanFrom(start), null, false);

        if (token.Kind == TokenKind.Star)
        {
            Advance();
            var delegated = ParseAssignment(noIn);
            return new YieldExpression(SpanFrom(start), delegated, true);
        }

        if (!StartsExpression(token))
            return new YieldExpression(SpanFrom(start), null, false);
        var argument = ParseAssignment(noIn);
        return new YieldExpression(SpanFrom(start), argument, false);
    }

    /// <summary>Whether a token, read where an expression may begin, can begin an AssignmentExpression.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=CD3A78
    // Broiler-Human:        PENDING
    private bool StartsExpression(Token token) => token.Kind switch
    {
        TokenKind.IdentifierName => token.IsEscaped || token.Word is not (Word.In or Word.Instanceof),
        TokenKind.NumericLiteral or TokenKind.StringLiteral or TokenKind.NoSubstitutionTemplate or TokenKind.TemplateHead
            or TokenKind.RegularExpressionLiteral or TokenKind.PrivateIdentifier => true,
        TokenKind.LeftParen or TokenKind.LeftBracket or TokenKind.LeftBrace => true,
        TokenKind.Plus or TokenKind.Minus or TokenKind.Bang or TokenKind.Tilde or TokenKind.PlusPlus or TokenKind.MinusMinus => true,
        TokenKind.At => (proposals & ProposalFlags.Decorators) != 0,
        _ => false,
    };

    /// <summary>
    /// A ConditionalExpression (clause 13.14). An async arrow read whole as a primary is no ShortCircuitExpression, so it
    /// is never a condition's test: it is returned to the AssignmentExpression that may be it.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=D158A3
    // Broiler-Human:        PENDING
    private Expression ParseConditional(bool noIn)
    {
        var test = ParseBinary(1, noIn);
        if (ReferenceEquals(test, primaryArrow) || Op.Kind != TokenKind.Question)
            return test;

        Advance();
        var consequent = ParseAssignment(noIn: false);
        Expect(TokenKind.Colon);
        var alternate = ParseAssignment(noIn);
        return new ConditionalExpression(SpanFrom(test.Span), test, consequent, alternate);
    }

    /// <summary>
    /// The binary operators of clauses 13.6 to 13.13 at or above <paramref name="minimum"/> precedence, with
    /// <c>#x in o</c> where a RelationalExpression may begin.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=735058
    // Broiler-Human:        PENDING
    private Expression ParseBinary(int minimum, bool noIn)
    {
        var token = Ex;
        Expression left;
        if (token.Kind == TokenKind.PrivateIdentifier)
        {
            var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
            if (!IsWord(next, Word.In) || noIn || minimum > Relational)
                throw Unexpected(token);
            Advance();
            var name = new PrivateIdentifier(SpanOf(token), token.Value!, token.IsEscaped);
            Advance();
            var right = ParseBinary(Relational + 1, noIn);
            left = new PrivateInExpression(SpanFrom(name.Span), name, right);
        }
        else
        {
            left = ParseUnary();
        }

        return ParseBinaryRest(left, minimum, noIn);
    }

    /// <summary>
    /// Extends a left operand with every binary operator of at least <paramref name="minimum"/> precedence, charging one level
    /// of tree depth for each node from the left operand's spine (decision D14). An async arrow read whole as a primary is
    /// no operand (clause 15.9: an AsyncArrowFunction is only an AssignmentExpression), so nothing extends it: the operator
    /// after it is left to the statement's end, where a line terminator before it inserts a semicolon (clause 12.10).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.6-s13.13 precedence, JSD-0083 D14 left-operand charge; IP=Low; Security=High; Resources=2; Fingerprint=EB11AA
    // Broiler-Falsified-If: an operator binds with another precedence or associativity than the grammar's, or a chain is charged without its left operand's spine
    // Broiler-Human:        PENDING
    private Expression ParseBinaryRest(Expression left, int minimum, bool noIn)
    {
        if (ReferenceEquals(left, primaryArrow))
            return left;

        var saved = treeDepth;
        treeDepth += left.Spine;
        try
        {
            while (true)
            {
                var token = Op;
                if (noIn && IsWord(token, Word.In))
                    return left;
                var precedence = Precedence(token);
                if (precedence == 0 || precedence < minimum)
                    return left;

                // ExponentiationExpression: its left operand is an UpdateExpression, never a bare UnaryExpression (clause 13.6).
                if (precedence == Exponent && left.Kind is SyntaxKind.UnaryExpression or SyntaxKind.AwaitExpression)
                    throw Unexpected(token);

                Advance();
                Expression right;
                if (precedence == Exponent)
                {
                    // The right operand of ** recurses at the same precedence: charged as nesting, which bounds the recursion.
                    EnterNesting(token);
                    try
                    {
                        right = ParseBinary(Exponent, noIn);
                    }
                    finally
                    {
                        ExitNesting();
                    }
                }
                else
                {
                    right = ParseBinary(precedence + 1, noIn);
                }

                Expression node;
                var span = SpanFrom(left.Span);
                if (LogicalOperatorOf(token) is { } logical)
                {
                    // A CoalesceExpression has no LogicalOR or LogicalAND operand, nor the other way round (clause 13.13).
                    if (MixesCoalesce(logical, left) || MixesCoalesce(logical, right))
                        throw Unexpected(token);
                    node = new LogicalExpression(span, logical, left, right);
                }
                else
                {
                    node = new BinaryExpression(span, BinaryOperatorOf(token), left, right);
                }

                Deepen(span);
                node.Spine = treeDepth - saved;
                left = node;
            }
        }
        finally
        {
            treeDepth = saved;
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=AF8190
    // Broiler-Human:        PENDING
    private static bool MixesCoalesce(LogicalOperator op, Expression operand) =>
        operand is LogicalExpression logical && (op == LogicalOperator.Coalesce) != (logical.Operator == LogicalOperator.Coalesce);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=E15182
    // Broiler-Human:        PENDING
    private static int Precedence(Token token) => token.Kind switch
    {
        TokenKind.QuestionQuestion => 1,
        TokenKind.BarBar => 2,
        TokenKind.AmpersandAmpersand => 3,
        TokenKind.Bar => 4,
        TokenKind.Caret => 5,
        TokenKind.Ampersand => 6,
        TokenKind.EqualsEquals or TokenKind.NotEquals or TokenKind.EqualsEqualsEquals or TokenKind.NotEqualsEquals => 7,
        TokenKind.LessThan or TokenKind.GreaterThan or TokenKind.LessThanEquals or TokenKind.GreaterThanEquals => Relational,
        TokenKind.IdentifierName when IsWord(token, Word.In) || IsWord(token, Word.Instanceof) => Relational,
        TokenKind.LeftShift or TokenKind.RightShift or TokenKind.UnsignedRightShift => 9,
        TokenKind.Plus or TokenKind.Minus => 10,
        TokenKind.Star or TokenKind.Slash or TokenKind.Percent => 11,
        TokenKind.StarStar => Exponent,
        _ => 0,
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=FB3228
    // Broiler-Human:        PENDING
    private static LogicalOperator? LogicalOperatorOf(Token token) => token.Kind switch
    {
        TokenKind.AmpersandAmpersand => LogicalOperator.And,
        TokenKind.BarBar => LogicalOperator.Or,
        TokenKind.QuestionQuestion => LogicalOperator.Coalesce,
        _ => null,
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=A2B5EE
    // Broiler-Human:        PENDING
    private static BinaryOperator BinaryOperatorOf(Token token) => token.Kind switch
    {
        TokenKind.StarStar => BinaryOperator.Exponent,
        TokenKind.Star => BinaryOperator.Multiply,
        TokenKind.Slash => BinaryOperator.Divide,
        TokenKind.Percent => BinaryOperator.Remainder,
        TokenKind.Plus => BinaryOperator.Add,
        TokenKind.Minus => BinaryOperator.Subtract,
        TokenKind.LeftShift => BinaryOperator.LeftShift,
        TokenKind.RightShift => BinaryOperator.SignedRightShift,
        TokenKind.UnsignedRightShift => BinaryOperator.UnsignedRightShift,
        TokenKind.LessThan => BinaryOperator.LessThan,
        TokenKind.GreaterThan => BinaryOperator.GreaterThan,
        TokenKind.LessThanEquals => BinaryOperator.LessThanOrEqual,
        TokenKind.GreaterThanEquals => BinaryOperator.GreaterThanOrEqual,
        TokenKind.EqualsEquals => BinaryOperator.Equal,
        TokenKind.NotEquals => BinaryOperator.NotEqual,
        TokenKind.EqualsEqualsEquals => BinaryOperator.StrictEqual,
        TokenKind.NotEqualsEquals => BinaryOperator.StrictNotEqual,
        TokenKind.Ampersand => BinaryOperator.BitwiseAnd,
        TokenKind.Caret => BinaryOperator.BitwiseXor,
        TokenKind.Bar => BinaryOperator.BitwiseOr,
        _ => token.Word == Word.In ? BinaryOperator.In : BinaryOperator.Instanceof,
    };

    /// <summary>
    /// A UnaryExpression or UpdateExpression (clauses 13.4, 13.5 and 15.8): each prefix operator that recurses is charged
    /// one level of nesting, as Broiler.VM's parser charges it.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=E78796
    // Broiler-Human:        PENDING
    private Expression ParseUnary()
    {
        var token = Ex;
        UnaryOperator? unary = token.Kind switch
        {
            TokenKind.Plus => UnaryOperator.Plus,
            TokenKind.Minus => UnaryOperator.Minus,
            TokenKind.Bang => UnaryOperator.LogicalNot,
            TokenKind.Tilde => UnaryOperator.BitwiseNot,
            TokenKind.IdentifierName when IsWord(token, Word.Delete) => UnaryOperator.Delete,
            TokenKind.IdentifierName when IsWord(token, Word.Void) => UnaryOperator.Void,
            TokenKind.IdentifierName when IsWord(token, Word.Typeof) => UnaryOperator.Typeof,
            _ => null,
        };
        var isUpdate = token.Kind is TokenKind.PlusPlus or TokenKind.MinusMinus;
        var isAwait = inAwait && IsWord(token, Word.Await);
        if (unary is null && !isUpdate && !isAwait)
            return ParsePostfix();

        EnterNesting(token);
        try
        {
            var start = StartOf(Advance());
            if (isAwait)
            {
                NoteAwait();
                var awaited = ParseUnary();
                return new AwaitExpression(SpanFrom(start), awaited);
            }

            var operand = ParseUnary();
            if (isUpdate)
                return new UpdateExpression(SpanFrom(start), token.Kind == TokenKind.PlusPlus ? UpdateOperator.Increment : UpdateOperator.Decrement, isPrefix: true, operand);
            return new UnaryExpression(SpanFrom(start), unary!.Value, operand);
        }
        finally
        {
            ExitNesting();
        }
    }

    /// <summary>A postfix UpdateExpression (clause 13.4): <c>++</c> or <c>--</c> on the same line, after anything but an async arrow read whole as a primary.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=506C97
    // Broiler-Human:        PENDING
    private Expression ParsePostfix()
    {
        var expression = ParseLeftHandSide();
        if (ReferenceEquals(expression, primaryArrow))
            return expression;
        var token = Op;
        if (token.Kind is TokenKind.PlusPlus or TokenKind.MinusMinus && !token.NewLineBefore)
        {
            Advance();
            return new UpdateExpression(SpanFrom(expression.Span), token.Kind == TokenKind.PlusPlus ? UpdateOperator.Increment : UpdateOperator.Decrement, isPrefix: false, expression);
        }

        return expression;
    }

    /// <summary>A LeftHandSideExpression (clause 13.3): a NewExpression, a CallExpression or an OptionalExpression.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=7A568A
    // Broiler-Human:        PENDING
    private Expression ParseLeftHandSide()
    {
        var token = Ex;
        Expression current;
        if (IsWord(token, Word.New))
            current = ParseNew();
        else if (IsWord(token, Word.Super))
            current = ParseSuper(allowCalls: true);
        else if (IsWord(token, Word.Import))
            current = ParseImportExpression(allowCalls: true);
        else
            current = ParsePrimary();

        if (ReferenceEquals(current, arrowCover) || ReferenceEquals(current, primaryArrow))
            return current;
        return ParseChainLinks(current, allowCalls: true, asyncHead: IsWord(token, Word.Async) && current is Identifier);
    }

    /// <summary>
    /// The links of a member, call or optional chain after its first expression (clauses 13.3.2 to 13.3.9 and 13.3.11),
    /// each charged one level of tree depth from the first expression's spine (decision D14). Without
    /// <paramref name="allowCalls"/> the chain is a MemberExpression, a <c>new</c> expression's constructor. With
    /// <paramref name="asyncHead"/>, a first argument list on the same line as <c>async</c> is read as the
    /// CoverCallExpressionAndAsyncArrowHead, which a following <c>=&gt;</c> converts.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.3 member call optional chains, JSD-0083 D14; IP=Low; Security=High; Resources=2; Fingerprint=486255
    // Broiler-Falsified-If: a link the grammar refuses is admitted, or a chain is charged without its first expression's spine
    // Broiler-Human:        PENDING
    private Expression ParseChainLinks(Expression current, bool allowCalls, bool asyncHead = false)
    {
        var saved = treeDepth;
        treeDepth += current.Spine;
        var start = current.Span;
        var optional = false;
        try
        {
            while (true)
            {
                var token = Op;
                switch (token.Kind)
                {
                    case TokenKind.Dot:
                        Advance();
                        if (current is SuperExpression && Op.Kind == TokenKind.PrivateIdentifier)
                            throw Unexpected(cur); // SuperProperty : super . IdentifierName (clause 13.3.7)
                        var name = ParseMemberName();
                        current = new MemberExpression(SpanFrom(start), current, name, isComputed: false, isOptional: false);
                        current = Fix(current, start);
                        break;

                    case TokenKind.QuestionDot:
                        if (!allowCalls)
                            throw Unexpected(token);
                        optional = true;
                        Advance();
                        var after = Op;
                        if (after.Kind == TokenKind.LeftParen)
                        {
                            var (arguments, trailing) = ParseArguments(defer: false);
                            current = new CallExpression(SpanFrom(start), current, arguments, trailing, isOptional: true);
                        }
                        else if (after.Kind == TokenKind.LeftBracket)
                        {
                            Advance();
                            var key = ParseExpression(noIn: false);
                            Expect(TokenKind.RightBracket);
                            current = new MemberExpression(SpanFrom(start), current, key, isComputed: true, isOptional: true);
                        }
                        else if (after.Kind is TokenKind.NoSubstitutionTemplate or TokenKind.TemplateHead)
                        {
                            var quasi = ParseTemplate();
                            current = new TaggedTemplateExpression(SpanFrom(start), current, quasi);
                        }
                        else
                        {
                            var optionalName = ParseMemberName();
                            current = new MemberExpression(SpanFrom(start), current, optionalName, isComputed: false, isOptional: true);
                        }

                        Fix(current, start);
                        break;

                    case TokenKind.LeftBracket:
                    {
                        Advance();
                        var key = ParseExpression(noIn: false);
                        Expect(TokenKind.RightBracket);
                        current = new MemberExpression(SpanFrom(start), current, key, isComputed: true, isOptional: false);
                        Fix(current, start);
                        break;
                    }

                    case TokenKind.LeftParen when allowCalls:
                    {
                        if (asyncHead && !token.NewLineBefore)
                        {
                            asyncHead = false;
                            var mark = coverInitialized.Count;
                            var awaitMark = awaitReferences.Count;
                            var (arguments, trailing) = ParseArguments(defer: true);
                            var call = new CallExpression(SpanFrom(start), current, arguments, trailing, isOptional: false);
                            var arrow = Op;
                            if (arrow.Kind == TokenKind.Arrow && !arrow.NewLineBefore)
                            {
                                var head = new AsyncArrowHead(call, mark, awaitMark);
                                arrowCover = head;
                                return head;
                            }

                            CheckCoverInitialized(mark);
                            current = call;
                        }
                        else
                        {
                            var (arguments, trailing) = ParseArguments(defer: false);
                            current = new CallExpression(SpanFrom(start), current, arguments, trailing, isOptional: false);
                        }

                        Fix(current, start);
                        break;
                    }

                    case TokenKind.NoSubstitutionTemplate:
                    case TokenKind.TemplateHead:
                    {
                        var quasi = ParseTemplate();
                        current = new TaggedTemplateExpression(SpanFrom(start), current, quasi);
                        Fix(current, start);
                        break;
                    }

                    default:
                        if (optional)
                        {
                            var chain = new ChainExpression(current.Span, current);
                            chain.Spine = current.Spine;
                            return chain;
                        }

                        return current;
                }

                asyncHead = false;
            }
        }
        finally
        {
            treeDepth = saved;
        }

        Expression Fix(Expression link, SourceSpan at)
        {
            Deepen(at);
            link.Spine = treeDepth - saved;
            return link;
        }
    }

    /// <summary>A member's name after <c>.</c> or <c>?.</c>: an IdentifierName, any word among them, or a PrivateIdentifier (clause 13.3).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=A0FA77
    // Broiler-Human:        PENDING
    private SyntaxNode ParseMemberName()
    {
        var token = Op;
        if (token.Kind == TokenKind.IdentifierName)
        {
            Advance();
            return new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
        }

        if (token.Kind == TokenKind.PrivateIdentifier)
        {
            Advance();
            return new PrivateIdentifier(SpanOf(token), token.Value!, token.IsEscaped);
        }

        throw Unexpected(token);
    }

    /// <summary>An Arguments list (clause 13.3.8): AssignmentExpressions and spreads, with a trailing comma where written.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=918504
    // Broiler-Human:        PENDING
    private (IReadOnlyList<Expression> Arguments, bool TrailingComma) ParseArguments(bool defer)
    {
        Expect(TokenKind.LeftParen);
        var list = new List<Expression>();
        var trailing = false;
        while (Ex.Kind != TokenKind.RightParen)
        {
            var token = cur;
            if (token.Kind == TokenKind.Ellipsis)
            {
                Advance();
                var argument = ParseAssignment(noIn: false, defer);
                list.Add(new SpreadElement(SpanFrom(StartOf(token)), argument));
            }
            else
            {
                list.Add(ParseAssignment(noIn: false, defer));
            }

            if (Op.Kind == TokenKind.RightParen)
                break;
            Expect(TokenKind.Comma);
            if (Ex.Kind == TokenKind.RightParen)
                trailing = true;
        }

        Advance();
        return (Lists.Freeze(list), trailing);
    }

    /// <summary>
    /// A <c>new</c> expression or <c>new.target</c> (clauses 13.3.5 and 13.3.12), each <c>new</c> charged one level of nesting
    /// as Broiler.VM's parser charges it: the constructor is a MemberExpression, with no call and no optional link.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=3CA657
    // Broiler-Human:        PENDING
    private Expression ParseNew()
    {
        var keyword = Ex;
        EnterNesting(keyword);
        try
        {
            var start = StartOf(Advance());
            if (Op.Kind == TokenKind.Dot)
            {
                Advance();
                var target = Op;
                if (!IsWord(target, Word.Target))
                    throw Unexpected(target);
                Advance();
                var meta = new IdentifierName(SpanOf(keyword), "new", false);
                return new MetaProperty(SpanFrom(start), meta, new IdentifierName(SpanOf(target), "target", false));
            }

            var token = Ex;
            Expression callee;
            if (IsWord(token, Word.New))
                callee = ParseNew();
            else if (IsWord(token, Word.Super))
                callee = ParseSuper(allowCalls: false);
            else if (IsWord(token, Word.Import))
                callee = ParseImportExpression(allowCalls: false);
            else
                callee = ParsePrimary();

            if (ReferenceEquals(callee, primaryArrow))
                throw UnexpectedAt(callee.Span);
            callee = ParseChainLinks(callee, allowCalls: false);

            IReadOnlyList<Expression> arguments = Array.Empty<Expression>();
            var hasArguments = false;
            var trailing = false;
            if (Op.Kind == TokenKind.LeftParen)
            {
                hasArguments = true;
                (arguments, trailing) = ParseArguments(defer: false);
            }

            var node = new NewExpression(SpanFrom(start), callee, arguments, hasArguments, trailing);
            node.Spine = callee.Spine;
            return node;
        }
        finally
        {
            ExitNesting();
        }
    }

    /// <summary><c>super</c> (clause 13.3.7): a SuperProperty's object, or a SuperCall's callee where calls are admitted; nothing else.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=7AA1EF
    // Broiler-Human:        PENDING
    private Expression ParseSuper(bool allowCalls)
    {
        var token = Advance();
        var next = Op;
        if (next.Kind is TokenKind.Dot or TokenKind.LeftBracket || (next.Kind == TokenKind.LeftParen && allowCalls))
            return new SuperExpression(SpanOf(token));
        throw Unexpected(next);
    }

    /// <summary>
    /// An ImportCall or <c>import.meta</c> (clauses 13.3.10 and 13.3.12), and the import phases' call forms under their flags
    /// (decision D17): an ImportCall is a CallExpression, never a <c>new</c> expression's constructor.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=E8E138
    // Broiler-Human:        PENDING
    private Expression ParseImportExpression(bool allowCalls)
    {
        var keyword = Advance();
        var start = StartOf(keyword);
        var token = Op;
        if (token.Kind == TokenKind.Dot)
        {
            Advance();
            var name = Op;
            if (IsWord(name, Word.Meta))
            {
                Advance();
                return new MetaProperty(SpanFrom(start), new IdentifierName(SpanOf(keyword), "import", false), new IdentifierName(SpanOf(name), "meta", false));
            }

            var phase = IsWord(name, Word.Source) && (proposals & ProposalFlags.ImportSourceCalls) != 0 ? ImportPhase.Source
                : IsWord(name, Word.Defer) && (proposals & ProposalFlags.ImportDeferCalls) != 0 ? ImportPhase.Defer
                : ImportPhase.Evaluation;
            if (phase == ImportPhase.Evaluation || !allowCalls)
                throw Unexpected(name);
            Advance();
            Expect(TokenKind.LeftParen);
            var specifier = ParseAssignment(noIn: false);
            var trailingComma = Eat(TokenKind.Comma);
            Expect(TokenKind.RightParen);
            return new ImportCall(SpanFrom(start), phase, specifier, null, trailingComma);
        }

        if (token.Kind != TokenKind.LeftParen || !allowCalls)
            throw Unexpected(token);

        Advance();
        var source = ParseAssignment(noIn: false);
        Expression? options = null;
        var trailing = false;
        if (Eat(TokenKind.Comma))
        {
            if (Ex.Kind != TokenKind.RightParen)
            {
                options = ParseAssignment(noIn: false);
                trailing = Eat(TokenKind.Comma);
            }
            else
            {
                trailing = true;
            }
        }

        Expect(TokenKind.RightParen);
        return new ImportCall(SpanFrom(start), ImportPhase.Evaluation, source, options, trailing);
    }

    /// <summary>A PrimaryExpression (clause 13.2), the async arrow function <c>async x =&gt; …</c> among them.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=C203D5
    // Broiler-Human:        PENDING
    private Expression ParsePrimary()
    {
        var token = Ex;
        switch (token.Kind)
        {
            case TokenKind.IdentifierName:
                return ParseNamePrimary(token);
            case TokenKind.NumericLiteral:
                Advance();
                return NumericLiteralOf(token);
            case TokenKind.StringLiteral:
                Advance();
                return StringLiteralOf(token);
            case TokenKind.NoSubstitutionTemplate:
            case TokenKind.TemplateHead:
                return ParseTemplate();
            case TokenKind.RegularExpressionLiteral:
                Advance();
                return new RegularExpressionLiteral(SpanOf(token), text.Substring(token.Start + 1, token.Split - token.Start - 1), text.Substring(token.Split + 1, token.End - token.Split - 1));
            case TokenKind.LeftParen:
                return ParseParenthesized();
            case TokenKind.LeftBracket:
                return ParseArrayLiteral();
            case TokenKind.LeftBrace:
                return ParseObjectLiteral();
            case TokenKind.At when (proposals & ProposalFlags.Decorators) != 0:
            {
                var decorators = ParseDecorators();
                if (!IsWord(Ex, Word.Class))
                    throw Missing(Ex, "class");
                return ParseClassExpression(decorators);
            }

            default:
                throw Unexpected(token);
        }
    }

    /// <summary>A primary expression that begins with a name: a keyword's, a function or class expression, an async arrow, or an IdentifierReference.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=C5E822
    // Broiler-Human:        PENDING
    private Expression ParseNamePrimary(Token token)
    {
        if (token.IsEscaped)
        {
            // An escaped name is an Identifier whatever it spells; a reserved word's is a static-semantic refusal (stage 4d).
            Advance();
            return new Identifier(SpanOf(token), token.Value!, true);
        }

        switch (token.Word)
        {
            case Word.This:
                Advance();
                return new ThisExpression(SpanOf(token));
            case Word.Null:
                Advance();
                return new NullLiteral(SpanOf(token));
            case Word.True:
            case Word.False:
                Advance();
                return new BooleanLiteral(SpanOf(token), token.Word == Word.True);
            case Word.Function:
                return ParseFunctionExpression(isAsync: false);
            case Word.Class:
                return ParseClassExpression(new List<Decorator>());
            case Word.Async:
            {
                var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
                if (IsWord(next, Word.Function) && !next.NewLineBefore)
                    return ParseFunctionExpression(isAsync: true);
                if (CanBind(next) && !next.NewLineBefore && IsArrowAfterName())
                {
                    // async [no LineTerminator here] AsyncArrowBindingIdentifier [no LineTerminator here] => (clause 15.9).
                    var mark = coverInitialized.Count;
                    Advance();
                    var parameter = ParseBindingIdentifier();
                    var arrow = Op;
                    if (arrow.Kind != TokenKind.Arrow || arrow.NewLineBefore)
                        throw Missing(arrow, "=>");
                    var function = ParseArrowFunction(StartOf(token), new[] { (Pattern)parameter }, false, isAsync: true, mark, assignmentNoIn);
                    primaryArrow = function;
                    return function;
                }

                break;
            }

            case Word.Yield when inYield:
            case Word.Await when inAwait:
                throw Unexpected(token);
            default:
                if (IsReserved(token.Word) && token.Word is not (Word.Yield or Word.Await))
                    throw Unexpected(token);
                break;
        }

        Advance();
        return NoteAwaitReference(new Identifier(SpanOf(token), token.Value!, false));
    }

    /// <summary>
    /// Records an unescaped <c>await</c> IdentifierReference read outside [Await], which an enclosing async arrow's head reads
    /// under [+Await], where it can only begin an AwaitExpression (clause 15.9): the conversion refuses it where its own
    /// parameters do not bind it. Answers the reference.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15.9 AsyncArrowHead [+Await], CoverCallExpressionAndAsyncArrowHead; IP=Low; Security=Medium; Resources=2; Fingerprint=EBB690
    // Broiler-Human:        PENDING
    private Identifier NoteAwaitReference(Identifier reference)
    {
        if (!reference.IsEscaped && reference.Name == "await")
            awaitReferences.Add(reference);
        return reference;
    }

    /// <summary>Forgets the last <c>await</c> reference recorded when it is <paramref name="identifier"/>, which the grammar reads as a BindingIdentifier instead.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15.9 AsyncArrowHead [+Await], CoverCallExpressionAndAsyncArrowHead; IP=Low; Security=Medium; Resources=2; Fingerprint=8303BC
    // Broiler-Human:        PENDING
    private void ForgetAwaitReference(Identifier identifier)
    {
        if (awaitReferences.Count > 0 && ReferenceEquals(awaitReferences[^1], identifier))
            awaitReferences.RemoveAt(awaitReferences.Count - 1);
    }

    /// <summary>Drops the <c>await</c> references a nested function read, from <paramref name="mark"/> on: its [Await] is its own, never an enclosing async arrow head's.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s15.9 AsyncArrowHead [+Await], CoverCallExpressionAndAsyncArrowHead; IP=Low; Security=Medium; Resources=2; Fingerprint=2BEB80
    // Broiler-Human:        PENDING
    private void DropAwaitReferences(int mark)
    {
        if (awaitReferences.Count > mark)
            awaitReferences.RemoveRange(mark, awaitReferences.Count - mark);
    }

    /// <summary>Whether, after the current <c>async</c> and the name after it, <c>=&gt;</c> follows on the same line: the AsyncArrowHead's two-token lookahead.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=5470CB
    // Broiler-Human:        PENDING
    private bool IsArrowAfterName()
    {
        Peek(LexicalGoal.RegExp);
        var state = tokenizer.Save();
        tokenizer.Next(LexicalGoal.Div);
        var arrow = tokenizer.Next(LexicalGoal.Div);
        tokenizer.Restore(state);
        return arrow.Kind == TokenKind.Arrow && !arrow.NewLineBefore;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=C35E77
    // Broiler-Human:        PENDING
    private NumericLiteral NumericLiteralOf(Token token)
    {
        var flags = NumericLiteralFlags.None;
        if ((token.Flags & TokenFlags.LegacyOctal) != 0)
            flags |= NumericLiteralFlags.LegacyOctal;
        if ((token.Flags & TokenFlags.NonOctalDecimal) != 0)
            flags |= NumericLiteralFlags.NonOctalDecimal;
        if ((token.Flags & TokenFlags.BigInt) != 0)
            flags |= NumericLiteralFlags.BigInt;
        if ((token.Flags & TokenFlags.DecimalSuffix) != 0)
            flags |= NumericLiteralFlags.DecimalSuffix;
        return new NumericLiteral(SpanOf(token), token.Number, text.Substring(token.Start, token.Length), flags);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=57BAC5
    // Broiler-Human:        PENDING
    private StringLiteral StringLiteralOf(Token token)
    {
        var flags = StringLiteralFlags.None;
        if ((token.Flags & TokenFlags.LegacyOctalEscape) != 0)
            flags |= StringLiteralFlags.LegacyOctalEscape;
        if ((token.Flags & TokenFlags.NonOctalDecimalEscape) != 0)
            flags |= StringLiteralFlags.NonOctalDecimalEscape;
        if ((token.Flags & TokenFlags.StringHasEscape) != 0)
            flags |= StringLiteralFlags.HasEscape;
        return new StringLiteral(SpanOf(token), token.Value!, flags);
    }

    /// <summary>
    /// A TemplateLiteral (clause 13.2.8), read in place: the tokenizer's head, middles and tail, each substitution's end read
    /// under the template goal. Its spine is its substitutions' deepest, so a chain it begins is charged through them.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=7A485B
    // Broiler-Human:        PENDING
    private TemplateLiteral ParseTemplate()
    {
        var first = Op;
        var start = StartOf(first);
        var quasis = new List<TemplateElement>();
        var expressions = new List<Expression>();
        Advance();
        quasis.Add(new TemplateElement(SpanOf(first), first.Value, first.Raw!));
        var spine = 0;
        if (first.Kind == TokenKind.TemplateHead)
        {
            while (true)
            {
                var substitution = ParseExpression(noIn: false);
                spine = Math.Max(spine, substitution.Spine);
                expressions.Add(substitution);
                var piece = Peek(LexicalGoal.TemplateTail);
                if (piece.Kind is not (TokenKind.TemplateMiddle or TokenKind.TemplateTail))
                    throw Missing(piece, "}");
                Advance();
                quasis.Add(new TemplateElement(SpanOf(piece), piece.Value, piece.Raw!));
                if (piece.Kind == TokenKind.TemplateTail)
                    break;
            }
        }

        var node = new TemplateLiteral(SpanFrom(start), Lists.Freeze(quasis), Lists.Freeze(expressions));
        node.Spine = spine;
        return node;
    }

    /// <summary>
    /// A CoverParenthesizedExpressionAndArrowParameterList (clause 13.2.10), read once: a ParenthesizedExpression, or, where
    /// <c>=&gt;</c> follows on the same line, the cover an AssignmentExpression converts into ArrowFormalParameters. The
    /// forms only parameters have (<c>()</c>, a trailing comma, a rest element) are refused without the arrow.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.2.10 CoverParenthesizedExpressionAndArrowParameterList; IP=Low; Security=High; Resources=2; Fingerprint=30764D
    // Broiler-Falsified-If: a form only parameters have is admitted without the arrow, or an arrow head is read again rather than converted
    // Broiler-Human:        PENDING
    private Expression ParseParenthesized()
    {
        var open = Advance();
        var start = StartOf(open);
        var mark = coverInitialized.Count;
        var items = new List<Expression>();
        RestElement? rest = null;
        var trailing = false;
        while (true)
        {
            var token = Ex;
            if (token.Kind == TokenKind.RightParen)
                break;
            if (token.Kind == TokenKind.Ellipsis)
            {
                Advance();
                var target = ParseBindingTarget();
                rest = new RestElement(SpanFrom(StartOf(token)), target);
                if (Op.Kind != TokenKind.RightParen)
                    throw Missing(Op, ")");
                break;
            }

            items.Add(ParseAssignment(noIn: false, defer: true));
            if (Op.Kind != TokenKind.Comma)
                break;
            Advance();
            if (Ex.Kind == TokenKind.RightParen)
            {
                trailing = true;
                break;
            }
        }

        Expect(TokenKind.RightParen);
        var next = Op;
        var isArrow = next.Kind == TokenKind.Arrow && !next.NewLineBefore;
        if (isArrow)
        {
            var cover = new ArrowParametersCover(SpanFrom(start), Lists.Freeze(items), rest, trailing, mark);
            arrowCover = cover;
            return cover;
        }

        if (items.Count == 0 || rest is not null || trailing)
            throw Unexpected(next);
        CheckCoverInitialized(mark);
        var inner = items.Count == 1 ? items[0] : new SequenceExpression(SpanFromTo(items[0].Span, items[^1].Span), Lists.Freeze(items));
        var node = new ParenthesizedExpression(SpanFrom(start), inner);
        node.Spine = inner.Spine;
        return node;
    }

    /// <summary>The span from <paramref name="first"/>'s start to <paramref name="last"/>'s end.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=4F3151
    // Broiler-Human:        PENDING
    private static SourceSpan SpanFromTo(SourceSpan first, SourceSpan last) =>
        new(first.Offset, last.Offset + last.Length - first.Offset, first.Line, first.Column);

    /// <summary>An ArrayLiteral (clause 13.2.4): elisions, spreads and a trailing comma kept; its elements' CoverInitializedNames deferred to a conversion.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=1DAD3B
    // Broiler-Human:        PENDING
    private Expression ParseArrayLiteral()
    {
        var start = StartOf(Advance());
        var elements = new List<Expression?>();
        var trailing = false;
        while (true)
        {
            var token = Ex;
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
                var argument = ParseAssignment(noIn: false, defer: true);
                elements.Add(new SpreadElement(SpanFrom(StartOf(token)), argument));
            }
            else
            {
                elements.Add(ParseAssignment(noIn: false, defer: true));
            }

            if (Op.Kind == TokenKind.RightBracket)
                break;
            Expect(TokenKind.Comma);
            if (Ex.Kind == TokenKind.RightBracket)
                trailing = true;
        }

        Advance();
        return new ArrayLiteral(SpanFrom(start), Lists.Freeze(elements), trailing);
    }

    /// <summary>
    /// An ObjectLiteral (clause 13.2.5): its PropertyDefinitions, methods and accessors, spreads, and the CoverInitializedName
    /// <c>{ a = 1 }</c>, which only a conversion to a pattern admits.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=581926
    // Broiler-Human:        PENDING
    private Expression ParseObjectLiteral()
    {
        var start = StartOf(Advance());
        var properties = new List<SyntaxNode>();
        var trailing = false;
        Token? coverAt = null;
        while (true)
        {
            var token = Op;
            if (token.Kind == TokenKind.RightBrace)
                break;
            properties.Add(ParsePropertyDefinition(ref coverAt));
            if (Op.Kind == TokenKind.RightBrace)
                break;
            Expect(TokenKind.Comma);
            if (Op.Kind == TokenKind.RightBrace)
                trailing = true;
        }

        Advance();
        var node = new ObjectLiteral(SpanFrom(start), Lists.Freeze(properties), trailing);
        if (coverAt is { } at)
        {
            coverInitialized.Add(node);
            coverInitializedAt[node] = at;
        }

        return node;
    }

    /// <summary>One PropertyDefinition (clause 13.2.5), or a MethodDefinition (clause 15.4) of an object literal.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=9554B3
    // Broiler-Human:        PENDING
    private SyntaxNode ParsePropertyDefinition(ref Token? coverAt)
    {
        var token = Op;
        var start = StartOf(token);
        if (token.Kind == TokenKind.Ellipsis)
        {
            Advance();
            var argument = ParseAssignment(noIn: false, defer: true);
            return new SpreadElement(SpanFrom(start), argument);
        }

        if (token.Kind == TokenKind.Star)
        {
            Advance();
            var key = ParsePropertyName(allowPrivate: true);
            var function = ParseMethod(start, MethodKind.Method, isAsync: false, isGenerator: true);
            return new PropertyDefinition(SpanFrom(start), PropertyKind.Method, key, new FunctionExpression(function.Span, function));
        }

        if (token.Kind == TokenKind.IdentifierName && !token.IsEscaped && token.Word is Word.Async or Word.Get or Word.Set)
        {
            var next = PeekAfter(LexicalGoal.Div, LexicalGoal.Div);
            var isName = token.Word == Word.Async ? !(StartsElementName(next) || next.Kind == TokenKind.Star) || next.NewLineBefore : !StartsElementName(next);
            if (!isName)
            {
                Advance();
                if (token.Word == Word.Async)
                {
                    var isGenerator = Eat(TokenKind.Star);
                    var key = ParsePropertyName(allowPrivate: true);
                    var function = ParseMethod(start, MethodKind.Method, isAsync: true, isGenerator);
                    return new PropertyDefinition(SpanFrom(start), PropertyKind.Method, key, new FunctionExpression(function.Span, function));
                }

                var kind = token.Word == Word.Get ? MethodKind.Get : MethodKind.Set;
                var accessorKey = ParsePropertyName(allowPrivate: true);
                var accessor = ParseMethod(start, kind, isAsync: false, isGenerator: false);
                return new PropertyDefinition(SpanFrom(start), kind == MethodKind.Get ? PropertyKind.Get : PropertyKind.Set, accessorKey, new FunctionExpression(accessor.Span, accessor));
            }
        }

        // A MethodDefinition's name is a ClassElementName, a PrivateIdentifier among them, whose refusal in an object literal
        // is a static-semantic rule (catalogue row EE0016, stage 4d); a PropertyName's is never one.
        var name = ParsePropertyName(allowPrivate: true);
        var after = Op;
        if (name is PrivateIdentifier && after.Kind != TokenKind.LeftParen)
            throw Unexpected(after);
        if (after.Kind == TokenKind.Colon)
        {
            Advance();
            var value = ParseAssignment(noIn: false, defer: true);
            return new PropertyDefinition(SpanFrom(start), PropertyKind.Init, name, value);
        }

        if (after.Kind == TokenKind.LeftParen)
        {
            var function = ParseMethod(start, MethodKind.Method, isAsync: false, isGenerator: false);
            return new PropertyDefinition(SpanFrom(start), PropertyKind.Method, name, new FunctionExpression(function.Span, function));
        }

        // A shorthand: an IdentifierReference (clause 13.1), with a CoverInitializedName's Initializer where `=` follows.
        if (name is not IdentifierName identifierName || !IsIdentifierReference(token))
            throw Unexpected(after);
        var reference = NoteAwaitReference(new Identifier(identifierName.Span, identifierName.Name, identifierName.IsEscaped));
        if (after.Kind == TokenKind.Equals)
        {
            Advance();
            coverAt ??= after;
            var initializer = ParseAssignment(noIn: false);
            var cover = new AssignmentExpression(SpanFrom(start), AssignmentOperator.Assign, reference, initializer);
            return new PropertyDefinition(SpanFrom(start), PropertyKind.Shorthand, name, cover);
        }

        return new PropertyDefinition(SpanFrom(start), PropertyKind.Shorthand, name, reference);
    }

    /// <summary>Whether a name token can be an IdentifierReference here (clause 13.1): escaped, no reserved word, <c>yield</c> outside [Yield] and <c>await</c> outside [Await].</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=3567FA
    // Broiler-Human:        PENDING
    private bool IsIdentifierReference(Token token)
    {
        if (token.Kind != TokenKind.IdentifierName)
            return false;
        if (token.IsEscaped)
            return true;
        return token.Word switch
        {
            Word.Yield => !inYield,
            Word.Await => !inAwait,
            _ => !IsReserved(token.Word),
        };
    }

    /// <summary>A PropertyName (clause 13.2.5), or a ClassElementName (clause 15.7) with a PrivateIdentifier where <paramref name="allowPrivate"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=72993B
    // Broiler-Human:        PENDING
    private SyntaxNode ParsePropertyName(bool allowPrivate)
    {
        var token = Op;
        switch (token.Kind)
        {
            case TokenKind.IdentifierName:
                Advance();
                return new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
            case TokenKind.StringLiteral:
                Advance();
                return StringLiteralOf(token);
            case TokenKind.NumericLiteral:
                Advance();
                return NumericLiteralOf(token);
            case TokenKind.PrivateIdentifier when allowPrivate:
                Advance();
                return new PrivateIdentifier(SpanOf(token), token.Value!, token.IsEscaped);
            case TokenKind.LeftBracket:
            {
                var start = StartOf(Advance());
                var expression = ParseAssignment(noIn: false);
                Expect(TokenKind.RightBracket);
                return new ComputedPropertyName(SpanFrom(start), expression);
            }

            default:
                throw Unexpected(token);
        }
    }

    /// <summary>
    /// The cover of ArrowFormalParameters a parenthesised list was read as (clause 13.2.10), which only an AssignmentExpression
    /// converts; it never reaches the tree.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=341678
    // Broiler-Human:        PENDING
    private sealed class ArrowParametersCover(SourceSpan span, IReadOnlyList<Expression> items, RestElement? rest, bool trailingComma, int mark)
        : ExpressionCover(span)
    {
        internal IReadOnlyList<Expression> Items { get; } = items;

        internal RestElement? Rest { get; } = rest;

        internal bool TrailingComma { get; } = trailingComma;

        internal int Mark { get; } = mark;
    }

    /// <summary>The CoverCallExpressionAndAsyncArrowHead an <c>async</c> call was read as (clause 15.9), which only an AssignmentExpression converts.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=DAF410
    // Broiler-Human:        PENDING
    private sealed class AsyncArrowHead(CallExpression call, int mark, int awaitMark) : ExpressionCover(call.Span)
    {
        internal CallExpression Call { get; } = call;

        internal int Mark { get; } = mark;

        internal int AwaitMark { get; } = awaitMark;
    }
}

/// <summary>A cover the parse read and an arrow's <c>=&gt;</c> converts: it never reaches the tree.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=D3BDE3
// Broiler-Human:        PENDING
internal abstract class ExpressionCover(SourceSpan span) : Expression(SyntaxKind.ParenthesizedExpression, span)
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions grammar, cover grammars; IP=Low; Security=Medium; Resources=2; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}
