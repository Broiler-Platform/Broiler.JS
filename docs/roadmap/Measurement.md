# Measurement, acceptance and reproduction

Everything about *measuring* this engine, in one place: what may be claimed and under
which conditions (the gate), the campaign's own protocol and conformance gates (§3), the
standing lessons the campaign has learned about measuring (§3.5), and the command line for
every probe that produced a number anywhere in this directory (Appendix A).

> The normative measurement document in [`docs/roadmap/`](README.md).
> [`Roadmap.md`](Roadmap.md) is the campaign crosswalk and quotes measurements throughout;
> [`Modernization.md`](Modernization.md) owns cross-track execution order. **None of the
> quoted historic timings are claimable under the rules below**, and both roadmaps say so.
>
> **Section numbers are the roadmap's**, unchanged, so an existing reference to §3.1 or
> §3.5 still resolves — it resolves here. §0, §1, §2 and §4 are in
> [`Roadmap.md`](Roadmap.md).
>
> Consolidated 2026-08-07 from four files: `Broiler.JS/docs/performance.md` (the gate),
> and the campaign's `protocol.md`, `lessons.md` and `appendix-a-reproducing.md` (§3 and
> Appendix A). The historical evidence and lessons remain intact. The evergreen gate at
> the top of this file has since been made fail-closed; where historical campaign wording
> conflicts with it, this gate governs.

---

## The gate — what may be claimed at all

Performance changes are accepted only with repeatable measurements and the semantic
tests that own the optimized path. Machine-specific output belongs under the ignored
`artifacts/performance/` directory, not in Markdown result logs.

### Evidence classes

Every result bundle used for a roadmap or release decision declares exactly one evidence
class:

- **Smoke** proves that the harness, workload and artifact path work. Developer machines
  and GitHub-hosted runners may produce smoke evidence. Smoke never accepts or rejects a
  performance change, even when all repetitions happen to be close.
- **Prioritization** sizes a population, mechanism or attainable ceiling. It may guide the
  next experiment but cannot close a performance item.
- **Acceptance** compares a candidate with an immutable control on a declared controlled
  lane and passes every rule below. Only acceptance evidence supports a performance or
  resource claim.
- **Diagnostic** explains an effect using tracing, sampling, counters or disassembly. A
  diagnostic run is not a primary timing run unless the instrument's overhead and output
  fidelity were independently validated for that workload.

Conformance, API and architecture results are not downgraded merely because the machine is
unsuitable for timing. A deterministic correctness failure remains a failure.

### Configuration

- Jobs: [`eng/performance/phase0.json`](../../eng/performance/phase0.json)
- Result schema:
  [`eng/performance/schemas/phase0-result.schema.json`](../../eng/performance/schemas/phase0-result.schema.json)
- Benchmark/test owners:
  [`eng/performance/ownership.json`](../../eng/performance/ownership.json)

The current `smoke` profile uses a broad 20% wiring guard. The current `baseline` profile
uses a provisional 7.5% repeatability guard and fresh-process lifecycle samples. Neither
configured percentage is a candidate acceptance threshold.

### Collect evidence

Developer wiring check:

```powershell
python scripts/performance/collect_phase0.py --profile smoke
```

One-arm controlled-host collection:

```powershell
dotnet tool install --tool-path artifacts/tools dotnet-trace
$env:PATH = "$(Resolve-Path artifacts/tools)$([IO.Path]::PathSeparator)$env:PATH"
python scripts/performance/collect_phase0.py `
  --profile baseline `
  --include-eventpipe `
  --include-build-baselines `
  --include-publish `
  --rid win-x64
