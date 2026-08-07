# Phase 0 — establish the baseline

**Nothing else in this campaign can be measured until this is done**, and both source
roadmaps said so independently ([`Roadmap.md` §1.2](Roadmap.md#12-both-roadmaps-are-blocked-on-the-same-missing-thing)).
**This phase contains no engineering.** It is the only one that blocks every other, and the
only one whose remaining items cannot be finished in a container.

> The plan half of [`Phase-0.status.md`](Phase-0.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase is for

Phase 0 answers one question: **may any number produced by this campaign be believed?**
Until the harness reports every suite, records its own noise, and has a permanent home for
the probes, a phase-3 result is an anecdote. Every other phase's exit gate is written
against instruments this phase builds.

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
| **0-4** | Quantify run-to-run noise | ✅ `--repetitions`, median + spread, `--noise-band`, `flaky` status |
| **0-5** | Check the code cache against CodeLoad's intent | ✅ checked, no problem — but **re-check if `DictionaryCodeCache.Current` is ever uncommented in `Program.cs`**, because the shell would then measure cache lookup instead of compilation and nothing in a score would say so |

### Evidence owed — the gate

| # | Item | State | Next action |
|---|---|---|---|
| **0-6** | Run the Octane workflow and commit refreshed results, with a noise band | ✅ **both halves** — 17/17 scores, all 15 suites `ok` for all three engines, `--repetitions 3`, 16 of 17 inside the declared 7.5% | None. The first *differenceable* pair is this run against the next banded one |
| **0-7** | A `PropertyOperationBenchmarks` / `FunctionCallBenchmarks` comparison | ⚠️ **run, not accepted** — win-x64 only, on a developer workstation | Re-run on an idle physical machine, or on CI |
| **0-8** | Two runs inside the band on **win-x64, linux-x64, linux-arm64**, reporting time, allocation and working set together | ❌ **not satisfied on any RID.** The win-x64 leg failed the band — 25 of 62 metrics outside 7.5%, worst 56% | **Needs hardware this environment does not have.** See below |
| **0-9** | A permanent home for the Appendix A probes, wired into `eng/performance/phase0.json` | ✅ `HotPathProbeBenchmarks`, all 14 scenarios plus P2-4's, registered in all three profiles | None |
| **0-10** | Pinned test262 over the four manifests | ✅ 8 313 tests, zero engine failures | Keep re-running it per item — that is §3.4's job, not this one's |
| **0-11** | An `ownership.json` entry per item | ✅ 37 entries | Retire the stale `tiered-unboxed-locals` entry, which duplicates `numeric-local-doubles`, when this phase is next revisited |

## What is left, and why it is not a task

**0-7 and 0-8 are the whole remainder, and neither is engineering.** They need an idle
physical machine on three RIDs. The win-x64 attempt failing its own band is **the protocol
working, not a defect**: `Measurement.md` requires an idle physical machine, the run was on
a 16-core developer workstation, and the band caught it. linux-x64 and linux-arm64 are
unavailable here regardless.

**Do not work around this.** The temptation is to widen the band or to quote the
single-machine numbers; both destroy the only thing phase 0 exists to produce. The correct
next action is to schedule the matrix on real hardware or in CI, not to relax the gate.

## Exit gate

1. 17 of 17 scores, no timeout at the 180 s floor, `comparison.md` reporting the triad
   (Broiler / Chromium / Jint) — ✅.
2. A noise band on record **from the machine the gate closes on** — ✅, and it taught the
   phase its own lesson: **5 of 13 outside the band in a container, 1 of 17 outside it on
   CI, so a band does not transfer between machines.** That is why the gate names the RID
   matrix rather than a number.
3. 0-7's BenchmarkDotNet comparison and 0-8's RID matrix, on idle physical hardware — ❌.

## Dependencies

**Phase 0 gates every claim in phases 1–5, and retroactively gates closing phases A–F.**
Nothing else depends on it being *finished* — items in later phases can be built and
measured — but nothing may be **claimed** until it is. See
[`Measurement.md`](Measurement.md).
