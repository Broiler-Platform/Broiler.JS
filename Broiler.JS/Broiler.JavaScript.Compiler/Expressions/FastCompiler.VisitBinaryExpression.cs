using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitBinaryExpression(AstBinaryExpression binaryExpression)
    {
        var @operator = binaryExpression.Operator;

        if (@operator > TokenTypes.BeginAssignTokens && @operator < TokenTypes.EndAssignTokens)
            return VisitAssignmentExpression(binaryExpression.Left, @operator, binaryExpression.Right);

        // Ergonomic brand check: `#name in rval`. A leading PrivateIdentifier is only
        // valid as the left operand of `in`; resolve it to its private-name key and
        // emit a brand-presence test rather than a normal `in` (which would try to
        // resolve `#name` as a variable).
        if (@operator == TokenTypes.In
            && binaryExpression.Left is AstIdentifier privateLeft
            && privateLeft.Name.Length > 0
            && privateLeft.Name.Value[0] == '#')
        {
            return JSObjectBuilder.PrivateNameIn(
                KeyOfPrivateName(privateLeft.Name),
                ToJSValueExpression(Visit(binaryExpression.Right)));
        }

        var (isLeftString, isLeftNumber, left) = ToNativeExpression(binaryExpression.Left);
        var (isRightString, isRightNumber, right) = ToNativeExpression(binaryExpression.Right);

        // With both operands already native doubles the whole tree stays unboxed and only
        // its final value is boxed. Without this a numeric local would be RE-boxed for any
        // operator that has no native case below — which cost `t += i % 100` an extra
        // allocation per iteration rather than saving one.
        if (isLeftNumber && isRightNumber
            && TryCreateNativeNumericOperation(@operator, left, right) is { } nativeNumeric)
        {
            return nativeNumeric;
        }

        // ONE side native, the other a JSValue (docs/performance-roadmap.md item 3-5). Without
        // this the native side is boxed to meet the JSValue operator, which is a whole allocation
        // to answer a question about a number the engine already has unboxed — and
        // `for (var i = 0; i < n; i++)` is the shape it happens in, so it happens per iteration.
        // Unboxing the other side behind a type test instead costs a branch and allocates nothing.
        if (@operator is TokenTypes.Less or TokenTypes.Greater)
        {
            if (isLeftNumber != isRightNumber
                && TryCreateMixedNumericComparison(@operator, isLeftNumber, left, right) is { } mixedNumeric)
            {
                CompilerSpecializationDiagnostics.RecordMixedNumericComparison();
                return mixedNumeric;
            }

            // Neither operand is an unboxed double, so nothing here can help it — counted as the
            // denominator, because "3-5 is a 3.4x win on its shape" only means something next to
            // how often the shape occurs.
            if (!isLeftNumber && !isRightNumber)
                CompilerSpecializationDiagnostics.RecordBoxedNumericComparison();
        }

        switch (@operator)
        {
            case TokenTypes.Plus:
                if (isLeftNumber && isRightNumber)
                    return JSNumberBuilder.New(BExpression.Add(left, right));

                if (isLeftString && isRightString)
                    return JSStringBuilder.New(ClrStringBuilder.Concat(left, right));

                if (isRightNumber)
                    return JSValueBuilder.AddDouble(ToJSValueExpression(left), right);

                if (isRightString)
                    return JSValueBuilder.AddString(ToJSValueExpression(left), right);

                return JSValueBuilder.Add(ToJSValueExpression(left), ToJSValueExpression(right));

            case TokenTypes.Equal:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.Equal(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.Equal(left, right));
                }

                return JSValueBuilder.Equals(ToJSValueExpression(left), right);

            case TokenTypes.NotEqual:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.NotEqual(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.NotEqual(left, right));
                }

                return JSValueBuilder.NotEquals(ToJSValueExpression(left), right);

            case TokenTypes.StrictlyEqual:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.Equal(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.Equal(left, right));
                }

                return JSValueBuilder.StrictEquals(ToJSValueExpression(left), right);

            case TokenTypes.StrictlyNotEqual:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.NotEqual(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.NotEqual(left, right));
                }

                return JSValueBuilder.NotStrictEquals(ToJSValueExpression(left), right);
        }
        
        var be = BinaryOperation.Operation(ToJSValueExpression(left), ToJSValueExpression(right), @operator);
        return be ?? throw new FastParseException(binaryExpression.Start, $"Undefined binary operation {@operator}");
    }

    public static BExpression ToJSValueExpression(BExpression exp)
    {
        if (typeof(JSValue).IsAssignableFrom(exp.Type))
            return exp;

        if (exp.Type == typeof(string))
            return JSStringBuilder.New(exp);

        if (exp.Type == typeof(double))
            return JSNumberBuilder.New(exp);

        throw new NotImplementedException();
    }

    /// <summary>
    /// <paramref name="operator"/> applied to two CLR doubles, still unboxed, or null when it
    /// has no native form and the operands have to go back through the JSValue operators.
    /// </summary>
    /// <remarks>
    /// Only operators whose JavaScript semantics on two numbers ARE the CLR double operator
    /// appear here. Two families are deliberately absent. The bitwise and shift operators run
    /// ToInt32 first, whose modulo-2^32 wrapping a plain cast does not reproduce. And
    /// <c>&lt;=</c> / <c>&gt;=</c> are absent because the backend emits an ORDERED compare for
    /// them, which answers true when either side is NaN — every relational comparison
    /// involving NaN is false in JavaScript. <c>&lt;</c> and <c>&gt;</c> do not have that
    /// problem and are the forms a loop test actually uses.
    /// </remarks>
    private static BExpression TryCreateNativeNumericValue(TokenTypes @operator, BExpression left, BExpression right)
        => @operator switch
        {
            TokenTypes.Plus => BExpression.Add(left, right),
            TokenTypes.Minus => BExpression.Subtract(left, right),
            TokenTypes.Multiply => BExpression.Multiply(left, right),
            TokenTypes.Divide => BExpression.Divide(left, right),
            TokenTypes.Mod => BExpression.Modulo(left, right),
            TokenTypes.Power => BExpression.Power(left, right),

            // The bitwise and shift family, through JSNumericOperators rather than as
            // BExpression nodes: their operands are ToInt32/ToUint32 of the double and not the
            // double, and that reduction is not a CLR cast. Sending them to one static method
            // apiece makes them identical to the JSValue operators by construction.
            TokenTypes.BitwiseAnd when NativeBitwiseOperators.Enabled => NumericOperator(nameof(JSNumericOperators.BitwiseAnd), left, right),
            TokenTypes.BitwiseOr when NativeBitwiseOperators.Enabled => NumericOperator(nameof(JSNumericOperators.BitwiseOr), left, right),
            TokenTypes.Xor when NativeBitwiseOperators.Enabled => NumericOperator(nameof(JSNumericOperators.BitwiseXor), left, right),
            TokenTypes.LeftShift when NativeBitwiseOperators.Enabled => NumericOperator(nameof(JSNumericOperators.LeftShift), left, right),
            TokenTypes.RightShift when NativeBitwiseOperators.Enabled => NumericOperator(nameof(JSNumericOperators.RightShift), left, right),
            TokenTypes.UnsignedRightShift when NativeBitwiseOperators.Enabled => NumericOperator(nameof(JSNumericOperators.UnsignedRightShift), left, right),

            _ => null,
        };

    private static readonly System.Collections.Generic.Dictionary<string, System.Reflection.MethodInfo> numericOperators = new();

    /// <summary>A call to one of <see cref="JSNumericOperators"/>'s two-double operators.</summary>
    private static BExpression NumericOperator(string name, BExpression left, BExpression right)
    {
        System.Reflection.MethodInfo method;
        lock (numericOperators)
        {
            if (!numericOperators.TryGetValue(name, out method))
            {
                method = typeof(JSNumericOperators).GetMethod(name, [typeof(double), typeof(double)]);
                numericOperators[name] = method;
            }
        }

        return BExpression.Call(null, method, left, right);
    }

    /// <summary>The boxed result of a native numeric operation or comparison, or null.</summary>
    private static BExpression TryCreateNativeNumericOperation(TokenTypes @operator, BExpression left, BExpression right)
    {
        if (TryCreateNativeNumericValue(@operator, left, right) is { } value)
            return JSNumberBuilder.New(value);

        return @operator switch
        {
            TokenTypes.Less => JSBooleanBuilder.NewFromCLRBoolean(BExpression.Binary(left, BOperator.Less, right)),
            TokenTypes.Greater => JSBooleanBuilder.NewFromCLRBoolean(BExpression.Binary(left, BOperator.Greater, right)),
            _ => null,
        };
    }

    /// <summary>
    /// <c>&lt;</c> or <c>&gt;</c> where one operand is already an unboxed double and the other is
    /// a <see cref="JSValue"/>: tests the value side and compares two doubles when it is a number,
    /// falling back to the ordinary operator when it is not (item 3-5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is the shape worth fixing.</b> Item 3-3 gave <c>var</c>, <c>let</c>/<c>const</c>
    /// and block-scoped <c>var</c> raw doubles, and left one gap: a parameter cannot reach the
    /// numeric tier, because what a caller passed is not knowable. The gap was recorded as a
    /// limitation and never priced. It costs <b>a box per iteration</b> — measured at
    /// <b>33.77 ns and 32 B</b> against <b>8.36 ns and 0 B</b> for the same loop with a literal
    /// bound — because <c>i &lt; n</c> boxes the raw <c>i</c> to meet the <c>JSValue</c> operator.
    /// </para>
    /// <para>
    /// <b>And the fix is not the one the gap suggests.</b> Making a parameter a numeric local needs
    /// an entry guard and a second body; unboxing the <em>other side</em> of the comparison needs
    /// neither, and it covers strictly more — <c>i &lt; a.length</c> is a property read, not a
    /// parameter, and boxed for the same reason.
    /// </para>
    /// <para>
    /// <b>Only <c>&lt;</c> and <c>&gt;</c></b>, for the reason
    /// <see cref="TryCreateNativeNumericValue"/> already records: the backend emits an ORDERED
    /// compare for <c>&lt;=</c> and <c>&gt;=</c>, which answers true when either side is NaN, and
    /// every relational comparison involving NaN is false in JavaScript.
    /// </para>
    /// <para>
    /// <b>Nothing is skipped when the guard holds.</b> Relational comparison runs ToPrimitive on
    /// both operands first, and ToPrimitive of a Number is that Number — no <c>valueOf</c>, no
    /// <c>toString</c>, no observable effect. So a receiver that is already a number takes the
    /// same path with the same answer; anything else takes the operator it took before.
    /// </para>
    /// <para>
    /// Both operands go into temporaries, in source order, because the value side is read twice
    /// (the test and the unboxing) and the native side is read in both arms. Evaluating in place
    /// would run the left operand after the right, or run one of them twice.
    /// </para>
    /// </remarks>
    private BExpression TryCreateMixedNumericComparison(
        TokenTypes @operator, bool leftIsNative, BExpression left, BExpression right)
    {
        if (@operator is not (TokenTypes.Less or TokenTypes.Greater))
            return null;

        var nativeSide = leftIsNative ? left : right;
        var valueSide = leftIsNative ? right : left;
        if (nativeSide.Type != typeof(double) || !typeof(JSValue).IsAssignableFrom(valueSide.Type))
            return null;

        // Block-declared locals rather than pooled temporaries, and the difference is
        // correctness. A pooled temp is acquired here — AFTER both operands were compiled — so a
        // temp one of them released while being built could be handed straight back, and the
        // second assignment would then clobber the first operand's saved value. `i < obj.m()` is
        // enough to reach it. Declaring locals in the block cannot collide with anything.
        var nativeLocal = BExpression.Parameter(typeof(double), "#cmpnum");
        var valueLocal = BExpression.Parameter(typeof(JSValue), "#cmpval");

        var comparison = leftIsNative
            ? BExpression.Binary(nativeLocal, ToBinaryOperator(@operator), JSValueBuilder.DoubleValue(valueLocal))
            : BExpression.Binary(JSValueBuilder.DoubleValue(valueLocal), ToBinaryOperator(@operator), nativeLocal);

        var generic = leftIsNative
            ? BinaryOperation.Operation(JSNumberBuilder.New(nativeLocal), valueLocal, @operator)
            : BinaryOperation.Operation(valueLocal, JSNumberBuilder.New(nativeLocal), @operator);

        if (generic == null)
            return null;

        // Source order: the left operand is evaluated first whichever side is the native one.
        var first = leftIsNative
            ? BExpression.Assign(nativeLocal, nativeSide)
            : BExpression.Assign(valueLocal, valueSide);
        var second = leftIsNative
            ? BExpression.Assign(valueLocal, valueSide)
            : BExpression.Assign(nativeLocal, nativeSide);

        return BExpression.Block(
            new Sequence<BParameterExpression> { nativeLocal, valueLocal },
            first,
            second,
            BExpression.Condition(
                JSValueBuilder.IsNumber(valueLocal),
                JSBooleanBuilder.NewFromCLRBoolean(comparison),
                generic,
                typeof(JSValue)));
    }

    private static BOperator ToBinaryOperator(TokenTypes @operator)
        => @operator == TokenTypes.Less ? BOperator.Less : BOperator.Greater;

    public (bool isString, bool isNumber, BExpression exp) ToNativeExpression(AstExpression ast)
    {
        if (ast.Type == FastNodeType.Literal && ast is AstLiteral a)
        {
            switch (a.TokenType)
            {
                case TokenTypes.String:
                    return (true, false, BExpression.Constant(a.StringValue));

                case TokenTypes.Number:
                    return (false, true, BExpression.Constant(a.NumericValue));
            }
        }

        // A local the analysis proved numeric is already a raw double.
        if (TryGetNumericLocalStorage(ast, out var storage))
            return (false, true, storage);

        // ... and an arithmetic node over two native operands is itself native, so a whole
        // tree stays unboxed and only its final value is boxed. Without this recursion only
        // the leaves were native and every intermediate result was re-boxed
        // (docs/performance-roadmap.md P2-2 item 3).
        //
        // Decided on the SYNTAX first, deliberately. Compiling a subtree speculatively and
        // throwing the result away would leak whatever the visit allocated on the way —
        // an inline-cache site per discarded attempt, among other compile-time state.
        if (ast is AstBinaryExpression nested && IsNativeNumericAst(nested))
        {
            var (_, _, nestedLeft) = ToNativeExpression(nested.Left);
            var (_, _, nestedRight) = ToNativeExpression(nested.Right);
            return (false, true, TryCreateNativeNumericValue(nested.Operator, nestedLeft, nestedRight));
        }

        return (false, false, Visit(ast));
    }

    /// <summary>
    /// Whether this expression compiles to an unboxed double without visiting it — a numeric
    /// literal, a numeric local, or a native arithmetic node over two of those.
    /// </summary>
    private bool IsNativeNumericAst(AstExpression ast) => ast switch
    {
        AstLiteral { TokenType: TokenTypes.Number } => true,
        AstIdentifier => TryGetNumericLocalStorage(ast, out _),
        AstBinaryExpression binary => IsNativeNumericOperator(binary.Operator)
            && IsNativeNumericAst(binary.Left)
            && IsNativeNumericAst(binary.Right),
        _ => false,
    };

    private static bool IsNativeNumericOperator(TokenTypes @operator) => @operator
        is TokenTypes.Plus or TokenTypes.Minus or TokenTypes.Multiply
        or TokenTypes.Divide or TokenTypes.Mod or TokenTypes.Power
        || (NativeBitwiseOperators.Enabled && @operator
            is TokenTypes.BitwiseAnd or TokenTypes.BitwiseOr or TokenTypes.Xor
            or TokenTypes.LeftShift or TokenTypes.RightShift or TokenTypes.UnsignedRightShift);

    /// <summary>The raw double storage behind <paramref name="ast"/>, if it is a numeric local.</summary>
    private bool TryGetNumericLocalStorage(AstExpression ast, out BExpression storage)
    {
        storage = null;
        if (ast is not AstIdentifier identifier
            || withBoundaries.Count != 0
            || evalShadowBoundary != null)
        {
            return false;
        }

        if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable?.NumericStorage == null)
            return false;

        storage = variable.NumericStorage;
        return true;
    }

    /// <summary>Coerces a compiled value to the CLR double a numeric local stores.</summary>
    private static BExpression ToDoubleExpression(BExpression exp)
        => exp.Type == typeof(double) ? exp : JSValueBuilder.DoubleValue(ToJSValueExpression(exp));

    /// <summary>
    /// An assignment into <paramref name="variable"/>, unboxing the value when the binding is
    /// a numeric local. Writing through <c>Expression</c> there would target a boxing read.
    /// </summary>
    private static BExpression AssignToVariable(FastFunctionScope.VariableScope variable, BExpression value)
        => variable.NumericStorage != null
            ? BExpression.Assign(variable.NumericStorage, ToDoubleExpression(value))
            : BExpression.Assign(variable.Expression, value);
}
