# Broiler.ECMAScript — the shared JavaScript libraries

Libraries that implement parts of the ECMAScript specification **once**, for both Broiler
JavaScript engines: Broiler.JS, the reference engine in this repository, and Broiler.VM's
JavaScript profile. They name neither engine and reference neither. Each engine calls them through
an adapter of its own, and each keeps its own values, objects, execution and resource policy.

The programme that creates them is owned in the Broiler.VM repository, under
`Broiler.VM/docs/javascript-shared-libraries/`. This repository's half of it is
[`docs/roadmap/SharedLibraries.md`](../docs/roadmap/SharedLibraries.md).

## Packages

| Package | Holds | State |
|---|---|---|
| `Broiler.ECMAScript.Algorithms` | The language algorithms: number conversion, numerics, text, Date, the Temporal core, the time-zone algorithms. References the base class library only, and carries no data but the white-space set's generated `Space_Separator` table. | The contract types (`WorkCharge`, `ResultStatus`, `Failure`, `LengthLimit`, `DepthLimit`), **the number-conversion area** of milestone SJL-3 (`Broiler.ECMAScript.Numbers`; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numbers.md)), **the numerics area** of milestone SJL-4 (the same namespace; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numerics.md)) , **the text area** of milestone SJL-5 (`Broiler.ECMAScript.Text`; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-text.md)), **the Date area** of milestone SJL-6 (`Broiler.ECMAScript.Time`; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-date.md)), **the time-zone and calendar area** of milestone SJL-8, the decoder of the data home's tables over the bytes an engine hands in, the zone algorithms and the table-backed calendars' years (the same namespace; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-time-zones.md)) and **the Temporal core** of milestone SJL-10.3: its records and limits, balancing, grammar and serialization (part A), its calendars, the identifiers, the formula calendars and the table calendars over those years, month codes, eras, reference years, and calendar addition and difference (part B), and its durations, rounding and differences, with and without `relativeTo`, over calendar and time-zone providers (part C) (`Broiler.ECMAScript.Temporal`; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-temporal.md)). |
| `Broiler.ECMAScript.Conformance` | The shared conformance vectors' reader, validator and runners. Test-only. | Not packed at this version; the corpus itself is [`conformance/vectors/`](../conformance/vectors/README.md). |
| `Broiler.ECMAScript.Generation` | The data homes' generators: the time data home's - the tz, CLDR and calendar pin readers, a tz compiler equivalent to `zic`, and the compact time zone and calendar tables - and the Intl data home's - the CLDR and UCD pin readers, the locale lists, and the Intl and collation tables - with writers that wrap the tables into C# or raw files. Test-only, as Broiler.Unicode.Generation is: engines' tests run it, and it carries no table. | **The time data home** of milestone SJL-8.2 (`Broiler.ECMAScript.Generation.Time`), ported from Broiler.VM's architecture tests; its inputs and tables are [`data/time/`](data/time/README.md). **The Intl data home** of milestone SJL-9.2b (`Broiler.ECMAScript.Generation.Intl`), ported from Broiler.VM's architecture tests: with Broiler.VM's parameters it writes Broiler.VM's table byte for byte before Broiler.VM's assurance pass; its inputs, each embedding's locale list and Broiler.JS's tables are [`data/intl/`](data/intl/README.md). Published since `0.1.0-preview.5`; the Intl namespace is not yet published. |
| `Broiler.ECMAScript.Intl` | The Intl algorithms (ECMA-402) over the locale data an engine hands in, ported from Broiler.VM's JavaScript profile one sub-area at a time (Broiler.VM's JSD-0082, decision D3). References `Broiler.ECMAScript.Algorithms` and `Broiler.Unicode.Algorithms` (decision D9), and carries no table: each engine embeds its own and hands it in. | **The skeleton** of milestone SJL-9.3a: the data contract of decision D8 - the tables as an `IIntlDataSet` (`IntlTable` keeps Broiler.VM's `JsIntlTable` numbers), decoded once into the question-level `ILocaleData` by `TableLocaleData`, with `CompositeLocaleData` ordering the shared data, an optional secondary provider, and the locale's parent and root - and `IntlStatus`. **Locale identifiers**, sub-area (a) of milestone SJL-9.3a: `LocaleId` (well-formedness, canonicalisation, aliases, likely subtags), `LocaleNegotiation` (CanonicalizeLocaleList's steps, lookup, best fit, filter and ResolveLocale over the default and available locales an engine gives), `LocaleRecord` (the `Intl.Locale` constructor's steps), `LocaleInfo` (calendars, collations, hour cycles, numbering systems, time zones, text direction and week info) and `SupportedValues` (`Intl.supportedValuesOf`). Not packed until milestone SJL-9.3f, so that no stream version carries part of it (decision D29). |

Further libraries (the frontend, further data generators) are added by the milestones that create
them. *(Revised 2026-10-17, at milestone SJL-8.2, when the generator's row was added; the sentence read
"Further libraries (the frontend, Intl, the data generators) are added by the milestones that create
them.")* *(Revised again 2026-10-27, at milestone SJL-9.3a, when the Intl library's row was added; the sentence
read "Further libraries (the frontend, Intl, further data generators) are added by the milestones that create
them.")* The frontend's design document, the contract it is built to, is
[`docs/frontend/design.md`](docs/frontend/design.md), filed before any of its code; milestone SJL-13 creates the
library. *(Added 2026-10-19, at milestone SJL-12.1.)* The Intl library's design document is
[`docs/intl/design.md`](docs/intl/design.md), filed at milestone SJL-9.1 before any of its code; its data home,
[`data/intl/`](data/intl/README.md), is milestone SJL-9.2b's, and milestone SJL-9.3 creates the library. *(Added
2026-10-26, at milestone SJL-9.2b, as bundle `docs/evidence/sjl-9-001`'s README foresaw.)*

*(The `Broiler.ECMAScript.Generation` row revised 2026-10-26, at milestone SJL-9.2b, when the Intl data home's
generator was added; its "Not yet published" had been stale since `0.1.0-preview.5`. The row read: "\|
`Broiler.ECMAScript.Generation` \| The time data home's generator: the tz, CLDR and calendar pin readers, a tz
compiler equivalent to `zic`, the compact time zone and calendar tables, and source writers that wrap them into C#.
Test-only, as Broiler.Unicode.Generation is: engines' tests run it, and it carries no table. \| **The time data
home** of milestone SJL-8.2 (`Broiler.ECMAScript.Generation.Time`), ported from Broiler.VM's architecture tests;
its inputs and tables are [`data/time/`](data/time/README.md). Not yet published. \|")*

The frontend's area verdict is filed, and its design document now records the library's names, request and result
(its section 9), with the stage table [`docs/frontend/stages.tsv`](docs/frontend/stages.tsv) beside the early-error
catalogue; the library's row is added to the table above when SJL-13.2's first stage lands. *(Added 2026-10-26, at
milestone SJL-13.1.)*

## Pins

- **Specification:** the ECMA-262 commit `conformance/vectors/sources/ecma262.pin` names.
- **Unicode:** the version `conformance/vectors/sources/unicode.pin` names.
- **Data:** the white-space set's `Space_Separator` members, Unicode 17.0.0
  (`NumberParsing.UnicodeVersion`), generated into the algorithms library from the pinned
  `PropList.txt`, which the number area's design places there. The data areas of SJL-7 onward ship
  no data in a shared package (decision SJL-D-6 (c)); each engine embeds the tables a shared
  generator writes. *(Revised 2026-10-12, at SJL-3.3. The bullet read: "**Data:** none. No shared
  package carries data (decision SJL-D-6 (c)); each engine embeds the tables a shared generator
  writes.")*
- **Time data:** IANA tzdb 2026e, CLDR 48.2.0's time zone keys, and ICU4X's `icu_calendar` 2.3.0 and
  `calendrical_calculations` 0.2.4 (`TimeDataVersions`), pinned in [`data/time/`](data/time/README.md)
  as copies of Broiler.VM's pins, with the tables `Broiler.ECMAScript.Generation` writes from them. No
  package carries them; rule SL7 regenerates the tables byte for byte. *(Added 2026-10-17, at SJL-8.2.)*
- **Intl data:** CLDR 48.2.0 with its UCA 17.0.0, and the Unicode Character Database 17.0.0's `UnicodeData.txt` and
  `PropList.txt` (`IntlDataVersions`), pinned in [`data/intl/`](data/intl/README.md): every entry of Broiler.VM's CLDR
  pin byte for byte with the main-data files Broiler.JS's locale list adds and the `common/main` XML files decision D21
  reads, and the UCD files equal to Broiler.Unicode's pin entries; with each embedding's locale list (Broiler.VM's
  JSD-0082, decision D5) and Broiler.JS's tables, which `Broiler.ECMAScript.Generation` writes from them. No package
  carries them; rule SL7 regenerates the tables byte for byte. *(Added 2026-10-26, at SJL-9.2b.)*
- **Temporal:** `tc39/proposal-temporal@e8cc03fc`, the revision both engines follow (Broiler.VM's JSD-0078,
  decision D1); where its text and its polyfill disagree, the text rules (decision D7). *(Added 2026-10-18, at
  SJL-10.3.)*

## Conventions

Every public API follows [the conventions](docs/conventions.md): neutral types, failure as data, a
caller's meter charged before the work, explicit limits, determinism, no mutable statics, no
dynamic code, no I/O, no platform globalization or regular expressions, Native AOT and trimming
with no warning suppressed, and an XML documentation comment citing the clause each member
implements. The rules that decide them, SL1 to SL10, run in
[`Broiler.ECMAScript.Architecture.Tests`](Broiler.ECMAScript.Architecture.Tests/rules.register.json).

## Building

```bash
dotnet build Broiler.ECMAScript.slnf -c Release
dotnet test Broiler.ECMAScript.slnf -c Release
# The Native AOT probe, with Broiler.VM's root settings, over the whole corpus:
dotnet publish Broiler.ECMAScript/Broiler.ECMAScript.Probe -c Release -r linux-x64
# The time data home's zone table against the host's zic (where zic is installed):
python3 -I Broiler.ECMAScript/data/time/tzdb/check-zic.py
```

## Versions

The shared packages have their own version stream, tag prefix (`ecmascript-v`) and changelog
([`CHANGELOG.md`](CHANGELOG.md)), apart from the engine's (decision SJL-D-3). See
[`docs/publishing.md`](../docs/publishing.md).

## Consumers

| Engine | Consumes | Since |
|---|---|---|
| Broiler.JS | `Broiler.ECMAScript.Algorithms`, by project reference, from its parser, runtime, built-ins and storage | milestone SJL-3.5 |
| Broiler.VM | `Broiler.ECMAScript.Algorithms` `0.1.0-preview.3`, through its committed feed, from its profile and its compiler | milestone SJL-3.4 |

*(Corrected 2026-10-13, at milestone SJL-4.3, for the SJL-3 cut-overs. The rows read "| Broiler.JS |
nothing yet | — |" and "| Broiler.VM | nothing in a product project; the probe package, test-only,
through its committed feed | milestone SJL-2 |"; the package table's state for the algorithms library
read "The contract types (...) and **the number-conversion area** of milestone SJL-3 (...)".)*

*(Corrected 2026-10-14, at milestone SJL-5.3, for the text area and the version Broiler.VM pins since
SJL-4. The package table's state for the algorithms library read "..., **the number-conversion area** of
milestone SJL-3 (...) and **the numerics area** of milestone SJL-4 (...)", and the Broiler.VM row read
"`Broiler.ECMAScript.Algorithms` `0.1.0-preview.1`".)*

*(Corrected 2026-10-15, at milestone SJL-6.3, for the Date area and the version Broiler.VM pins since
SJL-5. The package table's state for the algorithms library read "..., **the numerics area** of milestone
SJL-4 (...) and **the text area** of milestone SJL-5 (...)", and the Broiler.VM row read
"`Broiler.ECMAScript.Algorithms` `0.1.0-preview.2`".)*

*(Corrected 2026-10-18, at milestone SJL-10.3, part C, for the Temporal durations, rounding and differences. The package
table's state for the algorithms library read "... and its calendars, the identifiers, ..., and calendar addition and
difference (part B) (`Broiler.ECMAScript.Temporal`; ...)", with "its records and limits, ... (part A)," before it.)*

*(Corrected 2026-10-18, at milestone SJL-10.3, part B, for the Temporal calendars. The package table's state for the
algorithms library read "..., **the Date area** of milestone SJL-6 (...) and **the time-zone and calendar area** of
milestone SJL-8, ... (the same namespace; ...)".)*

*(Corrected 2026-10-17, at milestone SJL-8.3, for the time-zone and calendar area. The package table's state
for the algorithms library read "..., **the text area** of milestone SJL-5 (...) and **the Date area** of
milestone SJL-6 (...)".)*

*(Corrected 2026-10-18, at milestone SJL-10.3, for the Temporal core's first part. The package table's state for
the algorithms library read "..., **the Date area** of milestone SJL-6 (...) and **the time-zone and calendar
area** of milestone SJL-8, ... (the same namespace; [adapter guide](...))".)*

## Licence

Apache-2.0. Ported code records its origin; no share-alike or unclear-licence code enters. See
[`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md).
