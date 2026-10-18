# Adapter guide: `Broiler.ECMAScript.Algorithms` — the Temporal core

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/temporal.md`, and its verdict JSD-0078 in Broiler.VM's
profile.

**This guide covers part A of milestone SJL-10.3**: the records and limits, balancing (with regulation and
addition), the grammar, serialization, and the options' rules those sub-areas use. The calendars (part B) and
durations, rounding, differences and their options' rules (part C) extend it.

- **Library and version:** `Broiler.ECMAScript.Algorithms`, from the first version after `0.1.0-preview.5`,
  API baseline [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Temporal`. The records are
  SJL-8's `IsoDate`, `IsoTime` and `IsoDateTime` in `Broiler.ECMAScript.Time` (JSD-0078, decision D8).
- **Specification pins:** the Temporal proposal at `tc39/proposal-temporal@e8cc03fc` (decision D1), whose
  operations each member names; where its text and its polyfill disagree, the text rules (decision D7). SJL-4's
  rounding (`ExactRounding`, `RoundingMode`) and SJL-6's civil days (`IsoCalendarMath`) are the library's.
- **Data versions:** none.
- **Consumers:** listed here at the cut-overs, milestones SJL-10.4 (Broiler.VM) and SJL-10.5 (Broiler.JS);
  JSD-0078's X6 lists the members each replaces.
- **Vectors:** `conformance/vectors/temporal/`, corpus revision 24 or later (SJL-10.2). Until a runner reads
  them, the library's tests hold every row R, B, G and S of bundle `sjl-10-001`'s correspondence.

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

*Added 2026-10-18, at milestone SJL-10.3, part A. Revisions of this guide are dated in place, quoting the text
they replace.*
