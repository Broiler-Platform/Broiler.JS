# Changelog — the shared JavaScript libraries

Every release of the `Broiler.ECMAScript.*` packages has an entry here, newest first. The packages
follow semantic versioning under [convention 16](docs/conventions.md):

- a public API removal or a binary-breaking change is a major version;
- a new API is a minor version;
- a correction toward the specification is a minor version, with its own entry under
  **Observable changes**;
- a data-version change is a minor version, with its own entry under **Data**.

Each entry has the headings **Added**, **Changed**, **Observable changes** and **Data**, and says
"none" under a heading with nothing to report. A release without an entry here is not published.

## Unreleased

Not yet packed under a version of its own: the stream's next version is set when this entry is published.

### Added

- `Broiler.ECMAScript.Generation`: the Intl data home's generator, namespace
  `Broiler.ECMAScript.Generation.Intl` (milestone SJL-9.2b; Broiler.VM's decision JSD-0082, decisions D10, D11
  and D12). Ported from Broiler.VM's `CldrTableGenerator`, `UnicodeDatabase` and `CldrPin` at Broiler.VM
  `4cf9ca44`, with their behaviour unchanged for Broiler.VM's parameters:
  - `IntlCldrPin` and `IntlUcdPin`, with `IntlPinEntry`: each pin parsed from the text its caller hands in, and
    every file, its length, its SHA-256 and the stated CLDR, UCA and Unicode versions compared with it before
    anything is read, the UCA version tied to the Unicode version;
  - `CldrUcd`: the subset of the Unicode Character Database the collation tables need - categories, combining
    classes, canonical decompositions and the Unified_Ideograph ranges - read from `UnicodeData.txt` and
    `PropList.txt`;
  - `IntlLocaleList` and `IntlGeneratorOptions`, with `IntlNumberingSymbols`: decision D11's parameters, the
    locales the data supports, those whose CLDR main data is written, the languages whose plural, ordinal,
    plural-range and day-period rules are written, which numbering systems' symbols the number table carries
    (decision D21), the calendars and the collation tailorings;
  - `CldrTableGenerator.Generate` and its `IntlTables` and `IntlGeneratedTable`: Broiler.VM's tables - likely
    subtags, aliases, extensions, numbers, currencies, units, plurals, ordinals, plural ranges, day periods,
    dates, calendars, zone and metazone names, hour cycles, weeks, lists, relative times, display names,
    scripts, the root collation and the tailorings;
  - `IntlTableSource.Write`, with SJL-8's `TableSourceOptions`: the tables as a C# file for a namespace and class
    the caller chooses, which with Broiler.VM's options writes its `JsCldrTables.g.cs` byte for byte before its
    assurance pass;
  - `IntlTableFiles.Write` and `FileName`: the tables as raw files, one per table, and a `tables.manifest`
    naming the data versions and each file's length and SHA-256;
  - `IntlDataVersions`: `CldrVersion` (`48.2.0`), `CldrMajor` (`48`), `UcaVersion` and `UnicodeVersion`
    (`17.0.0`).

  The package still reads no file and carries no table and no pinned input (decision SJL-D-6 (c)): those are
  files of this repository, under [`data/intl/`](data/intl/README.md).
- `Broiler.ECMAScript.Intl`, a new library: the ECMA-402 algorithms over the locale data an engine hands in
  (milestone SJL-9.3a; Broiler.VM's JSD-0082, decisions D3, D8, D9, D18, D19 and D32; design
  [`docs/intl/design.md`](docs/intl/design.md)). It is **not packed** until stage SJL-9.3f, so that no version of the
  stream carries part of it (decision D29). Ported from Broiler.VM's JavaScript profile at `4cf9ca44`, with ASCII
  casing and `NumberFormatInfo.InvariantInfo` where Broiler.VM made a culture call (decision D19), a status where it
  threw, and the caller's `WorkCharge` on every operation whose work grows with a tag, an option or a list it is given,
  charged before the work in the units each documents: one per code unit of a tag or String it parses or checks, one
  per requested locale a matcher or FilterLocales examines, and, where it canonicalises, one per alias-table key each
  round of alias replacement looks up, a number that grows with the square of the tag's variants (decision D18). The
  members that read an identifier already made, and ResolveLocale over a match already made, whose making was charged,
  take none, nor do the checks of a language, script or region subtag, which read at most eight code units:
  - **the data contract** (decision D8): `IIntlDataSet`, the tables an engine hands in, and `IntlTable`, which keeps
    Broiler.VM's `JsIntlTable` numbers with 7, 23 and 24 retired; `TableLocaleData`, the decoder implementation of the
    question-level `ILocaleData`, each table decoded once per process, with the records it answers in
    (`CurrencyNames`, `RegionWeek`, `RegionHourCycles`, `ListPatterns`, `DayPeriodRule`, `PluralRuleText` and
    `PluralRuleType`); `CompositeLocaleData`, which asks the shared data first, then an optional secondary provider
    at a locale the shared data neither lists nor holds (decision D6), then the locale's parent and root; and
    `IntlStatus`;
  - **locale identifiers**: `LocaleId`, with `LocaleKeyword` - IsWellFormedLanguageTag over ASCII alone,
    CanonicalizeUnicodeLocaleId over CLDR's aliases and BCP 47 types, Add and Remove Likely Subtags, a tag's parts and
    keywords. A subdivision alias whose replacement is a region is written as UTS #35 Annex C writes it, `axzzzz`
    (row L9, a correction of Broiler.VM);
  - **negotiation**: `LocaleNegotiation`, with `LocaleMatch`, `RelevantKey` and `ResolvedLocale` -
    CanonicalizeLocaleList's steps over the elements' Strings, LookupMatchingLocaleByPrefix, best fit as the lookup
    matcher (row L38), FilterLocales, and ResolveLocale over the default locale, the available locales and the
    relevant keys' values an engine gives;
  - **Intl.Locale**: `LocaleRecord`, the constructor's steps after an engine has read its options (`LocaleOptions`),
    each option checked over ASCII alone; and `LocaleInfo`, with `LocaleWeekInfo`, `LocaleRegionPreference` and
    `ICollatorLocaleData` for the engine's Collator data - calendars, collations, hour cycles, numbering systems, time
    zones, text direction and week info. A locale whose language the Collator lacks answers DefaultLocale's
    collations, and only `und` answers `emoji` and `eor` (row L18, a correction of Broiler.VM);
  - **`SupportedValues`**: `Intl.supportedValuesOf`'s lists, with `AvailableCalendars` and `SanctionedUnits`.
