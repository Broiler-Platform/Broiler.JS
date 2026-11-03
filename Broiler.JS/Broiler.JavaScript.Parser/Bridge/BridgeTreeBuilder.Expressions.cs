#nullable enable

using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using AstUnaryOperator = Broiler.JavaScript.Ast.Misc.UnaryOperator;
using UnaryOperator = Broiler.ECMAScript.Syntax.UnaryOperator;

namespace Broiler.JavaScript.Parser.Bridge;

internal sealed partial class BridgeTreeBuilder
{
    private AstExpression Expression(Expression expression)
    {
        switch (expression)
        {
            case Identifier identifier:
                return new AstIdentifier(tokens.Name(identifier.Span.Offset, identifier.Span.Length, identifier.Name, identifier.IsEscaped));
            case ThisExpression:
                return new AstIdentifier(tokens.Name(expression.Span.Offset, expression.Span.Length, "this", escaped: false));
            case SuperExpression:
                return new AstSuper(tokens.Name(expression.Span.Offset, expression.Span.Length, "super", escaped: false));
            case NullLiteral:
                return new AstLiteral(TokenTypes.Null, tokens.Make(TokenTypes.Null, expression.Span.Offset, expression.Span.Length));
            case BooleanLiteral boolean:
            {
                var type = boolean.Value ? TokenTypes.True : TokenTypes.False;
                return new AstLiteral(type, tokens.Make(type, expression.Span.Offset, expression.Span.Length));
            }

            case NumericLiteral number:
                return Number(number);
            case StringLiteral @string:
                return String(@string);
            case RegularExpressionLiteral regex:
            {
                var cooked = BridgeRegularExpressions.Cooked(regex.Pattern, regex.Flags) ?? regex.Pattern;
                var token = tokens.Make(TokenTypes.RegExLiteral, regex.Span.Offset, regex.Span.Length, cooked, regex.Flags);
                return new AstLiteral(TokenTypes.RegExLiteral, token);
            }

            case TemplateLiteral template:
                return Template(template);
            case TaggedTemplateExpression or MemberExpression or CallExpression:
                return Link(expression, out _);
            case ChainExpression chain:
                return new AstOptionalChain(Link(chain.Expression, out _));
            case ArrayLiteral array:
                return Array(array);
            case ObjectLiteral @object:
                return Object(@object);
            case ParenthesizedExpression parenthesized:
                return Parenthesized(parenthesized);
            case FunctionExpression function:
                return Function(function.Function, isStatement: false, ifClause: false);
            case ArrowFunctionExpression arrow:
                return Arrow(arrow.Function);
            case ClassExpression @class:
                return Class(@class.Class, isDeclaration: false);
            case NewExpression @new:
                return New(@new);
            case MetaProperty meta:
            {
                var head = tokens.Name(meta.Meta.Span.Offset, meta.Meta.Span.Length, meta.Meta.Name, meta.Meta.IsEscaped);
                var property = tokens.Name(meta.Property.Span.Offset, meta.Property.Span.Length, meta.Property.Name, meta.Property.IsEscaped);
                return new AstMeta(new AstIdentifier(BridgeTokens.AsString(head)), new AstIdentifier(property));
            }

            case ImportCall import:
            {
                var start = Start(import);
                var source = Expression(import.Source);
                var options = import.Options is null ? null : Expression(import.Options);
                return new AstImportCall(start, source, options!, (options ?? source).End);
            }

            case UpdateExpression update:
            {
                var operand = Expression(update.Operand);
                var at = update.IsPrefix ? update.Span.Offset : EndOf(update) - 2;
                var token = tokens.Make(update.Operator == UpdateOperator.Increment ? TokenTypes.Increment : TokenTypes.Decrement, at, 2);
                var op = update.Operator == UpdateOperator.Increment ? AstUnaryOperator.Increment : AstUnaryOperator.Decrement;
                return new AstUnaryExpression(token, operand, op, update.IsPrefix);
            }

            case UnaryExpression unary:
            {
                var token = Start(unary);
                var operand = Expression(unary.Operand);
                return new AstUnaryExpression(token, operand, Unary(unary.Operator));
            }

            case AwaitExpression @await:
            {
                var token = Start(@await);
                var argument = Expression(@await.Argument);
                return new AstAwaitExpression(token, End(@await), argument);
            }

            case YieldExpression @yield:
            {
                var token = Start(@yield);
                var argument = @yield.Argument is null ? null : Expression(@yield.Argument);
                return new AstYieldExpression(token, End(@yield), argument!, @yield.IsDelegate);
            }

            case BinaryExpression binary:
            {
                var left = Expression(binary.Left);
                var right = Expression(binary.Right);
                return new AstBinaryExpression(left, Binary(binary.Operator), right);
            }

            case LogicalExpression logical:
            {
                var left = Expression(logical.Left);
                var right = Expression(logical.Right);
                var op = logical.Operator switch
                {
                    LogicalOperator.And => TokenTypes.BooleanAnd,
                    LogicalOperator.Or => TokenTypes.BooleanOr,
                    _ => TokenTypes.Coalesce,
                };
                return new AstBinaryExpression(left, op, right);
            }

            case PrivateInExpression privateIn:
            {
                var name = PrivateName(privateIn.Name);
                var right = Expression(privateIn.Right);
                return new AstBinaryExpression(name, TokenTypes.In, right);
            }

            case ConditionalExpression conditional:
            {
                var test = Expression(conditional.Test);
                var consequent = Expression(conditional.Consequent);
                var alternate = Expression(conditional.Alternate);
                return new AstConditionalExpression(test, consequent, alternate);
            }

            case AssignmentExpression assignment:
            {
                var target = assignment.Target switch
                {
                    Pattern pattern => CoverPattern(pattern),
                    Expression simple => Expression(simple),
                    _ => throw Unsupported(assignment.Target),
                };
                var value = Expression(assignment.Value);
                return new AstBinaryExpression(target, Assignment(assignment.Operator), value);
            }

            case SequenceExpression sequence:
            {
                var list = Expressions(sequence.Expressions);
                return new AstSequenceExpression(Start(sequence), End(sequence), list);
            }

            case SpreadElement spread:
                return Spread(spread);
            default:
                throw Unsupported(expression);
        }
    }

