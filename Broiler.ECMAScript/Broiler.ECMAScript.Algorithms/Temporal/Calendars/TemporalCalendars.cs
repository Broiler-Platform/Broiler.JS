// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   58
// Annotated:        58/58
// Exempt:           14
// Human-reviewed:   0/58
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       58
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The calendars of the Temporal proposal at <c>e8cc03fc</c> and the Intl era and month code proposal at
/// <c>5833eae</c> (JSD-0078, decision D1): the identifiers of Table 1, the formula calendars (the Gregorian family,
/// Coptic and Ethiopian, Indian, the tabular Hijri calendars and Hebrew), the table calendars over SJL-8's
/// <see cref="CalendarYears"/> (Chinese, Korean, Umm al-Qura and Persian), the month codes of Table 3, the eras of
/// Table 2 and the reference years of Table 6, with the calendar operations of Temporal s12.3 and the era
/// proposal's section 4.1 over them. Ported from Broiler.VM's <c>JsCalendars</c>, the core of
/// <c>JsTemporal.Calendar</c> and <c>JsTemporal.CalendarNonIso</c>, with their behaviour unchanged.
/// </summary>
/// <remarks>
/// <para>
/// <b>An instance holds the calendar systems over the tables an engine hands in</b>, as
/// <see cref="TimeZoneDatabase"/> holds the zones: the table calendars read their years from them, and the
/// library embeds no table (SJL-D-6 (c)). The identifiers, month codes and field merging need no table and are
/// static. Every instance is immutable, and every member is safe to call concurrently.
/// </para>
/// <para>
/// <b>Failure is data</b> (JSD-0078's X3): an unknown identifier, an invalid month code or era, a missing field
/// and a date outside a calendar or the limits each answer a <see cref="CalendarStatus"/>, in the order Broiler.VM
/// tests them, which is the specification's. An exception is a broken precondition, which each member lists.
/// </para>
/// <para>
/// <b>What stays in the engines</b>: the Temporal objects and their <c>[[Calendar]]</c> slots
/// (ToTemporalCalendarIdentifier, s12.3.10, answers the slot: the correspondence's row C13), the property reads
/// and conversions of PrepareCalendarFields (s12.3.3), the <c>overflow</c> option's read, ParseTemporalCalendarString,
/// which is the grammar's, and the mapping of each status to a <c>TypeError</c> or a <c>RangeError</c>.
/// </para>
/// <para>
/// <b>Charges</b>: none. Each member's work is bounded by the limits: a year is a few multiplications or a table
/// lookup, a difference's year and month counts start from estimates, and a reference date searches the years 1900
/// to 2035 at most (X3: there is no class C2).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=Temporal s12.3, Intl era and month code proposal 4.1 via Broiler.VM JsTemporal.Calendar and JsTemporal.CalendarNonIso; IP=Low; Security=Medium; Resources=2; Fingerprint=8E9312
// Broiler-Human:        PENDING
public sealed class TemporalCalendars : ICalendarArithmetic
{
    // The years and months a calendar adds are refused past these before any arithmetic, as Broiler.VM refuses
    // them: no result within the limits is that far from a date within them.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=None; Resources=0; Fingerprint=A539FC
    // Broiler-Human:        PENDING
    private const double MaxAddedYears = 1e9;
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=None; Resources=0; Fingerprint=B4E439
    // Broiler-Human:        PENDING
    private const double MaxAddedMonths = 1e10;
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=None; Resources=0; Fingerprint=095AA8
    // Broiler-Human:        PENDING
    private const double MaxAddedWeeks = 1e10;
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=None; Resources=0; Fingerprint=313E22
    // Broiler-Human:        PENDING
    private const double MaxAddedDays = 1e11;

    // A year from fields further than this from the epoch is refused before ISO 8601 regulates it.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.22 via Broiler.VM JsTemporal.DateToIso; IP=Low; Security=None; Resources=0; Fingerprint=188029
    // Broiler-Human:        PENDING
    private const double MaxIsoFieldYear = 1e9;

    // The reference year of an ISO 8601 month-day (CalendarMonthDayToISOReferenceDate, s12.3.24).
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.24 via Broiler.VM JsTemporal.MonthDayFromFields; IP=Low; Security=None; Resources=0; Fingerprint=63C0A3
    // Broiler-Human:        PENDING
    private const long IsoReferenceYear = 1972;

    // Every calendar but ISO 8601, by CalendarId; the first element is unused.
    private readonly CalendarSystem?[] systems;

    /// <summary>Creates the calendars over the calendar tables an engine hands in.</summary>
    /// <param name="tables">The calendar tables (SJL-8's <see cref="CalendarTables"/> over the data home's bytes).</param>
    /// <exception cref="ArgumentNullException">The tables are <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 via Broiler.VM JsCalendars..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=F850C7
    // Broiler-Human:        PENDING
    public TemporalCalendars(ICalendarTables tables)
    {
        ArgumentNullException.ThrowIfNull(tables);

        // THE SYSTEMS OF JsCalendars, with its epochs: Calendrical Calculations' R.D. of each reckoning's first day.
        systems = new CalendarSystem?[16];
        systems[(int)CalendarId.Buddhist] = new GregorianCalendarSystem(CalendarId.Buddhist, -543);
        systems[(int)CalendarId.Chinese] = new EastAsianCalendarSystem(CalendarId.Chinese, tables);
        systems[(int)CalendarId.Coptic] = new CopticCalendarSystem(CalendarId.Coptic, 103605, 0);
        systems[(int)CalendarId.Dangi] = new EastAsianCalendarSystem(CalendarId.Dangi, tables);
        systems[(int)CalendarId.Ethioaa] = new CopticCalendarSystem(CalendarId.Ethioaa, 2796, 5500);
        systems[(int)CalendarId.Ethiopic] = new CopticCalendarSystem(CalendarId.Ethiopic, 2796, 0);
        systems[(int)CalendarId.Gregory] = new GregorianCalendarSystem(CalendarId.Gregory, 0);
        systems[(int)CalendarId.Hebrew] = new HebrewCalendarSystem();
        systems[(int)CalendarId.Indian] = new IndianCalendarSystem();
        systems[(int)CalendarId.IslamicCivil] = new HijriCalendarSystem(CalendarId.IslamicCivil, thursday: false);
        systems[(int)CalendarId.IslamicTbla] = new HijriCalendarSystem(CalendarId.IslamicTbla, thursday: true);
        systems[(int)CalendarId.IslamicUmalqura] = new HijriCalendarSystem(CalendarId.IslamicUmalqura, thursday: false, tables);
        systems[(int)CalendarId.Japanese] = new GregorianCalendarSystem(CalendarId.Japanese, 0);
        systems[(int)CalendarId.Persian] = new PersianCalendarSystem(tables);
        systems[(int)CalendarId.Roc] = new GregorianCalendarSystem(CalendarId.Roc, 1911);
    }

    // ---- Identifiers -------------------------------------------------------------------------------------

    /// <summary>
    /// CanonicalizeCalendar (Temporal s12.1.1) over AvailableCalendars as the era proposal's 1.1.1 amends it: the
    /// identifier matched against Table 1's calendar types and its two aliases, <c>ethiopic-amete-alem</c> and
    /// <c>islamicc</c>, under ASCII case folding alone; no other character folds (the correspondence's rows C1 to
    /// C3). CLDR's legacy aliases, <c>gregorian</c> and <c>minguo</c>, are not read in (JSD-0078, decision D5).
    /// </summary>
    /// <param name="identifier">The identifier: a constructor's argument, or the value of a <c>u-ca</c> annotation the grammar read.</param>
    /// <param name="calendar">The calendar; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, or <see cref="CalendarStatus.UnknownCalendar"/>, the specification's <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.1.1, Intl era and month code proposal 1.1.1 via Broiler.VM JsTemporal.CanonicalizeCalendar; IP=Low; Security=High; Resources=1; Fingerprint=E8A556
    // Broiler-Falsified-If: an identifier that differs from a Table 1 type or alias in anything but the case of ASCII letters canonicalises to a calendar
    // Broiler-Human:        PENDING
    public static CalendarStatus Canonicalize(ReadOnlySpan<char> identifier, out CalendarId calendar)
    {
        calendar = default;
        const int Longest = 19; // ethiopic-amete-alem
        if (identifier.Length > Longest)
            return CalendarStatus.UnknownCalendar;

        // A BUFFER OF THE LONGEST IDENTIFIER'S LENGTH, whatever the input's.
        Span<char> lower = stackalloc char[Longest];
        lower = lower[..identifier.Length];
        for (var i = 0; i < identifier.Length; i++)
            lower[i] = identifier[i] is >= 'A' and <= 'Z' ? (char)(identifier[i] + 32) : identifier[i];

        for (var id = CalendarId.Iso8601; id <= CalendarId.Roc; id++)
        {
            if (lower.SequenceEqual(Identifier(id)))
            {
                calendar = id;
                return CalendarStatus.Completed;
            }
        }

        if (lower.SequenceEqual("ethiopic-amete-alem"))
        {
            calendar = CalendarId.Ethioaa;
            return CalendarStatus.Completed;
        }

        if (lower.SequenceEqual("islamicc"))
        {
            calendar = CalendarId.IslamicCivil;
            return CalendarStatus.Completed;
        }

        return CalendarStatus.UnknownCalendar;
    }

