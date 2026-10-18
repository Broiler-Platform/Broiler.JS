// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           3
// Human-reviewed:   0/1
// IP risk:          None
// Security risk:    None
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The kind of value a set of calendar fields describes: the <c>type</c> argument of the Temporal proposal's
/// CalendarResolveFields (s12.3.31) and ISODateToFields (s13.42), which decides the fields each needs.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=Temporal s12.3.31; IP=None; Security=None; Resources=0; Fingerprint=E363D4
// Broiler-Human:        PENDING
public enum CalendarFieldsType
{
    /// <summary><c>date</c>: a year, a month and a day.</summary>
    Date = 0,

    /// <summary><c>year-month</c>: a year and a month.</summary>
    YearMonth = 1,

    /// <summary><c>month-day</c>: a month code and a day, or a year, a month and a day.</summary>
    MonthDay = 2,
}
