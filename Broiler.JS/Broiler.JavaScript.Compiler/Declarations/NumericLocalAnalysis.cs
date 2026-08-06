using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Finds the <c>var</c> locals of a function that provably only ever hold a JavaScript
/// number, so the compiler can keep them in a CLR <c>double</c> instead of a heap-allocated
/// <see cref="Broiler.JavaScript.Runtime.JSValue"/>
/// (docs/performance-roadmap.md P2-2 item 3).
/// </summary>
/// <remarks>
/// <para>
/// JavaScript locals are dynamically typed, so this has to <em>prove</em> the type rather
/// than assume it. The analysis is an optimistic fixed point: every candidate name starts
/// out assumed numeric, and a name is dropped as soon as anything is found that could give
/// it another type. Dropping one can invalidate another (<c>a = b + 1</c> is only numeric
/// while <c>b</c> is), so the sweep repeats until nothing changes. Starting optimistic is
/// what lets a self-referential loop counter — <c>i = i + 1</c>, which depends on itself —
/// come out numeric at all.
/// </para>
/// <para>
/// The awkward part is not the type, it is <c>var</c> hoisting: a <c>var</c> is observably
/// <c>undefined</c> from function entry until its initializer runs, and <c>undefined</c> is
/// not a double. Rather than a definite-assignment dataflow, this requires the declaration
/// to be a direct statement of the function body (or the init of a top-level <c>for</c>) and
/// requires no reference to the name to appear textually before it. Together those mean the
/// initializer has always run before any read: a preceding top-level statement either
/// completes — and none of them mentions the name — or leaves the function, in which case
/// nothing after it runs.
/// </para>
/// <para>
/// A <c>var</c> declared inside a nested block gets the <em>same</em> argument, scoped to that
/// block instead of to the function body (docs/performance-roadmap.md item 3-3): the
/// declaration must be a direct statement of the block, and every reference to the name must
/// be inside that block and textually after it. Then any read is preceded by the initializer
/// for exactly the reason above — one level down. The containment half is what the function
/// body gets for free and a nested block does not: control can reach code *after* the block
/// without having entered it, so a reference outside the block could observe the binding
/// still <c>undefined</c>. This is a structural sufficient condition, not a full
/// definite-assignment dataflow; it admits the common case (a temporary declared and consumed
/// inside a loop body) and refuses everything it cannot see, including
/// <c>if (c) var x = 1;</c> with no block of its own, whose enclosing block does not dominate
/// it.
/// </para>
/// </remarks>
internal sealed class NumericLocalAnalysis
{
    private readonly HashSet<string> candidates = new(System.StringComparer.Ordinal);
    private readonly HashSet<string> rejected = new(System.StringComparer.Ordinal);
    private readonly List<(string Name, AstExpression Value)> assignments = [];

    /// <summary>
    /// Names this function declares with <c>const</c> at body top level. A write to one is a
    /// TypeError raised by the binding's <c>JSVariable</c> cell, and a raw double has no cell
    /// to raise it, so any such name that is written anywhere is rejected rather than
    /// specialized into a store that would silently succeed.
    /// </summary>
    private readonly HashSet<string> constants = new(System.StringComparer.Ordinal);

    /// <summary>
    /// For a <c>var</c> declared inside a nested block, the block whose entry proves its
    /// initializer has run. Every reference to such a name must be inside that block; one
    /// outside it could observe the binding still <c>undefined</c>, which a raw double hoisted
    /// to 0 cannot represent. Names declared at function-body top level are absent here — the
    /// function body dominates everything, so they carry no containment condition.
    /// </summary>
    private readonly Dictionary<string, AstBlock> confinedTo = new(System.StringComparer.Ordinal);

    /// <summary>
    /// Names whose definite assignment rests on a declaration the function body dominates, and
    /// the initializer expressions of those declarations.
    /// </summary>
    /// <remarks>
    /// Such a name becomes readable at ITS declaration, not at any other declaration of the
    /// same name. Without that distinction a non-dominating declaration textually earlier —
    /// <c>if (c) { var t = 1; } s = t; { var t = 2; }</c> — marks the name as declared and
    /// masks the read between the two, which can observe <c>undefined</c>. The initializer
    /// node is the identity because a declarator is a compact struct with no reference of its
    /// own; every offered declarator has a non-null one.
    /// </remarks>
    private readonly HashSet<string> dominatingNames = new(System.StringComparer.Ordinal);
    private readonly HashSet<AstExpression> dominatingInits =
        new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

    private bool HasDominatingDeclaration(string name) => dominatingNames.Contains(name);

    private bool IsDominatingInit(AstExpression init) => init != null && dominatingInits.Contains(init);

    /// <summary>
    /// The names of the function's <c>var</c> locals that can be held as a CLR
    /// <c>double</c>, or an empty set when none qualify.
    /// </summary>
    public static IReadOnlySet<string> Analyze(AstFunctionExpression function)
        => Analyze(function, out _);

    /// <param name="misses">
    /// Why each name the analysis did not admit was refused (docs/performance-roadmap.md item 3-1,
    /// `0106`). Handed back rather than only counted, so a boxing site can name the refusal that
    /// costs it and the refusals can be ranked by execution weight instead of by declaration count.
    /// </param>
    public static IReadOnlySet<string> Analyze(
        AstFunctionExpression function,
        out IReadOnlyDictionary<string, NumericLocalMiss> misses)
    {
        var analysis = new NumericLocalAnalysis();
        analysis.Collect(function);
        var survivors = analysis.Resolve();
        misses = analysis.Misses(survivors);
        return survivors;
    }

