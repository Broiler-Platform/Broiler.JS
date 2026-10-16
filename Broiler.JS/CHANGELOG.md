# Changelog — Broiler.JS

Changes to the engine (the `Broiler.JavaScript.*` packages) a consumer can observe, newest first.
This file starts with the shared JavaScript libraries programme: under its decision SJL-D-16 (a), a
cut-over that moves an area to a shared library lists here, by name, every result that changes
because the shared implementation follows the specification where this engine did not, each with the
vector or test262 path that names it. Earlier changes are recorded in the history and in
[`docs/public-api.md`](../docs/public-api.md). The shared libraries have their own changelog,
[`Broiler.ECMAScript/CHANGELOG.md`](../Broiler.ECMAScript/CHANGELOG.md).

## Unreleased

### The `era` option is written beside a format with no year (2026-10-16)

- **`Intl.DateTimeFormat` writes the `era` option's era beside a format that shows no year**, where the
  pattern appended its era field only to a date with a year, so the option was dropped from `format`
  and `formatToParts` alike. It follows a date, `{ era: "short", month: "short", day: "numeric" }`
  formats as `"Oct 16 AD"` where it was `"Oct 16"`, and leads a time alone, joined as a date is:
  `{ era: "short", minute: "2-digit", second: "2-digit" }` is `"AD, 03:05"`, where it was `"03:05"`.
  These are CLDR's en layouts, and V8's (ICU 77.1, CLDR 47) for the same options.
- **A lone day, month or weekday beside a short era is appended to the era**, with CLDR's en
  appendItems as V8 writes them: `{ era: "short", day: "numeric" }` is `"AD (day: 16)"`,
  `{ era: "short", month: "short" }` is `"AD (month: Oct)"`, and `{ era: "short", weekday: "short" }` is
  `"AD Fri"`; beside a time it joins as a date does (`"AD (day: 16), 12:03 PM"`). The field names are
  en's in every locale, as this engine's month, weekday and era names are. Beside a long or narrow era
  the field comes first (`"16 Anno Domini"`, `"October A"`), as ICU's skeleton matching mostly has it;
  ICU also appends for some of those widths and styles (`{ era: "long", day: "2-digit" }` is
  `"Anno Domini (day: 16)"` in V8, `"16 Anno Domini"` here), and joins a long month to a time with
  `" at "`, which this engine's patterns do not model.

An era-using calendar's era written without the option, such as `buddhist`'s `BE`, still needs a year,
as in CLDR; a Temporal type with no era, `PlainTime` or `PlainMonthDay`, still drops the option.
`DateTimeFormatEraWithoutYearTests` holds the cases.

### `formatToParts` writes a minute-and-second format through the pattern `format` writes (2026-10-16)

`Intl.DateTimeFormat.prototype.formatToParts` wrote a format with `minute` and `second` but no `hour` as
`mm:ss[.fff]` by hand, where `format` writes the format's resolved pattern. The hand-written branch is
gone, and such a format's parts join to what `format` writes. In the Latin-digit locales, for the formats
that had no other field, the parts are unchanged.

- **The digits follow the numbering system**, a locale's default one or a `-u-nu-` extension's, as
  `format`'s do: `new Intl.DateTimeFormat("ar-EG", { minute: "2-digit", second: "2-digit" })` writes the
  minute `"٠٣"`, where it was `"03"`, and with `fractionalSecondDigits` `arab` writes the fraction after
  its decimal separator U+066B, where it was `"."`.
- **The other fields the format asks for are written**: a date field, `weekday` or `timeZoneName` beside
  the minute and second was dropped from the parts, so
  `{ day: "numeric", minute: "2-digit", second: "2-digit" }` gave `minute`, `literal` and `second` alone.

`DateTimeFormatMinuteSecondPartsTests` holds the cases. The time-zone correction below holds on the
pattern's path, which reads the same zoned fields, and its tests still pass.

### `formatToParts` writes a minute and second in the format's time zone (2026-10-16)

- **`Intl.DateTimeFormat.prototype.formatToParts` writes a format with `minute` and `second` but no
  `hour` in its resolved `timeZone`**, from the zoned fields `format` writes, where it used the host's
  zone through `DateTimeOffset.ToLocalTime()`, bypassing this engine's provider of the host's zone
  (decision SJL-D-19) that `format` goes through. A zone whose offset is not a whole hour had the host's
  minute: `new Intl.DateTimeFormat("en", { minute: "2-digit", second: "2-digit", timeZone: "Asia/Kolkata" })`
  formats 12:00:05Z as parts that join to `"30:05"`, like `format`, where on a UTC host the minute was
  `"00"`. The fractional seconds come from the same fields.
- **That format writes a time value outside the years 0001 to 9999**: `formatToParts(8.64e15)` is
  `"00:00"` in UTC, where .NET's `ArgumentOutOfRangeException` reached the caller.

`DateTimeFormatMinuteSecondTimeZoneTests` holds the cases. Milestone SJL-6.6's duplicate search
(`simplification/duplicate-search.log` of bundle `sjl-6-001`) found it and named it for milestone SJL-9.

### The Date area moves to `Broiler.ECMAScript.Algorithms` (2026-10-16, milestone SJL-6.5)

The Date area is the shared library's, the stream's `0.1.0-preview.4` in this repository's
`Broiler.ECMAScript`:

- the time arithmetic of every getter and setter, the constructor, `Date.UTC`, `getYear` and `setYear`
  (`EcmaTime`);
- local time, `LocalTime` and `UTC` with the specification's disambiguation of skipped and repeated local
  times, over this engine's provider of the host's zone: `TimeZoneInfo.Local`, read at each instant with
  its daylight-saving time (decision SJL-D-19, `EcmaLocalTime`);
