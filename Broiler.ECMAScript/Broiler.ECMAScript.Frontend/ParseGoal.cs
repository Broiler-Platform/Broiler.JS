// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           9
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The goal symbol a source is parsed as (ECMA-262 clause 5.1.4, the syntactic grammar's goal symbols): the two goals of
/// a source text, eval code, and the two pieces of a dynamic function's source.
/// </summary>
/// <remarks>
/// <para>
/// <b>All five from the first stage</b> (Broiler.VM's JSD-0083, decision D10, with JSD-0080's decision D41, which reserved
/// the last three in both engines): neither engine parses eval code under a goal of its own today, nor a dynamic function's
/// parameters and body under theirs (correspondence rows P03 and P26), and the contract carries the goals for whichever
/// engine routes its eval code or its <c>CreateDynamicFunction</c> through them.
/// </para>
/// <para>
/// The numbers are stable: a goal is never renumbered and a number is never reused.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s5.1.4 goal symbols, JSD-0083 D10; IP=Low; Security=Low; Resources=0; Fingerprint=F26091
// Hairs-Human:        PENDING
public enum ParseGoal
{
    /// <summary>
    /// <c>Script</c> (ECMA-262 clause 16.1, ParseScript): strict only by its directive prologue or the request's forced
    /// strictness.
    /// </summary>
    Script = 0,

    /// <summary><c>Module</c> (ECMA-262 clause 16.2, ParseModule): strict code, with imports and exports.</summary>
    Module = 1,

    /// <summary>
    /// Eval code (ECMA-262 clause 19.2.1.1, PerformEval): the <c>Script</c> grammar, with the early errors PerformEval's
    /// step 11 adds read from the request's <see cref="ParseRequest.Context"/>, the caller's function context.
    /// </summary>
    Eval = 2,

    /// <summary>
    /// A dynamic function's parameters (ECMA-262 clause 20.2.1.1.1, CreateDynamicFunction): <c>FormalParameters</c> with
    /// the <c>[Yield]</c> and <c>[Await]</c> parameters the request's <see cref="ParseRequest.FunctionKind"/> gives it.
    /// </summary>
    FormalParameters = 3,

    /// <summary>
    /// A dynamic function's body (ECMA-262 clause 20.2.1.1.1, CreateDynamicFunction): <c>FunctionBody</c>,
    /// <c>GeneratorBody</c>, <c>AsyncFunctionBody</c> or <c>AsyncGeneratorBody</c>, as the request's
    /// <see cref="ParseRequest.FunctionKind"/> says.
    /// </summary>
    FunctionBody = 4,
}

/// <summary>
/// The kind of a dynamic function (ECMA-262 clause 20.2.1.1.1, CreateDynamicFunction's <c>kind</c>), which decides the
/// goals its parameters and its body are parsed under.
/// </summary>
/// <remarks>
/// The specification parameterises both goals by it, so a request for <see cref="ParseGoal.FormalParameters"/> or
/// <see cref="ParseGoal.FunctionBody"/> carries it; every other goal reads the default, <see cref="Normal"/>, and refuses
/// another.
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s20.2.1.1.1 CreateDynamicFunction kind; IP=Low; Security=Low; Resources=0; Fingerprint=C4A808
// Hairs-Human:        PENDING
public enum DynamicFunctionKind
{
    /// <summary><c>normal</c>: <c>FormalParameters[~Yield, ~Await]</c> and <c>FunctionBody[~Yield, ~Await]</c> (<c>Function</c>).</summary>
    Normal = 0,

    /// <summary><c>generator</c>: <c>FormalParameters[+Yield, ~Await]</c> and <c>GeneratorBody</c> (<c>GeneratorFunction</c>).</summary>
    Generator = 1,

    /// <summary><c>async</c>: <c>FormalParameters[~Yield, +Await]</c> and <c>AsyncFunctionBody</c> (<c>AsyncFunction</c>).</summary>
    Async = 2,

    /// <summary><c>async generator</c>: <c>FormalParameters[+Yield, +Await]</c> and <c>AsyncGeneratorBody</c> (<c>AsyncGeneratorFunction</c>).</summary>
    AsyncGenerator = 3,
}