- `Broiler.ECMAScript.Intl`, sub-area (b) (stage SJL-9.3b; Broiler.VM's JSD-0082, rows M and P, decisions D18, D19 and
  D20): the Intl mathematical value, the digit options, FormatNumericToString, and plural rules and ranges, ported from
  Broiler.VM's JavaScript profile at `4cf9ca44` with `NumberFormatInfo.InvariantInfo` and ASCII casing where it called
  the invariant culture (decision D19), a status where it threw, and the caller's `WorkCharge` where Broiler.VM charges,
  in its units (decision D18):
  - **`IntlDecimal`**, with `IntlDecimalKind`: ToIntlMathematicalValue's value - a Number by its `Number::toString`
    digits (row M1a), a BigInt by its exact digits (row M3), a String by StringIntlMV held to the Number range as
    RoundMVResult holds it (rows M2a and M2b), negative zero kept (row M8). A String is charged `length / 8 + 1` and a
    BigInt through its decimal digits; the BigInt policy a prefixed String is read under is the caller's;
  - **the digit options**: `DigitOptionRules` - SetNumberFormatDigitOptions' decisions after an engine's reads
    (`Plan`, `DefaultNumberOption`'s range, `IsAdmittedRoundingIncrement`, `SetNumberFormatDigitOptions`) and the
    fraction-digit defaults `Intl.NumberFormat` hands it (`NumberFormatDefaults`, row M5) - with `DigitOptionValues`,
    `DigitOptionsPlan`, `FractionDigitDefaults`, `DigitOptions`, and the option enumerations `NumberStyle`,
    `NumberNotation`, `CompactDisplay`, `NumberRoundingType`, `RoundingPriority` and `TrailingZeroDisplay`;
  - **`NumericFormat.FormatNumericToString`**, with `FormattedNumeric`: ToRawFixed and ToRawPrecision over the decimal
    digits, both rounding priorities, the admitted increments, `stripIfInteger` and the minimum integer digits (rows M6
    and M7), the rounding modes made unsigned by `ExactRounding`, charged `digits / 16 + 1`;
  - **`PluralEngine`**: a language's CLDR rules of one type, parsed once, with the operands `c` and `e` (decision D20),
    PluralRuleSelect over a decimal String (charged one unit per code unit), PluralRuleSelectRange over the language's
    plural ranges (row P5b, an adoption) and the categories in `resolvedOptions`' order;
  - **`PluralRulesRecord`**, with `ResolvedPlural`: an `Intl.PluralRules`'s slots and ResolvePlural and
    ResolvePluralRange over them (rows P1, P2, P5a, P5c, P6 and P8). With compact notation it passes the exponent
    ComputeExponent gives over the language's compact patterns, where Broiler.VM passed 0 (row P4): none of Broiler.VM's
    `de` and `en` rules reads `c` or `e`, so none of its answers moves;
  - **`IntlStatus`** gains `NumberOptionOutOfRange`, `RoundingIncrementNotAdmitted`, `FractionDigitsOutOfOrder`,
    `RoundingIncrementNeedsFractionDigits` (SetNumberFormatDigitOptions' `TypeError`),
    `RoundingIncrementNeedsEqualFractionDigits` and `RangeIsNotANumber`.
- `Broiler.ECMAScript.Intl`, sub-area (c) (stage SJL-9.3c; Broiler.VM's JSD-0082, rows N and S, decisions D13, D18, D19,
  D21 and D33): number formatting and the list, relative-time and display-name formatters, ported from Broiler.VM's
  JavaScript profile at `4cf9ca44` with `NumberFormatInfo.InvariantInfo` and ASCII casing where it made a culture call
  and its `Trim()`'s white space written out (decision D19), a status where its realm threw, and the caller's `WorkCharge`
  where Broiler.VM charges, in its units (decision D18). Each formatter reads the locale data at ResolveLocale's data
  locale through the `ILocaleData` it is handed, ordinarily a `CompositeLocaleData`, where Broiler.VM read the data
  locale's language; for Broiler.VM's tables the two are the same lines:
  - **`NumberFormatter`**, with `NumberFormatOptions`, `NumberPart` and the enumerations `CurrencyDisplay`,
    `CurrencySign`, `UnitDisplay`, `NumberGrouping` and `SignDisplay`: PartitionNumberPattern in three layers - the
    patterns and signs (rows N1, N2 and N7), compact notation with its exponent taken after rounding over the exact value
    (row N6), currencies with CLDR's currency spacing and names by the number's plural (rows N3 and N4), units, a compound
    with no CLDR pattern composed from its two units and the percent unit's percent pattern (rows N5 and N2c, decision
    D35) - PartitionNumberRangePattern with ICU's collapse, the separator's spacing and the collapsed name's plural range
    (row N9), and the parts. A numbering system's digits replace the ASCII ones (rows N8 and N17), and a system other than
    `latn` writes its own symbols where the data carries them, at the data locale then at root, and the `latn` symbols
    otherwise (decision D21); `NumberingSystemKeyValues` gives the `nu` values with the locale's default numbering system
    first (decision D13), `latn` for every locale of Broiler.VM's list. `IsWellFormedCurrencyCode` and
    `IsWellFormedUnitIdentifier` are the checks an engine makes at their steps. FormatNumericToString's
    `digits / 16 + 1` is charged at every rounding, ComputeExponent's and both of a range's ends' included;
  - **`ListFormatter`**, with `ListFormatType`, `FormatStyle` and `ListPart`: CreatePartsFromList over CLDR's list
    patterns, one template per type and style (rows S1 to S3), one unit charged per template step;
  - **`RelativeTimeFormatter`**, with `RelativeTimeNumeric` and `RelativeTimePart`: PartitionRelativeTimePattern with its
    own number format in the resolved numbering system (row S5) and plural rules (row S6), the magnitude written in the
    tense's pattern (row S7, decision D33 (a)), and SingularRelativeTimeUnit;
  - **`DisplayNamesCore`**, with `DisplayNamesType`, `DisplayNamesFallback` and `LanguageDisplay`:
    CanonicalCodeForDisplayNames by ASCII casing (row S9) and CLDR's names, a language's composed as ICU composes it (row
    S8, an adoption);
  - **`IntlStatus`** gains `RelativeTimeValueNotFinite`, `InvalidRelativeTimeUnit` and `InvalidDisplayNamesCode`.

  The library-level runner answers sub-area (c)'s vectors, `intl/numbers-and-text.vectors`' and the `format` and
  `formatToParts` vectors of `intl/values-and-plurals.vectors`, through `IVectorRecordLibrary`, and the Native AOT probe
  holds them; the bound `format`, the legacy constructor, the option reads after construction and StringListFromIterable
  stay the engines'.
- `Broiler.ECMAScript.Intl`, sub-area (d) (stage SJL-9.3d; Broiler.VM's JSD-0082, rows T and Z8, decisions D13, D15, D18, D19
  and D33): date-time formatting, intervals, day periods, hour cycles and zone names with metazone history, and Intl over
  Temporal, ported from Broiler.VM's JavaScript profile at `4cf9ca44` with `NumberFormatInfo.InvariantInfo` where it formatted
  an integer with the invariant culture (decision D19, row T41), a status where its realm threw, and the caller's `WorkCharge`
  where Broiler.VM charges, the formatted output's length (decision D18, row T40). Each format reads the locale data at
  ResolveLocale's data locale through the `ILocaleData` it is handed, where Broiler.VM read the data locale's language; for
  Broiler.VM's tables the two are the same lines:
  - **`DateTimeFormat`**, with `DateTimeFormatOptions`, `DateTimeComponents`, `DateTimeRequired` and `DateTimeDefaults`:
    CreateDateTimeFormat's steps after an engine's reads and ResolveLocale over `CalendarKeyValues`, `HourCycleKeyValues`
    and `NumberingSystemKeyValues` (the locale's default numbering system first, decision D13) - the hour cycle by `hour12`,
    the `hc` keyword and the region's time data (rows T9 and T10), the deprecated Hijri calendars' fallback to
    `islamic-tbla` (row T33), a calendar's layer of the date data, the style patterns with CLDR's `atTime` joins (row T12)
    and the components' best pattern through the pattern generator (row T13) - and `resolvedOptions`' components from the
    selected pattern (row T7), with no hour cycle for a pattern without an hour (row T8, decision D33 (b)). The formats of
    Temporal's types (the proposal's GetDateTimeFormat and AdjustDateTimeStyleFormat), and HandleDateTimeValue and the
    range's checks over a `DateTimeValue` of `DateTimeValueKind`, a Number or a Temporal object's slots: a plain value
    written at UTC and, unlike a Number, not clipped (`377fb80e`, row T38), the calendar checked before the type's fields
    (row T39);
  - **`DateTimeFormatter`**, with `DateTimePart`: FormatDateTimePattern as ICU's SimpleDateFormat writes a pattern - names in
    the data locale and calendar (rows T14 and T35), a calendar's fields and the day's weekday (row T34), a numbering
    system's digits (row T37), flexible day periods with noon (rows T17 to T20) - and PartitionDateTimeRangePattern as ICU's
    DateIntervalFormat writes a range over every field of the two dates (rows T29 to T31), each charged its output's length,
    for any integral time within the ISO dates of Temporal's values (`MinTime` to `MaxTime`, beyond TimeClip's range);
    behind them `DateTimePatternGenerator`, `DateIntervalFormatter` and `ZoneNamer` (specific, generic, partial-location and
    location names over CLDR's metazone history, and the localized GMT format with seconds, rows T22 to T26);
  - **`ResolvedTimeZone`**, with `TimeZoneKind`: a `timeZone` option's String resolved to an offset or an available named
    time zone, answering both the identifier as given in the database's case, which [[TimeZone]] reports, and its primary,
    by which the zone is named (decision D15; row Z8, a correction of Broiler.VM, and row T26, which keeps Broiler.VM's
    names); one unit charged per code unit of the option;
  - **`DateTimeData`**: the locale data, zones and calendars an engine hands every format, with the pattern generators and
    the zone names made once;
  - **`ILocaleData`** gains the metazone questions `MetazonePeriods` (with `MetazonePeriod`), `GoldenZone` and `PrimaryZone`,
    which `TableLocaleData` decodes from the `MetaZones` table and `CompositeLocaleData` asks as world data;
  - **`IntlStatus`** gains `TimeZoneOffsetHasSeconds`, `InvalidTimeZone`, `StyleWithComponents`, `DateFormatWithTimeStyle`,
    `TimeFormatWithDateStyle`, `TimeValueOutOfRange`, `TemporalTypeNotFormatted`, `TemporalCalendarMismatch`,
    `ZonedDateTimeNotFormatted` and `RangeOfDifferentTypes`.

  The library-level runner answers sub-area (d)'s vectors, `intl/date-times.vectors`' fourteen operations but the engine-level
  script forms, and the Native AOT probe holds them; the option reads and their spies, the bound `format`, the legacy
  constructor, ToDateTimeFormattable's ToNumber, a Temporal object's slots, the parts arrays and the default zone stay the
  engines'.
- `Broiler.ECMAScript.Intl`, sub-area (e) (stage SJL-9.3e; Broiler.VM's JSD-0082, rows U1 to U22, decisions D18, D19, D21, D24
  and D34): duration formatting, ported from Broiler.VM's JavaScript profile at `4cf9ca44` (`JsRealm.DurationFormat`) over the
  library's number and list formatters, with ASCII digits written by `BigIntText` where it wrote a fraction's integer with the
  invariant culture (decision D19, row U20), a status where its realm threw, and the caller's `WorkCharge` where Broiler.VM charges,
  in its units (decision D18, row U19): one per unit visited, and for each number and list format it makes the unit its
  constructor charges for the locale, each FormatNumericToString, a fraction's String read as a decimal (`length / 8 + 1`) and each
  step of the list's template. The data is read at ResolveLocale's data locale through the `ILocaleData` it is handed, where
  Broiler.VM read the data locale's language; for Broiler.VM's tables the two are the same lines:
  - **`DurationFormatCore`**, with `DurationUnit`, `DurationFormatStyle`, `DurationUnitStyle`, `DurationUnitDisplay`,
    `DurationUnitOptions`, `DurationDigitalFormat` and `DurationPart`: GetDurationUnitOptions with ValidateDurationUnitStyle after
    an engine has read a unit's two options - the display default `always` kept for minutes and seconds after a numeric unit (row
    U1, a correction of Broiler.JS), a sub-second `numeric` made `fractional` with the display default `auto` (row U2), the
    refusals (row U3) and the other option results (row U4); [[DigitalFormat]] from CLDR's `symbols.timeSeparator` and
    `duration.hms` at the data locale (rows U13 and U14, decision D34); and PartitionDurationFormatPattern over a valid duration
    record, Temporal's `DurationFields` - the clock (row U5), the sign on the first unit written (row U6), exact fractions
    truncated to `fractionalDigits` or nine digits (rows U11, U20 and U21), the parts with their units (row U12), each number
    in the format's numbering system with its own symbols where the data carries them (rows U15 and U16, decision D21), and the
    whole joined by a unit list format;
  - **`IntlStatus`** gains `FractionalDurationUnitAlwaysDisplayed`, `DurationUnitAfterFractional` and `DurationUnitAfterNumeric`.

  The library-level runner answers sub-area (e)'s vectors, `intl/durations.vectors`' eight operations but the engine-level
  script forms, and the Native AOT probe holds them; the reads of the argument (ToTemporalDuration, a String through Temporal's
  grammar, or ECMA-402's ToDurationRecord, rows U7 to U10 and U22), the option reads, `supportedLocalesOf`, the parts arrays and
  `resolvedOptions`' object stay the engines'.

  Decision D34's reading moves more than the `digital` answers it names. [[TwoDigitHours]] is true for `af` and `lt` in
  Broiler.JS's data, and GetDurationUnitOptions makes the hours `2-digit` whatever style they resolved (ECMA-402's step,
  as Broiler.VM ports it), the minutes and seconds after them too; so in those two locales a duration with hours is written
  as a clock in the `long`, `short` and `narrow` styles as well, and the hours resolve `2-digit`:
  `new Intl.DurationFormat("lt").format({hours: 1, minutes: 2})` is `"01:02:00"`, where Node 22 writes `"1 val. 2 min."`.
  Broiler.VM's tables carry no such locale. Vectors `intl/durations/families/0003` and `intl/durations/families/0048` (`af`
  and `lt`, `long`) keep the words as Broiler.JS's expectation, so SJL-9.5 lists the move under decision D34, or the owner
  restricts the step to `numeric` hours by a dated decision made in both engines. *(Noted 2026-11-02, at SJL-9.3f: settled
  as the owner's decision of that date, taken under the owner's direction of 2026-10-18, "Please do all recommended
  decisions and skip waiting for review.": the library follows the pinned text as written. ECMA-402 at `3d4c558`,
  `spec/durationformat.html:615`, GetDurationUnitOptions' step "If _unit_ is "hours" and _twoDigitHours_ is true, set
  _style_ to "2-digit"", is unconditional, and `tc39/ecma402` `main` at `3691634` (2026-10-23) has the same step at the
  same line; the slot's definition at `:170`, "[[TwoDigitHours]] must be a Boolean value indicating whether hours are
  always displayed using two digits when using style "numeric"", constrains the data and restricts no step. So no code
  changes, and SJL-9.5 lists the move under decision D34.)*
- `Broiler.ECMAScript.Intl`, stage SJL-9.3f, the integration (Broiler.VM's JSD-0082, the plan's task 5.f, decisions D29 and
  D33): the library is **packed** on the shared stream, with package validation as `Broiler.ECMAScript.Algorithms` and
  `Broiler.ECMAScript.Generation` have it; its publication is the owner's. Row T10, settled: where the data locale's region
  prefers a cycle of the other kind, `hour12` takes the first 12-hour (`h`, `K`) or 24-hour (`H`, `k`) format CLDR's time
  data allows for the region, `h12` and `h23` where none is, so `ja` and `ja-JP` with `hour12: true` resolve `h11` and write
  `K` hours, as test262's `intl402/DateTimeFormat/prototype/resolvedOptions/hourCycle-default.js` asks and CLDR's own `ja`
  patterns write; the library read the preferred format alone, as Broiler.VM does, and answered `h12`. Every other region of
  CLDR 48.2.0's time data allows `h` before any `K` and no `k`, so no other answer moves, Broiler.VM's `de` and `en` none.

  The library-level runner answers every vector of the Intl area's five files under `intl/` but thirty that need an engine,
  listed by identifier in the Conformance tests: the routes of stages SJL-9.3a to SJL-9.3e combined, and two more of
  sub-area (a), read whole: `(1234.5).toLocaleString()` with the default locale and `supportedLocalesOf` of the default
  locale a NumberFormat resolves and `en` (rows L33 and L44). The collation vectors under `intl/collation/` (corpus
  revision 33) are SJL-17's, not answered. The
  Native AOT probe runs the Intl vectors over Broiler.JS's generated tables compiled into its image, which its build
  writes from `data/intl/generated/` into a C# source it does not commit, under invariant globalization as before.
- `Broiler.ECMAScript.Conformance` (test-only, not packed): `IVectorRecordLibrary`, a library the library-level runner
  hands each vector whole, script included, which `LibraryRun.Run` calls in place of the operation and the decoded
  inputs (stage SJL-9.3b): the Intl area's sub-area (b) vectors give their options and arguments in the script alone.
