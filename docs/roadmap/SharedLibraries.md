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
conventions, its rules, its version stream and its Native AOT probe (SJL-2). Two areas have moved into
it, and both engines consume them: number conversion (SJL-3) and the numerics (SJL-4, whose cut-overs
are done and whose evidence remains). See [`SharedLibraries.status.md`](SharedLibraries.status.md).

*(Corrected 2026-10-12, at milestone SJL-2. It read: "The decisions are taken, the records are being
filed, and no shared code exists. Milestone SJL-0 is in progress in both repositories; every other
milestone is not started.")* *(Corrected again 2026-10-14, at milestone SJL-4.5; the correction was
due from SJL-3's cut-overs. The second sentence ended: "(SJL-2); no algorithm has moved into it, and
no engine consumes a shared package.")*

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
SJL-D-7, SJL-D-8 and SJL-D-13 are Broiler.VM's alone, and ask nothing of this repository. *(Noted 2026-10-19, at
SJL-12.1: SJL-D-11 and SJL-D-12 are recorded in "The frontend contract (SJL-12)" below, filed that day with the
contract record as work package SJL-12.2.)*

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

**Clause 7's measurement half: the owner's decision of 2026-10-20.** SJL-3's clause 7 asks, beside the pristine
consumer and package checks, that this engine's measurement meet its predeclared rule, part B of
[`decision-rule.md`](../evidence/sjl-3-001/decision-rule.md). Part B was taken on this repository's machine, which is
not a controlled lane, so the run is void and was not judged; its arithmetic would fail clause 5 on `Literals` (bundle
[`sjl-3-001`](../evidence/sjl-3-001/README.md), section 7). On 2026-10-20 the owner decided, as recommended, to leave
this half unmet. The decision is taken under the owner's direction of 2026-10-18, "Please do all recommended decisions
and skip waiting for review.", which changes no rule, in the work the owner's words of 2026-10-19 started: "Also
continue with the other remaining SJL milestones". The programme's ledger is to record it, and a dated note under
decision SJL-D-23 (`Broiler.VM/docs/javascript-shared-libraries/decisions.md`) the trigger below; both are filed in
Broiler.VM apart from this record.
- **What would meet the clause.** A MET reading of part B as written, on a controlled lane, at its own arms: B, the
  rule commit `1410079e`, whose engine projects are byte-identical to the base `44a214dc`; and A, the after commit
  `ef676932`, with the rule commit's workloads and harness *(corrected 2026-10-20, after review; read "**What meets
  the clause.** Part B as written, on a controlled lane, at its own arms")*. The void run was not judged, so that run
  would be part B's first judged run, at full strength, and not a re-run.
