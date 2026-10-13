# Shared JavaScript libraries — Broiler.JS's half of the programme

Give each JavaScript language algorithm, data set and source front end that Broiler.JS and
Broiler.VM's JavaScript profile both implement **one maintained implementation**, in libraries that
name neither engine, hosted in this repository where the programme places them, and consumed by both
engines. This engine stays an independently usable, public reference engine built on the same
libraries, and keeps its own value model, object model, execution, IL back end, hosting and resource
policy.

> The evidence half is [`SharedLibraries.status.md`](SharedLibraries.status.md). **The programme
> itself is owned in the Broiler.VM repository**: its roadmap, ledger, decision register, area
> designs and milestone files are under `Broiler.VM/docs/javascript-shared-libraries/`, and its
> milestone files hold every exit gate in full. This plan carries Broiler.JS's part of that
> programme, in this repository's plan/status convention, and closes nothing on its own: each item
> below closes against the Broiler.JS clauses of its milestone, met by evidence collected here.
>
> Added 2026-10-12, at the programme's milestone SJL-0.

## Current state

The decisions are taken and their records filed. The shared conformance corpus exists, with this
engine's runners over it (SJL-1), and so does the shared folder, `Broiler.ECMAScript/`, with its
conventions, its rules, its version stream and its Native AOT probe (SJL-2); no algorithm has moved
into it, and no engine consumes a shared package. See
[`SharedLibraries.status.md`](SharedLibraries.status.md).

*(Corrected 2026-10-12, at milestone SJL-2. It read: "The decisions are taken, the records are being
filed, and no shared code exists. Milestone SJL-0 is in progress in both repositories; every other
milestone is not started.")*

## What this repository hosts

Decision **SJL-D-1 (b)**, taken on 2026-10-11, places the libraries by component:

| Library | Package | Home |
|---|---|---|
| Language algorithms: number conversion, numerics, text, Date, the Temporal core, time-zone algorithms | `Broiler.ECMAScript.Algorithms` | **This repository** |
| Intl algorithms over a data provider, collation included (milestone SJL-17) | `Broiler.ECMAScript.Intl` | **This repository** |
| The frontend: tokenizer, parser, canonical syntax tree, positions, diagnostics, static semantics | `Broiler.ECMAScript.Frontend` | **This repository** |
| The shared conformance vectors | `Broiler.ECMAScript.Conformance` (test-only) | **This repository** |
| Locale data: Broiler.VM's CLDR generator and its pins (SJL-D-1 (b-ii)) | no package (SJL-D-6 (c)); `Broiler.ECMAScript.IntlData` reserved | **This repository** |
| Time data: the time-zone and calendar generators and their pins (SJL-D-1 (b-iii)) | no package (SJL-D-6 (c)); `Broiler.ECMAScript.TimeData` reserved | **This repository** |
| Unicode character data and algorithms | the Broiler.Unicode packages | Broiler.Unicode, under SJL-7.1's owner conditions; otherwise this repository |
| Regular expressions | `Broiler.Regex` | Broiler.RegEx, fixed to Broiler.VM's requirements (SJL-D-14 (a)) |

The shared projects live in their own folder, solution filter and version stream, apart from the
`Broiler.JavaScript.*` engine packages (milestone SJL-2).

## The decisions this plan carries

Every decision was taken on 2026-10-11 by the owner, who holds every role the programme names. This
plan is the Broiler.JS record of the part each one asks of this repository:

| Decision | What it asks of Broiler.JS |
|---|---|
| **SJL-D-1 (b)** homes | Host the libraries and generators in the table above. A sub-area of Unicode that Broiler.Unicode does not meet SJL-7.1's owner conditions for comes here, with the reason recorded |
| **SJL-D-2 (a)** package identity | The neutral prefix `Broiler.ECMAScript`, one package per boundary: `.Algorithms`, `.Intl`, `.Frontend`, `.Conformance`; `.TimeData` and `.IntlData` reserved. No reservation of the prefix on nuget.org is required *(revised 2026-10-12, when the owner withdrew it; read "The prefix is reserved on nuget.org before anything under it is published (SJL-2.13).")*. Broiler.JSeal's package gate admits it unchanged |
| **SJL-D-3** versioning | A separate version stream for the shared packages, with its own solution filter, pack invocation and tag prefix; semantic versioning, an API baseline, package validation against the last published version, and a changelog with "Observable changes" and "Data" headings. A specification correction and a data-version change are each a minor version. The engines declare minimum versions in their nuspecs and pin exact versions in their lock files. `docs/publishing.md` records it at SJL-2.13 |
| **SJL-D-4 (c)** how Broiler.VM obtains the packages | While an area is in development, its preview packages reach Broiler.VM through Broiler.VM's committed, hash-pinned feed, not through nuget.org (SJL-3's home clause 3); anything a Broiler.VM package release depends on is published to nuget.org first, so the home publishes before such a release |
| **SJL-D-5 (a)** assurance | Every relevant unit of a shared project carries a Broiler Code Assurance annotation, the artefacts are generated, and CI gates on them from the project's first commit; an area moves into a shared library only once that gate is active. Broiler.VM's release reads the review state of the revision it ships |
| **SJL-D-6 (c)** data | This repository holds the locale- and time-data generators and their pins; each engine embeds the tables they generate, in an assembly of its own; shared code reads tables through a provider. Every Broiler.JS behaviour that changes because pinned data replaces host data is a correction under SJL-D-16 |
| **SJL-D-9** fork policy | Fixes to a shared area flow through the shared library, in both directions. Engine code does not flow between the engines, and neither engine is the other's upstream. Evidence does not transfer. A shared library is never patched inside an engine: an engine pins back, or carries a dated adapter workaround naming the upstream defect and the release that removes it |
| **SJL-D-14 (a), (i)** regular expressions | Broiler.JS moves onto the fixed `Broiler.Regex` at SJL-11, and its .NET translator retires only after Broiler.Regex implements duplicate named groups |
| **SJL-D-16 (a)** observable corrections | A difference where a shared library follows the specification and this engine did not is a correction, listed by name in the changelog and admitted by the area's rule; a per-difference decision only for a difference with a known dependent consumer |
| **SJL-D-17** non-standard extensions | `Decimal` and `m` literals, `Number.prototype.clz`, `String.prototype.contains`, the .NET format-string extension to `toLocaleString`, and the SpiderMonkey-style date heuristics with the culture-dependent legacy `Date.parse` **stay in Broiler.JS**. Where one needs a shared primitive, a parameter of a shared operation provides it, such as a lexer flag that admits the `m` suffix |
| **SJL-D-18** no unification | Whole-runtime unification is **not planned**: `JSValue` and Broiler.VM's `JsValue` stay as they are, and so do objects, frames, dispatch, back ends, budgets and hosting. SJL-16 confirms it, or reopens it only on new evidence |
| **SJL-D-23 (a)** the pilot | The programme continues past the SJL-3 pilot, a choice taken before the pilot ran. SJL-3's criteria are its exit clauses and stop nothing else |