    private Sequence<AstExpression> Expressions(IReadOnlyList<Expression> expressions)
    {
        var list = new Sequence<AstExpression>(expressions.Count);
        foreach (var item in expressions)
            list.Add(Expression(item));
        return list;
    }

    private AstSpreadElement Spread(SpreadElement spread)
    {
        var token = Start(spread);
        var argument = Expression(spread.Argument);
        return new AstSpreadElement(token, argument.End, argument);
    }

    private AstLiteral Number(NumericLiteral number)
    {
        var offset = number.Span.Offset;
        var length = number.Span.Length;
        if ((number.Flags & NumericLiteralFlags.BigInt) != 0)
            return new AstLiteral(TokenTypes.BigInt, tokens.Make(TokenTypes.BigInt, offset, length));
        if ((number.Flags & NumericLiteralFlags.DecimalSuffix) != 0)
            return new AstLiteral(TokenTypes.Decimal, tokens.Make(TokenTypes.Decimal, offset, length));
        return new AstLiteral(TokenTypes.Number, tokens.Make(TokenTypes.Number, offset, length, number: number.Value));
    }

    private AstLiteral String(StringLiteral @string)
        => new(TokenTypes.String, tokens.Make(TokenTypes.String, @string.Span.Offset, @string.Span.Length, @string.Value));

    private AstIdentifier PrivateName(PrivateIdentifier name)
        => new(tokens.Make(TokenTypes.Hash, name.Span.Offset, 1), name.Name);

