// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsBigInt.cs
//     blob 8898b1970980797a0b10fe6581af4b14f5804678: JsBigInt, of which its value, its policy, its decimal string and the String integer parse are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: every member of JsBigInt but `MaximumBits`, `Policy`, the constructor, `Value`, `ToDecimalString`, `Equals`, `GetHashCode` and `TryParseStringInteger`: the arithmetic, the conversions to a Number and the constants, which no compared member calls.

using System;
using System.Numerics;
using Broiler.ECMAScript;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// One BigInt: an exact signed integer of bounded width. <b>Internal</b>; the language's BigInt,
/// built on it, is the <c>broiler.javascript.bigint</c> surface since card B05.
/// </summary>
/// <remarks>
/// <para>
/// <b>The magnitude is a <see cref="BigInteger"/>, and that is a choice between
/// allowed things rather than an assumption that a library is permitted</b> (decision JSD-0033).
/// It is part of the base class library, it is trimming- and Native-AOT-safe, it reaches no
/// reflection, dynamic loading or unmanaged code (rule B5), and it adds no reference outside the
/// framework (rule B1). A hand-written limb array was weighed and refused: it would be a second
/// arbitrary-precision implementation for this profile to get right, and arithmetic (JSeal B03) is
/// where a wrong carry would show up as a plausible wrong answer.
/// </para>
/// <para>
/// <b>A class and not the struct itself, for two reasons.</b> <see cref="JsValue"/> already carries
/// a reference field, so a BigInt costs the value layout nothing: the tag says BigInt, the double is
/// unused, and the reference is this. And a class is where the width ceiling, the size a charge is
/// computed from, and the canonical decoding live, so no caller handles a bare
/// <see cref="BigInteger"/> it could forget to bound.
/// </para>
/// <para>
/// <b>Identity means nothing.</b> Two instances holding one integer are the same BigInt: equality
/// and hashing are mathematical, which is what <c>===</c>, <c>SameValueZero</c> and a Map key
/// require of a primitive.
/// </para>
/// <para>
/// <b>The operations are the shared library's</b> (decision JSD-0069): the arithmetic, the bitwise
/// operations, the shifts, <c>asIntN</c> and <c>asUintN</c>, the text, the parse and the conversions
/// to and from a Number are <c>BigIntMath</c> and <c>BigIntText</c>, under <see cref="Policy"/> and
/// charged in this family's units to the fuel meter the engine passes. Each member below is an
/// adapter of one line: it passes the policy and the meter, and answers <see langword="null"/> for an
/// over-width status, which the engine turns into the <c>RangeError</c>.
/// </para>
/// </remarks>
internal sealed class JsBigInt : IEquatable<JsBigInt>
{
    /// <summary>
    /// The widest magnitude a BigInt value of this realm may hold, in bits: 1,048,576 (128 KiB).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constant bound (65,536 bits) and this value bound are two numbers with one owner. Every
    /// operation that computes a BigInt (JSeal B03-B04) answers <see langword="null"/> rather than a
    /// value past it, and the engine turns that into a <c>RangeError</c>; an operation whose result
    /// is known to be too wide from its operands' widths refuses before it allocates.
    /// </para>
    /// <para>
    /// <b>The width is <see cref="BigInteger.GetBitLength"/>'s</b>: the shortest
    /// two's-complement length without the sign bit. For a non-negative value that is the
    /// magnitude's bit count; for a negative power of two it is one less, so <c>-(2n ** 1048576n)</c>
    /// is admitted and <c>2n ** 1048576n</c> is not (decision JSD-0033, as amended for B03).
    /// </para>
    /// </remarks>
    internal const long MaximumBits = 1L << 20;

    /// <summary>
    /// <see cref="MaximumBits"/> as the shared library's width policy: every operation below runs
    /// under it, and an embedder's BigInt is admitted by it.
    /// </summary>
    internal static readonly BigIntPolicy Policy = new(MaximumBits);

    /// <summary>Creates a BigInt holding <paramref name="value"/>.</summary>
    internal JsBigInt(BigInteger value)
    {
        if (value.GetBitLength() > MaximumBits)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "a BigInt wider than the realm's ceiling was constructed");
        }

        Value = value;
    }

    /// <summary>The integer.</summary>
    internal BigInteger Value { get; }

    /// <summary>
    /// The abstract operation <c>BigInt::toString(x, 10)</c>: optional <c>-</c>, then decimal
    /// digits, and no <c>n</c>, with every step charged to <paramref name="charge"/> before it runs.
    /// </summary>
    /// <remarks>
    /// The shared library's <c>BigIntText.ToString</c>, divide and conquer: the digit bound is charged
    /// first, linearly, then each division and each leaf, so a wide value's conversion meets the
    /// cancellation poll between its steps.
    /// </remarks>
    internal string ToDecimalString(WorkCharge charge) => BigIntText.ToString(Value, 10, charge);

    /// <summary>Mathematical equality.</summary>
    public bool Equals(JsBigInt? other) => other is not null && Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is JsBigInt other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    // ---- arithmetic (JSeal B03) and bitwise operations (JSeal B04) ---------------------------------
    //
    // EVERY OPERATION CHARGES BEFORE IT COMPUTES AND ANSWERS NULL RATHER THAN A VALUE PAST THE
    // CEILING. The work is the shared library's BigIntMath; the charge goes to the meter the engine
    // passes, which is also where cancellation is polled, and the engine turns a null into the
    // RangeError the language gives a result it cannot represent. Argument errors the specification
    // names - a zero divisor, a negative exponent - are the engine's to raise before it calls here.

    /// <summary>
    /// The abstract operation <c>StringToBigInt</c>: <see langword="false"/> when
    /// <paramref name="text"/> is not a <c>StringIntegerLiteral</c>; otherwise the value, or
    /// <see langword="null"/> with its sign in <paramref name="tooWideSign"/> when the value is wider
    /// than the ceiling.
    /// </summary>
    /// <remarks>
    /// <b>A value past the ceiling is reported, not refused</b>, because the callers need different
    /// things from it: <c>BigInt(text)</c> raises the RangeError, <c>==</c> answers <c>false</c> (no
    /// value in the realm equals it), and a relational comparison answers by its sign - each exact.
    /// The parse is the shared library's <c>BigIntText.StringToBigInt</c>, which charges the text's
    /// length before it reads it.
    /// </remarks>
    internal static bool TryParseStringInteger(string text, WorkCharge charge, out JsBigInt? value, out int tooWideSign)
    {
        switch (BigIntText.StringToBigInt(text, Policy, charge, out var parsed, out tooWideSign))
        {
            case ResultStatus.Completed:
                value = new JsBigInt(parsed);
                return true;
            case ResultStatus.LimitExceeded:
                value = null;
                return true;
            default:
                value = null;
                return false;
        }
    }
}
