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
| `Broiler.ECMAScript.Algorithms` | The language algorithms: number conversion, numerics, text, Date, the Temporal core, the time-zone algorithms. Data-free; references the base class library only. | **Contract types only**: the `WorkCharge` meter, `ResultStatus`, `Failure`, `LengthLimit` and `DepthLimit`. Milestone SJL-3 moves the first algorithms in. |
| `Broiler.ECMAScript.Conformance` | The shared conformance vectors' reader, validator and runners. Test-only. | Not packed at this version; the corpus itself is [`conformance/vectors/`](../conformance/vectors/README.md). |

Further libraries (the frontend, Intl, the data generators) are added by the milestones that create
them.

## Pins

- **Specification:** the ECMA-262 commit `conformance/vectors/sources/ecma262.pin` names.
- **Unicode:** the version `conformance/vectors/sources/unicode.pin` names.
- **Data:** none. No shared package carries data (decision SJL-D-6 (c)); each engine embeds the
  tables a shared generator writes.

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
| Broiler.JS | nothing yet | — |
| Broiler.VM | nothing in a product project; the probe package, test-only, through its committed feed | milestone SJL-2 |

## Licence

Apache-2.0. Ported code records its origin; no share-alike or unclear-licence code enters. See
[`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md).
