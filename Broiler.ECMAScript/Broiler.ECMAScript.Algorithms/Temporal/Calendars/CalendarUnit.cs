// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           4
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
/// The largest unit of a calendar's difference: the date units CalendarDateUntil (Temporal s12.3.9) takes, which
/// <see cref="ICalendarArithmetic.DateUntil"/> fills from the largest down.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=Temporal s12.3.9; IP=None; Security=None; Resources=0; Fingerprint=26ED71
// Broiler-Human:        PENDING
public enum CalendarUnit
{
    /// <summary>Years, then months, then days.</summary>
    Year = 0,

    /// <summary>Months, then days.</summary>
    Month = 1,

    /// <summary>Weeks, then days.</summary>
    Week = 2,

    /// <summary>Days alone.</summary>
    Day = 3,
}