```

This command collects one source tree and its same-tree repetitions. It is an input to an
acceptance comparison, not a candidate/control decision by itself. In particular, the
current `--enforce-noise` option is only a repeatability wiring guard until it enforces the
exact-row, all-repetition and all-applicable-metric rules below. Its exit code alone cannot
accept a change.

Selected IL/JIT disassembly:

```powershell
python scripts/performance/collect_phase0.py --profile disassembly
```

The collector records commit/dirty state, commands, runtime, OS/RID, processor, GC and
tiering settings, lifecycle samples, BenchmarkDotNet results, package graph, managed
assembly sizes, and optional publish results. An acceptance wrapper must add the complete
source and dependency identity described below. Retain the raw BenchmarkDotNet, EventPipe,
binary-log, IL, publish, semantic and comparison artifacts with release evidence.

### Acceptance manifest and source identity

Candidate and control are reproducible build inputs, not just two labels. Before either arm
runs, write one immutable manifest containing:

- candidate and control commits, recursive submodule commits, and the benchmark/harness
  commit;
- a clean-worktree assertion, or a checksummed patch plus a manifest of untracked inputs;
- resolved package/dependency versions, SDK/runtime identity and relevant generated-source
  hashes;
- configuration, target framework, RID, publish properties, bootstrap profile, compiler
  backend, tiered-compilation/PGO/ReadyToRun settings, GC mode and CPU-feature policy;
- the pinned corpus revision, exact inclusion/exclusion manifest, and expected result-row
  identities/metrics; and
- the primary metric, direction, minimum relevant effect or equivalence budget, paired
  analysis, resource/semantic guardrails and their precedence.

A dirty boolean without the corresponding content is not reproducible evidence. A mutable
corpus ref such as `master` or `main` is not an acceptance identity. The comparator rejects
arms whose compatibility fields differ unless that field is the single factor deliberately
under test.

### Stability, resolution and the candidate decision

These quantities must not be used as synonyms:

| Quantity | Meaning | What it decides |
|---|---|---|
| **A/A stability envelope** | normal movement of the same build on one lane, calibrated per workload and metric | whether the lane/run is usable |
| **measurement resolution** | the uncertainty of the paired candidate/control estimate with the predeclared sample plan | whether the observed effect is distinguishable |
| **minimum relevant effect / equivalence budget** | the smallest product-relevant change worth accepting or the largest regression considered equivalent | accept, reject, equivalent, or below-resolution |
| **guardrail budget** | maximum allowed correctness, memory, GC, tail-latency, size or compatibility cost | vetoes an otherwise favourable primary metric |

The `baseline` profile's current 7.5% value is a provisional harness repeatability guard,
not a universal regression threshold. Stability is calibrated on the controlled lane from
same-build A/A runs and may differ by workload and metric. It is never widened after seeing
a candidate. Recalibration is a separate control-only change with retained evidence.

For an acceptance comparison:

1. run candidate and control on the same controlled host in a balanced, process-isolated
   order such as ABBA/BABA; use enough independent pairs for the predeclared analysis and
   repeat a provisionally qualifying result in a fresh confirmation run before acceptance;
2. use either a same-binary switch whose disabled arm is proven to have no relevant overhead,
   or isolated candidate/control builds produced by one harness/toolchain; record which
   design was used and keep a null control that can invalidate the comparison;
3. compare the exact expected row set. A missing, duplicate, failed, renamed or incomparable
   row invalidates the comparison; intersections and partial aggregates never pass;
4. include every configured repetition in the analysis. Two repetitions are the current
   wiring minimum, not proof that a lane or effect is decision-grade;
5. keep cold fresh-process lifecycle results separate from warmed microbenchmarks and keep
   cached and uncached results separate;
6. report the primary metric with its paired uncertainty and report applicable allocation,
   GC, working set/RSS, committed/virtual memory, thread count, tail latency, code/package
   size and publish metrics as guardrails; and
7. return exactly one predeclared decision: `accept`, `reject`, `equivalent`,
   `below-resolution`, or `invalid-run`. `Equivalent` requires the complete paired interval
   to remain inside the predeclared equivalence budget; an interval crossing the applicable
   superiority/non-inferiority/equivalence boundary is `below-resolution`. Lane instability
   produces `invalid-run`, not a wider candidate budget.

### Controlled lanes and effective configuration

The release certification matrix is Windows x64, Linux x64 and Linux Arm64. A narrower claim
names only the lanes and product profiles actually run; it must not imply unrun coverage.
SIMD/intrinsic claims additionally require x64 with each relevant feature enabled and
disabled, plus an AdvSimd-capable Arm64 host. Applicable workstation and server GC arms are
run separately.

Requested configuration is insufficient. Each measured child reports the effective RID,
process architecture, GC mode, bootstrap profile, tiering/PGO/ReadyToRun state and relevant
hardware-intrinsic support. The arm fails when requested and effective values disagree.
Record CPU model, microcode, logical/physical core topology, memory, OS, power/governor and
thermal policy with the lane. GitHub-hosted runners remain smoke lanes, not controlled
acceptance hardware.

### Semantic and compatibility bundle

Every candidate decision resolves its work-item ID through
[`eng/performance/ownership.json`](../../eng/performance/ownership.json), then runs the named
semantic owner and focused test262 manifest. Candidate and control results, exact suite pin,
manifest contents and row-level differences are stored with the comparison. Missing ownership,
an unrun manifest, a new failure/timeout, or an unexplained bucket movement prevents acceptance.
Package/startup changes also carry the declared bootstrap surface, public/API/package checks
and pristine consumer result. Faster initialization with missing globals is a regression.

### Observer effects

Profiled and traced runs are separate matched diagnostic arms. Their elapsed time is not used
as the primary candidate metric. Measure instrument overhead with an uninstrumented control,
and verify that the instrument can name the code or event being attributed before using its
largest row. Prefer exact low-overhead runtime counters when they answer the question. The
collector's EventPipe scenarios already run in separate child processes; preserve that
separation.

### Evidence retention

Store schema-versioned, checksummed summaries and raw BenchmarkDotNet, lifecycle, EventPipe,
build, publish, conformance and comparison artifacts in the durable release evidence store.
Short-lived CI artifacts are a transport/cache, not the evidence of record. A published
summary must be regenerable from the retained raw bundle and immutable manifest.

### Bootstrap profiles

`JavaScriptBootstrap` and `JavaScriptContextBuilder` accept a
`JavaScriptBootstrapProfile`. Three standard profiles are provided:

- `Full`: the supported full surface with lazy Intl/Temporal realization;
- `FullEager`: the comparison/compatibility profile that realizes the full surface
  eagerly; and
- `Minimal`: a deliberately reduced, non-conformant host surface.

Hosts should select a profile explicitly. A smaller package or faster context is not a
conformance win if required globals are absent.

This is a **JavaScript bootstrap profile**. Elsewhere in the platform `JavaScript` and
`WebAssembly` identify bytecode languages executed by the separate Broiler.VM component, while
`execution-only`, `narrow-runtime-compiler`, and `general-runtime-compiler` identify deployment/
compiler compositions. Record all applicable dimensions separately; a `Full` bootstrap result
does not by itself say which executor or compiler closure ran.

### Experimental execution modes

Function tiering is disabled unless the host supplies enabled tiering options. It is
bounded per realm and must retain the original delegate as the semantic fallback.

`Broiler.JavaScript.Portable` is a separate numeric bytecode/interpreter capability for
offline compilation and Native AOT. It supports numeric parameters/locals, arithmetic,
comparisons, assignment, blocks, `if`, `while`, counted `for`, and value returns. It does
not implement the JavaScript object model, strings, properties, arrays, calls, closures,
exceptions, modules, async/generators, host callbacks, `eval`, or runtime compilation.
Do not describe it as Native AOT support for the full engine.

A Broiler.VM JavaScript profile is a separate component with its own roadmap and evidence
ledger, and is not planned here. Until its own correctness and publish-and-run gates
pass, `Portable` measurements remain numeric-seed evidence. WebAssembly-profile measurements are
not JavaScript controls and belong in the Broiler.VM evidence set.


---

## 3. Measurement and acceptance protocol

### 3.1 What may be claimed

The evergreen gate above is the single normative acceptance protocol; this campaign section
does not duplicate it. A result is claimable only when its immutable bundle records the
`accept` decision produced by that protocol. A same-tree run inside 7.5%, a BenchmarkDotNet
ratio, or a committed Octane score is not sufficient by itself.

For the Octane half of that matrix the Octane Benchmarks workflow
(`.github/workflows/octane-benchmarks.yml`, in the parent repository) takes a
`platform` input — one RID, or `all` to fan out to a job per RID on `ubuntu-latest`,
`windows-latest` and `ubuntu-24.04-arm`. Each writes and commits its own
`tests/octane/results/<platform>/`, because a score off one machine says nothing
about another; a locally driven run picks the same directory up from
`--platform`, defaulting to the host's own RID. That is the *smoke harness* covering the
matrix, not acceptance of the matrix. GitHub-hosted results provide coverage, continuity
and A/A observations; they do not become acceptance evidence merely because all rows fall
inside the configured harness band.

> **Standing caveat on every number in §4.** The engine campaign's figures come from
> an ad-hoc in-process harness on a shared 4-core container with 10–15% run-to-run
> variance, reporting the slower of two runs. Allocation counts are deterministic and
> exact; timings are for **prioritization only**. Not one of them has been through
> the gates above.

### 3.2 Running the Octane harness

```bash
./scripts/run-octane-benchmarks.sh --repetitions 3
```

A single run tells you whether a suite completes; it does **not** tell you whether a
score moved — run-to-run variance is comfortably larger than most changes worth
making. With `--repetitions n` the harness reports the **median** per benchmark plus
the observed spread `(max − min) / median`, flagging `⚠` anything outside
`--noise-band` (default 7.5%, matching the provisional `baseline` harness guard).
That flag classifies same-run repeatability; it is not an acceptance threshold for a
candidate delta.

Three properties of that design are load-bearing:

- **A default run is unchanged byte for byte.** One repetition ⇒ the median is the
  sample, no stability data, no spread column.
- **Each repetition keeps its own log** (`<suite>.rep1.log`, …), so a flake keeps the
  evidence of the run that failed.
- **A suite is `ok` only if it was `ok` every time.** Mixed verdicts report `flaky`,
  never an average. Averaging a flake into a pass is the failure mode the harness
  exists to prevent.

Expect the two latency scores to be among the unstable rows, and treat a wide three-sample
spread as a signal to investigate. It is not a measured pause distribution. Attribute it to
pauses only after per-invocation tail-latency and GC/pause diagnostics support that claim.

**Per-suite budgets.** `--timeout` (default 180 s) is a **floor**; a suite that needs
longer raises its own via `timeoutSec` in
`scripts/octane-suites.json` — currently Mandreel
(1200 s, measured 313 s) and zlib (1800 s, measured 647 s). Before this, CI was
overriding the global timeout to 1800 s, which meant a genuine hang anywhere else had
thirty minutes to look like work.

**Isolation.** One fresh process or page per suite, driven by the manifest. Broiler is
experimental — a suite may score, throw, hang, or abort the process — and isolation
means one bad suite never discards the other sixteen. Failures are classified
`ok` / `error` / `timeout` / `crash` / `flaky`, with full evidence in
`tests/octane/results/<platform>/diagnostics.md`.

Harness parsing is covered by a test that needs no engine, checkout, or network:

```bash
node tests/octane/harness-selftest.mjs
```

### 3.3 Running the engine probes

Run from the `Broiler.JS` submodule root:

```powershell
python scripts/performance/collect_phase0.py --profile baseline --include-eventpipe --include-build-baselines --include-publish --rid win-x64
```

The collector records commit/dirty state, commands, runtime, OS/RID, processor, GC and
tiering settings, lifecycle samples, BenchmarkDotNet results, package graph, managed
assembly sizes, and optional publish results. Machine-specific output belongs under the
ignored `Broiler.JS/artifacts/performance/`, never in a Markdown result log. Retain the
raw BenchmarkDotNet, EventPipe, binary-log, IL and publish artifacts with release
evidence. The probe corpus itself is Appendix A.

**Bootstrap profile matters to any startup number.** `JavaScriptBootstrap` and
`JavaScriptContextBuilder` take a `JavaScriptBootstrapProfile` — `Full` (lazy
Intl/Temporal realization), `FullEager` (the comparison/compatibility profile), or
`Minimal` (deliberately reduced and non-conformant). Say which one a measurement used:
a smaller package or faster context is not a win if required globals are absent.
Also record the executor, deployment/compiler composition,
VM profile/format/feature versions, and whether the path compiled source, loaded a verified
artifact, or hit a persisted cache. Do not overload the bootstrap-profile field with them.

### 3.4 Conformance gates

The pinned manifests are `test262-arrays`, `test262-properties-proxy`,
`test262-strict-mode`, `test262-realm-isolation`, and — added 2026-08-03, see below —
`test262-lexical-declarations`. First taken 2026-08-01 at `cdb2fd41`
(suite ref `ccaac100`), **re-run 2026-08-02 at `a6f101cc` plus 2-9 with every count
unchanged**, **re-run at `71dda1b7` plus 3-3 with every count unchanged**, and **re-run five
times on 2026-08-03 on linux-x64 at `9bf9639b` (the pin at the time) — plus `patches/0067`, plus `0067` and
`0068`, plus `0067`–`0069`, plus `0067`–`0070`, and plus all five of `0067`–`0071` — with every count
identical every time, manifest by manifest** — so the table below describes the pinned pointer as well as the commit it was first
measured at.

**Re-run 2026-08-05 on linux-x64 at `cca39b4d` (the pin at the time) plus item 3-1's order-preserving guard
placement, on both settings of its switch. On the shipping arm every count is identical to the row
below, manifest by manifest — 8 710 executed, 8 617 passed, 84 failed, 251 skipped, 9 timed out.**
This is the run that most needed taking of anything in phase 3, because the change *removes an
eligibility rule whose entire justification is observable evaluation order* — a lost `valueOf`
call, a coercion that stops running, or a throw arriving from the wrong operand would surface here
rather than in a box count. So the arms were compared **file by file** and not only by total, which
is what makes the next paragraph readable at all.

**One test moved between two non-passing buckets on the control arm, and it is worth stating
exactly rather than as "identical".** `test262-arrays` reads **17 failed / 9 timed out** on the
ordered arm — the recorded row — and **18 / 8** on the hoisting one, because
`built-ins/Array/prototype/toReversed/length-exceeding-array-length-limit.js` was killed by the
30 s timeout in one and reported as a failure in the other, with empty stderr both times. **The set
of 26 non-passing files is the same on both arms**, and that file is already tracked in
`Broiler.JS/scripts/compliance/test262-failures.txt` as one of the nine integer-limit cases CI has
carried for a while. The other four manifests agree **file for file** on both arms (38, 26, 3 and
0 non-passing). So: no test passes on one arm and fails on the other, and what moved is which side
of a wall-clock boundary a known-failing test landed on under `--max-workers 4`. *It is recorded
because a total that reads 84 against 85 would otherwise look like a regression, and because
"identical" would have been the easy and wrong word.*

`--max-workers 4`; the suite came from a `git fetch --depth 1` of the pinned `ccaac100` passed
through `--suite-root`, for the reason recorded below, and the runner's own *"Selected 3 160
runnable test(s)"* for `arrays` is what says it is the same corpus.

**Re-run 2026-08-04 on linux-x64 at `07adeb44` plus `patches/0082` (item 1-1's remaining half) —
now `0aa8a558`, an ancestor of the pin, so this run describes the pinned tree rather than a local
build: every count is identical to the row below, manifest by manifest — 8 710 executed,
8 617 passed, 84 failed, 251 skipped, 9 timed out over all five.** The failures and timeouts are
the same *files*, not merely the same totals: all **84** failures need `$262` — including the 13
`language/global-code/script-decl-*` cases, every one of which includes it — and the **9** timeouts
are lines 7–15 of `test262-failures.txt`, nine for nine. The manifests that matter here are
`strict-mode` and `lexical-declarations` rather than `arrays`, because what `0082` removes is a
repeat of the walk that decides *which bindings a nested function captures*, and a lost capture
would surface as a scoping failure rather than an arithmetic one.

**The suite came from a git checkout at the pinned ref rather than from the runner's own download,
and that is a harness change worth recording.** `codeload.github.com` and `api.github.com` both
return **403** through this session's proxy, so `run_test262.py`'s `ensure_local_suite_root` cannot
fetch at all; `git fetch --depth 1 origin ccaac100…` against `github.com` succeeds, and
`--suite-root` takes the resulting checkout. What says this is the same corpus rather than a
smaller one is the runner's own selection count printed before it runs anything — **"Selected 3 160
runnable test(s)"** for `arrays`, which is the executed count in the row below to the test, and the
same for the other four.

**Re-run 2026-08-04 on linux-x64 at `61c8cc65` (the pin at the time), plus `patches/0078` (item 3-7), plus
`0078`–`0079` (item 3-8), plus `0078`–`0080` (item 3-1) and plus `0078`–`0081` (item 3-2): every
count is identical to the row below, manifest by manifest, on every arm.** The `0080` run matters most of the five, because that
patch changes what six core operators *emit* — `&`, `|`, `^`, `<<`, `>>`, `>>>` — and
`test262-arrays` is thick with `ToUint32` edge cases. All five manifests were run on 3-7's switch-ON arm — the shipping configuration — and
all five again with `BROILER_JS_CAPTURED_NUMERIC_LOCALS=0`; `properties-proxy` was then run a third
time at `0078`–`0079` with nothing else building, and a fourth on a **pristine build of the pin**
as a control. The last two agree **file for file** on which 38 fail, which is what makes this a
control rather than a matching total.

**One run of `properties-proxy` on the switch-ON arm came back 3 949 / 39, and the extra failure
was mine, not the engine's.** The stderr the runner captured says so outright: *"The JavaScript
compiler is not available. Reference the Broiler.JavaScript.Compiler assembly to enable script
compilation."* That child process had loaded `Broiler.JavaScript.Compiler.dll` **while a
`dotnet build` of the same solution was rewriting it** — a build I started for an unrelated edit
while the manifest was still running. It is not a `$262` case, it is not an assertion failure, and
`built-ins/Object/getOwnPropertyDescriptor/15.2.3.3-3-4.js` passes three times for three when run
alone on the widened build, and answers correctly on the widened build, on the same build with the
switch off, and on a pristine build of the pin. The manifest-level controls settle it: **a pristine
build of `61c8cc65`, the switch-off arm, and a re-run at `0078`–`0079` with nothing else building
all report 3 950 / 38 and agree file for file on which 38 fail** — so the 39th file is in none of
the three and is not a property of any change here.

> *This is §3.5's "check that the thing you measured is the thing you built", arriving from the
> other side: there the binary under test was older than the source, here it was being rewritten
> underneath a running suite.* **Do not build while a suite is running against the output.** The
> first diagnosis was "a flake under `--max-workers 8`" — plausible, consistent with the test
> passing three times in isolation, and wrong; what settled it was reading the captured stderr
> instead of re-running until it went away. A failure that reproduces nowhere is not thereby a
> flake, and the runner had recorded the real reason all along.

**Re-run 2026-08-07 on linux-x64 at `e5dc2610` (the pin) plus `patches/0115` (phase 5's item 2),
with `BROILER_JS_REGEX_TIERING=1` — the arm where the mechanism actually fires. Every count is
identical to the row below, manifest by manifest: 8 710 executed, 8 617 passed, 84 failed, 251
skipped, 9 timed out.** This is the manifest set that matters for that item, because `0115`
changes which `Regex` object serves a hot pattern and `properties-proxy` is thick with
`RegExp.prototype` receiver and descriptor cases — a promotion that altered a capture layout or a
`lastIndex` progression would surface there rather than in a benchmark. **The failing set is
checked as files rather than as totals**: all **84** failures need `$262`, verified by reading
each one's source rather than by matching a count, and the **9** timeouts are nine of nine the
integer-limit cases already tracked in `Broiler.JS/scripts/compliance/test262-failures.txt`. The
suite came from a `git fetch --depth 1` of the pinned `ccaac100` passed through `--suite-root`,
and the runner's own *"Selected 3 160 runnable test(s)"* for `arrays` is what says it is the same
corpus. Nothing else was building while it ran, for the reason recorded above.

| Manifest | Executed | Passed | Failed | Skipped | Timed out | Engine failures |
|---|---:|---:|---:|---:|---:|---:|
| `test262-arrays` | 3 160 | 3 134 | 17 | 0 | 9 | **0** |
| `test262-properties-proxy` | 3 988 | 3 950 | 38 | 13 | 0 | **0** |
| `test262-strict-mode` | 1 066 | 1 040 | 26 | 27 | 0 | **0** |
| `test262-realm-isolation` | 99 | 96 | 3 | 4 | 0 | **0** |
| | **8 313** | **8 220** | **84** | **44** | **9** | **0** |
| **`test262-lexical-declarations`** *(new)* | **397** | **397** | **0** | 207 | 0 | **0** |

Every one of the 84 failures needs `$262` (`createRealm`, `detachArrayBuffer`, or a
harness include that uses one), which the raw script host does not provide. All 9
timeouts are already tracked in `Broiler.JS/scripts/compliance/test262-failures.txt` —
lines 7–15, nine for nine, the integer-limit `slice`/`unshift`/`reduceRight`/
`toReversed` cases CI has carried for a while.

**`test262-lexical-declarations` is new, and it closes a gap rather than reporting one.**
Item 3-3's `let`/`const` half changes how lexical bindings are *compiled*, and **no pinned
manifest covered `let` or `const` at all** — `test262-language-basics` is twelve entries about
`throw`, commas and relational operators. The manifest is
`language/statements/{let,const,variable}` plus `language/block-scope`, and it was run **six
times from the same tree**: at `9bf9639b` (the pin at the time), and at that commit plus each successive
prefix of `patches/0067`–`0071`. **Identical, 397 of 397 passing on each.** So it did not
*detect* anything — its value is that a future regression on those paths now fails a pinned gate
instead of passing unnoticed, and `language/statements/variable` is exactly what `0068` touches.
The 207 skips are the negative-syntax and module cases the runner excludes by design, not silent
failures.

**Still not covered:** the Annex B forbidden-extension paths that P0-3 gates on
(`test/annexB/built-ins/Function`, `forbidden-ext/b2`) are in no manifest. Adding them
changes what CI enforces, so it is an open item rather than a silent edit.

> **Check out the suite with `core.autocrlf=false` on Windows, or `strict-mode` reports 27
> failures instead of 26.** Git's Windows default rewrites every LF to CRLF on checkout, and
> `built-ins/Function/prototype/toString/line-terminator-normalisation-LF.js` asserts that a
> function containing an LF round-trips through `toString` as an LF — so converting the *test
> file* makes it assert the opposite of its name. All 37 of its lines arrive as CRLF and it
> fails; its `CR` and `CR-LF` siblings are unaffected, which is why the damage is one test and
> not a family. Found while running 3-3, where the one-count difference from the recorded row
> was the only thing standing between "unchanged" and a claim that would have been wrong.
>
> **This is the third time the same root cause has produced a fake engine failure**, after the
> two §3.4 tooling defects below, and it is worth naming the general form: *a test whose subject
> is its own bytes cannot survive any layer that normalizes bytes* — the harness writing the
> assembled script (fixed), and now the checkout that supplies the file. `git cat-file -p HEAD:<path>`
> is the check, because it prints the blob rather than the working copy; re-checking out will not
> fix it once the index has recorded the translated form.


### 3.5 Standing measurement lessons

These were paid for once each. They apply to every phase below.

- **A bound taken at the wrong sites cannot be used to rank, not just to decide.** Item 3-1's
  consumer-side bound predicted `CallResult` at 9.41 and `PropertyRead` at 1.83 — five times apart,
  in that order. Measured on the mechanism itself they are **52.5 and 2.12**, the reverse and
  twenty-five times apart. `0110` established the bound was on a different quantity; `0112` shows
  that quantity does not preserve the ordering either. *A conservative-sounding bound is only
  conservative about the sites it covers; treat its ranking as unfounded until something measures
  the mechanism.*
- **Count the cost the mechanism actually pays, before building the mechanism.** Four attempts at a
  dual-representation local — 3-8a, `0109`, `0110`, `0111` — lost to the same quantity: the boxes
  minted reading the local. The first three measured it after building; the fourth measured it first
  and refused in one run. *The counter that decides a representation is the one on the representation
  itself, so the cheapest honest test is to build it for a candidate population behind a flag and
  read that counter — not to bound the cost from outside, which `0108` showed lands on a different
  quantity.*
- **A lower bound taken at the wrong sites is a bound on a different quantity.** Item 3-1's cost
  side was counted at five consumer positions and called a lower bound; it came out 25× under,
  because the cost is a box at the local's **own read expression** and those five are not where that
  happens. Being a bound protects against under-counting the sites you chose, and against nothing
  else. *Before quoting a bound, say which sites it covers and what share of the mechanism they are.*
- **A refusal census attributes a name to its first cause; removing that cause admits it only if it
  was the only blocker.** `0106` ranked `ElementRead` at 6.9 M boxed writes and the widening built
  for it collected 2.4 M, because `var t = a[0] * b + i` is charged to the element read and stays
  refused for the parameter. The census was right about what it measured and says nothing about what
  fixing it would admit — those are different questions and only one of them was asked.
- **A cost/benefit ratio prices an outcome; it does not establish that a mechanism reaches it.**
  Item 3-1's widening was selected on a measured cost/saving of 0.04 — the best number the phase had
  produced — and built, and it regressed at 1.061× because **868 of the 18.7 M boxes it was selected
  to remove were actually removed**. The saving lived at the tree's ROOT store, and the
  representation being widened into had raw arms for the leaf, the element read and the element
  write and none for the root. *Both the ratio and the build were correct about what they measured;
  nothing had checked that the two met.* Before building to a measured opportunity, name the emission
  site the saving lives at and confirm the mechanism has an arm there.
- **An instrument that changes its own population should be assumed broken before it is assumed
  biased.** Item 3-1's read-side counter wrapped a local's read expression in a counting call and
  the population it measured fell to **0.169×**, Gameboy's to zero. That reads exactly like a
  perturbing instrument — the kind this campaign has corrected for before — and the first fix
  assumed it was one. It was not: `variable.Expression` is *also* the assignment target, so `x++`
  compiled to an assignment whose target was a method call and the IL backend refused it. **The
  suites were crashing, and 0.169× was the share that still compiled.** A bias can be argued about,
  bounded, or quoted with a caveat; a crash cannot, and the log line said so on the first run.
  *Check that the arm ran before deciding what its numbers mean.*
- **A counter that names a category cannot rank its members, and three sections ranked them anyway.**
  The boxing census split its requests into *literal*, *conversion* and *what the operators mint*,
  and "conversion" was then used for three phases as though it named a producer. It does not: it is
  one factory entry that **21** compiler emission sites call. Attributing them (`0103`) found
  **61.8% of the corpus's conversions are a single one of the 21** — the guarded tree's root — and
  that the fallback arm the mechanism was suspected of leaking to is **226 of 69.3 M**. Both facts
  were unavailable from the total, and one of them retires a suspicion the phase had carried for
  three sections. *Before ranking a population, check that the counter can tell its members apart —
  a share computed over a category is a share of an unknown mixture.*
- **A struct copy in the source is not a struct copy in the code.** `0111` priced one 56-byte
  `Arguments` copy at 8.19 ns in a replica and argued — correctly — that a replica is legitimate
  here *because a struct copy has no inside*. Removing two such copies from the engine then bought
  **1.83 ns, not 16** (`0104`), because the JIT constructs in place when it can see the destination
  and had already elided most of the return-by-value traffic. The replica's **ratio** was sound and
  its **absolute** was not a count of anything the machine does. *A replica prices a mechanism's
  shape; only the engine can say how many times that shape survives compilation.*
- **A corpus that cannot be resumed past its worst member is a corpus that is never completed.**
  §4.2a fixed the census losing eight suites when the ninth aborted, by checkpointing after every
  suite — and that fix retains the rows *before* the abort while still losing every row *after* it.
  The suites run in one process in a fixed order, so Mandreel taking the process down had silently
  cost Gameboy, Typescript, Box2D, zlib and CodeLoad in the widened run too, which is why §4.2a
  reports twelve suites and not fifteen. Making the suite list selectable (`0103`) got the last
  three, and Gameboy — the suite §4.2a's own headline rests on — was among the ones a second abort
  had been quietly dropping. *Checkpointing answers "what did we keep"; it does not answer "what did
  we never reach", and only the second question finds a missing suite.*
- **Measuring an item's premise is how you find the item next to it.** 1-1's premise —
  "most front-end cost is function bodies" — needed a control, so one was built: the same
  source with every body replaced by `{}`. Five corpora agreed with the premise. The sixth,
  Mandreel, took **17.7 s with every body already removed**, and that residue was 1-4: an
  emitter quadratic in a scope's binding count, worth 3.04× on the *compile* of the suite 1-1
  was written around — though running that suite later showed its two scores do not measure
  compilation at all. Nobody was looking for it, and no probe could have shown it — a one-liner has one
  binding, and a quadratic needs width to be visible. *A control built to size one item
  measures everything that item is not, which is the only place a cost nobody has named can
  show up.*
- **A benchmark's name is not its contents — read what it times before aiming a phase at it.**
  Phase 1 was aimed at MandreelLatency, the worst score in the suite, on the strength of the
  word *latency* and a 5 MB machine-generated file. Octane compiles that file at script load
  and starts the timer afterwards; `MandreelLatency` is the RMS of pauses between 20 render
  frames over already-compiled code. Making the compile **3.04× faster moved it 0.992×** — and
  the saving is genuinely there, in the suite's wall clock (358.2 → 350.0 s), where no score
  looks. The same reading error, smaller, put CodeLoad at 100% compilation when it is ~27%.
  *Twenty lines of the benchmark's own source would have said so at any point in the last three
  phases, and nobody opened it.*
- **Two arms, three samples each, is a coin toss dressed as a measurement.** 1-1's CodeLoad
  run separated cleanly on its first pair — 94.3 eager against 105 deferred, no overlap — and
  then failed to separate at all on the reversed pair, 99.2 against 99.4. Both pairs were three
  repetitions an arm, on a suite whose own declared noise band is 7.5%, chasing an effect near
  10%. Twenty-four samples an arm settled it at 1.099× with 93% pairwise dominance, but *either
  early pair would have been reported as the answer*, and they disagreed. **Interleaving is not
  enough when the effect and the noise are the same size — the sample count has to grow until
  the arms separate by rank, not by median.**
- **"Big input is slow" is a description, not a diagnosis, and it hides the exponent.**
  B4 said machine-generated code is expensive to compile, which was true, and everyone read
  it as being about size. It was about *width*: 2 000 bindings in one scope cost 4× what
  1 000 did, while parse and tree construction stayed flat. The tell was available from the
  start — Mandreel's ratio to Box2D was far worse than their size ratio — and it reads as
  "Mandreel is enormous" until someone divides. *Before accepting that a large input is
  slow because it is large, halve it and check that the cost halves.*
- **A premise is not a finding.** P3 blamed the five `using` scopes around every call,
  built the fast path, measured it, and found no signal — the scopes never allocated.
  The real cost was an 80-byte activation record they were hiding. *Measure before
  implementing, and be willing to throw the implementation away.*
- **An acceptance criterion is a claim too — run it before the work.** 1-2's was "a
  generated 200k-line single-function script compiles without overflow", and it passed on
  the untouched tree: the item had inherited *size* from the line count of the function it
  was found on, when the cause was *nesting*. A criterion that passes before the change
  measures nothing and hides the real one. *Write the failing case first, and check that it
  fails.*
- **Check that the thing you measured is the thing you built.** `Broiler.JavaScript.csproj` — the
  `--script-host` shell every Octane and test262 run executes — was **not a member of
  `Broiler.JS.slnx`**, so `dotnet build Broiler.JS.slnx` left its output untouched. A full day of
  shell-driven verification therefore ran a binary from the previous session, and test262 came
  back "identical" for the trivial reason that both sides were the same executable. The unit
  suites and the `--object-alloc` / `--cache-metrics` emitters were unaffected, because those
  projects *are* in the solution — which is exactly why the discrepancy was invisible. Two
  things follow. The project is now in the solution, so a solution build refreshes the shell.
  And a run that cannot fail is not evidence: **assert something that is only true of the build
  under test before trusting a suite of it.** Here that is one command — deeply nested source
  with `BROILER_JS_COMPILE_STACK_BYTES=0` completes only with 1-2's guard present, and aborts
  without it. *The tell was there and was read as a puzzle rather than a signal: a guard that
  will not fire at a 100 KB threshold is not a subtle bug.*
- **Reproduce on the platform you will close on.** 1-2's repro was a win-x64 Octane run.
  The same suite completes on linux-x64 at the same pointer, so the CI run that was meant
  to confirm it never could. *A one-platform repro dates the item to that platform.* **And so
  does a one-platform verification matrix** — 1-2's own four-way table records "mitigation off /
  guard on completes", which is true on linux-x64 and false on win-x64, for the reason in the
  next bullet. The rule was applied to the item's repro and not to its proof.
- **A threshold larger than the resource it guards is not a guard, and it fails silently.**
  `StackSegment` segments a recursive walk after 4 MiB of stack. With the compilation mitigation
  disabled, the front end compiles in place on a win-x64 stack that measures **1 052 048 bytes** —
  so the threshold can never be reached, the guard never fires once, and the process aborts
  looking exactly as it would with no guard at all. The struct's own remarks predicted the
  shape of this ("a walk cannot know how large the stack it is standing on actually is"); what
  was missing is that the condition is reachable on a shipping platform rather than hypothetical.
  *An absolute limit on a resource whose size you cannot query is unfireable in precisely the
  cases you wrote it for — probe what is left (`RuntimeHelpers.TryEnsureSufficientExecutionStack`)
  instead of assuming how much there was.*
- **A formula's stated intent is not its behaviour.** 2-7 read the property map's 16-node floor as
  "buying amortized growth for medium objects with memory small objects do not use", and sized two
  alternatives against that reading. The rounding it describes only applies while
  `last * 2 <= max`, so past the first block the rule grew **linearly** and paid *more* copies than
  doubling. The floor bought nothing for medium objects; it overcharged small ones, and the
  replacement won on memory *and* time — including on the suite whose objects were supposed to be
  the reason for keeping it. *Trace the branch with real numbers before describing what a policy is
  for.*
- **A benchmark named as an item's justification is a test that item has to pass.** 2-8 existed
  because of DeltaBlue's 601× score, was measured with a loop written to look like DeltaBlue's hot
  path, and **broke DeltaBlue** — the real suite threw before scoring. The loop reproduced the
  *reads* the item was about and none of the *writes* the item's change also affected, so it could
  not have failed. Octane was available, takes minutes, and no test in 7 347 caught it. *A
  resemblance to a benchmark is not evidence about that benchmark; run the thing you named.*
- **A conservative bug passes its own tests.** 2-0 invalidated too much, never too little,
  so every staleness test in `PropertyShapeCacheTests` was green — and green for a reason
  none of them was checking. A correctness suite cannot find an
  over-invalidation, only a hit-rate counter can, which is why 0-9's emitter found in one
  run what twenty tests had been sitting on. *Fixing an over-invalidation invalidates the
  tests that covered it: re-check each path in the condition the fix creates.*
- **Verify a premise before building on it, and separate it from its explanation.** 2-1
  named the wrong missing structure — the shape-transition cache it called absent is
  present and working — while being exactly right about the symptom it predicted. *An item
  can be worth doing and still be wrong about why; a control run tells you which half you
  have.*
- **A deferral is a claim too, and it needs a citation.** 2-4 shipped its update half with a
  written reason for not doing the compound half: an abstraction the compound path "goes
  through". It does not — that abstraction serves the *identifier* form, and the member form was
  three lines below the branch the item had just edited. *A sentence explaining why work was
  skipped will be read as a finding by whoever arrives next, so it needs the same file-and-line
  backing as one explaining why work was done.* The cost here was small; the deferral was mine
  and I returned to it. Aimed at a stranger it is a dead end with a plausible-sounding sign on it.
- **"Pure removal" is a claim about the code, and it is usually wrong.** 2-3 proposed deleting
  one of two stores. They serve different access paths, so neither can go; the item's real
  content was a storage-layer redesign, and its measured ceiling was 3% of the most favourable
  workload available. *Before writing "pure removal", delete the thing in a scratch build and
  see what breaks — here it took one probe, and the wrong answer it returned was the proof.*
- **Re-measuring can make an item *worse*, not just smaller.** 2-3's memory case was "the slot
  array is 4.5% of an object's bytes". 2-7 then cut 672 B out of that object — bytes 2-3 was not
  targeting — so the same proposal came back at **1.9%** for the common shape. *An item's share can
  fall because the work around it succeeded, so a share recorded before that work is not a share at
  all; and the direction is not predictable from the numerator alone.*
- **An item can be overtaken by the items before it, and it has happened twice.** 2-3 was
  written when a store cost two key lookups; P1-3 and 2-1 removed the second, so most of its
  value was collected before anyone reached it. 2-5 was written against "an `AsyncLocal` per
  write" when P0-2 had already removed the expensive half — the ExecutionContext *write* — and
  left only a read that measures at 0%. Both were re-measured on arrival and neither survived.
  *Re-measure an item's premise when the work before it lands, not only when it is written* —
  and note that in both cases the item was inheriting a cost description from the campaign that
  had already fixed it.
- **Name the half you mean.** "The `AsyncLocal` is on the hot path" was true of 2-5 and told
  nobody that the write was the cost and the read was not. An item that says which operation,
  on which population, at what frequency can be checked in one probe; one that names a mechanism
  cannot be checked at all until someone rebuilds the reasoning.
- **Compare against the right pair.** Pooling frames measured as "no cost" against
  *allocating* them. Against an array slot it was worth 11%. The first comparison
  showed recycling costs about what allocating costs — not that either is free.
- **A failing test is a claim, not a verdict.** Five "pre-existing failures" asserted
  behaviour the engine is right to refuse; the pinned suite settled each faster than
  reasoning from spec text. Two of the five contradicted a test262 vector the engine
  passes. Separately, three *harness* defects produced five failures that looked like
  engine defects and were not.
- **Check how much of the probe the change can reach.** 2-4's compound half first measured
  0.915 with two of eleven pairs the wrong way, on a *top-level* `o.x += 1` loop that spends most
  of its time resolving a global binding the change never touches. The same change on the
  in-function shape: 0.903, seven of eight the same direction, against a control at 1.002. *A
  probe whose bulk is inert dilutes the effect and keeps all of the noise* — and it fails
  downward, so it reads as "the change barely helps" rather than as a broken measurement.
- **An item's title can hide the mechanism, and then the item asks for the wrong thing.** 3-3
  was "widen the *unboxed-locals* gate", and named parameters as its first target. That gate has
  two tiers, and a parameter was outside *both* — but the one it could actually reach is the
  **scalar** tier, because a `var` can be proved numeric by reading the function while a
  parameter's type is the caller's choice. Taken at its word the item would have delivered
  nothing; measured, the parameter gap turned out to be a per-call `JSVariable` **cell** and not a
  box at all, worth 56 B on every parameter of every call. *When an item names a category, check
  which tier of the mechanism that category is missing before accepting the tier in the title.*
- **A ranking inside an item is a claim, and it is usually the least-supported sentence in it.**
  3-3 said "parameters are the valuable one" of four ineligible categories. Measured, all four
  cost **31.98 B/iteration** — identical to the byte — so the ordering recorded the order they
  were written down. The measurement also *reversed* it: the three that were deferred can reach
  the numeric tier and the one that was promoted cannot. *An ordering with no number behind it
  will be followed anyway, because it reads like a conclusion.*
- **A comment that says "missing one here is a miscompile" is a checklist, and it has to be run
  against every member of its family.** `AstReduce` leaves `ObjectProperty`, `VariableDeclarator`
  and `Case` as leaves for its rewriting visitors. Two of the three walkers that must not accept
  that carry an override for each and a comment saying why. The third, `NameCollector` — which
  backs the *only* rejection path in `NumericLocalAnalysis` — carried none, so every name bound
  through an object pattern was invisible to every rejection at once: `var { a: s } = o` aborted
  compilation of the whole script with an unhandled `NotImplementedException`, and
  `({ a: s } = o)` returned NaN. *The hazard was known, written down and fixed twice; nobody
  grepped for the third case. When a comment explains a trap, search for every class that can
  fall into it before writing the comment again.*
- **A green single run is not a green feature when the bug needs two.** 3-3's `let`/`const` half
  passed every script-host check, every single-test run, and its own allocation measurement — and
  miscompiled the moment a second compilation happened in the same process. The script host
  evaluates one file per process, so it is structurally incapable of seeing a defect of that
  shape, and the unit tests only caught it because xUnit happens to reuse one process across test
  methods. *When a change touches state that outlives a compilation, the smallest honest test is
  two compilations — and if the harness you reach for runs one, it is not the harness.*
- **Probe the analysis you are about to extend, before extending it.** 3-3's successor widens
  `NumericLocalAnalysis` from `var` to `let`/`const`. Ten minutes of probing what the existing
  analysis does with unusual *writes* found two it could not see at all — one of them a
  process abort on valid JavaScript, shipped since P2-2. Extending first would have widened a
  wrong-answer bug to two more declaration forms rather than exposing it. *A gate is only as
  sound as the analysis behind it, and the cheapest time to audit that analysis is while you
  still think of it as someone else's.*
- **A per-unit figure cannot tell a fixed cost from a scaling one, and it reads as scaling.**
  Phase 5's profile reported `exec` at 0.22 bytes per subject character, which sounds like
  something walking the subject; measured per CALL at three subject lengths it is ~1 950 bytes
  FLAT plus 0.02 B/char. The normalization that made the first finding legible — bytes per
  character, which is how the per-match subject copy was spotted — made the second one
  invisible. *Normalize by the thing you think is driving the cost, then vary that thing to
  check.*
- **An item can be written from another engine's architecture.** 4-3 asked for a mid-function
  bailout that "reconstructs an interpreter frame from a specialized one" — the V8 model, with a
  stack map naming where each value lives. This engine has no interpreter frame to reconstruct:
  tier-1 is compiled IL and a JavaScript local is a CLR local of that method, which is what
  phases C–F were *for*. The design that fits is a fallback branch inside the specialized method,
  where the locals are shared because it is the same method — cheaper than the item, and it
  preserves the frame-stack invariants by never engaging them. *When an item names a mechanism
  rather than an outcome, check the mechanism exists here before sizing it.*
- **Eager work for a deprecated feature is still work, and it is charged to the feature that is
  not deprecated.** Annex B's `RegExp.leftContext` / `rightContext` partition the subject around
  the match, and keeping them warm copied the whole subject on every successful match — so
  `replace` with a global flag, which execs once per match, was quadratic in allocation at
  42 859 bytes a match. Nothing reads those statics in ordinary code; recording the span and
  slicing on read costs a reference. *A compatibility surface nobody calls should be paid for by
  the caller that arrives, not by every operation that might one day precede one.*
- **Check which component actually runs before profiling the one the plan names.** Phase 5 is
  written about `Broiler.Regex`'s closure matcher, and B5 ranked it as sitting on PdfJS's and
  Typescript's critical path. It does not: `JSRegExp` routes only semantic-gap patterns to it,
  and Octane's corpus has no look-behind and no `u` flag, so the suite the phase is justified by
  never reaches the component the phase is about. The engine that does serve it was one grep away
  — `new Regex(pattern, options)` with no `RegexOptions.Compiled`. *A blocker that names a file
  is making a routing claim, and routing is cheaper to check than to profile.*
- **A `StringBuilder`'s floor is two copies, and pre-sizing removes neither.** Phase 5's
  single-match `replace` assembled its answer through a builder: one copy into the chunk list,
  one back out through `ToString()`. Pre-sizing it was tried first and was worth 0.2% — .NET's
  `StringBuilder` chunks rather than doubles, so there was no reallocation waste to remove — and
  the change that worked was to not use a builder at all, `string.Concat` over the three spans,
  worth exactly half. The neighbouring `String.prototype.replace` had the same three appends into
  a builder that was *already sized exactly right*, and halved by the same amount — which is the
  cleanest statement of the point, since there was nothing left to tune. *When the final length is
  knowable in one pass, a builder is the wrong tool rather than a mis-tuned one, and tuning it
  optimizes the copy you should not be making.*
- **A defect found by profiling has siblings the profile cannot see.** The single-match `replace`
  was found in a `--regex-profile` row; the identical assembly in `String.prototype.replace`'s
  string-`searchValue` path had **no row at all**, and was found by reading the builtin next to
  the one being edited. Its before-slope then matched the profiled path's to three decimal places
  — 4.020 B/char both — which is what established them as one defect in two places rather than
  two resembling ones. *When a profile localizes a cost to a mechanism, grep for the mechanism;
  the corpus only measures what somebody thought to add to it.*
- **A fix recorded as landed covers the path it was measured on, not the feature.** 2-0 removed
  the per-allocation prototype invalidation and pinned it at "200 001 → 3" — on a `function`
  constructor. `class C{}; new C()` still publishes one per allocation, by the same mechanism the
  fix's own comment describes (a second write to the prototype that reads as `[[SetPrototypeOf]]`
  on a live object). The item was not wrong and its number was not stale; its *scope* was one
  construction path, and nothing in the record said so. *When a fix is verified through one
  syntax, name the syntax in the claim — the next reader will otherwise take the general
  statement, and a second path can carry the identical defect for as long as nobody spells it.*
- **A cache entry that cannot be replaced is worse than no entry.** The inline cache's add path
  deduplicated on `ShapeId` + `Holder`; a hit checked those plus four more guards. When one of the
  four went stale the read missed, reached the add path, was told the entry was already present,
  and returned — leaving the stale entry in place with no route back. That site then missed on
  that receiver forever, and it was **77.7% of DeltaBlue's misses**. A cold site would have
  recovered on its second read; this one could not recover at all. *Whenever a lookup and an
  insert disagree about what identifies an entry, the insert wins and the lookup starves — check
  that the dedup key is the whole guard, not a prefix of it.*
- **A process-wide invalidation makes every workload pay for the worst one.** The prototype
  version is deliberately coarse — one mutation anywhere retires every prototype-keyed entry
  everywhere — so a redundant write on one construction path held *Richards's* read cache at 86%
  when the machinery was capable of 99.97%. Richards does not construct classes; it was paying
  for someone else's writes. Every phase 2 probe measured the machinery in isolation, where the
  storm does not exist, and all of them reported it working. *A shared invalidation channel turns
  a local defect into a global one and hides it from every local measurement — the only
  instrument that sees it is a counter taken over a whole real workload.*
- **A counter that separates two workloads is a lead, not an explanation.** DeltaBlue fails
  phase 2's gate and Richards passes it, and of every inline-cache counter the sharpest split was
  dictionary fallbacks: **2 503 against 1**, three orders of magnitude. It traced to a real
  defect — `push` cost every array its shape permanently — and fixing it took DeltaBlue to **0**.
  **DeltaBlue's read hit rate then did not move by a hundredth of a percent.** The suite was
  losing shapes it never read through, because it puts no named properties on its arrays. The fix
  is worth keeping on its own merits and the investigation still has to start over. *Rank a
  counter by how well it explains the metric you care about, not by how sharply it separates the
  cases — and confirm the link by moving it, because "biggest difference" and "cause" are
  different claims and only one of them is testable cheaply.*
- **An exit criterion that has never been run is not a pending task, it is an unknown answer.**
  Phase 2's was *"DeltaBlue and Richards inside 200×"*, owed since the phase opened and carried
  through every item as one line of the sequencing table. Run at last, it **splits**: Richards is
  inside at 183× and DeltaBlue is outside at 576×, so the phase that was described as "every item
  landed or closed" has in fact failed half its own gate, on the suite item 2-8 was written for.
  Two repetitions would not have said that safely; five with a band did. *A gate carried unrun
  reads as "nearly done" for as long as nobody runs it, and the cost of running it is almost
  always less than the cost of the plan built on top of assuming it passes.*
- **A hypothesis with a plausible mechanism still needs the control that would refute it.** 2-9's
  losing side was explained by the Annex B deferred cells forcing a trie rebuild — a mechanism
  read straight off the code, correct in every step, and wrong about the cause. The control that
  settles it is one line of JavaScript: a **strict** function gets no deferred cells, so if the
  cells are the cause it must not pay. It pays exactly the same — 1.00 trie rebuilds per function
  on both — because the `prototype` install materializes first, for an unrelated correctness
  reason. *The question "what would I expect to see if this were false" has an answer that is
  usually cheaper to run than the fix the hypothesis implies, and running it first is what stops
  a fix being built for a cause that was not there.*
- **Count the thing, do not infer it from bytes.** The same hypothesis had been probed by
  allocation, where the deferred cells do show up — non-strict functions cost 4.8% more than
  strict, which reads like confirmation. A counter on the rebuild itself says 1.00 on both, and
  the 4.8% turns out to be the cells' own price and nothing to do with the trie. *An indirect
  instrument agreeing with you is weaker evidence than a direct one, and adding the direct one
  here was six lines.*
- **When an optimization skips work, the design is the observers, not the work.** Phase 5's
  streaming replace is four lines: append each replacement instead of collecting them all. Every
  hour of it went into establishing that nobody could watch the skipped result objects — and two
  of the three conditions that turned out to be needed are not in the item's description. The
  sharpest is the functional replacer: because the spec collects *all* matches before calling
  *any* replacer, the final failing `exec` has already reset `lastIndex` to 0 before user code
  runs, so a streamed replacer would see a *different value*, not merely a different order. The
  item said "changes the observable order"; the actual hazard was a changed value. *Enumerate who
  could have been watching, and check what each one would see — an item that names the hazard in
  the abstract has usually not enumerated them.*
- **"Is this builtin unpatched" can only be asked against a pristine capture.** The `exec` guard
  compares against `%RegExp.prototype.exec%` captured at realm init, before user code runs.
  Reading `RegExp.prototype.exec` at call time and comparing it to itself is circular — by then
  it may already be the patched one — and there is no property of the function object that says
  "genuine". *Identity against something captured earlier is the test; anything cheaper is
  answering a different question.*
- **A halving that lands exactly is a check on the decomposition, not just a win.** The same item
  predicted 4 B/char → 2 from "two full UTF-16 copies and nothing else". Measuring 4.02 → 2.02 at
  three subject lengths is what rules out a third copy hiding in the row; a saving of *roughly*
  half would have left the model unfalsified and untested. *Predict the number before the change,
  then treat a miss as evidence about the model rather than noise in the measurement.*
- **A count you inferred is not a count, however well it reconciles.** 3-6 sized its successor at
  290 names by reading survivors as *offered minus dropped*, and said its two figures "reconcile
  exactly" — they did, to **each other**, while both omitted the same third population: everything
  a rejection path removes before the fixed point runs, which had no counter at all. Counted
  directly, `offered 2 295 = rejected 133 + dropped 1 916 + surviving 246`, and the item's real
  population was **22 names, of which 8 were reachable** — off by 36×. *Two derived numbers
  agreeing is evidence about the arithmetic between them and about nothing else; adding the direct
  counter was four lines, and the campaign's own rule about indirect instruments (2-9) had already
  been written down.*
- **A conjunct that is doing two jobs hides the second one until you remove it.** Every numeric
  local a nested function mentions was refused, and the stated reason was that a closure captures
  through a cell. It was also, silently, the only thing preventing three defects: a hoisted
  function declaration reading the binding before its initializer, a nested function's parameter
  marking the outer name initialized, and a function declaration storing a function object into
  the binding being typed. All three were reachable the moment the refusal was lifted, and all
  three produce wrong answers rather than lost optimizations. *Before widening a gate, ask what
  else the conjunct you are deleting happens to be enforcing — the answer is not in its comment,
  because whoever wrote the comment did not know either.*
- **A static argument that rests on text order is defeated by hoisting, not by closures.** The
  numeric tier is sound because a name referenced before its declaration is refused, so text order
  implies execution order. A function *expression* preserves that — it does not exist until its
  statement runs — while a function *declaration* at body top level breaks it, existing at entry
  with its body textually anywhere. The distinction is worth 247 of 478 captured names on the
  Octane corpus, i.e. it is the majority case and not a corner. *When an item is described as
  "entirely static", check which of its conditions are about text and which are about time.*
- **Never build while a suite is running against the output, and read the failure before calling
  it a flake.** One `properties-proxy` run reported an extra failure on the arm under test and not
  on its control — the shape of a regression. It was neither: the runner's captured stderr said
  *"The JavaScript compiler is not available"*, because a `dotnet build` I had started for an
  unrelated edit was rewriting `Broiler.JavaScript.Compiler.dll` under the running children. The
  first diagnosis was "a flake under `--max-workers 8`", which fitted the evidence then available
  (the test passes three times in isolation, needs no `$262`, and answers correctly on every
  build) and was still wrong. *A failure that reproduces nowhere is not thereby a flake — the
  runner had recorded the actual reason, and reading it was cheaper than the three re-runs that
  did not settle it.* This is §3.5's "check that the thing you measured is the thing you built"
  from the other side: there the binary was older than the source, here it was being rewritten
  mid-suite.

- **A run of deltas is not a measurement of the mechanism, and the difference is a switch.**
  Items 3-0, 3-3, 3-5 and 3-7 each measured their own increment to the numeric-local tier against
  the tier as it stood, and each came out invisible on the corpus — 0.997×, 1.0001×. Four such
  readings look like a verdict on the mechanism and are a verdict on *eight more names*. Turning
  the whole tier off for the first time put a number on it: **0.36% of the engine's number boxing,
  0.41% of allocation**, from every raw-double local the campaign has ever produced. *When several
  items in a row report "no effect", the missing control is the one that removes all of them at
  once — and it is usually one conjunct and an environment variable.*
- **A per-unit figure repeated by every item is a description of the unit, not of the problem.**
  Phase 3 has now reported **31.98 bytes an iteration** for four ineligible categories (3-3), a
  parameter-bound comparison (3-5), a captured local (3-7) and all three provability causes (3-8).
  It is the same box, and it was never the question. The question is what share of a real
  workload's allocation is boxing at all — **41.89%**, and 66.96% on NavierStokes against 0.31% on
  DeltaBlue. *A number that comes out identical no matter which item measures it is measuring the
  representation, and the corpus share has to be measured separately or the phase will keep
  producing shapes that are 7× faster and suites that do not move.*
- **A corpus average can bury the very thing the phase is for.** The boxing share across the seven
  Octane suites is 41.89%, and reading only that average would have been almost as misleading as
  reading none: it is 0.31% on DeltaBlue and 66.96% on NavierStokes. Four suites where phase 3 has
  nothing to win outvote three where it has almost everything. *Report the spread before the
  aggregate whenever the items are representation changes, because those are exactly the changes
  whose value is concentrated in a workload shape rather than spread across one.*

- **A one-sentence premise is a cause claim, and it is the sentence least likely to have been
  checked.** Item 3-2 stood for the whole campaign as *"`shapeSlots` holds `JSValue` references, so
  `vector.x = 1.5` allocates"*. The line does allocate. The slot does not: `o.x = 2` costs **0.00
  bytes an iteration**, because storing an already-boxed value into a slot is a reference copy.
  What that example pays for is the **literal**, which is a different item worth 1.2% of the
  corpus's boxing — so for as long as the sentence stood unmeasured it aimed the item at the wrong
  half of its own mechanism. *The shorter an item's justification, the more of it is inference;
  probe the example it gives you before the mechanism it names.*
- **Two items described as twins should be measured against each other before either is built.**
  3-1 and 3-2 have been separate L's since the phase opened. Measured, their per-iteration figures
  are identical to the hundredth — 31.98 for a read into an addition and 96.00 for a
  read-modify-write, in an array and in an object field alike — because both are one mechanism (a
  value that stays unboxed from producer to consumer) with two storage backends. Their *populations*
  then turn out to be disjoint: **98% of the corpus's numeric property reads are Box2D's**, while
  NavierStokes performs **388** property reads and mints **30 M** boxes. *The shared half should be
  built once and the storage halves ranked by population — neither of which is visible from either
  item's own text.*

- **Check a corpus counter is deterministic before reading a delta out of it.** Item 3-1's
  bitwise change came back **+3 126 boxes on Crypto** — the wrong direction, on the suite it was
  aimed at. Running the *same arm twice* gave 42 418 727 and 42 421 217: Crypto generates RSA keys
  and its work is not fixed across runs, so its own variation is larger than any gap between the
  arms. Six of the seven suites are identical to the digit and only that one is not. *"Allocation
  is deterministic" is a property of most counters here and not of all of them, and the check
  costs one extra run of the arm you already have.*

- **An emitter that cannot be fed is not an optimization, and it will pass every test you write
  for it.** The bitwise operators were given a native form that takes `s = i & 1023` from 31.84
  bytes an iteration to **0.00**, is correct on 15 semantics cases, and removes **exactly zero
  boxes on the whole Octane corpus** — including on Crypto, a BigInteger implementation built on
  `&`, `|` and `>>` that mints 42.4 M boxes. The native form requires both operands to be numeric
  locals, and Crypto's digits live in `this.array[i]`. That is the same shape as 3-5's finding a
  phase earlier, and by now it is a rule: *before adding a fast path, count how many of its
  operands can actually reach it — the population feeding a specialization is a different
  measurement from the specialization's own speed, and only the first one predicts the corpus.*

- **A control built by deleting a syntactic category deletes the program when the program is one
  of them.** `--compile-profile` sizes item 1-1 by replacing every *outermost* function body with
  `{}`. jQuery has exactly one outermost function — the IIFE the library is written inside — so
  its control is an empty file (`bodyByteShare` **0.9991**), `full − stub` is the whole compile,
  and the resulting "96.5% ceiling" is *everything except the parse*. It is also unreachable, for
  a reason the same table cannot see: CodeLoad evaluates jQuery, so that body is the first thing
  called. The instrument that answers the question is a **count of what is never invoked**, and it
  says 83.6% rather than 96.5% — a different measurement, not a corrected one. *A differencing
  control is only a ceiling while the thing it removes is the thing that is optional; check the
  share it removes before quoting the difference.*
- **A phase that is deferred can still be walked, and the counter is one line.** Item 1-1 defers a
  nested function's IL to first invocation, and the relay that registers the deferral then ran the
  closure rewrite over that function's whole subtree — so deferring jQuery's single IIFE walked
  the entire program. The rewrite descends through nested lambdas already, which makes the relay's
  call a repeat at every level: a lambda at depth *d* was walked *d+1* times. Two counters on the
  relay say so exactly — **0 rewrites needed against 415, 978 and 1 574 skips** on three corpora.
  *After deferring a phase, count what the deferral still touches: the work that moves is easy to
  measure and the work that stays is what nobody looks at.*
- **A counter reading zero is a claim about the counter, and "turn it on" has a location.** The
  arithmetic-operand census read **0 invocations on all seven suites** against 85 M boxes, which
  would have been a finding — the generic operators are never called — and was an instrument
  switched on in the wrong method: the enable was inserted next to the first of two identical
  `NumberBoxingDiagnostics.Reset()` pairs, one in a call probe and one in the driver. The *boxing*
  counter next to it read correctly throughout, which is what made the zero look like data.
  *Before reporting an extreme count, make the instrument produce a non-extreme one on a case you
  constructed to move it* — here five test fixtures, three of which have to make the counter
  discriminate rather than merely fire.
- **Compile-time provability and run-time truth are different measurements, and this phase had only
  ever taken the first.** Every phase-3 item widens what the compiler can *prove* numeric, and the
  gate they widen reaches **0.75%** of the corpus's arithmetic invocations. What those operators are
  actually *handed* is two Numbers **100.00%** of the time — 73 817 515 of 73 818 646, every one
  but 1 131. Six correct, invisible items sit in the gap between those two numbers. *When a
  static analysis is the thing being widened, count what the dynamic answer would have been before
  widening it again; the two counts are usually available from the same probe run and only one of
  them predicts the corpus.*
- **Interleave, at process granularity.** Sub-1.5% effects are only visible ABBA-
  interleaved across independent builds, ten runs each, medians compared.
- **Two shapes that allocate at different rates cannot share a process, and the control is what
  says so.** 3-7's first timing run put the change at 0.1327× — and its *control*, the same code
  compiled the same way on both arms, at 1.2857×. A control that moves is a broken measurement,
  full stop: the winning arm allocated 192 MB over its loop and the collections landed on whatever
  ran next in the same process. Re-run one shape per process the control came back to 0.9535× and
  the answer held. *That is the `--compile-profile` corpus artifact one level down, and the general
  form is that a control exists to be checked, not to be quoted alongside the result.*
- **Hold the call site fixed when the callee is what changed.** Sizing a parameter's cost by
  comparing `h(a)` called with one argument against `h(a, c, d)` called with three measured the
  *arguments* as much as the bindings, and reported 88 B per parameter. Passing three arguments to
  both — so the only difference left is how many the callee declares — gave **56**, which the
  before/after then confirmed exactly at 168 B for three. *Same failure as 2-4's diluted probe,
  from the opposite direction: there the probe was mostly inert, here it moved two things at once.*
- **The local suite will not catch a lifetime bug.** All three frame-recycling defects
  appeared as corrupted parent chains — two only as an intermittent hang. The suite
  stayed green through every one. Diagnosis was bisection to a failure *rate*
  (20–40 runs per configuration), not reading.
- **Mutation-test an invariant.** Both frame-lifetime rules pass the JavaScript-level
  tests with either rule deleted, because the corruption needs a job-queue
  interleaving the xUnit host does not reproduce. Assert the rule against the API.
- **A share of a suite's own allocation forecasts nothing; the absolute rate forecasts everything.**
  3-1's `ToNumeric` reuse removed **50.0% of EarleyBoyer's boxes and moved it 1.002×**, and **23.0%
  of NavierStokes' and moved it 0.906× on six of six pairs**. The percentages say the opposite of
  the result; the rates say it exactly — 82 000 boxes a second removed against **4 240 000**. This
  document had quoted per-suite percentages as though they forecast time since phase 3 opened, and
  they never did: NavierStokes mints 18.5 M boxes a second and EarleyBoyer 165 000, two orders of
  magnitude apart, so no single corpus figure describes both. *Before predicting time from an
  allocation change, divide by the elapsed time — a proportion is a statement about the suite, and
  only a rate is a statement about the machine.* The corollary is that a driver total can be silent
  while the item works: 9.4% off a suite that is 8.7% of the corpus is 0.82% of the total, which is
  under the total's own noise before anything is built.
- **An unattributed residue is a claim about the census, and chasing it is where the item is.**
  3-1's boxing-source census named 59.5% of the corpus's requests and left **40.5% coming from
  nowhere**, which reads like builtins and rounding and was nearly written up that way. Two
  counters took it to **1.0%**, and what came out was not a scattering: **`++` and `--` are 30.9%
  of all boxing on the corpus and 51.6% on its biggest boxer** — larger than the compiler
  conversion the section had been written to measure, and invisible because the census was built
  around *binary* arithmetic and no one had counted a unary operator. Half of it is a `ToNumeric`
  copying a `JSNumber` into an equal `JSNumber`. *A residue is the part of the measurement that is
  not yet a measurement; the size of the thing hiding in it is bounded only by how big the residue
  is.* Corollary, from the same afternoon: `BitwiseXor` was the one generic binary operator the
  census never hooked, and nothing failed — **an unhooked operator is silent, not wrong**, which is
  the same failure mode as the counter that read zero, one level up.
- **An optimization with a numerator and no denominator is half a measurement, and the missing
  half is usually the item.** `0084` reported *"10 401 782 boxes removed, 12.2%, from 862 sites"*
  and explained the gap to its own 86.6% ceiling with a per-suite table. What it never reported is
  **how many sites it was offered** — and the answer is 5 396, so it was specializing 16.0% of the
  arithmetic and the other 84% had no attribution at all. Adding the waterfall took one enum and
  about thirty lines, and the largest result in phase 3 fell straight out of it: one rule
  (`OrderUnsafe`, 1 762) was manufacturing a second (`NoSavingToMake`, 2 718) by refusing chains
  from the top down until only a lone operator was left. *"X% removed" is a statement about the
  successes. The refusals are a population too, and until they are attributed the item does not
  know what it is next.*
- **A threshold nothing has ever hit is untested, not safe.** `MaximumSpeculativeLeaves` was 8 and
  read as a harmless code-size bound because it fired **zero** times on the corpus — but only
  because a rule above it refused those trees first. The moment that rule went, the cap turned down
  85 trees and cost **664 338 boxes, 2.1%** of what the change otherwise removes. *A constant's
  measured cost is only valid for the configuration it was measured in; changing anything upstream
  of a limit re-opens it.* This is the same shape as `0084`'s "two operators" rule, which was
  reasoned rather than measured and lost half that item's prize.
- **Price the thing you are optimizing before optimizing it, not after — and "allocation" is not
  one cost.** Phase 3 ran for eight items on box counts, and three of them measured an allocation
  cut against wall clock and got about a sixth of the share back, three times, with no explanation
  offered. Four lines of `GC.GetTotalPauseDuration()` say why: **collection is 1.8% of the driver**,
  so the collector was never the thing being bought back. Of the 768 ms the order-preserving
  emission removed, **54 ms was collection and 714 ms was the mutator** — the pointer bump, the
  zeroing, the write barriers and the cache traffic. *A box costs about fourteen times more to
  create than to collect on this corpus*, which is the number that makes "GC work is a non-goal"
  a measurement rather than an opinion, and which gives every future allocation item a rate to bid
  with — **711 ms per GB** — instead of a percentage.
- **Price a mechanism by what it lets through, not by what it catches.** Item 3-8 was shelved on
  `BROILER_JS_NUMERIC_LOCALS=0`: the whole raw-double local tier removes **0.36%** of the corpus's
  boxing, so widening it looked like an XL for nothing. That number is real and it answers a
  different question — it measures the population the analysis *can already prove*, which is small
  exactly because the proof is hard. Counting the same mechanism from the other side, at the
  `++`/`--` operator, the names it **fails** to type carry **22.6% of everything the corpus still
  allocates**. *An ablation switch prices the built thing; only a census of the misses prices the
  thing that was not built,* and the two differed sixty-fold here. This is the second time the same
  correction has been needed — `0083` found compile-time provability reaching 0.75% of the
  arithmetic against run-time truth's 100.00% — so it is a pattern rather than an accident:
  **whenever an item is turned down on an ablation, ask what the ablated mechanism never saw.**
- **Narrowing an item's population does not narrow its mechanism, and only one of the two decides
  the size.** 3-8a was re-sized from XL to M on the strength of its population: a run-time numeric
  guard aimed at one cascade instead of at every local. Taken to the build, the mechanism was
  unchanged — a speculative raw double is a double *only while a flag holds*, and every fast path
  in the compiler keys off the single `NumericStorage` field that means "this is a double", so all
  of them become guard-aware or read a dead value. *Size an item by the surface that has to change,
  which is a property of the representation, not by the number of names that would use it.* The
  tell was available before any code: the item's own sentence said "pointed at a representation".
- **A counter that has never read non-zero is not evidence of a zero.** 3-8a's population
  instrument read 0 on all seven suites *and* on the shape it was built for, and was reverted
  rather than reported. §3.5 already had the rule from `0083` — where the enable went next to the
  wrong one of two identical lines — and the same failure recurred here in a new form: **the enable
  for a COMPILE-time counter was placed among the run-time censuses, which are switched on after
  the corpus has finished compiling.** Fixing the placement changed nothing, which is what said the
  problem was the instrument and not the placement. *Before believing a zero, make the instrument
  produce a non-zero on a shape you constructed to produce one.*
- **"The suite that has the names is the suite that has the traffic" is not the same claim as "the
  names have the traffic", and only the arm tells them apart.** Item 3-8a's population came out as
  15 names in NavierStokes, which is also the suite carrying 9.46 M `LocalSlot` update steps, and
  the scoping treated the alignment as read. Built and measured, **those 15 names carry 835 584 of
  the 9.46 M — 8.8%.** The count was right and the inference on top of it was wrong, which is the
  same shape as item 3-6's 290 names being *inferred* from offered-minus-dropped rather than
  counted. *A population and a traffic figure that live in the same suite still need multiplying,
  and the multiplication is an A/B, not an argument.*
- **A bound can be right about the number and silent about the thing that decides it.**
  `--compile-phases` charged item 1-1's free-name walk at **5.4–9.9%** of tree construction and
  called it a lower bound because it counted identifiers and resolved nothing. Built for real, the
  walk lands at **6.6–12.2%** — so the bound was a good estimate — *of a walk written as one
  bottom-up pass*. Written the obvious way, one scan per function, it costs **47.7%** on the most
  deeply nested corpus, because scanning a function re-walks every function inside it and each
  enclosing level walks it again. *A precondition's price is a property of its implementation, and a
  bound that does not say which implementation it bounds can be off five-fold without being wrong.*
- **A cost you write down as the price of a change should be measured before it is written down,
  because it may not be that change's price at all.** `0099` recorded one deadlock as what the
  execution lock cost. Measured, there were **two**, and the first belonged to `0098`'s job queue —
  a change earlier than the note blaming the lock for it. The control row is what separates them: a
  host wait on unrelated work completes on both builds, so the two failures are mechanisms rather
  than one symptom seen twice. *A named cost is a claim; run it against each build that could have
  caused it before attributing it to the newest one.*
- **A concurrency counter measures the wrong thing by default, and the default is plausible enough
  to ship.** The detector built to check "one thread runs JavaScript in a context at a time" counted
  threads inside JavaScript **process-wide** on its first version. That is not the invariant: two
  independent contexts running in parallel is exactly what an embedder is supposed to be able to do,
  so the counter would have reported legitimate concurrency as a violation and fired on any
  full-suite run, where xUnit evaluates several test classes at once. *Before trusting a counter that
  checks an invariant, state the invariant's scope and check the counter has the same one.*
- **"In principle" in a written-up residual is a measurement not taken.** `0098` recorded that a job
  posted with nothing running "could in principle land during a later execution". Measured, it did
  so in **172 of 200 rounds**. The honesty of naming the gap was worth something; the estimate inside
  it was worth nothing, and the two are easy to mistake for each other in a document that otherwise
  insists on numbers.
- **A test that fails only under load is a race, and the race is more likely in the engine than in
  the test.** `SuspendingNestedFunctionsCaptureThroughTheSameBox` had passed every full-suite run in
  this phase; a saturated container made it fail three times in four, and what it was reporting was
  the engine running **user JavaScript on two threads in one context at once**. Both dispatch paths
  for a promise job were wrong — the thread pool when no `SynchronizationContext` was present, and
  `SynchronizationContext.Current` when one was, because a test host's context is not a JavaScript
  thread — and *each covered for the other's absence*, which is why a fix for one of them measured
  clean on a console harness and still failed the suite. **A rate measured on a loaded machine and
  re-measured on a quiet one is not an A/B**; what settled it was a fixture built to lose the race
  deterministically. *When a flake is timing-dependent, make the timing lopsided on purpose before
  believing any fix for it.*
- **A precondition count can close an item for the price of an instrument, and it is the cheapest
  outcome available.** Item 3-9's specification said to count first; the count came back **0 names
  and 0 offers on all seven suites**, so a mechanism that was sound, guard-free and genuinely
  attractive was declined without being written. Set that beside 3-8a directly above, whose
  population *was* real and which was built and lost anyway: *the count does not always say build,
  and the item that gets counted is the one that can be closed cheaply either way.* The counter
  stays in the tree with the condition that would re-open it written down — 3-9's supply is bounded
  by item 3-7's eight captured numeric locals, so widening 3-7 is the only thing that changes the
  answer.
- **A representation change is priced by the read/write ratio of the population, and that ratio
  has to be counted before the representation is built.** 3-8a's storage half does exactly what it
  was built to do — 835 584 update steps take a native double add and box nothing — and the corpus
  got **2.1% MORE boxes**. Three consumers were then built to close the gap, each a reasonable guess
  at where the remaining boxes were: the guarded tree's leaf and the element read took it to 1.7%,
  the element write to 1.2%. Only then was a counter added **at the read** (`CreateSpeculativeRead`,
  a fourth factory entry beside `CreateLiteral` and `CreateConversion`), and it settled the item in
  one line: **394 000 boxes minted reading, ≈5 300 removed.** The steps it takes off `Increment`
  mostly do not save an allocation at all, because they are `x[++i]` and the result is boxed to be
  an index either way. *Count the losing side at its own site before building the winning one — four
  builds and a measured regression is what it costs to count it afterwards.* Note the symmetry with
  item 3-1's bitwise operators, where the rule was *count how many of a fast path's operands can
  reach it*: here the operands reached it, and the **other** side of the trade was the uncounted one.
- **Every premise can survive and the item can still lose.** 3-8a's scoping A/B held exactly as
  measured — the enclosing-scope read is the defeat, testing it at run time removes the row, the
  population is real. *An item is not validated by its premises being true; it is validated by the
  number at the end, and the two can point opposite ways.*
- **A fixture written against a broken emitter can pass, and passing is not evidence.** Three of
  3-8a's read-path fixtures passed against the *bug they were written to catch*, for two different
  reasons: the trees they built were refused by an eligibility gate before the new leaf ran, and the
  ordering fixture's `i = "2"` defeated the local's candidacy at compile time, so the path under test
  was never emitted. *After writing a fixture for a new fast path, break the emitter deliberately and
  confirm the fixture fails* — the same discipline §3.5 already demands of a counter, applied to
  tests. It caught a stale-slot read (`"0!"` for `"3!"`) that no amount of re-reading the test had.
- **A sampling profiler is not automatically an instrument.** `dotnet-trace`'s sample profiler
  inflates this driver by ~29% and attributes 28% of self time to `Thread.PollGCWorker`, the
  rendezvous point its own stack walks force threads to — *the biggest frame in the profile is the
  profiler*. Independently, compiled JavaScript lives in `DynamicMethod`s that do not symbolicate,
  so 47.8% of the run lands on `JSFunction.InvokeFunction` and 2.4% on a named function body. Both
  facts were cheap to establish and neither was guessable. *Check what a new instrument costs and
  what it can name before believing its largest row* — the counter it displaced (an exact GC pause
  duration) was four lines and had none of these problems.
- **A fixture that asserts an eligibility *refusal* is an alarm for the next item, and should be
  written to go off.** `0085`'s `AnUpdateOnAPropertyCostsTwoBoxesNotOne` failed when `0086` landed
  under it; `0084`'s `ATreeWhoseOrderCannotBePreservedIsRefused` failed when the order-preserving
  emission landed under it. Both times the failing test was the correct and cheapest notification
  that a successor had changed the mechanism, and both times the repair was the same: **restate it
  as the invariant on both settings of the new switch** — the answer is unchanged, only which form
  computes it moves. *Assert the count as well as the answer, because an answer-only fixture passes
  silently when the mechanism underneath it is replaced.*
- **A read-only question asked through a mutating API is not a read-only question.** Item 1-1's
  population probe needed one thing from the compiler: where does this name resolve? The API for
  that is `FastFunctionScope.GetVariable`, and it **sets `RootScope.HasOuterFunctionCaptures` as a
  side effect of answering** — which is a conjunct of item 4-2a's tiering gate. A probe built the
  obvious way would have turned tiering *off* for every function it merely asked about, silently,
  and only on the arm where the counter was enabled: the measured arm would have differed from the
  shipping one in a way no assertion in the instrument could catch, because the instrument was the
  cause. It cost one grep for what reads the flag. *Before reading engine state through an existing
  accessor, read the accessor — an instrument that mutates is measuring a build nobody ships, and
  the arm it corrupts is the one you are reporting.*
- **Historical lesson: a declared repeatability guard was treated as an acceptance band before
  the campaign had measured a controlled lane.** `phase0.json` has carried 7.5% since 0-4 built
  `--repetitions`; older sections wrote acceptance rules against it even though it was only a
  configured harness value. Run in a container:
  **5 of 13 scores exceed it**, spread 0.4%–15.9%, with **Richards and DeltaBlue among the
  failures** — the pair phase 2's historical exit criterion rests on. Run as hosted-CI smoke:
  **1 of 17 exceeds it**, median 3.0%, and all five of the container's
  offenders are inside — Richards at **1.9%**, a 5.6× difference on one suite between two
  honest three-repetition runs of the same engine. **Both readings are right about their own
  machine and neither is an acceptance envelope or resolution estimate.** *Calibrate the
  workload/metric A/A envelope on the controlled lane where both arms will run, keep the
  practical decision threshold separate, and never carry a three-sample spread across a
  machine boundary — including from a development container or hosted runner into an
  acceptance rule.*
- **A measurement that decides a *design* has to be re-taken before the design ships, because a
  premise can expire.** Phase 5's item 2 declined to ship a `Compiled` policy on the strength of
  one pattern out of eleven measuring **4.3× slower compiled** — stable across three repetitions,
  decomposed with four extra probes, and written up as the reason a use-count rule is unsafe.
  Re-running the *same probe on the same patterns* months later, with nothing in Broiler changed:
  **all three losing rows changed sign**, and the shape the whole decision named now promotes at
  5.27× on Octane's own subject. The original reading had already said the loss *"is .NET's
  codegen, not this engine's"* — which is precisely why it was not a fact about this repository
  and could stop being true without anything here moving. *A number taken from a dependency is a
  reading of that dependency's current version. Encode it in a comparison the engine re-runs, not
  in a branch the engine carries.*
- **"The corpus" is a denominator, and an instrument that does not emit it will be quoted without
  one.** Every phase-3 and phase-4 headline in this document says *"the corpus"* — 41.89% of its
  allocation, 54.0% of it removed, 93.54% of its reads monomorphic — and the censuses producing
  those numbers ran **7 of Octane's 15 suites**. The totals were added up outside the instrument,
  which is the step where the suite list stopped travelling with the number. Widened, the
  monomorphic read share is **80.11%, not 93.54%**, and **87.7% of the corpus's reads were outside
  the seven**. *Emit the aggregate from the instrument, with the population size beside it, so a
  partial corpus is forced to say so at the point of use.*
- **A missing suite is a defect report nobody filed.** The seven were not chosen — Mandreel
  **aborted the census host** with an uncatchable .NET stack overflow, because item 0-2's 16 MiB
  thread and stack reserve are a property of the *shell*, and no benchmark host had them. The
  census then serialized its output only at the end, so that abort discarded the eight suites that
  had already run. Between them those two make a suite permanently unmeasurable and make finding
  out expensive. *When a corpus has a hole in it, the hole is the first thing to measure — and
  make every instrument checkpoint per item, because the run you cannot finish is the one whose
  partial results you most need.*
- **A ratio to another engine is a statement about both engines, and the third column tells you
  which one moved.** Phase 2's exit criterion is *"DeltaBlue and Richards inside 200× of
  Chromium"*, and for three sessions the 400×/141× split was read as a fact about this engine —
  four explanations eliminated, two real defects fixed, no dent in the ratio. Asking the *same*
  question of Jint, a managed interpreter with no JIT that has been in every committed run since
  the harness gained it, splits it in one division: DeltaBlue is **2.83× harder than Richards for
  Broiler and 2.56× harder for Jint**, so only **1.10×** of the gap is ours, and closing all of it
  reaches 362× against a 200× gate. *The criterion was unreachable by construction and nothing in
  the item said so.* The cost of finding out was a division on data already committed. **Whenever
  an acceptance test is expressed as a ratio to a system you do not control, compute it for a third
  system before spending a session inside the numerator** — and prefer a reference that fails the
  way you do (a managed interpreter) over one that does not (a production JIT), because only the
  first can tell a shared difficulty from a private defect.
- **An environmental canary moving with the subject is a cheap warning, not a candidate control.**
  Between the two committed Octane runs Broiler's geomean reads **351 → 498**, which looks like a
  1.42× improvement if any of it belongs to the engine. Chromium's geomean moved
  **57 080 → 74 297** on the same runner over the same two days and Jint's **616 → 820** — three
  engines moving 1.30–1.42× together, enough to warn that the host changed and the Broiler delta
  is not attributable. Different engines need not scale equally, so neither reference normalizes
  the result and the ratio column cannot “divide out” the machine. Both runs are single-repetition
  and say so. *Use reference-engine movement to invalidate an attribution, never to accept or
  rescale one; acceptance requires the same-engine null and candidate/control arms in one
  identity-attested session.*
- **A number computed over a subset stays wrong in the same direction every time you re-use it, and
  the subset does not announce itself.** §4.2a found three censuses stuck on 7 of Octane's 15 suites
  and fixed the hosts; what it could not fix is every *figure already derived* from them, because a
  derived figure carries no record of its denominator. Two have since been re-taken and **both
  moved by more than the effects they were used to justify**: item 4-2's `arithmeticBothNumbers`
  from 100.00% to 92.10% with a 0.46%–100% per-suite spread, and item 4-4's inlining ceiling from
  **1.89% to 2.43%** — the latter *upward*, because although *"from a promoted caller"* falls from
  64.0% to 42.1%, the suites nobody had counted make far more calls per millisecond than the seven
  do. Neither re-take needed new code; the widened hosts had been shipping for one patch, and
  the numbers were simply never read again. **The seven suites are 10.4% of the corpus's calls
  against 18.8% of its time** — call-poor, the opposite of how they were chosen. *When an
  instrument's reach changes, re-derive everything that was ever computed from it, and re-derive it
  by reproducing the old reading first* — both re-takes matched the old figure over the old subset,
  4-4's to within 0.0002% on a count of millions, which is what makes the new reading the same
  measurement rather than a different one.
- **A widened denominator has to exclude what does not run, and the suite that breaks it is the one
  that dominates it.** Both re-takes above were first computed against all fifteen Octane suites,
  which reported 4-2's arithmetic half at 0.038% and **4-4's ceiling at 0.65% — a third of the
  seven-suite figure it was correcting, and the wrong direction entirely.** Three suites fail
  (zlib's `read` is a shell builtin, RegExp has a pre-existing checksum, **Mandreel hits the stack
  guard**) and §4.2a had already written the rule: the widened headlines are over the twelve *"and
  the JSON says so"*. **Mandreel spends 286 728 ms failing** — 72% of a fifteen-suite wall clock —
  while making **1 488 of 59.7 M calls**, so it is almost the entire denominator and none of the
  numerator. Over the twelve the same data reads **2.43% and 8.06%**, both *larger* than the
  seven-suite figures, and 4-4's conclusion changes from *"too small to matter"* to *"too small to
  beat 4-5"*. **A fourth has since followed**: item 3-2's numeric-read table, whose 50.1% becomes
  **55.2% of 186 831 813** and whose *"3-2 is a Box2D item, 98% of the corpus's numeric reads are
  Box2D's"* becomes **9.6%** — the item was re-specified around a suite that turns out to be a
  fifteenth of its own population, while Typescript and Gameboy, 89% of it, had never been counted. **The catch came from a cross-check run for an unrelated reason** — a counters-off
  driver, to price the instrument's own overhead, which turned out to be nil (0.946×) and instead
  put the per-suite times side by side, where one row was 72% of the column. *A widening that fixes
  the numerator's coverage silently changes what belongs in the denominator; print the per-suite
  denominator before quoting any total built from it, and re-read the convention you already
  wrote down.*
- **A validated claim is validated of the property it tested, and the sentence that records it will
  drift to the property the item cares about.** `0104` predicted which bindings a deferred body
  captures and checked **membership**: zero missed on 5 157 sites, an honest and load-bearing
  result. Item 1-1's obstacle, in the item's own words, is *"a captured name's **index**"*. Between
  the check and the write-up the sentence became *"the capture layout `0104` settled"*, and four
  later patches — and several paragraphs written by the same person who ran the check — repeated it.
  **The prediction was a `HashSet` derived from a `HashSet`: it had no order, so it could not have
  answered the index question even in principle.** Nobody had to be careless for this; the drift is
  from a true sentence to a shorter one, and the shorter one is the one that gets quoted.
  *Restate the item's obstacle in the item's own words next to the result, and check that the result
  is about the same noun.* Asked properly, the answer was reassuring — 0 mismatches on 4 461
  comparable sites — **and it changed a design constraint**: over-approximation, recorded as a cost,
  shifts every later slot, so the prediction has to **drive** the layout rather than match it.
- **Price a fix before you build it, on the same terms you priced the problem.** Item 4-5's cost
  was measured (~44 ns on 60.16% of calls, 1.46% of the corpus) and its fix was then *named* — move
  the per-invocation frame off the function object onto a thread-local stack — with a size attached
  by inference rather than by measurement, which is the step that usually goes unexamined because
  the problem's number feels like it transfers to the solution. It does not. Priced, the relocation
  is **0.730×: 6.19 ns of the 22.96 the current shape costs, 0.20% of the corpus, for an M–L with a
  generator-suspension hazard in it.** *A third arm said why in one line* — a single 56-byte
  `Arguments` copy is 8.19 ns, so the cost is the **copying**, and the fix moved where the copying
  lands without removing any of it. **The arm that decides a fix is not the arm that measured the
  problem**, and the cheapest version of it is usually one line of the proposed design run in
  isolation. Doing it cost one probe and saved an M–L that would have bought 0.2%.
- **When a component pass cannot account for the whole, suspect the components before the tool.**
  Item 4-5 priced every mechanism in a call's prologue by *replicating* it — five nested `using`s at
  0.011 ns, EH at 0.73, dispatch at 0.68 — got about **10 ns of a ~147 ns call**, and concluded that
  **~85% was "unattributable from outside the engine"**, blocking itself on a sampling profiler the
  container does not have. The replicas were right about what they measured and wrong about what
  was asked: *a replica prices the mechanism, and says nothing about what the engine's own scopes do
  inside themselves.* The engine already shipped the control — `InvokeCallback`, the same call with
  one scope instead of five — and 4-4 had even written down that pricing the two against each other
  was **"the first thing 4-5 should do"**. Taken, it says **50.18 ns of 114.60, 44%**, and the item
  was never blocked. **Two habits, and the second is the cheaper one**: when the parts do not sum to
  the whole, the missing mass is more often in *how* the parts were priced than in a part nobody
  named; and **before declaring an item blocked on a tool, re-read the item that produced it for the
  measurement it already specified.**
- **A residue you can only describe is a residue you have not measured — classify it, and be ready
  for the classification to indict the instrument.** `0105` reported 84.1% of re-entered function
  bodies reproducing the eager tree and characterised the other 15.9% as *"ordinal divergence on
  every instance examined"* — an honest sentence, and an anecdote: it names what the failures looked
  like to somebody reading them, over a sample nobody counted. Classifying them cost one enum and
  two counters, and **three of the four causes turned out not to be about the mechanism at all**.
  The largest was the comparison's own ordinal table, shared across gensym families and keyed on the
  bare number, so `Context3` and `#TempJSValue3` collided and desynchronised every ordinal after
  them. The next two were the check's *second compilation*: it exhausts item 4-2b's process-wide
  site table (24 759 → exactly its 65 536 cap on one corpus), and it races the tier-2 rule that
  re-uses a tier-1 site. **Nothing was left over.** *The value of a classification is not the
  categories you expect to fill — it is the empty "other" bucket at the end, which is the only
  thing that turns "every instance I looked at" into "every instance".* And a checker's residue is
  the first place to look for the checker's own defects, because that is where they are indistin­
  guishable from the subject's.