- `Date.parse`'s Date Time String Format, through the library's strict dialect, and Broiler.VM's dialect
  after this engine's own fallback (`EcmaDateParser`);
- `toString`, `toDateString`, `toTimeString`, `toUTCString` and `toISOString` (`EcmaDateFormatter`);
- the civil-day arithmetic of `Temporal.Instant`, `PlainDate`, `PlainDateTime`, `PlainMonthDay`,
  `PlainYearMonth` and `ZonedDateTime`, of the ISO string reader and the non-ISO calendars, and Intl's
  date fields (`IsoCalendarMath`, `EcmaTime`).

**A `Date`'s storage is its time value alone**, where it was a `DateTimeOffset` with a raw time value
beside it for the dates the type cannot hold; the CLR views of it are in
[`docs/public-api.md`](../docs/public-api.md). The built-ins keep their argument conversion and its
order, their errors, the fallback of `Date.parse` and the .NET format string of `toLocaleString` (decision
SJL-D-17). **Two of this engine's own readings are decided** (correspondence rows P4 to P6 and R5):

- **`Date.parse` keeps the format's leniencies its own grammars read**, now in every form of the format
  and every year, where the .NET format lists read them in the years 0001 to 9999 only and the expanded
  years' expression read only some: a fraction of none to nine digits, cut to milliseconds
  (`"2021-03-04T10:00:00.1Z"` is 1614852000100), and an offset without its colon
  (`"0001-01-01T00:00-0530"`). A lower-case `t` keeps the culture parse's reading, and a `'T'`-joined
  string the format does not hold is still not read leniently (test262 `staging/sm/Date/non-iso.js`).
- **A .NET format string formats a date from the year 1 to the year 9999 only**:
  `new Date(Date.UTC(-1, 0, 1)).toLocaleString(undefined, "yyyy-MM-dd")` is a `RangeError`, where it
  answered `"0001-01-01"`, the invalid-date sentinel's fields. Within those years it formats the instant
  at the offset in force at it, where it used the offset the date was stored with (L2).

