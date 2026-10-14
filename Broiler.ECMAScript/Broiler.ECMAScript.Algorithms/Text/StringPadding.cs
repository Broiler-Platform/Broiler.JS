// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           2
// Human-reviewed:   0/10
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  3/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Text;

/// <summary>Where <see cref="StringPadding.Pad"/> puts the filler (ECMA-262 22.1.3.17.2 <c>StringPad</c>'s <c>placement</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.17.2; IP=Low; Security=Low; Resources=1; Fingerprint=031F5F
// Broiler-Human:        PENDING
public enum PadPlacement
{
    /// <summary>Before the String, as <c>padStart</c> does.</summary>
    Start = 0,

    /// <summary>After the String, as <c>padEnd</c> does.</summary>
    End = 1,
}

/// <summary>
/// <c>StringPad</c> (ECMA-262 22.1.3.17.2), the builder of <c>padStart</c> and <c>padEnd</c>, under
/// the caller's longest String.
/// </summary>
/// <remarks>
/// <b>The conversions are the caller's</b> (contract convention 2): <c>StringPaddingBuiltinsImpl</c>
/// (22.1.3.17.1) converts the receiver, then <c>maxLength</c> with <c>ToLength</c>, returns the
/// receiver when <c>maxLength</c> does not exceed its length, and only then converts the filler. A
/// caller follows those steps and hands this the converted values; a filler that is
/// <c>undefined</c> is <c>" "</c>. <b>The longest String is a policy</b>, each engine's own: a
/// result longer than <c>limit</c> is <see cref="ResultStatus.LimitExceeded"/>, which an engine
/// raises as <c>RangeError</c>, and nothing is built.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.17.2; IP=Low; Security=Medium; Resources=2; Fingerprint=81821C
// Broiler-Human:        PENDING
public static class StringPadding
{
    /// <summary><c>StringPad</c> (ECMA-262 22.1.3.17.2).</summary>
    /// <param name="text">The String, <c>S</c>.</param>
    /// <param name="maxLength">The length to pad to, <c>ToLength</c>'s result: zero to 2^53 - 1.</param>
    /// <param name="filler">The filler, <c>fillString</c>, already converted.</param>
    /// <param name="placement">Where the filler goes.</param>
    /// <param name="limit">The longest String the caller can hold, in code units.</param>
    /// <param name="result">The padded String, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="charge">
    /// The caller's meter: charged the result's length, and at least one unit, before it is built;
    /// nothing when the status is <see cref="ResultStatus.LimitExceeded"/>. <see langword="null"/> for none.
    /// </param>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/>, with <paramref name="text"/> itself when
    /// <paramref name="maxLength"/> does not exceed its length or the filler is empty; or
    /// <see cref="ResultStatus.LimitExceeded"/> when the padded String would be longer than
    /// <paramref name="limit"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">The text or the filler is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The length is negative or above 2^53 - 1, or the placement is not one of the enumeration's.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.17.2; IP=Low; Security=High; Resources=3; Fingerprint=0D98D3
    // Broiler-Falsified-If: a String longer than the limit is built or charged, an empty filler is not answered with the String itself, or the filler is not repeated and truncated to exactly maxLength minus the String's length
    // Broiler-Human:        PENDING
    public static ResultStatus Pad(string text, long maxLength, string filler, PadPlacement placement, LengthLimit limit, out string result, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(filler);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxLength, MaximumSafeInteger);
        if (placement is not (PadPlacement.Start or PadPlacement.End))
            throw new ArgumentOutOfRangeException(nameof(placement));

        if (maxLength <= text.Length || filler.Length == 0)
        {
            charge?.Invoke(text.Length == 0 ? 1UL : (ulong)text.Length);
            result = text;
            return ResultStatus.Completed;
        }

        if (!limit.Admits(maxLength) || maxLength > MaximumDotNetString)
        {
            result = string.Empty;
            return ResultStatus.LimitExceeded;
        }

        charge?.Invoke((ulong)maxLength);
        result = string.Create((int)maxLength, (text, filler, placement), static (destination, state) =>
        {
            var (source, fill, where) = state;
            var fillLength = destination.Length - source.Length;
            var padding = where == PadPlacement.Start ? destination[..fillLength] : destination[source.Length..];
            for (var at = 0; at < padding.Length; at += fill.Length)
                fill.AsSpan(0, Math.Min(fill.Length, padding.Length - at)).CopyTo(padding[at..]);

            source.AsSpan().CopyTo(where == PadPlacement.Start ? destination[fillLength..] : destination);
        });
        return ResultStatus.Completed;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=A9573C
    // Broiler-Human:        PENDING
    private const long MaximumSafeInteger = (1L << 53) - 1;