    /// <summary>
    /// The names item 3-8a would speculate on: the ones this function's own fixed point keeps
    /// <em>only</em> when a name from outside the function is assumed to hold a number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Computed by difference against the real fixed point rather than by a new rule, so the set
    /// cannot drift from what the analysis actually does: run the same resolution a second time
    /// with <see cref="IsNumeric"/> treating an identifier this function neither declares nor
    /// takes as a parameter — a global, or a binding from an enclosing scope — as numeric, and
    /// subtract the real survivors. A name dropped for a parameter, a property read, an element
    /// read or a call's return survives neither pass and is therefore absent; those want a guard
    /// at the value's own source and are a different item.
    /// </para>
    /// <para>
    /// <b>The transitive case is included for free</b>, because the second pass is a fixed point
    /// like the first: <c>var r = rowSize; var c = 2 * r;</c> keeps both, and the cascade the
    /// drop-cause counter reports as <c>DroppedCandidate</c> resolves rather than being counted
    /// separately.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The names item 3-9 would type STATICALLY: the ones this function's own fixed point keeps
    /// only when an identifier resolving to a numeric local of an ENCLOSING function is treated as
    /// the double it already is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The difference from <see cref="AnalyzeSpeculative"/> is the whole item, and it is the
    /// difference between assuming and knowing. 3-8a assumes any name from outside the function
    /// holds a number and pays for a run-time test; this one accepts only a name the enclosing
    /// scope has <em>already proved</em> is a raw <c>double</c>, so there is no test, no flag, no
    /// fallback representation — the local it types is an ordinary numeric local, and every fast
    /// path that reads <c>NumericStorage</c> works on it unchanged. **That is why 3-8a's failure
    /// mode cannot happen here**: 3-8a lost because its reads had to box the raw half back up, and
    /// a 3-9 name has no raw half to box.
    /// </para>
    /// <para>
    /// <b>Therefore 3-9's population is a strict subset of 3-8a's</b>, which is a bound the count
    /// can be checked against rather than a claim about it: everything the enclosing scope has
    /// proved numeric is also something an optimistic pass would have assumed numeric. A global is
    /// in 3-8a's set and not in this one — it has no enclosing binding to have proved anything —
    /// and so is an outer name the enclosing analysis dropped.
    /// </para>
    /// <para>
    /// <paramref name="outerNumeric"/> answers for the ENCLOSING scope chain, and it is asked about
    /// the binding rather than the spelling: a name this function declares or takes as a parameter
    /// shadows the outer one and is excluded before the probe is consulted, exactly as 3-8a's pass
    /// excludes them.
    /// </para>
    /// </remarks>
    public static IReadOnlySet<string> AnalyzeImportedOuter(
        AstFunctionExpression function, System.Func<string, bool> outerNumeric)
    {
        if (outerNumeric == null)
            return System.Collections.Immutable.ImmutableHashSet<string>.Empty;

        var real = new NumericLocalAnalysis();
        real.Collect(function);
        var proven = real.Resolve(count: false);

        var imported = new NumericLocalAnalysis { outerNumericNames = outerNumeric };
        imported.Collect(function);
        var withOuter = imported.Resolve(count: false);

        if (withOuter.Count == 0 && !ElementReadNumericLocals.Enabled)
            return System.Collections.Immutable.ImmutableHashSet<string>.Empty;

        var gained = new HashSet<string>(withOuter, System.StringComparer.Ordinal);
        gained.ExceptWith(proven);
        return gained;
    }

    public static IReadOnlySet<string> AnalyzeSpeculative(AstFunctionExpression function)
    {
        var real = new NumericLocalAnalysis();
        real.Collect(function);
        var proven = real.Resolve(count: false);

        var optimistic = new NumericLocalAnalysis { assumeOuterNamesAreNumeric = true };
        optimistic.Collect(function);
        var withOuter = optimistic.Resolve(count: false);

        if (withOuter.Count == 0)
            return System.Collections.Immutable.ImmutableHashSet<string>.Empty;

        var speculative = new HashSet<string>(withOuter, System.StringComparer.Ordinal);
        speculative.ExceptWith(proven);

        // Item 3-1's widening, unioned in rather than replacing: the two populations are refused
        // for different conjuncts and are measured apart, so a run can have either, both or
        // neither. `0108` refutes CallResult, NeverOffered and PropertyRead at the bound, so no
        // pass here offers them.
        if (ElementReadNumericLocals.Enabled)
        {
            var widened = new NumericLocalAnalysis { assumeElementReadsAreNumeric = true };
            widened.Collect(function);
            var withElements = widened.Resolve(count: false);

            foreach (var name in withElements)
            {
                if (!proven.Contains(name))
                    speculative.Add(name);
            }
        }

        return speculative;
    }

    /// <summary>
    /// Whether an identifier this function neither declares nor takes as a parameter is assumed
    /// to hold a number. Set only for <see cref="AnalyzeSpeculative"/>'s second pass.
    /// </summary>
    private bool assumeOuterNamesAreNumeric;

    /// <summary>
    /// Whether a computed ELEMENT read is assumed to hold a number — item 3-1's widening
    /// (docs/performance-roadmap.md item 3-1). Set only for
    /// <see cref="AnalyzeSpeculative"/>'s element-read pass.
    /// </summary>
    /// <remarks>
    /// It is an assumption and not a proof: <c>a[i]</c> can hold anything, which is exactly why the
    /// names it keeps go to the SPECULATIVE representation and never to the sound tier. The cascade
    /// needs no rule of its own — the pass is a fixed point, so a name kept only because a widened
    /// one was kept is kept with it, which is the same freeness item 3-8a's pass already relies on.
    /// </remarks>
    private bool assumeElementReadsAreNumeric;

    /// <summary>
    /// This function's parameter names, so a drop caused by reading one is attributed to the
    /// parameter rather than lumped in with globals (docs/performance-roadmap.md item 3-8).
    /// </summary>
    private readonly HashSet<string> parameterNames = new(System.StringComparer.Ordinal);

