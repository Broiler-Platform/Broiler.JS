# Broiler.JS performance and benchmark roadmap

The historical optimization catalogue and performance-evidence crosswalk for JavaScript
execution speed. [`Modernization.md`](Modernization.md) is the current cross-track sequencing
authority; this document preserves the detailed IL campaign, benchmark rationale, and phase
verdicts that it coordinates. This catalogue merges two earlier documents that were each
correct about half the picture:

| Merged from | What it contributed | Where it is now |
|---|---|---|
| The engine-internal campaign | P0–P3, phases A–F, their measurements, and the open items each deliberately left behind | [`Archive.md`](Archive.md), part one |
| The Octane roadmap | The forward plan driven by Octane 2.0 — phases 0–5, ordered by where the suite says the losses are | [`Archive.md`](Archive.md), part two |
| `tests/octane/benchmarks.md` | What each benchmark exercises, and the ranked blockers B1–B8 that connect the two | Still live, in the aggregate repository — it is a reference, not a plan |

**It lives in `Broiler.JS`, beside the engine it plans, and that reverses an earlier
decision this paragraph used to defend.** The argument for the parent repository was
that the plan spans both — the harness, the suite manifest and the workflow are
main-repo (`tests/octane/`, `scripts/`,
`.github/workflows/octane-benchmarks.yml`), the engine is a submodule, and a
document inside `Broiler.JS` cannot *link* outward to the harness. That is still
true and it turned out to be the smaller cost. **Every item in this plan changes
`Broiler.JS` source**, so a plan held one repository away from the code it directs
went stale in the way the provenance bullet below documents at length: a pointer
written into prose is wrong by default, and a plan that cannot be revised in the
same commit as the change it describes is the same failure at a larger scale. **Nine
outward links became unlinked `code spans`** — the path convention below says where —
against a plan that now moves with its engine.

- **Owner assemblies:** `Broiler.JavaScript.Runtime`, `.Engine`, `.BuiltIns`,
  `.Storage`, `.Compiler`, `.Parser`, plus `Broiler.Regex` for phase 5.
- **Acceptance protocol:** [`Measurement.md`](Measurement.md) governs what may be *claimed*;
  [`Modernization.md`](Modernization.md) MOD-M1 defines the upgrades still required before a
  performance milestone is accepted. Historic smoke runs and configured spread bands remain
  useful evidence, but they are not substitutes for calibrated A/A stability, exact-row
  comparison, reproducible build identity, resource limits, and semantic-owner conformance.
  **Nothing in this document closes merely on the numbers it quotes.**
- **Provenance:** the aggregate repository currently pins `Broiler.JS` at **`7fb17553`**,
  read 2026-08-22 with `git submodule status`; the long narrative below is the campaign's
  2026-08-07 provenance history, not a live patch ledger. **Read the current pointer with the
  command; never from this line.** Earlier readings found this sentence stale — `db4451c2`,
  `07adeb44`,
  `2ebc0c3c`, `71dda1b7`, `9bf9639b`, `61c8cc65`, `cca39b4d`, `14fa4f10`, `8308df51` and
  `e5dc2610` before it —
  which is a rate rather than an anecdote: **a pointer written into prose is wrong by default**, so
  the sentence to write next to any pointer is the command that reads it. **The ninth reading is
  the one that moved this document into the submodule** (see the paragraph above): a plan that has
  to name its own engine's commit, from outside that engine, is a plan that cannot be corrected in
  the commit that invalidates it. Living beside the engine does not make the pointer sentence
  correct — the parent still pins a commit and this file still cannot see which — but it removes
  every *other* reason the two drift. The sibling submodules
  are `Broiler.HTML` **`b829d1ff`**, `Broiler.CSS` **`f960f943`**, `Broiler.Graphics`
  **`e1ac7289`** and `Broiler.DOM` **`358cf058`** — three of the four had also moved since
  anything here described them. It is why §4.1's and §3.4's figures carry the commit they
  were taken at rather than "the pin".
- **Current patch handoff:** the aggregate tree has no `patches/` directory and no active
  `Broiler.JS` patch ledger. The paragraphs below retain the historical `0102`–`0115`
  handoff trail only so the recorded measurements remain traceable. `0115` landed before
  `db4451c2`; it is not pending work and must not be scheduled from this narrative.
- **The ten that were open at the last reading have landed, and checking that is what retired
  them.** `0103`–`0112` were recorded here as pushed-and-blocked against pin `8308df51`; the pin
  has since moved to `e5dc2610`, and **all ten subjects are present in the submodule log**, so the
  patch files and their `patches/README.md` rows are deleted per that file's own rule. *The check
  is the one the paragraph below already prescribes — match against the submodule log, not against
  `patches/` — and it matters that it is not the obvious one:* the added files of a
  sequentially-applied stack exist in the pinned tree whether the whole stack landed or only its
  first patch, so file existence cannot retire anything.
- **Before those two, everything this document measured was in the pin.**
  The twelve open at the last reading — `0102`–`0113`: item 1-1's remaining half in five of them,
  plus the widened census corpus, item 4-2's arithmetic half, item 4-5's four, and item 3-1's
  read/write ratio — have been applied, pushed and the pointer bumped. In patch order they are
  **`861daccc`, `18524c34`, `db81b5b2`, `d2711e1b`, `a49d8ba5`, `5ea934fb`, `a06ef9eb`, `046a55fc`,
  `2f8ed84f`, `19b7ac5b`, `ddb20e7d`, `8308df51`** — twelve subjects matched against the twelve
  commits `14fa4f10..HEAD` contains, in one unbroken run with nothing else in it. **That is a
  weaker check than the patch-by-patch `format-patch` diff earlier rounds recorded, and the reason
  is worth keeping:** a patch file is deleted once it lands, so the diff is available only while the
  handoff is still open. Verify a landed claim against the submodule log, not against `patches/`.
  **So every figure in this document describes the pinned pointer directly**, rather than a local
  build plus a patch series applied in order, which is what a succession of sections used to have
  to say. Every commit cited for a measurement anywhere below — `a6f101cc`, `685026c0`, `cdb2fd41`,
  `9bf9639b`, `61c8cc65`, `07adeb44`, `cca39b4d`, `14fa4f10`, `2ebc0c3c`, `71dda1b7`, `7ef80c03`,
  `8228b0da`, `45f4f679` — is an **ancestor** of the pin (`merge-base --is-ancestor`), so nothing
  recorded against any of them is invalidated.
- **A patch number is a citation of this document's history, not a stable file name.** `patches/`
  is one flat namespace across every submodule, so two branches numbering from the same high-water
  mark collide whenever both are open — the ordinary case rather than an unlucky one. It has just
  happened again: **`0102` is now a `Broiler.CSS` patch**, reusing the number item 1-1's
  capture-free population census held one reading ago. Sections below cite `0102`–`0113` as the
  units of work they were; the durable reference for each is the commit above.
