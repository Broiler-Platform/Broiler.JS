using System.Collections.Generic;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Item 3-1's shared half: an arithmetic tree whose operands the compiler cannot prove numeric is
/// computed on raw doubles behind one run-time type test, boxing only its root.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this and not a typed backing store.</b> The item is written around
/// <c>ElementArray</c>'s <c>IPropertyValue[]</c>, and measured, a typed store by itself is a wash:
/// it trades a write allocation for a read allocation. The count that re-specified it is on the
/// other side of the boundary — <b>73 817 515 of 73 818 646 generic arithmetic invocations across
/// the Octane corpus arrive with both operands already Numbers, every one but 1 131, and that
/// population is 86.6% of every box the corpus allocates</b>. The operator already receives two
/// Numbers whatever they are stored in. What it cannot do is <em>hand one back</em>, because its
/// consumer is a <c>JSValue</c> local, slot or element — so every intermediate result of an
/// expression is boxed, not just its value. <c>s = s + a[0] * 1.5</c> costs 96 bytes and three
/// boxes, of which <em>two</em> are intermediates.
/// </para>
/// <para>
/// <b>What the compiler could prove, for contrast: 0.75%.</b> The existing
/// <c>isLeftNumber &amp;&amp; isRightNumber</c> gate — the one every phase-3 item so far has
/// widened — reaches 556 053 of those 73.8 M invocations. This does not widen it. It leaves the
/// proof alone and tests at run time instead, which is the only thing that reaches the other
/// 99.25%.
/// </para>
/// <para>
/// <b>Evaluation order is the whole correctness argument, and it is why this bails more often
/// than it needs to.</b> The reference emission evaluates a node's two operands and then coerces
/// them, so in a nested tree a coercion happens <em>between</em> two leaf evaluations. Hoisting
/// every leaf into a temporary ahead of the test moves later leaves in front of that coercion —
/// and a coercion is observable, because <c>ToPrimitive</c> on an object runs <c>valueOf</c>. So a
/// tree is eligible only when every leaf evaluated <em>after</em> the first internal node in
/// postorder is one that cannot observe or cause anything: a numeric literal or a proven-numeric
/// local. Leaves before the first coercion are unrestricted, and that is not a narrow case —
/// JavaScript's precedence makes <c>s + a[0] * 1.5</c> parse right-leaning, so all three of its
/// leaves precede the multiply's coercion.
/// </para>
/// <para>
/// <b>When the guard holds nothing is skipped.</b> Every operator here applies ToNumeric (or, for
/// <c>+</c>, ToPrimitive then ToNumeric) to both operands, and both are of Number, where each is
/// the identity — no <c>valueOf</c>, no <c>toString</c>, no observable step. The native forms are
/// <see cref="FastCompiler.TryCreateNativeNumericValue"/>'s, the same ones the all-native path
/// emits, so the two are identical by construction rather than by inspection; that is the argument
/// item 3-1's bitwise half already used for <c>ToUint32</c>.
/// </para>
/// </remarks>
partial class FastCompiler
{
    /// <summary>
    /// The guarded form of <paramref name="root"/>, or <c>null</c> when it is not eligible.
    /// </summary>
    /// <remarks>
    /// Called before either operand is visited, because the decision is made on the syntax: a
    /// speculative compile whose result was thrown away would leak whatever the visit allocated on
    /// the way, an inline-cache site among it. That is the rule <c>ToNativeExpression</c>'s own
    /// recursion already follows.
    /// </remarks>
    private BExpression TryCreateSpeculativeNumericTree(AstBinaryExpression root)
    {
        if (!NumericSpeculation.Enabled)
            return null;

        // Not a numeric tree at all, or already one the all-native path handles without a guard.
        if (!IsNativeNumericOperator(root.Operator) || IsNativeNumericAst(root))
            return null;

        // A `with` or an eval shadow can turn any identifier into a property read, which is
        // exactly the "leaf that can observe something" this argument depends on not happening.
        if (withBoundaries.Count != 0 || evalShadowBoundary != null)
            return null;

        // What the guard buys, counted on the syntax before anything is visited. Two things pay:
        // every operator BUT the root produces an intermediate the guarded form never boxes, and
        // every already-native leaf — a literal or a proven-numeric local — is one the generic
        // form has to box to meet a JSValue operator. A tree with one operator and two unprovable
        // leaves saves neither, so it would buy a type test and nothing else.
        //
        // The first version of this condition said "at least two operators", on the argument that
        // a single node mints one box either way. That argument is right and the condition was
        // wrong, because it forgot the operand: `a[0] * 2` costs TWO boxes today, the literal and
        // the result. Measured, requiring two operators took the corpus from 10.4 M boxes removed
        // to 5.6 M — Crypto alone lost 4.7 M — which is what says the single-node-with-a-literal
        // case is most of the prize rather than a rounding error.
        if (CountOperators(root) - 1 + CountNativeLeaves(root) < 1)
            return null;

        var seenCoercion = false;
        if (!HoistingIsOrderSafe(root, ref seenCoercion))
            return null;

        var leaves = new List<AstExpression>();
        CollectLeaves(root, leaves);

        // One binary node has two leaves; the ceiling is arbitrary but a tree with dozens of
        // guarded leaves is a long chain of tests to reach one saved box, and the shapes this is
        // for have three or four.
        if (leaves.Count > MaximumSpeculativeLeaves)
            return null;

        var compiled = new Dictionary<AstExpression, SpeculativeLeaf>(leaves.Count);
        var locals = new Sequence<BParameterExpression>();
        var body = new Sequence<BExpression>();
        var guarded = 0;

        foreach (var leaf in leaves)
        {
            // Visited exactly once, here, in source order — both arms then read what this
            // produced rather than compiling the leaf again.
            var (isString, isNumber, expression) = ToNativeExpression(leaf);

            // A string literal makes the guard unsatisfiable, and for `+` it makes the answer a
            // concatenation. Nothing to win; leave the tree to the ordinary emission.
            if (isString)
                return null;

            if (isNumber)
            {
                // Already a raw double, and pure — a literal or a proven-numeric local — so it can
                // be read in both arms without a temporary.
                compiled[leaf] = new SpeculativeLeaf(expression, null);
                continue;
            }

            // Block-declared rather than pooled, for the reason item 3-5 records: a pooled temp
            // acquired after the operands were compiled can be one that compiling them released,
            // and the next assignment would clobber a value already saved.
            var local = BExpression.Parameter(typeof(JSValue), "#spec" + guarded);
            locals.Add(local);
            body.Add(BExpression.Assign(local, expression));
            compiled[leaf] = new SpeculativeLeaf(null, local);
            guarded++;
        }

        // Nothing to test means the all-native path already had it.
        if (guarded == 0)
            return null;

        var nativeTree = BuildNativeTree(root, compiled);
        if (nativeTree == null)
            return null;

        var genericTree = BuildGenericTree(root, compiled);
        if (genericTree == null)
            return null;

        BExpression guard = null;
        foreach (var leaf in leaves)
        {
            if (compiled[leaf].Local is not { } local)
                continue;

            var test = JSValueBuilder.IsNumber(local);
            guard = guard == null ? test : BExpression.Binary(guard, BOperator.BooleanAnd, test);
        }

        body.Add(BExpression.Condition(
            guard,
            JSNumberBuilder.New(nativeTree),
            genericTree,
            typeof(JSValue)));

        CompilerSpecializationDiagnostics.RecordSpeculativeNumericTree(guarded);
        return BExpression.Block(locals, body);
    }

