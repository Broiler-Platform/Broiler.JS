# Phase 0 — establish the baseline

**Nothing else in this campaign can be accepted until this is done**, and both source
roadmaps said so independently ([`Roadmap.md` §1.2](Roadmap.md#12-both-roadmaps-are-blocked-on-the-same-missing-thing)).
Discovery measurements and correctness work may continue, but their timings remain smoke or
prioritization evidence. This phase contains no engine optimization; its remaining work is
performance infrastructure and controlled-lane provisioning that cannot be completed in a
development container.

> The plan half of [`Phase-0.status.md`](Phase-0.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase is for

Phase 0 answers one question: **may a number produced by this campaign be used to accept or
reject a change?** Until the harness reports every expected row, distinguishes A/A stability
from a candidate threshold, identifies both source arms, attests the effective lane, and has
a permanent home for the probes and raw evidence, a phase-3 timing is prioritization evidence.
Every other phase's exit gate is written against instruments this phase builds.

**Owner:** the harness (`tests/octane/`, `scripts/run-octane.mjs`,
`.github/workflows/octane-benchmarks.yml` — all in the aggregate repository), plus
`eng/performance/phase0.json` and `benchmarks/Broiler.JavaScript.Engine.Benchmarks` here.

## Items

Two groups: the harness had to work at all, and then it had to produce evidence. **The
second group is the actual gate.**

### Harness readiness — all five landed

| # | Item | State |
|---|---|---|
| **0-1** | Land the pending `Broiler.JS` patches | ✅ landed; the pointer has advanced many times since |
| **0-2** | Stack reserve on by default in the shell | ✅ already on — the script host runs JS on a 16 MiB thread it sizes itself |
| **0-3** | Record each suite's real time budget | ✅ `timeoutSec` per suite; `--timeout` became a floor |
| **0-4** | Quantify run-to-run stability | ✅ `--repetitions`, median + spread, `--noise-band`, `flaky` status for harness smoke. The configured band is not a candidate decision threshold |
| **0-5** | Check the code cache against CodeLoad's intent | ✅ checked, no problem — but **re-check if `DictionaryCodeCache.Current` is ever uncommented in `Program.cs`**, because the shell would then measure cache lookup instead of compilation and nothing in a score would say so |

### Evidence owed — the gate

| # | Item | State | Next action |
|---|---|---|---|
| **0-6** | Run the Octane workflow and commit refreshed results, with a per-suite stability observation | ✅ **harness smoke complete** — 17/17 scores, all 15 suites `ok` for all three engines, `--repetitions 3`, 16 of 17 inside the provisional 7.5% band | Keep as coverage and historical continuity. An accepted delta is owned by 0-8, not by a later hosted-runner pair |
| **0-7** | A `PropertyOperationBenchmarks` / `FunctionCallBenchmarks` comparison | ⚠️ **run, not accepted** — win-x64 only, on a developer workstation, with `dirty: true` recorded but without the patch content needed to reproduce that source | Re-run candidate and immutable control on a controlled lane with a complete source manifest, exact expected rows and the semantic bundle |
| **0-8** | Decision-grade acceptance lanes for **win-x64, linux-x64, linux-arm64**, including effective CPU-feature/GC arms and applicable time, allocation, memory and size guardrails | ❌ **not satisfied on any RID.** The historical win-x64 A/A attempt failed its provisional band — 25 of 62 timing metrics outside 7.5%, worst 56% — and the present collector is not yet a fail-closed candidate/control comparator | Provision controlled lanes; implement exact-row/all-repetition comparison, effective-setting attestation, immutable source identity, semantic/test262 attachment and durable evidence retention |
| **0-9** | A permanent home for the Appendix A probes, wired into `eng/performance/phase0.json` | ✅ `HotPathProbeBenchmarks`, all 14 scenarios plus P2-4's, registered in all three profiles | None |
| **0-10** | Pinned test262 over the historical four-manifest baseline | ✅ 8 313 tests, zero engine failures in the recorded run | For every candidate decision, run the current manifest named by `ownership.json` on both source arms and retain the row-level result with the comparison |
| **0-11** | An `ownership.json` entry per item | ⚠️ **mapping implemented; validation open** — 39 current entries, but the configuration test still expects 21 | Retire the stale `tiered-unboxed-locals` duplicate, replace the fixed-count assertion with referential-integrity checks, and make an unknown/unowned comparison ID fail before measurement starts |

## What is left — acceptance infrastructure

**0-7, 0-8, and 0-11's validation repair are the remainder.** They require controlled hardware on three RIDs and
engineering in the collector/comparator: the current repeatability report compares neither
an immutable candidate/control pair nor every applicable resource metric. The win-x64 attempt
failing its own provisional band is useful smoke evidence, not a candidate verdict. Linux x64
and Linux Arm64 controlled lanes are unavailable in this environment regardless.

**Do not work around this.** Do not widen the band, intersect away missing rows, treat two
repetitions as statistical proof, or quote hosted/single-machine numbers as accepted. The
correct next action is to provision declared controlled lanes and finish the fail-closed
comparison path. GitHub-hosted CI remains valuable smoke coverage, but is not acceptance
hardware.

## Exit gate

1. Smoke coverage: 17 of 17 scores, no suite timed out under its effective manifest budget,
   and `comparison.md` reports the Broiler / Chromium / Jint triad — ✅. The default timeout
   floor is 180 s; Mandreel and zlib intentionally use 1,200 s and 1,800 s overrides.
2. Hosted-runner A/A observations identify per-suite instability — ✅ for smoke only. The
   recorded **5 of 13 outside the provisional band in a container and 1 of 17 on hosted CI**
   prove that an envelope does not transfer between machines; they do not calibrate an
   acceptance lane — ❌ for acceptance.
3. A fail-closed candidate/control comparator rejects missing, duplicate, failed, renamed or
   incomparable rows; consumes every repetition and applicable guardrail; and produces a
   predeclared `accept`, `reject`, `equivalent`, `below-resolution` or `invalid-run`
   decision — ❌.
4. Each claimed RID/CPU-feature/GC arm runs on controlled hardware, attests its effective
   settings, carries immutable source/dependency identity and attaches its semantic/test262
   bundle and durable raw evidence — ❌.

## Dependencies

**Phase 0 gates every performance/resource claim in phases 1–5, and retroactively gates
performance/resource closure of phases A–F.**
Nothing else depends on it being *finished* — items in later phases can be built and
measured — but nothing may be **claimed** until it is. See
[`Measurement.md`](Measurement.md).