- **A two-arm microbenchmark run in blocks is measuring the process's history, not the arms.** The
  probe that priced item 4-2's arithmetic half ran each arm's six samples consecutively and came
  back with generic-arm spreads of **161%, 76% and 470%** against effects near 3× — the exact
  condition §3.5 already forbids reading, produced by the instrument rather than found by it. It
  reported `multiply-generic` at **39.00 ns** and `less-generic` at **20.67 ns**; the same code,
  run round-robin with the arms reversed on alternate rounds and a blocking collection between
  samples, reports **15.42** and **3.93**. *A 2.5× and a 5.4× error, both in the direction that
  would have founded the item.* Consecutive samples hand each arm a private slice of the process —
  its own gen-0 debt, its own place in the tiered-JIT ramp, whatever the previous arm left on the
  heap — and in a fixed order the same arm pays the same debt every round, so the error is
  systematic rather than noisy and averaging more samples does not remove it. **Interleave the
  arms, reverse on alternate rounds, and ratio *within* a round**: a ratio of medians inherits
  whatever differed between the blocks, while a median of within-round ratios divides it out. On
  these arms the per-arm spreads stayed above 60% and the pair ratios were still clean at 11/12 and
  12/12 — which is the whole argument for the pairing in one line.

---


---

## Appendix A — reproducing the measurements

