namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Internal short-circuit sentinel for optional chaining (<c>?.</c>). When the base of an
/// optional link is nullish, that link yields this singleton instead of <c>undefined</c>;
/// every subsequent link in the same chain propagates it (rather than throwing on the
/// "missing" value), and the chain root (<see cref="Broiler.JavaScript.Ast.Expressions.AstOptionalChain"/>)
/// converts it back to <c>undefined</c>.
///
/// This lets a trailing <em>non-optional</em> access distinguish "the chain short-circuited
/// earlier" (propagate, result is undefined) from "a genuine undefined was produced" (throw),
/// e.g. <c>a?.b.c</c> must short-circuit to undefined when <c>a</c> is nullish, but throw a
/// TypeError when <c>a.b</c> is itself undefined.
///
/// The sentinel never escapes a chain: only skip-aware link operations ever observe it and they
/// check for it before any other use, and the chain root always unwraps it.
/// </summary>
internal sealed class JSOptionalChainSkip : JSValue
{
    internal static readonly JSValue Value = new JSOptionalChainSkip();

    private JSOptionalChainSkip() : base(null) { }

    internal override PropertyKey ToKey(bool create = true) => Escaped<PropertyKey>();

    public override JSValue TypeOf() => Escaped<JSValue>();

    public override bool BooleanValue => Escaped<bool>();

    public override bool Equals(JSValue value) => ReferenceEquals(this, value);

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    public override JSValue InvokeFunction(in Arguments a) => Escaped<JSValue>();

    public override string ToString() => "undefined";

    // The sentinel is never meant to reach ordinary value operations; if one is invoked
    // it means a chain failed to unwrap it, so fail loudly rather than corrupt results.
    private static T Escaped<T>()
        => throw NewTypeError("Internal optional-chain short-circuit sentinel escaped");
}
