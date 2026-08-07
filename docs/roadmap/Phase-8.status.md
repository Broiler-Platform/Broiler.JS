# Phase 8 — VM 3.0: a highly optimized interpreter — status

**No measurement exists. Nothing in phase 8 has been built, measured, or attempted.**

> The evidence half of [`Phase-8.md`](Phase-8.md). This file exists so the convention
> holds across the whole directory — **a plan document is never the place a number lives** —
> and so the phase's entry measurement has somewhere to land. When the first result arrives,
> it goes here.
>
> **Nothing in the plan document may be quoted as a result.** Every figure it cites is
> borrowed from phases 0-5 and is attributed there. [`Measurement.md`](Measurement.md)
> governs what may be claimed.

---

## State

| | |
|---|---|
| Items started | **0** |
| Items landed | **0** |
| Measurements taken | **0** |
| Blocked on | item **8-0 · Decompose the VM's execution time**, which has not been run |

**This phase is not scheduled.** It is written down so the option is specified rather than
vague, and so the entry measurement below is the first thing anyone doing it would have to
produce. Track two's justification is argued in
[`Roadmap.md`](Roadmap.md#track-two--the-vm-tier-phases-69), and **it is an argument, not a
measurement.**

---

## The entry measurement — item 8-0

**8-0 does not establish a baseline — 7-0 does. 8-0 attributes it**, and every other item in
phase 8 exists only if this measurement justifies it:

1. **Dispatch, operand decode, and the operations**, split by opcode family, per suite and
   in aggregate — over all fifteen suites.
2. **A bigram histogram of executed opcode pairs.** This is item 8-3's entire justification
   and costs almost nothing once the loop is instrumented. A superinstruction set copied
   from another engine is a set for another engine's compiler.
3. **Per-opcode operand-type distributions** — the population item 8-2 would serve, to be
   compared against item 4-2c's census, which refused the IL-path analogue at **0.119% of
   the corpus** with a failing guard costing **18.567x**.
4. **A rate for each candidate**: nanoseconds per operation, not a share of the total.

**Instrument the loop; do not sample it.** Phase 4 established that a sampling profiler does
not decompose this engine — it inflated the driver ~29%, its biggest frame was its own
rendezvous point, and compiled JavaScript does not symbolicate. **A VM does not have that
problem**: the interpreter loop is one method and counters inside it are exact. That is a
real advantage of the VM tier and it should be used.