    // A parenthesised expression is the expression marked as parenthesised, and a parenthesised sequence one that
    // spans its parentheses (the old parser's BracketExpression). The parentheses join the token chain.
    private AstExpression Parenthesized(ParenthesizedExpression parenthesized)
    {
        var open = tokens.Make(TokenTypes.BracketStart, parenthesized.Span.Offset, 1);
        var close = tokens.Make(TokenTypes.BracketEnd, EndOf(parenthesized) - 1, 1);
        AstExpression node = parenthesized.Expression is SequenceExpression sequence
            ? new AstSequenceExpression(open, close, Expressions(sequence.Expressions))
            : Expression(parenthesized.Expression);
        node.WasParenthesized = true;
        return node;
    }

    private AstArrayExpression Array(ArrayLiteral array)
    {
        var start = Start(array);
        var list = new Sequence<AstExpression>(array.Elements.Count);
        foreach (var element in array.Elements)
            list.Add(element is null ? null! : Expression(element));
        return new AstArrayExpression(start, End(array), list);
    }

    private AstObjectLiteral Object(ObjectLiteral @object)
    {
        var start = Start(@object);
        var list = new Sequence<AstNode>(@object.Properties.Count);
        foreach (var property in @object.Properties)
        {
            switch (property)
            {
                case SpreadElement spread:
                    list.Add(Spread(spread));
                    break;
                case PropertyDefinition definition:
                    list.Add(Property(definition));
                    break;
                default:
                    throw Unsupported(property);
            }
        }

        return new AstObjectLiteral(start, End(@object), list);
    }

    // An object literal's entry, as the old parser's ObjectProperty made it. The name is built first, so that the
    // entry's first token, where the name begins it, is the name's own, with its value.
    private AstClassProperty Property(PropertyDefinition definition)
    {
        var (key, computed, isPrivate) = Key(definition.Key);
        var current = Start(definition);
        switch (definition.PropertyKind)
        {
            case PropertyKind.Shorthand:
                return new AstClassProperty(current, End(definition), AstPropertyKind.Data, isPrivate, false, key, computed, key);
            case PropertyKind.Init:
            {
                var value = Expression(definition.Value);
                return new AstClassProperty(current, End(definition), AstPropertyKind.Data, isPrivate, false, key, computed, value, usesColon: true);
            }

            default:
            {
                var function = Method(((FunctionExpression)definition.Value).Function, EndOf(definition.Key));
                var kind = definition.PropertyKind switch
                {
                    PropertyKind.Get => AstPropertyKind.Get,
                    PropertyKind.Set => AstPropertyKind.Set,
                    _ => AstPropertyKind.Method,
                };
                return new AstClassProperty(current, function.End, kind, isPrivate, false, key, computed, function);
            }
        }
    }

    /// <summary>A property's or a class element's name, as the old parser's PropertyName read it.</summary>
    private (AstExpression Key, bool Computed, bool IsPrivate) Key(SyntaxNode key)
    {
        switch (key)
        {
            case IdentifierName name:
            {
                var token = tokens.Name(name.Span.Offset, name.Span.Length, name.Name, name.IsEscaped);
                return (new AstIdentifier(token.ContextualKeyword != FastKeywords.none ? token : BridgeTokens.AsString(token)), false, false);
            }

            case StringLiteral @string:
                return (String(@string), false, false);
            case NumericLiteral number:
                return (Number(number), false, false);
            case ComputedPropertyName computed:
                return (Expression(computed.Expression), true, false);
            case PrivateIdentifier @private:
                return (PrivateName(@private), false, true);
            default:
                throw Unsupported(key);
        }
    }

    // A new expression begins at its `new`, where its text begins; its end is its callee's (AstNewExpression's), as in
    // the old parser, which began it at the token after the callee instead.
    private AstExpression New(NewExpression @new)
    {
        var start = Start(@new);
        var callee = Callee(@new.Callee);
        return new AstNewExpression(start, callee, @new.HasArgumentList ? Expressions(@new.Arguments) : Sequence<AstExpression>.Empty);
    }