**`Date.parse` reads what both its grammars refuse in Broiler.VM's dialect** (JSD-0072's order: the
format, this engine's fallback, then Broiler.VM's). Every string this engine read keeps its reading,
except where a correction below says otherwise; among the strings it now reads are its own renderings of
the years outside 0001 to 9999 (P9, below), a day past its month's length, carried into the next month
(`"2021-02-30"` is March 2, where it was NaN, P5), and the dialect's textual forms, a weekday that does not
match its date and an offset of 60 minutes among them (P7).

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/date`, revision 16), with the row of bundle `sjl-6-001`'s correspondence it
confirmed (Broiler.VM's `docs/evidence/sjl-6-001/correspondence.md`). Corpus revision 17 retires the 1,016
fields, and [`docs/evidence/sjl-6-001/engine/retired-divergences-revision-17.tsv`](../docs/evidence/sjl-6-001/engine/retired-divergences-revision-17.tsv)
lists every retired vector under its reason.

- **One offset per instant, the one in force at it** (L2, 892 vectors of `date/offset`, `date/to-string`,
  `date/local-string`, `date/local` and `date/date-time`, in a zone with daylight-saving time): in
  New York, `new Date(2021, 6, 1)` is 04:00Z, midnight EDT, where storage and the components constructor
  took the zone's base offset, so it was 05:00Z with `getHours()` 0 and `toString()` 01:00 EDT; a local
  setter kept whichever offset the date was stored with.
- **A date-only form is UTC** (P1, 59 vectors of `date/date-only` and `date/utc`, in a zone whose offset is
  not zero): `Date.parse("2021-03-04")` is UTC midnight, where it was local midnight for the years 0001 to
  9999.
- **The ISO week of a date in a negative year** (G2, 22 vectors of `date/iso-week`):
  `Temporal.PlainDate.from("-000002-12-28").weekOfYear` is 53 of -2, where `WeeksInIsoYear` divided with
  truncation and answered week 1 of -1.
- **`Date.parse` reads back this engine's renderings of the years outside 0001 to 9999** (P9, 16 vectors of
  `date/round-trip`): `Date.parse(new Date(Date.UTC(-1, 0, 1)).toUTCString())` is that date, where the
  .NET formats could not hold the year and answered NaN.
- **A setter converts every argument before it writes the date** (T3, 14 vectors of `date/setter-order`):
  `setUTCMinutes`, `setUTCSeconds`, `setUTCMonth`, `setHours`, `setMinutes`, `setSeconds` and `setMonth`
  made the date invalid as soon as their first argument was `undefined` or NaN, so a later argument's
  `valueOf` read `getTime()` as NaN. `setYear` likewise reads the time value before it converts its
  argument, where it read it after, so a `valueOf` that moved the date set the year of the moved date.
- **A setter answers a valid date outside the years 0001 to 9999** (D1, 5 vectors of `date/utc-setter` and
  `date/local-setter`): `new Date(9999, 11, 31).setDate(32)` is January 1 of 10000, where the
  `DateTimeOffset` arithmetic threw and the date became invalid.
- **A setter sets a date outside those years** (D2, 5 vectors of the same files): `new Date(-1e15).setHours(1)`
  answers the new date, where it answered NaN and left the date unchanged.
- **The instant 0001-01-01T00:00:00.000Z is a date** (D3, 2 vectors of `date/utc-setter` and
  `date/round-trip`), -62135596800000, where it was the invalid-date sentinel.
- **A month at or past 2^53 is taken exactly** (T4, `date/make-day/0863`):
  `Date.UTC(-833333333333333333, 1e19, 1)` is May 1 of year 0, -62156764800000, where the month saturated
  as a 64-bit integer and the date fell in August.

**Corrections the corpus does not hold.** Their outcome depends on the zone, the culture or the host;
`SharedDateAlgorithmTests` holds each, the zone's in a collection that runs under `America/New_York`.

- **A skipped local time is read at the offset before its transition, a repeated one at its earlier
  instant, and a local time just after a transition at the offset after it** (L3): in New York,
  `new Date(2021, 2, 14, 2, 30)` is 07:30Z, 03:30 EDT, where `getHours()` answered 2;
  `new Date(2021, 10, 7, 1, 30)` is 05:30Z, where it was 06:30Z; and
  `Date.parse("2021-03-14T03:30:00.0000")` is 07:30Z, where it was 08:30Z.
- **A date outside `DateTimeOffset`'s range takes the offset the zone's rules give at its instant** (L4),
  where it took the offset in force at the moment of the call: in New York, `new Date(10000, 0, 1)` is
  05:00Z in any season, and a date before the zone's standard time takes its local mean time.
- **`getTimezoneOffset` reads a date outside the years 0001 to 9999** (D4), where it answered NaN.
- **A structured clone keeps a date outside those years** (D5): `structuredClone(new Date(-1e15))` is
  that date, where the clone copied only the `DateTimeOffset` storage and was invalid.
- **`toString`'s and `toTimeString`'s offset is written with U+002D** (R3), where `GMT-0500` was written
  with the culture's negative sign, U+2212 under `sv-SE`, and did not read back (P8).
- **`toUTCString` reads back under any culture** (P8): `Date.parse` reads its form before the culture's
  parse, which read the year in the culture's calendar under `fa-IR` and `ar-SA`.
- **A Gregorian-family `PlainMonthDay` with a year and a month past 12 is bound-checked in December**, as
  `constrain` regulates the month, where the civil copy the check called carried the month past the year:
  `Temporal.PlainMonthDay.from({ calendar: "gregory", year: 275759, month: 21, day: 13 })` is `12-13`, where
  it was a `RangeError`. The shared `EpochDays` takes a month from 1 to 12 only; `reject` refuses such a
  month either way.

**Messages and names.** The zone's name in parentheses is the host's daylight or standard name at the
instant (R4), where a date outside `DateTimeOffset`'s range took the name in force at the moment of the
call.

### Each locale's default numbering system is CLDR's (2026-10-15, milestone SJL-5.6)

A locale's default numbering system is CLDR's, release 47.0.0, where every locale defaulted to `latn`.
The 52 locales CLDR gives another default write its digits: `ar-EG`, `ar-SA` and the other Arabic regions
CLDR gives Arabic-Indic digits (`new Intl.NumberFormat("ar-EG").format(1234)` is `"١٬٢٣٤"`, where it was
`"1٬234"`), `fa`, `ps` and `ur-IN` Extended Arabic-Indic digits, `bn` Bengali, `mr` and `ne` Devanagari,
`my` Myanmar, and so on. `ar`, `en` and every other locale keep `latn`. NumberFormat, DateTimeFormat,
RelativeTimeFormat and DurationFormat resolve it, their `resolvedOptions().numberingSystem` reports it, and
`Intl.Locale.prototype.getNumberingSystems()` puts it first; a `-u-nu-` extension or a `numberingSystem`
option still chooses another. The table is generated by `scripts/intl/generate_default_numbering_systems.py`
from CLDR's `cldr-numbers-full` 47.0.0, which [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) names,
under the owner's data decision of 2026-10-15 recorded in
[`docs/roadmap/SharedLibraries.md`](../docs/roadmap/SharedLibraries.md); milestone SJL-9 moves it into the
shared locale data.

- **A Temporal value's digits follow the numbering system.** `Intl.DateTimeFormat.prototype.format` and
  `formatToParts`, and the Temporal types' `toLocaleString`, wrote a Temporal value's digits in ASCII
  whatever the numbering system, where a `Date`'s followed it (`formatRange` already did). test262:
  `intl402/DateTimeFormat/prototype/format/temporal-objects-no-time-clip-non-latin-numerals.js`, which
  passed before only because `String.prototype.includes` compared by culture, so that Latin `13` matched
  Arabic-Indic `١٣`: correction X2 below made the search ordinal, and the digits showed. Tests:
  `DefaultNumberingSystemTests`.

### Text algorithms move to `Broiler.ECMAScript.Algorithms` (2026-10-15, milestone SJL-5.5)

The text area is the shared library's, the stream's `0.1.0-preview.3` in this repository's
`Broiler.ECMAScript`:

- UTF-16: `isWellFormed`, `toWellFormed`, `codePointAt`, the String iterator's step, and
  `AdvanceStringIndex` wherever a RegExp method moves past an empty match (`[Symbol.split]`,
  `[Symbol.match]`, `[Symbol.replace]`, `matchAll`'s iterator and the engine's own enumerations);
- `padStart` and `padEnd`'s StringPad, and the relative-index normalisation of String `at`, `slice` and
  `substr`; Array `at`, `includes`, `indexOf`, `lastIndexOf`, `slice`, `toSpliced`, `with`, `copyWithin`,
  `fill` and `splice`; the typed arrays' `at`, `copyWithin`, `fill`, `includes`, `indexOf`, `lastIndexOf`,
  `slice` and `subarray`; and `ArrayBuffer`'s `slice` and `sliceToImmutable` and `SharedArrayBuffer`'s
  `slice`;
- the replacement templates of `replace`, `replaceAll` and `RegExp.prototype[Symbol.replace]`, which the
  library parses and this engine evaluates part by part;
- `encodeURI`, `encodeURIComponent`, `decodeURI`, `decodeURIComponent`, `escape` and `unescape`;
- `Uint8Array`'s base64 and hex codecs;
- `JSON.parse`'s and `JSON.rawJSON`'s reading, which no longer goes through System.Text.Json, and
  `JSON.stringify`'s quoting and gap.

The built-ins keep their coercions, their option reading, their errors and their drivers. **Two
policies are this engine's, and stated:** the longest String a pad or a repeat builds is
`JSString.MaximumLength`, 1,073,741,791 code units, the longest a .NET string can be, and past it the
call is a `RangeError`; and `JSON.parse` and `JSON.rawJSON` read a value at most 512 levels deep
(`JSJsonParser.MaximumDepth`), the document being level 0, and past it the parse is a `RangeError`. A
typed array's `with` keeps its normalisation inline: its relative step uses the length before the
value's conversion and its range test the length after it, which the shared one-length rule cannot
say. The API dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/text`, revision 12), with its reason in `scripts/conformance/text-divergences.txt`
and the row of bundle `sjl-5-001`'s correspondence it confirmed. Corpus revision 13 retires the 42 fields,
and [`docs/evidence/sjl-5-001/engine/retired-divergences-revision-13.tsv`](../docs/evidence/sjl-5-001/engine/retired-divergences-revision-13.tsv)
lists every retired vector under its reason.

