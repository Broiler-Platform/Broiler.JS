// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           12
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// One parse, as the caller asks for it (Broiler.VM's JSD-0080, "Input: an immutable parse request"; JSD-0083, decisions
/// D7, D8, D10, D11, D14 to D17 and D32): the source and its goal, the switches, the inherited context of eval code, the
/// proposal flags, the lexer extension, the limits and the providers. Immutable once made; everything a parse depends on
/// is here, and nothing comes from the thread or the process it runs on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Required</b>: the source, the goal and the two providers. Broiler.Unicode's <see cref="IUnicodeTables"/> has no
/// default (decision D8: "no default that throws"); the regular-expression validator is each engine's own (decision D7).
/// </para>
/// <para>
/// <b>Defaulted for the goal</b> when the caller states none: <see cref="Switches"/> as <see cref="ParseSwitches.For"/>
/// gives them; <see cref="Context"/> the top level's for <see cref="ParseGoal.Eval"/> and none for every other goal; no
/// proposal and no lexer extension; <see cref="ParseLimits.Default"/>; <see cref="DynamicFunctionKind.Normal"/>; and no
/// meter. That is <b>the library's default request for a goal</b>, which the library-level runner's frontend adapter parses
/// each <c>syntax</c> vector under (decision D25).
/// </para>
/// <para>
/// <b>What a goal does not admit is refused when the request is made</b>, as a broken precondition (convention 3): top-level
/// <c>await</c> outside the Script and Module goals, HTML-like comments in a module, an inherited context for any goal but
/// eval code, and a dynamic function's kind for any goal but its parameters and body.
/// </para>
/// <para>
/// <b>How each engine fills it.</b> Broiler.VM keeps <c>SliceParseOptions</c> as its public options and maps them inside
/// its compiler (decision D16): the goal, top-level <c>await</c> from its module options, the nesting depth and its eval
/// flags, <c>Strict</c> to <see cref="ParseSwitches.ForcedStrict"/> and the function context to <see cref="Context"/>, with
/// no private names; HTML-like comments by goal; no proposal and no lexer extension (decision D17); its diagnostic cap,
/// first error; <c>JsCompilerUnicode</c>'s tables and a validator over its binder's. Broiler.JS's bridge fills it from its
/// presets and its direct-eval support at SJL-14 (decision D44): the goals, top-level <c>await</c> for a module and an
/// awaiting script, its inherited context, the decorator, accessor and import-call flags, the <c>m</c> extension, the
/// library's supported maximum nesting, first error, its tables and its validator.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0080 parse request, JSD-0083 D7 D8 D10 D11 D14-D17 D32; IP=Low; Security=Medium; Resources=0; Fingerprint=8A6191
// Hairs-Human:        PENDING
public sealed class ParseRequest
{
    private readonly ParseSwitches switches;
    private readonly InheritedContext? context;
    private readonly ProposalFlags proposals;
    private readonly LexerExtensions extensions;
    private readonly ParseLimits limits = ParseLimits.Default;
    private readonly DynamicFunctionKind functionKind;

    /// <summary>Creates the library's default request for <paramref name="goal"/>: see the type's remarks for each default.</summary>
    /// <param name="source">The source.</param>
    /// <param name="goal">The goal symbol.</param>
    /// <param name="unicodeTables">Broiler.Unicode's tables, through which the tokenizer reads the identifier and white-space classes (decision D8).</param>
    /// <param name="regularExpressions">The engine's regular-expression validator (decision D7).</param>
    /// <exception cref="ArgumentNullException">The source or a provider is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The goal is not a <see cref="ParseGoal"/>.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 parse request, JSD-0083 D7 D8; IP=Low; Security=Medium; Resources=1; Fingerprint=DD62A1
    // Hairs-Human:        PENDING
    public ParseRequest(SourceText source, ParseGoal goal, IUnicodeTables unicodeTables, IRegularExpressionValidator regularExpressions)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(unicodeTables);
        ArgumentNullException.ThrowIfNull(regularExpressions);
        if (goal is not (ParseGoal.Script or ParseGoal.Module or ParseGoal.Eval or ParseGoal.FormalParameters or ParseGoal.FunctionBody))
            throw new ArgumentOutOfRangeException(nameof(goal), goal, "Not a goal symbol.");

