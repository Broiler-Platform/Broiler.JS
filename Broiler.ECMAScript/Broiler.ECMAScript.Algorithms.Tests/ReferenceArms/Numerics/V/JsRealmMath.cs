// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Math.cs
//     blob dd83888d7ea03060117c6abfc380f7f76733ecf1: the cbrt snap in SetupMath's "cbrt" lambda, MathExpMinusOne, MathLogOnePlus, MathRoundHalfUp, MathHypotenuse, MathPower, MathSumPrecise's kernel and MathDistil
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the members sit in a static class JsRealmMath of their own instead of the partial class JsRealm, whose other members are the engine's;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - MathExpMinusOne, MathLogOnePlus, MathRoundHalfUp and MathHypotenuse made internal (private in the source); MathSumPrecise made internal and static (private and an instance method in the source; it reads no instance state);
//   - the cbrt snap is SetupMath's "cbrt" lambda wrapped in the thin function Cbrt(double): the comment above the lambda is verbatim at its own indentation, now inside the function, and the lambda's lines are verbatim, indented four spaces less; the lambda's first line, `var value = MathNumberAt(engine, arguments, 0);` (ToNumber, the adapter's), is the parameter `value`, and `return JsValue.Number(near * near * near == value ? near : root);` is `return near * near * near == value ? near : root;`;
//   - MathHypotenuse's parameters (JsEngine engine, JsValue[] arguments) are (ReadOnlySpan<double> arguments, Action<ulong> charge): `var value = engine.ToNumber(arguments[at]);` is `var value = arguments[at];` (the values are already ToNumber'd, in order), and each `engine.Charge(1);` is `charge(1);`, the engine's JsEngine.Charge(ulong) passed as the delegate, so the copy makes every charge the source makes (1 a value in the first loop, then 1 a value again in the second when no value is infinite or NaN and the largest magnitude is not zero);
//   - MathSumPrecise's parameters (JsEngine engine, JsValue source) are (IReadOnlyList<double> source, Action<ulong> charge), the Numbers the iteration yields, in order: the iteration itself (JsRealm.CollectionEach in JsRealm.Collections.cs, which charges 1 an element and closes the iterator) is the engine's and is replaced by the private thin wrapper CollectionEach(IReadOnlyList<double>, Action<double>) below, which calls the step once an element in order; in the step, `engine.Charge(1);` is `charge(1);`, the element-type check that throws the TypeError (`if (!element.IsNumber) { throw engine.Error("TypeError", ...); }`) is the adapter's and is not copied, and `element.AsNumber()` is `element`; every state, constant and arithmetic step is verbatim;
//   - the using directives the copied members do not need (System.Buffers.Binary, System.Security.Cryptography) dropped.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>Broiler.VM's <c>Math</c> functions that are not the platform's: <c>cbrt</c>'s snap, <c>expm1</c>, <c>log1p</c>, <c>round</c>, <c>hypot</c>, <c>pow</c> and <c>sumPrecise</c>.</summary>
internal static class JsRealmMath
{
    /// <summary>The cbrt snap: the body of the <c>Math.cbrt</c> built-in after <c>ToNumber</c>.</summary>
    internal static double Cbrt(double value)
    {
        // THE SNAP IS NOT A TIDY-UP, it is what makes a perfect cube answer its own root. The
        // specification leaves this function's precision implementation-defined, so both
        // `Math.cbrt(27) === 3` and the platform's `3.0000000000000004` are conforming - but a
        // program that indexes by a cube root, or compares one against an integer, sees the second
        // as a defect. Snapping only when the rounded value cubes back EXACTLY means no result that
        // was not already a perfect cube is moved.
        var root = Math.Cbrt(value);
        var near = Math.Round(root);

        return near * near * near == value ? near : root;
    }