- **`setFromHex` stops at the destination** (B1, `js-set-from-hex-whole-input`,
  `text/set-from-hex/0001`, `0002`, `0011` to `0013` and `0021`): `new Uint8Array(1).setFromHex("aaZZ")`
  is `{ read: 2, written: 1 }`, where the whole text was parsed first and the `ZZ` the method never
  reads raised `SyntaxError`.
- **An options argument that is not an object is a `TypeError`** (B2, `js-options-not-object`,
  `text/options/0001`, `0002` and `0006`): `Uint8Array.fromBase64("", 1)` and `toBase64(null)` were
  read as no options.
- **A `null` `alphabet` or `lastChunkHandling` is a `TypeError`** (B3, `js-options-null-name`,
  `text/options/0003` and `0004`), where it was read as the default.
- **A detached or out-of-bounds array is a `TypeError` to the codecs** (B4, `js-detached-codec`,
  `text/options/0009` to `0012`): `toBase64` and `toHex` answered `""`, and `setFromBase64` and
  `setFromHex` answered `{ read: 0, written: 0 }`.
- **`JSON.parse` reads past 64 levels** (J1, `js-json-depth`, `text/parse-depth/0011` to `0022`):
  System.Text.Json's default depth, which nobody chose, refused a document nested 65 deep with
  `SyntaxError`; the depth is now the policy above.
- **A lone surrogate in a JSON string is that code unit** (J2 and J3, `js-json-escaped-surrogate` and
  `js-json-raw-surrogate`, `text/parse/2364`, `2365`, `2367` and `2368` and `text/parse-surrogate/0001`
  to `0004`): `JSON.parse('"\\ud800"')` raised `SyntaxError`, and a raw lone surrogate let the .NET
  `ArgumentException` of the text's transcoding to UTF-8 reach the script as a plain `Error`.
- **Only `JSON.rawJSON` makes a raw JSON object** (J6, `js-raw-json-brand`, `text/raw-json/0005` and
  `0006`): the brand is an internal slot of the object `JSON.rawJSON` returns, where any frozen object
  with an own string `rawJSON` passed for one, and `JSON.stringify(Object.freeze({ rawJSON: "}{" }))`
  wrote `}{` into its output unchecked.
- **The raw JSON object's `rawJSON` is enumerable** (J7, `js-raw-json-enumerable`, `text/raw-json/0003`):
  `Object.keys(JSON.rawJSON("1"))` is `["rawJSON"]`, where it was `[]`.
- **`%TypedArray%.prototype.lastIndexOf` reads `fromIndex` whenever a second argument is passed** (P5,
  `js-last-index-of-from-index`, `text/last-index-of/0002`), where a third argument made it ignored.
- **A named group's value is converted with ToString** (S2, `js-named-group-to-string`,
  `text/named-to-string/0001`): its `Symbol.toPrimitive` is called first, where the CLR's `ToString`
  called its `toString` directly.
- **`replace` with a RegExp whose `Symbol.replace` is `undefined` or `null` searches for its text** (S3,
  `js-absent-replace`, `text/absent-replace/0001` and `0002`): `"a/a/".replace(re, "x")` searches for
  `"/a/"`, where the pattern was matched as a regular expression by `JSRegExp.Replace`, whose templates
  never substituted `$<name>`.

**Corrections the corpus does not hold.** `SharedTextAlgorithmTests` holds each, beside control cases
that pass on both sides of the cut-over.

- **A pad or a repeat past the length policy is a `RangeError`** (P4): `"x".padEnd(2 ** 30 + 1)` and
  `"x".repeat(2 ** 30 + 1)` let .NET's `OutOfMemoryException` reach the script as a plain `Error`.
- **`split` with an empty separator honours its limit** (X1): `"abc".split("", 2)` is `["a", "b"]`,
  where it was `["a", "b", "c"]`.
- **`indexOf`, `includes`, `startsWith`, `endsWith` and the non-standard `contains` compare code units**
  (X2), where they used the current culture's comparison, which under ICU reads `"\r\n"` as one unit
  and ignores ignorable characters: `"a\r\nb".indexOf("\n")` is 2.

**Messages.** A JSON text `JSON.parse` refuses is a `SyntaxError` naming where the reader stopped, such
as "Unexpected token 'x' in JSON at position 3" or "Unexpected end of JSON input at position 1", where
the message was System.Text.Json's. A URI function's `URIError` and the codecs' `SyntaxError`s keep
their messages. `JSON.stringify`'s line terminator, the correspondence's J5, was corrected on
2026-10-14 (below).

