# Adapter guide: `Broiler.ECMAScript.Algorithms` — the Temporal core

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/temporal.md`, and its verdict JSD-0078 in Broiler.VM's
profile, with its seven sub-areas. Milestone SJL-10.3 adds them in three parts, and each part keeps its own
sections below: part A, the records and limits, balancing (with regulation and addition), the grammar,
serialization and the options' rules those sub-areas use; part B, the calendars; and part C, the durations,
rounding, differences and their options' rules.

- **Library and version:** `Broiler.ECMAScript.Algorithms`, from the first version after `0.1.0-preview.5`,
  API baseline [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Temporal`. The records are
  SJL-8's `IsoDate`, `IsoTime` and `IsoDateTime` in `Broiler.ECMAScript.Time` (JSD-0078, decision D8).
- **Specification pins:** the Temporal proposal at `tc39/proposal-temporal@e8cc03fc` (JSD-0078, decision D1), whose
  operations each member names; the Intl era and month code proposal's Tables 1, 2, 3 and 6 and its section 4.1 at
  `5833eae`; where the pinned text and its polyfill disagree, the text rules (decision D7). SJL-4's rounding
  (`ExactRounding`, `RoundingMode`) and SJL-6's civil days (`IsoCalendarMath`) are the library's.
- **Data versions:** none in the library. The table calendars' years are SJL-8's, from the data home's calendar
  table that each engine embeds and hands in (`icu_calendar` 2.3.0 and `calendrical_calculations` 0.2.4; decision
  SJL-D-21).
- **Consumers:** listed here at the cut-overs, milestones SJL-10.4 (Broiler.VM) and SJL-10.5 (Broiler.JS);
  JSD-0078's X6 lists the members each replaces.
- **Vectors:** `conformance/vectors/temporal/`, corpus revision 24 or later (SJL-10.2). Until a runner reads
  them, the library's tests hold every row R, B, G and S of bundle `sjl-10-001`'s correspondence, and replay
  Broiler.VM's recorded answers to its calendar rows' probes and the retained calendar dataset at library level.

---

## Records, balancing, grammar and serialization (SJL-10.3, part A)

The sections from "Records and limits" to "The options' rules" are part A's.

**What no operation here does**, by convention 2: no `ToString`, `ToNumber` or `ToIntegerWithTruncation` of an
argument, no reading of an options bag or of a `Temporal` object's slots, no calendar canonicalisation, no
lookup of a named time zone, no `RangeError` or `TypeError`. The adapter does those, in the order the
specification gives, and only then calls the library.

**Failure is data** (X3). A limit, a regulation under `reject` or an addition past a limit is a
`TemporalStatus`; a string the grammar refuses is a `TemporalParseError`; an option value off its rule is a
`false`. Each engine maps each to its own `RangeError` at the specification's moment, with its own message
(JSD-0054 section 5). A library exception is a broken precondition, listed under each operation; the
preconditions lie far outside every Temporal limit, so an adapter that checks the limits first never meets one.

**Charges** (X3): the grammar's members whose work grows with their input take the caller's `WorkCharge` and
charge the text's length in UTF-16 code units, once, before any character is read, when the text is not empty.
Both engines pass `null` today, as they charge nothing for Temporal now: no charge moves, and there is no class
C2 in SJL-10's rules. Every other member's work is bounded by the limits.

---

## Records and limits — `TemporalLimits`

### Signature

```csharp
public enum TemporalStatus { Completed, OutOfRange, InvalidDate, InvalidTime, InvalidDuration }
public enum TemporalUnit { Year, Month, Week, Day, Hour, Minute, Second, Millisecond, Microsecond, Nanosecond, Unset, Auto }
public enum TemporalOverflow { Constrain, Reject }
public readonly record struct DateDuration(double Years, double Months, double Weeks, double Days);
public readonly record struct InternalDuration(DateDuration Date, BigInteger TimeNanoseconds);
public readonly record struct DurationFields(double Years, …, double Nanoseconds);   // Table 7's ten fields

public static class TemporalLimits
{
    public const long NanosecondsPerDay, MinYear = -271821, MaxYear = 275760, MaxEpochDays = 100_000_000;
    public const double CalendarDurationLimit;               // 2^32
    public static readonly BigInteger MaxTimeDuration;       // 2^53 × 10^9 - 1
    public static TemporalStatus CheckIsoDate(IsoDate date);              // ISODateWithinLimits
    public static TemporalStatus CheckIsoDateTime(IsoDateTime dateTime);  // ISODateTimeWithinLimits
    public static TemporalStatus CheckIsoYearMonth(IsoDate yearMonth);    // ISOYearMonthWithinLimits
    public static TemporalStatus CheckIsoDaysRange(IsoDate date);         // CheckISODaysRange
    public static TemporalStatus CheckEpochNanoseconds(BigInteger ns);    // IsValidEpochNanoseconds
    public static TemporalStatus CheckDuration(DurationFields duration);  // IsValidDuration
    public static bool IsValidTime(IsoTime time);                         // IsValidTime
}
```

