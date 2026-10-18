// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           14
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
/// The outcome of a calendar operation (JSD-0078's X3: "an unknown identifier, an invalid month code or era,
/// and a date outside a calendar each answer a status"). Each engine maps a status other than
/// <see cref="Completed"/> to its own error at the specification's moment: a <c>TypeError</c> for the four
/// <see cref="TemporalCalendars.IsTypeError">missing-field statuses</see>, a <c>RangeError</c> for every other.
/// </summary>
/// <remarks>
/// A status names the specification's step that failed, not a message: messages stay each engine's (JSD-0054
/// section 5). The order in which an operation tests its inputs is Broiler.VM's, which is the specification's.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0078 X3; IP=None; Security=None; Resources=0; Fingerprint=3A5A88
// Broiler-Human:        PENDING
public enum CalendarStatus
{
    /// <summary>The operation completed, and its result is valid.</summary>
    Completed = 0,

    /// <summary>
    /// The identifier is no calendar type of Table 1 and neither of its two aliases (CanonicalizeCalendar,
    /// Temporal s12.1.1): a <c>RangeError</c>.
    /// </summary>
    UnknownCalendar = 1,

    /// <summary>
    /// The month code is not the grammar's <c>MonthCode</c> (ParseMonthCode, Temporal s12.2.1), or is not one of the
    /// calendar's (IsValidMonthCodeForCalendar, the era proposal's 4.1.4; ISO 8601's twelve): a <c>RangeError</c>.
    /// </summary>
    InvalidMonthCode = 2,

    /// <summary>No <c>year</c> was given where the fields need one, nor an era and era year that name it: a <c>TypeError</c>.</summary>
    MissingYear = 3,

    /// <summary>Neither <c>month</c> nor <c>monthCode</c> was given: a <c>TypeError</c>.</summary>
    MissingMonth = 4,

    /// <summary>No <c>day</c> was given where the fields need one: a <c>TypeError</c>.</summary>
    MissingDay = 5,

    /// <summary>An <c>era</c> was given without an <c>eraYear</c>, or one without the other: a <c>TypeError</c>.</summary>
    IncompleteEra = 6,

    /// <summary>
    /// The era is not one of the calendar's Table 2 codes or aliases, matched exactly (CanonicalizeEraInCalendar,
    /// the era proposal's 4.1.2): a <c>RangeError</c>.
    /// </summary>
    UnknownEra = 7,

    /// <summary>The <c>year</c> and the year the era and era year name disagree: a <c>RangeError</c>.</summary>
    EraYearMismatch = 8,

    /// <summary>The <c>month</c> and the month the <c>monthCode</c> names in that year disagree: a <c>RangeError</c>.</summary>
    MonthCodeMismatch = 9,

    /// <summary>
    /// Under <see cref="CalendarOverflow.Reject"/>, the year does not have the leap month the code names
    /// (ConstrainMonthCode, the era proposal's 4.1.6): a <c>RangeError</c>.
    /// </summary>
    MonthNotInYear = 10,

    /// <summary>
    /// Under <see cref="CalendarOverflow.Reject"/>, the month or the day is beyond the calendar's for that year
    /// (RegulateISODate, Temporal s3.5.7, and the era proposal's regulation): a <c>RangeError</c>.
    /// </summary>
    FieldOutOfRange = 11,

    /// <summary>
    /// A month-day that no reference year holds: a month code and day Table 6 marks with a dash under
    /// <see cref="CalendarOverflow.Reject"/>, or one no year from 1900 to 2035 has (NonISOMonthDayToISOReferenceDate,
    /// the era proposal's 4.1.21): a <c>RangeError</c>.
    /// </summary>
    NoReferenceDate = 12,

    /// <summary>
    /// The result, or a year the operation would compute, is outside the Temporal proposal's limits
    /// (ISODateWithinLimits, ISOYearMonthWithinLimits), or the input is outside the years every calendar is
    /// computed over: a <c>RangeError</c>.
    /// </summary>
    OutOfRange = 13,
}