### `JSON.stringify` breaks lines with LINE FEED on every platform (2026-10-14)

- **An indented `JSON.stringify` ends each line with LINE FEED (U+000A) alone**, as
  SerializeJSONObject and SerializeJSONArray specify: `JSON.stringify({ a: 1 }, null, 2)` is
  `"{\n  \"a\": 1\n}"` on every platform, where on Windows each line ended in CR LF, the platform's line
  terminator. Compact output, without a `space` argument, had no line breaks and is unchanged.
  `JsonStringifyLineTerminatorTests` holds the cases; test262's JSON tests run on Linux, where the two
  terminators agree, and the Windows Release job of #1025 found it.

### Numerics move to `Broiler.ECMAScript.Algorithms` (2026-10-13, milestone SJL-4.5)

The numerics area is the shared library's, version `0.1.0-preview.2` *(corrected 2026-10-14; read
"version `0.2.0`")*:

- the integer conversions (`ToInt32` to `ToUint8Clamp`, `ToIntegerOrInfinity`, `ToLength`, `ToIndex`'s
  range rule, `ToBigInt64` and `ToBigUint64`) and Float16;
- `Math.round`, `Math.pow` and `**` (`Number::exponentiate`, on every path the compiler has),
  `Math.hypot`, `cbrt`, `expm1`, `log1p` and `f16round`, and `Math.sumPrecise`'s sum;
- the exact rounding of Temporal and Intl: the rounding modes, `RoundNumberToIncrement` and the
  rounding of a ratio to the nearest Number;
- the BigInt kernel: every `BigInt::` operation, `BigInt.asIntN` and `asUintN`, a BigInt's Number and
  its comparison with a Number, its text at every radix, and `StringToBigInt`.

