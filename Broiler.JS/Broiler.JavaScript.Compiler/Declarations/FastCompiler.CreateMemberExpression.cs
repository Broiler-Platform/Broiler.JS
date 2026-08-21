using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    /// <summary>
    /// The raw <c>double</c> storage behind a computed key that is a numeric local, or null when
    /// the key is anything else (roadmap item 3-0).
    /// </summary>
    /// <remarks>
    /// A numeric local's readable expression boxes its storage — <c>FastFunctionScope</c> says so
    /// where it builds it — so <c>a[i]</c> allocated a <see cref="JSNumber"/> purely to name a
    /// slot. Measured at ~32 B per indexed read, which is the whole cost of one.
    /// <para>
    /// Offered only to the two lowerings that emit a CALL — the plain read in
    /// <c>VisitMemberExpression</c> and <see cref="TryCreateNumericIndexStore"/> — and the
    /// restriction is load-bearing rather than cautious, because a guarded access cannot be an
    /// index NODE and three places need one. <see cref="CreateMemberAssignmentTarget"/> is
    /// assigned through; <c>InternalUpdateExpression</c> switches on <c>right.NodeType</c> and
    /// takes a different branch for anything that is not <c>BExpressionType.Index</c>; and a
    /// compound assignment reads and writes through a single reference. The first two pass
    /// <c>computed: false</c> so they could not reach this anyway, but destructuring passes
    /// <c>property.Computed</c> straight through and would.
    /// </para>
    /// </remarks>
    private BExpression TryGetNumericKeyStorage(AstExpression property, bool computed)
    {
        if (!computed || property.Type != FastNodeType.Identifier)
            return null;

        return TryGetStaticIdentifierVariable((AstIdentifier)property, out var variable)
            ? variable?.NumericStorage
            : null;
    }

    /// <summary>
    /// The three halves of item 3-8a's speculative local when it is used as a computed key, or
    /// <c>null</c> when the key is not one.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="TryGetNumericKeyStorage"/> and deliberately so: that one answers
    /// "this key IS a raw double", which lets its caller emit one unconditional read. A
    /// speculative key is a raw double only while its flag holds, so its caller has to emit both
    /// arms — and a site that confused the two would read a dead double whenever the speculation
    /// was not live.
    /// </remarks>
    private (BExpression Raw, BExpression Flag, BExpression Slot)? TryGetSpeculativeKeyStorage(
        AstExpression property)
    {
        if (property.Type != FastNodeType.Identifier)
            return null;

        if (!TryGetStaticIdentifierVariable((AstIdentifier)property, out var variable)
            || variable?.SpeculativeNumericFlag == null)
        {
            return null;
        }

        return (variable.SpeculativeNumericStorage, variable.SpeculativeNumericFlag, variable.SpeculativeSlot);
    }

    /// <summary>
    /// Lowers `a[i] = value` to a write that keeps its index unboxed, or returns null when this
    /// member is not a plain numeric-local element write (roadmap item 3-0, write half).
    /// </summary>
    /// <remarks>
    /// The mirror of the read fast path in <c>VisitMemberExpression</c> and eligible on the same
    /// terms — a computed key that resolves to a numeric local, an ordinary base, no `super`, no
    /// optional chain. Emitted as a call rather than an assignment onto an index node, which is
    /// why it is a separate lowering here instead of a change to
    /// <see cref="CreateMemberAssignmentTarget"/>: that reference has to stay assignable.
    /// <para>
    /// Compound assignment (`a[i] += v`) is deliberately excluded and keeps the boxed index. Its
    /// target is read *and* written through one reference, so it needs a real assignable node;
    /// splitting it into a guarded read plus a guarded write would evaluate the base twice unless
    /// the whole form were rebuilt around a temp, which is a larger change than this item.
    /// </para>
    /// <para>
    /// The caller must not have compiled the value expression yet: the emitted call evaluates the
    /// receiver and the key before the right-hand side, which is the order §13.15.2 requires.
    /// </para>
    /// </remarks>
    private BExpression TryCreateNumericIndexStore(AstMemberExpression member, Func<BExpression> compileValue)
    {
        if (member.Coalesce
            || member.InOptionalChain
            || member.Object == null
            || member.Object.Type == FastNodeType.Super)
        {
            return null;
        }

        var index = TryGetNumericKeyStorage(member.Property, member.Computed);
        if (index != null)
        {
            var numericTarget = VisitExpression(member.Object);
            return JSValueBuilder.SetIndexByNumber(numericTarget, index, compileValue());
        }

        // Item 3-8a: the same write keyed by a SPECULATIVE local. The read half of the item was
        // measured without this and came out a regression on NavierStokes, whose inner loop is
        //
        //     lastX = x[currentRow] = (x0[currentRow] + a * (...)) * invC;
        //
        // — the local is read four ways and WRITTEN through once, and the one write was boxing an
        // index the three reads no longer box.
        if (!member.Computed)
            return null;

        if (TryGetSpeculativeKeyStorage(member.Property) is not { } speculative)
            return null;

        // Receiver, then value, then the store — the order §13.15.2 asks for, given that the key
        // "evaluation" here is a pair of register reads that cannot be observed. Both are bound to
        // temps because both arms need them and `compileValue` may be called only once: it is a
        // compilation, not a value, and running it twice would emit the right-hand side twice and
        // mint a second inline-cache site for it.
        using var receiver = scope.Top.GetTempVariable(typeof(JSValue));
        using var stored = scope.Top.GetTempVariable(typeof(JSValue));
        return BExpression.Block(
            BExpression.Assign(receiver.Expression, VisitExpression(member.Object)),
            BExpression.Assign(stored.Expression, ToJSValueExpression(compileValue())),
            BExpression.Condition(
                speculative.Flag,
                JSValueBuilder.SetIndexByNumber(receiver.Expression, speculative.Raw, stored.Expression),
                BExpression.Assign(
                    JSValueBuilder.Index(receiver.Expression, speculative.Slot),
                    stored.Expression),
                typeof(JSValue)));
    }

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

        // The read cache's description, on the write side: `config.server.tls.enabled = true`
        // against a nullish `config.server.tls` reports the expression it was written as. The key
        // is a constant KeyString by the guards above, so the property name is the member's own
        // identifier or string literal (NullishAccess).
        return JSValueBuilder.CachedStore(
            target, key, compileValue(), member.AccessCode(), PropertyNameText(member.Property));
    }

    /// <summary>
    /// The source text of a non-computed property name, for a site description. Empty for anything
    /// whose key is not a compile-time constant, which records no description.
    /// </summary>
    private static StringSpan PropertyNameText(AstExpression property) => property switch
    {
        AstIdentifier id => id.Name,
        AstLiteral { TokenType: TokenTypes.String } literal => literal.StringValue,
        _ => default,
    };
}