The instant limits are SJL-8's `ZonedTime.MinEpochNanoseconds` and `MaxEpochNanoseconds`. A year-month and a
month-day are `IsoDate`s whose day or year is the reference one, as the proposal's slots hold them.

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | each constructor, `ToIntegerWithTruncation` | converts each argument (SJL-4's `IntegerConversions.ToIntegerOrInfinity` after its own finite check), and narrows a year to `long` saturating: a year past `long`'s range is out of range either way |
| 2 | `IsValidISODate`, `IsValidTime` | nothing more: each check answers `InvalidDate` or `InvalidTime` for a record that is not one |

### Preconditions

None. **No check throws, for any record** (decision D9): a year outside `MinYear` to `MaxYear` is
`OutOfRange` before any civil-day arithmetic.

### What the caller does with each status

| Status | Meaning | The specification's error | Both engines |
|---|---|---|---|
| `Completed` | within the limit | — | create the object |
| `OutOfRange` | outside the limit | `RangeError` | `RangeError` |
| `InvalidDate`, `InvalidTime` | not a date or a time | `RangeError` (the constructor's `IsValidISODate` or `IsValidTime`) | `RangeError` |
| `InvalidDuration` | mixed signs, not finite, or too large | `RangeError` (`CreateTemporalDuration`) | `RangeError` |

### Engine-specific behaviour

**Broiler.VM's correction R6** is made by calling these checks: `new PlainDate` with a year of 2^53 + 2 or
±10^20, and `new PlainDateTime` and `new PlainMonthDay`'s reference year of 10^20, now answer `OutOfRange`, its
`RangeError`, where the shared epoch-day precondition ended the invocation. Broiler.JS's `int` years saturate
and answer as before (R6).

---

## Balancing, regulation and addition — `IsoArithmetic`

### Signature

```csharp
public static class IsoArithmetic
{
    public static int CompareIsoDate(IsoDate one, IsoDate two);                    // CompareISODate
    public static int CompareTime(IsoTime one, IsoTime two);                       // CompareTimeRecord
    public static int CompareIsoDateTime(IsoDateTime one, IsoDateTime two);        // CompareISODateTime
    public static long TimeToNanoseconds(IsoTime time);
    public static IsoDate BalanceIsoDate(long year, long month, long day);
    public static IsoDate AddDaysToIsoDate(IsoDate date, long days);               // AddDaysToISODate
    public static IsoTime BalanceTime(BigInteger nanoseconds, out long days);      // BalanceTime
    public static IsoTime BalanceTime(double hour, …, double nanosecond, out long days);
    public static IsoTime AddTime(IsoTime time, BigInteger timeDuration, out long days);   // AddTime
    public static BigInteger DifferenceTime(IsoTime one, IsoTime two);             // DifferenceTime
    public static TemporalStatus RegulateIsoDate(long year, double month, double day, TemporalOverflow overflow, out IsoDate result);
    public static TemporalStatus RegulateTime(double hour, …, double nanosecond, TemporalOverflow overflow, out IsoTime result);
    public static TemporalStatus AddIsoDate(IsoDate date, DateDuration duration, TemporalOverflow overflow, out IsoDate result);  // CalendarDateAdd, iso8601
    public static BigInteger GetUtcEpochNanoseconds(IsoDateTime dateTime);
    public static IsoDateTime GetIsoPartsFromEpoch(BigInteger epochNanoseconds);
    public static IsoDateTime BalanceIsoDateTime(IsoDate date, BigInteger timeNanoseconds);
    public static IsoTime RoundTime(IsoTime time, long increment, TemporalUnit unit, RoundingMode mode, out long days);
    public static IsoDateTime RoundIsoDateTime(IsoDateTime dateTime, long increment, TemporalUnit unit, RoundingMode mode);
    public static BigInteger RoundTemporalInstant(BigInteger epochNanoseconds, long increment, TemporalUnit unit, RoundingMode mode);
    public static TemporalStatus AddInstant(BigInteger epochNanoseconds, BigInteger timeDuration, out BigInteger result);
}
```

**A time balanced past midnight answers its days beside the `IsoTime`** (decision D8), as the `out` argument.
The wall clock of an instant in a zone is `GetIsoPartsFromEpoch(epochNanoseconds + offset)`, the offset from
SJL-8's `ZonedTime.GetOffsetNanosecondsFor`.

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | `GetTemporalOverflowOption` | reads `overflow` with `Get` and `ToString`, and maps it with `TemporalOptionRules.TryParseOverflow`; refuses with `RangeError` where it answers `false` |
| 2 | `ToTemporalPartialDurationRecord`, `ToPositiveIntegerWithTruncation` | reads and converts the fields; refuses a month or day below 1 before `RegulateIsoDate` |
| 3 | the methods' own refusals | `PlainYearMonth` refuses weeks, days and time units; `Instant` refuses date units; each passes the ISO date of a year-month's first day |
| 4 | `ValidateTemporalRoundingIncrement` | checks the increment for the unit with `TemporalOptionRules.IsValidRoundingIncrement` before rounding |

The methods compose the operations in their steps' order: `AddDurationToDateTime` is `AddTime`, then
`AddIsoDate` with the carried days, then `CheckIsoDateTime`; `AddZonedDateTime` is the wall clock,
`AddIsoDate`, `CheckIsoDateTime`, SJL-8's `GetEpochNanosecondsFor` and `AddInstant`; `PlainDate` plus time
units adds the time duration's whole days, truncated toward zero.

### Preconditions

- A time is valid (`TemporalLimits.IsValidTime`); a date handed to `AddIsoDate` is a valid ISO date.
- The fields of `RegulateIsoDate`, `RegulateTime` and `BalanceTime` are integers and not NaN; a date duration's
  fields are finite.
- A rounding increment is positive and the unit a time unit or `Day`; the overflow is one of the two.
- A day count is at most 2^60 in magnitude, and a balanced year at most 2^53: far past every limit.

### What the caller does with each status

| Status | From | The specification's error | Both engines |
|---|---|---|---|
| `Completed` | every member | — | continue |
| `InvalidDate` | `RegulateIsoDate`, `AddIsoDate` under `Reject` | `RangeError` | `RangeError` |
| `InvalidTime` | `RegulateTime` under `Reject` | `RangeError` | `RangeError` |
| `OutOfRange` | `AddIsoDate`, `AddInstant` | `RangeError` | `RangeError` |

`AddIsoDate` refuses years beyond 10^9, months or weeks beyond 10^10 and days beyond 10^11 first, as
Broiler.VM did: no result past them is within the limits, so the answer is the specification's.

### Engine-specific behaviour

None. Broiler.JS's corrections B5 (a year-month at the lower limit, answered by `AddIsoDate` on the month's first
day) and R7 (narrowing that wrapped) are made by calling these members with `long` years and `double` fields.

---

## The grammar — `TemporalGrammar`

### Signature

```csharp
public enum TemporalGoal { DateTime, ZonedDateTime, Instant, Time, YearMonth, MonthDay }
public enum TemporalParseError { None, Malformed, CriticalCalendarConflict, UnknownCriticalAnnotation,
    YearMonthOutsideIsoCalendar, MonthDayOutsideIsoCalendar, OutOfRange, NoTimeZone, SubMinuteTimeZoneOffset }
public readonly record struct TemporalAnnotation(bool Critical, string Key, string Value);
public readonly record struct TimeZoneIdentifierRecord(string? Name, int OffsetMinutes);
public sealed class IsoParseResult { Goal, Year, Month, Day, HasDay, Time, Z, Offset, OffsetNanoseconds,
    OffsetHasSeconds, TimeZone, Calendar, Annotations }

public static class TemporalGrammar
{
    public static bool TryParse(ReadOnlySpan<char> text, ReadOnlySpan<TemporalGoal> goals,
        out IsoParseResult? result, out TemporalParseError error, WorkCharge? charge = null);  // ParseISODateTime
    public static bool TryParseInstant(ReadOnlySpan<char> text, out BigInteger epochNanoseconds,
        out TemporalParseError error, WorkCharge? charge = null);                               // ParseTemporalInstant
    public static bool TryParseTimeZoneString(ReadOnlySpan<char> text, out TimeZoneIdentifierRecord zone,
        out TemporalParseError error, WorkCharge? charge = null);                               // ParseTemporalTimeZoneString
    public static bool TryParseTimeZoneIdentifier(ReadOnlySpan<char> text, out TimeZoneIdentifierRecord zone,
        WorkCharge? charge = null);                                                             // ParseTimeZoneIdentifier
    public static bool TryParseCalendarString(ReadOnlySpan<char> text, out string? calendar,
        out TemporalParseError error, WorkCharge? charge = null);                               // ParseTemporalCalendarString
    public static bool TryParseOffsetString(ReadOnlySpan<char> text, out long offsetNanoseconds);  // ToOffsetString's test
    public static bool TryParseDuration(ReadOnlySpan<char> text, out DurationFields duration,
        out TemporalParseError error, WorkCharge? charge = null);                               // ParseTemporalDurationString
    public static bool IsTimeZoneIdentifier(ReadOnlySpan<char> text, WorkCharge? charge = null);
    public static bool IsUtcOffset(ReadOnlySpan<char> text, bool allowSubMinute);
    public static bool IsAnnotationValue(ReadOnlySpan<char> text, WorkCharge? charge = null);
}
```

| Specification goal list | `goals` |
|---|---|
| `PlainDate.from`, `PlainDateTime.from` | `[DateTime]` |
| `PlainTime.from` | `[Time]` |
| `PlainYearMonth.from` | `[YearMonth]` |
| `PlainMonthDay.from` | `[MonthDay]` |
| `ZonedDateTime.from` | `[ZonedDateTime]` |
| `Instant.from` | `TryParseInstant` |
| `relativeTo` | `[ZonedDateTime, DateTime]` |

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | each `ToTemporal…`, `ToTemporalTimeZoneIdentifier`, `ToTemporalCalendarIdentifier` | classifies the argument and requires a String, with its own `TypeError` |
| 2 | `ToOffsetString` | `ToPrimitive` with hint string, and a `TypeError` for a non-String |

**After the grammar** the adapter canonicalises the calendar (`CanonicalizeCalendar`, the calendars sub-area),
looks a time zone `Name` up in its database (`GetAvailableNamedTimeZoneIdentifier`), applies the limits, and
only then reads options: the grammar takes none, so every refusal of the string precedes every option read
(row G24).

### Preconditions

Every goal is one of the enumeration's. Nothing else: any text is accepted or refused.

### What the caller does with each error

Every `TemporalParseError` is the specification's `RangeError` in both engines. The codes name the step:
`Malformed` (no goal matched, not a duration, not a calendar string), `CriticalCalendarConflict` and
`UnknownCriticalAnnotation` (`ParseISODateTime`'s annotation steps), `YearMonthOutsideIsoCalendar` and
`MonthDayOutsideIsoCalendar`, `OutOfRange` (`ParseTemporalInstant`), `NoTimeZone` and `SubMinuteTimeZoneOffset`
(`ParseTemporalTimeZoneString`). `TryParseOffsetString`'s `false` is `ToOffsetString`'s `RangeError`.

### Engine-specific behaviour

None. The grammar is Broiler.VM's, moved whole; a differential check against that engine's `JsTemporalParser`
over every probe string of the correspondence and some two and a half million generated ones found no
difference. **Broiler.JS's corrections** G3, G4, G6, G8 (the four plain types), G9, G11, G12, G13, G16, G17,
G18, G19, G20 and G22 are made by calling it in place of its regular expressions.

---

## Serialization — `TemporalFormat`

### Signature

```csharp
public static class TemporalFormat
{
    public const int AutoPrecision = -1, MinutePrecision = -2;
    public static string PadIsoYear(long year);
    public static string FormatFractionalSeconds(int subSecondNanoseconds, int precision);
    public static string FormatTimeString(int hour, int minute, int second, int subSecondNanoseconds, int precision);
    public static string TimeToString(IsoTime time, int precision);                                    // TimeRecordToString
    public static string FormatCalendarAnnotation(string calendar, ShowCalendarName show);
    public static string DateToString(IsoDate date, string calendar, ShowCalendarName show);           // TemporalDateToString
    public static string DateTimeToString(IsoDateTime dateTime, string calendar, int precision, ShowCalendarName show);  // ISODateTimeToString
    public static string YearMonthToString(IsoDate isoDate, string calendar, ShowCalendarName show);
    public static string MonthDayToString(IsoDate isoDate, string calendar, ShowCalendarName show);
    public static string InstantToString(BigInteger epochNanoseconds, long? offsetNanoseconds, int precision);
    public static string ZonedDateTimeToString(BigInteger epochNanoseconds, long offsetNanoseconds, string timeZone,
        string calendar, int precision, ShowCalendarName showCalendar, ShowTimeZoneName showTimeZone, ShowOffset showOffset);
    public static string DurationToString(DurationFields duration, int precision);                    // TemporalDurationToString
    public static string FormatUtcOffsetNanoseconds(long offsetNanoseconds);                           // the offset getter
    public static string FormatDateTimeUtcOffsetRounded(long offsetNanoseconds);                       // a string's offset
    public static string PlainTimeToString(IsoTime time, SecondsStringPrecision precision, RoundingMode mode);
    public static TemporalStatus TryPlainDateTimeToString(IsoDateTime dateTime, string calendar,
        SecondsStringPrecision precision, RoundingMode mode, ShowCalendarName show, out string? text);
    public static TemporalStatus TryDurationToString(DurationFields duration, SecondsStringPrecision precision,
        RoundingMode mode, out string? text);
}
```

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | `GetOptionsObject` | a `TypeError` for an options argument that is neither undefined nor an object (S3) |
| 2 | each `toString` | reads its options **in alphabetical order** - `calendarName`, `fractionalSecondDigits`, `offset`, `roundingMode`, `smallestUnit`, `timeZone`, `timeZoneName` as the method has them - each validated by its rule as it is read (S11) |
| 3 | `ValidateTemporalUnitValue` | after the last read: `TemporalOptionRules.IsValidToStringSmallestUnit` (S5) |
| 4 | `ToSecondsStringPrecisionRecord` | `TemporalOptionRules.ToSecondsStringPrecisionRecord` |
| 5 | `Instant` and `ZonedDateTime` | rounds the instant with `IsoArithmetic.RoundTemporalInstant`, then reads the offset in force at the **rounded** instant from its zone (`timeZone` resolved through `TemporalGrammar.TryParseTimeZoneString` for an `Instant`) |

`toJSON` is `toString` with every default: `AutoPrecision` and `ShowCalendarName.Auto`.

### Preconditions

A time is valid; a precision is in its domain (0 to 9, `AutoPrecision`, and `MinutePrecision` where a time is
written); an offset's magnitude is below a day; a duration is valid; identifiers are not null.

### What the caller does with each status

| Status | From | The specification's error | Both engines |
|---|---|---|---|
| `Completed` | both | — | return the string |
| `OutOfRange` | `TryPlainDateTimeToString` (the rounded date-time past the limits), `TryDurationToString` (the rounded time past `maxTimeDuration`) | `RangeError` | `RangeError` |
| `InvalidDuration` | `TryDurationToString` (the balanced duration not valid) | `RangeError` | `RangeError` |

### Engine-specific behaviour

None. Broiler.JS's per-type formatters and its five `calendarName` readers become calls to these members and
to the rules below; its offset getter's dropped fraction of a second (S8) is code no input reaches.

---

## The options' rules — `TemporalOptionRules`

```csharp
public enum ShowCalendarName { Auto, Always, Never, Critical }
public enum ShowTimeZoneName { Auto, Never, Critical }
public enum ShowOffset { Auto, Never }
public readonly record struct SecondsStringPrecision(int Precision, TemporalUnit Unit, long Increment);

public static partial class TemporalOptionRules
{
    public const TemporalOverflow DefaultOverflow = TemporalOverflow.Constrain;
    public const RoundingMode DefaultToStringRoundingMode = RoundingMode.Trunc;
    public static bool TryParseOverflow(ReadOnlySpan<char> value, out TemporalOverflow overflow);
    public static bool TryParseShowCalendarName(ReadOnlySpan<char> value, out ShowCalendarName show);
    public static bool TryParseShowTimeZoneName(ReadOnlySpan<char> value, out ShowTimeZoneName show);
    public static bool TryParseShowOffset(ReadOnlySpan<char> value, out ShowOffset show);
    public static bool TryParseFractionalSecondDigits(double value, out int digits);   // a Number
    public static bool IsFractionalSecondDigitsAuto(ReadOnlySpan<char> value);          // anything else, ToString'd
    public static bool TryParseUnit(ReadOnlySpan<char> value, out TemporalUnit unit);   // GetTemporalUnitValuedOption
    public static bool IsValidToStringSmallestUnit(TemporalUnit smallestUnit, bool duration);
    public static SecondsStringPrecision ToSecondsStringPrecisionRecord(TemporalUnit smallestUnit, int fractionalSecondDigits);
    public static bool IsValidRoundingIncrement(long increment, long dividend, bool inclusive);  // ValidateTemporalRoundingIncrement
}
```

Each string is compared exactly, after the engine's `ToString`; a `false` is the specification's `RangeError`.
An undefined option is the default: `Constrain`, `Auto`, `AutoPrecision`, `Unset` and `Trunc`. The
`roundingMode` names are SJL-4's `ExactRounding.TryParseRoundingMode`. The class is partial: part C adds the
rules of the rounding and difference options.

---

## Calendars (SJL-10.3, part B)

The calendars of JSD-0078's calendar sub-area: the identifiers of Table 1; the formula calendars (`gregory` and its
era variants `buddhist`, `roc` and `japanese`, `coptic`, `ethiopic` and `ethioaa`, `indian`, `islamic-civil` and
`islamic-tbla`, and `hebrew`); the table calendars over SJL-8's `CalendarYears` (`chinese`, `dangi`,
`islamic-umalqura` and `persian`); month codes (Table 3), eras (Table 2) and the month-day reference years (Table 6);
and the calendar operations of Temporal s12.3 and the era proposal's section 4.1 over them. Ported from Broiler.VM's
`JsCalendars`, the core of `JsTemporal.Calendar` and `JsTemporal.CalendarNonIso`, with their behaviour unchanged:
the tests hold every line of the calendar rows C1 to C18 the library can answer to Broiler.VM's recorded answer.

