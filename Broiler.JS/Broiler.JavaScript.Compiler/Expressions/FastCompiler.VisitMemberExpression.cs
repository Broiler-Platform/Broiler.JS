using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitMemberExpression(AstMemberExpression memberExpression)
    {
        var isSuper = memberExpression.Object?.Type == FastNodeType.Super;
        var target = isSuper ? scope.Top.ThisExpression : VisitExpression(memberExpression.Object);
        var super = isSuper ? scope.Top.Super : null;
        var mp = memberExpression.Property;

        // A SuperProperty (super.x / super[x]) is only legal where a [[HomeObject]] is in
        // scope — a method, accessor, or constructor. Outside one (e.g. a Function-constructor
        // body or a plain function) no super reference is resolved, so it is an early
        // SyntaxError rather than a runtime failure. Direct eval validates its own super
        // placement separately (DirectEvalSupport), so it is exempt here.
        if (isSuper && super == null && !isDirectEvalCompilation)
            throw new FastParseException(memberExpression.Start, "'super' keyword is only valid inside a method or constructor");

        // `super[Expression]`: the property-key Expression is evaluated BEFORE the super
        // base is read (MakeSuperPropertyReference evaluates the key, then calls
        // GetSuperBase = [[HomeObject]].[[Prototype]]). Because super is resolved
        // dynamically (SuperPrototypeOf reads the home's CURRENT prototype each access),
        // a key side effect such as `Object.setPrototypeOf(homeProto, null)` must be
        // observed by that read — e.g. `super[ruin()]` where ruin() nulls the prototype
        // must throw a TypeError. Evaluating super and the key as plain Index arguments
        // would read super first, so spill the key into a temp evaluated up front.
        if (isSuper && memberExpression.Computed)
        {
            // SuperProperty : super [ Expression ] evaluates GetThisBinding (step 2)
            // BEFORE the key Expression (step 3). In a derived constructor `this` is in
            // its TDZ until super() runs, so reading it must throw a ReferenceError
            // before any key side effect (`super[super()]`) is evaluated. Read `this`
            // into a temp first to force that ordering, then evaluate the key.
            using var thisTemp = scope.Top.GetTempVariable(typeof(JSValue));
            using var keyTemp = scope.Top.GetTempVariable(typeof(JSValue));
            return YExpression.Block(
                YExpression.Assign(thisTemp.Expression, target),
                YExpression.Assign(keyTemp.Expression, VisitExpression(mp)),
                JSValueBuilder.Index(thisTemp.Expression, super, keyTemp.Expression, memberExpression.Coalesce));
        }

        // Inside an optional chain, member access routes through the skip-aware links
        // (a `?.` link short-circuits on a nullish base; a trailing link only propagates
        // an in-flight short-circuit). Outside a chain it is an ordinary index.
        YExpression Access(YExpression keyExpr) =>
            memberExpression.InOptionalChain
                ? JSValueBuilder.ChainAccess(target, super, keyExpr, memberExpression.Coalesce)
                : JSValueBuilder.Index(target, super, keyExpr, memberExpression.Coalesce);

        switch (mp.Type)
        {
            case FastNodeType.Identifier:
                var id = mp as AstIdentifier;
                if (!memberExpression.Computed)
                {
                    // `obj.#x` is the only way to reach a non-computed IdentifierName
                    // starting with '#'; route it to the private key namespace so it
                    // does not alias a public `obj["#x"]` string property.
                    var isPrivate = id.Name.Length > 0 && id.Name.Value[0] == '#';
                    var key = isPrivate
                        ? KeyOfPrivateName(id.Name)
                        : KeyOfName(id.Name);

                    // `obj?.#x` (and a private access trailing an optional link): short-circuit
                    // on a nullish/short-circuited receiver, otherwise read through the
                    // brand-checking private indexer. The generic link path is unusable here:
                    // it takes the key by `in` reference — illegal for a captured per-evaluation
                    // private-key variable — and would swallow the brand-check TypeError that a
                    // present-but-foreign receiver must raise.
                    if (isPrivate && memberExpression.InOptionalChain)
                    {
                        using var recv = scope.Top.GetTempVariable(typeof(JSValue));
                        return YExpression.Block(
                            YExpression.Assign(recv.Expression, target),
                            YExpression.Condition(
                                JSValueBuilder.OptionalChainGuard(recv.Expression, memberExpression.Coalesce),
                                JSValueBuilder.OptionalChainSkip(),
                                JSValueBuilder.Index(recv.Expression, super, key, false)));
                    }

                    return Access(key);
                }

                return Access(VisitIdentifier(id));

            case FastNodeType.Literal:
                var l = mp as AstLiteral;
                switch (l.TokenType)
                {
                    case TokenTypes.True:
                        return Access(KeyOfName(l.StringValue));

                    case TokenTypes.False:
                        return Access(KeyOfName(l.StringValue));

                    case TokenTypes.String:
                        var text = l.StringValue;
                        if (NumberParser.TryGetArrayIndex(text, out var d))
                            return Access(YExpression.Constant(d));

                        return Access(KeyOfName(text));

                    case TokenTypes.Number:
                        var number = l.NumericValue;
                        if (number >= 0 && number < uint.MaxValue && (number % 1) == 0)
                            return Access(YExpression.Constant((uint)l.NumericValue));

                        return Access(VisitLiteral(l));

                    default:
                        // null / bigint / regexp / template literal key: evaluate
                        // the literal and coerce it to a property key at runtime.
                        return Access(VisitLiteral(l));
                }

            case FastNodeType.MemberExpression:
                var se = mp as AstMemberExpression;
                return Access(VisitExpression(se));
        }

        if (memberExpression.Computed)
            return Access(VisitExpression(memberExpression.Property));

        throw new NotImplementedException();
    }
}