    /// <summary>
    /// Leaves above which a chain of type tests stops being worth one saved box. Not a
    /// correctness bound.
    /// </summary>
    private const int MaximumSpeculativeLeaves = 8;

    /// <summary>One leaf of a speculative tree: either already a raw double, or a saved value.</summary>
    private readonly record struct SpeculativeLeaf(BExpression Native, BParameterExpression Local);

    /// <summary>
    /// Operators in the tree — the number of results, of which all but the root's are
    /// intermediates that the guarded form never boxes.
    /// </summary>
    private int CountOperators(AstExpression node)
        => node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator)
            ? 1 + CountOperators(binary.Left) + CountOperators(binary.Right)
            : 0;

    /// <summary>
    /// Leaves that are already unboxed doubles, decided syntactically — each one is a box the
    /// ordinary emission mints to hand the value to a <c>JSValue</c> operator.
    /// </summary>
    private int CountNativeLeaves(AstExpression node)
    {
        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
            return CountNativeLeaves(binary.Left) + CountNativeLeaves(binary.Right);

        return IsNativeNumericAst(node) ? 1 : 0;
    }

    private void CollectLeaves(AstExpression node, List<AstExpression> leaves)
    {
        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
        {
            CollectLeaves(binary.Left, leaves);
            CollectLeaves(binary.Right, leaves);
            return;
        }

        leaves.Add(node);
    }

    /// <summary>
    /// Whether hoisting every leaf ahead of the type test preserves what the ordinary emission
    /// would have observed.
    /// </summary>
    /// <remarks>
    /// Postorder is the reference evaluation order: a node's operands are evaluated, then coerced.
    /// Everything before the first internal node is therefore evaluated before any coercion could
    /// run, and hoisting it changes no order at all. Everything after one is being moved in front
    /// of a coercion, so it has to be a leaf that neither causes nor observes an effect.
    /// </remarks>
    private bool HoistingIsOrderSafe(AstExpression node, ref bool seenCoercion)
    {
        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
        {
            if (!HoistingIsOrderSafe(binary.Left, ref seenCoercion))
                return false;

            if (!HoistingIsOrderSafe(binary.Right, ref seenCoercion))
                return false;

            seenCoercion = true;
            return true;
        }

        return !seenCoercion || IsPureLeaf(node);
    }

    /// <summary>
    /// Whether evaluating <paramref name="node"/> can be moved earlier without anything being able
    /// to tell.
    /// </summary>
    /// <remarks>
    /// Deliberately the narrowest useful set. A numeric literal is a constant. A proven-numeric
    /// local is a raw CLR double the analysis has already established is assigned before it is
    /// read, so reading it cannot throw and cannot run user code. Everything else — an element
    /// read, a member read, a call, even a plain identifier, which can be a TDZ throw or a global
    /// object property — is refused, because each of them can either run user code or throw, and
    /// either one is an order the reference emission fixes.
    /// </remarks>
    private bool IsPureLeaf(AstExpression node)
        => node is AstLiteral { TokenType: TokenTypes.Number }
            || (node is AstIdentifier && TryGetNumericLocalStorage(node, out _));

    private BExpression BuildNativeTree(AstExpression node, Dictionary<AstExpression, SpeculativeLeaf> compiled)
    {
        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
        {
            var left = BuildNativeTree(binary.Left, compiled);
            var right = BuildNativeTree(binary.Right, compiled);
            if (left == null || right == null)
                return null;

            return TryCreateNativeNumericValue(binary.Operator, left, right);
        }

        var leaf = compiled[node];
        return leaf.Local is { } local ? JSValueBuilder.DoubleValue(local) : leaf.Native;
    }

    /// <summary>
    /// The tree as the ordinary emission would have built it, over the saved leaves rather than
    /// over the leaf expressions — so a leaf is evaluated once and the failing arm still coerces
    /// in the order it always did.
    /// </summary>
    private BExpression BuildGenericTree(AstExpression node, Dictionary<AstExpression, SpeculativeLeaf> compiled)
    {
        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
        {
            var left = BuildGenericTree(binary.Left, compiled);
            var right = BuildGenericTree(binary.Right, compiled);
            if (left == null || right == null)
                return null;

            return BinaryOperation.Operation(left, right, binary.Operator);
        }

        var leaf = compiled[node];
        return leaf.Local is { } local ? local : JSNumberBuilder.New(leaf.Native);
    }
}