    /// <summary>
    /// <c>exp(x) - 1</c>, computed without losing the significant digits near zero.
    /// </summary>
    /// <remarks>
    /// The operation is IEEE 754's <c>expm1</c> and it lives on <c>double</c> rather than on
    /// <c>System.Math</c>, which has no such member. The zero case is taken here rather than left
    /// to the platform because <c>double.ExpM1(-0)</c> answers <c>+0</c> and the language requires
    /// <c>-0</c> - a difference nothing but a conformance test ever sees, and one it always sees.
    /// </remarks>
    internal static double MathExpMinusOne(double value) =>
        value == 0 ? value : double.ExpM1(value);

    /// <summary>
    /// <c>log(1 + x)</c>, computed without losing the significant digits near zero.
    /// </summary>
    /// <remarks>
    /// IEEE 754's <c>log1p</c>, with the same signed-zero correction as
    /// <see cref="MathExpMinusOne"/>: <c>double.LogP1(-0)</c> answers <c>+0</c> where the language
    /// requires <c>-0</c>.
    /// </remarks>
    internal static double MathLogOnePlus(double value) =>
        value == 0 ? value : double.LogP1(value);

    /// <summary>
    /// The specification's <c>Math.round</c>, which is not <c>System.Math.Round</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language rounds a half toward +&#8734; and the platform rounds it to even, so the two
    /// disagree on every other half: <c>Math.round(2.5)</c> is <c>3</c> and
    /// <c>System.Math.Round(2.5)</c> is <c>2</c>. Rounding toward +&#8734; also means
    /// <c>Math.round(-0.5)</c> is <c>-0</c> rather than <c>-1</c>.
    /// </para>
    /// <para>
    /// The three guards before the <c>floor(x + 1/2)</c> are not decoration. An already-integral
    /// argument is returned unchanged, because for a magnitude at or above 2^52 the addition itself
    /// rounds and would answer <c>x + 1</c> for an odd <c>x</c>. The two half-open intervals around
    /// zero are returned as signed zeroes, because <c>floor(0.49999999999999994 + 0.5)</c> is
    /// <c>1</c> - the addition rounds up to exactly <c>1</c> - where the closest integer is
    /// <c>0</c>.
    /// </para>
    /// </remarks>
    internal static double MathRoundHalfUp(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
        {
            return value;
        }

        if (Math.Floor(value) == value)
        {
            return value;
        }

        if (value > 0 && value < 0.5)
        {
            return 0;
        }

        if (value < 0 && value >= -0.5)
        {
            return -0.0;
        }

        return Math.Floor(value + 0.5);
    }

    /// <summary>
    /// The specification's <c>Math.hypot</c>: <c>+0</c> for no arguments, <c>+&#8734;</c> when any
    /// argument is infinite even if another is <c>NaN</c>, and otherwise the square root of the sum
    /// of the squares.
    /// </summary>
    /// <remarks>
    /// The sum is taken over the magnitudes divided by the largest of them, so an argument near the
    /// top of the range does not overflow on being squared and one near the bottom does not flush to
    /// zero. The scaling is exact for powers of two and costs one extra pass, which is why the
    /// coerced values are kept rather than re-read - re-reading would run a <c>valueOf</c> twice.
    /// </remarks>
    internal static double MathHypotenuse(ReadOnlySpan<double> arguments, Action<ulong> charge)
    {
        var magnitudes = new List<double>(arguments.Length);
        var largest = 0.0;
        var sawInfinity = false;
        var sawNotANumber = false;

        for (var at = 0; at < arguments.Length; at++)
        {
            charge(1);
            var value = arguments[at];

            if (double.IsInfinity(value))
            {
                sawInfinity = true;
            }
            else if (double.IsNaN(value))
            {
                sawNotANumber = true;
            }
            else
            {
                var magnitude = Math.Abs(value);
                magnitudes.Add(magnitude);

                if (magnitude > largest)
                {
                    largest = magnitude;
                }
            }
        }

        if (sawInfinity)
        {
            return double.PositiveInfinity;
        }

        if (sawNotANumber)
        {
            return double.NaN;
        }

        if (largest == 0)
        {
            return 0;
        }

        var sum = 0.0;

        foreach (var magnitude in magnitudes)
        {
            charge(1);
            var scaled = magnitude / largest;
            sum += scaled * scaled;
        }

        return largest * Math.Sqrt(sum);
    }

