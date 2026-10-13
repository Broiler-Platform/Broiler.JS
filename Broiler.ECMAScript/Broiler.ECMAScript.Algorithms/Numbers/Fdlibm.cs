// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-FileCopyrightText: 1993 Sun Microsystems, Inc.
// SPDX-License-Identifier: Apache-2.0 AND SunPro
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   26
// Annotated:        26/26
// Exempt:           0
// Human-reviewed:   0/26
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       26
//
// GENERATED - DO NOT EDIT MANUALLY

// Ported from fdlibm 5.3 as FreeBSD's msun keeps it, lib/msun/src/s_expm1.c and s_log1p.c, which
// carry this notice:
//
//   Copyright (C) 1993 by Sun Microsystems, Inc. All rights reserved.
//
//   Developed at SunPro, a Sun Microsystems, Inc. business.
//   Permission to use, copy, modify, and distribute this
//   software is freely granted, provided that this notice
//   is preserved.

using System;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// <c>expm1</c> and <c>log1p</c> from fdlibm 5.3, whose error is below one unit in the last place
/// over the whole domain (its own analysis, repeated in each function's comment there).
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported line for line</b>, with C's word access written as bit operations on the double. The
/// code raises no floating-point flags here, so the expressions fdlibm writes only to raise the
/// inexact flag are kept where they also compute the result and dropped where they do not.
/// </para>
/// <para>
/// <b>Why fdlibm and not the platform.</b> On .NET 10 <c>double.ExpM1</c> and <c>double.LogP1</c>
/// compute <c>Exp(x) - 1</c> and <c>Log(x + 1)</c>, which lose every digit near zero; the clauses ask
/// for a result accurate there. V8 computes both with fdlibm too.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Medium; Resources=1; Fingerprint=D263BA
// Broiler-Human:        PENDING
internal static class Fdlibm
{
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=E88585
    // Broiler-Human:        PENDING
    private const double Huge = 1.0e+300;
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=6AA657
    // Broiler-Human:        PENDING
    private const double Tiny = 1.0e-300;
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=1C3962
    // Broiler-Human:        PENDING
    private const double OverflowThreshold = 7.09782712893383973096e+02; // 0x40862E42 FEFA39EF
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=5EBF34
    // Broiler-Human:        PENDING
    private const double Ln2High = 6.93147180369123816490e-01;           // 0x3FE62E42 FEE00000
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=94941C
    // Broiler-Human:        PENDING
    private const double Ln2Low = 1.90821492927058770002e-10;            // 0x3DEA39EF 35793C76
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=F7B681
    // Broiler-Human:        PENDING
    private const double InverseLn2 = 1.44269504088896338700e+00;        // 0x3FF71547 652B82FE
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=64A91A
    // Broiler-Human:        PENDING
    private const double TwoTo54 = 1.80143985094819840000e+16;           // 0x43500000 00000000

    // Scaled Q's of expm1: Qn here is 2^n times Qn of the rational approximation, for R(2z) with z = x * x / 2.
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=4ED9E5
    // Broiler-Human:        PENDING
    private const double Q1 = -3.33333333333331316428e-02; // 0xBFA11111 111110F4
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=F2AA31
    // Broiler-Human:        PENDING
    private const double Q2 = 1.58730158725481460165e-03;  // 0x3F5A01A0 19FE5585
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=B3DEB9
    // Broiler-Human:        PENDING
    private const double Q3 = -7.93650757867487942473e-05; // 0xBF14CE19 9EAADBB7
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=7DF89D
    // Broiler-Human:        PENDING
    private const double Q4 = 4.00821782732936239552e-06;  // 0x3ED0CFCA 86E65239
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Low; Resources=1; Fingerprint=0D76F6
    // Broiler-Human:        PENDING
    private const double Q5 = -2.01099218183624371326e-07; // 0xBE8AFDB7 6E09C32D

    // The polynomial of log1p's reduced argument.
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=4F31F6
    // Broiler-Human:        PENDING
    private const double Lp1 = 6.666666666666735130e-01; // 0x3FE55555 55555593
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=A00624
    // Broiler-Human:        PENDING
    private const double Lp2 = 3.999999999940941908e-01; // 0x3FD99999 9997FA04
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=FA6395
    // Broiler-Human:        PENDING
    private const double Lp3 = 2.857142874366239149e-01; // 0x3FD24924 94229359
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=EF7530
    // Broiler-Human:        PENDING
    private const double Lp4 = 2.222219843214978396e-01; // 0x3FCC71C5 1D8E78AF
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=B6604A
    // Broiler-Human:        PENDING
    private const double Lp5 = 1.818357216161805012e-01; // 0x3FC74664 96CB03DE
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=6739B6
    // Broiler-Human:        PENDING
    private const double Lp6 = 1.531383769920937332e-01; // 0x3FC39A09 D078C69F
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Low; Resources=1; Fingerprint=640E33
    // Broiler-Human:        PENDING
    private const double Lp7 = 1.479819860511658591e-01; // 0x3FC2F112 DF3E5244