### The engine probes

Each scenario is `ctx.Eval`'d once to warm and compile, then measured on a second
evaluation. Timing is `Stopwatch`; allocation is
`GC.GetAllocatedBytesForCurrentThread()` deltas after a forced gen2 collection. Cache
behaviour is read from `PropertyOptimizationDiagnostics.Snapshot()` after `Reset()` —
note the counters default to **off** since P0-1 and need an `Enable()` scope.

```js
// loop-empty            (3M)  var s=0; for (var i=0;i<3000000;i++) { s=i; } return s;
// arith-add             (3M)  var s=0; for (var i=0;i<3000000;i++) { s=s+i; } return s;
// prop-own-get          (3M)  var o={x:1,y:2}; ... s=s+o.x;
// prop-own-set          (3M)  var o={x:1};     ... o.x=i;
// fn-call               (1M)  function f(a){return a;}              ... s=s+f(i);
// fn-call-strict        (1M)  'use strict'; function f(a){return a;} ...
// closure-call          (1M)  var k=1; var f=function(a){return a+k;} ...
// proto-method-call     (1M)  function P(v){this.v=v;} P.prototype.get=function(){return this.v;};
// class-field           (3M)  class C { constructor(v){this.v=v;} }  ... s=s+c.v;
// builtin-call          (1M)  s = Math.max(s, i);
// array-rw              (1M)  var a=new Array(1000); ... s=s+a[i%1000];
// obj-alloc            (500k) last = {a:i, b:i+1, c:i+2};
// array-push           (500k) a.push(i);
// string-concat        (200k) s = 'x' + i;
```