- `Broiler.ECMAScript.Frontend`, a new library: the shared syntax library's skeleton (milestone SJL-13.2, stage a;
  Broiler.VM's JSD-0083, decisions D6 to D11, D14 to D17 and D32; design
  [`docs/frontend/design.md`](docs/frontend/design.md), section 9). It is **not packed** until stage SJL-13.2f, the
  done-when, so that no version of the stream carries part of it (decision D6). It references
  `Broiler.ECMAScript.Algorithms` and `Broiler.Unicode.Algorithms` only, rule SL1's new layering row, and no
  `Broiler.Regex`. In `Broiler.ECMAScript.Syntax`:
  - **the request**: `ParseRequest`, immutable once made, over a `SourceText` (text, name, base offset, line and
    column), one of the five goals of `ParseGoal` (Script, Module, eval code, and the dynamic function's parameters and
    body, with the `DynamicFunctionKind` that parameterises those two), `ParseSwitches` (forced strictness, top-level
    `await` for whichever of the Script and Module goals the request sets it, which also governs catalogue row EX0023,
    Annex B, HTML-like comments by goal), the `InheritedContext` of eval code (the function, method,
    derived-constructor and field-initialiser flags, and an optional set of visible private names whose absence leaves
    that check to the engine; the caller's strictness travels as forced strictness, decision D16), `ProposalFlags`
    (one per form), `LexerExtensions` (the `m` suffix), `ParseLimits` (the nesting depth, the diagnostic cap,
    `ErrorReporting`'s first error or all errors, and the tree-depth constant), Broiler.Unicode's `IUnicodeTables`,
    the `IRegularExpressionValidator` provider with its `RegularExpressionVerdict` and `RegularExpressionStatus`,
    shaped as Broiler.Regex's validate entry answers, and an optional `WorkCharge`. Each request's defaults for its
    goal are the library's default request; what a goal does not admit is refused when the request is made;
  - **the diagnostics' types**: `SyntaxDiagnostic` (a shared code of the catalogue, a message identifier of its
    form, its arguments, a `DiagnosticPhase`, a `SourceSpan` and the source's name);
  - **the parse entry**: `SyntaxParser.Parse`, with `ParseResult` and `ParseOutcome`, which answers
    `ParseOutcome.NotImplemented` for every source, reads neither provider and charges no meter: the tokenizer is
    stage 4b's and the tree and the parser stage 4c's.

  The library-level runner gains its frontend adapter, which parses each `syntax` vector under the library's default
  request for the goal its `op` names, and the Native AOT probe runs the `syntax` vectors and records their outcome
  without judging it until the library parses.
- `Broiler.ECMAScript.Frontend`, stage SJL-13.2b: **the tokenizer**, internal to the library (Broiler.VM's JSD-0083,
  sub-area T, decisions D7 to D9, D15, D17 and D21; design [`docs/frontend/design.md`](docs/frontend/design.md)). It
  reads the lexical grammar of ECMA-262 clause 12 one input element at a time, under the lexical goal its caller names
  (`InputElementDiv`, `InputElementRegExp`, `InputElementRegExpOrTemplateTail`, `InputElementTemplateTail`,
  `InputElementHashbangOrRegExp`), and never chooses a goal from the tokens it has read:
  - white space and line terminators, with Broiler.Unicode's classes through the request's `IUnicodeTables` beyond ASCII
    (decision D8); comments, a `MultiLineComment` holding a line terminator counting as one; the `HashbangComment` at
    the first code unit of a Script, a Module or eval code; Annex B.1.1's HTML-like comments by the request's switch
    (decision D17), `-->` only where nothing but trivia stands since the last line terminator or the text's start;
  - identifier names and private names, with `\u` escapes cooked and their early errors (catalogue rows EE0001, EE0002
    and EX0025); a reserved or contextual word is an identifier name whose word, escape and spelling the token carries,
    for the parser to rule on;
  - numeric literals scanned by their grammar and valued by `NumericLiterals.Evaluate`, with no change to
    `Broiler.ECMAScript.Algorithms` (decision D9): separators, the legacy octal and non-octal decimal forms recorded for
    the parser's strict-mode rule, the BigInt suffix checked for its form only, its value left to the engine's ceiling,
    the touching rule (EX0003), and the `m` suffix only through the request's lexer extension (row T18);
  - string literals with their SV, the legacy octal and non-octal decimal escapes recorded, line continuations and raw
    LS and PS (EX0004, EX0005);
  - templates read in place one piece at a time, each with its TV and its TRV, a `NotEscapeSequence` recorded with no
    cooked value for the parser's tagged-or-not rule (EE0020, EE0022 to EE0024), an unterminated template refused at
    its backtick (EX0007), the end of the input inside a substitution included;
  - regular-expression literals delimited, their body and flags split, their validity left to the request's provider
    at stage 4d (decision D7; EX0006);
  - every punctuator by its longest match, `?.` never before a decimal digit, and `@` as a token for the decorators
    proposal, which the parser refuses where it reaches one when the request's flag is off (decision D17);
  - positions in UTF-16 code units, 0-based offsets and 1-based lines and columns with the source's base applied, every
    line terminator a new line and CR LF one (decision D15), and the line-terminator fact automatic semicolon insertion
    and the restricted productions read.

  A lexical refusal is a `SyntaxDiagnostic` of phase `Tokenizer` with the catalogue's shared code and message
  identifier, at the construct's start as Broiler.VM's wide front end anchors it (decision D15). Two places follow both
  engines' front ends rather than the shortest reading of the lexical grammar, so that the refusal keeps their code and
  place: a legacy octal integer directly followed by a decimal fraction (`07.5`) is refused at the literal, and a numeric
  literal directly followed by an escaped identifier start (`1\u0061`) is left to the parser, which refuses the name.
  `SyntaxParser.Parse` now charges the request's meter for the text before it reads it, reads the Unicode provider, and
  answers `ParseOutcome.Refused` for a source the lexical grammar refuses under every reading the parser could give it,
  and `ParseOutcome.NotImplemented` for every other until the parser (stage 4c). The library-level runner's Unicode
  provider is the tables the owner's generator writes from this repository's pin, `data/unicode/pins/`; the probe still
  records the `syntax` vectors without judging them, a vector the tokenizer refuses now among the matches; since a refusal
  lost would go back to not implemented, which the probe counts as neither a failure nor a difference, the Conformance
  tests pin by id the vectors the tokenizer refuses. No public API changes.
- `Broiler.ECMAScript.Frontend`, stage SJL-13.2c: **the tree and the parser** (Broiler.VM's JSD-0083, sub-areas P and N,
  decisions D10, D11, D14, D15 and D17; design [`docs/frontend/design.md`](docs/frontend/design.md), section 9.3's note
  of this stage). In `Broiler.ECMAScript.Syntax`:
  - **the tree**, public and immutable, seeded from Broiler.VM's `JsSyntax`: `SyntaxNode` with its `SyntaxKind` and full
    `SourceSpan`; `ProgramNode` for every goal, with ParseModule's top-level-`await` fact; the `Expression`, `Statement`
    and `Pattern` families, one sealed class per production the tree keeps; `FunctionNode` and `ClassNode` shared by the
    declaration, expression, arrow and method forms; and the raw facts JSD-0080's output table lists: a directive's raw
    text, a numeric literal's text and its legacy-octal, non-octal-decimal, BigInt and `m` flags, a string literal's
    escape flags, each template piece's cooked and raw text, a regular-expression literal's body and flags, every
    parenthesis as a `ParenthesizedExpression`, and every trailing comma. Nothing is desugared: a function declaration
    that is an `if` clause under Annex B stays one, an arrow keeps its expression body, a `for` head keeps its
    declaration;
  - **the parser**, internal: the syntactic grammar of ECMA-262 clauses 13 to 16 for the Script and Module goals, eval
    code, and the dynamic function's parameters and body by its kind, each token read under the lexical goal the grammar
    stands at; cover grammars converted, never parsed again (parenthesised lists and async calls into
    ArrowFormalParameters, an async call's `await` references that its parameters do not bind refused, as the
    AsyncArrowHead's [+Await] reads them; array and object literals into assignment patterns; CoverInitializedNames
    refused where no conversion takes them); arrow functions without rewinding, an async arrow read as a primary
    extended by no operator, so that a line break before one inserts a semicolon; automatic semicolon insertion with the
    restricted productions, none in a for statement's head (rows P16 and P16b); modules with import attributes; classes
    with fields, private names, static blocks and accessors; templates read in place; Annex B's function in an `if`
    clause and `for`-`in` initialiser in sloppy code with the switch on; the decorators, `accessor` and import-phase
    proposals each under its own flag; the request's top-level-`await` switch (decision D10); and the contract's two
    limits checked before the stack is: the nesting depth charged as Broiler.VM's `JsParser` charges it, and also for
    the right operand of `**` and for each class heritage, two recursions Broiler.VM's parser leaves uncharged, and the
    tree depth charged for each node a binary or a member or call chain adds, from the spine of the chain's first
    operand, which closes the left-operand gap in its four shapes (decision D14).

  `SyntaxParser.Parse` now answers `ParseOutcome.Accepted` with the tree in `ParseResult.Tree`, or
  `ParseOutcome.Refused` with the first refusal (decision D11); `ParseOutcome.NotImplemented` stays and is no longer
  answered. A grammar refusal is a `SyntaxDiagnostic` of phase `Parser` with one of two **provisional** codes, JSE9108
  `syntax.token.unexpected` and JSE9109 `syntax.token.missing` (catalogue rows EX0008's and EX0009's), which stage 4d
  replaces by the row each site serves, and a provisional argument, the token's spelling or the one expected; a limit's
  refusal has its row's code, JSE9201 `limit.nesting.too-deep` (EX0011) or JSE9202 `limit.tree.too-deep` (EX0012), and
  its bound as its argument. The tree-depth constant stays `ParseLimits.MaximumTreeDepth`, Broiler.VM's bound, a
  provisional value until stage 4e derives it and the stack it requires. The static-semantic early errors and the
  regular-expression provider's answer are stage 4d's, so a source only they refuse is accepted until then. The token
  stream the parse reads is kept inside the library and is not public. The probe still records the `syntax` vectors
  without judging them, which stage 4d's check judges, and the Conformance tests pin by id every vector the library
  refuses and every one it accepts against an expected refusal.
- `Broiler.ECMAScript.Frontend`, stage SJL-13.2d: **the diagnostics and the parse-time early errors** (Broiler.VM's
  JSD-0083, sub-area D, decisions D6, D7, D10, D11 and D12; design [`docs/frontend/design.md`](docs/frontend/design.md),
  section 9.3's note of this stage):
  - **the catalogue's shared codes everywhere**: every refusal carries the code and the message identifier of the
    catalogue row it reports (`early-errors.tsv`), one internal entry per row the stage table assigns `syntax` and none
    for a `request`, `engine` or `semantics` row. Stage 4c's provisional grammar codes are the rows they stood for,
    JSE9108 EX0008's and JSE9109 EX0009's, kept where no other row explains the site; the sites that serve a row of
    their own report it: a cover that covers nothing (EE0017, EE0025, EE0032, EE0037, EE0038, EE0053, EE0087, EE0155),
    an operand that is no assignment target (EE0033 to EE0035), an optional chain's template (EE0026), a module item
    under the Script goal (EX0010), `return` outside a function or in a static block (EX0032, EX0033), Annex B's
    sloppy-only forms in strict code (EG0005, EG0007), an HTML-like comment in a module (EG0001), and a proposal's form
    its flag does not admit (EP0009 to EP0014). The limits' JSE9201 and JSE9202 were already EX0011's and EX0012's. The
    arguments each identifier takes are the catalogue's section 6 note of this stage. A refusal's phase is the pass that
    finds it: `Parser` for the grammar's, a limit's and those sites', `Semantics` for the declarations walk's and the
    provider's (design section 9.3's note says which rows each pass reports);
  - **the declarations walk**, in `Broiler.ECMAScript.Semantics`, over the tree the grammar admits, with a stack of its
    own: the static-semantic early errors of every `syntax` row, the union decision D10 took (names under strictness
    and the [Yield] and [Await] parameters, legacy octal literals and escapes, templates' escapes, assignment targets,
    `delete`, `with`, labelled functions and duplicate labels, `new.target` and `import.meta`, a function's parameters
    and directives, every scope's declared-name conflicts, a class's elements, `using` where nothing disposes, a
    module's export names and import attributes), with Annex B's relaxations under the switch; the top-level-`await`
    switch, the one `request` row (EX0023), read as the top level's [Await] parameter with no diagnostic of its own; and
    every regular-expression literal's validity asked of the request's provider, once per literal in source order,
    reported at the literal by the row its verdict's identifier names, or EE0019's. A verdict carries an identifier and
    no row, so of the provider's rows that share an identifier the first in catalogue order reports for all: EE0188 and
    EE0189 are reported as EE0186; EE0195, EE0210 and EE0212 as EE0193; EE0196, EE0208, EE0211 and EE0213 as EE0194;
    EE0198 as EE0197; EE0200 as EE0199; and EE0207 as EE0206. These twelve rows have no code of their own in a
    diagnostic until the verdict can name the row;
  - **the side tables**, public: `ParseResult.Declarations`, a `DeclarationTables` of every scope (`Scope`, its
    `ScopeKind` and its `Declaration`s, each with its `BindingKind`, whether lexical, and whether a block-level or an
    `if` clause's function) and of the program's and every function's strictness and parameter names
    (`FunctionFacts`), each ordered by source position;
  - **the library's order and the request's limits** (decision D11): diagnostics in source order, a construct's
    refusal where the parse reaches it and a scope's conflicts when it closes; under the first error, the first; under
    all errors, every one up to `ParseLimits.DiagnosticCap`. When the grammar refuses, the early errors of the top-level
    statements completed before its refusal come first.

  The library-level runner's regular-expression provider admits every literal (no `syntax` vector's outcome is a
  pattern's validity), and every `syntax` vector now agrees at the parse; the probe judges the area from this stage.
- `Broiler.ECMAScript.Frontend`, stage SJL-13.2e: **the limits, determinism and fuzzing** (Broiler.VM's JSD-0083,
  decisions D14 and D22; design [`docs/frontend/design.md`](docs/frontend/design.md), section 9.2's and 9.3's notes of
  this stage):
  - **the tree-depth constant derived** as Broiler.VM's JSD-0022 derived Broiler.VM's, in
    [`docs/frontend/limits/derivation.md`](docs/frontend/limits/derivation.md) with its measurements: the frames and
    bytes each level of the parser's recursion costs, under the JIT and under Native AOT, and the least stack each source
    of the home rule's deep-nesting corpus needs. A level of tree depth costs the library no frame (its chain builders
    are loops and its declarations walk keeps its own stack), so `ParseLimits.MaximumTreeDepth` stays Broiler.VM's bound,
    10,000, now derived and not provisional; a level of nesting costs from 1 to 10 frames and from 303 to 5,168 bytes at
    the JIT's first tier, its optimised tier and under Native AOT, and `ParseLimits.MaximumSupportedNestingDepth` is
    confirmed at 512;
  - **the precedence climb on an explicit stack**: the parser read each right operand of a higher precedence by a
    recursion the nesting bound did not charge, up to ten frame pairs between two levels of nesting, which at the
    supported maximum took the JIT's stack to 10,328,704 bytes; it now keeps the operators that wait on a stack of its
    own, and the canonical dump of every parse of the parse corpus is unchanged;
  - **a decorator's member chain charged**: under the decorators proposal's flag, `@a.b. ... .z()` is charged one level of
    tree depth per link, as any member chain, and refused past `ParseLimits.MaximumTreeDepth` (it was built);
  - **`ParseLimits.StackBytes`**, new: the stack the library documents for a parse, 16 MiB, the stack Broiler.VM's
    `CompilationStack` declares, on which the costliest construct measured needs at the supported maximum nesting
    3,416,352 bytes under the JIT and 1,682,944 under Native AOT;
  - **the left-operand charge** of stage 4c tested on the documented stack: a chain whose left operand is a
    parenthesised chain, however the two divide the bound and at every level of parentheses, and the four shapes of the
    home rule's deep-nesting corpus;
  - **determinism tests** over the parse corpus's stored inputs, both configurations and eval code: a canonical dump of
    each parse (the tree with every node's kind, span and raw facts, the token stream, the diagnostics and the side
    tables) is the same bytes in a repeated parse, on another thread, in the reverse order, on two threads at once and
    under tr-TR, lt-LT and the invariant culture;
  - **the Native AOT probe's frontend modes**, each its own run and none changing the probe's corpus run:
    `--parse-one` (one source parsed alone, on a thread of `ParseLimits.StackBytes`, its outcome its exit code: the
    deep-nesting corpus's runner, driven by `docs/evidence/sjl-13-001/tools/deep_nesting.py`), `--measure-frames` (the
    derivation's measurement), `--fuzz` (the home's fuzz target, decision D22, over the seeds the home rule's bundle
    commits) and `--digest-corpus` (the canonical dump's SHA-256 for every parse of the parse corpus, compared between the
    JIT and the Native AOT image). The library grants the probe its internals for the dump, as it grants its tests.

  No parse within the limits changes its answer; a decorator's member chain past the tree-depth constant, under the
  decorators proposal's flag, is refused where it was built.
- `Broiler.ECMAScript.Frontend`, stage SJL-13.2f: **done** (Broiler.VM's JSD-0083, decisions D6, D14 and D30; design
  [`docs/frontend/design.md`](docs/frontend/design.md), sections 9.1's and 9.2's notes of this stage):
  - **every shape of the tree charged against the tree depth**, by the owner's decision of 2026-11-02 on what the
    derivation's section 5 recorded at stage 4e: each chain node is charged its height, one more than the highest of all
    its children, and refused when that height passes `ParseLimits.MaximumTreeDepth`, with the tree-depth diagnostic
    (JSE9202, catalogue row EX0012), so that every tree deeper than the constant is refused wherever it is built - under
    a right operand of a chain's early node, a prefix operator's, `await`'s or `yield`'s operand, an update's operand, an
    array or object literal's element, value, spread or computed key, a call's, `new`'s or `import(…)`'s argument, a
    computed member's key, a tagged template's substitution, a decorator's expression, a parenthesised sequence,
    conditional or assignment, an arrow's or a function's body, a class's heritage or field initialiser, or `#x in`'s
    right operand - as along a chain's left spine, where the four shapes JSD-0083's decision D14 names are refused past
    the constant as before; and every tree within the constant is accepted, however its chains stand beside or below one
    another. A node's height is computed once, with an explicit stack, so the charge adds no frame per level of the
    tree. The derivation's section 5 is revised
    ([`docs/frontend/limits/derivation.md`](docs/frontend/limits/derivation.md)), and its figures stand;
  - **packed**: `IsPackable` is true, on the shared stream's `0.1.0-preview.N` scheme (the owner's words of 2026-11-01)
    with the other shared packages, whose next version is `0.1.0-preview.9`, with package validation as they run it (a
    first version, validated without a baseline). The package depends on `Broiler.ECMAScript.Algorithms` at the stream's
    version and on `Broiler.Unicode.Algorithms`, and carries no data. Publishing it is the owner's, with the owner's
    approval (decision D30).

  A source whose tree passes the constant in one of the shapes above, accepted at stage 4e, is refused; a source whose tree
  is within the constant, which stage 4e refused because it charged a right operand from the depth its chain had reached
  (`x = a+...+a + (b+...+b)`, 5,001 operands on each side, a tree 5,002 deep), is accepted. No source of the parse corpus
  changes its answer. No public API changes.

  *(Added 2026-11-02, at SJL-13.2f, after part A's first codes were triaged; bundle `sjl-13-001`'s README, "Part A's first
  codes, triaged".)* Two sites reported a row whose condition does not hold where another's does; each now reports the
  row that holds, and no source changes its outcome:
  - **an `await` binding in a class static block** is refused by EE0009 (JSE1305, `identifier.await.in-async`), the
    BindingIdentifier's row under the block's [+Await], and an escaped one by EE0011 (JSE1307) or, in a module, EE0013
    (JSE1309), where every `await` name in a static block was refused by EE0140 (JSE1573,
    `class.static-block.await`). EE0140, "ClassStaticBlockStatementList Contains `await`", looks into no function and finds
    no `await` terminal in an escaped name, so `class C { static { function await() {} } }` was refused by a row that
    does not reach it; EE0140 stays the row of an `await` expression and of an unescaped reference in the block;
  - **an assignment inside an arrow's head whose array or object literal covers no AssignmentPattern**
    (`([...[x], y] = [1]) => 0`, `({ a: [...x, y] = 1 }) => 0`) is refused by the arrow's cover row, EE0087 (JSE1520), or
    at an async head EE0155 (JSE1588), where it was refused by EE0032 (JSE1328) when the assignment was read: an arrow's
    parameters are no AssignmentExpression, so EE0032 holds of none of them. The literal's refusal now waits for the cover
    around it, as a CoverInitializedName's does, and is EE0032's where no arrow's head takes it (`([...[x], y] = [1]);`).

  In the parse corpus 112 refusals change their first code and none its outcome: 40 from JSE1573 to JSE1305 and 72 from
  JSE1328 to JSE1520 (both configurations).

  *(Added 2026-11-02, at SJL-13.2f, after review of the change above.)* The second item said that the deferred literal
  refusal "is EE0032's where no arrow's head takes it (`([...[x], y] = [1]);`)". That held only where a cover around the
  literal resolves, and two other cases now report the row whose condition holds:
  - **a deferred literal in a class heritage** (`class C extends [[...x, y] = 1] {}`,
    `export default class extends { a: [...x, y] = 1 } {}`) is refused by EE0032 (JSE1328) when the heritage has been read.
    No assignment encloses a heritage at a statement's level, so the deferred refusal was never made, and the walk
    refused the literal target by EE0033 (JSE1329), whose condition ("neither an ObjectLiteral nor an ArrayLiteral") does
    not hold of it. The same check closes an older gap: a CoverInitializedName in a heritage (`class C extends {a = 1} {}`,
    `class C extends [{a = 1}] {}`) was accepted, and is now refused by EE0017 (JSE1313);
  - **a literal assignment nested in a literal that the parse converts to an assignment pattern** (an assignment's target or
    a for-in/of head: `[[...x, y] = 1] = 2;`, `for ([[...x, y] = 1] of []);`, `({ a: [...x, y] = 1 } = 2);`) is refused
    by EE0038 (JSE1334), the DestructuringAssignmentTarget's row. Before the change above it was refused by EE0032
    (JSE1328) when the inner assignment was read; after it, by EE0038 unparenthesised and still by EE0032 within
    parentheses. In the pattern the inner literal is no AssignmentExpression's target, so EE0032 does not hold of it,
    and the outer literal does cover an AssignmentPattern; the conversion now takes the deferred literal and drops its
    EE0032 refusal. EE0032 stays the row where nothing converts the outer literal (`({ a: [...x, y] = 1 });`).

  No source of the parse corpus changes its outcome or its first code.

- `Broiler.ECMAScript.Intl`: **collation** (milestone SJL-17.3; Broiler.VM's JSD-0084, the area verdict "Collation (SJL-17.1)"
  of this repository's plan), in `Broiler.ECMAScript.Intl/Collation/`. `UcaCollator`, ECMA-402's CompareStrings over the
  Unicode Collation Algorithm, CLDR 48.2.0's root collation and a tailoring, under the resolved options as a record,
  `UcaCollatorOptions` (strength, case level, case order, the shifted alternate, numeric collation and the tailoring by
  name, with `CollationStrength` and `CollationCaseFirst`, and `FromResolved`, Broiler.VM's mapping of `sensitivity`,
  `ignorePunctuation`, `caseFirst` and `numeric` onto them); and `UcaCollationData`, the collation tables of an
  `IIntlDataSet` decoded once, with the questions an engine's Collator asks of them: the tailoring a language, usage and
  collation select (`TailoringFor`), the values of the relevant keys (`KeyValues`, ECMA-402's `[[SortLocaleData]]` and
  `[[SearchLocaleData]]`), and whether a tailoring shifts by default (`ShiftedByDefault`, Thai's, decision E5). Ported from
  Broiler.VM's `JsCollator`, `JsCollationData`, `NewCollator`'s choice of a tailoring and `CollatorKeyData` at its SJL-17 rule
  commit `4e6a41e9` with the three changes of the programme's library stages: no culture is read; a string whose NFD form
  passes the caller's ceiling is a status, `IntlStatus.NormalizedStringTooLong` (30), where Broiler.VM threw its
  `RangeError`; and the caller's `WorkCharge` is charged where Broiler.VM charged its meter, in its units, NFD as
  `normalize` charges it and each collation element of both strings (decision E7, no C2). NFD and the combining classes are
  Broiler.Unicode's `UnicodeNormalization`, which the engine hands in (decision E6). The element lookup is unchanged;
  `TailoringFor` and `KeyValues` read the tailorings the data carries, so that over Broiler.VM's three they answer as its
  code does, and a tailoring that states `[reorder]` moves its script's primaries before the other scripts'. Han across
  blocks keeps UCA 17's implicit weights by block, Broiler.VM's order (the owner's decision of 2026-11-02 on row O3, the
  Intl design's dated note). `IntlTable.CollationSettings` (32) is appended for the tailorings' settings, a table Broiler.VM's
  data never carries (decision D8), which an engine without it answers as an empty span.
- `Broiler.ECMAScript.Generation`: the collation tailorings of Broiler.JS's list (milestone SJL-17.3; JSD-0084, decision E3):
  `IntlGeneratorOptions.BroilerJsTailorings`, Broiler.VM's three followed by every collation CLDR 48.2.0 gives for `es`, `sv`
  and `th` (`es-standard`, `es-search`, `es-trad`, `sv-standard`, `sv-search`, `sv-trad`, `th-standard`), and the rule syntax
  they use: the reset `[before 1]`, an extension (`/`), and the settings `[alternate shifted]` and `[reorder Thai]`, which the
  new `CollationSettings` table carries. Any other syntax is refused by name, as before, CLDR's `eor` and `emoji` rules among
  it (finding F2). With Broiler.VM's options the generator writes Broiler.VM's tables byte for byte, as before: its three
  tailorings state no setting, so no settings table is written for them.
- `Broiler.ECMAScript.Conformance` (test-only, not packed): the library-level runner answers the collation vectors
  (`intl/collation/`, corpus revisions 33 and 34) through the shared collator over Broiler.JS's tables, with the Collator's
  available locales those the tables' collation data covers; every one gives its `out`, or, for the 34 Han vectors whose
  answer moves with the order of unified ideographs across blocks, Broiler.VM's `per-engine` expectation. `Broiler.ECMAScript.Intl.Tests`
  orders every line of CLDR 48.2.0's archived CollationTest files at library level (the L4 oracle, decision E9), and the
  differential check gains sections C1 and C2, Broiler.VM's collator and `CollatorKeyData` at `4e6a41e9` against the
  library.
- `Broiler.ECMAScript.Frontend`: **the token stream a parse read, public** (the owner's decision of 2026-11-04, "Expose +
  preview.10", for Broiler.VM's SJL-13.3 step 1, whose tokenizers-agreement row compares its slice tokenizer's stream with
  the library's; Broiler.VM's JSD-0083, decision D21; design [`docs/frontend/design.md`](docs/frontend/design.md),
  section 9.3's note of 2026-11-04). A minor change under convention 16, which the stream's next version,
  `0.1.0-preview.10`, carries:
  - `ParseRequest.RetainTokens`, false by default: whether the result publishes the stream. The parse reads the same
    tokens, makes the same tree, side tables and diagnostics either way, and allocates the same objects of the same
    sizes as at `0.1.0-preview.9`: the result holds the stream it already kept, and a flag beside its outcome;
  - `ParseResult.Tokens`: with the switch on, the tokens an accepted parse consumed, in source order, each read under the
    goal the parser named, so that each `/` is division or a regular-expression literal's start and each `}` a punctuator
    or a template's continuation as the grammar read it; empty with the switch off and for a refused source;
  - `Token`, an immutable struct whose constructor is internal, so that the library makes every token a result holds (a
    default `Token`, of kind `EndOfInput` with no facts, is no token of any stream) - `Kind`, `Word` (the reserved or
    contextual word an identifier name's cooked name spells, escaped or not), `Start` and `End` (offsets into the
    request's text, without the source's base offset, whose text is the raw spelling), `Line` and `Column` (1-based, with
    the source's base line and, on its first line, base column), `Value` (a name's StringValue, a string's SV, a template
    piece's TV), `Raw` (a template piece's TRV), `Number` (a numeric literal's Number value; NaN for a BigInt), `Flags`,
    `NewLineBefore`, `IsEscaped` and `Length` - with `TokenKind`, `TokenFlags` and `Word`, whose members and numbers are
    the tokenizer's since stage SJL-13.2b.

  The canonical dump of a parse reads the stream whatever the switch says, so every dump and digest is the same bytes
  with it off or on. No member is removed, and nothing an engine observes changes with the switch off.

- `Broiler.ECMAScript.Intl` and `Broiler.ECMAScript.Generation`: **a currency's name in every plural form, and the currency
  style's grouping separator** (the owner's decision of 2026-11-08, "Keep + fix library first", on rows N4 and N3c of
  Broiler.VM's JSD-0082; design [`docs/intl/design.md`](docs/intl/design.md), section 1's note of 2026-11-09). Nothing is
  removed. Whether it is a minor change under convention 16 waits for the owner's ruling: the currency table's four fields
  appended to a line keep the data format only on a reading of the design's section 3 that the owner has not confirmed, and
  if the owner does not, the change is a major version under that section. *(Revised 2026-11-09, at review; it read "A minor
  change under convention 16, which the stream's next version carries; nothing is removed:".)* *(Settled 2026-11-09: the owner
  ruled the same day, "Additive: extend D8", so this is a minor change under convention 16, which the stream's next version
  carries; the sentence above, "Whether it is a minor change under convention 16 waits for the owner's ruling", is answered.)* It adds:
  - `CurrencyNames.Zero`, `Two`, `Few` and `Many`, init properties empty by default, and `CurrencyNames.DisplayName(string)`,
    the name for a plural category with UTS #35's fallback to `other`'s form, then the display name;
    `TableLocaleData.Currency` fills the four from a currency line that carries them;
  - `IntlGeneratorOptions.CurrencyNamePlurals` and the enum `IntlCurrencyNamePlurals` (`OneAndOther`, the default and
    Broiler.VM's; `Every`), which appends the `zero`, `two`, `few` and `many` names to each line of the `Currencies` table.

- `Broiler.ECMAScript.Intl` and `Broiler.ECMAScript.Generation`: **the date-time format follows the pinned CLDR data** (the
  owner's decision of 2026-11-09, "Keep + fix library first", with the rulings of that day "Add file, same tarball" and "Fix both
  consumers"; design [`docs/intl/design.md`](docs/intl/design.md), section 1's note of 2026-11-09 on sub-area (d)). A minor change
  under convention 16: nothing is removed, the new interface member has a default implementation, and the one new table is an
  appended `IntlTable` value for a table Broiler.VM's data never carries, which decision D8 admits; no field is appended to a
  line, so the owner's reading "Additive: extend D8" is not needed for it. It adds:
  - `IntlTable.CalendarPreferences` (33), each region's calendars in order of preference;
  - `ILocaleData.CalendarPreferences(string)`, answering `null` by default, with `TableLocaleData.CalendarPreferences` and
    `CompositeLocaleData.CalendarPreferences`;
  - `DateTimeFormat.CalendarKeyValuesOf(ILocaleData, string)`, the `ca` values for a matched locale, the region's preferred
    calendar first, which an engine hands ResolveLocale as it hands `NumberingSystemKeyValues` for `nu`;
  - `IntlGeneratorOptions.CalendarPreferences` and the enum `IntlCalendarPreferences` (`None`, the default and Broiler.VM's;
    `Regions`), which writes the `CalendarPreferences` table.

### Changed

- `Broiler.ECMAScript.Intl`'s `NumberFormatter`, over data that carries them (the owner's decision of 2026-11-08, "Keep + fix
  library first"):
  - **a currency's name takes the formatted number's plural category** in every form the data gives - `zero`, `one`, `two`,
    `few`, `many` and `other` - falling back to `other`'s, then the display name, then the code, where it took `one`'s form
    for `one` and `other`'s for every other category (row N4). A value that is not finite keeps the name it had. With
    Broiler.JS's tables `cs`, `lt` and `sl` write `2,00 amerického dolaru`, `2,00 JAV doleriai` and `2,00 ameriški dolarji`;
    with Broiler.VM's, which carry `one` and `other` alone, nothing moves;
  - **the currency style groups its digits with `symbols.currencyGroup`**, falling back to `symbols.group`, where it grouped
    with `symbols.group` alone (row N3c). CLDR 48.2.0 states it for `de-AT` alone of either list, which writes `-€ 1.234,50`
    where it wrote `-€ 1 234,50`; Broiler.VM's locales state none.

- `Broiler.ECMAScript.Intl`'s `DateTimeFormat` and `LocaleInfo` (the owner's decision of 2026-11-09, "Keep + fix library
  first"):
  - **the hour cycle reads the locale's own time data first** (row T10; UTS #35 Part 4, Time Data): CLDR's entry for the data
    locale's language and region, then the region's, then the world's, where it read the region's alone, so `ar-001` takes
    `h12` and `fr-CA` `h23`; **Intl.Locale's HourCyclesOfLocale** does the same (row L19; the ruling "Fix both consumers"),
    where it looked the entry up as `language_region`, which names none;
  - **the region of a data locale without one is the likely region of the whole locale** (UTS #35 Part 1, Likely Subtags),
    where it was the likely region of its language alone: `zh-Hant` reads Taiwan's, `en-Shaw` Great Britain's;
  - **the default calendar is the region's first preference** where an engine hands ResolveLocale `CalendarKeyValuesOf`'s
    values (`fa` `persian`, `th` `buddhist`); with `CalendarKeyValues`, or data without the table, it stays `gregory`;
  - **a fractional second takes the numbering system's own decimal symbol** (decision D21), where it took the locale's `latn`
    symbol for every system: `en-u-nu-arab` writes U+066B.

  Broiler.VM's tables carry no calendar preference table and no `symbols@` key, and none of its data locales has a script or a
  time data entry of its own, so its date-time formats do not move; its Intl.Locale's hour cycles move for the tags that reach a
  time data entry (`fr-CA`, `ar-001`, `en-001`, `ku-SY`, ...), row L19. Interval patterns are unchanged: their field widths are
  ICU's (design, section 1's note of 2026-11-09).

*(Revised 2026-11-09, at review of the owner's decision of 2026-11-08; the section read "None.")*

### Observable changes

None in an engine yet, though the number formatter's own output changes for data that carries the forms or a
`currencyGroup` ("Changed" above, and the note of 2026-11-09 at the end of this paragraph): no engine reads the Intl
generator, its tables, the Intl library or the frontend yet. *(Revised 2026-11-09, at review; the opening read "None: no
engine reads the Intl generator, its tables, the Intl library or the frontend yet.")* Broiler.VM regenerates
its table with the generator at its work package SJL-9.2c, and Broiler.JS embeds the tables at its cut-over, milestone
SJL-9.5, whose own records list what each observes; rows L9, L18 and Z8's corrections move Broiler.VM's answers when it
cuts over to the library, at SJL-9.4. The frontend parses from stage 4c, reports the parse-time early errors from stage 4d, and neither engine
references it before SJL-13.3's step 1 and SJL-14's bridge. *(Revised 2026-11-02, at SJL-13.2d; the sentence read "The
frontend parses from stage 4c, and neither engine references it before SJL-13.3's step 1 and SJL-14's bridge.")* *(Revised 2026-11-02, at SJL-9.3d, when the date-time format's zone resolution was
added; the clause read "rows L9 and L18's corrections move Broiler.VM's answers".)* *(Added 2026-11-02, at SJL-17.3, when the collator was added:
neither engine compares strings with the library's collator before its cut-over, Broiler.VM's at SJL-17.4 and Broiler.JS's
at SJL-17.5, whose records list what each observes.)* *(Revised 2026-11-01, at SJL-13.2c,
when the parser was added; the sentence read "The frontend accepts no source before stage 4c, refusing from stage 4b only
a source the lexical grammar refuses, and neither engine references it before SJL-13.3's step 1 and SJL-14's bridge.")*
*(Revised 2026-11-01, at SJL-13.2b, when the tokenizer was added; the sentence read "The frontend answers no source
before stage 4c, and neither engine references it before SJL-13.3's step 1 and SJL-14's bridge.")* *(Revised
2026-11-01, at SJL-13.2a, when the frontend was added; it
read "None: no engine reads the Intl generator, its tables or the Intl library yet. Broiler.VM regenerates its table
with the generator at its work package SJL-9.2c, and Broiler.JS embeds the tables at its cut-over, milestone SJL-9.5,
whose own records list what each observes; rows L9 and L18's corrections move Broiler.VM's answers when it cuts over to
the library, at SJL-9.4.")* *(Revised 2026-10-27, at SJL-9.3a, when the Intl library was added; it read "None: no engine
reads the Intl generator or its tables yet. Broiler.VM regenerates its table with the generator at its work package
SJL-9.2c, and Broiler.JS embeds the tables at its cut-over, milestone SJL-9.5, whose own records list what each
observes.")* *(Added 2026-11-09, by the owner's decision of 2026-11-08, "Keep + fix library first": the number formatter names a
currency by the formatted number's plural category in every form the data gives, so `cs`, `lt` and `sl` write `2,00 amerického
dolaru`, `2,00 JAV doleriai` and `2,00 ameriški dolarji` where they wrote the `other` form (row N4, a correction), and groups a
currency's digits with CLDR's `currencyGroup`, so `de-AT` writes `-€ 1.234,50` where it wrote `-€ 1 234,50` (row N3c, data).
Broiler.VM's tables carry neither the extra forms nor a `currencyGroup` for its locales, so its answers do not move; Broiler.JS
observes both from its cut-over, SJL-9.5.)* *(Added 2026-11-09, by the owner's decision of that day, "Keep + fix library first": the
date-time format's hour cycle, region, default calendar and fractional-second separator follow CLDR 48.2.0 ("Changed" above), so
with Broiler.JS's tables `ar-001` writes `3 بعد الظهر`, `fr-CA` `15 h 07`, `zh-Hant` `下午3時`, `fa` and `th` their persian and
buddhist dates, and `en-u-nu-arab` `١٥:٠٧:٠٩٫١٢٣`; Broiler.JS observes them from its cut-over, SJL-9.5, which hands
`CalendarKeyValuesOf`'s values to ResolveLocale and removes its adapter of the decimal symbol. Broiler.VM's date-time formats do
not move; its Intl.Locale's `getHourCycles` moves for the tags whose language and region CLDR's time data names, at its next
re-pin (row L19).)*

### Data

- The Intl data home, [`data/intl/`](data/intl/README.md), outside every package (milestone SJL-9.2b;
  JSD-0082's decisions D5, D10, D12 and D21): CLDR **48.2.0**'s files - the `cldr-json` files, and from the CLDR
  repository the collation, root and UCA files and the `common/main` XML files decision D21 reads - and `cldr.pin`,
  its UCA **17.0.0**: every entry of Broiler.VM's pin at `4cf9ca44` byte for byte, the main-data files Broiler.JS's
  locale list needs from the same packages, each package's tarball checked against its recorded integrity, and the
  XML files of the list's locales and their parents, retrieved twice from the CLDR repository's tag; the Unicode
  Character Database **17.0.0**'s `UnicodeData.txt` and `PropList.txt` and `ucd.pin`, each entry equal to
  Broiler.Unicode's pin entry for the same file; each embedding's locale list, `broiler-vm.txt` and
  `broiler-js.txt`, per decision D5; and Broiler.JS's tables generated from them with their manifest. Rule SL7
  regenerates the tables. The `timezone.json` the time home pins is the same file, byte for byte.
- Broiler.JS's collation tables (milestone SJL-17.3; JSD-0084, decision E3): `collation-tailorings.bin` carries the
  tailorings of decision E3's list, Broiler.VM's three, unchanged, first, and `collation-settings.txt`, a new table, Thai's
  shifted alternate and reordering; `collation-root.bin` is unchanged, Broiler.VM's byte for byte. Their rules are CLDR
  **48.2.0**'s `common/collation/es.xml`, `sv.xml` and `th.xml`, which the Intl home's pin gains, retrieved twice from the
  CLDR repository's tag `release-48-2`. Broiler.VM's tables do not change (rule N28).
- Broiler.JS's currency table (the owner's decision of 2026-11-08, row N4): each line of `currencies.txt` carries four more
  fields, CLDR **48.2.0**'s `zero`, `two`, `few` and `many` names from the `cldr-json` files the pin already names, so the file
  grows from 1,752,680 to 1,982,660 bytes; its first nine fields are unchanged. Regenerated by rule SL7's write switch; two runs
  write the same bytes. No pin, list or other table changes, and Broiler.VM's tables do not change (rule N28). The number table
  already carried `symbols.currencyGroup` where CLDR states it (`de-AT`).
- Broiler.JS's calendar preference table (the owner's decision of 2026-11-09, "Keep + fix library first"; the ruling "Add file,
  same tarball"): `calendar-preferences.txt`, a new table of 52 regions (2,101 bytes), from CLDR **48.2.0**'s
  `supplemental/calendarPreferenceData.json`, which the Intl home's pin gains under a dated `file` line after Broiler.VM's lines:
  the member of the cldr-core 48.2.0 tarball the pin already names, retrieved twice and checked against its `json-integrity`
  line. Regenerated by rule SL7's write switch; two runs write the same bytes. No other table changes, and Broiler.VM's tables
  and pin lines do not change (rules N27 and N28).

## 0.1.0-preview.7 — published to nuget.org

*(Heading corrected 2026-10-19, when it was published; read "Unreleased". This entry was published as
`0.1.0-preview.7` by `Publish shared libraries`, run 39312237448, on 2026-10-19, from `5ddee404`, whose library code
is `6dbff805`'s, and tagged `ecmascript-v0.1.0-preview.7`.)*

Packed and published as `0.1.0-preview.7`, the stream keeping its `0.1.0` prefix while it is a preview: the owner
approved its publication on 2026-10-19, for decision D12. The next action is Broiler.VM's: it admits the packages to
its committed feed as published, and takes decision D12 with them. *(Corrected 2026-10-19, after review; read "Packed
and published as `0.1.0-preview.7`, the stream keeping its `0.1.0` prefix while it is a preview: the owner approved
its publication on 2026-10-19, for decision D12, and Broiler.VM admits the packages from its committed feed as
published." That read, until it was published, "To be packed and published as `0.1.0-preview.7`, the stream keeping
its `0.1.0` prefix while it is a preview: the owner approved its publication on 2026-10-19, for decision D12, and
Broiler.VM admits the packages from its committed feed as published." That read, until earlier the same day, "Not yet
packed under a version of its own: the stream's next version is set when this entry is published.")*

### Added

None.

### Changed

None.

### Observable changes

**Decision D12**, the owner's of 2026-10-19 under the direction of 2026-10-18:
decision D11's rule reaches `NudgeToZonedTime`'s assertion on the day span's sign (s7.5.35 step 8, "Assert:
TimeDurationSign(daySpan) = sign"). Where a zone's skipped day leaves the day from the nudge's start with no length
(Pacific/Apia's 2011-12-30, rounding to a time unit with a date largest unit back from 2011-12-31), or an engine's own
zone gives it the wrong sign, `RoundRelativeDuration` answers `OutOfRange`, and so do `DurationMath.Round` and the
zoned differences that reach it: a `RangeError` in both engines, as the reference polyfill throws one. `0.1.0-preview.6`
answers a duration from outside the text's domain there:
`ZonedDateTime.from("2011-12-31T16:15+14:00[Pacific/Apia]").since("2011-12-31T00:00+14:00[Pacific/Apia]", {largestUnit: "day", smallestUnit: "hour"})`
is `P1DT16H`, and Broiler.VM, which takes that version, answers so until it moves past it. Broiler.JS, which takes the
library as a project, answers the `RangeError` its own code answered before its cut-over. No public API changes, and the
library-level runner's 10,851 Temporal vectors all still match.

### Data

None.

## 0.1.0-preview.6 — published to nuget.org

*(Heading corrected 2026-10-19, at SJL-10.5; read "Unreleased". This entry was published as `0.1.0-preview.6` by
`Publish shared libraries`, run 39161419185, on 2026-10-18, from `fc829f8a`, whose library code is `bf81d1b7`'s.)*

Packed as `0.1.0-preview.6`, the stream keeping its `0.1.0` prefix while it is a preview. Broiler.VM's cut-over
(SJL-10.4) admitted the packages packed at `bf81d1b7` from its committed feed before they were published, and its
feed now holds them as published. *(Corrected 2026-10-19, at SJL-10.5; read "To be packed as `0.1.0-preview.6`, the
stream keeping its `0.1.0` prefix while it is a preview: Broiler.VM's cut-over (SJL-10.4) admits the packages from
its committed feed before they are published, which is the owner's to choose." That read, until SJL-10.4, "Not yet
packed under a version of its own: the stream's next version is set when this entry is published.")*

### Added

- `Broiler.ECMAScript.Algorithms`: the Temporal core's first part - its records and limits, balancing,
  grammar and serialization - in a new namespace, `Broiler.ECMAScript.Temporal` (milestone SJL-10.3, part A;
  Broiler.VM's JSD-0078, following `tc39/proposal-temporal@e8cc03fc` by its decision D1; adapter guide
  [`docs/adapter-guide-temporal.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-temporal.md)). The
  records are SJL-8's `IsoDate`, `IsoTime` and `IsoDateTime`, extended and not redeclared (decision D8):
  - **records and limits**: `TemporalUnit`, `TemporalStatus`, `TemporalOverflow`, `DateDuration`,
    `InternalDuration` and `DurationFields`; `TemporalLimits`, whose `CheckIsoDate`, `CheckIsoDateTime`,
    `CheckIsoYearMonth`, `CheckIsoDaysRange`, `CheckEpochNanoseconds` and `CheckDuration` take any record and
    answer a status, none throwing (decision D9), with `IsValidTime`, `MaxTimeDuration` and the limits'
    constants;
  - **balancing, regulation and addition**: `IsoArithmetic` - `CompareIsoDate`, `CompareTime`,
    `CompareIsoDateTime`, `TimeToNanoseconds`, `BalanceIsoDate`, `AddDaysToIsoDate`, `BalanceTime` (its
    overflow days answered beside the `IsoTime`), `AddTime`, `DifferenceTime`, `RegulateIsoDate` and
    `RegulateTime` under both overflows, `AddIsoDate` (the ISO 8601 `CalendarDateAdd`),
    `GetUtcEpochNanoseconds`, `GetIsoPartsFromEpoch`, `BalanceIsoDateTime`, `RoundTime`, `RoundIsoDateTime`,
    `RoundTemporalInstant` and `AddInstant`;
  - **the grammar**: `TemporalGrammar` - `TryParse` for every goal (`TemporalGoal`) into an `IsoParseResult`
    with its `TemporalAnnotation`s, `TryParseInstant`, `TryParseTimeZoneString`, `TryParseTimeZoneIdentifier`
    (with `TimeZoneIdentifierRecord`), `TryParseCalendarString`, `TryParseOffsetString`, `TryParseDuration`,
    `IsTimeZoneIdentifier`, `IsUtcOffset` and `IsAnnotationValue`, each refusal a `TemporalParseError`, each
    member whose work grows with its input charging the caller's meter the text's length;
  - **serialization**: `TemporalFormat` - every string form (`DateToString`, `TimeToString`,
    `DateTimeToString`, `YearMonthToString`, `MonthDayToString`, `InstantToString`, `ZonedDateTimeToString`
    and `DurationToString`), `PadIsoYear`, `FormatFractionalSeconds`, `FormatTimeString`,
    `FormatCalendarAnnotation`, `FormatUtcOffsetNanoseconds` and `FormatDateTimeUtcOffsetRounded`, and the
    steps after the option reads of `PlainTime`, `PlainDateTime` and `Duration`'s `toString`;
  - **the options' rules of these sub-areas**: `TemporalOptionRules` - `overflow`, and the `toString` options
    `calendarName`, `timeZoneName`, `offset`, `fractionalSecondDigits`, `smallestUnit` with
    `ToSecondsStringPrecisionRecord` and its `SecondsStringPrecision`, `roundingMode`'s default, and
    `IsValidRoundingIncrement`; `ShowCalendarName`, `ShowTimeZoneName` and `ShowOffset`.

  Each is ported from Broiler.VM's `JsTemporalCore`, `JsTemporalParser`, `JsTemporal.Conversions`, and the
  engine-free arithmetic of `JsTemporal.Calendar`, `JsTemporal.Durations` and `JsTemporal.Options`, with its
  behaviour unchanged, which a differential check against that code showed, but for row R6: a limit check
  answers `OutOfRange` for a year past 2^53, where Broiler.VM's constructors ended the invocation (decision
  D9). The grammar is that engine's linear parser, with no regular expression, and refuses every string of
  the correspondence's rows G1 to G24 that it refuses.

- `Broiler.ECMAScript.Algorithms`: the Temporal calendars, namespace `Broiler.ECMAScript.Temporal` (milestone
  SJL-10.3, part B; JSD-0078's calendar sub-area; adapter guide
  [`docs/adapter-guide-temporal.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-temporal.md), section
  "Calendars"):
  - `CalendarId`, the sixteen calendar types of the Intl era and month code proposal's Table 1, and
    `TemporalCalendars.Canonicalize` and `Identifier`: CanonicalizeCalendar over the types and the two aliases
    `ethiopic-amete-alem` and `islamicc`, ASCII case folded, with `gregorian`, `minguo` and `islamic` refused
    (JSD-0078, decision D5);
  - `MonthCode` and `MonthCode.Parse`, ParseMonthCode with ASCII digits only; `CalendarFields`, the calendar half of a
    Calendar Fields Record, and `TemporalCalendars.MergeFields`, CalendarMergeFields with the keys each field
    entails, Japanese eras included; `TemporalCalendars.SupportsEra`;
  - `TemporalCalendars` over SJL-8's `ICalendarTables`: the formula calendars (the Gregorian family with Table 2's
    eras and the Japanese eras from Meiji 6, Coptic, Ethiopic and Amete Alem, Indian, the civil and astronomical
    tabular Hijri calendars and Hebrew) and the table calendars over `CalendarYears` (Chinese, Korean, Umm al-Qura and
    Persian); `IsoToDate`, the Calendar Date Record `CalendarDate`; `DateToFields`; `DateFromFields`,
    `YearMonthFromFields` and `MonthDayFromFields` under `CalendarOverflow`, with Table 3's month codes, the
    constraint of a missing leap month and the eras of Table 2 matched exactly (decision D4); and the month-day
    reference dates, Table 6 for `chinese` and `dangi` and the 1900 to 2035 rule elsewhere, an ISO month-day's year
    past 10^9 reduced modulo 400;
  - `ICalendarArithmetic`, the calendar provider of the durations sub-area, with `DateAdd` and `DateUntil` over
    `CalendarDuration` and `CalendarUnit`, which `TemporalCalendars` implements for every calendar, ISO 8601
    included; and the calendar steps of `toPlainYearMonth`, `toPlainMonthDay` and both `toPlainDate`s
    (`DateToYearMonth`, `DateToMonthDay`, `MonthDayToDate`, `YearMonthToDate`);
  - `CalendarStatus`, each failure a status, with `TemporalCalendars.IsTypeError` naming the four that are the
    specification's `TypeError`.

  Each is ported from Broiler.VM's `JsCalendars`, the core of `JsTemporal.Calendar` and `JsTemporal.CalendarNonIso`,
  with its behaviour unchanged: the library's tests replay Broiler.VM's recorded answers to correspondence
  `sjl-10-001`'s calendar rows C1 to C18 (4,416 lines) and 3,894 lines of the retained calendar dataset at library
  level, and every one answers as Broiler.VM does. Nothing in the area charges (JSD-0078's X3), and the library
  embeds no table.

- `Broiler.ECMAScript.Algorithms`: the Temporal durations, rounding and differences, namespace
  `Broiler.ECMAScript.Temporal` (milestone SJL-10.3, part C; JSD-0078's durations and rounding sub-area and its
  differences sub-area; adapter guide
  [`docs/adapter-guide-temporal.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-temporal.md), section
  "Durations, rounding and differences"):
  - `DurationMath`: `Sign` (DurationSign, DateDurationSign, InternalDurationSign), `DefaultLargestUnit`,
    `ToInternal`, `ToInternalWith24HourDays` and `FromInternal`, `Negate` and `Abs`, `CreateDateDurationRecord`,
    `AddTimeDuration`, `Add24HourDaysToTimeDuration`, `RoundTimeDurationToIncrement`, `RoundTimeDuration` and
    `TotalTimeDuration`; `AddDurations` (calendar units refused whatever the second argument), `Compare`, `Round` and
    `Total` after the methods' reads, `DateDurationDays` and `AddZonedDateTime`; and the relative machinery,
    `RoundRelativeDuration` with its nudge window, nudges and bubbling and `TotalRelativeDuration`, over exact
    rationals and `ExactRounding`, the pinned text's nudge after a shifted window (decision D7, row D8);
  - `TemporalDifferences`: `DifferenceTemporalPlainDate`, `…PlainDateTime`, `…PlainTime`, `…Instant`,
    `…ZonedDateTime` and `…PlainYearMonth`, with `DifferenceInstant`, `DifferenceIsoDateTime`,
    `DifferenceZonedDateTime` and their `WithRounding` and `WithTotal` forms, the calendars' through part B's
    `ICalendarArithmetic`;
  - `ITimeZoneArithmetic`, the time-zone provider of the zoned arithmetic, one zone per instance, and
    `TemporalTimeZone`, its implementation over SJL-8's `ZonedTime` and `ITimeZoneRules`; `RelativeTo`, the resolved
    `relativeTo` with its providers; `DifferenceSettings`, `DifferenceOperation`, `TemporalType` and
    `TemporalUnitGroup`;
  - `TemporalOptionRules`' rules of the rounding and difference options after an engine's reads:
    `TryParseRoundingIncrement`, `IsCalendarUnit`, `IsDateUnit`, `LargerOfTwoUnits`, `MaximumRoundingIncrement`,
    `IsValidUnit`, `TryGetDifferenceSettings` (GetDifferenceSettings for each type), `TryGetDurationRoundUnits`,
    `IsValidTotalUnit` and `IsValidRoundOptions`, with the defaults' constants;
  - `TemporalStatus.RelativeToRequired`.

  Each is ported from Broiler.VM's `JsTemporal.Durations`, the duration members of `JsTemporalCore`, the rule half of
  `JsTemporal.Options` and the arithmetic of its realm files' duration and difference methods, with its behaviour
  unchanged, which the library's tests and a differential check against that code show, but at row D12: where a
  zone's skipped day leaves a day nudge window with no length (Pacific/Apia's 2011-12-30), which the specification
  asserts cannot happen, the library answers `OutOfRange`, where Broiler.VM's code ends the invocation with a .NET
  exception and the reference polyfill fails its own assertion. The tests replay Broiler.VM's recorded answers to
  the correspondence's duration and difference rows, run both retained datasets whole at library level, and the
  library-level runner answers 10,851 of corpus revision 25's 10,878 Temporal vectors, every one as expected. Nothing
  in the area charges.

### Changed

None.

### Observable changes

None: the area is new, and no engine reads it yet. Broiler.VM cuts over at milestone SJL-10.4, correcting row
R6 there, and Broiler.JS at SJL-10.5, whose own records list what each observes; Broiler.JS's corrections on
the calendar rows are JSD-0078's C2, C5, C6, C9, C12, C13, C15, C16 and C17, and on the duration and difference rows
D3, D4, D6, D7, D8, D11, F4 and F7. Row D12's case, a day the zone skips, becomes a `RangeError` in both engines at
their cut-overs.

### Data

None. The table calendars read SJL-8's calendar table, which each engine hands in; its versions are unchanged.

## 0.1.0-preview.5 — published to nuget.org

*(Heading corrected 2026-10-17, when the entry was published; read "Unreleased". This entry was published
as `0.1.0-preview.5` by `Publish shared libraries`, run 39064131506, from `a04cd44`, whose library code is
`2d37a38`'s.)*

Packed as `0.1.0-preview.5`, the stream keeping its `0.1.0` prefix while it is a preview. Broiler.VM's cut-over
(SJL-8.5) admitted the packages packed at `2d37a38` from its committed feed before they were published, and its
feed now holds them as published. The source's suffix is `preview.5` since the publication. *(Corrected
2026-10-17, when the entry was published; read "To be published as `0.1.0-preview.5`, the stream keeping its
`0.1.0` prefix while it is a preview. Broiler.VM's cut-over (SJL-8.5) admits the package packed at `2d37a38`
under that version from its committed feed before it is published, which is the owner's to choose. The source's
suffix stays `preview.4` until then: the engine's packages depend on the shared library at the suffix's version,
and CI restores them from nuget.org." That read, until SJL-8.5, "Not yet packed under a version of its own: the
stream's next version is set when this entry is published.")*

### Added

- `Broiler.ECMAScript.Generation`, a new test-only package: the time data home's generator (milestone
  SJL-8.2; Broiler.VM's decision JSD-0076, decisions SJL-D-1 (b-iii), SJL-D-6 (c), SJL-D-20 and SJL-D-21),
  namespace `Broiler.ECMAScript.Generation.Time`. Ported from Broiler.VM's architecture tests with their
  behaviour unchanged:
  - `TzdbPin`, `CldrPin` and `CalendarPin`, with `TzdbPinMember`, `CldrPinEntry`, `CalendarPinArchive` and
    `CalendarPinMember`: each pin parsed from the text its caller hands in, and every archive, member and
    stated version compared with it before anything is read;
  - `TzdbCompiler`, the compiler of the nine default tz source files equivalent to `zic`, with
    `TzdbSource`, `TzdbRule`, `TzdbZoneLine`, `TzdbZone`, `TzdbTransition` and `TzdbFinalRules`;
  - `TzdbTableGenerator.Generate` and its `TzdbTables`: the compact zone table, the identifier table with
    CLDR's primaries, the Links and the UTC family, and `zone.tab`'s regions;
  - `CalendarTableGenerator.Generate` and its `CalendarTables`: the Qing, Chinese and Korean years, the Umm
    al-Qura years and the Persian corrections. Broiler.VM's regular expressions over ICU4X's Rust sources
    are replaced by readers written by hand, which the package's tests hold to those expressions;
  - `TzdbTableSource.Write` and `CalendarTableSource.Write`, with `TableSourceOptions`: the tables as a C#
    file for a namespace and class the caller chooses, which with Broiler.VM's options writes its
    `JsTzdbTables.g.cs` and `JsCalendarTables.g.cs` byte for byte;
  - `TimeDataVersions`: `TzdbRelease` (`2026e`), `CldrVersion` (`48.2.0`), `IcuCalendarVersion` (`2.3.0`)
    and `CalendricalCalculationsVersion` (`0.2.4`).

  The package reads no file and carries no table and no pinned input (SJL-D-6 (c)): those are files of
  this repository, under [`data/time/`](data/time/README.md).

- `Broiler.ECMAScript.Algorithms`: the time-zone and calendar area, namespace `Broiler.ECMAScript.Time`
  (milestone SJL-8.3, JSD-0076; adapter guide
  [`docs/adapter-guide-time-zones.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-time-zones.md)):
  - `ITimeZoneDatabase`, `ITimeZoneRules` and `TimeZoneId`, the area design's section 3.2 shape, and
    `TimeZoneDatabase`, the decoder of the data home's compact tables over the bytes an engine hands in:
    lookup under decision SJL-D-20's ASCII case rule, CLDR primaries, the primary identifiers, the zones of a
    region, and each zone's offset, daylight flag and next and previous transitions, its recurring rules
    continuing it with no 2037 cliff;
  - `OffsetTimeZones`: `IsTimeZoneOffsetString`, the offset time zone identifiers with ASCII digits only and
    their formatting, and the `Etc/GMT±n` zones as fixed offsets; and `FixedOffsetTimeZoneRules`;
  - `ZonedTime`: `GetOffsetNanosecondsFor`, `GetPossibleEpochNanoseconds` (sampled at the wall time and a day
    either side), `DisambiguatePossibleEpochNanoseconds` and `GetEpochNanosecondsFor` in all four modes,
    `GetStartOfDay`, `GetNextTransition`, `GetPreviousTransition` and `TimeZoneEquals`, over `IsoDate`,
    `IsoTime` and `IsoDateTime`, each failure a `TimeStatus`;
  - `ICalendarTables`, `CalendarTables`, the decoder of the calendar table, and `CalendarYears.GetYear`: a
    `chinese`, `dangi`, `islamic-umalqura` or `persian` year from the table inside its range and from ICU4X's
    approximation, the civil tabular year or the 33-year rule outside, as a `CalendarYear`.

  Each is ported from Broiler.VM's `JsTimeZones`, `JsTemporal.Zones`, `JsRealm.DateTimeFormat`'s zone
  resolution and `JsCalendars`, with its behaviour unchanged, which a differential check against that code
  over its own tables showed. Nothing in the area charges (JSD-0076's X3), and the library embeds no table.

### Changed

None.

### Observable changes

None: no engine reads the generator, the area or the tables yet. Broiler.VM regenerates its tables with the
generator and cuts over to the area at milestone SJL-8.5, and Broiler.JS embeds the tables and cuts over at
SJL-8.6, whose own records list what each observes.

### Data

- The time data home, [`data/time/`](data/time/README.md), outside every package: IANA tzdb **2026e**,
  CLDR **48.2.0**'s `bcp47/timezone.json`, and ICU4X's `icu_calendar` **2.3.0** and
  `calendrical_calculations` **0.2.4**, each pin a byte-for-byte copy of Broiler.VM's at `9508bd5`; and the
  four tables generated from them with their manifest, byte for byte the data Broiler.VM's generated files
  hold at that commit. Rule SL7 regenerates them. The algorithms library embeds no table: each engine
  hands its tables to the decoders.

## 0.1.0-preview.4 — published to nuget.org

*(Heading corrected 2026-10-15, when the entry was published; read "Unreleased". This entry was published
as `0.1.0-preview.4` by `Publish shared libraries`, run 38813070829, from `d1e4b11`.)*

Packed as `0.1.0-preview.4`, the stream keeping its `0.1.0` prefix while it is a preview.
*(Corrected 2026-10-15; read "To be packed as `0.1.0-preview.4`, the stream keeping its `0.1.0` prefix
while it is a preview.")*

### Added

- `Broiler.ECMAScript.Algorithms`: the Date area, namespace `Broiler.ECMAScript.Time` (milestone SJL-6.3;
  adapter guide [`docs/adapter-guide-date.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-date.md)):
  - `EcmaTime`: the time-value arithmetic of ECMA-262 21.4.1, `Day` to `msFromTime`, `MakeTime`,
    `MakeDay`, `MakeDate`, `MakeFullYear` and `TimeClip`, with `MsPerSecond` to `MsPerDay` and
    `MaxTimeValue`; every step the specification writes on mathematical values is exact;
  - `IsoCalendarMath`: the Temporal proposal's ISO 8601 civil-day operations over 64-bit integers -
    `IsLeapYear`, `DaysInYear`, `DaysInMonth`, `EpochDays`, `FromEpochDays`, `DayOfWeek`, `DayOfYear`,
    `WeekOfYear`, `BalanceYearMonth` and `IsValidIsoDate`;
  - `ILocalTimeProvider`, the local-time provider of decision SJL-D-19, with
    `FixedOffsetLocalTimeProvider`, and `EcmaLocalTime`: `LocalTime` and `UTC` with the specification's
    disambiguation;
  - `EcmaDateParser.Parse` with its `DateParseDialect` (`Strict`, and `BroilerVM` with correction P9),
    its optional `WorkCharge` and its `EcmaParsedDate` of kind `Utc`, `Local` or `Invalid`;
  - `EcmaDateFormatter`: `TimeString`, `DateString`, `TimeZoneString`, `ToDateString`, `ToUtcString` and
    `TryToIsoString`, the zone's offset and name coming from the provider.

  The parser charges the text's length before reading it (JSD-0072, decision 4); nothing else in the
  area charges. Every Date vector of corpus revision 15 that the library answers passes at library level.

### Changed

None.

### Observable changes

None: the area is new in this stream. What each engine observes at its cut-over is listed in that
engine's own records.

### Data

None.

## 0.1.0-preview.3 — published to nuget.org

*(Heading corrected 2026-10-15, at milestone SJL-6.3, when the next version's entry was opened; read
"Unreleased". This entry was published as `0.1.0-preview.3` by `Publish shared libraries`.)*

Packed as `0.1.0-preview.3`, the stream keeping its `0.1.0` prefix while it is a preview.
*(Corrected 2026-10-15; read "To be packed as `0.1.0-preview.3`, the stream keeping its `0.1.0` prefix
while it is a preview.")*

### Added

- `Broiler.ECMAScript.Algorithms`: the text area, namespace `Broiler.ECMAScript.Text` (milestone SJL-5.3;
  adapter guide [`docs/adapter-guide-text.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-text.md)):
  - `Utf16`: `CodePointAt` with its `CodePointRecord`, `IsWellFormed`, `ToWellFormed` and
    `AdvanceStringIndex`. `TrimString` stays `NumberParsing.TrimStrWhiteSpace`;
  - `StringPadding.Pad` (`StringPad`) under the caller's longest String, with `PadPlacement`; and
    `RelativeIndex`, the relative-index normalisations: `Clamped`, `TryElement` and `LastSearchStart`;
  - `ReplacementTemplate.Parse`, `GetSubstitution`'s template parsed into the `ReplacementPart`s an
    engine evaluates;
  - `UriCodec`: `Encode`, `Decode`, Annex B's `Escape` and `Unescape`, and `HexDigitValue`;
  - `Base64Codec`: `FromBase64` with its `Base64Alphabet` and `LastChunkHandling`, `FromHex`, `ToBase64`
    and `ToHex`, decoding into the caller's span;
  - `JsonReader`, a pull reader of `JSON.parse`'s grammar under the caller's `DepthLimit`, with its
    `JsonToken`s and `Validate`; and `JsonText`: `Quote` (`QuoteJSONString`) and the gap.

  Every operation whose work grows with its input takes a `WorkCharge` and charges Broiler.VM's units
  before the cut-over (JSD-0070, decision 3). Every text vector of corpus revision 11 that the library
  answers passes at library level.

### Changed

None.

### Observable changes

None: the area is new in this stream. What each engine observes at its cut-over is listed in that
engine's own records.

### Data

None.

## 0.1.0-preview.2 — published to nuget.org

*(Heading corrected 2026-10-14, at milestone SJL-5.3, when the next version's entry was opened; read
"Unreleased". This entry was published as `0.1.0-preview.2` by `Publish shared libraries`.)*

Packed as `0.1.0-preview.2`: the owner kept the stream's `0.1.0` prefix while it is a preview, though a
new API is a minor version. *(Corrected 2026-10-14; read "Packed as `0.2.0-preview.1`: a new API is a
minor version.")*

### Added

- `Broiler.ECMAScript.Algorithms`: the numerics area, namespace `Broiler.ECMAScript.Numbers`
  (milestone SJL-4.3; adapter guide
  [`docs/adapter-guide-numerics.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numerics.md)):
  - `IntegerConversions`: `ToInt32`, `ToUint32`, `ToInt16`, `ToUint16`, `ToInt8`, `ToUint8`,
    `ToUint8Clamp`, `ToIntegerOrInfinity`, `ToLength`, `TryToIndex`, `ToBigInt64` and `ToBigUint64`;
  - `Float16`: `Encode` under a `Float16NanPolicy`, `Decode` and `Round` (`Math.f16round`);
  - `MathKernels`: `Round`, `Power` (`Number::exponentiate`), `Hypot`, `Cbrt`, `Expm1` and `Log1p`,
    each with its precision stated; and `PreciseSum`, the accumulator of `Math.sumPrecise`;
  - `ExactRounding`, public from this version: the rounding modes (`RoundingMode`,
    `UnsignedRoundingMode`), their option names, negation and application, the increment roundings,
    and `ToDouble`, a ratio of integers rounded once to the nearest Number;
  - the BigInt kernel: `BigIntMath`, every `BigInt::` operation, `asIntN`, `asUintN` and the
    conversions to and comparison with a Number, under a `BigIntPolicy` and charged in Broiler.VM's
    units; and `BigIntText`, `BigInt::toString` at every radix and `StringToBigInt`.

  Every numerics vector of corpus revision 8 that the library answers passes at library level.
  fdlibm 5.3 (SunPro notice) is ported for `Expm1` and `Log1p`, and the BigInt kernel is ported from
  Broiler.VM's `JsBigInt`; see `THIRD_PARTY_NOTICES.md`.

### Changed

- `BigIntPolicy.RepresentationLimit` is 2^31 - 128 bits, no longer 2^31 - 1 (2026-10-13, milestone
  SJL-4.5, after the pack Broiler.VM's committed feed held as `0.2.0-preview.1` until it was re-pinned
to the published `0.1.0-preview.2`) *(corrected 2026-10-14; read "after the `0.2.0-preview.1` pack that
Broiler.VM's committed feed holds")*. A .NET
  `BigInteger` holds at most 2^31 - 64 bits and throws `OverflowException` past them, so under
  `BigIntPolicy.Unbounded` an operation whose result was between the two widths threw instead of
  answering `LimitExceeded`: `1n << 2147483600n`, `(-1n) << 2147483646n` and
  `BigInt.asUintN(2147483600, -1n)` among them. An operation computes at most four bits past its
  policy before it refuses, so the new limit leaves 64 bits of room. Broiler.JS found it at its
  cut-over, which passes `Unbounded`; a policy of 2^20 bits, Broiler.VM's, never came near it.

### Observable changes

None: the area is new in this stream. What each engine observes at its cut-over is listed in that
engine's own records.

### Data

None.

## 0.1.0-preview.1 — published to nuget.org

*(Heading corrected 2026-10-13, at milestone SJL-4.3, when the next version's entry was opened; read
"Unreleased". This entry was published as `0.1.0-preview.1` by `Publish shared libraries`.)*

### Added

- `Broiler.ECMAScript.Algorithms`: the contract types of the conventions, `WorkCharge`,
  `ResultStatus`, `Failure`, `LengthLimit` and `DepthLimit`.
- `Broiler.ECMAScript.Algorithms`: the number-conversion area, namespace
  `Broiler.ECMAScript.Numbers` (milestone SJL-3.3; adapter guide
  [`docs/adapter-guide-numbers.md`](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numbers.md)):
  - `NumberText`: `Number::toString` at radix 10 (`ToString`, `TryFormat`) and at every radix from 2
    to 36 (`ToRadixString`), and the digits of `toFixed`, `toExponential` and `toPrecision`;
  - `NumberParsing`: `StringToNumber`, `ParseFloat` and `ParseInt`, correctly rounded at every
    radix, and the `StrWhiteSpaceChar` set (`IsStrWhiteSpaceChar`, `TrimStrWhiteSpace`);
  - `NumericLiterals.Evaluate`: the value of a numeric literal in every form, BigInt literals under
    the caller's width limit, and every refusal as a `Failure` with a message identifier;
  - `NumericKeys`: `TryParseArrayIndex` and `IsCanonicalNumericString`.

  Every number vector of corpus revision 4 passes at library level. Ryu (Apache-2.0) is ported for
  the shortest radix-10 digits; see `THIRD_PARTY_NOTICES.md`.

### Changed

None.

### Observable changes

None: the area is new in this stream. What each engine observes at its cut-over is listed in that
engine's own records (Broiler.JS's changelog; Broiler.VM's JSC entries).

### Data

- Unicode 17.0.0 (`NumberParsing.UnicodeVersion`): the `Space_Separator` members of the
  `StrWhiteSpaceChar` set, generated from the pinned `PropList.txt` by
  `scripts/conformance/generate_white_space_table.py` (convention 13, revised 2026-10-12).

## 0.0.1-probe.1 — not published

The probe pre-release of milestone SJL-2.13: `Broiler.ECMAScript.Algorithms` holding the contract
types listed above under Unreleased. It is packed for Broiler.VM's admission probe and reaches
Broiler.VM through that repository's committed feed. **It is never pushed to nuget.org**: no
Broiler.VM release depends on it, and its version is below every version this stream will publish.
