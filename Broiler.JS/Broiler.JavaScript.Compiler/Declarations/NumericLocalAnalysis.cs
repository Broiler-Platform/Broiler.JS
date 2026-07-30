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
/// nothing after it runs. A declaration nested inside an <c>if</c> or a loop is not eligible,
/// because control can reach code after it without having executed it.
/// </para>
/// </remarks>
internal sealed class NumericLocalAnalysis
{
    private readonly HashSet<string> candidates = new(System.StringComparer.Ordinal);
    private readonly HashSet<string> rejected = new(System.StringComparer.Ordinal);
    private readonly List<(string Name, AstExpression Value)> assignments = [];

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

        var collector = new Collector(this);

        // Only the function body's own statement list can declare an eligible name. The
        // walk below still descends into everything, but for USES and WRITES — a
        // declaration found deeper is recorded as a rejection instead.
        var statements = body.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            switch (statement)
            {
                case AstVariableDeclaration { Kind: FastVariableKind.Var } declaration:
                    OfferDeclaration(declaration);
                    break;

                case AstForStatement { Init: AstVariableDeclaration { Kind: FastVariableKind.Var } forInit }:
                    OfferDeclaration(forInit);
                    break;
            }
        }

        collector.Visit(body);
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

            // Declared twice: the second declaration may sit somewhere the first does not
            // dominate, so give up rather than reason about which one wins.
            if (!candidates.Add(name))
                rejected.Add(name);

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
        candidates.ExceptWith(rejected);
        if (candidates.Count == 0)
            return System.Collections.Immutable.ImmutableHashSet<string>.Empty;

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
    private sealed class NameCollector : AstReduce
    {
        public readonly List<string> Names = [];

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            Names.Add(identifier.Name.Value);
            return identifier;
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

        public Collector(NumericLocalAnalysis owner) => this.owner = owner;

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            var name = identifier.Name.Value;
            if (!declared.Contains(name))
                owner.rejected.Add(name);
            return identifier;
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            // The initializer is evaluated BEFORE the binding is initialized, so it is
            // visited first and a self-reference in it (`var x = x`) still reads undefined.
            if (declarator.Init != null)
                Visit(declarator.Init);

            if (declarator.Identifier is AstIdentifier identifier)
                declared.Add(identifier.Name.Value);
            else
                owner.RejectEveryNameIn(declarator.Identifier);

            return declarator;
        }

        protected override AstNode VisitBinaryExpression(AstBinaryExpression binary)
        {
            if (binary.Operator > TokenTypes.BeginAssignTokens
                && binary.Operator < TokenTypes.EndAssignTokens
                && binary.Left is AstIdentifier target)
            {
                // Record the store, then visit the operands. The target identifier is
                // deliberately NOT visited as a read: an assignment does not observe the
                // old value except in a compound form, which reads it through the operator
                // and is covered by IsNumericBinary.
                owner.assignments.Add((target.Name.Value, binary));
                if (binary.Operator != TokenTypes.Assign && !declared.Contains(target.Name.Value))
                    owner.rejected.Add(target.Name.Value);
                Visit(binary.Right);
                return binary;
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
