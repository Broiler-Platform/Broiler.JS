#nullable enable

using System.Collections.Generic;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>The goal a bridged parse starts from (the shared library's <c>ParseGoal</c>, in this engine's terms).</summary>
internal enum BridgeGoal
{
    /// <summary>A script, the goal of every compile that is not a module's.</summary>
    Script,

    /// <summary>A module.</summary>
    Module,

    /// <summary>Eval code, which inherits its caller's context (<see cref="BridgeRequest"/>'s flags).</summary>
    Eval,

    /// <summary>The <c>Function</c> constructor's parameter text, parsed alone.</summary>
    FormalParameters,

    /// <summary>The <c>Function</c> constructor's body text, parsed alone.</summary>
    FunctionBody,
}

/// <summary>The kind of function the <c>Function</c> constructors build (CreateDynamicFunction's <c>kind</c>).</summary>
internal enum BridgeFunctionKind
{
    /// <summary><c>Function</c>.</summary>
    Normal,

    /// <summary><c>GeneratorFunction</c>.</summary>
    Generator,

    /// <summary><c>AsyncFunction</c>.</summary>
    Async,

    /// <summary><c>AsyncGeneratorFunction</c>.</summary>
    AsyncGenerator,
}

/// <summary>
/// What one bridged parse is asked (milestone SJL-14.2, decision D44): its goal, its permission to await at the top
/// level, and, for eval code, the context it inherits from its caller as this engine's direct-eval support computes
/// it; for the <c>Function</c> constructor's pieces, the function's kind.
/// </summary>
/// <remarks>
/// <para>
/// The caller's strictness reaches the library as its forced-strictness switch; the function, method,
/// derived-constructor and field-initialiser flags as its inherited context; the visible private names as its
/// private-name set, which this engine checks at the parse of eval code today (bundle <c>sjl-14-001</c>'s
/// inherited-context probes), so they are passed. The caller's lexical names are not (decision D16).
/// </para>
/// </remarks>
internal sealed record BridgeRequest
{
    /// <summary>A script without top-level <c>await</c>.</summary>
    internal static BridgeRequest Script { get; } = new();

    /// <summary>A module, which may <c>await</c> at its top level.</summary>
    internal static BridgeRequest Module { get; } = new() { Goal = BridgeGoal.Module, AllowTopLevelAwait = true };

    /// <summary>The goal.</summary>
    internal BridgeGoal Goal { get; init; }

    /// <summary>Whether <c>await</c> may stand at the top level of a script or module.</summary>
    internal bool AllowTopLevelAwait { get; init; }

    /// <summary>Whether the code is strict from its start: eval code whose caller is strict.</summary>
    internal bool ForcedStrict { get; init; }

    /// <summary>Eval code's caller is in a non-arrow function: <c>new.target</c> is admitted.</summary>
    internal bool InFunction { get; init; }

    /// <summary>Eval code's caller has a super binding: a <c>super</c> property is admitted.</summary>
    internal bool InMethod { get; init; }

    /// <summary>Eval code's caller is a derived class's constructor: <c>super()</c> is admitted.</summary>
    internal bool InDerivedConstructor { get; init; }

    /// <summary>Eval code's caller is a class field initialiser: <c>arguments</c> is refused.</summary>
    internal bool InClassFieldInitializer { get; init; }

    /// <summary>The private names visible to eval code's caller, each with its <c>#</c>; null where not given.</summary>
    internal IReadOnlyList<string>? VisiblePrivateNames { get; init; }

    /// <summary>The kind of the dynamic function whose pieces the two dynamic-function goals parse.</summary>
    internal BridgeFunctionKind FunctionKind { get; init; }
}
