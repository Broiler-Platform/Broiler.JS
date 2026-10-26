# The shared Intl library and its locale data: design

[← Shared libraries](../../README.md) · [Conventions](../conventions.md) ·
[This repository's plan](../../../docs/roadmap/SharedLibraries.md)

**Library:** `Broiler.ECMAScript.Intl`, which the programme's milestone SJL-9.3 creates, over the locale data whose
home, `Broiler.ECMAScript/data/intl/`, and generator, in `Broiler.ECMAScript.Generation`, SJL-9.2 creates.
**Status:** design. **Filed:** 2026-10-26, at milestone SJL-9 (work package SJL-9.1), before any of the library's code
or the home's data exists. **Owner:** the Broiler.JS maintainer, with the JavaScript profile owner. One person holds
both roles (Broiler.VM's exception EX-30), and nothing here has been read by anyone but its author.

**What this document is.** The Intl library's design document in its home, one of the three places SJL-9.1 files the
area: Broiler.VM's JSD-0082, this repository's plan ([`SharedLibraries.md`](../../../docs/roadmap/SharedLibraries.md),
"Area verdicts", "Locale data and the Intl algorithms (SJL-9.1)"), and here. It records the provider contract, the
table format and its versioning, the generator's parameters and the host provider's scope, and decides nothing itself.

**What wins.** The programme's area design, `Broiler.VM/docs/javascript-shared-libraries/areas/locale-data-and-intl.md`,
and Broiler.VM's JSD-0082,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0082-locale-data-and-the-intl-algorithms-move-to-the-shared-libraries.md`,
are the area's records. **Where this document and they differ, they win, and this document is corrected to match**,
by a dated note that quotes what it replaces. A decision named "decision Dn" below is JSD-0082's. Decisions D1 to D31
are SJL-9's plan's, and D32 to D35, with the revisions of D7, D14, D15, D17 to D19, D21 and D24 to D26, answer the
findings of SJL-9.1's correspondence. Each is the owner's decision of 2026-10-26, taken under the owner's direction of
2026-10-18, "Please do all recommended decisions and skip waiting for review.", which changes no rule of the programme.

**Why there is no project yet.** SJL-9.1 writes no code. `Broiler.ECMAScript/` has no `Broiler.ECMAScript.Intl/`
folder and no `data/intl/`: a project would bring the library under [the conventions](../conventions.md)' scope and
rule SL1's layering table before it exists. SJL-9.2 creates the data home and the generator, and SJL-9.3 the library,
one stage at a time.

## 1. The two layers of the provider contract

Decision D8. The library reads locale data in two layers.

**Tables.** An engine hands the library its embedded tables as an `IIntlDataSet`, the shape of Broiler.VM's
`IJsIntlData` today:

```csharp
namespace Broiler.ECMAScript.Intl;

public interface IIntlDataSet
{
    string CldrVersion { get; }                  // the CLDR release the tables were generated from: 48.2.0
    ReadOnlySpan<byte> Table(IntlTable table);   // one table's bytes; empty for a table this data does not carry
}
```

- **`IntlTable` keeps `JsIntlTable`'s numbers.** Numbers 7, 23 and 24, which SJL-7.4 retired from Broiler.VM's
  enumeration, stay retired and are not reused. A new value is appended only for a table Broiler.VM's data never
  carries, and each engine answers an empty span for a table it lacks.
- **An implementation answers the same bytes for the life of the process.** The library decodes each table once per
  process, lazily and thread-safely (decision D23's L3 tests), with the decoders Broiler.VM's `JsIntlTables` holds
  today (its readers, `Unescape`, `Lines`, `Pairs`).
- **Broiler.VM keeps `IJsIntlData` and `JsIntlTable` public** in its Format assembly and adapts them to the shared
  interface internally, so no shared type enters its public API (its rule I6).

**Questions.** The library decodes the tables into a question-level `ILocaleData`: a locale's number symbols for a
numbering system, its plural rules, its patterns, its names, its week. Its decoder implementation over an
`IIntlDataSet` is the library's, and Broiler.JS's host-culture provider implements the same interface (section 5).
`CompositeLocaleData` orders the providers: the shared data first, then an optional secondary provider, then the
locale's parent and root.

**The algorithms.** `Broiler.ECMAScript.Intl` references `Broiler.ECMAScript.Algorithms` and
`Broiler.Unicode.Algorithms` (decision D9). It is organised by sub-area, in the order the sub-areas are built
(decision D3):
- **a.** `LocaleId` (parsing, canonicalisation, aliases, likely subtags, maximise and minimise) and
  `LocaleNegotiation` (CanonicalizeLocaleList's string part, lookup, filtering, and ResolveLocale over the default and
  available locales an engine gives it); Locale info; `supportedValuesOf`;
- **b.** `IntlDecimal`, the Intl mathematical value from `Number::toString`'s digits and a BigInt's; the digit
  options; FormatNumericToString; `PluralEngine`, with the operands `c` and `e` (decision D20) and plural ranges;
- **c.** `NumberFormatter` (patterns, compact notation, currencies, units, ranges and parts), `ListFormatter`,
  `RelativeTimeFormatter`, `DisplayNamesCore`;
- **d.** `DateTimePatternGenerator`, `DateTimeFormatter`, `DateIntervalFormatter`, day periods, hour cycles and
  `ZoneNamer`, with metazone history, over SJL-8's time zone rules and SJL-10's calendars, and Intl over Temporal;
- **e.** `DurationFormatCore`, over the number and list formatters.

Each takes **resolved options as records**, never an options object, and answers strings, parts records or a status
(conventions 2 and 3). Its code takes no culture call: what Broiler.VM's code does today through `ToUpperInvariant`,
`ToLowerInvariant`, `CultureInfo` or a culture-sensitive overload, the library does by ASCII casing or with
`NumberFormatInfo.InvariantInfo` (decision D19, convention 9). SJL-9.3 settles the names.

## 2. What each engine hands the library

### The default and available locales

Decision D7, as revised. The providers stay each engine's (decision SJL-D-19), and the library's ResolveLocale reads
the default locale and the available locales they give.
- **Broiler.VM:** `en-US`; its data's locales; an unknown locale resolves to the default.
- **Broiler.JS:** the host's `CurrentCulture`, `en-US` when it is empty or invariant; the host's cultures.
- **The default is available.** In both engines the available locales always include the default locale and the less
  narrow tags the same clause requires for it: the pinned text says "[[AvailableLocales]] ... must include the value
  returned by DefaultLocale", and test262's `intl402/default-locale-is-supported.js` asserts it. Under invariant
  globalization Broiler.JS's available locales are therefore `en-US` and `en`, where today they are none (row L44, a
  correction of Broiler.JS). In Broiler.JS every service's available locales come from the available-locale provider
  as revised at SJL-9.5, `Intl.Collator`'s included: the Collator's data and comparison stay the host's until SJL-17
  (section 5), but its negotiation is the engine's shared one, and `default-locale-is-supported.js` tests the Collator
  first.
- **One default.** Every reader of DefaultLocale in an engine reads the same default-locale provider: the Intl
  services, the `toLocale*` methods, and TransformCase with no locale (row L35). In Broiler.JS, under a host culture,
  `toLocaleLowerCase()` and `toLocaleUpperCase()` with no locale case by that culture's language, as its Intl services
  resolve it. SJL-7.5's C8 is read as "casing reads the host's culture only through the default-locale provider", and
  its refusal of a `null` locale stands.

### The meter

Decision D18, as revised. Every library path whose work grows with its input takes the caller's `WorkCharge`
(convention 4): tag parsing and canonicalisation, plural evaluation over digits, the pattern search over skeletons,
list and display-name composition, and number, date and duration formatting. **The library charges where Broiler.VM
charges today, in its units**, so that Broiler.VM's fuel verdicts do not move (no C2):

| Where the library charges | Unit |
|---|---|
| The mathematical value of a String | `length / 8 + 1` |
| The mathematical value of a BigInt | through its decimal digits |
| FormatNumericToString | `digits / 16 + 1` |
| CreatePartsFromList | one per template step |
| The date formatter | its output's length |
| PartitionDurationFormatPattern | one per unit visited, plus the charges of the number and list formats it builds |

Broiler.VM's adapters keep the engine-side charges: one per element where they walk a script value
(CanonicalizeLocaleList, FilterLocales, StringListFromIterable), and one per part where they build a `formatToParts`
array. Tag parsing, canonicalisation and likely subtags charge nothing in Broiler.VM today (row L43), so its
`WorkCharge` charges nothing there. **Broiler.JS charges nothing.** JSD-0082's decision D18 names each of Broiler.VM's
charge points by file and line, and the correspondence's fuel bisections of rows M9, N16, T40 and U19 are SJL-9.4's
references.

### The time zone

Decision D15, as revised. The formatter's resolved time zone is the identifier as given, case-normalised, as
`tc39/proposal-temporal@e8cc03fc`'s amendment of CreateDateTimeFormat has it. **The shared time-zone resolution answers
both the identifier and its primary.** The engine's adapter reports the identifier as [[TimeZone]], and the library
names the zone (its metazone, and its generic, specific and location names) by the primary. Broiler.VM's resolved
identifier moves at SJL-9.4 (row Z8), and no zone name moves in it; Broiler.JS's resolved identifier does not move,
and its names for an identifier whose CLDR names differ from its primary's, such as `Etc/GMT`, move to the primary's
(row T26, implementation-defined).

### A locale's collations

The library's CollationsOfLocale follows the pinned text over each engine's `Intl.Collator` data, which stays each
engine's until SJL-17. For a language the engine's Collator does not support it answers DefaultLocale's collations,
and `emoji` and `eor` only for `und`; Broiler.VM's answer moves there at SJL-9.4 (row L18, decision D32). Broiler.JS's
adapter is to hand the operation its Collator's own list, which names a collation for `en`. test262's
`Locale/prototype/getCollations/output-array-values.js`, which asks `en` for one, keeps passing only if it does (row
L17, decision D33 (c)): SJL-9.5's local pre-check holds it, and SJL-17 inherits the condition. With that list,
Broiler.JS's `zh` and `ko` answers are its Collator's, which neither ICU's lists nor Broiler.VM's datasets hold, and
part C of bundle `sjl-9-001`'s rule admits them by that list (class V).

### Options and arguments

Option reading stays in the engines, in the specification's order (the milestone's work package SJL-9.3), and the
library takes the resolved record. Broiler.VM's adapter keeps reading DurationFormat's argument, ECMA-402's own
ToDurationRecord included, and its options through the profile's shared GetOptionsObject; the library's duration core
takes a duration record and reads no argument (decision D24).

## 3. The table format and its versioning

**The format is Broiler.VM's**, which its `JsIntlTable` documents today. The text tables are ASCII, one record per line
and fields separated by `|`, in the order the generator writes them; a character a line cannot carry is written
`\uXXXX`. The collation tables are binary, and their reader states their layout; they are SJL-17's. The keyed tables
(`NumberLocales`, `DateLocales`) hold a language, a dotted key and a value per line.

**Each engine embeds the same format**, from the same generator (decision SJL-D-6 (c)):
- **Broiler.VM** embeds one generated source, `JsCldrTables.g.cs`, in its optional `.Intl` assembly. The home's
  generator, with Broiler.VM's options, writes it byte for byte, which Broiler.VM's rules N27 and N28 hold, and N28
  holds it under its budget.
- **Broiler.JS** embeds one file per table from `data/intl/generated/`, with `tables.manifest` (the versions, and each
  table's length and SHA-256), as resources in `Broiler.JavaScript.BuiltIns`, linked from the home and never copied,
  as the time tables are (decision D11).

**Versioning.** The format is documented and versioned by the data home. A change to it breaks the library's provider
contract, so it is a **major version** of `Broiler.ECMAScript.Intl`, and each engine regenerates its embedded tables in
the change that adopts it (the area design's section 6). Appending an `IntlTable` value for a table Broiler.VM's data
never carries keeps the format, since an engine that lacks a table answers an empty span (decision D8).

**The data versions are public** (exit clause 2, convention 13): `IIntlDataSet.CldrVersion`, and the generator's
`IntlDataVersions`, the CLDR, UCA and Unicode versions it writes from. The data's notices travel with the tables
(convention 19): CLDR's licence beside the pin.

## 4. The data home and the generator's parameters

**The pins** (decision D10). `data/intl/cldr/cldr.pin` is a **superset** of Broiler.VM's `src/tests/cldr/pins/cldr.pin`:
its entries byte-identical in path, length and SHA-256, with its `version`, `uca-version` and `json-integrity` lines,
plus Broiler.JS's extra main-data files from the same packages, each tarball checked against its integrity. The
archived files and `cldr-LICENSE.txt` sit beside it. `data/time/cldr/` is unchanged, and a home test holds its
`timezone.json` entry and copy equal to the Intl pin's. Broiler.VM's rule I4 holds its CLDR pin to the home's **entry
by entry**, read at a recorded commit of this repository, and the generator declares CLDR 48.2.0.

**The UCD** (decision D12). The generator reads General_Category, decompositions, the canonical combining classes and
Unified_Ideograph through a ported subset of Broiler.VM's `UnicodeDatabase`. The UCD stays an explicit, pinned input:
Broiler.VM hands its own pinned files; the home hands `data/intl/ucd/`'s `UnicodeData.txt` and `PropList.txt`, held
equal to Broiler.Unicode's `UnicodePin.Owner()` entries. The pin reader keeps Broiler.VM's check that ties the UCA
version to the Unicode pin. The fallback is Broiler.Unicode's own Generation reader, made public.

**The generator** (decision D11) moves whole, collation included, into `Broiler.ECMAScript.Generation/Intl/`:
`CldrTableGenerator`, `CldrUcd`, `IntlCldrPin`, `IntlTableSource` (C# with `TableSourceOptions`, as SJL-8's writers),
`IntlTableFiles` (one raw file per table plus `tables.manifest`) and `IntlDataVersions`. **Its parameters:**
- **the main-data locales**, whose number, currency, unit, date, list, relative-time and display-name data it writes;
- **the supplemental locales**, whose plural, ordinal, plural-range and day-period rules it writes;
- **the numbering systems**, whose digits, and for Broiler.JS whose symbols, it writes;
- **the calendars** and **the tailorings**.

Today's hard-coded list (Broiler.VM's `CldrTableGenerator.cs:33`) becomes a parameter.

**The two lists** (decision D5), `data/intl/locales/broiler-vm.txt` and `broiler-js.txt`:
- **Broiler.VM's** stays `de`, `de-DE`, `en`, `en-US` and `und`, so that its table does not change under N28;
- **Broiler.JS's** main data is the union of the languages its CLDR package serves today, every locale the intl402
  files passing at its base and the reference datasets name, `en-US` and `und`; its supplemental tables cover every
  CLDR language, and its global tables are Broiler.VM's. This repository's plan gives the list.

Every later addition to either list is a dated data decision with its measured size. Sizes are measured by the
generator and stated in the evidence bundle, never in a record.

**Numbering systems** (decisions D21, as revised, and D13). Broiler.VM's tables keep the `latn` symbols for every
numbering system, a limitation recorded under N28's budget. **Broiler.JS's embedding carries each numbering system's
own CLDR 48.2.0 symbols**, for every locale of its list, through the locale's parent chain to root, and the locale's
`latn` symbols where CLDR has none. Root holds `arab`'s and `arabext`'s in `common/main/root.xml`, which Broiler.VM's
pin already archives, and the `cldr-json` package's `und/numbers.json` carries only the `latn` ones, so the generator
reads root's from that file. Every formatter writes numbers from that one table, so `en-u-nu-arab` writes U+066B and
U+066C, and U+061C before the minus sign. Whether those symbols are appended keys of the number table or a table of
their own is SJL-9.2's; the plan's decision D21 allowed either. Each locale's default numbering system comes from the
same CLDR 48.2.0 data, which replaces this engine's CLDR 47.0.0 table, `JSIntlDefaultNumberingSystems.g.cs`, and its
script. *(Added 2026-10-26, at SJL-9.2b: a `cldr-json` `numbers.json` carries the symbols of a locale's default,
native, traditional and finance systems only, so the generator also reads the other systems' symbols from each
locale's `common/main` XML and its parent chain's, which the home's CLDR pin archives beside `root.xml`; the symbols
are appended keys of the number table, `symbols@<system>.<name>`.)*

**DurationFormat's digital format** (decision D34). For each locale the generator writes `symbols.timeSeparator` and
the `durationUnit-type-hm`, `-hms` and `-ms` patterns into the number table. [[TwoDigitHours]] is true where the `hms`
pattern begins `hh`, as Broiler.VM reads it.

**How the home is held:**
- **Rule SL7** regenerates every generated file from the pins and compares it byte for byte, in both directions, with
  its witness (one byte changed);
- **byte identity with Broiler.VM's table**: with Broiler.VM's options, over the home's copy of Broiler.VM's pinned
  files and the UCD copies, the writer's text equals Broiler.VM's `JsCldrTables.g.cs` before its assurance pass, held
  by a fixture at a named Broiler.VM commit and digest;
- **the completeness test** of exit clause 3: every locale of each list has every table its group carries, with
  decision D34's keys for every locale of Broiler.JS's list, so that none falls back to `:` and one-digit hours for
  want of a key;
- **determinism** (rule SL9): two runs, and two threads, write the same bytes.

## 5. The host provider's scope

Decisions SJL-D-22 (c) and D6. For locales the shared data lacks, Broiler.JS keeps an **optional provider backed by
host cultures**, `HostCultureLocaleData` in `Broiler.JavaScript.BuiltIns`' `Intl/`. It implements `ILocaleData` and is
the composite's secondary provider. **It is never a second implementation of the algorithms.**
- **It answers only what .NET exposes exactly**: number symbols and native digits; month, day and era names; AM and PM;
  the first day of the week. It answers only for a locale the shared data lacks.
- **Every other question answers "no data"**, so the algorithm uses the parent or root: .NET has no plural, compact,
  unit, list, relative-time, display-name, interval or day-period data.
- **It never throws for want of host data.** Under `InvariantGlobalization`, which Native AOT applications commonly set,
  `CultureInfo.GetCultureInfo` throws for a named culture, so the provider checks availability first and reports the
  locale unavailable, and negotiation falls back as for any locale the data lacks. Part D of bundle `sjl-9-001`'s rule
  ([`decision-rule.md`](../../../docs/evidence/sjl-9-001/decision-rule.md)) runs the reference datasets and
  `default-locale-is-supported.js` under invariant globalization.
- **It is not the default-locale or available-locale provider**, which section 2 describes and decision D7 keeps.
- **Broiler.VM registers none.**

Collation keeps its host path in Broiler.JS until SJL-17: the Collator's data and comparison, not its default and
available locales, which are section 2's.

## 6. What each engine keeps

| Broiler.VM keeps | Broiler.JS keeps |
|---|---|
| Its Intl objects, prototypes and `NewTarget` handling | Its Intl objects and public `JSIntl*` types, with every public member (SJL-D-25 (a), decision D24) |
| Option reading in the specification's order; DurationFormat's argument and options | Option reading in the specification's order |
| The legacy constructor chaining and unwrapping (rows N12 and T5) | The same, normative-optional (rows N12 and T5) |
| Bound `format` functions and their caching; parts arrays | The same, with the bound `format` identity corrected (rows N11 and T4) |
| Its default-locale and available-locale providers | Its default-locale and available-locale providers, over the host's cultures |
| The meter and its engine-side charges | A `WorkCharge` that charges nothing (decision D18) |
| `IJsIntlData`, `JsIntlTable` and the `.Intl` assembly's optionality | The host-culture provider (section 5) |
| Surface and composition gating | `Intl.Collator` and `localeCompare` on the host until SJL-17 |

## 7. The evidence

- **The pins of the text** (decision D14, as revised): ECMA-402 at `tc39/ecma402`
  `3d4c558180f643d7dea75f011a6355fb285f377e`, the last `main` commit on or before 2026-05-08, which already holds the
  Intl Locale Info proposal and names IsWellFormedLanguageTag; the Intl era and month code proposal at its last commit
  on or before the same date; and Intl over Temporal by `tc39/proposal-temporal@e8cc03fc`. SJL-9.2 records them in
  `conformance/vectors/sources/ecma402.pin`. Where a test262 file both engines pass asks for an answer the text does
  not give (rows S7 and T8), the file is the arbiter, and the library keeps both engines' answer (decision D33).
- **The vectors** (decision D17, as revised), `conformance/vectors/intl/`, one file per sub-area, from ICU through the
  Node.js release bundle `sjl-9-001`'s [`reference.txt`](../../../docs/evidence/sjl-9-001/reference.txt) names, each
  divergence argued against a clause of the text.
- **The reference datasets** (decision D16), Broiler.VM's, copied into `conformance/datasets/intl/` at SJL-9.2 with
  their divergences, and classed by bundle `sjl-9-001`'s
  [`divergence-groups.tsv`](../../../docs/evidence/sjl-9-001/divergence-groups.tsv).
- **The differential check** (decision D22): Broiler.VM's code at its rule commit, beside the library, test-only and
  JIT only, over every Intl vector, every dataset input as a library call, and seeded random tags, numbers, dates and
  durations. It gates each library stage.
- **The rules**, each committed before any change it judges: this repository's
  [`docs/evidence/sjl-9-001/decision-rule.md`](../../../docs/evidence/sjl-9-001/decision-rule.md), and Broiler.VM's
  `docs/evidence/sjl-9-001/decision-rule-area.md`.

## 8. What this document does not decide

- **The library's names, types and API** beyond the plan's, which SJL-9.3 settles under the conventions.
- **The table sizes**, which are SJL-9.2's measurement.
- **Collation**, which is SJL-17's.
- **Making Intl code optional** in either engine, which is a later decision (the milestone's "Does not do").
- **Either engine's default-locale and available-locale policy** beyond decision D7's two readings.

## Corrections

None.