    /// <summary>fdlibm's <c>expm1</c>.</summary>
    /// <param name="x">The argument.</param>
    /// <returns><c>e^x - 1</c> within one unit in the last place.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.15 via fdlibm s_expm1.c; IP=Low; Security=Medium; Resources=1; Fingerprint=B9DD4E
    // Broiler-Human:        PENDING
    internal static double Expm1(double x)
    {
        double y, hi, lo, c = 0, t, e, hxs, hfx, r1, twopk;
        int k;

        var hx = HighWord(x);
        var xsb = hx & 0x80000000;
        hx &= 0x7fffffff;

        // Huge and non-finite arguments.
        if (hx >= 0x4043687A)
        {
            if (hx >= 0x40862E42)
            {
                if (hx >= 0x7ff00000)
                {
                    if (((hx & 0xfffff) | LowWord(x)) != 0)
                        return x + x;

                    return xsb == 0 ? x : -1.0;
                }

                if (x > OverflowThreshold)
                    return double.PositiveInfinity;
            }

            if (xsb != 0)
                return Tiny - 1.0;
        }

        // Argument reduction.
        if (hx > 0x3fd62e42)
        {
            if (hx < 0x3FF0A2B2)
            {
                if (xsb == 0)
                {
                    hi = x - Ln2High;
                    lo = Ln2Low;
                    k = 1;
                }
                else
                {
                    hi = x + Ln2High;
                    lo = -Ln2Low;
                    k = -1;
                }
            }
            else
            {
                k = (int)((InverseLn2 * x) + (xsb == 0 ? 0.5 : -0.5));
                t = k;
                hi = x - (t * Ln2High);
                lo = t * Ln2Low;
            }

            x = hi - lo;
            c = (hi - x) - lo;
        }
        else if (hx < 0x3c900000)
        {
            return x;
        }
        else
        {
            k = 0;
        }

        // x is now in the primary range.
        hfx = 0.5 * x;
        hxs = x * hfx;
        r1 = 1.0 + (hxs * (Q1 + (hxs * (Q2 + (hxs * (Q3 + (hxs * (Q4 + (hxs * Q5)))))))));
        t = 3.0 - (r1 * hfx);
        e = hxs * ((r1 - t) / (6.0 - (x * t)));
        if (k == 0)
            return x - ((x * e) - hxs);

        twopk = FromWords((uint)(0x3ff + k) << 20, 0);
        e = (x * (e - c)) - c;
        e -= hxs;
        if (k == -1)
            return (0.5 * (x - e)) - 0.5;
        if (k == 1)
        {
            if (x < -0.25)
                return -2.0 * (e - (x + 0.5));

            return 1.0 + (2.0 * (x - e));
        }

        if (k <= -2 || k > 56)
        {
            y = 1.0 - (e - x);
            y = k == 1024 ? y * 2.0 * 8.98846567431157953865e+307 : y * twopk;
            return y - 1.0;
        }

        if (k < 20)
        {
            t = FromWords(0x3ff00000 - (0x200000u >> k), 0);
            y = t - (e - x);
            y *= twopk;
        }
        else
        {
            t = FromWords((uint)(0x3ff - k) << 20, 0);
            y = x - (e + t);
            y += 1.0;
            y *= twopk;
        }

        return y;
    }