Decisions applied later are recorded by the milestone that applies them: SJL-D-10's metering
contract in the shared conventions (SJL-2.10) and the pilot record (SJL-3.1); SJL-D-25's public-type
dispositions in [`docs/public-api.md`](../public-api.md) at SJL-3.5 and each area; SJL-D-15's BigInt
width at SJL-4; SJL-D-19's providers at SJL-6; SJL-D-20 and SJL-D-21 at SJL-8; SJL-D-22 and SJL-D-24
at SJL-9; SJL-D-11 and SJL-D-12 at SJL-12, with SJL-D-12's `Ast` and `Parser` disposition at SJL-14;
SJL-D-7, SJL-D-8 and SJL-D-13 are Broiler.VM's alone, and ask nothing of this repository.

## Area verdicts

Each area milestone's first work package applies the programme's extraction gate (conditions X1 to X7
of `Broiler.VM/docs/javascript-shared-libraries/roadmap.md` section 5) and files the verdict twice: as a
JSD in Broiler.VM's JavaScript profile series, and here.

**Number conversion (SJL-3.1), filed 2026-10-12: accepted.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0065-number-conversion-moves-to-the-shared-algorithms-library.md`
(JSD-0065), which states each condition. This repository's part of it:

- **The correspondence was re-derived** at this repository's `b7a2a3f`. Every file the area design
  (`Broiler.VM/docs/javascript-shared-libraries/areas/numbers.md`) names is byte-identical there to
  `44a214dc`, the revision the design read. Every difference SJL-1's run found is classified in
  `scripts/conformance/number-divergences.txt`; the ones the shared operations correct are listed in
  `CHANGELOG.md` at SJL-3.5, under decision SJL-D-16 (a), and the literal-scanning ones stay with
  SJL-14.
- **The deletion list gains two members** the design did not name: `Storage/KeyStrings`'
  `TryGetArrayIndex` and `TryGetCanonicalNumericIndex`, which classify every interned key with a second
  copy of the array-index test and a canonical-numeric test that reads .NET's `"R"` format. They become
  calls to the shared key operations, so `Broiler.JavaScript.Storage` references the library beside
  `Parser`, `Runtime` and `BuiltIns`. Without that, the engine would keep an intra-engine duplicate of
  the area, which SJL-3's exit clause 14 refuses.
- **Decision SJL-D-10 (a)** is applied in the library: each parsing operation takes a nullable
  `WorkCharge` and charges the input's length in UTF-16 code units, in blocks, before reading it.
  This engine has no budget, and its adapters pass none.
- **The non-standard extensions stay here** (SJL-D-17): the `m` literal suffix, `Number.prototype.clz`
  and `toLocaleString`'s .NET format strings. None is a shared operation.

**Numerics (SJL-4.1), filed 2026-10-13: all four sub-areas accepted.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0067-numerics-move-to-the-shared-algorithms-library.md`
(JSD-0067), which states each condition for integer conversions and Float16, the Math kernels, exact
rounding and the BigInt kernel, each on its own; its input, the correspondence from code with every
member by file and line, is `Broiler.VM/docs/evidence/sjl-4-001/correspondence.md`. This repository's
part of it:

- **The correspondence was read from code**, at this repository's `4899f9c`, because the area has no
  vectors yet; SJL-4.2's vectors confirm it. It found far more copies of the area here than the design
  named: about thirteen more `ToIndex`, `ToLength` and `ToIntegerOrInfinity` helpers, Atomics' own
  narrowing and BigInt arithmetic, `JSDate.CoercedIntValue`, a second `Math.Pow` emitter, about eleven
  copies of the rounding modes across Temporal and Intl, and BigInt text, comparison and key paths
  outside `JSBigInt`'s named members. Each joins the deletion list, and SJL-4.5 removes or forwards it.
- **The corrections**, each listed in `CHANGELOG.md` at SJL-4.5 under decision SJL-D-16 (a): `1 ** NaN`
  on every path and the ±1-to-an-infinity cases on the compiled fast paths; Atomics' operands beyond
  2^53; `%TypedArray%.prototype.fill`'s range; the typed-array constructor's conversion order;
  `%TypedArray%.from`'s length; a Date setter's argument; `String.prototype.endsWith`'s undefined
  position; the scientific and compact notations' rounding mode; `NudgeToCalendarUnit`'s window;
  `Duration.prototype.total`'s double rounding; the culture-dependent BigInt text; `Power`'s 32-bit
  exponent and a shift's .NET exceptions; `asUintN` at very large widths; the compiled `==` between a
  BigInt and a Number literal; BigInt property keys beyond 2^32 - 1 and below 0; and `ToBigInt`'s hint
  and Number refusal in the typed arrays. The Intl mathematical value and the Temporal consumers'
  quantities are wrong here too, and stay with SJL-9 and SJL-10, which the milestone leaves them to.
- **Decision SJL-D-15 (a) is recorded here**, as the decision names this record: this engine passes
  the BigInt kernel the unbounded width policy and a no-op meter, and maps the kernel's over-width
  status to `RangeError` at the representation's limit, never to a .NET exception. The kernel keeps
  Broiler.VM's charging units (JSD-0067, decision 3), which costs nothing here, where no meter is
  passed.
- **Decision SJL-D-25 applies to the public members the area empties**: `JSNumericOperators.ToInt32`
  and `ToUint32` stay as forwarders, and the public, saturating `IntegerValue` stays as a member that
  forwards to the library's `ToIntegerOrInfinity` and saturates as it does today; every specification
  path that used it calls the library instead. `docs/public-api.md` records each at SJL-4.5.
- **Not shared, and staying here:** `Math.fround`, `clz32`, `imul`, `max`, `min` and `sign`, whose
  result the platform already gives; `Math.random`'s single generator; `Math.sumPrecise`'s feature
  flag; typed arrays' host byte order. `JSMath.Clz32`'s comment cites Jurassic's `MathObject.cs`, and
  `SumPrecise` ports the `Math.sumPrecise` proposal's reference algorithm; the third-party notices name
  neither, and SJL-4.5 corrects that when the kernel moves.

**Numerics vectors (SJL-4.2), 2026-10-13.** Corpus revision 6 adds the area's vectors
(`conformance/vectors/numerics/`). Where the specification leaves a result to the implementation, a
vector's `per-engine.broiler-js` expectation is this plan's, and this is what the plan commits this
engine to:

- **A Float16 NaN keeps its payload.** This engine does not normalise: a NaN is written quiet, with
  its sign and the ten most significant bits of the double's payload, which is what its `(Half)`
  conversion does today and what it passes the shared `Float16.Encode` as its NaN policy at SJL-4.5.
