# Bundle SJL-6-001's reference-arm rule, revised 2026-10-15: the population's open readings, the local-time arbiter before 1913, and how the comparison writes and reads

**Written:** 2026-10-15, at milestone SJL-6.3, after the Date area was written into
`Broiler.ECMAScript.Algorithms` and both arms were copied and the population generator, the comparison and
its classifier were written, and **before any run of the comparison**, void or judged: no result of S or of
any arm on the population has been seen. **Owner:** the Broiler.JS maintainer. **Reviewer:** none (the
programme's exception EX-30: every role is one person).

**What it revises, and what it does not.** The rule's block, [`reference-arms.md`](reference-arms.md)'s
```` ```rule ```` section, stays in this directory unedited; its arms, operations, admitted classes, negative
controls and MET line stand as written. This file does five things:

1. it sets the readings the population leaves open, as the generator and the comparison implement them
   (section 1);
2. it says which inputs each arm is not called for, and why (section 2);
3. it reads the arbiter of local time where its premise fails: the rule's "the same tzdata through Python's
   zoneinfo" assumes that zoneinfo and the host's `TimeZoneInfo` read the same offsets from the same files,
   and before 1913 they do not (section 3);
4. it says how results are written and compared, how the classifier reads the classes, and how "every class
   (b) difference is in a vector" is read (section 4);
5. it says what is retained (section 5).

**Why the evidence is weaker or not.** No run of the comparison had been seen when this was written. Section
3 rests on a reading of the two zone readers made on 2026-10-15 at dates chosen for the purpose (below), not
on the population or on any result of S or an arm. The other readings set what the rule leaves open, or follow
from the arms' own signatures.

## 1. The population: the readings the rule leaves open

The generator is
[`scripts/conformance/generate_date_population.py`](../../../scripts/conformance/generate_date_population.py),
tested by `tests/test_date_population.py`. It uses the vector generator's SplitMix64
(`generate_number_vectors.splitmix64`), which that test and `tests/test_reference_population.py` test against
the published reference outputs, and states in its docstring how a draw is read: a choice among n things is
the draw modulo n, an integer from [low, high] is low plus the draw modulo the range's size, and a second
value is the draw shifted right by 16 bits. It writes each class as one gzip file, with a manifest of each
file's line count and SHA-256, the Python version and the host's tzdata release (2026c when this was written).
Where the rule names a seed, the generator uses it.

| Class | The rule's words | Read as |
|---|---|---|
| E1 | "-8.64e15 and 8.64e15, each with the integers 1 ms inside and outside it; every day's first and last millisecond in years -1 to 1, 1969 to 1972 and 9999 to 10000; -0, 0.5, ...; and 2^16 integers drawn" | file E1T, as written; **E1's valid time values**, which the renderings and the read-back strings take, are its integers of magnitude at most 8.64e15, -0 excluded (a Date never holds -0) |
| E2 | "each of [32 values] in each argument of MakeTime, MakeDay and MakeFullYear in turn, the others held at the epoch's; and 2^16 tuples drawn ... from those values and the integers -1000 to 1000" | file E2C: seven-component tuples (year, month, date, hour, minute, second, millisecond), each value in each component in turn with the others at (1970, 0, 1, 0, 0, 0, 0), then the drawn tuples, each component one draw among the 2,033 choices. MakeTime takes the last four, MakeDay the first three, MakeFullYear the first; Date.UTC's core takes all seven, as the rule composes it (without MakeFullYear) |
| E2 | "MakeDay, MakeDate and TimeClip each alone over the inputs where the specification's result is a time value or NaN" | MakeDay alone over the E2C tuples whose reference MakeDay is NaN or a day of magnitude at most 100,000,001, flagged by the generator (S runs every tuple alone); MakeDate alone over file E2M, the reference's MakeDay and MakeTime of each E2C tuple whose reference MakeDate is NaN or of magnitude at most 8.64e15, each pair once; TimeClip alone over E1T, whose results are all a time value or NaN |
| E3 | "every date of years -1200 to 2800, every month 1 to 12 and every day 1 to 31, and days 0, 32 and months 0 and 13 for the validity check" | file E3D: every year with months 0 to 13 and days 0 to 32, flagged valid by the reference; the validity check runs over every line, and the operations of a date (epoch days, weekday, day of the year, ISO week) over the valid ones, which are what an engine hands them |
| E3 | "the years -271821, 275760, -(2^31), 2^31 - 1, ±1e9 and ±(1e9 + k) for k from 0 to 400, with months 1 to 12 and days 1, 28, 29, 30 and 31" | appended to E3D, flagged alike; the years alone, with -1200 to 2800, are file E3Y, which the leap year, the days in a year, the month length (months 1 to 12) and Date's DaysInYear and DayFromYear take |
| E3 | "every epoch day from -1,000,000 to 1,000,000; 2^16 epoch days drawn ... from ±(1e11)" | file E3E, as written |
| E3 | "for BalanceISOYearMonth, the years above and months from -25 to 25 and at ±(2^53)" | file E3B: every E3Y year with each of those 53 months |
| E4 | "every transition in its data from 1800 to 2100, at the transition and at 1 ms, 1 min, 30 min, 1 h and 2 h on either side, read once as a UTC instant (LocalTime) and once as a local time (UTC)" | file E4L. A transition is found through zoneinfo: the offset is read every six hours from 1800-01-01 to 2101-01-01 and bisected to the millisecond where it changes. At a transition T with offsets ob before and oa after, the instants T + δ are read through LocalTime, and **the wall times T + ob + δ and T + oa + δ** through UTC, so that the local times a transition skips or repeats are among them; δ is each of the eleven deltas |
| E4 | "the local times of years 1, 1700, 9999, 10000, -1 and 100000" | January 1 at 00:00 and July 1 at 12:00 of each, read both ways |
| E4 | "2^14 instants drawn ... from years 1800 to 2200" | from the first millisecond of 1800 to the last of 2200, read both ways, the same instants in every zone |
| E5 | "every combination of the format's shapes ... over the values ..." | file E5F: each year value written as given (the four-digit shape takes 0000 to 10000, the signed shape the rest), with no month, a month, or a month and a day; then, with each, no time or `T` and `HH:mm`, `HH:mm:ss` or `HH:mm:ss.sss` over the values, followed by nothing, `Z` or each offset |
| E5 | "each also mutated to a lower-case t and z, a space for the T, zero, one, two, four, nine and ten fraction digits, an offset without its colon, and a leading and a trailing space" | each mutation applies where its element is present: `t` and the space for a string with `T`, `z` for one ending in `Z`, the fraction counts for one with a fraction (its digit repeated; zero digits keep the point), the colon for one with an offset; the spaces for every string. Each string is followed by its mutations, every string once |
| E5 | "the toString and toUTCString renderings of E1's valid time values in each engine's own form, Broiler.JS's in UTC and in America/New_York" | **built by the comparison**, from the arms' own renderers at run time (arm V's `DateToFullText` and `DateToUtcText`; arm J's `toString` in UTC and in New York and `toUTCString`), because each engine's rendering is its own code |
| E5 | "Broiler.VM's fallback shapes: ..." | file E5V: each dimension varied alone in three base strings (that engine's `toString` and `toUTCString` of the epoch, and its calendar form): the twelve abbreviations and full names in three letter cases; each weekday and none; `GMT` and `UTC` alone and with each sign and hhmm of 00 to 24 hours and 00 to 99 minutes; years of one to six digits, and zero-padded to four to six, with no sign, `+` and `-`; the other order; a comma, `", "` and a tab for every space; and a suffix added, emptied or removed; with the correspondence's row P7 strings and clock shapes |
| E5 | "2^16 strings drawn ... from a grammar of those tokens, each mutated by one character" | file E5G: a draw gives the token count, 1 to 8; each token's kind and value are drawn (the generator's `e5_token`), each gap is a space, a comma, `", "` or a tab; then one character of a 26-character alphabet is inserted, deleted or replaced. Only the mutated string is written |
| E6 | "E1's valid time values at the offsets ..., each with no name, an ASCII name and a name outside ASCII" | files E6O (the eleven offsets) and E6N (none, `Test Zone`, and a name with U+2019, accented letters, two CJK characters and a surrogate pair); the product is **built by the comparison** |

## 2. Inputs an arm is not called for

| Arm | Operation and inputs | Why |
|---|---|---|
| J, V | the field operations over NaN and the infinities | no engine calls them there: every getter answers NaN for an invalid date first. S runs over them alone |
| J | InLeapYear | `JSDateMath` has no member; the leap test is written inline (correspondence section 1) |
| V | LocalTime and UTC | Broiler.VM's local time is UTC by omission, with no `LocalTime` or `UTC` member (correspondence section 3) |
| V | TimeString, TimeZoneString and toString at any offset and name but zero and its own | its zone text is the constant `DateZoneText`; arm V's renderings are compared with S under a provider at offset zero named `Coordinated Universal Time` |
| J | TimeZoneString and toString with no name | Broiler.JS always writes the host zone's name |
| J | the parse outside the format's strings of E5 and its own renderings | the rule's own exclusion: the rest of its fallback stays in Broiler.JS (decision SJL-D-17) |
| V | the renderings of NaN but `toString`'s | its methods answer `Invalid Date` or throw before calling `DateToCalendarText`, `DateToUtcText` or `DateToIsoText` |
| V | the format path over strings that start with neither a sign nor four digits | `DateParseText` hands them to its textual reader |
| J | BalanceISOYearMonth; the days in an ISO year | none of the nine civil copies has a member (Date's `DaysInYear` is compared under its own name) |
| J7, J8 | a year outside `int` | their members take an `int` year; every population year fits one |
| J, J3, J6 | the weekday, the day of the year and the ISO week of a year outside `int` | as above |

Arms J2 to J9 are compared with S for each operation whose member they hold: the leap year (J2 to J8), the
month length (J2 to J7, and `JSDate.DaysInMonth` as arm J), epoch days both ways (J2, J3, J5, J6, J8, J9),
the weekday, the day of the year and the ISO week (J3, J6), and the validity (J3 to J6). Arm J's own
`IsValidISODate` also checks Temporal's limits, which stay in the engine (JSD-0072); its S is the library's
validity with the engine's own limit constants applied to S's epoch days.

## 3. The local-time arbiter before 1913

The rule's arbiter of local time is "the independent reference computation ..., whose local time reads the same
tzdata through Python's zoneinfo", and its population E4 is read "through the host's TimeZoneInfo by S's test
provider and by arm J's copy alike". **The two readers do not read the same offsets from the same files before
1913.** Read on 2026-10-15 at January 15 and July 15 of the years 1, 1800, 1883, 1900, 1950, 2011, 2021, 2100,
5000 and 9999, with tzdata 2026c, .NET 10's `TimeZoneInfo` gives New York's local mean time as -04:57 where
zoneinfo gives -04:56:02, Berlin's as +00:54 for +00:53:28, Lord Howe's as +10:36 for +10:36:20, St John's as
-03:30 for -03:30:52 until 1935's change, Kolkata's 1870 to 1905 offset as +05:22 for +05:21:10, and Apia's 1892
to 1911 offset as +12:34 where zoneinfo has -11:26:56: .NET keeps no seconds, and its adjustment rules do not
hold every change. From 1913 every reading agreed. Which reading of the tzdata is right is decision SJL-D-20's,
SJL-8's; the provider is each engine's policy (SJL-D-19).

**So the arbiter is read in two parts.** For instants from 1913-01-01 (an instant past year 9999 read 400 years
earlier as often as needed, the Gregorian calendar and the zone's rules repeating there), the reference reads
zoneinfo: `dateref.local_time`, and `dateref.utc`, which is PEP 495's `fold=0` reading of the wall time - the
offset of the earliest instant the local time names, or, for one a transition skips, of the last instant before
it, which is UTC's disambiguation (21.4.1.26). Before 1913, it reads **the offsets S's provider read from
TimeZoneInfo**, which the comparison writes beside every E4 input (`o` for LocalTime; for UTC the offsets a day
before and after, `b` and `a`, and at the instant less each, `pb` and `pa`), and computes UTC from them by the
specification's algorithm (`dateref.utc_over`), under the provider's contract that the offset changes at most
once in any two days. Before year 1 both read the zone's earliest offset at year 1's third day.

`toString` in a host zone is judged the same way, on its offset; the zone's name is the provider's, so a
difference in the name alone is judged by whether S's name follows the instant's daylight-saving time as
zoneinfo reads it.

## 4. How the comparison writes and reads

The comparison is `Broiler.ECMAScript.Algorithms.Tests`, run as `compare-date` (`ReferenceArms/Date/`), in
Release under the JIT, with `TZ=UTC` and every thread under the invariant culture. **A variant is S called as
that arm's engine will call it**, after its cut-over:

- **a parse is compared by its time value**: each arm's member answers a time value, not a kind, so S's result is
  projected onto the time value its engine makes of it - a UTC result as it is, a local one through the engine's
  `UTC` (UTC itself for Broiler.VM; the process zone, UTC, for Broiler.JS) and `TimeClip`;
- Broiler.VM's parse carries its charge after a bar: arm V's total, and S's meter **plus the one unit Broiler.VM's
  adapter charges** (JSD-0072, decision 4); arm V's `DateParseText` charges `length + 1` itself;
- Broiler.JS calls **the strict dialect, then Broiler.VM's** - its own culture-dependent reading, which stays
  between the two, is not compared;
- a rendering an engine composes differently is compared with S composed the same way: Broiler.JS's
  `ToTimeZoneString` writes the `GMT` that `TimeString` holds, so its S is `"GMT" + TimeZoneString`; its
  `toString` at a fixed offset is its body with `LocalTime(t) - t` the offset; Broiler.VM's `TimeString` is its
  clock and its zone text's `" GMT"`;
- Broiler.VM's `JsLocalTime.From`, its formatter's civil arithmetic, is compared over E1's valid time values at
  E6's offsets, with S's fields of the local time.

A result is written as text and compared exactly: a Number - a time value, an offset, a field - by its bit
pattern, so that -0 and +0 differ, an arm's integer as the Number it stands for; a string by its UTF-16 code
units; a civil date, a week and its year, a balanced year and month by their integers; a refusal by its
error's constructor; an arm's exception that is not an engine error by its type.

**The culture runs.** The rule's second run of arm J's string operations is `compare-date --culture <name>` for
each of `sv-SE`, `fa-IR` and `ar-SA`: those operations alone (the parse, the read-back renderings and the
renderings), arm J's variants alone, every thread under that culture. Each culture run is classified like the
first, and a difference only one run has is reported.

**The classification** is
[`scripts/conformance/classify_date_arms.py`](../../../scripts/conformance/classify_date_arms.py), written with
this file and before the judged run, against `dateref.py`; its docstring states each class. Two readings are
the rule's own made explicit:

- **Broiler.VM's dialect against arm V.** Any difference is class (c), except on correspondence row P9's
  strings, those holding a token of a minus sign and digits that S reads as the year and arm V refuses: on an
  engine's rendering of a negative year S's reading back the rendered time value without its milliseconds is
  class (a); elsewhere such a string is outside the format, so class (b).
- **Class (b) for arm J.** A string outside the format that S and arm J read differently is class (b): the
  specification admits both, and Broiler.JS's own reading, which stays, answers before Broiler.VM's dialect.

**"Every class (b) difference is in a vector at a named corpus revision"** is read as: every class (b)
difference is listed in `classification.tsv.gz` with its kind (the mutation or shape that puts its string
outside the format, its engine and its correspondence row where one names it), and **each kind of each engine
is held by at least one `impl-defined` vector** at the corpus revision the bundle names. The reason is
arithmetic: E5F holds 5,885,919 strings, of which every mutation is outside the format, so the class's
differences are expected by the hundred thousand, and a vector each would multiply the corpus by ten.

**The negative controls** are `compare-date --controls`, restricted to the two operations they change: arm V's
`DateParseText` charging one unit more for E5F's `"1970"`, and arm J's `JSTemporalPlainDate.DaysFromCivil`
answering one day more for 1970-01-01. Each must be reported as exactly one difference, and the classifier
must leave it unclassified. The control code is then removed, and a patch that restores it is retained.

## 5. Retention

As the rule says: `summary.json`, `classification.json`, `report.md`, the population's `manifest.json`, the
negative controls' runs, the tzdata release and the commands of every step are retained in
[`reference-arms-run/`](reference-arms-run/); the two listings are not committed, and the bundle records each
one's SHA-256 and line count. A run with a line limit or an operation filter is void. Arm J's local time and
zone name outside .NET's range depend on the moment of the run (row L4), so each summary records when it
started.
