// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           0
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// The Date renderings of ECMA-262 21.4.4: <c>TimeString</c>, <c>DateString</c>, <c>TimeZoneString</c> and
/// <c>ToDateString</c> (21.4.4.41), <c>toUTCString</c>'s text (21.4.4.43) and <c>toISOString</c>'s (21.4.4.36).
/// </summary>
/// <remarks>
/// Every name and number written is fixed ASCII text in the specification's form: three-letter English
/// names, ToZeroPaddedDecimalString's digits, U+002D for a negative year or offset. Nothing reads a
/// culture (contract convention 9); the zone's name is the provider's, written as it is given.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41; IP=Low; Security=Medium; Resources=1; Fingerprint=4F42EA
// Broiler-Human:        PENDING
public static class EcmaDateFormatter
{
    /// <summary>The text of an invalid date, which <c>toString</c> and its siblings answer for a NaN time value (21.4.4.41.4).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.4; IP=Low; Security=Low; Resources=1; Fingerprint=EF3BC4
    // Broiler-Human:        PENDING
    public const string InvalidDate = "Invalid Date";

    /// <summary>
    /// <c>TimeString</c> (ECMA-262 21.4.4.41.1): the hour, the minute and the second of a time, two digits
    /// each and joined by colons, followed by <c>" GMT"</c>.
    /// </summary>
    /// <param name="tv">A finite Number: a local time for <c>toString</c>, a time value for <c>toUTCString</c>.</param>
    /// <returns>For example <c>"00:00:00 GMT"</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tv"/> is not finite.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.1; IP=Low; Security=Medium; Resources=1; Fingerprint=3111B7
    // Broiler-Human:        PENDING
    public static string TimeString(double tv)
    {
        CheckFinite(tv);
        return Padded(EcmaTime.HourFromTime(tv), 2) + ":" + Padded(EcmaTime.MinFromTime(tv), 2) + ":" +
            Padded(EcmaTime.SecFromTime(tv), 2) + " GMT";
    }

    /// <summary>
    /// <c>DateString</c> (ECMA-262 21.4.4.41.2): the weekday, the month, the two-digit day and the year of
    /// a time, the year of at least four digits with a minus sign when it is negative.
    /// </summary>
    /// <param name="tv">A finite Number: a local time.</param>
    /// <returns>For example <c>"Thu Jan 01 1970"</c> or <c>"Fri Jan 01 -0001"</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tv"/> is not finite.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.2; IP=Low; Security=Medium; Resources=1; Fingerprint=A7C9F3
    // Broiler-Human:        PENDING
    public static string DateString(double tv)
    {
        CheckFinite(tv);
        return WeekDayName(tv) + " " + MonthName(tv) + " " + Padded(EcmaTime.DateFromTime(tv), 2) + " " + Year(tv);
    }

    /// <summary>
    /// <c>TimeZoneString</c> (ECMA-262 21.4.4.41.3): the sign, the two-digit hours and the two-digit minutes
    /// of the zone's offset at an instant, then <c>" ("</c>, the zone's name and <c>")"</c> when it has one.
    /// </summary>
    /// <param name="tv">A time value.</param>
    /// <param name="zone">The local time zone, which answers the offset and the name.</param>
    /// <returns>For example <c>"+0000 (Coordinated Universal Time)"</c> or <c>"-0500"</c>.</returns>
    /// <exception cref="ArgumentNullException">The zone is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tv"/> is not finite.</exception>
    /// <exception cref="InvalidOperationException">The zone answered an offset that breaks its contract.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.3; IP=Low; Security=Medium; Resources=1; Fingerprint=0CFC41
    // Broiler-Human:        PENDING
    public static string TimeZoneString(double tv, ILocalTimeProvider zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        CheckFinite(tv);

        var offset = EcmaLocalTime.OffsetFor(zone, tv);
        var sign = offset >= 0 ? "+" : "-";
        var absOffset = Math.Abs(offset);
        var name = zone.GetName(tv);
        var text = sign + Padded(EcmaTime.HourFromTime(absOffset), 2) + Padded(EcmaTime.MinFromTime(absOffset), 2);
        return name is null ? text : text + " (" + name + ")";
    }

    /// <summary>
    /// <c>ToDateString</c> (ECMA-262 21.4.4.41.4), the text of <c>toString</c>: <c>DateString</c>, a space,
    /// <c>TimeString</c> of the local time, and <c>TimeZoneString</c>; <c>"Invalid Date"</c> for NaN.
    /// </summary>
    /// <param name="tv">A time value or NaN.</param>
    /// <param name="zone">The local time zone.</param>
    /// <returns>For example <c>"Thu Jan 01 1970 00:00:00 GMT+0000 (Coordinated Universal Time)"</c>.</returns>
    /// <exception cref="ArgumentNullException">The zone is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The zone answered an offset that breaks its contract.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.4; IP=Low; Security=Medium; Resources=1; Fingerprint=2F0681
    // Broiler-Human:        PENDING
    public static string ToDateString(double tv, ILocalTimeProvider zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (double.IsNaN(tv))
            return InvalidDate;

        var t = EcmaLocalTime.LocalTime(tv, zone);
        return DateString(t) + " " + TimeString(t) + TimeZoneString(tv, zone);
    }

