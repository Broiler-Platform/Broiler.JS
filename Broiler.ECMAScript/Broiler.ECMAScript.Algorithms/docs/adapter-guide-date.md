# Adapter guide: `Broiler.ECMAScript.Algorithms` — Date

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/date.md`, and its verdict JSD-0072 in Broiler.VM's
profile, with its four sub-areas: the time arithmetic, the civil-day helpers, the Date string grammar
and the Date renderings, and the local-time provider of decision SJL-D-19.

- **Library and version:** `Broiler.ECMAScript.Algorithms` `0.1.0-preview.4` and later, API baseline
  [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Time` (JSD-0072, decision 1).
- **Specification pins:** ECMA-262 at commit `0248456c` (`conformance/vectors/sources/ecma262.pin`); the
  Temporal proposal's ISO 8601 calendar operations, which `IsoCalendarMath` names one by one, written on
  ECMA-262's proleptic Gregorian calendar.
- **Data versions:** none. No zone data enters the library: local time comes only from the provider the
  engine passes.
- **Consumers:** listed here at the cut-overs, milestones SJL-6.4 (Broiler.VM) and SJL-6.5
  (Broiler.JS); JSD-0072's X6 lists the members each replaces.
- **Vectors:** `conformance/vectors/date/`, corpus revision 14 or later. The library-level runner,
  `Broiler.ECMAScript/runners/AlgorithmsLibrary.Date.cs`, is also the reference adapter: every
  operation below is called there as an engine calls it, under a provider at offset zero named
  "Coordinated Universal Time", which is Broiler.VM's. The vectors whose outcome is the setters'
  argument conversion and its order, and those whose input is a sequence of local setter calls, are
  engine-level only.

**What no operation here does**, by convention 2: no `ToNumber`, `ToPrimitive` or `ToString` of an
argument, no reading of a `Date`'s `[[DateValue]]` or writing of it, no `RangeError`, no `toJSON` object
protocol and no routing of `toLocale*`. The adapter does those, in the order the specification gives,
and only then calls the library. **What stays in the engines** (JSD-0072, decision 3): argument
conversion and its order, storage, `toISOString`'s `RangeError`, `toJSON`'s object protocol,
`toLocale*`'s routing, the provider instance, the dialect and the order of grammars, Broiler.JS's
culture-dependent fallback, Temporal's and Intl's code around the civil helpers (Temporal's range limits
among it), and charging beyond the parser's.

**Failure is data** (X3): NaN for the arithmetic, `EcmaParsedKind.Invalid` for a parse, `false` from
`TryToIsoString`. A library exception is a broken precondition, listed under each operation.

**Charges**: only `EcmaDateParser.Parse` takes a meter, since only its work grows with its input
(JSD-0072, decision 4). An engine with no budget passes `null`.

**Policies are parameters** (SJL-D-19): the local time zone is the `ILocalTimeProvider` the engine
passes, and the reading of strings beyond the format is the `DateParseDialect` it passes. Broiler.VM
passes a `FixedOffsetLocalTimeProvider` at zero named "Coordinated Universal Time" and
`DateParseDialect.BroilerVM`; Broiler.JS passes its provider over the host's `TimeZoneInfo` and calls
the strict dialect, its own culture-dependent reading, then Broiler.VM's dialect, in that order.

---

## `EcmaTime` — ECMA-262 §21.4.1.3 to §21.4.1.31

### Signature

```csharp
public const double MsPerSecond = 1000;
public const double MsPerMinute = 60000;
public const double MsPerHour = 3600000;
public const double MsPerDay = 86400000;
public const double MaxTimeValue = 8.64e15;
public static double Day(double t);
public static double TimeWithinDay(double t);
public static double DaysInYear(double y);
public static double DayFromYear(double y);
public static double TimeFromYear(double y);
public static double YearFromTime(double t);
public static double DayWithinYear(double t);
public static double InLeapYear(double t);
public static double MonthFromTime(double t);
public static double DateFromTime(double t);
public static double WeekDay(double t);
public static double HourFromTime(double t);
public static double MinFromTime(double t);
public static double SecFromTime(double t);
public static double MsFromTime(double t);
public static double MakeTime(double hour, double minute, double second, double millisecond);
public static double MakeDay(double year, double month, double date);
public static double MakeDate(double day, double time);
public static double MakeFullYear(double year);
public static double TimeClip(double time);
```