    private AstExpression Callee(Expression callee)
        => callee is MemberExpression or CallExpression or TaggedTemplateExpression ? Link(callee, out _) : Expression(callee);

    /// <summary>
    /// A link of a member, call or tagged-template chain, as the old parser's SingleMemberExpression built it left to
    /// right: a link written with <c>?.</c> is marked so (<c>Coalesce</c>), and it and every link after it in its chain
    /// are in the optional chain.
    /// </summary>
    private AstExpression Link(Expression expression, out bool inOptional)
    {
        switch (expression)
        {
            case MemberExpression member:
            {
                var @object = Inner(member.Object, out inOptional);
                inOptional |= member.IsOptional;
                AstExpression property = member.Property switch
                {
                    IdentifierName name => new AstIdentifier(BridgeTokens.AsString(tokens.Name(name.Span.Offset, name.Span.Length, name.Name, name.IsEscaped))),
                    PrivateIdentifier @private => PrivateName(@private),
                    Expression computed => Expression(computed),
                    _ => throw Unsupported(member.Property),
                };
                return new AstMemberExpression(@object, property, member.IsComputed, member.IsOptional, inOptional);
            }

            case CallExpression call:
            {
                var callee = Inner(call.Callee, out inOptional);
                inOptional |= call.IsOptional;
                return new AstCallExpression(callee, Expressions(call.Arguments), call.IsOptional, inOptional);
            }

            case TaggedTemplateExpression tagged:
            {
                var tag = Inner(tagged.Tag, out inOptional);
                return new AstTaggedTemplateExpression(tag, TemplateParts(tagged.Quasi));
            }

            default:
                inOptional = false;
                return Expression(expression);
        }

        AstExpression Inner(Expression inner, out bool optional)
        {
            if (inner is MemberExpression or CallExpression or TaggedTemplateExpression)
                return Link(inner, out optional);
            optional = false;
            return Expression(inner);
        }
    }

    // A template literal: one piece of text with no substitution is a single literal of the closing kind, otherwise
    // the pieces and the substitutions in turn, every piece a TemplatePart literal over its own token.
    private AstTemplateExpression Template(TemplateLiteral template)
    {
        var parts = TemplateParts(template);
        return new AstTemplateExpression(parts[0].Start, parts[parts.Count - 1].End, parts);
    }

    private Sequence<AstExpression> TemplateParts(TemplateLiteral template)
    {
        var quasis = template.Quasis;
        var parts = new Sequence<AstExpression>(quasis.Count * 2);
        if (quasis.Count == 1)
        {
            parts.Add(new AstLiteral(TokenTypes.TemplateEnd, Piece(quasis[0], TokenTypes.TemplateEnd)));
            return parts;
        }

        for (var i = 0; i < quasis.Count; i++)
        {
            var type = i == 0 ? TokenTypes.TemplateBegin : i == quasis.Count - 1 ? TokenTypes.TemplateEnd : TokenTypes.TemplatePart;
            parts.Add(new AstLiteral(TokenTypes.TemplatePart, Piece(quasis[i], type)));
            if (i < template.Expressions.Count)
                parts.Add(Expression(template.Expressions[i]));
        }

        return parts;
    }

    private FastToken Piece(TemplateElement piece, TokenTypes type)
        => tokens.Make(type, piece.Span.Offset, piece.Span.Length, piece.Cooked ?? string.Empty, cookedInvalid: piece.Cooked is null);