    /// <summary>
    /// The text of <c>Date.prototype.toUTCString</c> (ECMA-262 21.4.4.43): the weekday, a comma, the
    /// two-digit day, the month, the year and <c>TimeString</c>; <c>"Invalid Date"</c> for NaN.
    /// </summary>
    /// <param name="tv">A time value or NaN.</param>
    /// <returns>For example <c>"Thu, 01 Jan 1970 00:00:00 GMT"</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tv"/> is infinite.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.43; IP=Low; Security=Medium; Resources=1; Fingerprint=EA8D2E
    // Broiler-Human:        PENDING
    public static string ToUtcString(double tv)
    {
        if (double.IsNaN(tv))
            return InvalidDate;

        CheckFinite(tv);
        return WeekDayName(tv) + ", " + Padded(EcmaTime.DateFromTime(tv), 2) + " " + MonthName(tv) + " " + Year(tv) + " " + TimeString(tv);
    }

    /// <summary>
    /// The text of <c>Date.prototype.toISOString</c> (ECMA-262 21.4.4.36): the Date Time String Format
    /// (21.4.1.32) with every element and <c>Z</c>, the year as four digits from 0 to 9999 and as a sign and
    /// six digits otherwise (21.4.1.32.1).
    /// </summary>
    /// <param name="tv">A time value or NaN.</param>
    /// <param name="text">The text, for a finite <paramref name="tv"/>; otherwise the empty String.</param>
    /// <returns>Whether <paramref name="tv"/> is finite; the caller throws its <c>RangeError</c> when it is not.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.36; IP=Low; Security=Medium; Resources=1; Fingerprint=B91659
    // Broiler-Human:        PENDING
    public static bool TryToIsoString(double tv, out string text)
    {
        if (!double.IsFinite(tv))
        {
            text = string.Empty;
            return false;
        }

        var year = EcmaTime.YearFromTime(tv);
        var yearText = year is >= 0 and <= 9999
            ? Padded(year, 4)
            : (year < 0 ? "-" : "+") + Padded(Math.Abs(year), 6);
        text = yearText + "-" + Padded(EcmaTime.MonthFromTime(tv) + 1, 2) + "-" + Padded(EcmaTime.DateFromTime(tv), 2) +
            "T" + Padded(EcmaTime.HourFromTime(tv), 2) + ":" + Padded(EcmaTime.MinFromTime(tv), 2) + ":" +
            Padded(EcmaTime.SecFromTime(tv), 2) + "." + Padded(EcmaTime.MsFromTime(tv), 3) + "Z";
        return true;
    }

    // The year with its sign when negative, its magnitude at least four digits (DateString's yearSign and paddedYear).
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.2; IP=Low; Security=Low; Resources=1; Fingerprint=2E242D
    // Broiler-Human:        PENDING
    private static string Year(double tv)
    {
        var year = EcmaTime.YearFromTime(tv);
        return (year >= 0 ? string.Empty : "-") + Padded(Math.Abs(year), 4);
    }

    // ToZeroPaddedDecimalString (ECMA-262 22.1.3.17.3) of a non-negative integral Number.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.17.3; IP=Low; Security=Low; Resources=1; Fingerprint=73A402
    // Broiler-Human:        PENDING
    private static string Padded(double value, int width) =>
        ((long)value).ToString(NumberFormatInfo.InvariantInfo).PadLeft(width, '0');

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.2; IP=Low; Security=Low; Resources=1; Fingerprint=4FC0E2
    // Broiler-Human:        PENDING
    private static string WeekDayName(double tv) => (int)EcmaTime.WeekDay(tv) switch
    {
        0 => "Sun",
        1 => "Mon",
        2 => "Tue",
        3 => "Wed",
        4 => "Thu",
        5 => "Fri",
        _ => "Sat",
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.2; IP=Low; Security=Low; Resources=1; Fingerprint=CD7F0A
    // Broiler-Human:        PENDING
    private static string MonthName(double tv) => (int)EcmaTime.MonthFromTime(tv) switch
    {
        0 => "Jan",
        1 => "Feb",
        2 => "Mar",
        3 => "Apr",
        4 => "May",
        5 => "Jun",
        6 => "Jul",
        7 => "Aug",
        8 => "Sep",
        9 => "Oct",
        10 => "Nov",
        _ => "Dec",
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41; IP=Low; Security=Low; Resources=1; Fingerprint=8B955C
    // Broiler-Human:        PENDING
    private static void CheckFinite(double tv)
    {
        if (!double.IsFinite(tv))
            throw new ArgumentOutOfRangeException(nameof(tv), tv, "A rendering takes a finite Number.");
    }
}
