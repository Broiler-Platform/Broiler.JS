using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using System;
using System.Reflection;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly MethodInfo NormalizeUpdatePropertyKeyMethod = typeof(JSValue)
        .GetMethod("NormalizePropertyKey", BindingFlags.NonPublic | BindingFlags.Static, [typeof(JSValue)])
        ?? throw new InvalidOperationException("JSValue.NormalizePropertyKey(JSValue) not found");

    private static readonly MethodInfo ThrowInvalidUpdateReferenceMethod = typeof(FastCompiler)
        .GetMethod(nameof(ThrowInvalidUpdateReference), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("FastCompiler.ThrowInvalidUpdateReference() not found");

    private static JSValue ThrowInvalidUpdateReference() =>
        throw JSEngine.NewReferenceError("Invalid left-hand side expression for update");

    /// <param name="discardResult">
    /// True when the caller is a statement position that throws the value away — a `for`
    /// update clause. It lets a numeric local's `i++` compile to a bare double add with no
    /// boxing at all, which is the whole point of holding the counter unboxed.
    /// </param>
    /// <summary>
    /// The constant <see cref="KeyString"/> for an <c>obj.name++</c> whose read and write-back
    /// may go through the property inline caches, or <c>null</c> when they may not.
    /// </summary>
    /// <remarks>
    /// Item 2-4 of <c>docs/performance-roadmap.md</c>. An update expression reads and writes the
    /// same property, and both halves used one assignable index reference, which reaches neither
    /// cache — measured at 0 hits AND 0 misses, so the counters never even saw it, against
    /// 199 999 hits for the plain <c>obj.name = value</c> beside it.
    /// <para>
    /// Eligible on exactly the terms <see cref="TryCreateCachedMemberStore"/> uses, and for the
    /// same reasons: a constant key, an ordinary base, no <c>super</c> and no optional chain. A
    /// private name is a brand check rather than an ordinary [[Get]]/[[Set]], and a computed key
    /// would drive one site through every key the expression produces. The caller has already
    /// evaluated the base into a temp, so the key is the only thing left to decide.
    /// </para>
    /// </remarks>
    private BExpression TryCreateCachedUpdateKey(AstMemberExpression member)
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
        return key.Type == typeof(KeyString) ? key : null;
    }

    private BExpression InternalVisitUpdateExpression(AstUnaryExpression updateExpression, bool discardResult = false)
    {
        // added support for a++, a--
        updateExpression.Argument.VerifyIdentifierForUpdate(IsStrictMode);

        if (updateExpression.Argument is AstCallExpression)
        {
            return BExpression.Block(
                VisitExpression(updateExpression.Argument),
                BExpression.Call(null, ThrowInvalidUpdateReferenceMethod));
        }

        if (updateExpression.Argument is AstIdentifier identifier)
        {
            if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable == null)
            {
                using var withObject = scope.Top.GetTempVariable(typeof(JSObject));
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<BParameterExpression> { withObject.Variable, current.Variable };
                var globalKey = KeyOfName(identifier.Name);

                if (previous != null)
                    variables.Add(previous.Variable);

                var dynamicStatements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, JSContextBuilder.ResolveIdentifier(globalKey)),
                    // Coerce to Number/BigInt once: the postfix result is the coerced
                    // old value and the operand's valueOf must run exactly once.
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                    dynamicStatements.Add(BExpression.Assign(previous.Variable, current.Expression));

                dynamicStatements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith)
                        : JSValueBuilder.Decrement(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith)));
                dynamicStatements.Add(JSContextBuilder.AssignIdentifier(globalKey, current.Expression));
                dynamicStatements.Add(previous?.Expression ?? current.Expression);

                var retainedWithReference = JSValueBuilder.Index(withObject.Expression, globalKey);
                var withStatements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, retainedWithReference),
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                    withStatements.Add(BExpression.Assign(previous.Variable, current.Expression));

                withStatements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith)
                        : JSValueBuilder.Decrement(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith)));
                withStatements.Add(JSContextBuilder.AssignWithObjectIdentifier(withObject.Expression, globalKey, current.Expression, IsStrictMode));
                withStatements.Add(previous?.Expression ?? current.Expression);

                return BExpression.Block(
                    variables,
                    BExpression.Assign(withObject.Expression, JSContextBuilder.ResolveWithObject(globalKey)),
                    BExpression.Condition(
                        BExpression.NotEqual(withObject.Expression, BExpression.Constant(null, typeof(JSObject))),
                        BExpression.Block(withStatements),
                        BExpression.Block(dynamicStatements),
                        typeof(JSValue)));
            }

            // `i++` on a numeric local: a native double add. No ToNumeric (the value is
            // already a number), no JSValue arithmetic, and — when the result is discarded,
            // which is where a loop counter lives — no boxing at all.
            if (variable.NumericStorage != null)
            {
                var storage = variable.NumericStorage;
                var delta = BExpression.Constant(updateExpression.Operator == UnaryOperator.Increment ? 1d : -1d);

                var advance = BExpression.Assign(storage, BExpression.Add(storage, delta));
                if (discardResult)
                    return advance;

                // Outside a statement position the expression still has to produce a
                // JSValue, so the result — and only the result — is boxed.
                if (updateExpression.Prefix)
                    return JSNumberBuilder.New(advance, NumberBoxingConversionSite.UpdateStep);

                using var previousValue = scope.Top.GetTempVariable(typeof(double));
                return BExpression.Block(
                    previousValue.Variable.AsSequence(),
                    BExpression.Assign(previousValue.Variable, storage),
                    advance,
                    JSNumberBuilder.New(previousValue.Expression, NumberBoxingConversionSite.UpdateStep));
            }

            // Item 3-8a: the step is the whole reason the dual representation exists. While the
            // raw half is live the increment is a native double add that writes NOTHING back to
            // the slot — which is the box that item 3-1's update-target census priced at 98.1% of
            // the corpus's steps. When it is not live the ordinary generic step runs and the two
            // halves are resynchronized from the slot afterwards.
            if (variable.SpeculativeNumericFlag != null)
            {
                var flag = variable.SpeculativeNumericFlag;
                var raw = variable.SpeculativeNumericStorage;
                var slot = variable.SpeculativeSlot;
                var step = BExpression.Constant(
                    updateExpression.Operator == UnaryOperator.Increment ? 1d : -1d);

                // The generic arm, written against the slot: coerce once, keep the coerced OLD
                // value for a postfix result, step, store back, and re-derive the halves.
                using var previous = scope.Top.GetTempVariable(typeof(JSValue));
                var genericArm = BExpression.Block(
                    BExpression.Assign(slot, JSValueBuilder.ToNumeric(slot)),
                    BExpression.Assign(previous.Variable, slot),
                    BExpression.Assign(
                        slot,
                        updateExpression.Operator == UnaryOperator.Increment
                            ? JSValueBuilder.Increment(slot, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot)
                            : JSValueBuilder.Decrement(slot, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot)),
                    ResyncSpeculative(variable),
                    updateExpression.Prefix ? slot : previous.Expression);

                if (discardResult)
                {
                    // A `for` update clause throws the value away, so the winning arm is a bare
                    // double add and allocates nothing at all.
                    return BExpression.Block(
                        previous.Variable.AsSequence(),
                        BExpression.Condition(
                            flag,
                            BExpression.Block(
                                BExpression.Assign(raw, BExpression.Add(raw, step)),
                                JSUndefinedBuilder.Value),
                            genericArm,
                            typeof(JSValue)));
                }

                // The value is used, so the winning arm still has to hand back a JSValue — but
                // only the RESULT is boxed, and the binding itself stays unboxed.
                using var before = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(double));
                var locals = new Sequence<BParameterExpression> { previous.Variable };
                if (before != null)
                    locals.Add(before.Variable);

                var nativeArm = updateExpression.Prefix
                    ? BExpression.Block(
                        BExpression.Assign(raw, BExpression.Add(raw, step)),
                        JSNumberBuilder.New(raw, NumberBoxingConversionSite.UpdateStep))
                    : BExpression.Block(
                        BExpression.Assign(before.Variable, raw),
                        BExpression.Assign(raw, BExpression.Add(raw, step)),
                        JSNumberBuilder.New(before.Expression, NumberBoxingConversionSite.UpdateStep));

                return BExpression.Block(
                    locals,
                    BExpression.Condition(flag, nativeArm, genericArm, typeof(JSValue)));
            }
            if (variable.Variable?.Type == typeof(JSVariable) && !variable.IsDeletable)
            {
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<BParameterExpression> { current.Variable };
                var statements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, variable.Expression),
                    // Coerce to Number/BigInt once: the postfix result is the coerced
                    // old value (`var y = "1"++` yields the Number 1).
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                {
                    variables.Add(previous.Variable);
                    statements.Add(BExpression.Assign(previous.Variable, current.Expression));
                }

                statements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.LocalCell)
                        : JSValueBuilder.Decrement(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.LocalCell)));
                statements.Add(BExpression.Assign(variable.Expression, current.Expression));
                statements.Add(previous?.Expression ?? current.Expression);

                return BExpression.Block(variables, statements);
            }

            // An eval-introduced global `var` is deletable: its read goes through the throwing
            // global resolution (ReadExpression, which raises a ReferenceError once the binding has
            // been deleted) while its write targets the assignable global-object property
            // (Expression). The generic member-update path below visits the identifier once and uses
            // that single expression as both the read source and the assignment target — for these
            // bindings the read is a (non-assignable) method Call, so the write must be split out
            // here to target the property index instead.
            if (variable.ReadExpression != null)
            {
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<BParameterExpression> { current.Variable };
                var statements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, variable.ReadExpression),
                    // Coerce to Number/BigInt once: the postfix result is the coerced old value.
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                {
                    variables.Add(previous.Variable);
                    statements.Add(BExpression.Assign(previous.Variable, current.Expression));
                }

                statements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith)
                        : JSValueBuilder.Decrement(current.Expression, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith)));
                statements.Add(BExpression.Assign(variable.Expression, current.Expression));
                statements.Add(previous?.Expression ?? current.Expression);

                return BExpression.Block(variables, statements);
            }
        }

        var list = new Sequence<BExpression>();

        FastFunctionScope.VariableScope target = null;
        FastFunctionScope.VariableScope key = null;
        FastFunctionScope.VariableScope superBase = null;
        FastFunctionScope.VariableScope @return = null;

        // Set for `obj.name++` and `obj.name--` on a constant key, where the read and the
        // write-back can each go through their inline cache instead of an assignable index
        // reference. See TryCreateCachedUpdateKey; null means the ordinary reference is used.
        BExpression cachedKey = null;

        // The source text of the access the two cached sites belong to, and the property it
        // names. Empty for every shape that does not take the cached pair.
        StringSpan cachedAccess = default;
        StringSpan cachedAccessProperty = default;

        // Where the operand lives, for item 3-1's update-target census. Decided here because the
        // step itself is emitted once for every member shape, and the shared tail can no longer
        // tell them apart. `Other` is the honest default: anything reaching the tail that this
        // census has not named lands there, and a non-zero Other row is a signal to come back
        // rather than a rounding error.
        //
        // An identifier reaching here has already passed TryGetStaticIdentifierVariable — the
        // dynamic, numeric-local, cell and deletable-global cases each returned above — so what is
        // left is a statically-resolved local or parameter in a plain assignable slot: a name the
        // numeric analysis did not prove numeric. That turns out to be the largest row on this
        // corpus, and the first version of this census put all of it in `Other`, which is what
        // said the census was wrong rather than the engine being surprising.
        var updateTarget = updateExpression.Argument is AstIdentifier
            ? ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot
            : ArithmeticOperandDiagnostics.UpdateTarget.Other;

        var right = VisitExpression(updateExpression.Argument);

        if (updateExpression.Argument is AstMemberExpression memberExpression)
        {
            var isSuper = memberExpression.Object?.Type == FastNodeType.Super;

            // Computed against named, which is the same split the numeric tree's order-blocker
            // sub-census uses. It is syntactic rather than semantic — `a["x"]++` counts as an
            // element and reaches a named property — but the shapes that matter here (`a[i]++`
            // against `o.x++`) fall on the right sides of it, and a syntactic rule is the only
            // one available before the key is evaluated.
            updateTarget = memberExpression.Computed
                ? ArithmeticOperandDiagnostics.UpdateTarget.Element
                : ArithmeticOperandDiagnostics.UpdateTarget.Property;

            target = scope.Top.GetTempVariable(typeof(JSValue));
            list.Add(BExpression.Assign(target.Variable, VisitExpression(memberExpression.Object)));

            if (isSuper)
            {
                // `++super[key]` / `++super.x`: the spec builds a single
                // SuperProperty Reference whose base (GetSuperBase) is resolved
                // BEFORE ToPropertyKey, and reuses that base and key for both the
                // read and the write. Capture them once here: evaluate the key
                // expression, then GetSuperBase, then normalize the key (whose
                // toString must observe the already-resolved base). A plain
                // member update would drop the super base and use `this` as the
                // base, reading/writing the wrong object.
                superBase = scope.Top.GetTempVariable(typeof(JSValue));

                if (memberExpression.Computed)
                {
                    key = scope.Top.GetTempVariable(typeof(JSValue));
                    list.Add(BExpression.Assign(key.Variable, VisitExpression(memberExpression.Property)));
                    list.Add(BExpression.Assign(superBase.Variable, scope.Top.Super));
                    list.Add(BExpression.Assign(key.Variable, BExpression.Call(null, NormalizeUpdatePropertyKeyMethod, key.Expression)));
                    right = JSValueBuilder.Index(target.Expression, superBase.Expression, key.Expression);
                }
                else
                {
                    list.Add(BExpression.Assign(superBase.Variable, scope.Top.Super));
                    right = JSValueBuilder.Index(target.Expression, superBase.Expression, CreatePropertyKeyExpression(memberExpression.Property, false));
                }
            }
            else if (memberExpression.Computed)
            {
                key = scope.Top.GetTempVariable(typeof(JSValue));
                list.Add(BExpression.Assign(key.Variable, VisitExpression(memberExpression.Property)));
                // Per spec, ToObject(base) must precede ToPropertyKey(key).
                // RequireObjectCoercible throws TypeError for null/undefined before
                // NormalizePropertyKey can trigger observable side effects (e.g. toString).
                list.Add(BExpression.Call(null, RequireObjectCoercibleMethod, target.Expression));
                list.Add(BExpression.Assign(key.Variable, BExpression.Call(null, NormalizeUpdatePropertyKeyMethod, key.Expression)));
                right = JSValueBuilder.Index(target.Expression, key.Expression);
            }
            else
            {
                cachedKey = TryCreateCachedUpdateKey(memberExpression);
                // `obj.x++` on a nullish `obj` fails on this read, before the step or the
                // write-back, so both sites carry the access's source text (NullishAccess).
                cachedAccess = memberExpression.AccessCode();
                cachedAccessProperty = PropertyNameText(memberExpression.Property);
                right = cachedKey != null
                    ? JSValueBuilder.CachedIndex(target.Expression, cachedKey, in cachedAccess, in cachedAccessProperty)
                    : CreateMemberExpression(target.Expression, memberExpression.Property, false);
            }
        }

        switch (right.NodeType)
        {
            case BExpressionType.Index:
                if (target == null)
                {
                    var index = right as BIndexExpression;
                    target = scope.Top.GetTempVariable(index.Type);
                    list.Add(BExpression.Assign(target.Variable, index.Target));
                    right = BExpression.Index(target.Variable, index.Property, index.Arguments);
                }
                break;
        }

        // ToNumeric reads the member/index once and coerces the operand to a
        // Number/BigInt exactly once, so a getter with side effects is observed only
        // once and the result of a postfix update is the coerced old value
        // (`obj.x++` where obj.x is "1" yields the Number 1, not the String "1").
        var coerced = scope.Top.GetTempVariable(typeof(JSValue));
        list.Add(BExpression.Assign(coerced.Variable, JSValueBuilder.ToNumeric(right)));

        var newValue = updateExpression.Operator == UnaryOperator.Increment
            ? JSValueBuilder.Increment(coerced.Expression, updateTarget)
            : JSValueBuilder.Decrement(coerced.Expression, updateTarget);

        // The write-back, through the store cache when the read went through the read cache.
        // Both forms end in the same JSValue indexer on a miss, so strict-mode reporting and
        // the silent-failure behaviour below are unchanged; a hit skips it.
        BExpression WriteBack(BExpression value)
            => cachedKey != null
                ? JSValueBuilder.CachedStore(target.Expression, cachedKey, value, in cachedAccess, in cachedAccessProperty)
                : BExpression.Assign(right, value);

        if (updateExpression.Prefix)
        {
            // For prefix update on member expressions, save the computed new value
            // before writing it back. The write may silently fail (e.g. non-writable
            // property in sloppy mode), but the expression must return the new value.
            @return = scope.Top.GetTempVariable(typeof(JSValue));
            list.Add(BExpression.Assign(@return.Variable, newValue));
            list.Add(WriteBack(@return.Variable));
        }
        else
        {
            // Postfix: the coerced old value is the result; write the new value back.
            list.Add(WriteBack(newValue));
            @return = coerced;
        }

        list.Add(@return.Variable);

        var r = BExpression.Block(list);
        @return?.Dispose();
        if (!ReferenceEquals(@return, coerced))
            coerced.Dispose();
        key?.Dispose();
        superBase?.Dispose();
        target?.Dispose();

        return r;
    }
}