        Source = source;
        Goal = goal;
        UnicodeTables = unicodeTables;
        RegularExpressions = regularExpressions;
        switches = ParseSwitches.For(goal);
        context = goal == ParseGoal.Eval ? InheritedContext.TopLevel : null;
    }

    /// <summary>The source.</summary>
    public SourceText Source { get; }

    /// <summary>The goal symbol the source is parsed as.</summary>
    public ParseGoal Goal { get; }

    /// <summary>The switches; by default <see cref="ParseSwitches.For"/> of the goal.</summary>
    /// <exception cref="ArgumentException">Top-level <c>await</c> is on for a goal but Script and Module, or HTML-like comments for the Module goal.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 D17 switches by goal; IP=Low; Security=Medium; Resources=1; Fingerprint=5E88E0
    // Hairs-Human:        PENDING
    public ParseSwitches Switches
    {
        get => switches;
        init
        {
            if (value.TopLevelAwait && Goal is not (ParseGoal.Script or ParseGoal.Module))
                throw new ArgumentException("Top-level await applies to the Script and Module goals alone, not to " + Goal + ".", nameof(Switches));
            if (value.HtmlLikeComments && Goal == ParseGoal.Module)
                throw new ArgumentException("The Module goal has no HTML-like comments (ECMA-262 Annex B.1.1).", nameof(Switches));
            switches = value;
        }
    }

    /// <summary>
    /// The context eval code inherits from its caller: for <see cref="ParseGoal.Eval"/>, by default
    /// <see cref="InheritedContext.TopLevel"/>, and never null; for every other goal, null.
    /// </summary>
    /// <exception cref="ArgumentNullException">The goal is eval code and the context is null.</exception>
    /// <exception cref="ArgumentException">The goal is not eval code and a context is given.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.1 PerformEval, JSD-0083 D16 D44 inherited context; IP=Low; Security=Medium; Resources=1; Fingerprint=CEBAE7
    // Hairs-Human:        PENDING
    public InheritedContext? Context
    {
        get => context;
        init
        {
            if (Goal == ParseGoal.Eval)
                ArgumentNullException.ThrowIfNull(value, nameof(Context));
            else if (value is not null)
                throw new ArgumentException("Only eval code inherits a context from its caller, not " + Goal + ".", nameof(Context));
            context = value;
        }
    }

    /// <summary>The proposals the parse admits; none by default (decision D17).</summary>
    /// <exception cref="ArgumentOutOfRangeException">A bit <see cref="ProposalFlags"/> does not define is set.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D17 proposal flags; IP=Low; Security=Low; Resources=1; Fingerprint=20C934
    // Hairs-Human:        PENDING
    public ProposalFlags Proposals
    {
        get => proposals;
        init
        {
            const ProposalFlags all = ProposalFlags.Decorators | ProposalFlags.AutoAccessors | ProposalFlags.ImportSourceDeclarations
                | ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferDeclarations | ProposalFlags.ImportDeferCalls;
            if ((value & ~all) != 0)
                throw new ArgumentOutOfRangeException(nameof(Proposals), value, "A proposal flag this library does not define.");
            proposals = value;
        }
    }

    /// <summary>The lexer extensions the tokenizer admits; none by default (decision D17).</summary>
    /// <exception cref="ArgumentOutOfRangeException">A bit <see cref="LexerExtensions"/> does not define is set.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D17 lexer extension; IP=Low; Security=Low; Resources=1; Fingerprint=D34037
    // Hairs-Human:        PENDING
    public LexerExtensions Extensions
    {
        get => extensions;
        init
        {
            if ((value & ~LexerExtensions.DecimalSuffix) != 0)
                throw new ArgumentOutOfRangeException(nameof(Extensions), value, "A lexer extension this library does not define.");
            extensions = value;
        }
    }

    /// <summary>The limits; <see cref="ParseLimits.Default"/> by default.</summary>
    /// <exception cref="ArgumentNullException">The limits are null.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D11 D14 limits; IP=Low; Security=Low; Resources=1; Fingerprint=3C40F1
    // Hairs-Human:        PENDING
    public ParseLimits Limits
    {
        get => limits;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Limits));
            limits = value;
        }
    }

    /// <summary>
    /// The kind of the dynamic function whose parameters or body are parsed, for <see cref="ParseGoal.FormalParameters"/>
    /// and <see cref="ParseGoal.FunctionBody"/>; <see cref="DynamicFunctionKind.Normal"/> by default, and for every other
    /// goal the only value admitted.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="DynamicFunctionKind"/>.</exception>
    /// <exception cref="ArgumentException">The goal is not a dynamic function's piece and the kind is not <see cref="DynamicFunctionKind.Normal"/>.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s20.2.1.1.1 CreateDynamicFunction kind; IP=Low; Security=Low; Resources=1; Fingerprint=C9F987
    // Hairs-Human:        PENDING
    public DynamicFunctionKind FunctionKind
    {
        get => functionKind;
        init
        {
            if (value is not (DynamicFunctionKind.Normal or DynamicFunctionKind.Generator or DynamicFunctionKind.Async or DynamicFunctionKind.AsyncGenerator))
                throw new ArgumentOutOfRangeException(nameof(FunctionKind), value, "Not a dynamic function's kind.");
            if (value != DynamicFunctionKind.Normal && Goal is not (ParseGoal.FormalParameters or ParseGoal.FunctionBody))
                throw new ArgumentException("A function kind applies to a dynamic function's parameters and body alone, not to " + Goal + ".", nameof(FunctionKind));
            functionKind = value;
        }
    }

    /// <summary>Broiler.Unicode's tables, through which the tokenizer reads the identifier and white-space classes (decision D8).</summary>
    public IUnicodeTables UnicodeTables { get; }

    /// <summary>The engine's regular-expression validator, which the parse asks of every literal it reaches (decision D7).</summary>
    public IRegularExpressionValidator RegularExpressions { get; }

    /// <summary>
    /// Whether the result publishes the token stream the parse read, as <see cref="ParseResult.Tokens"/>; false by default.
    /// The parse is the same either way: it reads the same tokens, under the same goals, makes the same tree, side tables
    /// and diagnostics, and allocates the same objects of the same sizes as at 0.1.0-preview.9, the result holding the
    /// stream it already kept and a flag beside its outcome; the switch decides only whether the result hands the stream
    /// out (the owner's decision of 2026-11-04, for Broiler.VM's tokenizers-agreement row; decision D21).
    /// </summary>
    public bool RetainTokens { get; init; }

    /// <summary>
    /// The caller's meter, or null for none (decision D32; convention 4). The parse charges one unit per UTF-16 code unit
    /// of <see cref="SourceText.Text"/>, the whole text in one block, before it reads the first one, so a meter that throws
    /// stops the parse before any work; it polls for no cancellation, its work being linear in the source with bounded
    /// constants. Both engines pass none at SJL-13.
    /// </summary>
    public WorkCharge? Meter { get; init; }
}
