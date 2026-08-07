# Phase 1 — the front end

Compile time: what it costs to turn JavaScript source into IL, before a line of it runs.
Blocker **B4**. This is the phase the engine roadmap had excluded, and the one with the
clearest value *outside* Octane — **it is page-load time**.

> The plan half of [`Phase-1.status.md`](Phase-1.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase is for

**Two of its three original targets were wrong, and running them is what proved it.** The
phase was aimed at MandreelLatency (4 646×), CodeLoad (371×) and Mandreel (300×) on the
reading that they measure the front end. Measured:

- **MandreelLatency measures no compilation at all.** Octane compiles `mandreel.js` at
  script load and times only `run`. Tripling compile speed moved neither score (0.993× and
  0.992×). It is an *execution-pause* benchmark and belongs to phase 3.
- **CodeLoad is about a quarter compilation**, not the whole of it.

What did move is outside every score: Mandreel's suite wall clock went **358.2 → 350.0 s**.
**So this phase's value is page-load time, and Octane is a poor instrument for it** —
Octane deliberately excludes load from what it times. Judge phase 1 on the compile probes
(`--compile-profile`, `--compile-scaling`), not on a suite score.

**Owner assemblies:** `Broiler.JavaScript.Parser`, `.Compiler`, `.BuiltIns`, and
**`.ExpressionCompiler`** — which 1-4 added, and which is where the phase's cost turned out
to be.

## The shape of the cost

The three-way split the phase was told to take exists as `--compile-scaling`. On **2 000
synthetic top-level declarations** it reads **parse ≈ 0.5%, tree construction ≈ 11%, IL
emission ≈ 89%**.

**That split is a fact about that shape and does not carry over.** On the real corpora,
deferring *all* nested-body IL emission removes only **17–36%** of compile time — tree
construction is a far larger share of a real program than of a wall of stubs. Use the
synthetic number for machine-generated declaration walls and the corpus number for
everything else.

**The phase splits in two, and not along its item numbering.** Mandreel is **wide** (1 364
top-level declarations in one scope — quadratic, that is 1-4) and jQuery is **deep** (532
functions nested in one IIFE, 96.5% of its compile in bodies never called — that is 1-1).

## Items

| # | Item | State | Size | Owner |
|---|---|---|---|---|
| **1-1** | **Lazy function compilation** — defer a function body's work until it is first invoked | ⚠️ **emission half landed; the deferral mechanism is open** and is the phase's remaining work | **L** | `.Compiler`, `.ExpressionCompiler` |
| **1-2** | Stop recursive compilation from overflowing the stack | ✅ **landed**, mitigation and real fix, on all three recursing passes | M | `.Parser`, `.Compiler` |
| **1-3** | Reduce compile cost per byte | 🔍 **open, and re-aimed** — measure before designing | ? | `.Parser`, `.Compiler` |
| **1-4** | The closure rewrite was quadratic in a scope's binding count | ✅ **landed — 3.04× on Mandreel** | S | `.ExpressionCompiler` |

### 1-1 · Lazy function compilation — **the phase's remaining work**

**What it is.** Do not compile a function body until something calls it. The population is
overwhelming: **84–99.7% of a script's functions are never invoked** once it has been
evaluated, and evaluating-and-stopping is exactly CodeLoad's shape and a page load's.

**What landed.** The *emission* half — IL generation deferred to first invocation. jQuery
**0.661×**, Box2D 0.636×, PdfJS 0.689×, allocation ~0.52× throughout, steady state 1.0009×,
and **CodeLoad 94.6 → 104.0 (1.099×)**. All four of the item's named risks are vacuous for
this half, because they are front-end properties and the front end still runs eagerly.

**What is left.** Deferring the **parse and tree construction** as well. On the real
corpora that is parse 9.4–13.5%, tree construction 33.6–63.9%, emission 25–57% — so the
remaining half is the larger one.

**What blocks it, precisely.** Not a pre-parser and not `EmitConstant`. The `Box[]` a
creation site passes **is** the capture mechanism; the obstacle is that its indices are
decided by `LambdaRewriter` from a tree the deferred body does not have.

**That obstacle is now priced rather than bounded.** The free-name map that makes the layout
addressable costs **6.6–12.2%** of body-tree construction as one bottom-up pass, and **up to
47.7%** written per-function, where the walk is superlinear in nesting depth. Mandreel —
wide, not deep — is the control that goes the other way (7.8% → 8.8%).

**And the cheap way in is closed off.** A site whose free names resolve to no enclosing
binding needs no `Box[]` and could be deferred today. That is **728 of 5 762 sites, 12.6%** —
39.7% on the flattest corpus and **7.4% on Mandreel**, i.e. worst exactly where the prize is
largest. `Dynamic` (the direct-`eval` risk the item leads with) refuses **7 sites of 5 762**.

**Next action.** Build the free-name map as **one bottom-up pass**, not per-function — the
measurement says the difference is five-fold — then defer parse and tree construction behind
it. Do not start from the capture-free population; it is 12.6% and it is smallest where it
would pay most.

**Do not re-derive this.** A spec-level fact about where a binding lives is not a fact about
where the compiler puts it: the reading that looked like an opening — Mandreel's 7 605 bound
free names being only 165 function-owned, because a top-level `var` is a global-object
property per spec — is refused by the counter built to test it. **`cellBacked` equals `bound`
exactly on all six corpora, 15 118 of 15 118.**

### 1-2 · Stop recursive compilation from overflowing — ✅ landed

Both halves. `StackGuard` repaired and put on `AstMapVisitor.Visit`; `FastParser.Expression`
guarded too, which was the last one — its descent aborted the process at 25 000 nesting
levels in the **default** configuration and now survives 90 000.

**One caveat worth keeping.** The four-way matrix's "mitigation off / guard on" row is a
**linux-x64** statement. On win-x64 the front end compiles in place on ~1 MiB while the
threshold is 4 MiB, so no segmenter can fire there.

### 1-3 · Reduce compile cost per byte — **measure first**

**Re-aimed, and its first task is a measurement rather than a change.** The synthetic split
(parse 0.5% / tree 11% / emission 89%) does not hold on real source. **Take the three-way
split on the real corpora before designing anything** — that is what 1-3 now is. It goes
after 1-1 because 1-1 changes the denominator.

### 1-4 · The quadratic closure rewrite — ✅ landed

The closure rewrite's per-lambda scope was a `List` asked `Contains` per parameter
reference, so **IL emission was quadratic in a scope's binding count**. Replaced with a
reference-keyed multiset, list-backed below 32 bindings: **28.5×** on 2 000 top-level
declarations, **3.04× on Mandreel** end-to-end, inside noise on narrow-scope corpora.

**Size: S — and it was found by measuring a different item.** That is the phase's most
transferable result and it is recorded in [`Measurement.md` §3.5](Measurement.md#35-standing-measurement-lessons).

**A second defect fell out of the same measurement and is fixed:** a closure subtree was
rewritten again at relay time although an enclosing walk had already rewritten it — worth
**0.782× on jQuery's whole compile and 0.867× on Typescript's**, six of six pairs each.

## Order

```
1-2 mitigation ✅ → 1-2 real fix ✅ → 1-4 ✅ → 1-1 emission half ✅
                                              → 1-1 remaining half (open, L)
                                              → 1-3 measure
```

**Start with 1-1's remaining half, not 1-3.** 1-3's own scope depends on what 1-1 removes.

## Exit gate

- test262 over the four pinned manifests: **no new failure and no new timeout**.
- MandreelLatency and CodeLoad out of the tail — **superseded as a criterion**: neither
  measures what this phase changes. Judge it on the compile probes and on suite wall clock,
  and say which.
- Everything closes under [`Measurement.md`](Measurement.md), unchanged.

## Dependencies

Independent of phase 2 and phase 5; they can run in parallel. 1-1's early-error surface is
the spec-visible risk — name the gating manifest when it ships.