    private static TokenTypes Binary(BinaryOperator op) => op switch
    {
        BinaryOperator.Exponent => TokenTypes.Power,
        BinaryOperator.Multiply => TokenTypes.Multiply,
        BinaryOperator.Divide => TokenTypes.Divide,
        BinaryOperator.Remainder => TokenTypes.Mod,
        BinaryOperator.Add => TokenTypes.Plus,
        BinaryOperator.Subtract => TokenTypes.Minus,
        BinaryOperator.LeftShift => TokenTypes.LeftShift,
        BinaryOperator.SignedRightShift => TokenTypes.RightShift,
        BinaryOperator.UnsignedRightShift => TokenTypes.UnsignedRightShift,
        BinaryOperator.LessThan => TokenTypes.Less,
        BinaryOperator.GreaterThan => TokenTypes.Greater,
        BinaryOperator.LessThanOrEqual => TokenTypes.LessOrEqual,
        BinaryOperator.GreaterThanOrEqual => TokenTypes.GreaterOrEqual,
        BinaryOperator.Instanceof => TokenTypes.InstanceOf,
        BinaryOperator.In => TokenTypes.In,
        BinaryOperator.Equal => TokenTypes.Equal,
        BinaryOperator.NotEqual => TokenTypes.NotEqual,
        BinaryOperator.StrictEqual => TokenTypes.StrictlyEqual,
        BinaryOperator.StrictNotEqual => TokenTypes.StrictlyNotEqual,
        BinaryOperator.BitwiseAnd => TokenTypes.BitwiseAnd,
        BinaryOperator.BitwiseXor => TokenTypes.Xor,
        BinaryOperator.BitwiseOr => TokenTypes.BitwiseOr,
        _ => throw new System.ArgumentOutOfRangeException(nameof(op), op, "Not a binary operator."),
    };

    private static TokenTypes Assignment(AssignmentOperator op) => op switch
    {
        AssignmentOperator.Assign => TokenTypes.Assign,
        AssignmentOperator.MultiplyAssign => TokenTypes.AssignMultiply,
        AssignmentOperator.DivideAssign => TokenTypes.AssignDivide,
        AssignmentOperator.RemainderAssign => TokenTypes.AssignMod,
        AssignmentOperator.AddAssign => TokenTypes.AssignAdd,
        AssignmentOperator.SubtractAssign => TokenTypes.AssignSubtract,
        AssignmentOperator.LeftShiftAssign => TokenTypes.AssignLeftShift,
        AssignmentOperator.SignedRightShiftAssign => TokenTypes.AssignRightShift,
        AssignmentOperator.UnsignedRightShiftAssign => TokenTypes.AssignUnsignedRightShift,
        AssignmentOperator.BitwiseAndAssign => TokenTypes.AssignBitwideAnd,
        AssignmentOperator.BitwiseXorAssign => TokenTypes.AssignXor,
        AssignmentOperator.BitwiseOrAssign => TokenTypes.AssignBitwideOr,
        AssignmentOperator.ExponentAssign => TokenTypes.AssignPower,
        AssignmentOperator.AndAssign => TokenTypes.AssignBooleanAnd,
        AssignmentOperator.OrAssign => TokenTypes.AssignBooleanOr,
        AssignmentOperator.CoalesceAssign => TokenTypes.AssignCoalesce,
        _ => throw new System.ArgumentOutOfRangeException(nameof(op), op, "Not an assignment operator."),
    };

    private static AstUnaryOperator Unary(UnaryOperator op) => op switch
    {
        UnaryOperator.Delete => AstUnaryOperator.delete,
        UnaryOperator.Void => AstUnaryOperator.@void,
        UnaryOperator.Typeof => AstUnaryOperator.@typeof,
        UnaryOperator.Plus => AstUnaryOperator.Plus,
        UnaryOperator.Minus => AstUnaryOperator.Minus,
        UnaryOperator.BitwiseNot => AstUnaryOperator.BitwiseNot,
        UnaryOperator.LogicalNot => AstUnaryOperator.Negate,
        _ => throw new System.ArgumentOutOfRangeException(nameof(op), op, "Not a unary operator."),
    };
}
