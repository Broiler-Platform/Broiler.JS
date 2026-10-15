// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           5
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  2/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// Which strings <see cref="EcmaDateParser.Parse"/> reads beyond the Date Time String Format, which is the
/// part of <c>Date.parse</c> (ECMA-262 21.4.3.2) the specification leaves to the implementation.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=F8E05E
// Broiler-Human:        PENDING
public enum DateParseDialect
{
    /// <summary>
    /// The Date Time String Format (21.4.1.32) and nothing else: a day past its month's length, a fraction
    /// of other than three digits, a lower-case <c>t</c> or <c>z</c>, an offset without its colon and every
    /// other shape are refused.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Broiler.VM's reading, adopted as a dialect by decision JSD-0072: the format with a day of up to 31
    /// carried into the next month, then, for a string that starts with neither four digits nor a sign,
    /// a textual reading of its own <c>toString</c> and <c>toUTCString</c> forms and the shapes around them,
    /// with a year that may be written with a minus sign.
    /// </summary>
    BroilerVM = 1,
}

/// <summary>What a parse found: no date, a UTC time value, or a local time the caller converts with <c>UTC</c>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=FC41CC
// Broiler-Human:        PENDING
public enum EcmaParsedKind
{
    /// <summary>The string is not a date in the dialect, or its date is outside the time values; <c>Date.parse</c> answers NaN.</summary>
    Invalid = 0,

    /// <summary>A time value, already clipped (21.4.1.31): a date-only form, or a form with a UTC offset.</summary>
    Utc = 1,

    /// <summary>
    /// A local time, not yet converted or clipped: a date-time form without an offset (21.4.1.32). The
    /// caller answers <c>TimeClip(UTC(value))</c> (21.4.1.26) under its own local time zone.
    /// </summary>
    Local = 2,
}

/// <summary>The result of <see cref="EcmaDateParser.Parse"/>.</summary>
/// <param name="Kind">What the string held.</param>
/// <param name="Value">The time value for <see cref="EcmaParsedKind.Utc"/>, the local time for <see cref="EcmaParsedKind.Local"/>, and NaN for <see cref="EcmaParsedKind.Invalid"/>.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=D252A7
// Broiler-Human:        PENDING
public readonly record struct EcmaParsedDate(EcmaParsedKind Kind, double Value);

