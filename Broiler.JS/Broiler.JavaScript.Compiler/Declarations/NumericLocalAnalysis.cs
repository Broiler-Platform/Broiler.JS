using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

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
    {
        var analysis = new NumericLocalAnalysis();
        analysis.Collect(function);
        return analysis.Resolve();
    }

    private void Collect(AstFunctionExpression function)
    {
        // A parameter shares its name with no eligible var: the value arrives as a JSValue
        // and nothing here proves it is a number.
        var parameters = function.Params.GetFastEnumerator();
        while (parameters.MoveNext(out var parameter))
        {
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

    private IReadOnlySet<string> Resolve()
    {
        // Item 3-6 splits "not proven numeric" in two, because the halves call for opposite
        // responses. A name never OFFERED is one whose declaration is not numeric at all —
        // `var a = []`, `var s = ''` — and no analysis makes a string a double. A name offered
        // and then DROPPED is one the fixed point could not keep, and that is the only part a
        // stronger analysis could reach.
        CompilerSpecializationDiagnostics.RecordNumericCandidatesOffered(candidates.Count);

        candidates.ExceptWith(rejected);
        if (candidates.Count == 0)
        {
            CompilerSpecializationDiagnostics.RecordNumericCandidatesDropped(0);
            return System.Collections.Immutable.ImmutableHashSet<string>.Empty;
        }

        var beforeFixedPoint = candidates.Count;

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
                    changed = true;
                }
            }
        }
        while (changed && candidates.Count > 0);

        CompilerSpecializationDiagnostics.RecordNumericCandidatesDropped(beforeFixedPoint - candidates.Count);
        return candidates;
    }

    /// <summary>
    /// Whether <paramref name="expression"/> can only evaluate to a JavaScript number,
    /// assuming every name currently in <see cref="candidates"/> holds one.
    /// </summary>
    private bool IsNumeric(AstExpression expression) => expression switch
    {
        AstLiteral { TokenType: TokenTypes.Number } => true,

        AstIdentifier identifier => candidates.Contains(identifier.Name.Value),

        // Parenthesised / sequence: the value is the last element.
        AstSequenceExpression sequence => IsNumeric(Last(sequence)),

        AstUnaryExpression unary => IsNumericUnary(unary),

        AstBinaryExpression binary => IsNumericBinary(binary),

        // A conditional is numeric only if both arms are.
        AstConditionalExpression conditional =>
            IsNumeric(conditional.True) && IsNumeric(conditional.False),

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

        public Collector(NumericLocalAnalysis owner, AstBlock functionBody)
        {
            this.owner = owner;
            this.functionBody = functionBody;
        }

        protected override AstNode VisitBlock(AstBlock block)
        {
            openBlocks.Add(block);
            // Offer this block's own direct `var` statements BEFORE descending, so the offer is
            // in place by the time the walk reaches a reference to one of them.
            if (!ReferenceEquals(block, functionBody))
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
                if (!owner.HasDominatingDeclaration(identifier.Name.Value)
                    || owner.IsDominatingInit(declarator.Init))
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
