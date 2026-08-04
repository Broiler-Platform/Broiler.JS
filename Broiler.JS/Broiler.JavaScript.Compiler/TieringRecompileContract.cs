using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Whether the tier-2 recompile may promote a function at all — the <b>recompile contract</b>
/// (docs/performance-roadmap.md item 4-2a).
/// </summary>
/// <remarks>
/// <para>
/// <c>JSFunction.RecompileForTiering</c> promotes a function by re-parsing its <em>source
/// text</em> as a fresh top-level script and keeping the delegate that falls out. Nothing about
/// that is a specialization — §4 already calls the path "a hook, not a tier" — but it is also
/// not a no-op, because a fresh compilation does not reproduce the scope the function was
/// written in. The one condition that makes it sound:
/// </para>
/// <para>
/// <b>The body must not be able to observe the function object it was compiled from.</b> The
/// recompile produces a <em>second</em> function object and installs only its delegate on the
/// original. So a body that reaches its own function object reaches the copy, while every other
/// reference in the program still reaches the original — and the two differ in every own
/// property the program installed on it.
/// </para>
/// <para>
/// That is not hypothetical: it is what broke Octane's DeltaBlue, whose constructors are written
/// <c>UnaryConstraint.superConstructor.call(this, strength)</c>. After promotion
/// <c>UnaryConstraint</c> names the copy, the copy has no <c>superConstructor</c>, and the suite
/// dies with <c>TypeError: Cannot get property call of undefined</c>. The engine had no rule
/// saying this was illegal, so it was simply wrong.
/// </para>
/// <para>
/// <b>Two ways a body can reach its own function object</b>, and both are refused here:
/// </para>
/// <list type="number">
/// <item><b>Its own name.</b> For a declaration the wrapper <c>({source})</c> turns the
/// declaration into a <em>named function expression</em>, which creates a self-name binding that
/// shadows the outer one the body actually meant. For a named function expression the
/// self-binding is genuine — and still binds the copy rather than the original. Same wrong
/// answer, opposite reasons, so the refusal covers both rather than distinguishing them.</item>
/// <item><b><c>arguments</c>.</b> <c>arguments.callee</c> is the function object again, by a
/// route no name check can see, and it can be reached through an alias
/// (<c>var a = arguments; a.callee</c>). Any mention of the name is refused rather than only
/// the direct member access, because the alias is what makes the narrow check unsound.</item>
/// </list>
/// <para>
/// <b>Recursion is not one of them</b>, and that is worth stating because it looks like it should
/// be: <c>fact(n - 1)</c> inside the copy calls the copy, which computes the same answer, so a
/// self-call is only wrong when the *identity* is observed rather than invoked. The refusal is
/// keyed on the name being mentioned at all — the conservative side — so recursion is refused
/// too. Named on purpose: this is the cost of the rule, not an oversight.
/// </para>
/// <para>
/// <b>Placed at the decision point</b>, for the reason item 4-3a records about its own condition
/// 3. The tiering gate in <c>FastCompiler.CreateFunction</c> is a conjunction of conditions that
/// exist for unrelated reasons, and a property that holds only because of where a call sits is a
/// property one ordinary refactor removes. This is a rule about what may be promoted, so it is
/// written down as one.
/// </para>
/// </remarks>
internal static class TieringRecompileContract
{
    /// <summary>
    /// Whether <paramref name="function"/> may be promoted by a source-text recompile.
    /// </summary>
    /// <param name="mentionsArguments">
    /// Whether the body names <c>arguments</c>, as already computed for scalar replacement.
    /// Its own conservative default is <c>true</c> ("did not look" reads as "may mention it"),
    /// which is the right default here too.
    /// </param>
    public static bool Admits(AstFunctionExpression function, bool mentionsArguments)
    {
        if (function == null || mentionsArguments)
            return false;

        var name = function.Id?.Name.Value;
        if (string.IsNullOrEmpty(name))
            return true;

        var detector = new SelfNameDetector(name);
        detector.Visit(function.Body);
        return !detector.Found;
    }

    /// <summary>
    /// Whether the body mentions one particular name.
    /// </summary>
    /// <remarks>
    /// Descends into nested functions rather than treating them as leaves. The tiering gate
    /// excludes functions that have any, so in practice there are none to descend into — but a
    /// detector that answers "no" because it stopped looking is the failure mode this whole item
    /// is about, so it looks.
    /// </remarks>
    private sealed class SelfNameDetector(string name) : AstReduce
    {
        public bool Found { get; private set; }

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            if (identifier.Name.Equals(name))
                Found = true;
            return identifier;
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            // A nested function that re-declares the name shadows it, so a mention inside is not
            // the outer function's. Refused anyway: distinguishing them needs a scope walk, and
            // the answer would only ever widen a case the gate above already excludes.
            base.VisitFunctionExpression(functionExpression);
            return functionExpression;
        }

        // AstReduce treats exactly three compact structs as LEAVES, because most rewriting
        // visitors handle them explicitly. A detector that inherits that treatment silently stops
        // looking — and this one did: `function fact(n) { var t = n <= 1 ? 1 : n * fact(n - 1); }`
        // hides its self-reference in a declarator's initializer, so the first draft admitted it
        // while refusing the same function written with an assignment statement. That is the
        // "did not look reads as did not find" failure this whole item is about, one level down.
        // ScalarReplacementHazardDetector overrides the same three for the same reason.
        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
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