Real-world scripts are the repository's own
`Broiler.JS/OtherTests/JIntPerfTests/Scripts/*.js`, each in a fresh `JSContext`.

### The front-end probes (phase 1)

Both take the same benchmarks host as every other emitter above. `--compile-profile` needs
an Octane checkout, because the shapes that matter here — hundreds of sibling declarations,
one IIFE holding hundreds of nested functions — do not occur in hand-written test sources;
`--compile-scaling` generates its own, since its job is to vary one property at a time.

```bash
cd Broiler.JS/Broiler.JS
DLL=benchmarks/Broiler.JavaScript.Engine.Benchmarks/bin/Release/net10.0/Broiler.JavaScript.Engine.Benchmarks.dll

# How much of each corpus's compile is function bodies (sizes item 1-1).
# Third argument is repetitions; the report is a median. Mandreel dominates the runtime.
dotnet $DLL --compile-profile /path/to/octane 3

# Parse / expression-tree / IL-emission split, against declaration count and name length
# (this is what found item 1-4). Streams a row per shape to stderr as it completes.
dotnet $DLL --compile-scaling

# The same three-way split as --compile-scaling, but on the REAL corpora and against the
# body-free control, plus the closure rewrite as its own column (sizes 1-1's remaining half).
# One corpus per process, for --compile-profile's reason. Third argument is repetitions.
dotnet $DLL --compile-phases /path/to/octane 5 codeload-jquery

# How many of a script's functions are ever invoked once it has been evaluated — the
# population 1-1's remaining half is worth. Evaluating and stopping is CodeLoad's own shape.
dotnet $DLL --defer-population /path/to/octane codeload-jquery
```

