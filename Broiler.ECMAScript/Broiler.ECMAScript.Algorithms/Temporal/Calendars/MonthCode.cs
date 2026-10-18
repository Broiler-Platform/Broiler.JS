// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           2
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  1/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// A month code: the Temporal proposal's <c>MonthCode</c> production, an <c>M</c>, two ASCII digits and an
/// optional <c>L</c> for a leap month, as ParseMonthCode (s12.2.1) reads it and CreateMonthCode (s12.2.2)
/// writes it. Whether a calendar has the month is a separate question: Table 3 of the Intl era and month code
/// proposal answers it, through the operations of <see cref="TemporalCalendars"/>.
/// </summary>
/// <remarks>
/// <c>M00</c> is not a month code; <c>M00L</c> is, though no calendar has it, as the grammar says
/// (<c>MonthCode ::: M00L | M0 NonZeroDigit L? | M1 DecimalDigit L? | ...</c>).
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s12.2.1 and s12.2.2; IP=None; Security=Low; Resources=0; Fingerprint=DF4014
// Broiler-Human:        PENDING
public readonly record struct MonthCode
{
    /// <summary>Creates a month code.</summary>
    /// <param name="number">The month's number, from 0 to 99; 0 only with <paramref name="isLeapMonth"/>.</param>
    /// <param name="isLeapMonth">Whether the code names the leap month after month <paramref name="number"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The number is outside 0 to 99, or is 0 for a common month.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal s12.2.2; IP=Low; Security=Low; Resources=0; Fingerprint=C0D577
    // Broiler-Human:        PENDING
    public MonthCode(int number, bool isLeapMonth)
    {
        if (number is < 0 or > 99 || (number == 0 && !isLeapMonth))
            throw new ArgumentOutOfRangeException(nameof(number), number, "A month code's number is from 1 to 99, or 0 for M00L.");

        Number = number;
        IsLeapMonth = isLeapMonth;
    }

    /// <summary>The month's number: the code's two digits.</summary>
    public int Number { get; }

    /// <summary>Whether the code ends in <c>L</c>: the leap month after month <see cref="Number"/>.</summary>
    public bool IsLeapMonth { get; }

    /// <summary>
    /// ParseMonthCode (Temporal s12.2.1) after the engine's <c>ToPrimitive</c> and its check that the value is a
    /// string: whether the text is a <c>MonthCode</c>. Its digits are ASCII; no other decimal digit is one
    /// (the correspondence's row C6).
    /// </summary>
    /// <param name="text">The string the property held.</param>
    /// <param name="monthCode">The month code; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, or <see cref="CalendarStatus.InvalidMonthCode"/>, the specification's <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.2.1 via Broiler.VM JsTemporal.MonthCodeOf; IP=Low; Security=High; Resources=0; Fingerprint=6D1E6C
    // Broiler-Falsified-If: a string other than M, two ASCII digits and an optional L, or the string M00, parses as a month code
    // Broiler-Human:        PENDING
    public static CalendarStatus Parse(ReadOnlySpan<char> text, out MonthCode monthCode)
    {
        monthCode = default;
        if (text.Length is not (3 or 4) || text[0] != 'M' || !IsAsciiDigit(text[1]) || !IsAsciiDigit(text[2]))
            return CalendarStatus.InvalidMonthCode;

        var leap = text.Length == 4;
        if (leap && text[3] != 'L')
            return CalendarStatus.InvalidMonthCode;

        var number = ((text[1] - '0') * 10) + (text[2] - '0');
        if (number == 0 && !leap)
            return CalendarStatus.InvalidMonthCode;

        monthCode = new MonthCode(number, leap);
        return CalendarStatus.Completed;
    }

    /// <summary>CreateMonthCode (Temporal s12.2.2): the code as a string, <c>M01</c> to <c>M99</c> with an <c>L</c> for a leap month.</summary>
    /// <returns>The code, three or four ASCII characters.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.2.2 via Broiler.VM JsCalendarSystem.Code; IP=Low; Security=Low; Resources=1; Fingerprint=075956
    // Broiler-Human:        PENDING
    public override string ToString()
    {
        Span<char> text = stackalloc char[4];
        text[0] = 'M';
        text[1] = (char)('0' + (Number / 10));
        text[2] = (char)('0' + (Number % 10));
        text[3] = 'L';
        return new string(text[..(IsLeapMonth ? 4 : 3)]);
    }

    // Broiler-AI:           Origin=AI; Spec=Temporal s12.2.1; IP=None; Security=Low; Resources=0; Fingerprint=3F788E
    // Broiler-Human:        PENDING
    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';
}