`Broiler.JavaScript.Expressions` references the shared library beside `Parser`, `Runtime`,
`BuiltIns` and `Storage`, for the compiled `**`. The built-ins keep their coercions, their errors, their
option reading and their value wrapping. The BigInt kernel runs under decision SJL-D-15 (a): no width
but the representation's (2^31 - 128 bits), no meter, and a result past it a `RangeError`. The API
dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/numerics`, revision 8), with its reason in
`scripts/conformance/numerics-divergences.txt`. Corpus revision 9 retires the 54 fields, and
[`docs/evidence/sjl-4-001/engine/retired-divergences-revision-9.tsv`](../docs/evidence/sjl-4-001/engine/retired-divergences-revision-9.tsv)
lists every retired vector under its reason.

- **`1 ** NaN` is NaN, and so are 1 and -1 to an infinity, on every path** (`js-pow-one-nan`,
  `numerics/exponentiate/0128`, `exponentiate-numeric/0128` and `exponentiate-literal/0098` to
  `0100`). The generic `JSValue.Power` lacked the NaN exponent's case, and the literal and compiled
  paths called `Math.Pow`, which gives 1.
- **`Math.cbrt` is correctly rounded** (`js-cbrt-rounding`, 9 vectors, such as `numerics/cbrt/0012`):
  `Math.cbrt(Number.MIN_VALUE)` is 2^-358, an exact cube root, where it was the double below it.
- **`Math.hypot` is within one unit in the last place** (`js-hypot-pairwise`, 5 vectors, such as
  `numerics/hypot/0051`): `Math.hypot(2, 10, 11)` is 15, where it was 15.000000000000002, because the
  arguments were combined in pairs, rounding at each step.
- **`Math.expm1` and `Math.log1p` are within one unit in the last place** (`js-expm1-log1p`,
  `numerics/expm1/0025`, `0042`, `0044` and `0059`, and `numerics/log1p/0057`): `Math.expm1(-745)` is
  -1, where it was -1.00075, and `Math.expm1(709.78)` is finite, where it was Infinity.
- **A BigInt exponent of 2^31 or more is exact** (`js-bigint-power-exponent`,
  `numerics/exponentiate/0349` to `0352` and `numerics/policy/0022`): `1n ** 2n ** 31n` is `1n`,
  `0n ** 2n ** 31n` is `0n` and `(-1n) ** (2n ** 31n)` is `1n`, where each raised a .NET
  `OverflowException`; a result past the representation, `2n ** 2147483648n`, is a `RangeError`.
- **`BigInt.asUintN` of a negative value at a width of 2^32 or more is a `RangeError`**
  (`js-asuintn-wide`, 18 vectors, such as `numerics/asuintn/0211`): the width was cut to 32 bits, so
  `BigInt.asUintN(2 ** 32, -1n)` was `0n`, `BigInt.asUintN(2 ** 32 + 3, -1n)` was `7n`, and other widths
  raised a .NET exception.
- **A BigInt property key beyond 2^32 - 1 or below 0 is the BigInt's decimal text**
  (`js-bigint-key`, `numerics/property-key/0001`, `0002`, `0003`, `0005` and `0007`): `({})[2n ** 40n]`
  and `o[-1n] = 2` raised a .NET `OverflowException`, and `o["4294967295"] = 3; o[4294967295n]` was
  `undefined`.
- **`StringToBigInt` of a `0x`, `0o` or `0b` text is linear in its length** (`js-bigint-hex-parse`,
  `numerics/policy/0018` and `0019`): each digit multiplied the whole value, and a hexadecimal text of
  262,144 digits did not complete within the runner's limit.

**Corrections the corpus does not hold.** Each is a case of the tests named, run against this
repository's `01209c4`, before the cut-over, and after it.

- *The integer conversions* (`SharedIntegerConversionTests`):
  - **Atomics work on the element's bytes**: an operand is converted as the element type converts it,
    so `Atomics.add(new Int32Array([1]), 0, 2 ** 53)` leaves 1, where it left 0, and so do `or` and
    `compareExchange`'s expected value (`2 ** 63` matches 0). The operand was saturated to a 64-bit
    integer and summed through a double. `Atomics.wait` reads its value with `ToInt32`.
  - **`%TypedArray%.prototype.fill` reads its range with `ToIntegerOrInfinity`**:
    `new Uint8Array(3).fill(1, 0, Infinity)` fills every element, where it filled none, and
    `fill(1, 2 ** 32)` fills none, where it filled every one.
  - **A typed array's misaligned offset is a `RangeError` before its length is converted**
    (`InitializeTypedArrayFromArrayBuffer`): the length's `valueOf` ran, and threw, first.
  - **`%TypedArray%.from` passes its constructor `ToLength` of the source's length**: a length of 2^32
    reached the constructor as 2147483647.
  - **A `Date` setter's component is not wrapped modulo 2^32**: `setUTCSeconds(2 ** 32 + 1)` moves the
    date by 4294967297 seconds, where it moved it by 1, and `setUTCMinutes(0, 0, -(2 ** 32))` by
    -2^32 milliseconds, where it did not move it. Dates beyond what the engine's `DateTimeOffset` holds
    (the year 9999) remain invalid; that is the engine's limit, not the conversion's.
  - **`String.prototype.endsWith` takes an undefined position as the length**:
    `'abc'.endsWith('c', undefined)` is `true`, where it was `false`; and `endsWith`, `startsWith` and
    `includes` convert the search string before the position.
- *The Math kernels* (`SharedMathTests`):
  - **`Math.sumPrecise`'s `TypeError` for a value that is not a Number wins over whatever the
    iterator's `return()` throws**, where `return()`'s error escaped; and a count reaching 2^53 is the
    `RangeError` the proposal specifies.
- *The BigInt kernel* (`SharedBigIntTests`, each case at the top level and compiled in a function):
  - **A BigInt's text ignores the culture**: `String(-1n)`, concatenation, template literals,
    `toString()`, `join` and property keys write `"-1"`, where a culture whose negative sign is U+2212
    wrote `"−1"`.
  - **A result past the representation is a `RangeError`**: `1n << 2147483647n`, `1n << 2147483600n`,
    `(-1n) << 2147483646n`, `2n ** (2n ** 64n)`, `BigInt.asUintN(2 ** 31, -1n)` and
    `BigInt.asUintN(2 ** 53 - 1, -5n)` raised an `Error` wrapping a .NET exception, and
    `1n ** 0x10000000000000000n` a .NET `OverflowException`.
  - **`==` between a BigInt and a Number is exact on the compiled path too**:
    `2n ** 53n + 1n == 2 ** 53 + 1` and `2n ** 1024n == 1e400` are `false`, where the compiled
    comparison with a literal went through a double and gave `true`.
  - **`ToBigInt` and `BigInt(value)` convert with the number hint**, where they took the default one:
    `BigInt.asUintN(8, obj)` and `BigInt(obj)`, for an `obj` whose `Symbol.toPrimitive` answers `1n` for
    the number hint and `2n` otherwise, are `1n`, where they were `2n`.
  - **A BigInt element converts with the number hint as well**: a `BigInt64Array` or `BigUint64Array`
    element, `fill`, `DataView`'s `setBigInt64` and `setBigUint64`, and Atomics on a BigInt array convert
    with `ToBigInt`, so `new BigInt64Array([obj])`, for the `obj` above, holds `1n`, where it held `2n`.
    They refused a Number before, and still do.
  - **`new (1n)()` is a `TypeError`**, where it was `0n`.
- *The exact rounding* (`SharedRoundingTests`):
  - **`Intl.NumberFormat`'s scientific and engineering notations round with the rounding mode, and
    decide the exponent from the rounded digits**: with `maximumFractionDigits: 1` and
    `roundingMode: "floor"`, 9.99 in scientific notation is `9.9E0`, where it was `0.9E1`, and 999.99 in
    engineering notation is `999.9E0`, where it was `0.9E3`; with `maximumSignificantDigits: 2`, 9.99 is
    `1E1`, where it was `10E0`. They rounded half away from zero, whatever the mode.
  - **Its compact notation rounds with the rounding mode, and rounds the Number's exact value**, as the
    standard notation already did: 1990 with `"floor"` is `1.9K`, where it was `2K`; 1250 with
    `"halfEven"` is `1.2K`, where it was `1.3K`; -1910 with `"floor"` is `-2K`, and -1990 with `"trunc"`
    is `-1.9K`, where they were `-1.9K` and `-2K`; and 1.15, whose exact value is below 1.15, is `1.1`,
    where it was `1.2`.
  - **`Temporal.Duration.prototype.round` rounds a calendar unit within its increment's window**
    (`NudgeToCalendarUnit`): `Temporal.Duration.from("P1M").round({ smallestUnit: "months",
    largestUnit: "months", roundingIncrement: 2, roundingMode: "halfTrunc", relativeTo: "2023-01-01" })`
    is `P2M`, where it was `PT0S`.
  - **`Temporal.Duration.prototype.total` rounds once**: five years and 26368.123456789 seconds,
    relative to a `ZonedDateTime` in UTC, total 5.000836127709817 years, where a sum of doubles gave
    5.0008361277098174.
  - `Duration.prototype.round`'s `RangeError` for an unknown rounding mode reads "Temporal: invalid
    roundingMode", as the other types' do.

- *The copies the search for survivors found* (2026-10-14; `SharedNumericsSurvivorsTests`, run
  against `728e958`, the cut-over before the search, and after it). Built-ins outside the correspondence's list converted
  their arguments by hand; each now calls the library, and these results change:
  - **`Function.prototype.apply`, `Reflect.apply` and `Reflect.construct` build their list with
    `CreateListFromArrayLike`**: `LengthOfArrayLike`, then `Get` of each index. A length of 2^31 or
    more is a `RangeError`, where the call got no arguments, or a .NET `OverflowException` escaped for
    an array; an array-like whose `length` is inherited, a `Proxy`, and a `String` object give their
    elements, where the call got none (a `Proxy` of an array raised a .NET `OverflowException`); and a
    hole is read through the prototype, so with `Array.prototype[1] = "p"` the hole at 1 is `"p"`,
    where it was `undefined`.
  - **`String.raw` reads its `raw` object with `LengthOfArrayLike`**: `String.raw({ raw: "abc" }, 1, 2)`
    is `"a1b2c"`, where it was `""`, and a `raw` of length 2^32 is a `RangeError`.
  - **`RegExp.prototype[Symbol.match]` advances an empty match from `ToLength(lastIndex)`**: a
    `lastIndex` of -5 becomes 1, where it became -4, and one of 2^31 becomes 2147483649, where it
    became -2147483648; with the `u` flag, a negative `lastIndex` raised a .NET
    `IndexOutOfRangeException`.
  - **`String.prototype.padStart` and `padEnd` convert their length with `ToLength`**, and a result
    too long for the engine is a `RangeError`: `"a".padStart(2 ** 32 + 3, "-")` raised an `Error`
    wrapping a .NET `OutOfMemoryException`.
  - **`%TypedArray%.prototype.set` reads an array-like source's length with `ToLength`**:
    `new Uint8Array(2).set({ length: -5 }, 3)` is a `RangeError`, an offset past the end, where the
    negative length let it through.
  - **The Set methods' `GetSetRecord` truncates `size` before its sign test**: a set-like object of
    size -0.5 is of size 0, where it was a `RangeError`.
  - **`TimeClip` is `ToIntegerOrInfinity`**: `new Date(0).setTime(-0.5)` is +0, where it was -0.
  - **`MakeTime` and `MakeDay` apply `ToIntegerOrInfinity` to each component**: `setUTCHours(1.5)`
    sets one hour, where it set an hour and a half; `setUTCDate(1.5)` and `setDate(1.5)` keep the
    first day, where they added twelve hours; and `new Date(1000).setUTCMilliseconds(-0.5)` is 1000,
    where it was 999.
  - **`Intl.NumberFormat`'s scientific and engineering notations take the mantissa and exponent from
    the Number's exact value**, where they divided by a power of ten in doubles: with 20 fraction
    digits, `1e-30` is `1.00000000000000008334E-30`, where it was `1E-30`, `9.999999999999999e22` is
    `9.99999999999999916114E22`, where it was `0.99999999999999977796E23`, and `5e-324` is
    `4.94065645841246544177E-324`, where it was `0.5E-323`. The default precision's results are
    unchanged.
  - **A subnormal Number's exact value is right in every notation**: it was scaled by 2^-1075 rather
    than 2^-1074, so its digits were those of half the value (`5e-324` to three significant digits
    ended in 247, where it ends in 494).
  - **A String's Intl mathematical value trims the specification's white space**: with 20 fraction
    digits, `format("\uFEFF0.1")` is `0.1`, where U+FEFF stopped the decimal reading and the text was
    read as the Number 0.1 (`0.10000000000000000555`).
- *The typed arrays' copies of the same element type* (2026-10-14; `SharedIntegerConversionTests`, run
  against `b598028`, SJL-4's base, and after `bc2d663`):
  - **`slice`, `copyWithin` and the constructor given a typed array keep a `Float32Array`'s signalling
    NaN**: `0x7F800001` stays `0x7F800001`, where it was quieted to `0x7FC00001`, because each element
    went through a double. They move bytes, as the specification asks, and `set` already did. test262's
    `staging/sm/TypedArray/slice-bitwise-same.js`, which failed at the base, passes.

**Implementation-defined, and changed.** `Math.f16round(NaN)` and a NaN read from a `Float16Array` or
with `DataView.prototype.getFloat16` are the platform's quiet NaN, where they kept the binary16 value's
sign and payload in the double; no result but the bits written to another buffer shows it. A NaN
written to a binary16 element still keeps its sign and the high bits of its payload, as this plan
committed this engine to. A copy between typed arrays of the same element type moves the bytes, as the
specification asks, so it keeps each element's encoding: `slice`, `copyWithin`, `set`, and the
constructor given a typed array. *(Added 2026-10-14. The cut-over had made `slice`, `copyWithin` and
that constructor copy through the value, so a binary16 NaN came out as the platform's, its sign set
and its payload dropped; test262's `TypedArray/prototype/slice/bit-precision.js` found it in bundle
sjl-4-001's first after run. `SharedIntegerConversionTests` holds the cases.)*

**Not corrected here.** `Temporal.PlainTime`, `PlainDateTime` and `ZonedDateTime`'s `round` round the
nanoseconds since midnight rather than the unit the specification names (`01:10` rounded to 20 minutes
with `"halfEven"` is `01:20`, where the specification gives `01:00`); that is the Temporal consumer's
quantity, which item SJL-10 answers, as it answers Temporal's other quantities. `Intl.NumberFormat`'s
mathematical value is item SJL-9's.

### Number conversion moves to `Broiler.ECMAScript.Algorithms` (2026-10-12, milestone SJL-3.5)

`Broiler.JavaScript.Parser`, `.Runtime`, `.BuiltIns` and `.Storage` reference the shared library,
and every number conversion is its: `Number::toString` at every radix, `toFixed`, `toExponential`
and `toPrecision`, `StringToNumber`, `parseInt` and `parseFloat`, the values of numeric literals,
array-index and canonical numeric keys, and the white-space set of `String.prototype.trim` and the
parsers. The built-ins keep their coercions, their RangeErrors and their string creation. The API
dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/numbers`, revision 4), with its reason in
`scripts/conformance/number-divergences.txt`; corpus revision 5 retires the fields, and
[`docs/evidence/sjl-3-001/engine/retired-divergences.tsv`](../docs/evidence/sjl-3-001/engine/retired-divergences.tsv)
lists every retired vector under its reason. The argument handling the corpus does not hold is
covered by `Broiler.JavaScript.BuiltIns.Tests/SharedNumberConversionTests.cs`.

