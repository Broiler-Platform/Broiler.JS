namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Why the local that consumes a guarded arithmetic tree's root box is not a numeric local, so the
/// boxes the refusal costs can be counted at run time rather than the names it refuses counted at
/// compile time (docs/performance-roadmap.md item 3-1, `0106`).
/// </summary>
/// <remarks>
/// <para>
/// <b>Item 3-6 already counted these causes, and counted them per NAME.</b> That is the census that
/// concluded the coverage is lost to *"none of the ones the item named"*, and it is the right shape
/// for asking how many bindings a stronger analysis would admit. It is the wrong shape for asking
/// what the refusals cost, because a name refused inside a loop that runs ten million times and a
/// name refused in initialization code weigh the same in it.
/// </para>
/// <para>
/// `0105` made that distinction matter: <b>44.36% of the corpus's guarded-tree root boxes are
/// consumed by a local</b>, and the seam hypothesis — that they land in locals the tier had already
/// accepted, and are boxed and immediately unboxed — was measured and refuted at <b>36 boxes of
/// 18.6 M</b>. So every one of them is a local the tier <em>refused</em>, and which refusal is
/// carrying the weight is a question only a run-time count can answer. This is the same
/// vocabulary as <c>NumericDropCause</c>, weighted by executions instead of by declarations.
/// </para>
/// </remarks>
public enum NumericLocalMiss
{
    /// <summary>No refusal recorded — the name is not one the analysis considered, or the boxing
    /// site is outside a function body the analysis ran on.</summary>
    Unknown = 0,

    /// <summary>
    /// Offered as a candidate and dropped because another dropped candidate appears in its
    /// assigned value — a cascade. The one cause that wants no new mechanism at all: typing its
    /// root types this for free.
    /// </summary>
    DroppedCandidate = 1,

    /// <summary>Dropped because a PARAMETER of the enclosing function reaches it: the caller picks the type.</summary>
    Parameter = 2,

    /// <summary>Dropped because a named property read, <c>o.x</c>, reaches it.</summary>
    PropertyRead = 3,

    /// <summary>Dropped because a computed element read, <c>a[i]</c>, reaches it.</summary>
    ElementRead = 4,

    /// <summary>Dropped because the return value of a call or a <c>new</c> reaches it.</summary>
    CallResult = 5,

    /// <summary>Dropped because some other name — a global, an outer-scope binding, a catch parameter — reaches it.</summary>
    OtherName = 6,

    /// <summary>Dropped because a non-numeric literal, or an object/array/function/template literal, reaches it.</summary>
    NonNumericLiteral = 7,

    /// <summary>Dropped because an operator the analysis does not type reaches it.</summary>
    UnhandledOperator = 8,

    /// <summary>Dropped for a reason none of the above name.</summary>
    OtherDrop = 9,

    /// <summary>
    /// Named by one of the analysis's rejection paths before the fixed point ran — read before its
    /// initializer, bound through a pattern, <c>delete</c>d, a written <c>const</c>, a for-in head.
    /// </summary>
    Rejected = 10,

    /// <summary>
    /// Never offered as a candidate: its declaration is not numeric at all. Item 3-6's point that
    /// no analysis makes a string a double — the part of the population no stronger fixed point
    /// reaches.
    /// </summary>
    NeverOffered = 11,
}
