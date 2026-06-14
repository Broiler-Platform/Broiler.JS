using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/781
//
// Fixed here (a calendar / time-zone slot value supplied as a full Temporal ISO string):
//   * Problems 5/8 — ToTemporalCalendarSlotValue rejected a `calendar` field that was an ISO
//     date/date-time string (e.g. "2020-01-01" or the leap-second "2016-12-31T23:59:60") with
//     "unsupported calendar". Per ParseTemporalCalendarString such a string is now parsed and its
//     [u-ca=…] annotation adopted (defaulting to iso8601); a bare unrecognized string remains a
//     RangeError.
//   * Problems 16/17 — ToTemporalTimeZoneIdentifier rejected a `timeZone` field that was an ISO
//     date-time string (e.g. "2021-08-19T17:30Z" or "2016-12-31T23:59:60+00:00[UTC]") with
//     "unknown time zone". Per ParseTemporalTimeZoneString its time-zone designator — a [TimeZone]
//     annotation, a Z (UTC) designator, or a numeric UTC offset — is now extracted and canonicalized.
//   * Problem 22 — the PlainDate / PlainYearMonth / PlainMonthDay (and Duration relativeTo) string
//     parsers used a lenient ".*" time tail that accepted a fraction on the minutes or hours. The
//     tail is now the strict Temporal time grammar (a fraction only on the seconds component), so
//     fractional minutes / hours are a RangeError.
//   * Problem 15 — those parsers accepted more than one [u-ca=…] calendar annotation; a Temporal
//     string may carry at most one, so two or more (critical or not) are now a RangeError.
//   * Problem 7 (subset) — Temporal.ZonedDateTime.prototype.with accepted a Temporal object (a
//     date-ish type carrying its own calendar / time zone) as its fields argument.
//     RejectObjectWithCalendarOrTimeZone now rejects such an object (and a `calendar` / `timeZone`
//     property) with a TypeError.
//   * Problem 24 — Temporal.ZonedDateTime.from (disambiguation / offset), since / until
//     (smallestUnit / roundingIncrement / roundingMode) and toString (offset / roundingMode /
//     smallestUnit / timeZoneName / fractionalSecondDigits) silently ignored these options, so an
//     invalid value passed without error. They are now read and validated (an invalid value →
//     RangeError, a Symbol → TypeError); the rounding / display behaviour they request is validated
//     but not yet applied (only "compatible" disambiguation and the full serialization are honoured).
//   * Problem 1 — Temporal.ZonedDateTime.from from a property bag ignored the overflow option and
//     mishandled the non-Gregorian calendars: it checked the raw fields with IsValidISODate and
//     threw "invalid ISO date" for an out-of-range month/day (instead of constraining) or for a
//     non-ISO calendar. The wall-clock date-time is now resolved through Temporal.PlainDateTime's
//     property-bag resolution (overflow-applied, calendar-aware) before the zone offset is resolved.
//   * Problems 9/28 — Temporal.PlainDate / PlainDateTime / ZonedDateTime.prototype.with on a
//     non-Gregorian (era) calendar resolved the fields through the Gregorian path, so an { era,
//     eraYear } pair hit EraToIso and threw "invalid era" for the calendar's own eras (am / ah / bh
//     / aa). with() now merges the bag fields onto the receiver's calendar fields and re-resolves in
//     calendar space (CalendarMergeFields + the calendar's date-from-fields), so the era mapping,
//     the year/monthCode re-resolution and the month/monthCode & year/era consistency checks all use
//     the active calendar.
//   * Problem 29 — Temporal.PlainYearMonth.prototype.with completed a partial { era, eraYear } pair
//     from the receiver (on both the Gregorian-family and the era-bearing non-ISO calendars), so
//     providing only eraYear (or only era) passed silently. Per CalendarFieldKeysToIgnore an era /
//     eraYear field ignores the receiver's era group, so a partial pair is now a TypeError.
//   * Problem 14 — Temporal.Duration.compare with calendar units (years/months/weeks) threw "a
//     relativeTo calendar is not supported". It now adds each duration's date part to the (shared)
//     relativeTo date and compares the resulting instants, reusing the round/total relativeTo
//     machinery (an ISO / Gregorian-family PlainDate relativeTo; a ZonedDateTime / PlainDateTime /
//     non-ISO-calendar relativeTo remains unimplemented).
//   * Problems 18/23 — Temporal.PlainDate / PlainDateTime until / since balanced the year/month/day
//     difference by stepping months one at a time from an already day-constrained intermediate, so a
//     month-end "wrap" (e.g. Jan 29 -> Feb 28) miscounted a whole month and the residual days. The
//     ISO date difference now follows the reference dateUntil: a candidate month count "surpasses"
//     the end when the *unconstrained* (start-day-preserving) date passes it, and the residual is
//     measured from the day-constrained intermediate. since is now -(this.until(other)) (anchored on
//     the receiver, then negated — +0 not -0), rather than swapping the operands. Applies to the
//     Gregorian-family (ISO) calendars; the non-ISO calendars share a separate difference path.
//   * Problem 11 — the parser accepted `import(…)` and `import.meta` but rejected the phased dynamic
//     import forms `import.defer(…)` / `import.source(…)` (the `import.meta` branch threw on any
//     identifier other than "meta"). They now parse as an ImportCall (the compiler treats them like a
//     plain `import(…)`), fixing the dynamic-import-syntax/valid/*import-defer* tests.
//   * Problems 18/23 (non-ISO) — the same month-end balancing bug existed in the non-ISO calendar
//     difference path (TemporalNonIso.Difference). It now anchors the year/month counting on the
//     unconstrained start day too, fixing the arithmetic (fixed-month-count) calendars — coptic,
//     ethiopic, islamic-*, persian, indian, etc. The lunisolar calendars (chinese/dangi/hebrew),
//     which need the reference's leap-month cycle logic, remain unimplemented.
//   * Problem 2 — the indian calendar reported its era as "saka"; the canonical Intl.Era-monthcode
//     code is "shaka". The era property is now "shaka" (the "saka" spelling stays accepted as an
//     input alias), fixing the indian-calendar era assertions.
//   * Problem 3 (subset) — Temporal.PlainYearMonth.add/subtract did not validate that the
//     intermediate date (day 1 of the receiver's month) is within the ISO date limits, and used the
//     last day of the month for subtractions. It now always starts from day 1 (matching the current
//     spec, which negates the duration for subtract) and rejects an out-of-range intermediate, so
//     adding to the minimum year-month or subtracting past the maximum is a RangeError.
public class Issue781Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t").ToString();
    }

    // ───────────── Problems 5/8: calendar property-bag field as an ISO string ─────────────

    [Theory]
    [InlineData("2020-01-01")]                       // a bare ISO date
    [InlineData("2016-12-31T23:59:60")]              // a leap-second date-time
    [InlineData("2016-12-31T23:59:59.999999999")]    // sub-second precision
    [InlineData("2016-12-31T23:59:59+00:00[UTC]")]   // offset + time-zone annotation
    [InlineData("1970-01-01[u-ca=iso8601]")]         // an explicit iso8601 annotation
    public void CalendarPropertyBagIsoStringResolvesToIso(string calendar)
        => Assert.Equal("iso8601",
            Eval($"Temporal.PlainDate.from({{year:2019, monthCode:'M11', day:18, calendar:'{calendar}'}}).calendarId"));

    // The [u-ca=…] annotation of a calendar string is honoured, not just the iso8601 default.
    [Fact]
    public void CalendarPropertyBagIsoStringAdoptsAnnotation()
        => Assert.Equal("gregory",
            Eval("Temporal.PlainDate.from({year:2019, monthCode:'M11', day:18, calendar:'1970-01-01[u-ca=gregory]'}).calendarId"));

    // withCalendar takes the same slot value, so an ISO string works there too.
    [Theory]
    [InlineData("2020-01-01")]
    [InlineData("2016-12-31T23:59:60")]
    public void WithCalendarIsoStringResolvesToIso(string calendar)
        => Assert.Equal("iso8601",
            Eval($"Temporal.PlainDate.from('1976-11-18').withCalendar('{calendar}').calendarId"));

    // A bare, unrecognized calendar string is still a RangeError (not silently parsed).
    [Theory]
    [InlineData("Temporal.PlainDate.from({year:2019, month:11, day:18, calendar:'bogus'})")]
    [InlineData("Temporal.PlainDate.from('1976-11-18').withCalendar('not-a-calendar')")]
    public void UnknownCalendarStringThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // ───────────── Problems 16/17: timeZone slot value as an ISO string ─────────────

    [Theory]
    [InlineData("2021-08-19T17:30Z", "UTC")]                          // a Z (UTC) designator
    [InlineData("2016-12-31T23:59:60+00:00[UTC]", "UTC")]             // a [TimeZone] annotation (leap second)
    [InlineData("2021-08-19T17:30+01:00", "+01:00")]                  // a numeric UTC offset
    [InlineData("2021-08-19T17:30-08:00[America/Vancouver]", "America/Vancouver")] // annotation wins over offset
    public void TimeZoneConstructorIsoStringExtractsDesignator(string timeZone, string expected)
        => Assert.Equal(expected, Eval($"new Temporal.ZonedDateTime(0n, '{timeZone}').timeZoneId"));

    // The property-bag timeZone field accepts the same ISO strings.
    [Fact]
    public void TimeZonePropertyBagIsoStringExtractsDesignator()
        => Assert.Equal("UTC",
            Eval("Temporal.PlainDate.from('2021-08-19').toZonedDateTime('2021-08-19T17:30Z').timeZoneId"));

    // A bare, unrecognized time-zone string is still a RangeError.
    [Fact]
    public void UnknownTimeZoneStringThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName("new Temporal.ZonedDateTime(0n, 'Not/AZone')"));

    // ───────────── Problem 22: a fraction is allowed only on the seconds component ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('2000-05-02T00:00.123456789')")]      // fractional minutes
    [InlineData("Temporal.PlainDate.from('2000-05-02T00.123456789')")]         // fractional hours
    [InlineData("Temporal.PlainYearMonth.from('2000-05-02T00:00.5')")]
    [InlineData("Temporal.PlainMonthDay.from('2000-05-02T12.5')")]
    public void FractionalMinutesOrHoursThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A fraction on the seconds (the smallest component) is still accepted and discarded.
    [Theory]
    [InlineData("Temporal.PlainDate.from('2000-05-02T12:34:56.789').toString()", "2000-05-02")]
    [InlineData("Temporal.PlainYearMonth.from('2000-05-02T12:34:56.789').toString()", "2000-05")]
    public void FractionalSecondsAreAccepted(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ───────────── Problem 15: more than one calendar annotation → RangeError ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601][u-ca=iso8601]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601][!u-ca=iso8601]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601][u-ca=gregory]')")]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[u-ca=iso8601][u-ca=iso8601]')")]
    [InlineData("Temporal.PlainYearMonth.from('1970-01[u-ca=iso8601][u-ca=iso8601]')")]
    [InlineData("Temporal.PlainMonthDay.from('01-01[u-ca=iso8601][u-ca=iso8601]')")]
    public void MultipleCalendarAnnotationsThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A single (even critical) calendar annotation remains valid.
    [Theory]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601]').calendarId", "iso8601")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[!u-ca=gregory]').calendarId", "gregory")]
    public void SingleCalendarAnnotationIsAccepted(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ───────────── Problem 7: ZonedDateTime.with rejects a Temporal object ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('2000-01-01')")]
    [InlineData("Temporal.PlainDateTime.from('2000-01-01T00:00')")]
    [InlineData("Temporal.PlainTime.from('12:00')")]
    [InlineData("Temporal.PlainYearMonth.from('2000-01')")]
    [InlineData("Temporal.PlainMonthDay.from('01-01')")]
    [InlineData("new Temporal.ZonedDateTime(0n, 'UTC')")]
    public void ZonedDateTimeWithTemporalObjectThrowsTypeError(string value)
        => Assert.Equal("TypeError",
            ErrorName($"new Temporal.ZonedDateTime(0n, 'UTC').with({value})"));

    // A `calendar` or `timeZone` field in the property bag is also rejected (TypeError).
    [Theory]
    [InlineData("new Temporal.ZonedDateTime(0n, 'UTC').with({ year: 2024, calendar: 'iso8601' })")]
    [InlineData("new Temporal.ZonedDateTime(0n, 'UTC').with({ year: 2024, timeZone: 'UTC' })")]
    public void ZonedDateTimeWithCalendarOrTimeZoneFieldThrowsTypeError(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    // A plain property bag still works.
    [Fact]
    public void ZonedDateTimeWithPlainBagWorks()
        => Assert.Equal("2024", Eval("String(new Temporal.ZonedDateTime(0n, 'UTC').with({ year: 2024 }).year)"));

    // ───────────── Problem 24: ZonedDateTime option values are validated ─────────────

    // from: the disambiguation / offset options are validated (an invalid string → RangeError).
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 1, day: 1, timeZone: 'UTC' }, { disambiguation: null })")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 1, day: 1, timeZone: 'UTC' }, { disambiguation: 'bogus' })")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 1, day: 1, timeZone: 'UTC' }, { offset: null })")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 1, day: 1, timeZone: 'UTC' }, { offset: 'bogus' })")]
    public void FromInvalidOptionThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // since / until: smallestUnit / roundingIncrement / roundingMode are validated.
    [Theory]
    [InlineData("a.since(b, { smallestUnit: null })")]
    [InlineData("a.since(b, { smallestUnit: 'bogus' })")]
    [InlineData("a.until(b, { roundingMode: null })")]
    [InlineData("a.until(b, { roundingIncrement: 0 })")]
    public void DifferenceInvalidOptionThrowsRangeError(string expr)
        => Assert.Equal("RangeError", ErrorName(
            "const a = new Temporal.ZonedDateTime(0n, 'UTC');" +
            "const b = new Temporal.ZonedDateTime(1000000000n, 'UTC');" + expr));

    // toString: offset / roundingMode / smallestUnit / timeZoneName are validated.
    [Theory]
    [InlineData("z.toString({ offset: null })")]
    [InlineData("z.toString({ offset: 'bogus' })")]
    [InlineData("z.toString({ roundingMode: null })")]
    [InlineData("z.toString({ smallestUnit: null })")]
    [InlineData("z.toString({ smallestUnit: 'year' })")]
    [InlineData("z.toString({ timeZoneName: null })")]
    [InlineData("z.toString({ timeZoneName: 'bogus' })")]
    public void ToStringInvalidOptionThrowsRangeError(string expr)
        => Assert.Equal("RangeError", ErrorName("const z = new Temporal.ZonedDateTime(0n, 'UTC');" + expr));

    // A Symbol option value is a TypeError (it cannot be coerced to a string / number).
    [Theory]
    [InlineData("z.toString({ roundingMode: Symbol() })")]
    [InlineData("z.since(z, { smallestUnit: Symbol() })")]
    public void SymbolOptionValueThrowsTypeError(string expr)
        => Assert.Equal("TypeError", ErrorName("const z = new Temporal.ZonedDateTime(0n, 'UTC');" + expr));

    // ───────────── Problem 1: ZonedDateTime.from applies overflow / non-ISO calendars ─────────────

    // An out-of-range month / day is constrained (the default and explicit "constrain"), not
    // rejected with "invalid ISO date".
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 13, day: 1, timeZone: 'UTC' }).month", "12")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 13, day: 1, timeZone: 'UTC' }, { overflow: 'constrain' }).month", "12")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2001, month: 2, day: 31, timeZone: 'UTC' }).day", "28")]
    public void FromConstrainsOutOfRangeFields(string code, string expected)
        => Assert.Equal(expected, Eval($"String({code})"));

    // overflow "reject" still rejects an out-of-range field.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2000, month: 13, day: 1, timeZone: 'UTC' }, { overflow: 'reject' })")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2001, month: 2, day: 31, timeZone: 'UTC' }, { overflow: 'reject' })")]
    public void FromRejectThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A Gregorian-family (non-ISO) calendar in the property bag resolves instead of throwing.
    [Theory]
    [InlineData("gregory")]
    [InlineData("buddhist")]
    [InlineData("japanese")]
    public void FromGregorianFamilyCalendarResolves(string calendar)
        => Assert.Equal(calendar,
            Eval($"Temporal.ZonedDateTime.from({{ year: 2000, month: 1, day: 1, timeZone: 'UTC', calendar: '{calendar}' }}).calendarId"));

    // A missing timeZone is still a TypeError.
    [Fact]
    public void FromMissingTimeZoneThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("Temporal.ZonedDateTime.from({ year: 2000, month: 1, day: 1 })"));

    // ───────────── Problems 13/19: Temporal.Duration range limits ─────────────

    // `days` is bounded only by the total-time limit, not by 2^32 — a large day count (well over
    // 2^32 ≈ 4.29e9, but under 2^53 seconds) is valid.
    [Theory]
    [InlineData("new Temporal.Duration(0, 0, 0, 104249991374).days", "104249991374")]
    [InlineData("Temporal.Duration.from({ days: 104249991374 }).days", "104249991374")]
    public void LargeDayCountIsValid(string code, string expected)
        => Assert.Equal(expected, Eval($"String({code})"));

    // years / months / weeks remain bounded by 2^32; the boundary value (2^32) is out of range,
    // 2^32 − 1 is valid.
    [Theory]
    [InlineData("new Temporal.Duration(4294967296)")]                 // years = 2^32
    [InlineData("new Temporal.Duration(0, 4294967296)")]              // months = 2^32
    [InlineData("new Temporal.Duration(0, 0, 4294967296)")]           // weeks = 2^32
    [InlineData("new Temporal.Duration(0, 0, 0, 200000000000)")]      // days push total time past 2^53 s
    public void OutOfRangeDurationThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Theory]
    [InlineData("new Temporal.Duration(4294967295).years", "4294967295")]   // years = 2^32 − 1
    [InlineData("new Temporal.Duration(0, 0, 4294967295).weeks", "4294967295")]
    public void MaxDateFieldIsValid(string code, string expected)
        => Assert.Equal(expected, Eval($"String({code})"));

    // ───────────── Problems 9/28: .with() on a non-ISO (era) calendar ─────────────

    // Providing { era, eraYear } no longer routes through the Gregorian EraToIso (which rejected
    // "am" / "ah" / "bh" with "invalid era"); the calendar's own era mapping is used.
    [Theory]
    [InlineData("coptic", "am")]
    [InlineData("ethiopic", "am")]
    [InlineData("islamic-civil", "ah")]
    [InlineData("islamic-umalqura", "ah")]
    public void WithEraOnNonIsoCalendarResolves(string calendar, string era)
    {
        var code =
            $"const d = Temporal.PlainDate.from({{ year: 1400, month: 3, day: 5, calendar: '{calendar}' }});" +
            $"d.with({{ era: '{era}', eraYear: 1402 }}).year";
        Assert.Equal("1402", Eval(code));
    }

    // Changing a single field keeps the others (in calendar space).
    [Theory]
    [InlineData("d.with({ day: 12 }).day", "12")]
    [InlineData("d.with({ year: 1700 }).year", "1700")]
    [InlineData("d.with({ monthCode: 'M02' }).monthCode", "M02")]
    public void WithSingleFieldOnCopticKeepsOthers(string expr, string expected)
        => Assert.Equal(expected, Eval(
            "const d = Temporal.PlainDate.from({ year: 1726, month: 5, day: 10, calendar: 'coptic' });" + expr));

    // The calendar id is preserved through with().
    [Fact]
    public void WithPreservesNonIsoCalendar()
        => Assert.Equal("coptic", Eval(
            "Temporal.PlainDate.from({ year: 1726, month: 5, day: 10, calendar: 'coptic' }).with({ day: 1 }).calendarId"));

    // Inconsistent / incomplete fields are still validated.
    [Theory]
    [InlineData("d.with({ month: 5, monthCode: 'M06' })")]   // month / monthCode disagree -> RangeError
    public void WithInconsistentFieldsThrowsRangeError(string expr)
        => Assert.Equal("RangeError", ErrorName(
            "const d = Temporal.PlainDate.from({ year: 1726, month: 5, day: 10, calendar: 'coptic' });" + expr));

    [Theory]
    [InlineData("d.with({ eraYear: 1700 })")]                // eraYear without era -> TypeError
    [InlineData("d.with({})")]                               // no fields -> TypeError
    public void WithBadFieldsThrowsTypeError(string expr)
        => Assert.Equal("TypeError", ErrorName(
            "const d = Temporal.PlainDate.from({ year: 1726, month: 5, day: 10, calendar: 'coptic' });" + expr));

    // PlainDateTime.with on a non-ISO calendar keeps the time and resolves the date.
    [Fact]
    public void PlainDateTimeWithNonIsoKeepsTime()
        => Assert.Equal("15:30", Eval(
            "const dt = Temporal.PlainDateTime.from({ year: 1726, month: 5, day: 10, hour: 15, minute: 30, calendar: 'coptic' });" +
            "const r = dt.with({ day: 12 });" +
            "String(r.hour).padStart(2,'0') + ':' + String(r.minute).padStart(2,'0')"));

    // ZonedDateTime.with delegates to the same non-ISO resolution.
    [Fact]
    public void ZonedDateTimeWithNonIsoResolves()
        => Assert.Equal("12", Eval(
            "Temporal.ZonedDateTime.from({ year: 1726, month: 5, day: 10, timeZone: 'UTC', calendar: 'coptic' })" +
            ".with({ day: 12 }).day.toString()"));

    // ───────────── Problem 29: PlainYearMonth.with rejects a partial era pair ─────────────

    // eraYear (or era) without its partner is a TypeError — it is not completed from the receiver,
    // for both the Gregorian family and the era-bearing non-ISO calendars.
    [Theory]
    [InlineData("gregory", "{ eraYear: 5 }")]
    [InlineData("buddhist", "{ eraYear: 5 }")]
    [InlineData("japanese", "{ eraYear: 5 }")]
    [InlineData("roc", "{ era: 'roc' }")]
    [InlineData("coptic", "{ eraYear: 5 }")]
    [InlineData("ethiopic", "{ eraYear: 5 }")]
    [InlineData("islamic-civil", "{ era: 'ah' }")]
    public void YearMonthWithPartialEraThrowsTypeError(string calendar, string bag)
        => Assert.Equal("TypeError", ErrorName(
            $"Temporal.PlainYearMonth.from({{ year: 1400, month: 3, calendar: '{calendar}' }}).with({bag})"));

    // A complete era pair, or a plain year, still works.
    [Theory]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2000, month: 5, calendar: 'gregory' }).with({ era: 'ce', eraYear: 1999 }).year", "1999")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2000, month: 5, calendar: 'gregory' }).with({ year: 1990 }).year", "1990")]
    public void YearMonthWithCompleteYearWorks(string code, string expected)
        => Assert.Equal(expected, Eval($"String({code})"));

    // ───────────── Problem 14: Duration.compare with a relativeTo ─────────────

    // With a (PlainDate) relativeTo, calendar units are weighed against the calendar: 1 month from
    // 2000-01-01 is 31 days (> 30), but from 2000-02-01 (leap February) is 29 days (< 30).
    [Theory]
    [InlineData("Temporal.Duration.compare({ months: 1 }, { days: 30 }, { relativeTo: '2000-01-01' })", "1")]
    [InlineData("Temporal.Duration.compare({ months: 1 }, { days: 30 }, { relativeTo: '2000-02-01' })", "-1")]
    [InlineData("Temporal.Duration.compare({ years: 1 }, { days: 365 }, { relativeTo: '2000-01-01' })", "1")]  // 2000 is a leap year (366 d)
    [InlineData("Temporal.Duration.compare({ months: 1 }, { days: 31 }, { relativeTo: '2000-01-01' })", "0")]
    public void DurationCompareWithRelativeToWeighsCalendar(string code, string expected)
        => Assert.Equal(expected, Eval($"String({code})"));

    // A relativeTo property bag (with a Gregorian-family calendar) also works.
    [Fact]
    public void DurationCompareWithRelativeToPropertyBag()
        => Assert.Equal("1", Eval(
            "String(Temporal.Duration.compare({ months: 1 }, { days: 30 }, " +
            "{ relativeTo: { year: 2000, month: 1, day: 1 } }))"));

    // Calendar units without a relativeTo are still a RangeError.
    [Fact]
    public void DurationCompareCalendarUnitsNoRelativeToThrows()
        => Assert.Equal("RangeError", ErrorName("Temporal.Duration.compare({ months: 1 }, { days: 30 })"));

    // ───────────── Problems 18/23: PlainDate.until year/month/day balancing ─────────────

    // A whole month counts only when the start day-of-month fits the target month (no constraining),
    // so the residual days are measured from the day-constrained intermediate, not by stepping months
    // from an already-constrained date. Values taken from the test262 until/wrapping-at-end-of-month
    // and basic-arithmetic tests.
    [Theory]
    // Jan→Feb (non-leap 1970): only Jan 28 makes a whole month; 29/30/31 are 30/29/28 days.
    [InlineData("1970-01-28", "1970-02-28", "months", "P1M")]
    [InlineData("1970-01-29", "1970-02-28", "months", "P30D")]
    [InlineData("1970-01-30", "1970-02-28", "months", "P29D")]
    [InlineData("1970-01-31", "1970-02-28", "months", "P28D")]
    // Jan→Feb (leap 1972): Jan 29 fits Feb 29.
    [InlineData("1972-01-29", "1972-02-29", "months", "P1M")]
    [InlineData("1972-01-30", "1972-02-29", "months", "P30D")]
    // Passing through a shorter month.
    [InlineData("1970-08-30", "1970-11-30", "months", "P3M")]
    [InlineData("1970-08-31", "1970-11-30", "months", "P2M30D")]
    [InlineData("1970-01-29", "1970-03-28", "months", "P1M28D")]
    // Years.
    [InlineData("2019-12-30", "2021-07-16", "years", "P1Y6M16D")]
    [InlineData("1970-12-31", "1973-04-30", "years", "P2Y3M30D")]
    [InlineData("1970-12-31", "1973-04-30", "months", "P27M30D")]
    [InlineData("1970-01-31", "1971-05-30", "years", "P1Y3M30D")]
    public void UntilBalancesMonthsAndDays(string start, string end, string largestUnit, string expected)
        => Assert.Equal(expected, Eval(
            $"Temporal.PlainDate.from('{start}').until('{end}', {{ largestUnit: '{largestUnit}' }}).toString()"));

    // Negative (backward) differences, with expectations from the test262 basic-arithmetic cases.
    [Theory]
    [InlineData("1970-02-28", "1970-01-29", "months", "-P30D")]
    [InlineData("2021-08-17", "2021-07-16", "months", "-P1M1D")]   // negative 1 month and 1 day
    [InlineData("2021-08-13", "2021-07-16", "months", "-P28D")]    // 28 days across a 31-day month
    [InlineData("2021-09-16", "2021-07-16", "months", "-P2M")]     // negative 2 months
    public void UntilBalancesNegativeDifference(string start, string end, string largestUnit, string expected)
        => Assert.Equal(expected, Eval(
            $"Temporal.PlainDate.from('{start}').until('{end}', {{ largestUnit: '{largestUnit}' }}).toString()"));

    // ───────────── Problem 11: phased dynamic import (import.defer / import.source) parses ─────────────

    // `import.defer(specifier)` / `import.source(specifier)` are valid ImportCall syntax (here inside
    // an un-called arrow, so only parsing/compilation is exercised) and no longer raise a SyntaxError.
    [Theory]
    [InlineData("let f = () => { import.defer('./x.js'); }; 'ok'")]
    [InlineData("let f = () => { import.source('./x.js'); }; 'ok'")]
    [InlineData("let f = () => import.defer(''); 'ok'")]
    [InlineData("let f = async () => { await import.defer('./x.js'); }; 'ok'")]
    public void PhasedDynamicImportParses(string code)
        => Assert.Equal("ok", Eval(code));

    // A plain dynamic import and an unknown phase keyword are still handled as before.
    [Fact]
    public void PlainDynamicImportStillParses()
        => Assert.Equal("ok", Eval("let f = () => import('./x.js'); 'ok'"));

    [Fact]
    public void UnknownImportPhaseThrowsSyntaxError()
        => Assert.Equal("SyntaxError", ErrorName("eval(\"import.bogus('./x.js')\")"));

    // ───────────── Problems 18/23 (non-ISO): arithmetic-calendar until month/day balancing ─────────────

    // The same month-end balancing now applies to the arithmetic (fixed-month-count) non-ISO
    // calendars. Coptic Mesori (M12, 30 days) -> Pi Kogi Enavot (M13, the 5/6-day epagomenal month):
    // only the 5th makes a whole month; the 28th/29th/30th are 7/6/5 days (test262 coptic values).
    [Theory]
    [InlineData("M12", 5, "P1M")]
    [InlineData("M12", 28, "P7D")]
    [InlineData("M12", 29, "P6D")]
    [InlineData("M12", 30, "P5D")]
    public void UntilBalancesCopticMonthEnd(string startMonthCode, int startDay, string expected)
        => Assert.Equal(expected, Eval(
            $"Temporal.PlainDate.from({{ year: 1970, monthCode: '{startMonthCode}', day: {startDay}, calendar: 'coptic' }})" +
            $".until(Temporal.PlainDate.from({{ year: 1970, monthCode: 'M13', day: 5, calendar: 'coptic' }}), {{ largestUnit: 'months' }}).toString()"));

    // ───────────── Problem 2: the indian calendar's canonical era is "shaka" (not "saka") ─────────────

    [Fact]
    public void IndianEraIsShaka()
        => Assert.Equal("shaka",
            Eval("Temporal.PlainDate.from('1957-03-22').withCalendar('indian').era"));

    // The "saka" spelling is still accepted as an input alias.
    [Fact]
    public void IndianEraAcceptsSakaAlias()
        => Assert.Equal("1922",
            Eval("String(Temporal.PlainDate.from({ era: 'saka', eraYear: 1922, month: 1, day: 1, calendar: 'indian' }).eraYear)"));

    // ───────────── Problem 3 (subset): PlainYearMonth.add/subtract ISO-range validation ─────────────

    // The intermediate date (day 1 of the receiver's month) must be within the ISO date limits, so
    // adding to the minimum year-month, or subtracting past the maximum, is a RangeError.
    [Theory]
    [InlineData("new Temporal.PlainYearMonth(-271821, 4).add(new Temporal.Duration())")]
    [InlineData("new Temporal.PlainYearMonth(-271821, 4).add({ months: 1 })")]
    [InlineData("new Temporal.PlainYearMonth(275760, 9).add({ months: 1 })")]
    public void YearMonthAddOutOfRangeThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A subtraction that stays representable still works (day 1 of the max month is in range).
    [Theory]
    [InlineData("new Temporal.PlainYearMonth(275760, 9).subtract({ months: 1 }).toString()", "+275760-08")]
    [InlineData("new Temporal.PlainYearMonth(275760, 9).subtract({ years: 1 }).toString()", "+275759-09")]
    public void YearMonthSubtractStaysRepresentable(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
