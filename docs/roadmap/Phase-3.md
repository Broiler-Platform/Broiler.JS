# Phase 3 — value representation

Number boxing, which is **41.89% of everything the corpus allocates**. Blocker **B1** — the
largest total win in the plan and the largest change. Deliberately after phases 1 and 2
because those are contained and this is not.

> The plan half of [`Phase-3.status.md`](Phase-3.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase is for

**Targets: Crypto (301×), zlib (340×), RayTrace (291×), EarleyBoyer (270×), Splay (152×),
NavierStokes (104×).**

**Owner assemblies:** `Broiler.JavaScript.Storage`, `.Runtime`, `.Compiler`.

### The one sentence to carry out of this phase

**A box is minted by the operator, not by the local, and not by the store.** Every item that
ignored this lost, and every item that acted on it won:

| Approach | Boxes removed |
|---|---|
| The **whole** raw-`double`-local tier — every item from P2-2 onward | **0.36%** |
| The guarded arithmetic tree, applied at the operator | **54.0%** |

That is a factor of 150 between two applications of the same idea, and it re-ordered this
phase three times.

### The second sentence, which sizes what is left

**54.0% of the allocation bought 3.1% of the time.** (12.2% bought 1.9%, and the third
reading agrees.) Collection is **1.8–2.0% of the driver**, and of the 768 ms an allocation
change removed, only **54 ms was collection** — *a box costs about fourteen times more to
create than to collect.*

At **711 ms per GB**, the **0.70 GB of number boxes left is worth ~2.6% of the driver**.
**Everything remaining in this phase is an XL bidding for under 2%.** Bid with a **rate**
(ms per GB, ns per box), never with a share.

## Items

| # | Item | State | Size |
|---|---|---|---|
| **3-0** | Stop boxing the index of an indexed access | ✅ **landed, both halves** — a read now allocates **0.00 B/element** against 31.67 | M |
| **3-1** | Unboxed backing stores for dense arrays | ⚠️ **compiler half built (the big win); storage half re-opened as unmeasured** | **XL** |
| **3-2** | Unboxed doubles in shape slots | 🔍 **measured; its premise sentence is wrong. A Box2D item, and it goes after 3-1** | L |
| **3-3** | Widen the unboxed-locals eligibility gate | ✅ **complete, all three halves** | M |
| **3-4** | A tagged value representation | ⛔ **cost, do not start** — strongest case in the phase and still behind 3-1/3-2 | XL |
| **3-5** | A numeric local compared against a `JSValue` | ✅ landed — 3.4× on its shape, invisible on the corpus | M |
| **3-6** | Which conjunct costs the coverage | ✅ **counted, and it is none of the ones the item named.** Splits into 3-7 and 3-8 | L |
| **3-7** | A raw-`double` cell for a captured numeric local | ✅ landed — worth **8 names**, not the predicted 290 | L |
| **3-8** | Guard a local's numeric-ness at run time | ⛔ **3-8a built complete and closed as a measured regression.** Off by default, staying off | M |
| **3-9** | Import an enclosing scope's numeric conclusion | ⛔ **closed at a population of ZERO** — 0 names on all seven suites | S |

### 3-1 · Unboxed backing stores for dense arrays — **the phase's remaining work**

**Re-specified four times by its own measurements**, and the two halves now point in
different directions.

**The compiler half is built and is the phase's largest result.** What the generic
arithmetic operators are handed at run time had never been measured: **73 817 515 of
73 818 646 invocations arrive with both operands already Numbers** — every one but 1 131 —
while the compiler's own *both are native* proof reaches **0.75%** of the same invocations.
*Compile-time provability reaches 0.75% of the arithmetic; run-time truth reaches 100.00%.*

So the mechanism is a **run-time-guarded specialization of an arithmetic expression tree** —
evaluate each leaf once, test for Number, compute on raw doubles, **box only the root**.
Built, and then made much larger by one insight: **nothing required the leaves to move.**
Emitting each at its own postorder position and putting the test where the coercion would
have run leaves the purity rule nothing to protect. Result: **53.4 M → 6.6 M generic
invocations, 36 633 528 boxes removed, 54.0% of everything the corpus allocates.**

**The storage half is re-opened as unmeasured, not refuted** — and the reason is a
denominator error worth internalizing. The census producing every phase-3 figure ran **7 of
15 suites and never said so**. Widened, it reads **90.6 M boxes and 12.93 GB against 31.4 M
and 3.13 GB — 65.4% of the boxes are outside the seven**, with **Gameboy alone at 41.3 M,
1.32× the whole previously-measured corpus**, minting 26.9 M conversions at 51.0% of its own
requests **on a `Uint8Array` memory image** — which is precisely the shape a typed backing
store was written for.

**Next action.** Measure the typed backing store against **Gameboy and the eight suites the
census never ran**, not against the seven. Then, before building any representation, **count
the read/write ratio of the population it targets** — that is the rule 3-8a's regression
bought.

**Do not re-derive these.** Six items have built machinery that array-resident data cannot
reach; every one is correct, every one is invisible, and every one is waiting on this item:

- `o.x = 2` allocates **nothing** — a slot store is a reference copy.
- Field rows equal element rows **to the hundredth** (31.98 and 96.00 both), so 3-1 and 3-2
  are one mechanism with two backends.
- The bitwise and shift operators now have a native form and it removes **no boxes at all**
  on the corpus, because Crypto's digits live in `this.array[i]`.
- A numeric literal is **re-boxed on every evaluation** — 1.2% of requests, recorded and not
  built.

### 3-2 · Unboxed doubles in shape slots — **after 3-1**

**Its one-sentence premise is wrong.** `o.x = 2` allocates nothing; `vector.x = 1.5` pays for
the **literal**, not the slot. The slot's own 32 B appears only in `o.x = v * 1.5`, where the
stored value is already a raw double.

**It is a Box2D item and only a Box2D item.** 4-1's numeric-vs-generic signal splits the two
exactly: **50.1% of all cache-answered reads hand back a number, but 98% of those are
Box2D's**, while **NavierStokes performs 388 property reads, zero numeric, and mints
29 977 471 boxes**. So **3-1 carries 85% of the corpus's boxes and 3-2 carries Box2D's**, and
no work on shape slots reaches the other two suites.

**Most of the machinery already exists**: 4-2b's specialized read already resolves a
monomorphic read to a literal slot index.

### 3-8 / 3-9 · The numeric-local tier — **counted three times, and it is the remainder**

**Do not start either as written.** But note what three independent counts now agree on:

- **`++`/`--` are 30.9% of the corpus's boxing**, 51.6% of NavierStokes' and 80.4% of
  EarleyBoyer's — and of 17 282 144 steps, **LocalSlot is 98.1%, Element 0, Property 0.3%.**
  The step shares no mechanism with a typed store; it belongs to the numeric local.
- **44.36% of the 42.8 M root boxes are consumed by a LOCAL** (17.91% element, 13.14%
  property). A proven-numeric local already has a raw `double` home, so a root landing there
  is one the numeric tier **failed to type**.
- Weighting the refusals by execution refutes the seam hypothesis at 36 boxes of 18.6 M, and
  of the 19.0 M boxes consumed by a refused local, **38.41% are cascades with no independent
  cause** and **36.35% are `ElementRead`** — the conjunct 3-1's guarded tree already settles
  at run time.

**3-8a is the cautionary tale and the reason for the standing rule.** It was built complete
— dual representation, writes, the `++`/`--` step, and all three consumers that can take a
raw double — and each consumer moved the number without moving it enough (1.021×, 1.017×,
1.012×). A counter at the **read** settled it: **NavierStokes mints 393 705 boxes reading a
speculative local against ≈5 300 removed.** *Every premise the item was scoped on survived
and the item still lost*, because what makes it lose is the **read/write ratio of the code it
targets** — a property of the workload, not of the mechanism.

**Next action for the remainder: count the read/write ratio of the refused-local population
before building any representation for it.**

## Order

```
3-0 ✅ → 3-3 ✅ → 3-5 ✅ → 3-6 ✅ → 3-7 ✅ → 3-8/3-9 ⛔ counted and closed
      → 3-1 compiler half ✅ (54.0% of the corpus's allocation)
      → 3-1 storage half — measure on Gameboy and the eight unmeasured suites
      → 3-2 (Box2D's), sharing 3-1's compiler half
      → then cost 3-4, and nothing before it
```

## Exit gate

- `test262-arrays`, `test262-binary-data`, and — added by 3-3's `let`/`const` half —
  `test262-lexical-declarations`.
- **Allocation reported per item alongside time.** This phase twice found the interesting
  half in the column it was not looking at.
- **A rate, not a share.** EarleyBoyer halved its boxes for 1.002×, because 82 000 a second
  is not 4 240 000 a second.
- Everything closes under [`Measurement.md`](Measurement.md).

## Dependencies

3-2 is cheaper after 2-1 and after 3-1. Phase 4 benefits from 3-1/3-2 having established
unboxed representations to speculate into. **Nothing in this phase should be started on a
box count again** — see §3.5's rules in [`Measurement.md`](Measurement.md#35-standing-measurement-lessons).
