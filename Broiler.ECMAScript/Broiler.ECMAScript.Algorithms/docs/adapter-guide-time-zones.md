# Adapter guide: `Broiler.ECMAScript.Algorithms` — time zones and calendar years

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/time-zones-and-calendars.md`, and its verdict JSD-0076 in
Broiler.VM's profile, with its three sub-areas: the tz database, the zone algorithms over it, and the
calendar tables.

- **Library and version:** `Broiler.ECMAScript.Algorithms`, from the first version after `0.1.0-preview.4`,
  API baseline [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Time` (JSD-0076, decision 1).
- **Specification pins:** ECMA-262 at commit `0248456c` (`conformance/vectors/sources/ecma262.pin`) for
  §21.4.1.20 to §21.4.1.33; ECMA-402's time zone names (§6.5); the Temporal proposal's time-zone
  operations, which each member names; the Intl era and month code proposal's Table 1 for the calendars.
- **Data versions:** none in the library. The tables are the data home's (milestone SJL-8.2) and each
  engine embeds them and hands their bytes in (decision SJL-D-6 (c)). The zone tables' release is the one
  the engine passes to `TimeZoneDatabase`, `2026e` at JSD-0076; the calendar sources are `icu_calendar`
  2.3.0 and `calendrical_calculations` 0.2.4 (decision SJL-D-21).
- **Consumers:** listed here at the cut-overs, milestones SJL-8.5 (Broiler.VM) and SJL-8.6 (Broiler.JS);
  JSD-0076's X6 lists the members each replaces.