`--compile-profile` builds its control by replacing every outermost function body with `{}`
and **re-parses it before timing anything** — a control the parser rejects would measure
failing early rather than compiling less. Set `BROILER_COMPILE_PROFILE_DUMP=<dir>` to write
each control out; that is how Mandreel's residue was read.

**`--compile-profile`'s control is not a ceiling for every corpus, and jQuery is the one it is
wrong about.** It stubs every *outermost* function body, and jQuery has exactly one — the IIFE
the whole library is written inside, which `bodyByteShare` reports as **99.91% of the source**.
So its control is an empty file, `full − stub` is the whole compile, and the 96.5% "ceiling" in
1-1's table is *everything except the parse* rather than anything a deferral can take: CodeLoad
evaluates jQuery, which runs that IIFE. `--defer-population` is the instrument that answers the
question the ceiling was being asked, because it counts what is never invoked instead of what is
inside a body.

**`--compile-phases` takes its end-to-end check first, and that ordering is the measurement.**
Every compile in the probe registers a deferred site per relayed lambda, each rooted by a
`GCHandle` that is never freed and each holding its subtree, so a phase timed late in the
sequence pays collection time the phases before it caused. Taken last, the end-to-end column read
**3.4× the sum of the phases on Box2D and 1.0× on jQuery** — and the difference between those two
corpora is exactly how many sites a *deferred* compile registers: **982 against 1**, because
jQuery's top level relays one IIFE and Box2D's relays every one of its top-level functions. That
is item 1-1's own retained-tree artifact one level down from where this document records it.