    /// <summary>
    /// Every name this function declares at any depth, whether or not it was ever offered as a
    /// numeric candidate. Item 3-8a's optimistic pass needs it to tell a name from OUTSIDE the
    /// function — the thing it assumes numeric — from one inside it that simply never qualified
    /// (<c>var a = []</c>). Without the distinction the pass would assume the second numeric too
    /// and report a population that no run-time test on an enclosing value could ever reach.
    /// </summary>
    private readonly HashSet<string> declaredNames = new(System.StringComparer.Ordinal);

    /// <summary>
    /// Item 3-9: asks the ENCLOSING scope chain whether a name is already a raw <c>double</c>
    /// there. <c>null</c> on every pass but the imported one, so the analysis is unchanged for
    /// every other caller.
    /// </summary>
    private System.Func<string, bool> outerNumericNames;

    /// <summary>
    /// Every name that was ever offered as a numeric candidate. A drop caused by reading one of
    /// these is a CASCADE — the name is dropped because its source was — and wants no guard of
    /// its own, which is the distinction item 3-8's premise does not make.
    /// </summary>
    private readonly HashSet<string> everOffered = new(System.StringComparer.Ordinal);

    /// <summary>
    /// Why each name the analysis did not admit was refused, kept per NAME so the refusal can be
    /// weighted by the boxes it costs at run time (docs/performance-roadmap.md item 3-1, `0106`).
    /// </summary>
    /// <remarks>
    /// The existing counters answer "how many names does each cause refuse", which is what item
    /// 3-6 asked. This answers "which refusal is on the hot path", which is what `0105` made the
    /// question: 44.36% of the corpus's root boxes are consumed by a local, and a per-name census
    /// weights a name refused in initialization code the same as one refused inside a ten-million
    /// iteration loop.
    /// </remarks>
    private readonly Dictionary<string, NumericLocalMiss> misses = new(System.StringComparer.Ordinal);

    private void Collect(AstFunctionExpression function)
    {
        // A parameter shares its name with no eligible var: the value arrives as a JSValue
        // and nothing here proves it is a number.
        var parameters = function.Params.GetFastEnumerator();
        while (parameters.MoveNext(out var parameter))
        {
            CollectParameterNames(parameter.Identifier);
            RejectEveryNameIn(parameter.Identifier);
            RejectEveryNameIn(parameter.Init);
        }

        if (function.Body is not AstBlock body)
            return;

        var collector = new Collector(this, body);
        OfferDominatedDeclarations(body, isFunctionBody: true);
        collector.Visit(body);
    }

