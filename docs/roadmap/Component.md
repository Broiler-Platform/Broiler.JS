# Broiler.JS component roadmap

This file is the current, unfinished-work roadmap for everything **except** execution
speed. Completed compliance campaigns, performance phases, issue triage notes, and rename
logs are represented by Git history and regression tests rather than retained as active
plans.

> One of the owning documents indexed by [`docs/roadmap/README.md`](README.md). **Execution
> speed is cross-referenced by [`Roadmap.md`](Roadmap.md), while cross-track order is owned
> by [`Modernization.md`](Modernization.md).** Section 4 below is the seam and carries only
> deployment evidence that is not owned by a performance phase.
>
> This file was `docs/roadmap.md` until the 2026-08-07 consolidation.

## Sources of truth

- `scripts/compliance/test262-failures.txt` is the current tracked-failure manifest.
- `../compliance/dashboard.md` records publishable compliance evidence.
- `../compliance/known-gaps.md` groups active semantic and host-coverage gaps.
- `eng/performance/phase0.json` and `eng/performance/ownership.json` define performance
  jobs and semantic owners.
- `Measurement.md` explains how to collect comparable evidence.
- [`Concurrency.md`](Concurrency.md) owns JavaScript-local compile-ahead, independent-context
  safety, and Worker-agent acceptance; [`Concurrency.status.md`](Concurrency.status.md)
  distinguishes implemented aggregate-repository slices from accepted MOD-M5–MOD-M7 outcomes.

Do not duplicate a changing test count here. A roadmap item closes only after a local
regression, the relevant pinned public-suite run, and an updated dashboard agree.

## 1. Close the supported test262 failure set

The checked-in failure manifest still contains real failures across RegExp, array length
limits and mutation, comments/regular-expression literals, `continue`, direct `eval`,
lexical environments, and Annex B behavior.

For each failure cluster:

1. reproduce against the pinned suite revision;
2. reduce it to the narrowest repository test;
3. fix the owning parser, compiler, runtime, or built-in layer;
4. rerun the focused cluster and the affected full shard; and
5. remove the path from the manifest only when CI confirms the fix.

The older issue-673/675 documents were removed because they mixed closed and open
states. Any still-relevant direct-eval, `Intl.DateTimeFormat` range, SameValue, or Proxy
ordering defect must be tracked through the current failure manifest or a linked issue,
not resurrected from those snapshots.

Exit gate: the pinned supported-mode run has no unexpected failures and
`test262-failures.txt` contains no test paths.

### Immediate correctness gate: `TypedArray.prototype.set`

Modernization MOD-M0-8 records a suspected overlap/offset wrong-answer case in
`TypedArray.prototype.set`. Reproduce it with the narrowest regression before doing any
bulk-copy optimization. If it reproduces, correctness is fixed and the focused test plus
affected test262 shard land first; only then may MOD-M8-5 price an optional fast copy path.
Failure to reproduce is also recorded with the exact cases tried rather than silently
removing the item.

**It does not reproduce.** The gate is satisfied by pinning the behaviour instead:
`Broiler.JavaScript.Integration.Tests/TypedArraySetOverlapTests.cs` (21 cases) is the
narrowest regression the gate asked for, and every case already answers as §23.2.3.26
requires. The cases tried:

- **Overlap, source and target sharing one buffer with DIFFERENT element types** — the three
  that actually constrain an implementation, because a naive element-by-element in-place loop
  reads bytes it has already overwritten and gives a different answer than the
  clone-the-source-first that SetTypedArrayFromTypedArray specifies. A `Uint16`/`Uint32`/`Int16`
  source copied over an overlapping `Uint8` target each produced the clone-first answer
  (`1,2,3,1,3,5,7,8`, `1,2,3,4,1,5,7,8`, `1,2,1,3,5,6,7,8`) and not the naive one
  (`…,3,3,…`, `…,1,1,…`, `…,1,1,…`). Each test names both answers.
- **Overlap with the same element type**, shifting forward and backward by one element and by
  a four-element window at a byte offset; and a typed array set from itself.
- **Offsets** — a typed-array source and an array-like source at an offset, and a fractional
  offset truncated toward zero.