- **`Number.prototype.toString(radix)` at a radix other than 10 gives the shortest digits that read
  back as the value** (reason `radix-longer`, 233 vectors of `numbers/to-string-radix`, such as
  `0051`). The fraction was expanded digit by digit, up to 1,100 digits; the digit cap is gone with
  it.
- **Its digits denote the value** (`radix-inexact`, 124 vectors, such as `numbers/to-string-radix/0065`).
  The fraction was expanded in double arithmetic, whose rounding error entered the digits.
- **Its integer part is exact at 2^63 and beyond** (`radix-64-bit`, 238 vectors, such as
  `numbers/to-string-radix/0015`). It was converted through a 64-bit integer.
- **A radix whose `ToIntegerOrInfinity` is 0 is a RangeError: `0`, `-0`, `null` and `NaN`**
  (`radix-argument`, `numbers/to-string-radix-arguments/0002`, `0005`, `0006` and `0010`). The first
  three were read as radix 10, and `NaN` escaped as a host `ArgumentException`.
- **`toPrecision` coerces its argument before it looks at the value, and checks the truncated
  precision** (`precision-argument`, `numbers/to-precision-arguments/0003`): `(1.5).toPrecision(100.5)`
  is 100 digits instead of a RangeError, and `Infinity.toPrecision(x)` now calls `x`'s `valueOf`, as
  step 3 does before step 4.