- **Vectors:** `conformance/vectors/time-zones/`, corpus revision 21 or later, whose expected values are
  `scripts/conformance/tzref.py`'s, computed from the pinned release with the host's `zic` and no code of this
  library or of the data home's generator. The library-level runner,
  `Broiler.ECMAScript/runners/AlgorithmsLibrary.TimeZones.cs`, is also the reference adapter: every operation
  below is called there as an engine calls it, over the data home's generated tables read from disk, and it
  answers every vector of the area. *(Revised 2026-10-17, at milestone SJL-8.4. The bullet read: "**Vectors:**
  none yet; SJL-8.4 adds the area's. Until then the library's tests and the differential check against
  Broiler.VM's code (milestone SJL-8.3) are its evidence.")*

**What no operation here does**, by convention 2: no `ToString` of a zone argument, no reading of a
`Temporal` object's slots, no option reading (`disambiguation`, `offset`), no `RangeError`, no host zone and
no clock. The adapter does those, in the order the specification gives, and only then calls the library.
**What stays in the engines** (JSD-0076, decision 4): optionality of the data, the mapping of statuses to
errors, the host zone (SJL-D-19), the formatter's resolved `timeZone` and zone display names (SJL-9), the
arithmetic over calendars (SJL-10), and the parsing of ISO strings with their annotations.

**Failure is data** (X3): an unknown name is a `false` from `TryFind`; a missing start of day, a refused
disambiguation, an instant outside the range and a year outside every source are `TimeStatus` values. A
library exception is a broken precondition, listed under each operation.

**Charges**: none. Each member's work is bounded by the data and the instant range, not by its input,
beyond an identifier's length, which the lookup bounds (X3: there is no class C2).

---

## `TimeZoneDatabase` — the tables, ECMA-402 §6.5 time zone names

### Signature

```csharp
public readonly record struct TimeZoneId(int Index, string Identifier);

public interface ITimeZoneDatabase
{
    string Release { get; }
    IReadOnlyList<string> PrimaryIdentifiers { get; }
    bool TryFind(ReadOnlySpan<char> identifier, out TimeZoneId id);
    TimeZoneId Primary(TimeZoneId id);
    IReadOnlyList<string> RegionTimeZones(ReadOnlySpan<char> region);
    ITimeZoneRules Rules(TimeZoneId id);
}

public sealed class TimeZoneDatabase : ITimeZoneDatabase
{
    public const long MaxEpochSeconds = 1_000_000_000_000_000;
    public TimeZoneDatabase(string release, ReadOnlySpan<byte> zoneTable, ReadOnlySpan<byte> identifierTable, ReadOnlySpan<byte> regionTable);
}
```

| Member | Operation |
|---|---|
| `TryFind` | `GetAvailableNamedTimeZoneIdentifier` (§6.5.2) under SJL-D-20's rule: an ASCII-case-insensitive match; a character above U+007F matches nothing |
| `Primary` | the record's `[[PrimaryIdentifier]]` (ECMA-262 §21.4.1.22): CLDR's, `Etc/UTC`, `Etc/GMT` and `GMT` resolving to `UTC` |
| `PrimaryIdentifiers` | `AvailablePrimaryTimeZoneIdentifiers` (§6.5.3), in ordinal order: `Intl.supportedValuesOf("timeZone")` |
| `RegionTimeZones` | `zone.tab`'s zones of a region, ordinal: the Intl Locale Info proposal's `TimeZonesOfLocale` for `getTimeZones` (JSD-0076, Z12) |
| `Rules` | the zone an identifier reads, which is its primary's |

### What the caller must have done first

| Built-in | What the adapter does |
|---|---|
| Temporal's `ToTemporalTimeZoneIdentifier`, the `ZonedDateTime` constructor | read the string; parse an ISO string's annotation itself; `OffsetTimeZones.TryParseOffsetIdentifier` first; otherwise `TryFind`, and its `false` is the `RangeError` |
| `Intl.DateTimeFormat`'s `timeZone` | `ToString`; `OffsetTimeZones.IsTimeZoneOffsetString`, then `TryParseOffsetIdentifier` (seconds are a `RangeError`); otherwise `TryFind`; which name the formatter reports (the name or `Primary`) is the engine's until Z8 is settled |
| `Intl.Locale.prototype.getTimeZones` | the locale's region subtag, canonical and upper case; `RegionTimeZones` |

### Preconditions

- The three tables are the generator's, whole: a table that does not hold the layout to its last byte, an
  identifier line without its three fields or with a zone index past the table, a primary that is not an
  identifier that is its own primary, two identifiers differing only in ASCII case, or a text byte above
  U+007F throws `ArgumentException` from the constructor. The release is not empty.
- A `TimeZoneId` passed back is one this database gave; another throws `ArgumentException`.

### Work and limits

- **Meter:** none. The constructor copies the zone table and reads the text tables once; a zone is decoded
  the first time its rules are asked for. Every member is safe to call concurrently.

### Statuses and identifiers

None: `TryFind` answers `false` for an unknown name.

## `ITimeZoneRules` — ECMA-262 §21.4.1.21, the Temporal proposal's transitions

### Signature

```csharp
public interface ITimeZoneRules
{
    int OffsetSecondsAt(long epochSeconds);
    bool IsDaylightAt(long epochSeconds);
    long? NextTransition(long afterEpochSeconds, long limitEpochSeconds, bool daylightFlag);
    long? PreviousTransition(long beforeEpochSeconds, bool daylightFlag);
}

public sealed class FixedOffsetTimeZoneRules : ITimeZoneRules
{
    public FixedOffsetTimeZoneRules(int offsetSeconds);
}
```

Instants are whole seconds since the epoch; an engine reading an instant in nanoseconds or milliseconds
reads its floor second, as `ZonedTime` does. The listed transitions answer through the last listed year,
and the recurring rules every later one, without limit: **there is no 2037 cliff**. `NextTransition`
answers a transition strictly after its instant and at or before the limit; `PreviousTransition` one
strictly before. `daylightFlag` counts a listed change of the daylight flag alone, which only zone names
read (Broiler.VM's `JsZoneNames`, SJL-9's); Temporal passes `false`. `IsDaylightAt` is the compiled
`isdst` flag, rearguard (Europe/Dublin's summer is daylight time).

### Preconditions

- An instant is at most `TimeZoneDatabase.MaxEpochSeconds` (10^15) in magnitude; beyond it the database's
  rules throw `ArgumentOutOfRangeException`. A limit beyond it is read as it.
- `FixedOffsetTimeZoneRules` takes an offset below a day in magnitude.

### Work and limits

- **Meter:** none. A recurring zone's search is bounded by the years between the instant and the limit.

## `OffsetTimeZones` — ECMA-262 §21.4.1.33, the offset time zone identifiers

### Signature

```csharp
public static bool IsTimeZoneOffsetString(ReadOnlySpan<char> text);
public static bool TryParseOffsetIdentifier(ReadOnlySpan<char> text, out int offsetMinutes);
public static string FormatOffsetIdentifier(int offsetMinutes);
public static bool TryGetEtcGmtOffset(ReadOnlySpan<char> primaryIdentifier, out int offsetMinutes);
```

| Member | Operation |
|---|---|
| `IsTimeZoneOffsetString` | §21.4.1.33.1: the `UTCOffset` grammar with seconds and up to nine fraction digits |
| `TryParseOffsetIdentifier` | `UTCOffset[~SubMinutePrecision]`, the offset branch of the proposal's `ParseTimeZoneIdentifier` |
| `FormatOffsetIdentifier` | `FormatOffsetTimeZoneIdentifier`: `±HH:MM`, `+00:00` for zero |
| `TryGetEtcGmtOffset` | IANA's `Etc/GMT+1` to `+12` and `Etc/GMT-1` to `-14`, POSIX's sign inverted, matched ordinally against a primary identifier: the fixed offsets Broiler.VM's formatter formats them as (A14) |

Digits are ASCII only (A13): `+٠٥:٣٠` is no offset, and the adapter's next step, the name lookup, refuses it.

### Preconditions

- `FormatOffsetIdentifier` takes an offset below a day in magnitude.

## `ZonedTime` — the Temporal proposal's time-zone operations

### Signature

```csharp
public readonly record struct IsoDate(long Year, int Month, int Day);
public readonly record struct IsoTime(int Hour, int Minute, int Second, int Millisecond, int Microsecond, int Nanosecond);
public readonly record struct IsoDateTime(IsoDate Date, IsoTime Time);
public enum Disambiguation { Compatible, Earlier, Later, Reject }
public enum TimeStatus { Completed, OutOfRange, Ambiguous, Skipped, NoStartOfDay }

public const int MaxPossibleEpochNanoseconds = 3;
public static readonly BigInteger MaxEpochNanoseconds;   // nsMaxInstant
public static readonly BigInteger MinEpochNanoseconds;   // nsMinInstant
public static long GetOffsetNanosecondsFor(ITimeZoneRules rules, BigInteger epochNanoseconds);
public static TimeStatus GetPossibleEpochNanoseconds(ITimeZoneRules rules, IsoDateTime local, Span<BigInteger> destination, out int count);
public static TimeStatus DisambiguatePossibleEpochNanoseconds(ITimeZoneRules rules, ReadOnlySpan<BigInteger> possible, IsoDateTime local, Disambiguation mode, out BigInteger epochNanoseconds);
public static TimeStatus GetEpochNanosecondsFor(ITimeZoneRules rules, IsoDateTime local, Disambiguation mode, out BigInteger epochNanoseconds);
public static TimeStatus GetStartOfDay(ITimeZoneRules rules, IsoDate date, out BigInteger epochNanoseconds);
public static BigInteger? GetNextTransition(ITimeZoneRules rules, BigInteger epochNanoseconds);
public static BigInteger? GetPreviousTransition(ITimeZoneRules rules, BigInteger epochNanoseconds);
public static bool TimeZoneEquals(ITimeZoneDatabase database, ReadOnlySpan<char> one, ReadOnlySpan<char> two);
```

Each member is the proposal's operation of its name; a named zone is read through its database's
`Rules`, an offset zone through `FixedOffsetTimeZoneRules`. **The possible instants are sampled at the wall
time and a day either side** (A2), so data with an offset in force for less than two days still gives
every candidate. In a gap, `Compatible` and `Later` move the wall time forward by the difference of the
offsets a day either side, and `Earlier` back; the moved time is sampled again.

### What the caller must have done first

The option reading (`ToTemporalDisambiguation`, `ToTemporalOffset`), `InterpretISODateTimeOffset`'s offset
matching over the possible instants, and the proposal's `ISODateTimeWithinLimits` where the caller's
operation checks it. `GetPossibleEpochNanoseconds` checks no day range itself for a named zone: a wall time a
day past the limit can still name a valid instant, west of UTC.

### Preconditions

- The date is a valid ISO date and the time a valid time; otherwise `ArgumentOutOfRangeException`. A year
  is at most 2^53 in magnitude.
- An instant passed in is at most 10^15 seconds from the epoch.
- The destination holds `MaxPossibleEpochNanoseconds`; the mode is one of the four.
- `TimeZoneEquals`' identifiers are offset identifiers or names the database has; an unknown name throws
  `ArgumentException`.
- The rules keep their contract: an offset below a day, and, across a gap, a moved wall time that names an
  instant. Rules that do not make the library throw `InvalidOperationException`; no zone of the tables does.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | the result is valid | — | the instant | the instant |
| `OutOfRange` | an instant found is outside `nsMinInstant` to `nsMaxInstant`, or the wall time is far outside them | `RangeError` | its `RangeError` | `RangeError`, "the date-time is outside the representable range" |
| `Ambiguous` | `Reject` and the wall time names two instants | `RangeError` | its `RangeError` | `RangeError`, "the local time is ambiguous" |
| `Skipped` | `Reject` and a transition skips the wall time | `RangeError` | its `RangeError` | `RangeError`, "the local time does not exist" |
| `NoStartOfDay` | midnight is skipped and no transition follows within the range; never local midnight | `RangeError` | its `RangeError` | `RangeError`, "the start of the day cannot be found" |

### Message identifiers

None: the statuses are the identifiers, and each engine keeps its own text.

## `CalendarTables` and `CalendarYears` — the table-backed calendars' years

### Signature

```csharp
public enum TableCalendar { Chinese, Dangi, IslamicUmmAlQura, Persian }

public readonly record struct CalendarYear
{
    public CalendarYear(long year, long newYearEpochDays, ReadOnlySpan<int> monthLengths, int leapMonth);
    public long Year { get; }
    public long NewYearEpochDays { get; }
    public int MonthsInYear { get; }
    public int LeapMonth { get; }
    public int DaysInYear { get; }
    public int DaysInMonth(int month);
}

public interface ICalendarTables
{
    bool TryGetEastAsianYear(TableCalendar calendar, long relatedIsoYear, out CalendarYear year);
    bool TryGetUmmAlQuraYear(long year, out CalendarYear calendarYear);
    bool IsPersianNonLeapYear(long year);
}

public sealed class CalendarTables : ICalendarTables
{
    public CalendarTables(ReadOnlySpan<byte> table);
}

public const long CalendarYears.MaxYearMagnitude = 300_000;
public static TimeStatus CalendarYears.GetYear(ICalendarTables tables, TableCalendar calendar, long year, out CalendarYear result);
```

A year is its new year, its months and their lengths, and its leap month's ordinal (JSD-0076). `GetYear`
answers it from the table inside the table's range - China's and Korea's 1912 to 2102 and the Qing years
1900 to 1911 for both, Umm al-Qura 1300 to 1600 AH - and outside from ICU4X's approximation (UTC+8 or UTC+9
after 1912, Beijing's mean time before), the civil tabular year, and the 33-year rule with the listed
corrections (1502 to 2987 AP). The years either side of a table's edge come from different sources, as in
Broiler.VM, and with the pinned data they meet: each year ends the day the next begins. **The arithmetic over
the years, month codes, eras and the formula calendars stay SJL-10's.**

### Preconditions

- The table is the generator's, whole; otherwise `ArgumentException` from the constructor.
- `TryGetEastAsianYear` takes `Chinese` or `Dangi`; `GetYear` one of the four.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | the year is valid | — | the year | the year |
| `OutOfRange` | the year's magnitude is above 300,000, where no date is within the ISO limits | `RangeError` | its `RangeError` | `RangeError`, "the year is outside the representable range" |

## Engine-specific behaviour

None is a parameter. What differs between the engines stays outside the library: Broiler.VM reports a
formatter's zone as its primary identifier and formats the `Etc/GMT` zones as offsets, through `Primary`
and `TryGetEtcGmtOffset`; Broiler.JS keeps the name (Z8) until the specification edition both engines pin
settles it. Broiler.VM's realm without Intl data reads `UTC` alone, with no database.

---

*Revisions of this guide are dated in place, quoting the text they replace.*