    /// <summary>
    /// Offers the declarations in <paramref name="block"/>'s own statement list that the
    /// function body dominates, and recurses through any plain nested block, which it also
    /// dominates.
    /// </summary>
    /// <remarks>
    /// An unlabelled <c>{ … }</c> that is a direct statement of a dominated block is entered
    /// whenever control reaches it, and the only ways out of it are <c>return</c> and
    /// <c>throw</c>, both of which leave the function — so nothing after it runs either. That
    /// makes it transparent to the dominance argument, and a <c>var</c> inside one needs no
    /// containment condition. A block reached through anything else — a label (which
    /// <c>break</c> can jump out of, skipping the declaration), an <c>if</c>, a loop, a
    /// <c>try</c>, a <c>switch</c> — is not transparent, and its declarations are handled by
    /// the confined path instead.
    /// </remarks>
    private void OfferDominatedDeclarations(AstBlock block, bool isFunctionBody)
    {
        var statements = block.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            switch (statement)
            {
                // `let` and `const` are offered on the same terms as `var`, but only in the
                // function body itself: a lexical binding in a nested block is a fresh binding
                // per entry, which a single raw double cannot represent. The temporal dead
                // zone is discharged by the dominance argument rather than removed — a name
                // with any reference before its declaration is rejected, so the TDZ throw is
                // unreachable on exactly the names that qualify. Const-ness needs one addition,
                // in VisitBinaryExpression/VisitUnaryExpression below — a write to a const is a
                // TypeError raised by the binding's cell, so a const written anywhere is
                // rejected outright rather than specialized into a silent store.
                case AstVariableDeclaration
                {
                    Kind: FastVariableKind.Let or FastVariableKind.Const
                } lexical when isFunctionBody:
                    if (lexical.Kind == FastVariableKind.Const)
                        NoteConstDeclaration(lexical);
                    OfferDeclaration(lexical);
                    break;

                case AstVariableDeclaration { Kind: FastVariableKind.Var } declaration:
                    OfferDeclaration(declaration);
                    break;

                // A `for` head declares into the loop's own scope for let/const (a fresh
                // binding per iteration), so only `var` is offered from there.
                case AstForStatement { Init: AstVariableDeclaration { Kind: FastVariableKind.Var } forInit }:
                    OfferDeclaration(forInit);
                    break;

                // Transparent: see the remarks above.
                case AstBlock nested:
                    OfferDominatedDeclarations(nested, isFunctionBody: false);
                    break;
            }
        }
    }

    /// <summary>
    /// Offers the <c>var</c> declarations that are DIRECT statements of <paramref name="block"/>,
    /// each confined to it. Called on entry to every block but the function body's own, so the
    /// offer is in place before the walk reaches any reference.
    /// </summary>
    /// <remarks>
    /// Only a direct statement qualifies. A declaration one level further down — in an
    /// <c>if</c> with no braces of its own, a <c>case</c>, a nested loop header — is not
    /// dominated by the block's entry, so it is passed over here and stays ineligible.
    /// </remarks>
    private void OfferBlockConfinedDeclarations(AstBlock block)
    {
        var statements = block.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            switch (statement)
            {
                // `let`/`const` are absent on purpose: a lexical binding declared in a nested
                // block is a fresh binding per entry, which a single raw double cannot
                // represent. Only the function body's own lexical declarations are offered
                // (in Collect), where there is exactly one entry per call.
                case AstVariableDeclaration { Kind: FastVariableKind.Var } declaration:
                    OfferConfinedDeclaration(declaration, block);
                    break;

                // The init of a `for` that is itself a direct statement of the block: it runs
                // whenever the loop is reached, so the block's entry dominates it too.
                case AstForStatement { Init: AstVariableDeclaration { Kind: FastVariableKind.Var } forInit }:
                    OfferConfinedDeclaration(forInit, block);
                    break;
            }
        }
    }

    private void OfferConfinedDeclaration(AstVariableDeclaration declaration, AstBlock block)
    {
        var declarators = declaration.Declarators.GetFastEnumerator();
        while (declarators.MoveNext(out var declarator))
        {
            if (declarator.Identifier is not AstIdentifier identifier)
            {
                RejectEveryNameIn(declarator.Identifier);
                continue;
            }

            var name = identifier.Name.Value;
            if (declarator.Init == null)
            {
                // `var x;` leaves x as undefined, which a double cannot represent.
                rejected.Add(name);
                continue;
            }

            // Already confined to some other block: two nested declarations, and neither
            // block dominates the other. Give up rather than reason about which one wins.
            if (confinedTo.ContainsKey(name))
            {
                rejected.Add(name);
                continue;
            }

            // Already offered at function-body top level, which dominates this block. Leave it
            // unconfined — Collector.VisitVariableDeclarator still records this declaration's
            // value, so a non-numeric one here still drops the name.
            if (candidates.Contains(name))
                continue;

            candidates.Add(name);
            confinedTo[name] = block;
            assignments.Add((name, declarator.Init));
        }
    }

    private void NoteConstDeclaration(AstVariableDeclaration declaration)
    {
        var declarators = declaration.Declarators.GetFastEnumerator();
        while (declarators.MoveNext(out var declarator))
        {
            if (declarator.Identifier is AstIdentifier identifier)
                constants.Add(identifier.Name.Value);
        }
    }

    /// <summary>
    /// Records a write to <paramref name="name"/>. A const declared at body top level cannot
    /// be assigned, so a write to one means the program is relying on the cell's TypeError and
    /// the name must keep its cell.
    /// </summary>
    private void NoteWrite(string name)
    {
        if (constants.Contains(name))
            rejected.Add(name);
    }

    private void OfferDeclaration(AstVariableDeclaration declaration)
    {
        var declarators = declaration.Declarators.GetFastEnumerator();
        while (declarators.MoveNext(out var declarator))
        {
            // A destructuring pattern binds through the generic path; only a plain
            // identifier with an initializer is a candidate.
            if (declarator.Identifier is not AstIdentifier identifier)
            {
                RejectEveryNameIn(declarator.Identifier);
                continue;
            }

            var name = identifier.Name.Value;
            if (declarator.Init == null)
            {
                // `var x;` leaves x as undefined, which a double cannot represent.
                rejected.Add(name);
                continue;
            }

            // Declared twice at DOMINATING positions — `var s = 0; { var s = 5; }`, or two
            // body-level declarations. That is not the hazard the old wording feared ("the
            // second may sit somewhere the first does not dominate"): every declaration offered
            // here dominates everything after itself, and `declared` opens at whichever comes
            // first in source order, so a read before all of them is still rejected and a read
            // after `declared` opens is after some initializer has run. The collector records
            // each declaration's value, so a non-numeric one still drops the name. Rejecting
            // here instead would undo the re-declaration case
            // `NumericLocalWriteVisibilityTests.ANumericReDeclarationKeepsTheLocalSpecialized`
            // pins — which is what caught it.
            candidates.Add(name);
            dominatingNames.Add(name);
            dominatingInits.Add(declarator.Init);
            assignments.Add((name, declarator.Init));
        }
    }

    private void RejectEveryNameIn(AstExpression expression)
    {
        if (expression == null)
            return;

        var names = new NameCollector();
        names.Visit(expression);
        foreach (var name in names.Names)
            rejected.Add(name);
    }

    private void CollectParameterNames(AstExpression identifier)
    {
        if (identifier == null)
            return;

        var names = new NameCollector();
        names.Visit(identifier);
        foreach (var name in names.Names)
            parameterNames.Add(name);
    }

    /// <summary>
    /// What defeated <paramref name="expression"/>'s numeric proof, attributed to the first leaf
    /// the analysis will not type (docs/performance-roadmap.md item 3-8).
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="IsNumeric"/> branch for branch — it descends exactly where that method
    /// recurses and stops exactly where it returns false — so the two cannot disagree about which
    /// sub-expression is responsible. For a binary or a conditional the LEFT/consequent side is
    /// checked first, matching evaluation order, so `a.x + f()` is charged to the property read.
    /// </remarks>
    private NumericDropCause Classify(AstExpression expression)
    {
        switch (expression)
        {
            case null:
                return NumericDropCause.Other;

            case AstLiteral { TokenType: TokenTypes.Number }:
                return NumericDropCause.Other;

            case AstLiteral:
                return NumericDropCause.NonNumericLiteral;

            case AstIdentifier identifier:
                var name = identifier.Name.Value;
                return everOffered.Contains(name) ? NumericDropCause.DroppedCandidate
                    : parameterNames.Contains(name) ? NumericDropCause.Parameter
                    : NumericDropCause.OtherName;

            case AstMemberExpression member:
                return member.Computed ? NumericDropCause.ElementRead : NumericDropCause.PropertyRead;

            case AstCallExpression:
            case AstNewExpression:
                return NumericDropCause.CallResult;

            case AstObjectLiteral:
            case AstArrayExpression:
            case AstFunctionExpression:
            case AstTemplateExpression:
                return NumericDropCause.NonNumericLiteral;

            case AstSequenceExpression sequence:
                return Classify(Last(sequence));

            case AstUnaryExpression unary
                when unary.Operator is UnaryOperator.Negate or UnaryOperator.BitwiseNot
                    or UnaryOperator.Increment or UnaryOperator.Decrement:
                return Classify(unary.Argument);

            case AstUnaryExpression:
                return NumericDropCause.UnhandledOperator;

            case AstBinaryExpression binary:
                return ClassifyBinary(binary);

            case AstConditionalExpression conditional:
                return !IsNumeric(conditional.True)
                    ? Classify(conditional.True)
                    : Classify(conditional.False);

            default:
                return NumericDropCause.Other;
        }
    }

    private NumericDropCause ClassifyBinary(AstBinaryExpression binary)
    {
        switch (binary.Operator)
        {
            // The operators IsNumericBinary types from both operands.
            case TokenTypes.Plus:
            case TokenTypes.Minus:
            case TokenTypes.Multiply:
            case TokenTypes.Divide:
            case TokenTypes.Mod:
            case TokenTypes.Power:
            case TokenTypes.BitwiseAnd:
            case TokenTypes.BitwiseOr:
            case TokenTypes.Xor:
            case TokenTypes.LeftShift:
            case TokenTypes.RightShift:
            case TokenTypes.UnsignedRightShift:
            case TokenTypes.AssignAdd:
            case TokenTypes.AssignSubtract:
            case TokenTypes.AssignMultiply:
            case TokenTypes.AssignDivide:
            case TokenTypes.AssignMod:
            case TokenTypes.AssignPower:
            case TokenTypes.AssignBitwideAnd:
            case TokenTypes.AssignBitwideOr:
            case TokenTypes.AssignXor:
            case TokenTypes.AssignLeftShift:
            case TokenTypes.AssignRightShift:
            case TokenTypes.AssignUnsignedRightShift:
                return !IsNumeric(binary.Left) ? Classify(binary.Left) : Classify(binary.Right);

            // A plain assignment's value is its right-hand side alone.
            case TokenTypes.Assign:
                return Classify(binary.Right);

            default:
                return NumericDropCause.UnhandledOperator;
        }
    }

    private static NumericLocalMiss ToMiss(NumericDropCause cause) => cause switch
    {
        NumericDropCause.DroppedCandidate => NumericLocalMiss.DroppedCandidate,
        NumericDropCause.Parameter => NumericLocalMiss.Parameter,
        NumericDropCause.PropertyRead => NumericLocalMiss.PropertyRead,
        NumericDropCause.ElementRead => NumericLocalMiss.ElementRead,
        NumericDropCause.CallResult => NumericLocalMiss.CallResult,
        NumericDropCause.OtherName => NumericLocalMiss.OtherName,
        NumericDropCause.NonNumericLiteral => NumericLocalMiss.NonNumericLiteral,
        NumericDropCause.UnhandledOperator => NumericLocalMiss.UnhandledOperator,
        _ => NumericLocalMiss.OtherDrop,
    };

    /// <summary>
    /// Every name this analysis refused, and why. A name it declared and never offered as a
    /// candidate reads <see cref="NumericLocalMiss.NeverOffered"/>; a survivor is absent.
    /// </summary>
    private IReadOnlyDictionary<string, NumericLocalMiss> Misses(IReadOnlySet<string> survivors)
    {
        foreach (var name in declaredNames)
        {
            if (!survivors.Contains(name) && !misses.ContainsKey(name))
                misses[name] = NumericLocalMiss.NeverOffered;
        }

        return misses;
    }

    private IReadOnlySet<string> Resolve(bool count = true)
    {
        // Item 3-6 splits "not proven numeric" in two, because the halves call for opposite
        // responses. A name never OFFERED is one whose declaration is not numeric at all —
        // `var a = []`, `var s = ''` — and no analysis makes a string a double. A name offered
        // and then DROPPED is one the fixed point could not keep, and that is the only part a
        // stronger analysis could reach.
        if (count)
            CompilerSpecializationDiagnostics.RecordNumericCandidatesOffered(candidates.Count);

        // The step between "offered" and "dropped" that had no counter, and it is the larger of
        // the two: a name any rejection path named — read before its initializer, bound through
        // a pattern, `delete`d, a written `const`, a for-in head — leaves here rather than in
        // the fixed point. Item 3-6 read survivors as offered MINUS dropped and so counted this
        // removal as nothing; with it counted, offered = rejected + dropped + surviving exactly
        // (docs/performance-roadmap.md item 3-7).
        var beforeRejection = candidates.Count;
        foreach (var name in rejected)
        {
            if (candidates.Contains(name))
                misses[name] = NumericLocalMiss.Rejected;
        }

        candidates.ExceptWith(rejected);
        if (count)
            CompilerSpecializationDiagnostics.RecordNumericCandidatesRejected(beforeRejection - candidates.Count);
        if (candidates.Count == 0)
        {
            if (count)
            {
                CompilerSpecializationDiagnostics.RecordNumericCandidatesDropped(0);
                CompilerSpecializationDiagnostics.RecordNumericCandidatesSurviving(0);
            }
            return System.Collections.Immutable.ImmutableHashSet<string>.Empty;
        }

        var beforeFixedPoint = candidates.Count;
        // Item 3-8 needs to tell a name dropped because a PROPERTY READ reached it from one
        // dropped because another dropped candidate did. Snapshot the survivors of the rejection
        // pass, so an identifier met during classification can be told apart from a global.
        everOffered.UnionWith(candidates);

        // Optimistic fixed point: drop any candidate whose assigned value is not numeric
        // under the current assumption, and repeat, because dropping one can invalidate
        // the assignments that read it.
        bool changed;
        do
        {
            changed = false;
            foreach (var (name, value) in assignments)
            {
                if (!candidates.Contains(name))
                    continue;

                if (!IsNumeric(value))
                {
                    candidates.Remove(name);
                    // Classified at the moment of the drop, because this is the assignment that
                    // removed the name — a later sweep would find a different one.
                    var cause = Classify(value);
                    misses[name] = ToMiss(cause);
                    if (count)
                        CompilerSpecializationDiagnostics.RecordNumericDropCause(cause);
                    changed = true;
                }
            }
        }
        while (changed && candidates.Count > 0);

        if (count)
        {
            CompilerSpecializationDiagnostics.RecordNumericCandidatesDropped(beforeFixedPoint - candidates.Count);
            CompilerSpecializationDiagnostics.RecordNumericCandidatesSurviving(candidates.Count);
        }
        return candidates;
    }

    /// <summary>
    /// Whether <paramref name="expression"/> can only evaluate to a JavaScript number,
    /// assuming every name currently in <see cref="candidates"/> holds one.
    /// </summary>
    private bool IsNumeric(AstExpression expression) => expression switch
    {
        AstLiteral { TokenType: TokenTypes.Number } => true,

        AstIdentifier identifier => candidates.Contains(identifier.Name.Value)
            // Item 3-8a's optimistic pass. A name this function neither declares nor takes as a
            // parameter comes from an enclosing scope or from the global object, and the whole
            // question the item asks is what happens if it holds a number. Parameters are
            // deliberately excluded: they are item 3-3's one acknowledged gap and want a guard at
            // entry rather than at an initializer, so counting them here would inflate the set
            // with a population this item does not serve.
            || (assumeOuterNamesAreNumeric
                && !parameterNames.Contains(identifier.Name.Value)
                && !declaredNames.Contains(identifier.Name.Value))
            // Item 3-9's static pass. Same exclusions for the same reason — a name this function
            // declares or takes as a parameter is a DIFFERENT binding that merely shares a
            // spelling, and typing it from the outer one would be a wrong answer rather than a
            // missed opportunity — but the outer name has to have been PROVED numeric rather than
            // assumed, so this needs no run-time test and produces an ordinary numeric local.
            || (outerNumericNames != null
                && !parameterNames.Contains(identifier.Name.Value)
                && !declaredNames.Contains(identifier.Name.Value)
                && outerNumericNames(identifier.Name.Value)),

        // Parenthesised / sequence: the value is the last element.
        AstSequenceExpression sequence => IsNumeric(Last(sequence)),

        AstUnaryExpression unary => IsNumericUnary(unary),

        AstBinaryExpression binary => IsNumericBinary(binary),

        // A conditional is numeric only if both arms are.
        AstConditionalExpression conditional =>
            IsNumeric(conditional.True) && IsNumeric(conditional.False),

        // Item 3-1's widening. A computed element read is the refusal `0108` measured at a
        // cost/saving of 0.04 over 6 908 985 boxed writes — the largest independent cause in the
        // tier and the cheapest to serve. Assumed here, tested at run time by the dual
        // representation, and never admitted to the sound tier.
        AstMemberExpression { Computed: true } when assumeElementReadsAreNumeric => true,

        _ => false,
    };

    private bool IsNumericUnary(AstUnaryExpression unary) => unary.Operator switch
    {
        // `-x` and `~x` on a number are numbers. On a BigInt they are BigInts, which is
        // why the operand has to be provably numeric rather than merely "not a string".
        UnaryOperator.Negate or UnaryOperator.BitwiseNot => IsNumeric(unary.Argument),

        // `++x` / `--x` yield ToNumeric(x); numeric in, numeric out.
        UnaryOperator.Increment or UnaryOperator.Decrement => IsNumeric(unary.Argument),

        _ => false,
    };

    private bool IsNumericBinary(AstBinaryExpression binary)
    {
        switch (binary.Operator)
        {
            // With both operands provably numbers there is no ToPrimitive, no string
            // concatenation and no BigInt path left — the result is a double.
            case TokenTypes.Plus:
            case TokenTypes.Minus:
            case TokenTypes.Multiply:
            case TokenTypes.Divide:
            case TokenTypes.Mod:
            case TokenTypes.Power:
            case TokenTypes.BitwiseAnd:
            case TokenTypes.BitwiseOr:
            case TokenTypes.Xor:
            case TokenTypes.LeftShift:
            case TokenTypes.RightShift:
            case TokenTypes.UnsignedRightShift:
                return IsNumeric(binary.Left) && IsNumeric(binary.Right);

            // A compound assignment's VALUE is what it stored, so it is numeric exactly
            // when the store was. The store itself was recorded separately.
            case TokenTypes.Assign:
                return IsNumeric(binary.Right);

            case TokenTypes.AssignAdd:
            case TokenTypes.AssignSubtract:
            case TokenTypes.AssignMultiply:
            case TokenTypes.AssignDivide:
            case TokenTypes.AssignMod:
            case TokenTypes.AssignPower:
            case TokenTypes.AssignBitwideAnd:
            case TokenTypes.AssignBitwideOr:
            case TokenTypes.AssignXor:
            case TokenTypes.AssignLeftShift:
            case TokenTypes.AssignRightShift:
            case TokenTypes.AssignUnsignedRightShift:
                return IsNumeric(binary.Left) && IsNumeric(binary.Right);

            default:
                return false;
        }
    }

    private static AstExpression Last(AstSequenceExpression sequence)
    {
        AstExpression last = null;
        var en = sequence.Expressions.GetFastEnumerator();
        while (en.MoveNext(out var item))
            last = item;
        return last;
    }

    /// <summary>Every identifier name appearing anywhere under a node.</summary>
    /// <remarks>
    /// The overrides below are not optional. <see cref="AstReduce"/> treats these compact
    /// structs as leaves because most rewriting visitors handle them explicitly, so without
    /// them this collector walks straight past the properties of an object pattern — and every
    /// caller of <see cref="RejectEveryNameIn"/> is a place where a missed name is a
    /// <em>miscompilation</em> rather than a lost optimization. <c>var { a: s } = o</c> and
    /// <c>({ a: s } = o)</c> both bind <c>s</c> through an object pattern, and while they were
    /// invisible here the analysis went on believing <c>s</c> held a number: a string assigned
    /// through one of them silently became NaN.
    /// <para>
    /// <c>ScalarReplacementHazardDetector</c> and <c>NestedFunctionScanner</c> in
    /// <c>FastCompiler.CreateFunction</c> carry the same three overrides for the same reason.
    /// This class is the one that was missed.
    /// </para>
    /// </remarks>
    private sealed class NameCollector : AstReduce
    {
        public readonly List<string> Names = [];

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            Names.Add(identifier.Name.Value);
            return identifier;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }

    /// <summary>
    /// Single walk of the body that records every write to a candidate and rejects a name
    /// on anything the analysis cannot account for.
    /// </summary>
    private sealed class Collector : AstReduce
    {
        private readonly NumericLocalAnalysis owner;

        // Names seen before their declaration was reached. A var is `undefined` until its
        // initializer runs, so a read that can precede it disqualifies the name.
        private readonly HashSet<string> declared = new(System.StringComparer.Ordinal);

        // The function's own body block, which is offered in Collect and needs no containment
        // condition — everything in the function is inside it.
        private readonly AstBlock functionBody;

        // The blocks currently open, outermost first. A confined name is only legal while its
        // owning block is on this stack.
        private readonly List<AstBlock> openBlocks = [];

        // How many nested functions the walk is currently inside. Writes are still recorded at
        // any depth — the walk conflates names, so a nested `s = 'x'` correctly drops an outer
        // numeric `s` — but a DECLARATION at depth > 0 must not open `declared` for the outer
        // name (docs/performance-roadmap.md item 3-7).
        private int functionDepth;

        public Collector(NumericLocalAnalysis owner, AstBlock functionBody)
        {
            this.owner = owner;
            this.functionBody = functionBody;
        }

        /// <summary>
        /// Descends into a nested function — the walk deliberately conflates names, so a write
        /// there drops the outer candidate — while recording that declarations inside it belong
        /// to a different binding, and rejecting the name a function DECLARATION binds.
        /// </summary>
        /// <remarks>
        /// A function declaration stores a function object into the very binding the analysis is
        /// typing: at body top level `var f = 1; function f() {}` leaves `f` holding the
        /// function, and `let f = 5; { function f() {} }` reaches the same binding through Annex
        /// B's copy-out. Neither is a write this walk can see — a declaration is not an
        /// assignment expression — and while every such name was refused for being *mentioned*
        /// by the declaration (its own identifier), the gap was covered by accident. Item 3-7
        /// lifts that refusal, so the store is rejected explicitly. A named function EXPRESSION
        /// is left alone: its name binds inside its own body, not out here.
        /// </remarks>
        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            if (functionExpression is { IsStatement: true, Id: AstIdentifier declaredName })
                owner.rejected.Add(declaredName.Name.Value);

            functionDepth++;
            var result = base.VisitFunctionExpression(functionExpression);
            functionDepth--;
            return result;
        }

        protected override AstNode VisitBlock(AstBlock block)
        {
            openBlocks.Add(block);
            // Offer this block's own direct `var` statements BEFORE descending, so the offer is
            // in place by the time the walk reaches a reference to one of them.
            //
            // Only while inside THIS function. The walk descends into nested functions on purpose
            // — that is what lets a closure's `s = 'x'` drop an outer numeric `s` — but a block
            // inside a nested function belongs to a different function's variable environment, and
            // offering its `var`s here made every such name a candidate of the enclosing function
            // as well as of its own. Harmless to the answer, since the enclosing function's
            // hoisting scope never contains the name and so never reaches the hoist site with it,
            // and pure inflation in the counts: a name three functions deep was offered, dropped
            // and counted three times over (docs/performance-roadmap.md item 3-8).
            if (functionDepth == 0 && !ReferenceEquals(block, functionBody))
                owner.OfferBlockConfinedDeclarations(block);

            var result = base.VisitBlock(block);
            openBlocks.RemoveAt(openBlocks.Count - 1);
            return result;
        }

        /// <summary>
        /// Rejects <paramref name="name"/> if it is confined to a block this reference is not
        /// inside. Applied to reads and writes alike: a plain write from outside would be
        /// harmless on its own, but distinguishing it costs more than it saves and being wrong
        /// about it is a miscompilation.
        /// </summary>
        private void NoteReference(string name)
        {
            if (!owner.confinedTo.TryGetValue(name, out var owningBlock))
                return;

            for (var i = openBlocks.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(openBlocks[i], owningBlock))
                    return;
            }

            owner.rejected.Add(name);
        }

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            var name = identifier.Name.Value;
            if (!declared.Contains(name))
                owner.rejected.Add(name);
            NoteReference(name);
            return identifier;
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            // The initializer is evaluated BEFORE the binding is initialized, so it is
            // visited first and a self-reference in it (`var x = x`) still reads undefined.
            if (declarator.Init != null)
                Visit(declarator.Init);

            if (declarator.Identifier is AstIdentifier identifier)
            {
                // A DECLARATION is a store, and this walk reaches the ones OfferDeclaration
                // cannot: a `var` re-declared inside a block, an `if`, a loop, a `try` or a
                // `switch` names the same function-scoped binding, so `var s = 0; { var s =
                // 'x'; }` really does put a string where the analysis proved a number. Only
                // the top-level declarations were recorded before, and the value silently
                // became NaN.
                //
                // Recording it here double-counts the top-level ones OfferDeclaration
                // already has, which costs one extra IsNumeric call each and changes no
                // answer — the fixed point asks the same question of the same expression.
                //
                // A declarator with NO initializer is deliberately not recorded: a second
                // `var s;` does not reset an initialized binding, so it stores nothing.
                if (declarator.Init != null)
                    owner.assignments.Add((identifier.Name.Value, declarator.Init));

                // A second declaration of a confined name from outside its block — one the
                // offer path never saw, because it is not a direct statement of any block
                // (`if (c) var t = 2;`). It re-binds the name where the confining block does
                // not dominate, so the name loses its raw double.
                NoteReference(identifier.Name.Value);

                // A name that rests on a dominating declaration becomes readable at THAT
                // declaration only. Marking it declared at some other one would mask a read
                // sitting between the two, which can still observe `undefined`.
                //
                // A declaration inside a NESTED function never initializes the outer binding, so
                // it may not open `declared` either — a nested function's own parameter or var
                // sharing the name would otherwise mask a read of the outer one that really does
                // see `undefined`. That was unreachable while every name a nested function
                // mentions kept its cell; item 3-7 removes exactly that refusal, so the rule has
                // to be stated rather than inherited.
                owner.declaredNames.Add(identifier.Name.Value);
                if (functionDepth == 0
                    && (!owner.HasDominatingDeclaration(identifier.Name.Value)
                        || owner.IsDominatingInit(declarator.Init)))
                {
                    declared.Add(identifier.Name.Value);
                }
            }
            else
            {
                owner.RejectEveryNameIn(declarator.Identifier);
            }

            return declarator;
        }

        protected override AstNode VisitBinaryExpression(AstBinaryExpression binary)
        {
            if (binary.Operator > TokenTypes.BeginAssignTokens
                && binary.Operator < TokenTypes.EndAssignTokens)
            {
                if (binary.Left is AstIdentifier target)
                {
                    // Record the store, then visit the operands. The target identifier is
                    // deliberately NOT visited as a read: an assignment does not observe the
                    // old value except in a compound form, which reads it through the operator
                    // and is covered by IsNumericBinary.
                    owner.assignments.Add((target.Name.Value, binary));
                    owner.NoteWrite(target.Name.Value);
                    // The target identifier is deliberately not visited, so the containment
                    // check has to be made here rather than in VisitIdentifier.
                    NoteReference(target.Name.Value);
                    if (binary.Operator != TokenTypes.Assign && !declared.Contains(target.Name.Value))
                        owner.rejected.Add(target.Name.Value);
                    Visit(binary.Right);
                    return binary;
                }

                // A destructuring assignment writes names without an identifier in target
                // position: `({ a: s } = o)` and `[s] = a` both store into `s`, and neither
                // reaches the branch above. Nothing here can say what the pattern will yield,
                // so every name it mentions is rejected rather than typed.
                //
                // A MEMBER expression is excluded from that and visited normally, because it
                // is not a binding target at all — the names in `a[i] = v` are reads, and
                // rejecting them would undo 3-0's unboxed numeric index.
                if (binary.Left is not AstMemberExpression)
                {
                    owner.RejectEveryNameIn(binary.Left);
                    Visit(binary.Right);
                    return binary;
                }
            }

            Visit(binary.Left);
            Visit(binary.Right);
            return binary;
        }

        protected override AstNode VisitUnaryExpression(AstUnaryExpression unary)
        {
            if (unary.Operator is UnaryOperator.Increment or UnaryOperator.Decrement
                && unary.Argument is AstIdentifier target)
            {
                // `x++` stores ToNumeric(x), which is numeric exactly when x already is.
                owner.assignments.Add((target.Name.Value, unary));
                owner.NoteWrite(target.Name.Value);
                NoteReference(target.Name.Value);
                if (!declared.Contains(target.Name.Value))
                    owner.rejected.Add(target.Name.Value);
                return unary;
            }

            // `delete x` and `typeof x` need the binding itself, not its value.
            if (unary.Operator is UnaryOperator.@delete or UnaryOperator.@typeof)
            {
                owner.RejectEveryNameIn(unary.Argument);
                return unary;
            }

            Visit(unary.Argument);
            return unary;
        }

        protected override AstNode VisitForInStatement(AstForInStatement statement, string label = null)
        {
            // The head binding takes whatever the enumeration yields — a string key for
            // for-in, anything at all for for-of.
            owner.RejectEveryNameIn(statement.Init as AstExpression);
            RejectDeclarationNames(statement.Init);
            Visit(statement.Target);
            Visit(statement.Body);
            return statement;
        }

        protected override AstNode VisitForOfStatement(AstForOfStatement statement, string label = null)
        {
            owner.RejectEveryNameIn(statement.Init as AstExpression);
            RejectDeclarationNames(statement.Init);
            Visit(statement.Target);
            Visit(statement.Body);
            return statement;
        }

        private void RejectDeclarationNames(AstNode init)
        {
            if (init is not AstVariableDeclaration declaration)
                return;

            var declarators = declaration.Declarators.GetFastEnumerator();
            while (declarators.MoveNext(out var declarator))
            {
                if (declarator.Identifier is AstIdentifier identifier)
                    owner.rejected.Add(identifier.Name.Value);
                else
                    owner.RejectEveryNameIn(declarator.Identifier);
            }
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }
}
