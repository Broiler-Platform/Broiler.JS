# Phase 2 — the call and property paths

Make the object shapes and inline caches the engine already has actually fire. Blocker
**B3**; **B6 is closed on the write path**. **Every item is landed or closed** — this
document is now mostly a record of *how* each closed, and one open question about the
phase's own exit criterion.

> The plan half of [`Phase-2.status.md`](Phase-2.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase was for

**Targets: DeltaBlue (601×), Richards (433×), Box2D (170×).**

This phase was exactly the "engineering deliberately left behind" table from the engine
campaign — a set of contained changes to structures that already exist and already work on
the sites they cover. **Best effort-to-value ratio on the list after phase 1**, and it held
up: the items landed, every one measured, and **three of eight turned out to be
mis-specified rather than merely undone** (2-2's targets, 2-3, 2-5).

The finding that opened the phase is worth restating, because it is the standing hazard of
reading any optimization list: **the shapes and the inline caches were already built, and
they were inert for most real JavaScript.** An ordinary property write destroyed the
object's shape; the cache did not cover prototype lookups, so method calls never hit.

**Owner assemblies:** `Broiler.JavaScript.Runtime`, `.Compiler`, `.Engine`.

## Items

| # | Item | State | Size |
|---|---|---|---|
| **2-0** | `new` retired every prototype-keyed cache entry in the process | ✅ landed | M |
| **2-1** | A store-cache entry that can describe a property *creation* | ✅ landed — **0 hits against 600 000 misses** before it | M |
| **2-2** | Widen shape eligibility | ✅ **arrays landed; the item's own four named targets were wrong** | M |
| **2-4** | `obj.name++` and `obj.name op= rhs` through both caches | ✅ landed, both halves — they reached **neither** cache before (0 hits *and* 0 misses) | M |
| **2-8** | Functions track their named properties by shape | ✅ landed — statics on a constructor were a 100% miss, DeltaBlue's hot path | M |
| **2-7** | The property map's 16-node floor costs ~1 KB per object | ✅ landed — **920 B unused** per first property; live map bytes 0.56× | M |
| **2-9** | Shape-tracked properties leave the radix trie | ✅ landed (2-3's successor) — a three-field object **0.36×**, an eight-field one **0.15×**; 16.2 M property maps over a run become 2.5 M | **L** |
| **2-3** | Remove the double storage | ⛔ **closed on measurements, twice** — not a pure removal, ~3% ceiling, and its premise is wrong: shape slots admit non-default attributes, which are per-object data a shared shape cannot hold. Superseded by 2-9 | — |
| **2-5** | Get strictness off the property-write path | ⛔ **closed — measured at 0%.** P0-2 had already taken the cost and 2-1 narrowed what was left | — |
| **2-6** | Monomorphic call-site caching | ⛔ **folded into 4-1** — there is no callee resolution to cache; a call costs ~250 ns and a call-site cache removes none of it | — |
| **2-10** | DeltaBlue's dictionary fallbacks | ✅ **found, fixed — and it is not the explanation** | S |

## The exit criterion, which splits and stays split

**Criterion: DeltaBlue and Richards inside 200× of Chromium.** Measured four times on two
machines, and it does not close:

| | Local | CI |
|---|---|---|
| **Richards** | 183× → **150×** after 2-11/2-12 | **144.9×** — ✅ passes |
| **DeltaBlue** | 576× → **447×** | **460×** — ❌ fails |

**2-13 decomposed the failing half against the third engine and bounded it.** DeltaBlue is
**2.83× harder than Richards for Broiler and 2.56× for Jint**, so **only 1.10× of the gap is
Broiler's**, and closing *all* of it reaches **362×** against a 200× gate.

**So the criterion is not reachable by removing a Broiler-specific deficiency**, and this is
the phase's most important handoff:

- Broiler is **ahead of Jint** on DeltaBlue (0.77×) as it is on Richards (0.69×).
- Read polymorphism is **falsified** as the cause — Crypto is 73.82% monomorphic and is
  Broiler's *best* suite against Jint.
- The genuinely Broiler-specific suites are **MandreelLatency (54.3×), CodeLoad (37.8×) and
  zlib (12.0×)** — none of them phase 2's.

**Next action: none in this phase.** The open question is about the *gate*, not the engine —
whether a criterion naming two suites that a competing engine also finds hard is measuring
Broiler at all. Raise it when the phase is closed under
[`Measurement.md`](Measurement.md), and re-aim at MandreelLatency, CodeLoad and zlib.

## Exit gate

1. An `ownership.json` entry and owned tests **per item** — ✅.
2. test262 properties/strict-mode manifests — ✅ satisfied, and re-run for 2-9, which
   rewrites the storage underneath `OrdinarySetWithOwnDescriptor`.
3. DeltaBlue and Richards inside 200× — **Richards ✅, DeltaBlue ❌, and 2-13 says the
   remainder is not Broiler's to close.** See above.
4. Everything closes under [`Measurement.md`](Measurement.md).

## Dependencies

Independent of phases 1 and 5. **2-1 makes 3-2 cheaper.** What was 2-6 is now inside 4-1,
and 4-2b's specialized read consumes 2-9's slot layout — so phase 2's storage work is the
foundation phase 4 speculates into.