**What no operation here does**, by convention 2: no property read (PrepareCalendarFields' `Get`s and conversions),
no `ToPrimitive` of a month code, no option read (`overflow`), no reading of a Temporal object's `[[Calendar]]` slot
(ToTemporalCalendarIdentifier, s12.3.10), no parse of an ISO string or its annotations (ParseTemporalCalendarString,
which is the grammar's), no `RangeError` and no `TypeError`. The adapter does those, in the order the specification
gives, and only then calls the library.

**What stays in the engines** (JSD-0078's X6): the Temporal objects and their slots; `PrepareFields`,
`CalendarWithIsoDefault`, `ToCalendarIdentifier` and `CalendarOf` in Broiler.VM, and their counterparts in
Broiler.JS; calendar identifiers as engine strings; the mapping of each status to an error, and its message.

**Failure is data** (X3): an unknown identifier, an invalid month code or era, a missing field, a field beyond the
calendar's under `reject`, and a date outside a calendar or the limits each answer a `CalendarStatus`, in the order
Broiler.VM tests them, which is the specification's. A library exception is a broken precondition, listed under each
operation.

**Charges**: none. Each member's work is bounded by the limits, not by its input beyond an identifier's length, which
`Canonicalize` bounds: a year is a few multiplications or a table lookup, a difference's years and months start from
estimates of their counts, and a reference date reads at most the years 1900 to 2035 (X3: there is no class C2).

### The types

```csharp
namespace Broiler.ECMAScript.Temporal;

public enum CalendarId { Iso8601, Buddhist, Chinese, Coptic, Dangi, Ethioaa, Ethiopic, Gregory, Hebrew, Indian,
                         IslamicCivil, IslamicTbla, IslamicUmalqura, Japanese, Persian, Roc }
public enum CalendarStatus { Completed, UnknownCalendar, InvalidMonthCode, MissingYear, MissingMonth, MissingDay,
                             IncompleteEra, UnknownEra, EraYearMismatch, MonthCodeMismatch, MonthNotInYear,
                             FieldOutOfRange, NoReferenceDate, OutOfRange }
public enum CalendarOverflow { Constrain, Reject }
public enum CalendarUnit { Year, Month, Week, Day }
public enum CalendarFieldsType { Date, YearMonth, MonthDay }

public readonly record struct MonthCode
{
    public MonthCode(int number, bool isLeapMonth);
    public int Number { get; }
    public bool IsLeapMonth { get; }
    public static CalendarStatus Parse(ReadOnlySpan<char> text, out MonthCode monthCode);
    public override string ToString();                       // "M01" to "M99", "M05L"
}

public readonly record struct CalendarFields                // each null when unset
{
    public string? Era { get; init; }
    public double? EraYear { get; init; }
    public double? Year { get; init; }
    public double? Month { get; init; }
    public MonthCode? MonthCode { get; init; }
    public double? Day { get; init; }
}

public readonly record struct CalendarDate(string? Era, long? EraYear, long Year, int Month, MonthCode MonthCode,
    int Day, int DayOfWeek, int DayOfYear, int? WeekOfYear, long? YearOfWeek, int DaysInMonth, int DaysInYear,
    int MonthsInYear, bool InLeapYear)
{
    public int DaysInWeek { get; }                           // 7
}

public readonly record struct CalendarDuration(long Years, long Months, long Weeks, long Days);
```

`CalendarId` holds Table 1's sixteen calendar types and no alias. `CalendarFields` is the calendar half of a
Calendar Fields Record (s12.3.2): the time, offset and time zone fields stay the engine's, and no calendar operation
reads them. `CalendarDate` is the Calendar Date Record (s12.3.1) every getter reads; `WeekOfYear` and `YearOfWeek`
are `null` outside ISO 8601, and `Era` and `EraYear` in `iso8601`, `chinese` and `dangi`.

### `TemporalCalendars` — the operations

```csharp
public sealed class TemporalCalendars : ICalendarArithmetic
{
    public TemporalCalendars(ICalendarTables tables);

    // Identifiers and fields: no table needed.
    public static CalendarStatus Canonicalize(ReadOnlySpan<char> identifier, out CalendarId calendar);
    public static string Identifier(CalendarId calendar);
    public static bool SupportsEra(CalendarId calendar);
    public static bool IsTypeError(CalendarStatus status);
    public static CalendarFields MergeFields(CalendarId calendar, CalendarFields fields, CalendarFields additional);

    // A date's calendar fields, and dates from fields.
    public CalendarStatus IsoToDate(CalendarId calendar, IsoDate date, out CalendarDate result);
    public CalendarStatus DateToFields(CalendarId calendar, IsoDate date, CalendarFieldsType type, out CalendarFields result);
    public CalendarStatus DateFromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow, out IsoDate result);
    public CalendarStatus YearMonthFromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow, out IsoDate result);
    public CalendarStatus MonthDayFromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow, out IsoDate result);

    // Arithmetic (ICalendarArithmetic).
    public CalendarStatus DateAdd(CalendarId calendar, IsoDate date, CalendarDuration duration, CalendarOverflow overflow, out IsoDate result);
    public CalendarStatus DateUntil(CalendarId calendar, IsoDate one, IsoDate two, CalendarUnit largestUnit, out CalendarDuration result);

    // The conversions' calendar steps.
    public CalendarStatus DateToYearMonth(CalendarId calendar, IsoDate date, out IsoDate result);
    public CalendarStatus DateToMonthDay(CalendarId calendar, IsoDate date, out IsoDate result);
    public CalendarStatus MonthDayToDate(CalendarId calendar, IsoDate monthDay, CalendarFields fields, out IsoDate result);
    public CalendarStatus YearMonthToDate(CalendarId calendar, IsoDate yearMonth, CalendarFields fields, out IsoDate result);
}
```

An engine makes **one instance per set of calendar tables**, as it makes one `TimeZoneDatabase`, over SJL-8's
`CalendarTables` and the bytes it embeds, and keeps it for its realm or its process: the instance holds the
fifteen calendar systems, is immutable, and is safe to call concurrently. Broiler.VM's `engine.Intl.Calendars`
becomes it; Temporal is admitted there only with Intl, as today (JSD-0054).

| Member | Operation | Rows |
|---|---|---|
| `Canonicalize` | CanonicalizeCalendar (s12.1.1) over Table 1's sixteen types and its two aliases, `ethiopic-amete-alem` and `islamicc`, ASCII case folded and nothing else: `gregorian`, `minguo`, `islamic`, `islamic-rgsa`, `İso8601` and a Cyrillic `а` are `UnknownCalendar` | C1, C2, C3 |
| `Identifier` | the canonical type, a Temporal object's `calendarId` | C1, C13 |
| `SupportsEra` | CalendarSupportsEra (era proposal 4.1.1): whether CalendarExtraFields (s12.3.27) adds `era` and `eraYear` to the fields read with `year` | C8 |
| `IsTypeError` | whether a status is the specification's `TypeError` (a missing field) rather than its `RangeError` | |
| `MonthCode.Parse` | ParseMonthCode (s12.2.1), the `MonthCode` production with ASCII digits only: `M00` refused, `M00L` admitted, `M٠١` refused | C4, C6 |
| `MergeFields` | CalendarMergeFields (s12.3.5) with CalendarFieldKeysToIgnore (s12.3.29) and NonISOFieldKeysToIgnore (4.1.23) | C15 to C17 |
| `IsoToDate` | CalendarISOToDate (s12.3.26), NonISOCalendarISOToDate (4.1.14) | C8, C14, C18 |
| `DateToFields` | ISODateToFields (s13.42) | C15 to C17 |
| `DateFromFields` | CalendarDateFromFields (s12.3.12): CalendarResolveFields (s12.3.31) or NonISOResolveFields (4.1.24), then CalendarDateToISO (s12.3.22) or NonISOCalendarDateToISO (4.1.20), then ISODateWithinLimits | C4 to C9, C11, C14 |
| `YearMonthFromFields` | CalendarYearMonthFromFields (s12.3.13): the day set to 1, then ISOYearMonthWithinLimits | C15 |
| `MonthDayFromFields` | CalendarMonthDayFromFields (s12.3.14) with CalendarMonthDayToISOReferenceDate (s12.3.24) or NonISOMonthDayToISOReferenceDate (4.1.21) and Table 6 | C10, C12 |
| `DateAdd` | CalendarDateAdd (s12.3.7), NonISODateAdd (4.1.18) | C18 |
| `DateUntil` | CalendarDateUntil (s12.3.9), NonISODateUntil (4.1.19) | C18 |
| `DateToYearMonth` | `PlainDate.prototype.toPlainYearMonth`'s calendar steps: ISODateToFields as a date, CalendarYearMonthFromFields under `constrain` | C15 |
| `DateToMonthDay` | `PlainDate.prototype.toPlainMonthDay`'s calendar steps: ISODateToFields as a date, CalendarMonthDayFromFields under `constrain` | C16 |
| `MonthDayToDate` | `PlainMonthDay.prototype.toPlainDate`'s calendar steps: ISODateToFields as a month-day, CalendarMergeFields, CalendarDateFromFields under `constrain` | C17 |
| `YearMonthToDate` | `PlainYearMonth.prototype.toPlainDate`'s calendar steps: ISODateToFields as a year-month, CalendarMergeFields, CalendarDateFromFields under `constrain` | |

**Reference dates.** An ISO 8601 month-day's reference year is 1972; the month and day are regulated against the
year the fields give, and a year further than 10^9 from the epoch is first reduced modulo 400, which keeps its leap
year (row C12: `{year: 2147483652, month: 2, day: 29}` is `02-29`). In `chinese` and `dangi` the reference year is
Table 6's, its dash a `NoReferenceDate` under `reject` and the common month under `constrain`, and `M11L` splits at
day 10 (2033, then 2034). In every other calendar it is the latest ISO date from 1900 to 1972 with the month code and
day, else the earliest from 1973 to 2035 (row C10). A year-month's reference date is the first day of the calendar
month (row C15).

### What the caller must have done first

| Built-in | What the adapter does before the library |
|---|---|
| A calendar argument (`ToTemporalCalendarIdentifier`, s12.3.10) | a Temporal object with a calendar answers its own `[[Calendar]]`, the `CalendarId` it holds, and no library call (row C13: a `PlainMonthDay` lends its calendar, not `iso8601`); a value that is not a string is a `TypeError`; a string goes through the grammar's ParseTemporalCalendarString, then `Canonicalize` |
| `from` and `with` of `PlainDate`, `PlainDateTime`, `PlainYearMonth`, `PlainMonthDay`, `ZonedDateTime` | PrepareCalendarFields (s12.3.3) in property order: `day`, `era`, `eraYear`, `month`, `monthCode`, `year` (the era fields only where `SupportsEra` and `year` is read), each converted by Table 19's conversion: `era` by `ToString`; `eraYear` and `year` by `ToIntegerWithTruncation`; `month` and `day` by `ToPositiveIntegerWithTruncation`; `monthCode` by `ToPrimitive`, a `TypeError` for a non-string, then `MonthCode.Parse` at the read, its `InvalidMonthCode` the `RangeError`. Its own `TypeError`s for a required field or an empty partial bag. Then the `overflow` option. Then `DateFromFields`, `YearMonthFromFields` or `MonthDayFromFields`; for `with`, `DateToFields` and `MergeFields` first |
| A getter (`year`, `monthCode`, `era`, `daysInMonth`, …) | the receiver check; then `IsoToDate`, and the record's field |
| `PlainDate.prototype.toPlainYearMonth`, `toPlainMonthDay` | the receiver check; `DateToYearMonth` or `DateToMonthDay`; the result keeps the date's calendar, the Gregorian family's included (row C16) |
| `PlainMonthDay.prototype.toPlainDate`, `PlainYearMonth.prototype.toPlainDate` | the receiver and argument checks; PrepareCalendarFields of the argument (`year`, with `era` and `eraYear` where `SupportsEra`; or `day`); then `MonthDayToDate` or `YearMonthToDate`. The year is the calendar's, never an ISO year (row C17) |
| `PlainYearMonth.prototype.add` and `subtract` (AddDurationToYearMonth, s9.5.8) | the duration and its checks; `DateToFields` as a year-month, its `Day` set to 1, `DateFromFields` under `Constrain`; `DateAdd` under the option's overflow; `DateToFields` as a year-month and `YearMonthFromFields` under it |
| `PlainYearMonth.prototype.until` and `since` (s9.5.7) | each year-month's first day as for `add`, then `DateUntil`, then the rounding the durations sub-area does |
| Every `until`, `since`, `add`, `subtract` and relative rounding | through `ICalendarArithmetic`, below |

### Preconditions

- `CalendarFields` hold what their conversions give: `Year` and `EraYear` finite and integral, of any magnitude;
  `Month` and `Day` finite, integral and at least 1; a `MonthCode` that `MonthCode.Parse` gave. Otherwise
  `ArgumentOutOfRangeException`.
- An `IsoDate` passed in has a month from 1 to 12 and a day of that month; otherwise `ArgumentOutOfRangeException`.
  Its year may be anything: outside -271821 to 275760 the operation answers `OutOfRange`.
- Each enumeration value is a member; otherwise `ArgumentOutOfRangeException`. The tables are not `null`.
- The library throws `InvalidOperationException` only where a year past the calendar year sources' bound would be
  read for dates within the limits, which no input reaches (JSD-0078: "the shared code asserts there").

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | the result is valid | — | the value | the value |
| `UnknownCalendar` | no calendar type or alias of Table 1 | `RangeError` | its `RangeError` | `RangeError`, "the calendar … is not supported" |
| `InvalidMonthCode` | not the `MonthCode` grammar, or not the calendar's (Table 3; ISO 8601's `M01` to `M12`) | `RangeError` | its `RangeError` (row C5's correction: no longer constrained) | `RangeError`, "… is not a month code" or "the … calendar has no month code …" |
| `MissingYear` | no `year`, nor an era and era year | `TypeError` | its `TypeError` | `TypeError`, "the year property, or era and eraYear, is required" |
| `MissingMonth` | neither `month` nor `monthCode` | `TypeError` | its `TypeError` | `TypeError`, "the month or monthCode property is required" |
| `MissingDay` | no `day` where one is needed | `TypeError` | its `TypeError` | `TypeError`, "the day property is required" |
| `IncompleteEra` | an era without its year, or the reverse | `TypeError` | its `TypeError` | `TypeError`, "era and eraYear must be given together" |
| `UnknownEra` | not one of the calendar's Table 2 codes or aliases, matched exactly | `RangeError` | its `RangeError` (row C9's correction, decision D4) | `RangeError`, "… is not an era of the … calendar" |
| `EraYearMismatch` | `year` and the era's year disagree | `RangeError` | its `RangeError` | `RangeError`, "the year and the era year disagree" |
| `MonthCodeMismatch` | `month` and `monthCode` disagree | `RangeError` | its `RangeError` | `RangeError`, "month and monthCode disagree" |
| `MonthNotInYear` | `reject`, and the year lacks the leap month | `RangeError` | its `RangeError` | `RangeError`, "the year … has no month …" |
| `FieldOutOfRange` | `reject`, and the month or day is beyond the calendar's | `RangeError` | its `RangeError` | `RangeError`, "the month … is out of range" or "… is not a date" |
| `NoReferenceDate` | a month-day no reference year holds | `RangeError` | its `RangeError` | `RangeError`, "no year of the … calendar has …" |
| `OutOfRange` | a result or a year outside the limits | `RangeError` | its `RangeError` | `RangeError`, "… is outside the representable range" |

### Message identifiers

None: the statuses are the identifiers, and each engine keeps its own text (JSD-0054 section 5).

### `ICalendarArithmetic` — the interface the durations sub-area calls

```csharp
public interface ICalendarArithmetic
{
    CalendarStatus DateAdd(CalendarId calendar, IsoDate date, CalendarDuration duration, CalendarOverflow overflow, out IsoDate result);
    CalendarStatus DateUntil(CalendarId calendar, IsoDate one, IsoDate two, CalendarUnit largestUnit, out CalendarDuration result);
}
```

This is the area design's `ICalendarArithmetic`, the calendar provider of the duration arithmetic, the relative
rounding (nudging and bubbling) and the differences of SJL-10.3's part C. `TemporalCalendars` implements it for every
calendar of `CalendarId`, ISO 8601 included, so part C has one path for all of them, as Broiler.VM's
`RoundRelativeDuration` has. Part C, or an engine's adapter, calls:

- **`DateAdd`** wherever the proposal calls CalendarDateAdd: `PlainDate`, `PlainDateTime` and `ZonedDateTime`
  addition, `PlainYearMonth` addition (above), `Duration.prototype.round` and `total` relative to a date, and the
  nudge windows. The duration is the date part of a valid duration, each field converted exactly from its double; the
  weeks and days are added after the years and months, in days.
  - Years beyond 10^9, months or weeks beyond 10^10 and days beyond 10^11 are `OutOfRange` before any arithmetic, as
    Broiler.VM refuses them; no result within the limits is that far from a date within them.
  - Under `Reject`, a day the target month lacks is `FieldOutOfRange`, and a leap month the target year lacks is
    `MonthNotInYear`. A result outside ISODateWithinLimits is `OutOfRange`.
- **`DateUntil`** wherever the proposal calls CalendarDateUntil: the differences of `PlainDate`, `PlainDateTime`,
  `PlainYearMonth` and `ZonedDateTime`, and the relative rounding.
  - The years and months are the largest that do not pass the second date, counted from estimates rather than one at
    a time, with the weeks and days a subtraction from the regulated intermediate date. Every field has one sign.
  - Equal dates answer zero, and `since` is the engine's negation of `until`.
  - The tests hold that adding the result to the first date under `Constrain` gives the second, in every calendar.

The interface takes the calendar's own types: `CalendarDuration` is exact integers, `CalendarOverflow` the option's two
values and `CalendarUnit` the four date units. Part C's records convert to them field by field.

### Evidence

- `Broiler.ECMAScript.Algorithms.Tests/Temporal/CalendarCorrespondenceTests.cs` replays Broiler.VM's answers to the
  calendar rows' probes (correspondence `sjl-10-001`, `correspondence-runs/syntax/c01` to `c10`, copied by their
  digests into `Temporal/Correspondence/`): 4,416 lines computed at library level, every one Broiler.VM's answer, and
  the 21 lines of row C13 that lend an object's slot named as the engine's.
- `CalendarDatasetTests.cs` runs the retained dataset, `conformance/datasets/time/temporal-calendars.js`, against its
  expected answer (the polyfill's at `e8cc03fc` with Broiler.VM's 70 divergences): 3,894 of its 3,999 lines are
  calendar operations and answer as expected; the 105 that round or total relative to a date are part C's.
- `CalendarApiTests.cs` holds the contract: identifiers, month codes, statuses, merging, preconditions, the interface,
  the round trip of a difference in every calendar, and concurrent use.

### Decisions taken at SJL-10.3, part B

JSD-0078 left "the library's names and the shapes of its provider interfaces" to SJL-10.3. Under the owner's
direction of 2026-10-18, part B took these, within JSD-0078's X3:

1. **The namespace is the area design's, `Broiler.ECMAScript.Temporal`**, every type in its own file under
   `Temporal/Calendars/`.
2. **The design's static `Calendars` is an instance, `TemporalCalendars`, over SJL-8's `ICalendarTables`**, as
   `TimeZoneDatabase` is: the table calendars need the tables an engine hands in. What needs no table is static.
3. **The calendars' records carry the calendars' names** (`CalendarDuration`, `CalendarOverflow`, `CalendarUnit`,
   `CalendarFieldsType`), so that they meet no record the other parts declare; `CalendarDuration` holds exact
   integers. Where part A's records (a `DateDuration`, an `overflow` enumeration, a unit enumeration) serve the
   same purpose, part C converts field by field, or a later change unifies them.
4. **`ICalendarArithmetic` is the design's two members**, each answering a `CalendarStatus`, implemented for ISO
   8601 too, so that part C has one path for every calendar.
5. **The calendars keep private copies of the limits they test** (`CalendarLimits`), so that they take no dependency
   on part A; the published limit checks are part A's.
6. **An input date outside the years the calendars compute over** (an ISO year outside -271821 to 275760, which holds
   every date and year-month reference date within the limits) **is `OutOfRange`**; a month or day that is no ISO
   date's, and a field outside its conversion's range, are broken preconditions.
7. **A month code is a value, `MonthCode`**, whose `Parse` the adapter calls at the read, as Broiler.VM's
   `MonthCodeOf` does after its `ToPrimitive`; calendar fields are `double`s, held exactly at any magnitude (C11).
8. **The year-month addition and difference are compositions** this guide states, not members: their checks of the
   duration and their rounding are the durations sub-area's. `FormatCalendarAnnotation` is serialization's.

### Engine-specific behaviour

None is a parameter. Broiler.JS's differences from Broiler.VM on the calendar rows are corrections the cut-over makes
by calling the library (JSD-0078: C2, C5, C6, C9, C12, C13, C15, C16, C17); its code that no input reaches (C3, and the
reference-date search's upper year in C10) is deleted with its file.

---

*Added 2026-10-18, at milestone SJL-10.3: parts A and B each wrote a section, and their preambles were combined
when the parts were joined. Revisions of this guide are dated in place, quoting the text they replace.*