- **`parseInt(null, radix)` and `parseInt(undefined, radix)` parse `"null"` and `"undefined"`**:
  `parseInt(null, 36)` is 1112745 instead of NaN (ECMA-262 §19.2.5 step 1;
  `SharedNumberConversionTests`). The radix is converted once, where it was converted twice.
- **The white-space set is the specification's**, generated from the pinned Unicode 17.0.0 data:
  `parseInt` no longer trims U+0085 NEXT LINE, which is not white space (`next-line-white-space`,
  `numbers/parse-int/0064` to `0070`); it trimmed with .NET's `string.Trim`. U+FEFF was, and is,
  trimmed by every parser and by `String.prototype.trim`, which read the set from a list of its own
  before. `BigInt(string)` and the comparisons of a BigInt with a string (StringToBigInt) trimmed
  .NET's set: they trim U+FEFF now, so `BigInt("﻿1")` is `1n` instead of a SyntaxError, and no
  longer trim U+0085 (`SharedNumberConversionTests`).
- **Hexadecimal, octal and binary integers beyond 2^53 are rounded to the nearest double, ties to
  even**, as literals and as strings (`non-decimal-rounding`, 62 vectors of `numbers/literal` and
  `numbers/string-to-number`, such as `numbers/literal/0213`). They were rounded twice.
- **`Number()` and `parseFloat` complete for a decimal significand of more than 309 digits**
  (`long-significand-hang`, 6 vectors, such as `numbers/parse-float/0777`), **and give its value, not
  NaN, below 1e-308** (`long-significand-nan`, 12 vectors, such as `numbers/parse-float/0918`).
- **Numeric literals the grammar refuses are SyntaxErrors**: `07.5` (`legacy-octal-fraction`,
  `numbers/literal/0091`), `1e` (`empty-exponent`, `0105` and `0106`), `01n` and `08n`
  (`bigint-leading-zero`, `0137` to `0140`) and `.5n` (`bigint-fraction`, `0145` and `0146`). The
  scanner's digit runs let them through and the value code read them; the scanner now takes each
  literal's value from the shared library, which refuses them.
- **Canonical numeric keys are `Number::toString`'s.** `"1E+21"` and `"1E-07"`, the .NET `"R"`
  format's spellings, were classified as canonical numeric strings, so a typed array ignored them as
  keys; they are ordinary property keys now (`SharedNumberConversionTests`).

- **A Number equals a String through ToNumber only.** Three fast paths of `==` compared .NET's text of
  the number with the string, so `NaN == "NaN"` and `"NaN" == NaN` were `true`; they are `false`
  (ECMA-262 §7.2.14; `SharedNumberConversionTests`). The search for surviving duplicates of the area
  (bundle sjl-3-001, milestone SJL-3.7) found them.
- **The portable compiler's generated C# holds every number constant exactly.**
  `PortableCompiler.EmitCSharp` wrote a constant in .NET's `"R"` format, which does not read back as
  the same double for every value: `2.9802322387695312e-8` (2\*\*-25) was written
  `2.980232238769531E-08`, the double below it, and an infinite one, such as the literal `1e400`, was
  written `Infinity`, which C# does not compile. Each is written as its IEEE-754 bits now
  (`PortableCompilerTests`). The same search found it.

**Not corrected here.** A `NonOctalDecimalIntegerLiteral` (`08`, `09.5`) in strict mode code is still
accepted (`strict-non-octal-decimal`, `numbers/literal/0082`, `0084`, `0088` and `0090`): strictness
is the compiler's, and item SJL-14 answers it.

**Kept, as decision SJL-D-17 has them:** the `m` (`Decimal`) literal suffix, `Number.prototype.clz`,
and `toLocaleString`'s .NET format strings.