    /// <summary>
    /// The specification's <c>Number::exponentiate</c>, which differs from IEEE 754 in four places.
    /// </summary>
    /// <remarks>
    /// IEEE 754 - and therefore <c>System.Math.Pow</c> - answers <c>1</c> for <c>pow(1, &#8734;)</c>
    /// and <c>pow(1, NaN)</c>, on the reasoning that one raised to anything is one. The language
    /// answers <c>NaN</c> whenever the exponent is <c>NaN</c> or the base has magnitude <c>1</c> and
    /// the exponent is infinite, and answers <c>1</c> for a zero exponent even over a <c>NaN</c>
    /// base. Those four cases are taken first and everything else is the platform's.
    /// <para>
    /// <b>It is <c>Math.pow</c> AND the <c>**</c> operator</b>, because the specification defines
    /// both as this one operation. The operator used to call <c>System.Math.Pow</c> directly, so
    /// <c>(-1) ** Infinity</c> answered <c>1</c> while <c>Math.pow(-1, Infinity)</c> answered
    /// <c>NaN</c>. No front end folds <c>**</c> over constants, so these two are the only callers.
    /// </para>
    /// </remarks>
    internal static double MathPower(double baseValue, double exponent)
    {
        if (double.IsNaN(exponent))
        {
            return double.NaN;
        }

        if (exponent == 0)
        {
            return 1;
        }

        if (double.IsNaN(baseValue))
        {
            return double.NaN;
        }

        if (double.IsInfinity(exponent) && Math.Abs(baseValue) == 1)
        {
            return double.NaN;
        }

        return Math.Pow(baseValue, exponent);
    }

    /// <summary>The exactly rounded sum of an iterable of Numbers.</summary>
    /// <remarks>
    /// <para>
    /// <b>Every element must already BE a Number.</b> This is the one arithmetic method in the
    /// namespace that does not coerce: a string in the list is a <c>TypeError</c> rather than a
    /// number, because a list of numbers is what the method is for and coercing would hide the
    /// element that is not one.
    /// </para>
    /// <para>
    /// <b>The infinities and NaN are decided before the arithmetic.</b> Both infinities present is
    /// NaN, either alone is itself, and a NaN anywhere wins - so the partial sums never have to
    /// carry a value that is not finite, which they cannot do.
    /// </para>
    /// <para>
    /// <b>An empty list sums to negative zero</b>, which is the identity this operation was given
    /// so that the sign of a sum of negative zeros survives.
    /// </para>
    /// </remarks>
    internal static double MathSumPrecise(IReadOnlyList<double> source, Action<ulong> charge)
    {
        var values = new List<double>();
        var positiveInfinities = 0;
        var negativeInfinities = 0;
        var seenNaN = false;
        var seenPositiveZero = false;
        var largest = 0.0;

        // ONE ELEMENT AT A TIME AND STOPPING AT THE FIRST THAT IS NOT A NUMBER, which the language
        // asks for twice over: nothing is coerced, and the iterator is CLOSED where the walk stops -
        // so a generator handed a bad list runs its `finally` and is asked for nothing more.
        CollectionEach(source, element =>
        {
            charge(1);

            var value = element;

            if (double.IsNaN(value))
            {
                seenNaN = true;
                return;
            }

            if (double.IsInfinity(value))
            {
                if (value > 0)
                {
                    positiveInfinities++;
                }
                else
                {
                    negativeInfinities++;
                }

                return;
            }

            if (value == 0)
            {
                // `-0` IS THE IDENTITY OF THIS OPERATION AND `+0` IS NOT. An empty list and a list
                // of nothing but negative zeros sum to `-0`; one `+0` anywhere makes the answer
                // `+0`. Neither contributes to the arithmetic, so the sign is all that is kept.
                seenPositiveZero |= !double.IsNegative(value);
                return;
            }

            values.Add(value);
            largest = Math.Max(largest, Math.Abs(value));
        });

        if (seenNaN)
        {
            return double.NaN;
        }


        // BOTH INFINITIES PRESENT IS NaN, which is the one case a running total would also have
        // got right and for the wrong reason: here it is decided before any arithmetic, so the
        // partial sums never have to hold a value they cannot represent.
        if (positiveInfinities > 0 && negativeInfinities > 0)
        {
            return double.NaN;
        }

        if (positiveInfinities > 0)
        {
            return double.PositiveInfinity;
        }

        if (negativeInfinities > 0)
        {
            return double.NegativeInfinity;
        }

        if (values.Count == 0)
        {
            return seenPositiveZero ? 0.0 : -0.0;
        }

        // AN INTERMEDIATE SUM MAY OVERFLOW WHERE THE ANSWER DOES NOT, which is what makes this
        // harder than a running total: `[8.99e307, 8.99e307, -1.8e308]` is about 1e292, and any
        // order that adds the first two first passes through infinity. Scaling by a power of two is
        // exact, so a list with a huge term in it is distilled in a scaled domain and scaled back;
        // what that drops is terms below the scaled range, which are orders of magnitude beneath the
        // last bit of an answer this large.
        var scale = largest > 8.98846567431158e307 / 4 ? Math.ScaleB(1.0, -600) : 1.0;
        var total = MathDistil(values, scale);

        return scale == 1.0 ? total : Math.ScaleB(total, 600);
    }

