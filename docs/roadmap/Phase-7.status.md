# Phase 7 — VM 2.0: respectable JavaScript performance — status

**No measurement exists. Nothing in phase 7 has been built, measured, or attempted.**

> The evidence half of [`Phase-7.md`](Phase-7.md). This file exists so the convention
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
| Blocked on | item **7-0 · Profile the VM**, which has not been run |

**This phase is not scheduled.** It is written down so the option is specified rather than
vague, and so the entry measurement below is the first thing anyone doing it would have to
produce. Track two's justification is argued in
[`Roadmap.md`](Roadmap.md#track-two--the-vm-tier-phases-69), and **it is an argument, not a
measurement.**

---

## The entry measurement — item 7-0

**What 7-0 must report before any item in this phase is designed:**

1. **The VM's own Octane profile** — all **fifteen** suites, `--repetitions 3`, medians and
   a per-suite spread, exactly as [`Phase-0.md`](Phase-0.md) requires of the IL path. Seven
   suites is how phases 3 and 4 acquired a denominator error that qualified every headline
   they produced.
2. **The per-suite ratio to the IL path**, measured on the same machine at the same time.
   **This is the only context in which the two paths may be compared** — everywhere else the
   VM's competitor is *not running at all*.
3. **Where the VM's time goes**, by family: dispatch, operand decode, property, element,
   arithmetic, call, allocation, control flow.
4. **Allocation beside time**, per family. This campaign twice found the interesting half in
   the column it was not looking at, and a fresh interpreter is where that happens again.
5. **Operand-stack traffic as its own line** — `Duplicate`/`Pop` and redundant load/store
   pairs. That number, and only that number, decides item 7-5.