    /// <summary>A calendar's canonical identifier: Table 1's calendar type, the value of <c>calendarId</c>.</summary>
    /// <param name="calendar">The calendar.</param>
    /// <returns>The identifier, in lower case.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a member of <see cref="CalendarId"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 via Broiler.VM JsTemporal.AvailableCalendars; IP=Low; Security=Low; Resources=0; Fingerprint=32FD9D
    // Broiler-Human:        PENDING
    public static string Identifier(CalendarId calendar) => calendar switch
    {
        CalendarId.Iso8601 => "iso8601",
        CalendarId.Buddhist => "buddhist",
        CalendarId.Chinese => "chinese",
        CalendarId.Coptic => "coptic",
        CalendarId.Dangi => "dangi",
        CalendarId.Ethioaa => "ethioaa",
        CalendarId.Ethiopic => "ethiopic",
        CalendarId.Gregory => "gregory",
        CalendarId.Hebrew => "hebrew",
        CalendarId.Indian => "indian",
        CalendarId.IslamicCivil => "islamic-civil",
        CalendarId.IslamicTbla => "islamic-tbla",
        CalendarId.IslamicUmalqura => "islamic-umalqura",
        CalendarId.Japanese => "japanese",
        CalendarId.Persian => "persian",
        CalendarId.Roc => "roc",
        _ => throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "The calendar is not one of Table 1's."),
    };

    /// <summary>
    /// CalendarSupportsEra (the era proposal's 4.1.1): whether the calendar has eras, and so whether CalendarExtraFields
    /// (Temporal s12.3.27) adds <c>era</c> and <c>eraYear</c> to the fields an engine reads with <c>year</c>.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <returns><see langword="false"/> for <c>iso8601</c>, <c>chinese</c> and <c>dangi</c>; <see langword="true"/> for every other.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a member of <see cref="CalendarId"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.1 via Broiler.VM JsTemporal.SupportsEra; IP=Low; Security=Low; Resources=0; Fingerprint=C43249
    // Broiler-Human:        PENDING
    public static bool SupportsEra(CalendarId calendar)
    {
        CheckCalendar(calendar);
        return calendar is not (CalendarId.Iso8601 or CalendarId.Chinese or CalendarId.Dangi);
    }

    /// <summary>
    /// Whether the specification's error for a status is a <c>TypeError</c>: a field the operation needs is missing
    /// (CalendarResolveFields, s12.3.31, and NonISOResolveFields, the era proposal's 4.1.24). Every other status but
    /// <see cref="CalendarStatus.Completed"/> is a <c>RangeError</c>.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <returns>Whether it is <see cref="CalendarStatus.MissingYear"/>, <see cref="CalendarStatus.MissingMonth"/>, <see cref="CalendarStatus.MissingDay"/> or <see cref="CalendarStatus.IncompleteEra"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.31, Intl era and month code proposal 4.1.24 via Broiler.VM JsTemporal.ResolveFields; IP=Low; Security=Low; Resources=0; Fingerprint=29E788
    // Broiler-Human:        PENDING
    public static bool IsTypeError(CalendarStatus status) =>
        status is CalendarStatus.MissingYear or CalendarStatus.MissingMonth or CalendarStatus.MissingDay or CalendarStatus.IncompleteEra;

    // ---- Fields ------------------------------------------------------------------------------------------

    /// <summary>
    /// CalendarMergeFields (Temporal s12.3.5) over the calendar fields, with CalendarFieldKeysToIgnore (s12.3.29) and
    /// NonISOFieldKeysToIgnore (the era proposal's 4.1.23): each field the additional fields hold overrides, and so do
    /// the fields it entails. A <c>month</c> drops <c>monthCode</c> and the reverse; in a calendar with eras, any of
    /// <c>era</c>, <c>eraYear</c> and <c>year</c> drops the other two; and in <c>japanese</c>, whose eras begin within a
    /// year, a <c>day</c>, <c>month</c> or <c>monthCode</c> drops <c>era</c> and <c>eraYear</c>.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="fields">The fields of the value <c>with</c> or <c>toPlainDate</c> starts from (<see cref="DateToFields"/>).</param>
    /// <param name="additional">The fields the engine read from the argument.</param>
    /// <returns>The merged fields. The time, offset and time zone fields, which the calendar does not hold, the engine merges itself: each one the additional fields hold overrides.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a member of <see cref="CalendarId"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.5 and s12.3.29, Intl era and month code proposal 4.1.23 via Broiler.VM JsTemporal.MergeFields; IP=Low; Security=Medium; Resources=1; Fingerprint=876259
    // Broiler-Human:        PENDING
    public static CalendarFields MergeFields(CalendarId calendar, CalendarFields fields, CalendarFields additional)
    {
        var eras = SupportsEra(calendar);
        var ignored = FieldKeys.None;

        void Ignore(FieldKeys key)
        {
            ignored |= key;
            if (key == FieldKeys.Month)
                ignored |= FieldKeys.MonthCode;
            else if (key == FieldKeys.MonthCode)
                ignored |= FieldKeys.Month;

            if (eras && key is FieldKeys.Era or FieldKeys.EraYear or FieldKeys.Year)
                ignored |= FieldKeys.Era | FieldKeys.EraYear | FieldKeys.Year;

            if (calendar == CalendarId.Japanese && key is FieldKeys.Day or FieldKeys.Month or FieldKeys.MonthCode)
                ignored |= FieldKeys.Era | FieldKeys.EraYear;
        }

        if (additional.Era is not null)
            Ignore(FieldKeys.Era);
        if (additional.EraYear is not null)
            Ignore(FieldKeys.EraYear);
        if (additional.Year is not null)
            Ignore(FieldKeys.Year);
        if (additional.Month is not null)
            Ignore(FieldKeys.Month);
        if (additional.MonthCode is not null)
            Ignore(FieldKeys.MonthCode);
        if (additional.Day is not null)
            Ignore(FieldKeys.Day);

        return new CalendarFields
        {
            Era = additional.Era ?? ((ignored & FieldKeys.Era) == 0 ? fields.Era : null),
            EraYear = additional.EraYear ?? ((ignored & FieldKeys.EraYear) == 0 ? fields.EraYear : null),
            Year = additional.Year ?? ((ignored & FieldKeys.Year) == 0 ? fields.Year : null),
            Month = additional.Month ?? ((ignored & FieldKeys.Month) == 0 ? fields.Month : null),
            MonthCode = additional.MonthCode ?? ((ignored & FieldKeys.MonthCode) == 0 ? fields.MonthCode : null),
            Day = additional.Day ?? ((ignored & FieldKeys.Day) == 0 ? fields.Day : null),
        };
    }

    /// <summary>
    /// CalendarISOToDate (Temporal s12.3.26), and in a calendar other than ISO 8601 NonISOCalendarISOToDate (s12.3.25,
    /// the era proposal's 4.1.14): the calendar's year, month, month code, day, era and era year of an ISO date, and
    /// the lengths and leap year of its month and year. Every date-bearing type's getters read it.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="date">The date: any ISO date of a Temporal value, a year-month's reference date included.</param>
    /// <param name="result">The calendar's date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, or <see cref="CalendarStatus.OutOfRange"/> for a date whose year is outside -271821 to 275760.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a member of <see cref="CalendarId"/>, or the month and day are not an ISO date's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.26, Intl era and month code proposal 4.1.14 via Broiler.VM JsTemporal.IsoToDate; IP=Low; Security=Medium; Resources=1; Fingerprint=38C081
    // Broiler-Human:        PENDING
    public CalendarStatus IsoToDate(CalendarId calendar, IsoDate date, out CalendarDate result)
    {
        CheckCalendar(calendar);
        result = default;
        if (!CalendarLimits.IsComputable(date))
            return CalendarStatus.OutOfRange;

        if (calendar == CalendarId.Iso8601)
        {
            var (week, weekYear) = IsoCalendarMath.WeekOfYear(date.Year, date.Month, date.Day);
            result = new CalendarDate(
                null,
                null,
                date.Year,
                date.Month,
                new MonthCode(date.Month, false),
                date.Day,
                IsoCalendarMath.DayOfWeek(date.Year, date.Month, date.Day),
                IsoCalendarMath.DayOfYear(date.Year, date.Month, date.Day),
                week,
                weekYear,
                IsoCalendarMath.DaysInMonth(date.Year, date.Month),
                IsoCalendarMath.DaysInYear(date.Year),
                12,
                IsoCalendarMath.IsLeapYear(date.Year));
            return CalendarStatus.Completed;
        }

        var system = SystemOf(calendar);
        var days = IsoCalendarMath.EpochDays(date.Year, date.Month, date.Day);
        var (year, month, day) = system.FromEpochDays(days);
        var (era, eraYear) = system.EraOf(year.Year, days);
        result = new CalendarDate(
            era,
            eraYear,
            year.Year,
            month,
            CalendarSystem.MonthCodeOf(year, month),
            day,
            IsoCalendarMath.DayOfWeek(date.Year, date.Month, date.Day),
            (int)(days - year.Start) + 1,
            null,
            null,
            year.DaysInMonth(month),
            year.Days,
            year.Months,
            system.InLeapYear(year));
        return CalendarStatus.Completed;
    }

    /// <summary>
    /// ISODateToFields (Temporal s13.42): the calendar fields of an ISO date that <c>with</c>, <c>toPlainDate</c> and the
    /// conversions start from: its <c>monthCode</c>, with its <c>day</c> for a date or month-day and its <c>year</c> for a
    /// date or year-month.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="date">The ISO date.</param>
    /// <param name="type">The kind of value the fields describe.</param>
    /// <param name="result">The fields; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, or <see cref="CalendarStatus.OutOfRange"/> as <see cref="IsoToDate"/> answers it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="IsoToDate"/>, or the type is not a member of <see cref="CalendarFieldsType"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.42 via Broiler.VM JsTemporal.DateToFields; IP=Low; Security=Medium; Resources=1; Fingerprint=DB6ED4
    // Broiler-Human:        PENDING
    public CalendarStatus DateToFields(CalendarId calendar, IsoDate date, CalendarFieldsType type, out CalendarFields result)
    {
        CheckType(type);
        result = default;
        var status = IsoToDate(calendar, date, out var parts);
        if (status != CalendarStatus.Completed)
            return status;

        result = new CalendarFields
        {
            MonthCode = parts.MonthCode,
            Day = type is CalendarFieldsType.MonthDay or CalendarFieldsType.Date ? parts.Day : null,
            Year = type is CalendarFieldsType.YearMonth or CalendarFieldsType.Date ? parts.Year : null,
        };
        return CalendarStatus.Completed;
    }

    /// <summary>
    /// CalendarDateFromFields (Temporal s12.3.12): CalendarResolveFields (s12.3.31) or NonISOResolveFields (the era
    /// proposal's 4.1.24), then CalendarDateToISO (s12.3.22) or NonISOCalendarDateToISO (4.1.20), then
    /// ISODateWithinLimits. Under <see cref="CalendarOverflow.Constrain"/> a month or day beyond the calendar's is
    /// clamped and a leap month the year lacks is constrained (ConstrainMonthCode, 4.1.6); under
    /// <see cref="CalendarOverflow.Reject"/> each is a status.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="fields">The fields PrepareCalendarFields read, converted as <see cref="CalendarFields"/> says.</param>
    /// <param name="overflow">The <c>overflow</c> option.</param>
    /// <param name="result">The ISO date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="CalendarStatus.Completed"/>; a <c>TypeError</c> status for a missing field (<see cref="IsTypeError"/>);
    /// or <see cref="CalendarStatus.InvalidMonthCode"/>, <see cref="CalendarStatus.UnknownEra"/>,
    /// <see cref="CalendarStatus.EraYearMismatch"/>, <see cref="CalendarStatus.MonthCodeMismatch"/>,
    /// <see cref="CalendarStatus.MonthNotInYear"/>, <see cref="CalendarStatus.FieldOutOfRange"/> or
    /// <see cref="CalendarStatus.OutOfRange"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar or overflow is not a member of its enumeration, or a field is outside what its conversion gives.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.12 via Broiler.VM JsTemporal.DateFromFields; IP=Low; Security=Medium; Resources=1; Fingerprint=57EC97
    // Broiler-Human:        PENDING
    public CalendarStatus DateFromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow, out IsoDate result)
    {
        CheckCalendar(calendar);
        var reject = IsReject(overflow);
        var working = new Fields(fields);
        result = default;

        var status = ResolveFields(calendar, ref working, CalendarFieldsType.Date);
        if (status == CalendarStatus.Completed)
            status = calendar == CalendarId.Iso8601 ? IsoDateToIso(working, reject, out result) : NonIsoDateToIso(SystemOf(calendar), working, reject, out result);

        if (status == CalendarStatus.Completed && !CalendarLimits.DateWithinLimits(IsoCalendarMath.EpochDays(result.Year, result.Month, result.Day)))
            status = CalendarStatus.OutOfRange;

        if (status != CalendarStatus.Completed)
            result = default;

        return status;
    }

    /// <summary>
    /// CalendarYearMonthFromFields (Temporal s12.3.13): the fields' day set to 1, then resolved and converted as
    /// <see cref="DateFromFields"/> does, then ISOYearMonthWithinLimits. The result is the first day of the calendar
    /// month, the year-month's reference date.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="fields">The fields PrepareCalendarFields read; their day is not read.</param>
    /// <param name="overflow">The <c>overflow</c> option.</param>
    /// <param name="result">The reference date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns>As <see cref="DateFromFields"/>, with <see cref="CalendarStatus.OutOfRange"/> outside the year-month limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="DateFromFields"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.13 via Broiler.VM JsTemporal.YearMonthFromFields; IP=Low; Security=Medium; Resources=1; Fingerprint=6C5899
    // Broiler-Human:        PENDING
    public CalendarStatus YearMonthFromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow, out IsoDate result)
    {
        CheckCalendar(calendar);
        var reject = IsReject(overflow);
        var working = new Fields(fields with { Day = 1 });
        result = default;

        var status = ResolveFields(calendar, ref working, CalendarFieldsType.YearMonth);
        if (status == CalendarStatus.Completed)
            status = calendar == CalendarId.Iso8601 ? IsoDateToIso(working, reject, out result) : NonIsoDateToIso(SystemOf(calendar), working, reject, out result);

        if (status == CalendarStatus.Completed && !CalendarLimits.YearMonthWithinLimits(result))
            status = CalendarStatus.OutOfRange;

        if (status != CalendarStatus.Completed)
            result = default;

        return status;
    }

    /// <summary>
    /// CalendarMonthDayFromFields (Temporal s12.3.14): the fields resolved, then the month-day's reference date. In
    /// ISO 8601 it is CalendarMonthDayToISOReferenceDate (s12.3.24): the month and day regulated against the year
    /// given, or 1972, and the year 1972; a year further than 10^9 from the epoch is first reduced modulo 400, which
    /// keeps its leap year (the correspondence's row C12). In another calendar it is NonISOMonthDayToISOReferenceDate
    /// (the era proposal's 4.1.21): Table 6's year for <c>chinese</c> and <c>dangi</c>, and else the latest ISO date
    /// from 1900 to 1972 with the month code and day, or the earliest from 1973 to 2035 (row C10).
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="fields">The fields PrepareCalendarFields read: a month code or a month with a year, and a day.</param>
    /// <param name="overflow">The <c>overflow</c> option.</param>
    /// <param name="result">The reference date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns>As <see cref="DateFromFields"/>, and <see cref="CalendarStatus.NoReferenceDate"/> for a month-day no reference year holds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="DateFromFields"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.14 and s12.3.24 via Broiler.VM JsTemporal.MonthDayFromFields; IP=Low; Security=Medium; Resources=2; Fingerprint=F2A9AF
    // Broiler-Human:        PENDING
    public CalendarStatus MonthDayFromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow, out IsoDate result)
    {
        CheckCalendar(calendar);
        var reject = IsReject(overflow);
        var working = new Fields(fields);
        result = default;

        var status = ResolveFields(calendar, ref working, CalendarFieldsType.MonthDay);
        if (status != CalendarStatus.Completed)
            return status;

        if (calendar != CalendarId.Iso8601)
            return NonIsoMonthDayToReferenceDate(SystemOf(calendar), working, reject, out result);

        // A YEAR PAST 1E9 IS REDUCED TO ITS REMAINDER MODULO 400, which is exact in floating point and keeps the
        // year's leap year, so its February's length.
        var year = working.Year ?? IsoReferenceYear;
        if (Math.Abs(year) > MaxIsoFieldYear)
            year %= 400;

        status = RegulateIsoDate(year, working.Month!.Value, working.Day!.Value, reject, out var regulated);
        if (status == CalendarStatus.Completed)
            result = new IsoDate(IsoReferenceYear, regulated.Month, regulated.Day);

        return status;
    }

    // ---- Arithmetic --------------------------------------------------------------------------------------

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">The calendar or overflow is not a member of its enumeration, or the month and day are not an ISO date's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=Medium; Resources=1; Fingerprint=C62260
    // Broiler-Human:        PENDING
    public CalendarStatus DateAdd(CalendarId calendar, IsoDate date, CalendarDuration duration, CalendarOverflow overflow, out IsoDate result)
    {
        CheckCalendar(calendar);
        var reject = IsReject(overflow);
        result = default;
        if (!CalendarLimits.IsComputable(date) ||
            Math.Abs((double)duration.Years) > MaxAddedYears || Math.Abs((double)duration.Months) > MaxAddedMonths ||
            Math.Abs((double)duration.Weeks) > MaxAddedWeeks || Math.Abs((double)duration.Days) > MaxAddedDays)
        {
            return CalendarStatus.OutOfRange;
        }

        var status = calendar == CalendarId.Iso8601
            ? IsoDateAdd(date, duration, reject, out result)
            : NonIsoDateAdd(SystemOf(calendar), date, duration, reject, out result);
        if (status != CalendarStatus.Completed)
            result = default;

        return status;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">The calendar or largest unit is not a member of its enumeration, or a month and day are not an ISO date's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.9 via Broiler.VM JsTemporal.DateUntil; IP=Low; Security=Medium; Resources=2; Fingerprint=7898DD
    // Broiler-Human:        PENDING
    public CalendarStatus DateUntil(CalendarId calendar, IsoDate one, IsoDate two, CalendarUnit largestUnit, out CalendarDuration result)
    {
        CheckCalendar(calendar);
        if (largestUnit is < CalendarUnit.Year or > CalendarUnit.Day)
            throw new ArgumentOutOfRangeException(nameof(largestUnit), largestUnit, "The largest unit is not a date unit.");

        result = default;
        if (!CalendarLimits.IsComputable(one) || !CalendarLimits.IsComputable(two))
            return CalendarStatus.OutOfRange;

        var sign = -CalendarLimits.Compare(one, two);
        if (sign == 0)
            return CalendarStatus.Completed;

        result = calendar == CalendarId.Iso8601
            ? IsoDateUntil(one, two, largestUnit, sign)
            : NonIsoDateUntil(SystemOf(calendar), one, two, largestUnit, sign);
        return CalendarStatus.Completed;
    }

    // ---- Conversions -------------------------------------------------------------------------------------

    /// <summary>
    /// <c>Temporal.PlainDate.prototype.toPlainYearMonth</c>'s calendar steps
    /// (<c>#sec-temporal.plaindate.prototype.toplainyearmonth</c>): the date's fields (ISODateToFields, as a date)
    /// and CalendarYearMonthFromFields under <c>constrain</c>. The result is the first day of the calendar month,
    /// not the date's own day (the correspondence's row C15).
    /// </summary>
    /// <param name="calendar">The date's calendar, which the year-month keeps.</param>
    /// <param name="date">The date.</param>
    /// <param name="result">The year-month's reference date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, or <see cref="CalendarStatus.OutOfRange"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a member of <see cref="CalendarId"/>, or the month and day are not an ISO date's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal sec-temporal.plaindate.prototype.toplainyearmonth via Broiler.VM JsRealm.TemporalPlainDate toPlainYearMonth; IP=Low; Security=Medium; Resources=1; Fingerprint=F0C65C
    // Broiler-Human:        PENDING
    public CalendarStatus DateToYearMonth(CalendarId calendar, IsoDate date, out IsoDate result)
    {
        result = default;
        var status = DateToFields(calendar, date, CalendarFieldsType.Date, out var fields);
        return status == CalendarStatus.Completed ? YearMonthFromFields(calendar, fields, CalendarOverflow.Constrain, out result) : status;
    }

    /// <summary>
    /// <c>Temporal.PlainDate.prototype.toPlainMonthDay</c>'s calendar steps
    /// (<c>#sec-temporal.plaindate.prototype.toplainmonthday</c>): the date's fields (ISODateToFields, as a date)
    /// and CalendarMonthDayFromFields under <c>constrain</c>. The month-day keeps the date's calendar, the Gregorian
    /// family's included (the correspondence's row C16).
    /// </summary>
    /// <param name="calendar">The date's calendar, which the month-day keeps.</param>
    /// <param name="date">The date.</param>
    /// <param name="result">The month-day's reference date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, <see cref="CalendarStatus.NoReferenceDate"/> or <see cref="CalendarStatus.OutOfRange"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a member of <see cref="CalendarId"/>, or the month and day are not an ISO date's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal sec-temporal.plaindate.prototype.toplainmonthday via Broiler.VM JsRealm.TemporalPlainDate toPlainMonthDay; IP=Low; Security=Medium; Resources=2; Fingerprint=8C8364
    // Broiler-Human:        PENDING
    public CalendarStatus DateToMonthDay(CalendarId calendar, IsoDate date, out IsoDate result)
    {
        result = default;
        var status = DateToFields(calendar, date, CalendarFieldsType.Date, out var fields);
        return status == CalendarStatus.Completed ? MonthDayFromFields(calendar, fields, CalendarOverflow.Constrain, out result) : status;
    }

    /// <summary>
    /// <c>Temporal.PlainMonthDay.prototype.toPlainDate</c>'s calendar steps
    /// (<c>#sec-temporal.plainmonthday.prototype.toplaindate</c>): the month-day's fields (ISODateToFields, as a
    /// month-day), merged with the fields the engine read from the argument (CalendarMergeFields), and
    /// CalendarDateFromFields under <c>constrain</c>. The year is read in the month-day's calendar, not as an ISO year
    /// (the correspondence's row C17).
    /// </summary>
    /// <param name="calendar">The month-day's calendar.</param>
    /// <param name="monthDay">The month-day's reference date.</param>
    /// <param name="fields">The fields PrepareCalendarFields read from the argument: <c>year</c>, and <c>era</c> and <c>eraYear</c> where the calendar has eras.</param>
    /// <param name="result">The date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns>As <see cref="DateFromFields"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="DateFromFields"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal sec-temporal.plainmonthday.prototype.toplaindate via Broiler.VM JsRealm.TemporalYearMonthDay toPlainDate; IP=Low; Security=Medium; Resources=1; Fingerprint=4951E2
    // Broiler-Human:        PENDING
    public CalendarStatus MonthDayToDate(CalendarId calendar, IsoDate monthDay, CalendarFields fields, out IsoDate result)
    {
        result = default;
        var status = DateToFields(calendar, monthDay, CalendarFieldsType.MonthDay, out var own);
        return status == CalendarStatus.Completed
            ? DateFromFields(calendar, MergeFields(calendar, own, fields), CalendarOverflow.Constrain, out result)
            : status;
    }

    /// <summary>
    /// <c>Temporal.PlainYearMonth.prototype.toPlainDate</c>'s calendar steps
    /// (<c>#sec-temporal.plainyearmonth.prototype.toplaindate</c>): the year-month's fields (ISODateToFields, as a
    /// year-month), merged with the fields the engine read from the argument (CalendarMergeFields), and
    /// CalendarDateFromFields under <c>constrain</c>.
    /// </summary>
    /// <param name="calendar">The year-month's calendar.</param>
    /// <param name="yearMonth">The year-month's reference date.</param>
    /// <param name="fields">The fields PrepareCalendarFields read from the argument: <c>day</c>.</param>
    /// <param name="result">The date; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns>As <see cref="DateFromFields"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="DateFromFields"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal sec-temporal.plainyearmonth.prototype.toplaindate via Broiler.VM JsRealm.TemporalYearMonthDay toPlainDate; IP=Low; Security=Medium; Resources=1; Fingerprint=C5A081
    // Broiler-Human:        PENDING
    public CalendarStatus YearMonthToDate(CalendarId calendar, IsoDate yearMonth, CalendarFields fields, out IsoDate result)
    {
        result = default;
        var status = DateToFields(calendar, yearMonth, CalendarFieldsType.YearMonth, out var own);
        return status == CalendarStatus.Completed
            ? DateFromFields(calendar, MergeFields(calendar, own, fields), CalendarOverflow.Constrain, out result)
            : status;
    }

    // ---- ISO 8601 ----------------------------------------------------------------------------------------

    // RegulateISODate (s3.5.7) over the values the fields hold, of any magnitude. Out-of-range years stay as they
    // are under constrain: the caller's limits refuse them afterwards.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.7 via Broiler.VM JsTemporal.RegulateDate; IP=Low; Security=Low; Resources=0; Fingerprint=49621B
    // Broiler-Human:        PENDING
    private static CalendarStatus RegulateIsoDate(double year, double month, double day, bool reject, out IsoDate result)
    {
        if (reject)
        {
            if (month is < 1 or > 12 || day < 1 || day > IsoCalendarMath.DaysInMonth((long)year, (int)month))
            {
                result = default;
                return CalendarStatus.FieldOutOfRange;
            }

            result = new IsoDate((long)year, (int)month, (int)day);
            return CalendarStatus.Completed;
        }

        var clampedMonth = (int)Math.Clamp(month, 1, 12);
        var clampedYear = (long)Math.Clamp(year, -1e15, 1e15);
        var clampedDay = (int)Math.Clamp(day, 1, IsoCalendarMath.DaysInMonth(clampedYear, clampedMonth));
        result = new IsoDate(clampedYear, clampedMonth, clampedDay);
        return CalendarStatus.Completed;
    }

    // CalendarDateToISO (s12.3.22) for ISO 8601.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.22 via Broiler.VM JsTemporal.DateToIso; IP=Low; Security=Low; Resources=0; Fingerprint=C1AC80
    // Broiler-Human:        PENDING
    private static CalendarStatus IsoDateToIso(Fields fields, bool reject, out IsoDate result)
    {
        if (Math.Abs(fields.Year!.Value) > MaxIsoFieldYear)
        {
            result = default;
            return CalendarStatus.OutOfRange;
        }

        return RegulateIsoDate(fields.Year.Value, fields.Month!.Value, fields.Day!.Value, reject, out result);
    }

    // CalendarDateAdd (s12.3.7) for ISO 8601: years and months balanced, the day regulated, then weeks and days.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=Low; Resources=1; Fingerprint=B08445
    // Broiler-Human:        PENDING
    private static CalendarStatus IsoDateAdd(IsoDate date, CalendarDuration duration, bool reject, out IsoDate result)
    {
        result = default;
        var (year, month) = IsoCalendarMath.BalanceYearMonth(date.Year + duration.Years, date.Month + duration.Months);
        var status = RegulateIsoDate(year, month, date.Day, reject, out var intermediate);
        if (status != CalendarStatus.Completed)
            return status;

        var days = IsoCalendarMath.EpochDays(intermediate.Year, intermediate.Month, intermediate.Day) + duration.Days + (7 * duration.Weeks);
        if (!CalendarLimits.DateWithinLimits(days))
            return CalendarStatus.OutOfRange;

        result = FromEpochDays(days);
        return CalendarStatus.Completed;
    }

    // CompareSurpasses (s3.5.5) for the ISO calendar's numeric months.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.5 via Broiler.VM JsTemporal.Surpasses; IP=Low; Security=Low; Resources=0; Fingerprint=D8E757
    // Broiler-Human:        PENDING
    private static bool Surpasses(int sign, long year, long month, long day, IsoDate target)
    {
        if (year != target.Year)
            return sign * (year - target.Year) > 0;
        if (month != target.Month)
            return sign * (month - target.Month) > 0;
        if (day != target.Day)
            return sign * (day - target.Day) > 0;

        return false;
    }

    // ISODateSurpasses (s3.5.6) for years and months: whether the base date moved by them, its day unregulated,
    // surpasses the target. Weeks and days are not passed: the published text returns false whenever months is
    // zero, which would never end its own weeks loop, so the weeks and days are reckoned in whole days afterwards, as
    // the proposal's polyfill reckons them.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.6 via Broiler.VM JsTemporal.DateSurpasses; IP=Low; Security=Low; Resources=0; Fingerprint=2B402C
    // Broiler-Human:        PENDING
    private static bool DateSurpasses(int sign, IsoDate baseDate, IsoDate target, long years, long months)
    {
        var y0 = baseDate.Year + years;
        if (Surpasses(sign, y0, baseDate.Month, baseDate.Day, target))
            return true;

        if (months == 0)
            return false;

        var (year, month) = IsoCalendarMath.BalanceYearMonth(y0, baseDate.Month + months);
        return Surpasses(sign, year, month, baseDate.Day, target);
    }

    // The largest count, from an estimate, that does not surpass: CalendarDateUntil's loops, counted from estimates.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.9 via Broiler.VM JsTemporal.DateUntil; IP=Low; Security=Low; Resources=2; Fingerprint=8C060B
    // Broiler-Human:        PENDING
    private static long Largest(int sign, long estimate, Func<long, bool> surpasses)
    {
        var count = estimate;
        while (count != 0 && surpasses(count))
            count -= sign;

        while (!surpasses(count + sign))
            count += sign;

        return count;
    }

    // CalendarDateUntil (s12.3.9) for ISO 8601: the largest years and months that do not surpass the second date,
    // found from an estimate rather than by counting up one at a time; then the weeks and days between the
    // constrained intermediate date and the second date.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.9 via Broiler.VM JsTemporal.DateUntil; IP=Low; Security=Low; Resources=2; Fingerprint=E136D6
    // Broiler-Human:        PENDING
    private static CalendarDuration IsoDateUntil(IsoDate one, IsoDate two, CalendarUnit largestUnit, int sign)
    {
        long years = 0;
        long months = 0;

        if (largestUnit is CalendarUnit.Year or CalendarUnit.Month)
        {
            if (largestUnit == CalendarUnit.Year)
                years = Largest(sign, two.Year - one.Year, y => DateSurpasses(sign, one, two, y, 0));

            var estimate = ((two.Year - one.Year - years) * 12) + (two.Month - one.Month);
            months = Largest(sign, estimate, m => DateSurpasses(sign, one, two, years, m));
        }

        var (yearAfter, monthAfter) = IsoCalendarMath.BalanceYearMonth(one.Year + years, one.Month + months);
        var constrainedDay = Math.Min(one.Day, IsoCalendarMath.DaysInMonth(yearAfter, monthAfter));
        var days = IsoCalendarMath.EpochDays(two.Year, two.Month, two.Day) - IsoCalendarMath.EpochDays(yearAfter, monthAfter, constrainedDay);
        long weeks = 0;

        if (largestUnit == CalendarUnit.Week)
        {
            weeks = days / 7;
            days %= 7;
        }

        return new CalendarDuration(years, months, weeks, days);
    }

    // ---- The era proposal's section 4.1 ------------------------------------------------------------------

    // CalendarResolveFields (s12.3.31) for ISO 8601, or NonISOResolveFields (the era proposal's 4.1.24).
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.31 via Broiler.VM JsTemporal.ResolveFields; IP=Low; Security=Low; Resources=1; Fingerprint=BEAF19
    // Broiler-Human:        PENDING
    private CalendarStatus ResolveFields(CalendarId calendar, ref Fields fields, CalendarFieldsType type)
    {
        if (calendar != CalendarId.Iso8601)
            return NonIsoResolveFields(SystemOf(calendar), ref fields, type);

        var needsYear = type is CalendarFieldsType.Date or CalendarFieldsType.YearMonth;
        var needsDay = type is CalendarFieldsType.Date or CalendarFieldsType.MonthDay;

        if (needsYear && fields.Year is null)
            return CalendarStatus.MissingYear;
        if (needsDay && fields.Day is null)
            return CalendarStatus.MissingDay;
        if (fields.Month is null && fields.MonthCode is null)
            return CalendarStatus.MissingMonth;

        if (fields.MonthCode is { } code)
        {
            // ISO 8601 HAS NO LEAP MONTH, and its months are M01 to M12.
            if (code.IsLeapMonth || code.Number > 12)
                return CalendarStatus.InvalidMonthCode;

            if (fields.Month is { } given && given != code.Number)
                return CalendarStatus.MonthCodeMismatch;

            fields.Month = code.Number;
        }

        return CalendarStatus.Completed;
    }

    // A calendar year from the fields' year, refused beyond every year a date within the limits lies in.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsTemporal.CalendarYear; IP=Low; Security=Low; Resources=1; Fingerprint=AB16AD
    // Broiler-Human:        PENDING
    private static CalendarStatus CalendarYear(CalendarSystem system, double year, out CalendarSystemYear result)
    {
        if (Math.Abs(year) > CalendarLimits.YearBound)
        {
            result = null!;
            return CalendarStatus.OutOfRange;
        }

        result = system.Year((long)year);
        return CalendarStatus.Completed;
    }

    // IsValidMonthCodeForCalendar (the era proposal's 4.1.4): M01 to M12, and Table 3's others for the calendar.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.4, Table 3 via Broiler.VM JsTemporal.IsValidMonthCode; IP=Low; Security=Low; Resources=0; Fingerprint=7DDDBB
    // Broiler-Human:        PENDING
    private static bool IsValidMonthCode(CalendarSystem system, MonthCode monthCode) =>
        (!monthCode.IsLeapMonth && monthCode.Number is >= 1 and <= 12) || system.IsAdditionalMonthCode(monthCode);

    // ConstrainMonthCode (the era proposal's 4.1.6): a leap month the year lacks becomes the common month of its
    // number, or Hebrew's M06, which follows Adar I.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.6 via Broiler.VM JsTemporal.ConstrainMonthCode; IP=Low; Security=Low; Resources=0; Fingerprint=F6D4B3
    // Broiler-Human:        PENDING
    private static CalendarStatus ConstrainMonthCode(CalendarSystem system, CalendarSystemYear year, MonthCode monthCode, bool reject, out MonthCode result)
    {
        if (CalendarSystem.Ordinal(year, monthCode) > 0)
        {
            result = monthCode;
            return CalendarStatus.Completed;
        }

        if (reject)
        {
            result = default;
            return CalendarStatus.MonthNotInYear;
        }

        result = system.SkipBackward ? new MonthCode(monthCode.Number, false) : new MonthCode(6, false);
        return CalendarStatus.Completed;
    }

    // NonISOResolveFields (the era proposal's 4.1.24).
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.24 via Broiler.VM JsTemporal.NonIsoResolveFields; IP=Low; Security=Low; Resources=1; Fingerprint=85A805
    // Broiler-Human:        PENDING
    private static CalendarStatus NonIsoResolveFields(CalendarSystem system, ref Fields fields, CalendarFieldsType type)
    {
        var needsYear = type != CalendarFieldsType.MonthDay || fields.MonthCode is null || fields.Month is not null;
        var needsDay = type != CalendarFieldsType.YearMonth;

        if (needsYear && fields.Year is null && (!system.HasEras || fields.Era is null || fields.EraYear is null))
            return CalendarStatus.MissingYear;
        if (system.HasEras && (fields.Era is null) != (fields.EraYear is null))
            return CalendarStatus.IncompleteEra;
        if (needsDay && fields.Day is null)
            return CalendarStatus.MissingDay;
        if (fields.Month is null && fields.MonthCode is null)
            return CalendarStatus.MissingMonth;

        if (system.HasEras && fields.EraYear is { } eraYear)
        {
            if (Math.Abs(eraYear) > 2 * CalendarLimits.YearBound)
                return CalendarStatus.OutOfRange;

            if (system.YearForEra(fields.Era!, (long)eraYear) is not { } year)
                return CalendarStatus.UnknownEra;

            if (fields.Year is { } given && given != year)
                return CalendarStatus.EraYearMismatch;

            fields.Year = year;
            fields.Era = null;
            fields.EraYear = null;
        }

        if (fields.MonthCode is { } code)
        {
            if (!IsValidMonthCode(system, code))
                return CalendarStatus.InvalidMonthCode;

            if (fields.Year is { } y)
            {
                var status = CalendarYear(system, y, out var year);
                if (status != CalendarStatus.Completed)
                    return status;

                ConstrainMonthCode(system, year, code, reject: false, out var constrained);
                var month = CalendarSystem.Ordinal(year, constrained);
                if (fields.Month is { } given && given != month)
                    return CalendarStatus.MonthCodeMismatch;

                fields.Month = month;
            }
        }

        return CalendarStatus.Completed;
    }

    // A month or day regulated against the calendar's most: clamped under constrain, refused under reject.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.20 via Broiler.VM JsTemporal.Regulated; IP=Low; Security=Low; Resources=0; Fingerprint=C033D0
    // Broiler-Human:        PENDING
    private static CalendarStatus Regulated(double value, int maximum, bool reject, out int result)
    {
        if (value <= maximum)
        {
            result = (int)value;
            return CalendarStatus.Completed;
        }

        result = maximum;
        return reject ? CalendarStatus.FieldOutOfRange : CalendarStatus.Completed;
    }

    // NonISOCalendarDateToISO (the era proposal's 4.1.20).
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.20 via Broiler.VM JsTemporal.NonIsoDateToIso; IP=Low; Security=Low; Resources=1; Fingerprint=E349B3
    // Broiler-Human:        PENDING
    private static CalendarStatus NonIsoDateToIso(CalendarSystem system, Fields fields, bool reject, out IsoDate result)
    {
        result = default;
        var status = CalendarYear(system, fields.Year!.Value, out var year);
        if (status == CalendarStatus.Completed && fields.MonthCode is { } code)
            status = ConstrainMonthCode(system, year, code, reject, out _);

        var month = 0;
        if (status == CalendarStatus.Completed)
            status = Regulated(fields.Month!.Value, year.Months, reject, out month);

        var day = 0;
        if (status == CalendarStatus.Completed)
            status = Regulated(fields.Day!.Value, year.DaysInMonth(month), reject, out day);

        if (status == CalendarStatus.Completed)
            result = FromEpochDays(year.Start + year.DaysBefore(month) + day - 1);

        return status;
    }

    // NonISOMonthDayToISOReferenceDate (the era proposal's 4.1.21), with Table 6 for the Chinese and Korean calendars.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.21, Table 6 via Broiler.VM JsTemporal.NonIsoMonthDayToReferenceDate; IP=Low; Security=Low; Resources=2; Fingerprint=709992
    // Broiler-Human:        PENDING
    private static CalendarStatus NonIsoMonthDayToReferenceDate(CalendarSystem system, Fields fields, bool reject, out IsoDate result)
    {
        result = default;
        MonthCode monthCode;
        int daysInMonth;

        if (fields.Year is { } y)
        {
            var status = CalendarYear(system, y, out var year);
            if (status != CalendarStatus.Completed)
                return status;

            if (year.Start + year.Days <= CalendarLimits.MinDay || year.Start > CalendarLimits.MaxDay)
                return CalendarStatus.OutOfRange;

            status = Regulated(fields.Month!.Value, year.Months, reject, out var month);
            if (status != CalendarStatus.Completed)
                return status;

            if (fields.MonthCode is { } code)
            {
                status = ConstrainMonthCode(system, year, code, reject, out monthCode);
                if (status != CalendarStatus.Completed)
                    return status;
            }
            else
            {
                monthCode = CalendarSystem.MonthCodeOf(year, month);
            }

            daysInMonth = year.DaysInMonth(month);
        }
        else
        {
            monthCode = fields.MonthCode!.Value;
            daysInMonth = system is EastAsianCalendarSystem ? 30 : MaxDaysInMonth(system, monthCode);
        }

        var dayStatus = Regulated(fields.Day!.Value, daysInMonth, reject, out var day);
        if (dayStatus != CalendarStatus.Completed)
            return dayStatus;

        IsoDate? found;
        if (system is EastAsianCalendarSystem)
        {
            if (system.TableReferenceYear(monthCode, day) is not > 0)
            {
                // TABLE 6'S DASH: no year holds the leap month with that day; constrain takes the common month.
                if (reject)
                    return CalendarStatus.NoReferenceDate;

                monthCode = new MonthCode(monthCode.Number, false);
            }

            var isoYear = system.TableReferenceYear(monthCode, day)!.Value;
            found = ReferenceDate(system, monthCode, day, IsoCalendarMath.EpochDays(isoYear, 1, 1), IsoCalendarMath.EpochDays(isoYear, 12, 31), latest: true);
        }
        else
        {
            found = ReferenceDate(system, monthCode, day, IsoCalendarMath.EpochDays(1900, 1, 1), IsoCalendarMath.EpochDays(1972, 12, 31), latest: true) ??
                ReferenceDate(system, monthCode, day, IsoCalendarMath.EpochDays(1973, 1, 1), IsoCalendarMath.EpochDays(2035, 12, 31), latest: false);
        }

        if (found is not { } date)
            return CalendarStatus.NoReferenceDate;

        result = date;
        return CalendarStatus.Completed;
    }

    // The latest or earliest ISO date between two days whose month code and day are the given ones.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.21 via Broiler.VM JsTemporal.ReferenceDate; IP=Low; Security=Low; Resources=2; Fingerprint=34ADCC
    // Broiler-Human:        PENDING
    private static IsoDate? ReferenceDate(CalendarSystem system, MonthCode monthCode, int day, long from, long to, bool latest)
    {
        var step = latest ? -1 : 1;
        for (var y = system.YearOf(latest ? to : from); ; y += step)
        {
            var year = system.Year(y);
            if (latest ? year.Start + year.Days <= from : year.Start > to)
                return null;

            var ordinal = CalendarSystem.Ordinal(year, monthCode);
            if (ordinal > 0 && day <= year.DaysInMonth(ordinal))
            {
                var days = year.Start + year.DaysBefore(ordinal) + day - 1;
                if (days >= from && days <= to)
                    return FromEpochDays(days);
            }
        }
    }

    // The most days a month code has in any year from 1900 to 2035 (the era proposal's 4.1.21).
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.21 via Broiler.VM JsTemporal.MaxDaysInMonth; IP=Low; Security=Low; Resources=2; Fingerprint=85BD1F
    // Broiler-Human:        PENDING
    private static int MaxDaysInMonth(CalendarSystem system, MonthCode monthCode)
    {
        var most = 0;
        var last = system.YearOf(IsoCalendarMath.EpochDays(2035, 12, 31));
        for (var y = system.YearOf(IsoCalendarMath.EpochDays(1900, 1, 1)); y <= last; y++)
        {
            var year = system.Year(y);
            var ordinal = CalendarSystem.Ordinal(year, monthCode);
            if (ordinal > 0)
                most = Math.Max(most, year.DaysInMonth(ordinal));
        }

        return most;
    }

    // BalanceNonISODate's months (the era proposal's 4.1.16): the year and ordinal month a possibly out-of-range month
    // of a year names, or null when that year is beyond the bound. The Chinese and Korean calendars count months from
    // their years, which the shared sources answer only within it (JSD-0077): a balance that would read a year past
    // it reaches a year no date within the ISO limits is in, and is null, as an estimate past it is.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.16 via Broiler.VM JsTemporal.BalanceMonths; IP=Low; Security=Low; Resources=1; Fingerprint=423B22
    // Broiler-Human:        PENDING
    private static (long Year, int Month)? BalanceMonths(CalendarSystem system, long year, long month)
    {
        var estimate = year + (long)Math.Floor((month - 1) / system.MeanMonths);
        if (Math.Abs(estimate) > CalendarLimits.YearBound)
            return null;

        var total = system.MonthsBefore(year) + month - 1;
        var y = estimate;
        var bounded = system.MonthsBeforeReadsYears;

        while (system.MonthsBefore(y) > total)
        {
            y--;
            if (bounded && y < -CalendarLimits.YearBound)
                return null;
        }

        while (true)
        {
            if (bounded && y + 1 > CalendarLimits.YearBound)
                return null;

            if (system.MonthsBefore(y + 1) > total)
                break;

            y++;
        }

        return (y, (int)(total - system.MonthsBefore(y)) + 1);
    }

    // BalanceMonths where every caller is within the limits, so that the year is within the bound.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.16 via Broiler.VM JsTemporal.NonIsoDateUntil; IP=Low; Security=Low; Resources=1; Fingerprint=E8631E
    // Broiler-Human:        PENDING
    private static (long Year, int Month) BalanceMonthsWithin(CalendarSystem system, long year, long month) =>
        BalanceMonths(system, year, month) ??
            throw new InvalidOperationException("A difference between dates within the limits balanced months past the calendar year sources' bound.");

    // NonISODateAdd (the era proposal's 4.1.18).
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.18 via Broiler.VM JsTemporal.NonIsoDateAdd; IP=Low; Security=Low; Resources=1; Fingerprint=F0B1D3
    // Broiler-Human:        PENDING
    private static CalendarStatus NonIsoDateAdd(CalendarSystem system, IsoDate date, CalendarDuration duration, bool reject, out IsoDate result)
    {
        result = default;
        var (year, month, day) = system.FromEpochDays(IsoCalendarMath.EpochDays(date.Year, date.Month, date.Day));
        var status = CalendarYear(system, year.Year + duration.Years, out var y0);
        if (status != CalendarStatus.Completed)
            return status;

        status = ConstrainMonthCode(system, y0, CalendarSystem.MonthCodeOf(year, month), reject, out var monthCode);
        if (status != CalendarStatus.Completed)
            return status;

        var m0 = CalendarSystem.Ordinal(y0, monthCode);
        if (BalanceMonths(system, y0.Year, m0 + duration.Months) is not { } balanced)
            return CalendarStatus.OutOfRange;

        var (endYear, endMonth) = balanced;
        var target = system.Year(endYear);
        status = Regulated(day, target.DaysInMonth(endMonth), reject, out var regulated);
        if (status != CalendarStatus.Completed)
            return status;

        var days = target.Start + target.DaysBefore(endMonth) + regulated - 1 + (7 * duration.Weeks) + duration.Days;
        if (!CalendarLimits.DateWithinLimits(days))
            return CalendarStatus.OutOfRange;

        result = FromEpochDays(days);
        return CalendarStatus.Completed;
    }

    // CompareSurpasses (s3.5.5) with a month code; codes order as their strings do, a leap month after its common one.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.5 via Broiler.VM JsTemporal.SurpassesByCode; IP=Low; Security=Low; Resources=0; Fingerprint=5AAE1F
    // Broiler-Human:        PENDING
    private static bool SurpassesByCode(int sign, long year, MonthCode monthCode, int day, CalendarPoint target)
    {
        if (year != target.Year)
            return sign * (year - target.Year) > 0;

        if (monthCode != target.MonthCode)
            return sign > 0 ? CompareCodes(monthCode, target.MonthCode) > 0 : CompareCodes(target.MonthCode, monthCode) > 0;

        return day != target.Day && sign * (day - target.Day) > 0;
    }

    // CompareSurpasses (s3.5.5) with an ordinal month.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.5 via Broiler.VM JsTemporal.SurpassesByMonth; IP=Low; Security=Low; Resources=0; Fingerprint=711685
    // Broiler-Human:        PENDING
    private static bool SurpassesByMonth(int sign, long year, int month, int day, CalendarPoint target)
    {
        if (year != target.Year)
            return sign * (year - target.Year) > 0;

        if (month != target.Month)
            return sign * (month - target.Month) > 0;

        return day != target.Day && sign * (day - target.Day) > 0;
    }

    // The ordinal order of two month codes' strings.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.5 via Broiler.VM JsTemporal.SurpassesByCode; IP=Low; Security=Low; Resources=0; Fingerprint=F51FD2
    // Broiler-Human:        PENDING
    private static int CompareCodes(MonthCode one, MonthCode two) =>
        one.Number != two.Number ? one.Number.CompareTo(two.Number) : one.IsLeapMonth.CompareTo(two.IsLeapMonth);

    // NonISODateUntil (the era proposal's 4.1.19), its loops counted from estimates: the years and months start from
    // the difference of the two dates' years and month counts and move until NonISODateSurpasses (4.1.17) holds, and
    // the weeks and days, which the proposal reckons in days from the last regulated date, are a subtraction.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.19 via Broiler.VM JsTemporal.NonIsoDateUntil; IP=Low; Security=Low; Resources=2; Fingerprint=D0C343
    // Broiler-Human:        PENDING
    private static CalendarDuration NonIsoDateUntil(CalendarSystem system, IsoDate one, IsoDate two, CalendarUnit largestUnit, int sign)
    {
        var oneDays = IsoCalendarMath.EpochDays(one.Year, one.Month, one.Day);
        var twoDays = IsoCalendarMath.EpochDays(two.Year, two.Month, two.Day);
        long years = 0;
        long months = 0;
        var intermediate = oneDays;

        if (largestUnit is CalendarUnit.Year or CalendarUnit.Month)
        {
            var (year1, month1, day1) = system.FromEpochDays(oneDays);
            var (year2, month2, day2) = system.FromEpochDays(twoDays);
            var code1 = CalendarSystem.MonthCodeOf(year1, month1);
            var target = new CalendarPoint(year2.Year, CalendarSystem.MonthCodeOf(year2, month2), month2, day2);

            // The start of the months NonISODateSurpasses adds: the first date's month code in the year reached.
            (long Year, int Month) Start(long y)
            {
                var year = system.Year(year1.Year + y);
                ConstrainMonthCode(system, year, code1, reject: false, out var constrained);
                return (year.Year, CalendarSystem.Ordinal(year, constrained));
            }

            bool Surpasses(long y, long m)
            {
                if (SurpassesByCode(sign, year1.Year + y, code1, day1, target))
                    return true;

                var (startYear, startMonth) = Start(y);
                var (added, addedMonth) = BalanceMonthsWithin(system, startYear, startMonth + m);
                return SurpassesByMonth(sign, added, addedMonth, day1, target);
            }

            if (largestUnit == CalendarUnit.Year)
                years = Largest(sign, year2.Year - year1.Year, y => Surpasses(y, 0));

            var (fromYear, fromMonth) = Start(years);
            var estimate = system.MonthsBefore(year2.Year) + month2 - (system.MonthsBefore(fromYear) + fromMonth);
            months = Largest(sign, estimate, m => Surpasses(years, m));

            var (endYear, endMonth) = BalanceMonthsWithin(system, fromYear, fromMonth + months);
            var end = system.Year(endYear);
            intermediate = end.Start + end.DaysBefore(endMonth) + Math.Min(day1, end.DaysInMonth(endMonth)) - 1;
        }

        var days = twoDays - intermediate;
        long weeks = 0;

        if (largestUnit == CalendarUnit.Week)
        {
            weeks = days / 7;
            days %= 7;
        }

        return new CalendarDuration(years, months, weeks, days);
    }

    // ---- Helpers -----------------------------------------------------------------------------------------

    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 via Broiler.VM JsTemporal.SystemOf; IP=Low; Security=Low; Resources=0; Fingerprint=4B9BF4
    // Broiler-Human:        PENDING
    private CalendarSystem SystemOf(CalendarId calendar) => systems[(int)calendar]!;

    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.9 via Broiler.VM JsIsoDate; IP=Low; Security=Low; Resources=0; Fingerprint=598B10
    // Broiler-Human:        PENDING
    private static IsoDate FromEpochDays(long days)
    {
        var (year, month, day) = IsoCalendarMath.FromEpochDays(days);
        return new IsoDate(year, month, day);
    }

    // Broiler-AI:           Origin=AI; Spec=Intl era and month code proposal Table 1; IP=Low; Security=Low; Resources=0; Fingerprint=6821FD
    // Broiler-Human:        PENDING
    private static void CheckCalendar(CalendarId calendar)
    {
        if (calendar is < CalendarId.Iso8601 or > CalendarId.Roc)
            throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "The calendar is not one of Table 1's.");
    }

    // Broiler-AI:           Origin=AI; Spec=Temporal s12.3.31; IP=Low; Security=Low; Resources=0; Fingerprint=62A1E4
    // Broiler-Human:        PENDING
    private static void CheckType(CalendarFieldsType type)
    {
        if (type is < CalendarFieldsType.Date or > CalendarFieldsType.MonthDay)
            throw new ArgumentOutOfRangeException(nameof(type), type, "The type is not date, year-month or month-day.");
    }

    // Broiler-AI:           Origin=AI; Spec=Temporal s13.6; IP=Low; Security=Low; Resources=0; Fingerprint=1614EF
    // Broiler-Human:        PENDING
    private static bool IsReject(CalendarOverflow overflow) => overflow switch
    {
        CalendarOverflow.Constrain => false,
        CalendarOverflow.Reject => true,
        _ => throw new ArgumentOutOfRangeException(nameof(overflow), overflow, "The overflow is neither constrain nor reject."),
    };

    // The calendar fields' keys, for CalendarFieldKeysToIgnore.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.29 via Broiler.VM JsTemporal.KeysToIgnore; IP=Low; Security=None; Resources=0; Fingerprint=9B95B0
    // Broiler-Human:        PENDING
    [Flags]
    private enum FieldKeys
    {
        None = 0,
        Era = 1,
        EraYear = 2,
        Year = 4,
        Month = 8,
        MonthCode = 16,
        Day = 32,
    }

    // A Calendar Date Record's year, month code, ordinal month and day, for CompareSurpasses.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.5 via Broiler.VM JsTemporal.CalendarPoint; IP=Low; Security=None; Resources=0; Fingerprint=769CE6
    // Broiler-Human:        PENDING
    private readonly record struct CalendarPoint(long Year, MonthCode MonthCode, int Month, int Day);

    // The calendar fields as resolution changes them, each value checked against its conversion's range.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.2 via Broiler.VM JsCalendarFields; IP=Low; Security=None; Resources=0; Fingerprint=95A71A
    // Broiler-Human:        PENDING
    private struct Fields
    {
        internal string? Era;
        internal double? EraYear;
        internal double? Year;
        internal double? Month;
        internal MonthCode? MonthCode;
        internal double? Day;

        // Broiler-AI:           Origin=AI; Spec=Temporal s12.3.2, Table 19; IP=Low; Security=Low; Resources=0; Fingerprint=76F33C
        // Broiler-Human:        PENDING
        internal Fields(CalendarFields fields)
        {
            Era = fields.Era;
            EraYear = Integral(fields.EraYear, double.NegativeInfinity, nameof(fields.EraYear));
            Year = Integral(fields.Year, double.NegativeInfinity, nameof(fields.Year));
            Month = Integral(fields.Month, 1, nameof(fields.Month));
            Day = Integral(fields.Day, 1, nameof(fields.Day));
            MonthCode = fields.MonthCode is { Number: 0, IsLeapMonth: false }
                ? throw new ArgumentOutOfRangeException(nameof(fields), "The month code is not one ParseMonthCode gives.")
                : fields.MonthCode;
        }

        // Broiler-AI:           Origin=AI; Spec=Temporal s12.3.2, Table 19; IP=Low; Security=Low; Resources=0; Fingerprint=21FE93
        // Broiler-Human:        PENDING
        private static double? Integral(double? value, double minimum, string name) =>
            value is { } v && (!double.IsFinite(v) || Math.Truncate(v) != v || v < minimum)
                ? throw new ArgumentOutOfRangeException(name, v, "The field is not a value its conversion gives.")
                : value;
    }
}
