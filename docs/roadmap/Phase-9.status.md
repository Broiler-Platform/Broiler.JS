# Phase 9 — VM 4.0: an adaptive two-tier engine — status

**No measurement exists. Nothing in phase 9 has been built, measured, or attempted.**

> The evidence half of [`Phase-9.md`](Phase-9.md). This file exists so the convention
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
| Blocked on | item **9-0 · Is tier-up worth it here?**, which has not been run |

**This phase is not scheduled.** It is written down so the option is specified rather than
vague, and so the entry measurement below is the first thing anyone doing it would have to
produce. Track two's justification is argued in
[`Roadmap.md`](Roadmap.md#track-two--the-vm-tier-phases-69), and **it is an argument, not a
measurement.**

---

## The entry measurement — item 9-0

**9-0 asks a question the catalogue's staging does not, because the catalogue assumes tier 1
is a VM. Here tier 1 is already IL, and it is fast.** So:

1. **Start-in-VM-and-promote against compile-everything-eagerly**, on the real corpora —
   time, allocation and peak working set together. The VM's case rests on phase 1's finding
   that **84-99.7% of a script's functions are never invoked**, so most eager compilation is
   wasted.
2. **The same comparison against item 1-1's deferral half**, which reaches that waste with
   no second tier and is already half-built (jQuery **0.661x**, PdfJS 0.689x, Box2D 0.636x,
   CodeLoad **1.099x**). **If 1-1 captures most of it, item 9-2 is redundant.**
3. **Promotion-threshold sensitivity — a curve, not a point.** Phase 5's item 5 is the
   precedent: a tiering race, built complete and correct, measured **1.010x on 3 of 6
   interleaved pairs** and shipped **off by default**, because no speed-up was worth a
   retained `DynamicMethod` per hot pattern. A per-function tier has the same failure mode
   at larger scale, and that outcome must stay available.

**Note what 9-0 does *not* gate.** Item **9-3** — deoptimize IL to bytecode — has an argument
independent of every number above: it is the mechanism phase 4's item 4-3 was specified for
and could not build, because there was no interpreter frame to reconstruct. **If 9-0 refuses
promotion, 9-3 may still be worth building**, and item 9-5 would then retire 4-3a's restart
compromise on the IL path.