- **Why no lane exists.** This repository's machine is a container shared with other work, and every GitHub-hosted
  runner, larger runners included, is a smoke lane: "GitHub-hosted runners remain smoke lanes, not controlled
  acceptance hardware" ([`Measurement.md`](Measurement.md), "Controlled lanes and effective configuration"). Of the
  missing lanes, Phase-0 says "Do not work around this" ([`Phase-0.md`](Phase-0.md), "What is left — acceptance
  infrastructure").
- **No revision onto a smoke lane is written.** Part B voids a smoke run, and a revision that admitted one would be
  the workaround Phase-0 refuses. No record of the programme waives a clause: the only route past one is a gate
  revision, which the trigger below leaves to a later dated choice.
- **The route is Phase-0's item 0-8**: a dedicated host the owner provisions, linux-x64 first, recorded as
  `Measurement.md` asks (the CPU model, microcode, core topology, memory, the OS, the power, governor and thermal
  policy, and each child's effective configuration). The same lane serves SJL-7's part B here (JSD-0031 §4(b)'s fifth
  reason, below), SJL-11's own measurement here, and SJL-14's clause 6, this engine's parse measurement. Only the
  owner can provision it.
- **If the original arms cannot be built on that lane**, a dated revision of part B moves both arms to the code then
  current, as SJL-7's part B waits for a lane "at whatever commit is then current" ("After the evidence, 2026-10-17",
  below). Its decision is weaker evidence, and it is part B's last measurement.
- **If clause 5 fails there**, part B reads the failure as "a regression SJL-3 answers before it closes": this engine
  fixes it, and one revised measurement follows under a dated revision, with arm A at the fix *(corrected
  2026-10-20, after review; read "a revised measurement follows")*.
- **The trigger.** The owner's next dated choice on this half is due when either (a) a controlled lane exists,
  recorded as `Measurement.md` requires; or (b) this half, Broiler.VM's clause 13 if it is still open, and SJL-4's
  clause 9, a Broiler.VM clause, are the only unmet clauses of SJL-3 to SJL-15 and SJL-17. At (b) the choice is
  between waiting for a lane and a gate revision under SJL-D-23's alternative (b), which would need dated edits to the
  programme's decision register, the milestone file, the ledger and this plan pair, and would be the first exit-gate
  revision made after the evidence it concerns. The trigger is noted, not taken.
- **What it changes.** No rule, no gate and no clause. SJL-3 stays `In progress`, and every milestone that may not
  meet its gate before SJL-3 meets SJL-3's still waits. Nothing is accepted: no second holder exists to review it.

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
(`2b0a16b`), with its adapter guide, and the shared stream's next version is `0.1.0-preview.2`, the owner
keeping the `0.1.0` prefix while the stream is a preview *(corrected 2026-10-14; read "and the shared
stream moves to `0.2.0`")*. Corpus revision 8 gives 72
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

**The cut-over (SJL-4.5), 2026-10-13 to 2026-10-14.** This engine's numerics are the shared library's: the integer
conversions and Float16, the Math kernels on every path the compiler has for `**`, the exact rounding
of Temporal and Intl, and the BigInt kernel (`0aa8369`, `73f1cd4`, `c565360`, `21a72c2` and
`e4094ec`). `CHANGELOG.md` lists every correction under decision SJL-D-16 (a), each with the vector or
test that names it, and corpus revision 9 (`f2c3597`) retires every divergence field of the area,
this engine's and those of Broiler.VM, whose cut-over (SJL-4.4) corrected its own. This repository's
part of the record:

- **Decision SJL-D-25 is applied, and no public member is removed**, so its consumer check is not
  needed: `JSNumericOperators.ToInt32` and `ToUint32` and `JSValue.IntValue` and `UIntValue` are
  forwarders, `IntegerValue` stays as this plan said, `JSMath.Hypot(double, double)` and
  `JSBigInt.ToNumber` are obsolete forwarders, and `BigIntegerExtensions.FromDouble`, which had no
  caller and which the cut-over did not empty, stays unchanged. `docs/public-api.md` records each.
- **Decision SJL-D-15 (a) found a defect of the library.** The representation's limit that the
  unbounded policy passes, `BigIntPolicy.RepresentationLimit`, was wider than a .NET `BigInteger`
  holds, so a result between the two widths raised a .NET exception, which the decision refuses. The
  limit is now below .NET's, with room for what an operation computes before it refuses (`ff7416b`,
  with a test of the edge and an entry in the library's changelog). Broiler.VM's width policy never
  came near it, and the package its feed holds is unaffected.
- **The search for surviving copies of the area**, made before the record, found hand-written
  conversions in built-ins outside the correspondence's list, most of them `ToIntegerOrInfinity` and
  `ToLength`, and some of them wrong. They call the library now (``eb3ff34``), and the changelog
  lists the corrections. Temporal's `ToIntegerWithTruncation` is one adapter over the library's
  `ToIntegerOrInfinity`.
- **One implementation-defined result changed:** a NaN read from binary16 (`Math.f16round`, a
  `Float16Array` element, `getFloat16`) is the platform's quiet NaN. A NaN written to binary16 keeps
  its sign and the high bits of its payload, as SJL-4.2's record above commits this engine to.
- **The third-party notices** name the two citations the numerics record found, `JSMath.Clz32`'s
  Jurassic reference and `SumPrecise`'s port of the proposal's algorithm, whose engine copy is gone,
  and correct what they said of arm J.
- **Left, as the area verdict leaves them:** the Temporal consumers' quantities (`PlainTime`,
  `PlainDateTime` and `ZonedDateTime`'s `round` round the nanoseconds since midnight) with SJL-10, and
  Intl's mathematical value with SJL-9. `UriHelper` cites Jurassic too, and the notices say that its
  derivation is not yet examined. *(2026-10-19: the Temporal consumers' rounding is corrected at milestone
  SJL-10.5, where `round` rounds a minute within its hour and a second within its minute; "The cut-over
  (SJL-10.5)" below, and the changelog's entry of that milestone. This item is unchanged.)*

**Text algorithms (SJL-5.1), filed 2026-10-14: all six sub-areas accepted.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0070-text-algorithms-move-to-the-shared-algorithms-library.md`
(JSD-0070), which states each condition for whitespace and UTF-16, padding and indices, substitution
templates, URI, base64 and hex, and JSON lexing, each on its own; its input, the correspondence from code
with every member by file and line and each difference numbered, is
`Broiler.VM/docs/evidence/sjl-5-001/correspondence.md`. SJL-5 started on 2026-10-14 by the owner's choice,
before SJL-3 is met, which the programme's ledger records. This repository's part of it:

- **The correspondence was read from code**, at this repository's `62c42e3`, because the area has no
  vectors yet; SJL-5.2's vectors confirm it. Trim is already the library's (SJL-3), and
  `trimCharacters` is gone. Everywhere else this engine holds more of the area than the design named:
  `AdvanceStringIndex` and six inline copies, a third template walk in `JSRegExp.Replace(string, string)`,
  and the relative-index normalisation inline in about twenty built-ins. Each joins the deletion list,
  and SJL-5.5 removes it.
- **The corrections**, each listed in `CHANGELOG.md` at SJL-5.5 under decision SJL-D-16 (a), with the
  correspondence's row: a pad or a repeat past .NET's largest string lets `OutOfMemoryException`
  through (P4); `%TypedArray%.prototype.lastIndexOf` ignores `fromIndex` with more than two arguments
  (P5); a named group's value is converted with the CLR's `ToString` (S2); a RegExp whose
  `Symbol.replace` is absent is matched as a pattern, not searched for as text (S3); `setFromHex`
  parses its whole input before honouring the destination (B1); a non-object options argument is
  ignored and a `null` option read as the default (B2, B3); a detached array encodes as empty (B4);
  JSON nesting past System.Text.Json's default depth is refused (J1); an escaped lone surrogate is
  refused and a raw one lets a .NET exception through (J2, J3); indented JSON
  uses CRLF on Windows (J5); the raw-JSON brand is structural and forgeable (J6); and the brand's
  `rawJSON` property is not enumerable (J7).
  *Corrected 2026-10-14, at SJL-5.2:* the list read "`1e400` is refused (J4)" after the surrogates. Corpus
  revision 12 classifies this engine's first run on the text area, and its vectors `text/parse-number/0001`
  and `0002` show that `JSON.parse("1e400")` and `JSON.parse("-1e400")` answer the infinities, as the
  specification does: on .NET 10, System.Text.Json's `GetDouble` returns the infinity rather than refusing
  the text. J4 is not a correction of this engine, and the changelog does not list it.
- **Three corrections are this engine's own code, not shared:** `split`'s limit with an empty separator
  (X1), the ordinal comparison in `indexOf`, `includes`, `startsWith`, `endsWith` and the non-standard
  `contains`, which use the current culture today (X2), and the raw-JSON brand, which becomes an internal
  slot of this engine's object model (J6).
- **Two policies become parameters, and this engine states its own at SJL-5.5:** the longest string a
  pad builds, which maps to `RangeError` rather than to a .NET exception, and the JSON nesting depth,
  which System.Text.Json's default fixes today and nobody chose. Both are listed in the changelog.
- **Charging.** The shared members keep Broiler.VM's charging units (JSD-0070, decision 3), which costs
  nothing here, where no meter is passed.
- **Provenance.** `UriHelper` cites Jurassic's `GlobalObject.cs`, and the third-party notices say its
  derivation is not yet examined. **It is deleted, not moved**: the shared codec is written from the
  specification and from Broiler.VM's implementation. The notices drop the open entry when it is
  deleted, and say that it was deleted rather than resolved.
- **Decision SJL-D-25 has nothing to dispose of**: every member on this engine's deletion list is
  internal or private. *Corrected 2026-10-15, at SJL-5.5: one is public, `JSRegExp.Replace(string,
  string)`, so the decision has one member to dispose of; it is kept, its body over the shared
  template parser, as [`docs/public-api.md`](../public-api.md) records. The bullet read as above.*
- **Not shared, and staying here:** the JSON drivers (object building, the reviver, source-text
  records, `JSON.stringify`), `split`, `replace` and the evaluation of named groups, string search, and
  the option reading, receiver checks and errors around every codec. Outside the area, and left to their
  milestones as the correspondence names them: U+0085 as white space in the lexer and in `RegExp.escape`
  (SJL-14, SJL-11), `normalize`'s .NET exception on a lone surrogate (SJL-7), `Date.parse`'s white space
  and culture (SJL-6), and the tagged-template path's trimming of a raw segment (SJL-14).
  *Revised 2026-10-16, at SJL-7.1, by Broiler.VM's JSD-0074 (decision 6): U+0085 as white space in the
  scanner and in `RegExp.escape` is SJL-7's, because their white-space lists are on SJL-7.5's deletion list
  and their answer becomes the owner's `IsWhiteSpace`; the lexer and `RegExp.escape`'s other escaping stay
  SJL-14's and SJL-11's. The sentence read as above.*
- **The rules** are committed with this record, before any code of the area: test262 per path
  (`docs/evidence/sjl-5-001/decision-rule.md`, whose base is taken afresh at the commit that carries it,
  because the shared stream's version moved since SJL-4's after run) and the library-level reference
  arms (`docs/evidence/sjl-5-001/reference-arms.md`).

**The text vectors and the first runs (SJL-5.2), 2026-10-14.** Corpus revision 10 adds the area's vectors
(`conformance/vectors/text/`), and revision 11 widens the schema's clause ids, which four of their citations
need and this engine's validator refused. No text vector carries a `per-engine.broiler-js` expectation:
the only results the area leaves to the implementation are the two pads at Broiler.VM's string ceiling,
which hold that engine's. Each engine's first engine-level run on revision 11 is classified at revision
12, and both engines then classify every vector. This engine's differences confirm the rows of the
correspondence listed above, B1 to B4, J1 to J3, J6, J7, P5, S2 and S3, and SJL-5.5 answers each of them.
They refute J4 (above). P4, X1 and X2 have no vector: the vectors pad no further than 2^24 + 1 and test
neither `split` nor string search. J5's three vectors match because the run was on Linux, whose line
ending is LF. Broiler.VM's differences are its rows P1, P2 and S1, which SJL-5.4 answers. Every
difference the runs found is one the area verdict classified, so its falsifier does not fire. The runs are
retained in [`sjl-5-001/engine/`](../evidence/sjl-5-001/engine/). *(Revised 2026-10-14: the paragraph
gave the number of differences each engine's run found, which this plan does not hold; the bundle and
`conformance/vectors/REVISIONS.md` give them.)*

**The implementation (SJL-5.3), 2026-10-14.** The text area is in `Broiler.ECMAScript.Algorithms`
(`7af1424`, merged with Broiler-Platform/Broiler.JS#1028), with its adapter guide, and every text vector
the library answers passes at library level. The reference-arm comparison is met under
[`reference-arms.md`](../evidence/sjl-5-001/reference-arms.md) as revised before any run (bundle
`sjl-5-001` section 3). Its first run did not meet the rule, because of a defect of the classifier, not of
the library; the classifier was corrected and the whole comparison run again. Every difference it found is
a correction the area verdict names. For this engine they are `setFromHex`'s whole-input parse (B1), the
third template walk's `$<name>` (S3), JSON nesting past System.Text.Json's depth (J1) and the lone
surrogates (J2, J3), all answered at SJL-5.5; for Broiler.VM, the pad's refusal with an empty filler (P2).
The run under `en-US` lists the same differences as the run under the invariant culture. The preview
package of the shared stream's next version follows, before either cut-over.

**The cut-over (SJL-5.5), 2026-10-15.** This engine's text algorithms are the shared library's, the
stream's `0.1.0-preview.3` (`0db9042`), which Broiler.VM took the day before at SJL-5.4: the UTF-16
readings and `AdvanceStringIndex` with its six copies, `StringPad` and the relative indices, the
template walks, `UriHelper`, `Uint8Array`'s codecs, and `JSON.parse`'s System.Text.Json reading with its
exception mapping, `QuoteString` and the gap. Every correction the area verdict lists for this engine is
made and listed in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md) under decision SJL-D-16
(a): P4, P5, S2, S3, B1 to B4, J1 to J3, J6 and J7, with X1 and X2 in this engine's own `split` and
string search; J5 was corrected on 2026-10-14, and J4 was refuted at SJL-5.2. The two policies are
stated there: the longest String a pad or a repeat builds is the longest a .NET string can be, and
`JSON.parse` reads 512 levels, as Broiler.VM does; past either, a `RangeError`. A typed array's `with`
keeps its normalisation inline, because its two steps read two lengths. `UriHelper` is deleted, and the
third-party notices say so. The corpus's next revision retires the fields these corrections answer.
*Added 2026-10-15:* corpus revision 13 retires them, with Broiler.VM's five.

**A data decision: each locale's default numbering system is CLDR's (SJL-5.6), 2026-10-15.** The owner
decided on 2026-10-15 that this engine takes each locale's default numbering system from CLDR, release
47.0.0, where every locale defaulted to `latn`: the table is generated in this repository by
`scripts/intl/generate_default_numbering_systems.py` from the `cldr-json` release's `cldr-numbers-full`
package, pinned by its integrity, and embedded in `Broiler.JavaScript.BuiltIns`, as decision SJL-D-6 (c)
has it for locale data; SJL-9 moves it into the shared locale data. The same change makes a Temporal
value's digits follow the numbering system, as a `Date`'s did. Bundle `sjl-5-001`'s first after run
found the need: `intl402/DateTimeFormat/prototype/format/temporal-objects-no-time-clip-non-latin-numerals.js`
passed at the base only because `String.prototype.includes` compared by culture, and correction X2
showed this engine's Latin digits for `ar-EG`. This is the dated data decision part A's class C5 of the
bundle's rule names: a row whose verdict changes because of it is listed there with this decision, and
in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md).

**Date (SJL-6.1), filed 2026-10-15: all four sub-areas accepted, and decision SJL-D-19 recorded.**
Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0072-date-algorithms-move-to-the-shared-algorithms-library.md`
(JSD-0072), which states each condition for the time arithmetic, the civil-day helpers, the Date string
grammar and the Date string renderings, each on its own; its input, the correspondence from code with every
member by file and line and each difference numbered (T, D, G, L, P, R), is
`Broiler.VM/docs/evidence/sjl-6-001/correspondence.md`. SJL-6 began with SJL-6.0, a Broiler.VM workflow that
changes no product code, before SJL-3 is met, which the programme's ledger records. This repository's part
of it:

- **The correspondence was read from code**, at this repository's `bc3597f`, and every cell of this
  engine's that a row rests on was run through the shell in UTC and in `America/New_York`; SJL-6.2's vectors
  confirm it. **Four of the area design's differences are already gone**: SJL-4.5 made `TimeClip` fold -0,
  `MakeTime` and the setters truncate, and the setters convert large arguments without a wrap, and
  `Broiler.JS/CHANGELOG.md` lists them there; they are not SJL-6's. Everywhere else this engine holds more
  of the area than the design named: the civil-day arithmetic in nine types (`JSTemporalInstant`,
  `JSTemporalPlainDate`, `JSTemporalPlainDateTime`, `JSTemporalPlainMonthDay`, `JSTemporalPlainYearMonth`,
  `JSTemporalZonedDateTime`, `TemporalIsoString`, `TemporalArithmeticCalendar`, `TemporalNonIso`) and in
  `JSDate.DaysInMonth`; MakeFullYear inline in three places; and Intl's dozen reads of `JSDateMath`. Each
  joins the deletion list, and SJL-6.5 removes it, with the dead `JSDate.ToDateTime`, `invalidDate` and
  `IsValid(JSValue, ...)`.
- **The corrections**, each listed in `CHANGELOG.md` at SJL-6.5 under decision SJL-D-16 (a), with the
  correspondence's row: a date-only string read as local time in years 0001 to 9999 (P1); a month at or
  past 2^53 that saturates (T4); a setter that writes the invalid date before converting its later
  arguments (T3); the ISO week of a date in a negative year (G2); two offsets for one zone, the base one
  for storage and the components constructor and the DST-aware one for `toString` (L2); no disambiguation of
  skipped and repeated local times, and the offset before a transition read after it (L3); the offset of
  the moment of the call for a date outside `DateTimeOffset`'s range (L4); this engine's renderings outside
  0001 to 9999 that `Date.parse` cannot read back (P9); and the culture's minus sign in `toString`'s
  `GMT-hhmm` (R3).
- **Date's storage becomes the time value**, and five defects of the `DateTimeOffset` storage go with it,
  each listed in the changelog: a valid date outside 0001 to 9999 refused by twelve setters (D1), a date
  stored raw that those setters cannot change (D2), the instant 0001-01-01T00:00Z read as invalid (D3),
  `getTimezoneOffset` without a raw path (D4), and a structured clone that loses a raw date (D5). They are
  this engine's own code, not shared.
- **Decision SJL-D-19 is recorded here**: the shared library defines the local-time provider and the
  specification's `LocalTime` and `UTC` over it, and **this engine's provider wraps the host's
  `TimeZoneInfo`**, read at each instant with its daylight-saving time, which corrects L2 to L4. Its
  `Temporal` reads its bundled tzdata, so `Date` and `Temporal` read different data until SJL-8 gives the
  provider the shared IANA data; the milestone's "Either way" is corrected there. The clock and locale
  providers of the same decision are defined at SJL-9 and SJL-10.
- **The order of grammars**: `Date.parse` calls the shared strict dialect, then this engine's own fallback
  (`DateParser`'s `N/N/N` heuristic, its other format lists and its culture parses, which stay under
  SJL-D-17), then Broiler.VM's dialect for what both refuse, which reads this engine's renderings outside
  0001 to 9999. `TryParseExtendedIso` and `DateParser`'s lists of the format are deleted. **Whether the
  fallback keeps what only they accepted beyond the format** - fractions of zero or of four to nine
  digits, lower-case `t` (P4, P6) - is decided at SJL-6.5 and listed in the changelog.
- **Charging.** The shared parser keeps Broiler.VM's charging units (JSD-0072, decision 4), which costs
  nothing here, where no meter is passed.
- **Decision SJL-D-25.** `DateParser` is public, but its `Parse` is internal, so it keeps its type and has
  nothing to forward. `JSDate`'s public constructor from a `DateTimeOffset`, its `Value` and `DateTime`, the
  unused public `epoch`, `JSValue.CreateDate` and `ClrProxy.Marshal`'s date overloads keep their signatures
  over the time value, as derived views; what each answers outside 0001 to 9999 is decided and recorded in
  [`docs/public-api.md`](../public-api.md) at SJL-6.5.
- **Not shared, and staying here:** storage, argument conversion and its order, the culture-dependent
  fallback and the .NET format string of `toLocale*` (SJL-D-17), and the CLR converters, whose
  `DateTime.Parse` reads a guest string into a host type and is not `Date.parse`. Outside the area, and left
  to their milestones as the correspondence names them: `Temporal`'s grammar and limits (SJL-10), the
  bundled tzdata and the non-ISO calendars (SJL-8), and Intl's `ArgumentOutOfRangeException` for
  `formatToParts(8.64e15)` (SJL-9).
- **The rules** are committed with this record, before any code of the area: test262 per path in UTC and
  in `America/New_York`, the latter the non-UTC host run of clause 4, each taken by `test262 evidence`
  with its new `time_zone` input (`docs/evidence/sjl-6-001/decision-rule.md`), and the library-level
  reference arms (`docs/evidence/sjl-6-001/reference-arms.md`).

**The Date vectors and the first runs (SJL-6.2), 2026-10-15.** Corpus revision 14 adds the area's vectors
(`conformance/vectors/date/`), from the reference computation `scripts/conformance/dateref.py`, written from
the specification's text and the Temporal proposal's ISO calendar and tested against Python's proleptic
Gregorian calendar. No vector's outcome depends on the time zone, so each one holds in UTC and in
`America/New_York` alike; this engine's runner was run in both, from one build. The numeric slash dates are
the area's `impl-defined` vectors, with this engine's expectation from its `N/N/N` heuristic, which SJL-D-17
keeps, and this engine meets it. Each engine's first engine-level run on revision 14 is classified at revision
15, and both engines then classify every vector. This engine's differences confirm the rows of the
correspondence listed above, T3, T4, G2, L2, P1 and P9, and D1 to D3; SJL-6.5 answers each of them. Rows L2
and P1 show only in New York, and under UTC their fields are reported as no longer holding, which retires none
of them: a field goes when its correction is listed and the run in New York shows it gone. Two rows are
refined, and none is refuted: the vectors show T3, which was worked from the code, for seven setters, while
`setUTCHours`, `setUTCFullYear` and `setFullYear` convert as the specification says; and L2's mechanism shows
also in the years before the zone's standard time, where the tzdata's offset is local mean time. L3, L4, L5,
D4, D5, R3 and R4 have no vector, nor have P4 to P7 beyond the slash dates: their outcome depends on the zone,
the culture or the host, or lies outside ECMA-262, and part B's run and the reference arms carry them.
Broiler.VM's differences are its rows T4, G3 and P9, which SJL-6.4 answers. Every difference the runs found is
one the area verdict classified, so its falsifier does not fire. The runs are retained in
[`sjl-6-001/engine/`](../evidence/sjl-6-001/engine/), and the bases of both parts of this repository's test262
rule were taken at its commit, `5fef116`, by `test262 evidence`.

**The implementation (SJL-6.3), 2026-10-15.** The Date area is in `Broiler.ECMAScript.Algorithms`
(`c69b70c`, with two defects the comparison's first run showed fixed at `b75d12e9`): `EcmaTime`,
`IsoCalendarMath`, the local-time provider with UTC's disambiguation, `EcmaDateParser` in the strict dialect
and in Broiler.VM's, and `EcmaDateFormatter`, with its adapter guide; every Date vector the library answers
passes at library level, and the vectors whose outcome is an engine's own, the setters' conversions and the
local setters, are engine-level only. The reference-arm comparison is met under
[`reference-arms.md`](../evidence/sjl-6-001/reference-arms.md) as revised before any run and, on two points,
after its first judged run was seen, which is weaker evidence; the bundle states one judgement, on arm V's
negative control (bundle `sjl-6-001` section 3). The first run did not meet the rule: the library answered
`Day(-0)` with -0 and reset an earlier clock's seconds in Broiler.VM's dialect, the local-time arbiter's
boundary at 1913 did not hold, and the classifier had defects; the library was fixed, the rule revised, and
the whole comparison run again. Every correction it found is a row the correspondence names, or refines: for
this engine, the ISO week in negative years (G2), local time at a transition (L3) and past .NET's range (L4),
and its own renderings read back (P8, P9), with the fraction of a time value or a year dropped or kept, and,
under `fa-IR` and `ar-SA`, a read-back through the culture's calendar, a second way P8 fails; for Broiler.VM,
`Day(-0)`, a fractional year and its renderings of negative years read back (P9). The other differences are
strings outside the format that each engine reads its own way. Corpus revision 16 holds each of their kinds in
an `impl-defined` vector, with this engine's reading before its cut-over as its expectation, and this engine's
runs on it, in UTC and in New York, meet every one. The preview package of the shared stream's next version,
`0.1.0-preview.4`, follows, before either cut-over.

**Broiler.VM's cut-over (SJL-6.4), 2026-10-15.** The stream's next version is `0.1.0-preview.4` (`e04874d`),
packed from this repository and admitted to Broiler.VM's committed feed before it is published; publishing
it is the owner's to choose. Broiler.VM's Date area is the library's, its getters and setters keeping its
UTC provider's identity, and its corrections T4, G3 and P9 are made; its pin of the corpus moves to revision
16, where each of its sixteen divergence fields reports that it no longer holds, and its record is its
JSD-0073. This engine's cut-over, SJL-6.5, follows on the same version; a later corpus revision retires
Broiler.VM's fields, with this engine's. *Added 2026-10-15:* on the owner's instruction, `Publish shared
libraries` published `0.1.0-preview.4` to nuget.org from `d1e4b11` (run 38813070829), whose library code is
`e04874d`'s, and tagged it `ecmascript-v0.1.0-preview.4`; Broiler.VM's feed now holds the package as
published.

**This engine's cut-over (SJL-6.5), 2026-10-16.** This engine's Date area is the shared library's, on the
stream's `0.1.0-preview.4`, which nuget.org serves. Its getters, setters, constructor, `Date.UTC`, `getYear`
and `setYear` call `EcmaTime`; its local time is `EcmaLocalTime` over its provider of the host's
`TimeZoneInfo`, `HostLocalTimeProvider` (SJL-D-19); `Date.parse` reads the strict dialect, then `DateParser`,
its own fallback, then Broiler.VM's dialect; its renderings are `EcmaDateFormatter`'s; and Temporal's and
Intl's civil-day arithmetic is `IsoCalendarMath`'s and `EcmaTime`'s. **A `Date`'s storage is its time value
alone.** The deletion list is absent, with what JSD-0072 added to it, and every correction the area record
names is made and listed in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md): those the corpus
holds, and D4, D5, L3, L4, R3 and P8, which `SharedDateAlgorithmTests` holds, its zone's cases under
`America/New_York`. **The readings the record left to this milestone are decided** (P4 to P6, R5):
`Date.parse`'s own grammar keeps the format's leniencies it read, a fraction of none to nine digits and an
offset without its colon, now in every form and year of the format; and a .NET format string of
`toLocaleString` formats the years 1 to 9999 only, and is a `RangeError` outside them. By the record's order,
`Date.parse` now reads in Broiler.VM's dialect what both of this engine's grammars refuse, a day past its
month's length among them, carried (P5). The CLR views' answers outside the years 1 to 9999 are recorded
in [`docs/public-api.md`](../public-api.md) under SJL-D-25. **One change the record did not name** follows
from the deletion: a Gregorian-family `Temporal.PlainMonthDay` with a year was bound-checked through a civil
copy that carried a month past 12 past the year, and is now bound-checked with its month as `constrain`
regulates it; the changelog lists it. The next corpus revision retires both engines' Date divergence
fields, and gives this engine's expectation of the strings outside the format its reading since the
cut-over; Broiler.VM's pin moves to it. *Added 2026-10-16:* corpus revision 17 does, and both engines'
expectations of two of those strings now agree on Broiler.VM's dialect's reading, which a library-level run
compares the library with: its runner reads a string outside the format as both engines ask the library
to, in the strict dialect and then, for what that refuses, in Broiler.VM's.

**Unicode (SJL-7.1), filed 2026-10-16: all six sub-areas accepted, placed with Broiler.Unicode, and
decision SJL-D-6 recorded for the first data area.** Broiler.VM's record is
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0074-unicode-moves-to-broiler-unicode-under-the-owner-conditions.md`
(JSD-0074), which states each condition of the extraction gate for properties and lexical classes, case
folding, case mapping, normalization, properties of strings and segmentation, each on its own; its input,
the correspondence from code with every member by file and line and each difference numbered (P, I, W, F,
C, N, S, G), is `Broiler.VM/docs/evidence/sjl-7-001/correspondence.md`. SJL-7 started before SJL-3 is met,
by the owner's choice, which the programme's ledger records. This repository's part of it:

- **The home is Broiler.Unicode**, under decision SJL-D-1 (b), because it adopts SJL-7.1's four owner
  conditions by a dated record in its `docs/roadmap.md`: the programme's conventions, its roadmap, an
  assurance gate, and generator inputs that are pinned and hashed. It meets them at SJL-7.2. **If it does
  not meet one by the end of SJL-7.2**, the sub-areas that condition reaches come to this repository, under
  `Broiler.ECMAScript`, by a dated record naming the condition, as the table above says.
- **The correspondence was read from code**, at this repository's `bdb1a3ca`, whose engine sources are
  `4d70821e`'s, and every cell of this engine's that a row rests on was run through the shell, on the
  host's ICU 74.2, which carries Unicode 15.1, as the hosted runners' does; SJL-7.3's vectors confirm it.
  **Two sub-areas are adoptions here, not moves**: this engine implements no normalization of its own, and
  takes graphemes from the runtime and has no sentence segmentation, so pinned data and the owner's
  algorithms replace the host's. **Its case folding under `u` and `v` is already Broiler.Regex's**, from the
  same derivation as Broiler.VM's, and the two agree; folding without `u` is the .NET translator's (F2),
  which SJL-11 retires.
- **The corrections**, each listed in `Broiler.JS/CHANGELOG.md` at SJL-7.5 under decision SJL-D-16 (a), with
  the correspondence's row: `\p{…}` names matched loosely, `Katakana_Or_Hiragana` refused, a name falling
  through to .NET's `\p`, and General_Category refused inside a `v`-mode set operation (P1 to P4);
  identifiers and group names that do not subtract Pattern_Syntax, and group names without the Unicode 17
  tables (I1, I3); U+0085 as white space to the scanner, `RegExp.escape` and the segmenter's words (W1);
  U+00AA and U+00BA not Cased (C1); the Turkic lower case through the runtime's older data (C4); four
  Soft_Dotted code points missing (C5); More_Above across a class-0 mark (C6); a list of locales that
  reaches `CultureInfo` and throws (C7); `null` and no argument read as the host's culture (C8); a lone
  surrogate that throws in `normalize` and `localeCompare` (N1); and sentence and word boundaries that do
  not follow UAX #29 (G1, G2). **Pinned data replaces the host's**, a correction of class C5 under
  SJL-D-6, for the identifier classes (I2), the default case mappings (C3), normalization at Unicode 16
  and 17 and under invariant globalization (N2, N3) and grapheme clusters (G3); and `isWordLike` becomes
  the owner's word status (G4).
- **The deletion list grows**, as JSD-0074 records: beside the milestone's, `IsValidNativeFunctionToStringName`'s
  `char.IsLetter`, the collator's `IsNormalized`, and the translator's loose property lookup with its
  `NormalizeKey`, which becomes a call to the owner's exact lookup while the translator itself stays
  SJL-11's. **Already gone**: `JSString.trimCharacters` and `NumberParser`'s list, since SJL-3.5, whose
  white space is the shared set in `trim`, `Number` and `parseFloat`.
- **Decision SJL-D-6 (c), as this engine applies it**: this engine references the owner's code packages
  directly, where today it reaches `Broiler.Unicode.Properties` only through Broiler.Regex while its code
  calls that package and the emoji package directly. It embeds the tables the owner's generator writes, in
  an assembly SJL-7.5 names, held byte for byte by a test of rule SL7's shape in this repository, and passes
  them to the owner's algorithms through the owner's provider. It references no package that carries
  tables.
- **Charging.** The owner's casing, normalization and segmentation take a meter and a maximum result
  length. This engine passes no meter, as with every shared area, and passes its own string ceiling as the
  maximum, keeping its own error.
- **Decision SJL-D-25.** `StringSpan.Trim`, `TrimStart` and `TrimEnd` are public and have no caller; their
  disposition is decided at SJL-7.5 and recorded in [`docs/public-api.md`](../public-api.md).
  `CharExtensions`' members are internal.
- **Not shared, and staying here:** receiver and argument conversion; this engine's locale
  canonicalisation and the mapping of a locale to the owner's language enumeration; its string ceiling and
  errors; `Intl.Segmenter`'s objects and iterators, whose shape differs from Broiler.VM's (G7) and is
  outside the area. Outside the area, and left to their milestones: the .NET translator and its folding
  and line terminators (F2, W5; SJL-11), the lexer (SJL-14) and collation, whose decomposition step alone
  moves here (SJL-17).
- **JSD-0031 §4(b)'s fifth reason**, the owner's runtime cost, is answered for this engine by its own
  measurement of `normalize`, casing and identifier scanning. If its lane is not a controlled lane, the
  part is void, as SJL-3's was, and the reason stays open for this engine.
- **The rules** are committed with this record, before any code of the area: test262 per path with
  negative-metadata tests included, so that the property-name and identifier corrections are inside the
  population, through `test262 evidence`'s new `include_negative` input, and the measurement over
  `UnicodeBenchmarks` (`docs/evidence/sjl-7-001/decision-rule.md`, parts A and B). The library-level rules,
  Unicode's own test files, the byte-for-byte generation and the reference arms, are the owner's, in
  Broiler.Unicode's bundle of the same name.

**Broiler.VM's cut-over (SJL-7.4), 2026-10-16.** Broiler.Unicode published `0.1.0-preview.4` of its packages
to nuget.org from its `6e14f93`, on the owner's instruction, and Broiler.VM's committed feed holds them as
published. Broiler.VM's Unicode area references the owner's code packages; its embedded tables are the owner's
generator's, from a pin of its own held to the owner's; its deletion list is gone; and its record is its
JSD-0075. The corpus at revision 19 names no divergence of Broiler.VM's, and its pin matches every vector
there after the cut-over, as before it.

**This engine's cut-over (SJL-7.5), 2026-10-16.** This engine's Unicode area is Broiler.Unicode's, on
`0.1.0-preview.4` of `Broiler.Unicode.Algorithms` and `Broiler.Unicode.Segmentation`, which nuget.org serves.
**Decision SJL-D-6 (c) is applied as recorded**: [`data/unicode/pins/`](../../data/unicode/pins/README.md) is
a byte-for-byte copy of the owner's pin, the tables the owner's generator writes from it are embedded in
this engine, in `Broiler.JavaScript.Runtime` at this cut-over and in `Broiler.JavaScript.Ast` since SJL-12.5
(`Unicode/JSUnicodeTableData.g.cs`), and `UnicodeTableRegenerationTests`, in `Runtime`'s tests then and in
`Ast`'s since SJL-12.5, runs the generator and compares its output byte for byte, as rule SL7 does for the
owner. This engine references no package that carries tables directly; `Broiler.Unicode.Properties` and the
emoji package still arrive in the closure through Broiler.Regex's dependencies until SJL-11, and no code of
this engine calls them. *(Revised 2026-10-25, at SJL-12.5's records: item 4 of SJL-12.5 moved the tables, the
same data with the two header comment lines that name the test project changed, and their test, with its
namespace and the paths and header lines it compares, from `Runtime` to `Ast` (Broiler-Platform/Broiler.JS#1066,
`792ff56e`). The sentence read "the tables the owner's generator writes from it are embedded in
`Broiler.JavaScript.Runtime` (`JSUnicodeTableData.g.cs`), and `UnicodeTableRegenerationTests` runs the generator
and compares its output byte for byte".)*
- **The deletion list is absent**, with what JSD-0074 added to it: the search for its members finds nothing,
  and the search for host Unicode APIs finds each of its hits outside the areas covered (bundle
  [`sjl-7-001`](../evidence/sjl-7-001/README.md), section 3, which lists them by milestone).
- **Every correction is made and listed** in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md), with
  its row and its vectors: P1 to P4, S2, I1, I3, W1, C1, C4 to C8, N1, G1 and G2, and, by decision, I2, C2,
  C3, N2, N3, G3 and the word status G4. On corpus revision 19 each of the 3,678 divergence fields reports that
  it no longer holds, and nothing is unclassified.
- **A property escape's name is checked by the parser too**, an early error for a literal, by the same exact
  lookup the `RegExp` constructor calls before Broiler.Regex or the translator reads the pattern. Inside a
  negated class the parser does not refuse a property of strings, because refusing a direct member would
  refuse valid patterns; the constructor still refuses it, and seven negative tests of test262 stay failed.
- **Policies decided here**: a result longer than this engine's strings hold is a `RangeError`, the owner
  taking `JSString.MaximumLength` as its maximum result length; the default language of `toLocaleUpperCase`
  and `toLocaleLowerCase` is this engine's, never the host's culture, while Intl's other services keep the
  host's culture until SJL-9.
- **Decision SJL-D-25**: `StringSpan.Trim`, `TrimStart` and `TrimEnd` stay, obsolete, as forwarders to the
  shared white-space set, and `Broiler.JavaScript.Ast` now references `Broiler.ECMAScript.Algorithms`
  ([`docs/public-api.md`](../public-api.md)).

The next corpus revision retires this engine's 3,678 divergence fields of the Unicode area, and Broiler.VM's
pin moves to it. *Added 2026-10-17:* corpus revision 20 (`a34b1f78`) does, and every vector of it has the result
its class expects in both engines' runs.

**The evidence (SJL-7.6), 2026-10-17.** Bundle [`sjl-7-001`](../evidence/sjl-7-001/README.md) reads this
engine's clauses at the after commit `a34b1f78`, whose product sources are `c7a19d35`'s:
- **Clause 7, test262 per path, is met under part A of its rule as written**: of the whole suite's 106,058 rows,
  with negative-metadata tests, 140 changed, each from failed to passed and each a correction of class C1 under
  rows P1, P3 and I1, and none regressed.
- **Clauses 5, 6 and 8 are met** as the bundle's section 5 reads them, with findings named for SJL-11, SJL-14 and
  the CLR binding.
- **Part B, the measurement, is void**: this repository's machine is not a controlled lane. Its arithmetic finds
  `Normalize` about three times and `Casing` about twice as slow after the cut-over, far beyond the A/A floor,
  and `Identifiers` unchanged. **JSD-0031 §4(b)'s fifth reason stays open for this engine**, and the plan's first
  risk, that the owner's normalizer and casing are slower than ICU and the runtime, is what the run shows.

**After the evidence, 2026-10-17.** Two changes have reached the default branch since SJL-7.6. Neither edits
bundle `sjl-7-001`, which reads clauses 5 to 8 at `a34b1f78`.
- **Two of the bundle's findings are fixed**, in [#1041](https://github.com/Broiler-Platform/Broiler.JS/pull/1041)
  (`f001ff09`):
  - the CLR binding's names are lowered with invariant casing, not the current culture's, with tests under
    `tr-TR` (`e8cc0532`);
  - a regular-expression literal's early errors now refuse what the constructor refuses: group names that are not
    a `RegExpIdentifierName`, a name declared twice in one alternative, an incomplete `\k`, and, under `v`, a
    negated class that may contain strings (`06ace97f`).
- **The owner's algorithms were made faster rather than the cost accepted.** Broiler.Unicode moved normalization,
  case mapping and the lexical classes onto constant-time tables and published them as `0.1.0-preview.5`.
  [#1042](https://github.com/Broiler-Platform/Broiler.JS/pull/1042) (`6563786e`) moves this engine to it and
  regenerates the embedded tables from the same pin. The owner's bundle shows its answers unchanged on the code the
  engines now run, and its changelog gives its own benchmarks' figures.

**Part B is not taken again here.** On this machine it would be void as the first run was, so it would decide
nothing. Its arithmetic describes `0.1.0-preview.4`, not the code this engine now runs. JSD-0031 §4(b)'s fifth
reason stays open for this engine until a controlled lane runs part B, at whatever commit is then current.

**Time-zone and calendar data (SJL-8.1), filed 2026-10-17: all three sub-areas accepted, decisions SJL-D-20 and
SJL-D-21 recorded, and tzdb 2026e pinned as the release both engines adopt.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0076-time-zone-and-calendar-data-move-to-the-data-home.md`
  (JSD-0076). It states each condition of the extraction gate for the tz database, the zone algorithms and the
  calendar tables, each on its own.
- **Its input** is `Broiler.VM/docs/evidence/sjl-8-001/correspondence.md`: the correspondence from code, read at
  this repository's `a1a96ca2`, whose engine sources are `6563786e`'s. It names every member by file and line and
  numbers each difference (Z, A, K). Every cell of this engine's that a row rests on was run through the shell, and
  every probe and answer is retained beside it.
- **SJL-8 started before SJL-3 is met**, at the owner's direction, which the programme's ledger records.

This repository's part of it:
- **This repository is the data home** (SJL-D-1 (b-iii)). SJL-8.2 brings in:
  - the tz archive's pin, CLDR's `bcp47/timezone.json` for the primaries, and the ICU4X crates' pin, each a copy of
    Broiler.VM's;
  - Broiler.VM's tz compiler, table generators and calendar table generator, moved here;
  - byte-for-byte regeneration (rule SL7) and public version constants.
- **No package carries the tables** (SJL-D-6 (c)). This engine embeds what the generator writes in `BuiltIns`,
  until its satellite plan (A-2) makes them optional, as Broiler.VM embeds them in its `.Intl` assembly. The
  decoder, the zone algorithms and the calendars' year sources join `Broiler.ECMAScript.Algorithms`, under
  `Broiler.ECMAScript.Time`.
- **The release is tzdb 2026e**, IANA's newest on 2026-10-17 and the one Broiler.VM's pin records. This engine's
  2025b is copied from a build host and cannot be regenerated (Z15). The policy is SJL-D-20's:
  - no backzone;
  - recurring rules after the last listed transition;
  - CLDR primaries;
  - no `Factory`;
  - ASCII case-insensitive lookup.

  The calendar sources are SJL-D-21's: ICU4X's pinned crates, and no .NET calendar class.
- **The correspondence found the two engines' zone algorithms in agreement** on every input inside the data both
  share. Every difference is the data's, three of this engine's input checks, or the host zone.
- **The corrections**, each listed in `Broiler.JS/CHANGELOG.md` at SJL-8.6 under decision SJL-D-16 (a), with the
  correspondence's row:
  - lookup by invariant upper-casing, which admits `America/Loſ_Angeles` (Z10);
  - an ISO date-time string taken as the formatter's `timeZone` (Z11);
  - `getTimeZones` from a hand table of fourteen regions (Z12);
  - an offset with non-ASCII digits surfacing a .NET `FormatException` (A13);
  - a host zone returned unvalidated (A16);
  - `chinese` 2057, inside Table 1's published range (K2);
  - far lunisolar dates that throw or give impossible months (K7);
  - Persian dates repeated at .NET's bounds (K12).

  **Two more are deletions of code no input reaches**: the zero offset for an unknown name, and start of day's
  local-midnight fallback (A6, A11).
- **The policy changes**, listed in the changelog with their decision:
  - the release (Z1, Z2, Z15);
  - backzone (Z3);
  - the 2037 freeze, which also held southern zones on daylight time (Z4, A5, A7, A10);
  - `Factory` (Z5);
  - the primaries (Z7);
  - the calendar years from ICU4X rather than the runtime and the CLDR package's astronomy (K4, K5, K6, K11, K14).
- **What stays this engine's:**
  - the formatter's resolved `timeZone`, which keeps the name, as `canonical-tz` asks, until the specification
    edition both engines pin settles it (Z8);
  - zone display names (SJL-9);
  - the calendars' arithmetic (SJL-10);
  - the host zone (SJL-D-19).

  The local-time provider may read the shared data for the host's zone (milestone SJL-8.6), which ends `Date` and
  `Temporal` reading one zone from two datasets (A15).
- **The deletion list**:
  - `Temporal/Tz/IanaTimeZone`, `IanaTimeZoneDatabase` and `TzifReader`;
  - `tzdata.bin` with its `EmbeddedResource` item, and `scripts/tzdata/generate_tzdata.py`;
  - `JSTemporalZonedDateTime`'s offset, candidate, disambiguation, start-of-day and transition helpers;
  - `JSIntl`'s offset-zone normalisation and its region table;
  - the formatting engine's zone helpers;
  - `TemporalLunisolarCalendar`, `TemporalSolarHijriCalendar`, and the Umm al-Qura and .NET dispatch of
    `TemporalArithmeticCalendar` and `TemporalCalendarMath`;
  - this engine's uses of `CldrChineseCalendar` and `CldrUmmAlQura`.

  **Every one is `internal`**, so no public type is emptied (SJL-D-25).
- **The rule**, [`docs/evidence/sjl-8-001/decision-rule.md`](../evidence/sjl-8-001/decision-rule.md), is committed
  before any code of the area. Its parts are:
  - test262 per path in UTC;
  - test262 per path in `America/New_York`;
  - Broiler.VM's reference datasets `timezones.js` and `temporal.js`, run in this engine against Broiler.VM's
    answers.

  Its admitted classes name every correction and policy change above by row. Both test262 bases are taken at the
  rule's commit with negative tests, as bundle `sjl-7-001`'s were.

**The data home and the algorithms (SJL-8.2 and SJL-8.3), 2026-10-17.**
- **The data home** is [`Broiler.ECMAScript/data/time/`](../../Broiler.ECMAScript/data/time/README.md), with its
  generator `Broiler.ECMAScript.Generation`, a test-only package. It holds IANA tzdb 2026e's data tarball, CLDR
  48.2.0's `bcp47/timezone.json`, and ICU4X's `icu_calendar` 2.3.0 and `calendrical_calculations` 0.2.4 crates.
  - Each pin and archive is a byte-for-byte copy of Broiler.VM's at `9508bd5`.
  - The four tables the generator writes from them are byte for byte the data Broiler.VM's generated files hold
    at that commit. Rule SL7 regenerates them.
  - The `zic` comparison passes across 1800 to 2037.
  - `TimeDataVersions` names the four versions.
  - **No package carries a table or an input** (SJL-D-6 (c)).
  - Bundle [`sjl-8-001`](../evidence/sjl-8-001/README.md), section 1, reads the six data-home clauses.
- **The algorithms** are in `Broiler.ECMAScript.Algorithms`, under `Broiler.ECMAScript.Time`:
  - the database and rules interfaces, and the decoder of the home's tables over the bytes an engine hands in;
  - offset zones;
  - the zone algorithms, each failure a status, and start of day never falling back to local midnight;
  - the table-backed calendars' years.

  Each is ported from Broiler.VM's code. A differential check against that code, over its own tables, made
  82,514,651 comparisons and found no difference (bundle `sjl-8-001`, section 2).
- **Nothing is cut over yet.** Broiler.VM's cut-over (SJL-8.5) follows on the shared stream's next version, and
  this engine's (SJL-8.6) after it. SJL-8.4's vectors and the reference datasets' copies come first.

**The vectors and the reference datasets (SJL-8.4), 2026-10-17.**
- **Corpus revision 21** adds the time-zone area: 3,969 vectors, every expected value from `scripts/conformance/tzref.py`.
  That reference compiles the pinned tzdb 2026e with the host's `zic`, reads the result with its own reader and
  applies SJL-D-20's policy. It shares no code with the generator or the library, which answers every vector.
- **Broiler.VM** matches every vector, at revision 21 and at revision 22.
- **This engine's 1,161 differences** are classified at revision 22, every one under a row of the area verdict:
  - 1,113 policy differences under SJL-D-20: the release, backzone, the 2037 freeze, `Factory` and the primaries;
  - 48 corrections: Z10, Z11, Z12 and A13.

  SJL-8.6 answers each one. The vectors falsify nothing in the verdict.
- **The reference datasets.** Broiler.VM's `timezones.js` and `temporal.js`, with its answers and divergences, are
  copied byte for byte into [`conformance/datasets/time/`](../../conformance/datasets/time/README.md), held by their
  digests. The harness [`scripts/conformance/run_reference_datasets.py`](../../scripts/conformance/run_reference_datasets.py)
  builds the expected answers as Broiler.VM's slice checks do.
  - At the rule's commit, `timezones.js` answers 51 of its 610 lines as Broiler.VM expects, and `temporal.js` 1,622
    of 1,629.
  - The differences are the policy rows, Z8, zone display (SJL-9's), and one `Duration.round` outside the area.
- **The bases of all three parts** of bundle [`sjl-8-001`](../evidence/sjl-8-001/README.md)'s rule are taken at its
  commit:
  - test262 in UTC and in `America/New_York`, with negative tests, each complete;
  - the reference datasets.

**This engine's cut-over (SJL-8.6), 2026-10-17.** This engine's time zones and table-backed calendars are the
data home's, read through `Broiler.ECMAScript.Time` on the shared stream's next version, `0.1.0-preview.5`, which
`BuiltIns` takes by project reference and nuget.org does not yet serve. **Decision SJL-D-6 (c) is applied as
recorded**: `BuiltIns` embeds the five files of
[`Broiler.ECMAScript/data/time/generated/`](../../Broiler.ECMAScript/data/time/README.md), linked and not copied,
and `SharedTimeZoneTests` holds each embedded file to the home's bytes and to the manifest's digests.
`TemporalTimeData` hands them to `TimeZoneDatabase` and `CalendarTables` once, on first use.
- **The deletion list is absent**, as this plan lists it. The search for its members finds them only in records,
  in the tests that assert their absence, and in corpus revision 22's divergence fields and reasons, which
  describe the engine before the cut-over. `TemporalArithmeticCalendar` and `TemporalCalendarMath` stay without
  their Umm al-Qura and .NET dispatch, as the list says: they hold the formula calendars, which are SJL-10's and
  which the library does not have yet.
- **Every correction and policy change is made and listed** in
  [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md), with its row and its vectors:
  - the corrections Z10, Z11, Z12, A13, A16, K2, K7 and K12;
  - the deletion of the two unreached fallbacks, A6 and A11;
  - the policy changes: the release (Z1, Z2, Z15), backzone (Z3), the 2037 cliff (Z4, A5, A7, A10), `Factory`
    (Z5), the primaries (Z7) and the calendar years (K4 to K6, K11, K14).

  On corpus revision 22, run at `76ec819e` in UTC and in `America/New_York`, whose reports are byte for byte the
  same, each of the area's 1,161 divergence fields reports that it no longer holds, and nothing is unclassified:
  92,430 of 92,434 vectors match, and the other 4 are the number area's classified fields. Before the cut-over,
  2,808 of the area's 3,969 vectors matched.
- **The host's zone (SJL-D-19)** is `TimeZoneInfo.Local`'s IANA name resolved through the shared database to its
  primary identifier. `Date`'s local time, `Temporal.Now` and the default `Intl.DateTimeFormat` zone all read it,
  which ends A15. **Decided here**, since neither the verdict nor the correspondence names one: a host zone the
  database does not hold reads as `UTC` (A16). `Date`'s names stay the host's display names, chosen by the shared
  daylight flag. The CLR views `JSDate.Value` and `DateTime` keep the host's offset, since a `DateTimeOffset`
  holds whole minutes only.
- **Two further readings are this engine's**: the `offset` of a property bag, of `with` or of a `relativeTo` bag
  is read by the library's `UTCOffset` grammar, so A13's correction holds there too; and `ZonedDateTime.round` to
  a time unit no longer computes the day's start, which it never used.
- **Two regressions the local test262 subset found, fixed before the records**:
  - `Duration.prototype.round` with a zoned `relativeTo` at the instant limit and no rounding requested built a
    next-day boundary past the limit, which the shared library refuses as the proposal does; it now returns the
    difference unrounded, as DifferenceZonedDateTimeWithRounding does (`022d3074`, one path).
  - A `chinese` or `dangi` `PlainMonthDay`'s reference search read the years 1898 to 2101, where ICU4X's years
    hold month-days that Table 6 of the Intl era and month code proposal says none holds; it now reads Table 6's
    span, 1900 to 2035 (`eb2ab599`, four paths).
- **The reference datasets**, run by part C's harness with the shell built at `eb2ab599`, which is not the rule's
  judged run; its answers equal the run at `76ec819e`, line for line:
  - `timezones.js` answers 84 of its 610 lines as Broiler.VM expects, where the base answered 51. Each of the
    other 526 differs only by the formatter's resolved identifier (Z8), by the `longOffset` name's dropped
    seconds (Z16, SJL-9's), or by both: 24, 374 and 128 lines. No line differs any more by the release,
    backzone, the 2037 cliff, `Factory`, the primaries or the regions.
  - `temporal.js` answers 1,622 of its 1,629 lines, and its 7 others are the base's, unchanged.
- **A local test262 subset**, not parts A and B's evidence: the area's 7,627 paths, negative tests included,
  from `built-ins/Date`, `built-ins/Temporal`, `intl402`'s `Date`, `DateTimeFormat`, `Intl/supportedValuesOf`,
  `Locale`, `Temporal` and `TimeZone`, and `staging`'s Temporal directories, run locally with the shell built
  at `eb2ab599`:
  - in UTC and in `America/New_York`, each path's verdict equals its verdict in that zone's base, taken by
    `test262 evidence` at the rule's commit: 7,622 pass, and the five `proto-from-ctor-realm` paths that fail
    at the base fail after;
  - the base shell, run the same way in UTC, gave the hosted base's verdict on every path;
  - the earlier runs, at `76ec819e` (in part) and `022d3074`, found only the two regressions above; none
    remains.
- **Not done here**: the bundle's after runs (SJL-8.7); the corpus revision that retires the area's 1,161
  divergence fields; and publishing `0.1.0-preview.5`, which this engine's packages need before they ship.

*Added 2026-10-17, later:* on the owner's instruction, `Publish shared libraries` published
`Broiler.ECMAScript.Algorithms` and `Broiler.ECMAScript.Generation` `0.1.0-preview.5` to nuget.org from `a04cd44`
(run 39064131506), whose library code is `2d37a38`'s, and tagged them `ecmascript-v0.1.0-preview.5`. Broiler.VM's
feed holds the packages as published, and the source's suffix moves to `preview.5`, so this engine's packages
depend on the version nuget.org serves.

*Added 2026-10-17, later again:* corpus revision 23 (`2d496a55`) retires the area's 1,161 divergence fields, and
every vector of it has the result its class expects in this engine's runs in UTC and in `America/New_York`, whose
reports are byte for byte the same (bundle [`sjl-8-001`](../evidence/sjl-8-001/README.md), section 4), and in
Broiler.VM's, whose pin moves to it.

**2026-10-18: row Z16 comes into SJL-8.6, at the owner's direction.** Part C's after run at `5f96cd40`, the commit
that merged the cut-over, was observed before any after run was judged:
- `temporal.js` answered as at the base.
- `timezones.js` answered 84 of its 610 lines as Broiler.VM expects. Of the 526 others, 24 differed only by
  the resolved identifier (Z8, class D1), 374 only by the `longOffset` name's dropped seconds (Z16, class D2),
  and 128 by both.
- Part C admits a line whose difference is *only* one of those, so the 128 fall in no class, and part C was
  not met as written there.

Offered a dated revision of part C that would admit them (weaker evidence), leaving the clause open until
SJL-9, or making the correction now, the owner chose the correction.
- **What changes.** An offset's GMT name keeps its seconds (`Broiler.JS/CHANGELOG.md`, 2026-10-18). With it,
  `timezones.js` answers 458 lines as Broiler.VM expects, and the 152 others differ by Z8 alone.
- **The after commit.** The rule's after runs are "at the commit that lands SJL-8.6's cut-over". With Z16 part
  of SJL-8.6, that is the commit that lands this correction. All three parts are taken there.
- **The rule itself is not revised.** D2 stays in it and admits what it admitted.
- **Not judged.** The runs at `5f96cd40` are recorded in bundle `sjl-8-001` as observed and not judged.
- **What else moves.** Z16 leaves milestone SJL-9's list. Broiler.VM's formatter already writes the seconds,
  so nothing moves there.

**The evidence (SJL-8.7), 2026-10-18.** Bundle [`sjl-8-001`](../evidence/sjl-8-001/README.md) reads this
engine's clauses at the after commit `79a0506c`, the merge that lands row Z16's correction, the last change of
SJL-8.6, whose product sources are `4d558ee1`'s. **Every clause of this engine's, 1 to 11, is met**, each part of
the rule as written:
- **The data home's clauses 1 to 6 still hold**: the generator's `PinTests`, rule SL7's regeneration, the `zic`
  comparison, the public versions in the API baseline and SL1 to SL10 all pass, and the pins are Broiler.VM's byte
  for byte. The packages nuget.org serves at `0.1.0-preview.5` hold the after commit's library code, and no entry
  of either holds a table's or a pinned archive's bytes.
- **Clause 9, test262 per path, is met under parts A and B**: in UTC and in `America/New_York`, with
  negative-metadata tests, each of the whole suite's 106,058 rows gives its base's verdict. No row changed, so none
  regressed and no class lists a row. The runners' tzdata, 2026c, did not move between the bases and the after
  runs.
- **Clause 10, the reference datasets, is met under part C**: `timezones.js` answers 458 of its 610 lines as
  Broiler.VM expects, and each of the other 152 differs only by the formatter's resolved identifier (Z8, class D1);
  `temporal.js`'s 7 are an unchanged `Duration.round` (D3).
- **Clauses 7, 8 and 11 are met** as the bundle's section 6 reads them: the deletion list is absent as JSD-0076's
  X6 reads it, with `TemporalArithmeticCalendar` and `TemporalCalendarMath` kept for the formula calendars;
  `Temporal` names no .NET calendar and reads only the host zone's name from the host; and every correction and
  policy change is listed in the changelog, Z16 among them.
- **Every vector of corpus revision 23** has the result its class expects at the after commit, in both zones, and
  a pristine consumer of the engine runs fifteen of SJL-8.6's corrections and policy changes from its own package
  cache, where the rule's commit fails every one.
- **The runs at `5f96cd40`** are recorded in the bundle as observed and not judged, and the rule is not revised.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 12 to 17 are judged in its own
bundle, and SJL-8 may not meet its gate before SJL-3 meets SJL-3's.

**The Temporal core (SJL-10.1), filed 2026-10-18: all seven sub-areas accepted, `tc39/proposal-temporal@e8cc03fc`
the revision both engines follow, and the owner's decisions of 2026-10-18 recorded.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0078-the-temporal-core-moves-to-the-shared-algorithms-library.md`
  (JSD-0078). It states each condition of the extraction gate for records and limits, balancing, durations and
  rounding, differences, the grammar, calendars and serialization, each on its own.
- **Its input** is `Broiler.VM/docs/evidence/sjl-10-001/correspondence.md`: the correspondence from code, read at this
  repository's `d3464413`. It names every member by file and line and numbers each difference (R, B, D, F, G, C, S).
  Every cell of this engine's that a row rests on was run through the shell, the probes of the four core sub-areas
  also under the proposal's polyfill, and every probe and answer is retained beside it. It was written in two halves
  on 2026-10-18 and merged.
- **SJL-10 started before SJL-6 and SJL-8, which it waits on, met their gates**, at the owner's direction, which the
  programme's ledger records. SJL-10 may not meet its gate before they meet theirs, and so not before SJL-3 meets
  SJL-3's.
- **The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
  review." Each decision the correspondence left open is taken as it recommends, and recorded as the owner's dated
  decision, D1 to D9 in JSD-0078; and the programme proceeds through SJL-10's work packages without pausing for the
  owner between them. **It changes no rule**: nothing is accepted without review, and the gate waits as above. In
  this record a decision is "decision Dn" and a row of the correspondence "row Dn".

This repository's part of it:
- **The sub-area verdicts.** Each sub-area moves to `Broiler.ECMAScript.Algorithms`, beside SJL-8's records in
  `Broiler.ECMAScript.Time`:
  - **records and limits**: the ISO records are SJL-8's `IsoDate`, `IsoTime` and `IsoDateTime`, with no parallel types
    (decision D8), beside the duration records; every limit check takes any year an engine hands it and answers a
    status (decision D9);
  - **balancing**: balancing, regulation, addition and the rounding of points, with `overflow`'s rule;
  - **durations and rounding**: validity, sign, the default largest unit, addition, comparison, rounding with nudging
    and bubbling, and totals with exact fractions, over calendar and time-zone provider interfaces, with the units,
    increments and rounding modes as rules;
  - **differences**: `until` and `since` for every type, through the same providers, with the difference settings as
    rules;
  - **the grammar**: a linear parser of the proposal's grammar, a `TryParse` that answers a record or an error code;
  - **calendars**: the formula calendars, the arithmetic over SJL-8's table years, month codes, eras, and the
    reference dates with the proposal's Table 6;
  - **serialization**: every string form, with the rules of its options.

  This engine keeps its `JSTemporal*` objects, the reads in specification order, coercion, `relativeTo` resolution,
  its errors, and `Temporal.Now`'s clock and host zone (SJL-D-19). Intl formatting of Temporal objects is SJL-9's.
  SJL-10.3 settles the library's names.
- **The proposal revision this engine now follows is `tc39/proposal-temporal@e8cc03fc`** (decision D1), the Stage 4
  state of 2026-07-27, which Broiler.VM has followed since its JSD-0053. Until now this engine pinned none. Its
  behaviour was that revision's on every marker probed but two, both corrected below: the era names (C9) and
  `Duration.prototype.add`'s read of a `relativeTo` the revision no longer has (D3). Its calendars follow the era and
  month code proposal's Tables 1, 2, 3 and 6 as they stand at `5833eae`, as Broiler.VM's do. A later revision moves
  both engines together, by a dated record.
- **The area's rules run test262 `ccaac100`** (decision D2), the suite this repository's workflows run. A move to the
  proposal's own submodule, `227d9055`, is a separate dated change outside SJL-10.
- **Where the pinned text and its polyfill disagree, the text rules** (decision D7). The one case found is row D8.
  *(SJL-10.2's vectors find 45 more, in the polyfill's parser and era table; see "The vectors (SJL-10.2)" below.)*
- **The corrections**, each listed in `Broiler.JS/CHANGELOG.md` at SJL-10.5 under decision SJL-D-16 (a), with the
  correspondence's row:
  - **the milestone's listed ones, as the rows confirm them:**
    - lenient grammar acceptances: G3, G4, G6, G8, G9, G11, G12, G13, G16, G17, G18, G19, G20 and G22;
    - integer narrowing in arithmetic, which wraps at four sites and saturates at three (R7), and an ISO month-day's
      year past 2^31, which saturates and loses its leap year (C12);
    - `Duration.prototype.add` with calendar units (D3);
    - rounding options ignored in non-ISO differences (F4);
    - day rounding on 24-hour days in `ZonedDateTime` `until` and `since` (F7). `round`, `hoursInDay` and rounding
      relative to a zoned date already use real day lengths (D9);
    - invalid month codes silently constrained (C5);
    - reference years computed without the proposal's table, which move to the table with no answer changed (C10);
  - **found beyond them:**
    - a year-month subtraction at the lower limit (B5);
    - `compare`'s range check (D4);
    - rounding to months re-derived through a clamped difference (D6, D7);
    - the nudge after a shifted window, where this engine answers P1Y, P11M or P10M29D by path and the text P11M (D8,
      decision D7);
    - a `relativeTo` in another calendar (D11);
    - the calendar aliases `gregorian` and `minguo`, refused as the pinned revision and Broiler.VM refuse them (C2,
      decision D5);
    - month codes with digits other than ASCII (C6);
    - era names: the era codes of the pinned revision are adopted, and the codes from before the era proposal's change
      of 2025-06-30, the aliases it removed on 2025-11-21, and `incarnation` are refused (C9, decision D4);
    - the calendar a `PlainMonthDay` lends (C13);
    - the three conversions: `toPlainYearMonth`'s day, `toPlainMonthDay`'s calendar in the Gregorian family, and
      `toPlainDate`'s year in `buddhist` and `roc` (C15, C16, C17).
- **The policy changes**, each listed in the changelog with its decision: the proposal revision (decision D1), and the
  text where it and its polyfill disagree (decision D7). Neither changes an answer the corrections above do not list.
- **Code no input reaches is deleted with its file**: the zoned machinery's shift of years and months, its
  "inconsistent Instants" branch and its fourth day correction (D12); the handling of U+2212 after a match (G10); the
  `islamic` entry (C3) and the reference-date search's upper year (C10); the offset getter's dropped fraction of a
  second (S8); `RejectIncompatibleStyle`, `NotImplementedArithmetic`, `ReadLargestUnit` and the unused
  `AddYearsMonths`. *(SJL-10.5 finds the "inconsistent Instants" branch reached, by Pacific/Apia's skipped day, and
  decision D12 governs it; the rest is deleted, much of it from files that remain rather than with its file. See
  "The cut-over (SJL-10.5)" below.)*
- **The deletion list**, as JSD-0078's X6 states it (the correspondence's section 11):
  - the per-type validity, regulation, addition and balancing copies, and the small copies beside them:
    `CompareISODate`, `FloorDiv` and the unit-length tables;
  - the three separate relative-rounding implementations, `compare`'s arithmetic, and `AddSubtract` with its "not yet
    implemented" `Error`;
  - `JSTemporalRoundingOptions`' arithmetic. Its rules become the library's, and its reads stay as adapter code;
  - every type's difference copies and the settings' rules, each type keeping only its reads, which become one
    adapter;
  - the eight per-type regular-expression parsers, the string fallback of `CanonicalizeTimeZone`, and
    `TemporalIsoString`, whose coercing members become adapter code and whose dead `RejectIncompatibleStyle` is
    removed;
  - the per-type formatters, the `toString` bodies' precision arithmetic, and the five `ReadCalendarName` copies, which
    become one adapter;
  - `TemporalNonIso`'s calendar parts, whose coercions and property reads become adapter code;
  - **by decision D3, the other calendar copies**: `TemporalArithmeticCalendar`, `TemporalTableCalendar`'s arithmetic
    over the shared years, `TemporalCalendarMath`, the Gregorian family's identifiers, years and era tables in
    `JSTemporalCalendar.cs`, and the five per-type month-code copies. SJL-8 kept the first and the third "for the
    formula calendars" (JSD-0076's X6, and SJL-8.7's reading of clause 7); SJL-10 takes the formula calendars, so they
    go. The milestone's SJL-10.5 list is revised to name them, with its superseded text quoted.

  **Every type in it is `internal` or `private`**, so no public type is emptied. The public `JSTemporal*` classes stay
  as shells (SJL-D-25).
- **Broiler.VM's one correction**, row R6, is made at its cut-over, SJL-10.4 (decision D9). This engine's
  constructors already answer as the proposal does there.
- **The rule**, [`docs/evidence/sjl-10-001/decision-rule.md`](../evidence/sjl-10-001/decision-rule.md), is committed
  before any code of the area. Its parts are:
  - test262 per path in UTC, with negative-metadata tests;
  - test262 per path in `America/New_York`;
  - Broiler.VM's reference datasets `temporal.js` and `temporal-calendars.js`, run in this engine against Broiler.VM's
    answers. The first is in [`conformance/datasets/time/`](../../conformance/datasets/time/README.md) since SJL-8.4;
    the second is copied there at SJL-10.2.

  Its admitted classes name every correction above by row. Both test262 bases are taken at the rule's commit with
  negative tests, as bundle `sjl-8-001`'s were.

**The vectors (SJL-10.2), 2026-10-18.**
- **Corpus revision 24** adds the Temporal area: 10,878 vectors under decision D10. Every expected value is the
  proposal's own polyfill's at `e8cc03fc`, run under Node 22.22.0 by `scripts/conformance/temporalref.mjs`, with its
  npm packages pinned. It shares no code with either engine.
- **Decision D7 is applied to 73 vectors**, each listed in the generator. Each takes the pinned text's answer and
  records the polyfill's:
  - row D8's nudge, 28;
  - 45 where the polyfill's own parser and era table read the pinned texts otherwise: a separator of two spaces, an
    offset's separator with no digit, the month-days `02-30` and `04-31`, year-month and month-day calendar strings,
    time zone strings that are a time or a partial date, the era aliases removed on 2025-11-21, and a regnal era read
    leniently. The correspondence did not run its grammar and calendar probes under the polyfill, so it could not
    see them.

  **So "The one case found is row D8" above no longer holds.** Broiler.VM answers as the texts on all 45, and this
  engine on 35. Its other 10 answer as the polyfill does and are corrections already listed (G17, C9), so no class
  changes.
- **Seven vectors keep the polyfill's answer** at ComputeNudgeWindow, where Broiler.VM's JSD-0054 section 4 records
  that the text does not do what it means. The vectors read decision D7 as naming row D8; the owner's record should
  say whether it reaches these.
- **Not vectored**, where the polyfill reckons through ICU4C or JavaScript's `Date` and so is no source:
  - the table calendars outside Table 1's published years;
  - the Hebrew years before AM 1, and 5806 and 5807;
  - a non-ISO calendar's fields at the ISO limits.
- **No library answers the area yet**: SJL-8.4's library-level runner, `942f8cd3`, has no counterpart until SJL-10.3
  adds the Temporal code. *(SJL-10.3 adds it: "The library (SJL-10.3)" below.)*
- **Revision 25** classifies every difference under a row of the area verdict:
  - this engine's 473, in both zones, all corrections SJL-10.5 answers;
  - Broiler.VM's 6, from a local run, all row R6, which SJL-10.4 answers.

  The vectors falsify no class. They refine row D11: a Coptic `ZonedDateTime` `relativeTo` is rounded in ISO months,
  not refused. They also find a sixth case of row R6, which counts five: `new PlainDateTime` with a year of 2^53 + 2.
  Bundle [`sjl-10-001`](../evidence/sjl-10-001/README.md), section 2, has the runs.

**The library (SJL-10.3), 2026-10-18.** The Temporal core is in `Broiler.ECMAScript.Algorithms`, under
`Broiler.ECMAScript.Temporal`, written in three parts and joined on one branch. Its adapter guide is
[`adapter-guide-temporal.md`](../../Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/docs/adapter-guide-temporal.md).
- **Part A** holds the records and limits, balancing, the grammar and serialization, with their options' rules.
- **Part B** holds the calendars, with `ICalendarArithmetic`, the calendar provider. Its private limits were unified
  onto part A's when the parts were joined, with no answer changed.
- **Part C** holds the durations, rounding and differences:
  - `DurationMath`, with and without `relativeTo`, and the relative machinery over exact rationals;
  - `TemporalDifferences`, `until` and `since` of every type;
  - `ITimeZoneArithmetic`, the zone provider, with `TemporalTimeZone` over SJL-8's algorithms;
  - `RelativeTo`, and the rules of the rounding and difference options.
- **Each part is ported from Broiler.VM's code**, and held to it:
  - Broiler.VM's recorded answers to the correspondence's calendar, duration and difference rows, computed at
    library level: 4,782 lines, every one Broiler.VM's;
  - both retained datasets at library level, every line as expected;
  - a differential check against its code at `14072b0`: 76,814,791 comparisons, no difference but row D12's case
    below.
- **The library-level runner answers the Temporal area**, as SJL-8.4's `942f8cd3` answers the time-zone area. Of corpus
  revision 25's 10,878 Temporal vectors, it answers 10,851, and every one matches. The other 27 are engine-level: a
  built-in's `length`, a getter's reads, and the calendar an object lends (row C13). The Native AOT probe expects
  them so, and its report is byte for byte the JIT's.
- **Row D12 is reached.** The correspondence read the specification's assertion, that a week or day nudge window
  holds the destination, as code no input reaches. A zone's skipped day reaches it: Pacific/Apia's 2011-12-30
  leaves a day window back from 2011-12-31 with no length.
  - Broiler.VM's code ends the invocation with a .NET exception there.
  - This engine answers `PT0S`, from its own day arithmetic, which never reaches the assertion.
  - The reference polyfill fails its own assertion.
  - The correspondence's open point on row D12 let the library assert or answer a status. It answers `OutOfRange`,
    a `RangeError` in both engines. JSD-0078 records this as the owner's decision D11 of 2026-10-18, taken under
    the same direction as decisions D1 to D10.

  It is the one place the library does not answer as Broiler.VM's code does. Broiler.VM's answer there is a
  correction its cut-over (SJL-10.4) makes. This engine's moves from `PT0S` to a `RangeError` at SJL-10.5, a policy
  change its changelog lists then.

  *(Note, 2026-10-19, at SJL-10.5. "This engine answers `PT0S`" and "moves from `PT0S` to a `RangeError`" hold of one
  input among several. With `a` 2011-12-31T16:15+14:00[Pacific/Apia] and `b` 2011-12-31T00:00 the same day, exactly
  these moved to a `RangeError`, in each of the nine rounding modes:*
  - *`a.since(b)` and `a.until(b)` with largest and smallest unit `day`, which answered `PT0S` or one day by mode
    (`since`: `PT0S` under `trunc` and `floor`, `P1D` in the other seven; `until`: `PT0S` under `trunc` and `ceil`,
    `-P1D` in the other seven);*
  - *`a.since(b)` with largest unit `month` and smallest unit `day`, which answered `PT0S` under `trunc` and
    `floor` and `P1D` in the other seven;*
  - *`-PT16H15M` rounded to days relative to `a`, and its total in days, which threw a plain `Error` carrying a .NET
    `ArgumentOutOfRangeException`, not `PT0S`.*

  *`b.until(a)` and `b.since(a)` in days, windows of weeks, differences in hours and everything relative to `b`
  answer as before. Rounding to a time unit with a date largest unit back from `a` answered a `RangeError` before and
  after; that is decision D12, in "The cut-over (SJL-10.5)" below. Bundle `sjl-10-001`'s `cutover/d12-ext.js` probes
  five of the modes, and `cutover/d12-half-modes.js` the other four.*

  *"It is the one place the library does not answer as Broiler.VM's code does" held of the library as SJL-10.3
  wrote it. Decision D12 makes a second, NudgeToZonedTime's day span: back from `a`, with a time unit rounded under a
  date largest unit, the library now answers `OutOfRange`, where Broiler.VM's code and the library at
  `0.1.0-preview.6` answer durations. The differential check ran before the decision and found no difference
  there. The sentences above are left as written.)*
- **The decisions taken** are in the guide, under each part, within JSD-0078's X3. Part C's: the design's names; the
  zone provider one zone per instance, where the design passed a zone with each call, as an offset zone has no
  identifier; `RelativeTo` carrying its providers; a new status, `RelativeToRequired`; the zones' equality left to the
  adapter; the year-month difference taking the calendars themselves; and row D12's status.
- **Nothing is cut over yet**, and the shared stream's version is unchanged. Broiler.VM's cut-over, SJL-10.4, sets the
  next version, as SJL-8.5's `2d37a38a` did, and this engine's, SJL-10.5, follows it. Bundle
  [`sjl-10-001`](../evidence/sjl-10-001/README.md), section 3, has the evidence.

**The cut-over (SJL-10.5), 2026-10-19.** This engine's Temporal core is the shared library's
`Broiler.ECMAScript.Temporal`, which `BuiltIns` takes by project reference: the stream's `0.1.0-preview.6`, which
nuget.org has served since 2026-10-18, with decision D12, below, which the stream's next version carries. It was
written on branch `sjl-10-5-js`, from `main` at `1c411b68`:
- part A, records, limits, balancing, the grammar and serialization: `f42eac6b`, `3503cab5`, `808d7838`;
- part B, calendars: `9242c342`, `b34f8c30`;
- part C, durations, rounding and differences: `8df9227e` to `41eaa90f`;
- the review's fixes, `3dd4626e` to `b72aefdd`, and decision D12, `163d43aa` and `6dbff805`.

Then:
- **The deletion list is absent**, as JSD-0078's X6 and this plan's SJL-10.1 record list it, the formula and table
  calendar copies among them (decision D3). The search for its members' names (bundle `sjl-10-001`,
  [`cutover/deletion-search.txt`](../evidence/sjl-10-001/cutover/deletion-search.txt)) finds them only as:
  - the adapter's thin members named after the specification's operations, each a call into the library;
  - comments that name those operations;
  - tests' comments on the engine before the cut-over;
  - a library record's member, `TimeNanoseconds`, which the adapter reads;
  - Intl's own `ResolveCalendar`, a different member of the same name.

  No file of `BuiltIns/Temporal/` uses a regular expression. The public `JSTemporal*` types stay as shells
  (SJL-D-25), and the engine's public API listing is unchanged ([`docs/public-api.md`](../public-api.md)).
- **The built-ins keep** the reads in specification order, coercion, `relativeTo` resolution, their errors,
  `Temporal.Now`'s clock and host zone, and Intl's formatting, which is SJL-9's. The adapter is one,
  `TemporalAdapter`, by concern, with `TemporalTimeZones.Arithmetic` the library's zone provider over SJL-8's rules.
- **Every correction is made and listed** in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md), each with
  its row and the vectors, dataset lines or probes that name it:
  - corpus revision 25's 473 fields, by row: R7 14, B5 5, D3 9, D4 3, D6 7, D7 23, D8 7, D11 15, F4 20, F7 11, G3 7,
    G4 6, G6 2, G8 12, G9 45, G11 13, G12 48, G13 15, G16 4, G17 32, G18 16, G19 4, G20 6, G22 3, C2 12, C5 29,
    C6 23, C9 24, C12 11, C13 3, C15 30, C16 12 and C17 2;
  - the reference datasets' lines, 7 of `temporal.js` (D6) and 103 of `temporal-calendars.js` (C5, C15, C16, C17,
    F4, D11);
  - C10, whose reference years are now Table 6's with no answer changed, and the code no input reaches, deleted
    (G10, C3, S8, and D12's shift of years and months and its fourth day correction).

  **Row D12's "inconsistent Instants" branch**, which the SJL-10.1 record lists as code no input reaches, is
  reached. Pacific/Apia's skipped day reaches it, as it reaches decision D11's window, and this engine answered a
  `RangeError` from it at the base. Decision D12 keeps that answer.
- **The changes no row lists are kept**, under the owner's direction of 2026-10-18: part A's two, part B's seven,
  five of part C's six (its fifth, that `add` and `subtract` read no second argument, is row D3's), and the review's
  U1 to U8. So are the two defects at the base that the review fixed: a Temporal object read as a property bag
  lends its own calendar (code-1), and no .NET exception reaches script from `ZonedDateTime.from` of a plain type
  (code-2). Each follows the pinned text and is the library's answer, and so Broiler.VM's. Each is listed as a
  correction under SJL-D-16 (a).
  - **These records found one more**: the rounding of points rounds a minute within its hour and a second within its
    minute. `01:10` rounded to 20 minutes under `halfEven` is `01:00`, where it was `01:20`. It is the case the
    SJL-4.5 changelog entry's "Not corrected here" names for SJL-10.
  - **The probes:** [`cutover/kept-changes.js`](../evidence/sjl-10-001/cutover/kept-changes.js) has one for each,
    with the base's answer and this change's. On every changed line this engine now answers as Broiler.VM and the
    polyfill do.
  - **One divergence from the text is left as it was**: a string outside the limits is refused before the
    `overflow` option is read.
- **Decision D11** moves exactly the operations the note under "The library (SJL-10.3)" above lists, to a
  `RangeError`, and nothing else.
- **Decision D12**, the owner's of 2026-10-19, under the direction of 2026-10-18:
  - **The rule.** Decision D11's rule reaches NudgeToZonedTime's assertion on the day span's sign. Where an input
    breaks it, the library answers `OutOfRange`, a `RangeError`.
  - **This engine.** Its answer there was already a `RangeError`: only the message moves, from "time zone returned
    inconsistent Instants" to "result is outside the representable range". Without the decision, the cut-over would
    have moved 36 probe lines from a `RangeError` to durations from outside the text's domain: part C's shell, at
    `41eaa90f`, answers them so (`cutover/d12z.part-c.txt`).
  - **Broiler.VM** answers those durations on `0.1.0-preview.6` until it takes the stream's next version.
    ([`cutover/d12-facts.md`](../evidence/sjl-10-001/cutover/d12-facts.md) has every line in every engine.)
- **The checks at the cut-over's final commit, `6dbff805`.** The branch then took `main` in the merge `e2b2a785`,
  which changes only `Broiler.ECMAScript/CHANGELOG.md` and the version suffix in
  `Broiler.ECMAScript/Directory.Build.props`, and no code.
  - **Build:** `Broiler.JS.slnx` builds in Release with no new warning.
  - **Unit tests:** `BuiltIns`' tests, the integration tests and the library's tests all pass.
  - **test262:** the local Temporal selection gives every path its base's verdict, in UTC and in
    `America/New_York`.
  - **The reference datasets:** every line of both programs equals its expected answer.
  - **The correspondence's probes:** every line is Broiler.VM's answer, but the five of row R6, which is Broiler.VM's
    own correction.
  - **The vectors:** on corpus revision 25, in both zones, the 473 fields report that they no longer hold, nothing
    else moves, and the two reports are byte for byte the same.

  Corpus revision 26 (`feee5b7a`) retires the area's 479 fields, this engine's 473 and Broiler.VM's 6. This engine's
  runs on it (`c97a9996`) match every Temporal vector in both zones. Bundle
  [`sjl-10-001`](../evidence/sjl-10-001/README.md), section 4, has the evidence.
- **Not done here**:
  - SJL-10.6's after runs of the rule's three parts, at the commit that lands this change;
  - Broiler.VM's pin move to revision 26;
  - publishing the stream's next version, which carries decision D12.

**The evidence (SJL-10.6), 2026-10-19.** Bundle [`sjl-10-001`](../evidence/sjl-10-001/README.md) reads this engine's
clauses and the home clauses at the after commit `00b65dd0`, the merge (Broiler-Platform/Broiler.JS#1054) that lands
SJL-10.5's cut-over and corpus revision 26, whose product sources are `6dbff805`'s but for the two changelogs and the
shared stream's version suffix. **The three home clauses and this engine's four are met**, each part of the rule as
written:
- **Part A, test262 per path in UTC, is met**: with negative-metadata tests, the after run (39252310861) and the base
  (39123945598) are complete and comparable, every shard in the zone and their host-zone records alike, and every row
  of the whole suite gives its base's verdict. No row changed, so none regressed and class C1 lists no row; the
  classification file beside each report is empty.
- **Part B, the same in `America/New_York`, is met** on the same grounds (runs 39252316070 and 39123948554). The
  runners' tzdata, 2026c, did not move between the bases and the after runs. In each zone the one path that timed
  out, in both variants, timed out again when the workflow ran it alone, at the base and after, and the comparison
  took the re-runs.
- **Part C, the reference datasets, is met**: every line of `temporal.js` (1,629) and of `temporal-calendars.js`
  (3,999) equals its expected answer, Broiler.VM's, the 7 and 103 lines that differed at the base among them.
- **The deletion list is absent**: every entry, searched in the folder that held it, with the rule's commit as the
  control; nine files are gone whole.
- **Every correction is listed**: each the correspondence names for this engine, with its row, the 473 vectors'
  counts row by row and the datasets' 110 lines; the code no input reaches, the policy changes and the changes no row
  lists. Test262 adds none, as no verdict changed.
- **The home clauses hold** by the tests SJL-10.3 committed: every sub-area's, the library-level runner over the
  Temporal area with its control against the empty library, and both datasets at library level, in a run of the
  shared folder's 1,489 tests with none failed. The Native AOT probe runs corpus revision 26 with no failure.
- **The consumer**: a pristine consumer of the engine set packed from the after commit runs 24 of SJL-10.5's
  corrections and policy changes from its own package cache, where the rule's commit fails 23 of them.
- **The vectors at revision 26**: every vector has the result its class expects at the after commit, in both zones,
  the 473 whose divergence fields revision 26 retired among them.

**The shared library as published.** With the shared library taken from nuget.org's published `0.1.0-preview.6`
instead of the after commit's, this engine answers decision D12's case with a duration, where the after commit
answers a `RangeError`: decision D12 (`163d43aa`) is under the shared changelog's Unreleased heading. The stream's next
version carries it, and its publication is the owner's to approve; Broiler.VM takes decision D12 with it too. That is
a next action, not a verdict on any part: the parts judge the after commit, whose library holds the decision.

*Added 2026-10-19, later:* on the owner's approval of 2026-10-19, `Publish shared libraries` published
`Broiler.ECMAScript.Algorithms` and `Broiler.ECMAScript.Generation` `0.1.0-preview.7` to nuget.org from `5ddee404`
(run 39312237448), whose library code is the after commit's file for file, and tagged that commit
`ecmascript-v0.1.0-preview.7`. Decision D12 is in that version, and the shared changelog's entry for it is retitled
as published. With it restored from nuget.org in place of the after commit's library, the pristine consumer's check
of decision D12 holds, a `RangeError`, and so do the other 23 (bundle
[`sjl-10-001`](../evidence/sjl-10-001/README.md), section 11). The next action on decision D12 is Broiler.VM's: it
takes the decision with `0.1.0-preview.7`. The paragraph above is left as written.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses are judged in its own bundle of the
same name, and SJL-10 may not meet its gate before SJL-3, SJL-6 and SJL-8 meet theirs.

## The frontend contract (SJL-12)

SJL-12 is not an area milestone: it applies no extraction gate, so its record is here and not under "Area verdicts",
whose every entry applies conditions X1 to X7 (Broiler.VM's JSD-0080, decision D6). *(Added 2026-10-19, at
SJL-12.1.)*

**The frontend contract and the in-place preparation (SJL-12.1 and SJL-12.2), filed 2026-10-19: the contract
recorded, decisions SJL-D-11 and SJL-D-12 recorded, and the owner's decisions of 2026-10-19 taken.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0080-the-shared-frontend-contract-and-the-in-place-preparation.md`
  (JSD-0080). It records the contract, decisions SJL-D-11 and SJL-D-12, and the owner's decisions D1 to D42 of
  2026-10-19. The home's design document is
  [`Broiler.ECMAScript/docs/frontend/design.md`](../../Broiler.ECMAScript/docs/frontend/design.md). With this record,
  they are the three places SJL-12.1 files the contract. The programme's area designs,
  `Broiler.VM/docs/javascript-shared-libraries/areas/frontend.md` and `areas/static-semantics.md`, and JSD-0080 win
  over this summary and the design document, which are corrected to match.
- **Both front ends were read for it** at this repository's `24510b0e` and Broiler.VM's `046357b`. The two leaks
  corrected below were witnessed by probes against this engine's assemblies at `24510b0e`.
- **SJL-12 started on 2026-10-19 by the owner's choice.** That day the owner wrote "Continue with SJL-12 after the
  plan is ready", after the survey for SJL-12's plan had been started on a recommendation: SJL-12 feeds SJL-13 on
  the programme's critical path, and may start after SJL-0. The programme's ledger is to record it in an entry of
  its own, filed in Broiler.VM apart from this record *(corrected 2026-10-19, at SJL-12.1, after review; read "The
  programme's ledger records it.")*. **SJL-12 may not exit before SJL-3 does**, because the contract cites the
  literal-value operations.
- **The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting
  for review." Each decision the plan left open is taken as it recommends, and recorded as the owner's dated
  decision of 2026-10-19, D1 to D42 in JSD-0080. **It changes no rule**: nothing is accepted without review, and
  SJL-12's gate waits as above. In this section, "decision Dn" is always JSD-0080's.

This repository's part of it:
- **SJL-12 is not an area verdict** (decision D1). It writes no shared code, so conditions X3 to X6 have no subject.
  The frontend area's verdict is SJL-13.1's, which applies the gate.
- **The contract**, as JSD-0080 records it from the area design's section 4 with decision D41's specifics:
  - **input, an immutable parse request:** the source, with its name, base offset, line and column; the goal,
    `Script` or `Module`, with `Eval`, `FormalParameters` and `FunctionBody` reserved for both engines; switches for
    forced strictness, top-level `await`, Annex B and HTML-like comments; the inherited evaluation context, which is
    Broiler.VM's evaluation flags (`Strict`, `InFunction`, `InMethod`, `InDerivedConstructor`,
    `InClassFieldInitializer` and `InClassBody`), the visible private names and the caller's lexical names, which
    this engine needs for direct `eval`; and proposal flags, off by default;
  - **a lexer extension**, through which this engine's `m` suffix arrives; its `Decimal` meaning stays here
    (SJL-D-17);
  - **limits:** the nesting depth a bounded parameter, the tree depth a constant of the library, a diagnostic cap,
    and first-error or all-errors;
  - **providers:** Broiler.Unicode's lexical classes, and regular-expression syntax validation through a seam until
    SJL-11;
  - **output:** an immutable tree with full spans, raw facts and identity-keyed side tables, and no desugaring;
    diagnostics in a deterministic order; and the semantic model, whose design (`areas/static-semantics.md` section
    3) is filed in JSD-0080 and the design document;
  - **properties:** deterministic across runs and threads, no ambient state and no mutable statics, limits checked
    before the stack is, Native AOT compatible, and never a source-to-verified-artifact path.
- **The deep-nesting policy is decided** (decision D27). In both engines, every input deeper than the bound is
  refused with the contract's nesting diagnostic. The inputs this engine accepts today past the bound, through stack
  segmentation, become listed refusals at SJL-14, of class C3 (an implementation limit), enumerated from the parse
  corpus's baseline. The bound's value is SJL-13's measurement, and **SJL-12 changes no acceptance of this engine's**.
- **Decision SJL-D-11 is recorded**, as taken on 2026-10-11. The shared front end emits a stable code, a phase, a
  span, a source name and a message identifier with arguments. This engine renders its own V8-style text from them;
  Broiler.VM maps the codes onto its registry, with a new registry number for every meaning that would change
  (Broiler.VM's JSD-0009). Neither engine's guest-visible text is part of the contract. At SJL-12 Broiler.VM's
  registry does not change (decision D13).
- **Decision SJL-D-12 is recorded**, as taken on 2026-10-11: (a) the shared tree is a new immutable tree seeded from
  Broiler.VM's wide design, and (i) this engine's public `Ast` and `Parser` packages become adapter packages that
  build the old tree from the new one, marked obsolete and removed in a later major version. **The disposition is
  SJL-14.4's**, in [`docs/public-api.md`](../public-api.md).
- **The catalogue and the corpus have their homes here** (decisions D30 to D33):
  - the early-error catalogue, from SJL-12.3, at `Broiler.ECMAScript/docs/frontend/early-errors.tsv` beside the
    design document: one row per early-error rule of ECMA-262's ES2026 edition at `tc39/ecma262@0248456c`, with the
    families EA, EG, EP and EX, shared codes `JSE` and four digits, and dotted message identifiers;
  - the parse corpus, from SJL-12.4, at `conformance/parse/`, which Broiler.VM copies byte for byte.

  The mapping from shared codes onto Broiler.VM's registry is Broiler.VM's.
- **This engine's in-place preparation, SJL-12.5**, as the owner's decisions D19 to D29 set it:
  - **Explicit options** (decision D19). An immutable `FastParserOptions` in `Parser` carries the goal, top-level
    `await` and the providers to `FastScanner`, `FastTokenStream`, `FastParser` and `ParseModule`, and the parser
    refuses a stream built with other options. `JSCompilationOptions` gains `AllowTopLevelAwait`, which enters the
    code cache's key. `IJSCompiler` gains a default interface method, which forwards a script without top-level
    `await` and otherwise throws `NotSupportedException` naming the host's compiler. Every compile site passes
    options: the module context, `EvalWithTopLevelAwaitAsync`, `SyntaxValidation`'s and `DirectEvalSupport`'s
    re-scans, and every run-time compile, which is a script without top-level `await`. `CoreScript`'s goal scopes
    and the `InternalsVisibleTo` they need are deleted.
  - **No `Runtime` reference** (decision D20). The parser's metadata at `24510b0e` names these `Runtime` types:
    `CoreScript`, `CancellableDisposableAction`, `RegExpUnicodeValidator`, `JSUnicode`, `JSUnicodePropertyNames` and
    `RegExpGroupNames`. The milestone's "exactly three types" are the first three, as the area design names them.
    `CoreScript` becomes the options. The Unicode tables and their `IUnicodeTables` implementation, `JSUnicode`,
    `JSUnicodePropertyNames`, `RegExpGroupNames` and `RegExpUnicodeValidator` move to `Broiler.JavaScript.Ast`, which
    is already below `Runtime` and `Parser`, and which takes with them the package reference to
    `Broiler.Unicode.Algorithms` that `Runtime` holds today; the public validator keeps a `[TypeForwardedTo]` in
    `Runtime`. The options' providers are typed by Broiler.Unicode's `IUnicodeTables`, the `Ast`-held table is the
    default, and **no default throws**. `CancellableDisposableAction` moves to `Ast` with a
    forwarder, and `UndoMark` is marked `[Obsolete]` (SJL-D-25 (b)); removing it waits for a recorded check of its
    consumers. SJL-13 defines the shared provider, and the tables move once more then.
  - **One counter per parse, and an end-of-file token per scanner**, the milestone's items 2 and 3.
  - **The for-loop pass** (decision D22): a preparation pass of the compiler after `ParseProgram`, in `FastCompiler`
    and in `DirectEvalSupport`, before `SyntaxValidation`, emitting today's shapes exactly. The parser records
    whether a loop body holds a `continue`, which an AST walk can miss inside a `switch`. `HeadTdzNames` and the
    using-for block wrap move to the pass, and `ThrowIfLabelledFunctionLoopBody` stays in the parser. A temporary is
    named `<d>:<n>`, with `d` the direct-eval nesting depth the compilation inherits and `n` a counter of the pass
    from 1: deterministic, never an identifier, and distinct from an enclosing compilation's carriers. The three
    walkers leave the parser. The pass's entry point is the internal static method
    `ForLoopPreparation.Prepare(AstProgram program, int directEvalDepth)` of `Broiler.JavaScript.Compiler`, which
    returns the prepared tree. The rule names it, so that its parse-corpus tool, fixed before the pass exists, finds
    the pass by name *(added 2026-10-19, at SJL-12.1, after review)*.
  - **What "does not desugar" reaches** (decision D23): the for-loop desugaring. The synthetic block Annex B gives a
    function declaration that is the whole of an `if` clause (`FastParser.Statement.cs`), and the rewrite of a
    regular-expression literal's `\u{…}` escapes (`FastScanner.cs`), introduce no temporary and restructure no
    control flow. They stay until SJL-14, whose shared tree keeps raw facts.
  - **What "reads no ambient state" reaches** (decision D24): the `Parser` assembly's own declarations and
    references. `StackSegment`'s `[ThreadStatic]` flag and its environment variable, `CompilationStack`'s statics,
    `Ast`'s `FastToken.Empty` and `ArraySpan<T>.Empty`, and `FastCompiler`'s read of `JSEngine.Current` for direct
    eval decide where a parse runs or are the compiler's. The inherited context becomes an option at SJL-14.
  - **The architecture test** (decision D25): a new project, `Broiler.JS/Broiler.JavaScript.Architecture.Tests`, in
    `Broiler.JS.slnx`. It links the shared folder's `AssemblyFacts.cs`, compiles its witness in memory, reads the
    parser assembly `BROILER_ARCH_PARSER_ASSEMBLY` names when it is set, and asserts the exact list of today's
    violations, which each later item shrinks until it is empty. It has no rule register.
  - **The order** (decision D26): the architecture test, the end-of-file token and the counter, in one change; then
    the options; then the `Runtime` reference; then the for-loop pass, with the records.
  - **The public API** (decision D29): forwarders by default. `docs/public-api.md` and the changelog record at
    SJL-12.5 the new overloads, `JSCompilationOptions.AllowTopLevelAwait`, the `IJSCompiler` default method as a
    **behavioural** change for a host's own compiler, `UndoMark` obsolete, the `Parser` package's dependency on
    `Runtime` dropped, which breaks a consumer that reached `Runtime` through it at the source level, and
    `JSCode.Key`'s changed text.
- **The corrections J1 and J2** (decision D21), each listed by name in `Broiler.JS/CHANGELOG.md` at SJL-12.5 under
  decision SJL-D-16 (a). These names are SJL-12's, not the text area's correspondence rows J1 to J7.
  - **J1:** code compiled while `EvalWithTopLevelAwaitAsync` runs, but not as its own program, is compiled as the
    specification's goal without top-level `await`, as under `Eval`: direct and indirect eval,
    `ShadowRealm.prototype.evaluate`, the Function constructor and the debugger's evaluate. Today the permission
    reaches them through `CoreScript`'s ambient state.
  - **J2:** the code cache keys a compilation on its top-level-`await` permission. Today one context can answer a
    compilation from the other mode's entry, so the same text answers by which mode compiled it first.

  The message "Unexpected await" stays, because the shell's retry reads it
  (`Broiler.JS/Broiler.JavaScript/Program.cs`).
- **The Modernization items** (decision D28):
  - **MOD-M2-4 closes at SJL-12.5**, when item 4 removes the `Parser` → `Runtime` edge, with dated revisions of
    [`Modernization.md`](Modernization.md), [`Assemblies.status.md`](Assemblies.status.md) and
    [`docs/architecture/dependencies.md`](../architecture/dependencies.md) then.
  - **MOD-M2-5 is superseded in part**: its boundary specification by the contract, and its extraction plan by
    SJL-13 to SJL-15.
  - **MOD-M2-3 stays open, and is not SJL-12's.** No work package of SJL-12 touches the foundation cycle or
    `Storage`'s dependency on `Ast`. The cross-reference of 2026-10-12 in `Modernization.md` is corrected there, with
    its text quoted, and Broiler.VM's governance row J6 in the same way.

  The programme's roadmap section 11 asks of SJL-12 "MOD-M2-3, MOD-M2-4 and MOD-M2-5 closed or cross-referenced".
  The cross-references of 2026-10-12 met it; the notes of 2026-10-19 in `Modernization.md` say what each item now
  is.
- **The rule and the bases.** The rule,
  [`docs/evidence/sjl-12-001/decision-rule.md`](../evidence/sjl-12-001/decision-rule.md), is committed with this
  record, before any code SJL-12.5 changes (decision D8). Its parts are (decision D38):
  - test262 per path in UTC, with negative-metadata tests;
  - the parser's architecture test and its witness;
  - the parse corpus's acceptance, at the parser alone and at the whole compile;
  - the determinism of the parser and of the for-loop pass, over two runs in two orders and two threads.

  Its admitted classes name corrections J1 and J2, and a whole compile's crash that the preparation resolves, which
  the changelog names as a correction *(corrected 2026-10-19, at SJL-12.1, after review; read "Its admitted classes
  name corrections J1 and J2.")*. **The test262 base is taken afresh, at the rule's commit, with
  negative tests** (decision D7). It is not SJL-0's base, which predates every cut-over since SJL-3 and excludes
  negative tests; the milestone's SJL-12.5 done-when, which named SJL-0's base, is revised in Broiler.VM by a dated
  note on the work package (the programme roadmap's section 13), not on the gate, whose clause 2 names no base.
  Parts C and D's bases are taken at the commit that adds the parse corpus, under the conditions the rule states
  (decision D9). No part runs in a zone with daylight-saving time: nothing SJL-12.5 changes reads the host's zone.

**SJL-12.3 to SJL-12.5, merged 2026-10-20 to 2026-10-25: the catalogue, the corpus and the bases, and this
engine's in-place preparation.** *(Added 2026-10-25, at SJL-12.5's records.)* The list above is SJL-12.1's, and its
"today" is 2026-10-19's. What merged since:
- **SJL-12.3, the early-error catalogue** (Broiler-Platform/Broiler.JS#1061, merged as `68f9a87a` on 2026-10-20):
  [`early-errors.tsv`](../../Broiler.ECMAScript/docs/frontend/early-errors.tsv) beside the design document, with
  [`early-errors.md`](../../Broiler.ECMAScript/docs/frontend/early-errors.md), its extractor and its completeness
  check. Broiler.VM's mapping names it by commit and SHA-256.
- **SJL-12.4, the parse corpus** (Broiler-Platform/Broiler.JS#1062, `1fe542ff`, 2026-10-20, the rule's corpus
  commit): `conformance/parse/`, the bundle's tool and `scripts/compliance/compare_parse_corpus.py`.
- **The bases**, in bundle [`sjl-12-001`](../evidence/sjl-12-001/README.md): parts C and D's at the corpus commit,
  with part C's control and part A's first two dispatches, which are void (Broiler-Platform/Broiler.JS#1063,
  `17b91384`, 2026-10-20); and part A's at the rule commit, `test262 evidence` run 40196127730, the fifth dispatch,
  retained with the third and fourth, which are void (Broiler-Platform/Broiler.JS#1067, `2faf464b`, 2026-10-25).
  Part B's base is built when part B runs.
- **SJL-12.5, item by item**, in decision D26's order:
  - **the architecture test, with items 3 and 2** (tasks 5.1 to 5.3; Broiler-Platform/Broiler.JS#1064, `48fee2d3`,
    2026-10-20): `Broiler.JavaScript.Architecture.Tests`, with part B's clauses and its witness, asserting the exact
    list of the parser's violations; `FastScanner`'s end-of-file and empty tokens per scanner; the for-loop
    temporaries numbered per parse;
  - **item 1, explicit options** (task 5.4; Broiler-Platform/Broiler.JS#1065, `2345da11`, 2026-10-20):
    `FastParserOptions` through every compile site, `JSCompilationOptions.AllowTopLevelAwait` in the cache's key and
    the `IJSCompiler` default method; `CoreScript`'s ambient scopes deleted; corrections J1 and J2 listed in the
    changelog;
  - **item 4, no `Runtime` reference** (task 5.5; Broiler-Platform/Broiler.JS#1066, `792ff56e`, 2026-10-21): the five
    types and the Unicode tables moved to `Ast`, which took the reference to `Broiler.Unicode.Algorithms`, with a
    forwarder in `Runtime` for each of the two public ones; `UndoMark` obsolete; `Parser`'s reference to `Runtime`
    removed. The reference is absent by assembly, as part B of the rule reads the clause: the parser still names the
    moved types' `Runtime` namespaces, which their forwarders keep, and `Expressions`, which it references, looks up
    `Runtime`'s `JSValue` by name (`JSClassBuilder.cs:26`), unchanged since before SJL-12;
  - **item 5, the for-loop pass** (task 5.6; Broiler-Platform/Broiler.JS#1068, `c29d653f`, 2026-10-25):
    `ForLoopPreparation.Prepare` after the parse, in `FastCompiler` and in `DirectEvalSupport`, before
    `SyntaxValidation`, emitting the shapes the parser emitted; `ContainsContinue` recorded by the parser; the three
    walkers out of the parser; the architecture test's list empty. Its merge is **the after commit** the rule names;
  - **the records** (task 5.7, this change): [`docs/public-api.md`](../public-api.md)'s closing record of SJL-12.5's
    public API, with SJL-D-25's dispositions; the changelog's corrections; MOD-M2-4 closed; and the stale records
    below. They change no product path, so the after commit stays the last product change before the after runs.
- **Where what merged differs from the decisions as written above**, each recorded where it merged:
  - **Decision D22's "distinct from an enclosing compilation's carriers"** holds along a chain of direct evals, by a
    compile-time depth the compiler passes into each direct eval it emits. Two direct evals from the same code may
    name alike, since neither encloses the other. Where the current engine is not a `JSContext`, eval code is
    validated at its depth and compiled at 0; and where eval code is one call of the intrinsic `eval` with a
    constant, the constant is validated and run in its place at that eval code's depth, not one deeper. No
    temporary's name is observable from scripts. The product's remarks that say eval code is validated and compiled
    at its depth, `DirectEvalSupport.cs:92-93` and `ForLoopPreparation.cs:34-37`, are left to the first product
    change to those files after the after runs, since a change to them would move the after commit.
  - **Decision D29's "forwarders by default"** is not applied to one member: `DirectEvalSupport.Execute`, which
    compiled code calls, gained a parameter in place. Its old signature is removed, which breaks binary
    compatibility, with no forwarder, no obsolete overload and none of SJL-D-25's dispositions, and no check of its
    callers is made. Whether that is the stop of the programme's risk D6, "a public member removed without its
    recorded disposition", or an exception the owner accepts, is the owner's to decide; no decision on it is
    recorded. A forwarder made before the after runs would move the after commit.
  - **SJL-D-25 (b)** is an obsolete forwarder with a removal version; `UndoMark`'s obsolete mark names no version and
    leaves its removal to (c)'s consumer check, which cannot be made here.
  - **The rule's part C admits a third class**, D: an input the parser's for-loop desugaring refused at the base,
    which the parser alone accepts after and the whole compile refuses after as it did at the base. The bullet on
    the rule above names J1, J2 and a resolved crash only. Bundle `sjl-12-001`'s README, section 2, names before
    the after runs how it reads class D, whose population at the base is empty.
- **The stale records the reviews found are corrected**, each by a dated note, which quotes what it replaced where it
  replaced text: [`docs/architecture/dependencies.md`](../architecture/dependencies.md), on the tables' home and on
  the `Engine → Parser → Runtime` edge; [`Assemblies.md`](Assemblies.md) and
  [`Assemblies.status.md`](Assemblies.status.md), on that edge; this plan's SJL-7.5 record, on the tables' home; and
  the early-error catalogue's citations of the moved files, read in `early-errors.md`, since Broiler.VM's mapping
  pins the catalogue's bytes.
- **MOD-M2-4 is closed**, in [`Modernization.md`](Modernization.md), as decision D28 says.
- **Next: the after runs**, at the after commit: part A by `test262 evidence`, and parts B, C and D on the local
  machine; then the bundle's README reads each part against the rule. Nothing here is a verdict. The owner's
  reading of `DirectEvalSupport.Execute`'s removed signature, above, is open beside them.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 4 to 6, and its half of shared
clauses 7 and 8, are read by the programme's ledger. **SJL-12 may not meet its gate before SJL-3 meets SJL-3's.**

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
| **SJL-12** — the frontend contract and in-place preparation | Remove the parser's ambient state, mutable statics, `Runtime` references and parse-time desugaring in place (Modernization's MOD-M2-4 closes with it, and the contract supersedes MOD-M2-5's boundary specification; MOD-M2-3 is not part of it), record the frontend contract here, and keep the early-error catalogue and the parse corpus's home *(revised 2026-10-19, at SJL-12.1, under Broiler.VM's JSD-0080, decision D28; read "Remove the parser's ambient state, mutable statics, `Runtime` references and parse-time desugaring in place (Modernization MOD-M2-3 to MOD-M2-5 are this half), and record the frontend contract here")*. *(Noted 2026-10-25, at SJL-12.5's records: the removal is merged, SJL-12.5's items 1 to 5, with MOD-M2-4 closed, and the catalogue and the corpus are here; the after runs of bundle `sjl-12-001` are next, as [`SharedLibraries.status.md`](SharedLibraries.status.md) says.)* | Clauses 1 to 3, and this repository's half of shared clauses 7 and 8: an architecture test with a witness enforces the parser's independence; test262 per path meets the rule; the contract record is in this plan; the early-error catalogue is retained here, and the parse corpus and this engine's baselines over it *(revised 2026-10-19, at SJL-12.1; read "Clauses 1 to 3: an architecture test with a witness enforces the parser's independence; test262 per path meets the rule; the contract record is in this plan")* |
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
