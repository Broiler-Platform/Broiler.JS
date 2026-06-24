using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    public AstExpression Combine(AstExpression left, TokenTypes type, AstExpression right, TokenTypes next = TokenTypes.SemiColon)
    {
        if (right == null)
            return left;

        switch (type)
        {
            case TokenTypes.SemiColon:
            case TokenTypes.EOF:
            case TokenTypes.BracketEnd:
            case TokenTypes.SquareBracketEnd:
            case TokenTypes.CurlyBracketEnd:
            case TokenTypes.LineTerminator:
                return left;

            case TokenTypes.QuestionMark:
                if (next != TokenTypes.Colon)
                    throw stream.Unexpected();

                if (!Expression(out var @false))
                    throw stream.Unexpected();

                return new AstConditionalExpression(left, right, @false);
        }

        if (type == TokenTypes.Dot)
            return new AstMemberExpression(left, right);

        if (type == TokenTypes.QuestionDot)
            return new AstMemberExpression(left, right, false, true);

        // A PrivateIdentifier may only be the LEFT operand of `in` (the brand check, handled by the
        // compiler) — never any other binary operand. This rejects `1 + #x in o`, where `#x` is
        // parsed as `+`'s right operand even though `in` textually follows it. (Member access
        // `obj.#x` is parsed in SingleMemberExpression and never reaches here as a `#`-identifier.)
        if (right is AstIdentifier rightId && rightId.Name.Length > 0 && rightId.Name.Value[0] == '#')
            throw new FastParseException(right.Start,
                "Private identifier is only allowed as the left operand of 'in'");

        // The left operand of `**` must be an UpdateExpression: an unparenthesised
        // unary expression (-x, +x, !x, ~x, typeof/void/delete x) is a SyntaxError,
        // because its precedence relative to `**` is ambiguous. Prefix/postfix
        // ++/-- are UpdateExpressions and remain valid, and parentheses
        // (`(-2) ** 2`) disambiguate.
        if (type == TokenTypes.Power
            && left is AstUnaryExpression unary
            && !unary.WasParenthesized
            && unary.Operator != UnaryOperator.Increment
            && unary.Operator != UnaryOperator.Decrement)
        {
            throw new FastParseException(left.Start,
                "Unary operator used immediately before exponentiation expression; parentheses must be used to disambiguate operator precedence");
        }

        // `??` is a ShortCircuitExpression sibling of `||`/`&&` and may not be
        // combined with either without parentheses: `a ?? b || c`, `a || b ?? c`
        // (and the `&&` forms) are early SyntaxErrors. Parenthesising one side
        // (`(a ?? b) || c`, `a ?? (b || c)`) is required and allowed.
        static bool IsUnparenthesizedLogical(AstExpression e)
            => e is AstBinaryExpression { WasParenthesized: false } b
                && (b.Operator == TokenTypes.BooleanOr || b.Operator == TokenTypes.BooleanAnd);

        static bool IsUnparenthesizedCoalesce(AstExpression e)
            => e is AstBinaryExpression { WasParenthesized: false, Operator: TokenTypes.Coalesce };

        if (type == TokenTypes.Coalesce
            ? IsUnparenthesizedLogical(left) || IsUnparenthesizedLogical(right)
            : (type == TokenTypes.BooleanOr || type == TokenTypes.BooleanAnd)
                && (IsUnparenthesizedCoalesce(left) || IsUnparenthesizedCoalesce(right)))
        {
            throw new FastParseException(left.Start,
                "Cannot mix the nullish coalescing operator '??' with '||' or '&&'; wrap an operand in parentheses to disambiguate");
        }

        return new AstBinaryExpression(left, type, right);
    }


    FastToken lastNextExpressionPosition;

    /// <summary>
    /// NextExpression evaluates and reads next set of tokens,
    /// It decides precedence of right side expression and combines
    /// expressions and returns true/false if expression has parsed successfully.
    /// 
    /// It will return true if expression ends successfully.
    /// 
    /// It will return true if no computable expression is found. It will only
    /// return false if parser does not find a valid expression, but it could be
    /// parsed by some other rule.
    /// 
    /// It will rewrite next token as semicolon.
    /// 
    /// This uses recursive calls as using inbuilt .net stack is much easier rather
    /// than using our own stack.
    /// 
    /// There might be some issue with stack overflow.. we will revisit it when needed.
    /// In that case we might introduce local stack.
    /// </summary>
    /// <param name="previous"></param>
    /// <param name="previousType"></param>
    /// <param name="node"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    bool NextExpression(ref AstExpression previous, ref TokenTypes previousType, out AstExpression node, out TokenTypes type, int depth = 0, int floor = int.MaxValue)
    {
        switch (previousType)
        {
            /**
             * Following are single expression terminators
             */

            case TokenTypes.Comma:
            case TokenTypes.LineTerminator:
            case TokenTypes.SemiColon:
            case TokenTypes.SquareBracketEnd:
            case TokenTypes.BracketEnd:
            case TokenTypes.CurlyBracketEnd:
            case TokenTypes.CurlyBracketStart:
            case TokenTypes.Colon:
            case TokenTypes.EOF:
            case TokenTypes.TemplateBegin:
            case TokenTypes.TemplatePart:
            case TokenTypes.TemplateEnd:
                node = null;
                type = TokenTypes.SemiColon;
                return true;

            case TokenTypes.In:
                if (!considerInOfAsOperators)
                {
                    node = null;
                    type = TokenTypes.SemiColon;

                    return true;
                }
                break;
        }

        PreventStackoverFlow(ref lastNextExpressionPosition);

        AstExpression right;

        switch (previousType)
        {
            // Associate right...
            case TokenTypes.Assign:
            case TokenTypes.AssignAdd:
            case TokenTypes.AssignBitwideAnd:
            case TokenTypes.AssignBitwideOr:
            case TokenTypes.AssignDivide:
            case TokenTypes.AssignLeftShift:
            case TokenTypes.AssignMod:
            case TokenTypes.AssignMultiply:
            case TokenTypes.AssignBooleanAnd:
            case TokenTypes.AssignBooleanOr:
            case TokenTypes.AssignCoalesce:
            case TokenTypes.AssignPower:
            case TokenTypes.AssignRightShift:
            case TokenTypes.AssignSubtract:
            case TokenTypes.AssignUnsignedRightShift:
            case TokenTypes.AssignXor:
                stream.Consume();
                if (!Expression(out right))
                    throw stream.Unexpected();

                // left must be converted to asssignable...
                if (previous.Type == FastNodeType.ArrayExpression || previous.Type == FastNodeType.ObjectLiteral)
                    previous = previous.ToPattern();

                previous = Combine(previous, previousType, right);

                node = null;
                type = TokenTypes.SemiColon;

                return true;

            case TokenTypes.QuestionMark:
                stream.CheckAndConsume(previousType);

                // The consequent of a ConditionalExpression is an
                // AssignmentExpression[+In]: `in` is always a valid operator
                // there, even inside a for-head where it is otherwise suppressed
                // (`for (a ? b in c : d; …)`). The alternative is [?In] and keeps
                // the surrounding setting.
                var savedInConsequent = considerInOfAsOperators;
                considerInOfAsOperators = true;
                var okConsequent = Expression(out var @true);
                considerInOfAsOperators = savedInConsequent;
                if (!okConsequent)
                    throw stream.Unexpected();

                stream.Expect(TokenTypes.Colon);
                if (!Expression(out var @false))
                    throw stream.Unexpected();

                previous = new AstConditionalExpression(previous, @true, @false);
                previousType = stream.Current.Type;

                if (stream.Previous.Type == TokenTypes.SemiColon)
                {
                    node = null;
                    type = TokenTypes.SemiColon;
                    return true;
                }

                return NextExpression(ref previous, ref previousType, out node, out type, depth + 1);
        }

        stream.CheckAndConsume(previousType);
        stream.SkipNewLines();

        if (!SinglePrefixPostfixExpression(out node, out var x, out var b))
        {
            if (EndOfStatement())
            {
                type = TokenTypes.SemiColon;
                return true;
            }

            type = TokenTypes.None;
            return false;
        }

        var m = stream.SkipNewLines();

        var begin = stream.Current;
        type = begin.Type;

        if (node.End.Type == TokenTypes.SemiColon)
        {
            type = TokenTypes.SemiColon;
            return true;
        }

        if (m.LinesSkipped && !type.IsOperator())
        {
            // ASI applies: the line break terminates this expression. Undo the new-line skip so
            // the line terminator stays in the token stream for the caller's EndOfStatement to
            // consume (class fields, for example, REQUIRE a trailing `;` / LineTerminator / `}`
            // and will otherwise see the next field's leading identifier and reject it — test262
            // language/{expressions,statements}/class/elements/fields-asi-5).
            m.Undo();
            type = TokenTypes.SemiColon;
            return true;
        }

        switch (type)
        {
            case TokenTypes.Comma:
            case TokenTypes.LineTerminator:
            case TokenTypes.SemiColon:
            case TokenTypes.SquareBracketEnd:
            case TokenTypes.BracketEnd:
            case TokenTypes.CurlyBracketEnd:
            case TokenTypes.Colon:
            case TokenTypes.EOF:
                // previous = previous.Combine(previousType, node);
                // node = null;
                type = TokenTypes.SemiColon;
                return true;

            // The `}` closing a template substitution ends the substitution expression.
            // For the LAST substitution it scans as TemplateEnd; for any EARLIER one it
            // scans as TemplatePart (the literal text up to the next `${`). Both terminate
            // the expression here — without the TemplatePart case a non-final substitution's
            // trailing binary operand was dropped (`` `${a + b}-${c}` `` parsed the first
            // substitution as just `a`).
            case TokenTypes.TemplatePart:
            case TokenTypes.TemplateEnd:
                return true;

            // associate right...
            case TokenTypes.Assign:
            case TokenTypes.AssignAdd:
            case TokenTypes.AssignBitwideAnd:
            case TokenTypes.AssignBitwideOr:
            case TokenTypes.AssignDivide:
            case TokenTypes.AssignLeftShift:
            case TokenTypes.AssignMod:
            case TokenTypes.AssignMultiply:
            case TokenTypes.AssignBooleanAnd:
            case TokenTypes.AssignBooleanOr:
            case TokenTypes.AssignCoalesce:
            case TokenTypes.AssignPower:
            case TokenTypes.AssignRightShift:
            case TokenTypes.AssignSubtract:
            case TokenTypes.AssignUnsignedRightShift:
            case TokenTypes.AssignXor:
                throw new FastParseException(begin, $"Invalid left hand side assignemnt at {begin.Start}");

            case TokenTypes.QuestionMark:

                // we should not take a decision here
                // pass it on to previous expression...

                stream.Consume();
                if (depth == 0)
                {
                    previous = Combine(previous, previousType, node);

                    // Consequent is [+In] (see the QuestionMark case above);
                    // re-enable `in` while parsing it so a for-head's `[~In]`
                    // does not leak into the ternary branch.
                    var savedIn = considerInOfAsOperators;
                    considerInOfAsOperators = true;
                    var ok = Expression(out var @true);
                    considerInOfAsOperators = savedIn;
                    if (!ok)
                        throw stream.Unexpected();

                    stream.Expect(TokenTypes.Colon);
                    if (!Expression(out var @false))
                        throw stream.Unexpected();

                    previous = new AstConditionalExpression(previous, @true, @false);
                    
                    // end of expression ??
                    // previousType = stream.Current.Type;
                    // return NextExpression(ref previous, ref previousType, out node, out type);
                    node = null;
                    type = TokenTypes.SemiColon;
                }
                return true;

            case TokenTypes.Multiply:
            case TokenTypes.Divide:
            case TokenTypes.Mod:
            case TokenTypes.Plus:
            case TokenTypes.Minus:
            case TokenTypes.BitwiseAnd:
            case TokenTypes.BitwiseOr:
            case TokenTypes.BitwiseNot:
            case TokenTypes.BooleanAnd:
            case TokenTypes.BooleanOr:
            case TokenTypes.Coalesce:
            case TokenTypes.Xor:
            case TokenTypes.LeftShift:
            case TokenTypes.RightShift:
            case TokenTypes.UnsignedRightShift:
            case TokenTypes.Less:
            case TokenTypes.LessOrEqual:
            case TokenTypes.Greater:
            case TokenTypes.GreaterOrEqual:
            case TokenTypes.In:
            case TokenTypes.InstanceOf:
            case TokenTypes.StrictlyEqual:
            case TokenTypes.StrictlyNotEqual:
            case TokenTypes.Equal:
            case TokenTypes.NotEqual:
            case TokenTypes.Power:

                // check type first as it may be in
                // recent memory accessed...
                if (type == TokenTypes.In)
                {
                    if (!considerInOfAsOperators)
                    {
                        type = TokenTypes.SemiColon;
                        return true;
                    }
                }

                // Precedence floor: this operator binds looser than (or equal to,
                // for a left-associative floor) the operator whose right operand we
                // are building, so it belongs to an outer level. Leave it unconsumed
                // and hand `node`/`type` back so the caller combines at the right
                // precedence (test262 S11.6.1_A4_T7: `a/b + c/d === e`).
                if (OperatorPrecedence(type) >= floor)
                    return true;

                stream.Consume();
                do
                {
                    if (Precedes(type, previousType))
                    {
                        if (!NextExpression(ref node, ref type, out right, out TokenTypes rightType, depth + 1, RightOperandFloor(previousType)))
                            break;

                        if (type == TokenTypes.SemiColon)
                            return true;

                        node = Combine(node, type, right);
                        type = rightType;

                        if (type == TokenTypes.SemiColon)
                            break;

                        // A looser operator returned from the right-operand recursion
                        // belongs to the caller's level: stop and hand it back.
                        if (OperatorPrecedence(type) >= floor)
                            return true;

                        continue;
                    }

                    previous = Combine(previous, previousType, node);
                    previousType = type;

                    if (!NextExpression(ref previous, ref previousType, out node, out type, depth + 1, RightOperandFloor(previousType)))
                        break;

                    if (type == TokenTypes.SemiColon)
                        return true;

                    // The next operator after this folded operand may belong to an
                    // outer level (looser than our floor): hand it back rather than
                    // chaining it here.
                    if (OperatorPrecedence(type) >= floor)
                        return true;
                } while (true);

                return true;
            
            default:
                return false;
        }
    }

    static bool Precedes(TokenTypes left, TokenTypes right)
    {
        if (left != TokenTypes.SemiColon && left != TokenTypes.EOF)
        {
            // `**` is right-associative: a ** b ** c == a ** (b ** c). When both
            // the incoming and the pending operator are exponentiation, let the
            // incoming one bind into the right operand first (which equal-
            // precedence left-associative handling below would otherwise prevent).
            if (left == TokenTypes.Power && right == TokenTypes.Power)
                return true;

            return OperatorPrecedence(left) < OperatorPrecedence(right);
        }

        return false;
    }

    // Binary operator precedence. Lower number == binds tighter. Exponentiation
    // (`**`) is the tightest binary operator — tighter than multiplicative — so it
    // ranks below 1. A non-operator token yields int.MaxValue (binds loosest), which
    // also serves as the "no floor" sentinel in NextExpression's precedence climb.
    static int OperatorPrecedence(TokenTypes token)
    {
        return token switch
        {
            TokenTypes.Power => 0,
            TokenTypes.Mod or TokenTypes.Divide or TokenTypes.Multiply => 1,
            TokenTypes.Plus or TokenTypes.Minus => 2,
            TokenTypes.LeftShift or TokenTypes.RightShift or TokenTypes.UnsignedRightShift => 3,
            TokenTypes.Less or TokenTypes.LessOrEqual or TokenTypes.Greater or TokenTypes.GreaterOrEqual or TokenTypes.In or TokenTypes.InstanceOf => 4,
            TokenTypes.Equal or TokenTypes.NotEqual or TokenTypes.StrictlyEqual or TokenTypes.StrictlyNotEqual => 5,
            TokenTypes.BitwiseAnd => 7,
            TokenTypes.Xor => 8,
            TokenTypes.BitwiseOr => 9,
            TokenTypes.BooleanAnd => 10,
            TokenTypes.BooleanOr => 11,
            // `??` is a ShortCircuitExpression sibling of `||`/`&&`; its operand
            // is a BitwiseORExpression, so it must bind looser than every other
            // binary operator (a ?? b | c parses as a ?? (b | c)).
            TokenTypes.Coalesce => 12,
            // The conditional operator `?:` binds looser than every binary operator.
            // It is not handled by the binary loop (the QuestionMark entry case builds
            // the ConditionalExpression), but it must still bubble up out of a floored
            // right-operand recursion to the level that owns it — so rank it just looser
            // than `??` rather than leaving it at the int.MaxValue "no floor" sentinel,
            // which would make a floor of int.MaxValue (the top level) swallow it.
            TokenTypes.QuestionMark => 13,
            _ => int.MaxValue,
        };
    }

    // The minimum-precedence floor passed to a recursive NextExpression that builds
    // the right operand of `op`: the recursion may consume operators that bind
    // strictly tighter than `op` (and, for the right-associative `**`, also `op`
    // itself), and must hand any looser operator back to the caller's level. Without
    // this bound the right-operand recursion greedily swallowed looser operators —
    // `a/b + c/d === e` parsed as `a/b + (c/d === e)` (test262 S11.6.1_A4_T7).
    static int RightOperandFloor(TokenTypes op)
        => OperatorPrecedence(op) + (op == TokenTypes.Power ? 1 : 0);
}