    /// <summary>The longest String the runtime can allocate, so that no limit a caller passes lets the builder fail.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=DF1516
    // Broiler-Human:        PENDING
    internal const int MaximumDotNetString = 0x3FFFFFDF;
}

/// <summary>
/// The relative-index normalisations of the language's built-ins: a relative position, where a
/// negative one counts back from the end, resolved against a length.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each input is already <c>ToIntegerOrInfinity</c>'s result</b> (contract convention 2): an
/// integral double, or an infinity. The conversion is observable and stays in the engine; what is
/// shared is the arithmetic after it, which the language writes out again in every method that
/// takes a start, an end or an index.
/// </para>
/// <para>
/// Clamping without the relative step - <c>substring</c>'s, or <c>String.prototype.indexOf</c>'s
/// position - is one <see cref="Math.Clamp(double, double, double)"/> and is not here.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.22; IP=Low; Security=Medium; Resources=1; Fingerprint=D9DBDA
// Broiler-Human:        PENDING
public static class RelativeIndex
{
    /// <summary>
    /// A relative start or end clamped into the length: the pattern of <c>String.prototype.slice</c>
    /// (ECMA-262 22.1.3.22, steps 4 to 6) and of <c>Array.prototype.slice</c>, <c>fill</c>,
    /// <c>copyWithin</c>, <c>splice</c>, <c>indexOf</c> and <c>includes</c>, and their kin.
    /// </summary>
    /// <param name="relative">The relative position, <c>ToIntegerOrInfinity</c>'s result.</param>
    /// <param name="length">The length, zero to 2^53 - 1.</param>
    /// <returns>Zero for negative infinity; the length plus a negative position, but not below zero; otherwise the position, but not above the length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is NaN or not integral, or the length is negative or above 2^53 - 1.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.22; IP=Low; Security=Medium; Resources=1; Fingerprint=AAC274
    // Broiler-Human:        PENDING
    public static long Clamped(double relative, long length)
    {
        Check(relative, length);
        if (relative < 0)
            return relative + length <= 0 ? 0 : (long)(relative + length);

        return relative >= length ? length : (long)relative;
    }

    /// <summary>
    /// A relative index of one element: the pattern of <c>String.prototype.at</c> (ECMA-262 22.1.3.1,
    /// steps 4 to 6), of <c>Array.prototype.at</c> and <c>with</c>, and of a typed array's.
    /// </summary>
    /// <param name="relative">The relative index, <c>ToIntegerOrInfinity</c>'s result.</param>
    /// <param name="length">The length, zero to 2^53 - 1.</param>
    /// <param name="index">The index, when there is an element there.</param>
    /// <returns><see langword="true"/> when the position, counted back from the end when it is negative, is an index below the length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is NaN or not integral, or the length is negative or above 2^53 - 1.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.1; IP=Low; Security=Medium; Resources=1; Fingerprint=EA7860
    // Broiler-Human:        PENDING
    public static bool TryElement(double relative, long length, out long index)
    {
        Check(relative, length);
        var k = relative >= 0 ? relative : length + relative;
        if (k < 0 || k >= length)
        {
            index = -1;
            return false;
        }

        index = (long)k;
        return true;
    }

    /// <summary>
    /// Where a backward search starts: the pattern of <c>Array.prototype.lastIndexOf</c> (ECMA-262
    /// 23.1.3.20, steps 4 to 6) and of a typed array's, for a <c>fromIndex</c> that was passed.
    /// </summary>
    /// <param name="relative">The relative index, <c>ToIntegerOrInfinity</c>'s result.</param>
    /// <param name="length">The length, zero to 2^53 - 1.</param>
    /// <returns>The index to start from, no greater than the length minus one; -1 when there is nothing to search.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is NaN or not integral, or the length is negative or above 2^53 - 1.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 23.1.3.20; IP=Low; Security=Medium; Resources=1; Fingerprint=158E88
    // Broiler-Human:        PENDING
    public static long LastSearchStart(double relative, long length)
    {
        Check(relative, length);
        if (relative >= 0)
            return relative >= length - 1 ? length - 1 : (long)relative;

        var k = length + relative;
        return k < 0 ? -1 : (long)k;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.5; IP=Low; Security=Medium; Resources=1; Fingerprint=14025B
    // Broiler-Human:        PENDING
    private static void Check(double relative, long length)
    {
        if (double.IsNaN(relative) || (double.IsFinite(relative) && Math.Truncate(relative) != relative))
            throw new ArgumentOutOfRangeException(nameof(relative), "The position is not ToIntegerOrInfinity's result.");

        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, (1L << 53) - 1);
    }
}
