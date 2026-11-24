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
at SJL-9; SJL-D-11 and SJL-D-12 at SJL-12, with SJL-D-12's `Ast` and `Parser` disposition at SJL-14 *(noted 2026-11-03: recorded at SJL-14.1 in bundle `sjl-14-001`'s `dispositions.md`, by decision D50, and applied at the cut-over, SJL-14.4)*;
SJL-D-7, SJL-D-8 and SJL-D-13 are Broiler.VM's alone, and ask nothing of this repository. *(Noted 2026-10-19, at
SJL-12.1: SJL-D-11 and SJL-D-12 are recorded in "The frontend contract (SJL-12)" below, filed that day with the
contract record as work package SJL-12.2.)* *(Noted 2026-10-25, at SJL-11.1: SJL-D-14, in the table above, is also
recorded as the regular-expression area applies it, in "Area verdicts", "Regular expressions (SJL-11.1)", with
Broiler.VM's JSD-0081.)* *(Noted 2026-10-26, at SJL-9.1: SJL-D-22 and SJL-D-24 are recorded in "Area verdicts",
"Locale data and the Intl algorithms (SJL-9.1)", with Broiler.VM's JSD-0082.)*

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

*(Noted 2026-11-17. **Trigger (a) is reached**: a controlled lane exists, recorded as `Measurement.md` requires, in
[`docs/evidence/lanes/win-x64-r7-5800x/`](../evidence/lanes/win-x64-r7-5800x/README.md). It is one machine and one
RID, `win-x64`: the owner's workstation, quieted for a run window, which the owner confirmed as the lane, RID included,
on 2026-11-17 before any run on it. The route above read "linux-x64 first"; no linux-x64 lane exists, and nothing here
is about one. The owner's choice at (a) was to take part B on that lane as written: "Take part B's first judged run,
and judge it as the rule says." Both original arms, `1410079e` and `ef676932`, build there, so neither fallback above
applies. **The reading is MET**, every clause of part B holding (bundle [`sjl-3-001`](../evidence/sjl-3-001/README.md),
section 7, and its sub-bundle
[`measurement-win-x64-r7-5800x/`](../evidence/sjl-3-001/measurement-win-x64-r7-5800x/README.md)); on `Literals`, the
workload the void run's arithmetic would have failed, the difference is below the lane's A/A floor. So, as "What would
meet the clause" above says, **clause 7's measurement half is met**. What it does not change: no rule, gate or clause;
SJL-3 stays `In progress`, because Broiler.VM's clause 13 stays open after the last measurement its rule's revision
allows, and a third measurement would first need a new dated revision of that rule by the owner; and nothing is
accepted, because no second holder exists to review it. The programme's ledger and a dated note under decision
SJL-D-23 are to record trigger (a) and this reading; both are filed in Broiler.VM apart from this record.)*

*(Noted 2026-11-18, at trigger (a), on the owner's own dated choice for Broiler.VM's clause 13. The note above reads
"Broiler.VM's clause 13 stays open after the last measurement its rule's revision allows, and a third measurement would
first need a new dated revision of that rule by the owner". On 2026-11-18 the owner was asked: "SJL-3 clause 13
(Broiler.VM's measurement) is open after the last measurement its 2026-10-20 revision allows. What should happen now
that a controlled lane exists?" The owner chose "Third run on the lane": "You make a new dated revision of the rule
whose only change is one more measurement on win-x64-r7-5800x, with the tolerance unchanged. I draft it for your words;
the run happens in a local session on your workstation." Not taken: "Keep it open": "No revision and no run. Clause 13
stays open until trigger (b), where the choice is between waiting and a gate revision."; and "Gate revision now":
"Revise SJL-3's exit gate under SJL-D-23 alternative (b) now, instead of measuring. This would be the first exit-gate
revision made after the evidence it concerns." This is the owner's own dated choice, as SJL-D-23 reserves it, and not
one taken under the owner's direction of 2026-10-18. **A third Broiler.VM measurement for clause 13 is to be taken on
the same lane, `win-x64-r7-5800x`, under a new dated revision of Broiler.VM's rule.** That revision is filed in
Broiler.VM, separately from this record, and this record neither restates nor reads it. No rule, gate or clause of
this repository changes. SJL-3 stays `In progress`, and Broiler.VM's clause 13 stays open until that measurement is
taken and judged under its revision. Nothing is accepted.)*

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
in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md). *(Noted 2026-10-26, at SJL-9.1: SJL-9.5 supersedes
this decision, as its "SJL-9 moves it into the shared locale data" foresaw. Each locale's default numbering system then
comes from the shared CLDR 48.2.0 data, `JSIntlDefaultNumberingSystems.g.cs` and
`scripts/intl/generate_default_numbering_systems.py` are deleted, and the move from 47.0.0 to 48.2.0 is a data change
listed in the changelog (Broiler.VM's JSD-0082, decision D13; "Locale data and the Intl algorithms (SJL-9.1)"
below).)*

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
  providers of the same decision are defined at SJL-9 and SJL-10. *(Noted 2026-10-26, at SJL-9.1: the
  default-locale and available-locale providers are recorded by Broiler.VM's JSD-0082, decision D7, in "Locale
  data and the Intl algorithms (SJL-9.1)" below.)*
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
  `formatToParts(8.64e15)` (SJL-9). *(Noted 2026-10-26, at SJL-9.1: "Intl's `ArgumentOutOfRangeException` for
  `formatToParts(8.64e15)` (SJL-9)" was fixed on 2026-10-16, before SJL-9 began (row T28), and decision D25
  drops it from SJL-9.5's list; "Locale data and the Intl algorithms (SJL-9.1)" below.)*
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
  the owner's word status (G4). *(Noted 2026-10-26, at SJL-9.1: of "`null` and no argument read as the host's
  culture (C8)", only the refusal of `null` stands as a correction from SJL-9.5. By Broiler.VM's JSD-0082,
  decision D7 as revised for finding F3 (row L35), casing with no argument then reads this engine's
  default-locale provider, which is the host's culture under one, as Intl's services do; "Locale data and the
  Intl algorithms (SJL-9.1)" below records it, and SJL-9.5's changelog entry quotes C8's line.)*
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
  host's culture until SJL-9. *(Noted 2026-10-26, at SJL-9.1: of "the default language of `toLocaleUpperCase`
  and `toLocaleLowerCase` is this engine's, never the host's culture, while Intl's other services keep the
  host's culture until SJL-9", Broiler.VM's JSD-0082, decision D7 as revised for finding F3 (row L35), reads
  "this engine's" as this engine's default-locale provider. From SJL-9.5, under a host culture, casing with
  no locale follows that culture's language through the provider, never through `CultureInfo` directly, and
  the refusal of a `null` locale stands. Intl's services keep the host's culture after SJL-9 as well
  (SJL-D-19). See "Locale data and the Intl algorithms (SJL-9.1)" below.)*
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

**Regular expressions (SJL-11.1), filed 2026-10-25: all six sub-areas accepted, placed with Broiler.RegEx under its
owner conditions, decision SJL-D-14 recorded, and the owner's decisions of 2026-10-20 and 2026-10-21 recorded.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0081-regular-expressions-move-to-broiler-regex-under-its-owner-conditions.md`
  (JSD-0081), decided on 2026-10-21. It states each condition of the extraction gate for syntax and early errors with
  literal validation (S), compilation and matching (M), the budget and the limits (B), flags (F), `RegExp.escape` and
  `source` (T) and the use of Unicode data (U), each on its own, and records decision SJL-D-14 as this area applies it.
- **Its input** is `Broiler.VM/docs/evidence/sjl-11-001/correspondence.md`, the correspondence from code. It read this
  repository at `1f91b8f1`, Broiler.VM at `aaa39fb`, and Broiler.Regex `0.1.0-preview.4`, which this engine restores
  for every `u` and `v` pattern, at `c1422f4d`. Every cell of this engine's that a row rests on was run through the
  shell: each pattern by three paths, the constructor, the literal evaluated and the literal never evaluated, and the
  case-insensitive rows again under `tr-TR` and `lt-LT`. Where the shell's fallback to .NET hides the package's own
  answer, the package was called directly. Every probe and answer is retained beside it. Its section "Revisions" reads
  this repository's `792ff56e`, where SJL-12.5 moved the validators to `Broiler.JavaScript.Ast` with comments and a
  forwarder as their only change, and finds that no cell could answer differently there.
- **SJL-11 started before SJL-7, which it waits on, and SJL-3 met their gates**, at the owner's words of 2026-10-19,
  "Also continue with the other remaining SJL milestones", which the programme's ledger records in Broiler.VM's own
  change for SJL-11.1. SJL-11 may not meet its gate before SJL-7 meets SJL-7's, and so not before SJL-3 meets SJL-3's.
- **The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
  review." Each decision SJL-11's plan left open is taken as it recommends, and recorded as the owner's dated decision
  in JSD-0081: D1 to D40 on 2026-10-20, and D41 on 2026-10-21. Where the correspondence showed a recommendation's text
  to be wrong, JSD-0081 records the decision corrected and quotes the text it replaces. **It changes no rule**: nothing
  is accepted without review, and the gate waits as above. In this record a decision is "decision Dn" and a row of the
  correspondence "row Xn". This record carries decisions D7, D10, D20 to D23, D33 and D34.

This repository's part of it:
- **The home is Broiler.RegEx**, under decision SJL-D-14 (a), as the table above says. Its package `Broiler.Regex` is
  fixed to the area design's requirements 1 to 11, which its roadmap numbers R1 to R11 and adopts as owner conditions
  by its dated record of 2026-10-20. The fix is a new backtracking machine written there, with Broiler.VM's matcher as
  its specification and no code ported (decision D5). **If a condition is not met by the end of SJL-11.2, the area is
  refused** (decision D1): neither engine moves onto the package, and this engine keeps its translator and its routing
  with a source pointer to JSD-0081.
- **The sub-area verdicts, as this engine meets them:**
  - **S**: the literal check, `Parser`'s `RegExpValidator` with its three prechecks and a .NET `Regex`, becomes
    Broiler.Regex's validate entry, and the constructor's prechecks and translator its compile;
  - **M**: the translator to .NET's `Regex` and the routing to Broiler.Regex's continuation closures become the new
    backend, for every pattern;
  - **B**: the policies below;
  - **F** and **T**: `ParseFlags`, `BuildFlagsString` and the escape helpers become Broiler.Regex's flag parser and
    text helpers (R9);
  - **U**: Broiler.Regex reads Unicode data through the Unicode owner's provider (R11, decision D11), so .NET's casing
    data leaves the area and the last old-family tables leave this engine's closure.

  This engine keeps its `RegExp` objects, `lastIndex`, the `Symbol` protocols, species, `GetSubstitution`'s
  evaluation, its legacy statics (`LegacyRegExpState`), argument conversion and its errors.
- **The policies (decisions D7 and D10).** This engine passes **no meter**, as in every shared area. It passes
  Broiler.VM's frame ceiling `1 << 20`, trail ceiling `1 << 21` and nesting limit 512, depth counted as Broiler.VM's
  matcher counts it: the pattern's own disjunction is depth 1, each group adds 1, and under `v` each class adds 1, so
  511 nested groups are accepted and 512 refused. A limit exceeded is a catchable `RangeError`, and the nesting status a
  `SyntaxError`.
  - **This engine's deeper patterns, which it accepts today at every depth that does not end its process (rows S3 and
    S3b), become listed refusals**, class C3 of its rule (SJL-D-16), and so does a test262 row that reaches a ceiling,
    shown by a library run under the same policy (decision D39).
  - **The spent-allowance no-match becomes a correction**: a catastrophic `u` or `v` pattern runs to the
    specification's answer, as its non-`u` patterns already do through .NET, listed as a behaviour change.
  - **SJL-11.6's adversarial corpus "in both engines" holds here for the classes that complete**; the unbounded
    classes, K catastrophic backtracking and A alternation without quantifiers, are judged at library level by
    Broiler.RegEx's rule. Broiler.VM's milestone file records the revision, quoting its text.
- **The corrections (decision D23)**, each to be listed in `Broiler.JS/CHANGELOG.md` at SJL-11.5 under decision
  SJL-D-16 (a), with its row. The milestone's list is revised by row, and Broiler.VM's milestone file quotes the old
  text. Loose property names and invalid group names were corrected at SJL-7.5 and by
  [#1041](https://github.com/Broiler-Platform/Broiler.JS/pull/1041), so their prechecks are deleted rather than
  corrected. The list:
  - case-insensitive matching through .NET: M2 (bundle `sjl-7-001`'s F2), M2a under `tr-TR` and under `lt-LT`, and
    M2b; and U3, by data decision (class C5, SJL-D-6 (c));
  - .NET's line terminators: M3 (W5), with `$` without `m`;
  - `lastIndex` inside a surrogate pair: M4's matches of half a pair. **SJL-11.5's step out of a pair moves only the
    matcher's start, never the reported `"index"` or substring**: the specification reports `lastIndex`, as this
    engine does today, and Broiler.VM's report of the pair's start is that engine's correction under the same row;
  - `source` rewriting: T1;
  - the literal check: S2b, S7, S8 (test262's breaking-change patterns and two more `v`-mode classes) and S11 (the
    invalid modifiers and `v`-mode classes this engine refuses only when the literal is evaluated), their literal path
    closed by decision D22's re-pointing, and S7's and S8's constructor path, where the library's refusal is caught
    and .NET admits the translation, by the translator's retirement. Rows S7, S8 and S11 between them explain every
    test262 file on a regular-expression path that this engine failed in bundle
    [`sjl-10-001`](../evidence/sjl-10-001/README.md)'s after run, but `test/built-ins/RegExp/proto-from-ctor-realm.js`,
    a realm test; the rule names each file;
  - an escaped duplicate name in one alternative: S2a;
  - a class that joins a lead-surrogate escape to a braced trail escape: S10's class case;
  - with Broiler.Regex's `0.1.0-preview.4`: the budget's no-match, shortened match, wrong index and flipped look-around
    (B1 to B3); the stack guard's fallback, to .NET and to no match (B5); the process aborts of a flat sequence and of
    deep nesting (B8 and S3b); and the look-behind back-reference inside a pair (M7);
  - `String.prototype.search`'s direct call: M6.

  **Not on it:** `\0` before a non-ASCII digit, which is Broiler.Regex's alone and not observable here (row S4); and an
  invalid literal read as division, which stays SJL-14's (row S9).
- **What the engines already answer alike** stays as it is: S2 at engine level, S4, S6 (exact property names in both
  since SJL-7.5), M1, M2c, M5's answers, F1 to F3 but S11's late refusals, T4, T5, U1 and U2. T2, whether a `/` in a
  class is escaped in `source`, is implementation-defined. **M4's empty match** from a `lastIndex` inside a pair is a
  defect of the specification's text, classed implementation-defined by decision D41 until the text is corrected.
- **The scanner (decision D22).** SJL-11.5 deletes `DecodeBracedUnicodeEscapes` and the parenthesis-balance patch,
  which corrects `source` (row T1), and re-points the scanner's `RegExpValidator.IsValid` call to the validate entry,
  which corrects the literal path's rows S2b, S7, S8 and S11. **The division fallback stays, SJL-14's**, listed (row
  S9). Broiler.VM's JSD-0080, decision D23, listed the braced-escape decoding for SJL-14; a dated note there records
  that SJL-11.5 deletes it, or SJL-14 if SJL-11 has not.
- **The deletion list (decision D20)**, as JSD-0081's X6 states it for this engine:
  - the translator, every member from `ClampLargeQuantifiers` to `HexValue` that is not object-model code, and
    `JSRegExp.UnicodeSets`;
  - `RegexTiering` and `RegexTieringDiagnostics`; the routing, `GapScan`, `TryRouteToBroiler` and `FromNet`; the
    overflow fallback; the .NET `value` field; and both `CreateRegex` overloads;
  - `Parser`'s `RegExpValidator`; the regular-expression prechecks of `RegExpGroupNames` and `JSUnicodePropertyNames`,
    now in `Broiler.JavaScript.Ast`; and the scanner's braced-escape decoding and parenthesis-balance patch;
  - `ParseFlags` and `BuildFlagsString`, and the escape helpers `Escape`, `TryAppendEscape` and
    `EscapePatternSource`, onto Broiler.Regex's flag parser and text helpers (R9);
  - `String.prototype.search`'s direct `value.Match` call;
  - the tests and the benchmarks that test only the deleted members: `RegExpTranslatorFallbackTests`,
    `RegexTieringTests`, the clamp parts of `Issue810RegExpLargeQuantifierTests`, `RegexTieringMetrics`, and
    `RegexProfileMetrics`' .NET rows or the whole file. The tests of behaviour that stays are rewritten onto the
    adapter.
- **Decision SJL-D-25 (decision D21).** `RegexTiering`, `RegexTieringDiagnostics` and both `CreateRegex` overloads:
  **(c), removal**, with a consumer check. An organisation-wide code search of 2026-10-19 found no use of them outside
  this repository but Broiler.Unicode's test-only copies; SJL-11.5 runs it again and records its queries and results
  in [`docs/public-api.md`](../public-api.md). Their own tests are deleted with them, and
  `SharedUnicodeTests.cs:204-235` is rewritten. `RegExpUnicodeValidator`: **(a)**, a forwarder to the validate
  entry. `JSRegExp.Match`, `Split` and `Replace`: kept, their bodies re-pointed. `CaptureGroupMap`: kept if used,
  else (b).
- **The version and the pin (decision D12).** Broiler.Regex's SJL-11 surface ships in the `0.1.0-preview.N` stream, as
  `0.1.0-preview.6` or a later version of it, and it breaks the surface this engine calls today. **This engine moves
  its pin only at SJL-11.5**, and no release of either engine depends on the version that carries the surface before
  both cut-overs have merged. *(Revised 2026-11-01, by the owner's decision of that day, "Please keep using version
  0.1.0-x scheme!", which revises decision D12 of Broiler.VM's JSD-0081; the owner published Broiler.Regex
  `0.1.0-preview.5` on 2026-10-29 from Broiler.Regex `24344b3`, so the next is `0.1.0-preview.6`. The bullet read
  "Broiler.Regex's SJL-11 surface ships as `0.2.0-preview.1`, and it breaks the `0.1` surface this engine calls today"
  and "no release of either engine depends on `0.2` before both cut-overs have merged".)*
  *(Noted 2026-11-16, on the owner's ruling of that day; the bullet above is left as written.)* The bullet reads "**This
  engine moves its pin only at SJL-11.5**, and no release of either engine depends on the version that carries the
  surface before both cut-overs have merged". The dependency update on the default branch, `cdf5559d` ("Update
  dependencies"), moved `Directory.Packages.props`' Broiler.Regex to `0.1.0-preview.6`, whose syntax tree,
  `RegexParser` and `RegexFlagsParser.Parse` are internal and whose `RegexMatch.Groups` is a span (Broiler.RegEx's task
  2.9 of SJL-11.2, its breaks against `0.1.0-preview.5`), so `JSRegExp.Broiler.cs` no longer compiled. Asked "How should
  I fix main?", with the options "Pin back to preview.5" (recommended), "Adapt to preview.6 now" and "Leave it to the
  contributor", the owner chose "Adapt to preview.6 now", described as "A PR that ports JSRegExp.Broiler.cs onto
  preview.6's public surface now. This pulls part of SJL-11.5's adapter work ahead of its rule's base and after
  commits, and changes regex code on main outside SJL-11.5's window." **So the adapter moved onto `0.1.0-preview.6`'s
  public surface on Broiler-Platform/Broiler.JS#1119, in the commit right after its merge of `cdf5559d`, ahead of
  SJL-11.5, by the owner's choice.** `GapScan` reads the pattern text by the grammar the package's parser reads outside
  Unicode mode, where it walked the tree; `TryRouteToBroiler` reads the flags through `RegexFlagsText.Parse`;
  `FromBroiler` reads the span's `Length`. The routing, the translator, the validators and every other member of the
  deletion list stay, and no answer of this engine is meant to move: Broiler-Platform/Broiler.JS#1119's description
  gives its runs at its head against `ff938b46`, whose product code is `2f012739`'s. What SJL-11.5 and bundle
  [`sjl-11-001`](../evidence/sjl-11-001/README.md) must now read differently:
  - **The pin is no longer SJL-11.5's to move.** SJL-11.5's base commit will already pin `0.1.0-preview.6` and route
    through it, so its base runs answer as that version answers for every `u` and `v` pattern and every gap shape, not
    as `0.1.0-preview.4` or `0.1.0-preview.5` answered. A row that the rule's list of corrections names "with
    Broiler.Regex `0.1.0-preview.4`" (B1 to B3, B5, B8, S3b's process abort and M7's look-behind) may already follow the
    specification at the base and then not move; such a row is one the runs cannot show, not a regression.
  - **Task 2.9's breaks, as this engine met them, are answered here by a stopgap.** After this merge the adapter reads
    none of the members it read (the tree, `RegexParser`, `RegexFlagsParser.Parse` and the type of `Groups`).
    SJL-11.5's adapter work is otherwise as its records say: the translator's retirement, the routing's deletion, the
    validate entry, the flag parser and text helpers, the ceilings and the nesting limit. `GapScan` and
    `TryRouteToBroiler`, which part E's search names, still exist and still go at SJL-11.5, so that search reads as
    written.
  - **For the owner, words that no longer fit** (no rule file is edited here): bundle `sjl-11-001`'s README, "The pins"
    and its dated note of 2026-11-01, which say that SJL-11.5 moves the pin; the rule's part C, whose N cases "end the
    process where Broiler.Regex `0.1.0-preview.4`'s parser recurses past the stack" at the base; and this bullet's "no
    release of either engine depends on the version that carries the surface before both cut-overs have merged", which
    a release of this engine from the default branch would now break, since the pin is the owner's to keep.
    Broiler.RegEx's roadmap records, under its task 2.10, that this engine's shell "cannot take `0.1.0-preview.6`
    without SJL-11.5's adapter"; that record is Broiler.RegEx's, and is not revised here. Broiler.VM's milestone
    `sjl-11-regexp.md` lists the same corrections "with Broiler.Regex's `0.1.0-preview.4`" and reads the same way, and
    its status record of 2026-11-01 already reads D12's sentences on the pin as no longer holding; both are
    Broiler.VM's records, and are not revised here.
- **The component's test262 (decision D14).** Broiler.RegEx's CI runs a host job through this engine's shell, at a
  commit of this repository that its roadmap names by a dated record, with the candidate package from a local feed:
  SJL-11.5's branch until it merges, then the default branch. So that branch exists early (decision D35), and the
  commit the record names is kept reachable.
- **Outside the area, and left to their owners:** the .NET `Regex` uses in `JSIntl` (SJL-9's), `DateParser` and the
  compiler's deferred bodies (decision D34).
- **The rule**, [`docs/evidence/sjl-11-001/decision-rule.md`](../evidence/sjl-11-001/decision-rule.md), is committed
  before any code of the area, with part B's workloads and part C's programs under the bundle's `inputs/`. Its parts:
  - A, test262 per path in UTC with negative-metadata tests, its classes naming every correction above by row, and the
    test262 files of rows S7, S8 and S11 by name;
  - B, this engine's measurement, over a new benchmark class, `RegExpBenchmarks`, that a later inputs commit adds,
    reading the workloads verbatim (decision D17). It is not a gate clause: a NOT MET is recorded in the cut-over
    record as "a slower but bounded matcher", and the part is void on a lane that is not controlled (decision D19);
  - C, the adversarial programs, written by Broiler.RegEx's `eng/adversarial-programs.py --engine broiler-js` from its
    corpus, whose SHA-256 the manifest names (decision D37);
  - D, the regular-expression vectors under the invariant culture, `tr-TR` and `lt-LT` (decision D33);
  - E, the deletion list, by a search and a scoped metadata check, with a control (decision D34).

  Every base is taken at the first parent of the merge that lands SJL-11.5, under a hold (decision D16).
- *(Noted 2026-11-17, at SJL-11.5; the bullets above are left as written.)* **SJL-11.5 is on its own branch,
  `sjl-11-5`, in this repository, and is not merged.** The owner allowed the branch on 2026-11-17: "I push the SJL-11.5
  cut-over to a new branch, sjl-11-5, in Broiler-Platform/Broiler.JS: fast-forward only, never force, opened as a PR
  only when its merge is due." It is the branch decision D35 asks for early, which Broiler.RegEx's host job (its task
  2.10) builds this engine's shell from. **Its merge waits** on Broiler.RegEx's candidate reading, on the hold under
  which every base is taken (decision D16), and on the base runs, taken at the first parent of the merge that lands it;
  no run of bundle [`sjl-11-001`](../evidence/sjl-11-001/README.md)'s rule has been taken. The branch holds, in order:
  - **the rule's inputs commit**, `RegExpBenchmarks` and the benchmark `Program.cs`'s `--regexp-answers`, which read
    the bundle's workloads verbatim. The rule names "the default-branch commit" from which the base descends, so it
    reaches the default branch before the base is taken; the bundle's README says so in a dated note;
  - **a revision of the rule**, [`decision-rule-revised-2026-11-17.md`](../evidence/sjl-11-001/decision-rule-revised-2026-11-17.md),
    filed before any of its runs under the owner's direction of 2026-10-18, which adds rows S12 to S15 and M8 to M27 to
    part A's list of this engine's corrections and says how the rows the list names "with Broiler.Regex
    0.1.0-preview.4" read now that the base routes through `0.1.0-preview.6`; `decision-rule.md` is not edited;
  - **the cut-over**, as the bullets above list it: the translator, the routing, `RegExpValidator`, the prechecks and
    both `CreateRegex` overloads deleted; `RegExpUnicodeValidator` a forwarder to the validate entry; the scanner's
    check re-pointed to it, with the braced-escape decoding and the parenthesis-balance patch deleted and the division
    fallback kept; the step out of a pair moving only the matcher's start (row M4); and every correction listed in
    [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md). `RegexTiering` and `RegexTieringDiagnostics`, which the
    deletion list above names, and the tests and benchmarks that test only them, are kept on the branch, and the
    records put that to the owner; part E's search finds them until they go.

  The branch's own local runs are no runs of the rule and judge nothing. Every clause of this engine's, 4 to 7, is
  unmet, and none of it is accepted.
- *(Noted 2026-11-17, after review of branch `sjl-11-5`; the note above is left as written.)* The note above reads
  "`RegexTiering` and `RegexTieringDiagnostics`, which the deletion list above names, and the tests and benchmarks
  that test only them, are kept on the branch, and the records put that to the owner". **That departs from decision
  D21's bullet**, which reads "`RegexTiering`, `RegexTieringDiagnostics` and both `CreateRegex` overloads: **(c),
  removal**, with a consumer check" and "Their own tests are deleted with them", and from decision D20's list, which
  names "`RegexTieringTests`", "`RegexTieringMetrics`, and `RegexProfileMetrics`' .NET rows or the whole file". The
  branch keeps them because the instruction it is made under reads "Never weaken, skip or delete a test"; which of the
  two governs is the owner's to rule, and the merge waits on that ruling as well. Nothing in the engine calls the two
  types on the branch, since the .NET `Regex` they tier is deleted: `BROILER_JS_REGEX_TIERING` changes nothing,
  `RegexTieringTests`' test of a hot pattern fails, and its other tests pass without reaching a race.
  - **A record outside the area that goes stale at the merge:** `Broiler.ECMAScript/docs/frontend/early-errors.md`'s
    table of moved paths and the rows that cite them (EE0183, EE0184, EE0186 to EE0190, EE0192, EE0193, EE0195,
    EE0197 to EE0205, EE0210, EE0212 and EX0026) name
    `Broiler.JavaScript.Ast/RegExpGroupNames.cs` and `Broiler.JavaScript.Ast/Unicode/JSUnicodePropertyNames.cs`,
    which the branch deletes, and lines of `RegExpUnicodeValidator.cs`, which it makes a forwarder. That record is
    Broiler.ECMAScript's, and its dated note is that owner's, after the merge; the branch changes nothing under
    `Broiler.ECMAScript/`.
- *(Noted 2026-11-17, on branch `sjl-11-5`; the two notes above are left as written.)* **The conflict the note above
  puts to the owner is resolved by decisions D20 and D21, and `RegexTiering` and `RegexTieringDiagnostics` are deleted
  on the branch with their tests.** The note above reads "The branch keeps them because the instruction it is made
  under reads "Never weaken, skip or delete a test"; which of the two governs is the owner's to rule, and the merge
  waits on that ruling as well", and the note before it "`RegexTiering` and `RegexTieringDiagnostics`, which the
  deletion list above names, and the tests and benchmarks that test only them, are kept on the branch, and the
  records put that to the owner; part E's search finds them until they go". The instruction's sentence was a guard
  against weakening a test to make a run pass; it was never meant to override the programme's own deletion list, and
  the session that wrote the instruction has corrected it so: decisions D20 and D21 govern. **This is a correction of
  that session's own instruction, not a ruling of the owner's and not a choice taken under the owner's direction**,
  so the merge no longer waits on a ruling on it. The branch deletes, as D20 and D21 list them, `RegexTiering` with
  its environment switch `BROILER_JS_REGEX_TIERING`, `RegexTieringDiagnostics`, their own tests `RegexTieringTests`,
  and the benchmark that measured only them, `RegexTieringMetrics` (its `--regex-tiering` emitter, and the
  `regexRaces` fields `--specializing-tier` read from `RegexTieringDiagnostics`); the consumer check D21 asks for is in
  [`docs/public-api.md`](../public-api.md), and the members are listed in
  [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md). D20's other test and benchmark entries,
  `RegExpTranslatorFallbackTests`, the clamp parts of `Issue810RegExpLargeQuantifierTests` and `RegexProfileMetrics`'
  .NET rows, are not touched by this change. Part E's search is a run of the rule and judges this; nothing here
  declares it met.
  - **A change no row names, a finding for Broiler.VM's correspondence.** Since `4e2e00ad` on this branch,
    `IJSRegExp.IsMatch` and the public `JSRegExp.Replace` honour the `y` flag, as `RegExpBuiltinExec` from
    `lastIndex` and `RegExp.prototype[@@replace]` do in the specification's text, and the public `JSRegExp.Split`
    reads every position as `RegExp.prototype[@@split]`'s sticky splitter does; at the branch's first parent the
    three honoured `y` only for a pattern routed to Broiler.Regex. No row of the correspondence names it, so it is
    recorded in the bundle's README as a finding for Broiler.VM's correspondence, whose row is Broiler.VM's to add.
- *(Noted 2026-11-18, on branch `sjl-11-5`; the notes above are left as written.)* **Broiler.RegEx's candidate reading
  is taken, part A has been read a second time, and the owner has ruled on what part A blocks and answered three
  questions on the cut-overs; the merge still waits on the inputs landing on the default branch, the hold, the base runs
  and the owner's open items that Broiler.RegEx's records name.** The note of 2026-11-17 above reads "**Its merge
  waits** on Broiler.RegEx's candidate reading, on the hold under which every base is taken (decision D16), and on the
  base runs, taken at the first parent of the merge that lands it; no run of bundle
  [`sjl-11-001`](../evidence/sjl-11-001/README.md)'s rule has been taken." By Broiler.RegEx's records (its
  `docs/roadmap.md`, the dated records of 2026-11-18 on task 4.2, and its bundle `sjl-11-001`'s README, at its default
  branch's `ba7ac8cf`):
  - **The candidate reading** of Broiler.RegEx's bundle `sjl-11-001` was taken at `49d7be1f`, the merge of
    Broiler-Platform/Broiler.Regex#27 and the reading's commit, and its records merged in #28 as `662f3499`. Part A read
    NOT MET; parts B to E and the reference arms read MET on the retained runs. Its record reads "**So by the rule, part
    A's NOT MET blocks both cut-overs** (risk D10's stop; `decision-rule.md`: "A part NOT MET at the candidate reading
    blocks both cut-overs")."
  - **Part A's second reading**, at the same commit `49d7be1f` under a revision of 2026-11-18 of Broiler.RegEx's rule,
    filed before its witness runs, merged in #29 as `ba7ac8cf`: "**Part A: read MET on the retained runs.**" The record
    goes on: "The candidate reading's part A NOT MET stays on record as that reading's. Parts B to E and the reference
    arms keep their candidate readings, MET on the retained runs, unchanged."
  - **The owner's ruling of 2026-11-18 on the blocking line**, which Broiler.RegEx's records give as "the owner's own
    ruling, not one taken under the owner's direction of 2026-10-18". The question's sentences after its first, which
    counts the witnesses and is left out here because this plan holds no figure, read: "But the 2026-11-18 revision kept
    the rule's line "A part NOT MET at the candidate reading blocks both cut-overs" unchanged, and the candidate
    reading's part A NOT MET stays on record. Read strictly, the block would never lift. Should part A's MET at its
    second reading lift that block for both cut-overs (SJL-11.5 in Broiler.JS, SJL-11.4 in Broiler.VM)?" The owner
    answered "Yes, the second reading lifts it" — "A dated note records your reading: the block on both cut-overs is
    lifted as far as part A goes. The cut-overs still wait on their own holds (H1, H2), their base runs, and your open
    items." The records add: "that wording item is answered. Nothing else is decided: SJL-11.5 in Broiler.JS and
    SJL-11.4 in Broiler.VM still wait on their own holds, H1 and H2, their base runs, and the owner's other open items."
    The labels H1 and H2 are Broiler.RegEx's and no record defines them; this engine's records name its hold in decision
    D16's words, "Broiler.JS's at the after merge's first parent, under a hold", and this note does not say which label
    is which.
  - **What Broiler.RegEx's task 4.2 waits on**, by its roadmap at `ba7ac8cf`. Just before the list the roadmap reads:
    "SJL-11.5 in Broiler.JS and SJL-11.4 in Broiler.VM still wait on their own holds, H1 and H2, which this repository's
    records do not decide, and on the owner's items below." The list reads: "1. **The owner on publication (D13).**
    `0.1.0-preview.6` was published from `df5cfc2` before the candidate reading, which read its sources and not its
    bytes. Whether to keep it, publish another version after a reading, or neither, is the owner's question. 2. **The
    owner on the decisions and readings of the candidate reading**, any of which the owner can overturn: the reading's
    commit, with its departure from `decision-rule.md`'s line "taken at the commit `0.1.0-preview.6` is packed from";
    the rule commit; the session's reading of class (b)'s vector clause; and class (c)'s "both runs are retained", whose
    pre-fix run is not retained. 3. **The scratch branch `sjl-11-2-control-b`**, for the owner to delete. 4. **The owner
    may review this revision's wording**: the choice is the owner's own; the clause list, the tests named for each
    clause and the two clauses read on their closest plant (A-4.4's Native AOT half, A-10.1's version) are this
    record's. So are the review's items in the dated note below: A-11.3's row, which names a test of A-5.6's among its
    own; the blocking line, which the revision lists as unchanged while pointing it, for part A, at the second reading;
    the two attempts not counted, a reading stricter than the revision's text; the guarantees inside a clause that have
    no plant of their own; and the revision's ..." The words cut at the end name a result in the revision's file, which
    the item calls "a result in a file that holds no figure"; this plan holds no figure either, so they are left out
    here. Of item 4's wording items, the owner's ruling above answers the blocking line alone ("that wording item is
    answered"). Nothing here reads task 4.2 as done; that reading is Broiler.RegEx's.
- *(Noted 2026-11-18, the same day; the note above is left as written.)* **The owner's answers of 2026-11-18, quoted
  verbatim.** Each is the owner's own, not a choice taken under the owner's direction of 2026-10-18.
  - **1.** Asked: "D13: Broiler.Regex 0.1.0-preview.6 was published from df5cfc2 before the candidate reading. The
    reading judged its sources, which are identical to the reading's commit 49d7be1f, not the package bytes. Broiler.JS
    pins preview.6, so a new version would have to be pinned before SJL-11.5's base runs. What should happen?" Chosen:
    "Keep preview.6" — "No new Regex release. Both cut-overs use preview.6, whose library sources the reading judged."
    Not taken: "Publish preview.7 first" — "You publish a new Regex version after the reading. Broiler.JS moves its pin
    before the SJL-11.5 base is taken, and Broiler.VM admits that version."
  - **2.** Asked: "The Broiler.Regex candidate reading rests on four session decisions that you can overturn: (1)
    reading at 49d7be1f instead of the packed-from commit df5cfc2 (same library sources); (2) the rule commit e568b88;
    (3) the session's reading of class (b)'s vector clause, made after the runs were seen; (4) class (c)'s pre-fix run
    is not retained, and its rerun matches byte for byte. Do you confirm them?" Chosen: "Confirm all four" — "A dated
    note records your confirmation, and task 4.2 then waits only on D13 and the scratch branch's deletion." Not taken:
    "I want to look first" — "Nothing is recorded now. I send you a short summary of each one so you can rule on them
    separately."
  - **3.** Asked: "SJL-11.5's base in Broiler.JS needs two things only you can order. First, its inputs commits
    (24fe7cba, 189d65ff) and a new dated rule revision must land on main by a merge commit before the base is taken.
    Second, test262-evidence.yml moved from ubuntu-latest to ubuntu-24.04 in your dependency update of 2026-10-29
    (3b4b6860), and the rule fixes the workflow "as this file's commit carries it". Only a dated rule revision filed
    before any run can admit that change. Do you allow both?" Chosen: "Allow both" — "The inputs land on main by a merge
    commit with one dated revision. The revision lists the y-flag correction row and admits the ubuntu-24.04 workflow.
    Then the hold starts and the base runs are taken at main's head." Not taken: "Inputs only" — "The inputs land, but
    the workflow change is not admitted. Part A's base can then only be taken with the workflow as the rule's commit
    carries it."; "Not yet" — "Nothing lands on main for SJL-11.5. Its base and merge wait."
  - **How they bear here.** Answers 1 and 2 are the owner's on items 1 and 2 of Broiler.RegEx's list above; recording
    them under task 4.2 is Broiler.RegEx's, and at `ba7ac8cf` its records do not yet. By answer 1 this engine keeps the
    pin it has, Broiler.Regex `0.1.0-preview.6` (`Directory.Packages.props`), which this branch does not move. Answer 3
    is the order that the bundle's README puts to the owner: "the rule's base commit "descends from the inputs commit",
    so this commit reaches the default branch before the base is taken, or the base does not satisfy the rule. That is
    the owner's to order; nothing here merges it." The dated rule revision answer 3 names is not filed here, and no rule
    file is added or edited here.
- *(Noted 2026-11-18, the same day; the two notes above are left as written.)* **What the merge still waits on, by the
  records' own words.** The first note of 2026-11-17 above reads "**Its merge waits** on Broiler.RegEx's candidate
  reading". The candidate reading is taken, and by the owner's ruling above part A's second reading lifts its block on
  this cut-over as far as part A goes. The records name these:
  - **The inputs on the default branch.** The inputs commit `24fe7cba` and the rule's revision `189d65ff`, with one new
    dated revision of the rule, land on the default branch by a merge commit before the base is taken (answer 3). By
    answer 3 that revision "lists the y-flag correction row and admits the ubuntu-24.04 workflow": the row is the one
    the bundle's README records as "**a finding for Broiler.VM's correspondence**, whose row is Broiler.VM's to add",
    and the workflow is the one the rule's part A fixes "by .github/workflows/test262-evidence.yml as this file's commit
    carries it", whose jobs `3b4b6860` of 2026-10-29 moved from `ubuntu-latest` to `ubuntu-24.04`.
  - **The hold and the base runs, at the default branch's head.** The rule reads: "**The base commit** is the
    default-branch commit that the merge landing SJL-11.5 has as its first parent. It descends from the inputs commit.
    Every base run is taken there, before the merge, while merges to `Broiler.JS/Broiler.JavaScript.*/` and
    `Broiler.ECMAScript/` are held (decision D16)." and "**The after commit** is that merge. If its first parent is not
    the commit the base runs were taken at, the base runs are void and are taken again before any after run is seen."
    Answer 3 reads "Then the hold starts and the base runs are taken at main's head."
  - **The owner's open items that Broiler.RegEx's records name for the cut-overs.** Its bundle's README lists them as
    "publication (D13); the departure from the rule's packed-from line (the reading's commit); the session's reading of
    class (b)'s vector clause; class (c)'s unretained pre-fix run; and the scratch branch `sjl-11-2-control-b`'s
    deletion". Its roadmap says the cut-overs "still wait on their own holds, H1 and H2, which this repository's records
    do not decide, and on the owner's items below", and those items are task 4.2's list quoted in the first note above,
    whose item 4, "**The owner may review this revision's wording**", names the record's own choices and the review's
    wording items, which the README's list does not name. Answers 1 and 2 above are the owner's on the README's first
    four items, and the owner's ruling above answers the blocking line, one of item 4's wording items. The owner's
    answers do not name the scratch branch, which is still in Broiler.RegEx on this note's date, or item 4's other
    wording items. Answer 2's chosen option reads "task 4.2 then waits only on D13 and the scratch branch's deletion";
    Broiler.RegEx's records at `ba7ac8cf` do not record answer 2, whether it closes item 4 for task 4.2 is
    Broiler.RegEx's to record, and this note reads nothing into it.

  No other record read here names a further condition of this merge, save decision D35 of Broiler.VM's JSD-0081, which
  reads of both cut-overs: "Both after SJL-12's after runs and after the candidate reading; either first; both in one
  release train". SJL-12's after runs have been taken: bundle [`sjl-12-001`](../evidence/sjl-12-001/README.md)'s README
  reads "Written 2026-10-26, at SJL-12's after evidence (work package 7.6), after every after run was taken." The
  candidate reading has been taken, by Broiler.RegEx's records above. Whether D35 asks more of the candidate reading
  than that it has been taken, such as task 4.2's close, and what "both in one release train" asks of this merge, no
  record read here says, and this note reads nothing into either. No run of the rule has been taken. Every clause of
  this engine's, 4 to 7, is unmet, and none of it is accepted.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses and the component's are judged in
their own bundles of the same name, and SJL-11 may not meet its gate before SJL-7 meets SJL-7's, and so not before
SJL-3 meets SJL-3's.

*(Noted 2026-11-18, on the branch `sjl-11-5-inputs`; the section above is left as written.)* **The rule's inputs
commit and its two revisions are filed for landing on the default branch, and have not landed.** The section reads
that part B runs "over a new benchmark class, `RegExpBenchmarks`, that a later inputs commit adds" and "Every base is
taken at the first parent of the merge that lands SJL-11.5, under a hold (decision D16)." The owner ordered the
landing on 2026-11-18, by an answer of the owner's own, not one taken under the direction of 2026-10-18: "Allow both"
— "The inputs land on main by a merge commit with one dated revision. The revision lists the y-flag correction row and
admits the ubuntu-24.04 workflow. Then the hold starts and the base runs are taken at main's head." The inputs commit
(`24fe7cba2dadbadb5917cf20e6c884d1ddabec85`) and the rule's first revision
(`189d65ffc84c3943338a5fab5be5e178fc4d3092`), both made on SJL-11.5's branch `sjl-11-5`, and that one dated revision,
[`decision-rule-revised-2026-11-18.md`](../evidence/sjl-11-001/decision-rule-revised-2026-11-18.md)
(`45a15487308eafb8c92d5325ffc22ee66e0a6475`, revised at review before it merged by
`909532bd2b2157eaf4364c4e8f6dfd0ff5e33f03`), are on the branch `sjl-11-5-inputs` with the default branch merged in,
filed to land by a merge commit before the base is taken. The revision names Broiler.VM's row M28 (merged in
Broiler-Platform/Broiler.VM#220, `79d728052f85bfc87da5a7510c2cf4ee667e72e0`) in part A's list, its `IsMatch` and
public `Replace` half as the cut-over's correction and its `split` half as a correction the cut-over does not make,
and has part A's runs taken by the evidence workflow as the default branch carries it at the base commit, admitting
`3b4b6860`'s move of its jobs to `ubuntu-24.04` and nothing else. Bundle
[`sjl-11-001`](../evidence/sjl-11-001/README.md)'s README names the commits; the merge that lands them is named after
it merges. No base is named and the hold has not started; no run of the rule has been taken, every clause of this
engine's, 4 to 7, is unmet, and none of it is accepted.

*(Noted 2026-11-19, on branch `sjl-11-5`; the note above is left as written.)* **The inputs landed, and the hold
started.** The note above reads "The rule's inputs commit and its two revisions are filed for landing on the default
branch, and have not landed." and "No base is named and the hold has not started; no run of the rule has been taken".
They landed on 2026-11-18 in Broiler-Platform/Broiler.JS#1125, merged as `5429c4065e8b273e5c7965598b8a2f5490a8ad98`,
a merge commit whose first parent is the default branch's `aecc5f89` and whose second is `20f1cbbb`, the head of
`sjl-11-5-inputs`: the inputs commit `24fe7cba` and the rule's revisions `189d65ff`, `45a15487` and `909532bd` are
default-branch commits from that merge on. The hold of decision D16 started there the same day, by the owner's order
quoted above ("Then the hold starts and the base runs are taken at main's head."), at `5429c406`, the default branch's
head when it started; nothing else merges to the default branch until the merge that lands SJL-11.5. The branch
`sjl-11-5` merged the default branch at `5429c406` into itself (`fff4172c`), which moves no default-branch commit.
The base runs of parts A, C and D were taken at `5429c406` during the hold and are retained in bundle
[`sjl-11-001`](../evidence/sjl-11-001/README.md)'s `base/`, whose README names each run; part B is not taken there,
and part E has no base run. `5429c406` is the base commit only if the merge landing SJL-11.5 has it as its first
parent; if the default branch moves before that merge, the base runs are void, as the rule says. No after run has been
taken, every clause of this engine's, 4 to 7, is unmet, and none of it is accepted.

*(Noted 2026-11-19, the same day; the notes above are left as written.)* **The owner's answers of 2026-11-19, quoted
verbatim.** Each is the owner's own, not a choice taken under the owner's direction of 2026-10-18, and they are
numbered as the answers of 2026-11-18 above continue.
- **4.** Asked: "SJL-11.5's part B (the RegExpBenchmarks measurement) runs arm B (base commit) and arm A (the merge)
  interleaved in every repetition, so it can only run after SJL-11.5 merges, and the rule judges it only on a
  controlled lane. Its 'every base run is taken there, before the merge' cannot hold for arm B as written. What should
  happen to part B?" Chosen: "On your lane after merge" — "After the merge, a local session on win-x64-r7-5800x builds
  arm B from 5429c406 and arm A from the merge and runs part B as written, as SJL-3's part B was run. A dated note reads
  arm B as built from the base commit, not as a run taken before the merge." Not taken: "Record it void" — "No part B
  run. It is recorded void, which by the rule leaves the measurement question open for this engine. It does not block
  anything."
- **5.** Asked: "Part D's base runs read the corpus at revision 43, but the rule names revision 32. The regexp vectors'
  ids, inputs and expected outputs are the same in both; only 22 class lines and the divergence lines changed, at
  revision 39. How should part D be read, at both the base and the after runs?" Chosen: "Read revision 32" — "As the
  rule says: the base reports are read again against revision 32's class lines through a scratch pin, with no new runs
  needed, and the after runs are read the same way." Not taken: "Read revision 43" — "Read the tree's revision 43 at
  both, with the equivalence of ids, inputs and outputs recorded. This departs from the rule's named revision."
- **6.** Asked: "Decision D35 orders both cut-overs "after SJL-12's after runs and after the candidate reading". The
  candidate reading is taken and merged (Broiler.Regex #28, #29, #30), but Regex task 4.2 stays open until the scratch
  branch sjl-11-2-control-b is deleted. Is D35 met for SJL-11.5's merge?" Chosen: "Yes, the reading is taken" — "D35
  asks only that the candidate reading come first, and it has. SJL-11.5 may merge once its PR's CI is green and the hold
  still holds." Not taken: "No, close task 4.2 first" — "SJL-11.5 waits until you delete the scratch branch and task
  4.2 is recorded closed. The hold stays on meanwhile."
- **How they bear here.** Bundle [`sjl-11-001`](../evidence/sjl-11-001/README.md)'s README records all three in a
  dated note with the rule's lines each reads. **Part B** is taken after the merge, on the controlled lane
  [`win-x64-r7-5800x`](../evidence/lanes/win-x64-r7-5800x/README.md), by a local session on the owner's workstation:
  arm B built from `5429c406`, arm A and A-again from the merge. The README reads the rule's "Every base run is taken
  there, before the merge" (`decision-rule.md:107`) for part B's arm B as an arm built from the base commit, beside
  `:109-110`, `:146` and `:147`, and revises no rule line. **Part D** is read at corpus revision 32 at the base and
  after: its base reports are read again against revision 32's class lines through a scratch pin, with no new run,
  and the reading is retained in the bundle's `base/part-d/`. **Decision D35**, which Broiler.VM's JSD-0081 reads
  "Both after SJL-12's after runs and after the candidate reading; either first; both in one release train; SJL-11.5's
  branch exists early for decision D14", is read as met for SJL-11.5's merge by the candidate reading's having been
  taken, which answers the second note of 2026-11-18 above, "Whether D35 asks more of the candidate reading than that
  it has been taken, such as task 4.2's close, ... no record read here says". Answer 6 does not name D35's "both in
  one release train", and this note reads nothing into it. By answer 6 "SJL-11.5 may merge once its PR's CI is green
  and the hold still holds"; Broiler.RegEx's task 4.2 stays open there, Broiler.RegEx's to record. No after run has
  been taken, every clause of this engine's, 4 to 7, is unmet, and none of it is accepted.

*(Noted 2026-11-19, the same day, after review; the two notes above are left as written.)* **Three corrections.**
- **The note it answers.** The note above reads "which answers the second note of 2026-11-18 above". The words it
  quotes, "Whether D35 asks more of the candidate reading than that it has been taken, such as task 4.2's close, ...
  no record read here says", are in the third note of 2026-11-18 in this section, "What the merge still waits on".
- **The rule's revisions.** The first of the two notes above reads "the rule's revisions `189d65ff`, `45a15487` and `909532bd`". The
  rule has two revisions, `189d65ff` and `45a15487`, the second revised at review by `909532bd` before it merged, as
  the note of 2026-11-18 on the inputs reads.
- **Part B's arm A.** The note above reads "arm A and A-again from the merge". The rule's part B fixes arm A as "the
  after commit, with the same two files", `RegExpBenchmarks.cs` and the benchmark `Program.cs` "as the inputs commit
  has them" (`decision-rule.md:146`). On the branch, `d892dd0b` removes `Program.cs`'s `--regex-tiering` block with
  the `RegexTieringMetrics` and the `RegexTiering` it calls (decisions D20 and D21), so the inputs commit's
  `Program.cs` does not build at the merge. How arm A is built is the owner's open question, and arm A is not built
  until the owner's reading is recorded; bundle [`sjl-11-001`](../evidence/sjl-11-001/README.md)'s README says the
  same in a dated note, with the files' blob ids, and revises no rule line. No after run has been taken, every clause
  of this engine's, 4 to 7, is unmet, and none of it is accepted.

*(Noted 2026-11-19, after SJL-11.5's merge; the notes above are left as written.)* **SJL-11.5 merged, the hold
ended, and the after runs of parts C, D and E were taken.** The notes above end "No after run has been taken, every
clause of this engine's, 4 to 7, is unmet, and none of it is accepted", and the last reads "How arm A is built is the
owner's open question, and arm A is not built until the owner's reading is recorded".
- **The merge.** SJL-11.5 merged to the default branch on 2026-11-19 as Broiler-Platform/Broiler.JS#1126,
  `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`, a merge commit whose first parent is the base commit
  `5429c4065e8b273e5c7965598b8a2f5490a8ad98` and whose second is `ce908940`, the head of `sjl-11-5`. It is the rule's
  after commit, `5429c406` is its base commit, and the base runs taken there stand. The hold of decision D16 ended with
  that merge.
- **The after runs.** Parts C, D and E's after runs were taken at the after commit, from a clean archive, with the base
  runs' commands, zone, cultures and toolchain, and are retained in bundle
  [`sjl-11-001`](../evidence/sjl-11-001/README.md)'s `after/`, with part C's and part E's controls in its `controls/`;
  the bundle's README names each file by its SHA-256 and reads each part by the rule. **Part C: read NOT MET.** One
  case, N05, passes the wall-clock limit at the base and throws its declared `SyntaxError` after, and neither class C1,
  whose base outcomes do not include passing the limit, nor C3, which asks that the case completed at the base, admits
  it; whether such a case reads under C1 is the owner's. **Part D: read MET on the retained runs**, at corpus revision
  32 by the owner's answer 5. **Part E: read MET on the retained runs.** Part A's after run, a dispatch of the evidence
  workflow at the after commit with the base run's inputs, has not been taken, and part A has no reading.
- **The owner's answer 7 of 2026-11-19, quoted verbatim**, the owner's own, not taken under the owner's direction of
  2026-10-18. Asked (2026-11-19): "SJL-11.5 part B's arm A is "the after commit, with the same two files"
  (RegExpBenchmarks.cs and the benchmark Program.cs "as the inputs commit has them"). RegExpBenchmarks.cs is unchanged
  at the merge. But the inputs commit's Program.cs calls the RegexTiering metrics that the cut-over deletes (D20/D21),
  so it cannot build at the merge. How should arm A be built?" Chosen: "Merge's own Program.cs" — "A dated revision,
  filed before any part B run, reads arm A's Program.cs as the merge's own. That file differs from the inputs commit's
  only by the removed --regex-tiering block, which part B never invokes. Its --regexp-answers path and the benchmark
  entry must be byte-identical; if they aren't, the run stops and comes back to you." Not taken: "Inputs file minus
  the block" — "Arm A uses the inputs commit's Program.cs with only the --regex-tiering block cut out, as a patched file
  named by hash in the manifest. It stays closer to the rule's words, but arm A then builds from a file no commit
  has."; "Record part B void" — "No part B run. The rule then leaves the measurement question open for this engine. It
  blocks nothing." The bundle's
  [`decision-rule-revised-2026-11-19.md`](../evidence/sjl-11-001/decision-rule-revised-2026-11-19.md) is that
  revision, filed before any part B run and written after other parts' runs were seen, which the rule calls weaker
  evidence; its condition holds at the after commit, as the bundle's README records. Part B's run on the lane
  `win-x64-r7-5800x` is still to be taken, by a local session on the owner's workstation.

No clause reading and no exit reading is taken here: no clause of this engine's, 4 to 7, has been read, and none of
it is accepted.

*(Noted 2026-11-19, later the same day, part A's after run and its reading, the owner's answer 8, and part C read a
second time; the notes above are left as written.)* **Part A: read MET on the retained runs; part C, read a second
time under a revision for case N05: read MET on the retained runs.** The note above reads "Part A's after run, a
dispatch of the evidence workflow at the after commit with the base run's inputs, has not been taken, and part A has
no reading" and, of part C's case N05, "whether such a case reads under C1 is the owner's".
- **Part A's after run** is a run of `.github/workflows/test262-evidence.yml`, which the bundle's README names,
  dispatched on 2026-11-19 on `main` at the after commit `b2b226cd` with the base run's inputs but for the commit, by
  the workflow blob the rule's revision of 2026-11-18 admits; every job concluded success, and its evidence artifact is retained unchanged in bundle
  [`sjl-11-001`](../evidence/sjl-11-001/README.md)'s `after/part-a/`, named by the zip's digest, with the rule's
  comparison of it with the base beside it. The bundle's README reads part A by the rule's MET line, each condition
  with the file and digest that shows it: both runs complete, one shell each, every shard in the zone, the two
  host-zone records the same, every duplicate-named-groups row passing at both commits, no row that passed at the base
  changed, and every changed verdict a C1 row of rows S7, S8 and S11, exactly the test262 files the rule's list names
  for them, each listed. No row moved for row M28's first half or through the `split` fallback, and no row is a
  finding against JSD-0081. **Part A: read MET on the retained runs.**
- **The owner's answer 8 of 2026-11-19**, the owner's own, not taken under the owner's direction of 2026-10-18,
  "Admit N05 by revision", is quoted verbatim, with the alternative not taken ("Keep part C NOT MET"), in the bundle's
  README and in the revision it orders,
  [`decision-rule-revised-2026-11-19b.md`](../evidence/sjl-11-001/decision-rule-revised-2026-11-19b.md). That
  revision quotes the rule whole and changes only part C: one named exception admits case N05 alone, past the
  wall-clock limit at the base and its declared `SyntaxError` after, and part C's MET line reads with it; the classes
  C1 and C3 and "no other class" stand for every other case. It was written after part C's runs and first reading had
  been seen, so its decision is weaker evidence under the rule's section "Revisions".
- **Part C, read a second time** on the same retained runs, with nothing rerun: after, every case gives its declared
  outcome; every case whose outcome differs from the base's is C3, as the first reading lists it, or is case N05 under
  its exception; and the control is reported as stated. **Part C, under the revision: read MET on the retained runs.**
  The first reading, "Part C: read NOT MET", stays on record beside it.
- **The rule's two revisions of 2026-11-19** are
  [`decision-rule-revised-2026-11-19.md`](../evidence/sjl-11-001/decision-rule-revised-2026-11-19.md), by answer 7,
  for part B's arm A, filed before any part B run, and `decision-rule-revised-2026-11-19b.md`, by answer 8, for case
  N05. Each was written after runs the rule judges were seen, and each is weaker evidence for that. The second changes
  nothing in part B.
- **Part B** is still to be taken, on the lane `win-x64-r7-5800x` by a local session on the owner's workstation, and
  has no reading.

No clause reading and no exit reading is taken here: no clause of this engine's, 4 to 7, has been read, and none of
it is accepted.

**Locale data and the Intl algorithms (SJL-9.1), filed 2026-10-26: all five sub-areas accepted, plural ranges and
display-name naming adopted, decisions SJL-D-22 and SJL-D-24 recorded, and the owner's decisions of 2026-10-26
recorded.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0082-locale-data-and-the-intl-algorithms-move-to-the-shared-libraries.md`
  (JSD-0082), decided on 2026-10-26. It states each condition of the extraction gate for the five sub-areas, each on its
  own: (a) locale identifiers, likely subtags, negotiation, Locale info and `supportedValuesOf`; (b) the Intl
  mathematical value, digit options and FormatNumericToString, with plural rules and plural ranges; (c) number
  formatting, with list, relative-time and display-name formatting; (d) date-time formatting, intervals, day periods,
  hour cycles and zone names with metazone history, with Intl over Temporal; and (e) duration formatting. It records
  decisions SJL-D-22 and SJL-D-24 as this area applies them, and the default-locale and available-locale providers that
  SJL-D-19 left to SJL-9. The home's design document,
  [`Broiler.ECMAScript/docs/intl/design.md`](../../Broiler.ECMAScript/docs/intl/design.md), gives the provider contract,
  the table format and its versioning, the generator's parameters and the host provider's scope.
- **Its input** is `Broiler.VM/docs/evidence/sjl-9-001/correspondence.md`, the correspondence from code. It read this
  repository at `f918b040`, Broiler.VM at `cd1928d` and test262 at `ccaac100`, and every behavioural cell was run in
  both engines: this engine's through the shell in UTC with no `LANG`, and, where a row depends on the host, also under
  a `tr_TR` or `de_DE` culture, in `America/New_York` or `Asia/Kolkata`, and under
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. It maps every `test/intl402` file this engine passes in bundle
  [`sjl-12-001`](../evidence/sjl-12-001/README.md)'s `base/part-a/` report (run 40196127730 at `4d813f2a`), the newest
  valid UTC whole-suite report with negative tests, to its rows and the locale data it needs, and it runs Broiler.VM's
  Intl reference datasets, but `orderings` and `segments`, in both engines. Every row carries its class, and every
  probe and answer is retained beside it.
- **SJL-9 started before SJL-7 and SJL-8, which it waits on with SJL-4's unsigned rounding, and SJL-3 met their
  gates**, at the owner's words of 2026-10-19, "Also continue with the other remaining SJL milestones", which the
  programme's ledger records in its SJL-9.0 entry, in Broiler.VM's own change for SJL-9.1. SJL-9 may not meet its gate
  before they meet theirs, and so not before SJL-3 meets SJL-3's.
- **The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
  review." Each decision SJL-9's plan left open is taken as it recommends, D1 to D31. The correspondence's findings F1
  to F21 are answered by D32 to D35 and by revisions of D7, D14, D15, D17 to D19, D21 and D24 to D26, each quoting the
  text it replaces, or recorded where they ask for no decision. JSD-0082 records them all as the owner's decisions of
  2026-10-26. **It changes no rule**: nothing is accepted without review, and the gate waits as above. In this record a
  decision is "decision Dn" and a row of the correspondence "row Xn".

This repository's part of it:
- **The sub-area verdicts, as this engine meets them.** Each sub-area moves to `Broiler.ECMAScript.Intl`, a new package
  that references `Broiler.ECMAScript.Algorithms` and `Broiler.Unicode.Algorithms` and that
  `Broiler.JavaScript.BuiltIns` takes by project reference (decision D9). The library is built in the order a to e, one
  merge per stage, and each engine's cut-over is one merge, prepared as one commit per sub-area (decision D3):
  - **a**: the locale-tag validation and canonicalisation, the hand-written alias tables, negotiation and
    `JSIntlLocale`'s parse helpers become `LocaleId` and `LocaleNegotiation`, with Locale info and `supportedValuesOf`;
  - **b**: `ExactRational`, which rounds a double's binary value, becomes the Intl mathematical value from
    `Number::toString`'s digits, with the digit options and FormatNumericToString; the CLDR package's plural rules over
    `double` become `PluralEngine`, with the operands `c` and `e` this engine already supplies (decision D20) and
    plural ranges;
  - **c**: the number engine and its hand-written compact and digit tables, and the list, relative-time and
    display-name internals, become the shared formatters;
  - **d**: `JSIntlDateTimeFormatEngine`, the English-only engine, and its interval and zone helpers become the pattern
    generator, the formatter, the interval formatter, day periods, hour cycles and zone names, with Intl over Temporal;
  - **e**: the duration-format internals become `DurationFormatCore`.

  This engine keeps its Intl objects and public types, option reading in the specification's order, the legacy
  constructors (rows N12 and T5), bound functions and parts arrays, the .NET format string of `toLocaleString`
  (decision SJL-D-17, row T3), its default-locale and available-locale providers, and the host-culture provider below.
  `Intl.Collator` and `localeCompare` keep their host path until SJL-17 (SJL-D-24 (c)).
- **X1: two adoptions.** This engine has no plural-range algorithm and no display-name naming: `selectRange` answers
  `"other"`, and `DisplayNames.prototype.of` answers the code. The shared library takes Broiler.VM's ResolvePluralRange
  over CLDR's plural ranges (rows P5b and N9d) and Broiler.VM's naming over CLDR's names (row S8) for every language
  this engine embeds. Every other sub-area has an implementation in both engines, and the shared one is Broiler.VM's,
  which follows the pinned text where this engine does not. X1 is met weakly for this engine's zone names and day
  periods, which are hand tables or GMT offsets (rows T17, T22 and T23).
- **Decision SJL-D-22, (a) with (c).** Broiler.VM's CLDR generator moves here with its pins and generates the locale
  data for both engines, each of which embeds its own tables (SJL-D-6 (c)). `Broiler.Unicode.Cldr.LocaleData` is not
  extended, and this engine's uses of it for Intl data go at SJL-9.5, with the package reference. This engine keeps a
  host-culture provider for the locales the shared data lacks. **Decision SJL-D-24 (c)**: collation is shared later,
  by SJL-17. *(Noted 2026-11-10, at SJL-9.5's records: this engine's uses of the package for Intl data are gone, but
  not "with the package reference". `Intl.supportedValuesOf`, sub-area (a), still reads the package's
  `IntlEnumerationData` for its currency and unit lists, and the shared library's `SupportedValues` is not wired for
  them, so `BuiltIns` keeps the reference and `Directory.Packages.props` the version until those lists move. Bundle
  [`sjl-9-001`](../evidence/sjl-9-001/README.md), "The cut-over's records (SJL-9.5)", records why.)*
  *(Noted 2026-11-11, on the owner's ruling of that day on SJL-9's clause 7, "Fix (1); move (2) to SJL-17": the note
  above ends "so `BuiltIns` keeps the reference and `Directory.Packages.props` the version until those lists move."
  They move in the change of 2026-11-11 that carries this note: `Intl.supportedValuesOf`'s currency and unit lists are
  the shared library's `SupportedValues.Of` over this engine's locale data, and the reference and the version are
  removed, no other project reading the package. The note above is left as written.)*
- **The data home** (decisions D10 to D12), `Broiler.ECMAScript/data/intl/` from SJL-9.2:
  - the CLDR 48.2.0 pin, a superset of Broiler.VM's whose entries are byte-identical to Broiler.VM's, with this engine's
    extra main-data files from the same packages; Broiler.VM's rule I4 holds its pin to the home's entry by entry;
  - the UCD files the generator reads, held equal to Broiler.Unicode's pin;
  - the generator, which moves whole, collation included, into `Broiler.ECMAScript.Generation`, with parameters for the
    main-data locales, the supplemental locales, the numbering systems, the calendars and the tailorings. With
    Broiler.VM's options it writes Broiler.VM's `JsCldrTables.g.cs` byte for byte; with this engine's it writes one file
    per table under `data/intl/generated/`, which `BuiltIns` embeds as resources, as the time tables are, held by rule
    SL7. A change to the table format is a major version of the library.
  *(Noted 2026-10-26, at SJL-9.2b, which built the home on branch `sjl-9-2b-home`: every entry of Broiler.VM's pin is
  in the pin's first block, byte for byte; the tarballs of the extra files' packages each match their `json-integrity`
  line in two retrievals; the UCD copies' lines equal Broiler.Unicode's; with Broiler.VM's parameters the source writer
  writes what Broiler.VM's generator writes at `4cf9ca44`, before its assurance pass, SHA-256
  `839bdc22afca187b58252f2487cd31c4b613396cd6596ffdcb6622fe1b534561` (bundle
  [`sjl-9-001`](../evidence/sjl-9-001/README.md), "The data home"); and this engine's tables are under
  `data/intl/generated/`, held by rule SL7 and by the completeness test of exit clause 3, and nothing in `BuiltIns`
  reads them before SJL-9.5; the bundle's section states the tables' sizes, as decision D5 says. Three readings the
  decisions leave to SJL-9.3 and SJL-9.5, which the bundle's section states with their counts: decision D13's table,
  which the shared data absorbs, names a default numbering system for locales of which only `ar-EG`, `fa` and `sa` are
  on decision D5's list, so the shared tables carry no default for the others (`bn`, `mr`, `ne`, `my` and the
  regional `ar-*` among them); for `sr`, `sr-Cyrl` and `sr-Latn`, CLDR's `hms` duration pattern writes `.` while
  their `timeSeparator` is `:`, which decision D34, reading the separator, leaves at `:`; and CLDR 48.2.0 lacks some
  per-locale names files for the list's locales, and calendar type or metazone names for a few of them, which the
  home's README and the bundle list. Decision D21 asks for each numbering system's own symbols through the locale's
  parent chain to root, and names only `common/main/root.xml` beside `cldr-json`, whose `numbers.json` carries a
  locale's default, native, traditional and finance systems alone; the home therefore also pins CLDR's `common/main`
  XML for the list's main-data locales and their parents, from the CLDR repository's tag `release-48-2`, and the
  generator writes each locale's own symbols from it, a reading of D21 and of D10's "from the same packages" that the
  bundle's section states for the owner to confirm.)*
  *(Noted 2026-11-11, on the owner's ruling of that day, "Confirm both", the owner's own dated choice, asked and
  answered. The note above ends "a reading of D21 and of D10's "from the same packages" that the bundle's section
  states for the owner to confirm."; the owner confirmed both readings as the home took them. The home's pin of
  CLDR's `common/main` XML for the list's main-data locales and their parents (which adds `en-001`) is decision D21's
  "through the locale's parent chain to root", beside the `root.xml` it names *(the words "and their parents (which
  adds `en-001`)" revised 2026-11-11, at review, to the words of the note above; read "and `en-001`")*; and those files' source, the CLDR repository at the tag
  `release-48-2`, which Broiler.VM's `root.xml` `repository-source` line names, is decision D10's "from the same
  packages". The alternatives not taken were "Confirm, revise text", dated revisions of D21 and D10 in Broiler.VM's
  JSD-0082, and "Do not confirm". Nothing in the home changes, and Broiler.VM's entries are unchanged. Bundle
  [`sjl-9-001`](../evidence/sjl-9-001/README.md), "The data home", records the ruling; JSD-0082 (Broiler.VM) gets its
  note on decisions D21 and D10 in Broiler.VM's half of SJL-9's exit reading. The note above is left as written.)*
- **The data contract** (decision D8): two layers, the tables as an `IIntlDataSet` and the questions as an
  `ILocaleData`, which this engine's host-culture provider also implements, ordered by `CompositeLocaleData`: the
  shared data first, then the optional secondary provider, then the parent and root. Broiler.VM keeps its
  `IJsIntlData` and `JsIntlTable` public and adapts them. The home's design document gives the contract.
- **The locale lists** (decision D5) are below this list.
- **The host-culture provider** (decision D6). It answers only what .NET exposes exactly: number symbols and native
  digits, month, day and era names, AM and PM, and the first day of the week, and only for locales the shared data
  lacks. Every other question answers "no data", so the algorithm uses the parent or root. It reports a locale
  unavailable when the host has no culture for it, and never throws, which part D of the rule checks under invariant
  globalization. Broiler.VM registers none.
- **The default-locale and available-locale providers** (decision D7, as revised for findings F2 and F3). The policies
  stay: the default is the host's `CurrentCulture`, `en-US` when it is empty or invariant, and the available locales are
  the host's cultures. Two readings follow from the pinned text:
  - **the default is available**: the available locales always include the default locale and the less narrow tags the
    same clause requires for it, so under invariant globalization they are `en-US` and `en`, where today they are none
    (row L44, a correction). Every service's available locales come from the available-locale provider as revised
    at SJL-9.5, `Intl.Collator`'s included: its collation data and comparison keep their host path until SJL-17, but
    its negotiation is this engine's shared one. Part D of the rule runs test262's
    `intl402/default-locale-is-supported.js` there, which tests `Intl.Collator` first;
  - **one default**: every reader of DefaultLocale reads the same provider, so under an `az`, `lt` or `tr` host culture
    `toLocaleLowerCase()` and `toLocaleUpperCase()` with no locale case by that culture's language, as the Intl
    services already resolve it (row L35). SJL-7.5's C8 is read as "casing reads the host's culture only through the
    default-locale provider", and its refusal of a `null` locale stands. No run of the rule takes a host culture;
    SJL-9.5 adds a test under one.
  *(Noted 2026-11-04, at corpus revision 35, on the owner's decision of that day, "Host only, fix vectors", their own
  dated choice, asked and answered: the policy above, "the available locales are the host's cultures", stays as
  written, and decision D7 is not read to make the embedding's locales available, the alternative not taken ("Host +
  embedded", a dated reading of D7 that would have made decision D5's extra locales available on every host). So
  decision D5's data for an extra locale answers only where the host lists a culture for it; on a host that lists
  none this engine resolves the locale as LookupMatcher finds it among the host's cultures (row L29 from SJL-9.5), to
  `en` or to the default `en-US`, as Broiler.VM's list does. The ICU 74.2 of the host the corpus is classified on
  lists none for `aa`, `ab`, `en-FR`, `en-Shaw`, `en-Shaw-GB`, `jbo` and `pap`, and SJL-9.5's stage-1 head
  `4ba95d2d` resolves `en-FR` to `en`, where vectors `intl/date-times/families/0091` and `0092` expected `en-FR`'s
  `"15:07"` and `h23` of this engine. Corpus revision 35 ([`conformance/vectors/REVISIONS.md`](../../conformance/vectors/REVISIONS.md))
  classifies, for this engine, the 14 vectors whose expectation then holds only where the host lists the locale,
  each field naming decision D7 and this note as this engine's record, since no task answers it on such a host, and
  restates the two `en-Shaw-GB` vectors whose answer moves with the resolution; no expected value and no Broiler.VM
  expectation changes. Broiler.VM's JSD-0082 records the decision by its own dated note with SJL-9.6's pin move.)*
- **The pins** (decision D14, as revised). ECMA-402 at `tc39/ecma402` `3d4c558180f643d7dea75f011a6355fb285f377e`, the
  last `main` commit on or before 2026-05-08, which already holds the Intl Locale Info proposal and names the
  operation IsWellFormedLanguageTag; the Intl era and month code proposal at its last commit on or before the same
  date; Intl over Temporal by `tc39/proposal-temporal@e8cc03fc`. SJL-9.2 records them in
  `conformance/vectors/sources/ecma402.pin`. test262 stays at `ccaac100`. **Where a test262 file both engines pass asks
  for an answer that text does not give, the file is the arbiter** (decision D33): rows S7 (the past pattern writes the
  number's magnitude: `RelativeTimeFormat/prototype/format/en-us-numeric-always.js` and
  `RelativeTimeFormat/prototype/formatToParts/en-us-numeric-always.js`) and T8 (`hourCycle` reported only for a
  format with an hour: `DateTimeFormat/prototype/resolvedOptions/hourCycle-dateStyle.js` and `hourCycle.js`) keep
  both engines' answer. Row L17's file, `Locale/prototype/getCollations/output-array-values.js`, asks `en` for a
  collation the text does not require; it keeps passing only if this engine's adapter hands the shared
  CollationsOfLocale its own Collator's list, which names one for `en`. SJL-9.5's local pre-check holds it, and SJL-17
  inherits the condition: this engine's Collator data for `en` keeps a collation, or SJL-17's rule names the file.
  The same list is what the rule's part C admits, under class V, for a `collations` line of the locales program's
  group of a Collator's tailorings: with it, this engine's `zh`, `zh-Hant`, `zh-TW`, `zh-Hans-SG` and `ko` lines
  equal neither the retained answers nor Broiler.VM's `[emoji,eor]`.
- **The resolved time zone** (decision D15, as revised for finding F9). This engine already reports the identifier as
  given, case-normalised, which the pinned Temporal text requires, and keeps it. The shared resolution answers the
  identifier and its primary, and the library names the zone by the primary, so this engine's names for an identifier
  whose CLDR names differ from its primary's, such as `Etc/GMT`, move to the primary's (row T26,
  implementation-defined). Broiler.VM's resolved identifier moves at SJL-9.4 (row Z8).
- **Metering** (decision D18, as revised for finding F8). This engine passes a `WorkCharge` that charges nothing, as in
  every shared area. The library charges where Broiler.VM charges today, in its units, which JSD-0082 names point by
  point, so Broiler.VM's fuel verdicts do not move.
- **Numbering systems** (decisions D13 and D21, as revised for finding F10). This engine's embedding carries each
  numbering system's own CLDR 48.2.0 symbols, for every locale of its list, through the parent chain to root, so
  `en-u-nu-arab` writes U+066B and U+066C and U+061C before the minus sign, where this engine writes the `latn` symbols
  today (rows N8a and U16, a data change). The reason is test262's
  `DateTimeFormat/prototype/format/numbering-system.js`, which this engine passes at its base with `arab`'s decimal
  separator (row T37), and which one shared symbol table must keep passing. Each locale's default numbering system
  comes from the same data: the CLDR 47.0.0 table, `JSIntlDefaultNumberingSystems.g.cs`, and its script are deleted,
  and the move to 48.2.0 is a data change, listed. Broiler.VM keeps the `latn` symbols, under N28's budget.
- **DurationFormat's digital format** (decision D34, for finding F11), a dated data decision. The generator writes each
  locale's clock separator and its `durationUnit` patterns, and [[TwoDigitHours]] is true where the `hms` pattern begins
  `hh`. So this engine's `digital` answers move where CLDR says: two-digit hours for `af` and `lt` (row U14), and `.` as
  the clock's separator for `da`, `en-DK`, `id` and `si` (row U13). The vectors carry them as data divergences, and the
  completeness test holds the keys for every locale of this engine's list.
- **The corrections** (decision D25, as revised for findings F6 and F7), the correspondence's column "Correction of
  Broiler.JS", row by row, each to be listed in `Broiler.JS/CHANGELOG.md` at SJL-9.5 under decision SJL-D-16 (a):
  - **a**: tags and option values with non-ASCII letters that culture casing folds into ASCII, or with a final line
    feed or non-ASCII digits (L2, L14, L45); a `-t-` language whose alias adds a subtag (L10); `Zzzz` and `ZZ` in likely
    subtags (L12); a Locale's information methods with the keyword (L15); `getCollations` for a language the Collator
    supports (L17); the hour cycles after the preferred one (L20); region preference (L21); `getTextInfo` with no known
    direction (L25); the resolved locale an available one (L29), with each service's relevant extension keys (L30);
    `Intl.DisplayNames.supportedLocalesOf` (L37); the default locale an available one (L44);
  - **b**: the Intl mathematical value from `Number::toString`'s digits, not the binary value (M1a), of an object
    whose primitive is a String (M1b), at RoundMVResult's zero edge (M2b) and of a BigInt (M3); the digit options'
    reads and refusals (M4) and defaults (M5); `trailingZeroDisplay` (M7); PluralRules' digit options (P1), `select`
    and `selectRange` of a BigInt (P2), `selectRange` when both ends format alike (P5a), `resolvedOptions` (P6) and the
    constructor's reads and refusals (P7);
  - **c**: `currencyDisplay: "name"` (N4); compact notation's exponent after rounding, its rounding, its currency form,
    a String beyond a double's precision and its magnitude (N6a, N6b, N6c, N6e, N6f); a range collapse that makes two
    different ranges print alike (N9b); options read again after construction (N10); the bound `format`'s identity
    (N11); `numberingSystem` in relative time (S5); DisplayNames' case regularization and validation of a code (S9);
  - **d**: the reads of Date's `toLocale*` (T2); the bound `format`'s identity (T4); `resolvedOptions` of the selected
    format's widths (T7); month, weekday, era and day-period names outside English, and other calendars' month, era
    and year names (T14, T35); `dayPeriod` with other fields (T19); the default zone's names (T27); a range that
    differs only in fields the format does not display, widened from the weekday-only range (T29); a non-Gregorian
    date's weekday (T34); the order of a Temporal value's checks (T39);
  - **e**: the display default after a numeric unit (U1); a sub-second unit given `"numeric"` (U2); a fractional unit
    displayed `"always"` (U3); a property bag's reads (U7); a String argument (U9).

  **Reclassed from the milestone's list and the plan's**: currency code spacing is data (N3b); range spacing and the
  collapse of shared affixes are implementation-defined (N9a, N17c); `selectRange` and `DisplayNames.of` are each two
  rows (P5a and P5b; S9 and S8); "German month names" widens to T14 and T35; and "option-reading order and repeated
  reads" holds for T2, N10, P7, M4 and U7, not for any constructor's option order, which both engines read alike.
  **Dropped**, fixed on 2026-10-16: the `timeZone` option ignored in one parts path and `formatToParts(8.64e15)` (row
  T28). The Collator's `compare` identity is SJL-17's.
- **The implementation-defined changes** this engine takes (C3), each listed by its row, the shared library keeping
  Broiler.VM's answer: every other `selectRange` (P5b); `unit: "percent"`'s long form, which keeps the percent pattern
  and so departs from CLDR's unit pattern (N2c, decision D35); compound units with no CLDR pattern (N5b); a unit name's
  plural (N5c); range spacing and the collapse of shared affixes (N9a, N17c); ranges written with names (N9d); pattern
  selection (T13); noon and midnight (T18); `dayPeriod` with a 24-hour cycle (T20); generic zone names (T23); the names
  of a non-primary identifier (T26, decision D15); interval patterns (T30); and the fallback of `islamic` and
  `islamic-rgsa`, from `gregory` to `islamic-tbla` (T33).
- **The data changes** (C5), each listed by its row and its decision (D5, D13, D21, D34) where this engine's answer
  moves: the rows of the column "Data policy", L5 to L8, L19, L22, L24, L26, P3, N1b, N2b, N3b, N3c, N5d, N6d, N7b,
  N8b, N9c, N9e, N14, S1b, S4b, S8, T10, T12, T15, T16, T17, T22, T24, T31, T36, T37 and U13 to U16, with N8a. Row L32
  is in that column too, but its data is each engine's collation data, SJL-17's (SJL-D-24 (c)), and no answer moves at
  SJL-9. The generated CLDR 48.2.0 tables replace the hand tables, the CLDR package, the host's ICU and the CLDR 47.0.0
  table; the `atTime` joins and U+202F are among them (T12).
  *(Noted 2026-11-09, at corpus revision 36, on the owner's decision of 2026-11-08, "Revision 36: follow CLDR", their
  own dated choice, asked and answered: where the pinned CLDR 48.2.0 contradicts a vector's expected value, the
  reference's answer (ICU 78.3, CLDR 48.0), this engine's answer from SJL-9.5 is the shared library's, which follows
  that data, and it is accepted; the alternative not taken, "Engine matches old answer", was engine-specific overrides
  against the pinned data. Two such data facts reach rows N3c and S1b: CLDR 48.2.0 gives `en-150` the currency pattern
  `#,##0.00 ¤`, U+00A0 before the sign (`common/main/en_150.xml` and cldr-json's `cldr-numbers-full`), which `en-DK`
  and `en-FR` inherit, where the reference answers `"-€1,234.50"`; and it gives `gv` and `ln` the list pattern
  `{0}, {1}` for start, middle, end and two alike, where the reference resolves both to `en-US` and answers
  `"A, B, and C"`. Corpus revision 36 ([`conformance/vectors/REVISIONS.md`](../../conformance/vectors/REVISIONS.md))
  classifies, for this engine, the five vectors these facts reach, `intl/numbers-and-text/families/0121`, `0141`,
  `0151`, `0309` and `0454`, under those rows as data changes (C5), each field naming the decision and this note as
  this engine's record, since no task answers it; no expected value and no Broiler.VM expectation changes. Not settled
  by it, and open for the owner's call before SJL-9.5 merges: CLDR 48.2.0 gives root's `{0}, {1}` list patterns to
  `aa`, `ab`, `en-Shaw`, `en-Shaw-GB`, `jbo` and `pap` too, whose row S1b vectors are normative with `"A, B, and C"`
  and match on the ICU 74.2 host the corpus is classified on, which lists none of them (decision D7, its note above);
  the windows-latest host of the engine vectors job lists `aa` and `pap` (corpus revision 28), where from SJL-9.5 this
  engine is expected to answer `"A, B, C"` for `families/0008` and `0514`, and revision 36 does not classify them.)*
  *(Noted 2026-11-10, at corpus revision 37, on the open point the note above leaves, "Not settled by it, and open for
  the owner's call before SJL-9.5 merges": it is settled under the owner's decision of 2026-11-04, "Host only, fix
  vectors" (decision D7, its note above), and needs no new decision. SJL-9.5's engine vectors job on windows-latest
  (run 42768566218, at `769f1b8e`), whose `icu.dll` lists `aa` and `pap`, answers 13 normative vectors of those two
  locales from CLDR 48.2.0's own data, where their expected value is the reference's answer, which resolves both
  locales to `en-US` since its ICU carries neither: `families/0008` and `0514` among them, with `"A, B, C"`, and
  with them the date pattern and names (row T16), the day period (T17), a duration's units and list (U15), the
  currency pattern (N3c), a long unit (N5d) and a relative time (S4b). Corpus revision 37
  ([`conformance/vectors/REVISIONS.md`](../../conformance/vectors/REVISIONS.md)) classifies, for this engine, those
  13 vectors under those data rows (C5), each field naming decision D7 and the note of 2026-11-04 as this engine's
  record, since no task answers it, as revision 35 does for a host that lacks a locale; on the ICU 74.2 host the
  corpus is classified on, which lists neither locale, this engine answers their expected value. No expected value
  and no Broiler.VM expectation changes. The open point's other locales, `ab`, `en-Shaw`, `en-Shaw-GB` and `jbo`,
  are not answered from their own data on either host of that run's engine vectors jobs. The same revision restates,
  in the same form, the fields of the 14 `aa` and `pap` vectors already classified whose closing clause, "answered by
  SJL-9.5", that run's windows-latest job contradicts, since SJL-9.5 answers them there from the same data.)*
  *(Noted 2026-11-09, on the owner's decision of 2026-11-09, "Keep + fix library first", and the owner's rulings of that day
  "Add file, same tarball", "Fix both consumers" and "Follow CLDR, dated ruling", each asked and answered: SJL-9.5's cut-over of
  sub-areas (d) and (e) stays held at its stage 3, and before it merges the shared library and the data home take, in a merge
  of their own, the five places where its date-time format departed from the pinned CLDR 48.2.0 data, as with the currency
  change of 2026-11-08; the alternative not taken was "Revert 8.5, fix later". The date-time format now reads CLDR's time
  data for a locale's language and region before its region's (row T10: `ar-001` `h12`, `fr-CA` `h23`; Intl.Locale's hour
  cycles too, row L19, in both engines), the whole locale's likely region (`zh-Hant` Taiwan's) and the region's calendar
  preference (`fa` `persian`, `th` `buddhist`, from CLDR's `calendarPreferenceData.json`, archived from the cldr-core tarball
  the pin already names; the alternative not taken was "Leave calendars out"), which no row names on its own and which are
  read under row T16's "the locale data of each" as data policy C5, and a numbering system's own decimal symbol, a locale's
  default system's included (row T37: `en-u-nu-arab`, and `fa` and `ar-EG` with no keyword), which replaces SJL-9.5's
  engine-side adapter; interval patterns keep ICU's widths, which the pinned reference shows to be the library's already.
  Row L16 keeps `getCalendars` at `["gregory"]`, so the date-time format's default and `getCalendars` disagree for `fa` and
  `th` in this engine. The root `THIRD_PARTY_NOTICES.md`'s CLDR entry, which lists the archive's files as Broiler.VM's, the
  main-data files and the `common/main` XML files, does not yet name the supplemental `calendarPreferenceData.json`; its
  licence sentence ("Every table in `data/intl/generated/` is derived from them") already covers the table. The entry gains a
  dated note naming the file, in the form of its SJL-17.3 note, at SJL-9.5's stage 4, since this merge's scope stops at
  `Broiler.ECMAScript/`, the bundle and this plan. *(Revised 2026-11-09, at review, before merge: the sentence read "The
  data rows T10, T16 and T37 now read CLDR's time data for a locale's language and region before its region's (`ar-001` `h12`, `fr-CA` `h23`; Intl.Locale's
  hour cycles too, row L19, in both engines), the whole locale's likely region (`zh-Hant` Taiwan's), the region's calendar
  preference (...) and a numbering system's own decimal symbol, which replaces SJL-9.5's engine-side adapter".)* Row T12's
  U+202F before a day period stays CLDR's, and this engine loses one test262 file under it,
  `intl402/DateTimeFormat/prototype/format/numbering-system.js`, listed by name: bundle `sjl-9-001`'s rule is revised for it,
  before SJL-9.5's judged runs, by the dated file
  [`docs/evidence/sjl-9-001/decision-rule-revised-2026-11-09.md`](../evidence/sjl-9-001/decision-rule-revised-2026-11-09.md),
  which admits that one path in parts A and B as class L; the alternative not taken was "Plain space like V8". Broiler.VM's
  date-time answers do not move; its Intl.Locale's `getHourCycles` moves for the tags CLDR's time data names an entry for, at
  its next re-pin, which also records the archived file and the rulings in its JSD-0082. The design's dated note of 2026-11-09
  on sub-area (d), [`Broiler.ECMAScript/docs/intl/design.md`](../../Broiler.ECMAScript/docs/intl/design.md), has the
  readings.)*
  *(Noted 2026-11-10, on the owner's decisions of 2026-11-10, "Dated part C revision" and "Accept, follows CLDR", each asked
  and answered after SJL-9.5's stage 4 local pre-check, which is not evidence. Row L19's move reaches one line of part C of
  bundle `sjl-9-001`'s rule: the `locales` program's `11 fr-CA hourCycles`, which this engine now answers `[h23,h12]`, from
  CLDR 48.2.0's time data for `fr-CA`, where the line's expected answer, Broiler.VM's as the pinned copies hold it, is
  `[h12,h23]`. The line is in a `spec` group, and no class of part C admitted it. The rule is revised for it, before
  SJL-9.5's judged runs, by the dated file
  [`docs/evidence/sjl-9-001/decision-rule-revised-2026-11-10.md`](../evidence/sjl-9-001/decision-rule-revised-2026-11-10.md),
  which admits that one line, by name, as class L19; the alternative not taken was "Keep old answer". The expected answer
  stays the pinned copy's after Broiler.VM's re-pin moves its own answer the same way, so the class still admits the line
  then. Part D compares no line with its expected answer, and is not revised. `fa`'s default `persian` calendar writes its
  minute and second as CLDR's `fa` data does, `۳:۰۵` with `minute` resolved `"numeric"` (rows T16 and T13). SJL-9.5's
  branch pins that answer in a unit test; no line of parts C and D reads it, so it needs no rule text.)*
- **What stays as it is.** The rows that are the same in both engines; and the engine policies, which the library's
  parameters keep this engine's: the default locale and zone, the available locales, the Collator's list, the meter
  and this engine's extensions (rows L28, L33, L40, L43, M9, N16, T3, T11, T40 and U19). Row L35 is the one engine
  policy whose answer moves, by decision D7.
- **Broiler.VM's corrections**, made at SJL-9.4: Z8 (decision D15), and L18 and L9 (decision D32). Neither Z8 nor L18
  is a change here: this engine already reports the identifier as given, and its Collator supports the languages L18
  names. L9's inputs move here at SJL-9.5 with the generated alias table, a data change under row L8.
- **The deletion list** (decision D24, as revised for finding F14), as JSD-0082's X6 states it for this engine:
  - in `JSIntl`: the locale-tag validation and canonicalisation, the hand-written alias tables, negotiation, the number
    engine with `ExactRational` and its compact and digit tables, and the plural, list, relative-time, display-name and
    duration-format internals;
  - `JSIntlDateTimeFormatEngine`'s English engine, interval patterns and zone helpers; `JSIntlLocale`'s parse helpers;
  - this engine's uses of `Broiler.Unicode.Cldr.LocaleData` for Intl data;
  - `JSIntlDefaultNumberingSystems.g.cs` and `scripts/intl/generate_default_numbering_systems.py` (decision D13);
  - the members of `JSIntlDateTimeFormat` no caller reaches (row T42): the constructor from a `CultureInfo`,
    `Format(DateTimeOffset, JSObject)`, `FormatEnglishHour(DateTimeOffset)`, `FormatDayPeriod(DateTimeOffset, string)`
    and the `locale` field, as row T42 names them. The overload `FormatEnglishHour(int)` is not among them: the
    `dayPeriod` paths of `format` and `formatToParts` call it.

  *(Noted 2026-11-11, on the owner's ruling of that day on SJL-9's clause 7, "Fix (1); move (2) to SJL-17", the
  owner's own dated choice, asked and answered. The list's first item names "negotiation" among `JSIntl`'s entries, and
  its third "this engine's uses of `Broiler.Unicode.Cldr.LocaleData` for Intl data". By the ruling's second part, the
  Collator's hand resolution leaves this list for SJL-17's, at SJL-17.5: `GetUnicodeExtensionType`,
  `RemoveUnicodeExtensionKeyword` and `ReflectExtensionKeyword`, `ResolveLocaleFromCanonical`'s keyword filter over
  `CollatorRelevantKeys`, and `JSIntlCollator`'s `StripUnicodeExtension`, so "negotiation" here is read as negotiation
  other than the Collator's, which the deletion list under "Collation (SJL-17.1)" below takes (its dated note). That is a
  revision of the list, and clause 7's reading rests on it for those entries, which is weaker evidence than the list as
  written. By the ruling's first part, the change of 2026-11-11 that carries this note removes the third item's last
  use, `Intl.supportedValuesOf`'s currency and unit lists, which are the shared library's `SupportedValues` now, with
  `BuiltIns`' package reference and the package's version in `Directory.Packages.props`, which no other project reads.
  The alternatives not taken were "Fix both in code now" and "Revise the list for both". JSD-0082's decision D24 and
  the milestone's SJL-9.5 deletion list are Broiler.VM's, and they get their dated notes in Broiler.VM's half of the
  reading. The list above is left as written.)*
- **Decision SJL-D-25** (decision D24). The public Intl types, `JSIntlNumberFormat` and the others, stay as the
  engine's Intl objects with every public member, disposition (a), and nothing public is removed.
  `JSIntlDateTimeFormat`'s public static `Get(CultureInfo)` and `ApplyAllDefaults` stay, over the library, a culture's
  name taken as the locale. SJL-9.5 records them in [`docs/public-api.md`](../public-api.md).
  *(Noted 2026-11-10, at SJL-9.5's records: `Get(CultureInfo)` runs over the library, a culture's name taken as the
  locale. `ApplyAllDefaults` and `ToDateTimeOptions` stay, hand-written, but no caller in this repository reaches them
  any more and neither calls the library, since `Date.prototype`'s `toLocale*` methods hand their required and defaults
  to the library's CreateDateTimeFormat (row T2); `docs/public-api.md`'s section of 2026-11-10 records it.)*
- **The leftover items of this repository's bundles** (decision D26, as revised for finding F15) each have a row:
  `sjl-3-001/README.md:207` (the plan cited `:194`), `sjl-4-001/README.md:267-268, 278`, `sjl-6-001/README.md:356, 368`
  and `sjl-7-001/README.md:172, 548`. The correspondence's section "The leftover items (D26)" gives each verdict; the
  minute rounding of `GmtOffset` and the parts path's host zone were fixed before it ran.
- **Acceptance data** (decisions D16, D17 as revised, D22 and D23). Broiler.VM's Intl reference datasets, all but
  `orderings` (SJL-17's), are copied into `conformance/datasets/intl/` at SJL-9.2, and their divergence groups are
  classed in the bundle's [`divergence-groups.tsv`](../evidence/sjl-9-001/divergence-groups.tsv), read with the dated
  corrections its README records, both as the rule commit holds them. The Intl vectors take their expected values from
  ICU through the Node.js release the bundle's [`reference.txt`](../evidence/sjl-9-001/reference.txt) names, which
  answers CLDR 48.0, each divergence argued against a clause of the text; three readings are fixed before any vector exists, for DurationFormat (rows U1 and U3),
  for U+202F (row T12, a date-time string being the concatenation of the reference's parts) and for rows U7 and U9,
  which no dataset reaches. No new reference datasets: this engine's extra locales rest on test262, the vectors and
  part C's class R. Each library stage is gated by a differential check against Broiler.VM's code at its rule commit,
  test-only and JIT only, and no J-arm copy of `JSIntl` is made, since it depends on the host. There is no measurement
  part (decision D23).
- **The rule**, [`docs/evidence/sjl-9-001/decision-rule.md`](../evidence/sjl-9-001/decision-rule.md), is committed
  before any code of the area, with the locale lists, the reference and the divergence groups (decision D30):
  - A, test262 per path in UTC with negative-metadata tests, its classes C1, C3 and C5 naming the rows above;
  - B, the same in `America/New_York`;
  - C, Broiler.VM's Intl reference datasets run in this engine against Broiler.VM's answers, with the classes V
    (Broiler.VM's coverage, a numbering system's symbols and this engine's Collator's own lists included), R (the
    release), Z8, O (`segments.js`, read first) and H (the default-locale provider);
  - D, part C's programs and `default-locale-is-supported.js` under invariant globalization, with no host exception
    escaping, for each Intl service constructor that file tests, `Intl.Collator` among them.

  **The bases** are taken at the base commit, the first parent of the merge that lands SJL-9.5, and the after runs at
  that merge (decision D4): parts A and B by `test262 evidence`, dispatched together promptly after the merge, and parts
  C and D locally. Broiler.VM cuts over first (decision D28), and the stream version that carries the library is
  published with the owner's approval before Broiler.VM's cut-over merges (decision D29).
- **A dated note** on the data decision of 2026-10-15, above, records that SJL-9.5 supersedes it (decision D13).

**Decision D5's two lists.** They are the text of task 1.2, read from bundle
[`sjl-9-001`](../evidence/sjl-9-001/README.md)'s `locales/`, which `scripts/intl/enumerate_locales.py` wrote from
bundle `sjl-10-001`'s after report (run 39252310861 at `00b65dd0`), test262 `ccaac100`, and Broiler.VM `01444a59`'s
datasets and CLDR 48.2.0 pin. Two departures from that text are of substance, and both are marked here: its sentence on
the report read, which finding F17 restates in this paragraph, and its "re-run on the report of SJL-9's base before
SJL-9.2 generates", which cannot be met, since SJL-9's base report is taken only at SJL-9.5, after its merge (decision
D4). Otherwise only the punctuation and this record's name for this engine differ. The population the correspondence
maps is bundle `sjl-12-001`'s `base/part-a/` report (run 40196127730 at `4d813f2a`), the newest valid UTC whole-suite
report with negative tests; both reports give the same verdict on every `test/intl402` path and variant. The lists are
run again before SJL-9.2 generates any table, on the newest valid UTC whole-suite report with negative tests of this
repository's default branch at that time. Tags are in BCP 47 case, and a deprecated language is shown with CLDR's
replacement.
- **List 1, Broiler.VM's embedding:** `de`, `de-DE`, `en`, `en-US` and `und`, unchanged, so that rule N28 holds
  `JsCldrTables.g.cs` byte for byte. It does not grow; SJL-D-22's size budget binds it.
- **List 2, this engine's embedding.** Its main data is the union of four sources:
  1. **the languages `Broiler.Unicode.Cldr.LocaleData` serves today**: `ar`, `de`, `en`, `es`, `fr`, `it`, `ja`, `ko`,
     `nl`, `pl`, `pt`, `ru`, `sv`, `tr`, `zh`, each also named by source 2;
  2. **every locale the intl402 files passing at the base and the reference datasets name**, the bundle's
     `locales/union.tsv`:
     - *languages with CLDR 48.2.0 main data*: `aa` (also as `aar`), `ab`, `af`, `ar`, `az`, `bg`, `cs` (also as
       `ces`), `da`, `de`, `en`, `es`, `fa`, `ff`, `fil` (as `tl`), `fr`, `gv`, `he` (also as `iw`, `heb`), `hi`, `hy`,
       `id` (also as `in`), `it`, `ja`, `jbo`, `ko`, `ln`, `lt`, `nb`, `nl`, `no`, `pap`, `pl`, `pt`, `ro` (also as
       `mo`), `ru`, `sa`, `si`, `sl`, `sr` (also as `sh` and `cnr`), `sv`, `th`, `tr`, `ur`, `uz`, `yi` (also as `ji`),
       `zh` (also as `cmn`), `zu`;
     - *languages named only by canonicalization and likely-subtags tests, with no CLDR main data*: `aae`, `art`
       (`art-lojban`), `ary`, `cel` (`cel-gaulish`), `hak`, `hsn`, `hyw`, `sgn` (`sgn-GR`). They have nothing to embed;
       their tests read the alias and likely-subtags tables, which are global;
     - *named locales that are CLDR 48.2.0 locales in their own right* (in `availableLocales.json` or
       `defaultContent.json`): `ar-001`, `ar-EG`, `de-AT`, `de-CH`, `de-DE`, `en-150`, `en-AU`, `en-DK`, `en-FR`,
       `en-GB`, `en-IN`, `en-NZ`, `en-Shaw`, `en-Shaw-GB`, `en-US`, `es-419`, `es-ES`, `fa-IR`, `fr-CA`, `fr-FR`,
       `he-IL`, `hi-IN`, `hy-AM`, `id-ID`, `ja-JP`, `ko-KR`, `nl-BE`, `pl-PL`, `pt-BR`, `pt-PT`, `ru-RU`, `sr-Cyrl`,
       `sr-Cyrl-RS`, `sr-Latn`, `sr-Latn-RS`, `sv-SE`, `th-TH`, `uz-Cyrl`, `zh-Hans`, `zh-Hans-CN`, `zh-Hans-SG`,
       `zh-Hant`, `zh-Hant-TW`, with the languages above;
     - *the other named base names* (`zh-TW`, `zh-CN`, `en-RU`, `de-DD`, `und-AQ` and the rest of `union.tsv`) have no
       CLDR locale of that name. The library resolves them as any tag, by likely subtags, then truncation and parent
       locales, so they add no main data beyond their resolution; `zh-TW` resolves to `zh-Hant-TW`;
     - *the strongest signal* is test262's own `locale:` metadata (counted as `metadata`), the locales a test declares
       it needs: `ar`, `ar-EG`, `de`, `de-AT`, `de-DE`, `en`, `en-IN`, `en-US`, `es`, `fa`, `fr`, `gv`, `hi`, `it`,
       `ja`, `ja-JP`, `ko`, `ko-KR`, `ln`, `pl-PL`, `pt-PT`, `si`, `sl`, `sr`, `sr-Thai-RS`, `sv`, `th`, `zh`, `zh-CN`,
       `zh-TW`, and `en` and `zh` with calendar, numbering-system and time-zone keywords;
  3. **`en-US`**, the default-locale provider's fallback (decision D7);
  4. **`und`**.

  **Supplemental data**: the plural, ordinal, plural-range and day-period tables cover every CLDR language, and the
  global tables are as Broiler.VM's.
- **Readings.** Exit clause 3 is met per embedding. SJL-D-22's "size budget in Broiler.VM's evidence" binds list 1,
  which does not grow. The sizes are measured by the generator at SJL-9.2 and stated in this engine's bundle, which has
  no budget rule. Each later addition is a dated data decision with its measured size.
- **What the enumeration does not see**, a known gap: tags a program computes at run time. The harness's
  `getLocaleSupportInfo` (`harness/testIntl.js` at `ccaac100`) builds its tags from the languages `zh`, `es`, `en`,
  `hi`, `ur`, `ar`, `ja` and `pa`, the scripts `Latn`, `Hans`, `Deva`, `Arab`, `Jpan`, `Hant` and `Guru`, and a list of
  regions, and the files that use it, each passing in the report read, are
  `test/intl402/fallback-locales-are-supported.js`, `test/intl402/supportedLocalesOf-consistent-with-resolvedOptions.js`
  and `test/intl402/supportedLocalesOf-unicode-extensions-ignored.js`. So `pa`, and every tag with the script `Guru`,
  appear nowhere in `union.tsv`. These files test that `supportedLocalesOf` and `resolvedOptions` agree with each other,
  not any locale's data, and they take whatever the host reports supported; availability stays the host's (decision D7).
  The gap is recorded, and no locale is added for it.
- **What the enumeration lists for a reader.** Literals in other positions that look like tags (era codes, calendar
  names, units, region codes, values compared with a result) are in `locales/uncounted.tsv` as `position`; every
  language among them that is a locale (`de`, `en`, `ar`, `zh`, `ja`, `fr`, `sr`) is named elsewhere in the lists. A
  locale passed to a test's own helper or subclass (`compareFTPtoFormat` in the `formatToParts/main.js` files, `new
  CustomLocale` in `Locale/subclassing.js`) is counted as `wrapper`. Literals that sit where a locale goes but
  whose language CLDR 48.2.0 does not know (`tlh`, `zxx`, `jp`, `zz`, `xyz`, `xyz-DE`, `mul`) are listed as
  `language:<ways>`; none has CLDR data to embed.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 11 to 15 are judged in its bundle of
the same name, and SJL-9 may not meet its gate before SJL-7 and SJL-8 meet theirs, and so not before SJL-3 meets
SJL-3's.

**This engine's cut-over (SJL-9.5), its state on 2026-11-10.** *(Added 2026-11-10, at SJL-9.5's records, plan task
8.7.)* All five sub-areas are cut over on branch `sjl-9-5-cutover`, one merge prepared as one commit per task, with
the review's fixes and merges of the default branch, the last of them the library's date-time fix
(Broiler-Platform/Broiler.JS#1108). The branch is not merged, and no part of bundle `sjl-9-001`'s rule has been run as
evidence: the bases and the after runs are taken at the merge that lands SJL-9.5 (decision D4).
- **The records are filed on the branch.** [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md), "Intl moves to
  the shared Intl library", lists every correction, implementation-defined change and data change by its row
  (decision D25); [`docs/public-api.md`](../public-api.md) records that no public member is removed or changes its
  signature (decision SJL-D-25, D24); the root `THIRD_PARTY_NOTICES.md` names the archived
  `calendarPreferenceData.json`, as the note of 2026-11-09 above said it would at SJL-9.5, and the embedded Intl
  tables, and no longer names the deleted CLDR 47.0.0 table and its script; and bundle
  [`sjl-9-001`](../evidence/sjl-9-001/README.md) holds the deletion search, which finds no deleted type or member
  referenced by code, in `cutover/`.
- **The owner's rulings of 2026-11-10**, each asked and answered. "Dated part C revision": part C's `fr-CA`
  `hourCycles` line, which row L19's move makes differ from Broiler.VM's expected answer in a `spec` group, is admitted
  by a dated revision of the rule, filed on the default branch in a merge of its own; the alternative not taken was
  "Keep old answer". "Accept, follows CLDR": `fa`'s default `persian` calendar writes its minute and second as CLDR's
  `m:ss` (rows T16 and T13), and the test that pinned the `gregory` answer asks `fa-u-ca-gregory`; the alternative not
  taken was "Revert the test change".
- **Where the records depart from this plan.** Task 8.7 says to remove `Broiler.Unicode.Cldr.LocaleData`; it stays for
  `supportedValuesOf` (the note on decision SJL-D-22 above). Bundle `sjl-8-001`'s collector names the deleted engine
  file and is left as it is, since it reads that bundle's own after commit. `Broiler.ECMAScript/docs/intl/design.md`
  still names the deleted adapter `NumberingSystemDecimalData`, and the library's next records merge owes it a dated
  note. Task 8.4 says the bound `format` and `compare` getters are cached; Broiler.VM's JSD-0082, decision D25, lists
  only the bound `format` of NumberFormat and DateTimeFormat (rows N11 and T4), which are made once, and the Collator's
  `compare` stays with the Collator until SJL-17.5. Task 8.5 names only `JSIntl.cs` and the engine file; the Date
  formatters and the Temporal callers changed with them. `GetUnicodeExtensionType`, `RemoveUnicodeExtensionKeyword`
  and `ReflectExtensionKeyword`, which the deletion list names, stay, since the Collator reads them until SJL-17.5.
  *(Noted 2026-11-11, on the owner's ruling of that day on SJL-9's clause 7, "Fix (1); move (2) to SJL-17". This item
  begins "Task 8.7 says to remove `Broiler.Unicode.Cldr.LocaleData`; it stays for `supportedValuesOf`" and ends
  "`GetUnicodeExtensionType`, `RemoveUnicodeExtensionKeyword` and `ReflectExtensionKeyword`, which the deletion list
  names, stay, since the Collator reads them until SJL-17.5." The departure on task 8.7 is closed by the change of
  2026-11-11 that carries this note: `supportedValuesOf`'s currency and unit lists are the shared library's
  `SupportedValues`, and `BuiltIns`' reference to the package and its version in `Directory.Packages.props` are gone.
  The departure on the three members is a departure no longer: by the ruling's second part those three members, with
  `ResolveLocaleFromCanonical`'s keyword filter over `CollatorRelevantKeys` and `JSIntlCollator`'s
  `StripUnicodeExtension`, are on SJL-17's deletion list, at SJL-17.5, and not on SJL-9's (the dated notes on both
  lists), so "which the deletion list names" is read as SJL-17's list from that day. The item is left as written.)*
- **The default numbering system outside decision D5's list.** The reading the note of 2026-10-26 above left to
  SJL-9.3 and SJL-9.5, decision D13's table naming defaults for locales the list lacks, is taken up by decision D6's
  host-culture provider: for a locale the shared data lacks, the default is the host culture's native digits, so it
  depends on the host's cultures. Bundle [`sjl-9-001`](../evidence/sjl-9-001/README.md) records it by a dated note.
- **Before the merge.** The branch merges the default branch once the part C revision has landed and runs part C again
  to show the line admitted; the windows-latest engine vectors job shows how `aa` and `pap` answer (corpus revision
  36's open point); and each judged run of parts A and B keeps the class-L probe the rule revision of 2026-11-09 asks
  for.

**The evidence (SJL-9.6), its state on 2026-11-10.** *(Added 2026-11-10, at SJL-9.6, plan tasks 9.4 to 9.6.)* SJL-9.5
merged as Broiler-Platform/Broiler.JS#1110 (`a5f2692e`, the after commit), with corpus revision 37 folded into it (the
owner's choice of 2026-11-10, "Fold into #1110"); its first parent, `88417996`, is the base. Bundle
[`sjl-9-001`](../evidence/sjl-9-001/README.md), "The evidence (SJL-9.6)", reads each part of its rule:
- **Parts A and B**, test262 per path in UTC and in `America/New_York` with negative tests, from the workflow's runs
  at both commits in each zone: NOT MET as the rule is written, since `DateTimeFormat/prototype/format/numbering-system.js`
  passes at the base and fails after in both variants; MET as the revision of 2026-11-09 restates them, that path being
  class L (row T12, the owner's ruling "Follow CLDR"), with its probe run in the after runs' own shell, rebuilt as the
  workflow builds it and found identical by its binaries' digests. No other verdict of the suite moved in either zone.
- **Part C**, Broiler.VM's Intl reference datasets: NOT MET as written, since `locales:128` (`fr-CA` `hourCycles`)
  differs in a `spec` group; MET as the revision of 2026-11-10 restates it, that line being class L19, and every other
  differing line being in class V, O or Z8, listed in the bundle's table, with the reference's run beside it.
- **Part D**, the providers under invariant globalization: MET as written; no line is an `abort:` line and
  `default-locale-is-supported.js` passes after in both variants.
- **The home** at the after commit: the shared folder's tests pass, the data home's and the Intl library's included,
  but for one SJL-13 frontend test that reads a bundle folder the archive left out and that passes with it restored
  (the bundle's README), and the Native AOT probe runs the corpus with no failure *(revised 2026-11-10, at review;
  read "the shared folder's tests pass, the data home's and the Intl library's included, and the Native AOT probe runs
  the corpus with no failure")*; the pristine consumer of the engine set runs, its Intl
  checks hold, and the shared Intl package carries no table's bytes, which this engine's `BuiltIns` package embeds.

Each part met only as revised is weaker evidence than a part met as written, as each revision says. Not done here, and
owed to the next stage: the corpus revision that retires both engines' Intl divergences, Broiler.VM's per-engine Z8
expectation among them (plan task 9.6), and then Broiler.VM's pin move, with the notes its JSD-0082 owes at that re-pin.

*Added 2026-11-10:* corpus revision 38 (`conformance/vectors/REVISIONS.md`) is that revision. It retires Broiler.VM's 41
Intl divergence fields (rows Z8, L9 and L18, corrected at SJL-9.4) and 1,705 of this engine's 1,773, which SJL-9.5
answers on both CI hosts, and makes row Z8's 157 time-zone vectors `normative`, the two engines' expectations merged
into out (decision D15). It keeps 68 of this engine's fields: the 44 whose answer depends on whether the host lists one
of decision D5's extra locales (decision D7, revisions 35 to 37), and 24 that still hold on both hosts, the four of the
owner's decision of 2026-11-08 and the 20 the changelog lists among the answers that still differ after SJL-9.5. Broiler.VM's
pin move is the next stage.

**SJL-9's exit reading, Broiler.JS's half, 2026-11-11.** *(Added 2026-11-11.)* The paragraph above ends "Broiler.VM's
pin move is the next stage."; that stage is taken, Broiler.VM's pin moved to corpus revision 38
(Broiler-Platform/Broiler.VM#199, observed), and SJL-9.6 has nothing left in either repository. Bundle
[`sjl-9-001`](../evidence/sjl-9-001/README.md), "SJL-9's exit reading, Broiler.JS's half", reads the milestone's clauses
1 to 10, the data home's, the Intl library's and this engine's, against its evidence at the after commit `a5f2692e`. It
takes no run, changes no rule and revises no clause:
- **Read met:** clauses 1 to 4, 6, 9 and 10. Clauses 2 and 10 rest on the rule and the home's tests as written, and
  clauses 4 and 6 too, save that the assurance half of rule SL8 rests on the shared workflow's push run at the after
  commit, observed and not retained. Clauses 1 and 3 rest, for the `common/main` XML files of the pin's third block,
  on the owner's ruling of 2026-11-11, "Confirm both" (the dated note under "The data home" above). Clause 9 rests on
  parts A and B as their revision of 2026-11-09 restates them, weaker evidence; as the rule is written it is not met.
- **Read met for this engine's share only:** clause 5, on part C as its revision of 2026-11-10 restates it, weaker
  evidence. It holds only with Broiler.VM's dataset runs after its cut-over, which decision D22 adds, and is completed,
  or not, by Broiler.VM's half reading them.
- **Not met:** clause 7, "The deletion list is absent". Two of its entries stay at the after commit, as this plan's
  records of the cut-over already say: this engine's use of `Broiler.Unicode.Cldr.LocaleData` for Intl data, the
  currency and unit lists of `Intl.supportedValuesOf`, with the package reference; and the Collator's hand resolution
  of its `co`, `kf` and `kn` keys, kept until SJL-17.5. No decision revises the list for either.
- **Not met:** clause 8, "Host globalization remains only behind the optional providers". It holds under invariant
  globalization, part D as the rule is written. Outside it, `Decimal.prototype.toLocaleString` reads the host's
  culture directly, behind no provider, and no decision keeps it, where the .NET format-string extension's readers
  fall outside the clause by SJL-D-17. The bundle says what would meet each of clauses 7 and 8; choosing is the owner's.
- **Open, and not done here:** corpus revision 38's kept fields whose reasons still end "answered by SJL-9.5", owed to
  a later restating revision; the shared stream's next version, published only with the owner's approval, which
  carries row L19's `getHourCycles` moves to Broiler.VM; the dated note the design document owes on the deleted
  adapter; JSD-0082's own note on decisions D21 and D10, in Broiler.VM's half; and clause 8's reader, which task 8.4
  neither moved onto the default-locale provider nor recorded.

*(The list above revised 2026-11-11, at review. Its first item read "**Read met:** clauses 1 to 6 and 8 to 10. Clauses
2, 4, 6, 8 and 10 rest on the rule and the home's tests as written. Clauses 1 and 3 rest, for the `common/main` XML
files of the pin's third block, on the owner's ruling of 2026-11-11, "Confirm both" (the dated note under "The data
home" above). Clause 5 rests on part C as its revision of 2026-11-10 restates it, weaker evidence, and holds only with
Broiler.VM's dataset runs after its cut-over, which decision D22 adds and Broiler.VM's half reads. Clause 9 rests on
parts A and B as their revision of 2026-11-09 restates them, weaker evidence; as the rule is written it is not met.";
its clause 7 item ended "No decision revises the list for either. The bundle says what would meet the clause;
choosing it is the owner's."; and its last item ended "and JSD-0082's own note on decisions D21 and D10, in
Broiler.VM's half.". The clause 5 and clause 8 items are new.)*

This is no verdict on SJL-9. Its gate waits on Broiler.VM's clauses 11 to 15, which Broiler.VM's half of the exit
reading reads in its bundle of the same name, and on SJL-7 and SJL-8 meeting theirs, and so on SJL-3 meeting SJL-3's.
None of it is accepted: no second holder exists to review it.

**Clauses 7 and 8 read again, 2026-11-11 (after the owner's rulings).** *(Added 2026-11-11.)* The exit reading above
says "The bundle says what would meet each of clauses 7 and 8; choosing is the owner's." The owner chose that day, each
asked and answered: for clause 7, "Fix (1); move (2) to SJL-17", the alternatives not taken being "Fix both in code
now" and "Revise the list for both"; for clause 8, "Fix in code", the alternative not taken being "Record as SJL-D-17
path". Branch `js-c78` carries the change: `Intl.supportedValuesOf`'s currency and unit lists are the shared library's
`SupportedValues`, and `BuiltIns` no longer references `Broiler.Unicode.Cldr.LocaleData`, whose version leaves
`Directory.Packages.props`; the Collator's hand resolution moves to SJL-17's deletion list, at SJL-17.5, by the dated
notes on this section's deletion list and on SJL-17's; and `Decimal.prototype.toLocaleString` formats through the
shared Intl number formatting, its locale with no `locales` the default-locale provider's. Bundle
[`sjl-9-001`](../evidence/sjl-9-001/README.md), "Clauses 7 and 8 read again, 2026-11-11 (after the owner's rulings)",
reads both clauses at the change's code:
- **Clause 7: met**, as written for every entry but the Collator's hand resolution, and for that entry on the owner's
  revision of the list, which is weaker evidence.
- **Clause 8: met for `Decimal.prototype.toLocaleString`, and not met as written**, for a reader the exit reading did
  not see: the `m` literal's `Decimal` reads the host's current culture in its own parse and text, through calls that
  name no culture, which the exit reading's search for named calls cannot find. No decision keeps that reading, and the
  ruling covers `toLocaleString` alone. The bundle says what would meet the clause; choosing is the owner's. *(Noted
  2026-11-11, at review: a search of the tree for such calls, made at review, also finds a few sites outside the `m`
  literal that read the host's culture for a sign or a linguistic comparison, none for a decimal or group separator;
  the bundle's clause 8 row names them, and the clause's reading stands.)* *(Noted 2026-11-11, on the owner's ruling of
  that day on the `m` literal's host-culture parse and text, "Fix in this PR", the alternatives not taken being "Fix in
  a follow-up PR" and "Record as SJL-D-17 path": this item reads "**Clause 8: met for
  `Decimal.prototype.toLocaleString`, and not met as written**, for a reader the exit reading did not see: the `m`
  literal's `Decimal` reads the host's current culture in its own parse and text". Branch `js-c78` now passes the
  invariant culture at every parse and every text conversion of the `m` literal's `Decimal`, with tests under host
  cultures, and no other path the literal takes reads a culture. The bundle's subsection "Clause 8 read again after the
  `m` literal's fix, 2026-11-11" reads clause 8 met for `toLocaleString` and for the `m` literal, and still not met as
  written, for the sites outside the `m` literal that the search at review names, which read the host's culture for a
  sign or a linguistic comparison, and for the CLR converters' `DateTime.Parse`, which reads the host's date format: the
  ruling names the Decimal's parses and formats alone, and no decision keeps those sites. The bundle says what would
  meet the clause; choosing is the owner's. No answer moved but the culture-broken parse and text answers of the `m`
  literal's `Decimal` under a host culture other than the invariant one, the literals', a `Decimal`'s text and the
  `Decimal` function's parse of a String, with the `toLocaleString` answers that follow from them.)* *(Revised
  2026-11-11, at review of that note; it read "this item reads clause 8 "met for `Decimal.prototype.toLocaleString`, and
  not met as written, for a reader the exit reading did not see: the `m` literal's `Decimal` reads the host's current
  culture in its own parse and text"", which dropped the item's bold and its words "Clause 8: "; "for a sign or a
  linguistic comparison: the ruling names", before the search at review of date and time parses found the CLR
  converters' `DateTime.Parse`; and "No answer moved but the `m` literal's under a host culture other than the invariant
  one, with the `toLocaleString` answers that follow from them.", where two of the answers that moved are not the
  literal's own but a `Decimal`'s text and the `Decimal` function's parse of a String.)* *(Noted 2026-11-12, on the
  owner's ruling of 2026-11-11 on clause 8's remaining host-culture readers, "Fix all + enforce", the alternatives not
  taken being "Fix all, no enforcement" and "Scope clause 8 to Intl": the note above reads clause 8 "still not met as
  written, for the sites outside the `m` literal that the search at review names, which read the host's culture for a
  sign or a linguistic comparison, and for the CLR converters' `DateTime.Parse`, which reads the host's date format".
  Branch `js-c78` now names the invariant culture or an ordinal comparison at every site the .NET globalization
  analyzers find in the engine's product projects, and at the readers outside their reach that the change's searches
  find: numeric parses that name no format provider, a case-insensitive RegExp's .NET regular expression, and values
  made text by interpolation whose decimal point, date or minus sign a culture decided; it has tests under host
  cultures. The engine's build holds the rules CA1304, CA1305, CA1309, CA1310 and CA1311 as errors in its product
  projects, with the banned-API rule RS0030 over a read of the host's culture by name outside the paths that keep it,
  and those paths, the providers (decisions D6 and D7), SJL-D-17's .NET format strings and legacy `Date.parse`, and
  `Intl.Collator`'s comparison until SJL-17, name the culture they read, suppress that rule in place and say why they
  stay. The bundle's subsection "Clause 8 read again after every host-culture reader is fixed and the rules are
  enforced, 2026-11-12" reads clause 8 met as written, the rules the build holds being the evidence that keeps it for
  the kinds they see. No answer moved but answers a host culture other than the invariant one had made wrong, each to
  the invariant culture's.)* *(Revised 2026-11-12, at review of that note; from "in its product projects" it read "in
  its product projects, and the paths that keep the host's culture, the providers (decisions D6 and D7), SJL-D-17's
  .NET format strings and legacy `Date.parse`, and `Intl.Collator`'s comparison until SJL-17, name the culture they
  read and say why they stay", and it ended "the rules the build holds being the evidence that keeps it."; the five
  rules accept a read of the host's culture by name, so a new one outside those paths built until the banned-API rule,
  and the owner's ruling asks for each kept path "suppressed with its reason".)*
  *(Noted 2026-11-12, on the owner's answers of that day to the questions Broiler-Platform/Broiler.JS#1114 put, "Accept
  RS0030 form" (the alternative not taken being "Add architecture test too"), "Keep as is" (not taken: "Narrow to the
  literal grammar") and "Follow-up arch test" (not taken: "Leave as recorded"): the note above reads clause 8 "met as
  written, the rules the build holds being the evidence that keeps it for the kinds they see", and that reading stands.
  The kept paths keep their in-place RS0030 suppressions in the form the owner accepted; the `Decimal` function keeps
  reading a String with `NumberStyles.Number` and the invariant culture, until a revision of the Decimal proposal is
  pinned; and branch `js-arch` adds the engine's architecture test `HostCultureTests`, which holds the readers those
  rules do not see, a numeric or date parse that names no format provider, a .NET regular expression that folds case
  without `RegexOptions.CultureInvariant`, and a value made text by interpolation or composite formatting with no
  provider, culture or invariant format, in the product projects outside the kept paths, and asserts, by MSBuild's
  evaluation of every project, that every product project is in the rules' scope and every test, benchmark, sample and
  tool project outside it. The readers it found, interpolations of integers a script sees only when they are not
  negative and the compiled RegExp arm's options, now name the invariant culture, and no answer moved. A value made text
  by concatenation, `StringBuilder.Append` or a writer is not one of the ruling's kinds and stays open. Bundle
  [`sjl-9-001`](../evidence/sjl-9-001/README.md), "The readers no build rule sees and the rules' scope, held by an
  architecture test, 2026-11-12", records it. None of it is accepted: no second holder exists to review it.)*
- **Nothing else moved**: the engine vectors, test262's `supportedValuesOf` directory and part C's and part D's programs
  answer at the change as at its base, the currency and unit lists among them, and `toLocaleString`'s answers move as
  the changelog lists them.

This is no verdict on SJL-9: its gate waits as the paragraph above says. Broiler.VM's half records the ruling on clause
7 in JSD-0082's decision D24 and the milestone's SJL-9.5 deletion list. None of it is accepted: no second holder exists
to review it.

**The frontend's syntax half (SJL-13.1), filed 2026-10-26: all five sub-areas accepted, the library's shape and its
request decided, the stage table and the classification filed, and the owner's decisions of 2026-10-26 recorded.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0083-the-shared-syntax-library-and-the-wide-path.md`
  (JSD-0083), decided on 2026-10-26. It states each condition of the extraction gate for the five sub-areas, each on its
  own (decision D4): T, the tokenizer; P, the parser and its tree; N, positions; D, diagnostics and the parse-time
  early errors; and R, the request, its switches, limits and providers. They are one library, so a refusal of any
  would have been a refusal of SJL-13. It reads ADR 0011's standing refusal of "a shared syntax tree or parser" through
  ADR 0014's scope reading and decision SJL-D-8, since this is the first library that is literally one (decision D5).
  It is the frontend's one area verdict: it covers this engine's front end too, which SJL-14 moves onto the library.
- **Its input** is `Broiler.VM/docs/evidence/sjl-13-001/correspondence.md`, the correspondence from code. It read this
  repository at `d6795fc8` and Broiler.VM at `4cf9ca4`, whose product code is SJL-12's after commit `1de3af6`'s, with
  the early-error catalogue (SHA-256 `7a9109b0db685df98a7327cd95b9337cc538337f834fe4f22692b249058aa4f1`), the parse
  corpus and both engines' SJL-12 after baselines. Every row whose answer no retained run held was probed in both
  engines, this engine's through the shell and SJL-12's parse-corpus tool (entries P and K). This repository's default
  branch moved to `8a95076c` (#1079) while it was written; #1079 changes only the for-loop pass, for a labelled C-style
  `for (using …)` or `for (await using …)` loop, and no row's answer can differ there. Every behavioural difference is
  classified, none as unknown, and what the rows do not settle is handed on by name (its hand-offs H1 to H24). Every
  probe and answer is retained beside it.
- **SJL-13 started at the owner's words of 2026-10-25**, "Continue with the remaining SJL milestones", which the
  programme's ledger reads in its SJL-13.0 entry, in Broiler.VM's own change for SJL-13.1, as the choice to start
  SJL-13; SJL-14 and SJL-15 file their own start entries, reading the same words the same way. That is a reading of the owner's words, not a
  decision taken under the direction below (decision D1). SJL-13 may not meet its gate before SJL-12, SJL-3 and SJL-7
  meet theirs.
- **The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
  review." Each decision SJL-13's plan left open is taken as it recommends, D2 to D39. The correspondence's findings F1
  to F32 and the stage table's four items are answered by revisions of D7, D10 to D18, D20, D21, D23, D25, D26, D28 and
  D39, and of D41 and D44 to D47, which bind SJL-14 and which this plan's SJL-14 section records at SJL-14.1, each
  quoting the text it replaces, or recorded where they ask for no decision. JSD-0083 records them all as the owner's
  decisions of 2026-10-26. Decision D48, on SJL-14's clause 6, and each publication stay the owner's own choices.
  **It changes no rule**: nothing is accepted without review, and the gate waits as above. In this record a decision
  is "decision Dn", JSD-0083's, and a row of the correspondence "row Xn". This record carries decisions D6 to D17, D25,
  D26 and D28, as revised.

This repository's part of it:
- **The sub-area verdicts.** Conditions X1 to X7, as JSD-0083 states them for each of T, P, N, D and R:
  - **X1**: both engines implement every sub-area in merged code: this engine's `FastScanner`, `FastParser` and its
    scope check `FastScopeItem.AddVariable`, with `SyntaxValidation` and the compiler's other early-error checks for
    the rows the stage table says it checks only in its compiler; Broiler.VM's `SliceTokenizer`'s wide members,
    `JsParser` and its binder's checks;
  - **X2**: the correspondence compares them at the heads above, against ECMA-262's ES2026 edition at
    `tc39/ecma262@0248456c`, test262 `ccaac100` and the catalogue's digest, and classifies every difference;
  - **X3**: the contract is neutral: no engine type, the providers and limits parameters of the request, and an
    optional `WorkCharge` (decision D32);
  - **X4**: the library references `Broiler.ECMAScript.Algorithms` and `Broiler.Unicode.Algorithms` only, and
    neither engine (rule SL1), with no `Broiler.Regex` edge (decisions D6 and D7);
  - **X5**: no `System.Text.RegularExpressions`, no culture and no ambient state in the library, and Broiler.VM admits
    it for its compiler only, out of its compiler-free roots;
  - **X6**: each engine's deletion list. This engine's is SJL-14's: SJL-14.5's list as decision D41 reads it,
    completed member by member by a dated note on JSD-0083 at SJL-14.1, from that milestone's `dispositions.md`.
    Broiler.VM's is `JsParser`, its tokenizer's wide-only members, its binder's `CheckRegExp` and every binder check of
    a row the stage table assigns `syntax`, at its step 1, and `JsSyntax` with its adapter at its step 2 (decision
    D20);
  - **X7**: the owners accept. One person holds every role (Broiler.VM's EX-30).
- **The library** (decision D6). `Broiler.ECMAScript.Frontend`, project and package, in
  `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` with `Broiler.ECMAScript.Frontend.Tests/`; the namespaces
  `Broiler.ECMAScript.Syntax` (the request, the tokenizer, the tree, the parser and the diagnostics) and
  `Broiler.ECMAScript.Semantics` (the declarations walk now, SJL-15's model later); SL1's layering row
  `Frontend → [Algorithms, Broiler.Unicode.Algorithms]`; a `PublicAPI.txt`; assurance from its first commit; and
  `IsPackable` false until SJL-13.2's done-when. **`Broiler.ECMAScript.Algorithms` does not change at SJL-13**, not
  even by an added member (decision D9): a helper the library needs is internal to it. This engine references
  `Algorithms` by project, so any change to it is a product change here, and SJL-13 has no cut-over here to carry one.
  The home's design document,
  [`Broiler.ECMAScript/docs/frontend/design.md`](../../Broiler.ECMAScript/docs/frontend/design.md), revised at SJL-13.1,
  gives the names, the request and the result.
- **The request** (decisions D7, D8, D16, D17 and D32, as revised):
  - **providers**: Broiler.Unicode's `IUnicodeTables`, required, with no default, through which this engine passes
    its own tables; a regular-expression validator interface, with no package edge, through which each engine
    supplies its own validator, this engine's at SJL-14, answering valid, or invalid with the failing condition's
    message identifier where the validator can tell it and EE0019's `regexp.literal.invalid` where it cannot; and an
    optional `WorkCharge`, which both engines pass as null at SJL-13;
  - **goals**: Script, Module, eval code, and the dynamic function's parameters and body;
  - **switches**: forced strictness; Annex B, on for scripts; HTML-like comments by goal; and top-level `await`, which
    applies to whichever goal a request sets it for: this engine sets it for a module, and for a script whose top
    level may await (its `ScriptWithTopLevelAwait` preset, `JSContext.EvalWithTopLevelAwaitAsync` and the shell's
    retry); Broiler.VM sets it for a module only;
  - **the inherited context of eval code**: strictness and the function, method, derived-constructor and
    field-initialiser flags; an optional set of visible private names, whose absence leaves that check to the engine;
    and **no caller's lexical names at SJL-13 or SJL-14**: the conflict they serve is EvalDeclarationInstantiation's,
    a run-time `SyntaxError` each engine raises today (row R28), until SJL-15's model;
  - **proposal flags**, off by default and **one per form**, each its own catalogue row: decorators, auto-accessors,
    and the import phases' declaration forms `import source x from` and `import defer * as ns from` (EP0011, EP0013)
    and call forms `import.source(…)` and `import.defer(…)` (EP0012, EP0014). This engine's configuration turns on the
    forms it parses today, decorators, `accessor` and the two call forms, and no other (rows R09 and P06b);
  - **the lexer extension**, the only way this engine's `m` suffix reaches the library; its `Decimal` meaning stays
    here (SJL-D-17). Broiler.VM passes none.
- **What the syntax half reports** (decision D10, as revised: the union). The grammar, and every early error either
  engine's parser reports at parse time today, Broiler.VM's `JsParser` family and this engine's
  `FastScopeItem.AddVariable` and its parser's other checks, through a declarations walk in
  `Broiler.ECMAScript.Semantics`, with a regular-expression literal's validity asked of the provider. The walk exposes
  side tables ordered by source position (each scope's kind and its declarations, each block-level function
  declaration with whether it is an `if` clause's, each function's parameter names, and each program's and function's
  strictness), so that neither engine computes them again. The catalogue's engine-only rows are sorted by their kind:
  - Grammar rows and the contract's two limits, nesting and tree depth, are the library's;
  - a Policy row governed by a switch the request already has is applied by the library: EX0023, top-level `await`,
    where with the switch off a top level is read as a non-async function's body of the same goal, so the row has no
    diagnostic of its own;
  - every other Policy or Limit row stays with its engine, and the library never reports it, EX0021 among them, an
    import attribute the host cannot honour, which this engine's bridge refuses at SJL-14 as its parser does today.

  Everything else stays with each engine until SJL-15's model: this engine's compiler stages, and Broiler.VM's binder.
- **The stage table**,
  [`Broiler.ECMAScript/docs/frontend/stages.tsv`](../../Broiler.ECMAScript/docs/frontend/stages.tsv), records that
  assignment for every row of the catalogue: `syntax`, `request`, `engine` or `semantics`, with each engine's current
  stage and site re-read at the correspondence's heads. For each `syntax` row it says whether this engine checks it
  only in its compiler, and whether lazily, the input of SJL-14's classes E (early-error stage) and T (timing). The
  stages decided by hand: EE0017 is `syntax` by the grammar clause, though no parser checks it today; EE0179 is
  `syntax`, because EE0168 refuses every source it refuses; EE0018 and EE0054, which no parser checks, are
  `semantics`. No `semantics` row has this engine's parser as its site, and the two rows of another stage that do,
  EX0021 (`engine`) and EX0023 (`request`), were read and confirmed.
- **The catalogue's cells are not edited** (decision D28, as revised). The stage table re-reads the sites, and its
  `stale_cells` column names the catalogue cells that no longer hold beyond their line numbers. Among them is the
  `status` of EE0082, EE0137, EE0166 and EE0176, whose "both" has been stale for this engine since #1075, which lets
  `continue` name any label of a loop's label set, and is wrong for the specification. JSD-0083 records them as stale.
- **Diagnostics** (decision D11, as revised): shared codes, `JSE` and four digits, from the catalogue; message
  identifiers with arguments, defined at SJL-13.2's stage 4d from `early-errors.md`'s section 6; phases; source order;
  the cap a parameter. First-error or all-errors is a parameter, and **both engines ask for the first error at SJL-13
  and SJL-14**: the library stops at its first refusal, in its order, which is a refusal as the parse reaches its
  construct, and a scope's declared-name conflicts when the scope closes, in the source order of the conflicting
  declarations. So this engine's first refusal moves on a source with a grammar error later in a scope than a
  redeclaration in it, which SJL-14's corrections name. This engine renders its own V8-style text, and keeps the texts
  hosts and tests read, "Unexpected await" among them, which the shell's retry reads.
- **Positions** (decision D15, as revised): offsets are 0-based in UTF-16 code units, and lines and columns 1-based,
  columns in UTF-16 code units; every line terminator of the specification starts a new line, and CR LF is one; the
  request takes text, and a leading byte-order mark is the host's to drop; every node has a full span. Where the
  library takes a refusal from Broiler.VM's front end, its diagnostic's span starts where that front end anchored it.
  This engine's message positions move under SJL-14's class M.
- **The limits** (decision D14, as revised). The tree-depth constant is derived at SJL-13.2's stage 4e as Broiler.VM's
  JSD-0022 derived Broiler.VM's, with the stack it requires stated beside it, and must admit Broiler.VM's bound on the
  stack Broiler.VM's `CompilationStack` declares; if it cannot, the library is reworked, with explicit stacks, and the
  bound is not lowered. The nesting depth is a parameter with each engine's default; this engine passes the library's
  supported maximum. **The left-operand gap is closed in the library** in all four shapes Broiler.VM's base compiles
  past its bound: a binary chain whose first operand is a parenthesised chain, a template whose substitution holds
  one, a binary chain whose first operand is a member or call chain, and a member or call chain whose object is a
  parenthesised binary chain. Each such refusal is an implementation limit, never a correction. **The tree depth is
  charged as Broiler.VM's `JsParser.Deepen` charges it**: one level for each node a binary chain or a member or call
  chain adds, and none for a parenthesis, a template or a substitution; and a left operand's spine adds its own length
  to the chain it begins, so a chain of `a` nodes as the left operand of a chain of `b` nodes is charged `a + b`. The
  home's rule, part G, reads that charge at the bound (its `docs/evidence/sjl-13-001/inputs/README.md`). This engine's
  acceptances past the bound become listed refusals at SJL-14 (JSD-0080, decision D27).
- **The vectors** (decision D25, as revised): a `syntax` area in schema 1, at a corpus revision before the library's
  first code, with both engines' first runs classified, and no schema revision. `op` names the parse (`ParseScript`,
  or `PerformEval`); `in` holds the source for the library-level runner, which gains a frontend adapter at stage 4a;
  `script` holds the same source, or a top-level direct `eval` of it, for the engine-level runners; `out` is `error:
  SyntaxError` or the completion value. A row that only a module goal, a switch or a parameter reaches has no vector,
  and the parse corpus judges it. The lexical and `syntax` vectors carry every correction of Broiler.VM that its step 1
  makes and a script or a direct eval can carry, each with its row in its provenance. Engine-policy rows are
  `impl-defined`, with per-engine fields; Broiler.VM's manifest refusals of normative constructs and this engine's `m`
  extension are `divergence` fields; each cites that engine's records. A revision after each engine's cut-over retires
  the divergence fields that no longer hold. Rule SL9 is not revised: it already runs the whole vector corpus through
  the library-level runner.
- **The classification** (decision D26, as revised):
  [`docs/evidence/sjl-13-001/classification/`](../evidence/sjl-13-001/classification/README.md), written at SJL-13.1
  by [`scripts/frontend/classify_parse_corpus.py`](../../scripts/frontend/classify_parse_corpus.py) from test262's
  metadata, the stage table, both engines' SJL-12 after baselines and the correspondence's corpus join, in two
  configurations, V (Broiler.VM's request) and J (this engine's). It gives every input, variant and configuration, and
  every eval request, its expected outcome at the syntax half, the rows whose shared codes may come first, and the
  reason. **The home's corpus runner answers a regular-expression literal as each configuration's engine validator
  answered the input in its SJL-12 after baseline**, as the classification records it, since the home holds neither
  validator, and reads a `json` variant under the Script goal.
- **Broiler.VM's half asks nothing of this repository**: its registry revision 19 at its step 1's registry commit
  (decisions D12 and D13), whose meanings are Broiler.VM's while the catalogue's shared codes keep the catalogue's
  conditions apart; its admission walk (decision D18); its adapter and its two steps (decisions D15 and D19); its
  deletions (decision D20); the two tokenizers' agreement (decision D21); and its tools commit, fuzz session and
  measurement (decisions D22 to D24 and D27). The stream version that carries the library is published with the
  owner's approval before Broiler.VM's step 1 merges (decision D30).
- **What does not change here at SJL-13.** This engine's front end, which SJL-14 moves onto the library (the
  milestone's "Does not do"). Every correction of this engine the correspondence classes is SJL-14's, listed in that
  milestone's `corrections.md` (hand-off H22); SJL-14's own section, at SJL-14.1, records decisions D40 to D56, with
  the revisions of D41 and D44 to D47 that JSD-0083 records.
- **The rule**, [`docs/evidence/sjl-13-001/decision-rule.md`](../evidence/sjl-13-001/decision-rule.md), is committed
  before any code of the library, with the classification, part F's fuzz seeds and part G's deep-nesting corpus. Its
  parts, each judged at the library commit:
  - A, the parse corpus and test262's syntax tests at library level, against the classification, in V and J;
  - B, the `syntax` vectors at library level (template T3);
  - C, determinism, by rule SL9 and three runs of the corpus in two orders and on two threads;
  - D, the Native AOT probe;
  - E, rules SL1 to SL10 with the frontend's layering row, the API baseline, package validation and the assurance check;
  - F, fuzzing at library level;
  - G, the contract's limits, over the deep-nesting corpus, with the tree-depth constant's derivation;
  - H, the contract's checklist: every item of the contract has a public member, but the caller's lexical names,
    which it lists as absent with decision D16's reason; no public type names an engine type; no `engine` row has a
    producer.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 4 to 10 are judged in its bundles
`sjl-13-001` and `sjl-13-002`, and SJL-13 may not meet its gate before SJL-12, SJL-3 and SJL-7 meet theirs.

**Collation (SJL-17.1), filed 2026-11-01: the area accepted as an adoption, and the owner's decisions of 2026-11-01
recorded.**
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0084-collation-moves-to-the-shared-intl-library.md`
  (JSD-0084), decided on 2026-11-01. It states each condition of the extraction gate for the area: the Unicode
  Collation Algorithm over CLDR's root collation and each embedding's tailorings, with `sensitivity`,
  `ignorePunctuation`, `numeric` and `caseFirst`, moving to `Broiler.ECMAScript.Intl` as `UcaCollator`, over the
  collation tables the data home's generator already writes (JSD-0082's decision D11). It amends Broiler.VM's JSD-0043
  §5 to §7 where they move.
- **Its input** is `Broiler.VM/docs/evidence/sjl-17-001/correspondence.md`, the correspondence from code, rows O. It
  read this repository at `61cb15b5`, Broiler.VM at `83290f9` and test262 at `ccaac100`, and every behavioural cell was
  run in both engines: this engine's through the shell in UTC with no `LANG`, every probe again under
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`, and the rows that read the default locale under a `de_DE` and a `tr_TR`
  culture; test262's `intl402/Collator`, both `localeCompare` directories and `Locale/prototype/getCollations` through
  this repository's runner, normal and invariant; and the reference Node.js release as a witness. Every row carries its
  class, and every probe and answer is retained beside it.
- **SJL-17 started before SJL-9, which it waits on, met its gate**, at the owner's words of 2026-10-19, "Also continue
  with the other remaining SJL milestones", which the programme's ledger records in its SJL-17.0 entry, in Broiler.VM's
  own change for SJL-17.1, once SJL-9.3a's provider and locale core merged here (`b22e7d6a`). SJL-17 may not meet its
  gate before SJL-9 meets SJL-9's.
- **The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
  review." Each decision SJL-17's plan left open is taken as it recommends, E1 to E14, and JSD-0084 records them as the
  owner's decisions of 2026-11-01; none was decided again, and where the correspondence found a drifted citation or a
  reading the plan leaves open, the decision says so beside it. **It changes no rule**: nothing is accepted without
  review, and the gate waits as above. In this record a decision is "decision En" and a row "row On".

This repository's part of it:
- **X1: an adoption** (decision E2). This engine implements no collation algorithm: `JSIntlCollator.Compare` hands both
  strings, after Broiler.Unicode's NFD, to the host's `CompareInfo`, with `sensitivity` and `ignorePunctuation` mapped
  onto `CompareOptions`, and `numeric` and `caseFirst` resolved but not applied. The shared collator is Broiler.VM's
  `JsCollator` and `JsCollationData`, and this engine adopts it.
- **What the shared collator covers here** (decision E3), per locale and collation: root, and CLDR 48.2.0's tailorings
  for `de` (phonebook, search), root `search`, `en`, `es`, `sv` and `th`. **CJK stays on the host provider** until a
  data decision sizes its tailorings, and so does every other locale, and a collation the shared data does not carry
  for a covered locale. Two passing test262 files read such collations: `Collator/prototype/resolvedOptions/resolved-collation-unicode-extensions-and-options.js`
  asks `de` for `eor`, and `Locale/prototype/getCollations/output-array-values.js` asks `en` for at least one
  collation (JSD-0082's decision D33 (c)); the data home's rule engine refuses the syntax CLDR's `eor` and `emoji`
  rules use, so SJL-17.3 either carries them or leaves them to the host provider, and the rule's part A names the
  second file (finding F2). Thai's default `ignorePunctuation` comes from the tailoring data (decision E5).
- **The corrections**, the correspondence's column "Correction of Broiler.JS", each to be listed in
  `Broiler.JS/CHANGELOG.md` at SJL-17.5 under decision SJL-D-16 (a):
  - `ignorePunctuation` ignores spaces and punctuation, as CLDR's root's variable range does, and not symbols (O5a);
  - `sensitivity: "base"` folds width and compatibility differences (O5b);
  - `numeric` and `caseFirst` applied (O7, O8), which moves the `orderings` lines that ask for them (O18);
  - the constructor reads its options in the specification's order, each once, and refuses a `collation` that is not a
    type sequence (O10);
  - the bound `compare` is made once and kept (O12, decision E8);
  - under invariant globalization no comparison falls back to code-unit order: every request resolves as the
    available-locale policy says (JSD-0082's decision D7) and collates by the root or `en` (O14, decision E4).
- **The data changes** (C5), each listed with its decision: CLDR 48.2.0's root and the tailorings of decision E3's list
  replace the host's ICU for the covered locales (O1, O2, O3, O16), and Thai's default from the data (O5c, E5).
- **What stays as it is.** The rows that are the same in both engines (O4, O6, O9, O13, and O19, whose answer this
  engine reaches today by the tie-break on the deletion list and the shared collator gives without it); the
  default-locale provider (O15); this engine's `Intl.Collator` objects, its errors and `localeCompare`'s coercion; and
  its meter, which is none (decision E7, row O17).
- **The deletion list** (decision E11): `ResolveCompareInfo` for the covered locales, `StripIgnorablePunctuation`'s
  tie-break (row O19), the `CompareOptions` mapping and the hard-coded Thai default; `NetCollationSuffix` moves into the
  host provider. `JSIntlCollator` keeps every public member (SJL-D-25 (a)), which SJL-17.5 records in
  [`docs/public-api.md`](../public-api.md).
  *(Noted 2026-11-11, on the owner's ruling of that day on SJL-9's clause 7, "Fix (1); move (2) to SJL-17", the owner's
  own dated choice, asked and answered. The list above begins "`ResolveCompareInfo` for the covered locales" and names
  nothing of the Collator's locale resolution. From the ruling it also holds the Collator's hand resolution of its
  `co`, `kf` and `kn` keys, which SJL-9's deletion list named and SJL-9.5 kept, the Collator reading it:
  `GetUnicodeExtensionType`, `RemoveUnicodeExtensionKeyword` and `ReflectExtensionKeyword`,
  `ResolveLocaleFromCanonical`'s keyword filter over `CollatorRelevantKeys`, and `JSIntlCollator`'s
  `StripUnicodeExtension`. SJL-17.5 deletes them, the Collator resolving those keys through the shared ResolveLocale,
  and SJL-17's deletion clause reads them; SJL-9's list no longer does (the dated note on it under "Locale data and the
  Intl algorithms (SJL-9.1)" above). The alternatives not taken were "Fix both in code now" and "Revise the list for
  both". Decision E11 is Broiler.VM's JSD-0084, which this note does not revise. The list above is left as written.)*
- **Data size** (decision E13): this engine's collation tables are generated by the data home for decision E3's list,
  and their size is stated in this repository's bundle `sjl-17-001` when SJL-17.3 generates them.
- **The rule**, [`docs/evidence/sjl-17-001/decision-rule.md`](../evidence/sjl-17-001/decision-rule.md), is committed
  before any code of the area, with its divergence groups and its invariant probe (decision E10):
  - A, test262 per path in UTC with negative-metadata tests, its classes C1 and C5 naming the rows above, and
    `getCollations/output-array-values.js` named;
  - B, Broiler.VM's `orderings` dataset in this engine against Broiler.VM's retained answer, which has no divergences
    file, so its classes V and R hold no line (finding F3);
  - C, `invariant-probe.js` under invariant globalization, each line's expected answer the reference's for the locale
    this engine resolves (risk K5, finding F6).

  **The bases** are taken at the base commit, the first parent of the merge that lands SJL-17.5, and the after runs at
  that merge: part A by `test262 evidence`, and parts B and C locally. Both cut-overs are one merge each, after SJL-9's
  after commits, and the library merges outside SJL-9's windows (decision E14). The stream version that carries the
  collator is published with the owner's approval, a `0.1.0-preview.N` by the owner's direction of 2026-11-01.

  *(Noted 2026-11-15, at SJL-17.5.)* **SJL-17.5, this engine's cut-over, is done on its branch `sjl-17-5-cutover`**, cut
  from the default branch at `5669f2bd29899ac93e0d7af130b6a7adc5791d6b` (Broiler-Platform/Broiler.JS#1117), to land as one
  merge; that it merged is observed when it does, and no run of the rule is taken here. What the branch does:
  - **Deleted**, from the deletion list above and the dated note on it: `ResolveCompareInfo` for the covered locales,
    `StripIgnorablePunctuation` and its tie-break, the `CompareOptions` mapping for the covered locales and the
    hard-coded Thai default; and `GetUnicodeExtensionType`, `RemoveUnicodeExtensionKeyword`, `ReflectExtensionKeyword`,
    `ResolveLocaleFromCanonical`'s keyword filter over `CollatorRelevantKeys` and `JSIntlCollator`'s
    `StripUnicodeExtension`, the Collator resolving `co`, `kf` and `kn` through the shared ResolveLocale.
  - **Moved**: `NetCollationSuffix` into the optional host provider, `HostCollationProvider`, with the host's comparison
    for the locales and collations the provider serves. "The `CompareOptions` mapping" is read, as decision E11's
    "`ResolveCompareInfo` for the covered locales" is, for the covered locales: the host provider keeps the mapping for
    the locales and collations it compares, since the host's `CompareInfo` reads the options no other way. The
    alternative not taken, a host comparison with no options, would drop `sensitivity` and `ignorePunctuation` for CJK
    and every other uncovered locale.
  - **Kept**: the host provider for CJK, every locale decision E3 does not list, and a collation the shared data does not
    carry for a listed locale (`eor` and `emoji`, so `de` keeps `eor` and `en` keeps a collation, and
    `getCollations/output-array-values.js` keeps its base verdict, as the rule's part A asks). Where the host has no
    collation data, under invariant globalization, the provider answers nothing and the shared collator compares, by the
    root or the listed tailoring, so no comparison falls back to code-unit order (decision E4). The Collator's data lists
    `eor` and `emoji` on every host, as before; the alternative not taken, listing them only where the host has data,
    would leave `en` without a collation under invariant globalization.
  - **The shared collator is unchanged**: no library under `Broiler.ECMAScript/` changes, so no stream publication is
    asked. One test there does: `Broiler.ECMAScript.Conformance.Tests`' library-level check of the collation vectors, whose
    Han vectors match once both engines' expectations agree (corpus revision 40).
  - **Records**: the changelog's entry of this date lists each correction (O5a, O5b, O7 and O8 with O18, O10, O12 and
    O14) and each data change (O1, O2, O3 and O16, and O5c) with its vectors; [`docs/public-api.md`](../public-api.md)
    records that `JSIntlCollator` keeps every public member; corpus revision 40 restates this engine's expectations that
    the cut-over moves under rows O3 (decision E-o3h) and O16, and leaves the `divergence.broiler-js` fields the cut-over
    answers to the revision that follows the merge, as revision 38 followed SJL-9.5's.
  - **The base conditions, read by git at the commit the branch is cut from** (`5669f2bd`), which is the base only if the
    branch merges with it as its first parent: it descends from the rule commit (the rule file's last change,
    `096793ae0f3aaef7f18829c4b5efd923f3dbef94`, merged in `d1e8f5beec824438f0fc42c37bd941ad7421ce52`,
    Broiler-Platform/Broiler.JS#1087, and unchanged since); it descends from bundle `sjl-9-001`'s after commit,
    `a5f2692e88a9aafeaa850bebe9c3399f6263353b`; and the third condition, that the after commit is its only first-parent
    successor changing a product path, is read when the merge lands. Bundle `sjl-17-001`'s README records the same reads.

  Nothing above is met or accepted, and no clause is read.

  *(Noted 2026-11-16, at the review of SJL-17.5, on the same branch.)* The review's findings change the branch and these
  records as follows; each choice is taken under the owner's direction of 2026-10-18 and recorded with its alternative.
  - **A region or variant of a listed language with a collation of its own stays the host provider's.** Decision E3's
    reading, per locale and collation, puts a collation the shared data does not carry on the host provider. CLDR 48.2.0
    gives two sort collations in files of their own to a region or variant of a listed language, `de_AT.xml`'s phonebook
    and `en_US_POSIX.xml`'s standard, and the data home pins neither, so `de-AT` with `phonebk` and `en-US-posix` are now
    the host provider's for sort, as they were before the cut-over; under invariant globalization the shared collator
    compares them, as it does every locale. The alternative not taken, the branch's first reading, compared them by the
    language's tailoring, which moved their answers with no row naming it.
  - **The host provider compares as before.** It maps `sensitivity` and `ignorePunctuation` onto the host's options and
    does not apply `numeric` or `caseFirst`, so the corrections of rows O5a, O5b, O7 and O8 hold for the locales and
    collations the shared collator compares, and the changelog and `docs/public-api.md` now say so; `emoji` and `eor`
    for a listed language therefore answer by the host where it has collation data and by the shared collator where it
    has none. The alternatives not taken: applying `numeric` on the host's path with .NET's numeric ordering option, which
    would move answers for locales no row lists; and comparing `emoji` and `eor` of a listed language by the shared
    collator's default, which decision E3's reading puts on the host provider.
  - **Thai under `usage: "search"`** now reports `ignorePunctuation` `false`, from the root's search collation, where the
    language check gave `true`. Row O5c says no answer moves and does not name this one; it is put to the owner, and the
    shared collator's tests hold it under row O5c. The alternative is to keep Thai's shifted default under search.
  - **Row O11a has no test.** The correspondence classes it implementation-defined, and the stage's brief admits new
    tests only under the rule's classes C1 and C5, so the case the branch added is dropped; the changelog still lists the
    change. The alternative not taken: keep the case as a choice under the owner's direction.
  - **The rule commit** has two candidates in bundle `sjl-17-001`'s records: `59380ef9`, which its part C section implies
    by naming `61cb15b5` "the rule commit's parent", and `096793ae`, the rule file's last change, merged in `d1e8f5be`.
    The base conditions read the same under each, and the choice is left to SJL-17.6's records or the owner. The base
    conditions' item above says "it descends from the rule commit (the rule file's last change,
    `096793ae0f3aaef7f18829c4b5efd923f3dbef94`, merged in `d1e8f5beec824438f0fc42c37bd941ad7421ce52`,
    Broiler-Platform/Broiler.JS#1087, and unchanged since)", and stands as a reading under the second candidate.
  - **For the owner:** the branch revises one test that SJL-17.3 committed and the home clauses 1 and 2 are judged by,
    `Broiler.ECMAScript.Conformance.Tests`' library-level check of the collation vectors, whose Han vectors now match
    because corpus revision 40 moves this engine's expectation to Broiler.VM's; the owner's acknowledgement is asked
    before SJL-17.6 reads the home clauses.
  - The item "The shared collator is unchanged" above was revised on 2026-11-16, before this note; it read "nothing under
    `Broiler.ECMAScript/` changes, so no stream publication is asked."

  Nothing in this note is met or accepted, and no clause is read.

  *(Noted 2026-11-16, at SJL-17.6, on the branch `sjl-17-6-evidence`.)* **SJL-17.5 merged, and SJL-17.6 runs and
  judges bundle [`sjl-17-001`](../evidence/sjl-17-001/README.md)'s rule.**
  - **The merge.** The branch `sjl-17-5-cutover` merged as `2f012739cfc4d99e91b22d28a09941c49d8bd646`
    (Broiler-Platform/Broiler.JS#1118, observed), in one merge, the rule's after commit. Its first parent,
    `5669f2bd29899ac93e0d7af130b6a7adc5791d6b`, is the base, and the base conditions hold on the bundle's git reads.
  - **The readings.** The bundle's section "SJL-17.6" files its readings before any run's output was read, each the
    recommended choice under the owner's direction of 2026-10-18, with its alternative:
    - the rule commit, `096793ae`, chosen of the two candidates the note above left to these records (it read "the
      choice is left to SJL-17.6's records or the owner");
    - this machine for parts B and C;
    - the hosted runs' SDK as the workflow resolves it;
    - base condition 3 read up to the after commit;
    - part A's classes by JSD-0084's columns;
    - the reference run;
    - "completes";
    - the commit the home clauses are read at.
  - **Parts A, B and C are each MET**, as the rule is written, on those readings. None rests on a revision of the rule
    or on a ruling of the owner's own:
    - part A by two hosted runs of `test262 evidence`, dispatched by this stage one at a time, in which no verdict
      changed and `getCollations/output-array-values.js` kept its base verdict;
    - part B by the `orderings` dataset, every line equal to Broiler.VM's retained answer after;
    - part C by the invariant probe, every line equal to its expected answer after, with no `abort:` line.
  - **Clauses.** The home clauses 1 and 2 are read met at SJL-17.3's merge, `c884f87b`, by the tests SJL-17.3
    committed, and at the after commit. This engine's clauses 3 to 5 are read met at the after commit, clause 3 on the
    SJL-17.5 records' reading of "the `CompareOptions` mapping" for the covered locales.
  - **Corpus revision 41** retires the collation area's `divergence.broiler-js` fields. Revision 40 left them, in the
    note of 2026-11-15 above, "to the revision that follows the merge, as revision 38 followed SJL-9.5's". CI's
    `windows-latest` job at the merge has read them, and each no longer holds there or on `ubuntu-24.04`.
  - **For the owner:**
    - the acknowledgement the note above asks about `LibraryRunTests` (it read "the owner's acknowledgement is asked
      before SJL-17.6 reads the home clauses") stays open, since this stage read the home clauses at both commits;
    - the bundle's readings, three of which a verdict could turn on: the machine; the hosted runs' SDK, which is the
      runner image's newer one since the repository pins none; and base condition 3 read up to the after commit, since
      the default branch moved past it to `cdf5559d`, which changes named paths *(revised 2026-11-16, later; it read
      "two of which a verdict could turn on (the machine, and the hosted runs' SDK, which is the runner image's newer one
      since the repository pins none)")*;
    - the items the review of SJL-17.5 left open.
  - **No gate is met.** SJL-17's gate waits on SJL-9's, on SJL-3's and SJL-4's measurement clauses, on Broiler.VM's
    clauses 6 to 9 and SJL-17's exit reading over both repositories, and on review (EX-30).

  This note declares no clause met at the gate, and nothing is accepted.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 6 to 9 are judged in its bundle of
the same name, and SJL-17 may not meet its gate before SJL-9 meets SJL-9's.

**Static semantics (SJL-15.1), filed 2026-11-17: the area moves, this repository's half, with the decisions that bind
it.** *(Added 2026-11-17, at SJL-15.1, Broiler.JS's half.)*
- **The records.** Broiler.VM's record is
  `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0088-static-semantics-move-to-the-shared-frontend.md`
  (JSD-0088), decided on 2026-11-16 as Broiler.VM's half and merged in Broiler-Platform/Broiler.VM#214 (`0ddc146f`), with
  the owner's answer to its decision D-census merged in Broiler-Platform/Broiler.VM#215 (`626e4ed8`) (observed). It states
  each condition of the extraction gate, X1 to X7, for the facts SJL-15's milestone lists: scopes and their kinds,
  declarations and conflicts, Annex B.3.3 aliases, per-function flags, reference resolution with dynamic markers, labels
  and jump targets, private-name environments, module records, named-evaluation names, free names and captures, and
  every early error outside the parse. Its input for Broiler.VM is its bundle `sjl-15-001`'s `correspondence.md`, read
  at Broiler.VM `a86b6f5d` and, for the model surface, at this repository's `6fda6ca4`.
- **The verdict** (JSD-0088's decision D-verdict): **the area moves to the semantics half of
  `Broiler.ECMAScript.Frontend`**, the namespace `Broiler.ECMAScript.Semantics` that "The frontend's syntax half
  (SJL-13.1)" above names for "the declarations walk now, SJL-15's model later", **grown from `DeclarationsWalk`**:
  SJL-15.2 adds the rest of the model to that walk and starts no second one. JSD-0088 is Broiler.VM's half of the
  verdict, and this entry is this repository's.
- **Free names and captures** (the correspondence's row FC1) are **an extraction from this engine's `FreeNameScan`**
  (`Broiler.JS/Broiler.JavaScript.Compiler/Declarations/FreeNameScan.cs`), the only producer of the fact in either
  engine, **and an adoption for Broiler.VM**, which computes none: every binding there lives in an environment record
  (JSD-0088's X1). Every other fact is an extraction from a producer in each engine.
- **This repository's half of X2 and X6** (JSD-0088's decision D-x2) is written in bundle
  [`sjl-15-001`](../evidence/sjl-15-001/README.md) before any code of SJL-15: this engine's column of the
  correspondence, read from code at `6fda6ca4`, with each row's class across both engines and the probes of both
  engines that settle the rows a reading cannot; and this engine's deletion list, member by member, with the disposition
  of every public type SJL-15.5 empties (SJL-D-25). It reaches Broiler.VM's bundle `sjl-15-001` and JSD-0088 by a dated
  note in a Broiler.VM change of its own, which this change does not make; until that change merges, JSD-0088 records no
  row's class across both engines, and no SJL-15.2 code merges (decision D-x2).
- **This repository's half of the other conditions**, read at `6fda6ca4`:
  - **X1**: this engine implements every fact in merged code: the parser's scope stack, `FastScope` and
    `FastScopeItem` (declarations, their conflicts and the Annex B.3.3 names); the compiler's `FastFunctionScope` (each
    function's facts and the resolution its variables carry), `FreeNameScan`, `SyntaxValidation`,
    `FastCompiler.EarlyErrors`, the early-error checks in `Visit*` methods and `DirectEvalSupport`; the bundle's column
    reads each;
  - **X3**: the model is JSD-0080's design, which the home's `design.md` restates and the declarations walk already
    meets; the home's clauses 1 to 3 judge the implementation, by this repository's bundle `sjl-15-001`;
  - **X4**: the library references neither engine (rule SL1), and **decision D65 gives it no package edge to
    Broiler.Regex** while SJL-11.4 and SJL-11.5 have not landed;
  - **X5**: the model is in `Broiler.ECMAScript.Frontend`, under the frontend's layering row of rule SL1, unchanged;
  - **X6**: this engine's deletion list is SJL-15.5's, as the milestone names it and as revised at SJL-14.1 (decision
    D41): the compiler's `FastFunctionScope`, `FreeNameScan`, the scope half of `FastScope`, `SyntaxValidation`,
    `FastCompiler.EarlyErrors`, and the early-error checks in `Visit*` methods and `DirectEvalSupport.Validate`, with
    the compiler reading no bridged tree; completed member by member in the bundle;
  - **X7**: the owners' decision D-verdict, under the owner's direction; one person holds every role (Broiler.VM's
    EX-30).
- **What this does not decide**: the model's design in detail (SJL-15.2's, under JSD-0080's contract); Broiler.VM's
  half, its windows and its rule; decision D-census, which the owner answered for Broiler.VM's clause 5; and each
  publication of the shared stream, which is the owner's (JSD-0083's decision D30). The records of this repository's
  half are in "Shared static semantics (SJL-15)" below.

None of it is accepted: no second holder exists to review it. SJL-15 may not meet its gate before SJL-13 and SJL-14 meet
theirs.

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
    *(Noted 2026-10-26, at SJL-13.1: "SJL-13 defines the shared provider, and the tables move once more then." no
    longer holds. The library's provider is Broiler.Unicode's `IUnicodeTables`, which this engine's options already
    take, and SJL-13 changes nothing of this engine (Broiler.VM's JSD-0083, decisions D8 and D9). The tables' next
    move is SJL-14's, whose decision D50 gives their home.)*
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
    control flow. They stay until SJL-14, whose shared tree keeps raw facts. *(Noted 2026-10-25, at SJL-11.1, by
    Broiler.VM's JSD-0081, decision D22. The sentence reads "They stay until SJL-14", and the second of them is the
    rewrite of a regular-expression literal's `\u{…}` escapes. SJL-11.5 deletes that one, with the scanner's
    parenthesis-balance patch, or SJL-14 does if SJL-11 has not; it stays at SJL-12, as decided. Section "Area
    verdicts", "Regular expressions (SJL-11.1)", records the deletion.)*
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
    change to those files after the after runs, since a change to them would move the after commit. *(Noted 2026-11-03, at SJL-14.1, decision D55: after #1076 and the later changes to the file, the second remark reads at `ForLoopPreparation.cs:47-50` at `33d3093b`; `DirectEvalSupport.cs:92-93` is unchanged. Both remarks are corrected in code at W1, SJL-14's first product change to those files.)*
  - **Decision D29's "forwarders by default"** is not applied to one member: `DirectEvalSupport.Execute`, which
    compiled code calls, gained a parameter in place. Its old signature is removed, which breaks binary
    compatibility, with no forwarder and no obsolete overload. Its disposition was taken 2026-10-25, as
    recommended, under the owner's direction of 2026-10-18 ("Please do all recommended decisions and skip waiting
    for review."), which changes no rule: SJL-D-25 option (c), removal, with the consumer check recorded in
    [`docs/public-api.md`](../public-api.md). Broiler.JSeal (`ebb5e6ac`) and Broiler.HtmlBridge
    (`3baa48c5`) refer to no `DirectEvalSupport`, and the old signature's only caller is code the compiler emitted.
    The removal fits the programme's risk D6 in Broiler.VM's `docs/javascript-shared-libraries/risks.md`, whose
    stop is "a public member removed without its recorded disposition", since its disposition is recorded. A
    folder of `AssemblyCodeCache`, the console host's persistent code cache, written before the change must be
    cleared after updating, since the cache's key does not change from one build to the next; keying it on the
    build is a suggested follow-up. The decision changes no product file, so the after commit stays `c29d653f`.
    *(Noted 2026-11-03, at SJL-14.1, decision D55: the follow-up is done. Broiler-Platform/Broiler.JS#1071,
    merged as `8dbef17f`, keys each entry of the cache on the build of the engine's assemblies (their module version
    identifiers), so a folder written by an earlier build is no longer served; "keying it on the build is a suggested
    follow-up" no longer holds.)*
  - **SJL-D-25 (b)** is an obsolete forwarder with a removal version; `UndoMark`'s obsolete mark names no version and
    leaves its removal to (c)'s consumer check, which these records do not make for it.
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
- **The after runs are taken and read**, in the block below. The follow-up the disposition of
  `DirectEvalSupport.Execute`'s old signature suggests, above, is a product change, which the after runs no longer
  hold back, and so is the correction of the product's remarks on the eval depth, above *(revised 2026-10-26, at
  SJL-12's after evidence; the item read "**Next: the after runs**, at the after commit: part A by
  `test262 evidence`, and parts B, C and D on the local machine; then the bundle's README reads each part against
  the rule. Nothing here is a verdict. The follow-up the disposition of `DirectEvalSupport.Execute`'s old signature
  suggests, above, is a product change, and so comes after them.")*.

**The after evidence: each part of bundle `sjl-12-001`'s rule is MET at the after commit `c29d653f`.** *(Added
2026-10-26, at SJL-12's after evidence.)* The bundle's README reads each part against the rule as written, with the
files each part reads ([`sjl-12-001`](../evidence/sjl-12-001/README.md), sections 3 to 8):
- **Part A, test262 per path in UTC with negative tests** (section 4): `test262 evidence` run 40231802611, taken at
  the after commit on 2026-10-25, against the base's run 40196127730 at the rule commit. Both runs are complete and
  comparable, their host-zone records are one file byte for byte, with tzdata 2026c, and no row of the suite changed
  its verdict. So class C1 lists no row, and corrections J1 and J2 move no verdict at this revision of the suite.
- **Part B, the architecture test and its witness** (section 5): the test passes over the after commit's parser. Over
  the rule commit's parser it fails, naming every violation the rule lists. It refuses each shape of its witness,
  and control K, a static counter in `FastParser`, fails it and passes after its checked revert.
- **Part C, the parse corpus's acceptance** (section 6): no input, variant and entry point differs from the base, so
  classes M, X and D list no row.
- **Part D, determinism** (section 7): the pass's entry point, `ForLoopPreparation.Prepare`, is found in each run,
  and no input of the population differs between the two orders and the two threads. Controls K1 and K2 are
  detected. Each revert is checked against the after commit, and the copy, built again after both reverts, agrees
  again *(revised 2026-10-26, after review; the item ended "Controls K1 and K2 are detected, and each agrees again
  after its checked revert.", but no run was taken between the two reverts)*.
- **This repository's clauses** (section 8): clauses 1 and 2 are met on these records, and clause 3 on the record
  above; this repository's half of shared clauses 7 and 8, the early-error catalogue and the parse corpus with its
  baselines, is retained.
- **Where the records disagree** (section 9): the corpus commit's record read its third condition with a pathspec
  that matches no file under `Broiler.JS/`; read with one that does, the condition holds at the corpus commit and at
  the after commit.

None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 4 to 6, and its half of shared
clauses 7 and 8, are read by the programme's ledger. **SJL-12 may not meet its gate before SJL-3 meets SJL-3's.**

## The shared syntax library (SJL-13)

SJL-13 is the frontend's area milestone. Its verdict is "The frontend's syntax half (SJL-13.1)" under "Area
verdicts", with Broiler.VM's JSD-0083, and this section is this repository's record of the library's work: its
design, the stage table, the classification, the rule and its commits. *(Added 2026-10-26, at SJL-13.1.)* In this
section, as in that entry, "decision Dn" is JSD-0083's.

- **The design and the API.** The library is built to the contract "The frontend contract (SJL-12)" records, above,
  with Broiler.VM's JSD-0080. Its names, its request and its result are in the home's design document,
  [`Broiler.ECMAScript/docs/frontend/design.md`](../../Broiler.ECMAScript/docs/frontend/design.md), section 9, which
  SJL-13.1 adds, with dated revisions of the sections the decisions change (decisions D6 to D17). Each type and
  member name is fixed by the library's `PublicAPI.txt` when SJL-13.2's stage 4a lands; the design document names
  what each carries.
- **The stage table**,
  [`Broiler.ECMAScript/docs/frontend/stages.tsv`](../../Broiler.ECMAScript/docs/frontend/stages.tsv), beside the
  catalogue: every catalogue row's stage under decision D10, the basis of that stage, the request switch that
  governs it, each engine's current stage and site, this engine's compiler-only and lazy checks, the correspondence's
  rows and the catalogue's stale cells. Its columns are described in the classification's README.
- **The classification**,
  [`docs/evidence/sjl-13-001/classification/`](../evidence/sjl-13-001/classification/README.md), written by
  [`scripts/frontend/classify_parse_corpus.py`](../../scripts/frontend/classify_parse_corpus.py), whose `check` mode
  rebuilds it byte for byte. It is what "as classified" means in the home's clause 2 and in SJL-13.2's done-when.
- **The work, in order**, each merge outside SJL-11.5's span:
  1. **SJL-13.1**, these records, with Broiler.VM's: the verdict, the start entry, and its bundles `sjl-13-001` and
     `sjl-13-002`'s rules;
  2. **the `syntax` vectors** at the next free corpus revision, both engines' first runs classified, and Broiler.VM's
     pin (decision D25): the rule's vectors commit; *(2026-10-27: corpus revisions 29 and 30 carry them, on the branch
     `sjl-13-2-vectors`, with both engines' first runs classified, a difference of this engine under row P16b, which
     the correspondence added for it under the owner's decision D-asi of 2026-10-27 (Broiler.VM's JSD-0083), as bundle
     [`sjl-13-001`](../evidence/sjl-13-001/README.md)'s README reports; the merge that carries them, after Broiler.VM's
     change that carries that row, is the rule's vectors commit, and Broiler.VM's pin moves in its own change after
     it)* *(revised 2026-10-27, decision D-asi; from "with both engines' first runs" the note read "with both engines'
     first runs classified but a difference of this engine that no row of the correspondence holds, which bundle
     `sjl-13-001`'s README reports; the merge that carries them, once the correspondence answers it, is the rule's
     vectors commit, and Broiler.VM's pin moves in its own change after it")*
  3. **SJL-13.2, the library**, one merge per stage, developed against the parse corpus, the vectors, the
     classification and the home's fuzz target, with no change to `Algorithms` (decision D9):
     - 4a, the skeleton: the projects, SL1's row, the solution filter and the solution, the assurance list,
       `PublicAPI.txt`, and every type of the request, with the library-level runner's frontend adapter and the probe's
       syntax path;
     - 4b, the tokenizer: the lexical grammar, identifiers through the Unicode provider, numeric literals through
       `NumericLiterals.Evaluate`, regular-expression delimiting by the parser's goal, structured templates, and the
       `m` suffix only through the extension;
     - 4c, the tree and the parser, seeded from Broiler.VM's `JsSyntax`, with full spans, raw facts and the module's
       top-level-`await` fact as the specification defines it; cover grammars by conversion, arrows without
       rewinding, and nesting and tree-depth checks before the stack;
     - 4d, the diagnostics and the parse-time early errors: the declarations walk, the side tables, the first refusal
       in the library's order, the `request` rows, no `engine` row, and the validator through the provider;
     - 4e, the limits, determinism and fuzzing: the tree-depth constant's derivation, the left-operand charge, the
       home's fuzz target and the deep-nesting corpus;
     - 4f, done: `IsPackable` true, the changelog and the assurance artefacts. Its merge is the rule's library commit,
       where every part of the rule is run.

     Until SJL-14's bridge merges, no product project of this repository references the library;
  4. **the publication** of the shared stream's version that carries the library, with the owner's approval, before
     Broiler.VM's step 1 merges (decision D30). This engine's SJL-14 references the library by project and needs no
     publication to build.

  Broiler.VM's tools commit and its steps 1 and 2 are Broiler.VM's, judged by its bundles `sjl-13-001` and
  `sjl-13-002`.
- **The rule and its commits.** [`docs/evidence/sjl-13-001/decision-rule.md`](../evidence/sjl-13-001/decision-rule.md)
  names three commits before they exist: the rule commit, which carries it; the vectors commit; and the library
  commit, where its parts A to H are run, each from a clean checkout. Bundle
  [`sjl-13-001`](../evidence/sjl-13-001/README.md) names each when it exists.
- **What SJL-13 leaves to the next milestones.** SJL-14 files its own start entry; its records can be written once
  SJL-13.1's have merged and stage 4c's tree is stable, and its bridge merges after stage 4f. SJL-15's model grows from
  the declarations walk SJL-13.2 begins in `Broiler.ECMAScript.Semantics` (decision D10).

## Broiler.JS on the shared syntax library (SJL-14)

SJL-14 moves this engine's compiler and portable compiler onto `Broiler.ECMAScript.Frontend`. *(Added 2026-11-03, at
SJL-14.0 and SJL-14.1.)* In this section "decision Dn" is SJL-13's plan's numbering, which Broiler.VM's JSD-0083 keeps:
D1 to D39 are JSD-0083's, and **D40 to D56 and D64 are SJL-14's, recorded here** (D64, which SJL-13's plan numbers
with SJL-15's decisions, is recorded here because its SJL-14 half binds this milestone's rule), with Broiler.VM's
records named in D40, D41, D45, D48, D49 and D50 filed in Broiler.VM's own change.

**SJL-14.0, the start.** On 2026-10-25 the owner wrote: "Continue with the remaining SJL milestones". The programme
reads those words as the choice to start SJL-13, and JSD-0083 records that "the same words, read the same way, start
SJL-14 and SJL-15 when each files its own start entry". This entry is SJL-14's: the words are read as the choice to
start SJL-14 now that SJL-13.1's records have merged and SJL-13.2's library is done (its stage 4c and stage 4f
merged; the library commit `d4b330d8`). **That is a reading of the owner's words, recorded as one, and not a decision
taken under the owner's direction** (decision D40, read as D1 reads them). It moves no gate: SJL-14 waits on SJL-13,
and may not meet its gate before SJL-13 meets its own. Broiler.VM's ledger records the same start in its SJL-14.0
entry.

**SJL-14 files no verdict** (decision D40). Its area was judged at SJL-13.1 for both engines: JSD-0083's X1 covers this
engine's front end. What the gate's X6 needs of this engine, the deletion list member by member, is
[`docs/evidence/sjl-14-001/dispositions.md`](../evidence/sjl-14-001/dispositions.md), and Broiler.VM's change for
SJL-14.1 adds it to JSD-0083 by a dated note, so that the gate roadmap section 5 applies "at the start of every area
milestone" has this engine's half on record before any code of SJL-14.

**The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
review." Each decision below that SJL-13's plan recommends and JSD-0083 did not revise (D40, D42, D43, D49 to D56
and D64) is taken as recommended and recorded as **the owner's decision of 2026-11-03**, the day it is filed. **D41
and D44 to D47 are recorded as JSD-0083 revised them on 2026-10-26, the owner's decisions of that day**, recorded here.
D45 is revised once more on 2026-11-03, on this bundle's finding S14-F1, under the same direction and in the way
JSD-0083 revised it on the correspondence's findings; Broiler.VM's change records that revision by a dated note under
JSD-0083's D45. Where the records of SJL-14.1 apply D50, D55 or D56 with a finding, the row quotes the decision as
recommended and states the finding beside it. The direction changes no rule of the programme: `Accepted` still needs review, a gate still waits on the
milestones it waits on, and rules still come before runs. **Two entries are not taken under it**: the start, above,
which is a reading; and D48, which SJL-D-23's note of 2026-10-20 reserves to the owner.

**The decisions** (of 2026-11-03, but D41 and D44 to D47, of 2026-10-26, and D45's revision of 2026-11-03).

| # | Decision | Reason |
|---|---|---|
| D40 | **SJL-14.0 at SJL-14.1's records**, quoting the owner's words read as D1 reads them (a reading); **no verdict JSD**: SJL-13.1's verdict covers both engines' front ends (its X1). **X6's Broiler.JS half is completed by a dated note on JSD-0083 at SJL-14.1**, member by member from `dispositions.md`. Dated notes under Broiler.VM's roadmap sections 5 and 11 | Roadmap section 5 lists SJL-14 among area milestones, but its area was judged at SJL-13.1 for both engines; X6 needs this engine's public-type dispositions (SJL-D-25), which exist only from SJL-14.1 |
| D41 | As JSD-0083 revised it on 2026-10-26 (finding F25): **at SJL-14 the compiler parses through the bridge**, which builds this engine's `AstNode` tree from the shared tree; `PortableCompiler` and `ModuleSourceAnalysis` read the shared tree directly. **The compiler moves to the shared tree at SJL-15.5**, with the model; the bridge then remains only as the obsolete adapter packages' implementation. A work-package revision of SJL-14.3's last sentence and of SJL-15.5, and **SJL-15's clause 10 revised at SJL-14.1, before any evidence and under update rule 5**, to add that the compiler reads no bridged tree. "The remains of `SyntaxValidation`" is read as its compiler-stage checks, which SJL-15.5 deletes; **its two token re-scans, `SyntaxValidation.ContainsLegacyOctalLiteral` and `DirectEvalSupport.ContainsStrictReservedWordUsage`, are deleted at SJL-14's cut-over**, with the direct-eval prefix (D44) | Clause 1 asks that the compiler "parse with" the frontend, which the bridge meets; moving the compiler now would port `SyntaxValidation`, `FastFunctionScope` and `FreeNameScan`, which SJL-15.5 deletes; the re-scans refuse legal programs (row R36) and check rows the library checks once the request carries forced strictness |
| D42 | **Two windows, W1 (the bridge, switch off) and W2 (the cut-over), one merge each**, bases at their first parents; the product paths named explicitly, `Broiler.JS/Broiler.JavaScript/` included. Library merges after W1 follow D52 | One merge per window (JSD-0082's D3 and D4); the pattern `Broiler.JS/Broiler.JavaScript.*/` of this repository's earlier rules does not cover `Broiler.JS/Broiler.JavaScript/` |
| D43 | **The test-only switch is an internal member of `JSCompilationOptions`**, set by tests through the existing internals grant and by a hidden flag of the shell; removed at W2; no environment read | An environment read in the parse path is ambient state, which SJL-12's clause 1 and the parser's architecture test exclude, and which SL3 forbids in the shared library |
| D44 | As JSD-0083 revised it (findings F13, F21 to F24, F26, F27): **the inherited context at W1** - strictness, the function, method, derived-constructor (from the class's heritage) and field-initialiser flags, the dynamic function's parameter and body goals; the visible private names only where this engine checks them at parse time today, which the probes of SJL-14.1 find it does, so **the bridge passes them** (read at SJL-14.1 by the probes; no decision changes); the caller's lexical names absent. The bridge passes the caller's strictness for direct eval and the prefix goes with its two skip sites; it serves every parse entry point (row R35), each with its request; it synthesises the linked `FastToken` chain (with each token's source, offset, length and start location), `HoistingScope` and `AnnexBFunctionNames` from the library's side tables in source order, `IsSyntheticFunctionStatementBlock` and the Annex B.3.4 block, `ContainsContinue`, `WasParenthesized` and `TokenTypes.Decimal`; not `IsLoopBookkeeping`; and it refuses an import attribute this engine cannot honour (EX0021) as its parser does | Without the inherited context the library would refuse `eval("new.target")` in a function and every other eval-context row the stage table assigns `syntax`; JSD-0080's D23 lists the Annex B.3.4 block for SJL-14. The probes: [`docs/evidence/sjl-14-001/inherited-context/`](../evidence/sjl-14-001/inherited-context/README.md) |
| D45 | As JSD-0083 revised it on 2026-10-26 (findings F8, F9, F23 to F25, F28, F32; its note of 2026-10-27 for P16b), **and revised again on 2026-11-03 on finding S14-F1**: the corrections **K1** to **K10**, listed by row in [`corrections.md`](../evidence/sjl-14-001/corrections.md), K10 holding the rows the correspondence classes and the rows IC1 to IC7 SJL-14.1's probes add; **class E** (early-error stage) and **class T** (timing), listed by row before evidence and admitted in the rule's parts C and D (E and T) and A (T). **Class E, as revised on 2026-11-03**: an input P accepts and P or B refuses, K refusing on both sides, with a SyntaxError whose shared code is a `syntax` row of the stage table, where either the row's catalogue `js_stage` names only compiler sites (`SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*`, `DirectEvalSupport`; the stage table's `js_compiler_only` `yes`), or the row's `js_compiler_only` is `partly` or `no` and bundle `sjl-13-001`'s classification expects the input refused in configuration J with that row among the rows whose codes may come first; a row the stage table marks `not-checked` is never class E. The text the revision replaces, as JSD-0083 revised it, read "**Class E (early-error stage)**: the `syntax` rows whose catalogue `js_stage` names only compiler sites (`SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*`, `DirectEvalSupport`), for an input P accepts and P or B refuses, K refusing on both sides (among them `await using` in a script's block, which `SyntaxValidation` refuses as "Unexpected await" today)". JSD-0080's D27's listed refusals are C3, not corrections | The hand-offs of SJL-3's, SJL-7's and SJL-12's bundles and the milestone's expected list, completed (14-h); finding S14-F1: an input this engine's parser accepts and its compiler refuses under a row it checks partly in its parser (`js_compiler_only` `partly`), or under a parser row its parser missed for that input, makes the same move from the compiler to the parse that class E names, and the wording by row left it unclassified in the rule's parts C and D |
| D46 | As JSD-0083 revised it (findings F8, F24): V8-style text rendered from identifiers; **the texts hosts and tests read are kept** ("Unexpected await", which the shell's retry reads, and the texts this engine's tests assert); every other change is class M, listed by a generated table, N25's position move among them; the changelog states the rendering change once | SJL-D-11; a changelog entry per message would repeat the generated table |
| D47 | As JSD-0083 revised it (finding F26): this engine passes the library's supported maximum nesting; **(a) the parse runs on a thread with the stack the library documents**, reused across parses, the hop skipped on a thread the engine started with that stack, with no `StackSegment`; the entry points that parse off `CompilationStack` enter it (`DirectEvalSupport.Validate`, `PortableCompiler`, `ModuleSourceAnalysis`). (b), the library's explicit stacks, is taken by a dark merge if the rule's part I or risk R26 show the hop's cost | JSD-0080's D27, and its "What each engine keeps" ("Stack segmentation, if it keeps it" is this engine's): the library holds no engine type |
| D48 | **Not taken: the choice is the owner's own** (below) *(Noted 2026-11-03, later: answered by the owner, "Please defer the measurement", the owner's own choice of alternative (i), not one taken under the direction above; see the note after "D48, the owner's own choice" below.)* *(Noted 2026-11-18: where this cell's note reads "answered by the owner, "Please defer the measurement"", the owner chose again at SJL-D-23's trigger (a), "Take it on the lane". Part I is to be taken as written on the controlled lane `win-x64-r7-5800x`, not before W2 merges, and clause 6 stays unmet until then. This is the owner's own dated choice; see the note of 2026-11-18 after "D48, the owner's own choice" below.)* | SJL-D-23's note of 2026-10-20 reserves the like choice for SJL-3's clause 7 |
| D49 | **Clause 5 revised before any evidence, under update rule 5**, its second sentence ("The pristine consumer and the downstream candidate-feed runs pass.") replaced by "The pristine consumer passes, and the downstream candidate-feed runs lose no test: every test that passes against the base candidate passes against the after candidate." Broiler.JSeal's module adapter, which reflects on `CoreScript` members SJL-12.5 removed, is recorded as SJL-12.5's finding, with a dated correction of `docs/public-api.md`'s consumer check (it searched for references, not reflection). Broiler.HtmlBridge runs in a scratch override | Broiler.JSeal and Broiler.HtmlBridge are not changed by this programme; a failure SJL-14 did not cause cannot be its to fix; a change of meaning is a gate revision, not a reading |
| D50 | **Per type** ([`dispositions.md`](../evidence/sjl-14-001/dispositions.md)): `Parser`'s public types a facade over the bridge, keeping every member SJL-12's tool and JSeal use and throwing today's exception types (`FastParseException` for a refusal), obsolete; `Ast`'s tree types obsolete; the non-tree types (`StringSpan`, `StringSpanReader`, `ArraySpan<T>`, the property contracts, `UnaryOperator`, `TokenTypes`, `CancellableDisposableAction`, `RegExpUnicodeValidator`, the Unicode tables) moved with `[TypeForwardedTo]` to `Broiler.JavaScript.Expressions`, which every reader already references; `FastScope` and `FastScopeItem` obsolete in the facade. **Clause 2 revised before any evidence, under update rule 5**, quoting "The deletion list of SJL-14.5 is absent." and replacing it by "The implementations on SJL-14.5's deletion list are absent; the public names SJL-D-12 (i)'s adapter keeps stay only as obsolete facades over the bridge.": `FastParser`'s partial files, `FastScanner`'s and `FastTokenStream`'s scanning code, `FastKeywordMap`'s table, `CharExtensions`' scanner members, `RegExpValidator` and the parser's half of `FastScope` leave the tree, and the rule's part K searches for them. **One removal version, `1.0.0`**, for every SJL-D-12 (i) and SJL-D-25 (b) mark, except `UndoMark`'s, which keeps its mark of SJL-12.5 without a removal version until a consumer check is recorded (JSD-0080's D20). The marks carry one diagnostic identifier, suppressed by one `NoWarn` in the repository root's `Directory.Build.props`, which every project of the repository inherits, SJL-12's tool under `docs/evidence/` included, until SJL-15.5. `Expressions` is in the portable compiler's closure only if it uses a moved non-tree type (D53); a dated note on SJL-14.3's sentence (clause 4 names only "the runtime"). *Applied at SJL-14.1 with three findings from the code at `33d3093b`, stated in `dispositions.md` ("Where this file departs from decision D50's list, and why"):* `FastParseException`, which the list does not name, is a non-tree type by the reason below (`Runtime` catches it, `CoreScript.cs:109` and `JSException.cs:265`), so it moves and is forwarded with them; "the Unicode tables" are internal since SJL-12.5 and name no public type, so they move with their readers and need no forwarder; and the facade keeps every public member of `Parser`, those SJL-12's tool and JSeal use among them | `Ast` is the engine's foundation, not only a tree: obsoleting it whole would mark types `Storage`, `Runtime` and `BuiltIns` use. SJL-12's tool sets `TreatWarningsAsErrors`, names `FastParser`, `FastTokenStream`, `FastKeywordMap` and `StringSpan`, and catches `FastParseException`, so the rule's parts B and D need it to build unchanged and to see today's exception types. Keeping the names changes what clause 2 means, which is a revision, not a reading |
| D51 | **W2 after SJL-11.5**: the provider then wraps Broiler.Regex's validate entry, and SJL-14 deletes the division fallback only. If W2 is ready first, `RegExpValidator`, `DecodeBracedUnicodeEscapes` and the parenthesis patch are **relocated behind the provider, not deleted**, and SJL-11.5's rule is revised by a new dated rule file in this repository's `docs/evidence/sjl-11-001/`, filed with W2's records, before any of that rule's runs, quoting the old one whole and restating its part E-3 for the provider. At SJL-14.1 neither SJL-11.5 nor SJL-9.5 has landed | JSD-0081's D22, as JSD-0080's D23's dated note quotes it; part E-3 names a scanner W2 deletes |
| D52 | **Each merge to `Broiler.ECMAScript.Frontend*/` after W1 is a dark window**: one merge, judged by the rule's part B identity with the switch off; its switch-on outputs retained and read, and part C, taken at W2's base, judges them. **This departs from rule 1 of the programme's working order (O-4)**, which lets no library stage change a project a Broiler.JS project references outside its milestone's Broiler.JS cut-over merge: the identity check and part C replace the window that rule asks for. A change that moves a switch-off outcome lands only inside a cut-over window. The same holds for SJL-15's model stages | This engine references the home by project, so every such merge is a product change here; the library fixes the side-by-side run asks for cannot all wait for W2 |
| D53 | `scripts/performance/collect_phase5.py` run locally at W2's base and after on linux-x64, with no dispatch; a closure listing at W2. **`Portable.Compiler.csproj` references the frontend and `Portable`, and `Broiler.JavaScript.Expressions` only if it uses a non-tree type that moves there**, and no longer `Ast` or `Parser`; a dated note on Broiler.VM's `architecture.md` section 6.2 | The check runs in CI only on a dispatch of `performance-baseline.yml`, which agents do not dispatch; today the project references `Ast`, `Parser` and `Portable` |
| D54 | **The parser's architecture test is retargeted at W2** to the adapter `Parser` assembly; its fixtures drive the facade. The library is held by SL2 and SL9 | The test's subject becomes the facade at W2 |
| D55 | Corrected at SJL-14.1 by dated notes: `SharedLibraries.md`'s and [`docs/public-api.md`](../public-api.md)'s `AssemblyCodeCache` follow-up (done by #1071); the remarks' citations (`ForLoopPreparation.cs` after #1076, `DirectEvalSupport.cs`); SJL-D-25's "no type forwarding anywhere" (a note on Broiler.VM's `decisions.md`); `governance.md` section 5.3. *Applied at SJL-14.1:* the citations' notes stand after the citations in this plan's SJL-12 section and in `docs/public-api.md` (`ForLoopPreparation.cs:47-50` at `33d3093b`; `DirectEvalSupport.cs:92-93` unchanged); the remarks themselves are code, and W1 corrects them, the first product change to those files; `governance.md` is Broiler.VM's, noted in its change | #1071 did the follow-up; #1076 moved the remark's lines; JSD-0080's D20 added a forwarder |
| D56 | **Each SJL-14 and SJL-15 Broiler.JS bundle has a `manifest.json`** by the bundle-manifest script. *Applied at SJL-14.1 with a finding:* this repository has no bundle-manifest script (the one Broiler.VM's `validation.md` section 6 describes is Broiler.VM's), so each bundle's manifest is written at its evidence step by that bundle's own script, as earlier bundles of this repository wrote theirs | Broiler.VM's `validation.md` section 6; bundle `sjl-12-001` recorded the exception |
| D64 | Class T (an error raised before evaluation that the base raised later, or not at all), listed, **at SJL-14 for the rows D10 moves into the parse** (the rule's parts A, C and D, by [`corrections.md`](../evidence/sjl-14-001/corrections.md)) **and at SJL-15 for the rest**. SJL-13's plan numbers it with SJL-15's decisions; it is taken here, with D40 to D56, because its SJL-14 half binds this milestone's rule, and SJL-15's records read its other half | Broiler.VM's `areas/static-semantics.md`'s acceptance; from SJL-14's W1 the library refuses at the parse some rows this engine checked lazily |

*(Noted 2026-11-03, at SJL-14.2 (window W1), before W1 merges, where W1's code departs from the decisions above or
leaves part of one to W2. **D43:** the switch is the internal `JSCompilationOptions.ParseThroughBridge`, set by tests
and by the shell's hidden flag as D43 says; in addition, the switch-on pass over the test projects is a build given
`-p:BroilerParseThroughBridge=true`, which defines `BROILER_PARSE_THROUGH_BRIDGE` and so turns
`JSCompilationOptions.BridgeByDefault` on for every compile. This is no environment read in the parse path, but
MSBuild reads an environment variable of the same name as a property, so `Broiler.JS/Directory.Build.props` ignores the
property when the variable is set, and the architecture test `BridgeSwitchTests` fails a build with the switch on.
W1 adds three internals grants: Engine to the shell (`BroilerJS`, for the flag), Expressions to Parser (the bridge
reads the compile worker's stack), and Parser to the compiler, BuiltIns and Parser.Tests. **D30:** the parser
references `Broiler.ECMAScript.Frontend` privately, so the `Broiler.JavaScript.Parser` package carries no dependency on
it at W1 and a consumer's restore from nuget.org is unchanged; W2's package waits for a published Frontend. **D46:**
"Unexpected await" is not one of the bridge's texts. The old parser never read a script's `await` as an expression, so
`SyntaxValidation`'s "Unexpected await" never came from a script's parse, and the shell's retry never fired on one;
keeping the text would have given the retry a behaviour the old path never had (a script awaiting at its top level
run with the switch on and refused with it off). The bridge renders the library's diagnostic, with its position.
**D44:** left to W2, with the reasons recorded: the direct-eval code compiled with the switch on still carries
`DirectEvalSupport`'s `"use strict";` prefix, and so does the text its validation parses (with `ForcedStrict` set
there, not in the compile's request): the compiler reads the code's strictness from that directive, so the prefix, its
two skip sites and the reserved-word re-scan go together at W2, and N25, N26 and R36 stay uncorrected at W1; the derived-constructor
flag is computed as today (`allowSuperCall`, from `MemberInits`), not from the class's heritage (R24), because the
library does not read it at this pin and the heritage would reach `DirectEvalSupport.Execute` only through a new
parameter of that public method, which W1 must not move; a class static block's flags have no slot in the library's
`InheritedContext`, so none is passed, with no observable effect at this pin; and the token facts are made from the
tree's spans, since the library does not publish its token stream (`ParseResult.Tokens`), which only the parenthesis
check reads and which a library dark merge (D52) would add. **D41 and R35:** `PortableCompiler` and
`ModuleSourceAnalysis` still parse with `FastParser` with the switch on; D41 has them read the shared tree directly,
which is W2's change. **K10** gains three rows SJL-14's own side-by-side run found, W1a to W1c, in
[`corrections.md`](../evidence/sjl-14-001/corrections.md).)*

**D48, the owner's own choice: clause 6 while no controlled lane exists.** SJL-D-23's note of 2026-10-20 reserves the
like choice for SJL-3's clause 7: "That choice is the owner's own when it comes, not one taken under the owner's
direction of 2026-10-18." The question is put to the owner with two alternatives: **(i) keep clause 6** ("Broiler.JS's
parse measurement meets its rule."), unmet until a controlled lane exists, and, by a dated note on SJL-D-23, name it
among the clauses its trigger (b) lists, so that trigger (b) stays reachable; or **(ii) revise it** before any evidence,
under update rule 5, to "Broiler.JS's parse measurement meets its rule on a controlled lane, or, until one exists, is
taken under its rule and recorded void". **Until the owner chooses, clause 6 stands as written**, and the rule's part I
is void, neither MET nor NOT MET, on a lane that is not controlled.

*(Noted 2026-11-03, later, on the owner's answer to D48. On 2026-11-03 the owner wrote: "Please defer the
measurement". The paragraph above said "Until the owner chooses, clause 6 stands as written"; the owner's words choose
(i). **Clause 6 is kept as written**, "Broiler.JS's parse measurement meets its rule.", unmet until a controlled lane
exists: the lane of Phase-0's item 0-8, which "Clause 7's measurement half" in this plan's SJL-3 section names as
serving SJL-14's clause 6. Broiler.VM's change names it, by a dated note of the same date on SJL-D-23, among the
clauses that decision's trigger (b) lists, so that trigger (b) stays reachable; the trigger as that SJL-3 section
states it, "(b) this half, Broiler.VM's clause 13 if it is still open, and SJL-4's clause 9, a Broiler.VM clause, are
the only unmet clauses of SJL-3 to SJL-15 and SJL-17", is read with SJL-14's clause 6 among those clauses. The rule's
part I is unchanged: on a lane that is not controlled it is void, neither MET nor NOT MET, and its runs there are
retained and recorded void. (ii) is not taken, and no revision is written. **This is the owner's own dated choice**,
as SJL-D-23's note of 2026-10-20 reserves it, and not one taken under the owner's direction of 2026-10-18. Like the
owner's decisions of 2026-10-20 on SJL-3's clause 7 and SJL-4's clause 9, it writes no revision onto a lane that is
not controlled.)*

*(Noted 2026-11-18, at decision SJL-D-23's trigger (a), on the owner's own dated choice for clause 6. The note above
reads "**Clause 6 is kept as written**, "Broiler.JS's parse measurement meets its rule.", unmet until a controlled lane
exists". A controlled lane exists, [`win-x64-r7-5800x`](../evidence/lanes/win-x64-r7-5800x/README.md), as the note of
2026-11-17 in this plan's SJL-3 section records. On 2026-11-18 the owner was asked: "SJL-14 clause 6 (Broiler.JS's
parse measurement, part I of the sjl-14-001 rule) was deferred on 2026-11-03 until a controlled lane exists. Now that
win-x64-r7-5800x is recorded, what should happen?", and chose "Take it on the lane": "Part I is taken on
win-x64-r7-5800x as written, the way part B was. I write a short brief for a local session on your workstation; you run
it there." Not taken: "Keep it deferred": "Clause 6 stays unmet. It is still listed under trigger (b), and nothing is
run." **Part I is to be taken as written on the controlled lane `win-x64-r7-5800x`.** It cannot be taken yet. Its arms
are "B the base commit's build; A the after commit's" (the rule's part I), and the after commit is W2, "the
default-branch merge that lands SJL-14.3 to SJL-14.5", whose first parent is the base commit (the rule's "The commits,
named before they exist"). Neither exists: W2 is held for SJL-11.5 (decision D51; the SJL-14 row of this plan's item
table, its note of 2026-11-15). Part I is therefore not taken on the lane before W2 merges, and nothing is run for it
before then. Until then clause 6 stays unmet, part I has no run and stays as bundle
[`sjl-14-001`](../evidence/sjl-14-001/README.md) records it (void on a lane that is not controlled), and clause 6 stays
among the clauses trigger (b) lists. The rule is not revised. **This is the owner's own dated choice**, as SJL-D-23
reserves it, and not one taken under the owner's direction of 2026-10-18. Broiler.VM's dated note under SJL-D-23 is
filed in Broiler.VM, separately from this record. Nothing is met or accepted.)*

*(Noted 2026-11-19, after SJL-11.5's merge: the note above reads "Neither exists: W2 is held for SJL-11.5 (decision
D51; the SJL-14 row of this plan's item table, its note of 2026-11-15)." SJL-11.5 merged on 2026-11-19 as
Broiler-Platform/Broiler.JS#1126 (`b2b226cd`), and the hold of decision D16 ended with that merge (this plan's SJL-11
section). W2 is no longer held for SJL-11.5; it still does not exist, and neither does its base commit, so part I is
still not taken on the lane before W2 merges, and clause 6 is unmet. Its protocol points are filed before any part I
run, in bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s "Before W2's base, read on 2026-11-19" (reading B10).
The note above is left as written; the note after "The work, in order" below records the rest.)*

**The records SJL-14.1 files here** (bundle [`docs/evidence/sjl-14-001/`](../evidence/sjl-14-001/README.md)): the rule,
[`decision-rule.md`](../evidence/sjl-14-001/decision-rule.md), eleven parts A to K, filed before any change it judges;
the corrections K1 to K10 with the classes E and T and the inherited-context probes,
[`corrections.md`](../evidence/sjl-14-001/corrections.md); the disposition of every public type and member of `Ast`
and `Parser`, with the deletion list member by member, [`dispositions.md`](../evidence/sjl-14-001/dispositions.md);
and part I's workloads and part J's seeds, [`inputs/`](../evidence/sjl-14-001/inputs/README.md).

**The corrections K1 to K10** (decision D45; each with its rows and test262 paths in
[`corrections.md`](../evidence/sjl-14-001/corrections.md)):
- **K1** the strict-mode `NonOctalDecimalIntegerLiteral` (`strict-non-octal-decimal`), and a legacy octal literal as a
  strict object key or class element name refused at the parse;
- **K2** the digit lookahead after `?.` and `.` (`char.IsDigit`);
- **K3** the tagged template's raw-segment trimming;
- **K4** line terminators in the lexer, the same in outcome;
- **K5** an invalid regular-expression literal refused, not read as division (row S9);
- **K6** the braced-escape decoding and the parenthesis-balance patch, only if SJL-11.5 has not landed (D51);
- **K7** early errors the parser missed (the catalogue's `syntax` rows this engine does not check);
- **K8** the grammar failures this engine admits (bundle `sjl-12-001`'s `completeness.md`);
- **K9** the regular-expression rows EE0186 to EE0190 and EE0208, only if the provider wraps Broiler.Regex's validate
  entry;
- **K10** the rows the correspondence adds, and the rows IC1 to IC7 of SJL-14.1's inherited-context probes.

**The dispositions** (decision D50; every public type and member in
[`dispositions.md`](../evidence/sjl-14-001/dispositions.md)):

| Disposition | Applies to | Removal |
|---|---|---|
| `facade-obsolete` | every public type and member of `Parser` but one, `FastScope` and `FastScopeItem` among them | `1.0.0` |
| `kept-obsolete-no-version` | `FastTokenStream.UndoMark()` | none |
| `obsolete` | every public type of `Ast` but the non-tree types | `1.0.0` |
| `moved-forwarded` | `Ast`'s non-tree types, moved to `Broiler.JavaScript.Expressions` with `[TypeForwardedTo]` in `Ast`: `StringSpan`, `StringSpanReader`, `ArraySpan<T>`, `IPropertyAccessor`, `IPropertyValue`, `UnaryOperator`, `TokenTypes`, `CancellableDisposableAction`, `RegExpUnicodeValidator` and `FastParseException` | none |

**The rule and its windows.** The rule names its commits before they exist: the rule commit, which carries it; **W1**,
the merge that lands SJL-14.2's bridge with the switch off; **any dark commit** between W1 and W2 that changes only
`Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` (D52); and **W2**, the merge that lands SJL-14.3 to SJL-14.5, whose
first parent is the base commit. Part B judges W1 and each dark commit by identity with the switch off; part C judges
the side-by-side run at W2's base; parts A and D to K judge the cut-over at W2's base and W2.

**The work, in order** (each merge outside SJL-11.5's span and SJL-9.5's window): SJL-14.1, these records with
Broiler.VM's; **W1**, the bridge and the switch, with the side-by-side tool; the side-by-side runs at W1, read, and the
library fixes they need as dark merges; **W2**, preferably after SJL-11.5, one commit per stage (the switch on and
removed with the old parser's implementations; the portable compiler and the module analysis on the shared tree; the
two re-scans and the eval prefix deleted; the adapter packages; the regular-expression code and the division fallback;
the architecture test, the pristine consumer, `docs/public-api.md` and `Broiler.JS/CHANGELOG.md`); then the evidence,
SJL-14.6.

*(Noted 2026-11-19, after SJL-11.5's merge and before W2's base.)* The paragraph above reads "(each merge outside
SJL-11.5's span and SJL-9.5's window)" and "**W2**, preferably after SJL-11.5". SJL-9.5's window is past (#1110,
`a5f2692e`), and SJL-11.5 merged on 2026-11-19 as Broiler-Platform/Broiler.JS#1126 (`b2b226cd`), its after records as
#1127 (`fefb4f9c`); the hold of decision D16 ended with that merge. What follows is recorded in bundle
[`sjl-14-001`](../evidence/sjl-14-001/README.md)'s README, "Before W2's base, read on 2026-11-19", with the rule
not edited and no line of it revised:
- **The owner's answer 9 of 2026-11-19**, the owner's own and not one taken under the owner's direction of 2026-10-18.
  Asked (2026-11-19): "SJL-14's W2 cut-over (the frontend switch removed, the re-scans and facades deleted, the
  division fallback deleted) needs its base runs taken at Broiler.JS main's head, with W2 as the very next merge. Any
  merge in between, including docs-only ones, voids those base runs. For SJL-11.5 you ordered that hold yourself. May I
  hold Broiler.JS main the same way for W2, from the first part C base run until W2 merges? That is expected to take a
  day or two." Chosen: "Allow the W2 hold" — "Records and the W2 branch are prepared first, with nothing held. The hold
  then starts at the first base run, and nothing merges to Broiler.JS main until W2 merges. I record the hold's start
  and end, and I dispatch test262-evidence.yml for part A once at the base and once at the after commit, as for
  SJL-11.5." Not taken: "Prepare only, hold later" — "I prepare the records and the W2 branch but take no base run. The
  hold waits until you say it can start." **W2's hold begins at the first base run, of any part, and ends with W2's
  merge; nothing merges to the default branch meanwhile; part A's base and after runs are taken by dispatches of
  `test262-evidence.yml`.** The hold has not started.
- **The readings taken under the owner's direction of 2026-10-18**, each with its alternative not taken, filed before
  any base run: **B1**, the rule's commit conditions read with decision D51 against SJL-11.5's merge, which changed the
  bridge's regular-expression provider to wrap Broiler.Regex's validate entry and deleted `RegExpValidator`,
  `DecodeBracedUnicodeEscapes` and the parenthesis patch, as D51's order gives SJL-11.5 to do (not taken: the rule's
  words alone, under which W1's condition fails, and part B, judged at W1, is void, and so is every part judged at the
  base commit or W2); the question is added to the owner's optional ruling on part B; **B2**, "SJL-11.5's span" read as decision D16's hold, which has ended (not taken: waiting for
  SJL-11.5's part B on the lane); **B4**, W2 before any merge of SJL-15.2's model (not taken: model merges before W2 as
  dark windows under D52); **B9**, part C's open items classified at the base commit, with any K10 row they need added
  before the first part C base run, rows that go beyond the rule's words on the rows it classifies by, as K10's rows
  W1a to W1c did, and are chosen after the run at W1 was seen, which is weaker evidence (not taken: a dated revision of
  the rule, or the rule's words alone; the owner may rule among the three); and **B10**, part I's protocol points (a
  number of repetitions that balances an order rotating by repetition, as Measurement.md's acceptance gate asks a
  "balanced, process-isolated order", where bundle `sjl-3-001`'s run on the same lane took one that does not; the
  harness's own repetitions argument, and Q the median over an arm's processes of each process's median; a fresh
  confirmation run, as that gate asks; the harness's own order as the warm-up; one corpus per process; the manifest
  naming the lane record's `machine.txt` by SHA-256; each arm its own commit's harness), filed before any part I run
  (not taken: putting each to the owner, who may override any of them before the run).
- **Part G's commits**, named before the base commit exists: Broiler.JSeal `a1216a80034e185d69d0d76d9ef855105698c00b`
  and Broiler.HtmlBridge `334974f90c655181bba2ea6587ebb65336154c5a`, each its default branch's head on 2026-11-19.
- **The shared stream's published version**: `0.1.0-preview.12`, published from `2f012739` (#1118), whose
  `Broiler.ECMAScript/` equals the default branch's; this plan's SJL-13 row records only `0.1.0-preview.11`. W2's merge
  waits for no publication (JSD-0083's decision D30); the note of 2026-11-03 at SJL-14.2 above, "W2's package waits for
  a published Frontend", is left as written. Two texts of `docs/public-api.md` that no longer hold, the heading of
  SJL-11.5's section ("not merged") and W1's "which has no published version yet", are W2's to correct by dated notes,
  as W2 changes that file.
- **The merges since W1** are listed through `fefb4f9c` in the bundle's README and its
  `bridge/merges-since-w1-paths.txt`; none since #1105 changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/`.

Nothing is met or accepted; every clause of this engine's, 1 to 6, is unmet.

*(Changed the same day, before this note merged, after review.)* The hold's start read "W2's hold begins at the first
part C base run", the question's words; the chosen answer reads "The hold then starts at the first base run", and the
SJL-15 note below, this plan's SJL-14 row and the status record's SJL-14 row read the same way now. B1's alternative
read "under which W1's condition fails and part B is void"; B9's read "with any K10 row they need added before the
first part C base run (not taken: a dated revision of the rule)"; B10's first point read "the repetitions in an order
that rotates by repetition as bundle `sjl-3-001`'s run on the same lane took them"; and the sentence on
`docs/public-api.md` is new, its notes having been taken out of this change.

*(Noted 2026-11-23, during W2's hold, before W2 merges.)* The note above reads "The hold has not started." and "Nothing
is met or accepted; every clause of this engine's, 1 to 6, is unmet." The hold started with the first base run at
`69a8a13b`, and W2's base runs are taken and retained in bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md),
whose README reads part C at the base commit NOT MET on the retained runs under the rule as written. On 2026-11-23 the
owner gave **answers 10 and 11**, the owner's own and not ones taken under the owner's direction of 2026-10-18, quoted
verbatim with their questions in that README's "W2's base, records part 5". Answer 10 chose "Revise rule
(Recommended)" — "I draft one dated revision of sjl-14-001's rule from your answer, admitting groups 1-5, 7, 8 (and 9
if you choose C below) for part C and part D, marked as weaker evidence written after the runs were seen. Part C is
then re-read on the same retained runs, nothing re-run, and W2 proceeds to its PR."; not taken: "Merge as NOT MET" and
"Hold W2". Answer 11 chose "Admit by revision (Recommended)" — "Include group 9 in the dated revision. W2's trees stay
as they are; no other span changes."; not taken: "Fix in W2". The revision is
[`decision-rule-revised-2026-11-23.md`](../evidence/sjl-14-001/decision-rule-revised-2026-11-23.md), written after
part C's base runs were seen and so weaker evidence: part C admits the differences of groups 1 to 5, 7, 8 and 9 the
answers name, and part D those of groups 1 to 3, 8 and 9, each class naming its inputs, tests or paths, and nothing
wider *(changed the same day, after review, before this note merged: it read "parts C and D admit the differences of
the groups the answers name", wider than the revision, which admits groups 4, 5 and 7 in part C alone)*; group 6, `test/staging/sm/regress/regress-610026.js`,
is not admitted, and W2 fixes it inside W2 by the bundle's decision B8 (`e0b2d17b`, with its review fix `80b04d26`, on
branch `sjl-14-w2`); part A is not revised. **Part C, read again under the revision on the same retained runs: read
NOT MET on the retained runs, for `regress-610026.js` alone.** The bundle records finding S14-F2 against the
correspondence for `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js`, as part A's C1 line
asks. The rule's file is not edited. Nothing is met or accepted; every clause of this engine's, 1 to 6, is unmet.

*(Noted 2026-11-24, after W2's merge.)* The note above reads "during W2's hold, before W2 merges" and "W2 fixes it
inside W2 ... on branch `sjl-14-w2`". **W2 merged** on 2026-11-23 as `83a9bd82a5d258d3001468095ebf43f9efc3e9a7`, the
merge of Broiler-Platform/Broiler.JS#1129, its first parent the base commit `69a8a13b`: it is the rule's after commit,
and **the hold of the owner's answer 9 ended** with it. Part A's after run, `test262-evidence.yml` run 45264304825 at
the after commit, and the after commit's local runs of parts D, E, F, G, H, J and K are retained in bundle
[`sjl-14-001`](../evidence/sjl-14-001/README.md)'s `after/`, with the runs the readings owed, and its README's "W2's
after commit, recorded on 2026-11-24" reads each part in the rule's words, under the revision where it changes the
part: **part A read NOT MET on the retained runs** (the `await using` for-of path of finding S14-F2, which part A's C1
line reads as a regression, and a Terser variant the runner skips as minifier-invalid output; `regress-610026.js`
passes at both commits); **part D read NOT MET on the retained runs** (span lines of the trees after the compiler's
for-loop pass, which part C never compared and no class admits; every outcome, message and parser-tree difference is
in a class); **parts E, F, G and H read MET on the retained runs** (part G with Broiler.JSeal's base side per test
taken after the after runs, under the base's harness); **part J read NOT MET on the retained runs** (the compiler's
documented `import.meta` refusal for module code without a module record, which the session's harness compiles, and
timeouts of one generated seed that reproduce unloaded; the session that compiles those module seeds is W2's own
tool); **part K read NOT MET on the retained runs** as the rule is written (lines of the changelogs, comments and
records, and other types' members of the same name, are hits that are neither facade members nor relocations, and the
search took common-word names in narrower scopes than the rule's, where bundle `sjl-11-001`'s E-1, the model part K
names, had explicit exceptions for such hits), with reading K1, under which it would read MET, put to the owner.
**Part I is pending** on the owner's lane `win-x64-r7-5800x`, its runs to be retained in the bundle's `measurement/`.
The items read NOT MET, with reading K1, **await the owner**; no fix and no rule change is made for them. The rule's
files are not edited. Nothing is met or accepted; every clause of this engine's, 1 to 6, is unmet. *(Revised
2026-11-24, in review of this note before it merged: it read "**part K read MET on the retained runs** on a reading
taken under the owner's direction of 2026-10-18, with its alternative recorded" and "The items read NOT MET, and part
K's reading, **await the owner**".)*

*(Noted 2026-11-24, later, on the owner's answers 12 to 15.)* The note above reads "The items read NOT MET, with
reading K1, **await the owner**; no fix and no rule change is made for them." On 2026-11-24 the owner gave answers 12
("Revise for 2 rows (Recommended)", part A), 13 ("Revise by list (Recommended)", part D), 14 ("Fix tool + revise V2
(Recommended)", part J) and 15 ("Revise like E-1 (Recommended)", part K), the owner's own and not ones taken under the
owner's direction of 2026-10-18, quoted with their questions in bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s
README, "W2's after commit, records part 2, recorded on 2026-11-24". They file one dated revision,
[`decision-rule-revised-2026-11-24.md`](../evidence/sjl-14-001/decision-rule-revised-2026-11-24.md), written after the
after runs were seen and so weaker evidence: part A admits the two rows by name (rev-for-of and rev-minifier), part D
the pass-tree span differences of the input variants its listed file names (rev-pass), part J a compile past the bound
of the seed V000002 and of its accepted mutants, and part K, as bundle `sjl-11-001`'s E-1 does, changelog, comment and
record lines and other types' members of the same name, each listed with its declaration, and the narrowed scopes;
nothing wider. Part J's mutation session was corrected to compile module seeds with a module record (`53afd382`) and
taken again with its control at the after commit, recorded as corrected after the findings were seen. Read again,
with the first readings left on record: **part A read MET on the retained runs**, **part D read MET on the retained
runs**, **part J read MET on the retained runs** of the corrected session, each under the revision, and **part K read
NOT MET on the retained runs** under the revision, for the hits that are the words of the bridge's message strings,
which answer 15 does not name; they go to the owner. **Part I is pending**
on the owner's lane `win-x64-r7-5800x`. The rule's files are not edited. Nothing is met or accepted; every clause of
this engine's, 1 to 6, is unmet.

*(Noted 2026-11-24, later, on the owner's answer 16.)* The note above reads "**part K read NOT MET on the retained
runs** under the revision, for the hits that are the words of the bridge's message strings, which answer 15 does not
name; they go to the owner." On 2026-11-24 the owner gave answer 16 ("Revise for msg text (Recommended)"), the owner's
own and not one taken under the owner's direction of 2026-10-18, quoted with its question in bundle
[`sjl-14-001`](../evidence/sjl-14-001/README.md)'s README, "W2's after commit, records part 3, recorded on
2026-11-24". It files one more dated revision,
[`decision-rule-revised-2026-11-24-k.md`](../evidence/sjl-14-001/decision-rule-revised-2026-11-24-k.md), written after
the runs and part K's first and second readings were seen and so weaker evidence, which changes part K alone: its MET
line also admits words inside a string literal that is a message text, listed with the message table that holds it
and checked to be no member name used by reflection or lookup, and lets a member of a .NET library type be shown by
naming that type and its assembly in place of a file and line; nothing wider. Part K's listing aid was extended to
list those hits so. Read a third time on the retained search, the first and second readings left on record, **part K
read MET on the retained runs** under the rule and its three revisions. **Part I is pending** on the owner's lane
`win-x64-r7-5800x`. The rule's earlier files are not edited. Nothing is met or accepted; every clause of this
engine's, 1 to 6, is unmet.

## Shared static semantics (SJL-15)

SJL-15 makes the frontend's semantic model the single source of the static-semantics facts, and moves this engine's
compiler onto it at SJL-15.5. *(Added 2026-11-17, at SJL-15.0 and SJL-15.1, Broiler.JS's half.)* In this section
"decision Dn" is SJL-13's plan's numbering, which Broiler.VM's JSD-0083 keeps: D1 to D39 are JSD-0083's, D40 to D56 and
D64 SJL-14's (above), and D57 to D66 SJL-15's. "Decision D-name" is JSD-0088's, or, where this section says so, this
section's own. The area verdict is "Static semantics (SJL-15.1)" in "Area verdicts" above.

**SJL-15.0, the start.** On 2026-10-25 the owner wrote: "Continue with the remaining SJL milestones". The programme reads
those words as the choice to start SJL-13, and JSD-0083 records that "the same words, read the same way, start SJL-14
and SJL-15 when each files its own start entry". This entry is SJL-15's in this repository: the words are read as the
choice to start SJL-15 now that SJL-13.1's and SJL-14.1's rules are filed, as this repository's status row for SJL-15
already said ("its records once SJL-13.1's and SJL-14.1's rules are filed"). **That is a reading of the owner's words,
recorded as one, and not a decision taken under the owner's direction**, as JSD-0088 and Broiler.VM's ledger's SJL-15.0
entry record the same start. It moves no gate: SJL-15 waits on SJL-13 and SJL-14, and may not meet its gate before they
meet theirs.

**The owner's direction.** On 2026-10-18 the owner wrote: "Please do all recommended decisions and skip waiting for
review." JSD-0088 takes under it each decision SJL-15.1 needs in Broiler.VM, as the owner's decisions of 2026-11-16; this
section records those that bind this repository, by JSD-0088's names and numbers, and takes under the same direction the
choices this repository's half needs, as the owner's decisions of 2026-11-17, each with the alternative not taken. The
direction changes no rule of the programme: `Accepted` still needs review, a gate still waits on the milestones it waits
on, and rules still come before runs. **Three kinds of entry are not taken under it**: the start, above, which is a
reading; decision D-census, which the owner answered for Broiler.VM's clause 5 on 2026-11-16 and which asks nothing of
this repository; and each publication of the shared stream (JSD-0083's decision D30).

**The decisions that bind this repository** (JSD-0088's, of 2026-11-16, as it records them).

| Decision | What it asks of this repository |
|---|---|
| D61 | **The `semantics` vectors follow the `syntax` vectors' route**: schema 1, as decision D25 of JSD-0083 set it for the `syntax` vectors, Script and direct-eval sources only, a module-goal or switch-dependent row having no vector; added to this repository's corpus before any shared code of SJL-15 lands (SJL-1.6), classified on both engines' first runs, each engine's divergence fields citing its records; retired, field by field, by a corpus revision after each engine's cut-over, with Broiler.VM's pin move. Only "SJL-15's `semantics` vectors follow the same route" is D61's own text, as JSD-0083 quotes it; the rest is D25's, which JSD-0088 reads as the route |
| D64, its second half | **Class T at SJL-15** (an error raised before evaluation that the base raised later, or not at all), for the rows SJL-15.5 moves from this engine's compiler into the model (`SyntaxValidation`, `FastCompiler.EarlyErrors`, the checks in `Visit*`, `DirectEvalSupport.Validate`), **listed by row before any run** in this repository's rule for SJL-15. Its first half is filed in "Broiler.JS on the shared syntax library (SJL-14)" above, and is not taken again |
| D65 | **No package edge from `Broiler.ECMAScript.Frontend` to Broiler.Regex** while SJL-11.4 and SJL-11.5 have not landed; the regular-expression seam stays the request's validator provider (JSD-0083's decision D7). When both have landed, D65 is taken again by a dated note on JSD-0088, before any change that would add the edge |
| D-x2 | **This repository's half of X2 and X6 before any SJL-15 code**: this engine's column of the correspondence with each row's class across both engines, and this engine's deletion list member by member with the disposition of every public type SJL-15.5 empties (SJL-D-25), carried into Broiler.VM by a change that adds JSD-0088's dated note completing X2 and X6. Until it is filed no row has a class across both engines, and no SJL-15.2 code merges |
| D-measure | **SJL-15.6's "each compiler's own measurement" is a rule part, not a gate clause**, as SJL-13 read its own: SJL-15's exit gate has no measurement clause, and none is added. On a lane that is not controlled the part is void, its runs retained and read and deciding nothing, as part I of bundle `sjl-14-001`'s rule is |
| D57, D58, D59, D62, D63, D66 | **Quoted nowhere, and not reconstructed.** SJL-13's plan, which numbers them, is held in no repository, and JSD-0088 records each as unquoted. This section does the same: nothing is put in their place. If the plan's text is found, each is compared with these records and every difference filed by a dated note |

**This section's own decisions of 2026-11-17**, each taken as recommended under the owner's direction and recorded as
the owner's decision of 2026-11-17.

| Decision | Taken | Not taken, and why |
|---|---|---|
| D-js-column | **This engine's column of the correspondence is held in this repository's bundle**, as `correspondence-broiler-js.md`, read from code at `6fda6ca4`, with the probes that settle the rows a reading cannot; the Broiler.VM change that completes JSD-0088's X2 and X6 copies it into Broiler.VM's `correspondence.md` by a dated note, quoting the cells it fills | The column written only into Broiler.VM's file. A corpus divergence field cites its record, and this repository's `semantics` divergence fields would then cite a file no merged change holds until a Broiler.VM change lands; and the rule here classifies by the rows, so it needs them in its own bundle |
| D-js-revisions | **Two corpus revisions**, as the `syntax` vectors took: revision 42 adds the area, with no difference classified, and revision 43 classifies each engine's first run on revision 42, and adds the vectors this engine's probes showed the area lacked, run on both engines before it is committed, as revision 30 added the delimiting vectors review asked for *(noted 2026-11-17, after review: this cell was widened in place after the vectors' first runs, when revision 43 was drafted; it had read "**Two corpus revisions**, as the `syntax` vectors took: revision 42 adds the area, with no difference classified, and revision 43 classifies each engine's first run on revision 42". Revision 43 did not carry the eval-code inputs of probes V029 and V030, Broiler.VM's answers for row AB1, or of probe V046, this engine's for row DE3, though the `DirectEval` operation holds each: they are next, a later corpus revision of the area, classified on both engines' runs as revision 43's additions were, before any SJL-15.2 code merges, taken as recommended under the owner's direction. Not taken: adding them to revision 43 now, whose runs on both engines, retained in bundle `sjl-15-001`, would then be of another corpus and would have to be taken again)* *(noted 2026-11-24, at corpus revision 44: the note above reads "they are next, a later corpus revision of the area"; that revision is corpus revision 44 (`conformance/vectors/REVISIONS.md`, its row 44), which adds the eval-code inputs of probes V029, V030 and V046 as `DirectEval` vectors and classifies both engines' runs on them as revision 43's additions were, Broiler.JS's at the default branch's engine after SJL-14's W2 and Broiler.VM's from a scratch pin, the runs retained in bundle `sjl-15-001`'s engine folder; it lands by this change's merge. The cell is left as written)* | One revision that adds the area and classifies it at once. The first runs would then be taken on a corpus no revision names, and decision D61 asks for the `syntax` vectors' route, which revisions 29 and 30 set |
| D-js-ops | **The area's two operations are `ScriptEvaluation` and `DirectEval`**, each with `in` the Script's source and `script` the same one-line Script: under `ScriptEvaluation` the Script's own static semantics decide the vector, and under `DirectEval` the eval code a direct `eval` in that Script evaluates does, in the context the Script gives it | The `syntax` area's `ParseScript` and `PerformEval`. The `syntax` area's tests refuse those names outside `syntax/`, since a library-level runner sees no area; and `PerformEval`'s vectors evaluate their eval code at a Script's top level only, where the `semantics` rows of eval code need a method, a constructor, a field initialiser or a class around the call |
| D-js-dispositions | **Every public type SJL-15.5 empties is obsolete with the removal version `1.0.0`**, SJL-D-25 (b), with the diagnostic identifier and the `NoWarn` decision D50 gave `Ast` and `Parser`, recorded by type in bundle `sjl-15-001`'s `dispositions.md` | Removal under SJL-D-25 (c). It needs a recorded consumer check of each type, which this repository has for none of them, and D50 chose (b) for the like types of SJL-14 |
| D-js-timing | **Class T is listed by row from the stage table**: every `semantics` row whose Broiler.JS site is `FastCompiler.EarlyErrors`, a `Visit*` method or `DirectEvalSupport`, which run as a function body is compiled or when eval runs, beside the rows the probes show raised late or not at all, each with its sites | Class T taken from the probes alone. A probe reaches only the inputs it holds, and SJL-14's class T was listed by row from the stage table in the same way (bundle `sjl-14-001`'s `corrections.md`) |

**The order of SJL-15's Broiler.JS work**, as SJL-14's decisions and the records of SJL-15.1 in both repositories set it:
- **SJL-15.1 first**, in both repositories: Broiler.VM's records and rule (merged), then these records, this
  repository's rule for SJL-15, this engine's column and deletion list, and the `semantics` vectors (decision D61),
  revisions 42 and 43; then the Broiler.VM change that carries this engine's column and JSD-0088's dated note on X2 and
  X6 (decision D-x2).
- **SJL-15.2's model merges after both repositories' SJL-15.1 rules and the `semantics` vectors.** Before this engine's
  SJL-14 cut-over (window W2), each merge of it is a dark window of SJL-14 under decision D52 ("The same holds for
  SJL-15's model stages"), judged by part B of bundle `sjl-14-001`'s rule with the switch off; a change that moves a
  switch-off outcome lands only inside a cut-over window.
- **None merges during SJL-11.5's hold**: SJL-11.5's bases are taken "while merges to this engine's and the shared
  folder's code are held" (bundle `sjl-11-001`'s README, decision D16).
- **SJL-15.5 waits on W2** (decision D41: at SJL-14 the compiler parses through the bridge, and it moves to the shared
  tree with the model at SJL-15.5).
- **Each publication of a stream version carrying the model is the owner's** (JSD-0083's decision D30), and Broiler.VM
  reads the model only from such a version, admitted by a pin commit of its own (JSD-0088's decision D-pin).

*(Noted 2026-11-18, before any SJL-15.2 code.)* The first item of this list reads "**SJL-15.1 first**, in both
repositories: Broiler.VM's records and rule (merged), then these records, this repository's rule for SJL-15, this
engine's column and deletion list, and the `semantics` vectors (decision D61), revisions 42 and 43; then the Broiler.VM
change that carries this engine's column and JSD-0088's dated note on X2 and X6 (decision D-x2)." Each step it names has
merged: these records, the rule, the column, the deletion list and revisions 42 and 43 in
Broiler-Platform/Broiler.JS#1120 (`e4e45d38`), and the Broiler.VM change of decision D-x2 in
Broiler-Platform/Broiler.VM#217 (`450c785f`, observed), which fills the column "Broiler.JS" of Broiler.VM's bundle
`sjl-15-001`'s `correspondence.md` and adds JSD-0088's dated note completing X2 and X6. Decision D-x2's cell above reads
"Until it is filed no row has a class across both engines, and no SJL-15.2 code merges"; it is filed, and JSD-0088's
note reads "No row's class is unknown". Bundle [`sjl-15-001`](../evidence/sjl-15-001/README.md)'s README names the merge
with its commit by its note of 2026-11-18, as the rule's revision of 2026-11-17 asks of each merge "before any run at
the model commit is taken". That lets no SJL-15.2 code merge by itself. Decision D-js-revisions' note of 2026-11-17 puts
the eval-code inputs of probes V029, V030 and V046 in "a later corpus revision of the area, classified on both engines'
runs as revision 43's additions were, before any SJL-15.2 code merges", and no such revision exists yet; and this list's
third item stands: "None merges during SJL-11.5's hold". The list is left as written.

*(Noted 2026-11-19, after SJL-11.5's merge.)* The note above reads "this list's third item stands: "None merges during
SJL-11.5's hold"". That hold ended with SJL-11.5's merge (Broiler-Platform/Broiler.JS#1126, `b2b226cd`). The list's
second item reads "Before this engine's SJL-14 cut-over (window W2), each merge of it is a dark window of SJL-14 under
decision D52"; under reading B4 of SJL-14, filed on 2026-11-19 in bundle
[`sjl-14-001`](../evidence/sjl-14-001/README.md)'s README, W2 merges before any SJL-15.2 model merge, so none is such a
dark window, and from W2's first base run until W2 merges nothing merges to the default branch (the owner's
answer 9, the note after "The work, in order" in SJL-14's section above). SJL-15.2's model still waits on the later
corpus revision this note names. The list is left as written.

*(Noted 2026-11-24, at corpus revision 44.)* The note above reads "SJL-15.2's model still waits on the later corpus
revision this note names." That revision is corpus revision 44 (`conformance/vectors/REVISIONS.md`, its row 44): the
eval-code inputs of probes V029, V030 and V046 as `DirectEval` vectors, classified on both engines' runs, Broiler.JS's
at the default branch's engine after W2 (Broiler-Platform/Broiler.JS#1129, with SJL-14's after records in #1130) and
Broiler.VM's at its origin/main from a scratch pin, as revision 43's additions were; the runs are in bundle
[`sjl-15-001`](../evidence/sjl-15-001/README.md)'s engine folder. It lands by this change's merge, and W2's hold has
ended with W2's merge, so SJL-15.2's model comes next, then SJL-15.5, then the evidence. The revision meets no clause and
moves no gate. The list is left as written.

**The records SJL-15.1 files here** (bundle [`docs/evidence/sjl-15-001/`](../evidence/sjl-15-001/README.md)): the rule,
[`decision-rule.md`](../evidence/sjl-15-001/decision-rule.md), committed before any change it judges, in the form of
bundle `sjl-14-001`'s; its class E and class T rows, before any run; this engine's column of the correspondence and its
probes (decision D-js-column); and the deletion list with the dispositions (decision D-js-dispositions). The `semantics`
vectors are corpus revisions 42 and 43 (`conformance/vectors/REVISIONS.md`), whose generator is
`scripts/conformance/generate_semantics_vectors.py`.
*(Noted 2026-11-17, after review, before any run of the rule.)* The rule has a revision of the same date,
[`decision-rule-revised-2026-11-17.md`](../evidence/sjl-15-001/decision-rule-revised-2026-11-17.md), written before any
run it judges: a model commit lands after Broiler-Platform/Broiler.VM#214 and after the Broiler.VM change of decision
D-x2 have merged, each observed, where the rule had it descend from them; part J is void on a lane that is not
controlled, as bundle `sjl-14-001`'s part I is, and a rule part and no gate clause, as SJL-13 read its own; the class
tables, the column and the probes, added after the commit that first carried the rule, are filed by the branch's one
merge, and the bundle's rule commit is the last commit that changes the rule or its revision. The paragraph above is left
as written.

*(Noted 2026-11-24, at SJL-15.2 stage 1, the first model commit.)* **SJL-15.2's stages.** SJL-15.2's model lands in
stages, each one model commit of bundle [`sjl-15-001`](../evidence/sjl-15-001/README.md)'s rule, judged by its part B
against its own first parent, in this order: **stage 1**, the model's foundation (the request member, the model's scopes
with their parents, each node's scope, each binding identifier's declaration and the per-function facts); **stage 2**,
the early-error channel and the function-context rows; **stage 3**, jump targets and labels; **stage 4**, private-name
environments; **stage 5a**, resolution and the function environment facts; **stage 5b**, Annex B.3.3's aliases; **stage
5c**, the caller's names in the inherited context; **stage 6**, module records; **stage 7**, named evaluation, free names
and captures; **stage 8**, the library's last model commit; and then **stage 9's records**, the listing JSD-0083's
decision D30 checks before the owner is asked about a publication, which is no model commit. No stage carries any part of
SJL-15.5: no compiler reads the model and no bridge sets the request member before it.

**This stage's decisions of 2026-11-24**, each taken as recommended under the owner's direction and recorded as the
owner's decision of 2026-11-24. Their authority is the direction quoted above, which leaves out only "the start ...;
decision D-census ...; and each publication of the shared stream (JSD-0083's decision D30)"; none below is one of those.
Every later stage's decisions are recorded when that stage lands.

| Decision | Taken | Not taken, and why |
|---|---|---|
| D-js-entry | **An opt-in request member, `ParseRequest.BuildSemanticModel`**, init-only and false by default; the model built by the same walk, held by the side tables and published as the computed `ParseResult.Model` (`Declarations?.Model`), null unless the member is on and the source is accepted | (a) A new field of `ParseResult`: the result's layout is fixed by the Frontend tests ("a result is the size it was at 0.1.0-preview.9"). (b) Revising that test: it breaks a stated invariant for an output member. (c) Building and reporting always: it moves entry points P and K and the bridge, where part B admits no difference. (d) `SemanticModel.Build(ParseResult)`: a second walk, where the milestone says the model grows from the walk and "does not start a second one". (e) A fifth `ParseSwitches` member: "a caller that states switches states all four" |
| D-js-default-off | **Off by default through SJL-15.2; each engine sets it at its own cut-over.** It is a request parameter, as `RetainTokens` and the first-error parameter are, not a switch that Broiler.VM's roadmap rule 11 ("No fallback copy, no switch and no dead path survives it") deletes; whether rule 11 reaches a library request member is unknown, and it is read so | Flipping the default at stage 8: the bridge would then read the model, which is SJL-15.5's part. Deleting the off path at the last cut-over: it removes a parameter other consumers use (an inference) |
| D-js-packable | **JSD-0083's decision D30, "a library not complete is not packable", governs publication**, which stays the owner's, asked only after stage 8 and stage 9's listing; the Frontend stays packable for the Parser package's dependency and the pristine consumer, and the shared folder's `Directory.Build.props` comment is noted | (a) The new members internal until stage 8, tested through `InternalsVisibleTo`: the new members of public enums cannot be internal, and stage 8 would flip the whole surface at once. (b) `IsPackable` false: it breaks the Parser package's dependency on the Frontend |
| D-js-o4 | **A reading of decision D52's "The same holds for SJL-15's model stages" after W2**: part B of bundle `sjl-15-001`'s rule is the identity check that replaces the window of the programme's working-order rule 1 (O-4) for each model merge, as D52's part B did before W2 | O-4 read as forbidding model merges outside a cut-over window after W2: SJL-15.2 would then land only with SJL-15.5, which the rule's model commits, each judged against its own first parent, contradict |
| D-js-instances | **The model's scopes are a superset of the tables', sharing their instances**; relations and the facts that depend on the member are the model's members; the facts that do not (function kind, async, generator, simple list, parameter expressions, mutability by kind) are on the shared types, filled on every instance, the member on or off | (a) Separate instances: two answers to one fact. (b) A new type family: duplicate names, where the area design's sketch reuses `Scope`, `Declaration` and `FunctionFacts` |
| D-js-kinds | **`ScopeKind` gains `With`, `ClassBody`, `FunctionName` and `Parameters`; eval code is `FunctionKind.EvalCode` on the program's facts, its scope still a `Script` scope; the separate variable environment is a function fact (row FF4), stage 5a's.** This is the answer to Broiler.VM's finding F7 ("SJL-15.2's records say how each is carried") | `ScopeKind.Eval` and `ScopeKind.VariableEnvironment`: a second kind for the one program scope the tables already have |
| D-js-scope-shape | **Ownership**: every class has a `ClassBody` scope, an anonymous class's too; every function a `Parameters` scope, the program's the model's first for the dynamic function's goals; a named function expression a `FunctionName` scope. **Order** by (the start of the scope's node, a model sequence counted only with the member on, at every scope's and every facts' creation, the tables' included). **`ScopeOf`** by the rule table of the home's design, section 10; entries recorded where the walk handles a child without visiting it, with a completeness test over the determinism tests' population; a node of another tree refused alike by `ScopeOf`, `DeclarationOf` and `FactsOf` | (a) The walk's own sequence: harmless gaps, but it also orders the diagnostics. (b) `ScopeOf` by the nearest visited ancestor: a node has no parent link. (c) A `FieldInitializer` scope kind: its environment declares nothing |
| D-js-parameters | **`BindingKind.Parameter` declarations in the `Parameters` scope**, one per BoundNames entry, with `FunctionFacts.ParameterNames` kept; `IsLexical`, `IsBlockFunction` and `IsIfClauseFunction` false for the three new kinds, `Parameter`, `FunctionName` and `ClassName`: none is in LexicallyDeclaredNames or VarDeclaredNames, and the kind says which binding it is | The parameters in the tables' `Function` scope: it moves the bridge's hoisting, which reads that scope's declarations |
| D-js-declaration-of | **`DeclarationOf` is single-valued**: a class declaration's name answers the outer `BindingKind.Class` declaration the tables hold, the inner `ClassName` binding being in its `ClassBody` scope's declarations with the same node; a class expression's name answers its `ClassName` declaration | `IReadOnlyList<Declaration> DeclarationsOf(SyntaxNode)`: a list for one case, where every other node has one declaration or none |
| D-js-function-kind | **A public `FunctionKind` enum, with async and generator separate booleans; static blocks and fields with an initialiser get facts of the model's own** | (a) The walk's private shape made public: it has no constructor, derived-constructor, field or static-block kind. (b) A kind per combination of async and generator: the kinds multiply, where the area design lists async and generator beside the kind |
| D-js-names | **The sketch's names mapped onto the tree's types**: `Identifier`, `BreakStatement` and `ContinueStatement`, `PrivateIdentifier` and `SyntaxDiagnostic` for the sketch's `IdentifierReference`, `BreakOrContinue`, `PrivateNameReference` and `Diagnostic`; the new types `Resolution`, `LabelTarget`, `PrivateNameBinding` and `ModuleRecord` at their stages, and `FunctionKind` now; the spelling recorded in the home's design, a new section 10 | Wrapper types over the tree's nodes: more types, and no fact added. A note in the design's section 4: it would make the design differ from the records that win over it |
| D-js-stages | **Stages 1, 2, 3, 4, 5a, 5b, 5c, 6, 7 and 8, one merge each, in that order**, part B taken for each before the next merges | Stage 3 folded with 4 and 5b with 5c, or one merge: fewer part B runs, but larger reviews and a harder attribution of a NOT MET |
| D-js-part-b-method | **Part B compared by `scripts/compliance/compare_parse_corpus.py` and the dumps' hashes; both workflows' push runs imported, ci.yml's and Shared libraries'; nothing merged to the default branch after a model commit until its ci.yml push run has concluded**, since a later push cancels it | A comparison script of part B's own: the rule names none, and part C's is already in the tree |
| D-js-adapter | **A library-level adapter for `ScriptEvaluation` and `DirectEval` from stage 2**, answering only the vectors whose row the model reports, growing stage by stage | The adapter only at stage 8: part F would then be its first run |
| D-js-part-g | **The canonical dump writes a model section when `ParseResult.Model` is present; the corpus runner's dump mode and the Native AOT probe request the model by stage 8; part G's control K2 plants a hash-ordered model side table** | A dated rule revision naming the method: part G already requires the model's side tables in what the determinism runs compare |
| D-js-part-f-checklist | **A checklist script in bundle `sjl-15-001`'s `tools/`, extending bundle `sjl-13-001`'s `contract_check.py` to the milestone's facts, committed before any run of part F; part H's two controls run** | Bundle `sjl-13-001`'s script unchanged: it covers JSD-0080's syntax rows, not the milestone's facts |
| D-js-library-runs | **Parts F, G and E taken at stage 8's commit after its merge**, each read "at stage 8's commit, the library commit if no model commit lands before the base commit", and taken again at the actual library commit if another model commit lands or the rule does not admit the early run | Waiting for SJL-15.5's merge: it delays the evidence the owner may want before being asked about a publication |

None of it is accepted: no second holder exists to review it. Every SJL-15 clause is unmet, in both repositories.

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
| **SJL-12** — the frontend contract and in-place preparation | Remove the parser's ambient state, mutable statics, `Runtime` references and parse-time desugaring in place (Modernization's MOD-M2-4 closes with it, and the contract supersedes MOD-M2-5's boundary specification; MOD-M2-3 is not part of it), record the frontend contract here, and keep the early-error catalogue and the parse corpus's home *(revised 2026-10-19, at SJL-12.1, under Broiler.VM's JSD-0080, decision D28; read "Remove the parser's ambient state, mutable statics, `Runtime` references and parse-time desugaring in place (Modernization MOD-M2-3 to MOD-M2-5 are this half), and record the frontend contract here")*. *(Noted 2026-10-25, at SJL-12.5's records: the removal is merged, SJL-12.5's items 1 to 5, with MOD-M2-4 closed, and the catalogue and the corpus are here; the after runs of bundle `sjl-12-001` are next, as [`SharedLibraries.status.md`](SharedLibraries.status.md) says.)* *(Noted 2026-10-26, at SJL-12's after evidence: the after runs are taken, and bundle `sjl-12-001` reads each part of its rule as MET; clauses 1 to 3 are met on the records, and this repository's half of shared clauses 7 and 8 is retained. The gate waits on Broiler.VM's clauses and on SJL-3's exit, as [`SharedLibraries.status.md`](SharedLibraries.status.md) says.)* | Clauses 1 to 3, and this repository's half of shared clauses 7 and 8: an architecture test with a witness enforces the parser's independence; test262 per path meets the rule; the contract record is in this plan; the early-error catalogue is retained here, and the parse corpus and this engine's baselines over it *(revised 2026-10-19, at SJL-12.1; read "Clauses 1 to 3: an architecture test with a witness enforces the parser's independence; test262 per path meets the rule; the contract record is in this plan")* |
| **SJL-4** — numerics | The area's library work and cut-over; SJL-D-15 recorded | Its Broiler.JS clauses: deletion list absent, dispositions recorded, test262 per path, vectors, corrections listed |
| **SJL-5** — text algorithms | As above | Its Broiler.JS clauses |
| **SJL-6** — Date | As above, with the provider interfaces of SJL-D-19 and Date storage migrated | Its Broiler.JS clauses, the non-UTC host run included |
| **SJL-7** — Unicode | Move off host Unicode data onto the owner's API | Its Broiler.JS clauses: no host Unicode API in the areas covered |
| **SJL-8** — time zones and calendars | Move the generators and pins here and `Temporal` off host zones and .NET calendars | Its Broiler.JS clauses, the reference datasets included |
| **SJL-11** — regular expressions | After Broiler.Regex meets every requirement and implements duplicate named groups, retire the translator, the routing and `RegExpValidator`, re-point the scanner's literal check to Broiler.Regex's validate entry, and keep `RegExpUnicodeValidator` as a forwarder to it *(revised 2026-10-25, at SJL-11.1, by Broiler.VM's JSD-0081, decisions D21 and D22; read "After Broiler.Regex meets every requirement and implements duplicate named groups, retire the translator and the validators")* | Clauses 4 to 7: translator gone, test262 `RegExp` per path, duplicate named groups still working, corrections listed; `Component.md` §3 closed |
| **SJL-10** — Temporal core | The area's library work and cut-over | Its Broiler.JS clauses, the polyfill dataset included |
| **SJL-9** — locale data and Intl | SJL-9.2: the acceptance data (Broiler.VM's Intl reference datasets copied here, and the Intl vectors), then the data home here, the CLDR generator and its pins with this engine's tables; SJL-9.3: `Broiler.ECMAScript.Intl`, stage by stage; publication with the owner's approval; then SJL-9.5, this engine's cut-over, in one merge, with host globalization only behind the optional provider (SJL-D-22 (c)) *(revised 2026-10-26, at SJL-9.1, by Broiler.VM's JSD-0082, decisions D3 and D16; read "Move the CLDR generator and pins here; host globalization only behind the optional provider (SJL-D-22 (c))")* *(Noted 2026-11-11, at SJL-9's exit reading, at review: the steps this cell names, from "SJL-9.2: the acceptance data" to "SJL-9.5, this engine's cut-over, in one merge", are taken, and SJL-9.6 has nothing left in either repository. The next action is Broiler.VM's half of the exit reading, its clauses 11 to 15, its dataset runs for clause 5 and the record over both repositories, and the owner's choice on clauses 7 and 8, which this engine's half reads not met ("SJL-9's exit reading, Broiler.JS's half, 2026-11-11" above). The cell is left as written.)* | Its Broiler.JS clauses 7 to 10, and the data-home and home clauses 1 to 6, which this repository hosts *(revised 2026-10-26, at SJL-9.1; read "Its Broiler.JS clauses")* |
| **SJL-17** — collation | Share the collation algorithm after SJL-9 | Its Broiler.JS clauses: host collation only behind the optional provider |
| **SJL-13** — the shared syntax library | SJL-13.1 is filed: the area verdict, the stage table, the classification and the rule ("The frontend's syntax half (SJL-13.1)" and "The shared syntax library (SJL-13)" above). The `syntax` vectors are corpus revisions 29 and 30, both engines' first runs classified (bundle [`sjl-13-001`](../evidence/sjl-13-001/README.md)'s README); next, their merge, after Broiler.VM's change that carries the correspondence's row P16b (decision D-asi of 2026-10-27), and Broiler.VM's pin; then SJL-13.2, `Broiler.ECMAScript.Frontend` in stages 4a to 4f, one merge each, developed against the parse corpus, the vectors and the classification; then the publication of the stream version that carries it, with the owner's approval. Broiler.VM's two steps are Broiler.VM's *(revised 2026-10-26, at SJL-13.1, by Broiler.VM's JSD-0083; read "Implement the frontend's syntax half in this repository")* *(revised 2026-10-27, at the `syntax` vectors; read "Next, the `syntax` vectors at a corpus revision; then SJL-13.2, `Broiler.ECMAScript.Frontend` in stages 4a to 4f")* *(revised 2026-10-27, decision D-asi; read "both engines' first runs classified but a difference left for the correspondence to answer (bundle `sjl-13-001`'s README); next, their merge and Broiler.VM's pin;")* *(Noted 2026-11-15, at the records after Broiler.VM's SJL-13 exit reading: this cell reads "next, their merge, after Broiler.VM's change that carries the correspondence's row P16b (decision D-asi of 2026-10-27), and Broiler.VM's pin; then SJL-13.2, `Broiler.ECMAScript.Frontend` in stages 4a to 4f, one merge each, developed against the parse corpus, the vectors and the classification; then the publication of the stream version that carries it, with the owner's approval." Each step it names is taken. The `syntax` vectors merged in Broiler-Platform/Broiler.JS#1082 (`ed494b2e`). SJL-13.2's library, stages 4a to 4f, is done: its last merge, the rule's library commit, is `d4b330d8` (Broiler-Platform/Broiler.JS#1096). Bundle [`sjl-13-001`](../evidence/sjl-13-001/README.md) reads the home's clauses 1 and 2 met at the library commit, and clause 3 met at its part D commit `9da8feb9` (Broiler-Platform/Broiler.JS#1098) under its rule's third revision, written after the library commit's part D run was seen and so weaker evidence; under the rule as revised twice, clause 3 is not met, as that bundle records (merged in Broiler-Platform/Broiler.JS#1097 as `90b5790d` and Broiler-Platform/Broiler.JS#1099 as `33d3093b`). The stream version that carries the library, `0.1.0-preview.11`, was published to nuget.org on 2026-11-13 from `e4b6b49e`, as Broiler.VM's milestone file records. In Broiler.VM (observed): its step 2 merged as `467ad7c3` (Broiler-Platform/Broiler.VM#206) and step 2's evidence, its bundle `sjl-13-002`, as `1e8f648b` (Broiler-Platform/Broiler.VM#207); its records catch-up, which observes Broiler-Platform/Broiler.JS#1100, #1102 and #1103, as `6efc0631` (Broiler-Platform/Broiler.VM#208); and SJL-13's exit reading, Broiler.VM's half, with SJL-13.5, as `4f914a3f` (Broiler-Platform/Broiler.VM#209): clauses 4 to 10 read met on the bundles, JS-2 closed on the bundles and the JSB-1 row added. Clauses 1 to 3 are this repository's, read in bundle [`sjl-13-001`](../evidence/sjl-13-001/README.md) as above. Broiler.VM's ledger states the gate in its SJL-13 row: "Every clause read met on the bundles; the gate waits on SJL-12's, SJL-3's and SJL-7's gates and on review (EX-30). None is accepted." This note declares nothing met or accepted. No step of SJL-13 is left in this repository; what remains is review and the gates SJL-13 waits on. The next action is no longer this repository's; [`SharedLibraries.status.md`](SharedLibraries.status.md)'s SJL-13 row says the same. The cell is left as written.)* | Home clauses 1 to 3: under the contract and conventions, the parse corpus and syntax tests at library level as the classification fixes them, SL9 and the Native AOT probe, each judged at the library commit by bundle [`sjl-13-001`](../evidence/sjl-13-001/README.md)'s rule *(revised 2026-10-26, at SJL-13.1; read "Home clauses 1 to 3: under the contract and conventions, the parse corpus and syntax tests at library level, SL9 and the Native AOT probe")* |
| **SJL-14** — Broiler.JS on the shared syntax library | SJL-14.0 and SJL-14.1 are filed: the start, a reading of the owner's words, and the decisions D40 to D56 and D64 (D48 the owner's own, not taken), in "Broiler.JS on the shared syntax library (SJL-14)" above; bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s rule, corrections, dispositions and inputs. Next, W1: the bridge, with the inherited context and the test-only switch off by default, judged by identity; the side-by-side runs; then W2, the cut-over, preferably after SJL-11.5, with the adapter packages and the deletions; then the evidence *(revised 2026-11-03, at SJL-14.1; read "Move the compiler and the portable compiler onto the frontend; make `Ast` and `Parser` obsolete adapter packages (SJL-D-12 (i))")* *(Noted 2026-10-26, at SJL-13.1: SJL-14 waits on SJL-13. It files its own start entry; its records can be written once SJL-13.1's have merged and SJL-13.2's stage 4c has given the tree, and its bridge merges after stage 4f. SJL-13.1's area verdict, Broiler.VM's JSD-0083, covers this engine's front end too, so SJL-14 files no verdict of its own. JSD-0083's decision D41 reads how the compiler parses with the frontend at SJL-14: through a bridge to its own tree, moving onto the shared tree at SJL-15.5. SJL-14.1's records revise this row.)* *(Noted 2026-11-03, later, on the owner's answer to D48: where this row reads "D48 the owner's own, not taken", the owner has since answered, "Please defer the measurement", choosing (i): clause 6 is kept as written, unmet until a controlled lane exists, as the note after "D48, the owner's own choice" above records.)* *(Noted 2026-11-15, at SJL-14.2's tail, before its runs are retained: this cell reads "Next, W1: the bridge, with the inherited context and the test-only switch off by default, judged by identity; the side-by-side runs; then W2, the cut-over, preferably after SJL-11.5, with the adapter packages and the deletions; then the evidence". W1 merged as Broiler-Platform/Broiler.JS#1102 (`8e5519d8`, its first parent `e2ed7fae`): the bridge, the test-only switch `JSCompilationOptions.ParseThroughBridge`, off by default, and the side-by-side tool, with what W1 departs from or leaves to W2 recorded in this plan's SJL-14 section (D43, D30, D46, D44, D41 and R35, and the rows W1a to W1c added to K10). The owner's answer to D48, "Please defer the measurement", is recorded in Broiler-Platform/Broiler.JS#1103 (`6f7284da`). Since W1, these merges have changed `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/`, Broiler-Platform/Broiler.JS#1104 (`29061d65`, the token stream behind `ParseRequest.RetainTokens`) and Broiler-Platform/Broiler.JS#1105 (`35da77ec`, whose change there renames the assurance headers), and each changed other product paths too; bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s README names them, and the other product merges since W1, with their paths. Next: part B, W1's CI run of every test project and the local identity runs with the switch off at W1 and at those library merges, each against its first parent, retained in the bundle's `bridge/`; the side-by-side run at W1, retained and read, not judged, in `side-by-side/w1/`; and the owner's optional ruling on whether those merges are dark commits, which the bundle's README puts. **W2 (SJL-14.3 to SJL-14.5) is held and not started: it waits for SJL-11.5**, decision D51's recommended order, taken under the owner's direction of 2026-10-18; the alternative not taken is W2 before SJL-11.5, with `RegExpValidator`, `DecodeBracedUnicodeEscapes` and the parenthesis patch relocated behind the provider and SJL-11.5's rule revised by a new dated rule file in `docs/evidence/sjl-11-001/` (D51). W2 merges outside the hold SJL-11.5's bases are taken under (bundle `sjl-11-001`'s README), and its package waits for a published Frontend (D30). Every clause of this engine's, 1 to 6, is unmet, and none of it is accepted. [`SharedLibraries.status.md`](SharedLibraries.status.md)'s SJL-14 row says the same. The cell is left as written.)* *(Noted 2026-11-15, later, when SJL-14.2's tail is retained: the note above reads "Next: part B, W1's CI run of every test project and the local identity runs with the switch off at W1 and at those library merges, each against its first parent, retained in the bundle's `bridge/`; the side-by-side run at W1, retained and read, not judged, in `side-by-side/w1/`; and the owner's optional ruling on whether those merges are dark commits, which the bundle's README puts." Part B and the side-by-side run at W1 are taken. Bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md), "Part B, judged on 2026-11-15", judges part B MET at the commits taken, W1 against `e2ed7fae`, #1104 against `6f7284da` and #1105 against `29061d65`, on the reading of decision D52 taken under the owner's direction of 2026-10-18, that each merge to `Frontend*/` after W1 is a dark commit whatever else it changes, with the rule's void sentence read narrowly; it is read again at each later dark commit up to the base commit, which does not exist yet. On the rule's words, the first alternative not taken, only W1 is judged, and part B is MET at W1, read again at any later merge that changes only `Frontend*/`; on D52's reading with the void sentence as written, the second, part B is void. Its runs are in `bridge/`, with W1's CI run. Part B is a part of the rule and no clause. "The side-by-side run at W1, read on 2026-11-15" lists the differences for part C at the base commit, and judges none; part C's other runs at W1, which the rule also asks for, are still to be taken. Next: the owner's optional ruling among those readings; W2 stays held for SJL-11.5, as the note above says. Every clause of this engine's, 1 to 6, is unmet, and none of it is accepted. [`SharedLibraries.status.md`](SharedLibraries.status.md)'s SJL-14 row says the same. The cell is left as written.)* *(Noted 2026-11-18, at SJL-D-23's trigger (a): where the note of 2026-11-03 above reads "clause 6 is kept as written, unmet until a controlled lane exists", a controlled lane exists, `win-x64-r7-5800x`, and the owner chose "Take it on the lane". Part I of bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s rule is to be taken as written on that lane. Its arms are the base commit and W2, so it is not taken before W2 merges, which stays held for SJL-11.5. Until then clause 6 is unmet, as the note after "D48, the owner's own choice" in this plan's SJL-14 section records. This is the owner's own dated choice, not one taken under the owner's direction of 2026-10-18. The cell is left as written.)* *(Noted 2026-11-19, after SJL-11.5's merge: the notes above read "W2 stays held for SJL-11.5, as the note above says" and "which stays held for SJL-11.5". SJL-11.5 merged on 2026-11-19 as Broiler-Platform/Broiler.JS#1126 (`b2b226cd`), and the hold of decision D16 ended with it; W2 is no longer held for SJL-11.5. Bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s README, "Before W2's base, read on 2026-11-19", records the owner's answer 9 of 2026-11-19, "Allow the W2 hold" (W2's hold begins at the first base run and ends with W2's merge, nothing merging to the default branch meanwhile, part A's base and after runs taken by dispatches of `test262-evidence.yml`), the readings B1, B2, B4, B9 and B10 taken under the owner's direction of 2026-10-18 with their alternatives, part G's commits, and the published `0.1.0-preview.12`, as the note after "The work, in order" in this plan's SJL-14 section does. Next: W2 written, part C's runs at W1, W2's base runs under the hold, W2's merge and its after runs; part I on the lane after W2 merges. Every clause of this engine's, 1 to 6, is unmet, and none of it is accepted. The cell is left as written.)* | Clauses 1 to 6, the Native AOT sample regenerated and run included |
| **SJL-15** — shared static semantics | SJL-15.0 and SJL-15.1 are filed in this repository: the start, a reading of the owner's words; the area verdict's entry, "Static semantics (SJL-15.1)" in "Area verdicts" above, Broiler.VM's JSD-0088 being its other half; and the decisions that bind this repository, with this repository's own decisions of 2026-11-17, in "Shared static semantics (SJL-15)" above; bundle [`sjl-15-001`](../evidence/sjl-15-001/README.md)'s rule, this engine's column of the correspondence with the probes of both engines, the classes E and T and the corrections by row, and SJL-15.5's deletion list with the dispositions; and the `semantics` vectors, corpus revisions 42 and 43 (decision D61). Next: their review and merge; the Broiler.VM change that carries this engine's column and JSD-0088's dated note completing X2 and X6 (decision D-x2); then SJL-15.2's model, each merge before W2 a dark window of SJL-14 (decision D52) and none during SJL-11.5's hold; then SJL-15.5, after W2 (decision D41); then the evidence *(revised 2026-11-17, at SJL-15.1; read "Implement the semantic model here; delete the scattered early errors and scope analysis")* *(Noted 2026-10-26, at SJL-13.1: SJL-15 waits on SJL-13 and SJL-14. Its model grows from the declarations walk SJL-13.2 begins in `Broiler.ECMAScript.Semantics`, which reports the parse-time early errors and exposes each scope's declarations as side tables, by JSD-0083's decision D10.)* *(Noted 2026-11-18, before any SJL-15.2 code: this cell reads "Next: their review and merge; the Broiler.VM change that carries this engine's column and JSD-0088's dated note completing X2 and X6 (decision D-x2); then SJL-15.2's model, each merge before W2 a dark window of SJL-14 (decision D52) and none during SJL-11.5's hold; then SJL-15.5, after W2 (decision D41); then the evidence". The merge is taken, with no review, no second holder existing: Broiler-Platform/Broiler.JS#1120 (`e4e45d38`). The Broiler.VM change of decision D-x2 merged as Broiler-Platform/Broiler.VM#217 (`450c785f`, observed), and bundle [`sjl-15-001`](../evidence/sjl-15-001/README.md)'s README names it with its commit, as the rule's revision of 2026-11-17 asks before any run at a model commit; what that merge does and does not unblock is in the note after "The order of SJL-15's Broiler.JS work" above. Next: the later corpus revision with the eval-code inputs of probes V029, V030 and V046, classified on both engines' runs, which decision D-js-revisions' note of 2026-11-17 puts "before any SJL-15.2 code merges"; then SJL-15.2's model, as this cell says, none of it during SJL-11.5's hold; then SJL-15.5, after W2; then the evidence. Every clause is unmet, and none of it is accepted. [`SharedLibraries.status.md`](SharedLibraries.status.md)'s SJL-15 row records the same merges and next steps. The cell is left as written.)* *(Noted 2026-11-19, after SJL-11.5's merge: the note above reads "then SJL-15.2's model, as this cell says, none of it during SJL-11.5's hold; then SJL-15.5, after W2". That hold ended with SJL-11.5's merge (`b2b226cd`). Under SJL-14's reading B4 of 2026-11-19 (bundle [`sjl-14-001`](../evidence/sjl-14-001/README.md)'s README), W2 merges before any SJL-15.2 model merge, and nothing merges during W2's hold (the owner's answer 9); so W2 and the later corpus revision both come before SJL-15.2's model, then SJL-15.5, then the evidence. The cell is left as written.)* *(Noted 2026-11-24, at corpus revision 44: the note above reads "so W2 and the later corpus revision both come before SJL-15.2's model". W2 merged as Broiler-Platform/Broiler.JS#1129 (`83a9bd82`), its hold ending with it, and the later corpus revision is corpus revision 44 ([`conformance/vectors/REVISIONS.md`](../../conformance/vectors/REVISIONS.md), its row 44), the eval-code inputs of probes V029, V030 and V046 classified on both engines' runs, which lands by this change's merge; then SJL-15.2's model, then SJL-15.5, then the evidence. Every clause is unmet, and none of it is accepted. The cell is left as written.)* | Home clauses 1 to 3 and Broiler.JS clauses 10 and 11 |
| **SJL-16** — close-out | Sweep the deletion lists, retire the reference arms, confirm SJL-D-18 *(Noted 2026-10-26, at SJL-13.1: SJL-16 waits on every milestone from SJL-0 to SJL-15, and SJL-17, being met or `Refused`, as its milestone file says.)* | The close-out's sweep in this repository |

The state of each item is one line in [`SharedLibraries.status.md`](SharedLibraries.status.md).
Following this directory's convention, no changing count is written here: a test262 total, a vector
count or a measurement belongs in a bundle under `docs/evidence/`, and the status record links it.
