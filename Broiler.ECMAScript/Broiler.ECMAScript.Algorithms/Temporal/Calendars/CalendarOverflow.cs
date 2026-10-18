// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           2
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
/// How a calendar operation treats a month or day beyond the calendar's: the value of the Temporal proposal's
/// <c>overflow</c> option (GetTemporalOverflowOption, s13.6), which the engine reads and hands in.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=Temporal s13.6; IP=None; Security=None; Resources=0; Fingerprint=6A0EE1
// Broiler-Human:        PENDING
public enum CalendarOverflow
{
    /// <summary><c>"constrain"</c>, the option's default: the month or day is clamped to the nearest the calendar has.</summary>
    Constrain = 0,

    /// <summary><c>"reject"</c>: a month or day beyond the calendar's is a status, which the engine throws as a <c>RangeError</c>.</summary>
    Reject = 1,
}
