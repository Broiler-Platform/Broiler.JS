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
| `Broiler.ECMAScript.Algorithms` | The language algorithms: number conversion, numerics, text, Date, the Temporal core, the time-zone algorithms. References the base class library only, and carries no data but the white-space set's generated `Space_Separator` table. | The contract types (`WorkCharge`, `ResultStatus`, `Failure`, `LengthLimit`, `DepthLimit`), **the number-conversion area** of milestone SJL-3 (`Broiler.ECMAScript.Numbers`; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numbers.md)), **the numerics area** of milestone SJL-4 (the same namespace; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-numerics.md)) and **the text area** of milestone SJL-5 (`Broiler.ECMAScript.Text`; [adapter guide](Broiler.ECMAScript.Algorithms/docs/adapter-guide-text.md)). |
| `Broiler.ECMAScript.Conformance` | The shared conformance vectors' reader, validator and runners. Test-only. | Not packed at this version; the corpus itself is [`conformance/vectors/`](../conformance/vectors/README.md). |

Further libraries (the frontend, Intl, the data generators) are added by the milestones that create
them.

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
```

## Versions

The shared packages have their own version stream, tag prefix (`ecmascript-v`) and changelog
([`CHANGELOG.md`](CHANGELOG.md)), apart from the engine's (decision SJL-D-3). See
[`docs/publishing.md`](../docs/publishing.md).

## Consumers

| Engine | Consumes | Since |
|---|---|---|
| Broiler.JS | `Broiler.ECMAScript.Algorithms`, by project reference, from its parser, runtime, built-ins and storage | milestone SJL-3.5 |
| Broiler.VM | `Broiler.ECMAScript.Algorithms` `0.1.0-preview.2`, through its committed feed, from its profile and its compiler | milestone SJL-3.4 |

*(Corrected 2026-10-13, at milestone SJL-4.3, for the SJL-3 cut-overs. The rows read "| Broiler.JS |
nothing yet | — |" and "| Broiler.VM | nothing in a product project; the probe package, test-only,
through its committed feed | milestone SJL-2 |"; the package table's state for the algorithms library
read "The contract types (...) and **the number-conversion area** of milestone SJL-3 (...)".)*

*(Corrected 2026-10-14, at milestone SJL-5.3, for the text area and the version Broiler.VM pins since
SJL-4. The package table's state for the algorithms library read "..., **the number-conversion area** of
milestone SJL-3 (...) and **the numerics area** of milestone SJL-4 (...)", and the Broiler.VM row read
"`Broiler.ECMAScript.Algorithms` `0.1.0-preview.1`".)*

## Licence

Apache-2.0. Ported code records its origin; no share-alike or unclear-licence code enters. See
[`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md).