    /// <summary>Shewchuk's distillation: a list of non-overlapping partial sums, added once.</summary>
    private static double MathDistil(List<double> values, double scale)
    {
        var partials = new List<double>();

        foreach (var element in values)
        {
            var carried = element * scale;
            var kept = 0;

            for (var at = 0; at < partials.Count; at++)
            {
                var held = partials[at];

                if (Math.Abs(carried) < Math.Abs(held))
                {
                    (carried, held) = (held, carried);
                }

                var high = carried + held;
                var low = held - (high - carried);

                if (low != 0)
                {
                    partials[kept] = low;
                    kept++;
                }

                carried = high;
            }

            partials.RemoveRange(kept, partials.Count - kept);
            partials.Add(carried);
        }

        // THE LAST STEP IS NOT A LOOP OF ADDITIONS, and that is where exactness is won or lost.
        // The partials are non-overlapping and increasing, so summing them from the top stops at
        // the first addition that is inexact - and the correction below is what makes half-even
        // rounding come out right across two of them, which is the case a plain loop gets wrong by
        // one unit in the last place.
        if (partials.Count == 0)
        {
            return 0.0;
        }

        var index = partials.Count - 1;
        var answer = partials[index];
        var remainder = 0.0;

        while (index > 0)
        {
            index--;
            var above = answer;
            var next = partials[index];
            answer = above + next;
            remainder = next - (answer - above);

            if (remainder != 0.0)
            {
                break;
            }
        }

        if (index > 0 &&
            ((remainder < 0.0 && partials[index - 1] < 0.0) ||
             (remainder > 0.0 && partials[index - 1] > 0.0)))
        {
            var doubled = remainder * 2.0;
            var raised = answer + doubled;

            if (doubled == raised - answer)
            {
                answer = raised;
            }
        }

        return answer;
    }

    /// <summary>
    /// Calls <paramref name="step"/> with each Number of <paramref name="source"/>, in order: this
    /// arm's stand-in for the engine's iteration, <c>JsRealm.CollectionEach</c>, which is not copied.
    /// </summary>
    private static void CollectionEach(IReadOnlyList<double> source, Action<double> step)
    {
        foreach (var element in source)
        {
            step(element);
        }
    }
}
