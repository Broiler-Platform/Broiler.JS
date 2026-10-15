# Bundle SJL-6-001's reference-arm rule, revised 2026-10-15 after its first judged run was seen: two defects of S fixed, the local-time arbiter's boundary, and P9's attribution

**Written:** 2026-10-15, at milestone SJL-6.3, **after the first judged run of the comparison had been seen**
(started 2026-10-15T18:00:59Z at `5501705`) and before the run that judges. Its decisions on the two points of
section 2 are therefore weaker evidence than the rule's and its first revision's, which were written before
any run. **Owner:** the Broiler.JS maintainer. **Reviewer:** none (the programme's exception EX-30).

**What it revises, and what it does not.** The rule, quoted whole at the end of this file as it asks, and its
revision of the same day written before any run,
[`reference-arms-revised-2026-10-15.md`](reference-arms-revised-2026-10-15.md), stand except where section 2
below replaces that revision's text, which it quotes. The arms, the population, the operations, the admitted
classes, the negative controls and the MET line are unchanged. Both runs are retained.

## 1. What the first run showed

The first run's classification listed 336 differences unclassified, beside 426,962 of class (a) and 121,562 of
class (b). Read one by one, they had four causes:

| Cause | Differences | What it is |
|---|---|---|
| `EcmaTime.Day(-0)` answered -0 | 1 | **A defect of S, class (c).** Day is 𝔽 of the mathematical floor, which is +0; arm J and the reference answer +0. Fixed: Day answers +0, with a test |
| Broiler.VM's reader keeps an earlier clock's seconds when a later clock has two parts, and S's reset them | 6 | **A defect of S, class (c)**: S's Broiler.VM dialect is that engine's reading exactly but for P9 (JSD-0072, decision 1). Fixed: a clock reads only the parts it has, with a test |
| local time in St John's from 1913 to 1935 | 252 | the arbiter, section 2 |
| a classifier that read E5's strings once escaped, judged a rendering's year in UTC, and took a string to be P9's only when arm V refused it | 77 | defects of `classify_date_arms.py`, whose docstring records each; section 2 for P9 |

The reference also stopped on an instant in the last hours of year 9999 in a zone east of UTC, whose local
date zoneinfo cannot hold; `dateref.zone_offset_ms` now reads such an instant 400 years earlier, as it already
read every instant past 9999. No result it gave changed.

## 2. What is revised

**The local-time arbiter's boundary.** The first revision's section 3 read, in its words: "From 1913 every
reading agreed." and "For instants from 1913-01-01 (an instant past year 9999 read 400 years earlier as often
as needed, the Gregorian calendar and the zone's rules repeating there), the reference reads zoneinfo: ...
Before 1913, it reads **the offsets S's provider read from TimeZoneInfo** ...". The reading it rested on held
no year from 1901 to 1949, and St John's offsets of -03:30:52 and -02:30:52, which .NET reads as -03:30 and
-02:31, last until 1935. **Now the reference reads zoneinfo wherever the offsets S's provider read, listed
beside the input, are zoneinfo's at every instant concerned, and the listed offsets wherever they are not**,
with the same two algorithms. That is the first revision's intent - zoneinfo where the two readers agree - with
no date standing in for it.

**P9's attribution.** The first revision's section 4 read: "Any difference is class (c), except on correspondence
row P9's strings, those holding a token of a minus sign and digits that S reads as the year and arm V refuses".
A signed year can also make a later number the day rather than the year, with both arms answering; and whether
S read the token as the year is S's own behaviour, which a classifier should not assume. **Now the comparison
lists, for each Broiler.VM-dialect string holding such a token, S's reading of the string without those tokens,
which Broiler.VM's reader skips; a difference is P9's when that reading is arm V's**, and is then class (b)
outside the renderings, as before. On an engine's rendering the test is the reference's, as before, of a year
of at most 0, since New York renders year 0's first hours as -0001.

## 3. The rule, quoted whole

> # Bundle SJL-6-001's reference-arm rule: the shared Date algorithms against both engines' pre-cut-over implementations
>
> **Written:** 2026-10-15, at milestone SJL-6 (work package SJL-6.1) of the shared JavaScript libraries
> programme, before the Date area exists in `Broiler.ECMAScript.Algorithms`, and before either arm, the
> area's vectors or the generator this rule's population names exist. **Milestone judged:** SJL-6,
> `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-6-date.md`, its home clauses (their
> reference-arm half) and work package SJL-6.3. **Owner:** the Broiler.JS maintainer, as the home's owner.
> **Reviewer:** none. Every role this rule names, the JavaScript profile owner whose engine supplies one arm
> included, is held by one person (the programme records this as Broiler.VM's exception EX-30), and this
> rule does not claim that anyone independent has read it.
>
> **What this file is.** The predeclared decision of the library-level reference-arm comparison of the
> Date area, in template T4 of the programme's validation document,
> `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4, with the procedure of its section
> 3, in the shape bundles `sjl-4-001` and `sjl-5-001` took. It declares the population: every class of
> input, and the seeds of the random parts. **It holds no figure**: the counts and seeds below are parameters
> of a population declared before any run, not results of one.
>
> **The members each arm copies** are the ones the area verdict's correspondence names, by file and line:
> Broiler.VM's `docs/evidence/sjl-6-001/correspondence.md`, read at Broiler.VM `9c3d809` and this
> repository's `bc3597f`, with its rows T, D, G, L, P and R.
>
> ```rule
> evidence class: conformance at library level (not a measurement)
> arms:
>   S  the shared implementation: Broiler.ECMAScript.Algorithms at the commit under test, called through its public API: EcmaTime, IsoCalendarMath, the local-time provider's LocalTime and UTC, EcmaDateParser in each dialect, and EcmaDateFormatter
>   J  a test-only copy, in the home's test project, of this engine's implementation at bc3597f: JSDateMath's arithmetic from Day to TimeClip, with MakeFullYear as Date.UTC writes it inline; JSDateMath's LocalTime and UTC; the civil members of JSTemporalPlainDate (IsLeapYear, its month length, DaysFromCivil, CivilFromDays, IsoDayOfWeek, IsoWeek, WeeksInIsoYear, IsValidISODate and its inline day of the year), and, as arms J2 to J9, the civil members of each of the eight other types correspondence section 2 names; JSDate.TryParseExtendedIso and DateParser.Parse; and DateString, ClockString, HumanYearString, ToTimeZoneString and ToIsoString with toUTCString's body
>   V  a test-only copy, in the same project, of Broiler.VM's implementation at 9c3d809: the Date* arithmetic members from DateFloorMod to DateTimeClip, with MakeFullYear as DateFromFields writes it inline; JsTemporalCore's civil members, JsZone's DaysFromCivil, MonthLength and Weekday, and JsLocalTime.From's civil arithmetic; DateParseText with DateParseTextual and its helpers, its charge kept; and DateToFullText, DateToCalendarText, DateToClockText, DateToUtcText and DateToIsoText
>   each copy records the source path and git blob of every file it copies; where a member takes an engine type, the copy takes the CLR value that type carries, and where it reads the host's zone (TimeZoneInfo.Local) or the current culture, the copy takes the zone or the culture as a parameter; each change is recorded beside it; arm V's charge delegate is kept, and every charge it makes is recorded; a member that throws is copied to throw a recorded exception instead; an arm whose copy changes a result or a charge is a defect of the arm, fixed in the arm, and the comparison is run again
> operations: Day, TimeWithinDay, DaysInYear, DayFromYear, YearFromTime, InLeapYear, MonthFromTime, DateFromTime, WeekDay and the hour, minute, second and millisecond extractors over time values; MakeTime; MakeFullYear; MakeDay, MakeDate and TimeClip each alone over the inputs where the specification's result is a time value or NaN, and composed as TimeClip(MakeDate(MakeDay(y, m, d), MakeTime(h, mi, s, ms))), the core of Date.UTC; the leap year, the month length, the days in a year, epoch days in both directions, the weekday from Monday 1 to Sunday 7, the day of the year, the ISO week and its year, BalanceISOYearMonth and the ISO date's validity; LocalTime and UTC under a provider over each zone below; the parse of each string below in the strict dialect and in Broiler.VM's; and DateString, TimeString, TimeZoneString, the UTC rendering and the ISO rendering. An arm is called only for the operations it implements, and the bundle names each operation an arm does not implement. Argument conversion, storage, receiver checks and error construction stay in the engines' adapters and are not compared, except that a refusal is compared by its kind
> population:
>   E1  time values: -8.64e15 and 8.64e15, each with the integers 1 ms inside and outside it; every day's first and last millisecond in years -1 to 1, 1969 to 1972 and 9999 to 10000; -0, 0.5, -0.5, 1.5, -1.5, 2^53, NaN, -Infinity and +Infinity; and 2^16 integers drawn with seed 0x534A4C362D303031 from [-8.64e15, 8.64e15], a draw being a SplitMix64 output read as stated in the generator's README
>   E2  components: each of -Infinity, -2^63, -1e19, -2^53 - 2, -(2^53), -1e15 - 1, -1e15, -400, -13, -12, -1.5, -1, -0.5, -0, +0, 0.5, 1, 1.5, 11, 12, 13, 99, 100, 2^31, 2^32, 2^53, 1e15, 1e15 + 1, 1e19, 2^63, +Infinity and NaN in each argument of MakeTime, MakeDay and MakeFullYear in turn, the others held at the epoch's; and 2^16 tuples drawn with seed 0x534A4C362D303032 from those values and the integers -1000 to 1000
>   E3  civil dates: every date of years -1200 to 2800, every month 1 to 12 and every day 1 to 31, and days 0, 32 and months 0 and 13 for the validity check; the years -271821, 275760, -(2^31), 2^31 - 1, ±1e9 and ±(1e9 + k) for k from 0 to 400, with months 1 to 12 and days 1, 28, 29, 30 and 31; every epoch day from -1,000,000 to 1,000,000; 2^16 epoch days drawn with seed 0x534A4C362D303033 from ±(1e11); for BalanceISOYearMonth, the years above and months from -25 to 25 and at ±(2^53)
>   E4  local time: the zones UTC, America/New_York, Europe/Berlin, Asia/Kolkata, Australia/Lord_Howe, America/St_Johns and Pacific/Apia, each read through the host's TimeZoneInfo by S's test provider and by arm J's copy alike; for each, every transition in its data from 1800 to 2100, at the transition and at 1 ms, 1 min, 30 min, 1 h and 2 h on either side, read once as a UTC instant (LocalTime) and once as a local time (UTC); the local times of years 1, 1700, 9999, 10000, -1 and 100000; and 2^14 instants drawn with seed 0x534A4C362D303034 from years 1800 to 2200
>   E5  strings: every combination of the format's shapes - a four-digit or a signed six-digit year, with or without a month, a day, and a time of HH:mm, HH:mm:ss or HH:mm:ss.sss, with Z, an offset or neither - over the values 0000, 0001, 1969, 1970, 9999, 10000, -000000, ±000001, ±271821 and ±275760 for the year, 00, 01, 02, 12 and 13 for the month, 00, 01, 28, 29, 30, 31 and 32 for the day, 00, 23, 24 and 25 for the hour, 00, 59 and 60 for the minute and the second, 000 and 999 for the fraction, and ±00:00, ±05:30, ±23:59 and ±24:00 for the offset; each also mutated to a lower-case t and z, a space for the T, zero, one, two, four, nine and ten fraction digits, an offset without its colon, and a leading and a trailing space; the toString and toUTCString renderings of E1's valid time values in each engine's own form, Broiler.JS's in UTC and in America/New_York; Broiler.VM's fallback shapes: the twelve month abbreviations and their full names in three letter cases, weekdays that match and do not, GMT and UTC alone and with every sign and hhmm of 00 to 24 hours and 00 to 99 minutes, years of one to six digits with and without a sign, day-first and month-first orders, comma and tab separators, and a parenthesised suffix; and 2^16 strings drawn with seed 0x534A4C362D303035 from a grammar of those tokens, each mutated by one character
>   E6  renderings: E1's valid time values at the offsets 0, ±1 min, ±30 min, -4 h 56 min 2 s, +5 h 30 min, ±14 h and ±(24 h - 1 min), each with no name, an ASCII name and a name outside ASCII
>   every operation runs over every class whose inputs it takes
> configuration: the home's test project, built in Release and run under JIT only, never in the Native AOT probe; the process's current culture set to the invariant culture and its zone to UTC, and arm J's string operations run a second time under the sv-SE, fa-IR and ar-SA cultures; the zones of E4 read from the host's tzdata, whose release is recorded; the population generated by the vector generator SJL-6.2 builds, at the commit the bundle names, its SplitMix64 implementation tested against the generator's published reference outputs
> comparison: per operation, input and arm, S's result against the arm's result, exactly: a time value or an offset by its bit pattern, so that -0 and +0 differ; an integer by its value; a string by its UTF-16 code units; a parse by its kind (UTC, local or invalid) and its value; and, for every operation arm V implements with a charge, the total charge S makes through its meter, with the one unit this family's adapter adds, against the total charge arm V makes, exactly. Arm J's parse is compared over the format's strings of E5 and over its own renderings only: the rest of its fallback stays in Broiler.JS (decision SJL-D-17) and is not compared. Arm V's parse is compared with S's Broiler.VM dialect over all of E5, and its format path with S's strict dialect over the format's strings
> arbiter: which side of a difference is right is decided by the specification's algorithm, computed by the independent reference computation of the vector generator, written in Python from the specification's text, whose local time reads the same tzdata through Python's zoneinfo; never by the agreement of the arms. A charge has no arbiter but arm V: Broiler.VM's units are the specification of the charge (JSD-0072, decision 4). Broiler.VM's dialect has no arbiter but arm V, except in its renderings of negative years (correspondence row P9), which the reference reads as the specification's expectation of Date.parse
> admitted difference classes:
>   (a) correction - an arm is wrong and S is right; listed with the engine it corrects (J, V or both), the input and the clause or the reference value that shows it, and, where the area verdict's correspondence names it, its row (T3, T4, G2, L2, L3, L4, P1, P8, P9, R3)
>   (b) implementation-defined - the specification admits both results: a string outside the format, where S's dialect answers that engine's reading; listed, and moved into a vector at a named corpus revision
> every other difference is class (c), a defect of S, which is fixed before either cut-over lands, after which the whole comparison is run again and both runs are retained; a charge of S that differs from arm V's is always class (c), and so is a difference between S's Broiler.VM dialect and arm V on any string but P9's
> negative control: a copy of arm V with one charge deliberately changed for one input of E5, and a copy of arm J with one result deliberately changed for one input of E3, are each run once; the comparison reports each as a difference and does not classify it; the copies are then removed
> MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a) or (b) and is listed, every class (b) difference is in a vector at a named corpus revision, no class (c) difference remains, every charge of S equals arm V's, and both negative controls were reported unclassified.
> NOT MET otherwise.
> ```
>
> **How it is read.** As bundle `sjl-5-001`'s rule: the comparison writes `summary.json` and
> `differences.tsv.gz`, and **classifies nothing**. The classification is the owner's, written as a script
> before the judged run and applied mechanically, into `classification.tsv.gz`, `classification.json` and
> `report.md`. **"Listed"**, in the MET line, is read as present in `classification.tsv.gz` with its class.
> **Arms J2 to J9** are this engine's other civil copies: a copy whose answers differ from arm J's is listed
> by name, so that the cut-over knows which copy held which defect.
>
> **What the bundle retains.** `summary.json`, `classification.json`, `report.md`, the population's
> `manifest.json`, the negative controls' runs and their arms' sources, the tzdata release, and the commands
> of every step. The two listings are **not committed**: the bundle records the SHA-256 of each one's
> uncompressed text and its line count, and each is regenerated byte for byte from the commit the bundle
> names and the population, itself regenerated by the generator and checked against the manifest's digests.
> A run that stopped before the end of any operation's population is void and is not judged.
>
> **Revisions.** A rule revised after any run it judges has been seen is a new dated file in this
> directory, quoting this one whole and saying it was written afterwards; its decision is then weaker
> evidence. Before SJL-6.3's code lands, a later bundle shows this file unchanged. The arms are retired
> at SJL-16, or kept as a frozen regression population by decision.
>
> ## What it does not decide
>
> - **Either engine's cut-over.** Engine-level vectors, test262 in either zone, Broiler.VM's fuel parity
>   and this engine's storage are judged by each engine's own rule; this population meets no test262 clause.
> - **Which engine is better.** A class (a) difference names the engine it corrects; the arms are
>   compared with S and with the reference, never with each other.
> - **The drivers.** Argument conversion and its order, storage, the getters and setters, `toJSON`,
>   `toLocale*` and this engine's culture-dependent fallback stay in the engines and are compared only
>   through test262 and the engine-level vectors.
> - **A time-zone dataset.** E4 reads the host's tzdata, the same in S's test provider, in arm J and in the
>   reference; SJL-8 chooses the shared one.
> - **Performance.** Nothing here is timed.