/// <summary>
/// The string reading of <c>Date.parse</c> and of the <c>Date</c> constructor with one String
/// (ECMA-262 21.4.3.2 and 21.4.2.1): the Date Time String Format (21.4.1.32), and, in Broiler.VM's
/// dialect, that engine's reading of other strings.
/// </summary>
/// <remarks>
/// <para>
/// <b>The format.</b> A year of four digits, or a sign and six (21.4.1.32.1, which refuses
/// <c>-000000</c>); optionally a month, and with it optionally a day; then optionally <c>T</c> and a time of
/// <c>HH:mm</c>, <c>HH:mm:ss</c> or <c>HH:mm:ss.sss</c>, with exactly three fraction digits, followed
/// optionally by <c>Z</c> or a <c>±HH:mm</c> offset. Every element is in bounds: a month from 01 to 12, a
/// day from 01 to 31, an hour to 24 only for <c>24:00</c> with no seconds, minutes and seconds to 59, an
/// offset's hours to 23. A date-only form is UTC, and a date-time form without an offset is local time.
/// </para>
/// <para>
/// <b>The charge</b> (decision JSD-0072, 4): the text's length, in code units, before any character is
/// read, when the text is not empty, whatever the outcome. Broiler.VM's adapter adds its own unit.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.32; IP=Low; Security=High; Resources=2; Fingerprint=0047BF
// Broiler-Falsified-If: a string outside the format is read by the strict dialect, a string of the format is refused or read at another time value, or Broiler.VM's dialect answers differently from that engine's DateParseText on a string other than a rendering of a negative year
// Broiler-Human:        PENDING
public static class EcmaDateParser
{
    /// <summary>Reads a string as a date in a dialect.</summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="dialect">Which strings beyond the format to read.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>The kind of date found and its value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The dialect is not one of the enumeration's.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=High; Resources=2; Fingerprint=5C16E2
    // Broiler-Falsified-If: the meter is charged other than the text's length before the first character is read, or charged for an empty text
    // Broiler-Human:        PENDING
    public static EcmaParsedDate Parse(ReadOnlySpan<char> text, DateParseDialect dialect, WorkCharge? charge = null)
    {
        if (dialect is not (DateParseDialect.Strict or DateParseDialect.BroilerVM))
            throw new ArgumentOutOfRangeException(nameof(dialect));

        if (charge is not null && text.Length > 0)
            charge((ulong)text.Length);

        if (dialect == DateParseDialect.Strict)
            return ReadFormat(text, strict: true);

        // Broiler.VM reads the format when the text starts with a sign or four digits, and the textual
        // forms otherwise; a text that starts like the format and is not one is not read again.
        if (text.Length > 0 && (text[0] is '+' or '-' || StartsWithDigits(text, 4)))
            return ReadFormat(text, strict: false);

        return ReadTextual(text);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=E57066
    // Broiler-Human:        PENDING
    private static readonly EcmaParsedDate Invalid = new(EcmaParsedKind.Invalid, double.NaN);

    // The Date Time String Format. The strict dialect holds a day to its month's length; Broiler.VM's
    // admits any day to 31 and carries it, as MakeDay does.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.32; IP=Low; Security=Medium; Resources=1; Fingerprint=151641
    // Broiler-Human:        PENDING
    private static EcmaParsedDate ReadFormat(ReadOnlySpan<char> text, bool strict)
    {
        var at = 0;
        long year;
        if (text.Length > 0 && text[0] is '+' or '-')
        {
            var negative = text[0] == '-';
            at = 1;
            if (!ReadDigits(text, ref at, 6, out year) || (negative && year == 0))
                return Invalid;
            if (negative)
                year = -year;
        }
        else if (!ReadDigits(text, ref at, 4, out year))
        {
            return Invalid;
        }

        long month = 1;
        long day = 1;
        long hour = 0;
        long minute = 0;
        long second = 0;
        long millisecond = 0;
        long offsetMinutes = 0;
        var hasTime = false;
        var hasOffset = false;

        if (At(text, at, '-'))
        {
            at++;
            if (!ReadDigits(text, ref at, 2, out month))
                return Invalid;
            if (At(text, at, '-'))
            {
                at++;
                if (!ReadDigits(text, ref at, 2, out day))
                    return Invalid;
            }
        }

        if (At(text, at, 'T'))
        {
            hasTime = true;
            at++;
            if (!ReadDigits(text, ref at, 2, out hour) || !At(text, at, ':'))
                return Invalid;
            at++;
            if (!ReadDigits(text, ref at, 2, out minute))
                return Invalid;
            if (At(text, at, ':'))
            {
                at++;
                if (!ReadDigits(text, ref at, 2, out second))
                    return Invalid;
                if (At(text, at, '.'))
                {
                    at++;
                    if (!ReadDigits(text, ref at, 3, out millisecond))
                        return Invalid;
                }
            }

            if (At(text, at, 'Z'))
            {
                hasOffset = true;
                at++;
            }
            else if (At(text, at, '+') || At(text, at, '-'))
            {
                var negative = text[at] == '-';
                at++;
                if (!ReadDigits(text, ref at, 2, out var offsetHour) || !At(text, at, ':'))
                    return Invalid;
                at++;
                if (!ReadDigits(text, ref at, 2, out var offsetMinute) || offsetHour > 23 || offsetMinute > 59)
                    return Invalid;

                hasOffset = true;
                offsetMinutes = negative ? -((offsetHour * 60) + offsetMinute) : (offsetHour * 60) + offsetMinute;
            }
        }

        if (at != text.Length
            || month is < 1 or > 12
            || day < 1 || day > (strict ? DaysInMonth(year, (int)month) : 31)
            || minute > 59 || second > 59
            || hour > 24 || (hour == 24 && (minute != 0 || second != 0 || millisecond != 0)))
        {
            return Invalid;
        }

        var local = EcmaTime.MakeDate(
            EcmaTime.MakeDay(year, month - 1, day),
            EcmaTime.MakeTime(hour, minute, second, millisecond));

        // A date-only form is UTC; a date-time form is local time unless it carries an offset.
        if (hasTime && !hasOffset)
            return new EcmaParsedDate(EcmaParsedKind.Local, local);

        return Clipped(local - (offsetMinutes * EcmaTime.MsPerMinute));
    }

    // Broiler.VM's textual reading: the tokens between spaces, commas and tabs, read for a month name, a
    // clock, a GMT or UTC offset and numbers, up to a parenthesised zone name. It reads what that engine
    // renders - "Thu, 01 Jan 1970 00:00:00 GMT", "Thu Jan 01 1970 00:00:00 GMT+0000 (...)" and
    // "Thu Jan 01 1970" - and the shapes around them, exactly as Broiler.VM's DateParseTextual did,
    // except that a minus sign and digits are a year (correspondence row P9 of bundle sjl-6-001), so that
    // its renderings of negative years read back.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Medium; Resources=1; Fingerprint=0D2460
    // Broiler-Human:        PENDING
    private static EcmaParsedDate ReadTextual(ReadOnlySpan<char> text)
    {
        double month = -1;
        double day = -1;
        var year = double.NaN;
        double hour = 0;
        double minute = 0;
        double second = 0;
        double offsetMinutes = 0;
        var hasZone = false;

        var at = 0;
        while (NextToken(text, ref at, out var token))
        {
            if (token[0] == '(')
                break;

            var named = MonthByName(token);
            if (named >= 0 && month < 0)
            {
                month = named;
                continue;
            }

            if (token.Contains(':'))
            {
                if (!ReadClock(token, ref hour, ref minute, ref second))
                    return Invalid;
                continue;
            }

            if (token.StartsWith("GMT", StringComparison.Ordinal) || token.StartsWith("UTC", StringComparison.Ordinal))
            {
                hasZone = true;
                var zone = token[3..];
                if (zone.Length == 0)
                    continue;

                if (zone.Length != 5 || zone[0] is not ('+' or '-')
                    || !WholeNumber(zone[1..3], out var zoneHour)
                    || !WholeNumber(zone[3..], out var zoneMinute))
                {
                    return Invalid;
                }

                offsetMinutes = (zoneHour * 60) + zoneMinute;
                if (zone[0] == '-')
                    offsetMinutes = -offsetMinutes;
                continue;
            }

            if (token.Length > 1 && token[0] == '-' && WholeNumber(token[1..], out var magnitude))
            {
                // A year with its sign, as this engine renders a negative year; a later one is skipped,
                // as every token that is not a number was before.
                if (double.IsNaN(year))
                    year = -magnitude;
                continue;
            }

            if (!WholeNumber(token, out var number))
                continue;

            if (token.Length >= 3 || (day >= 0 && double.IsNaN(year)))
                year = number;
            else
                day = number;
        }

        if (month < 0 || day < 1 || day > 31 || double.IsNaN(year) || hour > 24 || minute > 59 || second > 59)
            return Invalid;

        var local = EcmaTime.MakeDate(EcmaTime.MakeDay(year, month, day), EcmaTime.MakeTime(hour, minute, second, 0));
        if (!hasZone)
            return new EcmaParsedDate(EcmaParsedKind.Local, local);

        return Clipped(local - (offsetMinutes * EcmaTime.MsPerMinute));
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.31; IP=Low; Security=Low; Resources=1; Fingerprint=6B1FE0
    // Broiler-Human:        PENDING
    private static EcmaParsedDate Clipped(double time)
    {
        var clipped = EcmaTime.TimeClip(time);
        return double.IsNaN(clipped) ? Invalid : new EcmaParsedDate(EcmaParsedKind.Utc, clipped);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.32; IP=Low; Security=Low; Resources=1; Fingerprint=6B90B1
    // Broiler-Human:        PENDING
    private static int DaysInMonth(long year, int month) =>
        month is >= 1 and <= 12 ? IsoCalendarMath.DaysInMonth(year, month) : 0;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.32; IP=Low; Security=Low; Resources=1; Fingerprint=29D898
    // Broiler-Human:        PENDING
    private static bool At(ReadOnlySpan<char> text, int at, char expected) => at < text.Length && text[at] == expected;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=3F77FF
    // Broiler-Human:        PENDING
    private static bool StartsWithDigits(ReadOnlySpan<char> text, int count)
    {
        if (text.Length < count)
            return false;
        foreach (var c in text[..count])
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }

    // Exactly count decimal digits at the cursor, which moves past them.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.32; IP=Low; Security=Medium; Resources=1; Fingerprint=3414B1
    // Broiler-Human:        PENDING
    private static bool ReadDigits(ReadOnlySpan<char> text, ref int at, int count, out long value)
    {
        value = 0;
        if (at + count > text.Length)
            return false;

        for (var step = 0; step < count; step++)
        {
            var c = text[at + step];
            if (c is < '0' or > '9')
                return false;
            value = (value * 10) + (c - '0');
        }

        at += count;
        return true;
    }

    // The next token between the separators Broiler.VM split on - space, comma and tab - skipping empty ones.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Medium; Resources=1; Fingerprint=405624
    // Broiler-Human:        PENDING
    private static bool NextToken(ReadOnlySpan<char> text, ref int at, out ReadOnlySpan<char> token)
    {
        while (at < text.Length && IsSeparator(text[at]))
            at++;

        var start = at;
        while (at < text.Length && !IsSeparator(text[at]))
            at++;

        token = text[start..at];
        return token.Length > 0;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=3F4562
    // Broiler-Human:        PENDING
    private static bool IsSeparator(char c) => c is ' ' or ',' or '\t';

    // HH:mm or HH:mm:ss, each part a whole number of any length. A clock of two parts keeps the seconds an
    // earlier clock of three set, as Broiler.VM's reader did: only the parts a clock has are read.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Medium; Resources=1; Fingerprint=92DE4C
    // Broiler-Human:        PENDING
    private static bool ReadClock(ReadOnlySpan<char> token, ref double hour, ref double minute, ref double second)
    {
        var first = token.IndexOf(':');
        var rest = token[(first + 1)..];
        var next = rest.IndexOf(':');
        if (next < 0)
            return WholeNumber(token[..first], out hour) && WholeNumber(rest, out minute);

        var last = rest[(next + 1)..];
        return !last.Contains(':')
            && WholeNumber(token[..first], out hour)
            && WholeNumber(rest[..next], out minute)
            && WholeNumber(last, out second);
    }

    // A token that is entirely decimal digits, accumulated as Broiler.VM did: value × 10 + digit, in doubles.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=F56E00
    // Broiler-Human:        PENDING
    private static bool WholeNumber(ReadOnlySpan<char> token, out double value)
    {
        value = 0;
        if (token.Length == 0)
            return false;

        foreach (var c in token)
        {
            if (c is < '0' or > '9')
            {
                value = 0;
                return false;
            }

            value = (value * 10) + (c - '0');
        }

        return true;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.3.2; IP=Low; Security=Low; Resources=1; Fingerprint=D60FEC
    // Broiler-Human:        PENDING
    private static int MonthByName(ReadOnlySpan<char> token) => token switch
    {
        "Jan" => 0,
        "Feb" => 1,
        "Mar" => 2,
        "Apr" => 3,
        "May" => 4,
        "Jun" => 5,
        "Jul" => 6,
        "Aug" => 7,
        "Sep" => 8,
        "Oct" => 9,
        "Nov" => 10,
        "Dec" => 11,
        _ => -1,
    };
}