    /// <summary>fdlibm's <c>log1p</c>.</summary>
    /// <param name="x">The argument.</param>
    /// <returns><c>ln(1 + x)</c> within one unit in the last place.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.3.2.21 via fdlibm s_log1p.c; IP=Low; Security=Medium; Resources=1; Fingerprint=4E9423
    // Broiler-Human:        PENDING
    internal static double Log1p(double x)
    {
        double hfsq, f = 0, c = 0, s, z, r, u;
        int k, hu = 0;

        var hx = unchecked((int)HighWord(x));
        var ax = hx & 0x7fffffff;

        k = 1;
        if (hx < 0x3FDA827A)
        {
            // 1 + x < sqrt(2)+.
            if (ax >= 0x3ff00000)
            {
                if (x == -1.0)
                    return double.NegativeInfinity;

                return double.NaN;
            }

            if (ax < 0x3e200000)
            {
                // |x| < 2^-29.
                if (TwoTo54 + x > 0 && ax < 0x3c900000)
                    return x;

                return x - (x * x * 0.5);
            }

            if (hx > 0 || hx <= unchecked((int)0xbfd2bec4))
            {
                // sqrt(2)/2- <= 1 + x < sqrt(2)+.
                k = 0;
                f = x;
                hu = 1;
            }
        }

        if (hx >= 0x7ff00000)
            return x + x;

        if (k != 0)
        {
            if (hx < 0x43400000)
            {
                u = 1.0 + x;
                hu = unchecked((int)HighWord(u));
                k = (hu >> 20) - 1023;
                c = k > 0 ? 1.0 - (u - x) : x - (u - 1.0);
                c /= u;
            }
            else
            {
                u = x;
                hu = unchecked((int)HighWord(u));
                k = (hu >> 20) - 1023;
                c = 0;
            }

            hu &= 0x000fffff;

            // The approximation to sqrt(2) in these thresholds is not critical; the ones above must
            // be looser than this one, so that k == 0 is never reached from here.
            if (hu < 0x6a09e)
            {
                u = WithHighWord(u, (uint)(hu | 0x3ff00000));
            }
            else
            {
                k += 1;
                u = WithHighWord(u, (uint)(hu | 0x3fe00000));
                hu = (0x00100000 - hu) >> 2;
            }

            f = u - 1.0;
        }

        hfsq = 0.5 * f * f;
        if (hu == 0)
        {
            // |f| < 2^-20.
            if (f == 0)
            {
                if (k == 0)
                    return 0;

                c += k * Ln2Low;
                return (k * Ln2High) + c;
            }

            r = hfsq * (1.0 - (0.66666666666666666 * f));
            if (k == 0)
                return f - r;

            return (k * Ln2High) - ((r - ((k * Ln2Low) + c)) - f);
        }

        s = f / (2.0 + f);
        z = s * s;
        r = z * (Lp1 + (z * (Lp2 + (z * (Lp3 + (z * (Lp4 + (z * (Lp5 + (z * (Lp6 + (z * Lp7))))))))))));
        if (k == 0)
            return f - (hfsq - (s * (hfsq + r)));

        return (k * Ln2High) - ((hfsq - ((s * (hfsq + r)) + ((k * Ln2Low) + c))) - f);
    }

    /// <summary>The high 32 bits of a double's representation, C's <c>GET_HIGH_WORD</c>.</summary>
    /// <param name="value">The double.</param>
    /// <returns>Its sign, exponent and the top 20 bits of its fraction.</returns>
    // Broiler-AI:           Origin=Ported; Spec=IEEE 754-2019 3.4 via fdlibm math_private.h; IP=Low; Security=Low; Resources=1; Fingerprint=A7C2B0
    // Broiler-Human:        PENDING
    private static uint HighWord(double value) => (uint)(BitConverter.DoubleToUInt64Bits(value) >> 32);

    /// <summary>The low 32 bits of a double's representation, C's <c>GET_LOW_WORD</c>.</summary>
    /// <param name="value">The double.</param>
    /// <returns>The low 32 bits of its fraction.</returns>
    // Broiler-AI:           Origin=Ported; Spec=IEEE 754-2019 3.4 via fdlibm math_private.h; IP=Low; Security=Low; Resources=1; Fingerprint=8C230D
    // Broiler-Human:        PENDING
    private static uint LowWord(double value) => (uint)BitConverter.DoubleToUInt64Bits(value);

    /// <summary>A double from its two words, C's <c>INSERT_WORDS</c>.</summary>
    /// <param name="high">The high word.</param>
    /// <param name="low">The low word.</param>
    /// <returns>The double.</returns>
    // Broiler-AI:           Origin=Ported; Spec=IEEE 754-2019 3.4 via fdlibm math_private.h; IP=Low; Security=Low; Resources=1; Fingerprint=0E48B1
    // Broiler-Human:        PENDING
    private static double FromWords(uint high, uint low) => BitConverter.UInt64BitsToDouble(((ulong)high << 32) | low);

    /// <summary>A double with its high word replaced, C's <c>SET_HIGH_WORD</c>.</summary>
    /// <param name="value">The double.</param>
    /// <param name="high">The new high word.</param>
    /// <returns>The double with the low word kept.</returns>
    // Broiler-AI:           Origin=Ported; Spec=IEEE 754-2019 3.4 via fdlibm math_private.h; IP=Low; Security=Low; Resources=1; Fingerprint=94E4C7
    // Broiler-Human:        PENDING
    private static double WithHighWord(double value, uint high) => FromWords(high, LowWord(value));
}