Both phase-1 changes carry a switch so they can be A/B'd on a single build, which is the only
way to compare two compilers without also comparing two builds:

```bash
# Item 1-4: scope size above which the closure rewrite indexes instead of scanning
# (default 32). Any value larger than a real scope restores the pre-1-4 linear scan.
BROILER_JS_REWRITER_INDEX_THRESHOLD=1000000000 dotnet $DLL --compile-profile /path/to/octane 1

# Item 1-1: 0 restores eager IL generation. Default is on.
BROILER_JS_DEFER_IL=0 dotnet $DLL --compile-profile /path/to/octane 1

# Item 1-1's remaining half: 0 restores the relay-time closure rewrite of a subtree an
# enclosing walk has already rewritten. Default is on. --defer-population reports the three
# counters behind it: relaysRewritten (0 on every corpus), relaysSkipped, and — only on the
# arm below, which is the one that still runs the repeat — capturesInRepeat, the captures the
# repeat creates that the first walk had not. Also 0 on every corpus, and it is the counter
# that separates "the walk repeats" from "the walk is inert".
BROILER_JS_RELAY_REWRITE_ONCE=0 dotnet $DLL --defer-population /path/to/octane codeload-jquery
BROILER_JS_RELAY_REWRITE_ONCE=0 dotnet $DLL --compile-profile /path/to/octane 1 codeload-jquery

# Item 3-7: 0 restores the JSVariable cell for a numeric local a closure names.
# Default is on. The soundness conjunct it does NOT gate — a name a hoisted function
# declaration mentions — holds on both settings, so the two arms differ only in policy.
BROILER_JS_CAPTURED_NUMERIC_LOCALS=0 dotnet $DLL --local-alloc
BROILER_JS_CAPTURED_NUMERIC_LOCALS=0 dotnet $DLL --specializing-tier /path/to/octane baseline counters

# Item 3-8: 0 removes the numeric-local tier ENTIRELY — every raw double local, not one
# item's increment to it. This is the control four phase-3 items were each measured
# without, and the arm that says the whole mechanism is worth 0.36% of the engine's
# number boxing. Default is on.
BROILER_JS_NUMERIC_LOCALS=0 dotnet $DLL --specializing-tier /path/to/octane baseline counters

# Item 3-1: 0 restores the generic JSValue operators for `&`, `|`, `^`, `<<`, `>>`, `>>>`
# on two proven-numeric operands. Default is on. Worth a full box on its shape and
# exactly nothing on the corpus, because the operands there are array elements.
BROILER_JS_NATIVE_BITWISE=0 dotnet $DLL --local-alloc

# Item 3-1's shared half: 0 restores the unguarded emission for an arithmetic tree over
# operands the compiler cannot prove numeric. Default is on.
BROILER_JS_NUMERIC_SPECULATION=0 dotnet $DLL --specializing-tier /path/to/octane baseline counters

# Item 3-1's order-preserving half: 0 restores the HOISTING form of the guarded tree —
# every leaf evaluated into a temporary ahead of one combined test, and the purity rule
# that needs. Default is on. This is the arm to compare against, not the one above:
# BROILER_JS_NUMERIC_SPECULATION=0 turns the whole guarded tree off and would charge this
# change for what 0084 already did.
BROILER_JS_NUMERIC_TREE_ORDER=0 dotnet $DLL --specializing-tier /path/to/octane baseline counters

# Item 3-8a: 1 turns ON the dual-representation speculative numeric local — a raw double, a
# flag, and the JSValue slot, with the ++/-- step, the guarded tree's leaf, the element read
# and the element write all able to take the raw half. Default is OFF, and it stays off: the
# arm is a measured 1.2% regression on the corpus's boxing, and boxingSpeculativeReadRequests
# says why in one number. Kept switchable because the mechanism is correct and tested on both
# settings, so a future workload with a different read/write ratio can be measured on it
# without rebuilding it.
BROILER_JS_SPECULATIVE_NUMERIC_LOCALS=1 dotnet $DLL --specializing-tier /path/to/octane baseline counters

# Phase 5 item 2: 1 turns ON the per-pattern RegexOptions.Compiled decision — after a pattern's
# thousandth match the engine builds the compiled form, times both arms on the subject in hand,
# and keeps the winner. Default is OFF. The switch is the arm to measure against; `--regex-tiering`
# flips it internally and reports both, so it needs no environment at all.
BROILER_JS_REGEX_TIERING=1 dotnet $DLL --specializing-tier /path/to/octane baseline counters
```

**`--specializing-tier … counters` also reports item 3-9's population, and the counter that makes
its zero readable.** `importedOuterNumericCandidates` is how many locals would be numeric if an
identifier resolving to a numeric local of an ENCLOSING function were typed rather than classified
`OtherName` — computed by difference against the real fixed point, like 3-8a's, so it cannot drift
from what the analysis does. It reads **0 on all seven suites**, and a single zero cannot separate
"nested functions never read an enclosing numeric local" from "they read them constantly and never
anywhere typable", so `importedOuterNumericOffers` counts **how often the enclosing scope chain
answers that a name is already a raw `double`** while the pass runs. That is **0 too**: the reads do
not exist. Both are COMPILE-time counters on the same terms as
`speculativeNumericCandidates` — the switch (`BROILER_JS_OUTER_NUMERIC_COUNT=1`) has to be on before
the corpus is compiled — and **`importedOuterNumericCandidates` is bounded above by
`speculativeNumericCandidates` by construction**, since everything an enclosing scope has *proved*
numeric is also something 3-8a's pass would have *assumed* numeric. A reading above 26 on this
corpus is a defect in the counter rather than a discovery, and every fixture asserts the bound.

**`--specializing-tier` takes an optional fifth argument: a comma-separated list of suites.**

