// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           6
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
/// The calendar half of a Calendar Fields Record (Temporal s12.3.2, Table 19): <c>era</c>, <c>eraYear</c>,
/// <c>year</c>, <c>month</c>, <c>monthCode</c> and <c>day</c>, each a value or unset (<see langword="null"/>).
/// The engine fills it from a property bag with PrepareCalendarFields (s12.3.3), reading and converting each
/// property in the specification's order; the library reads no property (convention 2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The values arrive converted</b>, as Table 19's conversions leave them: <see cref="Era"/> a string
/// (<c>ToString</c>); <see cref="EraYear"/> and <see cref="Year"/> integral and finite, of any magnitude
/// (<c>ToIntegerWithTruncation</c>); <see cref="Month"/> and <see cref="Day"/> integral, finite and at least 1
/// (<c>ToPositiveIntegerWithTruncation</c>); and <see cref="MonthCode"/> parsed (<see cref="Temporal.MonthCode.Parse"/>).
/// A value outside these is a broken precondition. A field beyond 32 bits is held exactly: no narrowing reaches
/// a calendar field (the correspondence's row C11).
/// </para>
/// <para>
/// <b>The time, offset and time zone fields stay the engine's</b>: no calendar operation reads them, and
/// CalendarMergeFields lets each of them that the additional fields hold override, which the engine does itself.
/// The engine reads <see cref="Era"/> and <see cref="EraYear"/> only where <see cref="TemporalCalendars.SupportsEra"/>
/// is true and <c>year</c> is among the fields read (CalendarExtraFields, s12.3.27); a calendar without eras
/// ignores them.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.2 via Broiler.VM JsCalendarFields; IP=None; Security=None; Resources=0; Fingerprint=B310C4
// Broiler-Human:        PENDING
public readonly record struct CalendarFields
{
    /// <summary>The <c>era</c> field: an era code or alias of Table 2, matched exactly; or unset.</summary>
    public string? Era { get; init; }

    /// <summary>The <c>eraYear</c> field: the year within <see cref="Era"/>; or unset.</summary>
    public double? EraYear { get; init; }

    /// <summary>The <c>year</c> field: the calendar's arithmetic year; or unset.</summary>
    public double? Year { get; init; }

    /// <summary>The <c>month</c> field: the ordinal month, from 1; or unset.</summary>
    public double? Month { get; init; }

    /// <summary>The <c>monthCode</c> field; or unset.</summary>
    public MonthCode? MonthCode { get; init; }

    /// <summary>The <c>day</c> field: the day of the month, from 1; or unset.</summary>
    public double? Day { get; init; }
}