- **The approximated Math results are the library's**, as JSD-0067 decision 5 states them: `cbrt`
  correctly rounded; `expm1` and `log1p` within one unit in the last place; `hypot` within one unit in
  the last place until the library states its bound; and `**` and `Math.pow`'s general case,
  `System.Math.Pow`'s, pinned only where every approximation gives one result.
- **A BigInt past any representation is a `RangeError`**, as decision SJL-D-15 (a) has it, never a .NET
  exception or a wrong value.

**The first runs (SJL-4.2), 2026-10-13.** Each engine's first engine-level run on revision 6 is
classified at revision 7, and both engines then classify every vector. SJL-4.5 answers every one of
this engine's differences: as corrections listed in `CHANGELOG.md`, BigInt powers past a 32-bit
exponent, `asUintN` at very large widths, BigInt property keys beyond 2^32 - 1, `1 ** NaN` and the
cases of 1 to an infinity, `expm1` and `log1p` beyond any approximation, and a hexadecimal
`StringToBigInt` whose time grows with the square of its digits; and, by the library's bounds, `cbrt`,
`hypot` and the rest of `expm1` and `log1p`. Four differences between
the engines were not in the area verdict's classification, which fires the verdict's first falsifier;
Broiler.VM's `JSD-0068` (`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0068-the-numerics-vectors-find-what-x2-did-not-classify.md`)
classifies them and keeps the verdict. For this engine it corrects one reading above, that the cases of
1 to an infinity are wrong "on the compiled fast paths": `1 ** Infinity` written as literals is 1 here
too, and SJL-4.5 corrects every path. It also refutes the verdict's reading that the library adopts this
engine's `expm1`, which is not accurate across the range: the library implements its own.

**The implementation (SJL-4.3), 2026-10-13.** The numerics area is in `Broiler.ECMAScript.Algorithms`
(`2b0a16b`), with its adapter guide, and the shared stream moves to `0.2.0`. Corpus revision 8 gives 72
vectors the operation and inputs that decide them, so that the library-level runner can read them, and
every numerics vector the library answers passes at library level. The reference-arm comparison is met
under [`reference-arms.md`](../evidence/sjl-4-001/reference-arms.md) as revised before any run
(bundle `sjl-4-001` section 2). Its first run found one defect of the library, which was corrected
before the run that met the rule: an exact value at a rounding window's upper end was rounded down
under "zero". For this engine, the comparison's corrections are what SJL-4.5 answers:

- Atomics' operands are saturated to 64 bits before they wrap;
- the ToIntegerOrInfinity, ToLength and ToIndex helpers give −0, saturate at their types' bounds, or
  refuse indices the clause admits;
- BigInt powers with a huge exponent throw a .NET overflow, and `asUintN` wraps or divides by zero at
  very large widths;
- a rational's nearest double is not rounded once, and an integer at the overflow threshold gives the
  largest double instead of the infinity;
- BigInt text follows the process's culture;
- a BigInt compared with a literal Number is compared through a double;
- `expm1` and `log1p` are more than a unit in the last place out near zero.

The comparison admits this engine's `cbrt`, `hypot` and the rest of `expm1` and `log1p` as
approximations, which the library's bounds replace.

## Invariants

1. **No edge between the engines, in either direction.** No shared project references a
   `Broiler.JavaScript.*` engine project or package, or anything in Broiler.VM (rule SL1). This engine
   references the shared packages the way any consumer does.
2. **The shared libraries implement the specification only.** Extensions stay in this engine's
   adapters (SJL-D-17).
3. **Adapters hold no algorithm.** An adapter converts values, orders coercions, raises this engine's
   errors and charges; the algorithm is the library's.
4. **Deletion in the same change.** A cut-over that leaves a member of its area's deletion list in
   place has not removed the duplication.
5. **Each repository's gate is met by its own evidence.** Agreement between the engines is a
   cross-check, never an oracle; the two test262 runners' totals are never compared.
6. **No cross-engine speed comparison**, in any document. Each measurement compares this engine with
   itself, under a rule written before the run.

## Items

One item per programme milestone with Broiler.JS or home clauses, in the programme's delivery order.
**Owner of every item: the Broiler.JS maintainer**, with the JavaScript profile owner for the
programme's coordination; one person holds both roles, so no item here is independently confirmed by
a second reader. Each exit criterion is the Broiler.JS and home clauses of the named milestone file;
the summary here does not replace them.