```bash
# The whole corpus, all fifteen suites, counters on.
dotnet $DLL --specializing-tier /path/to/octane Specializing counters

# Only these, when an earlier suite aborts the process and costs every suite after it.
dotnet $DLL --specializing-tier /path/to/octane Specializing counters "Gameboy,Box2D,zlib,CodeLoad,Typescript"
```

It exists because **checkpointing after every suite retains the rows before an abort and still
loses every row after one** — the suites run in one process in a fixed order, so Mandreel taking
the process down had cost Gameboy, Typescript, Box2D, zlib and CodeLoad in §4.2a's widened run as
well, which is why that section reports twelve suites rather than fifteen. A filtered run writes
its checkpoint to a **different path** (`broiler-specializing-tier-partial-<suites>.json`), so a
partial corpus can never overwrite the full one's and be read later as though it were complete, and
a suite named but not recognised is an error rather than an empty selection. Item `0103`'s
fifteen-suite table is two runs combined this way.

**`--specializing-tier` reports GC pause per suite on *both* modes, `counters` and `timing`**
(item 3-1): `gcPauseMs` is `GC.GetTotalPauseDuration()` across the driver run — the runtime's own
accounting of time with execution suspended, exact rather than sampled — with `gen0Collections`,
`gen1Collections` and `gen2Collections` beside it, because pause time alone cannot separate "many
cheap gen0s" from "a few expensive gen2s" and those want opposite follow-ups. **Read it against
`elapsedMs` before pricing any allocation item**: it comes out at **1.8–2.0%**, so the collector is
not what an allocation change buys back, and read the *difference* between two arms against the
difference in `elapsedMs` — 54 ms of 768 ms — to see that the rest is the mutator. It costs four
`GC` reads per suite and is on unconditionally, since it is not on any hot path.

**A sampling profiler is not a substitute for it and was checked.** `dotnet-trace collect --format
speedscope --providers Microsoft-DotNETCore-SampleProfiler -- dotnet $DLL --specializing-tier …`
runs and converts cleanly, and then says almost nothing: the driver inflates from ~19.5 s to
**25.4 s**, **28.0%** of self time lands in `Thread.PollGCWorker` (the rendezvous its own stack
walks force, *not* collection — the counter above says collection is 1.8%), and compiled JavaScript
lives in `DynamicMethod`s the stack walker cannot name, so **47.8%** of the run is
`JSFunction.InvokeFunction` with an anonymous JavaScript frame beneath it against **2.4%** on a
named body. Item 4-5's "blocked on a profiler" needs a tool that can symbolicate a `DynamicMethod`.

**`--specializing-tier … counters` also reports item 3-8a's population**: `speculativeNumericCandidates`
is how many locals the analysis would prove numeric if a name the function neither declares nor
takes as a parameter were known to hold a number — computed by difference against the real fixed
point rather than by a new rule, so it cannot drift from what the analysis does. **It is a
COMPILE-time counter and its switch (`SpeculativeNumericLocals.Counting`,
`BROILER_JS_SPECULATIVE_NUMERIC_COUNT=1`) has to be on before the code being measured is compiled**
— set beside the run-time censuses it reports zero, which is how the first version of it was nearly
published. Read it against `numericLocals`: the corpus total is **232 → 258, 1.11×**, and the row that
matters is NavierStokes at **24 → 39, 1.62×**. Off by default because it costs a second analysis
pass per compiled function.

**`--specializing-tier … counters` also reports what item 3-8a's dual representation COSTS**:
`boxingSpeculativeReadRequests` counts boxes minted reading a speculative local, attributed at the
read by a fourth `JSNumber` factory entry (`CreateSpeculativeRead`) beside `CreateLiteral` and
`CreateConversion`. **This is the counter that decides the item, and it is the one that was built
last** — three consumers were converted first, each a guess at where the remaining boxes were,
checked only by whether the total moved. Read it against the fall in `arithmeticUpdateTargets`'
`LocalSlot` row, which is what the representation buys: the item pays exactly while the second
exceeds the first, and on NavierStokes it is **393 705 against ≈5 300**. A run-time counter, so
unlike `speculativeNumericCandidates` it needs no switch of its own beyond
`NumberBoxingDiagnostics.Enabled`.

**`--specializing-tier … counters` also reports where each `++`/`--` step's operand lives**
(item 3-1): `arithmeticUpdateTargets` splits `arithmeticUnaryUpdate` into `Element` (a computed
member), `Property` (a named one), `LocalCell` (a `JSVariable` cell — which is what a *top-level*
`var` is), `LocalSlot` (a statically-resolved local or parameter the numeric analysis did not prove
numeric), `GlobalOrWith` and `Other`. The kind is a compile-time constant carried into the step, so
the run-time cost is the `Enabled` test the step already paid. **The rows sum to
`arithmeticUnaryUpdate` by construction** — the total is recorded by `Increment` itself and the
rows by the overload the compiler calls — so an emission site the census forgot appears as a
shortfall rather than vanishing, and `Other` at a non-zero value is a signal to go back. **Read
them as requests and multiply by the suite's own request-to-allocation ratio before calling them
memory**: Crypto's 7.2 M steps are 0.1% real (the small-integer cache answers its counters) and
NavierStokes' 9.46 M are 71.4%. **A numeric local appears in no row at all**, which is the point
rather than a gap — `i++` on a raw double is a native add that never reaches `Increment` — and it
is what makes 98.1% in `LocalSlot` a statement about the tier's *coverage* rather than about the
operator.

**`--specializing-tier … counters` also reports the numeric-tree refusal waterfall** (item 3-1):
`numericTreeRefusals` attributes every candidate arithmetic node to the **first** eligibility
condition it fails, on the same terms as `numericRejections` — so the counts add up and each row
reads as "widen this and that many sites move". Only a binary node whose operator has a native form
is a candidate; counting anything else would put every `===` and `&&` in the denominator. **Read it
knowing that a refused root re-offers its children**, so a refused chain contributes several rows
and the totals are of candidate *nodes*, not of source expressions — which is the right denominator
here, since the question is how much arithmetic reaches the guarded form. `numericTreeOrderBlockers`
reads against the `OrderUnsafe` row alone, which is its total, and names the kind of leaf that
blocked it: **1 028 property reads against 34 element reads** is what said the order rule is not an
array problem. Both are compile-time counters touched once per site, so they are unconditional and
have no `Enabled` flag.

**`--specializing-tier … counters` reports `cacheHitsNumeric`** (item 3-2): of the property reads
the inline cache answers, how many hand back a number. This is item 4-1's third signal —
"numeric-vs-generic per site" — which 4-1 left uncollected and 3-8 named as the missing instrument.
It costs one `IsNumber` test on the two hit returns and only while
`PropertyOptimizationDiagnostics.Enabled`. **Read it per suite:** the corpus total is 50.1%, and
that single figure conceals Box2D at 54.0% of 18.2 M reads against NavierStokes at 0% of 388.
**And that 50.1% was another seven-suite figure**: over the twelve that run it is **55.2% of
186 831 813** — the seven are **10.7%** of the corpus's cache-answered reads — which **inverts item
3-2's plan**. Box2D is **9.6%** of the corpus's numeric reads rather than 98%; **Typescript
(64.2 M) and Gameboy (27.4 M) are 89% of them** and neither had been counted.

**`--specializing-tier … counters` also reports the arithmetic-operand census** (item 3-1):
`arithmeticGeneric` is every invocation of a generic two-`JSValue` arithmetic or bitwise operator,
`arithmeticBothNumbers` the subset whose operands were already Numbers before any coercion — i.e.
what a native form guarded on that test could answer — and `arithmeticRawDouble` the shape item 3-5
specialized for `<` and `>`, one side an unboxed double and the other a `JSValue`. Read the second
against `boxesAllocated`, not against the first: 100.00% of the invocations is what says the guard
predicts, and **86.6% of the boxes** is what says the guard is worth building. `arithmeticRawDouble`
counts `+` alone, because it is the only operator with a `JSValue × double` overload — the other
four re-box a raw double to call the generic form. Counters are off by default
(`ArithmeticOperandDiagnostics.Enabled`); the emitter turns them on around the driver run only.

**The census covers the unary operators too, and `arithmeticGeneric` alone will under-report.**
`arithmeticUnaryNegate` is `-x` and `~x`, `arithmeticUnaryUpdate` the `++`/`--` step, and
`arithmeticUnaryToNumeric` the coercion of a `++`/`--` operand — which mints unconditionally, so
the last two are equal on any run whose updates are all on Numbers and **`++` is two boxes, not
one**. They are 30.9% of the corpus's boxing against the binary operators' 47.6%, so a reading that
takes `arithmeticGeneric` for "the operators" is short by two fifths. **The attribution only closes
when every source is subtracted**: `boxingRequests` minus `boxingConversionRequests`,
`boxingLiteralRequests`, `arithmeticGeneric` and the three unary columns leaves 1.0%, which is
builtins reaching the factory directly. Anything larger than that means a hook is missing, which is
how `BitwiseXor` — unhooked, and silent about it — was found.

**`--specializing-tier … counters` also reports the boxing census** (item 3-8):
`boxingRequests` is every call to `JSNumber.Create`, `boxesCached` the share the small-integer
table answers without allocating, and `boxesAllocated` the rest — the last times 24 B is the
ceiling on every raw-double item in phase 3 at once. `boxingLiteralRequests` and
`boxingConversionRequests` split off two named callers through separate factory entries
(`CreateLiteral`, `CreateConversion`) rather than a stack walk: the first is a numeric literal
re-boxed to meet an operator, the second is the compiler carrying a raw double across into a
`JSValue` — **the ceiling on what a typed backing store can remove**, and 5.0% of NavierStokes'
requests against 31.0% of Crypto's. `NumberBoxingDiagnostics.Enabled` is off by
default and the emitter turns it on around the driver run only. **Read it per suite, never only as
a total**: the share runs from 0.31% on DeltaBlue to 66.96% on NavierStokes, and the average of the
seven hides both ends.

**Item 3-7's timing arms need one shape per process.** The winning arm removes two boxes per
iteration, so over 3 M iterations the *off* arm allocates ~192 MB more; run in one process its
collections are charged to whichever function runs next, and the control — identical code on both
arms — reads 1.2857× instead of ~1.000. Generate one file per shape, rotate
`off/on/on/off`, and read the null control first: a control outside the controlled lane's
predeclared A/A envelope invalidates the run.

**Give `--compile-profile` a corpus name as its fourth argument and run one per process.**
The corpora share a heap and item 1-1 keeps an un-generated lambda's tree alive, so a corpus
measured after Mandreel's 5 MB pays collection time that has nothing to do with its own
compile. Measured together, 1-1 read **1.6× and 2.6× slower** on the last two corpora and
0.56–0.65× faster on the first three, with bimodal ratios; measured one per process it is
0.64–0.83× on five of six. *That artifact cost a full A/B run to find, and the tell was the
bimodality, not the sign.*

```bash
BROILER_JS_DEFER_IL=1 dotnet $DLL --compile-profile /path/to/octane 1 codeload-jquery
```

**Phase 5's three regex emitters, and which question each answers.** `--regex-profile` measures
the matcher — nine JS-level shapes per subject character, plus the eleven Octane patterns through
`System.Text.RegularExpressions` with and without `RegexOptions.Compiled`. `--regex-tiering` runs
the same eleven **through the engine** on both settings of `BROILER_JS_REGEX_TIERING` and reports
which way each race went; it flips the switch itself, so it needs no environment.
`--regex-call-envelope` is the one that re-ordered the phase: the identical work at the identical
iteration count, once through `re.test` / `re.exec` / `String.prototype.search` and once through
`Regex.IsMatch` and `Regex.Match` directly, so the difference is everything the engine does around
a match. **Read the envelope first.** Its `-long` row is the discriminator that stops the 2 431 B
per call being mistaken for a subject copy — the same anchored pattern on a subject 18.8× longer
allocates the same bytes to the digit.

```bash
dotnet $DLL --regex-profile
dotnet $DLL --regex-tiering
dotnet $DLL --regex-call-envelope
```

**These probes now have a permanent home** — `HotPathProbeBenchmarks` in
`Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks`, wired into all three
`Broiler.JS/eng/performance/phase0.json` profiles, with phase C's hit rates on their own
`--cache-metrics` emitter (item 0-9). That landed in `aa2b1562` and is carried by the
pinned pointer, so this appendix is the description of the corpus rather than the only copy
of it. §4.1's figures are still one-off *observations* — they were taken by the ad-hoc
harness, and the corpus has since contradicted two of its rows (see 0-9) — but they are now
checkable from a clean checkout, which is the part that was missing.

### Octane

```bash
# Full run (clones chromium/octane, builds BroilerJS, installs Chromium):
./scripts/run-octane-benchmarks.sh --repetitions 3

# Faster local iteration against an existing checkout / build:
./scripts/run-octane-benchmarks.sh --octane-dir /path/to/octane --skip-build --engines broiler

# Re-run one suite with the child's output streamed live:
./scripts/run-octane-benchmarks.sh --engines broiler --skip-build --only Crypto --verbose
```

`--only` writes under `logs/partial/`, so a debugging run never overwrites committed
results. Also: `--keep-scripts` (keep the combined script for passing suites),
`--no-trace` (drop the breadcrumbs for an undisturbed timing run), and
`--broiler-env K=V` to pass an engine diagnostic switch through, e.g.
`--broiler-env BROILER_GENERATE_IL_LOGS=1`.

**Start a failure diagnosis at
`tests/octane/results/linux-x64/diagnostics.md`**, not
at the logs. For every suite that did not complete it gives the failing exception type,
the benchmark / phase / iteration it died in, the .NET stack, the JavaScript stack, and
a command to re-run that one suite. Three things make that possible: Broiler's managed
stack lives in the JS error's *message* and is captured in full rather than truncated;
stack traces are rewritten from the concatenated temp file back to `base.js:371`; and
the runner prints a breadcrumb on entering each `Setup`/`run`/`tearDown` phase and on
iterations 1, 2, 4, 8, …, so a suite that aborts the process still names what was live
when it died.

### test262

From the `Broiler.JS` submodule root:

```sh
python scripts/compliance/run_test262.py --path-file scripts/compliance/test262-<name>.txt \
  --suite-root <pinned checkout> \
  --broiler-dll Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll \
  --max-workers 8
```

`Broiler.JS/scripts/compliance/test262-failures.txt` is **generated** by
`Broiler.JS/.github/workflows/test262.yml` from a run's own results — a hand-written
entry is overwritten by the next run, and an entry only appears if some file actually
fails.
Gaps that no test262 file reaches are therefore pinned by repository tests instead
(`StrictModeFlowTests.KnownGap_AsyncAndGeneratorBodiesDoNotEnterRuntimeStrictMode`,
`ReflectSetReceiverAttributesTests`).

---