- **Measurement dates.** §4.1's figures and §3.4's test262 run were taken at `cdb2fd41` and have
  not been repeated — `685026c0` also carries a string-allocation fix (#936) and item 0-9's probe
  corpus (`aa2b1562`, #938). Octane code sites verified at `45f4f679`. **Phase 2's own
  measurements — §0 and each 2-x section — were taken at exactly the tree `a6f101cc` now is.**
  Item rows are checked against the tree rather than inherited from the prose above them; doing
  that is what caught that **item 1-2's acceptance criterion already passed before any work**
  (phase 1).

> **Path convention.** This document and its parts live in the `Broiler.JS` submodule, so
> **every path is written relative to the `Broiler.JS` root** — `eng/performance/phase0.json`,
> `Broiler.Regex/docs/roadmap.md`. Source *files* named in the item tables
> (`Runtime/ObjectShape.cs`, `BuiltIns/Function/JSFunction.cs`, …) are relative to
> `Broiler.JavaScript.*`, as they were in the original.
>
> **Paths into the aggregate repository cannot be linked and are written as bare code
> spans, relative to the aggregate root** — `tests/octane/`, `scripts/octane-suites.json`,
> `patches/README.md`, `.github/workflows/octane-benchmarks.yml`. A submodule has no
> relative path to its parent, so a link would be a lie rather than a broken link. There
> are nine of them, in this file and in `Measurement.md`.

---

---

## How this directory is organised

**This file is the performance-campaign umbrella and historical crosswalk**: the scope the
campaign was merged from, its measured history, the optimization catalogue, and Appendix
B's traceability. [`Modernization.md`](Modernization.md) is the current dependency and
execution authority for modernization work; the individual phase plans remain authoritative
for their items and exit gates. Where an older table here conflicts with either, follow
`Modernization.md` and the phase plan and correct this crosswalk in the same change.

> **This directory splits every plan from its evidence.** `X.md` is the plan — what each
> item is, what to do next, how big it is, and the gate it closes against. `X.status.md` is
> the record — what was built, what was measured, what was refuted, and every number. A
> claim in a plan document is a one-liner with a link; the argument behind it is always in
> the status document.

| | Plan | Evidence |
|---|---|---|
| **The campaign** | this file | [`Roadmap.status.md`](Roadmap.status.md) — §0 in full, plus §4's dated cross-phase evidence snapshot |
| **Phase 0** — establish the baseline | [`Phase-0.md`](Phase-0.md) | [`Phase-0.status.md`](Phase-0.status.md) |
| **Phase 1** — the front end | [`Phase-1.md`](Phase-1.md) | [`Phase-1.status.md`](Phase-1.status.md) |
| **Phase 2** — the call and property paths | [`Phase-2.md`](Phase-2.md) | [`Phase-2.status.md`](Phase-2.status.md) |
| **Phase 3** — value representation | [`Phase-3.md`](Phase-3.md) | [`Phase-3.status.md`](Phase-3.status.md) |
| **Phase 4** — speculation | [`Phase-4.md`](Phase-4.md) | [`Phase-4.status.md`](Phase-4.status.md) |
| **Phase 5** — RegExp | [`Phase-5.md`](Phase-5.md) | [`Phase-5.status.md`](Phase-5.status.md) |
| | | |
| **Phase 6** — JavaScript profile 1.0, correctness | [`Phase-6.md`](Phase-6.md) | [`Phase-6.status.md`](Phase-6.status.md) |
| **Phase 7** — JavaScript profile 2.0, shippability | [`Phase-7.md`](Phase-7.md) | [`Phase-7.status.md`](Phase-7.status.md) |
| **Phase 8** — JavaScript profile 3.0, measured optimization and persistence | [`Phase-8.md`](Phase-8.md) | [`Phase-8.status.md`](Phase-8.status.md) |
| **Phase 9** — JavaScript profile 4.0, optional IL/bytecode adaptivity | [`Phase-9.md`](Phase-9.md) | [`Phase-9.status.md`](Phase-9.status.md) |
| **The assembly restructure** — track two's precondition | [`Assemblies.md`](Assemblies.md) | [`Assemblies.status.md`](Assemblies.status.md) |
| **The `ExpressionCompiler` split** — its first executable piece, analyzed in full | [`AssemblySplit.md`](AssemblySplit.md) | [`AssemblySplit.status.md`](AssemblySplit.status.md) |
| **Concurrency and compile-ahead** — ownership, bounded work and Workers | [`Concurrency.md`](Concurrency.md) | [`Concurrency.status.md`](Concurrency.status.md) |
| **Modernization orchestration** — dependencies and terminal decisions across all tracks | [`Modernization.md`](Modernization.md) | MOD-M0-1 will create the single machine-readable state source at `eng/performance/roadmap-items.json`; it does not exist yet |

**Phases 0–5 are track one**, the IL path, and contain the campaign's measured history.
**Phases 6–9 are [track two](#track-two--the-vm-tier-phases-69)**, the JavaScript built-in
profile on Broiler.VM. No production JavaScript-profile phase has started. The generic VM
host/catalog and WebAssembly built-in are owned by `Broiler.VM/docs/roadmap.md`; MOD-M9 selects
only the JavaScript `execution-only`, `narrow-runtime-compiler`, or
`general-runtime-compiler` composition and replaces the older stand-alone 6-0 scoping exercise.

**Three documents are neither**, because they are not phases of this campaign:

| | |
|---|---|
| [`Measurement.md`](Measurement.md) | **The gate, and §3 of this plan.** What may be claimed, the protocol, the conformance gates, §3.5's standing lessons, and Appendix A's command lines. Nothing here closes without it. |
| [`Component.md`](Component.md) | The engine's non-performance roadmap — test262, host modes, RegExp backend adoption, packaging. |
| [`Archive.md`](Archive.md) | Both superseded plans, kept for their measurements and **not back-ported**. |

**Read in this order if you are new:** §1 and §2 below, then `Measurement.md` §3 and
especially §3.5 — the standing lessons are what stop a new probe repeating an old mistake —
then the phase you are working on, plan first.

---

## 0. Status — one line per phase

**Last updated 2026-08-22.** One line per phase. **Every one of these is a digest of a
paragraph** — the long form, with the measurements each verdict rests on, is
[`Roadmap.status.md`](Roadmap.status.md), and the item's own section is where
anything may actually be checked. *Nothing here is closed*; §3 governs what may be claimed.
Under the current gate, hosted/container timings are smoke or prioritization evidence even
when historical rows call their spread a satisfied “noise band.”

| Phase | Verdict | Detail |
|---|---|---|
| **0** — evidence | The historical smoke workflow reports 17/17 scores and a three-repetition spread, but **the modernization acceptance baseline is not yet accepted**: 0-7/0-8 and MOD-M1's A/A calibration, exact-row comparator, effective-settings attestation, reproducible build identity, resource metrics, and conformance bundle remain open | [phase 0](Phase-0.md), [measurement gate](Measurement.md) |
| **1** — compile-time | **1-2 ✅, 1-4 ✅ (3.04× on Mandreel), 1-1's emission half ✅ (CodeLoad 1.099×).** 1-1's deferral mechanism is still open and is the phase's remaining L; it is no longer blocked on an unpriced precondition, and the measured corpus observed 0 missed sites on 5 157 checked. Semantic fixtures and conformance—not that finite census—remain the soundness gate | [phase 1](Phase-1.md) (item 1-1 included) |
| **2** — property access | **Every item landed or closed.** The exit criterion splits and stays split on four measurements: **Richards passes at 145×, DeltaBlue fails at 512×** | [phase 2](Phase-2.md) |
| **3** — arithmetic | **3-0, 3-3, 3-5, 3-6, 3-7 ✅; 3-8 counted and refused as written.** The dual-representation numeric local is **refuted on four populations running**, each measured before it was built. What is left is an XL bidding against 2.6%, and nothing here should be started on a box count again | [phase 3](Phase-3.md), [items 3-1 and 3-8](Phase-3.md) |
| **4** — tiering | **4-1, 4-2a, 4-2b, 4-3a, 4-3b ✅; 4-2c refuted at 0.119%.** The phase's largest measured target is **4-5 at 6.50% of the corpus**, of which 92% of the bookkeeping is Annex B `caller`/`arguments`; its named fix was priced at 0.20% and refused, and the 1.46% that is left is gated on a soundness question nobody has answered | [phase 4](Phase-4.md) |
| **5** — regex | **Every item this phase named is closed, item 2 included.** The gate overturned the phase once (`Matcher.cs` is not on the Octane path) and item 2 overturned it again: **the matcher is 4.6–6.5% of what `re.test` costs**, so nothing aimed at matching can move this suite. The remaining target is the **fixed ~2.4 µs and 2 431 B every regex call pays** | [phase 5](Phase-5.md) |
| **6** — JavaScript VM correctness | ❌ **not started.** MOD-M9 must select `execution-only`, `narrow-runtime-compiler`, or `general-runtime-compiler`. The landed numeric portable seed and source compiler are evidence for an execution-only island, not the JavaScript built-in or a production runtime-compiler closure; VM core or WebAssembly work cannot close this row | [phase 6](Phase-6.md), [MOD-M9 composition](Modernization.md#mod-m9--select-the-javascript-built-ins-deploymentcompiler-composition) |
| **7** — VM baseline performance | ❌ **not started.** Requires Phase 6's approved JavaScript capability manifest and MOD-M1's uninstrumented baseline. Runtime structures may be reused only through explicit function/realm ownership; process-global mutable caches are not inherited “unchanged” | [phase 7](Phase-7.md) |
| **8** — persistence and adaptive interpretation | ❌ **not started.** Versioned verified persistence, feedback, quickening, superinstructions and dispatch work are separate, measurement-gated items; each requires a current workload population and concurrency-safe ownership | [phase 8](Phase-8.md) |
| **9** — optional tiering/deoptimization | ❌ **not started.** An interpreter frame alone does not make deoptimization possible. Tier-up, explicit `DeoptState`, invalidation, reconstruction, OSR and threshold policy require separate feasibility and correctness gates | [phase 9](Phase-9.md) |

---

## 1. What the merge produces that neither document had

Two findings come out of putting these side by side. Both change the plan, so they
lead.

### 1.1 The engine roadmap's scope statement is wrong, and Octane is why

[`performance-roadmap.md` §9](Archive.md) declares two
areas out of scope:

> - **Parsing and compilation.** Fresh-context startup is 1.20 ms and
>   `script:evaluation` runs in 37 ms; neither showed up as a bottleneck.
> - **A real JIT / tiered compilation.** […] Everything above is achievable in the
>   current architecture.

Both exclusions are **superseded here**, and the reason is not a change of opinion —
it is that the probe corpus could not see the effect:

- **Front end.** The probes are one-liners in a fresh `JSContext`. Octane runs 15
  large real programs, and when this was written the two worst scores in the entire suite
  were **MandreelLatency at 4646×** and **CodeLoad at 371×**. `script:evaluation` at 37 ms was
  a true measurement of a corpus small enough that eager compilation is free. It is not
  free on jQuery, on the TypeScript compiler, or on a 152,948-line generated function.
  **The front end is phase 1.**
  > **Correction, from running both suites (phase 1).** This bullet used to call those two
  > scores "the two that measure nothing but the front end", and used them as the phase's
  > success metric. Measured, **CodeLoad is ~27% compilation and MandreelLatency is 0%** —
  > Octane compiles `mandreel.js` at script load and starts its timer afterwards. The
  > argument above is unaffected, because it rests on the *probe corpus being too small to
  > see eager compilation*, which is still true and is why phase 1 exists. What it cost was
  > the phase's target list: see Phase 1's header.
  >
  > **The two scores it named are no longer the two worst** (§4.2, 2026-08-03): CodeLoad is
  > 228× and three suites are now behind it — DeltaBlue 460×, Mandreel 290×, RayTrace 256×.
  > MandreelLatency at 4 584× is still the tail by an order of magnitude. Neither correction
  > touches the argument, which was never about *which* scores were worst.
- **Speculation.** Engine §9 scoped itself to "achievable in the current
  architecture", which was an honest boundary for a bookkeeping-removal campaign. The
  remaining ~100× is not achievable inside it. **Speculation is phase 4**, and the
  scaffolding for it is already built and tested.

This is the general hazard the merge exists to close: **an in-process probe answers
"what does this operation cost", and a benchmark suite answers "what does this
program spend its time on".** Neither substitutes for the other, and the excluding
section was written with only the first.

*(Section numbers prefixed **engine** or **Octane** refer to a source document; bare
`§n.n` refers to this one. Phases are always named, never numbered as sections.)*

### 1.2 Both roadmaps exposed the same missing gate; smoke exists, acceptance does not

The source roadmaps described the same evidence gap from two directions. Their concrete
smoke/tooling debts have since moved: 0-6 regenerated the workflow at the then-current pin,
0-9 put the probes in a permanent benchmark project, and 0-7 produced a developer-workstation
run. None is modern acceptance evidence: 0-7 lacks a reproducible source/control bundle, no
RID has a controlled MOD-M1 lane, and the comparator still lacks exact-row/all-repetition/resource
failure behavior and effective-setting attestation. The current gap is therefore **0-7/0-8,
0-11 validation, and MOD-M1**, not an unrun workflow or missing probe home.

Phase 0 now contains real measurement-engineering work. It blocks performance/resource
closure, while deterministic correctness and architecture evidence keep their independent
gates.

---

## 2. Metrics

Track five numbers, in two families. Reporting one without the other is how the two
source documents ended up disagreeing.

### 2.1 The suite view — three numbers per Octane run

| Metric | Superseded run (2026-07-31) | **Committed run at the pin (2026-08-07)** | Target |
|---|---|---|---|
| **Scores reported** out of 17 | 12 / 17 | **17 / 17** — 15 of 15 suites `ok` | **17 / 17** |
| **Geomean** over all 17 scores | 245 over the 12 that completed | **372** over all 17 | — |
| **Spread** = worst ÷ best, as ×-slower-than-Chromium | 4 646 / 45 ≈ **103×** | 4 375 / 31.7 ≈ **138×** | **< 5×** |
| **Against Jint**, geomean of per-benchmark ratios | not measured | **0.644×** | > 1 |
| **Observed repeatability smoke**, scores outside the provisional 7.5% harness guard | not measured | **1 / 17** — median three-sample spread 3.0% | — |

The right column is the workflow's own run, so its Chromium and Jint columns were measured on
the same machine at the same time and the ratios are directly comparable as descriptive
cross-engine results. Three repetitions per suite add observed repeatability smoke. They do
not decide whether a future change is claimable, estimate candidate/control uncertainty, or
make a later hosted-runner pair attributable. Acceptance requires one separately controlled,
identity-attested candidate/control session under MOD-M1.

**The spread went up, and that is not a regression.** It is 138× against the superseded run's
103× because the superseded run had *five suites scoring nothing at all*: a suite that fails
contributes no ratio, and the four of those five that now score (Crypto, PdfJS, zlib,
Typescript) landed across the middle of the range while the best axis improved from 45× to
31.7×. Spread is a ratio of two suites, so widening the denominator widens it. Compare the
column honestly or not at all — which is the reason this table names both dates rather than
saying "before" and "after".

**Spread is the organizing metric.** Because the suite total is a geometric mean,
flattening the curve and raising the total are the same work: moving MandreelLatency
from 14.5 to 1000 is worth more than tripling every score already above 300. A run
where every suite is uniformly 150× off is a far healthier engine than today's at a
similar geomean, because no single subsystem is pathological.

All three are emitted by `run-octane.mjs` into `results/<platform>/comparison.md` and
`comparison.json`, so the trend comes out of the run rather than being reconstructed
by hand.

### 2.2 The engine view — time *and* allocation per hot path

Wall clock alone hid the largest result of the completed campaign. The shadow-stack
change (§4.1) took an argument-less call from 80 B to **0 B** with throughput
*unchanged*; the pooled predecessor was banked as an allocation win at no speedup.
Conversely P2-2's item 3 looked like an allocation change and turned out to be worth
9–99% of wall clock once its eligibility gate was widened.

**Report time, allocation, and working set together.** This is already the rule in
`Roadmap.md`; it is restated because the campaign twice found the interesting
half in the column it was not looking at.

### 2.3 Which number answers which question

| Question | Instrument |
|---|---|
| Did this operation get cheaper? | In-process probes, Appendix A |
| What does an object or an element cost in bytes? | `--object-alloc`, `--element-alloc` |
| What does a local, a binding or a parameter cost? | `--local-alloc`, which reports the compiler's own eligibility counts beside the bytes |
| What does a regex cost, and which engine ran it? | `--regex-profile` |
| How much of a compile is function bodies — i.e. what can 1-1 win? | `--compile-profile <octane-dir>` |
| Which of parse / tree construction / IL emission is the cost, and is it linear? | `--compile-scaling` |
| Did the cache actually start hitting? | `PropertyOptimizationDiagnostics.Snapshot()` |
| Did real programs get faster? | Octane, ≥3 repetitions, median + spread |
| Is the engine still correct? | test262 over the pinned manifests |
| May we publish the number? | `Roadmap.md` gates only — none of the above |

---

> **§3 is [`Measurement.md`](Measurement.md).** What may be claimed, how to run the
> harness and the probes, the conformance gates, and §3.5's standing lessons all live
> there, together with the acceptance gate that governs them and Appendix A's command
> lines. The numbering is unchanged, so an existing reference to §3.1 or §3.5 still
> resolves.

---

> **§3 is [`Measurement.md`](Measurement.md).** What may be claimed, how to run the harness
> and the probes, the conformance gates, and §3.5's standing lessons all live there,
> together with the acceptance gate that governs them and Appendix A's command lines. The
> numbering is unchanged, so an existing reference to §3.1 or §3.5 still resolves.
>
> **§4 is [`Roadmap.status.md`](Roadmap.status.md)** — where the engine stood at the recorded
> snapshot is evidence, not a current sequencing authority.

---

## Track two — the VM tier (phases 6–9)

**Phases 0–5 improve the IL path. Phases 6–9 now describe the JavaScript built-in profile
for Broiler.VM and its later, separately justified JavaScript/IL adaptive tier.** They do not
own the generic VM core, WebAssembly execution, or future built-in registration. No production
JavaScript-profile phase has started. Modernization milestone MOD-M9 selects an
`execution-only`, `narrow-runtime-compiler`, or `general-runtime-compiler` JavaScript
composition and closes/replaces the older Phase 6 item 6-0 study; it cannot cancel Broiler.VM
or WebAssembly.

### The capability gap

**Broiler.JS today has no general JavaScript execution path on a platform that forbids
`System.Reflection.Emit`.** The repository now contains a small `Broiler.JavaScript.Portable`
runtime, a separate `Broiler.JavaScript.Portable.Compiler`, and a Native AOT sample. That is
valuable implementation evidence, but the current portable language is a numeric island and
the sample exercises precompiled bytecode. It proves neither a general JavaScript runtime nor
that the parser/compiler closure can compile source inside a Native AOT process.

**The original impossible dependency edge has been removed.** The expression model now lives
in the Emit-free `Broiler.JavaScript.Expressions` assembly and the IL emitter remains in
`Broiler.JavaScript.ExpressionCompiler`; `Parser` no longer has to reference the emitter.
[`AssemblySplit.status.md`](AssemblySplit.status.md) records that landed state. Validation and
the larger production boundary are still open: [`Assemblies.md`](Assemblies.md) must prove an
acyclic graph, a backend-neutral semantic contract, an honest IL boundary, explicit backend
registration, and separate execution-only versus runtime-compiler AOT closures.

| Phase | Catalogue stage | Delivers | Blocked on |
|---|---|---|---|
| [**6**](Phase-6.md) | JavaScript correctness and deployment | A shared production JavaScript semantic IR, JavaScript value/frame ABI, JavaScript profile format/verifier, vertical interpreter slices, and the capability manifest approved by MOD-M9 on the Broiler.VM foundation | Broiler.VM core/profile contracts, the MOD-M9 composition decision, and the applicable MOD-M2/MOD-M3/MOD-M4 graph and packaging gates |
| [**7**](Phase-7.md) | uninstrumented baseline performance | A shippability decision based on the current product corpus, with function/realm-owned inline-cache state and explicit slow paths | accepted Phase 6 scope, MOD-M1 baseline and MOD-M6 ownership where state is shared or concurrent |
| [**8**](Phase-8.md) | persistence and measured adaptive interpretation | Independently gated bytecode persistence, feedback, quickening, superinstructions and dispatch improvements | stable verified format, Phase 7 attribution, MOD-M1, and MOD-M6 for shared/adaptive state |
| [**9**](Phase-9.md) | optional JavaScript tiering/deoptimization | Independently gated JavaScript function promotion, explicit deopt-state/reconstruction, and OSR slices whose product value justifies their complexity; none is a WebAssembly or generic-VM gate | a runtime-compiler JavaScript composition, a dynamic-code-capable host that requires adaptivity, stable Phase 6 identities/ABI, accepted Phase 7 baseline/shippability evidence, MOD-M1, and the gate of the selected branch |

### Four things that must be said before anyone starts

**Capability and performance are separate decisions.** Phase 6 may be worthwhile because it
runs where the IL backend cannot. It closes on correctness, format safety and deployability,
not on a throughput claim. Startup, steady-state, memory and package-size comparisons remain
separate MOD-M1 decisions; persistence is not presumed to beat lazy IL compilation.

**It is a second backend, and the risk is a second semantics.** Sharing an AST is
insufficient. The production semantic analysis/lowering boundary must be extracted and the
IL arm migrated first. Conformance is three-way: pinned expected result, IL result and
bytecode result. IL/bytecode agreement cannot excuse a shared wrong answer.

**Reuse semantics and immutable structure; do not inherit mutable ownership accidentally.**
Shapes, property maps, element storage and slow semantic operations can be shared behind
explicit contracts. Inline caches, feedback, quickening overlays and tier counters must be
owned by a function/script/realm as appropriate, bounded, generation-aware and safe under
the concurrency model. Process-global emitted-site indexes are not a bytecode state model.

**A JavaScript-profile bytecode format is a trust and compatibility boundary once persisted.**
Qualify its identity by Broiler.VM language-profile ID, format version, and feature manifest. Version it,
verify it before execution, bound every section/resource, define cache keys and atomic
replacement, reject corrupt/incompatible data, and fuzz the verifier. A runtime-compiler
composition may fall back to source recompilation; execution-only must instead fail the bad
load deterministically and accept a fresh verified precompiled artifact. Do not freeze a
“whole-language” opcode list before the semantic IR and JavaScript-profile ABI.

### What the track may pay back into track one

**Item 4-3 asked for V8-style deoptimization and was re-specified because it is not
expressible here** — tier-1 locals are CLR locals of an IL method and `CallFrame` carries no
JavaScript values, so there is no interpreter frame to reconstruct. It became a restart
contract plus an in-method fallback; the restart contract is sound only under a condition
held by *two unrelated accidents*; and item **4-4 is still deferred** partly behind that
compromise.

**Phase 6 can create a reconstructable frame ABI, but that does not itself create
deoptimization.** Phase 9 must explicitly define `DeoptState`, safepoints, value
materialization, exception/suspension state, invalidation and reconstruction before any
**deopt-enabled** configuration ships. Function-level VM→IL promotion is a separate 9-0 →
9-1 → 9-2 branch with a retained VM fallback, failure/backoff policy, and quiescent
publication; it does not imply IL→VM reconstruction. Deoptimization is optional work with
its own correctness and value gate, not an automatic dividend of writing an interpreter
loop.

### Sequencing against track one

The tracks do not block routine IL-path fixes, but they are **not dependency-independent**.
MOD-M9 may perform capability discovery after MOD-M0, then the selected JavaScript composition requires MOD-M2's shared
front-end/graph boundary and the applicable MOD-M3/MOD-M4 AOT and packaging gates. Phase 7 requires
MOD-M1; shared or adaptive state also requires MOD-M6. Phase 9 is narrower still: only
a runtime-compiler composition may enter it, and only for a dynamic-code-capable host whose
product composition requires adaptivity. A runtime-compiler no-go selects execution-only; it
does not end the JavaScript executor, Broiler.VM, or WebAssembly. The selected outcome adopts
the reordered JavaScript phase plans below rather than reviving the old catalogue order.

---

## Sequencing

| Phase | Order within it | Size | Unblocks / expected effect | Exit gate |
|---|---|---|---|---|
| **0** | 0-1…0-5 ✅, 0-9…0-10 ✅, 0-11 mapping implemented/validation open → 0-6 workflow smoke ✅ (17/17 committed, refreshed 2026-08-07; per-suite spread recorded) → 0-7, 0-8 | — | Smoke coverage and permanent probes. The hosted run establishes harness continuity, not a candidate threshold or acceptance lane | 17/17 and no timeout under each suite's effective manifest budget ✅; `comparison.md` triad ✅; provisional spread observed on container/hosted runners ✅ for smoke. **Still open:** 0-11 referential-integrity/test repair; immutable candidate/control comparison with exact rows/all repetitions/resources; controlled win-x64/linux-x64/linux-arm64 lanes with effective CPU-feature/GC attestation; semantic/test262 bundle and durable raw evidence |
| **1** | 1-2 mitigation ✅ → 1-2 real fix ✅ (all three passes) → **1-4 ✅** → **1-1 emission half ✅** → **1-1's remaining half measured, and the repeated closure rewrite it found is fixed ✅**; the capture mechanism itself is still open → 1-3 measure | 1-4 S, 1-1 remainder L | The two worst scores in the suite; page-load time generally. **1-4 took the Mandreel half (3.04×); 1-1's deferred emission takes 0.64–0.69× off jQuery, PdfJS and Box2D at 1.0009× steady state, and CodeLoad 94.6 → 104.0 (1.099×)**. **The remaining half is now sized rather than inferred**: parse 9.4–13.5% / tree construction 33.6–63.9% / emission 25–57% on the real corpora, over a population that is **84–99.7% never invoked**. What blocks it is not a pre-parser and not `EmitConstant` — the `Box[]` a creation site passes *is* the capture mechanism — but that its indices are decided by `LambdaRewriter` from a tree the deferred body does not have. **That obstacle is now built and priced rather than bounded** (`0101`): the free-name map that makes the layout addressable costs **6.6–12.2%** of body-tree construction as one bottom-up pass, and **up to 47.7%** written per-function, where the walk is superlinear in nesting depth — so the previously recorded 5.4–9.9% *lower bound* was a fair estimate of the right implementation and five-fold low for the obvious one. Mandreel, wide and not deep, is the control that goes the other way (7.8% → 8.8%). The mechanism itself is still unbuilt and still **L**. **And the population that could skip it entirely is now counted, which closes off the cheap way in** (`0102`): a site whose free names resolve to no enclosing binding needs no `Box[]` and could be deferred today, and that is **728 of 5 762 sites, 12.6%** — 39.7% on the flattest corpus and **7.4% on Mandreel**, i.e. worst exactly where the prize is largest. `Dynamic`, the direct-`eval` risk the item leads with, refuses **7 sites of 5 762**. The reading that looked like an opening — Mandreel's 7 605 bound free names being only **165 function-owned**, because a top-level `var` is a global-object property per spec — is refused by the counter built to test it: **`cellBacked` equals `bound` exactly on all six corpora, 15 118 of 15 118**, since this engine gives a program-level binding a CLR local like any other. *A spec-level fact about where a binding lives is not a fact about where the compiler puts it.* **The repeated closure rewrite the measurement found is fixed and is worth 0.782× on jQuery's whole compile and 0.867× on Typescript's, six of six pairs each** | test262 over the four pinned manifests, no new failure **and no new timeout**; MandreelLatency and CodeLoad out of the tail |
| **2** | 2-0 ✅ → 2-1 ✅ → 2-2 ✅ → 2-4 ✅ → 2-7 ✅ → 2-8 ✅ → **2-9 ✅** (2-3's successor, L); 2-5 and **2-3 closed on measurements**, 2-6 folded into 4-1. **Every item is landed or closed** | M each, 2-9 L | The Richards/DeltaBlue/Box2D cluster | An ownership entry and owned tests **per item**; test262 properties/strict-mode **satisfied** — unchanged at `a6f101cc` plus 2-9; **DeltaBlue and Richards inside 200×** — **measured twice, agreeing: Richards PASSES (183× → 150× after 2-11/2-12 locally; 144.9× in CI), DeltaBlue FAILS (576× → 447× locally; 460× in CI)**, five repetitions per engine on one machine and the committed CI run on another. **2-13 then decomposed the failing half against the third engine and bounded it**: DeltaBlue is 2.83× harder than Richards for Broiler and **2.56× for Jint**, so **1.10× of the gap is Broiler's** (1.118× on the previous run, independently) and closing all of it reaches **362×** against a 200× gate. The criterion is **not reachable by removing a Broiler-specific deficiency**; Broiler is ahead of Jint on DeltaBlue (0.77×) as it is on Richards (0.69×), and the genuinely Broiler-specific suites are MandreelLatency (54.3×), CodeLoad (37.8×) and zlib (12.0×). Read polymorphism is falsified as the cause by Crypto, 73.82% monomorphic and Broiler's best suite against Jint. **2-10 closes as measured**, handing forward a question about the gate |
| **3** | 3-0 ✅ → **3-3 ✅** → 3-5 ✅ → 3-6 ✅ → 3-7 ✅ → 3-8 ✅ (counted, do not start as written) → **3-1 (85% of the corpus's boxes) → 3-2 (Box2D's 11.6 M), sharing one compiler half, and nothing else until they land** → then *cost* 3-4 | L–XL, 3-8 XL | Uniform lift across arithmetic and allocation-heavy suites. **3-7 closes the static half of the coverage question and 3-8 is what is left**: the widening reached 8 names of 2 920 (224 → 232), because 247 of 3-6's 478 captured names are held by a *hoisting* rule that is correctness rather than policy, and 2 439 are not proven numeric. **3-8 then measured the two numbers this phase never had, and they re-order it.** Number boxing is **41.89% of the corpus's allocation** (2.05 GB of 4.88, and 66.96% of NavierStokes) — so the prize was always large — while the **whole** raw-double local tier, every item from P2-2 onward, removes **0.36% of those boxes**. A box is minted by the operator, not by the local, and 76.4% of the names 3-8 would guard take their value from a property read or a call. **3-1 and 3-2 move to the front**: they unbox the sites that mint the boxes, and they have been ranked behind the locals work since the phase opened on no measurement at all. **Started, and the first count moves the item off storage.** What the generic arithmetic operators are handed at run time had never been measured: **73 817 515 of 73 818 646 invocations arrive with both operands already Numbers — every one but 1 131 — and that population is 86.6% of every box the corpus allocates**, while the compiler's own `both are native` proof reaches **0.75%** of the same invocations. *Compile-time provability reaches 0.75% of the arithmetic; run-time truth reaches 100.00%.* So the shared half is a **run-time-guarded specialization of an arithmetic expression tree** — box only the root — and not the typed backing store the item is written around; a typed store returns to being the live-memory item it always measured as. It also partly reverses 3-8's "do not start as written" without contradicting it: 3-8 priced the guard at the **local** (0.36%), this counts it at the **operator** (86.6%). **The shared half is then built** — evaluate each leaf once, test for Number, compute on raw doubles, box only the root — and removes **10 401 782 boxes of 85 249 783, 12.2% of the corpus's allocation, from 862 sites**, against **0.36% for every previous phase-3 item combined**. Short of the 86.6% ceiling for a reason the per-suite column gives rather than hides: NavierStokes loses 10.1% of its generic invocations and 1.8% of its boxes, EarleyBoyer 99.7% and none, so **most of those two suites' boxes are minted somewhere that is not a binary arithmetic operator** — the next count. Wall clock then measured: **driver 0.981× on six of six ABBA pairs, Crypto 0.912× on six of six**, against controls at 1.005× and 1.006× — so **12.2% of the allocation buys 1.9% of the time**, and no suite is slower. **3-1's own re-measurement then made that stronger.** The element chain decomposes exactly — 0.00 for a raw double, 31.98 for `s = s + a[0]`, 95.99 with a multiply, 159.67 for a read-modify-write — and the element STORE is in none of it: the boxes are minted by the operators, and the read is free today only because what it hands back is already a box. Two things fell out. A numeric literal is **re-boxed on every evaluation** (`a[0] * 1.5` costs two boxes where `a[0] * 2` costs one), measured at **1.2% of requests** and recorded rather than built. And the **bitwise and shift operators had no native form** although the analysis has always typed them — `s = i + 1023` costs 0.00 B/iter and `s = i & 1023` cost 31.84. That half **is built** (`JSNumericOperators`, all six through `ToUint32`, 15 tests on both arms) and takes its shape to **0.00** — **and removes no boxes at all on the corpus**: six suites identical to the digit, and Crypto (42.4 M boxes) differing by less than its own run-to-run variation, measured by running one arm twice. The native form needs both operands native and Crypto's digits live in `this.array[i]`. *Six items have now built machinery array-resident data cannot reach; every one is correct, every one is invisible, and every one is waiting on 3-1*. **3-2 was then measured too, and its one-sentence premise is wrong**: `o.x = 2` allocates **nothing** — a slot store is a reference copy — so `vector.x = 1.5` pays for the **literal**, not the slot, and the slot's own cost shows up only in `o.x = v * 1.5` where the value is a raw double (32 B, the same 32 B for the eleventh time). The field rows match the element rows **to the hundredth** — 31.98 and 96.00 both — so 3-1 and 3-2 are one mechanism with two backends. And 4-1's uncollected "numeric-vs-generic" signal, built at last, splits them exactly: **50.1% of all cache-answered reads hand back a number**, but **98% of those are Box2D's**, while **NavierStokes performs 388 property reads, zero numeric, and mints 29 977 471 boxes**. So **3-1 carries 85% of the corpus's boxes and 3-2 carries Box2D's**, and no work on shape slots reaches the other two suites. **The next count then named every box the corpus mints, and moved the item again.** The compiler's boxing conversion — the only thing a typed store could remove without further operator work — is **5.0% of NavierStokes' requests against 31.0% of Crypto's**, i.e. the two suites are the opposite way round from this item's premise. Chasing the **40.5%** that first pass left unattributed down to **1.0%** found the answer in the operators no census had counted: **`++` and `--` are 30.9% of the corpus's boxing, 51.6% of NavierStokes' and 80.4% of EarleyBoyer's**, and **half of that is `ToNumeric` re-boxing a value that is already a Number** — 17 281 232 requests, 15.4% of all boxing, removable by a guard. **Built, in nine lines**: 17 285 913 requests removed against that prediction (0.03%), **7 050 834 real allocations, 9.4%**, NavierStokes **23.0% of its boxes and 0.906× of its time on six of six pairs**, and the corpus **0.795×** with `0084`. **What did not move is the finding**: EarleyBoyer halved its boxes for 1.002×, because 82 000 a second is not 4 240 000 a second — *a share of a suite's own allocation forecasts nothing, the absolute rate forecasts everything*. **Then the refusal waterfall, which is the count `0084` never took and the largest result the phase has had.** Of 5 396 candidate arithmetic nodes only **862 specialize**; `OrderUnsafe` refuses 1 762 and `NoSavingToMake` 2 718, and those are **one** finding — a left-leaning `a[0]+a[1]+a[2]+a[3]` refuses at the root for order, again at each left child, and its bottom node is then a lone operator with nothing to save. The sub-census names the blocking leaf: **1 028 property reads against 34 element reads**, so the rule this phase assumed was an array problem is an **object-field** one, 984 of them Box2D's. **The fix is that nothing required the leaves to move**: emit each at its own postorder position and put the test where the coercion would have run, and the purity rule has nothing left to protect. **53 353 957 → 6 626 052 generic invocations and 67 795 858 → 31 162 330 boxes — 36 633 528 removed, 54.0% of everything the corpus allocates**, `OrderUnsafe` 1 762 → 0 and `NoSavingToMake` 2 718 → 1 181 untouched. From the pre-`0084` baseline the corpus is **0.366×**. **Driver 0.969× on six of six ABBA pairs, NavierStokes 0.834× and Crypto 0.893× both six of six**, two zero-box controls at 1.002× and 0.999×; Box2D cuts 51% of its own boxes for 1.003× because 861 000/s is not 6 500 000/s, which is `0086`'s lesson holding a second time. *54.0% of the allocation buys 3.1% of the time* — with `0084`'s 12.2% → 1.9%, the third reading of the constant that should size the rest of the phase  **And then the denominator the phase never had**: collection is **1.8–2.0% of the driver**, and of the 768 ms the order-preserving emission removed only **54 ms was collection** — the other 714 ms is the mutator's own allocation work. *A box costs ~14× more to create than to collect*, which makes §Non-goals' "the collector is not the problem" a measurement. At **711 ms per GB** the **0.70 GB of number boxes left is worth ~2.6% of the driver**, so everything remaining here is an XL bidding for under 2% — count the `++`/`--` step's operands before building the typed store, and bid with a rate rather than a share. A sampling profiler was tried and does not decompose this engine: it inflates the driver ~29%, its biggest frame is its own rendezvous point, and compiled JavaScript does not symbolicate  **And the `++`/`--` count is taken**: of 17 282 144 steps, **Element 0, Property 0.3%, LocalSlot 98.1%, Other 0** — the step shares no mechanism with a typed store and belongs to the numeric local. ≈**7.05 M real boxes, 22.6% of what the corpus still allocates**, 6.76 M of it NavierStokes', where one untypable closure variable (`rowSize`) cascades into every `++currentRow`. **Re-opens 3-8**, which priced the guard at the local and measured the tier's *yield* (0.36%); this measures what it *lets through*  **Then scoped**: eight shapes, one per conjunct, rule three suspects out — a nested function declaration is innocent, 3-7's hoisting rule produces a `LocalCell` (NavierStokes: 9 461 760 slots against six cells), and passing the value in only trades `OtherName` for `Parameter`. **One conjunct is left — the analysis will not type a name from outside the function, even one already proven numeric.** Splits into **3-9** (static, import the enclosing scope's conclusion; does *not* reach NavierStokes, whose root is held by 3-7's correctness rule; count its population first) and **3-8a** (run-time, one `IsNumber` test where the value enters) — scoped at **≈115 ms, 0.6% of the driver, an M rather than an XL**. **3-8a was then built complete and closed as a measured regression.** Its population is 26 names, 15 in NavierStokes; the dual representation and all three consumers that can take a raw double are built (the guarded tree's leaf, the element read, the element write), and each moved the number without moving it enough — 1.021×, 1.017×, 1.012×. A counter added **at the read** then settled it: **NavierStokes mints 393 705 boxes reading a speculative local against ≈5 300 removed**, because the 835 584 steps it takes off `Increment` are mostly `x[++i]`, whose result is boxed to be an index either way. *Every premise the item was scoped on survived and the item still lost* — what makes it lose is the read/write ratio of the code it targets, a property of the workload rather than of how many consumers the compiler grows. **Off by default and staying off; §3.5 gains the rule that a representation change is priced by that ratio, counted before the representation is built.** **3-9, the static half of the same split, is closed at a population of ZERO** — 0 names and 0 outer-numeric offers on all seven suites, against 3-8a's 26 from the same call site in the same run — because 3-9 can only import from a name that is both proven numeric and still a raw double despite being captured, which is item 3-7's eight, and none of the eight is read from an assignment inside the function that captures it. *Counted with an instrument proven to discriminate on nine constructed shapes first, and closed for one instrument and no mechanism*. **Then the denominator itself was checked** (§4.2a): the census producing every figure in this row ran **7 of 15 suites**, and widened it reads **90.6 M boxes and 12.93 GB against 31.4 M and 3.13 GB — 65.4% of the boxes outside the seven**, with **Gameboy alone at 41.3 M, 1.32× the whole measured corpus**. `0090`'s GC denominator survives (1.80% against 2.29%); the phase's ranking of its own remainder does not. **Attributing the widened corpus then partly reverses 3-1's move off storage**: conversions go **24.6 M → 69.3 M** with **64.4% of them outside the seven**, and **Gameboy alone mints 26.9 M at 51.0% of its own requests** — more than all seven together — on a `Uint8Array` memory image, which is the shape a typed backing store was written for. **3-1's storage half re-opens as unmeasured rather than refuted** | `test262-arrays`, `test262-binary-data`, and — added by 3-3's `let`/`const` half — `test262-lexical-declarations`; allocation reported per item alongside time **Then the conversion counter was split by emission site over all fifteen suites** (`0103`), and it both retires a suspicion and re-points what is left: **61.79% of 69.3 M conversions are the guarded tree's ROOT box**, the generic fallback arm is **226 of 69.3 M**, and Gameboy — the suite §4.2a re-opened the storage half on — is **28.7% `++`/`--` step**, i.e. item 3-8's population and not the store's. **The next measurement is the root box's CONSUMER**, which is a compile-time attribution rather than another run-time counter. **Then the root's consumer was counted** (`0105`) and it answers the question: **44.36% of the 42.8 M root boxes are consumed by a LOCAL**, 17.91% by an element and 13.14% by a property, so neither storage item is where the remaining boxes go — a proven-numeric local already has a raw `double` home, and a root landing there is one the numeric tier failed to type. **Phase 3's remainder is the numeric-local tier**, which is now the third independent count to say so. **Then the refusals were weighted by execution** (`0106`): the seam hypothesis is refuted at 36 boxes of 18.6 M, and of the 19.0 M boxes consumed by a refused local, **38.41% are cascades with no independent cause** and **36.35% are `ElementRead`** — the conjunct item 3-1's guarded tree already settles at run time. Next measurement: the read/write ratio for that population, before any representation is built (§3.5, and item 3-8a's regression). |
| **4** | 4-3 design ✅ → **4-1 ✅** (shapes and callees; numeric-vs-generic still open per site — item 3-2 collected the aggregate read share, 50.1%, for a phase 3 ranking) → **4-3a ✅** → **4-3b ✅** → **4-2a ✅** → **4-2b ✅** → **4-2c ✅ refuted** (the arithmetic half priced at 0.119% and closed, the relational lead closed with it at 0.022%, and the whole generic binary-operator surface bounded at 0.475% of the corpus) → **4-5 ✅ unblocked** (44% of a call entry is bookkeeping the engine's own short path skips — **2.85% of the corpus**, the largest measured target left in the phase, and an ablation of eight named operations rather than a profiler) → **4-4 ✅ measured, not started** (its ceiling re-taken over the twelve suites that run is **2.43%**, *larger* than the seven-suite 1.89% — the promotion gate reaches 42.1% of the corpus's JavaScript calls rather than 64.0%, but the never-counted suites are far call-denser per millisecond — while 4-5's surface is **8.06%**, so the ranking holds by 3.3×) | XL | The remaining order of magnitude. **4-1 measured the premise: 93.5% of reads and 96.7% of calls are monomorphic by execution weight, so 4-2 and 4-4 are well-founded** — over **seven** suites. **§4.2a re-took it over twelve and it is 80.11% and 86.35%**, because the census corpus every phase-3 and phase-4 headline is computed over was 7 of 15 and never said so; Mandreel had been aborting the census host with an uncatchable stack overflow, since item 0-2's stack reserve is a property of the *shell* and no benchmark host had it. Fixed, and the number is still high enough to found the phase. 4-3a stated and enforced the restart contract — and found its no-suspendable-bodies condition was held only by two unrelated accidents, two ordinary refactors away from an async function returning a number instead of a Promise. **4-2 then split the same way**: measuring the branch it was told to replace found it produced *wrong answers* — DeltaBlue died on the shipping tier-2 hook — which 4-2a fixes, and 4-2b's specialization takes **44.7% of the corpus's executed reads off the cache path at 0.818× each**, which is **0.83% of suite time**. That number is the phase's own warning: the whole read path is ≤ ~9% of Octane's execution time here and the whole call path ≤ ~5.5%, so **4-4's ceiling is smaller than the phase assumed** | Deopt correctness proven **before** any speculation ships; full test262 matrix **4-5's floor moved 0.100% on the lever `0111` named** (`0104`, `out` parameter, 9 of 12 ABBA pairs) and its 1.46% frame is untouched; the useful residue is that removing two struct copies bought 1.83 ns against a replica's 8.19 ns each, so *a struct copy in the source is not a struct copy in the code*. |
| **5** | profile ✅ → per-match subject copy on `replace`/`exec` ✅ → single-match `replace` without a builder ✅ (both builtins) → the global case's retained result list ✅ → **`Compiled` per pattern ✅ — built as a race, measured, and shipped switchable with the default off** → ~~then consider compiling `Broiler.Regex`~~ **→ the per-call envelope, which is where the phase's remaining time actually is** | L | RegExp, plus PdfJS and Typescript | Octane regex corpus profiled **before** any rewrite — **satisfied**, and it re-ordered the phase twice. The second re-ordering is item 2's: the matcher is **4.6–6.5%** of a `re.test`, so nothing aimed at matching — the .NET compiler, and by the same argument a compiled `Broiler.Regex` — can move this suite. **The ~2.4 µs and 2 431 B a regex call pays before any matching happens is the item**, and it is unstarted |
| **A** | **Expression-model/emitter split landed; validation pending** → MOD-M2 dependency census, boundary ADR and fake backend → MOD-M3 honest IL/AOT graph → MOD-M4 hosting/compiler/tool packaging, only where the measured package graph justifies it | M–XL by slice | A maintainable graph in which front-end semantics, execution backends, host composition and optional tools have explicit owners and no accidental dynamic-code edge | Build-proven acyclic graph; source scan plus compiled dependency closure for `System.Reflection.Emit`; explicit backend registration; IL conformance unchanged; separate execution-only and runtime-compiler AOT samples; no new assembly without an API/dependency/startup rationale |
| **6** | MOD-M9 ADR = 6-0 → independent expected-result harness → shared production semantic IR with IL migrated first → JavaScript value/frame ABI → minimal versioned JavaScript profile format/verifier → vertical interpreter slices → approved hard semantics | **several XL** for a general runtime compiler; execution-only or deliberately narrow runtime compilation is sized from its explicit manifest rather than inheriting the full estimate | The JavaScript capability selected by MOD-M9 on the Broiler.VM core, without dynamic code | Expected/IL/VM conformance for the declared JavaScript manifest; verifier/resource failures are deterministic; each claimed static AOT composition publishes and runs its real workload; **no performance claim** |
| **7** | Uninstrumented MOD-M1 baseline → decompose dispatch/boxing/property/element/call/host/resource costs → add only measured function/realm-owned fast paths → remeasure each slice | L–XL | A shippability decision and a bounded baseline interpreter, not a pre-decided “fast enough” claim | Exact candidate/control rows, A/A-calibrated decision, current product corpus, memory/CPU/startup evidence, conformance unchanged and no global mutable cache ownership |
| **8** | Choose independently: persistence gate → format/cache safety; feedback → owned sidecars; quickening/superinstructions/dispatch → measured opcode populations; broader PGO only after those results | M–L each | Startup or interpreter improvements whose population and rate are current and explicit | Each item cites its own population, resource budget and MOD-M1 result; cached/uncached and cold/warm paths are separate; verifier/fuzz/corruption behavior follows the selected execution-only or runtime-compiler composition; MOD-M6 for shared state |
| **9** | After the runtime-capable/dynamic-host gate, branch: 9-0 curve → 9-1 owned state → 9-2 opt-in function promotion; independently 9-3 `DeoptState`/reconstruction → 9-5 restart decision; 9-4 OSR only after validated promotion and its own population/entry-stub spike | M–XL | Optional adaptivity; deopt may replace the limited Phase 4 restart compromise, while promotion does not depend on it | Expected/IL/bytecode/enabled-tier configurations conform; threshold curve and resource/lifecycle bounds for promotion; forced guards only for enabled deopt safepoints; rollback flag; deopt and OSR may each remain a recorded no-go |

**Dependencies.**

- Phase 0 gates every performance/resource claim in phases 1–5 *and* retroactively gates
  performance/resource closure of A–F.
- Phases 1 and 2 are independent of each other and of phase 5, and can run in parallel.
- 3-2 is cheaper after 2-1.
- Phase 4 depends on 4-3 (for everything in the phase) and on 4-1 for 4-4's callee feedback — what was 2-6 is now inside 4-1 — and
  benefits from 3-1/3-2 having established unboxed representations to speculate into.
- Modernization MOD-M9 replaces Phase 6's old 6-0 and selects JavaScript capability depth;
  it cannot cancel Broiler.VM core or WebAssembly.
- The selected JavaScript composition requires Broiler.VM's core/profile contract,
  MOD-M2/MOD-M3, and the applicable MOD-M4 packaging boundary before Phase 6;
  Phase 7 and Phase 8 require MOD-M1, while shared/adaptive state also requires MOD-M6. Phase 9
  additionally requires a runtime-compiler composition, a dynamic-code-capable host, and an
  explicit adaptivity requirement.

**The bolded item in each phase is the one to start with**, and in three of the five it
is not the one that sounds most important: 1-1 over 1-3, 2-1 over 2-2, 4-3 over 4-2.
Each ordering is argued where the item is described.

**Every performance claim closes under [`Measurement.md`](Measurement.md) and MOD-M1.** The
decision must fail closed unless exact candidate/control rows pass A/A-calibrated stability,
the predeclared practical threshold, effective-settings and build-identity attestation,
semantic-owner conformance, and the required time/allocation/working-set/CPU/resource gates.
Historic “two runs inside a configured band” evidence is smoke or prioritization evidence,
not the modern acceptance protocol. **Phases A–F are implemented but not retroactively
accepted until their relevant gates are reproduced.**

---

---

## The optimization catalogue — the design space this plan was chosen out of

### Read the first four columns as estimates, and the fifth as measurement

Two things have to be said before the table, because the campaign has now measured
enough of it to contradict it in places.

#### The catalogue assumes a bytecode VM. Broiler.JS is not one.

The table is written for an engine that translates the AST into VM opcodes and
dispatches them in a loop. **Broiler.JS compiles JavaScript to LINQ expression trees and
then to IL** — `Broiler.JavaScript.LinqExpressions`, `.ExpressionCompiler`, `.Compiler` —
and the CLR's own JIT turns that into machine code. A tier-1 local is a **CLR local of an
IL method**, not a slot in a virtual register file.

That is not a quibble about vocabulary; it decides whether a row is reachable at all.
Nine rows — compact bytecode, register VM, dispatch, superinstructions, peephole,
opcode reordering, adaptive opcodes, bytecode cache, and PGO for the VM itself — describe
a machine this engine does not have, and are marked **n/a** below. **Their prize is not
lost**, because the CLR JIT already performs the equivalent work on the IL: dispatch is
not a cost when there is no dispatch loop.

> **These nine rows are candidates, not a funded sequence.** [Phases 6–9](#track-two--the-vm-tier-phases-69)
> own the corresponding questions only after MOD-M9 records a go outcome. Phase 6 takes the
> minimal correctness/ABI/verification work; persistence and every adaptive technique are
> independently population- and rate-gated later. They remain **n/a to the general engine
> that ships today**, and a generic catalogue entry is not evidence that Broiler.JS should
> implement one.

The one place the catalogue's assumption does hold is
**`Broiler.JavaScript.Portable`**, a separate numeric bytecode/interpreter capability for
offline compilation and Native AOT. It is deliberately tiny — numeric parameters and
locals, arithmetic, comparisons, assignment, blocks, `if`, `while`, counted `for`, value
returns — and implements no part of the JavaScript object model. See
[`Measurement.md`](Measurement.md). Rows marked **n/a** are n/a for the general engine;
they become live questions only within the JavaScript capability manifest MOD-M9 approves. None of those
rows is a reason to grow the numeric seed. The product capability gap is the reason;
current measurements and the phase gates choose which techniques, if any, follow.

#### Three of the five-star rows have been measured and are worth less than filed

The estimate columns are the author's prior, and the campaign has since put numbers on
some of them. Where it has, the row says so — and it disagrees three times, each time in
the same direction:

- **Call-site cache** (⭐⭐⭐⭐, "very high"). Measured and **refuted**: a call costs ~250 ns
  and a call-site cache removes none of it, because there is no callee resolution to
  cache. Folded into item 4-1. Appendix B, row 2-6.
- **Hidden classes / shapes** and **inline caches** (⭐⭐⭐⭐⭐, "extremely high"). Both were
  *already built* when this campaign opened, and the finding that opened it is that they
  were **inert for most real JavaScript** — an ordinary property write destroyed the
  object's shape, and the cache did not cover prototype lookups, so method calls never
  hit. See [`Archive.md`](Archive.md) §5. **A technique's
  value is a property of its implementation, not of its name**, which is the standing
  hazard of reading any catalogue including this one.
- **Unboxed numbers** (⭐⭐⭐⭐⭐, "extremely high"). Correct about the target and wrong about
  where to apply it, four times running. Boxing is **41.89% of what the corpus
  allocates**, so the prize is real; but the whole raw-`double`-*local* tier removes
  **0.36%** of those boxes, because a box is minted by the **operator**, not by the local.
  Applying the same idea at the operator instead removed **54.0%**. See
  [`Phase-3.md`](Phase-3.md) and
  [`Phase-3.md`](Phase-3.md).

**And the exchange rate the campaign keeps re-measuring is the one no column here has.**
Removing 54.0% of the corpus's allocation bought **3.1% of its time**; removing 12.2%
bought 1.9%. An effect column that ranks by allocation removed will mis-rank by roughly
an order of magnitude against one that ranks by time. [`measurement.md §3.5`](Measurement.md#35-standing-measurement-lessons) is where
that and the rest of the campaign's measuring rules live, and it is the file to read
before designing a probe from any row below.

---

### The catalogue itself

*Effect, effort, Native AOT and priority are **as filed** — the original note's estimates,
untouched. **Where it stands** is this repository's, and cites the item that measured it.*

| Optimization | Principle | Effect | Effort | NativeAOT | Prio | Where it stands |
| --- | --- | ---: | ---: | :---: | :---: | --- |
| **Compact bytecode** | Translate the AST into VM opcodes once | High | Medium | ✅ | ⭐⭐⭐⭐⭐ | **n/a** — expression trees → IL (above) |
| **Register VM** | Virtual registers instead of an operand stack | High | High | ✅ | ⭐⭐⭐⭐ | **n/a** (above) |
| **Efficient dispatch** | One large `switch` / jump table | High | Low–medium | ✅ | ⭐⭐⭐⭐⭐ | **n/a** — no dispatch loop (above) |
| **Specialized opcodes** | e.g. `AddInt32` instead of a generic `Add` | High | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Reached by another route ✅** — the guarded arithmetic tree specializes the *node*: 53.4 M → 6.6 M generic invocations. Item 3-1 |
| **Superinstructions** | Fuse frequent opcode sequences | Medium–high | Medium | ✅ | ⭐⭐⭐⭐ | **n/a** (above) |
| **Constant folding** | Turn `2 * 3` into `6` up front | Medium | Low | ✅ | ⭐⭐⭐⭐⭐ | **Not scoped.** Adjacent finding: a numeric *literal* is re-boxed on every evaluation — 1.2% of requests, recorded not built (item 3-1) |
| **Dead code elimination** | Remove unreachable code | Medium | Medium | ✅ | ⭐⭐⭐⭐ | **Not scoped** |
| **Copy propagation** | Eliminate unnecessary moves | Medium | Medium | ✅ | ⭐⭐⭐ | **Not scoped** — the CLR JIT does this on the emitted IL |
| **Peephole optimizer** | Simplify local bytecode patterns | High/effort | Low | ✅ | ⭐⭐⭐⭐⭐ | **n/a** (above) |
| **Fast paths** | Handle the common type cases directly | Very high | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Landed, repeatedly** — phase 2's caches, phase 3's guarded tree. The campaign's most productive shape |
| **Slow paths** | Move complex special cases off the hot path | High | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Landed** — P0-3 took Annex B `caller`/`arguments` off the call path; item 4-5 is the rest, and the largest measured target left in phase 4 |
| **Type feedback** | Record observed types per instruction | Very high | High | ✅ | ⭐⭐⭐⭐ | **Landed ✅ (item 4-1)**, and it measured the premise the phase rests on: **80.11% of reads and 86.35% of calls are monomorphic** by execution weight over twelve suites |
| **Adaptive opcodes** | `Add` → `AddInt32` after observation | Very high | High | ✅ | ⭐⭐⭐⭐ | **n/a as written** (above); the equivalent is item 4-2b's specialized read — **44.7% of executed reads off the cache path at 0.818× each** |
| **Inline caches (IC)** | Cache property lookups and calls | **Extremely high** | High | ✅ | ⭐⭐⭐⭐⭐ | **Built before the campaign and inert; repaired in phase 2 ✅.** §1.2 |
| **Hidden classes / shapes** | Classify object structures | **Extremely high** | High | ✅ | ⭐⭐⭐⭐⭐ | **Same ✅** — an ordinary write used to destroy the shape (P1-1). §1.2 |
| **Monomorphic IC** | Cache one frequent shape directly | Very high | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Landed ✅** — items 2-1 (get), 2-4 (`o.x++`, `o.x op=`), P1-3 (store) |
| **Polymorphic IC** | Cache several frequent shapes | Very high | High | ✅ | ⭐⭐⭐⭐ | **Open.** Item 4-1 sized the population it would serve; read polymorphism is *falsified* as the cause of the DeltaBlue gap (item 2-13) |
| **Megamorphic cache** | Global cache for highly variable accesses | Medium–high | High | ✅ | ⭐⭐⭐ | **Not scoped** |
| **Call-site cache** | Remember a call site's target | Very high | Medium | ✅ | ⭐⭐⭐⭐ | **Refuted at 0** (item 2-6 → 4-1). §1.2 |
| **Method / property indexing** | Name → slot/index instead of a dictionary lookup | Very high | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Landed ✅** — items 2-7, 2-9. A three-field object is **0.36×**, an eight-field one **0.15×**; 16.2 M property maps over an Octane run become 2.5 M |
| **String interning** | Share identical property names | High | Low–medium | ✅ | ⭐⭐⭐⭐⭐ | **Not scoped as such**; item 2-9's shape-tracked properties removed most of what it would pay for |
| **Efficient `JsValue`** | Compact tagged-value representation | **Extremely high** | High | ✅ | ⭐⭐⭐⭐⭐ | **Item 3-4 — cost, do not start.** Its case is the strongest in phase 3 and it is still behind 3-1/3-2, which reach the same boxes without an engine-wide redesign |
| **Unboxed numbers** | Hold numbers without a .NET object | **Extremely high** | Medium–high | ✅ | ⭐⭐⭐⭐⭐ | **Phase 3, and the catalogue's largest correction.** §1.2 |
| **NaN boxing** | Encode type + value in 64 bits | Very high | Very high | ✅ | ⭐⭐⭐ | **Not scoped** — same family as 3-4, and behind it |
| **Allocation avoidance** | Avoid temporary objects | Very high | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Landed repeatedly ✅** — P2-1 (`push` descriptor), P2-2 (small-number cache), item 3-0 (an indexed access boxed its index: **0.00 B/element** against 31.67) |
| **Frame pooling** | Reuse call frames | Medium–high | Low | ✅ | ⭐⭐⭐⭐ | **Tried, then superseded ✅** — recycling first, then a **shadow stack**: an argument-less call went 80 B → **0 B**, and the bookkeeping was worth **11%**. Archive §7 |
| **Array fast path** | Special-case dense numeric arrays | Very high | High | ✅ | ⭐⭐⭐⭐⭐ | **Half landed, half open** — P2-3 made a dense element one reference instead of a 32-byte descriptor (`new Array(1000)` allocates **73% less**); the unboxed backing store is **item 3-1**, still open |
| **TypedArray fast path** | Direct memory operations | Very high | Medium | ✅ | ⭐⭐⭐⭐⭐ | **Open, and re-opened by measurement** — the widened census found Gameboy minting **26.9 M conversions on a `Uint8Array` memory image**, more than all seven previously-measured suites together. Item 3-1's storage half |
| **String fast paths** | Specialize frequent string operations | High | Medium | ✅ | ⭐⭐⭐⭐ | **Landed ✅** — P2-4: repeated concatenation is no longer quadratic (**150×** on the accumulation loop, 10.6× less allocation) |
| **SIMD / `Vector128`** | Vectorize strings, TypedArrays, … | Medium–high | High | ✅ | ⭐⭐⭐ | **Implemented, evidence owed** — a SIMD claim needs x64 with the feature enabled *and* disabled, per [`Measurement.md`](Measurement.md). That is item **0-8**, and a container cannot produce it |
| **ARM64 intrinsics** | Use NEON etc. deliberately | High in special cases | High | ✅ | ⭐⭐ | **Same — 0-8**, needs an AdvSimd-capable Arm64 host |
| **Branchless operations** | Replace branches with masks/selects | Medium | Medium | ✅ | ⭐⭐⭐ | **Not scoped** |
| **Opcode reordering** | Arrange hot handlers cache-friendly | Small–medium | Low | ✅ | ⭐⭐⭐ | **n/a** (above) |
| **Hot/cold splitting** | Move rare ECMAScript cases out of line | Medium | Medium | ✅ | ⭐⭐⭐⭐ | **Landed in part** — P0-3; item 4-5 is the measured remainder, **6.50% of the corpus**, 92% of it Annex B bookkeeping |
| **PGO for the VM itself** | Optimize the Native AOT build on real JS workloads | Medium–high | Low–medium | ✅ | ⭐⭐⭐⭐ | **n/a as written** (above); nothing equivalent is scoped |
| **Lazy compilation** | Translate functions to bytecode on first use | High at startup | Medium | ✅ | ⭐⭐⭐⭐ | **Item 1-1 — emission half ✅, deferral open.** jQuery **0.661×**, PdfJS 0.689×, Box2D 0.636×, steady state 1.0009×, **CodeLoad 94.6 → 104.0 (1.099×)** |
| **Lazy parsing** | Parse function bodies fully only on demand | High at startup | High | ✅ | ⭐⭐⭐ | **Item 1-1's remaining half — open, priced, and blocked on one thing** (`0101`): the free-name map that makes the capture layout addressable costs 6.6–12.2% of body-tree construction done right, up to 47.7% done obviously. Population is **84–99.7% never invoked** |
| **Bytecode cache** | Reuse already-compiled bytecode | Very high at startup | Medium | ✅ | ⭐⭐⭐⭐ | **n/a** (above) |
| **Hotness counter** | Detect frequently executed functions/loops | Foundation | Low | ✅ | ⭐⭐⭐⭐⭐ | **Landed ✅** — items 4-3a/4-3b; also the per-pattern regex race (phase 5 item 2), which counts a pattern's own matches |
| **IL tier-up** | Hot bytecode via `Emit` → RyuJIT | **Extremely high** | High | ⚠️ optional | ⭐⭐⭐⭐⭐ | **Already the tier-1 path**, so the item is the *specializing tier 2*: 4-2a ✅ (which found the shipping hook produced **wrong answers** — DeltaBlue died on it), 4-2b ✅, **4-2c refuted at 0.119%** |
| **Deoptimization** | Leave optimized code when assumptions break | Extremely high | **Very high** | ⚠️ | ⭐⭐⭐ | **Design landed, re-specified ✅ (4-3a)** — V8-style frame reconstruction is **inexpressible here**, because tier-1 locals are CLR locals of an IL method and `CallFrame` carries no JavaScript values. Restart + an in-method fallback branch instead |
| **OSR** | Switch a running hot loop into JIT code | Extremely high | **Very high** | ⚠️ | ⭐⭐ | **Out of scope**, for 4-3a's reason above |

**Twelve rows this plan has never scoped**, listed so their absence is a decision rather
than an oversight: constant folding, dead code elimination, copy propagation, megamorphic
cache, string interning, NaN boxing, branchless operations, and the five n/a rows that
would only matter to a bytecode VM. None is refused on a measurement; each is simply
behind something that was measured.

---

### The five with the best expected return

The original note's own shortlist, before any of it was measured. **Two of the five have
since been refuted or heavily qualified** (see the three measured rows above) — it is kept as filed, because a
shortlist that was wrong in a specific way is more useful than one quietly corrected.

```text
              JavaScript
                  │
                  ▼
             Broiler IR
                  │
          Constant Folding
          Peephole Optimize
                  │
                  ▼
          compact bytecode
                  │
                  ▼
        ┌────────────────────┐
        │ Broiler.JS VM      │
        │                    │
        │ Fast Paths         │
        │ Inline Caches      │
        │ Shapes             │
        │ efficient JsValue  │
        └─────────┬──────────┘
                  │
            Hotness Counter
                  │
          ┌───────┴───────┐
          ▼               ▼
        cold             hot
          │               │
          ▼               ▼
         VM          IL → RyuJIT
```

**Shapes and inline caches** especially should not be underestimated. In JavaScript,
something like:

```js
for (let i = 0; i < 1_000_000; i++)
    sum += person.age;
```

naively costs, every single time:

```text
"age"
  ↓
find property
  ↓
prototype?
  ↓
descriptor?
  ↓
getter?
  ↓
value
```

With a shape and a monomorphic IC, after the first access it becomes roughly:

```text
person.Shape == #42?
        │
       YES
        ↓
Slot #3
        ↓
Value
```

That can make a **massive** difference.

> **What actually happened.** Broiler.JS had both structures and this loop still did not
> hit, because the write on the line before it destroyed the shape and the cache did not
> follow prototypes. The diagram is right; the campaign's first phase was making it *true*.
> [`Archive.md`](Archive.md) §5.

---

### Historical generic staging — background, not execution order

The source catalogue grouped optimizations into four conventional stages:

| Stage | Features | Goal |
| --- | --- | --- |
| **VM 1.0** | Bytecode, dispatch, fast paths, constant folding, peephole | A correct baseline interpreter |
| **VM 2.0** | Shapes, ICs, string interning, array fast paths | Respectable JS performance |
| **VM 3.0** | Type feedback, adaptive opcodes, superinstructions, PGO | A highly optimized interpreter |
| **VM 4.0** | Hotness + IL tier-up, and possibly OSR/deopt | An adaptive JIT engine |

In that generic design, VM 1–3 can avoid dynamic code and VM 4 can use it only on platforms
where it is permitted. That architectural sketch is:

```text
JS → bytecode → highly optimized VM
```

and on Windows/Linux use:

```text
JS
 ↓
bytecode VM
 ↓
hot?
 ├─ no  → VM
 └─ yes → IL → RyuJIT → native code
```

> **Do not execute this table as phases 6–9.** The current phase plans deliberately reorder
> it: MOD-M9 selects a JavaScript capability manifest and deployment/compiler composition; Phase 6 establishes shared semantics, ABI,
> verification and correctness; Phase 7 takes an uninstrumented baseline; Phase 8 evaluates
> persistence and adaptive interpretation separately; Phase 9 treats tiering/deopt/OSR as
> optional feasibility work.
>
> **How it maps onto the engine that exists.** The AOT boundary is real and the staging
> is sound; the layers are not the ones drawn. Broiler.JS's *baseline* is already
> `Reflection.Emit`, so the AOT-safe tier is not "VM 1–3" but
> `Broiler.JavaScript.Portable`'s numeric subset (above), and the tier-up in phase 4 is
> IL → **specialized** IL rather than bytecode → IL. What transfers unchanged is the
> conclusion: **every promoted path needs a semantically complete fallback**, which is
> exactly 4-3a's restart contract, and exactly the rule
> [`Measurement.md`](Measurement.md) states for tiering — *retain the original delegate as
> the semantic fallback*.

---

### The benchmark argument, which is now the campaign's own

This would also make a good benchmark setup for Broiler.JS: **VM vs. VM+JIT** against
Jint, V8/Node and JavaScriptCore. Then at every optimization stage you see fairly
immediately which measures actually gain anything — because with VM optimizations, the
theoretically "clever" measures are not always the ones that win most on real hardware.

> **This is the campaign's founding argument, and it was built.** The harness is
> `tests/octane/` in the aggregate repository, running Octane 2.0 against Chromium and
> Jint on the same machine at the same time; the current run reports **17 / 17 scores, a
> geomean of 372, and 0.644× against Jint**. See [`performance.md §0.1`](Roadmap.status.md#0-status) and
> [`performance.md §1` §2](Roadmap.md#1-what-the-merge-produces-that-neither-document-had).
>
> The closing sentence has been vindicated more often than any other line in this file.
> The clever measure that did not win: **item 3-8a**, a dual-representation numeric local,
> built complete with all three consumers — 1.021×, 1.017×, 1.012×, and closed as a
> regression. Every premise it was scoped on survived and it still lost, on a property of
> the workload (its read/write ratio) that no amount of cleverness in the mechanism could
> reach. That is now a standing rule in [`measurement.md §3.5`](Measurement.md#35-standing-measurement-lessons).

---

## Non-goals

Stated explicitly so effort does not drift into them.

- **GC work.** SplayLatency at 45× is the *best* result in the suite (B7). The
  allocation **rate** is a severe problem — that is phase 3, and it is a problem with
  what the engine asks the collector to do, not with the collector.
  **This is now measured rather than asserted (item 3-1).** `GC.GetTotalPauseDuration()` puts
  collection at **1.8–2.0% of the driver**, and of the 768 ms an allocation change removed, **54 ms
  was collection and 714 ms was the mutator** — the pointer bump, the zeroing, the write barriers
  and the cache traffic of touching a gigabyte of fresh memory. *A box costs about fourteen times
  more to create than to collect on this corpus.* Aiming at the collector would have been aiming at
  a fourteenth of the problem, which is what this bullet always claimed and could not previously
  show. **Qualified since, and the qualification is that "the corpus" was seven suites** (§4.2a):
  measured over every suite that runs, collection is **1.07%** of elapsed — but the spread is
  **0.7% to 10.3%**, and the top of it is **Splay**, the suite Octane includes to stress the
  collector and the one no census had ever run. The conclusion holds everywhere measured
  (allocation dominates collection on every suite); what should stop being quoted is a single
  exchange rate, since on Splay it is nearer 9:1 than 14:1.
- **asm.js or WebAssembly special-casing** for Mandreel and zlib. Recognizing asm.js
  type annotations would move two scores and is exactly the optimize-for-the-benchmark
  behaviour that got Octane retired in 2017. Phases 3 and 4 reach the same code through
  general mechanisms.
- **Chasing the geomean directly.** If a change raises the total without raising the
  worst scores, it has not smoothed anything (§2.1).
- **Anything that trades conformance for speed.** Every item is a
  same-observable-behaviour change. Where the spec-visible surface is genuinely at risk
  (1-1's early errors, 2-1…2-4's `OrdinarySetWithOwnDescriptor`, all of phase 4) the
  risk is called out and the gating manifest named.
- **Security.** Broiler.JS is not a sandbox, and none of this changes that. Compliance
  and performance completion must never be presented as isolation of untrusted scripts.

**Scope discipline.** Octane was retired by its authors precisely because engines began
optimizing for its shapes. Every item above is justified by a *mechanism* that matters
to real JavaScript, with the benchmark used as **evidence that the mechanism is
missing** — never as the target.

**No longer non-goals:** parsing/compilation and a speculating tier, both of which the
engine roadmap excluded. See §1.1.

---

---

## Appendix B — traceability

Where each item came from, so existing cross-references still resolve.

| This document | Engine roadmap | Octane roadmap | State |
|---|---|---|---|
| §4.1 phase A | P0-1, P0-3 | — | Implemented, not closed |
| §4.1 phase B | P0-2 | — | Implemented, not closed |
| §4.1 phase C | P1-1, P1-4 | — | Implemented, not closed |
| §4.1 phase D | P1-2, P1-3 | — | Implemented, not closed |
| §4.1 phase E | P2-1, P2-2 (+ engine §6.5 array defects) | — | Implemented, not closed. **P2-2 item 3 shipped a wrong-answer bug**, found and fixed while working 3-3's successor: two writes to a numeric local were invisible to the analysis proving it numeric — a `var` re-declared in a nested statement, and any name bound through an object destructuring pattern. The first returned NaN; the second aborted compilation of the whole script with an unhandled `NotImplementedException`. See 3-3 |
| §4.1 phase F | P2-3, P2-4, P3 | — | Implemented, not closed |
| 0-1 … 0-5 | — | 0-1 … 0-5 | Implemented |
| 0-6 | — | Octane §2.6 | **Implemented for hosted smoke; not acceptance evidence** |
| 0-7, 0-8 | engine §8.1 acceptance evidence | — | **0-7 ran without an acceptable result bundle; 0-8 remains owed** |
| 0-9, 0-10 | engine §8.1, §8.2 | — | Done |
| 0-11 | engine §8.2 | — | **Mapping implemented; validation repair open** |
| 1-1 | *excluded by engine §9* | 1-1 | **Emission half landed; capture half open.** Deferring IL generation to first invocation makes all four of the item's named risks vacuous — they are front-end properties and the front end still runs eagerly. jQuery **0.661×**, Box2D 0.636×, PdfJS 0.689×, allocation ~0.52× throughout, steady state **1.0009×**, and **Octane CodeLoad 94.6 → 104.0 (1.099×, 24 samples an arm, 93% pairwise dominance)** — the benchmark the item names, run and passed, though 1.099× is far short of the "large multiple" the item predicts because compilation is only ~27% of what CodeLoad measures. Typescript 1.034× and unexplained. Shipped as `patches/0066` while its push was blocked by a 403; **since applied and pushed — it is commit `9bf9639b`, an ancestor of the pin**. What remains is deferring the parse and tree construction, which needs the capture mechanism |
| 1-3 | *excluded by engine §9* | 1-3 | Open, and **re-aimed**: the synthetic split (parse 0.5% / tree 11% / emission 89%) does not hold on real source, where deferring *all* nested-body emission removes only 17–36%. 1-3 is a front-end item, and its first task is that split on the corpora |
| 1-4 | — (found measuring 1-1's premise) | — | **Landed** — the closure rewrite's per-lambda scope was a `List` asked `Contains` per parameter reference, so IL emission was **quadratic in a scope's binding count**. A reference-keyed multiset, list-backed below 32 bindings: **28.5×** on 2 000 top-level declarations, **3.04× on Mandreel** end-to-end (ABBA, six pairs), inside noise on the narrow-scope corpora. Shipped as `patches/0065` while its push was blocked by a 403; **since applied and pushed — it is commit `1070525a`, an ancestor of the pin** |
| 1-2 mitigation | *excluded by engine §9* | 1-2 | **Landed** — `43bc4230`, in the pinned pointer |
| 1-2 real fix | — | 1-2 | **Landed on all three recursing passes.** `StackGuard` was repaired and put on `AstMapVisitor.Visit`; `FastParser.Expression` is now guarded too, which was the last one — its descent aborted the process at 25 000 nesting levels in the DEFAULT configuration and now survives 90 000, median paired ratio 0.9993. The four-way matrix's "mitigation off / guard on" row is a **linux-x64** statement: on win-x64 the front end compiles in place on ~1 MiB while the threshold is 4 MiB, so no segmenter can fire there |
| 2-0 | — (P1-2's guard, reached in a state it cannot recognise) | — | **Landed** — `2df877a0`, in the pinned pointer |
| 2-1 | P1-3 remainder | 2-1 | **Landed** — `5d31617a`, in the pinned pointer; **test262 owed** |
| 2-4 | P1-3 remainder | 2-4 | **Landed, both halves** — `f9c2193f` (`o.x++`) and `c5842c9d` (`o.x op= rhs`), both in the pinned pointer; computed keys, `super`, optional chains, private names and the three short-circuiting compound forms stay out on purpose |
| 2-2 | P1-4 remainder | 2-2 | **Landed for arrays** — `641241af`, in the pinned pointer; its four named benchmarks were the wrong targets |
| 2-8 | — (the blocked half of 2-2) | — | **Landed** — `850121a0`, in the pinned pointer; both prerequisites fixed. **Shipped a regression that broke DeltaBlue** (a cached store to `f.prototype` bypassed `JSFunction`'s cached-field sync); the gate that fixes it is folded into the same commit |
| 2-3 | P1-4 remainder | 2-3 | **Closed** — measured twice. Not a pure removal, ~3% throughput ceiling, and after 2-7 its own proposal is worth 1.0-4.3% of per-property object bytes. Its premise is also wrong: shape slots admit non-default attributes, which are per-object data a shared shape cannot hold |
| 2-9 | — (found closing 2-3) | — | **Landed** — shape-tracked properties no longer live in the radix trie; it is written only when something needs a real descriptor. A three-field object is **0.36x**, an eight-field one **0.15x**, against +8 B on every object; over an Octane run **16.2 M property maps become 2.5 M**. All 22 cache rows byte-identical; test262 unchanged across all four manifests; Octane 14/15 with the fifteenth confirmed pre-existing against a control |
| 2-7 | — (found measuring 2-3) | — | **Landed** — `55c6b1fb` (the measurement) and `a6f101cc` (the policy), both in the pinned pointer. 43.9% of 47 M real maps never outgrow one four-node group; live map bytes 0.56x, allocated 0.82x, Typescript 0.92x |
| 2-5 | P0-2 remainder | 2-5 | **Closed** — measured at 0%; P0-2 had already taken the cost, and 2-1 narrowed what was left |
| 2-6 | — | 2-6 | **Folded into 4-1** — no callee resolution to cache; a call costs ~250 ns and a call-site cache removes none of it |
| 3-0 | — (found measuring 3-1) | — | **Landed, both halves** — an indexed access boxed its index. A read now allocates **0.00 B/element** against 31.67 and a write loses ~32 B; write-once-read-once goes 0.46x for a numeric element and 0.25x for a reference one. Compound assignment keeps its boxed index, on purpose |
| 3-1 | — | 3-1 | **Open, re-specified three times, now FIRST and no longer contained.** Its own measurement made it a live-memory item; 3-8's census overturned that ranking (**42.01% of the corpus's allocation is number boxes, 66.96% of NavierStokes**); and its own re-measurement showed the element chain decomposes entirely into **operator** boxes, so a typed store *alone* stays the wash it always was and the item that pays is storage **plus an unboxed element read the numeric operators can consume** — a joint Storage + Compiler **XL**. It is the precondition for every item phase 3 has landed. Its premise measurement also **built the bitwise half** (`JSNumericOperators`) and found a literal is re-boxed per evaluation (1.2% of requests, not built) |
| 3-2 | — | 3-2 | **Open, measured, and re-specified: it is a Box2D item and it goes AFTER 3-1.** Its premise sentence is wrong — `o.x = 2` allocates nothing, so `vector.x = 1.5` pays for the literal and not the slot; the slot's own 32 B appears only when the stored value is already a raw double. Field rows equal element rows to the hundredth, so 3-1 and 3-2 share one compiler half. Sized with 4-1's uncollected numeric-vs-generic signal, built here: **50.1% of cache-answered reads are numeric, 98% of them Box2D's**, against NavierStokes' **388 reads / 0 numeric / 30.0 M boxes**. **4-2b's specialized read already resolves a monomorphic read to a literal slot index**, which is most of the machinery a raw slot needs |
| 3-3 | P2-2 item 3 remainder | 3-3 | **Parameters landed; `let`/`const` and block `var` open and re-ranked ahead of them.** Measured before starting, and the item was right about the target and wrong about the tier: a parameter was excluded from the *scalar* gate, not the numeric one, so it allocated a `JSVariable` cell on every call — **56 B per parameter, a three-parameter call 230.2 → 62.2 B**. The numeric tier cannot be widened to parameters at all, because the caller picks the type; that is phase 4. All four ineligible categories cost the same per site, so the item's ordering was never a cost claim |
| 3-6 | — (found measuring 3-5) | — | **Counted and closed** — the conjunction 3-5 blamed costs 0.1% of the coverage. Splits into 3-7 and 3-8; nothing built, deliberately |
| 3-7 | — (3-6's static half) | — | **Landed** — a captured numeric local lives in the `Box<double>` the expression compiler already makes for any captured CLR local, so the "cell" the item asked for needed no code. Worth **8 names, 224 → 232**, not 3-6's predicted 290/2.4×: **247 of the 478 captured names are named by a hoisted function declaration** and are closed permanently, and 3-6's population was inferred from a subtraction with a missing term (`offered = rejected + dropped + surviving`, and `rejected` had no counter). Lifting the conjunct exposed **two wrong answers and one compile failure** that it had been masking. **63.97 → 0.01 B/iter and shape ÷ control 7.19× → 1.0000× on its shape; +32 B and 1.111× on the losing one; 1.0001× on the corpus.** Switch `BROILER_JS_CAPTURED_NUMERIC_LOCALS` |
| 3-8 | — (3-6's runtime half) | — | **Counted, and closed as written** — the mechanism is right and the target is not. Number boxing is **41.89% of the corpus's allocation** and the **whole** raw-double local tier removes **0.36% of those boxes**, because a box is minted by the operator and a local is one link in the chain. Of 1 916 drops, **76.4% take their value from a property read or a call** and only **2.5% from a parameter** — the category 3-3 deferred as the one that mattered. **Do not start; 3-1 and 3-2 move ahead of it.** Adds `NumberBoxingDiagnostics`, the `BROILER_JS_NUMERIC_LOCALS` whole-tier control, a drop-cause classifier and `NumericDropCauseTests`; also fixes a bookkeeping defect that inflated 3-7's `offered`/`rejected` |
| 3-4 | — (`tagged-js-value` in ownership.json) | 3-4 | Cost, do not start — **but its case is now the strongest in the phase, and 3-8 is why**. A tagged value removes the box at the *operator*, which is where 41.89% of the corpus's allocation is minted, rather than at one end of it; 3-7 had already given it the 247 names a hoisted declaration holds, which need a representation that can carry *uninitialized*. Still a cost rather than a task, and still behind 3-1 and 3-2, which reach the same boxes without an engine-wide redesign |
| 4-1 … 4-4 | *excluded by engine §9* | 4-1 … 4-4 | Open — superseded, see §1.1. **4-3's design is written**: the item asked for V8-style frame reconstruction, which this engine cannot express (tier-1 locals are CLR locals of an IL method, and `CallFrame` carries no JavaScript values). Re-specified as restart (shipping in the pilot) plus an in-method fallback branch |
| 5 | — | Octane §7 "regex, until late" | **Profiled — gate satisfied, phase re-specified.** `Matching/Matcher.cs` is not on the Octane path at all (only semantic-gap patterns route to it); the default engine is .NET's, built without `RegexOptions.Compiled`. B5's ranking of the closure matcher was never checked against the routing |
| Lazy frame materialization | P3 remainder | — | Candidate, not a task — no measured cost to remove |

**Status of the three source documents.**
[`Archive.md`](Archive.md) and
`tests/octane/roadmap.md` are **archives** — superseded plans
kept for what they contributed, carrying diagnoses this document has since corrected, and
**not back-ported**. Both now say so at the top. **The engine one used to be labelled only
here**, "because it is inside the submodule and this repository cannot annotate it without a
pointer bump" — an obstacle the 2026-08-07 consolidation removed by moving the plan into the
submodule beside it, and the banner is now on the file. Its name changed with the label:
it was `Broiler.JS/docs/performance-roadmap.md`. `tests/octane/benchmarks.md` is different:
it is a *reference*, not a plan, and stays live in the aggregate repository as the
per-benchmark description.

**Dropped in the merge, deliberately:** the engine roadmap's detailed defect
narratives (the `SAUint32Map<T>` sentinel, the Debug-build stack-trace-on-throw, the
six pre-existing test failures, the three frame-recycling defects) are history, not
plan. They stay in
[`Archive.md`](Archive.md),
which remains the archive of record; only their transferable lessons were lifted into
§3.5. Likewise `tests/octane/benchmarks.md` remains the per-benchmark reference —
§4.3 carries only the ranked blockers.

**A fourth source joined at the consolidation.** The optimization catalogue
([`Roadmap.md`](Roadmap.md#the-optimization-catalogue--the-design-space-this-plan-was-chosen-out-of)) is the design-space survey the campaign is a traversal of.
It contributed no items — every technique it names that this plan pursues was already an
item — but it is the record of what was *considered and not chosen*, which no other document
here holds. Its rows carry the phase or item that reached them, and the twelve it lists that
this plan has never scoped are marked as such rather than silently absent.

---

_Merged 2026-08-01 from `tests/octane/roadmap.md`, `tests/octane/benchmarks.md` and
`Broiler.JS/docs/performance-roadmap.md`; consolidated into `Broiler.JS/docs/roadmap/`
2026-08-07, which added the optimization catalogue and the component roadmap as neighbours
and moved the plan into the submodule it directs. Engine facts verified against `Broiler.JS` at
`cdb2fd41`; Octane code sites at `45f4f679`. Phase 2 worked and measured 2026-08-01/02 at
pointer `685026c0` plus the then-pending `0050`–`0058`, since applied and pinned as
`a6f101cc`; status summary in §0._