| Item | Next action | Exit criterion |
|---|---|---|
| **SJL-0** — records, decisions and baselines | File this plan pair and the revisions of `dependencies.md`, `public-api.md`, `README.md`, `Assemblies.md`, `Roadmap.md`, `ModernizationDelivery.md`, `Modernization.md` and `builtins-vs-clr-types.md`; commit the decision-rule files; collect bundle `docs/evidence/sjl-0-001/` | Clauses 8 to 11 of `milestones/sjl-0-records-decisions-and-baselines.md`: every document above carries a dated revision, the plan pair exists, the bundle retains test262 per path, a public API listing, package contents and nuspecs and the pilot measurement at a named commit, and the decision-rule files hold no figure |
| **SJL-1** — the shared conformance vectors | Write the vector schema, the validator and its negative-control file, then the number vectors, then the engine-level runner | Home clauses 1 to 3: the validator refuses the malformed control and any vector without an independent source; every number vector has provenance; the engine-level run is retained with every difference classified under `docs/evidence/sjl-1-001/decision-rule.md` |
| **SJL-2** — the admission path | Create the shared folder with its conventions, the `SL` rules, the API baseline, the assurance gate, the version stream and the Native AOT probe *(corrected 2026-10-12: the list named "the prefix reservation" before the probe, which the owner withdrew)* | Clauses 7 to 10: SL1 to SL10 `Active` with witnesses; the API baseline and the assurance gate in CI, each watched failing on an injected drift; the probe pre-release packs, restores and passes package validation; the Native AOT probe publishes with zero warnings and runs |
| **SJL-3** — number conversion, the pilot | Implement the algorithms library's number area; run the reference arms; cut this engine over from `Parser`, `Runtime` and `BuiltIns` | Clauses 1 to 7: the library under the conventions with its vectors and reference arms classified; every deletion-list member gone and every public disposition applied; test262 per path with no path passing at base failing after; corrections in the changelog; the measurement under `docs/evidence/sjl-3-001/decision-rule.md` |
| **SJL-12** — the frontend contract and in-place preparation | Remove the parser's ambient state, mutable statics, `Runtime` references and parse-time desugaring in place (Modernization MOD-M2-3 to MOD-M2-5 are this half), and record the frontend contract here | Clauses 1 to 3: an architecture test with a witness enforces the parser's independence; test262 per path meets the rule; the contract record is in this plan |
| **SJL-4** — numerics | The area's library work and cut-over; SJL-D-15 recorded | Its Broiler.JS clauses: deletion list absent, dispositions recorded, test262 per path, vectors, corrections listed |
| **SJL-5** — text algorithms | As above | Its Broiler.JS clauses |
| **SJL-6** — Date | As above, with the provider interfaces of SJL-D-19 and Date storage migrated | Its Broiler.JS clauses, the non-UTC host run included |
| **SJL-7** — Unicode | Move off host Unicode data onto the owner's API | Its Broiler.JS clauses: no host Unicode API in the areas covered |
| **SJL-8** — time zones and calendars | Move the generators and pins here and `Temporal` off host zones and .NET calendars | Its Broiler.JS clauses, the reference datasets included |
| **SJL-11** — regular expressions | After Broiler.Regex meets every requirement and implements duplicate named groups, retire the translator and the validators | Clauses 4 to 7: translator gone, test262 `RegExp` per path, duplicate named groups still working, corrections listed; `Component.md` §3 closed |
| **SJL-10** — Temporal core | The area's library work and cut-over | Its Broiler.JS clauses, the polyfill dataset included |
| **SJL-9** — locale data and Intl | Move the CLDR generator and pins here; host globalization only behind the optional provider (SJL-D-22 (c)) | Its Broiler.JS clauses |
| **SJL-17** — collation | Share the collation algorithm after SJL-9 | Its Broiler.JS clauses: host collation only behind the optional provider |
| **SJL-13** — the shared syntax library | Implement the frontend's syntax half in this repository | Home clauses 1 to 3: under the contract and conventions, the parse corpus and syntax tests at library level, SL9 and the Native AOT probe |
| **SJL-14** — Broiler.JS on the shared syntax library | Move the compiler and the portable compiler onto the frontend; make `Ast` and `Parser` obsolete adapter packages (SJL-D-12 (i)) | Clauses 1 to 6, the Native AOT sample regenerated and run included |
| **SJL-15** — shared static semantics | Implement the semantic model here; delete the scattered early errors and scope analysis | Home clauses 1 to 3 and Broiler.JS clauses 10 and 11 |
| **SJL-16** — close-out | Sweep the deletion lists, retire the reference arms, confirm SJL-D-18 | The close-out's sweep in this repository |

The state of each item is one line in [`SharedLibraries.status.md`](SharedLibraries.status.md).
Following this directory's convention, no changing count is written here: a test262 total, a vector
count or a measurement belongs in a bundle under `docs/evidence/`, and the status record links it.
