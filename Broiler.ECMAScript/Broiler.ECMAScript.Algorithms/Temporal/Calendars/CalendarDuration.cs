// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           0
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
/// The date part of a duration as a calendar adds it and answers it: the years, months, weeks and days of a
/// Date Duration Record (Temporal s7.5.1), exact integers. <see cref="ICalendarArithmetic.DateAdd"/> takes one
/// and <see cref="ICalendarArithmetic.DateUntil"/> answers one.
/// </summary>
/// <param name="Years">The years.</param>
/// <param name="Months">The months.</param>
/// <param name="Weeks">The weeks.</param>
/// <param name="Days">The days.</param>
/// <remarks>
/// A valid duration's date part always fits (IsValidDuration, s7.5.16: years, months and weeks below 2^32 in
/// magnitude, days below 2^53 seconds), so an engine converts each of its fields exactly.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s7.5.1; IP=None; Security=None; Resources=0; Fingerprint=65243C
// Broiler-Human:        PENDING
public readonly record struct CalendarDuration(long Years, long Months, long Weeks, long Days);
