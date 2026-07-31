using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private BExpression CreatePropertyKeyExpression(AstExpression property, bool computed)
    {
        switch (property.Type)
        {
            case FastNodeType.Identifier:
                var id = (AstIdentifier)property;
                if (computed)
                    return VisitIdentifier(id);
                // `.#x` is a private member reference; key it in the private namespace.
                return id.Name.Length > 0 && id.Name.Value[0] == '#'
                    ? KeyOfPrivateName(id.Name)
                    : KeyOfName(id.Name);

            case FastNodeType.Literal:
                var l = (AstLiteral)property;
                switch (l.TokenType)
                {
                    case TokenTypes.True:
                        return computed ? VisitLiteral(l) : KeyOfName(l.StringValue);

                    case TokenTypes.False:
                        return computed ? VisitLiteral(l) : KeyOfName(l.StringValue);

                    case TokenTypes.String:
                        return computed ? VisitLiteral(l) : KeyOfName(l.Start.CookedText);

                    case TokenTypes.Number:
                        if (l.NumericValue >= 0
                            && l.NumericValue < uint.MaxValue
                            && (l.NumericValue % 1 == 0))
                            return BExpression.Constant((uint)l.NumericValue);

                        return VisitLiteral(l);

                    case TokenTypes.BigInt:
                        // A BigInt destructuring key (`let { 1n: a } = o`) keys by the
                        // canonical value of its numeric part.
                        return BigIntPropertyKey(l.StringValue);

                    default:
                        if (computed)
                            return VisitLiteral(l);
                        throw new NotImplementedException();
                }

            case FastNodeType.MemberExpression:
                var se = (AstMemberExpression)property;
                // A computed key such as obj[Symbol.iterator] evaluates the complete
                // member expression. Visiting only `.Property` incorrectly resolves
                // `iterator` as a standalone identifier.
                return Visit(se);
        }

        if (computed)
            return Visit(property);

        throw new NotImplementedException();
    }

    private BExpression CreateMemberExpression(BExpression target, AstExpression property, bool computed)
    {
        var key = CreatePropertyKeyExpression(property, computed);
        if (key.Type == typeof(KeyString) || key.Type == typeof(uint) || key.Type == typeof(int) || key.Type.IsJSValueType())
            return JSValueBuilder.Index(target, key);

        throw new NotImplementedException();
    }

    /// <summary>
    /// Builds an assignable member reference without routing the read through an
    /// inline-cache helper call. Destructuring and for-in/of heads share this path
    /// with ordinary assignment writes.
    /// </summary>
    private BExpression CreateMemberAssignmentTarget(AstMemberExpression member)
    {
        var key = CreatePropertyKeyExpression(member.Property, member.Computed);
        return member.Object.Type == FastNodeType.Super
            ? JSValueBuilder.Index(scope.Top.ThisExpression, scope.Top.Super, key, member.Coalesce)
            : JSValueBuilder.Index(VisitExpression(member.Object), key, member.Coalesce);
    }

    /// <summary>
    /// Lowers `obj.name = value` to a store inline cache, or returns null when this member is
    /// not a plain constant-key write and has to keep the ordinary assignable index reference.
    /// </summary>
    /// <remarks>
    /// The mirror image of the read cache in
    /// <see cref="VisitMemberExpression"/>, and eligible on the same terms: a constant
    /// <see cref="KeyString"/> key, an ordinary base, and no super or optional-chain
    /// involvement. A private name is excluded because `obj.#x = v` is a brand check rather
    /// than an ordinary [[Set]], and a computed key is excluded because it would drive one
    /// site through every key the expression produces (see docs/performance-roadmap.md P1-3).
    /// <para>
    /// The caller must not have compiled the value expression yet: the emitted call evaluates
    /// the base before the value, and the tree has to be built in that order.
    /// </para>
    /// </remarks>
    private BExpression TryCreateCachedMemberStore(AstMemberExpression member, Func<BExpression> compileValue)
    {
        if (member.Computed
            || member.Coalesce
            || member.InOptionalChain
            || member.Object == null
            || member.Object.Type == FastNodeType.Super)
        {
            return null;
        }

        if (member.Property is AstIdentifier { Name.Length: > 0 } id && id.Name.Value[0] == '#')
            return null;

        var key = CreatePropertyKeyExpression(member.Property, false);
        if (key.Type != typeof(KeyString))
            return null;

        var target = VisitExpression(member.Object);
        return JSValueBuilder.CachedStore(target, key, compileValue());
    }
}