Every member is the clause of its name. A step the clause writes in Number arithmetic is computed in
`double` arithmetic; a step written on mathematical values (`ℝ(t) modulo msPerDay`, `floor(ℝ(m) / 12)`,
`DayFromYear`'s sum) is computed exactly and rounded once. So `MakeDay` carries any finite month into
its year exactly (correspondence row T4).

### What the caller must have done first

| Built-in | Clause | What the adapter does |
|---|---|---|
| `Date.UTC`, the constructor with components | §21.4.3.4, §21.4.2.1 | `ToNumber` of each argument present, in order; an absent month is +0, an absent date 1 and an absent time component +0; then `MakeFullYear(year)`, `MakeDay`, `MakeTime`, `MakeDate`; the constructor applies `EcmaLocalTime.Utc` before `TimeClip` |
| the UTC setters | §21.4.4.28 to §21.4.4.34 | read `t` from `[[DateValue]]`; `ToNumber` of each argument present, in order, **even when `t` is NaN**; answer NaN for a NaN `t`, but `setUTCFullYear`, which reads it as +0 (§21.4.4.29 step 4); the fields left out from `t`; then the library's composition and `TimeClip`; store it |
| the local setters | §21.4.4.20 to §21.4.4.26 | as the UTC ones, on `EcmaLocalTime.LocalTime(t, zone)` (`setFullYear` reading a NaN `t` as +0, §21.4.4.21 step 4), and `EcmaLocalTime.Utc` before `TimeClip` |
| the getters | §21.4.4.2 to §21.4.4.19 | read `t`; NaN for an invalid date; `LocalTime(t, zone)` for a local getter, then the field operation |
| `getTimezoneOffset` | §21.4.4.11 | `(t - EcmaLocalTime.LocalTime(t, zone)) / MsPerMinute`, NaN for an invalid date |

### Preconditions

- The field operations (`YearFromTime` to `DateFromTime`) take a finite Number of magnitude at most
  2^62, far past any time value or local time; a NaN or an infinity answers NaN. A larger finite
  argument throws `ArgumentOutOfRangeException`.
- `DayFromYear` and `TimeFromYear` answer NaN for an argument that is not an integral Number.
- `MakeTime` and `MakeDay` apply `ToIntegerOrInfinity` themselves, as their clauses do; their arguments
  are Numbers already converted.

### Work and limits

- **Meter:** none.
- **Limits:** none. `MakeDay` answers NaN where no finite time value lies in its month, and `TimeClip`
  NaN beyond `MaxTimeValue`.

### Statuses and identifiers

None: every member answers a Number, NaN for an invalid result.

## `IsoCalendarMath` — the Temporal proposal's ISO 8601 calendar operations

### Signature

```csharp
public static bool IsLeapYear(long year);
public static int DaysInYear(long year);
public static int DaysInMonth(long year, int month);
public static long EpochDays(long year, int month, int day);
public static (long Year, int Month, int Day) FromEpochDays(long days);
public static int DayOfWeek(long year, int month, int day);
public static int DayOfYear(long year, int month, int day);
public static (int Week, long Year) WeekOfYear(long year, int month, int day);
public static (long Year, int Month) BalanceYearMonth(long year, long month);
public static bool IsValidIsoDate(long year, int month, int day);
```

| Member | Operation |
|---|---|
| `IsLeapYear`, `DaysInYear` | `MathematicalInLeapYear`, `MathematicalDaysInYear` (ECMA-262 §21.4.1.5's calendar) |
| `DaysInMonth` | `ISODaysInMonth` |
| `EpochDays` | `ISODateToEpochDays` with a one-based month: ECMA-262's `MakeDay(year, month - 1, day)` on integers |
| `FromEpochDays` | `EpochDaysToISODate`, its inverse |
| `DayOfWeek`, `DayOfYear`, `WeekOfYear` | `ISODayOfWeek` (Monday 1 to Sunday 7), `ISODayOfYear`, `ISOWeekOfYear` (week 1 holds the year's first Thursday) |
| `BalanceYearMonth` | `BalanceISOYearMonth` |
| `IsValidIsoDate` | `IsValidISODate` |

### What the caller must have done first

Every argument is a mathematical integer the caller has already read and converted: Temporal's
`ToIntegerWithTruncation`, its property bag and option reading, `RegulateISODate`'s overflow choice, and
**Temporal's range limits** (`ISODateWithinLimits`, every date from -271821-04-19 to +275760-09-13),
which stay the engine's until SJL-10 (JSD-0072, "Civil-day helpers"). The runner's
`Temporal.PlainDate.prototype.add, days` shows the order: epoch days, the limits, then `FromEpochDays`.

### Preconditions

- A year is at most 2^53 in magnitude (`WeekOfYear`: 2^53 - 1), an epoch day count at most 2^60, and
  `BalanceYearMonth`'s arguments at most 2^62; a month outside 1 to 12, where an operation needs a valid
  one, throws `ArgumentOutOfRangeException`. `IsValidIsoDate` takes any month and day and answers.
- `EpochDays` carries a day outside the month into the months around it; `DayOfWeek`, `DayOfYear` and
  `WeekOfYear` take a valid date.

### Work and limits

- **Meter:** none. **Limits:** none beyond the magnitudes above.

### Statuses and identifiers

None.

## `ILocalTimeProvider` and `EcmaLocalTime` — ECMA-262 §21.4.1.25 LocalTime, §21.4.1.26 UTC

### Signature

```csharp
public interface ILocalTimeProvider
{
    double GetOffsetMilliseconds(double epochMilliseconds);
    string? GetName(double epochMilliseconds);
}

public sealed class FixedOffsetLocalTimeProvider : ILocalTimeProvider
{
    public FixedOffsetLocalTimeProvider(double offsetMilliseconds, string? name);
}

public static double EcmaLocalTime.LocalTime(double t, ILocalTimeProvider zone);
public static double EcmaLocalTime.Utc(double t, ILocalTimeProvider zone);
```

### What the provider must answer

`GetOffsetMilliseconds` answers, for every finite Number of milliseconds since the epoch, a time value
or not, `truncate(GetNamedTimeZoneOffsetNanoseconds(ℤ(ℝ(t) × 10^6)) / 10^6)` (§21.4.1.21): an integral
Number of magnitude below 86,400,000, positive east of UTC, the same for the same instant on every call.
The zone's offset changes at most once in any two days, which every zone of the IANA Time Zone Database
does. `GetName` answers the zone's name for `TimeZoneString` at an instant, or `null` for none.

An offset that breaks the contract (not finite, not integral, a day or more) makes the library throw
`InvalidOperationException`; it is a defect of the provider, never an outcome a script sees.

### What `Utc` does

It reads the offsets one day before and one day after the local time, as instants, and keeps each
offset whose instant `t - offset` gives that offset back: one such instant is the answer; of two, a
repeated local time, the earlier (the larger offset); with none, a skipped local time, the offset before
the transition. This is §21.4.1.26's disambiguation, decision SJL-D-19's; it corrects Broiler.JS's L3.
`Utc` does not clip: the caller applies `TimeClip`.

### Preconditions

- `zone` is not null. A NaN or infinite `t` answers NaN.

### Work and limits

- **Meter:** none. `LocalTime` reads the provider once, `Utc` three or four times.

### Engine-specific behaviour

| Parameter | Broiler.JS passes | Broiler.VM passes | Why they differ |
|---|---|---|---|
| `zone` | its provider over the host's `TimeZoneInfo`, one offset per instant with daylight-saving time (corrections L2 to L4), until SJL-8's shared zone data | `FixedOffsetLocalTimeProvider(0, "Coordinated Universal Time")` | SJL-D-19: the host policy is each engine's; this family's local time is UTC, as the profile's Proposed records JSD-0053 and JSD-0027 state it |

## `EcmaDateParser.Parse` — ECMA-262 §21.4.3.2 Date.parse, §21.4.1.32 Date Time String Format

### Signature

```csharp
public enum DateParseDialect { Strict = 0, BroilerVM = 1 }
public enum EcmaParsedKind { Invalid = 0, Utc = 1, Local = 2 }
public readonly record struct EcmaParsedDate(EcmaParsedKind Kind, double Value);
public static EcmaParsedDate Parse(ReadOnlySpan<char> text, DateParseDialect dialect, WorkCharge? charge = null);
```

### What the caller must have done first

| Built-in | Clause | What the adapter does |
|---|---|---|
| `Date.parse` | §21.4.3.2 | `ToString` of the argument |
| the constructor with one argument | §21.4.2.1 step 4 | a `Date` argument's `[[DateValue]]`; otherwise `ToPrimitive` of the argument, and a String is parsed while anything else is `ToNumber`'d |

### What the caller does with the result

| Kind | Meaning | `Date.parse` and the constructor |
|---|---|---|
| `Utc` | `Value` is a time value, already clipped: a date-only form, or a form with `Z` or an offset | the value |
| `Local` | `Value` is a local time, not converted or clipped: a date-time form without an offset | `TimeClip(EcmaLocalTime.Utc(Value, zone))` |
| `Invalid` | not a date in the dialect, or outside the time values | NaN, or the next grammar of the engine's order |

### The dialects

- **`Strict`**: the format (§21.4.1.32 and §21.4.1.32.1) and nothing else. A year of four digits, or a
  sign and six (`-000000` refused); optionally a month, and with it a day; then optionally `T` and
  `HH:mm`, `HH:mm:ss` or `HH:mm:ss.sss` with exactly three fraction digits; then optionally `Z` or
  `±HH:mm`. Every element is in bounds, **a day past its month's length is refused** (JSD-0072), and
  `24:00` is the only hour 24.
- **`BroilerVM`**: Broiler.VM's `DateParseText`, exactly: for a text that starts with a sign or four
  digits, the format with any day up to 31 carried into the next month; for any other text, the textual
  reading of `DateParseTextual` - tokens split on space, comma and tab, a token opening with `(` ending
  the reading, English month abbreviations, a clock `H:m` or `H:m:s`, a `GMT` or `UTC` token alone or
  with a sign and four digits, and whole numbers taken as the day or the year - which answers `Local`
  without a zone. **The one difference is P9**
  (correction of this family): a minus sign and digits are a year, when no year has been read yet, so
  that the engine's own renderings of negative years read back.

### Preconditions

- `dialect` is one of the enumeration's; another throws `ArgumentOutOfRangeException`.

### Work and limits

- **Meter:** the text's length, in code units, before any character is read, when the text is not
  empty, whatever the outcome (JSD-0072, decision 4). Broiler.VM's adapter charges its own one unit at
  entry, so `Date.parse` and the string constructor charge `length + 1`, as before the cut-over.
- **Limits:** none. The reading is one pass over the text.

### Engine-specific behaviour

| Parameter | Broiler.JS passes | Broiler.VM passes | Why they differ |
|---|---|---|---|
| `dialect` | `Strict`, then its own culture-dependent reading (SJL-D-17), then `BroilerVM` | `BroilerVM` | each engine's order of grammars is its own (JSD-0072, decision 3) |
| `charge` | `null` | its meter | Broiler.JS has no budget |

## `EcmaDateFormatter` — ECMA-262 §21.4.4.41, §21.4.4.43, §21.4.4.36

### Signature

```csharp
public const string InvalidDate = "Invalid Date";
public static string TimeString(double tv);
public static string DateString(double tv);
public static string TimeZoneString(double tv, ILocalTimeProvider zone);
public static string ToDateString(double tv, ILocalTimeProvider zone);
public static string ToUtcString(double tv);
public static bool TryToIsoString(double tv, out string text);
```

Every name and number written is fixed ASCII text in the specification's form: three-letter English
names, `ToZeroPaddedDecimalString`'s digits (§22.1.3.17.3), U+002D for a negative year or offset, and the
year at least four digits, or for `toISOString` a sign and six digits outside 0 to 9999. The zone's name
is the provider's, written as given.

### What the caller must have done first

| Built-in | Clause | What the adapter does |
|---|---|---|
| `toString` | §21.4.4.41 | read `tv`; `ToDateString(tv, zone)`, which answers `InvalidDate` for NaN |
| `toDateString` | §21.4.4.35 | `InvalidDate` for NaN; otherwise `DateString(LocalTime(tv, zone))` |
| `toTimeString` | §21.4.4.42 | `InvalidDate` for NaN; otherwise `TimeString(LocalTime(tv, zone)) + TimeZoneString(tv, zone)` |
| `toUTCString` | §21.4.4.43 | `ToUtcString(tv)`, which answers `InvalidDate` for NaN |
| `toISOString` | §21.4.4.36 | `TryToIsoString(tv, out text)`; **a `RangeError` when it answers false** |
| `toJSON` | §21.4.4.37 | the object protocol: `ToPrimitive(O, number)`, `null` for a non-finite Number, then a call of the object's `toISOString` |

### Preconditions

- `TimeString`, `DateString` and `TimeZoneString` take a finite Number; another throws
  `ArgumentOutOfRangeException`. `ToUtcString` takes a time value or NaN. `zone` is not null.

### Work and limits

- **Meter:** none. **Limits:** none: every result is at most a few dozen code units plus the zone's name.

### Statuses and identifiers

None: `TryToIsoString`'s `false` is the only failure, and the adapter turns it into `RangeError`.

---

*Revisions of this guide are dated in place, quoting the text they replace.*
