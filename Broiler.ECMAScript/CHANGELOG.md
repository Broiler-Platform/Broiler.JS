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

To be published as `0.1.0-preview.5`, the stream keeping its `0.1.0` prefix while it is a preview. Broiler.VM's
cut-over (SJL-8.5) admits the package packed at `2d37a38` under that version from its committed feed before it is
published, which is the owner's to choose. The source's suffix stays `preview.4` until then: the engine's packages
depend on the shared library at the suffix's version, and CI restores them from nuget.org. *(Revised 2026-10-17,
at SJL-8.5; read "Not yet packed under a version of its own: the stream's next version is set when this entry is
published.")*

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
