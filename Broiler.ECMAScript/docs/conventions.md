# Conventions of the shared JavaScript libraries

[← Shared libraries](../README.md) · [Rules register](../Broiler.ECMAScript.Architecture.Tests/rules.register.json) ·
[Adapter-guide template](adapter-guide-template.md)

**This document is normative.** It makes the contract conventions of the programme's architecture,
[section 4 of `architecture.md`](../../../Broiler.VM/docs/javascript-shared-libraries/architecture.md#4-contract-conventions)
in the Broiler.VM repository, binding on every project below `Broiler.ECMAScript/`. A change that
breaks one of them is a defect, whoever makes it. Where this document and that section differ, that
section wins and this document is corrected.

Filed 2026-10-12, at milestone SJL-2.10.

## Scope

- **A shared library** is a project below `Broiler.ECMAScript/` whose output is a library and that
  is not a test project: `Broiler.ECMAScript.Algorithms` and the test-only
  `Broiler.ECMAScript.Conformance` today, and the frontend, Intl and data generators as the
  milestones that create them add them. Every convention applies to each one.
- **The Native AOT probe**, `Broiler.ECMAScript.Probe`, is an executable. It reads the corpus from
  disk, so convention 8's I/O clause does not hold for it; conventions 12 and 17 do.
- **Test projects** are held to convention 17's style rules, and to nothing that would stop xunit
  discovering them.

## The conventions

Each convention is either decided by a test, the `SL` rule named, or a review obligation that each
area's adapter guide names. A rule's register row, its test and its witness are in
[`rules.register.json`](../Broiler.ECMAScript.Architecture.Tests/rules.register.json).

| # | Convention | Decided by |
|---|---|---|
| 1 | **Neutral types only.** Public signatures use base class library types (`string`, `ReadOnlySpan<char>`, `Span<char>`, `double`, `float`, `Half`, the integer types, `BigInteger`, `bool`) and types the library defines (records, structs, enums). No public signature names an engine type, `object`, `dynamic`, `Type`, or a delegate that can run guest code. The test-only Conformance library also names the BCL's read-only collections. | Review; SL1 keeps engine types out of reach |
| 2 | **No coercion, no property access, no guest callbacks.** Inputs arrive converted. Where the specification interleaves conversions with computation, the library exposes the steps and the adapter runs the conversions between them, in specification order. | Review, per adapter guide |
| 3 | **Failure is data.** An expected failure is a status (`ResultStatus`) with a position where one exists (`Failure`). A library throws only when its caller breaks a documented precondition. Message text is not contract; a stable message identifier is (SJL-D-11). A default English rendering is a constant string, never a satellite resource. | Review; the API baseline (SL6) shows every signature |
| 4 | **Work is charged by the caller's meter.** An operation whose work grows with its input takes a nullable `WorkCharge` and charges before the work it charges for, in units it documents. The meter may throw; the library then leaves no shared state changed. The library never reads a clock and never polls cancellation. | Review; SL3 and SL4 refuse the clock |
| 5 | **Limits are explicit.** Output length, depth and width come from the caller (`LengthLimit`, `DepthLimit` and their successors). Exceeding one is `ResultStatus.LimitExceeded`, never a crash, a silent "no match" or a shortened result. **A limit is never the stack.** | Review; the probe runs every vector under Native AOT |
| 6 | **Determinism.** A result depends on its inputs and the library version only: no culture, time zone or host Unicode, no randomness, environment or process history, no hash-table iteration order. | SL4, SL9 |
| 7 | **No ambient or mutable static state.** No mutable static field, settable static property, `[ThreadStatic]`, `AsyncLocal` or `ThreadLocal`. Static data is immutable: constant spans, frozen or immutable collections, or lazily built immutable values. | SL2 |
| 8 | **No dynamic code and no hidden machinery.** Nothing on Broiler.VM's B5, B5b and B5c lists; no generic instantiation over a run-time `Type`, no `Enum.GetValues(Type)`, no `dynamic`, no reflection-based serialization, no file or network I/O, no environment read. Providers and data are handed in by the caller. | SL3 |
| 9 | **Platform independence.** No `string.Normalize` or `IsNormalized`, no `System.Text.RegularExpressions`, no `CultureInfo`, `TextInfo`, `CompareInfo`, `TimeZoneInfo` or `DateTime.Now`, no `CharUnicodeInfo`, `char.Is*` or `Rune.Is*` beyond ASCII. A number formatted or parsed through a .NET primitive passes `NumberFormatInfo.InvariantInfo`, and the vectors pin the primitive's behaviour. | SL4 |
| 10 | **Thread safety.** Every public member is safe to call concurrently. | SL2, SL9 |
| 11 | **Allocation discipline.** Hot operations have span-based overloads writing into caller-owned buffers. No cache grows without bound. | Review |
| 12 | **Native AOT and trimming.** `IsAotCompatible` and `IsTrimmable` for every shared library, with the trim, AOT and single-file analyzers reporting nothing, as errors. No such warning is suppressed. The probe publishes with Broiler.VM's root settings and runs the whole corpus. | SL5 |
| 13 | **Data versions are public**, as constants and package metadata, and tables are `ReadOnlySpan<byte>` over constant data that regenerate byte for byte. No library carries data at this version (SJL-D-6 (c)). | SL7 |
| 14 | **Names that do not collide with engine scans.** No public member named `Normalize`, `IsNormalized` or `NormalizationForm`: Broiler.VM's N23 would refuse the adapter that calls it. | Review; SL6's baseline lists every name |
| 15 | **Every public member cites what it implements**: the ECMA-262, ECMA-402, Temporal, Unicode or RFC clause, at the pinned edition, in its XML documentation. A missing documentation comment is a build error here. | The build (CS1591 as an error); review for the citation |
| 16 | **Versioning and compatibility.** Semantic versioning; the API baseline compared in both directions; package validation against the last published version; a changelog entry for every observable behaviour change and every data-version change. | SL6; [`CHANGELOG.md`](../CHANGELOG.md) |
| 17 | **Frameworks and code style.** `net10.0`; nullable enabled; implicit usings disabled; warnings as errors; no preprocessor directives; LF line endings under `.gitattributes`. | The build (`Directory.Build.props`); SL10 |
| 18 | **Assurance.** Every relevant declaration carries the Broiler Code Assurance annotation, the generated artefacts are current, and CI fails on drift from the folder's first commit (SJL-D-5 (a)). | SL8 |
| 19 | **Licensing and provenance.** Apache-2.0. Ported code records its origin (`Origin=Ported`). No share-alike or unclear-licence code enters. Data notices travel with the tables. | Review; [`THIRD_PARTY_NOTICES.md`](../../THIRD_PARTY_NOTICES.md) |

## How a convention is changed

The conventions are the programme's, not this repository's. A change is made first in Broiler.VM's
`architecture.md` section 4, under the programme's governance, and then here, in place, with a dated
revision quoting the text it replaces. A rule that decides a convention changes in the same commit
as the convention, and its witness is watched failing and passing again.

## Every library also carries

Section 10 of the programme's [`governance.md`](../../../Broiler.VM/docs/javascript-shared-libraries/governance.md#10-documentation-obligations-per-library):
a README with its scope, pins, conventions and consumers; a changelog with "Observable changes" and
"Data" headings; XML documentation citing the clause of every public member; a provenance file for
every data set; and an adapter guide, from [the template](adapter-guide-template.md), for every
operation an engine calls.