- **Range validation** — a typed source running past the end, an array-like longer than the
  target, and a negative offset, all `RangeError`; plus the ordering rule that the offset is
  rejected before any source element is read (an array-like's element getter never runs).
- **Element conversion** — wrapping into `Int8`, clamping and round-half-to-even into
  `Uint8Clamped`, float truncation into `Int32`, an array-like hole reading through `undefined`
  (0 in an integer target, `NaN` in a float one), and BigInt/Number content mixing throwing a
  `TypeError` in both directions.

So correctness is not blocking, and MOD-M8-5 may price an optional fast copy path — with these
tests as its guard, since a fast path is exactly what would reintroduce the overlap hazard.

## 2. Expand host-mode coverage

The `script`, `module` and `raw` modes are implemented and each reports its own selected,
executed, passed, failed, skipped and timed-out totals (`hostModeSummary`, carried through
the shard merge and both CI summaries). Module tests run in place under `--module-host`
with their harness preloaded as a script; raw tests are handed the file's own bytes.
`$262` defines `global`, `createRealm`, `detachArrayBuffer`, `evalScript` and `gc`, and a
test is excluded for the hook it names rather than for mentioning the host object.

`flags: [async]` results use test262's marker protocol, so an async test can fail; see
[known-gaps](../compliance/known-gaps.md#host-coverage-gaps) for the measured correction
and `scripts/compliance/fixtures/async-protocol/` for the fixtures that hold it, run by
`run_test262.py --self-check` before every CI shard.

What is left in this item:

- `$262.agent`, `$262.IsHTMLDDA` and `$262.AbstractModuleSource` are the remaining
  exclusions; the first is Worker-agent work owned by [`Concurrency.md`](Concurrency.md),
  and the other two need engine capabilities that do not exist. Each needs a published
  product decision rather than a host stub.
- Negative-metadata execution is still opt-in (`--include-negative`). Release workflows
  must turn it on and publish the totals for the pinned suite revision.
- The module mode's own failures — module early errors, binding initialisation, and the
  files that hang — are engine work, not host work, and belong to the language items above
  and to the aggregate scripts/tasks/modules track.

Exit gate: every test262 file is either executed by an appropriate host mode or has a
specific, published scope exclusion. Release workflows enable the supported modes by
default and publish totals for the pinned suite revision.

## 3. Finish RegExp backend adoption

Broiler.Regex is routed only for a conservative set of semantic gaps; the .NET
translator still handles the rest and still owns `Split`/`Replace`.

The component-owned work is tracked in
[`Broiler.Regex/docs/roadmap.md`](../../Broiler.Regex/docs/roadmap.md).
Broiler.JS owns the integration gate:

- route only features the native engine implements and tests;
- compare both backends during expansion;
- move `Exec`, `Split`, and `Replace` to one match-data abstraction; and
- retire the translator only after the pinned RegExp corpus is clean.

## 4. Performance and deployment evidence

The phase 0-5 optimization campaign is complete for the work it scoped (storage layouts,
startup, packaging, SIMD, tiering experiments). It did not leave the engine's steady-state
execution paths finished: a subsequent investigation found that the object-shape layout and
property inline cache those phases delivered are inert for most real JavaScript, and that
three pieces of always-on bookkeeping dominate the call path. That investigation became the
[performance and benchmark roadmap](Roadmap.md) — phases 0–5, judged on Octane and the
engine probes — and **none of it is tracked here.** The investigation itself is
[`Archive.md`](Archive.md), which is an archive and carries
diagnoses the plan has since corrected.

The deployment evidence and product decisions still outstanding are:

- establish controlled candidate/control acceptance lanes on Windows x64, Linux x64, and
  Linux Arm64, with lane-specific A/A calibration and effective-setting attestation;
- exercise x64 with AVX2 enabled and disabled and Arm64 with AdvSimd where claimed;
- keep allocation, latency, working set, publish bytes, and code size together;
- resolve or explicitly scope linker warnings before claiming trimmed support;
- remove legacy magic-name assembly probing after a documented compatibility window;
- decide whether feature satellites beyond the sample materially improve startup and
  working set; and
- keep function tiering, tagged-value experiments, and the portable Native AOT subset
  opt-in until their supported semantics and fallback behavior are release-tested.

**Four of those are now owned by [`Assemblies.md`](Assemblies.md)** rather than tracked here,
because they turn out to be one piece of work: resolving linker warnings before claiming
trimmed support, removing legacy magic-name assembly probing, deciding whether feature
satellites beyond the sample improve startup and working set, and making the portable subset
a real capability rather than a numeric island. **The magic-name probing in particular is
promoted from hygiene to blocker** — reflective discovery defeats Native AOT, so item A-7's
gate cannot pass while it exists.

Compile-ahead, optimizer-state isolation, and Worker agents are similarly routed to
[`Concurrency.md`](Concurrency.md). The aggregate `docs/architecture/multithreading.md`
retains cross-component implementation history and measurements, but does not close the
JavaScript-local MOD-M5–MOD-M7 gates.

No performance change closes on a developer or hosted-runner smoke result. Use MOD-M1 and the
fail-closed exact-row, A/A, practical-threshold, resource, source-identity, and semantic
gates in [`Measurement.md`](Measurement.md).

## 5. API, package, and preview readiness

> **The assembly layout this section governs is being re-laid.** Its first executable piece,
> [`AssemblySplit.md`](AssemblySplit.md), has structurally landed through S-6: the expression
> model now lives in `Broiler.JavaScript.Expressions`, while final S-7 validation remains
> open. [`Assemblies.md`](Assemblies.md) plans the rest, but its original Base/Core merge
> sketch is superseded pending the verified MOD-M2 graph. The current hypotheses keep a shared
> FrontEnd/Semantics layer independent of both IL and bytecode lowering and require a
> bytecode-only publish-and-run Native AOT gate. Item **A-9** is
> the `Broiler.JavaScript.*` → `Broiler.JS.*` rename, which is a **breaking change to every
> assembly name and package id** and closes under the rules below.

- Keep `../public-api.md` aligned with shipped assemblies and bootstrap profiles.
- Add pristine-consumer tests for every package intended for external use.
- Document breaking changes to assembly or bootstrap behavior before release.
- Run the complete repository, compliance, packaging, trim, and benchmark gates.
- Update `HUMAN_REVIEW.md` for the exact release commit and scope.

Broiler.JS is not a security sandbox. Compliance and performance completion must never
be presented as isolation of untrusted scripts.
