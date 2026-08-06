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
/// <b>Evaluation order is the whole correctness argument, and there are two emitters here because
/// of it.</b> The reference emission evaluates a node's two operands and then coerces them, so in
/// a nested tree a coercion happens <em>between</em> two leaf evaluations. <b>Hoisting</b> every
/// leaf into a temporary ahead of one combined test moves later leaves in front of that coercion —
/// and a coercion is observable, because <c>ToPrimitive</c> on an object runs <c>valueOf</c> — so
/// that form is eligible only when every leaf evaluated <em>after</em> the first internal node in
/// postorder is a numeric literal or a proven-numeric local.
/// </para>
/// <para>
/// Measured, that rule is most of the item: of 5 396 candidate arithmetic nodes on the Octane
/// corpus it refuses <b>1 762</b> outright, and it manufactures most of the <b>2 718</b> refused
/// for having no saving to make, because <c>+</c> is left-associative and a refused root re-offers
/// its children until only a lone operator is left. So <see cref="CreateOrderedNumericTree"/>
/// emits each leaf at its own postorder position and puts the test <b>where the coercion it stands
/// in for would have run</b>, which moves nothing and needs no purity rule at all. That is the
/// default; <c>BROILER_JS_NUMERIC_TREE_ORDER=0</c> restores the hoisting form for A/B.
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
    /// <summary>
    /// What will consume the guarded tree about to be compiled, when the visitor that is about to
    /// consume it said so (docs/performance-roadmap.md item 3-1, `0105`).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only ever set by <see cref="VisitConsumedBy"/>, and only for a node that <em>is</em> an
    /// <c>AstBinaryExpression</c> — the one shape that can reach the tree builder directly. That
    /// restriction is the whole safety argument: the field cannot survive into a nested visit,
    /// because it is never set for a node whose compilation would perform one. Without it,
    /// <c>a[i] = b + f(c * 2)</c> would attribute the inner tree inside the call argument to the
    /// element store, which is precisely the mis-attribution that would make this instrument agree
    /// with whatever it was pointed at.
    /// </para>
    /// <para>
    /// The tree builder clears it for the duration of its own construction for the same reason:
    /// its leaves are visited, and a leaf may itself contain a tree.
    /// </para>
    /// </remarks>
    private NumberBoxingConversionSite pendingTreeConsumer = NumberBoxingConversionSite.GuardedTreeRoot;

    /// <summary>
    /// When <see cref="pendingTreeConsumer"/> is a refused local, which refusal it is
    /// (docs/performance-roadmap.md item 3-1, `0106`).
    /// </summary>
    private NumericLocalMiss pendingLocalMiss = NumericLocalMiss.Unknown;

    /// <summary>
    /// Visits <paramref name="node"/>, telling a guarded tree at its root what will consume its
    /// box.
    /// </summary>
    private BExpression VisitConsumedBy(
        AstExpression node,
        NumberBoxingConversionSite consumer,
        NumericLocalMiss miss = NumericLocalMiss.Unknown)
    {
        if (node is not AstBinaryExpression)
            return Visit(node);

        var previousConsumer = pendingTreeConsumer;
        var previousMiss = pendingLocalMiss;
        pendingTreeConsumer = consumer;
        pendingLocalMiss = miss;
        try
        {
            return Visit(node);
        }
        finally
        {
            pendingTreeConsumer = previousConsumer;
            pendingLocalMiss = previousMiss;
        }
    }

    /// <summary>Why the numeric tier refused <paramref name="name"/>, if it did.</summary>
    private NumericLocalMiss MissFor(string name)
        => scope.Top?.NumericLocalMisses is { } m && m.TryGetValue(name, out var miss)
            ? miss
            : NumericLocalMiss.Unknown;

    private BExpression TryCreateSpeculativeNumericTree(AstBinaryExpression root)
    {
        if (!NumericSpeculation.Enabled)
            return null;

        // Not a numeric tree at all: not a candidate, and counting it would put every `===`,
        // `&&` and `instanceof` in the denominator of the refusal census below.
        if (!IsNativeNumericOperator(root.Operator))
            return null;

        // Already one the all-native path handles without a guard.
        if (IsNativeNumericAst(root))
            return Refuse(NumericTreeRefusal.AlreadyNative);

        // A `with` or an eval shadow can turn any identifier into a property read, which is
        // exactly the "leaf that can observe something" this argument depends on not happening.
        if (withBoundaries.Count != 0 || evalShadowBoundary != null)
            return Refuse(NumericTreeRefusal.WithOrEvalShadow);

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
        if (CountOperators(root) - 1 + CountNativeLeaves(root) + CountSpeculativeLeaves(root) < 1)
            return Refuse(NumericTreeRefusal.NoSavingToMake);

        // The hoisting form's order gate. Under the order-preserving emission nothing is hoisted,
        // so there is nothing for it to protect — see BuildOrderedTree.
        if (!NumericTreeOrdering.Enabled)
        {
            var seenCoercion = false;
            if (!HoistingIsOrderSafe(root, ref seenCoercion))
                return Refuse(NumericTreeRefusal.OrderUnsafe);
        }

        var leaves = new List<AstExpression>();
        CollectLeaves(root, leaves);

        // A code-size bound, not a correctness one — and one the order-preserving form made bite,
        // because the trees it accepts are whole chains rather than their right-hand ends.
        if (leaves.Count > MaximumSpeculativeLeaves)
            return Refuse(NumericTreeRefusal.TooManyLeaves);

        if (NumericTreeOrdering.Enabled)
            return CreateOrderedNumericTree(root);

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
                return Refuse(NumericTreeRefusal.StringLeaf);

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
            return Refuse(NumericTreeRefusal.NothingToGuard);

        var nativeTree = BuildNativeTree(root, compiled);
        if (nativeTree == null)
            return Refuse(NumericTreeRefusal.Unbuildable);

        var genericTree = BuildGenericTree(root, compiled);
        if (genericTree == null)
            return Refuse(NumericTreeRefusal.Unbuildable);

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
            JSNumberBuilder.New(nativeTree, NumberBoxingConversionSite.GuardedTreeRoot),
            genericTree,
            typeof(JSValue)));

        CompilerSpecializationDiagnostics.RecordSpeculativeNumericTree(guarded);
        CompilerSpecializationDiagnostics.RecordNumericTreeDecision(NumericTreeRefusal.Specialized);
        return BExpression.Block(locals, body);
    }

    /// <summary>
    /// Records a refusal and answers <c>null</c>, so the caller reads as one line per condition.
    /// </summary>
    /// <remarks>
    /// The counter is touched once per compiled site rather than per evaluation, so it is
    /// unconditional — there is no <c>Enabled</c> flag to read, as there is on the run-time
    /// censuses. It exists because item 3-1's own measurement left a question its numbers could
    /// not answer: the guarded form reached 0.583x of Crypto's generic invocations and 0.899x of
    /// NavierStokes', and nothing said whether the difference was one eligibility rule or five.
    /// </remarks>
    private BExpression Refuse(NumericTreeRefusal reason)
    {
        CompilerSpecializationDiagnostics.RecordNumericTreeDecision(reason);
        return null;
    }

    /// <summary>
    /// The guarded form of <paramref name="root"/> with each test placed where the coercion it
    /// stands in for would have run, or <c>null</c> when it cannot be built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> The hoisting form is bounded by evaluation order rather than by
    /// what the guard can predict, and the refusal census says that bound is most of the item:
    /// of 5 396 candidate arithmetic nodes across the Octane corpus, <b>1 762 are refused as
    /// order-unsafe and 2 718 more for having no saving to make</b> — and the second number is
    /// largely a consequence of the first, because a refused root re-offers its children and the
    /// bottom node of a chain is a single operator with nothing left to save. Only 862 specialize.
    /// </para>
    /// <para>
    /// <b>Why it is sound, and it is the same argument the hoisting form makes — from the other
    /// end.</b> The reference emission evaluates a node's two operands and then coerces them, so
    /// the coercion of the left operand runs <em>after</em> the right operand is evaluated and
    /// <em>before</em> anything above the node is. Hoisting moves later leaves in front of that
    /// coercion, which is why it needs them pure. This emits each leaf at its own postorder
    /// position — so no evaluation moves at all — and puts the type test exactly where the
    /// coercion was. When the test holds, the coercion it replaces was the identity (ToNumeric of
    /// a Number is that Number), so nothing observable is skipped. When it fails, the same generic
    /// operator runs at the same point, over the values already in hand.
    /// </para>
    /// <para>
    /// <b>What that costs is a two-armed node rather than a two-armed tree.</b> Each internal node
    /// carries three temporaries — a <c>bool</c> saying the subtree stayed numeric, a raw
    /// <c>double</c> holding its value when it did, and a <c>JSValue</c> holding it when it did
    /// not — and one branch. So a failure part-way up no longer discards the native work below it:
    /// the accumulated double is boxed once, at the node that failed, and the rest of the tree
    /// proceeds generically. The hoisting form has to fall back to the whole generic tree.
    /// </para>
    /// <para>
    /// <b>It admits no leaf the hoisting form refused for a reason that still applies.</b> A
    /// getter, a call, an element read and a plain identifier are all evaluated exactly where they
    /// were; what changes is only that the test no longer has to precede them.
    /// </para>
    /// </remarks>
    private BExpression CreateOrderedNumericTree(AstBinaryExpression root)
    {
        var locals = new Sequence<BParameterExpression>();
        var body = new Sequence<BExpression>();
        var guarded = 0;

        // Take the consumer before any leaf is visited, and clear it for the duration: a leaf may
        // contain a tree of its own, and that one's box is consumed by this tree's operator rather
        // than by whatever consumes this tree's root.
        var consumer = pendingTreeConsumer;
        var miss = pendingLocalMiss;
        pendingTreeConsumer = NumberBoxingConversionSite.GuardedTreeRoot;
        pendingLocalMiss = NumericLocalMiss.Unknown;
        try
        {
            return BuildOrderedNumericTree(root, locals, body, ref guarded, consumer, miss);
        }
        finally
        {
            pendingTreeConsumer = consumer;
            pendingLocalMiss = miss;
        }
    }

    private BExpression BuildOrderedNumericTree(
        AstBinaryExpression root,
        Sequence<BParameterExpression> locals,
        Sequence<BExpression> body,
        ref int guarded,
        NumberBoxingConversionSite consumer,
        NumericLocalMiss miss)
    {
        var built = BuildOrderedTree(root, locals, body, ref guarded, out var refusal);
        if (built is not { } node)
            return Refuse(refusal);

        // Nothing to test means the all-native path already had it, by a route IsNativeNumericAst
        // did not recognize.
        if (guarded == 0)
            return Refuse(NumericTreeRefusal.NothingToGuard);

        body.Add(AsJSValue(node, consumer, miss));

        CompilerSpecializationDiagnostics.RecordSpeculativeNumericTree(guarded);
        CompilerSpecializationDiagnostics.RecordNumericTreeDecision(NumericTreeRefusal.Specialized);
        return BExpression.Block(locals, body);
    }

    /// <summary>
    /// One subtree of an order-preserving guarded tree: its value as a raw double while the
    /// speculation holds, the test that says whether it does, and its value as a
    /// <c>JSValue</c> when it does not.
    /// </summary>
    /// <param name="Ok">
    /// <c>null</c> when the subtree is statically native — a numeric literal, a proven-numeric
    /// local, or an operator over two of them — in which case there is no test and no
    /// <see cref="Value"/>.
    /// </param>
    /// <param name="IsLeaf">
    /// A guarded leaf's <see cref="Value"/> is the operand itself and is correct whether or not
    /// the test holds, so it needs no conditional to read; an internal node's does. Read as a
    /// property of the VALUE rather than of the syntax — item 3-8a's speculative leaf is a leaf
    /// and sets this <c>false</c>, because the slot it carries is stale exactly while its test
    /// holds.
    /// </param>
    private readonly record struct OrderedNode(
        BExpression Native,
        BExpression Ok,
        BExpression Value,
        bool IsLeaf);

    private OrderedNode? BuildOrderedTree(
        AstExpression node,
        Sequence<BParameterExpression> locals,
        Sequence<BExpression> body,
        ref int guarded,
        out NumericTreeRefusal refusal)
    {
        refusal = NumericTreeRefusal.Specialized;

        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
        {
            // Postorder, and it is the whole correctness argument: the left subtree's leaves are
            // emitted before the right subtree's, exactly as the reference emission evaluates them.
            var left = BuildOrderedTree(binary.Left, locals, body, ref guarded, out refusal);
            if (left is not { } l)
                return null;

            var right = BuildOrderedTree(binary.Right, locals, body, ref guarded, out refusal);
            if (right is not { } r)
                return null;

            var native = TryCreateNativeNumericValue(binary.Operator, l.Native, r.Native);
            if (native == null)
            {
                refusal = NumericTreeRefusal.Unbuildable;
                return null;
            }

            // Both sides statically native: no test, no branch, and the value stays a raw double
            // all the way up. This is the all-native path, reached one node at a time.
            if (l.Ok == null && r.Ok == null)
                return new OrderedNode(native, null, null, IsLeaf: false);

            // Named off the local count, which is strictly increasing, rather than off the guarded
            // count, which is not: two nodes above the same leaf would otherwise share a name.
            var index = locals.Count;
            var ok = BExpression.Parameter(typeof(bool), "#ok" + index);
            var value = BExpression.Parameter(typeof(double), "#num" + index);
            var boxed = BExpression.Parameter(typeof(JSValue), "#gen" + index);
            locals.Add(ok);
            locals.Add(value);
            locals.Add(boxed);

            var test = l.Ok == null
                ? r.Ok
                : r.Ok == null
                    ? l.Ok
                    : BExpression.Binary(l.Ok, BOperator.BooleanAnd, r.Ok);

            // `ok = test ? (num = <native>, true) : (gen = <generic>, false)`. Written as one
            // assignment rather than as a statement-level branch so both arms have a type: the
            // block's type is its last expression's.
            body.Add(BExpression.Assign(ok, BExpression.Conditional(
                test,
                BExpression.Block(
                    BExpression.Assign(value, native),
                    BExpression.Constant(true)),
                BExpression.Block(
                    BExpression.Assign(boxed, BinaryOperation.Operation(
                        AsJSValue(l, NumberBoxingConversionSite.GuardedTreeOperand),
                        AsJSValue(r, NumberBoxingConversionSite.GuardedTreeOperand),
                        binary.Operator)),
                    BExpression.Constant(false)),
                typeof(bool))));

            return new OrderedNode(value, ok, boxed, IsLeaf: false);
        }

        // Item 3-8a: a speculative local is ALREADY the shape this tree wants — a raw double, a
        // flag saying it is live, and a JSValue to fall back on. Offering it directly is what
        // makes the dual representation pay: without this the leaf goes through `Expression`,
        // which boxes on the very arm the item exists to keep unboxed.
        //
        // It is still SNAPSHOTTED rather than read in place, and that is not caution. A later
        // leaf can assign to the same local — `c + f()` where `f` writes `c` — and the reference
        // emission reads `c` before `f` runs. Copying two CLR locals and a reference preserves
        // that for nothing; reading the storage at the node instead would answer with the value
        // the side effect left. The flag is snapshotted for the same reason and it is the half
        // easiest to leave out: a side effect that turns the local into a String changes which
        // ARM is correct, not just what the double is worth.
        //
        // NOT a leaf for AsJSValue's purposes, and this is the one place the two representations
        // are not interchangeable. `IsLeaf` means "the saved operand is the value whichever way
        // the test went" — true of a guarded leaf, which saved the JSValue it was handed, and
        // false of this one, whose slot is DELIBERATELY STALE while the flag is up. So the
        // generic arm has to re-materialize from the flag, which costs a box on exactly the arm
        // that was going to box anyway.
        if (NumericTreeOrdering.Enabled
            && node is AstIdentifier speculativeName
            && TryGetStaticIdentifierVariable(speculativeName, out var speculativeVariable)
            && speculativeVariable?.SpeculativeNumericFlag != null)
        {
            var rawCopy = BExpression.Parameter(typeof(double), "#specnum" + guarded);
            var flagCopy = BExpression.Parameter(typeof(bool), "#specok" + guarded);
            var slotCopy = BExpression.Parameter(typeof(JSValue), "#specval" + guarded);
            locals.Add(rawCopy);
            locals.Add(flagCopy);
            locals.Add(slotCopy);
            body.Add(BExpression.Assign(flagCopy, speculativeVariable.SpeculativeNumericFlag));
            body.Add(BExpression.Assign(rawCopy, speculativeVariable.SpeculativeNumericStorage));
            body.Add(BExpression.Assign(slotCopy, speculativeVariable.SpeculativeSlot));
            guarded++;

            return new OrderedNode(rawCopy, flagCopy, slotCopy, IsLeaf: false);
        }

        // A leaf. Visited exactly once, here, at its own position in the reference order.
        var (isString, isNumber, expression) = ToNativeExpression(node);

        // A leaf the compiler already knows is a String makes the test unsatisfiable, and under
        // `+` it makes the answer a concatenation. Nothing to win.
        if (isString)
        {
            refusal = NumericTreeRefusal.StringLeaf;
            return null;
        }

        if (isNumber)
            return new OrderedNode(expression, null, null, IsLeaf: true);

        // Block-declared rather than pooled, for the reason item 3-5 records: a pooled temp
        // acquired after the operands were compiled can be one that compiling them released, and
        // the next assignment would clobber a value already saved.
        var temp = BExpression.Parameter(typeof(JSValue), "#spec" + guarded);
        locals.Add(temp);
        body.Add(BExpression.Assign(temp, expression));
        guarded++;

        return new OrderedNode(
            JSValueBuilder.DoubleValue(temp),
            JSValueBuilder.IsNumber(temp),
            temp,
            IsLeaf: true);
    }

    /// <summary>The subtree's value as a <c>JSValue</c>, which is what the generic arm needs.</summary>
    /// <param name="site">
    /// Which boxing the caller is asking for. The same helper serves the tree's ROOT — the one box
    /// a specialized tree is meant to keep — and the OPERANDS an interior node hands to its generic
    /// arm, and item 3-1's remaining prize depends entirely on which of the two a run is paying
    /// for. A root box is the design working; an operand box is a leaf the speculation could not
    /// keep native.
    /// </param>
    private static BExpression AsJSValue(
        OrderedNode node,
        NumberBoxingConversionSite site,
        NumericLocalMiss miss = NumericLocalMiss.Unknown)
    {
        if (node.Ok == null)
            return Box(node.Native, site, miss);

        // A guarded leaf's saved operand is already the value, whichever way its test went — so
        // reading it costs nothing and, more to the point, does not re-test it.
        if (node.IsLeaf)
            return node.Value;

        return BExpression.Condition(
            node.Ok,
            Box(node.Native, site, miss),
            node.Value,
            typeof(JSValue));
    }

    /// <summary>
    /// The root's box, routed through the entry that names the numeric-tier refusal when the
    /// consumer is a local the tier turned down (docs/performance-roadmap.md item 3-1, `0106`).
    /// </summary>
    private static BExpression Box(BExpression native, NumberBoxingConversionSite site, NumericLocalMiss miss)
        => site == NumberBoxingConversionSite.GuardedTreeRootIntoLocal
            ? JSNumberBuilder.NewIntoRefusedLocal(native, miss)
            : JSNumberBuilder.New(native, site);

    /// <summary>
    /// Leaves above which a chain of type tests stops being worth one saved box. Not a
    /// correctness bound.
    /// </summary>
    /// <remarks>
    /// <b>Measured rather than reasoned, because the last invented threshold in this file cost
    /// half the prize.</b> It was 8 while the hoisting form was the only emitter, where it never
    /// fired at all on the Octane corpus — the order rule refused those trees first. The
    /// order-preserving form accepts whole chains, and at 8 it turned down 85 of them (80 of them
    /// Box2D's). At 16 that is 8, and the corpus loses a further <b>664 338 boxes, 2.1% of what
    /// the ordered form leaves</b> — Box2D 0.954x, NavierStokes 0.983x, Crypto 0.985x — while the
    /// tree count goes DOWN (Box2D 1 109 to 1 090), because a longer chain absorbs sub-trees that
    /// were separately specialized. The 8 that remain above 16 are a bounded, named residue.
    /// </remarks>
    private const int MaximumSpeculativeLeaves = 16;

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

    /// <summary>
    /// Leaves that are item 3-8a's speculative locals — each one a box the ordinary emission mints
    /// <em>conditionally</em>, because the variable's readable expression boxes the raw half back
    /// up whenever the flag is live.
    /// </summary>
    /// <remarks>
    /// Counted alongside <see cref="CountNativeLeaves"/> rather than inside it, because it is not
    /// the same kind of saving: a proven-numeric leaf is one box removed, and this is one box
    /// removed on the arm the speculation wins and nothing changed on the arm it loses.
    /// <para>
    /// It is nevertheless the difference between a tree being built and refused. <c>c + p.v</c> has
    /// one operator and no native leaf, so <c>CountOperators - 1 + CountNativeLeaves</c> is zero and
    /// the tree is turned down for having no saving to make — which is <b>the shape item 3-8a's
    /// population is mostly made of</b>. Without this term the speculative leaf below is reachable
    /// only from trees that were already eligible for some other reason, i.e. it is very nearly
    /// dead code, and the read half of the item does not happen.
    /// </para>
    /// <para>
    /// Self-gating: with <c>BROILER_JS_SPECULATIVE_NUMERIC_LOCALS</c> off no variable carries a
    /// flag, so this is zero and the eligibility rule is byte-for-byte the one the control arm had.
    /// </para>
    /// </remarks>
    private int CountSpeculativeLeaves(AstExpression node)
    {
        if (node is AstBinaryExpression binary && IsNativeNumericOperator(binary.Operator))
            return CountSpeculativeLeaves(binary.Left) + CountSpeculativeLeaves(binary.Right);

        return node is AstIdentifier name
            && TryGetStaticIdentifierVariable(name, out var variable)
            && variable?.SpeculativeNumericFlag != null
            ? 1
            : 0;
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

        if (!seenCoercion || IsPureLeaf(node))
            return true;

        // The sub-census: which kind of leaf this conjunct is actually refusing. Recorded at the
        // FIRST blocking leaf, so it reads against the OrderUnsafe row one-for-one.
        CompilerSpecializationDiagnostics.RecordNumericTreeOrderBlocker(ClassifyOrderBlocker(node));
        return false;
    }

    private static NumericTreeOrderBlocker ClassifyOrderBlocker(AstExpression node)
        => node switch
        {
            AstMemberExpression { Computed: true } => NumericTreeOrderBlocker.ElementRead,
            AstMemberExpression => NumericTreeOrderBlocker.PropertyRead,
            AstCallExpression or AstNewExpression => NumericTreeOrderBlocker.CallResult,
            AstIdentifier => NumericTreeOrderBlocker.OtherName,
            _ => NumericTreeOrderBlocker.Other,
        };

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
        return leaf.Local is { } local ? local : JSNumberBuilder.New(leaf.Native, NumberBoxingConversionSite.GuardedTreeOperand);
    }
}
